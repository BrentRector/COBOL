*>--- conformance-case
*> id: pb3017_sdidi_divide_remainder_beyond_the_int128_carrier
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3017 - DIVIDE ... GIVING ... REMAINDER under ARITHMETIC IS
      *>   STANDARD-DECIMAL forms its quotient, its subsidiary quotient and
      *>   its remainder in the standard-decimal intermediate data item
      *>   (SDIDI), so an operand the 128-bit integer carrier cannot hold
      *>   is no size error.
      *>
      *>   14.9.12.4 1): "When standard-decimal arithmetic ... is in effect,
      *>   the quotient is the result of the arithmetic expression
      *>   (dividend / divisor)" (cite.py --check 14.9.12.4 "the quotient
      *>   is the result of the arithmetic expression" -> OK 14.9.12.4 1)).
      *>   14.9.12.4 6) c): "a subsidiary quotient is developed that is
      *>   signed and derived from the quotient by truncation of digits at
      *>   the least significant end and that has the same number of
      *>   digits and the same decimal point location as the data item
      *>   referenced by identifier-3" (cite.py --check 14.9.12.4 "derived
      *>   from the quotient by truncation of digits at the least
      *>   significant end" -> OK 14.9.12.4 6)).
      *>   14.9.12.4 7): "the remainder is the result of the arithmetic
      *>   expression (dividend - (subsidiary-quotient * divisor))"
      *>   (cite.py --check 14.9.12.4 "(dividend - (subsidiary-quotient"
      *>   -> OK 14.9.12.4 7)).
      *>   8.8.1.5.2 1): "If the operand value cannot be expressed exactly
      *>   in an SDIDI, the value is rounded according to the rules in
      *>   11.9.11" (cite.py --check 8.8.1.5.2 -> OK 8.8.1.5.2 1)).
      *>   A 16-byte binary item owns its whole container (13.18.60.4 12),
      *>   cite.py OK): the subsidiary quotient keeps every digit it holds.
      *>
      *>   Derived by hand (34-digit significands, nearest rounding):
      *>   W = 2**128 - 1 = 340282366920938463463374607431768211455 is the
      *>   SDIDI 340282366920938463463374607431768200000 (the five dropped
      *>   digits 11455 begin with 1).
      *>   W / 7 = 48611766702991209066196372490252600000 exactly (34
      *>   digits 4861176670299120906619637249025260, exponent 4), an
      *>   integer, so the subsidiary quotient is that and 7 times it is W
      *>   again: the remainder is 0.  Q (PIC 9(19) COMP-5) shows its LOW
      *>   19 digits; QDD is Q / 10**11 truncated, its HIGH 27 digits.
      *>   W / 10000000000 = 34028236692093846346337460743.17682 exactly
      *>   (the SDIDI 3402823669209384634633746074317682, exponent -5):
      *>   Q = 34028236692093846346337460743 (the fraction is truncated,
      *>   no ROUNDED), subsidiary quotient the same, and the remainder is
      *>   W - Q * 10**10 = 340282366920938463463374607431768200000
      *>   - 340282366920938463463374607430000000000 = 1768200000.
      *>   F = 1.0E+30 (FLOAT-LONG) converts to the SDIDI 1E+30 exactly
      *>   (8.8.1.5.1 shortest round-trip decimal): 1E30 / 3 =
      *>   333333333333333333333333333333.3333333333333333333333333333333
      *>   rounds to 34 digits, Q = 333333333333333333333333333333 (30
      *>   digits), remainder = 1E30 - 3 * Q = 1.
      *>   F = 1.0E+40 / 7 = 1.428571428571428571428571428571429E+39 is
      *>   past 2**128 - 1 (about 3.4E+38): the quotient's size error, Q
      *>   and R unchanged (14.7.5 rule 1 - the resultants keep their
      *>   values).
      *>   Small operands on the same lane: the subsidiary quotient is the
      *>   TRUNCATED quotient even when the stored quotient is ROUNDED
      *>   (6) c) "derived from the quotient by truncation of digits at the
      *>   least significant end").  7 / 2 = 3.5: Q ROUNDED = 4, the
      *>   subsidiary quotient 3, R = 7 - 3 * 2 = 1.  -7 / 2 = -3.5: Q
      *>   ROUNDED = -4 (nearest away from zero), the subsidiary quotient
      *>   -3 (toward zero), R = -7 - (-3 * 2) = -1; without ROUNDED Q =
      *>   -3, R = -1.  10 / 3 into PIC S9(5)V9: Q = 3.3, subsidiary
      *>   quotient 3.3, R = 10 - 3.3 * 3 = 0.1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3017A.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V.
          05 W PIC 9(19) COMP-5.
       01 VX REDEFINES V PIC X(16).
       01 TEN10 PIC 9(11) VALUE 10000000000.
       01 F1 COMP-2 VALUE 1.0E+30.
       01 F2 COMP-2 VALUE 1.0E+40.
       01 Q PIC 9(19) COMP-5 VALUE 7.
       01 R PIC S9(19) COMP-5 VALUE 7.
       01 QS PIC S9(31) COMP-5 VALUE 7.
       01 QDD PIC 9(27).
       01 NEG PIC S9(5) SIGN IS LEADING SEPARATE VALUE -7.
       01 QI PIC S9(5) SIGN IS LEADING SEPARATE VALUE 0.
       01 RI PIC S9(5) SIGN IS LEADING SEPARATE VALUE 0.
       01 QFR PIC S9(5)V9 SIGN IS LEADING SEPARATE VALUE 0.
       01 RFR PIC S9(5)V9 SIGN IS LEADING SEPARATE VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" TO VX.
           DIVIDE W BY 7 GIVING Q REMAINDER R
               ON SIZE ERROR DISPLAY "W/7 size error"
               NOT ON SIZE ERROR DISPLAY "W/7 ok"
           END-DIVIDE.
           PERFORM SHOWQ.
           DIVIDE W BY TEN10 GIVING Q REMAINDER R
               ON SIZE ERROR DISPLAY "W/10**10 size error"
               NOT ON SIZE ERROR DISPLAY "W/10**10 ok"
           END-DIVIDE.
           PERFORM SHOWQ.
           MOVE 7 TO Q R.
           DIVIDE W BY 7 GIVING Q REMAINDER R.
           PERFORM SHOWQ.
           DIVIDE F1 BY 3 GIVING QS REMAINDER R
               ON SIZE ERROR DISPLAY "1E30/3 size error"
               NOT ON SIZE ERROR DISPLAY "1E30/3 ok"
           END-DIVIDE.
           DISPLAY "  QS " QS " R " R.
           MOVE 7 TO Q R.
           DIVIDE F2 BY 7 GIVING Q REMAINDER R
               ON SIZE ERROR DISPLAY "1E40/7 size error"
               NOT ON SIZE ERROR DISPLAY "1E40/7 ok"
           END-DIVIDE.
           PERFORM SHOWQ.
           DIVIDE 7 BY 2 GIVING QI ROUNDED REMAINDER RI.
           DISPLAY "7/2 rounded: Q " QI " R " RI.
           DIVIDE NEG BY 2 GIVING QI ROUNDED REMAINDER RI.
           DISPLAY "-7/2 rounded: Q " QI " R " RI.
           DIVIDE NEG BY 2 GIVING QI REMAINDER RI.
           DISPLAY "-7/2 truncated: Q " QI " R " RI.
           DIVIDE 10 BY 3 GIVING QFR REMAINDER RFR.
           DISPLAY "10/3 scale 1: Q " QFR " R " RFR.
           STOP RUN.
       SHOWQ.
           COMPUTE QDD = Q / 100000000000.
           DISPLAY "  Q " Q " high " QDD " R " R.
