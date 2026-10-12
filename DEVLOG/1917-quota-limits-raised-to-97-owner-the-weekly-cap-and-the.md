## Entry 1917 — 2026-10-07 09:49 PDT — Quota limits raised to 97 % (owner): the weekly cap and the session soft stop; the 09:06 STOP cleared

**What changed.** The owner at 09:40: "raise session and weekly quota limits to 97%". `scripts/orchestrator/model_rules.json`
now carries `weekly_cap_pct` 97 (was 85 since 06:00 today) and `session_soft_stop_pct` 97 (was 70), with `session_hard_stop_pct`
99 so a hold still precedes a kill; each constant restates its date and source. `test_budget.py` checks the cap at 97 and the
hold-session at a 95 % anchor. MANDATORY-PRACTICES O3 names the two constants instead of the old "~85 % session STOP";
`DESIGN-orchestrator-loop.md` and plan §0 read the new numbers; kb/Work R69 §6 records the decision under its two
predecessors.

**Why it mattered this morning.** At 08:50 the session meter read 71 % (53 % twenty minutes earlier: the Draft 3 reviser of
PB2118 and the loop's wave of six Opus implementers were running together), so the attended session set the STOP under the
70 % rule; the loop wound its wave unit down and the reviser finished with a green gate before the stop reached it. The owner
raised both limits at 09:40 and the STOP was cleared at 09:45 (meter: weekly 79 %, session 76 %). The rule the limits serve
is unchanged: never exceed the session window, because an aborted job wastes the quota it spent. At 10:15 the owner added "You can push it to 100% as long as you do not cause any work to abort due to lack of quota": 100 is the ceiling, 97 the dispatch cap (the ceiling minus one wave's estimation margin), so in-flight work finishes inside the last three points.

