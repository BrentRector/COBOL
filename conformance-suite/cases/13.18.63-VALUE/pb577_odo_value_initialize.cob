*>--- conformance-case
*> id: pb577_odo_value_initialize
*> rules: [GR-13.18.63.4-6, GR-13.18.63.4-9]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB577 — ISO 13.18.63.4 GR6 and GR9 at the OTHER occasion the VALUE clause takes effect on:
      *> GR4c's second bullet, "during the execution of an INITIALIZE statement". Before kb/Work PB393 this whole
      *> surface ABORTED - `INITIALIZE <group containing an occurs-depending table>` reached a runtime
      *> NotImplementedCobolFeatureException at all four editions - so no golden could exist; the sibling
      *> conformance:cases/13.18.63-VALUE/pb577_odo_value_initial_state carries GR4c's FIRST bullet. The TO VALUE phrase is
      *> COBOL-2002 (the edition floor is witnessed by conformance:cases/14.9.20-INITIALIZE/pb420-initialize-float-to-value-85).
      *> Every expected value below is derived from the rules, not measured:
      *>   L1 GR9, "causes EVERY occurrence of the associated data item to be assigned the specified value", at
      *>      this occasion: N1 is OUTSIDE S1 and holds 2, so 14.9.20.4 GR8 ("the number of occurrences
      *>      initialized is determined by the rules of the OCCURS clause for a receiving data item") reaches
      *>      13.18.38.4 GR8a, "only that part of the table area that is specified by the value of the data item
      *>      referenced by data-name-1 AT THE START OF THE OPERATION will be used" - occurrences 1 and 2.
      *>   L2 ⚖ DETERMINATION D-ODO1 (docs/CONFORMANCE.md §3): the same statement leaves occurrence 5 untouched.
      *>      13.18.63.4 GR6's maximum-pretence governs the occasions on which no statement supplies a count;
      *>      14.9.20.4 GR8 is the more specific rule for how many occurrences an INITIALIZE statement
      *>      initializes, and it is the one that answers here. T1(5) therefore keeps the dirtying MOVE's ZZ.
      *>   L3 The OTHER quadrant of 13.18.38.4 GR8: N2 is INSIDE S2, and S2 is a receiving operand, so GR8b's
      *>      "If the group is a receiving operand, the maximum length of the group will be used" gives all five
      *>      occurrences - and GR6's last sentence, "If a VALUE clause is associated with the data item
      *>      referenced by a DEPENDING phrase, that value is considered to be placed in the data item AFTER the
      *>      occurs-depending table is initialized", is why N2 reads 2 while the table beside it carries five.
      *>      Reading N2 BEFORE it is re-raised is what makes this a witness of the order rather than a DERIVATION.
      *>   L4 GR6 case c, "a data item that is subordinate to an occurs-depending table", at this occasion.
      *>   L5 GR9's SECOND named arm, "an entry that is SUBORDINATE to an OCCURS clause", at this occasion -
      *>      the arm conformance:cases/13.18.63-VALUE/table_value_occurs cannot reach, because all three of its elements carry
      *>      their own OCCURS clause. Every occurrence, not just the first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB577OIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9 VALUE 2.
       01 S1.
          05 T1 PIC X(2) OCCURS 1 TO 5 DEPENDING ON N1 VALUE "AB".
       01 S2.
          05 N2 PIC 9 VALUE 2.
          05 T2 PIC X(2) OCCURS 1 TO 5 DEPENDING ON N2 VALUE "CD".
       01 N3 PIC 9 VALUE 2.
       01 S3.
          05 T3 OCCURS 1 TO 5 DEPENDING ON N3.
             10 A3 PIC X(2) VALUE "EF".
             10 B3 PIC 9 VALUE 4.
       01 R9.
          05 G9 OCCURS 3.
             10 A9 PIC X(2) VALUE "XY".
             10 B9 PIC 9 VALUE 7.
       PROCEDURE DIVISION.
           MOVE 5 TO N1
           MOVE "ZZ" TO T1(1)
           MOVE "ZZ" TO T1(2)
           MOVE "ZZ" TO T1(5)
           MOVE 2 TO N1
           INITIALIZE S1 ALL TO VALUE
           DISPLAY "L1 OUT-CUR 1=[" T1(1) "] 2=[" T1(2) "]"
           MOVE 5 TO N1
           DISPLAY "L2 OUT-MAX 5=[" T1(5) "]"
           MOVE 5 TO N2
           MOVE "ZZ" TO T2(1)
           MOVE "ZZ" TO T2(5)
           MOVE 9 TO N2
           INITIALIZE S2 ALL TO VALUE
           DISPLAY "L3 IN-N2=" N2
           MOVE 5 TO N2
           DISPLAY "L3 IN-MAX  1=[" T2(1) "] 5=[" T2(5) "]"
           MOVE "ZZ" TO A3(1)
           MOVE 9 TO B3(1)
           INITIALIZE S3 ALL TO VALUE
           DISPLAY "L4 SUB     1=[" A3(1) B3(1) "]"
           MOVE "ZZ" TO A9(3)
           MOVE 9 TO B9(3)
           INITIALIZE R9 ALL TO VALUE
           DISPLAY "L5 G9-ARM2 1=[" A9(1) B9(1) "] 3=[" A9(3) B9(3) "]"
           STOP RUN.
