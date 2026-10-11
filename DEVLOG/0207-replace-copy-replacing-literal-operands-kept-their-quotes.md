## Entry 207 — 2026-05-28: REPLACE/COPY REPLACING — literal operands kept their quotes (COMPILE_FAIL 6→4)

SM201A/SM202A still failed to parse after the crash guard: a `REPLACE … BY "TRUE "` turned
`MOVE FALSE-DATA-1 TO AREA-1` into `MOVE TRUE TO AREA-1` — the bare reserved word `TRUE`, because
`ReadReplaceOperand` returned the literal's *content* (`TRUE `) with the quotation marks
stripped. In REPLACE / COPY REPLACING the quotation marks are part of the literal token, so the
replacement must yield a quoted literal. Fixed the literal branch to include the surrounding
quotes (`"TRUE "`).

SM COMPILE_FAIL 6→4 (SM201A/SM202A now compile and run → 1 FAIL* each). Guard ALL GREEN
(1000 / 336 / 137). Remaining: SM105A/205A/206A/208A compile fails and a handful of FAIL*.

