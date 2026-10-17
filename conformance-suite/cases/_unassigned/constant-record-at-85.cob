*>--- conformance-case
*> id: constant-record-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.15 - the CONSTANT RECORD clause is a COBOL-2002 introduction: at --std 85 the
*> version-conformance pass rejects it (COBOLNET0900, registry row constant-record-2002).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CFG CONSTANT RECORD.
   05 CFG-TAG PIC X(4) VALUE "COBL".
PROCEDURE DIVISION.
MAIN.
    DISPLAY CFG-TAG.
    STOP RUN.
