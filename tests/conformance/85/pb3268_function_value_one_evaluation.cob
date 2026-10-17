      *> kb/Work PB3268 - a NUMERIC function moved to MORE THAN ONE receiver. ISO 14.9.25.4 GR1: a
      *> function-identifier "is evaluated only once, immediately before data is moved to the first of
      *> the receiving operands", and the rule writes the shape out as MOVE a TO temp / MOVE temp TO b /
      *> MOVE temp TO c, "where 'temp' is an intermediate result item provided by the implementor" - so
      *> every receiver must store what a MOVE of the function itself stores. 15.4.1 adds that the
      *> returned value "is the same for all instances of a given function within a single execution of
      *> the runtime element" (arguments, collating sequence and locale unchanged), and 14.9.13.4 GR3
      *> gives an EVALUATE selection subject the same one evaluation. The intermediate used to be a fixed
      *> S9(21)V9(9), so every digit of the value past the ninth fraction digit was lost on the way to
      *> a second receiver (and an EVALUATE subject was compared at nine places).
      *>
      *> The values are derived, not copied: SQRT(2) is 1.41421356237309504880..., truncated by the MOVE
      *> to the receiver's 15 places (14.6.8.2 rule 4: zero fill or truncation on either end) ->
      *> 1.414213562373095; NUMVAL("1.23456789012") is exactly that numeric value (15.67.4 r1); MEAN(1 2 2)
      *> is 5/3 = 1.666666666666... -> 12 places 1.666666666666.
      *>
      *> M1  SQRT(2) to Q1 Q2 beside the single-receiver control Q3: 1414213562373095 in all three.
      *> M2  NUMVAL to two PIC 9V9(11) beside the control: 123456789012 in all three.
      *> M3  MEAN(1 2 2) to two PIC 9V9(12) beside the control: 1666666666666 in all three.
      *> M4  a receiver of 2 places beside one of 15: each receiver truncates the SAME value (1.41 /
      *>     1.414213562373095): 00141 / 1414213562373095.
      *> M5  RANDOM to two receivers is ONE draw: the two receivers hold the same value.
      *> M6  EVALUATE FUNCTION SQRT(2) against 1.414213562 and 1.41421356237309: the subject is held at
      *>     the value SQRT(2) has, which is neither of those nine- and fourteen-place numbers -> OTHER
      *>     (the subject held at nine places compared EQUAL to the first).
      *> M7  EVALUATE FUNCTION NUMVAL("1.23456789012") WHEN 1 WHEN 1.23456789012 -> EQ: the exact value
      *>     (eleven places) compares equal to the literal it was parsed from.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268ON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q1 PIC 9V9(15).
       01 Q2 PIC 9V9(15).
       01 Q3 PIC 9V9(15).
       01 N1 PIC 9V9(11).
       01 N2 PIC 9V9(11).
       01 N3 PIC 9V9(11).
       01 M1 PIC 9V9(12).
       01 M2 PIC 9V9(12).
       01 M3 PIC 9V9(12).
       01 S1 PIC 9(3)V99.
       01 S2 PIC 9V9(15).
       01 R1 PIC 9V9(9).
       01 R2 PIC 9V9(9).
       PROCEDURE DIVISION.
           MOVE FUNCTION SQRT(2) TO Q1 Q2
           MOVE FUNCTION SQRT(2) TO Q3
           DISPLAY "M1=" Q1 " " Q2 " " Q3
           MOVE FUNCTION NUMVAL("1.23456789012") TO N1 N2
           MOVE FUNCTION NUMVAL("1.23456789012") TO N3
           DISPLAY "M2=" N1 " " N2 " " N3
           MOVE FUNCTION MEAN(1 2 2) TO M1 M2
           MOVE FUNCTION MEAN(1 2 2) TO M3
           DISPLAY "M3=" M1 " " M2 " " M3
           MOVE FUNCTION SQRT(2) TO S1 S2
           DISPLAY "M4=" S1 " " S2
           MOVE FUNCTION RANDOM TO R1 R2
           IF R1 = R2
               DISPLAY "M5=SAME"
           ELSE
               DISPLAY "M5=DIFFERENT"
           END-IF
           EVALUATE FUNCTION SQRT(2)
               WHEN 1.414213562 DISPLAY "M6=EQ9"
               WHEN 1.41421356237309 DISPLAY "M6=EQ14"
               WHEN OTHER DISPLAY "M6=OTHER"
           END-EVALUATE
           EVALUATE FUNCTION NUMVAL("1.23456789012")
               WHEN 1 DISPLAY "M7=ONE"
               WHEN 1.23456789012 DISPLAY "M7=EQ"
               WHEN OTHER DISPLAY "M7=OTHER"
           END-EVALUATE
           STOP RUN.
