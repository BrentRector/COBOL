*>--- conformance-case
*> id: pb983-range-trailing-word-not-alphabet
*> rules: [FMT-14.9.13.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13.2: IN is an optional word in the range-expression's `[ IN alphabet-name-1 ]` (not
      *> underlined; 5.2.3), so a bare word after a THROUGH range's right-hand operand is in the
      *> alphabet-name-1 position. NO-SUCH-ALPHA declares no alphabet (12.3.7), so 14.9.13.3 SR3's
      *> alphabet-name-1 names nothing and 14.7.8 rule 2's "collating sequence defined by that alphabet" has
      *> nothing to resolve to - refused, never read as the native order (kb/Work PB983).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB983NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC X VALUE "C".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-C
               WHEN "A" THRU "M" NO-SUCH-ALPHA DISPLAY "IN"
               WHEN OTHER                      DISPLAY "OUT"
           END-EVALUATE
           STOP RUN.
