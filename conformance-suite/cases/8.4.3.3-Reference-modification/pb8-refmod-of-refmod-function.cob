*>--- conformance-case
*> id: pb8-refmod-of-refmod-function
*> rules: [SR-8.4.3.3.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.3 SR3: "Identifier-1 shall not be a reference-modification format identifier." A ref-mod
      *> result is a NEW unique data item (8.4.3.3.4 GR5), so a second modifier has no defined base to count
      *> from. Enforced in the BINDER, not by the grammar's arity, so the function and data-reference sides
      *> report the same rule - see pb8-refmod-of-refmod-data.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB8NEGSR3F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(2).
       PROCEDURE DIVISION.
           MOVE FUNCTION UPPER-CASE("abcdefgh") (1:4)(1:2) TO T
           STOP RUN.
