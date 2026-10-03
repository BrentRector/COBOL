      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1292 - ISO 13.18.53.3 SR4: "If identifier-1 specifies a report section item, it shall be a report
      *> counter identifier or a sum counter defined in the current report." DET-AMT is a printable item of a detail
      *> group - a report section item that is neither - so naming it as the SOURCE of another item is refused:
      *> COBOLNET2144 (the report-section-name screen both SOURCE operand forms share).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1292N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1292n1.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-K PIC 99 VALUE 7.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 30 LINES.
       01  DET-A TYPE DETAIL LINE PLUS 1.
           02  DET-AMT COLUMN 1 PIC 99 SOURCE WS-K.
           02  COLUMN 5 PIC 99 SOURCE DET-AMT.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE DET-A.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
