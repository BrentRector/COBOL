#!/usr/bin/env python3
"""Plan the next fix-lane wave deterministically, from a budget in weekly points.

    python scripts/orchestrator/plan_wave.py --budget-points 3 --dry-run          # --wave defaults to next
    python scripts/orchestrator/plan_wave.py --wave 1016 --from-budget --scratch <dir> [--reports <dir>]
    python scripts/orchestrator/plan_wave.py --cluster PB2108 --budget-points 3 --dry-run     # a campaign wave

Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 9 (kb/Work PB1981 item 3). Waves 1011-1015 each
cost the orchestrator a hand-written groups.json; this computes it.

1. AWAITING LANDING: an open note whose newest report is DONE and whose branch is not on main yet is excluded (the
   `land` unit lands it first; re-planning it would dispatch the same work twice).
2. FINISHERS FIRST: an open note whose newest report says SPLIT or NOT STARTED (its branch landed, gone, or still
   unlanded: the finisher then cherry-picks the branch's commits, because a `land` unit lands only DONE branches;
   kb/Work PB2575), a `status: half` note, and an UNLANDED branch (prune_worktrees.classify) whose commit subjects
   name an open note with no report. One finisher group per predecessor; its `pred` names the predecessor report and
   branch.
3. CLUSTERS from fix_clusters.py --json (the public submodule's view of kb/Work), minus the notes above, ranked by
   inventory rows claimed (the sum of each note's `inventory_rows`), then harm. A finisher on a cluster's primary
   file absorbs that cluster's notes up to the cap of five.
4. ONE GROUP PER PRIMARY FILE: a later group on the same file is a same-file successor (`after:`, MANDATORY-PRACTICES O2).
5. MODEL per group from model_rules.json (Opus for design-heavy files and areas, an open design question, or a note
   that already failed twice; Sonnet otherwise; never Fable).
6. COST per group from model_rules.json; groups are taken in rank order while they fit (a group that does not fit is
   skipped and a smaller later one may still fit; a successor only with its predecessor), plus one lander per train.
7. Codes (three per group) and lead-id blocks (five per train) come from alloc.py; --dry-run peeks and reserves
   nothing, and writes nothing.
8. A note whose body says it needs a frontier model under the owner's approval (model_rules.json
   routing.owner_approval_patterns) is never planned: it is listed as waiting (MANDATORY-PRACTICES P1).

--cluster LEAD plans a CAMPAIGN wave instead (design section 9.1, kb/Work PB2120): the notes are work.py's
cluster_order() of LEAD (every open note whose `cluster` names it, whatever its harm flags), clustered by the sites
they name anywhere in the tree (model_rules.json `campaign`), one blocked_by depth at a time; a note waiting on
another planned note runs after it as a successor (`after:`); everything else above is the same mechanism.
THE FILE-SET PARTITION (owner, kb/Work PB2118 question 5, 2026-10-07; R69 §2; DESIGN-architecture-review §8.7) is the
ONLY gate between a campaign's restructuring waves and the fix lane's trains: a campaign group whose file set (the
files its notes' sites resolve to, bare type names included, plus every file scripts/arch/member_index.py computes as
a caller of what each note moves or changes, on THE index of this tree, regenerated into <coord>/member-index when
stale: PB2118 Draft 8, J2; Draft 9, K1) meets a file of the in-flight work
(inflight_file_sets: every worktree's and every unlanded branch's changed files, committed or not, and every
dispatched group whose notes are still open, from the coordination directory's dispatch ledger, which a planned wave
and every hand dispatch the dispatch guard admits both write) or a file of the open external-repository slices' in-flight set
(work.slice_file_set, DESIGN-external-repository §12) is not planned: it waits, with the collision named, and the
train is never held for it. The in-flight set never reads a branch CLASSIFICATION: a dispatched branch with no commit
classifies MERGED and one that edits only existing files classifies LANDED, so a classification-filtered set missed
both (PB2118 Draft 7, the sixth refuter's partition lead). --no-branches skips the git half (a planning preview),
never the dispatch ledger or the slice half. This set is the ADMISSION estimate; the GUARANTEE is the landing check
(scripts/orchestrator/landing_check.py, run by push-main.sh): a landing whose ACTUAL diff leaves its declared set or
meets another in-flight branch's stops and re-plans (PB2118 Draft 9).

Without --dry-run it writes <scratch>/groups.json (make_dispatch_specs.py's format), runs make_dispatch_specs.py
(which renders the specs and runs check_practices.py), and writes <scratch>/wf-args-w<wave>.json, the args of
.claude/skills/workstream/templates/wf_rolling_wave.js. Exit 1 when check_practices.py is red.
"""
from __future__ import annotations

import argparse
import dataclasses
import importlib.util
import json
import math
import pathlib
import re
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "spec"))
import alloc  # noqa: E402
import coord  # noqa: E402
import work  # noqa: E402  (cluster_order: the ONE reader of cluster: and blocked_by:)
from work import parse_frontmatter  # noqa: E402  (the ONE frontmatter reader, with its C# parity twin)

REPO = coord.REPO
FIX_CLUSTERS = REPO / "tools" / "claude-skills" / "skills" / "agent-fleet" / "references" / "fix_clusters.py"
MAKE_SPECS = REPO / ".claude" / "skills" / "workstream" / "make_dispatch_specs.py"
PB = re.compile(r"\bPB\d+\b")
REPORT_NAME = re.compile(r"^w(?P<wave>\d+)(?P<letter>[a-z0-9]*)-(?P<lead>PB\d+)-report\.md$")
STATUS_WORD = re.compile(r"^\**(?:Status:\**)?\s*\**\s*(?P<s>DONE|SPLIT|DISCHARGED|BLOCKED|NOT STARTED)\b", re.I)
BRANCH = re.compile(r"\b(worktree-[A-Za-z0-9_][A-Za-z0-9_.\-]*[A-Za-z0-9_])")
WORKTREE = re.compile(r"([A-Za-z]:\\[^\s`*]*?\\worktrees\\[A-Za-z0-9_.\-]+)")
HEAD = re.compile(r"\bHEAD\b[:*`\s]*([0-9a-f]{7,40})\b|\bhead ([0-9a-f]{7,40})\b")
FINISH_STATUSES = {"SPLIT", "NOT STARTED"}
LANDED_CLASSES = {"MERGED", "LANDED", "ABSENT"}


# ── inputs ───────────────────────────────────────────────────────────────────────────────────────────────────────
@dataclasses.dataclass
class Note:
    id: str
    status: str
    area: str
    title: str
    rows: list[str]
    body: str
    harm: int = 0


@dataclasses.dataclass
class Report:
    path: pathlib.Path
    wave: int
    slug: str
    lead: str
    status: str
    ids: list[str]
    not_started: bool
    branch: str | None
    worktree: str | None
    head: str | None


def load_notes(work_dir: pathlib.Path, harm_weights: dict[str, int]) -> dict[str, Note]:
    notes = {}
    for p in sorted(work_dir.glob("PB*.md")):
        fm = parse_frontmatter(p.read_text(encoding="utf-8", errors="replace"))
        if not fm:
            continue
        nid = str(fm.get("id") or p.stem)
        rows = fm.get("inventory_rows") or []
        notes[nid] = Note(nid, str(fm.get("status", "")), str(fm.get("area", "")), str(fm.get("title", "")),
                          list(rows) if isinstance(rows, list) else [], str(fm.get("_body", "")),
                          sum(w for k, w in harm_weights.items() if fm.get(k) is True))
    return notes


