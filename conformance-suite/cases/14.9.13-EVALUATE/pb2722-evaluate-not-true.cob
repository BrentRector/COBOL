*>--- conformance-case
*> id: pb2722-evaluate-not-true
*> rules: [FMT-14.9.13.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2722. ISO 1989:2023 14.9.13.2 prints the bracketed NOT on five selection-object alternatives
      *> only - the figure note: "The bracketed `NOT` applies only to the first five alternatives, exactly as
      *> stacked" (identifier-2, literal-2, arithmetic-expression-2, boolean-expression-2, range-expression) - and
      *> prints TRUE as a selection object of its own, with no NOT. The word TRUE is not a condition, so NOT TRUE is
      *> no condition-2 either: source no format admits, owed a compile-time indication by 4.2.2 ("to indicate
      *> violations of the general formats"): COBOLNET3710, at every edition. Before the fix it compiled clean.
      *>   cite.py --check 14.9.13.2 "where selection-object is" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2722NT.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE TRUE
              WHEN NOT TRUE DISPLAY "NOT-TRUE"
              WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.
