      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.39.4 GR2 c): "No report line will appear higher than this position on the page" (HEADING).
      *> cite.py: OK  13.18.39.4 2) c)  (General rules)
      *> A page heading of LINE PLUS 0, on every page with no report heading before it, starts on HEADING + integer-2 - 1 = 0
      *> (13.18.35.4 GR5 b 2.), above line 1; its upper limit is HEADING (13.18.57.4 GR7 b).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1270PAGEHEADINGRELATIVEZ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1270PAGEHEADINGRELATIVEZ.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WK PIC 9 VALUE 1.
       01  TB.
           05  TE PIC 9 OCCURS 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 10 LINES HEADING 1 FIRST DETAIL 4 FOOTING 8.
       01  G1 TYPE PH LINE PLUS 0.
           03  COLUMN 1 PIC X VALUE 'H'.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE 'A'.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
