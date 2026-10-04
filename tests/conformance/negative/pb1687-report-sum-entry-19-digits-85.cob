      *> reject-at: 85
      *> kb/Work PB1687 - ISO 13.18.40.3 SR14: "For data items of category numeric, and for fixed-point data items of category numeric-edited, the number
      *> of digit positions described by character-string-1 shall range from 1 through 31."   cite.py: OK  13.18.40.3 14)  (Syntax rules)
      *> 13.15.4 GR2 makes a report group entry's PICTURE the data description entry's clause; COBOL-85 caps the digit positions at 18
      *> (COBOLNET0802), as the data division does.
      *> Here: a SUM entry (the counter's PICTURE).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1687REPORTSUMENTRY19DIGI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1687REPORTSUMENTRY19DIGI.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WK PIC 9 VALUE 1.
       01  TB.
           05  TE PIC 9 OCCURS 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9(19) SUM WK.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
