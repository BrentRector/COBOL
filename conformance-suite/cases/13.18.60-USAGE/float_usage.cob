*>--- conformance-case
*> id: float_usage
*> rules: [GR-13.18.60.4-13]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18 — standard floating-point USAGE FLOAT-SHORT (IEEE-754 single = COMP-1), FLOAT-LONG
      *> (IEEE-754 double = COMP-2), and FLOAT-EXTENDED (mapped to double — .NET has no native quad).
      *> 10.5*2 = 21, 100.25*4 = 401, 250.5*2 = 501.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S USAGE FLOAT-SHORT.
       01 WS-L FLOAT-LONG.
       01 WS-X USAGE FLOAT-EXTENDED.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 10.5 TO WS-S.
           COMPUTE WS-R = WS-S * 2.
           DISPLAY "S=" WS-R.
           MOVE 100.25 TO WS-L.
           COMPUTE WS-R = WS-L * 4.
           DISPLAY "L=" WS-R.
           MOVE 250.5 TO WS-X.
           COMPUTE WS-R = WS-X * 2.
           DISPLAY "X=" WS-R.
           STOP RUN.
