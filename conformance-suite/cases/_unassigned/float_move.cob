      *>--- conformance-case
      *> id: float_move
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLTMOV.
      *> literal->single, single->double (exact widen), double->single.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S USAGE FLOAT-SHORT.
       01 WS-D USAGE FLOAT-LONG.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1.5 TO WS-S.
           DISPLAY "S=" WS-S.
           MOVE WS-S TO WS-D.
           DISPLAY "D=" WS-D.
           MOVE 2.25 TO WS-D.
           DISPLAY "D2=" WS-D.
           MOVE WS-D TO WS-S.
           DISPLAY "S2=" WS-S.
           STOP RUN.
