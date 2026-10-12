## Entry 213 — 2026-05-29: COPY … OF/IN library-name — multi-library resolution (SM207A CLEAN)

SM207A copies the same text-name `ALTLB` from two libraries — `COPY ALTLB OF XXXXX047` (must give
PERFORM PASS) and `COPY ALTLB IN XXXXX048` (must give PERFORM FAIL, which the test reads as "the
*correct* X-48 library"). We had parsed and discarded the library qualifier, so both resolved to
the one flat `ALTLB.cpy` and QUAL-TEST-02 reported "TEXT COPIED FROM WRONG LIBRARY".

Compiler (spec-general, ISO §7.4.2): `ExpandCopyStatements` now captures the OF/IN library-name and
passes it to `FindCopybook`, which resolves a library to a same-named subdirectory of a search
path (`<copylib>/<library>/<text-name>`), falling back to the unqualified default library when the
qualified one has no such member. So one text-name yields different text per library.

Harness: created the CCVS two-library layout the test documents (its own copybook comments and the
`+ALTLB` / `+ALTL1,,,ALTLB` plus-cards): `copylib/XXXXX047/ALTLB.cpy` = ALTLB text (PASS),
`copylib/XXXXX048/ALTLB.cpy` = ALTL1 text (FAIL). SM207A → CLEAN; SM CLEAN 11→12. Guard ALL GREEN
(1000 / 336 / 137). Remaining SM: SM104A (file read), SM105A/SM205A (SORT — overlaps the ST suite).

