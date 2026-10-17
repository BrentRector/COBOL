*>--- conformance-case
*> id: pb59-display-of-group-substitution
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> PB59 / AR-15.26.3-2 - a six-position GROUP as argument-2 must draw the
      *> 15.26.3 r2 LENGTH half (the partial KnownWidth silently skipped groups).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGDG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1    PIC N(1) VALUE N"A".
       01 G6.
          05 GA PIC X(3) VALUE "ABC".
          05 GB PIC X(3) VALUE "DEF".
       01 AR    PIC X(2).
       PROCEDURE DIVISION.
           MOVE FUNCTION DISPLAY-OF(N1, G6) TO AR
           STOP RUN.
