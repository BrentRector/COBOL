      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1288 - ISO 13.15.4 GR2 imports the JUSTIFIED clause's rules into the report group entry.
      *> cite.py: OK  13.15.4 2)  (General rules)
      *> 13.18.32.3 SR3: "The JUSTIFIED clause may be specified only for a data item whose category is alphabetic, alphanumeric,
      *> boolean, or national."   cite.py: OK  13.18.32.3 3)  (Syntax rules)
      *> The item is PIC 99, numeric; it compiled and printed 01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1288REPORTJUSTIFIE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1288REPORTJUSTIFIE.rpt".
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
               05  COLUMN 1 PIC 99 JUSTIFIED SOURCE WN.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
