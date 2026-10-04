      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1299 - ISO 13.18.57.3 SR13: "REPORT HEADING, PAGE HEADING, REPORT FOOTING, and PAGE FOOTING may each appear no more
      *> than once in any given report description."   cite.py: OK  13.18.57.3 13)  (Syntax rules)
      *> Two PAGE FOOTING groups: the run-time slot of the type holds one, so the second silently REPLACED the first and its lines were lost.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1299TWOPAGEFOOTINGS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1299TWOPAGEFOOTINGS.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WK PIC 9 VALUE 1.
       01  TB.
           05  TE PIC 9 OCCURS 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 10 LINES HEADING 1 FIRST DETAIL 4 FOOTING 8.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE 'A'.
       01  G1 TYPE PF LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE '1'.
       01  G2 TYPE PF LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE '2'.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
