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
   `clear-halt` is the operator's. `--self-test`: 29 checks (planted survey rows, exit 3 with and without the overlap,
   exit 5 at the same and a new head, exit 9 then 2, DIED (before and after the run committed), exit 8 halting a second ready branch and the mailbox
   message, the three stops, ENQUEUED and its collection, a stale survey, the tick lock, and a real detached runner).
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

**Measure after it lands:** `landing_measure.py summary` (DONE → enqueued → merged per pull request) for the first waves
after the lane against wave 1069's landings.
