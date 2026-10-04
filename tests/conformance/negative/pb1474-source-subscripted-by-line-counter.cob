      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1474 - ISO 8.4.2.3.3 SR8: "In the report section, neither a sum counter nor the LINE-COUNTER and PAGE-COUNTER identifiers may be used as a subscript."
      *> cite.py: OK  8.4.2.3.3 8)  (Syntax rules)
      *> Here: SOURCE TE(LINE-COUNTER).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1474SOURCESUBSCRIPTEDBYL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1474SOURCESUBSCRIPTEDBYL.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WK PIC 9 VALUE 1.
       01  TB.
           05  TE PIC 9 OCCURS 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 99 SOURCE TE(LINE-COUNTER).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
