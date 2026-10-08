      *> kb/Work PB2638 - an ARITHMETIC store into a FLOATING-POINT
      *>   numeric-edited receiver obeys the receiver's ROUNDED [MODE].
      *>   14.7.4.3 rule 4: "the arithmetic value is rounded to the
      *>   nearest value that can be represented in the resultant
      *>   identifier" - for FE PIC +9.99E+99 the representable values
      *>   have a three-digit significand (14.6.8.4 rule 1 normalizes
      *>   it), so the significand rounds at its LAST DIGIT. Rule 2: no
      *>   ROUNDED phrase is TRUNCATION. Rule 7: PROHIBITED and an
      *>   inexact value -> the size error condition, FE unchanged,
      *>   EC-SIZE-TRUNCATION (14.7.5 no-phrase rule 4 names the same
      *>   level-3 condition). 14.7.5 rule 3 is "after radix point
      *>   alignment and any applicable rounding", so a significand
      *>   that rounds up past its last digit is renormalized and its
      *>   exponent tested after the carry.
      *>   Every expected image is derived from those rules:
      *>   1  2 / 3 ROUNDED          -> 6.666.. -> +6.67E-01
      *>   2  1.235 ROUNDED          -> tie, farther from zero -> +1.24
      *>   3  1.225 NEAREST-EVEN     -> tie, even last digit   -> +1.22
      *>   4  1.225 NEAREST-TOWARD-ZERO                        -> +1.22
      *>   5  1.239 no phrase        -> truncation             -> +1.23
      *>   6  1.231 AWAY-FROM-ZERO   -> +1.24 ; -1.231 -> -1.24
      *>   7  1.231 TOWARD-GREATER   -> +1.24 ; -1.239 -> -1.23
      *>   8  1.239 TOWARD-LESSER    -> +1.23 ; -1.231 -> -1.24
      *>   9  9.996 ROUNDED          -> carry, renormalized +1.00E+01
      *>  10  PROHIBITED, 1.234      -> size error, FE unchanged
      *>  11  PROHIBITED, 1.23       -> exact, +1.23E+00
      *>  12  9995000000 ROUNDED into +9.99E+9 -> 1.00E+10 is past the
      *>      exponent capacity: size error, receiver unchanged
      *>  13  the same, no ROUNDED  -> truncation +9.99E+09, no error
      *>  15  1 / 3E24 ROUNDED (the outermost quotient has no scale to
      *>      be computed AT; the significand rounds) -> +3.33E-25
      *>  14  EC-SIZE checking: the size error names EC-SIZE-TRUNCATION
      *>   cite.py --check 14.7.4.3 "the arithmetic value is rounded to
      *>   the nearest value that can be represented in the resultant
      *>   identifier" -> OK 14.7.4.3 4)
      *>   cite.py --check 14.7.4.3 "If the PROHIBITED phrase is
      *>   specified, and the arithmetic value cannot be represented
      *>   exactly in the resultant identifier" -> OK 14.7.4.3 7)
      *>   cite.py --check 14.7.5 "if the result of the arithmetic
      *>   statement is a value further from zero than permitted for the
      *>   associated resultant data item, the EC-SIZE-TRUNCATION
      *>   exception condition is set to exist" -> OK 14.7.5 4)
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2638FE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FE   PIC +9.99E+99.
       01 FE2  PIC +9.99E+9.
       01 A    PIC S9V999 VALUE 0.
       01 B    PIC S9V99  VALUE 1.23.
       01 BIG  PIC 9(10)  VALUE 9995000000.
       01 NINE PIC 9V999  VALUE 9.996.
       01 HUGE PIC 9(25)  VALUE 3000000000000000000000000.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE FE ROUNDED = 2 / 3.
           DISPLAY "1 [" FE "]".
           MOVE 1.235 TO A.
           COMPUTE FE ROUNDED = A.
           DISPLAY "2 [" FE "]".
           MOVE 1.225 TO A.
           COMPUTE FE ROUNDED MODE IS NEAREST-EVEN = A.
           DISPLAY "3 [" FE "]".
           COMPUTE FE ROUNDED MODE IS NEAREST-TOWARD-ZERO = A.
           DISPLAY "4 [" FE "]".
           MOVE 1.239 TO A.
           COMPUTE FE = A.
           DISPLAY "5 [" FE "]".
           MOVE 1.231 TO A.
           COMPUTE FE ROUNDED MODE IS AWAY-FROM-ZERO = A.
           DISPLAY "6 [" FE "]".
           COMPUTE FE ROUNDED MODE IS AWAY-FROM-ZERO = 0 - A.
           DISPLAY "6 [" FE "]".
           COMPUTE FE ROUNDED MODE IS TOWARD-GREATER = A.
           DISPLAY "7 [" FE "]".
           MOVE -1.239 TO A.
           COMPUTE FE ROUNDED MODE IS TOWARD-GREATER = A.
           DISPLAY "7 [" FE "]".
           MOVE 1.239 TO A.
           COMPUTE FE ROUNDED MODE IS TOWARD-LESSER = A.
           DISPLAY "8 [" FE "]".
           MOVE -1.231 TO A.
           COMPUTE FE ROUNDED MODE IS TOWARD-LESSER = A.
           DISPLAY "8 [" FE "]".
           COMPUTE FE ROUNDED = NINE.
           DISPLAY "9 [" FE "]".
           MOVE 1.234 TO A.
           COMPUTE FE ROUNDED MODE IS PROHIBITED = A
               ON SIZE ERROR DISPLAY "10 size error"
               NOT ON SIZE ERROR DISPLAY "10 stored"
           END-COMPUTE.
           DISPLAY "10 [" FE "]".
           COMPUTE FE ROUNDED MODE IS PROHIBITED = B
               ON SIZE ERROR DISPLAY "11 size error"
               NOT ON SIZE ERROR DISPLAY "11 stored"
           END-COMPUTE.
           DISPLAY "11 [" FE "]".
           MOVE 1.5 TO FE2.
           COMPUTE FE2 ROUNDED = BIG
               ON SIZE ERROR DISPLAY "12 size error"
               NOT ON SIZE ERROR DISPLAY "12 stored"
           END-COMPUTE.
           DISPLAY "12 [" FE2 "]".
           COMPUTE FE2 = BIG
               ON SIZE ERROR DISPLAY "13 size error"
               NOT ON SIZE ERROR DISPLAY "13 stored"
           END-COMPUTE.
           DISPLAY "13 [" FE2 "]".
           COMPUTE FE ROUNDED = 1 / HUGE.
           DISPLAY "15 [" FE "]".
           MOVE 1.234 TO A.
           COMPUTE FE ROUNDED MODE IS PROHIBITED = A
               ON SIZE ERROR
                   DISPLAY "14 EC=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE.
           STOP RUN.
