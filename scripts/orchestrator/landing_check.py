#!/usr/bin/env python3
"""landing_check.py — the file-set partition's GUARANTEE, checked where every landing passes (scripts/push-main.sh).

WHY (kb/Work PB2118 Drafts 9-10; DESIGN-architecture-review §8.7). The owner made the mechanical file-set partition the
ONLY gate between the R3 restructuring waves (cluster PB2119) and the fix lane's trains (PB2118 question 5). The
planner (plan_wave.py --cluster) admits a wave on its COMPUTED file set: its sites, the callers the member index
finds, the files under its site folders. Three refuter rounds each found a new hole in that computation (hand lists,
then the index's blind spots, then its staleness), so the planner's set is an ADMISSION ESTIMATE; a miss in it must
be caught, never silently landed. This is the second line: before a landing reaches main it compares what the landing
ACTUALLY changed with

  1. its DECLARED file set, for the R3 work it lands: the set the dispatch ledger recorded for those notes when they
     were planned, read from the FULL ledger (a landing flips its own notes to terminal, and that declaration is
     still the authority for it), or, for a note no entry records, the set computed now. A file an R3 commit changed
     that existed on main and lies outside that set STOPS the landing: the wave touched a file nothing protected;
  2. every OTHER in-flight branch's actual changes (plan_wave.inflight_file_sets: worktrees, unlanded branches, the
     dispatch ledger's declared sets), when the shared file is R3 work on either side. A shared file stops the LATER
     of the two (by dispatch time: the ledger's `at`, else the branch's first commit): it re-plans and rebases onto
     the earlier after that lands. The earlier one lands, and the check names the branch that must rebase.

R3 work is attributed PER COMMIT (PB2703). A train is MIXED: the lander makes one commit per cluster, and some clusters
work on R3 notes while the rest are fix lane or tooling. The landing's R3 work is the commits that work on an R3 note
(the ids leading the subject, or an R3 status the commit flips) and the partition files those commits change; a
fix-lane commit in the same train is fix lane, so its files neither leave the R3 set (1) nor make a shared file R3 (2).
A squashed commit that names both kinds is R3 whole: it stops rather than land silently.

Draft 10 (the ninth refuter's K1-K3). Every diff is read with --no-renames (plan_wave.changed_files), so a moved file
counts at its old path too. An in-flight branch is R3 work because its DISPATCH says so, never by its commit wording
(implementers commit `WIP checkpoint: …`): a train manifest that lists it, the dispatch-ledger groups whose declared
files its actual diff meets, then the ids that lead its subjects or whose status it flips. A dispatch entry stays in
flight with its whole declared set until its notes are terminal, started or not. The landing's OWN members are
excluded by its train manifest (<coord>/scratch/train*-manifest.json) and by content (a file whose version on the
member IS the landing's: the lander re-commits or patches its clusters); anything else that shares a file and was
dispatched earlier stops it, and the stop names how it was identified and how to bind a member it failed to recognise.

Fix-lane trains with no R3 work in flight pass untouched: fix-lane implementers share files by design, and a train
merges them. The partition's domain is the campaign's code roots (model_rules.json `campaign`: src, tests, scripts,
.github, docs, minus exclude_dir) and only CODE files there (its extensions but .md): a document both sides edit is a
text merge the rebase resolves, and kb/Work and DEVLOG.md are every landing's bookkeeping.

Usage:
    python scripts/orchestrator/landing_check.py [--rev HEAD] [--base origin/main] [--coord DIR]
    python scripts/orchestrator/landing_check.py --self-test
Exit 0: the landing may proceed (warnings name branches that must rebase). Exit 1: STOP (each reason printed).
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import re
import subprocess
import sys
import tempfile
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "spec"))
import coord  # noqa: E402
import plan_wave  # noqa: E402  (inflight_file_sets: THE in-flight enumeration; note_file_set: THE declared set)
import work  # noqa: E402

PB = re.compile(r"\bPB\d+\b")
LEADING_IDS = re.compile(r"^((?:PB\d+[\s+,&-]*)+)", re.M)   # "PB1939+PB2004: ..." — the notes a commit works on
ID_RANGE = re.compile(r"\bPB(\d+)-PB(\d+)\b")                # "PB2478-PB2483: ..." — every id of the range
MAX_RANGE = 64
STATUS_LINE = re.compile(r"^([-+])status:[ ]*(\S*)")   # a frontmatter status line removed or added by a patch
WORK_DIR = "kb/Work/"
MANIFESTS = "scratch/train*-manifest.json"   # wf_rolling_wave.js writes one per train: each member's branch, worktree, notes


def _git(repo: pathlib.Path) -> Callable[..., Any]:
    def run(*args: str, cwd: Any = repo) -> subprocess.CompletedProcess:
        return subprocess.run(["git", "-C", str(cwd), *args], capture_output=True, text=True, encoding="utf-8",
                              errors="replace")
    return run


def partitioned(rules: dict[str, Any]) -> Callable[[str], bool]:
    """Whether a path is in the partition's domain: a code file under the campaign's roots (module doc)."""
    cfg = rules["campaign"]
    roots = tuple(r.rstrip("/") + "/" for r in cfg["src"])
    exts = tuple(e.strip() for e in cfg["ext"].split(",") if e.strip() and e.strip() != ".md")
    skip = set(cfg["exclude_dir"])
    return lambda p: p.startswith(roots) and p.endswith(exts) and not skip & set(p.split("/")[:-1])


def subject_ids(subjects: str) -> set[str]:
    """The note ids that LEAD the commit subjects, ranges expanded (`PB2478-PB2483:` is six notes, not two)."""
    out = set()
    for m in LEADING_IDS.findall(subjects):
        out |= set(PB.findall(m))
        out |= {f"PB{n}" for a, b in ID_RANGE.findall(m) if 0 <= int(b) - int(a) <= MAX_RANGE
                for n in range(int(a), int(b) + 1)}
    return out


def work_notes(git: Callable[..., Any], base: str, rev: str) -> set[str]:
    """The notes the LANDING `base..rev` works on: the ids that lead a commit subject (the lander re-commits one
    `PB…: …` commit per cluster) and the notes whose `status` it changes (a landing flips its note in the commit that
    lands it, CLAUDE.md rule 8). Never every id a message mentions: a design commit that names the notes it re-plans
    works on none of them. An IN-FLIGHT branch's identity never comes from here alone (identity(), K2)."""
    out = subject_ids(git("log", "--format=%s", f"{base}..{rev}").stdout)
    # ONE patch of the register, not two `git show`s per changed note (the ninth refuter's N1: 400 shows were 17 of
    # the check's 24 s on a real range). A note's status changed when its patch removes one `status:` line and adds a
    # different one; a note FILED here has no removed line, so it is not worked on here.
    nid, was = None, {}
    for line in git("diff", "--no-renames", "-U0", f"{base}...{rev}", "--", WORK_DIR).stdout.splitlines():
        if line.startswith("diff --git "):
            stem = pathlib.PurePosixPath(line.rsplit(" b/", 1)[-1]).stem
            nid = stem if PB.fullmatch(stem) else None
        elif nid and (m := STATUS_LINE.match(line)):
            was.setdefault(nid, {})[m.group(1)] = m.group(2)
    return out | {n for n, s in was.items() if "-" in s and s["-"] != s.get("+")}


