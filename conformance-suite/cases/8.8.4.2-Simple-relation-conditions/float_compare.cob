*>--- conformance-case
*> id: float_compare
*> rules: [GR-8.8.4.2.1-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLTCMP.
      *> float vs fixed-point (algebraic value, §8.8.4.2.4) and float vs integer.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F USAGE FLOAT-LONG.
       01 WS-X PIC S9(3)V99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 10.5 TO WS-F.
           MOVE 10.50 TO WS-X.
           IF WS-F = WS-X
               DISPLAY "EQ"
           ELSE
               DISPLAY "NE"
           END-IF.
           IF WS-F > 10
               DISPLAY "GT"
           ELSE
               DISPLAY "LE"
           END-IF.
           STOP RUN.
