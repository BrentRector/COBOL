      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1222 - ISO 13.18.35.3 SR6 e): "If the description of any absolute line appears later than that of a relative
      *> line, they shall each be subject to a different PRESENT WHEN clause."   cite.py: OK  13.18.35.3 6) e)  (Syntax rules)
      *> LINE 8 is described after LINE PLUS 1, and neither is subject to a PRESENT WHEN clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1222ABSOLUTELINEAF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1222ABSOLUTELINEAF.rpt".
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
       01  D1 TYPE DE.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X VALUE "A".
           03  LINE 8.
               05  COLUMN 1 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
