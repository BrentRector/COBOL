#!/usr/bin/env python3
"""landing_measure.py — the landing pipeline's measurement, one record per pull request (kb/Work PB3028).

    python scripts/orchestrator/landing_measure.py record --pr 17 [--branch B] [--changes N] [--done ISO] \
        [--review-clean ISO] [--enqueued ISO] [--merged ISO] [--bounces N] [--bounce-causes CAUSE[,CAUSE...]] \
        [--pre-queue-reds N] [--ci-wall-s S] [--local-gate-s S] [--local-gate-runs N] [--note TEXT]
    python scripts/orchestrator/landing_measure.py complete [--pr N]
    python scripts/orchestrator/landing_measure.py summary [--since ISO] [--before PATH]
    python scripts/orchestrator/landing_measure.py freeze
    python scripts/orchestrator/landing_measure.py --self-test

WHY. The owner authorized the landing-pipeline redesign on measurements (kb/Work PB3015; evidence
docs/rearchitecture/evidence/fleet-optimization/2026-10-11-pipeline-measurement-2026-10-10.md) and the redesign is
judged by the same instrument that found the problem: changes landed per hour and minutes per landing, before and
after (DESIGN-test-build-ci.md §3.17.5). This script replaced `train_measure.py`, whose record is a train (clusters,
first run, attribution minutes) — a unit that no longer exists once a finished branch is one pull request in GitHub's
merge queue (kb/Work PB3024). The retired file's records are the "before" column of `summary`.

THE RECORD, one JSON object per pull request in `<coord>/landing-measurements.jsonl` (outside every git worktree,
scripts/orchestrator/coord.py), built up by PATCHES: each `record` call sets only the fields it names, and the others
stay as an earlier call left them, because no single tool sees the whole life of a landing.
  done_each     when each change the pull request carries was finished: one time per change (per lead note), the
                committer time of the HEAD commit its DONE implementer report names — git's time, never a report
                file's, which the branch review rewrites when it stamps (`done_times`); `enqueue.py queue` fills it
  done          the earliest of `done_each` (the order check below runs on it); a later patch never moves it later
  review_clean  the finish review came back clean — whoever ran the review records it (`record --review-clean`)
  enqueued      the FIRST time the branch was queued (a bounce and a re-enqueue keep it: the wait includes the bounce)
  merged        GitHub's `mergedAt` — `complete` reads it
  bounces       merge-group runs of this pull request that FAILED (a run GitHub cancelled because an earlier entry
                failed is a ripple, not this pull request's bounce); `complete` counts them
  bounce_causes one of CAUSES per bounce, named by the finisher that fixed it; a bounce with no cause is counted
                UNATTRIBUTED, never guessed
  pre_queue_reds the pull request's OWN runs (event `pull_request`, its head branch) that FAILED before the queue took
                it (kb/Work PB3096): a red there never reaches a merge group, so `bounces` cannot see it; `complete`
                counts them, and a branch with no pull-request run at all stays null (UNMEASURED), never 0
  ci_wall_s     the successful merge-group run's wall time (`complete`)
  local_gate_s  the seconds of every local gate the landing worktree ran (the sum of its verdict.json `timings.wall_s`)
A field never observed stays null and `summary` says how many landings lack it: a MISSING observation is not a zero.

`summary` prints the after column (this file) beside the before column (the retired train records, then the frozen
2026-10-10 evidence): changes landed per hour, median and p90 minutes DONE → on main per change, bounces per landing
and their causes, pull-request reds before the queue per landing, merge-group CI wall, local gate minutes. `freeze` copies the retired train file under
docs/rearchitecture/evidence/fleet-optimization/ together with the first after-summary (it refuses until a pull request
has landed), after which `summary` reads the tracked copy and the coordination directory's file is no longer needed.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
from typing import Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402
import ruleset  # noqa: E402

REPO = HERE.parents[1]
FILE = "landing-measurements.jsonl"
RETIRED_TRAINS = "train-measurements.jsonl"          # train_measure.py's file; no writer remains
EVIDENCE_DIR = REPO / "docs" / "rearchitecture" / "evidence" / "fleet-optimization"
FROZEN_TRAINS = EVIDENCE_DIR / "train-measurements-frozen.jsonl"
FIRST_AFTER = EVIDENCE_DIR / "first-after-summary.md"
EVIDENCE_MD = EVIDENCE_DIR / "2026-10-11-pipeline-measurement-2026-10-10.md"
WORKFLOW = "build-and-test.yml"
SCHEMA = 1
CAUSES = ("cluster-defect", "load-timing", "conflict", "infrastructure")
STAMPS = ("done", "review_clean", "enqueued", "merged")      # in the order a landing passes them
UNMEASURED = "UNMEASURED"
#: The line each cluster commit of a lander's train carries (`Report: <path>`), written by the retired train applier
#: and by the last trains' landers; the commits stay in main's history, so the measurement still reads them.
TRAILER_REPORT = re.compile(r"^Report:[ \t]*(\S[^\r\n]*?)[ \t]*$", re.M)
#: What each headline's minutes measure, said where the figure is made so no reader re-labels it (finding R1-3).
MEASURES_AFTER = "DONE → on main, per change"
MEASURES_TRAINS = "lander start → push per landed change: a floor for DONE → on main, without the wait for a lander"

#: The 2026-10-10 evidence's figures that the train records cannot give (the lander minutes in gates and in the former
#: direct-push landing plus CI, the CI wall; "push-main + CI" is the frozen evidence's own label). Typed from the frozen evidence file; `self_test` re-reads that file and fails when a
#: figure here is not in its text, so none of them is remembered.
EVIDENCE = {"lander_minutes": 567, "gate_minutes": 233, "gate_pct": 41, "push_ci_minutes": 94, "push_ci_pct": 16.5,
            "clusters_landed": 38, "ci_wall_min": 13}
EVIDENCE_QUOTES = ("567 lander minutes, of which 233 (41 %) inside Windows/WSL gate runs and 94\n(16.5 %) inside "
                   "push-main + CI", "13 minutes on 20 runners", "38 clusters on main")

Gh = Callable[[list[str]], tuple[int, str, str]]
Say = Callable[[str], None]
Git = Callable[[list[str]], tuple[int, str]]   # git argv -> (exit, stdout), in the landing worktree


def _when(text: str) -> dt.datetime:
    t = dt.datetime.fromisoformat(str(text).strip().replace("Z", "+00:00"))
    if t.tzinfo is None:
        raise ValueError(f"{text!r} has no UTC offset (write e.g. 2026-10-12T09:15:00-07:00)")
    return t


def now_iso() -> str:
    return dt.datetime.now().astimezone().isoformat(timespec="seconds")


# ── the record file ──────────────────────────────────────────────────────────────────────────────────────────────

def read_all(path: pathlib.Path) -> list[dict]:
    if not path.exists():
        return []
    out = []
    for n, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if line.strip():
            try:
                out.append(json.loads(line))
            except ValueError as e:
                raise ValueError(f"{path}:{n} is not JSON ({e})") from None
    return out


def _write_all(path: pathlib.Path, recs: list[dict]) -> None:
    tmp = path.with_name(f"{path.name}.{os.getpid()}.tmp")
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:       # LF everywhere (kb/Work PB3015)
        f.write("".join(json.dumps(r, sort_keys=True) + "\n" for r in recs))
    os.replace(tmp, path)


def validate(rec: dict) -> dict:
    """One landing record, checked as a whole: its stamps in the order a landing passes them, its counts consistent."""
    if not isinstance(rec.get("pr"), int) or rec["pr"] < 1:
        raise ValueError(f"--pr {rec.get('pr')!r} is not a pull-request number")
    if not rec.get("branch"):
        raise ValueError(f"pull request {rec['pr']} has no --branch (the first record of a landing names it)")
    if rec["changes"] < 1:
        raise ValueError(f"--changes {rec['changes']} must be at least 1")
    each = [_when(t) for t in rec["done_each"]]
    if rec.get("enqueued") and any(t > _when(rec["enqueued"]) for t in each):
        raise ValueError(f"pull request {rec['pr']}: a change finished after it was enqueued ({rec['done_each']})")
    stamps = [(k, _when(rec[k])) for k in STAMPS if rec.get(k)]
    for (ka, ta), (kb, tb) in zip(stamps, stamps[1:]):
        if tb < ta:
            raise ValueError(f"pull request {rec['pr']}: {kb} {rec[kb]} is before {ka} {rec[ka]} "
                             f"(a landing passes {' → '.join(STAMPS)})")
    for k in ("bounces", "pre_queue_reds", "local_gate_runs"):
        if rec.get(k) is not None and rec[k] < 0:
            raise ValueError(f"--{k.replace('_', '-')} {rec[k]} is negative")
    for k in ("ci_wall_s", "local_gate_s"):
        if rec.get(k) is not None and rec[k] < 0:
            raise ValueError(f"--{k.replace('_', '-')} {rec[k]} is negative")
    bad = [c for c in rec["bounce_causes"] if c not in CAUSES]
    if bad:
        raise ValueError(f"bounce cause {bad} is not one of {', '.join(CAUSES)}")
    if rec["bounce_causes"] and rec.get("bounces") is None:
        raise ValueError("--bounce-causes without --bounces: say how many bounces the causes name")
    if rec.get("bounces") is not None and len(rec["bounce_causes"]) > rec["bounces"]:
        raise ValueError(f"{len(rec['bounce_causes'])} bounce cause(s) name more than the {rec['bounces']} bounce(s)")
    return rec


def patch_record(existing: dict | None, patch: dict) -> dict:
    """`existing` with every non-None field of `patch` laid over it, validated. The FIRST `enqueued` and `done_each`
    are kept and `done` only moves earlier: a re-enqueue after a bounce is the same landing, still waiting."""
    rec = {"schema": SCHEMA, "pr": patch["pr"], "branch": None, "changes": 1, "done": None, "review_clean": None,
           "enqueued": None, "merged": None, "bounces": None, "bounce_causes": [], "pre_queue_reds": None,
           "done_each": [], "ci_wall_s": None,
           "local_gate_s": None, "local_gate_runs": None, "note": "", **(existing or {})}
    for k, v in patch.items():
        if v is None or (k in ("bounce_causes", "done_each") and not v):
            continue
        if k in ("enqueued", "done_each") and rec.get(k):
            continue
        if k == "done" and rec.get("done") and _when(v) >= _when(rec["done"]):
            continue
        rec[k] = v
    return validate(rec)


def record(path: pathlib.Path, patch: dict) -> dict:
    recs = read_all(path)
    old = next((r for r in recs if r.get("pr") == patch["pr"]), None)
    new = patch_record(old, patch)
    _write_all(path, [*(r for r in recs if r is not old), new])
    return new


# ── the figures ──────────────────────────────────────────────────────────────────────────────────────────────────

def percentile(values: list[float], p: float) -> float:
    """Nearest-rank percentile (p in 0..100) of a non-empty list."""
    s = sorted(values)
    return s[max(0, min(len(s) - 1, -(-len(s) * p // 100) - 1))] if p else s[0]


def median(values: list[float]) -> float:
    s = sorted(values)
    m = len(s) // 2
    return s[m] if len(s) % 2 else (s[m - 1] + s[m]) / 2


def _minutes(a: str, b: str) -> float:
    return (_when(b) - _when(a)).total_seconds() / 60


def _dones(rec: dict) -> list[str]:
    """Each change's finish (`done_each`), else the one `done` a hand `record --done` gave, else none."""
    return rec.get("done_each") or ([rec["done"]] if rec.get("done") else [])


