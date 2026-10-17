*>--- conformance-case
*> id: pb459-index-name-not-in-table
*> rules: [SR-8.4.2.3.3-4, GR-14.9.39.4-1, GR-14.9.39.4-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.4.2.3.3 syntax rule 4 - "Index-name-1 shall correspond to a data description entry in the
*> hierarchy of the table being referenced that contains an INDEXED BY phrase specifying that index-name."
*> IX1 is the index of T1; E2 is an element of T2. The reference E2 (IX1) therefore names no occurrence of
*> the table it subscripts, and the rule is a SYNTAX rule, so it is a compile-time diagnostic at every
*> edition - 8.4.2.3.3 SR4 is unchanged text from COBOL-85 onward.
*>
*> kb/Work PB459: 14.9.39.4 GR1 exists so the rest of the SET clause can say "that table" ("Index-names are
*> associated with a given table by being specified in the INDEXED BY phrase of the OCCURS clause for that
*> table"), and the association WAS built - DataItem.IndexNames - but outside SEARCH nothing read it. So this
*> program compiled clean and wrote 77 through an unrelated table's occurrence number, with no diagnostic at
*> any stage and no run-time symptom either.
*>
*> Refused in BOTH dialect lanes (strict and --permissive), and the diagnostic says why: the reference has a
*> perfectly computable occurrence number, so accepting it under a leniency would silently read or write the
*> wrong table element. There is no coercion to offer that is not a wrong answer.
*>
*> The SET on the line above is the CONTROL and must NOT be diagnosed: 14.9.39.4 GR2 a) 3. b/c make
*> SET IX1 TO IX2 - an index-name of one table set from an index-name of another - legal, and GR2 a) 1. c
*> ends "even if that occurrence is not a valid occurrence within this table". Only the SUBSCRIPT is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB459IXNOTIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 PIC 9(2) OCCURS 5 TIMES INDEXED BY IX1.
       01 T2.
          05 E2 PIC 9(2) OCCURS 9 TIMES INDEXED BY IX2.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX2 TO 8
           SET IX1 TO IX2
           SET IX1 TO 3
           MOVE 77 TO E2 (IX1)
           STOP RUN.
