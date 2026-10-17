      *>--- conformance-case
      *> id: l1-subtract-sr3-literal2-nonnumeric
      *> rules: [SR-14.9.44.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.3 SR3 (FORMATS 1 AND 2) — "Literal-1 and
      *> literal-2 shall be numeric literals."
      *> THE LITERAL-2 ARM.  Literal-2 exists only in format 2, the
      *> `SUBTRACT … FROM {identifier-2 | literal-2} GIVING …` shape
      *> of §14.9.44.2, where it is the MINUEND.  "20" is written as
      *> an alphanumeric literal, so `SUBTRACT 1 FROM "20" GIVING G`
      *> violates SR3.  As with the literal-1 arm, the digits being
      *> decimal is the trap and not the licence.
      *> G is PIC 9(4), a category the GIVING resultant rule SR4
      *> admits outright, so SR4 cannot be the reason for the
      *> rejection and literal-1 (the numeric literal 1) satisfies the
      *> other half of SR3 — the only rule broken is SR3 at
      *> literal-2.
      *> Its literal-1 sibling is
      *> negative/l1-subtract-sr3-literal1-nonnumeric; the two
      *> positions reach the binder by DIFFERENT paths (the operand
      *> list versus the FROM phrase), which is why one fixture cannot
      *> stand for both.
      *> Reject-at names every edition: SR3 carries no edition
      *> condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBSR3B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           SUBTRACT 1 FROM "20" GIVING G.
           STOP RUN.
