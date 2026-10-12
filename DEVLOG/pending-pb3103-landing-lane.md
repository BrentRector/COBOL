## PB3103: the loop's landing lane — a reviewed branch lands the moment it is ready, beside whatever unit runs; landing gates first in the gate-slot queue

**Why (owner-approved design, 2026-10-12, "Supervisor lane (Recommended)").** Wave 1069 measured the cost of landing
through a unit: group D was stamped `Review: CLEAN` at 12:34 PDT and E and F by 13:05, and all three read WAITING TO
LAND while G and H ran on; they could land only once the wave unit ended and `next_unit.py` (rule 6: DONE branches in
the handoff → `enqueue`) started an enqueue unit, whose step 3 ran `enqueue.py land` and blocked on its gates and the
merge queue while no implementer ran. And `gate_slot.py` was one FIFO, so a landing's gate could wait behind three
implementer gates.

**Re-probe (this worktree).** `next_unit.py` with a fresh meter reading and a handoff listing `w9a` DONE printed
`{"unit": "enqueue", "reason": "finished branches waiting: w9a"}`; `units/enqueue.md` step 3 ran `enqueue.py land
<branch>` itself; `GateSlots.acquire` counted `len(self._live_tickets_before(seq))`, with no class.

**The fix, the note's five items and nothing else.**
1. `scripts/orchestrator/land_lane.py` (new, deterministic): `tick` reaps finished attempts (a runner gone with no exit
   code is DIED; an exit 8 writes `land-lane\HALT` and names it in the operator's mailbox), surveys through
   `prune_worktrees.survey` (imported) into `land-lane\survey.json`, and, unless HALT, STOP-UNIT, the loop's STOP or the
   global stop exists, starts `enqueue.py land <branch> --worktree <wt>` DETACHED for every WAITING TO LAND row in
   survey order and every branch whose last attempt is retryable at an unchanged tip. `decide`: a new head launches;
   ENQUEUED waits for the queue; exit 3 is retried at its head once `landing_check.py` passes on the branch's own
   range; exit 2, 9 or DIED are retried once; exit 4, 5, 6, 7, 11 (or a repeated 2/9) are STOPPED for the enqueue
   unit. A retry is driven by the lane's attempt record, not the survey: a run that commits makes the survey read the
   branch IN FLIGHT. `status` prints one line per branch, `collect` marks attempts the enqueue unit handled,
   `clear-halt` is the operator's. `--self-test`: 30 checks (planted survey rows, exit 3 with and without the overlap,
   exit 5 at the same and a new head, exit 9 then 2, DIED (before and after the run committed), exit 8 halting a second ready branch and the mailbox
   message, the three stops, ENQUEUED and its collection, a stale survey, the tick lock, an unreadable attempt file said aloud, and a real detached runner).
2. `orchestrate.ps1`: `Start-LaneTick` / `Complete-LaneTick`, the checkpoint's background pattern (PB2590): a tick as
   each unit starts and every `-LandLaneSeconds` (300; 0 runs no lane), one at a time, never awaited, never fatal.
3. `gate_slot.py`: ticket classes, `ticket-<seq>-landing.lock` beside `ticket-<seq>.lock`; every landing ticket is
   served before every implementer ticket, ticket order within each class, the cap unchanged; `enqueue.py run_gates`
   sets `COBOL_GATE_CLASS=landing` for its gates (its self-test proves the gate sees it). The 11th self-test arm queues
   W1 L1 W2 L2 behind one holder at cap 1 and requires L1 L2 W1 W2, `status` in that order, and an unknown class
   refused; `GateSlotDriftTests` holds the arm.
4. `units/enqueue.md` step 3 reads `land_lane.py status` (ticking once itself only when the survey is stale) and
   lands nothing; `next_unit.py` rule 6 is `enqueue_work`: an enqueue unit only for agent work (`land_lane.agent_work`:
   WAITING FOR REVIEW or BOUNCED in a current survey, a STOPPED or ENQUEUED attempt not collected), or DONE branches
   while the lane has no survey younger than 30 min. `test_orchestrate_campaign.ps1` 8f re-states the rule (54/54).
5. MANDATORY-PRACTICES O11 (and L1, L2), DESIGN-orchestrator-loop.md (components, coordination files, unit table,
   rule 6, the new section 4.9, tests) and DESIGN-test-build-ci.md (§3.14.6's two classes and eleven arms, §3.17.2,
   §3.17.5) move with it, and the sentences elsewhere that said the enqueue unit lands (wave.md, resume.md, SKILL.md,
   landing.md, golden-lane-brief.md, wf_rolling_wave.js comments, prune_worktrees.py, plan_wave.py, enqueue.py,
   DOC_INDEX.md) now say the lane does. `LandingLaneDriftTests` (Unit): the supervisor starts the lane in the
   background from Invoke-Unit at least twice, and enqueue.md step 3 names `land_lane.py status` and runs no
   `enqueue.py land`, with a planted-step arm.

**Branch review round 1 (two findings, both fixed).** R1-1: a stop resolved WITHOUT a new commit had no way back
into the lane. `decide` never relaunches a STOPPED or queued attempt at the same head, `candidates` skipped BOUNCED
rows, and step 3 may no longer run `enqueue.py land`, so a bounce its finisher attributed to infrastructure, a GATE RED
that was main's own, or an UNREVIEWED exit 11 later stamped CLEAN would have stranded its branch silently. The fix is
the enqueue unit's `land_lane.py rearm <branch> --why <cause>`: a `land-lane/rearm/<branch>.json` record at the
worktree's tip that `candidates` and `decide` honour once (the launch consumes it, a moved tip drops it, a running
attempt refuses it), and `agent_work` no longer counts a BOUNCED branch the lane is landing or re-armed to land.
Self-test case 12 plants all three stops (8 checks). R1-2: the wording sweep had missed MANDATORY-PRACTICES O2 and L12,
units/enqueue.md step 4, finisher-brief.md, wf_rolling_wave.js's meta, landing.md's exit-3 line, check_practices.py's
comment, SKILL.md (two more) and DESIGN-orchestrator-loop.md's failure table; all now say the lane lands and the unit
re-arms. `LandingLaneDriftTests` also requires step 3 to name `land_lane.py rearm`.

**Branch review round 2 (R2-1).** Round 1's wording sweep missed four sentences its pattern could not match:
prune_worktrees.py's BOUNCED row (a finisher "re-runs enqueue.py"), the cobol-finisher role definition ("landed by the
enqueue unit"), a plan_wave.py comment ("its finished work is the enqueue unit's") and the BLOCKED line enqueue.py
writes on a conflict ("re-runs enqueue.py"). All four now say the finisher returns DONE, the new tip is reviewed, the
landing lane lands it, and a no-commit resolution is re-armed. Nothing guarded this wording but a hand grep, so
`LandingLaneDriftTests.NoCurrentFile_SaysTheEnqueueUnitLands` now sweeps the tracked tree outside the history paths for
the retired phrasings. Its planted-failure fact checks that each phrasing is found and that the current sentences
("lands nothing itself", "its lane lands it", "the enqueue unit's `rearm`") are not.

**Measure after it lands:** `landing_measure.py summary` (DONE → enqueued → merged per pull request) for the first waves
after the lane against wave 1069's landings.
