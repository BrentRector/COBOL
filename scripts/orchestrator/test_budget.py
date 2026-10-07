#!/usr/bin/env python3
"""Self-test for budget.py: the week and day boundaries in America/Los_Angeles, the anchor, the token-to-point
conversion, each of the four decisions, borrowing, --record, and the ACCOUNT (kb/Work PB2478): each account's own
weekly reset, readings and telemetry.
Run: python scripts/orchestrator/test_budget.py   (no submodule, no build, no telemetry sink)."""
import copy
import datetime as dt
import json
import os
import pathlib
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import account as accounts  # noqa: E402
import budget  # noqa: E402
import coord  # noqa: E402

RULES = coord.rules()
# The estimator's ARITHMETIC is tested against a FIXED calibration, so a refit of model_rules.json's constants never
# rewrites these expectations; section 8 tests the LIVE constants against the meter readings they were fitted to.
FIXTURE = copy.deepcopy(RULES)
FIXTURE["calibration"].update(tokens_per_point={"sonnet": 1_000_000, "opus": 400_000, "fable": 160_000, "mythos": 160_000},
                              session_pct_per_weekly_pct=5.0)
LA = budget.ZoneInfo("America/Los_Angeles")
fails, checked = [], []
UUID1, UUID2 = "uuid-account-1", "uuid-account-2"
TMP = pathlib.Path(tempfile.mkdtemp(prefix="budget-test-"))


def acct(name, uuid):
    """The table's row `name`, signed in as `uuid` (a temp config dir whose .claude.json names it)."""
    row = next(r for r in RULES["accounts"]["list"] if r["name"] == name)
    d = TMP / name
    d.mkdir(exist_ok=True)
    (d / ".claude.json").write_text(json.dumps({"oauthAccount": {"accountUuid": uuid}}), encoding="utf-8")
    return accounts.Account(name=name, config_dir=d, explicit=True, weekly_reset=row["weekly_reset"],
                            quota={**RULES["quota"], **row.get("quota", {})})


A1, A2 = acct("account1", UUID1), acct("account2", UUID2)
RESET1, RESET2 = A1.weekly_reset, A2.weekly_reset


def check(name, got, want):
    checked.append(name)
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def at(y, mo, d, h, mi=0):
    return dt.datetime(y, mo, d, h, mi, tzinfo=LA)


def ev(t, model, inp=0, out=0, cw=0, cr=0, who=UUID1):
    e = {"event.timestamp": t.astimezone(dt.timezone.utc).isoformat().replace("+00:00", "Z"), "model": model,
         "input_tokens": inp, "output_tokens": out, "cache_creation_tokens": cw, "cache_read_tokens": cr}
    if who:
        e["user.account_uuid"] = who
    return e


def source(events):
    return lambda since, until: list(events)


# 1. the week starts Sunday 03:00 Los Angeles (2026-10-04 is a Sunday)
check("week start, Sunday after 03:00", budget.week_start(at(2026, 10, 4, 14), RESET1), at(2026, 10, 4, 3))
check("week start, Sunday before 03:00", budget.week_start(at(2026, 10, 4, 2), RESET1), at(2026, 9, 27, 3))
check("week start, Wednesday", budget.week_start(at(2026, 10, 7, 9), RESET1), at(2026, 10, 4, 3))
# DST ends 2026-11-01 at 02:00: the reset that day is still 03:00 wall-clock
check("week start across DST", budget.week_start(at(2026, 11, 1, 12), RESET1).hour, 3)

# 2. points: 1 M counted Sonnet tokens = 1 point; 0.4 M Opus = 1 point; cache reads are not counted
cal = FIXTURE["calibration"]
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
reading = {"noted_at": at(2026, 10, 5, 9).isoformat(), "account": "account1", "weekly_pct": 20.0, "session_pct": 10.0,
           "session_reset": at(2026, 10, 5, 13).isoformat()}
r = budget.estimate(now, [reading], source([ev(at(2026, 10, 5, 10), "sonnet", out=2_000_000)]), FIXTURE, A1)
check("go: weekly", r["weekly_est_pct"], 22.0)
check("go: allowance day 2", (r["day"], r["allowance_pct"]), (2, 28.6))
check("go: session = anchor + 2 points x 5", r["session_est_pct"], 20.0)
check("go: decision", r["decision"], "go")
check("go: headroom", r["headroom_pct"], 6.6)

r = budget.estimate(now, [reading], source([ev(at(2026, 10, 5, 10), "sonnet", out=9_000_000)]), FIXTURE, A1)
check("hold-day over allowance", (r["decision"], r["resume_at"]), ("hold-day", at(2026, 10, 6, 3, 5).isoformat()))
r = budget.estimate(now, [reading], source([ev(at(2026, 10, 5, 10), "sonnet", out=9_000_000)]), FIXTURE, A1, borrow_days=1)
check("borrowing a day turns hold-day into go/hold-session", r["allowance_pct"], 42.9)

