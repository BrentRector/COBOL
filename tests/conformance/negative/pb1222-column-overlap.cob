      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1222 - ISO 13.18.14.3 SR8 a): "If any two or more items overlap each other, they shall each be subject to a
      *> different PRESENT WHEN clause."   cite.py: OK  13.18.14.3 8) a)  (Syntax rules)
      *> The first item occupies columns 1-5, the second 3-4; they are in increasing order (SR7 is satisfied) and overlap.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1222COLUMNOVERLAP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1222COLUMNOVERLAP.rpt".
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
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(5) VALUE "AAAAA".
           03  COLUMN 3 PIC X(2) VALUE "BB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
