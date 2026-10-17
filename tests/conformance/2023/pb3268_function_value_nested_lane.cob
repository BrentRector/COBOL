      *> kb/Work PB3268 (review round 2, R2-1) - the intermediate result item of ISO 14.9.25.4 GR1
      *> holds the VALUE a numeric function returns: GR1 writes the shape out as MOVE a TO temp /
      *> MOVE temp TO b / MOVE temp TO c, and 15.4.1 says "the returned value is the same for all
      *> instances of a given function within a single execution of the runtime element". So a MOVE
      *> of one function to two receivers must store into each what the MOVE of that function to
      *> that one receiver stores. The carrier a NESTED function's value arrives on decides what
      *> the outer function computes on, so each shape below puts a function that returns on the
      *> SDIDI (the exact decimal intermediate) inside a function that, given a binary64 argument,
      *> would compute in binary64:
      *>  - FUNCTION NUMVAL / NUMVAL-C: 15.67.4 r1 / 15.68.4 r1 "The returned value is the numeric
      *>    value represented by argument-1" - exact in every arithmetic mode, so an outer
      *>    VARIANCE / STANDARD-DEVIATION / ANNUITY computes on the exact value;
      *>  - FUNCTION SUM over PIC 9(30) beside PIC V9(9): the aligned arguments are 39 digits, wider
      *>    than the Int128 carrier, so SUM is computed on the SDIDI (kb/Work PB621) and the outer
      *>    ANNUITY computes on it too.
      *> Expected, derived from the equivalent arithmetic expressions of 15.4.1 r1:
      *> VARN VARIANCE(0.1 0.2 0.4) = 7/450 = 0.0155555...    -> 00155555555555555555555555 (truncated)
      *> ANNN ANNUITY(0.1 3) = 0.1 / (1 - 1.1 ** -3)          -> 04021148036253776435045317 (truncated)
      *> SDVC STANDARD-DEVIATION(0.1 0.2 0.4) = sqrt(7/450)   -> 01247219128924647128527916 (truncated)
      *> ANSM ANNUITY(SUM(A B) 3) with A = 0, B = 0.1         -> 04021148036253776435045317
      *> and the line BAD=0000 counts the shapes whose two-receiver and one-receiver stores differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268NL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAD PIC 9(4) VALUE 0.
       01 Q1 PIC 9V9(25).
       01 Q2 PIC 9V9(25).
       01 Q3 PIC 9V9(25).
       01 X1 PIC X(5) VALUE "0.1".
       01 A PIC 9(30) VALUE 0.
       01 B PIC V9(9) VALUE .1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION VARIANCE(FUNCTION NUMVAL("0.1")
                FUNCTION NUMVAL("0.2") FUNCTION NUMVAL("0.4"))
                TO Q1 Q2
           MOVE FUNCTION VARIANCE(FUNCTION NUMVAL("0.1")
                FUNCTION NUMVAL("0.2") FUNCTION NUMVAL("0.4")) TO Q3
           DISPLAY "VARN " Q1 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           MOVE FUNCTION ANNUITY(FUNCTION NUMVAL(X1) 3) TO Q1 Q2
           MOVE FUNCTION ANNUITY(FUNCTION NUMVAL(X1) 3) TO Q3
           DISPLAY "ANNN " Q1 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           MOVE FUNCTION STANDARD-DEVIATION(FUNCTION NUMVAL-C("0.1")
                0.2 0.4) TO Q1 Q2
           MOVE FUNCTION STANDARD-DEVIATION(FUNCTION NUMVAL-C("0.1")
                0.2 0.4) TO Q3
           DISPLAY "SDVC " Q1 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           MOVE FUNCTION ANNUITY(FUNCTION SUM(A B) 3) TO Q1 Q2
           MOVE FUNCTION ANNUITY(FUNCTION SUM(A B) 3) TO Q3
           DISPLAY "ANSM " Q1 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           DISPLAY "BAD=" BAD
           STOP RUN.
