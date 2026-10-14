## Enqueue leads of 2026-10-14: PB3139 and PB3140 filed; PB2922, PB3084 and PB3126 extended

Register-only change; no code. PB3139 (new, kind analysis) records the unprobed Tier-C arm of
`OoConformance.DescriptionMismatch` for a strong variable-length group with a pointer leaf, carried from PB3129's
review. Reachability needs a probe on a pinned build, so it is an analysis note with the code site and the to-do.

PB3140 (new) records that every `actions/upload-artifact` step is `if: always()` with no `continue-on-error`, so an
`ETIMEDOUT` in the artifact service failed a job whose 5338 tests passed and bounced PR #39 from the merge queue.

PB2922 gains a second arm: the same TEMP-wide steer-file count fails every later gate when a killed dry run leaks its
file (`orchestrate.ps1:500` writes it, only the `finally` at `:701` removes it). PB3084 gains a second occurrence: the
prose "merged the reviewed branch <name>" in a report header made `prune_worktrees.BRANCH_FIELD` give a predecessor
the successor's report. PB3126 gains a third arm: the harness refusing the isolation worktree (7 of 26 spawns of the
wave that ended 2026-10-14). Not filed: the COBOLNET0902 diagnostic without a file:line, which needs a compiler
judgment on whether it is a defect.
