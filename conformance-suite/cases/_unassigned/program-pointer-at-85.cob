*>--- conformance-case
*> id: program-pointer-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.60 GR24 - USAGE PROGRAM-POINTER is a COBOL-2002 introduction: at --std 85 the
*> version-conformance pass rejects it (COBOLNET0900, registry row usage-program-pointer-2002).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPP01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 PP USAGE PROGRAM-POINTER.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
