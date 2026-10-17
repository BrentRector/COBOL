*>--- conformance-case
*> id: stat-min-zero-length-literal-argument
*> rules: [AR-15.63.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.63.3 rule 3 (MIN): "Argument-1 shall not be a zero-length literal."
      *>
      *> §15.63.2's general format is `FUNCTION MIN ( { argument-1 } ... )`, so
      *> "argument-1" NAMES THE WHOLE VARIADIC LIST and the prohibition covers every
      *> written position, not merely the first. This fixture puts the zero-length
      *> literal at POSITION 3 OF 3 — the axis pb35-zero-length-literal-max-argument
      *> (position 1, and MAX's §15.59.3 r3) does not vary.
      *>
      *> WHY THE DIAGNOSTIC IS EVIDENCE FOR RULE 3 AND NOT MERELY FOR A REJECTION.
      *> §15.63.3 has exactly three argument rules and the other two cannot fire on
      *> this list: r1 is a NEGATIVE class list ("shall not be of class boolean,
      *> message-tag, object, or pointer, nor ... a strongly-typed group item") and
      *> class alphanumeric is on none of it; r2 ("All arguments shall be of the same
      *> class ...") is satisfied because all three arguments are alphanumeric
      *> literals (§8.5.2.1 Table 2). Rule 3 is the only rule this program violates.
      *>
      *> THE TEST IS ON THE LITERAL, NEVER ON A WIDTH. The clause says "zero-length
      *> LITERAL" in those words; the legal complement — a zero-length ITEM, a
      *> figurative constant, an item operand of any width — is pinned by
      *> 2023/pb35_zero_length_literal_legal_forms, which must keep compiling.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1MIN03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION MIN("AB" "CD" "") TO R.
           DISPLAY "R=[" R "]".
           STOP RUN.
