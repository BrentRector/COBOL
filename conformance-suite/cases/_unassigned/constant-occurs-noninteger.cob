*>--- conformance-case
*> id: constant-occurs-noninteger
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.10.3 SR2 - only an INTEGER constant-name may specify an OCCURS integer position
*> (COBOLNET1547).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC06.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT AS "AB".
01 W-TAB.
   05 T-ENT PIC X OCCURS K.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
