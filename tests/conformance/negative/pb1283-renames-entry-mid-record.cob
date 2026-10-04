      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1283 - SR2 - a data description entry follows the record's RENAMES entries.
      *>   cite.py --check 13.18.45.3 "All RENAMES entries referring to data items within a given record shall immediately follow the last data description entry" -> OK
      *> COBOLNET2740 (renames-entry-rule). The positive twin is conformance/85/pb1283_renames_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1283N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC X(2) VALUE "AB".
       66 X1 RENAMES A.
          05 B PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
