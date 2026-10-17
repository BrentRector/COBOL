*>--- conformance-case
*> id: constant-figurative
*> rules: [SR-13.10.3-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.10.3 SR6 - literal-1 of a constant entry shall not be a figurative constant
*> (COBOLNET1547).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC05.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT AS SPACE.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
