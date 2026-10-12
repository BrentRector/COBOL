## Entry 1877 — 2026-10-04 18:25 PDT — Wave 1018's first plan would have redone wave 1017's six WIP branches from main; plan_wave.py now hands a half note's unlanded branch to its finisher

The `wave` unit after the `resume` unit (DEVLOG 1876) ran `plan_wave.py --from-budget --borrow-days 1`. It planned
eight finishers, A to F being the six groups wave 1017 left as WIP worktrees. Every rendered spec said only "finish
the open half" and cut its worktree from main; none named a `worktree-wf_a4d90677-e09-*` branch. Dispatched as
planned, six implementers would have re-derived committed work and stranded the branches.

Two mechanisms, both in `scripts/orchestrator/plan_wave.py` (kb/Work PB2013):
- the `status: half` loop claimed the notes before the unlanded-branch loop, which skips claimed notes, so a branch
  whose notes are also half lost its resume instruction;
- `unlanded_branch_notes` kept only `UNLANDED` branches, and branch 2 (which only edited existing files) classified
  `CHECK`.

The half loop now appends the branch's resume instruction (new helper `branch_resume`, which `finisher_pred` reuses),
and `CHECK` branches count as unlanded. `test_plan_wave.py` gained two checks (30/30). Re-planned, all six specs
name their branch. The first plan's reservations (COBOLNET2822-2845, PB1993-PB2002) are abandoned, never used.

Wave 1018 then started (`wf_80c785b7-d71`). Its stall watchdog exited within seconds, reporting two just-spawned
agents' empty transcripts as stalled. Filed as kb/Work PB2014 (fix in the public skill, then move the pin). The
watchdog was restarted once the agents had written records.

