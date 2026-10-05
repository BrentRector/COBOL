UNIT: wave.

1. Load the `workstream` skill (dispatch_guard.py refuses the Workflow call otherwise) and follow
   .claude/skills/workstream/templates/MANDATORY-PRACTICES.md.
2. `python scripts/orchestrator/plan_wave.py --from-budget --borrow-days {BORROW_DAYS} --scratch {SCRATCH}` plans the wave deterministically,
   allocates codes and lead ids, renders the specs with make_dispatch_specs.py and runs check_practices.py. Read its
   table. Change the plan only for a reason you write into the handoff; re-run it rather than editing groups.json.
3. Start `.claude/skills/workstream/templates/wf_rolling_wave.js` with the args file it wrote
   ({SCRATCH}\wf-args-w<wave>.json), and run stall_watch.py beside it as the skill says.
4. Stay in this session until the Workflow returns and its last lander train has landed (the Workflow dies with this
   process). The supervisor keeps this session alive and wakes you when a background task finishes, but do not rely on
   being woken: wave 1017's one-shot session was terminated 600 s after its model ended a turn with the Workflow
   running, and an eight-agent fleet died. Prefer waiting by tool calls: repeat, as separate foreground Bash calls,
   `timeout 580 bash -c 'until grep -q totalToolCalls "{TASKS_DIR}/<workflow task id>.output"; do sleep 20; done'`
   (the file is empty while the Workflow runs and holds its result with `totalToolCalls` when it returns) and read the
   stall watchdog's log between waits. Never end a turn while that file is empty.
   Judge each lander report: a dropped cluster gets its reason into its kb/Work note.
5. In the same turn as the landing report, refresh the ledger (`python scripts/spec/gen_ledger.py`, as
   .claude/skills/workstream/references/landing.md says).
6. Write the handoff: `workflow.state`, `landed`, and every unlanded branch in `branches_pending`; `next_unit` null
   unless you know better.
