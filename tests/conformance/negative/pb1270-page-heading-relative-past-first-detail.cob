      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.39.4 GR2 d): "Any report heading (when not on a page by itself) or page heading shall be
      *> defined so that it terminates before this line [the FIRST DETAIL integer]."   cite.py: OK  13.18.39.4 2) d)  (General rules)
      *> A page heading of three relative lines starts on HEADING + integer-2 - 1, line 1 (13.18.35.4 GR5 b 2.) and ends on line 3,
      *> which is the FIRST DETAIL line itself; its lower limit is FIRST DETAIL - 1 = 2 (13.18.57.4 GR8 c). The three lines
      *> print on lines 1-3 and the first detail then overprints line 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1270PAGEHEADINGREL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1270PAGEHEADINGREL.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3.
       01  PH1 TYPE PH.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X VALUE "H".
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X VALUE "H".
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X VALUE "H".
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
