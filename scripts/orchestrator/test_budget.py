#!/usr/bin/env python3
"""Self-test for budget.py: the week and day boundaries in America/Los_Angeles, the anchor, the token-to-point
conversion, each of the four decisions, borrowing, and --record.
Run: python scripts/orchestrator/test_budget.py   (no submodule, no build, no telemetry sink)."""
import datetime as dt
import json
import os
import pathlib
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import budget  # noqa: E402
import coord  # noqa: E402

RULES = coord.rules()
LA = budget.ZoneInfo("America/Los_Angeles")
fails, checked = [], []


def check(name, got, want):
    checked.append(name)
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def at(y, mo, d, h, mi=0):
    return dt.datetime(y, mo, d, h, mi, tzinfo=LA)


def ev(t, model, inp=0, out=0, cw=0, cr=0):
    return {"event.timestamp": t.astimezone(dt.timezone.utc).isoformat().replace("+00:00", "Z"), "model": model,
            "input_tokens": inp, "output_tokens": out, "cache_creation_tokens": cw, "cache_read_tokens": cr}


def source(events):
    return lambda since, until: list(events)


# 1. the week starts Sunday 03:00 Los Angeles (2026-10-04 is a Sunday)
check("week start, Sunday after 03:00", budget.week_start(at(2026, 10, 4, 14), RULES["quota"]), at(2026, 10, 4, 3))
check("week start, Sunday before 03:00", budget.week_start(at(2026, 10, 4, 2), RULES["quota"]), at(2026, 9, 27, 3))
check("week start, Wednesday", budget.week_start(at(2026, 10, 7, 9), RULES["quota"]), at(2026, 10, 4, 3))
# DST ends 2026-11-01 at 02:00: the reset that day is still 03:00 wall-clock
check("week start across DST", budget.week_start(at(2026, 11, 1, 12), RULES["quota"]).hour, 3)

# 2. points: 1 M counted Sonnet tokens = 1 point; 0.4 M Opus = 1 point; cache reads are not counted
cal = RULES["calibration"]
p, fam = budget.points([ev(at(2026, 10, 5, 10), "claude-sonnet-5-5", inp=100_000, out=400_000, cw=500_000, cr=9_000_000),
                        ev(at(2026, 10, 5, 11), "claude-opus-5-5", out=200_000, cw=200_000)],
                       at(2026, 10, 5, 0), at(2026, 10, 5, 23), cal)
check("points", round(p, 6), 2.0)
check("per family", fam, {"sonnet": 1_000_000, "opus": 400_000})
# 2b. the frontier tier is priced at its own rate, never at the opus fallback (kb/Work R69): 160,000 Mythos tokens = 1 point
p, fam = budget.points([ev(at(2026, 10, 5, 11), "claude-mythos-5-1", out=160_000)], at(2026, 10, 5, 10), at(2026, 10, 5, 23), cal)
check("mythos rate", p, 1.0)
check("mythos family", fam, {"mythos": 160_000})
p, _ = budget.points([ev(at(2026, 10, 5, 10), "claude-sonnet-5-5", out=1_000_000)], at(2026, 10, 5, 10), at(2026, 10, 5, 23), cal)
check("event at the anchor instant is excluded", p, 0.0)

# 3. decisions. Monday 2026-10-05 12:00 is day 2: allowance 28.6
now = at(2026, 10, 5, 12)
reading = {"noted_at": at(2026, 10, 5, 9).isoformat(), "weekly_pct": 20.0, "session_pct": 10.0,
           "session_reset": at(2026, 10, 5, 13).isoformat()}
r = budget.estimate(now, [reading], source([ev(at(2026, 10, 5, 10), "sonnet", out=2_000_000)]), RULES)
check("go: weekly", r["weekly_est_pct"], 22.0)
check("go: allowance day 2", (r["day"], r["allowance_pct"]), (2, 28.6))
check("go: session = anchor + 2 points x 5", r["session_est_pct"], 20.0)
check("go: decision", r["decision"], "go")
check("go: headroom", r["headroom_pct"], 6.6)

