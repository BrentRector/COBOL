*>--- conformance-case
*> id: intrinsics_trim
*> rules: [AR-15.3-7, AR-15.96.3-3, RV-15.96.4-1, RV-15.96.4-2, RV-15.96.4-3, FMT-15.96.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.96 TRIM — delete LEADING (rule 1) / TRAILING (rule 2) / both (rule 3) characters matching the
      *> delete set. With no argument-2 the set is a space (rule 3.a, the 2014 form); an explicit single-character
      *> argument-2 (here "0") is the 2023 enhancement (Annex E.3.3 item 31). WS-S = "  HELLO   ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INTRTRIM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S PIC X(10) VALUE "  HELLO   ".
       01 WS-Z PIC X(8)  VALUE "0042".
       01 WS-R PIC X(12).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION TRIM(WS-S) TO WS-R.
           DISPLAY "BOTH=" WS-R.
           MOVE FUNCTION TRIM(WS-S LEADING) TO WS-R.
           DISPLAY "LEAD=" WS-R.
           MOVE FUNCTION TRIM(WS-S TRAILING) TO WS-R.
           DISPLAY "TRAIL=" WS-R.
           MOVE FUNCTION TRIM(WS-Z LEADING "0") TO WS-R.
           DISPLAY "ZERO=" WS-R.
           STOP RUN.
       END PROGRAM INTRTRIM.
