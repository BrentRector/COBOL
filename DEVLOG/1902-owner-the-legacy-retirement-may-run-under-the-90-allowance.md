## Entry 1902 — 2026-10-07 00:20 PDT — Owner: the legacy retirement may run under the 90 % allowance this week; wave 1025 dispatched (PB2108 + PB2109 as one implementer, PB2110 as its successor)

**The question and the answer.** Entry 1901 ended on one bare question: "May the legacy retirement cluster run under the 90 %
allowance this week?" The owner answered "yes" (kb/Work R69 §5; the quota memory records the same). The meter, read on the
claude.ai usage page at 00:25 PDT and recorded with `budget.py --record`: session 9 %, weekly 57 %, Fable 28 %. Weekly 57 % is
exactly the day-4 ordinary target (4 × 14.3 %), and other work is capped at 44 % (owner 2026-10-05), so the review's R0 items
(PB2115–PB2117) and PB2120 are NOT in this wave; only the retirement cluster runs, under the 90 % allowance.

**The dispatch, through the required path.** The `workstream` skill (over `brent-tools:agent-fleet` 1.19.0) and
MANDATORY-PRACTICES were loaded; the quota meter was read on the claude.ai usage page before the dispatch and recorded with
`budget.py --record`; the groups file `E:\COBOL-coord\scratch\groups-w1025.json` was rendered into six specs by
`make_dispatch_specs.py` and `check_practices.py` printed GREEN over them and the groups file; the Workflow rolling wave runs
them with the stall watchdog beside it. Group A is PB2108 + PB2109 in ONE Opus implementer: Cut 1a and Cut 1b are one
mechanism (remove every edge from the greenfield test, script and CI graph to the legacy engine), so one orientation serves
both. Group B is PB2110 (Cut 2, the deletion) as A's same-file-successor (`after: 'A'`): it merges A's branch and orients from
A's report, and A lands THROUGH it, so the train is one landing carrying both cuts (`train_size` 1, `min_final_train` 1: a
one-cluster landing is L1's forbidden corner, allowed here because Cut 1 + Cut 2 is the blocking step for the rest of the
cluster and nothing else is ready). The archive tag `legacy-byte-engine-final` is created by the orchestrator on main's last
pre-delete commit after the train lands, never by an implementer on a branch commit. PB2111–PB2114 follow in the next wave.

**Why by hand.** `work.py next` and the orchestrator plan harm-flagged defects only (Entry 1901), so this is the hand-written
`groups.json` path the skill sanctions for a supervised one-off; PB2120 makes the next campaign wave plannable by the machinery
and runs when the ordinary allowance has headroom.

