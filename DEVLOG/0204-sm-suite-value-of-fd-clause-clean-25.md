## Entry 204 — 2026-05-28: SM suite — VALUE OF FD clause (CLEAN 2→5)

With copybooks resolving, `FD … COPY K1FDA.` expanded into three obsolete FD clauses. LABEL
RECORDS and DATA RECORDS were already supported; `VALUE OF implementor-name IS data-name/literal`
was not, so the parser hit "unexpected VALUE". Added `valueOfClause` to `fileDescriptionClause`
(ISO §13.18 removed feature, semantically inert): `VALUE OF (cobolWord | literal | IS)+`, which
consumes the implementor-defined label-field operands until the next clause keyword or the
period. The NIST `XXXXX###` placeholders inside the clause are harmless — VALUE OF is inert, so
its operands are never resolved.

SM CLEAN 2→5 (SM101A/SM103A/SM203A now clean), COMPILE_FAIL 12→9. Guard ALL GREEN
(1000 / 336 / 137). Remaining SM compile fails cluster into a stray-comma case
(SM102A/104A/204A), an unexpected-END case (SM105A/205A), and a string-literal case (SM208A).

