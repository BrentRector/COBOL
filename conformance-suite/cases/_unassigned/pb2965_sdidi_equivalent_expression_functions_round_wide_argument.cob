*>--- conformance-case
*> id: pb2965_sdidi_equivalent_expression_functions_round_wide_argument
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2965 - the functions whose returned value is an
      *>   equivalent arithmetic expression read a wide binary
      *>   container argument as an SDIDI operand, which ROUNDS it.
      *>
      *>   15.4.1: "When standard-decimal arithmetic or standard-binary
      *>   arithmetic is in effect and an equivalent arithmetic
      *>   expression is specified: the returned value shall equal the
      *>   value of the equivalent arithmetic expression."
      *>   (cite.py --check 15.4.1 "the returned value shall equal the
      *>   value of the equivalent arithmetic expression" -> OK
      *>   15.4.1 1)).
      *>   8.8.1.5.2 r1: "Any operand of an arithmetic expression that
      *>   is not already in SDIDI form is converted into this form. If
      *>   the operand value cannot be expressed exactly in an SDIDI,
      *>   the value is rounded according to the rules in 11.9.11,
      *>   INTERMEDIATE ROUNDING clause." (cite.py OK 8.8.1.5.2 1)).
      *>   The default INTERMEDIATE ROUNDING mode is NEAREST-AWAY-FROM-
      *>   ZERO.  The SDIDI keeps 34 digits.
      *>
      *>   ABS 15.7.4 r1 (b): the equivalent expression is
      *>   (- (argument-1)) for a negative argument.  SIGN 15.81.4 r1:
      *>   (1), (0) or (-1).  INTEGER-PART 15.49.4 r1: (FUNCTION SIGN
      *>   (argument-1) * FUNCTION INTEGER (FUNCTION ABS (argument-1))).
      *>   FRACTION-PART 15.42.4 r1: (argument-1 - FUNCTION INTEGER-PART
      *>   (argument-1)).  All four cite.py --check OK.
      *>
      *>   S PIC S9(19) COMP-5 holds its whole 16-byte container
      *>   (13.18.60.4 GR12), here -2**127 =
      *>   -170141183460469231731687303715884105728 (39 digits).  As an
      *>   operand it is the SDIDI -170141183460469231731687303715884100000
      *>   (the dropped digits 05728 begin with 0).
      *>   ABS(S) = 170141183460469231731687303715884100000, which the
      *>   container of R (up to 2**127-1 =
      *>   170141183460469231731687303715884105727) holds, so R shows its
      *>   low 19 digits 1687303715884100000 and no size error is set.
      *>   Before the fix the exact Int128 body took |-2**127| and
      *>   reported a size error.  D = R + S: both operands round to
      *>   +/-...100000, so 0.  N PIC S9(31) cannot hold the 39-digit
      *>   result: the size error is the RESULT's (14.7.5).
      *>   T = 3 * 2**124 = 63802943797675961899382738893456539648 drops
      *>   9648, round digit 9, so ABS(T) is
      *>   63802943797675961899382738893456540000: the low 19 digits are
      *>   9382738893456540000, not the exact 9382738893456539648.
      *>   INTEGER-PART(S) is -...100000: low 19 digits -1687303715884100000.
      *>   SIGN(S) is -1 and SIGN(T) is 1 whatever the rounding.
      *>   V PIC S9(22)V9(9) COMP-5 holds -2**127 at scale 9, the value
      *>   -170141183460469231731687303715.884105728; as an SDIDI operand it
      *>   is -170141183460469231731687303715.8841 (34 digits), so
      *>   FRACTION-PART(V) = V - INTEGER-PART(V) = -0.8841, not the
      *>   exact -0.884105728.
      *>
      *>   INTEGER 15.44.4 r1 has NO equivalent arithmetic expression:
      *>   "The returned value is the greatest integer less than or equal
      *>   to the value of argument-1" (cite.py OK 15.44.4 1)), and MAX /
      *>   MIN 15.59.4 r1 return "the content of the argument-1 having the
      *>   greatest value" (cite.py OK 15.59.4 1)), so the value of a data
      *>   item argument is the item's own: INTEGER(S) and MIN(S, 1)
      *>   stay -2**127 (low 19 digits -1687303715884105728) and
      *>   MAX(S, 1) is 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2965A.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 S PIC S9(19) COMP-5.
       01 GX REDEFINES G PIC X(16).
       01 H.
          05 T PIC S9(19) COMP-5.
       01 HX REDEFINES H PIC X(16).
       01 K.
          05 V PIC S9(22)V9(9) COMP-5.
       01 KX REDEFINES K PIC X(16).
       01 R PIC S9(19) COMP-5 VALUE 7.
       01 Q PIC 9(19) COMP-5 VALUE 7.
       01 D PIC S9(5) VALUE 99.
       01 F PIC S9V9(9) VALUE 7.
       01 N PIC S9(31) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"80000000000000000000000000000000" TO GX.
           MOVE X"30000000000000000000000000000000" TO HX.
           MOVE X"80000000000000000000000000000000" TO KX.
           COMPUTE R = FUNCTION ABS(S)
               ON SIZE ERROR DISPLAY "ABS(S) size error"
               NOT ON SIZE ERROR DISPLAY "ABS(S) R " R
           END-COMPUTE.
           COMPUTE D = R + S
               ON SIZE ERROR DISPLAY "R+S size error"
               NOT ON SIZE ERROR DISPLAY "R+S D " D
           END-COMPUTE.
           COMPUTE N = FUNCTION ABS(S)
               ON SIZE ERROR DISPLAY "ABS(S) into N size error"
               NOT ON SIZE ERROR DISPLAY "ABS(S) N " N
           END-COMPUTE.
           COMPUTE Q = FUNCTION ABS(T)
               ON SIZE ERROR DISPLAY "ABS(T) size error"
               NOT ON SIZE ERROR DISPLAY "ABS(T) Q " Q
           END-COMPUTE.
           COMPUTE D = FUNCTION SIGN(S)
               ON SIZE ERROR DISPLAY "SIGN(S) size error"
               NOT ON SIZE ERROR DISPLAY "SIGN(S) D " D
           END-COMPUTE.
           COMPUTE D = FUNCTION SIGN(T)
               ON SIZE ERROR DISPLAY "SIGN(T) size error"
               NOT ON SIZE ERROR DISPLAY "SIGN(T) D " D
           END-COMPUTE.
           COMPUTE R = FUNCTION INTEGER-PART(S)
               ON SIZE ERROR DISPLAY "INTEGER-PART(S) size error"
               NOT ON SIZE ERROR DISPLAY "INTEGER-PART(S) R " R
           END-COMPUTE.
           COMPUTE F = FUNCTION FRACTION-PART(V)
               ON SIZE ERROR DISPLAY "FRACTION-PART(V) size error"
               NOT ON SIZE ERROR DISPLAY "FRACTION-PART(V) F " F
           END-COMPUTE.
           COMPUTE R = FUNCTION INTEGER(S)
               ON SIZE ERROR DISPLAY "INTEGER(S) size error"
               NOT ON SIZE ERROR DISPLAY "INTEGER(S) R " R
           END-COMPUTE.
           COMPUTE R = FUNCTION MIN(S, 1)
               ON SIZE ERROR DISPLAY "MIN(S,1) size error"
               NOT ON SIZE ERROR DISPLAY "MIN(S,1) R " R
           END-COMPUTE.
           COMPUTE R = FUNCTION MAX(S, 1)
               ON SIZE ERROR DISPLAY "MAX(S,1) size error"
               NOT ON SIZE ERROR DISPLAY "MAX(S,1) R " R
           END-COMPUTE.
           STOP RUN.
