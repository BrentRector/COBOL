*>--- conformance-case
*> id: pb170-compute-index-data-item
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> THE OTHER ARM of the same rule, pinned where it was silently passing.
      *> ExpressionBinder's private operand-class switch had no Usage.Index arm
      *> although its receiving-side twin ScreenResultant did, so
      *> COMPUTE N = IDX + 1 compiled clean under STRICT and computed the
      *> occurrence number (measured on 9a89fbd1: N=0003). Unifying the screen
      *> over the ONE 8.5.2.1 Table-2 classifier closes it with no extra rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB170N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IDX USAGE INDEX.
       01 N   PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           SET IDX TO 2
           COMPUTE N = IDX + 1
           STOP RUN.
