*>--- conformance-case
*> id: l1-integer-arity
*> rules: [FMT-15.44.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.44.2 — the general format is `FUNCTION INTEGER ( argument-1 )`: two underlined words and ONE
      *> parenthesized argument, with no optional element and no repetition. §15.3 makes that format the
      *> authority on the count — "The definition of a function specifies the number of arguments required,
      *> which may be zero, one, or more" — so the two-argument list below is not a reference to this
      *> function, and §15.44 offers no second format it could be.
      *>
      *> Written at EVERY edition the row spans: §15.44's format is unchanged across 85 / 2002 / 2014 / 2023,
      *> so the rejection is too, and an arity model that lived behind an edition gate would show up here.
      *> The ACCEPT side is conformance:2023/l1_integer_family_notes and conformance:85/l1_integer_floor_85.
      *> Expected: COBOLNET1504, the §15.3 argument-count diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1INTARITY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9V99 VALUE 1.50.
       01 B PIC S9V99 VALUE 2.25.
       01 R PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION INTEGER(A B).
           DISPLAY R.
           STOP RUN.
