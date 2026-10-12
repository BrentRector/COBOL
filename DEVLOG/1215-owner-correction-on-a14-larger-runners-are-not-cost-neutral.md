## Entry 1215 — 2026-08-07 22:42 PDT — Owner correction on A14: larger runners are NOT cost-neutral — the baseline is the free tier

Entry 1214 (and the A14 note) claimed larger Linux runners were "roughly cost-neutral" by comparing
per-core-minute RATES. The owner's correction: the standard-runner minutes in use today run inside GitHub's
FREE allocation — the comparison baseline is $0, and larger runners bill from the first minute with no free
tier. Pay-for-speed, full stop. The note is corrected; the sharding (options 1+2, landed in 1214) was the
right implementation choice precisely because it achieves the speedup at the actual current cost of zero.

