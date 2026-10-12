## Entry 242 — File-I/O wall, part 6: not-open I-O status codes (SQ149A/SQ154A → CLEAN)

First of the runtime-correctness long tail. SQ149A (READ a closed file → expected I-O status 47)
was FAIL*: the read returned status **42**. Root cause was a **pattern bug** across all three file
handlers (`SequentialFileHandler`, `IndexedFileHandler`, `RelativeFileHandler`): every operation's
not-open guard returned `FileStatus.FileNotOpen` ("42"). But ISO/IEC 1989:2023 §9.1.13.7 reserves
**42 for CLOSE/UNLOCK only** ("a CLOSE or UNLOCK statement is attempted for a file connector that is
not in an open mode"). The correct not-open codes are operation-specific:

- **47** — READ or START on a connector not open in the input or I-O mode (§9.1.13.7 item 7).
- **48** — WRITE on a connector not open in the correct mode (item 8).
- **49** — DELETE/REWRITE on a connector not open in the I-O mode (item 9).

A closed file is, by definition, not open in any of those modes, so the same guard that produced
"42" for a closed-file READ should produce "47" (and 48/49 for WRITE/DELETE/REWRITE). Fixed the
not-open guard in every I-O method across the three handlers to return the operation-appropriate
code; `Close()` keeps 42 (correct). While there, split the conflated REWRITE/DELETE guard
`!IsOpen || _currentKey==null` (indexed) / `… || _currentRecord==0` (relative): not-open → 49, but
**open-in-I-O-with-no-prior-successful-read → 43** (§9.1.13.6 — "the last input-output statement …
prior to … DELETE/REWRITE … was not a successfully executed READ statement"), and moved the
wrong-mode check ahead of the position check so a wrong open mode still wins (49 over 43).

The existing FileIO integration tests already covered the *wrong-mode* paths (open-INPUT→WRITE=48,
open-OUTPUT→READ=47, open-INPUT→REWRITE=49) and CLOSE-on-not-open=42 — all unaffected. The bug was
specifically the *not-open-at-all* path, which no test had pinned.

Result: **SQ149A (READ closed → 47) and SQ154A (WRITE closed → 48)** both go CLEAN (0 FAIL*,
"001 OF 001 TESTS WERE EXECUTED SUCCESSFULLY"), deterministic across reruns. Baselined both into
`tests/nist/valid/` + `scripts/guard.sh`. **NIST baselines 181 → 183** (94 NC + 42 IF + 12 SM +
4 IC + **25 SQ** + 5 RL + 1 IX). Full guard ALL GREEN (1000 unit / 348 integration [347+1 skip] /
183 NIST). SQ re-survey: CLEAN 23→25, FAIL* 16, COMPILE_FAIL 10, NO_OUTPUT 31, RUNTIME 3; RL/IX
unchanged by this fix (their remaining tail is parse-form + other runtime bugs). Next in the SQ
runtime tail: SQ106A variable-length WRITE status, SQ128A read-back data integrity (each a distinct
small investigation).

