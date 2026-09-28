      *> reject-at: 85
      *> kb/Work PB1234 - a floating-point CONTROL operand needs a
      *> floating-point data item, and USAGE FLOAT-LONG (like the rest
      *> of the float trio) is a COBOL-2002 introduction, refused at 85
      *> by its introduction gate (COBOLNET0900). The positive twin is
      *> conformance:2002/pb1234_report_control_float.
      *> "For each data-name-1 an internal data item, known as a prior
      *> control, is implicitly defined, having the same data
      *> description as the corresponding data item"
      *>   cite.py: OK  §13.18.16.4 3)  (General rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1234R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1234r.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  CN      USAGE FLOAT-LONG VALUE 1.
       01  WS-K    PIC 99 VALUE 5.
       REPORT SECTION.
       RD  R-1 CONTROLS ARE FINAL CN.
       01  DE-1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 99 SOURCE WS-K.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
