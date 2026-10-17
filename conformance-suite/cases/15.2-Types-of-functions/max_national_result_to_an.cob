*>--- conformance-case
*> id: max_national_result_to_an
*> rules: [GR-15.2-3]
*> source-format: free
*> portable: unverified
*>---
*> V54 (CONFORMANCE-FIX-QUEUE): FUNCTION MAX/MIN over NATIONAL arguments returns a NATIONAL result (ISO 15.59.1 /
*> 15.63.1 result-type table), so moving that result to an alphanumeric receiver is invalid (14.9.25.3 Table 16,
*> National row / AN column = No; FUNCTION DISPLAY-OF is the sanctioned narrowing). Pre-fix, MAX/MIN hardcoded an
*> Alphanumeric result category, which bypassed this guard.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGV54.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N1 PIC N(3) VALUE N"ABC".
01 W-N2 PIC N(3) VALUE N"XYZ".
01 W-X PIC X(6).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION MAX(W-N1 W-N2) TO W-X.
    STOP RUN.