def parse_report(p: pathlib.Path) -> Report | None:
    m = REPORT_NAME.match(p.name)
    if not m:
        return None
    lines = p.read_text(encoding="utf-8", errors="replace").splitlines()
    header = []
    for i, line in enumerate(lines):
        if i > 0 and line.startswith("## "):
            break
        header.append(line)
    text = "\n".join(header)
    status = next((s.group("s").upper() for l in header[:6] if (s := STATUS_WORD.match(l.strip()))), "")
    if not status and re.search(r"\bSPLIT\b", text):
        status = "SPLIT"
    h = HEAD.search(text)
    b, w = BRANCH.search(text), WORKTREE.search(text)
    ids = list(dict.fromkeys(PB.findall(text) + [m.group("lead")]))
    return Report(p, int(m.group("wave")), f"w{m.group('wave')}{m.group('letter')}", m.group("lead"), status, ids,
                  bool(re.search(r"(?i)not\s+started", text)), b.group(1) if b else None, w.group(1) if w else None,
                  (h.group(1) or h.group(2)) if h else None)


def load_reports(reports_dir: pathlib.Path | None) -> list[Report]:
    if not reports_dir or not reports_dir.is_dir():
        return []
    reps = [r for p in reports_dir.glob("w*-PB*-report.md") if (r := parse_report(p))]
    return sorted(reps, key=lambda r: (r.wave, r.path.stat().st_mtime))


def git_branch_classifier() -> Callable[[str], str]:
    """branch -> MERGED | LANDED | CHECK | UNLANDED | ABSENT, by prune_worktrees' content test (reused, not forked)."""
    import prune_worktrees  # noqa: PLC0415  (git work at import time is none, but the grep below is: only on demand)
    names: set[str] | None = None
    cache: dict[str, str] = {}

    def classify(branch: str) -> str:
        nonlocal names
        if branch not in cache:
            if prune_worktrees.git("rev-parse", "--verify", "--quiet", f"refs/heads/{branch}").returncode != 0:
                cache[branch] = "ABSENT"
            else:
                names = names if names is not None else prune_worktrees.main_names()
                cache[branch] = prune_worktrees.classify(branch, names)[0]
        return cache[branch]
    return classify


def unlanded_branch_notes(open_ids: set[str]) -> dict[str, list[str]]:
    """{UNLANDED or CHECK branch: [open note ids its commit subjects name]} over every local branch. CHECK (a C# name
    the branch declares is missing from main) is unlanded work until someone reads it: wave 1017's group B branch,
    which only edited existing files, classified CHECK, and its WIP would have been planned over from main."""
    import prune_worktrees  # noqa: PLC0415
    names = prune_worktrees.main_names()
    out = {}
    for b in prune_worktrees.git("branch", "--format=%(refname:short)").stdout.split():
        if b == "main" or prune_worktrees.classify(b, names)[0] not in ("UNLANDED", "CHECK"):
            continue
        subjects = prune_worktrees.git("log", "--format=%s", f"origin/main..{b}").stdout
        out[b] = sorted({i for i in PB.findall(subjects) if i in open_ids}, key=lambda s: int(s[2:]))
    return out


DISPATCH_LEDGER = "inflight-groups.json"
HAND_TTL_SECONDS = 24 * 3600   # a hand dispatch's entry outlives no agent: its worktree is source 1 after its first edit
_NOT_PRODUCT = ("STATUS.md", "TestResults/")


def _names(out: str) -> set[str]:
    return {s.strip() for s in out.splitlines() if s.strip() and not s.strip().startswith(_NOT_PRODUCT)}


def changed_files(git: Callable[..., Any], *revs: str, cwd: Any = None) -> set[str]:
    """THE changed-file reader of the partition (the planner's admission and the landing check): `git diff
    --name-only --no-renames <revs>`. A moved file counts at BOTH paths: git's default rename detection lists only the
    NEW path, so the old one (the path a declared set and every other branch name) dropped out and an R3 `git mv` of
    an undeclared file passed (PB2118 Draft 10, the ninth refuter's K1)."""
    return _names(git("diff", "--name-only", "--no-renames", *revs, **({} if cwd is None else {"cwd": cwd})).stdout)


def _still_differs(git: Callable[..., Any], rev: str, cwd: Any) -> set[str]:
    """The files `rev` changed since it left origin/main AND that still differ from origin/main: a branch landed by
    content (squash, cherry-pick) drops out; one whose edits are not on main stays, however it classifies."""
    since = changed_files(git, f"origin/main...{rev}", cwd=cwd)
    return since & changed_files(git, "origin/main", rev, cwd=cwd) if since else set()


