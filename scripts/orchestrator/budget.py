#!/usr/bin/env python3
"""Estimate the weekly and 5-hour-session quota from local telemetry, anchored on the latest owner-meter reading.

    python scripts/orchestrator/budget.py [--json] [--borrow-days N] [--now ISO]
    python scripts/orchestrator/budget.py --record --weekly 52 --session 31 --session-reset 2026-10-04T17:00:00-07:00

Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 8. Output (JSON with --json):
    {weekly_est_pct, allowance_pct, headroom_pct, session_est_pct, decision, resume_at, ...}
decision: `go` · `hold-session` (until the session reset) · `hold-day` (until the next day's reset + 5 min) ·
`stop-week` (the weekly cap is reached).

The ANCHOR is the newest reading in `<coord>/readings.json` (a list of {noted_at, weekly_pct, session_pct,
session_reset}), which the `meter` unit appends with --record. Spend since the anchor comes from the telemetry sink
through `usage_report.events()` (reused, never re-parsed), converted to weekly points per model with
model_rules.json's calibration. A reading taken before the current week's reset anchors nothing.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import sys
from typing import Any, Iterable
from zoneinfo import ZoneInfo

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "telemetry"))
import coord  # noqa: E402
import usage_report  # noqa: E402

DAY = dt.timedelta(days=1)


def parse_ts(s: str) -> dt.datetime:
    t = dt.datetime.fromisoformat(s.replace("Z", "+00:00"))
    if t.tzinfo is None:
        raise ValueError(f"timestamp without a zone: {s!r}")
    return t


def week_start(now: dt.datetime, q: dict[str, Any]) -> dt.datetime:
    """The most recent weekly reset (Sunday 03:00 America/Los_Angeles by default) at or before `now`."""
    r = q["weekly_reset"]
    local = now.astimezone(ZoneInfo(r["tz"]))
    back = (local.weekday() - r["weekday"]) % 7
    start = (local - dt.timedelta(days=back)).replace(hour=r["hour"], minute=r["minute"], second=0, microsecond=0)
    if start > local:
        start -= dt.timedelta(days=7)
    # Re-localize the wall time so a DST change inside the week does not shift the reset by an hour.
    return start.replace(tzinfo=None).replace(tzinfo=ZoneInfo(r["tz"]))


def points(events: Iterable[dict[str, Any]], since: dt.datetime, until: dt.datetime,
           cal: dict[str, Any]) -> tuple[float, dict[str, int]]:
    """Weekly points spent by api_request events in (since, until], and the counted tokens per model family."""
    per_family: dict[str, int] = {}
    for a in events:
        ts = a.get("event.timestamp")
        if not ts:
            continue
        t = parse_ts(str(ts))
        if not (since < t <= until):
            continue
        fam = coord.family(str(a.get("model") or "")) or "-"
        per_family[fam] = per_family.get(fam, 0) + sum(int(float(a.get(k) or 0)) for k in cal["counted_token_kinds"])
    rates = cal["tokens_per_point"]
    fallback = rates[cal["unknown_model_rate"]]
    return sum(n / rates.get(f, fallback) for f, n in per_family.items()), per_family


def telemetry_events(tdir: pathlib.Path, since: dt.datetime, until: dt.datetime):
    """api_request events from the sink's per-UTC-day files covering [since, until]."""
    day = since.astimezone(dt.timezone.utc).date()
    last = until.astimezone(dt.timezone.utc).date()
    files = []
    while day <= last:
        f = tdir / f"{day:%Y-%m-%d}.jsonl"
        if f.exists():
            files.append(f)
        day += DAY
    return usage_report.events(files)


