      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1222 - ISO 13.18.35.3 SR6 d): "If the report group consists of one or more absolute lines, not subject to any
      *> PRESENT WHEN clause, and ends in a set of relative lines, or groups of relative lines, they shall not cause the report
      *> group's lower limit to be exceeded unless each of them is subject to a different PRESENT WHEN clause".
      *> cite.py: OK  13.18.35.3 6) d)  (Syntax rules)
      *> LAST DETAIL is 10 (13.18.57.4 GR8 e). LINE 9 is unconditional and absolute; LINE PLUS 2 follows it to line 11, past the
      *> lower limit, and is subject to no PRESENT WHEN clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1222RELATIVETAILPA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1222RELATIVETAILPA.rpt".
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
           03  LINE 9.
               05  COLUMN 1 PIC X VALUE "A".
           03  LINE PLUS 2.
               05  COLUMN 1 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
