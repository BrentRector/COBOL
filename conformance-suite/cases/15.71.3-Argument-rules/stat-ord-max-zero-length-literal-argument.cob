*>--- conformance-case
*> id: stat-ord-max-zero-length-literal-argument
*> rules: [AR-15.71.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.71.3 rule 2 (ORD-MAX): "Argument-1 shall not be a zero-length literal."
      *>
      *> THE ORDINAL IS THE FUNCTION'S OWN. MAX states the identical prohibition at
      *> §15.59.3 r3 and MIN at §15.63.3 r3, but ORD-MAX orders its argument rules
      *> differently — r1 is the negative class list, r2 is THIS rule, r3 is the
      *> all-same-class rule — so a diagnostic citing "§15.59.3 r3" here would be the
      *> inherited-citation failure, not a citation.
      *>
      *> §15.71.2's general format is `FUNCTION ORD-MAX ( { argument-1 } ... )`, so
      *> every written position IS argument-1 and carries the rule. This fixture puts
      *> the zero-length literal at POSITION 2 OF 3.
      *>
      *> WHY THE DIAGNOSTIC IS EVIDENCE FOR RULE 2. §15.71.3's other two rules cannot
      *> fire on this list: r1 excludes classes boolean, message-tag, object, pointer
      *> and a strongly-typed group item, and every argument here is class
      *> alphanumeric (§8.5.2.1 Table 2); r3 wants all arguments of the same class and
      *> all three are alphanumeric. Rule 2 is the only rule this program violates.
      *>
      *> The legal complement is 2023/pb35_zero_length_literal_legal_forms, whose
      *> line 5 (`FUNCTION ORD-MAX("B" "A")`) must keep compiling: the rule is on the
      *> LITERAL, never on an operand's width.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OMX02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION ORD-MAX("AB" "" "CD") TO N.
           DISPLAY "N=" N.
           STOP RUN.
