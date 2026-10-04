      *> reject-at: 2002 2014 2023
      *> kb/Work PB1283 - SR6 - both operands within a CONSTANT RECORD.
      *>   cite.py --check 13.18.45.3 "Neither data-name-2 nor data-name-3 shall refer to an entry within a record whose data description entry includes the CONSTANT RECORD clause" -> OK
      *> COBOLNET2740 (renames-entry-rule). The positive twin is conformance/85/pb1283_renames_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1283N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R CONSTANT RECORD.
          05 A PIC X(2) VALUE "AB".
          05 B PIC X(2) VALUE "CD".
       66 X1 RENAMES A THRU B.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
