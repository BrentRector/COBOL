## Entry 1963 — 2026-10-11 18:07 PDT — Train 1064: the committed merge-commit pair GitHub accepts (PB3024), landed alone to unblock the merge queue

**Why one cluster.** A deliberately BLOCKING one-cluster train (operator steer, owner 16:47 "keep the current order"; the
manifest's `why_alone`): on the owner's 16:27 go, the first `ruleset.py apply` stopped at its first call because GitHub
returned HTTP 422 `invalid_merge_commit_setting_combo` for the committed settings pair, so the merge queue cannot be
switched on until the corrected pair is on main. No ruleset was created, the classic protection stood, `ruleset.py check`
stayed ABSENT, and this train lands through push-main. The other waiting branches (b61-1/A, b61-7/A2, b61-8/C2, b61-6/F)
are held for the next land unit.

**H — PB3024 (branch `ruleset-settings-PB3024`, head `5a9890d2c`, reviewed CLEAN by
land-review-ruleset-settings-PB3024-r2 at that head).** The mechanism: `scripts/orchestrator/ruleset-main.json` paired
`merge_commit_title=MERGE_MESSAGE` with `merge_commit_message=PR_BODY`, which is not one of the three pairs GitHub accepts
(PR_TITLE+PR_BODY, PR_TITLE+BLANK, MERGE_MESSAGE+PR_TITLE), and the 9-arm self-test could not see it because its fake `gh`
accepts anything. The fix: the message becomes PR_TITLE and the title stays MERGE_MESSAGE, because PR_TITLE as the subject
would put `Train <label>:` on the merge commit, which `prune_worktrees.py`'s `train_heads` reads as a train's last commit
(and `queued_train_members` keys open PRs on that title). `ruleset.py` gains `VALID_MERGE_COMMIT_PAIRS` and
`merge_commit_pair_problem()`, and `apply()` refuses an invalid pair before any call reaches GitHub (exit 2, "nothing was
sent"); its self-test gains two arms (11 in all), and `LandingPipelineDriftTests.TheQueueAndTheRuleset_DeclareEveryArm`
pins both pair-guard arms. `DESIGN-test-build-ci.md` §3.17.6 no longer promises the forensic message on the merge commit:
the merge commit carries GitHub's `Merge pull request #N from …` subject with the PR's one-line title, and the forensic
record of a landing is the branch's own commits beneath it and the PR body. The branch review (r1) found two things, both
fixed on the branch by its review finisher (`5a9890d2c`): the note's "What changes" item 1 now says what the merge commit
carries, and the drift test pins the two new arms. PB3024 stays `status: owner` (its phase 2, applying the ruleset, is
the owner's go already given: the Mythos session re-runs `ruleset.py apply` after this lands); `closes_rows: []`, process
tooling, no inventory row. No diagnostic code, no golden.

**The train.** `train_apply.py` GREEN (5 paths, 3 as on the branch, 2 merged with earlier changes; citation audits and
`work.py check` PASS). Lease taken at 00:56Z; origin/main was still `01f392184`, so no rebase. Windows gate (leg 1, Normal
priority, run 20261012T005645Z-27c32a) `LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 4,950/11,488, Unit
32,534/33,448, Characterization 36/36; leg 2 left to CI), with `TheQueueAndTheRuleset_DeclareEveryArm` Passed; Linux gate
GREEN on the same head `35d159f50` (selftests, unit, characterization, conformance, guard). Arch oracle IDENTICAL (7,780
cases, baseline fb934cb4a329, whose inputs are unchanged: `landing_oracle.py` PASS, no re-record). Semgrep: no count rose
(3, 44, 2, 268). GAP unchanged (no row re-verdicted). Nothing ejected, nothing fixed in the train.

