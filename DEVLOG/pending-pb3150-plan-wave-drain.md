## The wave planner drains: --drain plans only the finishers of started work (PB3150)

Under the owner's drain (2026-10-13 21:35 PDT: no new work items, only in-process work to main), wave 1075's unit ran
`plan_wave.py --max-groups 2` as the last handoff asked, for the two review finishers left (e99-4 R4-1 and
license-product-name R1-1). The plan could not be used. Its group A was the SPLIT of `w1074a` on
`worktree-wf_619cbe77-e99-1`, and that finisher would have started PB3101, which nobody had started. It would also
have cherry-picked e99-1's commits onto main again, although e99-4 (A2 of wave 1074) had merged e99-1 and carries
them. The e99-4 finisher came only after A. license-product-name was not planned at all: its lead PB3146 is
`process_only` and exists only on its branch. The unit did not launch.

`plan_wave.py` now does three new things (kb/Work PB3150):

- `branch_carrier` finds a branch that another DONE branch contains (`git merge-base --is-ancestor`). The finisher
  of a carried SPLIT merges the carrier instead of cherry-picking. A carried FINDINGS branch whose lead the fix lane
  cannot see gets no finisher of its own.
- `branch_lead_notes` reads a FINDINGS report's lead from its own branch when main lacks that note. Such a report now
  gets its finisher in every mode.
- `--drain` plans only the finishers of started work. It plans no cluster and no absorption, and it never starts a
  note that no report started. Each waiting note gets a reason, and the clusters' notes are counted.

On the live reports the drain plan is A = e99-4's review finisher and B = license-product-name's. PB546, PB3030 and
PB3101 wait, carried by e99-4. Self-test `test_plan_wave.py`: 142/142 (section 6b'''). DESIGN-orchestrator-loop.md
section 9 and `units/wave.md` step 2 (a drain adds `--drain`, never `--max-groups` alone) changed in the same change.
