*>--- conformance-case
*> id: algebraic_counter_registers
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R26 - 8.5.2.12 items 3/4/5 make LINAGE-COUNTER, PAGE-COUNTER and LINE-COUNTER
      *> category-numeric DATA ITEMS, so 15.43.3/15.58.3/15.83.3 r1 ADMITS them (they used to draw
      *> COBOLNET1516 "not a data item"). The folds are compile-time constants: LINAGE-COUNTER's
      *> size "is equal to the page size specified in the LINAGE clause" (8.4.3.14.4 GR1) so
      *> HIGHEST = 66 here; the report counters carry no spec size (8.4.3.15.4 GR1) and take the
      *> documented implementor shape PIC 9(18) (CONFORMANCE.md section 3, the counter registers'
      *> declared capacity). All three are UNSIGNED:
      *> LOWEST = 0, SMALLEST = 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R26REGS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "r26-linage.tmp"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT RPT ASSIGN TO "r26-report.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD PRT LINAGE IS 66 LINES.
       01 PRT-REC PIC X(10).
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 R PIC 9(18).
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 20 LINES FIRST DETAIL 1.
       01 D-1 TYPE DETAIL.
          02 LINE 1 COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION HIGHEST-ALGEBRAIC(LINAGE-COUNTER OF PRT)
           DISPLAY R
           COMPUTE R = FUNCTION LOWEST-ALGEBRAIC(LINAGE-COUNTER OF PRT)
           DISPLAY R
           COMPUTE R = FUNCTION SMALLEST-ALGEBRAIC(LINAGE-COUNTER OF PRT)
           DISPLAY R
           COMPUTE R = FUNCTION HIGHEST-ALGEBRAIC(PAGE-COUNTER OF R-1)
           DISPLAY R
           COMPUTE R = FUNCTION LOWEST-ALGEBRAIC(LINE-COUNTER OF R-1)
           DISPLAY R
           COMPUTE R = FUNCTION SMALLEST-ALGEBRAIC(PAGE-COUNTER OF R-1)
           DISPLAY R.
           STOP RUN.
