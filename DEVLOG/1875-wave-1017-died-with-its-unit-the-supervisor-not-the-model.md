## Entry 1875 — 2026-10-04 17:40 PDT — Wave 1017 died with its unit; the supervisor, not the model, now owns the session's lifetime, and STOP closes work down without losing any

The second real `wave` unit (opus, 17:03) planned eight groups (wave 1017, groups A-H, Sonnet implementers) and launched the rolling-wave
Workflow, which ran six implementers for eleven minutes (488 tool calls, 1.2 M tokens). The unit then wrote "Six implementers are
running and the watchdog is back up. I'm waiting for the Workflow to finish." and ended its turn. Under `claude -p` a session exits
when its model ends a turn (the Workflow tool's "you will be notified when it completes" needs a live session), so the process exited
0 with no handoff, the supervisor scored FAILURE 1 of 3, and the Workflow and two implementer gate runs were killed (the unit's stream
ends with `task_notification ... status: stopped`). Stranded: six worktrees `wf_a4d90677-e09-1..6`, two with WIP commits (groups C and
D) and four with uncommitted changes.

First fix, and why it was not enough: `wave.md` step 4 told the model to wait with foreground `until grep -q totalToolCalls` calls on
the Workflow's task-output file (`{TASKS_DIR}`, substituted by the supervisor). That depends on a model obeying a prompt, which the
owner rightly called not robust. The structural fix: the unit's lifetime belongs to the supervisor. The prompt is now the first
`--input-format stream-json` message and stdin stays open; a probe on Haiku showed a session ended its turn at 16.8 s and was WOKEN by
a background task's completion at 41 s. The supervisor tracks `result` and `system/background_tasks_changed` events and closes stdin
only when the model has ended a turn, no background task runs, and the stream has been quiet for `-IdleCloseSeconds` (20; a task's
completion arrives just after its list empties). The wait paragraph stays as a second line of defence. Verified on the real CLI with a
`meter` unit (63 s, valid handoff, reading recorded); the fake-claude harness now stays alive after `result` until the supervisor closes
stdin, with cases for a turn that ends with a task running and is woken, and for closing a finished unit.

STOP (owner: "close down work asap when needed without losing any"): `STOP` used to act only between units, so a multi-hour wave ran on.
During a unit the supervisor now winds it down itself: `STOP-UNIT` plus the fleet's `scratch\STOP` (every agent checkpoint-commits and
returns `SPLIT`), the unit hands off `next_unit: resume`, and only then the loop ends; a kill comes only after `-GraceMinutes`. A stale
fleet STOP is removed at the next unit's start. `scripts/orchestrator/stop.ps1` creates STOP, `-Status` reports, `-Clear` removes it.
`test_orchestrate.ps1` 58/58, including a STOP raised mid-unit, the stale fleet STOP, and the script.

Friction recorded: one lander-gate run went RED on `MoveTable16AskerDriftTests` with `UnauthorizedAccessException` reading
`FigurativeConstants.cs` (not an assertion); the immediate re-run was green (10,468 / 30,555 / 35). Cause unknown, a single occurrence,
so no note yet; a second occurrence becomes a `kb/Work` defect. The 28 `dotnet` processes seen then were idle MSBuild node-reuse workers.

**Files:** `scripts/orchestrator/orchestrate.ps1`, `scripts/orchestrator/stop.ps1`, `scripts/orchestrator/units/wave.md`,
`scripts/orchestrator/test_orchestrate.ps1`, `scripts/orchestrator/testdata/fake-claude.ps1`, `docs/rearchitecture/DESIGN-orchestrator-loop.md`.
No diagnostic code used.