r = budget.estimate(now, [reading], source([ev(at(2026, 10, 5, 10), "sonnet", out=9_000_000)]), RULES)
check("hold-day over allowance", (r["decision"], r["resume_at"]), ("hold-day", at(2026, 10, 6, 3, 5).isoformat()))
r = budget.estimate(now, [reading], source([ev(at(2026, 10, 5, 10), "sonnet", out=9_000_000)]), RULES, borrow_days=1)
check("borrowing a day turns hold-day into go/hold-session", r["allowance_pct"], 42.9)

r = budget.estimate(now, [reading], source([ev(at(2026, 10, 5, 10), "opus", out=32_000_000)]), RULES, borrow_days=6)
check("stop-week at the cap", (r["decision"], r["allowance_pct"]), ("stop-week", 97.0))  # the cap is model_rules.json weekly_cap_pct (owner 2026-10-07 09:40, kb/Work R69 section 6)

hot = dict(reading, session_pct=95.0)  # the soft stop is 97 (owner 2026-10-07 09:40)
r = budget.estimate(now, [hot], source([ev(at(2026, 10, 5, 10), "sonnet", out=1_000_000)]), RULES)
check("hold-session at the soft stop", (r["decision"], r["resume_at"]), ("hold-session", at(2026, 10, 5, 13).isoformat()))

# 4. a reading from last week anchors nothing; the newest valid reading wins
old = {"noted_at": at(2026, 10, 3, 9).isoformat(), "weekly_pct": 90.0, "session_pct": 0, "session_reset": None}
r = budget.estimate(now, [old], source([]), RULES)
check("last week's reading ignored", (r["anchor"], r["weekly_est_pct"]), (None, 0.0))
newer = dict(reading, noted_at=at(2026, 10, 5, 11).isoformat(), weekly_pct=25.0)
r = budget.estimate(now, [reading, newer, old], source([]), RULES)
check("newest reading is the anchor", r["weekly_est_pct"], 25.0)

# 5. a session reset already passed: only the spend in the current window counts
past = dict(reading, session_reset=at(2026, 10, 5, 10).isoformat())
r = budget.estimate(now, [past], source([ev(at(2026, 10, 5, 9, 30), "sonnet", out=4_000_000),
                                          ev(at(2026, 10, 5, 11), "sonnet", out=1_000_000)]), RULES)
check("new window after reset", (r["session_est_pct"], r["resume_at"]), (5.0, None))

# 6. --record appends a reading through the CLI
cdir = pathlib.Path(tempfile.mkdtemp(prefix="budget-test-"))
env = dict(os.environ, COBOL_COORD_DIR=str(cdir))
cmd = [sys.executable, str(HERE / "budget.py"), "--record", "--weekly", "31.5", "--session", "12",
       "--session-reset", "2026-10-05T13:00:00-07:00", "--now", "2026-10-05T12:00:00-07:00"]
check("record exit", subprocess.run(cmd, env=env, capture_output=True).returncode, 0)
check("record exit again", subprocess.run(cmd, env=env, capture_output=True).returncode, 0)
saved = json.loads((cdir / "readings.json").read_text(encoding="utf-8"))
check("readings appended", [x["weekly_pct"] for x in saved], [31.5, 31.5])
bad = subprocess.run(cmd[:-2] + ["--session-reset", "17:00"], env=env, capture_output=True)
check("zoneless reset refused", bad.returncode != 0, True)
r = subprocess.run([sys.executable, str(HERE / "budget.py"), "--json", "--now", "2026-10-05T12:30:00-07:00",
                    "--telemetry-dir", str(cdir)], env=env, capture_output=True, text=True)
out = json.loads(r.stdout)
check("CLI estimate from the recorded anchor", (out["weekly_est_pct"], out["decision"]), (31.5, "hold-day"))

for f in fails:
    print("FAIL:", f)
print(f"budget self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
