      *> kb/Work PB3268 - a NUMERIC function moved to MORE THAN ONE receiver where a receiver is a
      *> floating-point item. ISO 14.9.25.4 GR1: a function-identifier "is evaluated only once, immediately
      *> before data is moved to the first of the receiving operands", and the rule writes the shape out as
      *> MOVE a TO temp / MOVE temp TO b / MOVE temp TO c, "where 'temp' is an intermediate result item
      *> provided by the implementor" - so every receiver must store what a MOVE of the function itself
      *> stores (15.4.1: "the returned value is the same for all instances of a given function within a
      *> single execution of the runtime element"). The intermediate used to be a fixed S9(21)V9(9): a
      *> COMP-2 receiver beside another received 3.141592653 where the single-receiver MOVE stores the
      *> binary64 value of PI. The expected outcomes are comparisons between the multi-receiver and the
      *> single-receiver MOVE (the standard leaves the character image of a float to the implementor,
      *> 14.9.11.4 GR1) and one fixed-point read-back of the value.
      *>
      *> F1  PI to two COMP-2 beside the control: all three equal -> SAME.
      *> F2  PI read back through a PIC 9V9(15) (MOVE of a float to a fixed item truncates, 14.6.8.2 rule 4):
      *>     the binary64 nearest to 3.14159265358979323846 is 3.14159265358979311600..., so 3141592653589793.
      *> F3  SQRT(2) to a COMP-2 and a PIC 9V9(15) in ONE MOVE: the COMP-2 equals the control COMP-2 and the
      *>     fixed item holds 1.414213562373095 (1.41421356237309504880... truncated).
      *> F4  PI to two COMP-1 beside the control: all three equal -> SAME.
      *> F5  PI to a COMP-2 and a COMP-1 beside their controls: each equals its control -> SAME.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268FL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 COMP-2.
       01 F2 COMP-2.
       01 F3 COMP-2.
       01 G1 COMP-1.
       01 G2 COMP-1.
       01 G3 COMP-1.
       01 Q1 PIC 9V9(15).
       PROCEDURE DIVISION.
           MOVE FUNCTION PI TO F1 F2
           MOVE FUNCTION PI TO F3
           IF F1 = F3 AND F2 = F3
               DISPLAY "F1=SAME"
           ELSE
               DISPLAY "F1=DIFFERENT"
           END-IF
           MOVE F1 TO Q1
           DISPLAY "F2=" Q1
           MOVE FUNCTION SQRT(2) TO F1 Q1
           MOVE FUNCTION SQRT(2) TO F3
           IF F1 = F3
               DISPLAY "F3=SAME " Q1
           ELSE
               DISPLAY "F3=DIFFERENT " Q1
           END-IF
           MOVE FUNCTION PI TO G1 G2
           MOVE FUNCTION PI TO G3
           IF G1 = G3 AND G2 = G3
               DISPLAY "F4=SAME"
           ELSE
               DISPLAY "F4=DIFFERENT"
           END-IF
           MOVE FUNCTION PI TO F1 G1
           MOVE FUNCTION PI TO F3
           MOVE FUNCTION PI TO G3
           IF F1 = F3 AND G1 = G3
               DISPLAY "F5=SAME"
           ELSE
               DISPLAY "F5=DIFFERENT"
           END-IF
           STOP RUN.
