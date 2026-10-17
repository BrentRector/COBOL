*>--- conformance-case
*> id: pb2722-evaluate-not-any
*> rules: [FMT-14.9.13.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2722. ISO 1989:2023 14.9.13.2 prints the bracketed NOT on five selection-object alternatives
      *> only - the figure note: "The bracketed `NOT` applies only to the first five alternatives, exactly as
      *> stacked" (identifier-2, literal-2, arithmetic-expression-2, boolean-expression-2, range-expression) - so
      *> NOT ANY is source no format admits, and 4.2.2 owes it a compile-time indication ("to indicate violations
      *> of the general formats"): COBOLNET3710, at every edition. Before the fix it compiled clean and bound to the
      *> negation of 14.9.13.4 GR4 a) 1.'s constant true ("If the selection object is the word ANY, the result is
      *> true"), so the arm could never run.
      *>   cite.py --check 14.9.13.2 "where selection-object is" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2722NA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE X
              WHEN NOT ANY DISPLAY "NOT-ANY"
              WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.
