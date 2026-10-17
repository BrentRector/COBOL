*>--- conformance-case
*> id: stat-ord-min-zero-length-literal-argument
*> rules: [AR-15.72.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.72.3 rule 2 (ORD-MIN): "Argument-1 shall not be a zero-length literal."
      *>
      *> THE ORDINAL IS THE FUNCTION'S OWN — §15.72.3 orders its rules as ORD-MAX
      *> does (r1 negative class list, r2 this rule, r3 all-same-class), which is NOT
      *> the MAX/MIN order (r3), so the diagnostic must cite §15.72.3 r2 and nothing
      *> else. §15.72.2's general format is `FUNCTION ORD-MIN ( { argument-1 } ... )`,
      *> so every written position is argument-1; this fixture puts the zero-length
      *> literal at POSITION 1 OF 2, the ordinal the clause names literally.
      *>
      *> WHY THE DIAGNOSTIC IS EVIDENCE FOR RULE 2. The other two rules of §15.72.3
      *> cannot fire: r1's excluded classes are boolean, message-tag, object, pointer
      *> and a strongly-typed group item, and both arguments are class alphanumeric
      *> (§8.5.2.1 Table 2); r3 wants one class across the list and both are
      *> alphanumeric. Rule 2 is the only rule this program violates.
      *>
      *> The legal complement is 2023/pb35_zero_length_literal_legal_forms, whose
      *> line 6 (`FUNCTION ORD-MIN(A B)` over two PIC X(2) items) must keep
      *> compiling: the prohibition is on the LITERAL, never on an operand's width.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OMN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION ORD-MIN("" "AB") TO N.
           DISPLAY "N=" N.
           STOP RUN.
