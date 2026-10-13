## PB3107 + PB3123: no landing check depends on what else is queued (the oracle commits nothing; a DEVLOG number is held to the tree it was stamped on)

The owner asked on 2026-10-13: "Why are many, many github runs failing? Due to our local testing, we should rarely see
failures." Measured with `gh run list` from 2026-10-12 23:46Z to 2026-10-13 07:06Z: 12 of 39 merge-group runs failed.
Nine failed `ArchOracleDriftTests.TheRecordedBaseline_IsOneWellFormedManifest` (two or three manifests in the tree),
two failed the DEVLOG step (`number 1990 is not above the base's newest entry 1991`, `1994 … 1996`). Both are one
mechanism: **a merge group is built on every group queued ahead of it**, in the order GitHub picks when each pull
request's checks finish, so it tests a tree no branch ever checked, and two landing checks held a branch to something
that only holds when it lands alone. The CI invariant (CLAUDE.md) makes each red a local-process defect too: no local
step modeled the stacked tree.

**Re-probe.** `gh run view 38358556637 --log-failed` (PR 29, which changed no oracle input): `Assert.Single() Failure:
The collection contained 2 items`. `gh run view 38360587401 --log-failed`: `number 1994 is not above the base's newest
entry 1996`; the base held 1996 before PR 30 reached main, so a group's `base_sha` is the head of the group ahead of it.

**PB3123, the DEVLOG number.** The number is allocated at the stamp and GitHub orders the queue later, so "above the
base's newest" cannot hold for a landing queued behind a later stamp. Number order is now STAMP order: `devlog.py check`
holds a new entry above every entry of the tree it was STAMPED on (the first parent of the commit in base..rev that
added its file, renames off: `stamp_floor`, through `range_check`) and unique on the landing's tree (`lint`). A stamped
entry keeps its number for good: `unstamp_overtaken` is deleted and `enqueue.py`'s `_stamp` stamps only a pending
entry. Rejected: serializing auto-merge (a bounced pull request re-enters behind later stamps) and numbering at queue
entry (a pull request's content is fixed first). Self-test 44/44 builds the stacked trees as the queue does, a merge
commit of each branch onto the group ahead, in both orders (the 13-then-12 arm was RED on the old rule with the
measured message), and refuses a twin number across two queued branches and a hand number below its own stamp tree.

**PB3107, the oracle.** Recording the baseline on the merged head at enqueue (the note's candidate) still stacks two
records when two pull requests queue together, so no committed record of one tree can work. A capture is a pure
function of its commit (DESIGN-architecture-review.md §4.1), so the committed manifest was only a cache, and the oracle
now commits nothing: `compare_oracle.py [--base REV]` captures the change's own base commit (default: the merge base
with origin/main) with that commit's `capture_oracle.py` in a detached worktree, keeps it under
`TestResults/arch-oracle/<commit12>/`, and compares case by case. Deleted with the cache: `capture_oracle.py --record`,
the manifest's `inputs` (the MSBuild-closure walk, the row sources' `DataRoots` and `DataInputs`), the landing oracle
script and enqueue.py's exit 7, the finisher's ORACLE stop, the golden lane's re-record step, the recorded manifest and
two `ArchOracleDriftTests` facts. `LandingPipelineDriftTests` retires the landing oracle with the direct-push scripts and
adds `NoTrackedRecord_DescribesOneBranchsTree` (red on the base tree, where the manifest was tracked). The loop no longer
serializes re-records: there are none.

**Sibling sweep.** The witness-loss audit and the `changes` job read base..head, which is the pull request's own change
because `base_sha` is the group ahead's head (the workflow comment that said "every PR in the group" is corrected).
`landing_check.py` reads in-flight branches on purpose, and only locally. The conformance manifests' merge conflicts are
PB3116 (a git conflict, not a check). The rule, recorded in DESIGN-test-build-ci.md §3.17.2 step 4, MANDATORY-PRACTICES
L11 and DESIGN-orchestrator-loop.md §4.7: a landing check's verdict may not depend on what else is queued.

**Docs swept:** MANDATORY-PRACTICES L1, L8, L11, L12; the finisher and golden-lane briefs and `check_practices.py`; the
workstream, land-a-fix and architecture-review skills; `units/enqueue.md`; `land_lane.py`; the cobol-finisher role;
DESIGN-test-build-ci.md §3.17.2, §3.17.4, §3.17.8; DESIGN-architecture-review.md §3 R0, §4.1, §8;
DESIGN-orchestrator-loop.md §4.5, §4.7 and its wiring note; DOC_INDEX; the plan's live-state line.

**Verification.** The new oracle path end to end: `=== ARCH-ORACLE: IDENTICAL — 7799 cases (baseline 3b1261dfe86f, new 5cb2f1a37c2e) ===`. `devlog.py --self-test` 45/45 (the stacked arms, a
retitled entry keeping its stamp's floor), `enqueue.py --self-test` PASS, `check_practices.py` GREEN, `work.py check`
clean; the leg-2 `LandingPipelineDriftTests` and `SelfTestDiscoveryDriftTests` run by name, 14/14. The first gate was
RED on the citations audit (a phantom bare `§3.17.4` in PB3123's note, fixed); the second: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — … Conformance 5,703/11,536 · Unit 32,649/33,474 · Characterization 7/36 cases ran (skipped 0) in 1 of 2 leg(s) … run 20261013T212112Z-029494 ===`.
