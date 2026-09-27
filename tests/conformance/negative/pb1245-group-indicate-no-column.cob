      *> reject-at: 85 2002 2014 2023
      *> GROUP INDICATE ON AN ENTRY WITH NO COLUMN CLAUSE (kb/Work PB1245 -- COBOLNET2519).
      *> ISO/IEC 1989:2023 §13.18.28.3 SR1: "The GROUP INDICATE clause may be specified only within a
      *> detail report group description, in an elementary entry that also contains a COLUMN clause and a
      *> SOURCE or VALUE clause."
      *>   cite.py --check 13.18.28.3 "in an elementary entry that also contains a COLUMN clause and a
      *>     SOURCE or VALUE clause"  -> OK  §13.18.28.3 1)  (Syntax rule)
      *> WX has a SOURCE clause but no COLUMN clause, so it is not a printable item; it compiled and ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1245NC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB1245NC.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE REPORT IS R.
       WORKING-STORAGE SECTION.
       01  WA PIC X(5) VALUE "AAAAA".
       01  WB PIC X(5) VALUE "BBBBB".
       REPORT SECTION.
       RD  R.
       01  DA TYPE DETAIL LINE PLUS 1.
           03 WX PIC X(5) SOURCE WA GROUP INDICATE.
           03 COLUMN 10 PIC X(5) SOURCE WB.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT-FILE. INITIATE R.
           GENERATE DA.
           TERMINATE R. CLOSE RPT-FILE. STOP RUN.
