#!/usr/bin/env python3
"""landing_measure.py — the landing pipeline's measurement, one record per pull request (kb/Work PB3028).

    python scripts/orchestrator/landing_measure.py record --pr 17 [--branch B] [--changes N] [--done ISO] \
        [--review-clean ISO] [--enqueued ISO] [--merged ISO] [--bounces N] [--bounce-causes CAUSE[,CAUSE...]] \
        [--ci-wall-s S] [--local-gate-s S] [--local-gate-runs N] [--note TEXT]
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
  done          the implementer's finish (its report's time) — `enqueue.py queue` fills it from the reports directory
  review_clean  the finish review came back clean — whoever ran the review records it (`record --review-clean`)
  enqueued      the FIRST time the branch was queued (a bounce and a re-enqueue keep it: the wait includes the bounce)
  merged        GitHub's `mergedAt` — `complete` reads it
  bounces       merge-group runs of this pull request that FAILED (a run GitHub cancelled because an earlier entry
                failed is a ripple, not this pull request's bounce); `complete` counts them
  bounce_causes one of CAUSES per bounce, named by the finisher that fixed it; a bounce with no cause is counted
                UNATTRIBUTED, never guessed
  ci_wall_s     the successful merge-group run's wall time (`complete`)
  local_gate_s  the seconds of every local gate the landing worktree ran (the sum of its verdict.json `timings.wall_s`)
A field never observed stays null and `summary` says how many landings lack it: a MISSING observation is not a zero.

`summary` prints the after column (this file) beside the before column (the retired train records, then the frozen
2026-10-10 evidence): changes landed per hour, median and p90 minutes DONE → on main, bounces per landing and their
causes, merge-group CI wall, local gate minutes. `freeze` copies the retired train file under
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

#: The 2026-10-10 evidence's figures that the train records cannot give (the lander minutes in gates and in push-main
#: plus CI, the CI wall). Typed from the frozen evidence file; `self_test` re-reads that file and fails when a
#: figure here is not in its text, so none of them is remembered.
EVIDENCE = {"lander_minutes": 567, "gate_minutes": 233, "gate_pct": 41, "push_ci_minutes": 94, "push_ci_pct": 16.5,
            "clusters_landed": 38, "ci_wall_min": 13}
EVIDENCE_QUOTES = ("567 lander minutes, of which 233 (41 %) inside Windows/WSL gate runs and 94\n(16.5 %) inside "
                   "push-main + CI", "13 minutes on 20 runners", "38 clusters on main")

Gh = Callable[[list[str]], tuple[int, str, str]]
Say = Callable[[str], None]


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
    stamps = [(k, _when(rec[k])) for k in STAMPS if rec.get(k)]
    for (ka, ta), (kb, tb) in zip(stamps, stamps[1:]):
        if tb < ta:
            raise ValueError(f"pull request {rec['pr']}: {kb} {rec[kb]} is before {ka} {rec[ka]} "
                             f"(a landing passes {' → '.join(STAMPS)})")
    for k in ("bounces", "local_gate_runs"):
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
    """`existing` with every non-None field of `patch` laid over it, validated. The FIRST `enqueued` is kept."""
    rec = {"schema": SCHEMA, "pr": patch["pr"], "branch": None, "changes": 1, "done": None, "review_clean": None,
           "enqueued": None, "merged": None, "bounces": None, "bounce_causes": [], "ci_wall_s": None,
           "local_gate_s": None, "local_gate_runs": None, "note": "", **(existing or {})}
    for k, v in patch.items():
        if v is None or (k == "bounce_causes" and not v):
            continue
        if k == "enqueued" and rec.get("enqueued"):
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


def after_figures(recs: list[dict]) -> dict:
    """The after column from the landing records (`merged` ones are the landings)."""
    landed = [r for r in recs if r.get("merged")]
    out: dict = {"records": len(recs), "landings": len(landed), "changes": sum(r["changes"] for r in landed)}
    starts = [_when(r.get("done") or r["enqueued"]) for r in landed if r.get("done") or r.get("enqueued")]
    if landed and starts:
        window_h = (max(_when(r["merged"]) for r in landed) - min(starts)).total_seconds() / 3600
        out["window_h"] = window_h
        out["per_h"] = out["changes"] / window_h if window_h > 0 else None
    mins = [_minutes(r["done"], r["merged"]) for r in landed if r.get("done")]
    out["done_to_main"] = ({"n": len(mins), "median": median(mins), "p90": percentile(mins, 90)} if mins else None)
    out["no_done"] = len(landed) - len(mins)
    counted = [r for r in landed if r.get("bounces") is not None]
    causes = {c: sum(r["bounce_causes"].count(c) for r in counted) for c in CAUSES}
    out["bounces"] = {"n": len(counted), "total": sum(r["bounces"] for r in counted), "causes": causes,
                      "unattributed": sum(r["bounces"] - len(r["bounce_causes"]) for r in counted)}
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
    least two pull requests have landed over a window above zero, else the retired trains' batched rate."""
    a = after_figures(recs)
    if a["landings"] >= 2 and a.get("per_h"):
        d = a["done_to_main"]
        return {"per_h": a["per_h"], "changes": a["changes"], "window_h": a["window_h"], "landings": a["landings"],
                "source": "pull requests through the merge queue", "median_min": d["median"] if d else None}
    b = before_figures(trains)
    if b:
        f = b["floor"]
        return {"per_h": b["per_h"], "changes": b["changes"], "window_h": b["window_h"], "landings": b["trains"],
                "source": "the retired trains, batched gating", "median_min": f["median"] if f else None}
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
    rows.append(("minutes DONE → on main, median / p90",
                 (f"{d['median']:.0f} / {d['p90']:.0f} ({d['n']} landing(s))" if d else UNMEASURED)
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

def done_from_report(branch: str, reports_dir: pathlib.Path, not_after: str) -> str | None:
    """The finish time of the implementer that worked on `branch`: the newest report in `reports_dir` whose header
    (first 12 lines) names the branch on a `branch` line, as its file's modification time. A report written after
    `not_after` is ignored (a lander's report about the branch it queued from is written AFTER the queue; it is no
    implementer's DONE); None when none is left."""
    if not reports_dir.is_dir():
        return None
    pat = re.compile(r"(?<![\w/.\-])" + re.escape(branch) + r"(?![\w/.\-])")
    hits = []
    for p in reports_dir.glob("*.md"):
        try:
            head = p.read_text(encoding="utf-8", errors="replace").splitlines()[:12]
        except OSError:
            continue
        if any("branch" in ln.lower() and pat.search(ln) for ln in head):
            hits.append(p)
    limit = _when(not_after).timestamp()
    times = [m for m in (p.stat().st_mtime for p in hits) if int(m) <= int(limit)]   # stamps are whole seconds
    return dt.datetime.fromtimestamp(max(times)).astimezone().isoformat(timespec="seconds") if times else None


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
                    changes: int | None = None, enqueued: str | None = None) -> dict:
    """What `enqueue.py queue` can record the moment a pull request is queued: the branch, the enqueue time, the DONE
    time from the reports directory and the local gate seconds from the worktree's verdict.json files."""
    at = enqueued or now_iso()
    g = local_gate(worktree)
    return record(path, {"pr": pr, "branch": branch, "changes": changes, "enqueued": at,
                         "done": done_from_report(branch, reports_dir, at),
                         "local_gate_s": round(g[0], 1) if g else None, "local_gate_runs": g[1] if g else None})


def complete(path: pathlib.Path, gh: Gh, only: int | None = None, say: Say = print) -> int:
    """Complete every record with no `merged` time (or the one pull request `only`) from GitHub: `mergedAt`, the count
    of failed merge-group runs of the pull request (bounces) and the successful run's wall (CI wall). Returns 0 when
    every record asked about was read, 2 when GitHub could not be asked."""
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
        mine = [x for x in runs if re.search(rf"/pr-{r['pr']}-", x.get("headBranch") or "")]
        ok = [x for x in mine if x.get("conclusion") == "success"]
        patch: dict = {"pr": r["pr"], "merged": pr["mergedAt"],
                       "bounces": sum(x.get("conclusion") in ("failure", "timed_out") for x in mine)}
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
            f"CI wall {_cell(new['ci_wall_s'] and new['ci_wall_s'] / 60)} min")
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
    r.add_argument("--ci-wall-s", type=float)
    r.add_argument("--local-gate-s", type=float)
    r.add_argument("--local-gate-runs", type=int)
    r.add_argument("--note")
    c = sub.add_parser("complete", help="read merge time, bounces and CI wall from GitHub for unmerged records")
    c.add_argument("--pr", type=int, help="only this pull request (merged ones too)")
    s = sub.add_parser("summary", help="the after column beside the before column")
    s.add_argument("--since", help="only landings enqueued at or after this ISO time")
    s.add_argument("--before", help="the train records to compare against (default: the frozen copy, else the "
                                    "coordination directory's)")
    sub.add_parser("freeze", help="freeze the retired train records and the first after-summary under the evidence directory")
    return ap


