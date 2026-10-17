      *> kb/Work PB3268 (review round 4, R1-1) - the intermediate result item of ISO 14.9.25.4 GR1
      *> ("an intermediate result item provided by the implementor") must hand each receiver of a
      *> MOVE the value the MOVE of the same function to that one receiver stores (15.4.1: "the
      *> returned value is the same for all instances of a given function"). An INTEGER function
      *> (15.2 item 5: "An integer function has an operational sign and no digits to the right of
      *> the decimal point") fixes the value's TYPE, never its magnitude: INTEGER of a FLOAT-LONG
      *> item holding 1.5E50 is a 51-digit integer, which no fixed-point item of the Int128 carrier
      *> holds, so a binary64 value (or one landing only in floating-point receivers) is held on a
      *> binary64 intermediate, as any other numeric function's is.
      *> Expected (15.44.1 "the greatest integer value that is less than or equal to the
      *> argument"; 15.49.1 "the integer portion of argument-1"): every binary64 of magnitude
      *> 2**53 or more is an integer, so INTEGER(F) = INTEGER-PART(F) = F = 1.5E50, and
      *> INTEGER(1.5E40) = 1.5E40. Each line shows the two-receiver and the one-receiver store
      *> divided by 1.0E40 (or 1.0E30), rounded: 15000000000 = 1.5E50 / 1.0E40, and
      *> 15000000000 = 1.5E40 / 1.0E30. INTEGER(-2.5E0) = -3 beside a fixed-point receiver.
      *> BAD=0000 counts the shapes whose two-receiver and one-receiver stores differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268IF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAD PIC 9(4) VALUE 0.
       01 F  USAGE FLOAT-LONG VALUE 1.5E50.
       01 C1 USAGE FLOAT-LONG.
       01 C2 USAGE FLOAT-LONG.
       01 C3 USAGE FLOAT-LONG.
       01 X1 PIC 9(11).
       01 X3 PIC 9(11).
       01 N1 PIC S9(4) SIGN LEADING SEPARATE.
       01 N3 PIC S9(4) SIGN LEADING SEPARATE.
       01 Y1 PIC S9(4) SIGN LEADING SEPARATE.
       01 Y3 PIC S9(4) SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION INTEGER(F) TO C1 C2
           MOVE FUNCTION INTEGER(F) TO C3
           COMPUTE X1 ROUNDED = C1 / 1.0E40
           COMPUTE X3 ROUNDED = C3 / 1.0E40
           DISPLAY "INTF " X1 " " X3
           IF C1 NOT = C3 OR C2 NOT = C3 OR C3 NOT = F
              ADD 1 TO BAD
           END-IF
           MOVE FUNCTION INTEGER-PART(F) TO C1 C2
           MOVE FUNCTION INTEGER-PART(F) TO C3
           COMPUTE X1 ROUNDED = C1 / 1.0E40
           COMPUTE X3 ROUNDED = C3 / 1.0E40
           DISPLAY "IPRF " X1 " " X3
           IF C1 NOT = C3 OR C2 NOT = C3 OR C3 NOT = F
              ADD 1 TO BAD
           END-IF
           MOVE FUNCTION INTEGER(1.5E40) TO C1 C2
           MOVE FUNCTION INTEGER(1.5E40) TO C3
           COMPUTE X1 ROUNDED = C1 / 1.0E30
           COMPUTE X3 ROUNDED = C3 / 1.0E30
           DISPLAY "INTL " X1 " " X3
           IF C1 NOT = C3 OR C2 NOT = C3 ADD 1 TO BAD END-IF
           MOVE FUNCTION INTEGER(-2.5E0) TO N1 C1
           MOVE FUNCTION INTEGER(-2.5E0) TO N3
           MOVE FUNCTION INTEGER(-2.5E0) TO C3
           COMPUTE Y1 = C1
           COMPUTE Y3 = C3
           DISPLAY "INTM " N1 " " N3 " " Y1 " " Y3
           IF N1 NOT = N3 OR C1 NOT = C3 ADD 1 TO BAD END-IF
           DISPLAY "BAD=" BAD
           STOP RUN.
