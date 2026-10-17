*>--- conformance-case
*> id: float_88
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLT88.
      *> a level-88 condition with a FRACTIONAL VALUE on a float item
      *> compares by algebraic value (§8.8.4.2.4), not truncated. D16 review.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-H USAGE COMP-2.
          88 IS-HALF VALUE 0.5.
          88 IN-RANGE VALUE 1.5 THRU 2.5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0.5 TO WS-H.
           IF IS-HALF DISPLAY "HALF" ELSE DISPLAY "NOHALF".
           MOVE 2.3 TO WS-H.
           IF IN-RANGE DISPLAY "IN" ELSE DISPLAY "OUT".
           MOVE 1.2 TO WS-H.
           IF IN-RANGE DISPLAY "IN2" ELSE DISPLAY "OUT2".
           STOP RUN.
