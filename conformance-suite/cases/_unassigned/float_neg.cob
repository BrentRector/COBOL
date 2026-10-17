      *>--- conformance-case
      *> id: float_neg
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLTNEG.
      *> negative + fractional fixed-point literals into a float, and a
      *> float back to PIC 9(4). (Exponent-form float LITERALS — 1.5E3 —
      *> are a separate deferred leg, §8.3.3.3.3; a float item holds a
      *> fixed-point literal directly.)
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A USAGE FLOAT-LONG.
       01 WS-B USAGE FLOAT-LONG.
       01 WS-C PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE -1500 TO WS-A.
           DISPLAY "A=" WS-A.
           MOVE 0.025 TO WS-B.
           DISPLAY "B=" WS-B.
           MOVE 10 TO WS-A.
           COMPUTE WS-C = WS-A.
           DISPLAY "C=" WS-C.
           STOP RUN.
