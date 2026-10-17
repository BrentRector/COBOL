*>--- conformance-case
*> id: float_literal
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLTLIT.
      *> floating-point numeric literals, exponent form (ISO §8.3.3.3.3):
      *> significand (with decimal point) E [+-] exponent. D16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A USAGE FLOAT-LONG.
       01 WS-B USAGE FLOAT-LONG.
       01 WS-C PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE -1.5E3 TO WS-A.
           DISPLAY "A=" WS-A.
           MOVE 2.5E-2 TO WS-B.
           DISPLAY "B=" WS-B.
           MOVE 1.0E1 TO WS-A.
           COMPUTE WS-C = WS-A.
           DISPLAY "C=" WS-C.
           COMPUTE WS-A = 1.5E2 * 2.
           DISPLAY "M=" WS-A.
           STOP RUN.
