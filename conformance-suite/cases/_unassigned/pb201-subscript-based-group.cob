*>--- conformance-case
*> id: pb201-subscript-based-group
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The SECOND shape kb/Work PB201 names, and the one that proved the defect
      *> was a CARRIER defect and not a class defect: a BASED group has no C#
      *> field of its own name at all, so the bind-time renderer emitted the raw
      *> COBOL word and the backend failed with CS0103 - a DIFFERENT failure from
      *> the WORKING-STORAGE group's CS1503, for source the same two clauses
      *> reject identically. 8.4.2.3.2 + 8.8.1.1 + 8.5.2.1, exactly as in
      *> pb201-subscript-group-item.
      *> reject-at omits 85: the BASED clause is a 2002 addition, so at 85 the
      *> program is rejected for the clause, not for the subscript.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB201N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG BASED.
          05 BF1 PIC 9(2).
       01 R  PIC X.
       01 T.
          05 E PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO T
           ALLOCATE BG
           MOVE 2 TO BF1
           MOVE E(BG) TO R
           STOP RUN.