def after_figures(recs: list[dict]) -> dict:
    """The after column from the landing records (`merged` ones are the landings)."""
    landed = [r for r in recs if r.get("merged")]
    out: dict = {"records": len(recs), "landings": len(landed), "changes": sum(r["changes"] for r in landed)}
    starts = [_when(r.get("done") or r["enqueued"]) for r in landed if r.get("done") or r.get("enqueued")]
    if landed and starts:
        window_h = (max(_when(r["merged"]) for r in landed) - min(starts)).total_seconds() / 3600
        out["window_h"] = window_h
        out["per_h"] = out["changes"] / window_h if window_h > 0 else None
    mins = [_minutes(d, r["merged"]) for r in landed for d in _dones(r)]
    out["done_to_main"] = ({"n": len(mins), "median": median(mins), "p90": percentile(mins, 90)} if mins else None)
    out["no_done"] = sum(1 for r in landed if not _dones(r))
    counted = [r for r in landed if r.get("bounces") is not None]
    causes = {c: sum(r["bounce_causes"].count(c) for r in counted) for c in CAUSES}
    out["bounces"] = {"n": len(counted), "total": sum(r["bounces"] for r in counted), "causes": causes,
                      "unattributed": sum(r["bounces"] - len(r["bounce_causes"]) for r in counted)}
    seen = [r["pre_queue_reds"] for r in landed if r.get("pre_queue_reds") is not None]
    out["pre_queue_reds"] = {"n": len(seen), "total": sum(seen)}
    ci = [r["ci_wall_s"] / 60 for r in landed if r.get("ci_wall_s") is not None]
    out["ci_wall"] = {"n": len(ci), "median": median(ci), "p90": percentile(ci, 90)} if ci else None
    gate = [r["local_gate_s"] / 60 for r in landed if r.get("local_gate_s") is not None]
    out["local_gate"] = {"n": len(gate), "median": median(gate), "total": sum(gate)} if gate else None
    return out


