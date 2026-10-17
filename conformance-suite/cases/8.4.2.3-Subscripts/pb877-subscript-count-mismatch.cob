*>--- conformance-case
*> id: pb877-subscript-count-mismatch
*> rules: [SR-8.4.2.3.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB877 - ISO 8.4.2.3.3 SR3, the COUNT half of the same rule.
*>
*> SR3: "Except as defined in Syntax rule 5, when a reference is made to a table element, the number of
*> subscripts shall equal the number of OCCURS clauses in the description of the table element being
*> referenced." X below is subordinate to exactly ONE OCCURS clause, so X (1, 2) writes one subscript too
*> many, at every edition. SR5's seven exceptions all admit an OMITTED subscript list (a SEARCH subject, a
*> REDEFINES clause, an OCCURS KEY IS phrase, a SORT key or table subject, a screen entry's FROM/TO/USING
*> phrase, a report SUM addend); none of them admits an extra one.
*>
*> This is the shape PB877's own defect MANUFACTURED out of legal source: the declaration-informed '('
*> splitter asked DataItem.IsTable, SR2's first half only, so `Y (X(1))` - one subscript - was read as the
*> two subscripts `Y (X) (1)` and rejected here. The rule now decides both directions from one predicate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB877COUNT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
         02 G2          OCCURS 3.
           03 X         PIC 9 VALUE 4.
       PROCEDURE DIVISION.
           DISPLAY X (1, 2)
           STOP RUN.
