      *>--- conformance-case
      *> id: pb202_odo_refmod_group_direction
      *> rules: [GR-13.18.38.4-8, FMT-8.4.3.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB202. ISO 1989:2023 13.18.38.4 GR8 decides how much of an occurs-depending group an operation
      *> uses, and the two arms AGREE on the sending side: GR8a (data-name-1 outside the group) and GR8b (data-name-1
      *> "included in the same group and the group data item is referenced as a sending operand") both use "only
      *> that part of the table area that is specified by the value of the data item referenced by data-name-1".
      *> Only a RECEIVING operand splits: GR8b "If the group is a receiving operand, the maximum length of the group
      *> will be used." 8.4.3.3.4 GR5 makes a reference-modified group "a subset of the data item referenced by
      *> identifier-1", so G(4:) READ is the group as a sending operand, and GR5c with length omitted runs to "the
      *> rightmost position of the data item referenced by identifier-1" - the CURRENT extent 1 + 2 x 3 = 7.
      *>   R1 = 2cccddd  G(1:7), depending INSIDE, count 2.
      *>   R2 = cddd     G(4:) stops at the current extent (the unused area holds eee fff ggg).
      *>   R3 = cddd     H(3:), the depending-OUTSIDE control (GR8a): extent 2 x 3 = 6.
      *>   R4 = 2cccddd  the whole-group MOVE, which always took the current extent.
      *> RECEIVING: MOVE "XYZXYZXYZ" TO G(2:9) at count 2 - GR8b gives the receiver the MAXIMUM length 16, so
      *> positions 2-10 take the value and positions 11-16 (fff ggg) are outside the unique data item and
      *> unmodified; raising the count to 5 shows the whole group:
      *>   R5 = 5XYZXYZXYZfffggg
      *> Before the fix R2 read cdddeee (the maximum image, into the unused table area).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB202ODORMDIR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 NN PIC 9 VALUE 5.
          05 GT OCCURS 1 TO 5 TIMES DEPENDING ON NN PIC X(3).
       01 MM PIC 9 VALUE 5.
       01 H.
          05 HT OCCURS 1 TO 5 TIMES DEPENDING ON MM PIC X(3).
       01 OUT7 PIC X(7).
       PROCEDURE DIVISION.
       P1.
           MOVE "ccc" TO GT (1)  MOVE "ddd" TO GT (2)
           MOVE "eee" TO GT (3)  MOVE "fff" TO GT (4)
           MOVE "ggg" TO GT (5)
           MOVE "ccc" TO HT (1)  MOVE "ddd" TO HT (2)
           MOVE "eee" TO HT (3)
           MOVE 2 TO NN  MOVE 2 TO MM
           DISPLAY "R1=" G (1:7)
           DISPLAY "R2=" G (4:)
           DISPLAY "R3=" H (3:)
           MOVE G TO OUT7
           DISPLAY "R4=" OUT7
           MOVE "XYZXYZXYZ" TO G (2:9)
           MOVE 5 TO NN
           DISPLAY "R5=" G
           STOP RUN.
