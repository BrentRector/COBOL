## Entry 2119 — 2026-10-16 16:12 PDT — Work register: CI SDK-download bounce filed (PB3308); three --help encoding notes merged into PB1546

Two register chores, no compiler change.

PB3308 (new): merge-group run 38974314534 failed only at `Setup .NET 10` (the `aka.ms` SDK download errored, no retry,
no cache), bounced PR #139 out of the queue and PR #149 behind it. The note carries the repro, the six workflow steps
in `.github/workflows/build-and-test.yml`, the fix shape, and the PB1957 row 8 obligation.

PB1546, PB2615, PB2915 (merged): one mechanism, argparse `--help` with non-ASCII text crashing on a cp1252 stdout
because the reconfigure runs after `parse_args()` or never. Verified on main 0c53cc440: `work.py` and
`filter_population.py` still crash, `gen_ledger.py` does not; 107 scripts call `reconfigure`, 11 argparse scripts never
do. PB1546 is the one open note (shared helper plus drift test); PB2615 and PB2915 are retired. Two script comments that
named PB2615 now name PB1546.

