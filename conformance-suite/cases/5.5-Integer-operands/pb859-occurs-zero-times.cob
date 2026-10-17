*>--- conformance-case
*> id: pb859-occurs-zero-times
*> rules: [SR-5.5-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §5.5 1): "When the term 'integer-n' (n = 1, 2, ...) is used in a general format and
      *> associated rules, it refers to a fixed-point integer literal that shall be unsigned and nonzero unless
      *> otherwise specified in the associated rules." OCCURS Format 1's lone integer-2 has no rule that
      *> otherwise-specifies (§13.18.38.3 SR16 permits zero only for integer-1 of integer-1 TO integer-2), so a
      *> zero is refused by the one integer-n screen, COBOLNET2386 (kb/Work PB859). It used to be accepted,
      *> lowered, and reported by Roslyn as CS0029 in generated C#. §5.5's rule and the OCCURS format are the
      *> same at every edition, so every edition rejects.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W56IZNEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E               PIC X OCCURS 0 TIMES.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHED"
           STOP RUN.