def patch_from_args(a: argparse.Namespace) -> dict:
    p = {"pr": a.pr, "branch": a.branch, "changes": a.changes, "bounces": a.bounces, "ci_wall_s": a.ci_wall_s,
         "local_gate_s": a.local_gate_s, "local_gate_runs": a.local_gate_runs, "note": a.note,
         "bounce_causes": [c.strip() for c in a.bounce_causes.split(",") if c.strip()] if a.bounce_causes else []}
    for k in STAMPS:
        v = getattr(a, k)
        p[k] = _when(v).isoformat() if v else None
    return p


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
ARMS = ("a record is a patch: later calls fill fields, the first enqueue time stays, and every stamp must follow the last",
        "an inconsistent record is refused: stamps out of order, an unknown cause, causes with no bounces, a bad number",
        "done comes from the newest report naming the branch, never from one written after the queue",
        "local gate seconds are the sum of every verdict.json in the worktree",
        "complete reads the merge time, counts only failed merge-group runs as bounces and takes the CI wall of the "
        "successful run",
        "summary sets the after column beside the before column, names unattributed bounces and says UNMEASURED for a "
        "figure it never saw",
        "the headline is the merge queue's rate from two landings and the retired trains' rate before that",
        "freeze waits for a landing, refuses to rewrite the frozen copy, and the evidence figures are in the evidence file")