def before_figures(trains: list[dict]) -> dict | None:
    """The before column from the retired train records (batched gating, the policy in force on 2026-10-10) and the
    frozen evidence. A train's minutes (lander start → push) are a FLOOR for DONE → main: the implementers' wait for a
    lander is not in them."""
    rs = [r for r in trains if r.get("gating") == "batched"]
    if not rs:
        return None
    landed = sum(r["landed"] for r in rs)
    window_h = (max(_when(r["pushed"]) for r in rs) - min(_when(r["started"]) for r in rs)).total_seconds() / 3600
    if landed < 1 or window_h <= 0:       # a rate needs a landed change over a window above zero
        return None
    per_change = [m for r in rs for m in [_minutes(r["started"], r["pushed"])] * r["landed"]]
    return {"trains": len(rs), "changes": landed, "window_h": window_h, "per_h": landed / window_h,
            "floor": {"median": median(per_change), "p90": percentile(per_change, 90)} if per_change else None,
            "ejected": sum(r["ejected"] for r in rs), "ci_reds": sum(r["ci"] == "red" for r in rs)}


def headline(recs: list[dict], trains: list[dict]) -> dict | None:
    """The one landing rate the completion ledger and its remaining-work estimate use: the merge queue's own when at
    least two pull requests have landed over a window above zero, else the retired trains' batched rate. Its
    `median_measures` says what `median_min` is: DONE → on main only for the merge queue; the trains' minutes are
    lander start → push, a floor (before_figures), and a reader prints that label, never its own."""
    a = after_figures(recs)
    if a["landings"] >= 2 and a.get("per_h"):
        d = a["done_to_main"]
        return {"per_h": a["per_h"], "changes": a["changes"], "window_h": a["window_h"], "landings": a["landings"],
                "source": "pull requests through the merge queue", "median_min": d["median"] if d else None,
                "median_measures": MEASURES_AFTER}
    b = before_figures(trains)
    if b:
        f = b["floor"]
        return {"per_h": b["per_h"], "changes": b["changes"], "window_h": b["window_h"], "landings": b["trains"],
                "source": "the retired trains, batched gating", "median_min": f["median"] if f else None,
                "median_measures": MEASURES_TRAINS}
    return None


def _cell(v: float | None, fmt: str = "{:.1f}") -> str:
    return UNMEASURED if v is None else fmt.format(v)


def summarize(recs: list[dict], trains: list[dict], since: dt.datetime | None = None) -> list[str]:
    if since is not None:
        recs = [r for r in recs if r.get("enqueued") and _when(r["enqueued"]) >= since]
    a, b = after_figures(recs), before_figures(trains)
    rows: list[tuple[str, str, str]] = []
    rows.append(("changes landed per hour",
                 (f"{a['per_h']:.2f} ({a['changes']} change(s), {a['landings']} pull request(s), {a['window_h']:.1f} h)"
                  if a.get("per_h") else (f"{UNMEASURED} (" + (f"{a['landings']} landing(s); a rate needs a window above zero"
                                                              if a["landings"] else "no pull request has landed yet") + ")")),
                 f"{b['per_h']:.2f} ({b['changes']} changes, {b['trains']} trains, {b['window_h']:.1f} h)" if b else UNMEASURED))
    d = a["done_to_main"]
    rows.append(("minutes DONE → on main per change, median / p90",
                 (f"{d['median']:.0f} / {d['p90']:.0f} ({d['n']} change(s))" if d else UNMEASURED)
                 + (f"; {a['no_done']} landing(s) with no DONE time" if a["no_done"] else ""),
                 (f"{b['floor']['median']:.0f} / {b['floor']['p90']:.0f} per landed change (lander start → push: a floor)"
                  if b and b["floor"] else UNMEASURED)))
    bo = a["bounces"]
    causes = ", ".join(f"{c} {n}" for c, n in bo["causes"].items() if n)
    rows.append(("bounces per landing",
                 (f"{bo['total'] / bo['n']:.2f} ({bo['total']} in {bo['n']} landing(s)"
                  + (f"; {causes}" if causes else "") + (f"; {bo['unattributed']} UNATTRIBUTED" if bo["unattributed"] else "")
                  + ")") if bo["n"] else UNMEASURED,
                 f"{b['ejected'] / b['changes']:.2f} ejected to a finisher per landed change; {b['ci_reds']} CI red(s); "
                 "causes UNMEASURED" if b else UNMEASURED))
    pq = a["pre_queue_reds"]
    rows.append(("pull-request reds before the queue per landing",
                 (f"{pq['total'] / pq['n']:.2f} ({pq['total']} in {pq['n']} landing(s))" if pq["n"] else UNMEASURED)
                 + (f"; {a['landings'] - pq['n']} landing(s) UNMEASURED" if pq["n"] and a["landings"] > pq["n"] else ""),
                 f"{UNMEASURED} (a train opened no pull request)"))
    ci = a["ci_wall"]
    rows.append(("merge-group CI wall, minutes",
                 (f"median {ci['median']:.1f}, p90 {ci['p90']:.1f} ({ci['n']} landing(s))" if ci else UNMEASURED),
                 f"{EVIDENCE['ci_wall_min']} (train 1057's head, evidence); push-main + CI "
                 f"{EVIDENCE['push_ci_minutes'] / EVIDENCE['clusters_landed']:.1f} per landed change "
                 f"({EVIDENCE['push_ci_pct']:g} % of lander minutes)"))
    g = a["local_gate"]
    rows.append(("local gate minutes per landing",
                 (f"median {g['median']:.1f}, total {g['total']:.0f} ({g['n']} landing(s))" if g else UNMEASURED),
                 f"{EVIDENCE['gate_minutes'] / EVIDENCE['clusters_landed']:.1f} in the landers' gates per landed change "
                 f"({EVIDENCE['gate_pct']} % of {EVIDENCE['lander_minutes']} lander minutes)"))
    w = max(len(r[0]) for r in rows)
    return [f"{'':<{w}}  AFTER (merge queue, this file)  |  BEFORE (retired trains, 2026-10-10 evidence)"] + \
           [f"{k:<{w}}  {x}  |  {y}" for k, x, y in rows]


