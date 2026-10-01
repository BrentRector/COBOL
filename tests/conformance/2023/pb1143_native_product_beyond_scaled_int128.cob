      *> kb/Work PB1143 (rows GR-14.9.26.4-1, -2, -4) - 14.9.26.4 GR1/GR2: "The product of the multiplier and
      *>   the multiplicand is stored as the new value".  14.7.7 r2 a) limits the COMPOSITE of operands (their
      *>   superimposition aligned on the decimal point) to 31 digits - not the product of their UNSCALED
      *>   values: PIC 9V9(30) times PIC 9V9(30) is a legal statement (composite 1 + 30 = 31 digits) whose
      *>   unscaled operands are 31 digits each and whose scaled product is 61 digits.  The emitter multiplied
      *>   the two scaled values unchecked, so the product wrapped modulo 2**128 and a wrong value was stored
      *>   with no condition (measured: 1.5 * 2.25 stored 7.3...).  The exact products below:
      *>   - 1.5 * 2.25 = 3.375;  1.5 * 0.5 = 0.75;  (1.5 * 2.25) + 0.5 = 3.875;
      *>   - 9.5 * 9.5 = 90.25, which a PIC 9V9(29) receiver (ONE integer digit) cannot hold: a REAL size
      *>     error, taken through ON SIZE ERROR with the receiver unchanged (14.7.5 rule 1 / case 3) - the
      *>     control that proves the fix did not just stop raising;
      *>   - the DIVIDE sibling: the radix alignment of the dividend multiplies it by 10**exp, exp =
      *>     divisor scale + receiver scale - dividend scale = 20 + 10 - 0 = 30, so 1234567890 * 10**30 is
      *>     1.2e39 - past the carrier - while the quotient 1234567890 / 3.0 = 411522630 is tiny.
      *>   cite.py --check 14.9.26.4 "The product of the multiplier and the multiplicand is stored as the
      *>     new value of the data item referenced by identifier-2" -> OK 14.9.26.4 1)
      *>   cite.py --check 14.7.7 "the composite of operands shall not contain more than 31 digits"
      *>     -> OK 14.7.7 2) a)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1143PRD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC 9V9(30) VALUE 1.5.
       01 B  PIC 9V9(30) VALUE 0.5.
       01 C  PIC 9V9(30) VALUE 2.25.
       01 X  PIC 9V9(30) VALUE 9.5.
       01 R  PIC 9V9(29).
       01 E  PIC 9.9(5).
       01 D1 PIC 9(10) VALUE 1234567890.
       01 D2 PIC 9V9(20) VALUE 3.0.
       01 Q  PIC 9(10)V9(10).
       01 EQ PIC 9(10).9(2).
       01 A1 PIC S9V9(30) VALUE 1.000000000000000000000000000001.
       01 B1 PIC S9V9(30) VALUE 1.000000000000000000000000000001.
       01 N1 PIC S9V9(30) VALUE -1.000000000000000000000000000001.
       01 R30 PIC S9V9(30) VALUE 7.
       01 E30 PIC +9.9(30).
       PROCEDURE DIVISION.
       MAIN.
           MULTIPLY A BY C GIVING R
           MOVE R TO E
           DISPLAY "GIVING=" E
           COMPUTE R = A * C
           MOVE R TO E
           DISPLAY "COMPUTE=" E
           MULTIPLY A BY B
           MOVE B TO E
           DISPLAY "FORMAT-1=" E
           COMPUTE R = (A * C) + 0.5
           MOVE R TO E
           DISPLAY "NESTED=" E
           MULTIPLY A BY C GIVING R
               ON SIZE ERROR DISPLAY "SIZE-ERROR-UNEXPECTED"
               NOT ON SIZE ERROR
                   MOVE R TO E
                   DISPLAY "NO-SIZE-ERROR=" E
           END-MULTIPLY
           MOVE 1 TO R
           MULTIPLY X BY X GIVING R
               ON SIZE ERROR DISPLAY "SIZE-ERROR-95-TIMES-95"
               NOT ON SIZE ERROR DISPLAY "SIZE-ERROR-MISSED"
           END-MULTIPLY
           MOVE R TO E
           DISPLAY "RECEIVER-UNCHANGED=" E
           DIVIDE D1 BY D2 GIVING Q
           MOVE Q TO EQ
           DISPLAY "DIVIDE=" EQ
           COMPUTE Q = D1 / D2
           MOVE Q TO EQ
           DISPLAY "COMPUTE-DIVIDE=" EQ
      *> THE ROUNDED MODES SEE A TAIL PAST THE 34 DIGITS THE SDIDI KEEPS.  A1 x B1 is exactly
      *> 1 + 2e-30 + 1e-60 (61 digits).  Stored at scale 30 (14.7.4.3): no ROUNDED phrase is
      *> TRUNCATION, ...002; NEAREST-EVEN and NEAREST-AWAY-FROM-ZERO see a tail far below half,
      *> ...002; AWAY-FROM-ZERO and TOWARD-GREATER of a POSITIVE value go up on any nonzero
      *> tail, ...003.  For the NEGATIVE product (-1.000...001 x 1.000...001 = -(1 + 2e-30 +
      *> 1e-60)) AWAY-FROM-ZERO and TOWARD-LESSER give -...003 and TOWARD-GREATER gives -...002.
      *> A truncation to 34 digits would have hidden the 1e-60.  Expected values: exact product
      *> rounded once (Python fractions).
           COMPUTE R30 = A1 * B1
           MOVE R30 TO E30
           DISPLAY "TRUNCATION=" E30
           COMPUTE R30 ROUNDED MODE IS NEAREST-EVEN = A1 * B1
           MOVE R30 TO E30
           DISPLAY "NEAREST-EVEN=" E30
           COMPUTE R30 ROUNDED MODE IS AWAY-FROM-ZERO = A1 * B1
           MOVE R30 TO E30
           DISPLAY "AWAY-FROM-ZERO=" E30
           COMPUTE R30 ROUNDED MODE IS TOWARD-GREATER = A1 * B1
           MOVE R30 TO E30
           DISPLAY "TOWARD-GREATER=" E30
           COMPUTE R30 ROUNDED MODE IS AWAY-FROM-ZERO = N1 * B1
           MOVE R30 TO E30
           DISPLAY "NEGATIVE-AWAY=" E30
           COMPUTE R30 ROUNDED MODE IS TOWARD-GREATER = N1 * B1
           MOVE R30 TO E30
           DISPLAY "NEGATIVE-TOWARD-GREATER=" E30
      *> PROHIBITED (14.7.4.3 r7): the exact product cannot be represented at scale 30, so the
      *> size error condition exists and the receiver is unchanged.
           MOVE 1 TO R30
           MULTIPLY A1 BY B1 GIVING R30 ROUNDED MODE IS PROHIBITED
               ON SIZE ERROR DISPLAY "SIZE-ERROR-PROHIBITED"
               NOT ON SIZE ERROR DISPLAY "PROHIBITED-MISSED"
           END-MULTIPLY
           MOVE R30 TO E30
           DISPLAY "PROHIBITED-UNCHANGED=" E30
           STOP RUN.
