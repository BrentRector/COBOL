      *> reject-at: 85
      *> kb/Work PB1509/PB1666 - a report SUM of a 19-digit addend. The
      *> PICTURE clause caps a fixed-point item's digit positions:
      *> "the number of digit positions described by character-string-1
      *> shall range from 1 through 31."
      *>   cite.py: OK  §13.18.40.3 14)  (Syntax rules)
      *> COBOL-85 capped a fixed-point item at 18 digits; the 19-31
      *> digit tier (and with it a sum counter wider than 18 digits
      *> over such an addend) is 2002 and later, so the 19-digit
      *> addend is refused at 85 (COBOLNET0802). The positive twins are
      *> conformance:2002/pb1509_sum_counter_wide_native and
      *> conformance:2014/pb1509_sum_counter_wide_standard_decimal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1509R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1509r.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC 9(19) VALUE 6000000000000000000.
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL.
       01  DE-1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CF-F TYPE CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1  PIC 9(18) SUM WS-A.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
