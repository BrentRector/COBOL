      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.35.3 SR6 c) with 13.18.57.4 GR8 b): "The lower limit for a report heading that does not appear
      *> on a page by itself is the line preceding the first line of the page heading."   cite.py: OK  13.18.35.3 6) c)  (Syntax
      *> rules)  and  13.18.57.4 8)  (General rules)
      *> The page heading starts on line 3, so a report heading that shares its page with it ends by line 2; RH LINE 4 lies below
      *> that lower limit. (The report heading has no NEXT GROUP NEXT PAGE, so it is not on a page by itself, GR8 a.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1270REPORTHEADINGA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1270REPORTHEADINGA.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES HEADING 2 FIRST DETAIL 8.
       01  RH1 TYPE RH LINE 4.
           03  COLUMN 1 PIC X VALUE "R".
       01  PH1 TYPE PH LINE 3.
           03  COLUMN 1 PIC X VALUE "H".
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
