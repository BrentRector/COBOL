UNIT: resume.

The previous unit ended split, failed, was stopped with work in flight, or left no valid handoff. Rebuild the state
from disk; do not re-do finished work.
1. `git status` and `git log origin/main..main` in E:\COBOL; `python scripts/prune_worktrees.py` (dry run);
   for each worktree it lists, `python tools/claude-skills/skills/agent-fleet/references/status_delta.py <worktree>`;
   the newest reports under {SCRATCH}\reports; the supervisor's last log under {COORD}\logs.
2. Classify every branch by `python scripts/prune_worktrees.py`'s lifecycle (MANDATORY-PRACTICES O10, owner
   2026-10-08, kb/Work PB2600): IN FLIGHT (leave it), WAITING TO LAND (the land unit takes it), LANDED (`--apply`
   removes it) or DECISION. DECIDE every DECISION row in this unit, never carry it forward to the next handoff: to
   finish (SPLIT or interrupted: plan_wave.py picks it up as a finisher), to land, or ABANDONED (a line in its kb/Work
   note naming the branch, the word ABANDONED and why; then `--apply --abandon <branch>`). One the owner must decide
   becomes `outcome: owner-question`. Commit or remove nothing you have not read.
3. Write the handoff with `branches_pending` and `next_unit`: `land` when anything is ready, otherwise `wave`.
