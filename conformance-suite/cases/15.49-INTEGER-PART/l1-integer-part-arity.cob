*>--- conformance-case
*> id: l1-integer-part-arity
*> rules: [FMT-15.49.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.49.2 — the general format is `FUNCTION INTEGER-PART ( argument-1 )`: two underlined words
      *> and ONE parenthesized argument, with no optional element and no repetition. §15.3 makes the format
      *> the authority on the count — "The definition of a function specifies the number of arguments
      *> required, which may be zero, one, or more" — so the two-argument list below names no function this
      *> standard defines, and §15.49 has no second format.
      *>
      *> ⚠ INTEGER-PART IS THE SHARPEST PLACE TO WRITE THIS, because §15.49.4 r1 defines it by an equivalent
      *> arithmetic expression over THREE other functions — SIGN, INTEGER and ABS — each of which is itself
      *> a one-argument format. A binder that built the EAE by passing an argument list through would take
      *> its arity from whichever inner call it landed on rather than from §15.49.2, and every one-argument
      *> fixture in the corpus would still pass.
      *>
      *> Written at EVERY edition the row spans; §15.49's format is unchanged across 85 / 2002 / 2014 / 2023.
      *> The ACCEPT side is conformance:cases/15.42.3-Argument-rule/l1_integer_family_notes (and, at --std 85, the P-* lines of
      *> conformance:cases/15.44.3-Argument-rule/l1_integer_floor_85).
      *> Expected: COBOLNET1504, the §15.3 argument-count diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IPARITY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9V99 VALUE 1.50.
       01 B PIC S9V99 VALUE 2.25.
       01 R PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION INTEGER-PART(A B).
           DISPLAY R.
           STOP RUN.
