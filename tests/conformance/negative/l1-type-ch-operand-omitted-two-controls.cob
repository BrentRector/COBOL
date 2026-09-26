      *> reject-at: 85 2002 2014 2023
      *> ISO §13.18.57.3 SR11 — "Following CONTROL HEADING or CONTROL
      *> FOOTING, data-name-1, data-name-2, or FINAL may be omitted only
      *> if the CONTROL clause in the corresponding report description
      *> entry contains exactly one operand." R-1's CONTROL clause has
      *> TWO operands (CX major, CY minor), so the operand-less TYPE CH
      *> cannot select a control level and shall be refused. Everything
      *> else is conforming: both controls are working-storage items with
      *> no OCCURS (§13.18.16.3 SR2/SR3), and the report has a DETAIL body
      *> group (§13.18.57.3 SR15). Writing TYPE CH CX (or CY) is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TCH2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "l1tch2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CX PIC X(3) VALUE "AAA".
       01 CY PIC X(3) VALUE "BBB".
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1
           CONTROL IS CX CY
           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2.
       01 CH-G TYPE CH LINE PLUS 1.
          02 COLUMN 1 PIC X(3) SOURCE IS CX.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
