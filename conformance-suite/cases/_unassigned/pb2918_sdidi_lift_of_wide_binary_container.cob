*>--- conformance-case
*> id: pb2918_sdidi_lift_of_wide_binary_container
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2918 - a 16-byte binary container value past the
      *>   standard-decimal intermediate data item's 34 digits.
      *>
      *>   8.8.1.5.2 r1: "Any operand of an arithmetic expression that is
      *>   not already in SDIDI form is converted into this form. If the
      *>   operand value cannot be expressed exactly in an SDIDI, the
      *>   value is rounded according to the rules in 11.9.11,
      *>   INTERMEDIATE ROUNDING clause." (cite.py --check 8.8.1.5.2
      *>   "If the operand value cannot be expressed exactly in an SDIDI,
      *>   the value is rounded according to the rules in 11.9.11" -> OK
      *>   8.8.1.5.2 1)). The default INTERMEDIATE ROUNDING mode is
      *>   NEAREST-AWAY-FROM-ZERO (11.9.11).
      *>
      *>   S PIC S9(19) COMP-5 holds its whole 16-byte container
      *>   (13.18.60.4 GR12 "The implementor may allow a wider range" ->
      *>   cite.py OK 13.18.60.4 12)): here -2**127 =
      *>   -170141183460469231731687303715884105728, 39 digits.  As an
      *>   operand it is the SDIDI -1.701411834604692317316873037158841E+38
      *>   = -170141183460469231731687303715884100000 (the 5 dropped digits
      *>   05728 begin with 0, so the nearest value is below in magnitude).
      *>   T = 3 * 2**124 = 63802943797675961899382738893456539648 drops
      *>   9648, round digit 9, so NEAREST-AWAY gives
      *>   63802943797675961899382738893456540000.
      *>
      *>   14.7.5: the size error condition belongs to the RESULT.  S + 1
      *>   is the SDIDI -170141183460469231731687303715884100000 (34
      *>   digits, rounded again), which is inside the container of R
      *>   (-2**127 .. 2**127-1), so it is stored and NOT ON SIZE ERROR
      *>   runs.  R shows its low 19 digits.  Before the fix the lift put
      *>   the 39-digit significand into the intermediate unrounded and
      *>   the addition reported a size error.
      *>   D = R - S: both operands round to the same SDIDI, so 0.
      *>   N PIC S9(31) cannot hold a 39-digit result: size error, N
      *>   unchanged (14.7.5 case 3 and storing rule 2).
      *>   U = 2**128-1 PIC 9(19) COMP-5 (unsigned container) rounds to
      *>   340282366920938463463374607431768200000 and is greater than
      *>   A + B = 2 (8.8.4.2.4: relation conditions compare algebraic
      *>   values) - a relation is not arithmetic, so it never needs the
      *>   Int128 intermediate the unsigned operand cannot enter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2918A.
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
       01 V.
          05 U PIC 9(19) COMP-5.
       01 VX REDEFINES V PIC X(16).
       01 R PIC S9(19) COMP-5 VALUE 7.
       01 D PIC S9(5) VALUE 99.
       01 N PIC S9(31) VALUE 7.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"80000000000000000000000000000000" TO GX.
           MOVE X"30000000000000000000000000000000" TO HX.
           MOVE X"FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" TO VX.
           COMPUTE R = S + 1
               ON SIZE ERROR DISPLAY "S+1 size error"
               NOT ON SIZE ERROR DISPLAY "S+1 R " R
           END-COMPUTE.
           COMPUTE D = R - S
               ON SIZE ERROR DISPLAY "R-S size error"
               NOT ON SIZE ERROR DISPLAY "R-S D " D
           END-COMPUTE.
           COMPUTE R = T + 0
               ON SIZE ERROR DISPLAY "T+0 size error"
               NOT ON SIZE ERROR DISPLAY "T+0 R " R
           END-COMPUTE.
           COMPUTE R = S - 1
               ON SIZE ERROR DISPLAY "S-1 size error"
               NOT ON SIZE ERROR DISPLAY "S-1 R " R
           END-COMPUTE.
           COMPUTE N = S + 1
               ON SIZE ERROR DISPLAY "N size error"
               NOT ON SIZE ERROR DISPLAY "N " N
           END-COMPUTE.
           IF U > A + B DISPLAY "U > A+B" ELSE DISPLAY "U not > A+B"
           END-IF.
           STOP RUN.
