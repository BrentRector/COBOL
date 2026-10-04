      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.35.3 SR6 c) with 13.18.57.4 GR7 e): "The upper limit for a page footing is the line number
      *> obtained by adding 1 to the FOOTING integer."   cite.py: OK  13.18.35.3 6) c) and 13.18.57.4 7)  (General rules)
      *> FOOTING is 12, so the page footing's upper limit is 13; PF LINE 12 lies above it. (13.18.39.4 GR2 g: a page footing
      *> "shall be defined so that it begins after this line".)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1270PAGEFOOTINGABO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1270PAGEFOOTINGABO.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES HEADING 2
           FIRST DETAIL 5 LAST DETAIL 10 FOOTING 12.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "A".
       01  PF1 TYPE PF LINE 12.
           03  COLUMN 1 PIC X VALUE "P".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
