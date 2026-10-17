*>--- conformance-case
*> id: reverse_national_result_to_an
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> CA25 (CONFORMANCE-FIX-QUEUE): FUNCTION UPPER-CASE/LOWER-CASE/REVERSE over a NATIONAL argument returns a NATIONAL
*> result (ISO 15.97.1 / 15.57.1 / 15.78.1 result-type tables), so moving that result to an alphanumeric receiver is
*> invalid (14.9.25.3 Table 16, National row / AN column = No; FUNCTION DISPLAY-OF is the sanctioned narrowing).
*> Pre-fix the catalog hardcoded an Alphanumeric result category, bypassing this guard.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCA25.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N1 PIC N(3) VALUE N"ABC".
01 W-X  PIC X(6).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION REVERSE(W-N1) TO W-X.
    STOP RUN.
