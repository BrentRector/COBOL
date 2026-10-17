*>--- conformance-case
*> id: declined-validate-destination
*> rules: [SR-13.18.18.3-1, SR-13.18.18.3-2, SR-13.18.18.3-3,
*>    GR-13.18.18.4-1, GR-13.18.18.4-2, GR-13.18.18.4-3,
*>    GR-13.18.18.4-4, GR-13.18.18.4-5, GR-13.18.18.4-6, FMT-13.18.18.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.18 DESTINATION clause - Annex A.4.14 item 3. MEASURED before COBOLNET1708, at every edition:
*> "COBOL0001: no viable alternative at input 'DESTINATION'" - a wholly generic error.
*> (The analysis that proposed this row predicted COBOLNET0901 "'DESTINATION' is a reserved word" here; the
*> measurement refutes it. The 0901 belongs to the NEIGHBOURING name slot - "01 DESTINATION PIC X." - where
*> it is CORRECT, and conformance:cases/_unassigned/declined-validate-entry-name-still-0901 pins that it still fires.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLDEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B PIC X(4).
       01 WS-REC.
          05 WS-A PIC X(4) DESTINATION IS WS-B.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