def landing_rate_input(coord_dir: pathlib.Path) -> dict | None:
    """`headline` over the files on this machine (None when neither exists): the ledger's landing-rate input."""
    recs = read_all(coord_dir / FILE) if coord_dir.is_dir() else []
    return headline(recs, read_all(before_path(coord_dir)))


def before_path(coord_dir: pathlib.Path | None = None) -> pathlib.Path:
    """The frozen copy once `freeze` has run (tracked, so every machine sees it), else the coordination directory's."""
    return FROZEN_TRAINS if FROZEN_TRAINS.exists() else (coord_dir or coord.coord_path()) / RETIRED_TRAINS


# ── observation: what enqueue.py can see, and what `complete` reads from GitHub ──────────────────────────────────

def _report_header(p: pathlib.Path) -> str:
    """A report's header: its lines above the first `## ` heading (the same header plan_wave and the review stamp read)."""
    head = []
    for i, line in enumerate(p.read_text(encoding="utf-8", errors="replace").splitlines()):
        if i > 0 and line.startswith("## "):
            break
        head.append(line)
    return "\n".join(head)


def _names_branch(header: str, branch: str) -> bool:
    """The header names `branch` on a line that says `branch` (exactly that name, not a longer one containing it)."""
    pat = re.compile(r"(?<![\w/.\-])" + re.escape(branch) + r"(?![\w/.\-])")
    return any("branch" in ln.lower() and pat.search(ln) for ln in header.splitlines())


def git_runner(cwd: pathlib.Path) -> Git:
    def run(args: list[str]) -> tuple[int, str]:
        p = subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")
        return p.returncode, p.stdout
    return run


def done_times(branch: str, reports_dir: pathlib.Path, not_after: str, git: Git,
               upstream: str = "refs/remotes/origin/main") -> list[str]:
    """When each change this landing carries was FINISHED: one ISO time per change, earliest first ([] when none is
    known). The time is git's, never a file's: the committer time of the HEAD commit a DONE implementer report names
    (the implementer's last commit, minutes before its report). A report file's time is NOT used, because the branch
    review rewrites the report it stamps (review_findings.py stamp, kb/Work PB3027) and a finisher's re-review re-stamps
    its own, so a file time is the LAST rewrite, never the finish (review finding R1-1 of PB3028's branch).

    The changes are found from two seeds, each a DONE report (plan_wave.parse_report: its status, lead note and HEAD):
      - a report whose header names `branch` (the implementer's own branch, queued as it is);
      - a report a commit in `upstream..HEAD` names on its `Report:` line: each cluster commit of a lander's train
        carries one (the retired train applier wrote it), and no implementer report names that train's branch
        (review finding R1-2).
    A change is one LEAD note: its time is the earliest of every DONE report with that lead whose HEAD is an ancestor
    of the seed's HEAD — so a review finisher's branch, which merged its predecessor's (kb/Work PB3027, I10), keeps the
    first implementer's finish, and the review and finisher wait is inside DONE → on main. (A branch REBASED onto main
    rewrites its predecessor's commits; the chain then ends at the finisher's own report.) A report whose HEAD commit
    is after `not_after` (the queue) is no DONE of this landing, and a lander's report (status LANDED) is no seed.
    A report whose HEAD is already on `upstream` is a change that LANDED before this one: it is neither a seed nor in
    a chain (review finding R1-1 of PB3028's finisher). The merge queue merges with method MERGE (ruleset-main.json),
    so every landed branch head is an ancestor of main and of every branch cut after it; without this a later landing
    under a reused lead note (PB2426 had seven separate records landings; PB3024 lands in phases) would take the FIRST
    landing's finish as its DONE. plan_wave's branch survey applies the same rule: an ancestor of origin/main landed."""
    import plan_wave  # noqa: PLC0415  (THE report reader: status, lead and HEAD are parsed in one place)
    reps = [r for r in plan_wave.load_reports(reports_dir) if r.status == "DONE" and r.head]
    if not reps:
        return []
    rc, log = git(["log", "--format=%B", f"{upstream}..HEAD"])
    named = {pathlib.PureWindowsPath(m.group(1).strip()).name for m in TRAILER_REPORT.finditer(log if rc == 0 else "")}
    seeds = [r for r in reps if r.path.name in named or _names_branch(_report_header(r.path), branch)]
    limit = _when(not_after)
    times: dict[str, str | None] = {}

    def finished(sha: str) -> str | None:
        if sha not in times:
            rc, out = git(["show", "-s", "--format=%cI", sha])
            t = out.strip() if rc == 0 and out.strip() else None
            times[sha] = t if t and _when(t) <= limit else None
        return times[sha]

    def is_ancestor(a: str, b: str) -> bool:
        return a == b or git(["merge-base", "--is-ancestor", a, b])[0] == 0

    landed: dict[str, bool] = {}

    def unlanded(r: plan_wave.Report) -> bool:
        if r.head not in landed:
            landed[r.head] = is_ancestor(r.head, upstream)
        return not landed[r.head]

    per_lead: dict[str, dt.datetime] = {}
    for s in filter(unlanded, seeds):
        chain = [r for r in reps if r.lead == s.lead and (r is s or is_ancestor(r.head, s.head)) and unlanded(r)]
        found = [_when(t) for r in chain if (t := finished(r.head))]
        if found:
            per_lead[s.lead] = min([*found, *([per_lead[s.lead]] if s.lead in per_lead else [])])
    return [t.isoformat() for t in sorted(per_lead.values())]


def local_gate(worktree: pathlib.Path) -> tuple[float, int] | None:
    """(seconds, runs) over every gate the worktree ran: the sum of `timings.wall_s` in its verdict.json files."""
    secs, runs = 0.0, 0
    for p in (worktree / "TestResults" / "build-local").glob("*/verdict.json"):
        try:
            secs += float(json.loads(p.read_text(encoding="utf-8"))["timings"]["wall_s"])
            runs += 1
        except (OSError, ValueError, KeyError, TypeError):
            continue
    return (secs, runs) if runs else None


def record_enqueued(path: pathlib.Path, worktree: pathlib.Path, branch: str, pr: int, reports_dir: pathlib.Path,
                    changes: int | None = None, enqueued: str | None = None, git: Git | None = None) -> dict:
    """What `enqueue.py queue` can record the moment a pull request is queued: the branch, the enqueue time, each
    carried change's finish (`done_times`, run in the landing worktree) and the local gate seconds from the worktree's
    verdict.json files. `changes` defaults to the changes found (one per lead note), else 1."""
    at = enqueued or now_iso()
    g = local_gate(worktree)
    each = done_times(branch, reports_dir, at, git or git_runner(worktree))
    return record(path, {"pr": pr, "branch": branch, "changes": changes or len(each) or None, "enqueued": at,
                         "done": each[0] if each else None, "done_each": each,
                         "local_gate_s": round(g[0], 1) if g else None, "local_gate_runs": g[1] if g else None})


