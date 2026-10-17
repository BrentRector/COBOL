*>--- conformance-case
*> id: boolean_operators_below_2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The boolean operators B-AND/B-OR/B-XOR/B-NOT (ISO §8.7.2/§8.8.2) are a COBOL-2002 introduction. Residue
      *> migration #2 (DESIGN-version-conformance-pipeline.md): the parse-time {is2002()}? tier predicates AND the
      *> reverse-signature ReservedWordEditionHints arms are GONE — the boolean tiers parse at all editions behind the
      *> operand-adjacency boolExprAhead() ENTRY (a plain comparison never enters them — DEVLOG 621), and the gate is
      *> at BIND (BindPrimaryBoolean -> Check(BooleanOperators2002)), so below 2002 it is an exact COBOLNET0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BOB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 1 USAGE BIT VALUE B"1".
       01 B PIC 1 USAGE BIT VALUE B"0".
       PROCEDURE DIVISION.
       M. IF A B-AND B THEN DISPLAY "Y" END-IF. STOP RUN.