def inflight_file_sets(*, git: Callable[..., Any] | None = None, repo: pathlib.Path = REPO,
                       coord_dir: pathlib.Path | None = None, terminal: Callable[[str], bool] | None = None,
                       with_git: bool = True, now: float | None = None,
                       meta: dict[str, dict] | None = None) -> dict[str, set[str]]:
    """{in-flight owner: the files it changes}: THE in-flight file set of the partition (module doc), read from
    sources that cannot miss started work, never from a branch classification (PB2118 Draft 7):
      1. every worktree but this checkout: `_still_differs(HEAD)` plus its uncommitted and untracked files (an agent's
         first edits precede its first commit);
      2. every local branch with no worktree whose tip is not on origin/main: `_still_differs(branch)`;
      3. every group in the dispatch ledger (<coord>/inflight-groups.json) with a note that is not terminal: the
         files the group was planned on, which exist before the agent has a worktree or a commit. A planned wave
         writes it (record_dispatch), and so does every HAND dispatch the dispatch guard admits
         (record_hand_dispatch: an attended Fable or Mythos author, a direct Agent call), which Draft 7 missed until
         its worktree changed a file (the seventh refuter, J3); a hand entry expires after HAND_TTL_SECONDS.
    `git` has prune_worktrees.git's shape (*args, cwd=); `terminal(note_id)` says whether a note is landed or retired.
    `meta`, when given, receives per owner what the landing check (landing_check.py) needs to order two branches: a
    worktree's or branch's `rev`, `path` and `branch` (and a worktree's `uncommitted` files), a dispatch entry's
    `notes` and `at`. The per-worktree git work runs on a thread pool (it is subprocess-bound: the ninth refuter
    measured 64 s, about 2 s a worktree, for 33 worktrees run one after another)."""
    out: dict[str, set[str]] = {}
    if with_git:
        if git is None:
            import prune_worktrees  # noqa: PLC0415
            git = prune_worktrees.git
        here = str(repo).replace(chr(92), "/").rstrip("/").lower()
        with_tree, trees = set(), []
        blocks = git("worktree", "list", "--porcelain", cwd=repo).stdout.split(chr(10) * 2)
        for blk in [b for b in blocks if b.strip()]:
            path = re.search(r"^worktree (.+)$", blk, re.M).group(1).strip()
            br = re.search(r"^branch refs/heads/(.+)$", blk, re.M)
            if br:
                with_tree.add(br.group(1))
            if path.replace(chr(92), "/").rstrip("/").lower() != here:
                trees.append((path, br.group(1) if br else None))

        def tree(t: tuple[str, str | None]) -> tuple[str, str | None, set[str], set[str], str]:
            path, br = t
            loose = changed_files(git, "HEAD", cwd=path)
            loose |= _names(git("ls-files", "--others", "--exclude-standard", cwd=path).stdout)
            head = git("rev-parse", "HEAD", cwd=path).stdout.strip() if meta is not None else ""
            return path, br, _still_differs(git, "HEAD", path), loose, head

        def branch(b: str) -> tuple[str, set[str]]:
            if git("merge-base", "--is-ancestor", b, "origin/main", cwd=repo).returncode == 0:
                return b, set()
            return b, _still_differs(git, b, repo)

        loose_branches = [b for b in git("branch", "--format=%(refname:short)", cwd=repo).stdout.split()
                          if b != "main" and b not in with_tree]
        with ThreadPoolExecutor(max_workers=8) as pool:
            tree_rows, branch_rows = list(pool.map(tree, trees)), list(pool.map(branch, loose_branches))
        for path, br, files, loose, head in tree_rows:
            if files | loose:
                owner = f"worktree {pathlib.PurePath(path).name} ({br or 'detached'})"
                out[owner] = files | loose
                if meta is not None:
                    meta[owner] = {"rev": head, "uncommitted": loose, "path": path, "branch": br}
        for b, files in branch_rows:
            if files:
                out[f"branch {b}"] = files
                if meta is not None:
                    meta[f"branch {b}"] = {"rev": b, "uncommitted": set(), "path": None, "branch": b}
    ledger = coord.read_json((coord_dir or coord.coord_dir()) / DISPATCH_LEDGER, {"groups": []})
    for g in ledger.get("groups", []):
        if terminal is not None and all(terminal(i) for i in g.get("notes", [])):
            continue
        if g.get("hand") and (now if now is not None else time.time()) - g.get("at", 0) > HAND_TTL_SECONDS:
            continue
        if files := {f.replace(chr(92), "/") for f in g.get("files", [])}:
            owner = f"dispatched w{g.get('wave')}{g.get('letter', '')} ({', '.join(g.get('notes', [])[:3])})"
            out[owner] = files
            if meta is not None:
                meta[owner] = {"notes": list(g.get("notes", [])), "at": g.get("at", 0.0)}
    return out


def record_dispatch(coord_dir: pathlib.Path, wave: str, groups: list[Any], terminal: Callable[[str], bool]) -> None:
    """Append a wave planned for dispatch to the dispatch ledger and drop the entries whose notes are all terminal,
    so the ledger holds exactly the dispatched work still in flight (inflight_file_sets, source 3)."""
    path = coord_dir / DISPATCH_LEDGER
    ledger = coord.read_json(path, {"groups": []})
    keep = [g for g in ledger.get("groups", []) if not all(terminal(i) for i in g.get("notes", []))]
    at = time.time()   # the dispatch order the landing check reads (the later of two overlapping branches re-plans):
    #   one millisecond apart in letter order, so two groups of one wave never tie (a tie left neither "later" and both
    #   proceeded with a warning: the ninth refuter's N4)
    keep += [{"wave": wave, "letter": g.letter, "notes": list(g.notes), "files": sorted(g.files), "at": at + k / 1000}
             for k, g in enumerate(groups)]
    coord.write_json(path, {"groups": keep})


def release_dispatch(coord_dir: pathlib.Path, selector: str) -> list[dict[str, Any]]:
    """Drop the dispatch-ledger entries `selector` names (a wave `1040`, a group `1040a`, or a note id `PB2249`) and
    return them. A wave entry never expires on a clock (an expired entry would release the files of a wave still
    running), so an ABORTED dispatch is released here, by hand, explicitly (the ninth refuter's N5)."""
    path = coord_dir / DISPATCH_LEDGER
    groups = coord.read_json(path, {"groups": []}).get("groups", [])
    if re.fullmatch(r"PB\d+", selector):
        hit = lambda g: selector in g.get("notes", [])  # noqa: E731
    else:
        hit = lambda g: selector in (str(g.get("wave")), f"{g.get('wave')}{g.get('letter', '')}")  # noqa: E731
    gone = [g for g in groups if hit(g)]
    coord.write_json(path, {"groups": [g for g in groups if not hit(g)]})
    return gone


def note_file_set(text: str, fc: Any, idx: dict, exts: list[str], callers: Callable[[str], set[str]] | None) -> set[str]:
    """ONE note's file set: every site its text names (paths, bare file names, Type.Member and bare type names,
    whatever the score) plus the callers the member index computes for it (PB2118 Draft 8, J2)."""
    return set(fc.sites(text, idx, exts)) | (callers(text) if callers else set())


def member_callers(repo: pathlib.Path = REPO, cache_dir: pathlib.Path | None = None,
                   regenerate: bool = True) -> Callable[[str], set[str]]:
    """scripts/arch/member_index.py's computation as a function of a note's text (the index loaded and the drifted
    files read once). With `cache_dir` (the planner) the index is THE index of this tree (member_index.current: found,
    regenerated into `cache_dir`, or refused when `regenerate` is off): a stale index cannot see a file or type a
    landing added (PB2118 Draft 9, the eighth refuter's K1 (3)). Without it (a hand dispatch's record, which a hook's
    90 seconds bound) the newest committed index plus drift and folder expansion. A missing index is a refusal."""
    sys.path.insert(0, str(HERE.parent / "arch"))   # this script's own member_index, whatever tree it measures
    import member_index  # noqa: PLC0415
    index = (member_index.current(repo, cache_dir, regenerate) if cache_dir is not None
             else member_index.load(repo))
    drifted = member_index.drifted_files(index, repo)
    return lambda text: set(member_index.note_file_set(text, repo, index, drifted)["files"])


def record_hand_dispatch(coord_dir: pathlib.Path, label: str, note_ids: list[str], files: set[str],
                         terminal: Callable[[str], bool], now: float | None = None) -> None:
    """A hand dispatch's notes and file set into the dispatch ledger (inflight_file_sets, source 3), so the planner
    sees it before its agent edits a file (PB2118 Draft 8, J3). scripts/hooks/dispatch_guard.py calls this (through
    --record-dispatch) for every write-capable Agent call it admits."""
    path = coord_dir / DISPATCH_LEDGER
    ledger = coord.read_json(path, {"groups": []})
    keep = [g for g in ledger.get("groups", []) if not all(terminal(i) for i in g.get("notes", []))]
    keep.append({"wave": "hand", "letter": "", "label": label, "hand": True, "at": now if now is not None else time.time(),
                 "notes": list(note_ids), "files": sorted(files)})
    coord.write_json(path, {"groups": keep})


def file_set_collisions(files: list[str] | set[str], busy: dict[str, set[str]]) -> list[str]:
    """The in-flight file sets a group's files meet, each as '<owner> (<first file>)': the ONE collision rule of the
    file-set partition (kb/Work PB2118 question 5). Paths are repo-relative POSIX."""
    mine = {f.replace(chr(92), "/") for f in files}
    return [f"{owner} ({sorted(mine & theirs)[0]})" for owner, theirs in sorted(busy.items()) if mine & theirs]


