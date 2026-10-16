## PB3291 filed: plan_wave --wave accepts a wave number the reports or the ledger already hold

On 2026-10-16 the orchestrator loop planned wave 1098 at 12:10, and the attended Mythos session passed `--wave 1098`
explicitly at 13:00 and 13:09, although `next_wave()` would have returned 1099. The second run overwrote the shared
`groups.json`, `wf-args-w1098.json` and `msg-w1098-*.txt`, replaced the loop's ledger entries, shared one STOP file and
mixed two branches per letter in the review records. PB3291 asks `plan_wave.py` to refuse an explicit `--wave N` that the
reports directory, the dispatch ledger or the scratch directory already hold, with a drift test in `test_plan_wave.py`.
Notes only; no source change.
