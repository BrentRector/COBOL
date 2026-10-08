      *> kb/Work PB1900 - a native product past the Int128 carrier NESTED in an arithmetic expression keeps every digit
      *>   until the one final transfer.  8.8.1.3: "Native arithmetic is an implementor-defined method of evaluating an
      *>   arithmetic expression"; the implementor's choice follows GnuCOBOL (CLAUDE.md rule 1), whose cob_decimal_mul /
      *>   cob_decimal_sub are exact over an arbitrary-precision integer.  14.7.4.3 / 14.7.7 rule 3 NOTE 1: ROUNDED governs only
      *>   the final transfer to the resultant.  The operands are PIC 9(21) (31 digits is the 2002 limit, 13.18.40.3
      *>   SR14), so each product is 41 digits - past the 38 an Int128 holds - and the two nearly equal products cancel:
      *>   - A = 10**20+1, B = 10**20+1, C = 10**20, D = 10**20+2: A*B = 10**40+2*10**20+1, C*D = 10**40+2*10**20,
      *>     so A*B - C*D = 1 EXACTLY.  A 34-digit intermediate left only its own rounding error (stored 10000000).
      *>   - P = 10**20+1, Q = 10**20+3, S = T = 10**20+2: P*Q = 10**40+4*10**20+3 and S*T = 10**40+4*10**20+4 differ
      *>     only in their LAST digit, below any 34-digit reduction, so the relations P*Q = S*T (FALSE), P*Q < S*T (TRUE)
      *>     are decided by the exact products (8.8.4.2.4 compares algebraic values).
      *>   - the scaled pair (PIC 9(11)V9(10), scale 10 each, products at scale 20): U = 10**10+10**-10,
      *>     V = 10**10+3*10**-10, W = X = 10**10+2*10**-10: U*V - W*X = (10**20 + 4 + 3e-20) - (10**20 + 4 + 4e-20)
      *>     = -1e-20 EXACTLY; into PIC S9V9(20) that is -0.00000000000000000001, and ROUNDED
      *>     (the 2002 default NEAREST-AWAY-FROM-ZERO, 14.7.4.3) into PIC S9V9(19) is zero, +0.0000000000000000000.
      *>   - a SEVERAL-RECEIVER COMPUTE has ONE initial evaluation (14.7.7 GR4) that every receiver rounds for itself.
      *>   cite.py --check 8.8.1.3 "Native arithmetic is an implementor-defined method of evaluating an arithmetic expression"
      *>     -> OK 8.8.1.3
      *>   cite.py --check 14.7.7 "the composite of operands shall not contain more than 31 digits" -> OK 14.7.7 2) a)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1900G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC 9(21) VALUE 100000000000000000001.
       01 B  PIC 9(21) VALUE 100000000000000000001.
       01 C  PIC 9(21) VALUE 100000000000000000000.
       01 D  PIC 9(21) VALUE 100000000000000000002.
       01 P  PIC 9(21) VALUE 100000000000000000001.
       01 Q  PIC 9(21) VALUE 100000000000000000003.
       01 S  PIC 9(21) VALUE 100000000000000000002.
       01 T  PIC 9(21) VALUE 100000000000000000002.
       01 U  PIC 9(11)V9(10) VALUE 10000000000.0000000001.
       01 V  PIC 9(11)V9(10) VALUE 10000000000.0000000003.
       01 W  PIC 9(11)V9(10) VALUE 10000000000.0000000002.
       01 X  PIC 9(11)V9(10) VALUE 10000000000.0000000002.
       01 R  PIC 9(9).
       01 RS PIC S9(3).
       01 R1 PIC 9(9).
       01 R2 PIC 9(3)V9.
       01 ERS PIC +9(3).
       01 F20 PIC S9V9(20).
       01 F19 PIC S9V9(19).
       01 E20 PIC +9.9(20).
       01 E19 PIC +9.9(19).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = A * B - C * D
           DISPLAY "DIFFERENCE=" R
           COMPUTE RS = C * D - A * B
           MOVE RS TO ERS
           DISPLAY "NEGATED=" ERS
           COMPUTE RS = -(A * B) + C * D + 5
           MOVE RS TO ERS
           DISPLAY "UNARY=" ERS
           COMPUTE R1 R2 = A * B - C * D
           DISPLAY "SEVERAL=" R1 " " R2
           IF P * Q = S * T
              DISPLAY "EQUAL-WRONG"
           ELSE
              DISPLAY "NOT-EQUAL"
           END-IF
           IF P * Q < S * T
              DISPLAY "LESS"
           ELSE
              DISPLAY "LESS-MISSED"
           END-IF
           COMPUTE F20 = U * V - W * X
           MOVE F20 TO E20
           DISPLAY "SCALED=" E20
           COMPUTE F19 ROUNDED = U * V - W * X
           MOVE F19 TO E19
           DISPLAY "ROUNDED=" E19
           IF A * B - C * D = 1
              DISPLAY "EQUAL-ONE"
           ELSE
              DISPLAY "EQUAL-ONE-MISSED"
           END-IF
           EVALUATE A * B - C * D
              WHEN 1 DISPLAY "WHEN-ONE"
              WHEN OTHER DISPLAY "WHEN-OTHER"
           END-EVALUATE
      *> The exact A*B - C is 10**40 + 10**20 + 1 - far past any receiver: a
      *> REAL size error (14.7.5 case 3), the receiver unchanged.
           MOVE 7 TO R
           COMPUTE R = A * B - C
              ON SIZE ERROR DISPLAY "SIZE-ERROR"
              NOT ON SIZE ERROR DISPLAY "SIZE-ERROR-MISSED"
           END-COMPUTE
           DISPLAY "UNCHANGED=" R
           STOP RUN.