def load_fix_clusters() -> Any:
    """The public submodule's fix_clusters.py as a module: a campaign reuses its site index and clustering (never a
    fork of them) over a wider tree than .agent-fleet.json gives the fix lane."""
    if not FIX_CLUSTERS.exists():
        raise SystemExit(f"⛔ {FIX_CLUSTERS} is missing: run `git submodule update --init tools/claude-skills`")
    spec = importlib.util.spec_from_file_location("fix_clusters", FIX_CLUSTERS)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def campaign_clusters(view: dict[str, Any], notes: dict[str, Note], texts: dict[str, str], rules: dict[str, Any],
                      fc: Any, repo: pathlib.Path, callers: Callable[[str], set[str]] | None = None
                      ) -> tuple[list[dict], list[dict], dict[str, list[str]], dict[str, str]]:
    """A campaign's plannable notes as fix_clusters-shaped clusters: `(open, half, deps, waiting)`.

    `view` is work.cluster_order(): every open note of the cluster in blocked_by order. A note is PLANNABLE when it is
    ready, or when every note it waits on is plannable (then plan() runs it after them, `deps`); every other note is
    `waiting`, with the reason. Plannable notes are clustered by the code sites their text names (fix_clusters'
    `sites` and `cluster`, over model_rules.json `campaign.src` / `ext` / `exclude_dir`), one topological depth at a
    time so a cluster never mixes a blocker with what it blocks; a note naming no site is a group of its own, ordered
    by its blocked_by chain. A cluster's `files`, the set the partition checks, is every site of its notes whatever
    its score (fix_clusters keeps only scores of 2 and up, which dropped a bare type name: the seventh refuter, J3)
    plus each note's computed callers (`callers(text)`, member_index; PB2118 Draft 8, J2), which never steer the
    clustering itself. The clusters holding one dependent's blockers merge when they share a depth and fit the
    cap, so the dependent can follow one successor chain. `texts` is {note: its file text}."""
    cfg, cap = rules["campaign"], rules["wave"]["max_notes_per_group"]
    plannable: dict[str, dict] = {}
    waiting: dict[str, str] = {}
    for r in view["notes"]:  # topological order: every blocker is decided before what it blocks
        if r["id"] not in notes or r["id"] not in texts:
            waiting[r["id"]] = "not a kb/Work/PB*.md note: the wave planner dispatches PB notes only"
        elif all(w in plannable for w in r["waiting_on"]):
            plannable[r["id"]] = r
        else:
            waiting[r["id"]] = "waiting on " + ", ".join(w for w in r["waiting_on"] if w not in plannable)
    exts = [e.strip() for e in cfg["ext"].split(",") if e.strip()]
    roots = [repo / s for s in cfg["src"] if (repo / s).is_dir()]
    idx = fc.source_index(roots, exts, cfg["exclude_dir"], repo) if plannable else {}
    tiers: dict[str, dict[int, list[dict]]] = {"open": {}, "half": {}}
    for nid, r in plannable.items():
        entry = {"id": nid, "area": r["area"], "harm": notes[nid].harm, "title": r["title"][:140],
                 "sites": fc.sites(texts[nid], idx, exts),
                 "fileset": note_file_set(texts[nid], fc, idx, exts, callers)}
        tiers["half" if r["status"] == "half" else "open"].setdefault(r["depth"], []).append(entry)
    out: dict[str, list[dict]] = {"open": [], "half": []}
    for key, by_depth in tiers.items():
        for d in sorted(by_depth):
            ns = by_depth[d]
            cl = fc.cluster([n for n in ns if n["sites"]], cap)
            cl += [{"file": "", "harm": n["harm"], "files": [], "notes": [n]} for n in ns if not n["sites"]]
            for c in cl:
                c["files"] = sorted(set().union(*(n["fileset"] for n in c["notes"])) | ({c["file"]} - {""}))
                c["depth"] = d
                c["notes"] = [{k: n[k] for k in ("id", "area", "harm", "title")} for n in c["notes"]]
            out[key] += cl
    deps = {nid: list(r["waiting_on"]) for nid, r in plannable.items() if r["waiting_on"]}
    # A successor follows ONE chain, so a note whose blockers fell into two clusters of one depth would wait a whole
    # wave. Merge those clusters while they fit the cap: wave 1025 was planned so by hand (PB2108 + PB2109 in one
    # group, PB2110 after it).
    for nid in deps:  # topological order
        holders = [c for c in out["open"] if any(n["id"] in deps[nid] for n in c["notes"])]
        if len(holders) < 2 or len({c["depth"] for c in holders}) > 1 or sum(len(c["notes"]) for c in holders) > cap:
            continue
        head = holders[0]
        for c in holders[1:]:
            head["notes"] += c["notes"]
            head["harm"] += c["harm"]
            head["files"] = sorted(set(head["files"]) | set(c["files"]))
            head["file"] = head["file"] or c["file"]
            out["open"].remove(c)
    return out["open"], out["half"], deps, waiting


# ── planning ─────────────────────────────────────────────────────────────────────────────────────────────────────
@dataclasses.dataclass
class Group:
    kind: str                      # finisher | cluster
    notes: list[str]
    file: str                      # primary file ('' when unknown)
    files: list[str]
    pred: str = ""
    letter: str = ""
    after: str = ""
    model: str = ""
    why_model: str = ""
    cost: float = 0.0
    blocked_by: list[str] = dataclasses.field(default_factory=list)   # campaign: open blockers planned before it


def letters():
    n = 0
    while True:
        n += 1
        s, k = "", n
        while k:
            k, r = divmod(k - 1, 26)
            s = chr(65 + r) + s
        yield s


def attempts(nid: str, reports: list[Report]) -> int:
    return sum(1 for r in reports if nid in r.ids)


def choose_model(g: Group, notes: dict[str, Note], reports: list[Report], rules: dict[str, Any]) -> tuple[str, str]:
    rt = rules["routing"]
    ow = rt["opus_when"]
    for f in [g.file, *g.files]:
        for pat in ow["file_patterns"]:
            if f and pat in f:
                return "opus", f"design-heavy file {f} ({pat})"
    for nid in g.notes:
        n = notes[nid]
        for pat in ow["area_patterns"]:
            if re.search(pat, n.area, re.I):
                return "opus", f"{nid} area {n.area}"
        for pat in ow["body_patterns"]:
            if re.search(pat, n.body):
                return "opus", f"{nid}: an open design question ({pat})"
        if attempts(nid, reports) >= ow["failed_attempts_at_least"]:
            return "opus", f"{nid} already attempted {attempts(nid, reports)} times without landing"
    model = rt["default"]
    if model not in rt["allowed"]:
        raise SystemExit(f"⛔ model_rules.json routing.default {model!r} is not in routing.allowed")
    return model, "default"


def group_cost(model: str, n_notes: int, rules: dict[str, Any]) -> float:
    c, cal = rules["cost"], rules["calibration"]
    return (c["per_group_tokens"][model] + c["per_note_tokens"][model] * n_notes) / cal["tokens_per_point"][model]


def lander_cost(rules: dict[str, Any]) -> float:
    c = rules["cost"]
    return c["lander_tokens"] / rules["calibration"]["tokens_per_point"][c["lander_model"]]


