      *>--- conformance-case
      *> id: float_rounded
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLTRND.
      *> float->PIC 9(2): default truncation vs ROUNDED (nearest-away).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F USAGE FLOAT-LONG.
       01 WS-T PIC 9(2).
       01 WS-R PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 10.5 TO WS-F.
           COMPUTE WS-T = WS-F.
           DISPLAY "T=" WS-T.
           COMPUTE WS-R ROUNDED = WS-F.
           DISPLAY "R=" WS-R.
           MOVE 2.5 TO WS-F.
           COMPUTE WS-R ROUNDED = WS-F.
           DISPLAY "R2=" WS-R.
           STOP RUN.
