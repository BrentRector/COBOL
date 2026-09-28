      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1284 — ISO/IEC 1989:2023 §13.18.45.3 SR8: "None of the items within the range, including data-name-2
      *> and data-name-3, if specified, shall be ... an occurs-depending table." T, subordinate to G, is within the
      *> range of both entries (GR1: G and its subordinates; GR2: A through the last item of G). Before the fix
      *> X1 displayed blank and X2 displayed 'AB   ' — a fixed window over storage whose extent is not fixed.
      *> (No edition change to SR8 is recorded in docs/VERSION_CHANGE_REFERENCE.md, so it holds at every edition.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1284RO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 R.
          05 A PIC X(2) VALUE "AB".
          05 G.
             10 T PIC X OCCURS 1 TO 3 DEPENDING ON N.
       66 X1 RENAMES G.
       66 X2 RENAMES A THRU G.
       PROCEDURE DIVISION.
           DISPLAY "[" X1 "][" X2 "]".
           STOP RUN.
