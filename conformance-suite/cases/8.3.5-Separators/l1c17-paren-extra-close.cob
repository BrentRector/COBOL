      *>--- conformance-case
      *> id: l1c17-paren-extra-close
      *> rules: [SR-8.3.5-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.5 4) — an unmatched right parenthesis
      *> "Except in pseudo-text, parentheses may appear only in
      *>  balanced pairs of left and right parentheses delimiting
      *>  subscripts, a list of function or method arguments, a
      *>  reference modifier, arithmetic or boolean expressions, or
      *>  conditions."
      *> cite.py --check 8.3.5 "Except in pseudo-text, parentheses may
      *>   appear only in balanced pairs of left and right parentheses
      *>   delimiting subscripts, a list of function or method
      *>   arguments, a reference modifier, arithmetic or boolean
      *>   expressions, or conditions." -> OK §8.3.5 4)
      *> The expression closes one parenthesis more than it opens:
      *> not a balanced pair.  Pure-syntax rule, so the generic parse
      *> code is the diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17H2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-A PIC 999.
       01 N-B PIC 999 VALUE 4.
       01 W-S PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N-A = (N-B + 1)).
           STOP RUN.
