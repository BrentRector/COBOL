#!/usr/bin/env python3
"""Choose the orchestrator's next unit deterministically when the last handoff does not name one.

    python scripts/orchestrator/next_unit.py [--handoff FILE] [--last-failed] [--last-started ISO] [--repo DIR]

Prints JSON {unit, reason}. The order (docs/rearchitecture/DESIGN-orchestrator-loop.md section 3.2), first match wins:
  1. the handoff carries owner_question            -> owner-question
  2. the handoff names next_unit                    -> that unit
  3. no meter reading in the last 3 hours            -> meter
  4. the repository is dirty or has unpushed commits -> resume
  5. the last unit failed or ended split, or a branch classified UNLANDED got a commit after the last unit
     started (an agent of a unit that died)          -> resume
  6. branches_pending with status DONE               -> land
  7. otherwise                                       -> wave
(A stale ledger page is NOT a reason for a unit: a headless unit cannot publish, so the supervisor announces an owed
publish through ledger_state.py and the attended session publishes.)
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import coord  # noqa: E402



def git(repo: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True, encoding="utf-8",
                          errors="replace")


def meter_age_hours(cdir: pathlib.Path, now: dt.datetime) -> float | None:
    readings = coord.read_json(cdir / "readings.json", [])
    if not readings:
        return None
    newest = max(dt.datetime.fromisoformat(r["noted_at"].replace("Z", "+00:00")) for r in readings)
    return (now - newest).total_seconds() / 3600


def repo_state(repo: pathlib.Path) -> str:
    dirty = [l for l in git(repo, "status", "--porcelain").stdout.splitlines()
             if not l.endswith(".claude/settings.local.json")]
    if dirty:
        return f"{len(dirty)} uncommitted path(s) in {repo}"
    ahead = git(repo, "rev-list", "--count", "@{u}..HEAD").stdout.strip()
    if ahead and ahead != "0":
        return f"{ahead} unpushed commit(s) in {repo}"
    return ""


def fresh_unlanded(repo: pathlib.Path, since: dt.datetime) -> list[str]:
    import prune_worktrees  # noqa: PLC0415  (classifies against origin/main by content; no fetch here)
    names = None
    out = []
    for b in git(repo, "branch", "--format=%(refname:short) %(committerdate:iso-strict)").stdout.splitlines():
        name, _, when = b.partition(" ")
        if name == "main" or not when or dt.datetime.fromisoformat(when) <= since:
            continue
        names = names if names is not None else prune_worktrees.main_names()
        if prune_worktrees.classify(name, names)[0] == "UNLANDED":
            out.append(name)
    return out


def choose(handoff: dict | None, cdir: pathlib.Path, repo: pathlib.Path, now: dt.datetime, last_failed: bool,
           last_started: dt.datetime | None, max_meter_age_h: float) -> dict[str, str]:
    h = handoff or {}
    if h.get("owner_question"):
        return {"unit": "owner-question", "reason": "the last handoff asks the owner"}
    if h.get("next_unit"):
        return {"unit": h["next_unit"], "reason": "named by the last handoff: " + h.get("next_unit_reason", "")}
    age = meter_age_hours(cdir, now)
    if age is None or age > max_meter_age_h:
        return {"unit": "meter", "reason": "no meter reading" if age is None else f"meter reading {age:.1f} h old"}
    state = repo_state(repo)
    if state:
        return {"unit": "resume", "reason": state}
    if last_failed or h.get("outcome") in ("split", "failed"):
        return {"unit": "resume", "reason": "the last unit failed or ended split"}
    if last_started:
        fresh = fresh_unlanded(repo, last_started)
        if fresh:
            return {"unit": "resume", "reason": "unlanded branches from the last unit: " + ", ".join(fresh)}
    done = [b["branch"] for b in h.get("branches_pending", []) if b.get("status") == "DONE"]
    if done:
        return {"unit": "land", "reason": "finished branches waiting: " + ", ".join(done)}
    return {"unit": "wave", "reason": "nothing pending"}


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--handoff", help="the last unit's handoff.json (absent or unreadable = none)")
    ap.add_argument("--last-failed", action="store_true")
    ap.add_argument("--last-started", help="ISO time the last unit started")
    ap.add_argument("--repo", default=str(coord.REPO))
    ap.add_argument("--coord")
    ap.add_argument("--now")
    a = ap.parse_args(argv)
    handoff = None
    if a.handoff:
        try:
            handoff = json.loads(pathlib.Path(a.handoff).read_text(encoding="utf-8"))
        except (OSError, ValueError):
            handoff = None
    now = dt.datetime.fromisoformat(a.now) if a.now else dt.datetime.now(dt.timezone.utc)
    started = dt.datetime.fromisoformat(a.last_started) if a.last_started else None
    rules = coord.rules()
    print(json.dumps(choose(handoff, coord.coord_dir(a.coord), pathlib.Path(a.repo), now, a.last_failed, started,
                            rules["quota"]["meter_max_age_hours"])))
    return 0


if __name__ == "__main__":
    sys.exit(main())
