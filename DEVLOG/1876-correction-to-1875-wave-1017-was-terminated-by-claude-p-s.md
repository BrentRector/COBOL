## Entry 1876 — 2026-10-04 18:00 PDT — Correction to 1875: wave 1017 was terminated by `claude -p`'s 600 s background ceiling, not by an ended turn; the first `resume` unit recovers all six worktrees

Entry 1875 said a headless session exits when its model ends a turn. That mechanism was wrong, and the real one was in the unit's own log all along: `logs/20261004-170356-wave.stderr.txt` reads "Background tasks still running after 600s; terminating. Set
CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0 to wait indefinitely." The model did end its turn at about minute one, a one-shot `claude -p` then
waits for background tasks, and at 600 s (the unit lasted 653 s) it terminated them, Workflow included. I read the stream's tail
(`end_turn`, tasks `killed`) and stopped there; the first-ever `resume` unit read the stderr file and reported the ceiling. Probes
2026-10-04: with stdin held open a task's completion wakes an idle model (turn ended 16.8 s, woken 41 s), and with the ceiling set to 5 s the
open-stdin session was still alive and woken at 30 s, so the supervisor-owned design of 1875 is correct and independent of the
ceiling: the open session never enters the exit wait. The design doc, the supervisor comment and `units/wave.md` now state the real
mechanism and the evidence; the wait-loop paragraph stays as a second line of defence.

`resume` (opus, 141 s, 20 calls, $1.17, handoff done) read all six wave-1017 worktrees, committed the uncommitted WIP in four of them as
"WIP checkpoint (interrupted, UNGATED)", wrote a `STATUS.md` in each, listed them as `branches_pending` and chose `next_unit: wave`:
`plan_wave.py` schedules the six interrupted groups (A-F) as finishers first, then G (PB1529) and H (PB1179), which never started. Worktree 2
has a stale lock (its pid is gone). The first real unit that ran to completion through the open-stdin path, and its handoff schema, held.

**Files:** `scripts/orchestrator/orchestrate.ps1` (comment), `scripts/orchestrator/units/wave.md`,
`docs/rearchitecture/DESIGN-orchestrator-loop.md`. No diagnostic code used.

