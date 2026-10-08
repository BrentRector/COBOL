#!/usr/bin/env python3
"""Choose the orchestrator's next unit deterministically when the last handoff does not name one.

    python scripts/orchestrator/next_unit.py [--handoff FILE] [--last-failed] [--last-started ISO] [--repo DIR]
                                             [--cluster LEAD ... [--work DIR]]

Prints JSON {unit, reason}. The order (docs/rearchitecture/DESIGN-orchestrator-loop.md section 3.2), first match wins:
  1. the handoff carries owner_question            -> owner-question
  2. the handoff names next_unit                    -> that unit (but not `land` while the landing lease is held);
     a wave-type unit (`wave` or `campaign`) names only a WAVE, and with --cluster the campaign rule below decides
     its lane (kb/Work PB2522)
  3. no meter reading of THIS account in 3 hours   -> meter (two accounts share readings.json; kb/Work PB2478)
  4. the repository is dirty or has unpushed commits -> resume
  5. the last unit failed or ended split, or a branch classified UNLANDED got a commit after the last unit
     started (an agent of a unit that died)          -> resume
  6. branches_pending with status DONE               -> land, unless another lander holds the landing lease
     (landing_lease.py, kb/Work PB2537): then rule 7 chooses, and its reason says the land waits and on whom (a
     handoff that named `land` is deferred the same way, at rule 2)
  7. otherwise                                       -> wave
THE CAMPAIGN RULE (--cluster LEAD, repeatable: the CAMPAIGN lane, kb/Work PB2120) decides every wave-type choice, from
rule 2 or rule 7: `campaign` when a lead's cluster has a ready note and the last wave-type unit was not a campaign, so
campaign waves alternate with fix-lane waves; the ready leads take turns, least recently run first, and the JSON names
the one chosen in `cluster`. A wave-type choice then carries `campaign` (run | between | waiting | landed | unknown, the
lane as a whole), and its reason keeps a land the lease deferred. With --cluster every choice but an owner question
carries `campaigns` ({lead: ready | waiting | landed | unknown}: the supervisor ends a lead's lane at `landed` and
records the map in units.jsonl) and `starved` (the leads whose ready note has waited through more than one wave-type
unit since their own last campaign wave, measured from that record). Without --cluster the output is the fix lane's,
byte for byte.
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

WAVE_TYPES = ("wave", "campaign")


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


def unit_history(cdir: pathlib.Path) -> list[dict]:
    """units.jsonl, the supervisor's own record of every unit it ran, oldest first (an unreadable line is skipped)."""
    path = cdir / "units.jsonl"
    if not path.is_file():
        return []
    out = []
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            rec = json.loads(line)
        except ValueError:
            continue
        if isinstance(rec, dict):
            out.append(rec)
    return out


def ran_campaign(rec: dict, lead: str) -> bool:
    """A campaign unit of `lead`. A line written before the supervisor named the cluster ran the one lead it had."""
    return rec.get("unit") == "campaign" and rec.get("cluster", lead) == lead


def lane_states(leads: list[str], work_dir: pathlib.Path) -> dict[str, dict]:
    """Each lead's lane, from work.py's cluster_order (the one reader of cluster: and blocked_by:)."""
    items = work.load(work_dir)
    lanes = {}
    for lead in leads:
        view = work.cluster_order(items, lead)
        ready = [n["id"] for n in view["notes"] if n["ready"]]
        state = ("unknown" if not view["named"] else "landed" if not view["notes"]
                 else "ready" if ready else "waiting")
        lanes[lead] = {"state": state, "ready": ready, "notes": view["notes"]}
    return lanes


def started_at(rec: dict) -> dt.datetime | None:
    """A units.jsonl line's start, offset-aware, or None when it has none the walk can trust."""
    try:
        when = dt.datetime.fromisoformat(str(rec["started_at"]).replace("Z", "+00:00"))
    except (KeyError, ValueError):
        return None
    return when if when.tzinfo is not None else None


def starved(lanes: dict[str, dict], history: list[dict], now: dt.datetime) -> list[dict]:
    """The leads whose ready note has waited through MORE THAN ONE wave-type unit since their own last campaign wave
    (kb/Work PB2522: PB2151 waited almost nine hours through eight units, three of them waves, and nothing said so).
    Measured from the per-choice lane state the supervisor records in units.jsonl (`campaigns`): the walk back from
    the newest unit stops at the lead's
    own campaign unit or at a line that did not record the lead ready, so a gap in the record under-reports, never
    over-reports. `hours` runs from the oldest unit of that unbroken run to now."""
    out = []
    for lead, lane in lanes.items():
        if lane["state"] != "ready":
            continue
        since, waves = None, 0
        for rec in reversed(history):
            if ran_campaign(rec, lead) or (rec.get("campaigns") or {}).get(lead) != "ready":
                break
            when = started_at(rec)
            if when is None:
                break
            since = when
            waves += rec.get("unit") in WAVE_TYPES
        if waves > 1 and since is not None:
            last = next((r.get("started_at") for r in reversed(history) if ran_campaign(r, lead)), None)
            out.append({"cluster": lead, "hours": round((now - since).total_seconds() / 3600, 1), "waves": waves,
                        "last_campaign_at": last})
    return out


def lane_why(lead: str, lane: dict) -> str:
    if lane["state"] == "unknown":
        return f"no kb/Work note names the cluster {lead}"
    if lane["state"] == "landed":
        return f"the cluster {lead} is landed: the campaign lane ends"
    return f"campaign {lead}: no ready note (" + "; ".join(
        f"{n['id']} waits on {', '.join(n['waiting_on'])}" for n in lane["notes"][:4]) + ")"