r = budget.estimate(now, [reading], source([ev(at(2026, 10, 5, 10), "opus", out=32_000_000)]), FIXTURE, A1, borrow_days=6)
check("stop-week at the cap", (r["decision"], r["allowance_pct"]), ("stop-week", 97.0))  # the cap is model_rules.json weekly_cap_pct (owner 2026-10-07 09:40, kb/Work R69 section 6)

hot = dict(reading, session_pct=95.0)  # the soft stop is 97 (owner 2026-10-07 09:40)
r = budget.estimate(now, [hot], source([ev(at(2026, 10, 5, 10), "sonnet", out=1_000_000)]), FIXTURE, A1)
check("hold-session at the soft stop", (r["decision"], r["resume_at"]), ("hold-session", at(2026, 10, 5, 13).isoformat()))

# 4. a reading from last week anchors nothing; the newest valid reading wins
old = {"noted_at": at(2026, 10, 3, 9).isoformat(), "account": "account1", "weekly_pct": 90.0, "session_pct": 0, "session_reset": None}
r = budget.estimate(now, [old], source([]), FIXTURE, A1)
check("last week's reading ignored", (r["anchor"], r["weekly_est_pct"]), (None, 0.0))
newer = dict(reading, noted_at=at(2026, 10, 5, 11).isoformat(), weekly_pct=25.0)
r = budget.estimate(now, [reading, newer, old], source([]), FIXTURE, A1)
check("newest reading is the anchor", r["weekly_est_pct"], 25.0)

# 5. a session reset already passed: only the spend in the current window counts
past = dict(reading, session_reset=at(2026, 10, 5, 10).isoformat())
r = budget.estimate(now, [past], source([ev(at(2026, 10, 5, 9, 30), "sonnet", out=4_000_000),
                                          ev(at(2026, 10, 5, 11), "sonnet", out=1_000_000)]), FIXTURE, A1)
check("new window after reset", (r["session_est_pct"], r["resume_at"]), (5.0, None))

# 6. --record appends a reading through the CLI
cdir = pathlib.Path(tempfile.mkdtemp(prefix="budget-test-"))
env = dict(os.environ, COBOL_COORD_DIR=str(cdir))
cmd = [sys.executable, str(HERE / "budget.py"), "--account", "account1", "--record", "--weekly", "31.5", "--session", "12",
       "--session-reset", "2026-10-05T13:00:00-07:00", "--now", "2026-10-05T12:00:00-07:00"]
check("record exit", subprocess.run(cmd, env=env, capture_output=True).returncode, 0)
check("record exit again", subprocess.run(cmd, env=env, capture_output=True).returncode, 0)
saved = json.loads((cdir / "readings.json").read_text(encoding="utf-8"))
check("readings appended", [x["weekly_pct"] for x in saved], [31.5, 31.5])
check("readings stamped with the account", [x["account"] for x in saved], ["account1", "account1"])
bad = subprocess.run(cmd[:-2] + ["--session-reset", "17:00"], env=env, capture_output=True)
check("zoneless reset refused", bad.returncode != 0, True)
r = subprocess.run([sys.executable, str(HERE / "budget.py"), "--json", "--now", "2026-10-05T12:30:00-07:00",
                    "--account", "account1", "--telemetry-dir", str(cdir)], env=env, capture_output=True, text=True)
out = json.loads(r.stdout)
check("CLI estimate from the recorded anchor", (out["weekly_est_pct"], out["decision"], out["account"]),
      (31.5, "hold-day", "account1"))
r = subprocess.run([sys.executable, str(HERE / "budget.py"), "--json", "--now", "2026-10-05T12:30:00-07:00",
                    "--account", "account2", "--telemetry-dir", str(cdir)], env=env, capture_output=True, text=True)
check("CLI: another account's reading anchors nothing", json.loads(r.stdout)["anchor"], None)
r = subprocess.run([sys.executable, str(HERE / "budget.py"), "--json", "--account", "nobody"], env=env,
                   capture_output=True, text=True)
check("CLI: an account the table lacks is refused", r.returncode, 2)

# 7. two accounts (kb/Work PB2478). Account 2's week resets Saturday 10:00 Los Angeles (2026-10-10 is a Saturday).
check("account 2 week start, Saturday after 10:00", budget.week_start(at(2026, 10, 10, 11), RESET2), at(2026, 10, 10, 10))
check("account 2 week start, Saturday before 10:00", budget.week_start(at(2026, 10, 10, 9), RESET2), at(2026, 10, 3, 10))
check("account 2 week start, Sunday", budget.week_start(at(2026, 10, 11, 4), RESET2), at(2026, 10, 10, 10))
# 7a. at Saturday 10:30 account 2 is in a NEW week: its Saturday-09:00 reading of 90 % anchors nothing, so it goes
#     (one account's reset made the loop hold while the meter read 0 %: the note's lost quota).
sat = at(2026, 10, 10, 10, 30)
r2 = {"noted_at": at(2026, 10, 10, 9).isoformat(), "account": "account2", "weekly_pct": 90.0, "session_pct": 10.0,
      "session_reset": None}
r = budget.estimate(sat, [r2], source([]), FIXTURE, A2)
check("Saturday-10:00 reset: account 2's new week", (r["anchor"], r["weekly_est_pct"], r["day"], r["decision"]),
      (None, 0.0, 1, "go"))
