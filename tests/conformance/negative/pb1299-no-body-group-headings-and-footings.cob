      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1299 - ISO 13.18.57.3 SR15: "Each report description shall include at least one body group."   cite.py: OK  13.18.57.3 15)
      *> A report of REPORT HEADING, PAGE HEADING and PAGE FOOTING has no body group.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1299NOBODYGROUPHEADINGSA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1299NOBODYGROUPHEADINGSA.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WK PIC 9 VALUE 1.
       01  TB.
           05  TE PIC 9 OCCURS 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 10 LINES HEADING 1 FIRST DETAIL 4 FOOTING 8.
       01  G1 TYPE RH LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE '1'.
       01  G2 TYPE PH LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE '2'.
       01  G3 TYPE PF LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE '3'.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           CONTINUE.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
