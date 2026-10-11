UNIT: land.

Finished implementer branches are waiting for a lander.
1. Load the `workstream` skill; read .claude/skills/workstream/references/landing.md and
   .claude/skills/workstream/templates/lander-train-brief.md.
2. The candidates are the branches `python scripts/prune_worktrees.py --brief` lists WAITING TO LAND (its newest
   `w<wave><letter>-PB<lead>-report.md` under {COORD}\scratch\reports says DONE: the loop's own branches AND the attended
   session's, which writes its finished branches' reports there instead of dispatching a lander while the loop runs,
   MANDATORY-PRACTICES O11, kb/Work PB2602). ⛔ The train is EXACTLY the rows `python scripts/prune_worktrees.py --train`
   names TRAIN: at most six (the owner's band of 4-6 clusters), the most harmful lead first. The HELD rows stay WAITING
   TO LAND for the next land unit; name them in the handoff's `branches_pending`. Never one train of every waiting
   branch (owner 2026-10-10, kb/Work PB2981: a 15-branch train split into three serial landers and landed nothing for
   hours). A previous handoff's `branches_pending` is already in the survey. An operator steer that names the train
   (`handoff.last.json`) wins over `--train`. Dispatch one cobol-lander train over them, its prompt naming `LOOP STATE: running` (the
   dispatch guard refuses a lander call without it), with lead ids from `python scripts/orchestrator/alloc.py pb 5`,
   and stay until it lands or reports every drop. A lander whose push-main.sh the permission layer refused reports
   `READY-TO-PUSH <worktree> <sha>` (L13): hand that line off in `summary`; never push some other way. The lander
   takes the LANDING LEASE before its final rebase and gates (lander-train-brief step 2b, kb/Work PB2537): if
   `python scripts/orchestrator/landing_lease.py status` names another holder, the lander waits for it rather than
   gating against a main that holder is about to move; push-main.sh releases it. The lander brings the branches in with ONE command, `python scripts/orchestrator/train_apply.py <its train manifest>` (one commit per cluster; it stops only at a conflict, which the lander resolves), and push-main.sh refuses a train whose arch-oracle baseline is behind its tree (exit 7, kb/Work PB2885). After the push, `python scripts/prune_worktrees.py --apply` removes the branches that landed.
3. Render the ledger in the same turn: `python scripts/spec/gen_ledger.py --out {COORD}\ledger.html` (every figure on it is computed; its
   trend file gains a point when GAP or the register's series moved). You cannot PUBLISH it
   (a headless session has no Artifact tool): the supervisor announces the owed publish after you end, and the attended
   session publishes `{COORD}\ledger.html` to the owner's artifact. Do not look for a way around that.
4. Write the handoff (`landed`, the branches still pending, `next_unit` null unless you know better).