def branch_resume(branch: str, nid_list: list[str]) -> str:
    return (f"Branch {branch} carries unlanded work for {', '.join(nid_list)} with no report. Run "
            f"`python tools/claude-skills/skills/agent-fleet/references/status_delta.py <its worktree>` if the "
            f"worktree still exists, read its STATUS.md, cherry-pick its commits onto current main (never merge the "
            f"branch), resolve conflicts against the spec, re-gate, then finish the notes at their roots.")


def finisher_pred(rep: Report | None, branch: str | None, cls: str, nid_list: list[str]) -> str:
    if rep is None:
        return "FINISH-FIRST: " + branch_resume(branch or "?", nid_list)
    where = (f"Its branch {rep.branch} (worktree {rep.worktree or 'removed'}, head {rep.head or '?'}) is {cls}: "
             + ("cherry-pick its unlanded commits onto current main (never merge the branch) and re-gate before new work."
                if cls in ("UNLANDED", "CHECK") else
                "its landed part is already on main, so orient from the report only."))
    return (f"FINISH-FIRST: predecessor {rep.slug} ({rep.lead}) returned {rep.status or 'an unfinished report'} for "
            f"{', '.join(nid_list)}. Read {rep.path} first, its NEXT and its \"For the next implementer\" section, and "
            f"run `python tools/claude-skills/skills/agent-fleet/references/status_delta.py <its worktree>` when the "
            f"worktree still exists. {where}")


def plan(notes: dict[str, Note], clusters: list[dict[str, Any]], half_clusters: list[dict[str, Any]],
         reports: list[Report], classify: Callable[[str], str], unlanded: dict[str, list[str]],
         rules: dict[str, Any], budget_points: float, max_groups: int | None = None,
         deps: dict[str, list[str]] | None = None, busy: dict[str, set[str]] | None = None) -> dict[str, Any]:
    """`clusters` and `half_clusters` are fix_clusters.py --json views of the open and the half notes; they also
    define which notes are plannable at all (fix_clusters applies .agent-fleet.json's kind and skip flags).

    A CAMPAIGN (`--cluster`, kb/Work PB2120) passes `deps`, {note: its open blockers}, and clusters that carry a
    `depth` (their place in the blocked_by topology, ranked first). A group whose notes have blockers runs AFTER the
    group that holds them: it joins the tail of that group's successor chain (`after:`), as a same-file successor
    does, because a successor merges its predecessor's branch and the rolling wave lands a chain through its last
    group. A group whose blockers are planned in no group, or in two chains, waits for a later wave (`waiting`).
    A campaign also passes `busy`, {in-flight owner: the files it changes}: a group whose files meet one waits
    (file_set_collisions, the file-set partition), and so does every group that would follow it.
    Without `deps` nothing here changes the fix lane's plan (test_plan_wave.py check 8 holds it to a golden)."""
    wave_rules = rules["wave"]
    cap = wave_rules["max_notes_per_group"]
    open_ids = {n["id"] for c in clusters + half_clusters for n in c["notes"] if n["id"] in notes}
    note_file = {n["id"]: c["file"] for c in clusters + half_clusters for n in c["notes"]}
    note_files = {n["id"]: c.get("files", []) for c in clusters + half_clusters for n in c["notes"]}

    newest: dict[str, Report] = {}
    for r in reports:  # sorted oldest first, so the last write wins
        for i in r.ids:
            if i in open_ids:
                newest[i] = r

    awaiting: dict[str, list[str]] = {}
    finisher_by_rep: dict[pathlib.Path, list[str]] = {}
    for nid, r in newest.items():
        cls = classify(r.branch) if r.branch else "ABSENT"
        # A SPLIT or NOT STARTED report is unfinished whatever its branch's state: a `land` unit lands only DONE
        # branches (units/land.md), so holding an unlanded SPLIT branch for one stranded it (kb/Work PB2575). Its
        # finisher cherry-picks the branch's commits (finisher_pred's UNLANDED arm). A DONE report that names a
        # note NOT started still lands first: its finished work is the land unit's.
        if r.status in FINISH_STATUSES or (cls in LANDED_CLASSES and r.not_started):
            finisher_by_rep.setdefault(r.path, []).append(nid)
        elif cls not in LANDED_CLASSES:
            awaiting.setdefault(r.branch or str(r.path), []).append(nid)

    taken: set[str] = {i for ids in awaiting.values() for i in ids}
    # A note that needs a frontier model under the owner's per-dispatch approval is never planned (MANDATORY-PRACTICES
    # P1): an unattended wave cannot ask, and routing it to Opus would pre-empt the owner's choice.
    waiting: dict[str, str] = {}
    for nid in sorted(open_ids - taken, key=work.id_order):
        if any(re.search(pat, notes[nid].body) for pat in rules["routing"]["owner_approval_patterns"]):
            waiting[nid] = "needs the owner's approval of a frontier model for this dispatch (P1): ask in the attended session"
            taken.add(nid)
    groups: list[Group] = []
    rep_by_path = {r.path: r for r in reports}
    for path, ids in finisher_by_rep.items():
        rep = rep_by_path[path]
        ids = sorted(set(ids) - taken, key=lambda s: int(s[2:]))
        for k in range(0, len(ids), cap):
            chunk = ids[k:k + cap]
            groups.append(Group("finisher", chunk, note_file.get(chunk[0], ""),
                                sorted({f for i in chunk for f in note_files.get(i, [])}),
                                pred=finisher_pred(rep, rep.branch, classify(rep.branch) if rep.branch else "ABSENT", chunk)))
            taken |= set(chunk)
    # A half note may also sit on an unlanded branch (a killed wave's WIP): its group carries that branch's resume
    # instruction too, or the finisher starts over from main and the branch's work is lost (wave 1018's plan).
    branch_of = {i: b for b, ids in sorted(unlanded.items()) for i in ids if i not in newest}
    for c in half_clusters:  # `status: half` notes, clustered by fix_clusters.py --open-status half
        chunk = [n["id"] for n in c["notes"] if n["id"] not in taken][:cap]
        if chunk:
            on_branch = {b: [i for i in unlanded[b] if i in chunk]
                         for b in sorted({branch_of[i] for i in chunk if i in branch_of})}
            groups.append(Group("finisher", chunk, c["file"], list(c.get("files", [])),
                                pred=("FINISH-FIRST: " + ", ".join(chunk) + (" is" if len(chunk) == 1 else " are") +
                                      " `status: half`: read each note's record of the landed half and its kb/Work "
                                      "history before touching code; finish the open half at its root." +
                                      "".join(" " + branch_resume(b, ids) for b, ids in on_branch.items()))))
            taken |= set(chunk)
    for branch, ids in sorted(unlanded.items()):
        ids = [i for i in ids if i not in taken and i not in newest]
        if ids:
            groups.append(Group("finisher", ids[:cap], note_file.get(ids[0], ""),
                                sorted({f for i in ids[:cap] for f in note_files.get(i, [])}),
                                pred=finisher_pred(None, branch, "UNLANDED", ids[:cap])))
            taken |= set(ids[:cap])

    def rows(ids: list[str]) -> int:
        return sum(len(notes[i].rows) for i in ids if i in notes)

    ranked = []
    for c in clusters:
        ids = [n["id"] for n in c["notes"] if n["id"] in open_ids and n["id"] not in taken]
        if ids:
            ranked.append((c, ids))
    # a campaign's clusters carry their topological depth, which ranks first; the fix lane's carry none (all 0)
    ranked.sort(key=lambda ci: (ci[0].get("depth", 0), -rows(ci[1]), -ci[0].get("harm", 0)))
    # A finisher on a cluster's primary file absorbs that cluster's notes up to the cap (one orientation). Never a
    # campaign cluster past depth 0: its notes wait on blockers planned after the finisher, which would hold it back.
    for g in groups:
        for c, ids in ranked:
            if g.file and c["file"] == g.file and len(g.notes) < cap and not c.get("depth"):
                room = cap - len(g.notes)
                g.notes += ids[:room]
                g.files = sorted(set(g.files) | set(c.get("files", [])))
                del ids[:room]
    groups += [Group("cluster", ids, c["file"], list(c.get("files", []))) for c, ids in ranked if ids]

    # Letters, successors, models, costs. A successor chain is linear (A, A2, A3: each merges the one before it), so
    # a group that must follow another joins the TAIL of that one's chain. In the fix lane the only link is a shared
    # primary file, whose chain tail is always the file's last group, so this is the same-file rule unchanged.
    gen = letters()
    last_on_file: dict[str, Group] = {}
    chain_of: dict[int, int] = {}            # id(group) -> id(its chain's head)
    tail: dict[int, Group] = {}              # id(chain head) -> the chain's last group
    group_of: dict[str, Group] = {}          # note -> the group planned for it
    placed: list[Group] = []
    for g in groups:
        links = [last_on_file[g.file]] if g.file and g.file in last_on_file else []
        if busy and (hit := file_set_collisions(g.files, busy)):
            for i in g.notes:
                waiting[i] = "file-set collision with " + "; ".join(hit) + ": the partition defers the wave, never the train"
            continue
        if deps:
            g.blocked_by = sorted({b for i in g.notes for b in deps.get(i, [])} - set(g.notes), key=work.id_order)
            unplanned = [b for b in g.blocked_by if b not in group_of]
            if unplanned:
                for i in g.notes:
                    waiting[i] = f"blocker {', '.join(unplanned)} is not planned in this wave"
                continue
            links += [group_of[b] for b in g.blocked_by]
        chains = {chain_of[id(x)] for x in links}
        if len(chains) > 1:
            for i in g.notes:
                waiting[i] = ("its blockers and its file are in different successor chains ("
                              + ", ".join(sorted(tail[c].letter for c in chains)) + "): a later wave")
            continue
        prev = tail[chains.pop()] if chains else None
        if prev:
            base = re.sub(r"\d+$", "", prev.letter)
            g.letter = f"{base}{int(prev.letter[len(base):] or 1) + 1}"
            g.after = prev.letter
            chain_of[id(g)] = chain_of[id(prev)]
        else:
            g.letter = next(gen)
            chain_of[id(g)] = id(g)
        tail[chain_of[id(g)]] = g
        if g.file:
            last_on_file[g.file] = g
        for i in g.notes:
            group_of[i] = g
        g.model, g.why_model = choose_model(g, notes, reports, rules)
        g.cost = group_cost(g.model, len(g.notes), rules)
        placed.append(g)
    groups = placed

    # Fill the budget in rank order (finishers first); one lander per train.
    chosen: list[Group] = []
    skipped: list[Group] = []
    spent = 0.0
    train = wave_rules["train_size"]
    for g in groups:
        if max_groups is not None and len(chosen) >= max_groups:
            skipped.append(g)
            continue
        if g.after and g.after not in {c.letter for c in chosen}:
            skipped.append(g)
            continue
        landers_after = math.ceil((len(chosen) + 1) / train) - math.ceil(len(chosen) / train)
        need = g.cost + landers_after * lander_cost(rules)
        if spent + need > budget_points + 1e-9:
            skipped.append(g)
            continue
        chosen.append(g)
        spent += need
    return {"groups": chosen, "skipped": skipped, "awaiting_landing": awaiting, "points": spent,
            "trains": math.ceil(len(chosen) / train) if chosen else 0,
            "unlanded_without_note": sorted(b for b, ids in unlanded.items() if not ids), "waiting": waiting}


