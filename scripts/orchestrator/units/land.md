UNIT: land.

Finished implementer branches are waiting for a lander.
1. Load the `workstream` skill; read .claude/skills/workstream/references/landing.md and
   .claude/skills/workstream/templates/lander-train-brief.md.
2. The candidates are the previous handoff's `branches_pending` with status DONE, cross-checked with
   `python scripts/prune_worktrees.py` (UNLANDED and CHECK rows). Dispatch one cobol-lander train over them, with
   lead ids from `python scripts/orchestrator/alloc.py pb 5`, and stay until it lands or reports every drop. The lander
   takes the LANDING LEASE before its final rebase and gates (lander-train-brief step 2b, kb/Work PB2537): if
   `python scripts/orchestrator/landing_lease.py status` names another holder, the lander waits for it rather than
   gating against a main that holder is about to move; push-main.sh releases it.
3. Render the ledger in the same turn: `python scripts/spec/gen_ledger.py --out {COORD}\ledger.html`. You cannot PUBLISH it
   (a headless session has no Artifact tool): the supervisor announces the owed publish after you end, and the attended
   session publishes `{COORD}\ledger.html` to the owner's artifact. Do not look for a way around that.
4. Write the handoff (`landed`, the branches still pending, `next_unit` null unless you know better).
