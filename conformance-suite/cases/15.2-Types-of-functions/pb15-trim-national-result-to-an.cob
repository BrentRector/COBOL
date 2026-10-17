*>--- conformance-case
*> id: pb15-trim-national-result-to-an
*> rules: [GR-15.2-3]
*> source-format: free
*> portable: unverified
*>---
*> PB15: FUNCTION TRIM over a NATIONAL argument-1 returns a NATIONAL result (ISO 15.96.1 result-type table:
*> Alphabetic->Alphanumeric, Alphanumeric->Alphanumeric, National->National), so moving that result to an
*> alphanumeric receiver is invalid (14.9.25.3 SR10, Table 16, National row / AN column = No; FUNCTION
*> DISPLAY-OF is the sanctioned narrowing).
*>
*> TRIM IS THE ONE THAT PROVES THE CATALOG ROW ALONE IS NOT THE FIX. It has a BESPOKE bind path (the
*> LEADING/TRAILING phrase), so it builds its own bound node and never reached the generic result-type
*> resolution - the two-arm dispatch, in its silent form. Pre-fix this compiled clean.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGP15T.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N1 PIC N(6) VALUE N"  AB  ".
01 W-X  PIC X(6).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION TRIM(W-N1) TO W-X.
    STOP RUN.