def first_commit_time(git: Callable[..., Any], base: str, rev: str) -> float | None:
    out = git("log", "--reverse", "--format=%ct", f"{base}..{rev}").stdout.split()
    return float(out[0]) if out else None


def _key(path: str | None) -> str:
    return (path or "").replace(chr(92), "/").rstrip("/").lower()


def train_manifests(coord_dir: pathlib.Path) -> list[dict[str, Any]]:
    """Every train manifest's members: {branch, worktree (normalised), notes (set)}. wf_rolling_wave.js writes the
    manifest from the wave's results before its lander starts, so it names the branches a train carries."""
    out = []
    for f in sorted(coord_dir.glob(MANIFESTS)):
        for m in coord.read_json(f, []) or []:
            if isinstance(m, dict):
                notes = m.get("notes") or []
                out.append({"branch": m.get("branch"), "worktree": _key(m.get("worktree")),
                            "notes": set(PB.findall(notes if isinstance(notes, str) else " ".join(notes)))})
    return out


def check(repo: pathlib.Path, base: str, rev: str, coord_dir: pathlib.Path, items: list[dict], rules: dict[str, Any],
          declared_for: Callable[[list[str]], set[str]], git: Callable[..., Any] | None = None,
          r3_lead: str = work.R3_PROGRAM) -> tuple[list[str], list[str]]:
    """(stops, warnings) for landing `rev` onto `base`. `declared_for(note ids)` computes a declared set the dispatch
    ledger does not hold."""
    git = git or _git(repo)
    inside = partitioned(rules)
    r3 = {i.get("id") for i in items if r3_lead in (i.get("cluster") or [])}
    status = {i.get("id"): i.get("status") for i in items}
    terminal = lambda n: status.get(n) in work.TERMINAL_STATUSES  # noqa: E731
    mine = {p for p in plan_wave.changed_files(git, f"{base}...{rev}") if inside(p)}
    notes = work_notes(git, base, rev)
    # R3 work is attributed PER COMMIT (PB2703): a train is MIXED — one commit per cluster (lander-train-brief step 5),
    # some clusters R3 notes, the rest fix lane or tooling — so the R3 work is the commits that work on an R3 note and
    # the partition files THEY change, never the whole landing's diff. A squashed commit naming both kinds is R3 whole.
    r3_notes, mine_r3_files = set(), set()
    for sha in git("rev-list", "--reverse", f"{base}..{rev}").stdout.split():
        if hit := work_notes(git, f"{sha}^", sha) & r3:
            r3_notes |= hit
            mine_r3_files |= {p for p in plan_wave.changed_files(git, f"{sha}^", sha) if inside(p)}
    mine_r3 = sorted(r3_notes, key=lambda s: int(s[2:]))
    full_ledger = coord.read_json(coord_dir / plan_wave.DISPATCH_LEDGER, {"groups": []}).get("groups", [])
    ledger = [g for g in full_ledger if not all(terminal(i) for i in g.get("notes", []))]
    manifests = train_manifests(coord_dir)
    members = [m for m in manifests if m["notes"] & notes]   # THIS landing's train members (K3)
    my_at = min([g["at"] for g in ledger if "at" in g and set(g.get("notes", [])) & notes]
                + [first_commit_time(git, base, rev) or float("inf")]
                + [t for m in members if m["branch"] and (t := first_commit_time(git, base, m["branch"]))])
    stops, warnings = [], []

    if mine_r3:   # 1. the declared set
        # From the FULL ledger, terminal or not: the declaration recorded when the work was planned is the authority
        # for the landing that makes those notes terminal (`work.load` reads the LANDING's tree, where its own notes
        # are already landed, PB2703). A note no entry records falls back to the set computed now.
        recorded = [g for g in full_ledger if set(g.get("notes", [])) & r3_notes]
        unrecorded = sorted(r3_notes - {n for g in recorded for n in g.get("notes", [])}, key=lambda s: int(s[2:]))
        declared = {f.replace(chr(92), "/") for g in recorded for f in g.get("files", [])}
        declared |= declared_for(unrecorded) if unrecorded else set()
        source = " + ".join(s for s, on in (("the dispatch ledger", recorded), ("computed now", unrecorded)) if on)
        existed = {p for p in mine_r3_files if git("cat-file", "-e", f"{base}:{p}").returncode == 0}
        if outside := sorted(existed - declared):
            stops.append(f"R3 work ({', '.join(mine_r3)}) changed {len(outside)} file(s) outside its declared file set "
                         f"({source}): {', '.join(outside[:8])}"
                         f"{' …' if len(outside) > 8 else ''} — re-plan: the planner's set missed them (a member_index "
                         f"hole: file a kb/Work note), and check them against the in-flight work before landing")

    meta: dict[str, dict] = {}
    busy = plan_wave.inflight_file_sets(git=git, repo=repo, coord_dir=coord_dir, meta=meta, terminal=terminal)

    def contained(their_rev: str, files: set[str]) -> set[str]:
        """The files whose version on `their_rev` IS this landing's: a lander that re-committed or patched a member's
        work carries it by content, not by commit (lander-train-brief step 1)."""
        return {f for f in files if git("rev-parse", "-q", "--verify", f"{rev}:{f}").stdout.strip()
                == git("rev-parse", "-q", "--verify", f"{their_rev}:{f}").stdout.strip()}

    def identity(owner: str, m: dict, files: set[str]) -> tuple[set[str], float, str]:
        """(the notes an in-flight branch works on, its dispatch time, how that is known). By what DISPATCHED it,
        never by its commit wording alone (K2: implementers commit `WIP checkpoint: …`): a train manifest that lists
        its branch or worktree, the dispatch-ledger groups whose declared files its actual diff meets, and the ids
        that lead its subjects or whose status it flips."""
        their = set().union(*[x["notes"] for x in manifests
                              if (m.get("branch") and x["branch"] == m["branch"]) or
                              (m.get("path") and x["worktree"] == _key(m["path"]))]) if manifests else set()
        how = "train manifest" if their else ""
        bound = [g for g in ledger if files & {f.replace(chr(92), "/") for f in g.get("files", [])}]
        if bound:
            their |= {n for g in bound for n in g.get("notes", [])}
            how = how or "dispatch ledger"
        if subj := work_notes(git, base, m["rev"]):
            their |= subj
            how = how or "commit subjects"
        at = min([g["at"] for g in ledger if "at" in g and set(g.get("notes", [])) & their]
                 + [first_commit_time(git, base, m["rev"]) or float("inf")])
        return their, at, how or "nothing"

    for owner, files in sorted(busy.items()):
        m = meta.get(owner, {})
        if m.get("rev"):                                   # a worktree or an unlanded branch, by its ACTUAL diff
            if any((m.get("branch") and x["branch"] == m["branch"]) or (m.get("path") and x["worktree"] == _key(m["path"]))
                   for x in members):
                continue                                   # this landing's own train member (its manifest)
            if git("merge-base", "--is-ancestor", m["rev"], rev).returncode == 0:
                files = set(m.get("uncommitted", set()))   # its commits are in this landing; later edits are its own
            shared = {f for f in mine & files if inside(f)}
            if not shared:
                continue
            their_notes, their_at, how = identity(owner, m, files)
            if not their_notes or their_notes & notes:     # not known to be OTHER work: its re-committed content
                shared -= contained(m["rev"], shared - set(m.get("uncommitted", set())))   # is this landing's (K3)
                if not shared:
                    continue
        else:                                              # a dispatch-ledger entry: its DECLARED set, started or not
            their_notes, their_at, how = set(m.get("notes", [])), m.get("at", 0.0), "dispatch ledger"
            if their_notes & notes:
                continue                                   # this landing's own dispatch entry
            shared = {f for f in mine & files if inside(f)}
            if not shared:
                continue
        if not shared & mine_r3_files and not their_notes & r3:
            continue                                       # fix lane against fix lane: a train merges them (PB2703:
                                                           # per file — only a file an R3 commit changed is R3 here)
        names = f"{', '.join(sorted(shared)[:5])}{' …' if len(shared) > 5 else ''}"
        if their_at < my_at:
            unbound = (" It is " + (f"{', '.join(sorted(their_notes)[:4])} work by its {how}" if how != "nothing"
                                    else "bound to no dispatch: no train manifest, dispatch-ledger entry or commit "
                                         "subject names its notes") +
                       "; if it IS this landing's own implementer, list it in this train's manifest "
                       f"(<coord>/{MANIFESTS}) and re-run.") if m.get("rev") else ""
            stops.append(f"shares {names} with {owner}, dispatched earlier: this landing is the LATER branch — it "
                         f"waits, re-plans and rebases onto {owner} after that lands.{unbound}")
        else:
            warnings.append(f"shares {names} with {owner}, dispatched later: this landing proceeds; {owner} must "
                            f"re-plan and rebase onto it")
    return stops, warnings


