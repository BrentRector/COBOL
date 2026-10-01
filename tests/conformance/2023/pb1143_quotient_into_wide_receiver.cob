      *> kb/Work PB1143 (train review finding N1, the DIVIDE sibling) - 14.9.12.4 stores the quotient in the receiver
      *> and 14.7.4.3 applies the ROUNDED phrase to that ONE transfer.  A divisor with a scale (PIC 9V9(30) holding 4)
      *> makes the radix alignment multiply the dividend by 10**30, past the Int128 carrier, although the QUOTIENT of a
      *> 38-digit dividend by 4 is itself 38 digits and a 16-byte COMP-5 receiver holds it whole.  The alignment was
      *> formed on the SDIDI, which keeps 34 digits, so the last four digits of the quotient were lost and ROUNDED saw
      *> a tail that was no longer the true one.  EXACT DECIMAL ARITHMETIC, hand-derived:
      *>   A = 12345678901234567890123456789012345678  (38 digits: X = 1234567890123456789012345678901, A = X*10**7 + 2345678)
      *>   A / 4 = 3086419725308641972530864197253086419.5   (an exact tie)
      *>     DIV 10**7 = 308641972530864197253086419725   MOD 10**7 = 3086419 (truncated) / 3086420 (rounded up)
      *>     TRUNCATION, NEAREST-TOWARD-ZERO, TOWARD-LESSER -> ...419;  NEAREST-AWAY, NEAREST-EVEN (419 is odd),
      *>     AWAY-FROM-ZERO, TOWARD-GREATER -> ...420;  PROHIBITED -> inexact: the size error, the receiver unchanged.
      *>   A / 1 = A (the quotient is the dividend itself, 38 digits): LO = 2345678.
      *> The receiver's digits are shown as HI = R / 10**7 and LO = R mod 10**7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1143QW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC 9(31) VALUE 1234567890123456789012345678901.
       01 A  PIC S9(31) COMP-5.
       01 B1 PIC 9V9(30) VALUE 1.
       01 B4 PIC 9V9(30) VALUE 4.
       01 R  PIC S9(31) COMP-5 VALUE 7.
       01 HI PIC 9(31).
       01 LO PIC 9(7).
       PROCEDURE DIVISION.
       MAIN.
           MOVE X TO A
           MULTIPLY 10000000 BY A
           ADD 2345678 TO A
           COMPUTE R = A / B1
           PERFORM SHOW
           DISPLAY "T1 A / 1"
           COMPUTE R = A / B4
           PERFORM SHOW
           DISPLAY "T2 truncation (.5)"
           COMPUTE R ROUNDED = A / B4
           PERFORM SHOW
           DISPLAY "T3 nearest away (.5)"
           COMPUTE R ROUNDED MODE IS NEAREST-EVEN = A / B4
           PERFORM SHOW
           DISPLAY "T4 nearest even (.5, 419 odd)"
           COMPUTE R ROUNDED MODE IS NEAREST-TOWARD-ZERO = A / B4
           PERFORM SHOW
           DISPLAY "T5 nearest toward zero (.5)"
           COMPUTE R ROUNDED MODE IS AWAY-FROM-ZERO = A / B4
           PERFORM SHOW
           DISPLAY "T6 away from zero (.5)"
           COMPUTE R ROUNDED MODE IS TOWARD-LESSER = A / B4
           PERFORM SHOW
           DISPLAY "T7 toward lesser (.5)"
           COMPUTE R ROUNDED MODE IS TOWARD-GREATER = A / B4
           PERFORM SHOW
           DISPLAY "T8 toward greater (.5)"
           MOVE 7 TO R
           COMPUTE R ROUNDED MODE IS PROHIBITED = A / B4
               ON SIZE ERROR DISPLAY "T9 prohibited: size error"
               NOT ON SIZE ERROR DISPLAY "T9 prohibited: no size error"
           END-COMPUTE
           DISPLAY "T9 R still 7: " R
           STOP RUN.
       SHOW.
           COMPUTE HI = R / 10000000
           COMPUTE LO = FUNCTION MOD(R, 10000000)
           DISPLAY "HI=" HI " LO=" LO.
