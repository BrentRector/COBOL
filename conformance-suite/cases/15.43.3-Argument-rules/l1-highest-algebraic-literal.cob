*>--- conformance-case
*> id: l1-highest-algebraic-literal
*> rules: [AR-15.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.43.3 r1 — "Argument-1 shall be a DATA ITEM of category numeric or numeric-edited ..."
      *>
      *> THE DATA-ITEM CONJUNCT. A numeric literal is of category numeric (§8.3.3, §8.5.2.1's closing "The
      *> class and category of a literal are defined in 8.3.3"), so a screen written on CATEGORY ALONE lets
      *> it through — and then §15.43.4 r2 has nothing to compute: "the positive algebraic value of greatest
      *> finite magnitude that may be represented in ARGUMENT-1" is a property of a data description entry,
      *> and a literal has none. Folding 999 to 999 would look plausible and be meaningless, which is the
      *> failure mode this fixture exists to make loud.
      *>
      *> The sibling conjuncts: category, by conformance:cases/15.43.3-Argument-rules/l1-highest-algebraic-category; the function
      *> exclusion, by conformance:cases/15.43.3-Argument-rules/l1-highest-algebraic-intrinsic-arg and
      *> conformance:cases/15.43.3-Argument-rules/algebraic-udf-argument.
      *> Expected: COBOLNET1516, the algebraic-family argument diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1HALIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION HIGHEST-ALGEBRAIC(999).
           DISPLAY R.
           STOP RUN.
