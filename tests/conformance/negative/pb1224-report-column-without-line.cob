      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1224 - ISO 13.15.3 SR9: "Every elementary entry with a COLUMN clause but no LINE clause shall be subordinate
      *> to an entry with a LINE clause."   cite.py: OK  13.15.3 9)  (Syntax rules)
      *> No entry of the group has a LINE clause. This case was refused before, but on the shared COBOLNET0899 "recognized but not
      *> implemented" code citing 13.18.14; it is now the rule's own COBOLNET2247, and had no witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1224REPORTCOLUMNWI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1224REPORTCOLUMNWI.rpt".
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
       01  D1 TYPE DE.
           03  COLUMN 10 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
