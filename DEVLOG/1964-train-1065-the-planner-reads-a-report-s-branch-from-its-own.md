## Entry 1964 — 2026-10-11 19:43 PDT — Train 1065: the planner reads a report's branch from its own field (PB3077), the first landing through the merge queue

**Why one cluster.** The operator's steer (handoff.last.json, 19:14) names this train exactly: the FIRST landing through
GitHub's merge queue, now that `ruleset.py check` reports `ACTIVE` on WiseOwlSoftware/COBOL:main, carrying only
`plan-report-branch-PB3077` at `bbd6fce05` (report w1064g-PB3077, reviewed CLEAN by
hand-review-plan-report-branch-PB3077-r2 at that head). PB3078 was to join only with a DONE, CLEAN report, and it has
none, so it waits; C2 (PB3028) and F (PB3029) are held for wave 1064. The landing goes through `enqueue.py queue`, never
push-main.sh.

**G — PB3077 (branch `plan-report-branch-PB3077`, head `bbd6fce05`).** `plan_wave.parse_report` read a report's branch
from the first `worktree-…` word of its header. Since PB3027's review stamp (one line under Status, above an O11 header's
`Branch:` line, reviewer label `land-review-<branch>-r1`), that word was the reviewer's label, so the planner got a branch
that does not exist and classified it ABSENT. A reviewed branch then read as landed, and its notes would have been planned
again as fresh work. The branch, worktree and head now come from the header's own fields, with the stamp lines skipped.
`BRANCH_FIELD` is read only at a field position (a line's start, or right after the template's ` · ` separator), so
header prose such as "this branch: every file …" (the real reports w1043x-PB2806 and w1055z-PB2426) no longer matches.
That anchor was the branch review's round-1 finding, fixed on the branch by its review finisher. `test_plan_wave.py`
gains the arms for the stamp above and below the field, no field, and mid-prose `branch:`; `review_findings.py` shares
the one definition of a stamp line (`REVIEW_LINE`); DESIGN-test-build-ci.md §3.17.2 step 2 says where the stamp sits and
what the planner reads. Found planning wave 1064's review finishers. PB3077 flips `open` → `landed`; `closes_rows: []`
(process tooling, no inventory row). No diagnostic code, no golden.

**The train.** `train_apply.py` GREEN (5 paths, 4 as on the branch, 1 merged with earlier changes; citation audits and
`work.py check` PASS). Lease taken at 02:28Z; origin/main was still `dfb0a611d`, so no rebase. Windows gate (leg 1,
Normal priority, run 20261012T022903Z-938a0b) `LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 4,685/11,488,
Unit 32,547/33,448, Characterization 36/36; leg 2 left to the merge group), with `test_plan_wave.py` re-run GREEN in
its audits; Linux gate GREEN on the same head `e10dee0ac` (selftests, unit, characterization, conformance, guard). Arch
oracle IDENTICAL (7,780 cases, baseline fb934cb4a329, `landing_oracle.py` PASS, no re-record). Semgrep: no count rose
(3, 44, 2, 268). GAP unchanged (no row re-verdicted). Nothing ejected, nothing fixed in the train.

