      *> kb/Work PB244 - the RECEIVING operand of a variable-length group MOVE uses an OCCURS
      *> DEPENDING table to the count the standard names, not to its maximum.
      *>
      *> 13.18.38.4 GR8: "a) If the data item referenced by data-name-1 is outside the
      *> group, only that part of the table area that is specified by the value of the
      *> data item referenced by data-name-1 at the start of the operation will be used.
      *> ... b) If the data item referenced by data-name-1 is included in the same group
      *> and the group data item is referenced as a sending operand, only that part ... will
      *> be used in the operation. If the group is a receiving operand, the maximum length
      *> of the group will be used." 14.9.25.4 GR9 moves the common part of the two groups
      *> of a variable-length group MOVE, so the receiving table's occurrences past the
      *> count keep the content they have.
      *>
      *> EXPECTED VALUES, DERIVED (G1: H 3, D "abc", T "x" "y" "z", count K = 3):
      *>   A  G2's DEPENDING item K2 is outside G2 and holds 1: only occurrence 1 of T2 is
      *>      used, so T2(2) "q" and T2(3) "r" survive; shown at K2 = 3     = 3abcxqr
      *>   B  G4's DEPENDING item K4 is INSIDE G4 (GR8 b, a receiving group): the maximum is
      *>      used, so all three occurrences arrive, and K4, the first byte of
      *>      the sent group, becomes 3                                      = 3abcxyz
      *>   C  the cell-backed twin GX (EXTERNAL, KX outside holding 1): as A  = 3abcxqr
      *>   D  the table is beneath a NESTED group (G6.SUB6), K6 outside and holding 1:
      *>      the count reaches the nested group, so occurrences 2 and 3 survive = abcppxqr
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244RCV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 3.
       01 K2 PIC 9 VALUE 1.
       01 KX PIC 9 EXTERNAL.
       01 G1.
          05 H1 PIC 9 VALUE 3.
          05 D1 PIC X DYNAMIC LENGTH LIMIT 5.
          05 T1 PIC X OCCURS 1 TO 3 DEPENDING ON K.
       01 G2.
          05 H2 PIC 9 VALUE 0.
          05 D2 PIC X DYNAMIC LENGTH LIMIT 5.
          05 T2 PIC X OCCURS 1 TO 3 DEPENDING ON K2.
       01 G4.
          05 K4 PIC 9 VALUE 1.
          05 D4 PIC X DYNAMIC LENGTH LIMIT 5.
          05 T4 PIC X OCCURS 1 TO 3 DEPENDING ON K4.
       01 GX EXTERNAL.
          05 HX PIC 9.
          05 DX PIC X DYNAMIC LENGTH LIMIT 5.
          05 TX PIC X OCCURS 1 TO 3 DEPENDING ON KX.
       01 K5 PIC 9 VALUE 3.
       01 K6 PIC 9 VALUE 1.
       01 G5.
          05 D5 PIC X DYNAMIC LENGTH LIMIT 5.
          05 SUB5.
             10 P5 PIC XX.
             10 T5 PIC X OCCURS 1 TO 3 DEPENDING ON K5.
       01 G6.
          05 D6 PIC X DYNAMIC LENGTH LIMIT 5.
          05 SUB6.
             10 P6 PIC XX.
             10 T6 PIC X OCCURS 1 TO 3 DEPENDING ON K6.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO D1
           MOVE "x" TO T1(1)
           MOVE "y" TO T1(2)
           MOVE "z" TO T1(3)
           MOVE "q" TO T2(2)
           MOVE "r" TO T2(3)
           MOVE G1 TO G2
           MOVE 3 TO K2
           DISPLAY "A=[" G2 "]"
           MOVE "q" TO T4(2)
           MOVE "r" TO T4(3)
           MOVE G1 TO G4
           DISPLAY "B=[" G4 "]"
           MOVE 3 TO KX
           MOVE "q" TO TX(2)
           MOVE "r" TO TX(3)
           MOVE 1 TO KX
           MOVE G1 TO GX
           MOVE 3 TO KX
           DISPLAY "C=[" GX "]"
           MOVE "abc" TO D5
           MOVE "pp" TO P5
           MOVE "x" TO T5(1)
           MOVE "y" TO T5(2)
           MOVE "z" TO T5(3)
           MOVE "q" TO T6(2)
           MOVE "r" TO T6(3)
           MOVE G5 TO G6
           MOVE 3 TO K6
           DISPLAY "D=[" G6 "]"
           STOP RUN.