check("Saturday-10:00 reset: the week start", r["week_start"], at(2026, 10, 10, 10).isoformat())
r = budget.estimate(sat, [dict(r2, account="account1")], source([]), FIXTURE, A1)
check("the same instant is still account 1's old week", (r["weekly_est_pct"], r["day"]), (90.0, 7))
# 7b. interleaved readings: account 1 at 86 % (13:08), account 2 at 0 % (13:26), an unstamped one in between
wed = at(2026, 10, 7, 14)
mix = [{"noted_at": at(2026, 10, 7, 13, 8).isoformat(), "account": "account1", "weekly_pct": 86.0, "session_pct": 20.0,
        "session_reset": None},
       {"noted_at": at(2026, 10, 7, 13, 20).isoformat(), "weekly_pct": 50.0, "session_pct": 1.0, "session_reset": None},
       {"noted_at": at(2026, 10, 7, 13, 26).isoformat(), "account": "account2", "weekly_pct": 0.0, "session_pct": 0.0,
        "session_reset": None}]
r = budget.estimate(wed, mix, source([]), FIXTURE, A1)
check("interleaved: account 1 anchors on its own 86 %", (r["weekly_est_pct"], r["account"]), (86.0, "account1"))
check("interleaved: the unstamped reading anchors nothing and is counted", r["unstamped_readings"], 1)
r = budget.estimate(wed, mix, source([]), FIXTURE, A2)
check("interleaved: account 2 anchors on its own 0 %", (r["weekly_est_pct"], r["anchor"]["noted_at"]),
      (0.0, at(2026, 10, 7, 13, 26).isoformat()))
# 7c. another account's telemetry is ignored; an event with no account attribute is counted (cannot be attributed)
evs = [ev(at(2026, 10, 7, 13, 30), "opus", out=4_000_000, who=UUID1),
       ev(at(2026, 10, 7, 13, 40), "opus", out=400_000, who=UUID2),
       ev(at(2026, 10, 7, 13, 50), "sonnet", out=1_000_000, who=None)]
r = budget.estimate(wed, mix, source(evs), FIXTURE, A2)
check("account 2 counts its own and the unattributed spend only", (r["spend"]["points"], r["weekly_est_pct"]), (2.0, 2.0))
check("account 2's telemetry filter", r["telemetry_account"], UUID2)
r = budget.estimate(wed, mix, source(evs), FIXTURE, A1)
check("account 1 counts its own and the unattributed spend only", r["spend"]["points"], 11.0)
# 7d. per-account quota overrides apply
capped = accounts.Account(name="account2", config_dir=A2.config_dir, explicit=True, weekly_reset=RESET2,
                          quota={**RULES["quota"], "weekly_cap_pct": 1})
r = budget.estimate(wed, mix, source(evs), FIXTURE, capped, borrow_days=6)
check("a per-account cap is the account's own", (r["decision"], r["allowance_pct"]), ("stop-week", 1.0))

# 8. THE LIVE CALIBRATION against the meter it was refit to (kb/Work PB2478, model_rules.json calibration.source):
#    account 2 read week 0 % / session 0 % at 13:34 PDT and week 6 % / session 24 % at 14:54, with its own telemetry
#    counting 9,481,140 Opus, 108,118 Sonnet and 646,023 Haiku tokens between. At 14:54 the estimate must agree with the
#    meter and GO; the 2026-10-04 constants (the FIXTURE) made it 25 points and a 127 % session and HELD the loop.
r0 = {"noted_at": at(2026, 10, 7, 13, 34).isoformat(), "account": "account2", "weekly_pct": 0.0, "session_pct": 0.0,
      "session_reset": at(2026, 10, 7, 15, 9).isoformat()}
spend = [ev(at(2026, 10, 7, 14, 0), "claude-opus-5-5", inp=282_339, out=2_091_032, cw=7_107_769, cr=401_354_268, who=UUID2),
         ev(at(2026, 10, 7, 14, 10), "claude-sonnet-5-5", inp=5_116, out=11_941, cw=91_061, who=UUID2),
         ev(at(2026, 10, 7, 14, 20), "claude-haiku-4-5", inp=561_919, out=84_104, who=UUID2),
         ev(at(2026, 10, 7, 14, 30), "claude-opus-5-5", out=40_000_000, who=UUID1)]   # account 1's: never counted
meter = at(2026, 10, 7, 14, 54)
r = budget.estimate(meter, [r0], source(spend), RULES, A2)
check("live calibration: week within a point of the meter's 6 %", abs(r["weekly_est_pct"] - 6.0) <= 1.0, True)
check("live calibration: session within 4 points of the meter's 24 %", abs(r["session_est_pct"] - 24.0) <= 4.0, True)
check("live calibration: the 14:54 reading does not hold the loop", r["decision"], "go")
r = budget.estimate(meter, [r0], source(spend), FIXTURE, A2)
check("the 2026-10-04 constants held it (the defect)", r["decision"], "hold-session")

for f in fails:
    print("FAIL:", f)
print(f"budget self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
