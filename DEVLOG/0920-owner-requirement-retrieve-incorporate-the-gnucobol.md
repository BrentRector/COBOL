## Entry 920 — 2026-07-19 18:10 PDT — Owner requirement: retrieve + incorporate the GnuCOBOL testsuite (§11 A4 → the execution-grade P14 Step 13)

§11 row A4 upgraded from an analysis campaign to a REQUIREMENT with full execution detail as **P14 Step 13**:
retrieve the GnuCOBOL testsuite (the ~1000+ `syn_*/run_*.at` autotest groups + its NIST runner config) and
incorporate it into our test infrastructure. **The load-bearing licensing posture is encoded in the step:**
GnuCOBOL's testsuite is GPL, this repo is BSL — so retrieval is FETCH-ON-DEMAND into a git-ignored dir
(pinned, checksum-verified tarball via `scripts/fetch-gnucobol-tests.ps1`); NOTHING of theirs is ever
committed. The committed artifacts are ours alone: the fetch script, the `.at` extractor, the per-edition
adapter suite, and the independently-authored classification/expectations ledger
(`tests/external/gnucobol-expectations.json`: ISO-CONFORMING / GNUCOBOL-EXTENSION (excluded — reject is OUR
correct outcome) / IMPLEMENTOR-SPECIFIC / DIVERGENT, each with a §-cited rationale). Divergences get the
review treatment (spec-first adjudication, 2-lens verify) → inventory GAP rows or dispositions. The NIST
module-coverage map is produced in the same campaign. Exit: zero UNCLASSIFIED groups, all divergences
adjudicated, a periodic CI job. If retrieval/licensing proves blocking, the step records the attempt and the
fallback escalates to the owner ("if possible" honored). Remains a ⛔ P15 gate.

