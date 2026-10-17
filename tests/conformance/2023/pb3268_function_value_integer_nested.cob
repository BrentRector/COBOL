      *> kb/Work PB3268 (review round 3, R3-1) - the intermediate result item of ISO 14.9.25.4 GR1
      *> ("an intermediate result item provided by the implementor") must hand each receiver of a
      *> MOVE the value the MOVE of the same function to that one receiver stores, because 15.4.1
      *> says "the returned value is the same for all instances of a given function". An INTEGER
      *> function (15.2 item 5: "An integer function has an operational sign and no digits to the
      *> right of the decimal point") fixes the TYPE of a value, not the carrier it arrives on, so:
      *>  - INTEGER / INTEGER-PART of a floating-point literal is computed on the SDIDI (the exact
      *>    decimal intermediate), and an outer ANNUITY computes on that exact value;
      *>  - SUM over PIC 9(31) elements may need more digits than the Int128 carrier (kb/Work
      *>    PB621), is computed on the SDIDI, and an outer ANNUITY computes on it too;
      *>  - an integer value of 31 digits is held whole for every receiver.
      *> Expected, derived from the equivalent arithmetic expressions of 15.4.1 r1:
      *> INTD ANNUITY(INTEGER(1.5E0) 3) = 1 / (1 - 2 ** -3) = 8/7 -> 11428571428571428571428571
      *> IPRT ANNUITY(INTEGER-PART(1.5E0) 3) = 8/7                -> 11428571428571428571428571
      *> ISUM ANNUITY(SUM(T(ALL)) 3) with every T = 0 = 1/3       -> 03333333333333333333333333
      *> (each truncated to PIC 9V9(25)), the 31-digit INTEGER(N31) and SUM(U(1)) stored whole,
      *> and the line BAD=0000 counts the shapes whose two-receiver and one-receiver stores differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268IN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAD PIC 9(4) VALUE 0.
       01 Q1 PIC 9V9(25).
       01 Q2 PIC 9V9(25).
       01 Q3 PIC 9V9(25).
       01 R1 PIC 9(31).
       01 R2 PIC 9(31).
       01 R3 PIC 9(31).
       01 N31 PIC S9(31) VALUE 9876543210987654321098765432101.
       01 TB.
          05 T PIC 9(31) OCCURS 3 VALUE 0.
       01 UB.
          05 U PIC 9(31) OCCURS 2 VALUE 1234567890123456789012345678901.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION ANNUITY(FUNCTION INTEGER(1.5E0) 3) TO Q1 Q2
           MOVE FUNCTION ANNUITY(FUNCTION INTEGER(1.5E0) 3) TO Q3
           DISPLAY "INTD " Q1 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           MOVE FUNCTION ANNUITY(FUNCTION INTEGER-PART(1.5E0) 3)
                TO Q1 Q2
           MOVE FUNCTION ANNUITY(FUNCTION INTEGER-PART(1.5E0) 3) TO Q3
           DISPLAY "IPRT " Q1 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           MOVE FUNCTION ANNUITY(FUNCTION SUM(T(ALL)) 3) TO Q1 Q2
           MOVE FUNCTION ANNUITY(FUNCTION SUM(T(ALL)) 3) TO Q3
           DISPLAY "ISUM " Q1 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           MOVE FUNCTION INTEGER(N31) TO R1 R2
           MOVE FUNCTION INTEGER(N31) TO R3
           DISPLAY "I31  " R1 " " R3
           IF R1 NOT = R3 OR R2 NOT = R3 ADD 1 TO BAD END-IF
           MOVE FUNCTION SUM(U(1)) TO R1 R2
           MOVE FUNCTION SUM(U(1)) TO R3
           DISPLAY "S31  " R1 " " R3
           IF R1 NOT = R3 OR R2 NOT = R3 ADD 1 TO BAD END-IF
           DISPLAY "BAD=" BAD
           STOP RUN.
