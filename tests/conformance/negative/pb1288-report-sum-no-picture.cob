      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1288 - ISO 13.15.3 SR12: "A PICTURE clause shall be specified in every elementary entry that has a SOURCE or
      *> SUM clause."   cite.py: OK  13.15.3 12)  (Syntax rules)
      *> S1 is an elementary entry with a SUM clause and no PICTURE. 13.18.54.4 GR1 sizes the sum counter from that PICTURE, so the
      *> compiler created a counter whose description the standard requires the programmer to supply.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1288REPORTSUMNOPIC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1288REPORTSUMNOPIC.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 CONTROL FINAL PAGE LIMIT IS 20 LINES.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 SOURCE WN.
       01  TYPE CF FINAL.
           03  LINE PLUS 1.
           05  S1 SUM WN.
           05  COLUMN 1 PIC X VALUE "T".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
