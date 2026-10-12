## Entry 239 — File-I/O wall, part 3: 23 SQ (sequential) tests baselined

With the parse wall (237) and the over-strict FILE STATUS / REWRITE checks (238) fixed, a full SQ
survey now reads: **CLEAN=23, FAIL*=18, COMPILE_FAIL=10, NO_OUTPUT=31, RUNTIME=3** (was essentially
all COMPILE_FAIL at the start of the wall). So the sequential file-I/O *runtime* already works for a
large slice — 23 tests compile, run, and produce a CCVS report with 0 FAIL*.

Baselined those 23 (SQ101M SQ102A SQ104A SQ108A SQ111A SQ112A SQ113A SQ117A SQ126A SQ127A SQ131A
SQ143A SQ146A SQ150A SQ155A SQ202A SQ204A SQ207M SQ211A SQ213A SQ217A SQ230A SQ302M) into
`tests/nist/valid/` and `scripts/guard.sh`. The guard re-runs each and confirms a deterministic
MATCH, so these are now locked. **NIST baselines 152 → 175** (94 NC + 42 IF + 12 SM + 4 IC + 23 SQ).
Full guard ALL GREEN.

Remaining SQ: 10 COMPILE_FAIL (parse forms — LINAGE-COUNTER special register, FD `RECORD …
CHARACTERS`, `RECORD DELIMITER` clause, +2), 18 FAIL* and 3 RUNTIME (sequential-I/O runtime
correctness tail), 31 NO_OUTPUT (callee-only / no-report). IX/RL/ST not yet surveyed under the new
FILE-CONTROL grammar — they should benefit from the same parse fixes.

