## Entry 2121 — 2026-10-16 16:21 PDT — PB3291 filed: plan_wave takes a wave number the reports, the ledger or a scratch directory already hold

On 2026-10-16 the orchestrator loop planned wave 1098 at 12:12 (three groups, its files at the scratch root), and the
attended Mythos session's planner passed `--wave 1098` explicitly and rendered eight groups into `scratch\w1098` at
13:09, although the reports and the dispatch ledger already held wave 1098. No spec was overwritten, and the STOP files
and review records stayed apart. The damage came from the two records keyed on the number. The dispatch ledger's upsert on
(wave, letter) replaced the loop's 1098A to C entries, so PB3268 (the loop's C, still running) had no dispatch hold and
no declared file set from 13:09 to its report at 13:59. The in-flight file-set reader counts a ledger group answered by
any report with its slug, so the loop's `w1098a` to `c` reports hid the Mythos A to C groups' file sets while they ran.
A second repro at about 15:05 showed the default form has the same hole: once wave 1100's ledger entries were released
and it had written no report, `next_wave()` returned 1100 although `scratch\w1100` still held its specs, and the session
stepped past it by hand. PB3291 asks for one `wave_taken(n)` predicate (reports, ledger, DEVLOG head and the scratch
directory) that `next_wave()` steps past and an explicit `--wave` is refused by, pinned by a drift test in
`scripts/orchestrator/test_plan_wave.py`. Notes only; no source change.

