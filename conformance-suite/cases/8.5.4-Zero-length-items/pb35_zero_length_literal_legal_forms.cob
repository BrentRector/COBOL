*>--- conformance-case
*> id: pb35_zero_length_literal_legal_forms
*> rules: [GR-8.5.4-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.59.3 r3 / 15.63.3 r3 / 15.71.3 r2 / 15.72.3 r2 bar a ZERO-LENGTH
      *> LITERAL from the MAX/MIN/ORD-MAX/ORD-MIN argument list (fix-queue PB35).
      *> The rejection is pinned by the negative fixture
      *> pb35-zero-length-literal-max-argument.
      *>
      *> THIS FILE IS THE OTHER HALF, AND IT IS THE ONE THAT CONSTRAINS THE FIX.
      *> A screen for "zero-length" written on the operand's WIDTH rather than on
      *> its being a LITERAL would reject most of what follows, and every one of
      *> these is conforming source. PB1 is the standing proof that this is the
      *> expensive direction to get wrong: an argument screen enforced from an
      *> unverified table turned away 12 legal corpus programs.
      *>
      *> Each clause says "zero-length LITERAL" in those words, so the test is on
      *> the literal, never on a computed or item width.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB35LEGALZL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC X(6).
       01 N  PIC 9(4).
       01 A  PIC X(2) VALUE "AB".
       01 B  PIC X(2) VALUE "ZZ".
       01 NM PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
      *> 1 — ordinary non-empty literals, the shape the screen must not disturb.
           MOVE FUNCTION MAX("AB" "CD") TO R.
           DISPLAY "1=[" R "]".
      *> 2 — a FIGURATIVE constant. 15.59.3 r1 is a NEGATIVE class list and SPACE
      *> is alphanumeric by 8.3.3.6.4 GR1, so this is two alphanumeric arguments
      *> under r2. A figurative is not a literal with a length the screen can
      *> measure, and it must pass.
           MOVE FUNCTION MAX(SPACE "A") TO R.
           DISPLAY "2=[" R "]".
      *> 3-4 — ITEM operands, whose width the screen must not read: the rule is
      *> about literals, and an item is never one.
           MOVE FUNCTION MAX(A B) TO R.
           DISPLAY "3=[" R "]".
           MOVE FUNCTION MIN(A B) TO R.
           DISPLAY "4=[" R "]".
      *> 5-6 — the ORD family, which carries the same rule at a different ordinal
      *> (r2, not r3) and returns an ordinal position rather than a value.
           MOVE FUNCTION ORD-MAX("B" "A") TO N.
           DISPLAY "5=" N.
           MOVE FUNCTION ORD-MIN(A B) TO N.
           DISPLAY "6=" N.
      *> 7 — a numeric argument list: the polymorphic kind admits it, and nothing
      *> about the zero-length rule may narrow that.
           MOVE FUNCTION MAX(NM 3) TO N.
           DISPLAY "7=" N.
           STOP RUN.
