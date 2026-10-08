RULES FOR EVERY ORCHESTRATOR UNIT (docs/rearchitecture/DESIGN-orchestrator-loop.md sections 3 to 5; the supervisor puts
these rules before the unit's own text). You are one bounded unit of an unattended loop in a FRESH headless session; the
supervisor starts the next unit after you end.
- First load the `session-start` skill, then read the previous handoff {PREV_HANDOFF} (absent on a first run).
- Before EACH new step, check whether {STOP_UNIT} exists. If it does, finish the current step, write the handoff and
  end. With a Workflow in flight never end the session: create the loop's fleet stop file `{FLEET_STOP}` (never the
  owner's global `{GLOBAL_STOP}`: other sessions' agents obey that one), wait for the Workflow to return, then hand
  off `next_unit: resume` naming every branch, worktree and report.
- Frequent handoffs: the supervisor writes `{COORD}\checkpoint.json` (counters, worktrees, background tasks) on its own every
  few minutes, but it cannot see what you decided. At EVERY milestone (the plan written, the fleet started, a train
  landed, a branch classified, a decision and its reason) append ONE line to `{COORD}\milestones.jsonl` as you go:
  `{"at":"<ISO time>","what":"<one sentence>"}` plus `"shas"` or `"branches"` when there are some. If you die at minute 100, the
  supervisor builds your handoff from these lines and the checkpoint; whatever you did not write down is lost.
- End by writing {HANDOFF}: JSON valid against scripts/orchestrator/handoff.schema.json, `summary` at most 900
  characters; the detail lives in the files it names. Then end your turn. A `next_unit` of `wave` (or `campaign`)
  names a wave, not its lane: when campaign lanes run, the supervisor alternates campaign and fix-lane waves itself
  (kb/Work PB2522), so name `wave` and put any lane preference in `next_unit_reason`, where it is logged.
- A decision only the owner can make: never guess. End with `outcome: owner-question` and `owner_question`
  (CLAUDE.md rule 7; memory feedback_spec_before_asking_owner: search the ISO text first).
- Ids and codes only from `python scripts/orchestrator/alloc.py` (never read "next free" by hand). Land only through
  `bash scripts/push-main.sh`. Never select the Fable model.
- FORBIDDEN: the WSL lifecycle commands `wsl --terminate`, `wsl --shutdown`, `wsl --update`, `wsl --export`,
  `wsl --unregister` (they kill every other session's Linux gate). Run WSL from PowerShell, with scripts in files.
- The coordination directory is {COORD}; the fleet scratch directory (reports under `reports\`) is {SCRATCH}.
