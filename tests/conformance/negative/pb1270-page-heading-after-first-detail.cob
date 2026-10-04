      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.39.3 SR6: "Integer-3, integer-4, integer-5, integer-6, integer-7, and integer-1 shall be
      *> greater than zero. Wherever specified, they shall be in ascending order, with equality allowed."
      *> cite.py: OK  13.18.39.3 6)  (Syntax rules)
      *> The HEADING integer 5 is greater than the FIRST DETAIL integer 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1270PAGEHEADINGAFT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1270PAGEHEADINGAFT.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES HEADING 5 FIRST DETAIL 3.
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
