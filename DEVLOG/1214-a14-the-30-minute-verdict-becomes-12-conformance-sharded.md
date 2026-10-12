## Entry 1214 — 2026-08-07 22:20 PDT — A14: the 30-minute verdict becomes ~12 — conformance sharded with a population guard that cannot lose a test silently

**The 30 minutes was ONE STEP.** Measured on run 31238417792: build ~1.1 min; the conformance suite ~23-24 min
on a 2-core runner in BOTH slow jobs, plus unit ~5 min behind it on the same critical path. No parallelism
knob was missing — xUnit defaults are on and the suite is CPU-bound — so the levers are processes and cores,
not threads.

**LANDED (A14 options 1+2):** greenfield unit + characterization moved to their own job, and conformance is
SHARDED 3× on both platforms — `matrix` (2020) · `corpus-nist` (823) · **`rest` as an EXCLUSION filter**, so
the next test class added automatically lands in a shard; there is no inclusion list to forget. ⛔ The shape
that makes sharding safe is the **`conformance-population` guard job**: every shard publishes its executed
Total, and the guard re-derives discovery with `--list-tests` and asserts each platform's shards SUM to it
EXACTLY — proven locally before push (2020+823+1419 = 4262 = discovered). A filter typo, a class rename, or a
double-count is a loud mismatch; the failure direction is a false red, never a silently-untested class
(feedback_measure_the_selectors_complement — a shard list without its complement measured is how work hides).
The Windows leg keeps the FULL suite deliberately: the cross-platform net is load-bearing (the BOM break was
Windows-caught), and never cache build outputs across runs — the from-scratch build is what caught it.

Option 3 (larger runners, ~cost-neutral on Linux) stays available to the owner on top of this — the two
compose. Expected verdict wall: ~30 → ~10-14 min.

