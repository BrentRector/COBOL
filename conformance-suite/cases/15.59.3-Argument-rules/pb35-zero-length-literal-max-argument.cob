*>--- conformance-case
*> id: pb35-zero-length-literal-max-argument
*> rules: [AR-15.59.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.59.3 rule 3 (MAX): "Argument-1 shall not be a zero-length
      *> literal." 15.63.3 r3 (MIN), 15.71.3 r2 (ORD-MAX) and 15.72.3 r2
      *> (ORD-MIN) state the identical rule; 15.66.3 r3 (NATIONAL-OF) and
      *> 15.85.3 r4 (STANDARD-COMPARE) make six clauses in all, and exactly ONE
      *> of them was enforced before fix-queue PB35 - NATIONAL-OF's, hand-written
      *> in the repertoire checker, so nothing about it generalized.
      *>
      *> The general format is `FUNCTION MAX ( { argument-1 } ... )`, so
      *> "argument-1" IS the whole variadic list and the prohibition covers every
      *> argument, not just the first.
      *>
      *> The rule now rides the argument SCHEMA beside each position's class
      *> rule, and cites its OWN ordinal: MAX's class rule is r1, this is r3.
      *> pb35_zero_length_literal_legal_forms is the companion that pins what
      *> must keep compiling.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB35NEGMAX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION MAX("" "AB") TO R.
           DISPLAY "R=[" R "]".
           STOP RUN.
