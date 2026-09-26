      *> reject-at: 85 2002 2014 2023
      *> ISO §13.18.57.3 SR11 — "Following CONTROL HEADING or CONTROL
      *> FOOTING, data-name-1, data-name-2, or FINAL may be omitted only
      *> if the CONTROL clause in the corresponding report description
      *> entry contains exactly one operand." FINAL is itself an operand
      *> of the CONTROL clause (SR10: "Each data-name-1, data-name-2, and
      *> FINAL, if specified, shall be the same as one of the operands of
      *> the CONTROL clause"), so CONTROL IS FINAL CX has TWO operands and
      *> the operand-less TYPE CF shall be refused. With CONTROL IS FINAL
      *> alone, or CONTROL IS CX alone, the same TYPE CF is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TCF2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "l1tcf2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CX PIC X(3) VALUE "AAA".
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1
           CONTROL IS FINAL CX
           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       01 CF-G TYPE CF LINE PLUS 1.
          02 COLUMN 1 PIC X(3) SOURCE IS CX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