def computed_declared(repo: pathlib.Path) -> Callable[[list[str]], set[str]]:
    """The declared set computed now (plan_wave.note_file_set over the newest member index): for R3 work the dispatch
    ledger never recorded."""
    def run(ids: list[str]) -> set[str]:
        rules = coord.rules()
        cfg = rules["campaign"]
        fc = plan_wave.load_fix_clusters()
        exts = [e.strip() for e in cfg["ext"].split(",") if e.strip()]
        idx = fc.source_index([repo / s for s in cfg["src"] if (repo / s).is_dir()], exts, cfg["exclude_dir"], repo)
        callers = plan_wave.member_callers(repo)
        out: set[str] = set()
        for i in ids:
            if (f := repo / "kb" / "Work" / f"{i}.md").is_file():
                out |= plan_wave.note_file_set(f.read_text(encoding="utf-8", errors="replace"), fc, idx, exts, callers)
        return out
    return run


# ── self-test: plants an out-of-set edit and a two-branch overlap in a scratch repository ───────────────────────
def self_test() -> int:
    root = pathlib.Path(tempfile.mkdtemp(prefix="landing-check-"))
    repo, cdir = root / "repo", root / "coord"
    repo.mkdir()
    cdir.mkdir()
    git = _git(repo)
    def sh(*args: str, when: int | None = None) -> None:
        env = dict(os.environ, GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@t", GIT_COMMITTER_NAME="t",
                   GIT_COMMITTER_EMAIL="t@t", **({"GIT_COMMITTER_DATE": f"@{when} +0000",
                                                   "GIT_AUTHOR_DATE": f"@{when} +0000"} if when else {}))
        subprocess.run(["git", "-C", str(repo), *args], check=True, capture_output=True, env=env)

    def write(rel: str, text: str) -> None:
        (repo / rel).parent.mkdir(parents=True, exist_ok=True)
        (repo / rel).write_text(text, encoding="utf-8")

    sh("init", "-q", "-b", "main")
    for f in ("src/A.cs", "src/B.cs", "src/C.cs", "docs/D.md"):
        write(f, "// base\n")
    write("kb/Work/PB9002.md", "---\nid: PB9002\nstatus: open\n---\n")
    sh("add", "-A")
    sh("commit", "-qm", "base", when=1_000_000)
    sh("update-ref", "refs/remotes/origin/main", "HEAD")

    def branch(name: str, msg: str, edits: dict[str, str], when: int) -> None:
        sh("checkout", "-q", "-b", name, "origin/main")
        for f, t in edits.items():
            write(f, t)
        sh("add", "-A")
        sh("commit", "-qm", msg, when=when)
        sh("checkout", "-q", "main")

    items = [{"id": "PB9001", "status": "open", "cluster": ["PB2119"]},
             {"id": "PB9002", "status": "open", "cluster": ["PB2119"]},
             {"id": "PB9100", "status": "open", "cluster": []},
             {"id": "PB9101", "status": "open", "cluster": []}]
    rules = {"campaign": {"src": ["src", "docs"], "ext": ".cs,.md", "exclude_dir": []}}
    coord.write_json(cdir / plan_wave.DISPATCH_LEDGER, {"groups": [
        {"wave": "1", "letter": "a", "notes": ["PB9001"], "files": ["src/A.cs"], "at": 1_000_100},
        {"wave": "1", "letter": "b", "notes": ["PB9002"], "files": ["src/C.cs"], "at": 1_000_300}]})
    never = lambda ids: set()  # noqa: E731
    results = []

    def expect(label: str, got: tuple[list[str], list[str]], stops: int, warns: int) -> None:
        ok = (len(got[0]), len(got[1])) == (stops, warns)
        results.append((label, ok, got))

    # in-set R3 work, nothing else in flight: lands
    branch("r-ok", "PB9001: extract in set", {"src/A.cs": "// moved\n", "src/New.cs": "// new\n",
                                               "docs/D.md": "doc\n"}, 1_000_200)
    expect("an R3 landing inside its declared set (a new file and a doc edit) lands",
           check(repo, "origin/main", "r-ok", cdir, items, rules, never, git), 0, 0)
    sh("branch", "-D", "r-ok")
    # PLANT 1: an out-of-set edit (src/B.cs existed on main and is not declared)
    branch("r-out", "PB9001: extract, one file too many", {"src/A.cs": "// moved\n", "src/B.cs": "// caller\n"},
           1_000_200)
    expect("PLANT 1: an R3 landing that edits a file outside its declared set STOPS",
           check(repo, "origin/main", "r-out", cdir, items, rules, never, git), 1, 0)
    sh("branch", "-D", "r-out")
    # PLANT 2: a two-branch overlap — R3 work (dispatched at 1_000_100) and a fix-lane branch (first commit later)
    branch("r-one", "PB9001: extract", {"src/A.cs": "// moved\n"}, 1_000_200)
    branch("f-two", "PB9100: fix", {"src/A.cs": "// fixed\n"}, 1_000_500)
    expect("PLANT 2a: the LATER branch (the fix-lane train) sharing a file with in-flight R3 work STOPS (against "
           "the R3 branch and against its dispatch entry)",
           check(repo, "origin/main", "f-two", cdir, items, rules, never, git), 2, 0)
    expect("PLANT 2b: the EARLIER branch (the R3 wave) lands and names the branch that must rebase",
           check(repo, "origin/main", "r-one", cdir, items, rules, never, git), 0, 1)
    # a branch whose commits the landing contains IS the landing: a train built on the R3 branch r-one names only
    #   f-two (dispatched later) as a branch to rebase, never r-one itself
    sh("checkout", "-q", "-b", "train", "r-one")
    write("src/F.cs", "// a fix-lane file the train adds\n")
    sh("add", "-A")
    sh("commit", "-qm", "PB9101: fix in the train", when=1_000_700)
    sh("checkout", "-q", "main")
    expect("a branch whose commits the landing contains is not in flight against it",
           check(repo, "origin/main", "train", cdir, items, rules, never, git), 0, 1)
    sh("branch", "-D", "train")
    # fix lane against fix lane: a train merges them (no R3 work on either side)
    sh("branch", "-D", "r-one")
    items[0]["status"] = "landed"   # r-one landed: its dispatch entry is no longer in flight
    branch("f-three", "PB9101: fix", {"src/A.cs": "// fixed too\n"}, 1_000_600)
    expect("fix-lane branches sharing a file with no R3 work in flight pass",
           check(repo, "origin/main", "f-three", cdir, items, rules, never, git), 0, 0)
    # a declared set computed now when the ledger never recorded the work (a hand dispatch)
    # a design commit that NAMES an R3 note it does not work on is not R3 work (its subject does not lead with it)
    branch("design", "WIP checkpoint: re-plan PB9001 and PB9002", {"src/B.cs": "// tooling\n"}, 1_000_750)
    expect("a commit that only mentions an R3 note is not R3 work",
           check(repo, "origin/main", "design", cdir, items, rules, never, git), 0, 0)
    sh("branch", "-D", "design")
    # a hand wave whose subject names no note but which flips its note's status IS R3 work (the status flip)
    branch("r-hand", "Hand wave: extract", {"src/B.cs": "// hand\n",
                                             "kb/Work/PB9002.md": "---\nid: PB9002\nstatus: landed\n---\n"}, 1_000_800)
    coord.write_json(cdir / plan_wave.DISPATCH_LEDGER, {"groups": []})
    expect("an unrecorded R3 landing is checked against the set computed now (outside it: STOP)",
           check(repo, "origin/main", "r-hand", cdir, items, rules, lambda ids: {"src/C.cs"}, git), 1, 0)
    expect("... and inside it: lands",
           check(repo, "origin/main", "r-hand", cdir, items, rules, lambda ids: {"src/B.cs"}, git), 0, 0)
    for b in ("f-two", "f-three", "r-hand"):
        sh("branch", "-D", b)

    # ── Draft 10: the ninth refuter's K1 (renames), K2 (identity), K3 (a landing's own members), N2, N5 ──
    items[0]["status"] = "open"
    def ledger(*groups: dict) -> None:
        coord.write_json(cdir / plan_wave.DISPATCH_LEDGER, {"groups": list(groups)})

    def moved(name: str, msg: str, old: str, new: str, when: int) -> None:
        sh("checkout", "-q", "-b", name, "origin/main")
        sh("mv", old, new)
        sh("commit", "-qm", msg, when=when)
        sh("checkout", "-q", "main")

    def worktree(name: str, msg: str, edits: dict[str, str], when: int) -> pathlib.Path:
        wt = root / name
        sh("worktree", "add", "-q", "-b", name, str(wt), "origin/main")
        for f, t in edits.items():
            (wt / f).write_text(t, encoding="utf-8")
        subprocess.run(["git", "-C", str(wt), "add", "-A"], check=True, capture_output=True)
        env = dict(os.environ, GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@t", GIT_COMMITTER_NAME="t",
                   GIT_COMMITTER_EMAIL="t@t", GIT_COMMITTER_DATE=f"@{when} +0000", GIT_AUTHOR_DATE=f"@{when} +0000")
        subprocess.run(["git", "-C", str(wt), "commit", "-qm", msg], check=True, capture_output=True, env=env)
        return wt

    def drop(name: str) -> None:
        sh("worktree", "remove", "--force", str(root / name))
        sh("branch", "-D", name)

    a_set = {"wave": "2", "letter": "a", "notes": ["PB9001"], "files": ["src/A.cs"], "at": 1_000_100}
    ledger(a_set)
    moved("r-mv", "PB9001: move", "src/B.cs", "src/Moved.cs", 1_000_200)
    expect("K1a: an R3 `git mv` of a file outside its declared set STOPS (the OLD path counts)",
           check(repo, "origin/main", "r-mv", cdir, items, rules, never, git), 1, 0)
    sh("branch", "-D", "r-mv")
    ledger()
    moved("r-mv2", "PB9001: move", "src/A.cs", "src/A2.cs", 1_000_200)
    branch("f-mv", "PB9100: fix", {"src/A.cs": "// fixed\n"}, 1_000_500)
    expect("K1b: a fix-lane edit to a path an in-flight R3 branch RENAMED STOPS (only the rename shares it)",
           check(repo, "origin/main", "f-mv", cdir, items, rules, never, git), 1, 0)
    sh("branch", "-D", "r-mv2")
    sh("branch", "-D", "f-mv")

    ledger(a_set)
    worktree("wt-wip", "WIP checkpoint: extract", {"src/A.cs": "// moved\n", "src/B.cs": "// caller\n"}, 1_000_200)
    branch("f-b", "PB9100: fix", {"src/B.cs": "// fixed\n"}, 1_000_500)
    expect("K2: an R3 worktree whose commits are all `WIP checkpoint:` is R3 work by its dispatch entry: a later "
           "fix-lane edit to a file it changed OUTSIDE its declared set STOPS",
           check(repo, "origin/main", "f-b", cdir, items, rules, never, git), 1, 0)
    sh("branch", "-D", "f-b")
    drop("wt-wip")

    worktree("wt-impl", "WIP checkpoint: extract", {"src/A.cs": "// moved\n"}, 1_000_200)
    branch("r-train", "PB9001: extract", {"src/A.cs": "// moved\n"}, 1_000_900)
    expect("K3a: a train that re-committed its own implementer's work (recorded dispatch) lands with no warning",
           check(repo, "origin/main", "r-train", cdir, items, rules, never, git), 0, 0)
    ledger()
    expect("K3a': ... and so does the same train with the dispatch unrecorded (its content is the landing's)",
           check(repo, "origin/main", "r-train", cdir, items, rules, lambda ids: {"src/A.cs"}, git), 0, 0)
    sh("branch", "-D", "r-train")
    branch("r-train2", "PB9001: extract", {"src/A.cs": "// moved, conflict resolved\n"}, 1_000_900)
    got = check(repo, "origin/main", "r-train2", cdir, items, rules, lambda ids: {"src/A.cs"}, git)
    expect("K3b: an unrecorded member the lander changed (no manifest) STOPS, naming the train manifest", got, 1, 0)
    expect("K3b': ... and the stop says how to bind it", ([s for s in got[0] if "train manifest" in s], []), 1, 0)
    (cdir / "scratch").mkdir(exist_ok=True)
    coord.write_json(cdir / "scratch" / "train9-manifest.json",
                     [{"cluster": "A", "notes": "PB9001", "branch": "wt-impl", "worktree": str(root / "wt-impl")}])
    expect("K3c: the same member listed in the train manifest is the landing's own: lands",
           check(repo, "origin/main", "r-train2", cdir, items, rules, lambda ids: {"src/A.cs"}, git), 0, 0)
    worktree("wt-other", "WIP checkpoint: another", {"src/A.cs": "// someone else\n"}, 1_000_150)
    expect("K3d: ... and another earlier worktree on the same file still STOPS it",
           check(repo, "origin/main", "r-train2", cdir, items, rules, lambda ids: {"src/A.cs"}, git), 1, 0)
    drop("wt-other")
    drop("wt-impl")
    sh("branch", "-D", "r-train2")
    (cdir / "scratch" / "train9-manifest.json").unlink()

    ledger(dict(a_set, files=["src/A.cs", "src/C.cs"]))
    worktree("wt-start", "WIP checkpoint: part 1", {"src/A.cs": "// moved\n"}, 1_000_200)
    branch("f-c", "PB9100: fix", {"src/C.cs": "// fixed\n"}, 1_000_500)
    expect("N2: a STARTED R3 wave keeps its untouched declared files: a later fix-lane edit to one STOPS",
           check(repo, "origin/main", "f-c", cdir, items, rules, never, git), 1, 0)
    sh("branch", "-D", "f-c")
    drop("wt-start")

    # ── PB2703: a MIXED train (one commit per cluster: R3 and fix lane) is attributed per commit ──
    def commits(name: str, *steps: tuple[str, dict[str, str], int]) -> None:
        sh("checkout", "-q", "-b", name, "origin/main")
        for msg, edits, when in steps:
            for f, t in edits.items():
                write(f, t)
            sh("add", "-A")
            sh("commit", "-qm", msg, when=when)
        sh("checkout", "-q", "main")

    ledger(a_set, {"wave": "2", "letter": "b", "notes": ["PB9002"], "files": ["src/C.cs"], "at": 1_000_300})
    commits("mixed", ("PB9001: extract in set", {"src/A.cs": "// moved\n"}, 1_000_200),
            ("PB9100: fix-lane cluster", {"src/B.cs": "// fixed\n"}, 1_000_250))
    expect("PB2703a: a mixed train's fix-lane commit editing a file outside the R3 set lands",
           check(repo, "origin/main", "mixed", cdir, items, rules, never, git), 0, 0)
    branch("f-early", "PB9101: earlier fix", {"src/B.cs": "// earlier fix\n"}, 1_000_050)
    expect("PB2703b: ... its fix-lane file shared with an EARLIER in-flight fix-lane branch lands",
           check(repo, "origin/main", "mixed", cdir, items, rules, never, git), 0, 0)
    sh("branch", "-D", "f-early")
    branch("f-early2", "PB9101: earlier fix", {"src/A.cs": "// earlier fix\n"}, 1_000_050)
    expect("PB2703b': ... while its R3 commit's file shared with an EARLIER in-flight branch STOPS",
           check(repo, "origin/main", "mixed", cdir, items, rules, never, git), 1, 0)
    sh("branch", "-D", "f-early2")
    sh("branch", "-D", "mixed")
    flipped = [dict(i, status="landed") if i["id"] == "PB9002" else i for i in items]   # as the landing's tree reads
    commits("r-self", ("PB9002: delete, and land the note", {"src/C.cs": "// deleted\n", "kb/Work/PB9002.md":
                                                             "---\nid: PB9002\nstatus: landed\n---\n"}, 1_000_400))
    expect("PB2703c: R3 work that flips its own note to landed keeps its (now terminal) ledger declaration: lands",
           check(repo, "origin/main", "r-self", cdir, flipped, rules, never, git), 0, 0)
    sh("branch", "-D", "r-self")
    commits("squashed", ("PB9001+PB9100: both kinds in one", {"src/A.cs": "// moved\n", "src/B.cs": "// fix\n"},
                         1_000_200))
    expect("PB2703d: ONE commit naming an R3 id and a fix-lane id is R3 whole: its file outside the set STOPS",
           check(repo, "origin/main", "squashed", cdir, items, rules, never, git), 1, 0)
    sh("branch", "-D", "squashed")
    ledger()

    ids = sorted(subject_ids("PB9000-PB9002: a range" + chr(10) + "PB9100+PB9101: two"))
    expect("N5: a subject range names every note in it",
           ([] if ids == ["PB9000", "PB9001", "PB9002", "PB9100", "PB9101"] else [ids], []), 0, 0)
    bad = [r for r in results if not r[1]]
    for label, ok, got in results:
        print(f"  {'ok ' if ok else 'BAD'} {label}" + ("" if ok else f": {got}"))
    print(f"=== LANDING CHECK SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results) - len(bad)}/{len(results)}) ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--rev", default="HEAD")
    ap.add_argument("--base", default="origin/main")
    ap.add_argument("--coord", help=f"coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return self_test()
    repo = pathlib.Path(subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True,
                                       check=True).stdout.strip())
    stops, warnings = check(repo, a.base, a.rev, coord.coord_dir(a.coord), work.load(repo / "kb" / "Work"),
                            coord.rules(), computed_declared(repo))
    for w in warnings:
        print(f"⚠ landing check: {w}")
    for s in stops:
        print(f"⛔ landing check: {s}")
    print(f"=== LANDING CHECK: {'STOP' if stops else 'PASS'} ({len(stops)} stop(s), {len(warnings)} warning(s)) ===")
    return 1 if stops else 0


if __name__ == "__main__":
    sys.exit(main())
