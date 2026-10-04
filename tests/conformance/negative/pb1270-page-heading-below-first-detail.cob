      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.35.3 SR6 c): "Any absolute report lines shall be defined in such a way that no line appears
      *> above the upper limit or below the lower limit allowed for the report group."   cite.py: OK  13.18.35.3 6) c)
      *> 13.18.57.4 GR8 c): "The lower limit for a page heading is the line number obtained by subtracting 1 from the FIRST DETAIL
      *> integer."   cite.py: OK  13.18.57.4 8) c)  (General rules)
      *> FIRST DETAIL is 5, so the page heading's lower limit is 4; PH LINE 7 lies below it. (13.18.39.4 GR2 d: a page heading
      *> "shall be defined so that it terminates before this line".)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1270PAGEHEADINGBEL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1270PAGEHEADINGBEL.rpt".
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
       01  PH1 TYPE PH LINE 7.
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
