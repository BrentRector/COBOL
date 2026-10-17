*>--- conformance-case
*> id: move-national-to-an
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.25.3 Table 16: MOVE national -> alphanumeric is invalid (National row, AN column = No);
*> FUNCTION DISPLAY-OF (15.26) is the sanctioned narrowing. Enforces the national_data N2A re-baseline.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC N(3).
01 W-X PIC X(3).
PROCEDURE DIVISION.
MAIN.
    MOVE N"ABC" TO W-N.
    MOVE W-N TO W-X.
    STOP RUN.