def _self_test(tmp: pathlib.Path) -> list[str]:
    fails: list[str] = []
    patch_arm, refuse_arm, done_arm, gate_arm, complete_arm, summary_arm, headline_arm, freeze_arm = ARMS
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
    cli = patch_from_args(parser().parse_args(["record", "--pr", "5", "--branch", "b5", "--changes", "3", "--enqueued",
                                               t("10:00"), "--bounces", "2", "--bounce-causes", "conflict, load-timing"]))
    made = patch_record(None, cli)
    if (made["changes"], made["bounces"], made["bounce_causes"], made["branch"]) != (3, 2, ["conflict", "load-timing"], "b5"):
        fails.append(f"{patch_arm}: the command line's mapping: {made}")
    # refusals
    for name, p in (("merged before enqueued", {"pr": 17, "merged": t("09:50")}),
                    ("an unknown cause", {"pr": 17, "bounces": 1, "bounce_causes": ["flake"]}),
                    ("causes with no bounces", {"pr": 17, "bounce_causes": ["conflict"]}),
                    ("more causes than bounces", {"pr": 17, "bounces": 1, "bounce_causes": ["conflict", "conflict"]}),
                    ("a negative gate", {"pr": 17, "local_gate_s": -1.0}),
                    ("no branch on a first record", {"pr": 18, "enqueued": t("10:00")}),
                    ("a time with no offset", {"pr": 17, "merged": "2026-10-12T11:00:00"})):
        try:
            record(path, {k: (_when(v).isoformat() if k in STAMPS and v else v) for k, v in p.items()})
            fails.append(f"{refuse_arm}: accepted {name}")
        except ValueError:
            pass
    if len(read_all(path)) != 1 or read_all(path)[0]["bounces"] is not None:
        fails.append(f"{refuse_arm}: a refused patch changed the file: {read_all(path)}")
    # done_from_report
    rd = tmp / "reports"
    rd.mkdir()
    (rd / "old.md").write_text("DONE\n**branch:** `other-b17-x`\n", encoding="utf-8")
    (rd / "mine.md").write_text("DONE\n**Status:** DONE · **branch:** `b17` · **HEAD:** abc\n", encoding="utf-8")
    (rd / "lander.md").write_text("DONE\nbranch b17\n", encoding="utf-8")
    os.utime(rd / "mine.md", (1_800_000_000, 1_800_000_000))
    os.utime(rd / "lander.md", (1_900_000_000, 1_900_000_000))
    at_mine = dt.datetime.fromtimestamp(1_800_000_000).astimezone().isoformat(timespec="seconds")
    after = dt.datetime.fromtimestamp(1_850_000_000).astimezone().isoformat(timespec="seconds")
    got = done_from_report("b17", rd, after)
    if got != at_mine or done_from_report("b17", rd, "2000-01-01T00:00:00+00:00") is not None \
            or done_from_report("zzz", rd, after) is not None or done_from_report("b1", rd, after) is not None:
        fails.append(f"{done_arm}: {got} vs {at_mine}")
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

    def gh(args: list[str]) -> tuple[int, str, str]:
        if args[:2] == ["run", "list"]:
            return 0, json.dumps(runs), ""
        if args[:2] == ["pr", "view"]:
            return 0, json.dumps({"state": "MERGED", "mergedAt": "2026-10-12T18:26:00Z"}), ""
        return 1, "", "unrouted"
    lines: list[str] = []
    rc = complete(path, gh, say=lines.append)
    r = read_all(path)[0]
    if rc != 0 or r["bounces"] != 1 or r["ci_wall_s"] != 930.0 or not r["merged"].startswith("2026-10-12T18:26"):
        fails.append(f"{complete_arm}: rc={rc} {r} {lines}")
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
    want = ["0.41 (1 change(s), 1 pull request(s), 2.4 h)", "2.00 (8 changes, 2 trains, 4.0 h)", "146 / 146 (1 landing(s))",
            "0.12 ejected to a finisher per landed change; 1 CI red(s); causes UNMEASURED",
            "1.00 (1 in 1 landing(s); cluster-defect 1)", "median 15.5, p90 15.5 (1 landing(s))",
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
