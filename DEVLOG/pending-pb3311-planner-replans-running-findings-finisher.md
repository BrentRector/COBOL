## Work register: file PB3311 (the wave planner re-plans a review finisher that another session is running)

Wave 1105's unit (the loop, account 2) ran `plan_wave.py --from-budget --borrow-days 0` at 17:12 PDT. Its plan held
eight groups, 1.88 of 5.99 weekly points. Group A was a `FINISH-FIRST` for PB3198, answering the round-3 branch review
finding of w1101a. An account-1 session was answering that same finding at that moment, in the branch's own worktree
(`wf_7bb8ec7e-39f-11`): a WIP commit for R3-1 at 17:02, origin/main merged in at 17:03, and a `build-local.ps1` gate
running since 17:04. The unit dropped A from the Workflow args, released `1105a` from the dispatch ledger
(`plan_wave.py --release 1105a`), and launched B to H.

PB3311 records the two holes that let the planner plan it:

- a finisher group's own branch is exempt from the RUNNING collision (`Group.continues`), and the FINDINGS arm never
  asks whether that branch is IN FLIGHT or whether its tip is still the reviewed head;
- `held_by_dispatch()` decides that a report answered a dispatch from the report file's mtime, and the branch
  reviewer's `Review:` stamp rewrites the report, so every review releases the hold of a finisher dispatch that is
  still running.

The note carries the fix shape, the self-tests and the sibling sweep (`prune_worktrees.py`, `land_lane.py`,
`next_unit.py`). Notes only; no code changed.
