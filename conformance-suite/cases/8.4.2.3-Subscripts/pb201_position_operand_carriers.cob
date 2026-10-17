*>--- conformance-case
*> id: pb201_position_operand_carriers
*> rules: [GR-8.4.2.3.4-1, GR-8.4.2.3.4-2, SR-8.4.3.3.3-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB201 - THE CARRIER axis of a position operand; the twin of
      *> pb170_position_operand_legal_shapes, which pins the CLASS axis.
      *> ISO 8.8.1.1 admits "an identifier referencing a numeric data item" and
      *> 8.4.2.3.2 makes a subscript arithmetic-expression-1, so EVERY numeric
      *> carrier is legal in the position; 8.4.3.3.3 rule 4 ("leftmost-position
      *> and length shall be arithmetic expressions") carries the same rule to a
      *> reference-modification bound. 8.4.2.3.4 GR1b makes the subscript "the
      *> result of the evaluation of arithmetic-expression-1", so an INTEGRAL
      *> float value selects its occurrence and raises nothing.
      *> ReferenceResolver.PositionRead emitted CobolTable.Occ(<field>) and bet on
      *> C# overload resolution, whose overload set is long/string only. MEASURED
      *> on e4850fc7 (the DEFAULT strict lane, not a leniency):
      *>   E(W-FL) FLOAT-LONG   -> CS1503 cannot convert from 'double' to 'long'
      *>   E(W-BIG) PIC 9(20)   -> CS1503 cannot convert from 'System.Int128' ...
      *>   E(W-U) BINARY-DOUBLE UNSIGNED -> CS1503 ... from 'ulong' to 'long'
      *>   W-W(W-FL:2)          -> CS1503 ... from 'double' to 'long'
      *> Expected values are computed from the occurrence numbers below over
      *> T = "ABCD" and W-W = "ABCDE": 3->C, 2->B, 4->D, 1->A; W-W(3:2) = "CD"
      *> and W-W(4:2) = "DE".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB201CARRIERS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-FL  USAGE FLOAT-LONG.
       01 W-FS  USAGE FLOAT-SHORT.
       01 W-BIG PIC 9(20) USAGE COMP.
       01 W-U   USAGE BINARY-DOUBLE UNSIGNED.
       01 W-W   PIC X(5) VALUE "ABCDE".
       01 W-R   PIC X.
       01 W-R2  PIC X(2).
       01 T.
          05 E PIC X OCCURS 4 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCD" TO T
           MOVE 3 TO W-FL
           MOVE 2 TO W-FS
           MOVE 4 TO W-BIG
           MOVE 1 TO W-U
           MOVE E(W-FL)     TO W-R
           DISPLAY "FLOATLONG=" W-R
           MOVE E(W-FS)     TO W-R
           DISPLAY "FLOATSHORT=" W-R
           MOVE E(W-BIG)    TO W-R
           DISPLAY "WIDE=" W-R
           MOVE E(W-U)      TO W-R
           DISPLAY "ULONG=" W-R
           MOVE W-W(W-FL:2) TO W-R2
           DISPLAY "REFMODFLOAT=" W-R2
           MOVE W-W(W-BIG:2) TO W-R2
           DISPLAY "REFMODWIDE=" W-R2
           STOP RUN.