# ── rendering ────────────────────────────────────────────────────────────────────────────────────────────────────
def title_of(g: Group) -> str:
    stem = pathlib.Path(g.file).name.split(".")[0] if g.file else ""
    words = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", stem).upper() if stem else "NOTES WITHOUT A COMMON FILE"
    return ("FINISH: " if g.kind == "finisher" else "") + words


def body_of(g: Group, notes: dict[str, Note]) -> str:
    parts = []
    for nid in g.notes:
        n = notes[nid]
        title = re.sub(rf"^{nid}\s*[—-]\s*", "", n.title)[:220]
        parts.append(f"{nid} ({len(n.rows)} inventory rows claimed): {title}")
    sites = ", ".join(g.files[:8]) if g.files else "see the notes"
    tail = ("Re-probe each note on your build, fix each at its root, sweep the sibling arms, and write closes_rows for "
            "every row the landing really closes.")
    if g.model == "opus" and g.why_model != "default":
        tail += f" Routed to Opus: {g.why_model}; settle any open design question against its design doc first."
    if g.blocked_by:
        tail += (f" ⛔ DEPENDENCY SUCCESSOR (blocked_by, kb/Work PB2120): these notes are blocked by "
                 f"{', '.join(g.blocked_by)}, which group {g.after}'s chain does in this wave. Build on that work, never "
                 f"around it: merge the predecessor branch the wave names before touching code, and if it did not "
                 f"finish its blocker, stop and return BLOCKED naming it.")
    return "NOTES: " + " · ".join(parts) + f" CODE SITES the notes name: {sites}. {tail}"


def as_group_json(g: Group, wave: str, notes: dict[str, Note], codes: str) -> dict[str, Any]:
    lead = max(g.notes, key=lambda i: (len(notes[i].rows), -g.notes.index(i)))
    out = {"letter": g.letter, "model": g.model, "slug": f"w{wave}{g.letter.lower()}", "group": title_of(g),
           "notes": ", ".join(g.notes), "lead": lead, "codes": codes,
           "root": (g.file + (" (also " + ", ".join(f for f in g.files if f != g.file)[:300] + ")"
                              if len(g.files) > 1 else "")) if g.file else "the files the notes name",
           "files": ", ".join(f"`kb/Work/{i}.md`" for i in g.notes), "body": body_of(g, notes)}
    if g.pred:
        out["pred"] = g.pred
    if g.after:
        out["after"] = g.after
    return out


def previous_train(repo: pathlib.Path) -> str:
    with open(repo / "DEVLOG.md", encoding="utf-8", errors="replace") as f:
        for line in f:
            m = re.match(r"^## Entry \d+ .*?\bTrain (\d+[a-z]?)", line)
            if m:
                return f"train {m.group(1)}"
    return "the previous train (see DEVLOG.md)"