def estimate(now: dt.datetime, readings: list[dict[str, Any]], events_for, rules: dict[str, Any],
             borrow_days: int = 0) -> dict[str, Any]:
    """The decision. `events_for(since, until)` yields telemetry events; injected so the tests need no sink."""
    q, cal = rules["quota"], rules["calibration"]
    start = week_start(now, q)
    valid = sorted((r for r in readings if start <= parse_ts(r["noted_at"]) <= now), key=lambda r: r["noted_at"])
    anchor = valid[-1] if valid else None
    a_time = parse_ts(anchor["noted_at"]) if anchor else start
    spent, per_family = points(events_for(a_time, now), a_time, now, cal)
    weekly = (float(anchor["weekly_pct"]) if anchor else 0.0) + spent

    # Session window: the anchor's session % while its reset is ahead; after the reset, a new window that started
    # at the first call after it (unknown reset -> treat the whole spend since the anchor as this window's).
    window = dt.timedelta(hours=q["session_window_hours"])
    reset = parse_ts(anchor["session_reset"]) if anchor and anchor.get("session_reset") else None
    if reset and reset > now:
        session = float(anchor["session_pct"]) + spent * cal["session_pct_per_weekly_pct"]
        session_reset = reset
    else:
        since = reset or a_time
        evs = [e for e in events_for(since, now) if e.get("event.timestamp")]
        first = min((parse_ts(str(e["event.timestamp"])) for e in evs if parse_ts(str(e["event.timestamp"])) > since),
                    default=None)
        w_spent = points(evs, since, now, cal)[0]
        session = w_spent * cal["session_pct_per_weekly_pct"]
        session_reset = (first + window) if first else None
        if session_reset and session_reset <= now:
            # More than one window has passed since: only the spend inside the current window counts.
            while session_reset <= now:
                session_reset += window
            w_spent = points(events_for(session_reset - window, now), session_reset - window, now, cal)[0]
            session = w_spent * cal["session_pct_per_weekly_pct"]

    day_n = int((now - start) / DAY) + 1
    cap = float(q["weekly_cap_pct"])
    allowance = min(cap, (day_n + max(0, borrow_days)) * float(q["daily_pct"]))
    next_day = start + day_n * DAY + dt.timedelta(minutes=q["day_resume_minutes_after_reset"])
    next_week = start + 7 * DAY + dt.timedelta(minutes=q["day_resume_minutes_after_reset"])
    next_day = next_day.replace(tzinfo=None).replace(tzinfo=start.tzinfo)  # wall-clock 03:05 across DST
    if weekly >= cap:
        decision, resume = "stop-week", next_week
    elif weekly >= allowance:
        decision, resume = "hold-day", next_day
    elif session >= q["session_soft_stop_pct"]:
        decision, resume = "hold-session", session_reset or (now + window)
    else:
        decision, resume = "go", None
    return {
        "weekly_est_pct": round(weekly, 2), "allowance_pct": round(allowance, 2),
        "headroom_pct": round(allowance - weekly, 2), "session_est_pct": round(session, 2),
        "decision": decision, "resume_at": resume.isoformat() if resume else None,
        "session_hard_stop_pct": q["session_hard_stop_pct"], "session_soft_stop_pct": q["session_soft_stop_pct"],
        "week_start": start.isoformat(), "day": day_n, "borrow_days": borrow_days,
        "anchor": anchor, "spend": {"points": round(spent, 3), "counted_tokens": per_family},
    }


def record(cdir: pathlib.Path, weekly: float, session: float, session_reset: str | None, now: dt.datetime) -> dict:
    if session_reset:
        parse_ts(session_reset)  # refuse a zoneless or malformed reset before it is stored
    path = cdir / "readings.json"
    readings = coord.read_json(path, [])
    entry = {"noted_at": now.isoformat(), "weekly_pct": weekly, "session_pct": session, "session_reset": session_reset}
    readings.append(entry)
    coord.write_json(path, readings)
    return entry


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--json", action="store_true")
    ap.add_argument("--borrow-days", type=int, default=0, help="days of later allowance the owner allowed (never assumed)")
    ap.add_argument("--now", help="ISO time with a zone (tests); default now")
    ap.add_argument("--coord", help=f"coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    ap.add_argument("--telemetry-dir", default=str(usage_report.DIR))
    ap.add_argument("--record", action="store_true", help="append a meter reading instead of estimating")
    ap.add_argument("--weekly", type=float)
    ap.add_argument("--session", type=float)
    ap.add_argument("--session-reset", help="ISO time with a zone of the session window's reset")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    now = parse_ts(a.now) if a.now else dt.datetime.now(dt.timezone.utc)
    cdir = coord.coord_dir(a.coord)
    if a.record:
        if a.weekly is None or a.session is None:
            ap.error("--record needs --weekly and --session")
        print(json.dumps(record(cdir, a.weekly, a.session, a.session_reset, now)))
        return 0
    tdir = pathlib.Path(a.telemetry_dir)
    out = estimate(now, coord.read_json(cdir / "readings.json", []),
                   lambda s, u: telemetry_events(tdir, s, u), coord.rules(), a.borrow_days)
    if a.json:
        print(json.dumps(out, indent=1))
    else:
        print(f"weekly ≈ {out['weekly_est_pct']} % of an allowance of {out['allowance_pct']} % (day {out['day']}"
              f"{', borrowing ' + str(a.borrow_days) if a.borrow_days else ''}); session ≈ {out['session_est_pct']} %"
              f" → {out['decision']}{' until ' + out['resume_at'] if out['resume_at'] else ''}"
              f"{'' if out['anchor'] else ' (no meter reading this week: estimate from telemetry alone)'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
