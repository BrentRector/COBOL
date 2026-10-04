      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1270 - ISO 13.18.39.3 SR6, the page limit (integer-1) is the last of the ascending integers.
      *> cite.py: OK  13.18.39.3 6)  (Syntax rules)
      *> The FOOTING integer 12 is greater than the page limit 10. The runtime ran from an inverted page model.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1270PAGEFOOTINGOVE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1270PAGEFOOTINGOVE.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES FOOTING 12.
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
