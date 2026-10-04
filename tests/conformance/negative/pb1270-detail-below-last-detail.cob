      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.35.3 SR6 c) with 13.18.57.4 GR8 e): "The lower limit for a detail is the line given by the
      *> LAST DETAIL integer."   cite.py: OK  13.18.35.3 6) c) and 13.18.57.4 8) e)  (General rules)
      *> LAST DETAIL is 10; DE LINE 11 lies below it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1270DETAILBELOWLAS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1270DETAILBELOWLAS.rpt".
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
       01  D1 TYPE DE LINE 11.
           03  COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
