UNIT: resume.

The previous unit ended split, failed, was stopped with work in flight, or left no valid handoff. Rebuild the state
from disk; do not re-do finished work.
1. `git status` and `git log origin/main..main` in E:\COBOL; `python scripts/prune_worktrees.py` (dry run);
   for each worktree it lists, `python tools/claude-skills/skills/agent-fleet/references/status_delta.py <worktree>`;
   the newest reports under {SCRATCH}\reports; the supervisor's last log under {COORD}\logs.
2. Classify every branch: ready to land (DONE, gated), to finish (SPLIT or interrupted: plan_wave.py picks it up as
   a finisher), or abandoned (record why in its kb/Work note). Commit or remove nothing you have not read.
3. Write the handoff with `branches_pending` and `next_unit`: `land` when anything is ready, otherwise `wave`.
