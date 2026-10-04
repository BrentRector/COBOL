      *> reject-at: 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.35.3 SR6 c) with 13.18.57.4 GR7 d) 3.: "If the body group is a detail, the upper limit is the line
      *> following the last line of the lowest-level control heading that has an OR PAGE phrase."   cite.py: OK  13.18.35.3 6) c)
      *> and 13.18.57.4 7)  (General rules)
      *> The OR PAGE control heading is printed first on a page, its first line on FIRST DETAIL, 3, and its second on 4; the
      *> detail's upper limit is line 5, and DE LINE 4 lies above it. (OR PAGE is a COBOL 2002 phrase, so the program is refused
      *> for this rule at 2002 and later.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1270DETAILINSIDEOR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1270DETAILINSIDEOR.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 CONTROL IS WN PAGE LIMIT IS 20 LINES FIRST DETAIL 3.
       01  CH1 TYPE CH ON WN OR PAGE.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X VALUE "C".
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X VALUE "C".
       01  D1 TYPE DE LINE 4.
           03  COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
