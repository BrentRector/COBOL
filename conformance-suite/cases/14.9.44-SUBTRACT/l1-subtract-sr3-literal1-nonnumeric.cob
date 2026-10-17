      *>--- conformance-case
      *> id: l1-subtract-sr3-literal1-nonnumeric
      *> rules: [SR-14.9.44.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.3 SR3 (FORMATS 1 AND 2) — "Literal-1 and
      *> literal-2 shall be numeric literals."
      *> THE LITERAL-1 ARM.  In format 1 literal-1 is the subtrahend,
      *> and "12" is written as an alphanumeric literal, not a numeric
      *> one, so `SUBTRACT "12" FROM N` violates SR3.  Its digits
      *> being decimal is exactly the trap: SR3 constrains the KIND of
      *> literal written, not what its characters happen to look like,
      *> so a compiler that de-quoted the string and computed 38 would
      *> be accepting source the standard forbids.
      *> The receiver N is PIC 9(4) so SR2's receiving half holds and
      *> cannot be the reason for the rejection.
      *> Its literal-2 sibling is
      *> negative/l1-subtract-sr3-literal2-nonnumeric — SR3 names TWO
      *> literal positions in two different formats, and only fixing
      *> one arm of a two-arm rule is this repository's most
      *> reproducible defect shape.
      *> ⚠ SR3 STILL ADMITS ONE FIGURATIVE IN THIS VERY SLOT, AND
      *> THE LICENCE IS NOT §8.8.1.1.  Format 1's subtrahend slot is
      *> `{identifier-1 | literal-1}` (§14.9.44.2), not
      *> arithmetic-expression-1, so §8.8.1.1 — which says what an
      *> ARITHMETIC EXPRESSION may be built from — does not govern
      *> it.  The clause that does is §8.3.3.6.3 SR1 a): "If the
      *> literal is restricted to a numeric literal, the only
      *> figurative constant permitted is ZERO (ZEROS, ZEROES)
      *> without the ALL phrase."  SR3 restricts literal-1 to a
      *> numeric literal, so `SUBTRACT ZERO FROM N` is legal and
      *> must NOT be caught by whatever screen rejects the line
      *> below, while `SUBTRACT ALL ZEROS FROM N` is outside the
      *> admission SR1 a) grants, the ALL phrase being excluded by
      *> name.  (§8.8.1.1 describes only WHICH SCREEN this compiler
      *> happens to report the rejection through — never the
      *> licence.)
      *> Reject-at names every edition: SR3 carries no edition
      *> condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBSR3A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 50.
       PROCEDURE DIVISION.
       MAIN.
           SUBTRACT "12" FROM N.
           STOP RUN.
