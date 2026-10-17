*>--- conformance-case
*> id: ec_bound_overflow
*> rules: [GR-8.5.1.9.6-1, GR-13.18.38.4-17]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> EC-BOUND-OVERFLOW (ISO §8.5.1.9.6 GR1; Table 13 Nonfatal): a dynamic-capacity
      *> table (OCCURS DYNAMIC, §8.5.1.9 — COBOL-2014) whose IMPLICIT growth (a receiving
      *> subscript) first crosses its expected capacity (TO integer-5) sets the nonfatal
      *> EC-BOUND-OVERFLOW under CHECKING ON. GR1's "if the change in capacity was implicit
      *> and the expected capacity had already been exceeded before the operation, no
      *> exception shall exist" makes it the FIRST crossing only. Observed via
      *> FUNCTION EXCEPTION-STATUS (§15.33); SET LAST EXCEPTION TO OFF clears it (§14.9.39
      *> Format 13). The growth proceeds regardless (nonfatal), so with checking OFF the
      *> observable result is identical.
      >>TURN EC-BOUND-OVERFLOW CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-BOUND-OVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 4.
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> grow 2 -> 3: 3 not past expected 4 -> no EC.
           MOVE 11 TO WS-E (3).
           DISPLAY "S1=" FUNCTION EXCEPTION-STATUS.
      *> grow 3 -> 5: first crossing of expected 4 -> EC-BOUND-OVERFLOW (nonfatal).
           MOVE 22 TO WS-E (5).
           DISPLAY "S2=" FUNCTION EXCEPTION-STATUS.
           SET LAST EXCEPTION TO OFF.
      *> grow 5 -> 7: already exceeded before an implicit change -> GR1 no exception.
           MOVE 33 TO WS-E (7).
           DISPLAY "S3=" FUNCTION EXCEPTION-STATUS.
           STOP RUN.
