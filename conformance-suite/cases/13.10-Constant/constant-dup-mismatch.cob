*>--- conformance-case
*> id: constant-dup-mismatch
*> rules: [SR-13.10.3-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.10.3 SR9 - a duplicated constant-name shall carry the SAME specification as the
*> prior entry (COBOLNET1547).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC07.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT AS 42.
01 K CONSTANT AS 43.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
