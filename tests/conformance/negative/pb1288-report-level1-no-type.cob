      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1288 - ISO 13.15.3 SR5, second half: "The TYPE clause may be specified only in a level 1 entry and shall be
      *> specified in every level 1 entry."   cite.py: OK  13.15.3 5)  (Syntax rules)
      *> D1 is a level 1 entry with no TYPE clause. The binder used to give every group a detail default, so this compiled and
      *> printed A; the rule has no default, it requires the clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1288REPORTLEVEL1NO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1288REPORTLEVEL1NO.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES.
       01  D1.
           03  LINE 1.
               05  COLUMN 1 PIC X SOURCE WA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
