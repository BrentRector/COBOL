#!/usr/bin/env python3
"""Choose the orchestrator's next unit deterministically when the last handoff does not name one.

    python scripts/orchestrator/next_unit.py [--handoff FILE] [--last-failed] [--last-started ISO] [--repo DIR]
                                             [--cluster LEAD [--work DIR]]

Prints JSON {unit, reason}. The order (docs/rearchitecture/DESIGN-orchestrator-loop.md section 3.2), first match wins:
  1. the handoff carries owner_question            -> owner-question
  2. the handoff names next_unit                    -> that unit (but not `land` while the landing lease is held)
  3. no meter reading of THIS account in 3 hours   -> meter (two accounts share readings.json; kb/Work PB2478)
  4. the repository is dirty or has unpushed commits -> resume
  5. the last unit failed or ended split, or a branch classified UNLANDED got a commit after the last unit
     started (an agent of a unit that died)          -> resume
  6. branches_pending with status DONE               -> land, unless another lander holds the landing lease
     (landing_lease.py, kb/Work PB2537): then rule 7 chooses, and its reason says the land waits and on whom
  7. otherwise                                       -> wave; with --cluster (the CAMPAIGN lane, kb/Work PB2120)
     -> campaign when the cluster has a ready note and the last wave-type unit was not a campaign, so campaign
     waves alternate with fix-lane waves; the JSON then also carries `campaign`: run | between | waiting | landed |
     unknown, and `landed` tells the supervisor the lane is over
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
sys.path.insert(0, str(HERE.parent / "spec"))
import account  # noqa: E402
import coord  # noqa: E402
import landing_lease  # noqa: E402  (current: is another lander on main?)
import work  # noqa: E402  (cluster_order: the one reader of cluster: and blocked_by:)



def git(repo: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True, encoding="utf-8",
                          errors="replace")


def meter_age_hours(cdir: pathlib.Path, now: dt.datetime, acct_name: str) -> float | None:
    """Hours since the account's newest reading (None: it has none). Another account's reading is not this meter."""
    readings = [r for r in coord.read_json(cdir / "readings.json", []) if r.get("account") == acct_name]
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


def last_wave_unit(cdir: pathlib.Path) -> str | None:
    """The newest `wave` or `campaign` line of units.jsonl (the supervisor's own record), or None."""
    path = cdir / "units.jsonl"
    if not path.is_file():
        return None
    last = None
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            unit = json.loads(line).get("unit")
        except ValueError:
            continue
        if unit in ("wave", "campaign"):
            last = unit
    return last


def campaign_choice(cluster: str, cdir: pathlib.Path, work_dir: pathlib.Path) -> dict[str, str]:
    """Rule 7 of the campaign lane: a campaign wave between fix-lane waves while the cluster has a ready note."""
    view = work.cluster_order(work.load(work_dir), cluster)
    if not view["named"]:
        return {"unit": "wave", "reason": f"no kb/Work note names the cluster {cluster}", "campaign": "unknown"}
    if not view["notes"]:
        return {"unit": "wave", "reason": f"the cluster {cluster} is landed: the campaign lane ends",
                "campaign": "landed"}
    ready = [n["id"] for n in view["notes"] if n["ready"]]
    if not ready:
        return {"unit": "wave", "reason": f"campaign {cluster}: no ready note (" + "; ".join(
            f"{n['id']} waits on {', '.join(n['waiting_on'])}" for n in view["notes"][:4]) + ")", "campaign": "waiting"}
    if last_wave_unit(cdir) == "campaign":
        return {"unit": "wave", "reason": f"a fix-lane wave between campaign {cluster} waves", "campaign": "between"}
    return {"unit": "campaign", "reason": f"campaign {cluster}: {len(ready)} ready note(s): {', '.join(ready[:6])}",
            "campaign": "run"}


def choose(handoff: dict | None, cdir: pathlib.Path, repo: pathlib.Path, now: dt.datetime, last_failed: bool,
           last_started: dt.datetime | None, max_meter_age_h: float, acct_name: str, cluster: str | None = None,
           work_dir: pathlib.Path = work.WORK) -> dict[str, str]:
    h = handoff or {}
    if h.get("owner_question"):
        return {"unit": "owner-question", "reason": "the last handoff asks the owner"}
    # ONE LANDER ON MAIN AT A TIME (kb/Work PB2537): while another lander holds the landing lease a `land` unit would
    # only wait for it, so neither a handoff naming `land` nor finished branches start one; the other rules choose.
    lease = landing_lease.current(cdir, now)
    land_deferred = lease is not None and h.get("next_unit") == "land"
    if h.get("next_unit") and not land_deferred:
        return {"unit": h["next_unit"], "reason": "named by the last handoff: " + h.get("next_unit_reason", "")}
    age = meter_age_hours(cdir, now, acct_name)
    if age is None or age > max_meter_age_h:
        return {"unit": "meter", "reason": f"no {acct_name} meter reading" if age is None
                else f"{acct_name} meter reading {age:.1f} h old"}
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
    if done and lease is None:
        return {"unit": "land", "reason": "finished branches waiting: " + ", ".join(done)}
    choice = campaign_choice(cluster, cdir, work_dir) if cluster else {"unit": "wave", "reason": "nothing pending"}
    if done or land_deferred:
        choice["reason"] += (f"; land deferred ({', '.join(done) or 'named by the last handoff'}): the landing lease is "
                             + landing_lease.describe(lease, now))
    return choice


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--handoff", help="the last unit's handoff.json (absent or unreadable = none)")
    ap.add_argument("--last-failed", action="store_true")
    ap.add_argument("--last-started", help="ISO time the last unit started")
    ap.add_argument("--repo", default=str(coord.REPO))
    ap.add_argument("--coord")
    ap.add_argument("--now")
    ap.add_argument("--cluster", help="the campaign lane's cluster (orchestrate.ps1 -Cluster)")
    ap.add_argument("--work", default=str(work.WORK), help="the register directory (a test seam)")
    a = ap.parse_args(argv)
    handoff = None
    if a.handoff:
        try:
            handoff = json.loads(pathlib.Path(a.handoff).read_text(encoding="utf-8"))
        except (OSError, ValueError):
            handoff = None
    now = dt.datetime.fromisoformat(a.now) if a.now else dt.datetime.now(dt.timezone.utc)
    started = dt.datetime.fromisoformat(a.last_started) if a.last_started else None
    acct = account.current()
    print(json.dumps(choose(handoff, coord.coord_dir(a.coord), pathlib.Path(a.repo), now, a.last_failed, started,
                            acct.quota["meter_max_age_hours"], acct.name, a.cluster, pathlib.Path(a.work))))
    return 0


if __name__ == "__main__":
    sys.exit(main())
