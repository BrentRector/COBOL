      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1283 - SR3 - data-name-2 is subject to its ancestor's OCCURS clause.
      *>   cite.py --check 13.18.45.3 "Data-name-1, data-name-2 and data-name-3 shall not be subject to any OCCURS clauses" -> OK
      *> COBOLNET2740 (renames-entry-rule). The positive twin is conformance/85/pb1283_renames_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1283N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 T OCCURS 2.
             10 A PIC X VALUE "A".
       66 X1 RENAMES A.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