def complete(path: pathlib.Path, gh: Gh, only: int | None = None, say: Say = print) -> int:
    """Complete every record with no `merged` time (or the one pull request `only`) from GitHub: `mergedAt`, the count
    of failed merge-group runs of the pull request (bounces), the count of its own failed `pull_request` runs on its head
    branch (pre_queue_reds, kb/Work PB3096) and the successful merge-group run's wall (CI wall). Returns 0 when every
    record asked about was read, 2 when GitHub could not be asked."""
    recs = [r for r in read_all(path) if (only is None and not r.get("merged")) or r["pr"] == only]
    if only is not None and not recs:
        say(f"landing-measure: no record for pull request {only}")
        return 2
    if not recs:
        say("landing-measure: every recorded pull request is already complete")
        return 0
    rc, out, err = gh(["run", "list", "--workflow", WORKFLOW, "--event", "merge_group", "--limit", "200",
                       "--json", "headBranch,conclusion,startedAt,updatedAt"])
    if rc != 0:
        say(f"⛔ landing-measure: gh run list failed (exit {rc}): {(out + err).strip()} — nothing completed")
        return 2
    runs = json.loads(out or "[]")
    bad = 0
    for r in recs:
        rc, out, err = gh(["pr", "view", str(r["pr"]), "--json", "state,mergedAt"])
        if rc != 0:
            say(f"⛔ landing-measure: gh pr view {r['pr']} failed (exit {rc}): {(out + err).strip()}")
            bad += 1
            continue
        pr = json.loads(out)
        if not pr.get("mergedAt"):
            say(f"landing-measure: PR #{r['pr']} is {pr.get('state', '?')} and not merged — left open")
            continue
        # The pull request's own runs (the cheap jobs, before the queue): a red there never reaches a merge group, so
        # `bounces` cannot count it (kb/Work PB3096). Every pull request has at least one such run, so none at all is
        # an observation missed (an expired or truncated listing), recorded as null, never as 0.
        rc, out, err = gh(["run", "list", "--workflow", WORKFLOW, "--event", "pull_request", "--branch", r["branch"],
                           "--limit", "100", "--json", "conclusion"])
        if rc != 0:
            say(f"⛔ landing-measure: gh run list --event pull_request --branch {r['branch']} failed (exit {rc}): "
                f"{(out + err).strip()}")
            bad += 1
            continue
        own = json.loads(out or "[]")
        mine = [x for x in runs if re.search(rf"/pr-{r['pr']}-", x.get("headBranch") or "")]
        ok = [x for x in mine if x.get("conclusion") == "success"]
        patch: dict = {"pr": r["pr"], "merged": pr["mergedAt"],
                       "bounces": sum(x.get("conclusion") in ("failure", "timed_out") for x in mine)}
        if own:
            patch["pre_queue_reds"] = sum(x.get("conclusion") in ("failure", "timed_out") for x in own)
        else:
            say(f"⚠ landing-measure: PR #{r['pr']}: no pull_request run of {WORKFLOW} on branch {r['branch']} — "
                "pre-queue reds UNMEASURED")
        if ok:
            x = max(ok, key=lambda y: y["updatedAt"])
            patch["ci_wall_s"] = round((_when(x["updatedAt"]) - _when(x["startedAt"])).total_seconds(), 1)
        else:
            say(f"⚠ landing-measure: PR #{r['pr']} merged but no successful merge-group run of {WORKFLOW} names it — "
                "bounces and CI wall recorded as observed, the CI wall UNMEASURED")
        try:
            new = record(path, patch)
        except ValueError as e:
            say(f"⛔ landing-measure: PR #{r['pr']}: {e}")
            bad += 1
            continue
        say(f"landing-measure: completed PR #{new['pr']}: merged {new['merged']}, {new['bounces']} bounce(s), "
            f"{_cell(new['pre_queue_reds'], '{}')} pre-queue red(s), CI wall {_cell(new['ci_wall_s'] and new['ci_wall_s'] / 60)} min")
    return 2 if bad else 0


def freeze(coord_dir: pathlib.Path, say: Say = print, today: str | None = None, frozen: pathlib.Path = FROZEN_TRAINS,
           first_after: pathlib.Path = FIRST_AFTER) -> int:
    """Copy the retired train file and the first after-summary under the evidence directory. Refused until a pull
    request has landed (the first after-summary needs a landing), when the first after-summary already exists, and when
    a frozen copy already differs: frozen evidence is written once and never edited."""
    src = coord_dir / RETIRED_TRAINS
    recs = read_all(coord_dir / FILE)
    if not any(r.get("merged") for r in recs):
        say("⛔ freeze: no pull request has landed yet — the first after-summary needs one")
        return 2
    if first_after.exists():
        say(f"⛔ freeze: {first_after} exists — the first after-summary is written once and never edited")
        return 2
    if not src.exists() and not frozen.exists():
        say(f"⛔ freeze: {src} does not exist and nothing is frozen")
        return 2
    if src.exists():
        data = src.read_bytes().replace(b"\r\n", b"\n")
        if frozen.exists() and frozen.read_bytes() != data:
            say(f"⛔ freeze: {frozen} exists and differs from {src}; the evidence is never edited")
            return 2
        frozen.write_bytes(data)
    lines = summarize(recs, read_all(frozen))
    first_after.write_text(
        f"# The first after-summary ({today or dt.date.today().isoformat()})\n\n> FROZEN EVIDENCE: written by "
        "`landing_measure.py freeze`, never edited.\n\n```\n" + "\n".join(lines) + "\n```\n",
        encoding="utf-8", newline="\n")
    say(f"landing-measure: froze {frozen.name} and {first_after.name} under {frozen.parent}")
    return 0


# ── command line ─────────────────────────────────────────────────────────────────────────────────────────────────