def next_wave(repo: pathlib.Path, reports: list[Report]) -> int:
    """One past the highest wave OR train number in the reports directory or the newest 200 DEVLOG lines. A wave's
    first train is labelled with the wave number, so a train label counts as taken too (kb/Work PB2562: trains 1035
    and 1036 came from waves 1031 and 1032, the planner proposed wave 1035, and "Train 1034" had already landed twice)."""
    seen = [r.wave for r in reports]
    with open(repo / "DEVLOG.md", encoding="utf-8", errors="replace") as f:
        for _, line in zip(range(200), f):
            seen += [int(w) for w in re.findall(r"\b(?:[Ww]aves?|[Tt]rains?) (\d{3,})", line)]
    return max(seen, default=0) + 1


def record_dispatch_main(a: argparse.Namespace) -> int:
    """--record-dispatch: the named notes that are open register notes, with their computed file sets, into the
    dispatch ledger. Unknown or terminal notes are skipped; nothing to record is not an error."""
    items = {i.get("id"): i for i in work.load()}
    ids = [i for i in dict.fromkeys(a.record_dispatch.split(",")) if i in items
           and items[i].get("status") not in work.TERMINAL_STATUSES]
    if not ids:
        print("=== DISPATCH LEDGER: nothing to record (no open note named) ===")
        return 0
    rules = coord.rules()
    cfg = rules["campaign"]
    fc = load_fix_clusters()
    exts = [e.strip() for e in cfg["ext"].split(",") if e.strip()]
    idx = fc.source_index([REPO / s for s in cfg["src"] if (REPO / s).is_dir()], exts, cfg["exclude_dir"], REPO)
    callers = member_callers(REPO)
    files: set[str] = set()
    for i in ids:
        files |= note_file_set((REPO / "kb" / "Work" / items[i]["_file"]).read_text(encoding="utf-8", errors="replace"),
                               fc, idx, exts, callers)
    record_hand_dispatch(coord.coord_dir(a.coord), a.label, ids, files,
                         lambda n: (items.get(n) or {}).get("status") in work.TERMINAL_STATUSES)
    print(f"=== DISPATCH LEDGER: recorded {a.label}: {', '.join(ids)} ({len(files)} files) ===")
    return 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--wave", default="next", help="the wave number, or next (default): one past the highest seen")
    b = ap.add_mutually_exclusive_group()
    b.add_argument("--budget-points", type=float, help="weekly points this wave may spend")
    b.add_argument("--from-budget", action="store_true", help="use budget.py's headroom_pct")
    ap.add_argument("--borrow-days", type=int, default=0, help="with --from-budget")
    ap.add_argument("--scratch", help="where groups.json, the specs and the args go (required without --dry-run)")
    ap.add_argument("--reports", help="the reports directory (default <scratch>/reports)")
    ap.add_argument("--stop-file", help="this fleet's own graceful-stop file (default <scratch>/STOP-w<wave>; the loop's "
                    "wave unit passes the supervisor's <scratch>/STOP-loop). Never the owner's global STOP (kb/Work PB2483)")
    ap.add_argument("--clusters-json", help="a fix_clusters.py --json output (open notes) to use instead of running it")
    ap.add_argument("--half-clusters-json", help="the same for --open-status half")
    ap.add_argument("--no-branches", action="store_true", help="skip the UNLANDED-branch scan and treat every "
                    "report branch as landed (fast; for a planning preview only)")
    ap.add_argument("--max-groups", type=int)
    ap.add_argument("--authorization", default=("the owner's standing opt-in for running fleets through the Workflow "
                                                "tool (MANDATORY-PRACTICES O4) and the owner's direction of 2026-10-04 "
                                                "to run the fix lane through the orchestrator loop (kb/Work PB1981)"))
    ap.add_argument("--cluster", metavar="LEAD", help="plan a CAMPAIGN wave from the open notes whose `cluster` names "
                    "LEAD (work.py next --cluster), whatever their harm flags, instead of the fix lane's clusters")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--record-dispatch", metavar="IDS", help="record a HAND dispatch of these comma-separated notes in "
                    "the dispatch ledger (their computed file sets) and exit: scripts/hooks/dispatch_guard.py runs it")
    ap.add_argument("--release", metavar="SEL", help="drop the dispatch-ledger entries of an ABORTED dispatch and exit: "
                    "a wave (1040), a group (1040a) or a note id (PB2249); exit 1 when nothing matched")
    ap.add_argument("--label", default="hand dispatch", help="with --record-dispatch: who was dispatched")
    ap.add_argument("--coord", help=f"coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    ap.add_argument("--no-index-regenerate", action="store_true", help="with --cluster: refuse (never regenerate) "
                    "when no member index describes this tree (scripts/arch/member_index.py current)")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.record_dispatch:
        return record_dispatch_main(a)
    if a.release:
        gone = release_dispatch(coord.coord_dir(a.coord), a.release)
        for g in gone:
            print(f"released w{g.get('wave')}{g.get('letter', '')} ({', '.join(g.get('notes', []))}): "
                  f"{len(g.get('files', []))} files")
        print(f"=== DISPATCH LEDGER: released {len(gone)} entr{'y' if len(gone) == 1 else 'ies'} ({a.release}) ===")
        return 0 if gone else 1
    if a.budget_points is None and not a.from_budget:
        ap.error("one of the arguments --budget-points --from-budget is required")
    if not a.dry_run and not a.scratch:
        ap.error("--scratch is required without --dry-run")
    if a.cluster and (a.clusters_json or a.half_clusters_json):
        ap.error("--cluster plans from the cluster's own notes; it takes no --clusters-json or --half-clusters-json")
    rules = coord.rules()
    cdir = coord.coord_dir(a.coord)
    if a.from_budget:
        out = subprocess.run([sys.executable, str(HERE / "budget.py"), "--json", "--borrow-days", str(a.borrow_days)],
                             capture_output=True, text=True, encoding="utf-8", check=True).stdout
        budget_points = max(0.0, json.loads(out)["headroom_pct"])
    else:
        budget_points = a.budget_points
    fleet = json.loads((REPO / ".agent-fleet.json").read_text(encoding="utf-8"))
    harm = {k: int(v) for k, v in (p.split("=") for p in fleet["harm"].split(","))}
    notes = load_notes(REPO / "kb" / "Work", harm)
    def fix_clusters(status: str, given: str | None) -> list[dict[str, Any]]:
        if given:
            return json.loads(pathlib.Path(given).read_text(encoding="utf-8"))["clusters"]
        if not FIX_CLUSTERS.exists():
            raise SystemExit(f"⛔ {FIX_CLUSTERS} is missing: run `git submodule update --init tools/claude-skills`")
        tmp = cdir / f"clusters-{status}.json"
        subprocess.run([sys.executable, str(FIX_CLUSTERS), "--max", str(rules["wave"]["max_notes_per_group"]),
                        "--open-status", status, "--json", str(tmp)], cwd=REPO, check=True, capture_output=True)
        return json.loads(tmp.read_text(encoding="utf-8"))["clusters"]

    deps, waiting, campaign = None, {}, ""
    if a.cluster:
        view = work.cluster_order(work.load(), a.cluster)
        if not view["named"]:
            print(f"⛔ no kb/Work note names the cluster {a.cluster!r} in its `cluster:` list")
            return 2
        if not view["notes"]:
            print(f"=== CLUSTER {a.cluster} LANDED: every one of its {view['named']} note(s) is landed or retired ===")
            return 0
        texts = {r["id"]: f.read_text(encoding="utf-8", errors="replace").replace("\r\n", "\n")
                 for r in view["notes"] if (f := REPO / "kb" / "Work" / f"{r['id']}.md").is_file()}
        clusters, half_clusters, deps, waiting = campaign_clusters(
            view, notes, texts, rules, load_fix_clusters(), REPO,
            callers=member_callers(REPO, cdir / "member-index", regenerate=not a.no_index_regenerate))
        coord.write_json(cdir / f"clusters-campaign-{a.cluster}.json",
                         {"cluster": a.cluster, "open": clusters, "half": half_clusters, "deps": deps, "waiting": waiting})
        campaign = f" — CAMPAIGN {a.cluster}"
        a.authorization += (f"; this wave runs the campaign lane for the kb/Work cluster {a.cluster} that the "
                            f"orchestrator was started with (orchestrate.ps1 -Cluster, kb/Work PB2120)")
    else:
        clusters = fix_clusters("open", a.clusters_json)
        half_clusters = fix_clusters("half", a.half_clusters_json)
    reports_dir = pathlib.Path(a.reports) if a.reports else (pathlib.Path(a.scratch) / "reports" if a.scratch else None)
    reports = load_reports(reports_dir)
    open_ids = {n["id"] for c in clusters + half_clusters for n in c["notes"]}
    if a.no_branches:
        classify, unlanded = (lambda _b: "ABSENT"), {}
    else:
        classify, unlanded = git_branch_classifier(), unlanded_branch_notes(open_ids)
    busy = None
    items = work.load()
    status_of = {i.get("id"): i.get("status") for i in items}

    def terminal(nid: str) -> bool:
        return status_of.get(nid) in work.TERMINAL_STATUSES

    if a.cluster:  # the file-set partition: the in-flight work (worktrees, branches, dispatched groups), the slices
        busy = inflight_file_sets(coord_dir=cdir, terminal=terminal, with_git=not a.no_branches)
        if any(work.EXTERNAL_SLICE_TITLE.match(i.get("title") or "") and i.get("status") not in work.TERMINAL_STATUSES
               for i in items):
            busy["the open external-repository slices"] = work.slice_file_set()
    p = plan(notes, clusters, half_clusters, reports, classify, unlanded, rules, budget_points,
             a.max_groups or rules["wave"]["max_groups"], deps=deps, busy=busy)
    waiting.update(p["waiting"])

    wave = str(next_wave(REPO, reports)) if a.wave == "next" else str(a.wave)
    train = rules["wave"]["train_size"]
    peek_code = alloc.peek("code", REPO, cdir)
    peek_pb = alloc.peek("pb", REPO, cdir)
    per = rules["wave"]["codes_per_group"]
    gjson, blocks = [], []
    for k, g in enumerate(p["groups"]):
        if a.dry_run:
            first = peek_code + k * per
        else:
            first, _ = alloc.allocate("code", per, REPO, cdir)
        gjson.append(as_group_json(g, wave, notes, alloc.render("code", first, per)))
    for t in range(p["trains"]):
        n = rules["wave"]["lead_ids_per_train"]
        first = peek_pb + t * n if a.dry_run else alloc.allocate("pb", n, REPO, cdir)[0]
        blocks.append(alloc.render("pb", first, n))

    print(f"=== WAVE {wave} PLAN{campaign}: {len(p['groups'])} groups, {p['trains']} train(s), {p['points']:.2f} of "
          f"{budget_points:.2f} weekly points{' (DRY RUN: nothing reserved or written)' if a.dry_run else ''} ===")
    for g, j in zip(p["groups"], gjson):
        print(f"  {j['letter']:>3} {g.model:6} {('after ' + g.after) if g.after else '':9} {g.kind:8} "
              f"{sum(len(notes[i].rows) for i in g.notes):3} rows  {g.cost:.2f} pt  {j['codes']}  {g.file or '-'}")
        print(f"      notes {j['notes']} (lead {j['lead']}); model: {g.why_model}"
              + (f"; blocked by {', '.join(g.blocked_by)} (planned before it)" if g.blocked_by else ""))
    if p["awaiting_landing"]:
        print("  awaiting landing (excluded; a `land` unit first): " + "; ".join(
            f"{b}: {', '.join(ids)}" for b, ids in sorted(p["awaiting_landing"].items())))
    if waiting:
        print("  waiting (a later wave): " + "; ".join(f"{i}: {why}" for i, why in waiting.items()))
    if p["unlanded_without_note"]:
        print("  UNLANDED branches naming no open note (judgment): " + ", ".join(p["unlanded_without_note"]))
    print(f"  next {len(p['skipped'])} candidates skipped (budget, cap or predecessor): " +
          ", ".join(f"{g.letter}({'+'.join(g.notes[:2])}{'…' if len(g.notes) > 2 else ''})" for g in p["skipped"][:8]))
    print(f"  lead-id blocks: {', '.join(blocks) or 'none'}; devlog_n {alloc.peek('devlog', REPO, cdir)}; "
          f"previous {previous_train(REPO)}")
    if a.dry_run or not p["groups"]:
        return 0

    scratch = pathlib.Path(a.scratch)
    scratch.mkdir(parents=True, exist_ok=True)
    head = subprocess.run(["git", "rev-parse", "--short", "origin/main"], cwd=REPO, capture_output=True,
                          text=True).stdout.strip()
    gfile = scratch / "groups.json"
    stop_file = pathlib.Path(a.stop_file) if a.stop_file else coord.fleet_stop(scratch, f"w{wave}")
    if stop_file == coord.global_stop(a.coord):
        print(f"--stop-file {stop_file} is the owner's GLOBAL stop; a fleet's stop is its own STOP-<scope> (kb/Work PB2483)")
        return 2
    gfile.write_text(json.dumps({"wave": wave, "scratch": str(scratch), "base": f"{head} (origin/main when planned)",
                                 "stop_file": str(stop_file), "groups": gjson}, indent=1, ensure_ascii=False) + "\n",
                     encoding="utf-8")
    args = {"scratch": str(scratch), "wave": wave, "stop_file": str(stop_file), "global_stop": str(coord.global_stop(a.coord)),
            "concurrency": rules["wave"]["concurrency"], "train_size": train,
            "min_final_train": rules["wave"]["min_final_train"], "devlog_n": alloc.peek("devlog", REPO, cdir),
            "previous_train": previous_train(REPO), "lead_id_blocks": blocks,
            "implementer_model": rules["wave"]["implementer_model"], "authorization": a.authorization,
            "groups": [{k: j[k] for k in ("letter", "lead", "notes", "codes", "model", "after") if k in j}
                       for j in gjson]}
    afile = scratch / f"wf-args-w{wave}.json"
    afile.write_text(json.dumps(args, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    record_dispatch(cdir, wave, p["groups"], terminal)
    print(f"wrote {gfile} and {afile}; the dispatch ledger {cdir / DISPATCH_LEDGER}")
    rc = subprocess.call([sys.executable, str(MAKE_SPECS), str(gfile)], cwd=REPO)
    print(f"make_dispatch_specs.py + check_practices.py: {'GREEN' if rc == 0 else 'RED'}")
    return rc


if __name__ == "__main__":
    sys.exit(main())
