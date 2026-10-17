*>--- conformance-case
*> id: pb2722-evaluate-not-false
*> rules: [FMT-14.9.13.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2722. The FALSE twin of pb2722-evaluate-not-true. ISO 1989:2023 14.9.13.2 prints the bracketed
      *> NOT on five selection-object alternatives only - the figure note: "The bracketed `NOT` applies only to the
      *> first five alternatives, exactly as stacked" - and prints FALSE as a selection object of its own, with no
      *> NOT; the object here faces a CONDITION subject (Table 15 admits TRUE or FALSE there), so only the NOT is
      *> wrong: COBOLNET3710 at every edition, where the source used to compile clean.
      *>   cite.py --check 14.9.13.2 "where selection-object is" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2722NF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE X > 2
              WHEN NOT FALSE DISPLAY "NOT-FALSE"
              WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.
