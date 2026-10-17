*>--- conformance-case
*> id: pb170_position_operand_legal_shapes
*> rules: [GR-8.4.2.3.4-1, FMT-8.4.2.3.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB170 - the REGRESSION FLOOR for the resolver's operand-class
      *> screen, because that screen sits on the ONE code path every subscript and
      *> reference-modification in the corpus takes. Each line is a shape a
      *> previous fix-queue item had to establish, and every one must keep its
      *> value: an integer item, a SCALED item (PB41 - the position is the VALUE
      *> 2.0, not the stored 20), an index-name, ZERO + 1 (PB50 - 8.8.1.1 admits
      *> the figurative), an integer constant-name (13.10.3 SR2), a
      *> function-identifier (D18/PB17), ** and a decimal literal (PB42), a
      *> quotient (PB136), and a ref-mod bound over an integer item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB170SHAPES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-I  PIC 9(2) VALUE 2.
       01 W-S  PIC 9V9  VALUE 2.0.
       01 W-A  PIC 9(2) VALUE 3.
       01 W-B  PIC 9(2) VALUE 1.
       01 W-W  PIC X(5) VALUE "ABCDE".
       01 W-R  PIC X.
       01 W-R2 PIC X(2).
       01 K-TWO CONSTANT AS 2.
       01 T.
          05 E PIC X OCCURS 4 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCD" TO T
           SET IX TO 3
           MOVE E(W-I)                TO W-R
           DISPLAY "INT=" W-R
           MOVE E(W-S)                TO W-R
           DISPLAY "SCALED=" W-R
           MOVE E(IX)                 TO W-R
           DISPLAY "INDEX=" W-R
           MOVE E(ZERO + 1)           TO W-R
           DISPLAY "ZERO=" W-R
           MOVE E(K-TWO)              TO W-R
           DISPLAY "CONST=" W-R
           MOVE E(FUNCTION INTEGER(3)) TO W-R
           DISPLAY "FUNC=" W-R
           MOVE E(W-B ** 2)           TO W-R
           DISPLAY "POWER=" W-R
           MOVE E(2.0)                TO W-R
           DISPLAY "DEC=" W-R
           MOVE E((W-A + W-B) / 2)    TO W-R
           DISPLAY "QUOT=" W-R
           MOVE W-W(W-I:2)            TO W-R2
           DISPLAY "REFMOD=" W-R2
           STOP RUN.
