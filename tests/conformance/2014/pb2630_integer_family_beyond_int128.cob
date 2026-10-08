      *> ISO 15.44.4 r1: INTEGER "is the greatest integer less than or equal to
      *> the value of argument-1". 15.49.4 r1: INTEGER-PART is SIGN(x) *
      *> INTEGER(ABS(x)). 15.42.4 r1: FRACTION-PART is (argument-1 - FUNCTION
      *> INTEGER-PART (argument-1)). 15.64.4 and 15.77.4 define MOD and REM by
      *> the same equivalent arithmetic expressions. 15.4.1: under
      *> standard-decimal arithmetic the returned value is held in a standard
      *> intermediate data item, which holds 10 ** 40 and 34 significant digits
      *> at that magnitude exactly.
      *>
      *> kb/Work PB2630: the SDIDI INTEGER / INTEGER-PART / FRACTION-PART bodies
      *> landed the argument through an unchecked Int128 transfer that keeps only
      *> the low-order digits of a magnitude of 10 ** 38 or more, so INTEGER(10 **
      *> 40) was 0, INTEGER(-10 ** 40) was -1 and FRACTION-PART(10 ** 40) was
      *> 10 ** 40. A value with no fraction digits is its own integer part.
      *>
      *> Every expected value is derived from the rules above:
      *>   INTEGER(10 ** 40) / 10 ** 38            = 100
      *>   INTEGER(-(10 ** 40)) / 10 ** 38         = -100
      *>   INTEGER-PART(10 ** 40) / 10 ** 38       = 100
      *>   FRACTION-PART(10 ** 40)                 = 0
      *>   INTEGER(X * 10 ** 6) = X * 10 ** 6      (X has 31 digits, so the
      *>       product is a 34-digit significand at 10 ** 6: an integer)
      *>   MOD(10 ** 40, 3): the quotient is 3.333...E+39 (34 digits), three
      *>       times it is 9.999...E+39, and 10 ** 40 less that is 1000000;
      *>       REM agrees because both quotients are positive integers.
      *>   INTEGER(-2.75) = -3, INTEGER(2.75) = 2, INTEGER-PART(-2.75) = -2,
      *>       FRACTION-PART(-2.75) = -0.75: the controls with a fraction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2630INT.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(7).
       01 X PIC 9(31) VALUE 1234567890123456789012345678901.
       01 D PIC S9(3)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION INTEGER(10 ** 40) / 10 ** 38.
           DISPLAY "1-INTEGER-POS=" R.
           COMPUTE R = FUNCTION INTEGER(0 - 10 ** 40) / 10 ** 38.
           DISPLAY "2-INTEGER-NEG=" R.
           COMPUTE R = FUNCTION INTEGER-PART(10 ** 40) / 10 ** 38.
           DISPLAY "3-INTEGER-PART=" R.
           COMPUTE R = FUNCTION INTEGER-PART(0 - 10 ** 40) / 10 ** 38.
           DISPLAY "4-INTEGER-PART-NEG=" R.
           IF FUNCTION FRACTION-PART(10 ** 40) = 0
               DISPLAY "5-FRACTION-PART=ZERO"
           ELSE
               DISPLAY "5-FRACTION-PART=NONZERO"
           END-IF.
           IF FUNCTION FRACTION-PART(0 - 10 ** 40) = 0
               DISPLAY "6-FRACTION-PART-NEG=ZERO"
           ELSE
               DISPLAY "6-FRACTION-PART-NEG=NONZERO"
           END-IF.
           IF FUNCTION INTEGER(X * 10 ** 6) = X * 10 ** 6
               DISPLAY "7-INTEGER-34-DIGITS=EQUAL"
           ELSE
               DISPLAY "7-INTEGER-34-DIGITS=DIFFERENT"
           END-IF.
           COMPUTE R = FUNCTION MOD(10 ** 40, 3).
           DISPLAY "8-MOD=" R.
           COMPUTE R = FUNCTION REM(10 ** 40, 3).
           DISPLAY "9-REM=" R.
           COMPUTE D = FUNCTION INTEGER(-2.75).
           DISPLAY "10-INTEGER-FRACTION-NEG=" D.
           COMPUTE D = FUNCTION INTEGER(2.75).
           DISPLAY "11-INTEGER-FRACTION-POS=" D.
           COMPUTE D = FUNCTION INTEGER-PART(-2.75).
           DISPLAY "12-INTEGER-PART-FRACTION-NEG=" D.
           COMPUTE D = FUNCTION FRACTION-PART(-2.75).
           DISPLAY "13-FRACTION-PART-FRACTION-NEG=" D.
           STOP RUN.
