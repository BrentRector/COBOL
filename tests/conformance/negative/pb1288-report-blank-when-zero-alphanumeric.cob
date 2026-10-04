      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1288 - ISO 13.15.4 GR2: "the USAGE, PICTURE, BLANK WHEN ZERO and JUSTIFIED clauses are the same clauses as
      *> those that are described under the general format for a data description entry and shall obey the syntax rules and
      *> general rules defined for each clause."   cite.py: OK  13.15.4 2)  (General rules)
      *> 13.18.8.3 SR1: "The BLANK WHEN ZERO clause may be specified only for an elementary item described by its picture
      *> character-string as category numeric-edited or as numeric without the picture symbol 'S'."   cite.py: OK  13.18.8.3 1)
      *> The item is PIC X(3), alphanumeric. The data division's subject screen (kb/Work PB507) ran over the data forest only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1288REPORTBLANKWHE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1288REPORTBLANKWHE.rpt".
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
               05  COLUMN 1 PIC X(3) BLANK WHEN ZERO SOURCE WA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