def parser() -> argparse.ArgumentParser:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="prove the record, the figures and the completion")
    ap.add_argument("--coord", help=f"the coordination directory (default: ${coord.ENV}, else {coord.DEFAULT})")
    sub = ap.add_subparsers(dest="action")
    r = sub.add_parser("record", help="set fields of one pull request's landing record (a patch)")
    r.add_argument("--pr", type=int, required=True)
    r.add_argument("--branch", help="the branch the pull request lands (required on a landing's first record)")
    r.add_argument("--changes", type=int, help="changes the pull request lands (default 1; a lander's train is several)")
    for k in STAMPS:
        r.add_argument("--" + k.replace("_", "-"), help=f"ISO time with offset: {k.replace('_', ' ')}")
    r.add_argument("--bounces", type=int)
    r.add_argument("--bounce-causes", help=f"comma-separated, one per bounce, of: {', '.join(CAUSES)}")
    r.add_argument("--pre-queue-reds", type=int, help="the pull request's own failed runs before the queue took it")
    r.add_argument("--ci-wall-s", type=float)
    r.add_argument("--local-gate-s", type=float)
    r.add_argument("--local-gate-runs", type=int)
    r.add_argument("--note")
    c = sub.add_parser("complete", help="read merge time, bounces, pre-queue reds and CI wall from GitHub for unmerged "
                                        "records")
    c.add_argument("--pr", type=int, help="only this pull request (merged ones too)")
    s = sub.add_parser("summary", help="the after column beside the before column")
    s.add_argument("--since", help="only landings enqueued at or after this ISO time")
    s.add_argument("--before", help="the train records to compare against (default: the frozen copy, else the "
                                    "coordination directory's)")
    sub.add_parser("freeze", help="freeze the retired train records and the first after-summary under the evidence directory")
    return ap


def patch_from_args(a: argparse.Namespace) -> dict:
    p = {"pr": a.pr, "branch": a.branch, "changes": a.changes, "bounces": a.bounces,
         "pre_queue_reds": a.pre_queue_reds, "ci_wall_s": a.ci_wall_s,
         "local_gate_s": a.local_gate_s, "local_gate_runs": a.local_gate_runs, "note": a.note,
         "bounce_causes": [c.strip() for c in a.bounce_causes.split(",") if c.strip()] if a.bounce_causes else []}
    for k in STAMPS:
        v = getattr(a, k)
        p[k] = _when(v).isoformat() if v else None
    return p


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
ARMS = ("a record is a patch: later calls fill fields, the first enqueue time stays, and every stamp must follow the last",
        "an inconsistent record is refused: stamps out of order, an unknown cause, causes with no bounces, a bad number",
        "done is each carried change's earliest DONE report by its HEAD commit's time, never a report file's time: a "
        "finisher keeps its predecessor's, a train's changes come from its Report lines, nothing after the queue or "
        "already on main counts",
        "local gate seconds are the sum of every verdict.json in the worktree",
        "complete reads the merge time, counts only failed merge-group runs as bounces and takes the CI wall of the "
        "successful run",
        "summary sets the after column beside the before column, names unattributed bounces and says UNMEASURED for a "
        "figure it never saw",
        "the headline is the merge queue's rate from two landings and the retired trains' rate before that, each "
        "saying what its minutes measure",
        "freeze waits for a landing, refuses to rewrite the frozen copy, and the evidence figures are in the evidence file",
        "complete counts the pull request's own failed runs before the queue on its head branch (pre_queue_reds), and a "
        "branch with no pull-request run stays UNMEASURED, never 0")


