UNIT: land.

Finished implementer branches are waiting for a lander, or the ledger is older than the last landing.
1. Load the `workstream` skill; read .claude/skills/workstream/references/landing.md and
   .claude/skills/workstream/templates/lander-train-brief.md.
2. The candidates are the previous handoff's `branches_pending` with status DONE, cross-checked with
   `python scripts/prune_worktrees.py` (UNLANDED and CHECK rows). Dispatch one cobol-lander train over them, with
   lead ids from `python scripts/orchestrator/alloc.py pb 5`, and stay until it lands or reports every drop.
3. Refresh the ledger in the same turn (`python scripts/spec/gen_ledger.py`). With nothing to land, only refresh it.
4. Write the handoff (`landed`, the branches still pending, `next_unit` null unless you know better).
