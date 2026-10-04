      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1222 - ISO 13.18.14.3 SR8 b): "The rightmost column positions of all absolute items shall not exceed the page
      *> width."   cite.py: OK  13.18.14.3 8) b)  (Syntax rules)
      *> The page width is 12 and the item at COLUMN 10 is five wide, so its rightmost column is 14. (SR6, COBOLNET2710, bounds the
      *> OPERAND, 10; SR8 b) bounds the item's end.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1222COLUMNPASTPAGE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1222COLUMNPASTPAGE.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES 12 COLUMNS.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 10 PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