def campaign_choice(lanes: dict[str, dict], history: list[dict], named_reason: str | None) -> dict[str, str]:
    """The campaign rule: the lane a wave-type unit runs. `named_reason` is the handoff's reason when the last handoff
    named the wave, else None; the alternation is the supervisor's rule, not the model's (kb/Work PB2522), so the
    handoff's reason is kept in the log and decides nothing."""
    named = "" if named_reason is None else f"named by the last handoff: {named_reason}; "
    states = [lane["state"] for lane in lanes.values()]
    ready = [lead for lead, lane in lanes.items() if lane["state"] == "ready"]
    if not ready:
        overall = ("waiting" if "waiting" in states else "landed" if all(s == "landed" for s in states)
                   else "unknown")
        return {"unit": "wave", "reason": named + "; ".join(lane_why(k, v) for k, v in lanes.items()),
                "campaign": overall}
    wave_types = [r for r in history if r.get("unit") in WAVE_TYPES]
    if wave_types and wave_types[-1].get("unit") == "campaign":
        return {"unit": "wave", "reason": named + f"a fix-lane wave between campaign waves ({', '.join(ready)} ready)",
                "campaign": "between"}
    # Round-robin: the ready lead whose own last campaign unit is oldest (never run = oldest), ties in -Cluster order.
    last_run = {lead: max((i for i, r in enumerate(history) if ran_campaign(r, lead)), default=-1) for lead in ready}
    lead = min(ready, key=lambda k: (last_run[k], ready.index(k)))
    ids = lanes[lead]["ready"]
    handoff = "" if named_reason is None else f"; the handoff named wave: {named_reason}"
    return {"unit": "campaign", "cluster": lead,
            "reason": f"campaign {lead}: {len(ids)} ready note(s): {', '.join(ids[:6])}{handoff}", "campaign": "run"}


def base_choice(h: dict, cdir: pathlib.Path, repo: pathlib.Path, now: dt.datetime, last_failed: bool,
                last_started: dt.datetime | None, max_meter_age_h: float,
                acct_name: str) -> tuple[dict[str, str], str]:
    """Rules 2 to 7: the unit, before the campaign rule decides the lane of a wave-type one, and the land the landing
    lease deferred ('' when none), which the campaign rule's reason keeps."""
    # ONE LANDER ON MAIN AT A TIME (kb/Work PB2537): while another lander holds the landing lease a `land` unit would
    # only wait for it, so neither a handoff naming `land` nor finished branches start one; the other rules choose.
    lease = landing_lease.current(cdir, now)
    land_deferred = lease is not None and h.get("next_unit") == "land"
    if h.get("next_unit") and not land_deferred:
        return {"unit": h["next_unit"], "reason": "named by the last handoff: " + h.get("next_unit_reason", "")}, ""
    age = meter_age_hours(cdir, now, acct_name)
    if age is None or age > max_meter_age_h:
        return {"unit": "meter", "reason": f"no {acct_name} meter reading" if age is None
                else f"{acct_name} meter reading {age:.1f} h old"}, ""
    state = repo_state(repo)
    if state:
        return {"unit": "resume", "reason": state}, ""
    if last_failed or h.get("outcome") in ("split", "failed"):
        return {"unit": "resume", "reason": "the last unit failed or ended split"}, ""
    if last_started:
        fresh = fresh_unlanded(repo, last_started)
        if fresh:
            return {"unit": "resume", "reason": "unlanded branches from the last unit: " + ", ".join(fresh)}, ""
    done = [b["branch"] for b in h.get("branches_pending", []) if b.get("status") == "DONE"]
    if done and lease is None:
        return {"unit": "land", "reason": "finished branches waiting: " + ", ".join(done)}, ""
    deferred = ""
    if done or land_deferred:
        deferred = (f"; land deferred ({', '.join(done) or 'named by the last handoff'}): the landing lease is "
                    + landing_lease.describe(lease, now))
    return {"unit": "wave", "reason": "nothing pending" + deferred}, deferred


def choose(handoff: dict | None, cdir: pathlib.Path, repo: pathlib.Path, now: dt.datetime, last_failed: bool,
           last_started: dt.datetime | None, max_meter_age_h: float, acct_name: str,
           clusters: list[str] | None = None, work_dir: pathlib.Path = work.WORK) -> dict:
    h = handoff or {}
    if h.get("owner_question"):
        return {"unit": "owner-question", "reason": "the last handoff asks the owner"}
    choice, deferred = base_choice(h, cdir, repo, now, last_failed, last_started, max_meter_age_h, acct_name)
    if not clusters:
        return choice
    lanes = lane_states(clusters, work_dir)
    history = unit_history(cdir)
    if choice["unit"] in WAVE_TYPES:
        # The handoff named THIS wave only when its next_unit is wave-type: a `land` the lease deferred named none.
        named = h.get("next_unit_reason", "") if h.get("next_unit") in WAVE_TYPES else None
        choice = campaign_choice(lanes, history, named)
        choice["reason"] += deferred
    choice["campaigns"] = {lead: lane["state"] for lead, lane in lanes.items()}
    choice["starved"] = starved(lanes, history, now)
    return choice


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--handoff", help="the last unit's handoff.json (absent or unreadable = none)")
    ap.add_argument("--last-failed", action="store_true")
    ap.add_argument("--last-started", help="ISO time the last unit started")
    ap.add_argument("--repo", default=str(coord.REPO))
    ap.add_argument("--coord")
    ap.add_argument("--now")
    ap.add_argument("--cluster", action="append", default=[],
                    help="a campaign lane's lead (repeatable, in the supervisor's -Cluster order)")
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
