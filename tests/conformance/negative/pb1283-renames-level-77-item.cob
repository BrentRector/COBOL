      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1283 - SR5 (and SR2) - a RENAMES of a level-77 item, which is no record description entry.
      *>   cite.py --check 13.18.45.3 "Neither data-name-2 nor data-name-3 shall refer to an entry that is described with level-number 1, 66, 77, or 88" -> OK
      *> COBOLNET2740 (renames-entry-rule). The positive twin is conformance/85/pb1283_renames_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1283N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 Z PIC X(2) VALUE "ZZ".
       66 X1 RENAMES Z.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
