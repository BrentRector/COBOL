*>--- conformance-case
*> id: move_group_overpunch
*> rules: [GR-14.9.25.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> CA28 (CONFORMANCE-FIX-QUEUE): a MOVE with a GROUP receiver is NOT an elementary move (ISO 14.9.25.4 GR4)
      *> — "no conversion of data from one form of internal representation to another"; GR6a's operational-sign
      *> drop applies only to valid elementary moves. So a signed-numeric elementary source keeps its DISPLAY
      *> overpunch image ('-123' -> '12L'). Pre-fix the sign was stripped, giving '123'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA28.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(3) VALUE -123.
       01 G.
          05 B PIC X(3).
       PROCEDURE DIVISION.
           MOVE A TO G.
           DISPLAY B.
           STOP RUN.
