*>--- conformance-case
*> id: pb1616_define_zero_length_literal
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1616 - the zero-length literal in a compiler-directive
      *>   operand, at the edition that introduced it (COBOL-2014).
      *> ISO 8.3.3.1: "If the opening and closing delimiters are
      *>   contiguous, the length of the literal is zero, and it is
      *>   known as a zero-length literal."
      *>   cite.py --check 8.3.3.1 "the length of the literal is zero,
      *>     and it is known as a zero-length literal" -> OK
      *> ISO 7.3.11.4 GR8: "If literal-1 is specified, compilation-
      *>   variable-name-1 references literal-1."
      *>   cite.py --check 7.3.11.4 "If literal-1 is specified,
      *>     compilation-variable-name-1 references literal-1." -> OK
      *> ISO 7.3.8.3 GR2: a non-numeric comparison is by character; "If
      *>   the literals are of unequal length they are not equal."
      *>   cite.py --check 7.3.8.3 "If the literals are of unequal
      *>     length they are not equal." -> OK
      *> Derivation of each expected line. ZL references the
      *>   zero-length literal, so:
      *>   ZL-EMPTY  ZL = "" - both of length zero, no character
      *>             differs, so they are equal.
      *>   ZL-NE-A   ZL = NE, NE referencing "A" - unequal lengths
      *>             (0 and 1), so not equal.
      *> A wrong binding prints the -WRONG line instead. The same
      *>   source below COBOL-2014 is rejected: see
      *>   negative/pb1616-define-zero-length-below-2014.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1616ZL.
       PROCEDURE DIVISION.
       MAIN-P.
       >>DEFINE ZL AS ""
       >>DEFINE NE AS "A"
       >>IF ZL = ""
           DISPLAY "ZL-EMPTY".
       >>ELSE
           DISPLAY "ZL-EMPTY-WRONG".
       >>END-IF
       >>IF ZL = NE
           DISPLAY "ZL-NE-A-WRONG".
       >>ELSE
           DISPLAY "ZL-NE-A".
       >>END-IF
           STOP RUN.
