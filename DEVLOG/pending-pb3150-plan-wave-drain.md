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
  of a carried SPLIT merges the carrier instead of cherry-picking. A carried FINDINGS branch still gets its
  finisher, and its pred names the carrier.
- `branch_lead_notes` reads a FINDINGS report's lead from its own branch when main lacks that note. Such a report now
  gets its finisher in every mode.
- `--drain` plans only the finishers of started work. It plans no cluster and no absorption, and it never starts a
  note that no report started. Each waiting note gets a reason, and the clusters' notes are counted.

On the live reports the drain plan is A = e99-4's review finisher and B = license-product-name's. PB546, PB3030 and
PB3101 wait, carried by e99-4. The branch review (round 1) found two holes in the unseen-lead arm, and both are
answered by making it the open notes' rule: the newest report naming the lead, on any branch, decides it, so a review
finisher's SPLIT on its own branch is resumed (R1-1), and a FINDINGS branch another lead's DONE branch merged is
never silently dropped (R1-2). Round 2 found the dispatch hold still read the open notes only, so an unseen lead whose
review finisher was dispatched and had not reported was planned again (R2-1): the hold, the frontier-model hold and
the unlanded branches' notes now all read the planner's leads. Self-test `test_plan_wave.py`: 159/159 (sections 6b'''
and 6c). DESIGN-orchestrator-loop.md
section 9 and `units/wave.md` step 2 (a drain adds `--drain`, never `--max-groups` alone) changed in the same change.
