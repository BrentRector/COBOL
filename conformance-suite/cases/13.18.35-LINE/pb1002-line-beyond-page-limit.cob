*>--- conformance-case
*> id: pb1002-line-beyond-page-limit
*> rules: [SR-13.18.35.3-3]
*> source-format: free
*> portable: unverified
*>---
*> A LINE NUMBER BEYOND THE PAGE LIMIT (kb/Work PB1002). ISO/IEC 1989:2023 §13.18.35.3 SR3: "Neither
*> integer-1 nor integer-2 shall exceed the page limit, or 9999 if the report is not divided into pages."
*> PAGE LIMIT 10 with LINE 20 compiled clean before PB1002, and the engine then printed the line on a page
*> that has no line 20. A rule of every edition: the single-operand LINE clause is COBOL-85 syntax.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1002PL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1002-line-beyond-page-limit.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 10 LINES.
       01  DE-1 TYPE DETAIL LINE 20.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DE-1.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
