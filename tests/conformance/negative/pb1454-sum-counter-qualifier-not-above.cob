      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1454 - ISO/IEC 1989:2023 section 8.4.2.2.3 SR4: "Each data-name-2 shall be the name associated
      *> with a level number to which the item being qualified is subordinate."
      *>   cite.py: OK  8.4.2.2.3 4)  (Syntax rules)
      *> TOT2 is subordinate to CF1 and to R1, never to RF1, so TOT2 OF RF1 names no sum counter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1454N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1454n.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WX PIC 99 VALUE 7.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  D1 TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC 99 SOURCE WX.
       01  CF1 TYPE CF FINAL.
           02 LINE PLUS 1.
              03 TOT2 COLUMN 5 PIC 99 SUM WX UPON D1.
       01  RF1 TYPE REPORT FOOTING.
           02 LINE PLUS 1.
              03 TOT1 COLUMN 1 PIC 99 SUM WX UPON D1.
       PROCEDURE DIVISION.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
           DISPLAY TOT2 OF RF1.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
