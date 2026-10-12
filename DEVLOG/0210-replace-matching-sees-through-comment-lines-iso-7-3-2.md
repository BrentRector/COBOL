## Entry 210 — 2026-05-29: REPLACE matching sees through comment lines (ISO §7.3.2)

REP-TEST-6 (SM208A) and PST-TEST-007 (SM206A) split a pseudo-text match across *comment lines*:
`MOVE "FAIL" TO` written as `MOVE` / three `*` comment lines / `"FAIL"` / `TO`. Per COBOL-85 a
comment line is not a text word, so REPLACE matching must be transparent to it. `TokenizeTextWords`
now skips a free-form `*>` comment (which fixed-form comment lines and `*D` debug lines normalize
into) to end of line, so the operand's text words match across the comments. SM206A 2→1,
SM208A 2→1 FAIL*. Guard ALL GREEN (1000 / 336 / 137).