def _self_test(tmp: pathlib.Path) -> list[str]:
    fails: list[str] = []
    patch_arm, refuse_arm, done_arm, gate_arm, complete_arm, summary_arm, headline_arm, freeze_arm, prequeue_arm = ARMS
    path = tmp / FILE
    z = "2026-10-12T"

    def t(hm: str) -> str:
        return f"{z}{hm}:00-07:00"

    # patch
    record(path, {"pr": 17, "branch": "b17", "enqueued": t("10:00"), "local_gate_s": 300.0, "local_gate_runs": 2})
    record(path, {"pr": 17, "done": t("09:00"), "review_clean": t("09:30"), "enqueued": t("10:30")})
    r = read_all(path)[0]
    if r["enqueued"] != t("10:00") or r["done"] != t("09:00") or r["branch"] != "b17" or r["local_gate_s"] != 300.0:
        fails.append(f"{patch_arm}: {r}")
    record(path, {"pr": 17, "done": t("09:20")})                         # a later DONE never moves it later
    if read_all(path)[0]["done"] != t("09:00"):
        fails.append(f"{patch_arm}: a later done moved the earliest: {read_all(path)[0]}")
    cli = patch_from_args(parser().parse_args(["record", "--pr", "5", "--branch", "b5", "--changes", "3", "--enqueued",
                                               t("10:00"), "--bounces", "2", "--bounce-causes", "conflict, load-timing",
                                               "--pre-queue-reds", "1"]))
    made = patch_record(None, cli)
    if (made["changes"], made["bounces"], made["bounce_causes"], made["branch"], made["pre_queue_reds"]) \
            != (3, 2, ["conflict", "load-timing"], "b5", 1):
        fails.append(f"{patch_arm}: the command line's mapping: {made}")
    # refusals
    for name, p in (("merged before enqueued", {"pr": 17, "merged": t("09:50")}),
                    ("an unknown cause", {"pr": 17, "bounces": 1, "bounce_causes": ["flake"]}),
                    ("causes with no bounces", {"pr": 17, "bounce_causes": ["conflict"]}),
                    ("more causes than bounces", {"pr": 17, "bounces": 1, "bounce_causes": ["conflict", "conflict"]}),
                    ("a negative gate", {"pr": 17, "local_gate_s": -1.0}),
                    ("negative pre-queue reds", {"pr": 17, "pre_queue_reds": -1}),
                    ("no branch on a first record", {"pr": 18, "enqueued": t("10:00")}),
                    ("a time with no offset", {"pr": 17, "merged": "2026-10-12T11:00:00"})):
        try:
            record(path, {k: (_when(v).isoformat() if k in STAMPS and v else v) for k, v in p.items()})
            fails.append(f"{refuse_arm}: accepted {name}")
        except ValueError:
            pass
    if len(read_all(path)) != 1 or read_all(path)[0]["bounces"] is not None:
        fails.append(f"{refuse_arm}: a refused patch changed the file: {read_all(path)}")
    # done_times: git's commit time of each DONE report's HEAD, never the file's time (review findings R1-1, R1-2)
    rd = tmp / "reports"
    rd.mkdir()

    def report(name: str, status: str, branch: str, head: str) -> pathlib.Path:
        p = rd / name
        p.write_text(f"# {name}\n\n**Status:** {status} · **branch:** `{branch}` · **HEAD:** `{head}`\n"
                     f"**Review:** CLEAN r {head}\n\n## Rule\n", encoding="utf-8")
        os.utime(p, (1_900_000_000, 1_900_000_000))        # stamped long after: a file time would be the stamp's
        return p
    report("w1-PB1-report.md", "DONE", "b17", "aaaaaaa1")          # the implementer
    report("w2-PB1-report.md", "DONE", "b17f", "bbbbbbb2")         # its review finisher, which merged b17
    report("w1-PB2-report.md", "DONE", "c2", "ccccccc3")           # another change, brought into a train
    report("w1-PB3-report.md", "SPLIT", "b17", "ddddddd4")         # not finished
    report("w3-PB4-report.md", "DONE", "b17", "eeeeeee5")          # committed after the queue
    report("w1-PB5-report.md", "DONE", "b170", "fffffff6")         # a longer branch name
    report("w0-PB1-report.md", "DONE", "b16", "1111111a")          # PB1's EARLIER landing, merged into main (R1-1)
    report("w0-PB6-report.md", "DONE", "b17", "2222222b")          # a landed change under a reused branch name
    (rd / "t1-lander-report.md").write_text("Status: LANDED\nBranch: b17\nHEAD: aaaaaaa9\n", encoding="utf-8")
    commits = {"aaaaaaa1": t("09:00"), "bbbbbbb2": t("09:50"), "ccccccc3": t("08:00"), "ddddddd4": t("07:00"),
               "eeeeeee5": t("23:00"), "fffffff6": t("06:00"), "aaaaaaa9": t("05:00"), "1111111a": t("01:00"),
               "2222222b": t("02:00")}
    main = "refs/remotes/origin/main"           # the merge queue's MERGE method: a landed head is main's ancestor
    on_main = frozenset({("1111111a", main), ("1111111a", "aaaaaaa1"), ("1111111a", "bbbbbbb2"), ("2222222b", main)})

    def fake_git(log: str = "", ancestry: frozenset = frozenset({("aaaaaaa1", "bbbbbbb2")}) | on_main) -> Git:
        def run(args: list[str]) -> tuple[int, str]:
            if args[0] == "log":
                return 0, log
            if args[:2] == ["show", "-s"]:
                return (0, commits[args[-1]] + chr(10)) if args[-1] in commits else (128, "")
            if args[:2] == ["merge-base", "--is-ancestor"]:
                return (0 if (args[2], args[3]) in ancestry else 1), ""
            return 2, ""
        return run
    q = t("10:30")
    trailers = chr(10).join(["Cluster A brought in by the train applier", "Notes: PB2",
                             "Report: E:" + chr(92) + "COBOL-coord" + chr(92) + "w1-PB2-report.md", "",
                             "Report: (none named)", "Report: /coord/scratch/reports/w2-PB1-report.md"])
    got = {"own": done_times("b17", rd, q, fake_git()),
           "finisher": done_times("b17f", rd, q, fake_git()),
           "rebased finisher": done_times("b17f", rd, q, fake_git(ancestry=on_main)),
           "train": done_times("lander-x", rd, q, fake_git(trailers)),
           "before every finish": done_times("b17", rd, t("08:30"), fake_git()),
           "no report": done_times("zzz", rd, q, fake_git())}
    want = {"own": [t("09:00")], "finisher": [t("09:00")], "rebased finisher": [t("09:50")],
            "train": [t("08:00"), t("09:00")], "before every finish": [], "no report": []}
    rpath = tmp / "enqueued.jsonl"
    rec = record_enqueued(rpath, tmp / "no-worktree", "lander-x", 40, rd, enqueued=q, git=fake_git(trailers))
    if got != want or (rec["done"], rec["done_each"], rec["changes"]) != (t("08:00"), want["train"], 2):
        fails.append(f"{done_arm}: {got} (want {want}); enqueued record {rec}")
    # local gate
    wt = tmp / "wt"
    for i, w in enumerate((100.5, 200.0)):
        d = wt / "TestResults" / "build-local" / f"run{i}"
        d.mkdir(parents=True)
        (d / "verdict.json").write_text(json.dumps({"timings": {"wall_s": w}}), encoding="utf-8")
    bad = wt / "TestResults" / "build-local" / "runbad"
    bad.mkdir()
    (bad / "verdict.json").write_text("{}", encoding="utf-8")
    if local_gate(wt) != (300.5, 2) or local_gate(tmp / "nowhere") is not None:
        fails.append(f"{gate_arm}: {local_gate(wt)}")
    # complete
    runs = [{"headBranch": "gh-readonly-queue/main/pr-17-aaa", "conclusion": "failure", "startedAt": t("10:40").replace("-07:00", "Z"),
             "updatedAt": t("11:00").replace("-07:00", "Z")},
            {"headBranch": "gh-readonly-queue/main/pr-17-bbb", "conclusion": "cancelled", "startedAt": "2026-10-12T18:00:00Z",
             "updatedAt": "2026-10-12T18:05:00Z"},
            {"headBranch": "gh-readonly-queue/main/pr-17-ccc", "conclusion": "success", "startedAt": "2026-10-12T18:10:00Z",
             "updatedAt": "2026-10-12T18:25:30Z"},
            {"headBranch": "gh-readonly-queue/main/pr-170-ddd", "conclusion": "failure", "startedAt": "2026-10-12T18:10:00Z",
             "updatedAt": "2026-10-12T18:11:00Z"}]

    # the pull request's own runs, per head branch: two reds before the queue (a failure, a timeout), a green re-run, and
    # a run cancelled because its head moved (superseded, not a red); branch b19 has none at all
    pr_runs = {"b17": [{"conclusion": "failure"}, {"conclusion": "timed_out"}, {"conclusion": "cancelled"},
                       {"conclusion": "success"}]}

    def gh(args: list[str]) -> tuple[int, str, str]:
        if args[:2] == ["run", "list"]:
            if args[args.index("--event") + 1] == "pull_request":
                return 0, json.dumps(pr_runs.get(args[args.index("--branch") + 1], [])), ""
            return 0, json.dumps(runs), ""
        if args[:2] == ["pr", "view"]:
            return 0, json.dumps({"state": "MERGED", "mergedAt": "2026-10-12T18:26:00Z"}), ""
        return 1, "", "unrouted"
    lines: list[str] = []
    rc = complete(path, gh, say=lines.append)
    r = read_all(path)[0]
    if rc != 0 or r["bounces"] != 1 or r["ci_wall_s"] != 930.0 or not r["merged"].startswith("2026-10-12T18:26"):
        fails.append(f"{complete_arm}: rc={rc} {r} {lines}")
    if r["pre_queue_reds"] != 2:
        fails.append(f"{prequeue_arm}: {r['pre_queue_reds']} pre-queue red(s), not 2: {lines}")
    unseen = tmp / "unseen.jsonl"
    record(unseen, {"pr": 19, "branch": "b19", "enqueued": t("10:00")})
    seen_lines: list[str] = []
    rc19 = complete(unseen, gh, say=seen_lines.append)
    if rc19 != 0 or read_all(unseen)[0]["pre_queue_reds"] is not None \
            or not any("pre-queue reds UNMEASURED" in x for x in seen_lines):
        fails.append(f"{prequeue_arm}: an unobserved branch: rc={rc19} {read_all(unseen)[0]} {seen_lines}")
    def gh_down(args: list[str]) -> tuple[int, str, str]:
        if args[:2] == ["run", "list"] and "pull_request" in args:
            return 1, "", "HTTP 502"
        return gh(args)
    down = tmp / "down.jsonl"
    record(down, {"pr": 20, "branch": "b20", "enqueued": t("10:00")})
    if complete(down, gh_down, say=seen_lines.append) != 2 or read_all(down)[0]["merged"] is not None:
        fails.append(f"{prequeue_arm}: a failed pull-request listing completed the record: {read_all(down)[0]}")
    # summary / headline
    record(path, {"pr": 17, "bounce_causes": ["cluster-defect"], "bounces": 1})
    trains = [{"gating": "batched", "started": "2026-10-10T10:00:00-07:00", "pushed": "2026-10-10T12:00:00-07:00",
               "landed": 4, "ejected": 1, "ci": "green"},
              {"gating": "batched", "started": "2026-10-10T12:00:00-07:00", "pushed": "2026-10-10T14:00:00-07:00",
               "landed": 4, "ejected": 0, "ci": "red"},
              {"gating": "whole", "started": "2026-10-09T10:00:00-07:00", "pushed": "2026-10-09T11:00:00-07:00",
               "landed": 9, "ejected": 0, "ci": "green"}]
    one = summarize(read_all(path), trains)
    text = "\n".join(one)
    want = ["0.41 (1 change(s), 1 pull request(s), 2.4 h)", "2.00 (8 changes, 2 trains, 4.0 h)", "146 / 146 (1 change(s))",
            "0.12 ejected to a finisher per landed change; 1 CI red(s); causes UNMEASURED",
            "1.00 (1 in 1 landing(s); cluster-defect 1)", "2.00 (2 in 1 landing(s))",
            "UNMEASURED (a train opened no pull request)", "median 15.5, p90 15.5 (1 landing(s))",
            "median 5.0, total 5 (1 landing(s))"]
    missing = [w for w in want if w not in text]
    if missing:
        fails.append(f"{summary_arm}: missing {missing} in\n{text}")
    record(path, {"pr": 17, "bounces": 2})
    if "1 UNATTRIBUTED" not in "\n".join(summarize(read_all(path), [])) or "UNMEASURED" not in "\n".join(summarize([], [])):
        fails.append(f"{summary_arm}: unattributed / empty\n" + "\n".join(summarize(read_all(path), [])))
    # headline
    h0 = headline(read_all(path), trains)
    record(path, {"pr": 18, "branch": "b18", "done": t("12:00"), "enqueued": t("12:10")})
    patch = {"pr": 18, "merged": "2026-10-12T20:00:00Z"}
    record(path, patch)
    h2 = headline(read_all(path), trains)
    if not (h0 and h0["source"].startswith("the retired trains") and abs(h0["per_h"] - 2.0) < 1e-9
            and h0["median_measures"] == MEASURES_TRAINS and h2 and h2["median_measures"] == MEASURES_AFTER
            and h2 and h2["source"].startswith("pull requests") and h2["landings"] == 2 and h2["per_h"] > 0
            and headline([], []) is None):
        fails.append(f"{headline_arm}: {h0} / {h2}")
    # freeze + the evidence figures
    quotes_missing = [q for q in EVIDENCE_QUOTES if q not in EVIDENCE_MD.read_text(encoding="utf-8")] \
        if EVIDENCE_MD.exists() else ["(the evidence file is missing)"]
    cd = tmp / "coord-empty"
    cd.mkdir()
    lines = []
    waits = freeze(cd, say=lines.append)
    ev = tmp / "evidence"
    ev.mkdir()
    fz, fa = ev / "frozen.jsonl", ev / "first.md"
    (tmp / RETIRED_TRAINS).write_bytes(json.dumps(trains[0]).encode("utf-8") + bytes([13, 10]))
    first = freeze(tmp, say=lines.append, today="2026-10-12", frozen=fz, first_after=fa)
    written = fa.read_text(encoding="utf-8") if fa.exists() else ""
    again = freeze(tmp, say=lines.append, frozen=fz, first_after=fa)            # the first after-summary exists
    fa.unlink()
    (tmp / RETIRED_TRAINS).write_text("{}" + chr(10), encoding="utf-8")
    differs = freeze(tmp, say=lines.append, frozen=fz, first_after=fa)          # the frozen copy would change
    if not (waits == 2 and first == 0 and again == 2 and differs == 2 and not fa.exists() and not quotes_missing
            and bytes([13]) not in fz.read_bytes() and "AFTER (merge queue" in written):
        fails.append(f"{freeze_arm}: waited={waits} first={first} again={again} differs={differs} missing={quotes_missing} {lines}")
    return fails


