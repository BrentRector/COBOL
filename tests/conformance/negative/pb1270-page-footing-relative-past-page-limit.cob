      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.39.4 GR2 a): "No report line will appear below this position on any page" (the page limit).
      *> cite.py: OK  13.18.39.4 2) a)  (General rules)
      *> A page footing of LINE PLUS 3 starts on FOOTING + integer-2 = 8 + 3 = 11 (13.18.35.4 GR5 b 4.), below the page limit 10; its
      *> lower limit is the page limit (13.18.57.4 GR8 g).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1270PAGEFOOTINGRELATIVEP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1270PAGEFOOTINGRELATIVEP.rpt".
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
       01  G1 TYPE PF LINE PLUS 3.
           03  COLUMN 1 PIC X VALUE 'F'.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
