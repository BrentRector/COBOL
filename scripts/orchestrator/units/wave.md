UNIT: wave.

1. Load the `workstream` skill (dispatch_guard.py refuses the Workflow call otherwise) and follow
   .claude/skills/workstream/templates/MANDATORY-PRACTICES.md.
2. `python scripts/orchestrator/plan_wave.py --from-budget --scratch {SCRATCH}` plans the wave deterministically,
   allocates codes and lead ids, renders the specs with make_dispatch_specs.py and runs check_practices.py. Read its
   table. Change the plan only for a reason you write into the handoff; re-run it rather than editing groups.json.
3. Start `.claude/skills/workstream/templates/wf_rolling_wave.js` with the args file it wrote
   ({SCRATCH}\wf-args-w<wave>.json), and run stall_watch.py beside it as the skill says.
4. Stay in this session until the Workflow returns and its last lander train has landed (the Workflow dies with this
   process). Judge each lander report: a dropped cluster gets its reason into its kb/Work note.
5. In the same turn as the landing report, refresh the ledger (`python scripts/spec/gen_ledger.py`, as
   .claude/skills/workstream/references/landing.md says).
6. Write the handoff: `workflow.state`, `landed`, and every unlanded branch in `branches_pending`; `next_unit` null
   unless you know better.
