      *> reject-at: 85 2002 2014 2023
      *> GROUP INDICATE OUTSIDE A DETAIL REPORT GROUP (kb/Work PB1245 -- COBOLNET2519).
      *> ISO/IEC 1989:2023 §13.18.28.3 SR1: "The GROUP INDICATE clause may be specified only within a
      *> detail report group description, in an elementary entry that also contains a COLUMN clause and a
      *> SOURCE or VALUE clause."
      *>   cite.py --check 13.18.28.3 "may be specified only within a detail report group description"
      *>     -> OK  §13.18.28.3 1)  (Syntax rule)
      *> The indicated item below is in a CONTROL HEADING group; it compiled and ran exit 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1245ND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB1245ND.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE REPORT IS R.
       WORKING-STORAGE SECTION.
       01  WA PIC X(5) VALUE "AAAAA".
       01  WK PIC X VALUE "1".
       REPORT SECTION.
       RD  R CONTROL IS WK.
       01  HCH TYPE CONTROL HEADING WK LINE PLUS 1.
           03 COLUMN 1 PIC X(5) SOURCE WA GROUP INDICATE.
       01  DA TYPE DETAIL LINE PLUS 1.
           03 COLUMN 1 PIC X(5) SOURCE WA.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT-FILE. INITIATE R.
           GENERATE DA.
           TERMINATE R. CLOSE RPT-FILE. STOP RUN.
