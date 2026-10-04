      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1288 - ISO 13.15.3 SR12: "A PICTURE clause shall be specified in every elementary entry that has a SOURCE or
      *> SUM clause."   cite.py: OK  13.15.3 12)  (Syntax rules)
      *> The last 05 entry is elementary, has a SOURCE clause and no PICTURE. It has no COLUMN clause either (so it prints nothing,
      *> 13.18.53.4 GR3), and SR12 has no COLUMN condition. The binder asked for the PICTURE only inside its printable-item branch.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1288REPORTSOURCENO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1288REPORTSOURCENO.rpt".
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
           03  LINE 1.
               05  COLUMN 1 PIC X SOURCE WA.
               05  SOURCE WB.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
