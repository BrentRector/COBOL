      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1283 - SR11 - data-name-3 (A) is subordinate to data-name-2 (G): its end does not follow G's end.
      *>   cite.py --check 13.18.45.3 "The beginning of the storage area described by data-name-3 shall not precede the beginning of the storage area described by data-name-2" -> OK
      *> COBOLNET2740 (renames-entry-rule). The positive twin is conformance/85/pb1283_renames_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1283N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 G.
             10 A PIC X(2) VALUE "AB".
             10 B PIC X(2) VALUE "CD".
          05 C PIC X(2) VALUE "EF".
       66 X1 RENAMES G THRU A.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