def self_test() -> int:
    d = pathlib.Path(tempfile.mkdtemp(prefix="landing-measure-"))
    try:
        fails = _self_test(d)
    finally:
        shutil.rmtree(d, ignore_errors=True)
    for name in ARMS:
        bad = [f for f in fails if f.startswith(name)]
        print(f"  {'FAIL' if bad else 'PASS'}  {name}" + "".join(f"\n        {f}" for f in bad))
    print(f"=== landing_measure SELF-TEST: {'PASS' if not fails else 'FAIL'} ({len(ARMS)} arms) ===")
    return 1 if fails else 0


def main(argv: list[str]) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    a = parser().parse_args(argv)
    if a.self_test:
        return self_test()
    try:
        if a.action == "record":
            rec = record(coord.coord_dir(a.coord) / FILE, patch_from_args(a))
            print(f"landing-measure: recorded PR #{rec['pr']} ({rec['branch']})")
            return 0
        if a.action == "complete":
            return complete(coord.coord_dir(a.coord) / FILE, ruleset.real_gh, a.pr)
        if a.action == "summary":
            d = coord.coord_path(a.coord)
            before = pathlib.Path(a.before) if a.before else before_path(d)
            for line in summarize(read_all(d / FILE), read_all(before), _when(a.since) if a.since else None):
                print(line)
            return 0
        if a.action == "freeze":
            return freeze(coord.coord_dir(a.coord))
    except (ValueError, ruleset.Unobserved) as e:
        print(f"landing_measure.py: {e}", file=sys.stderr)
        return 2
    parser().print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
