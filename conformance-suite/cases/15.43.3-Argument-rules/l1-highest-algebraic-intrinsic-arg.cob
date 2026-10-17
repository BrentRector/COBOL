*>--- conformance-case
*> id: l1-highest-algebraic-intrinsic-arg
*> rules: [AR-15.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.43.3 r1 — "... and shall not be an INTEGER FUNCTION or NUMERIC FUNCTION."
      *>
      *> THE EXCLUSION CONJUNCT, written for an INTRINSIC function — the shape the rule's own words name.
      *> conformance:cases/15.43.3-Argument-rules/algebraic-udf-argument already covers a USER-DEFINED function result; this is
      *> the other arm, and the two reach the argument through different bindings, so one of them holding is
      *> no evidence about the other. FUNCTION INTEGER is an integer function by §15.44.1 ("The type of this
      *> function is integer"), so `FUNCTION HIGHEST-ALGEBRAIC(FUNCTION INTEGER(N))` violates r1 outright.
      *>
      *> ⛔ THE INNER CALL IS DELIBERATELY LEGAL ON ITS OWN. N is PIC S9(4)V99, a numeric data item, so
      *> FUNCTION INTEGER(N) satisfies §15.44.3 r1 and the ONLY rule this program breaks is §15.43.3 r1 —
      *> a rejection that came from the inner call would be the right error for the wrong reason.
      *> The exclusion has to be written down separately from the category test because §8.5.2.12 gives
      *> function results a category too, so "category numeric" alone would admit this.
      *> Expected: COBOLNET1516, the algebraic-family argument diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1HAFUNC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4)V99 VALUE 12.75.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R =
               FUNCTION HIGHEST-ALGEBRAIC(FUNCTION INTEGER(N)).
           DISPLAY R.
           STOP RUN.
