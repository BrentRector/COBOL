      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1283 - SR5 - data-name-2 is the level-01 record itself.
      *>   cite.py --check 13.18.45.3 "Neither data-name-2 nor data-name-3 shall refer to an entry that is described with level-number 1, 66, 77, or 88" -> OK
      *> COBOLNET2740 (renames-entry-rule). The positive twin is conformance/85/pb1283_renames_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1283N0.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC X(2) VALUE "AB".
       66 X1 RENAMES R.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
