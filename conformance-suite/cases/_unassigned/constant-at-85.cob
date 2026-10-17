*>--- conformance-case
*> id: constant-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.10 - the constant entry is a COBOL-2002 introduction: at --std 85 the
*> version-conformance pass rejects it (COBOLNET0900, registry row constant-entry-2002).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT AS 42.
01 W PIC 99.
PROCEDURE DIVISION.
MAIN.
    MOVE K TO W.
    DISPLAY W.
    STOP RUN.
