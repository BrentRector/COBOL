      *> kb/Work PB1143 (train review finding N1) - 14.9.26.4 GR1/GR2: "The product of the multiplier and the
      *> multiplicand is stored as the new value of the data item referenced by identifier-2", and 14.7.4.3 applies
      *> the ROUNDED phrase to that ONE transfer: GR2 "If the ROUNDED phrase is not specified, execution is as if
      *> ROUNDED MODE IS TRUNCATION", PROHIBITED an inexact result is the size error EC-SIZE-TRUNCATION.
      *> A 16-byte COMP-5 receiver owns a 38-digit container (13.18.60.4 GR12), so a product of two 20-digit
      *> operands (up to 40 digits, 38 here) can be held EXACTLY and every digit of it is the receiver's to round.
      *> The landing kept 34 digits of such a product, so PIC S9(31) COMP-5 stored ...5370000 for ...5361999 and
      *> ROUNDED MODE PROHIBITED raised nothing.  EVERY VALUE BELOW IS EXACT DECIMAL ARITHMETIC (hand-derived):
      *>   E = 12345678901234567891, F = 1234567890123456789:
      *>     E * F = 15241578753238836751425087877625361999   (38 digits)
      *>     DIV 10**7 = 1524157875323883675142508787762      MOD 10**7 = 5361999
      *>   P = 1234567890123456789.5, Q = 9876543210987654321.5 (PIC 9(19)V9: 20 digits each):
      *>     P * Q = 12193263113702179527930193561668190824.25   (scale 2; the tail .25 is below one half)
      *>       TRUNCATION, NEAREST-AWAY-FROM-ZERO, NEAREST-EVEN -> ...190824;  AWAY-FROM-ZERO -> ...190825;
      *>       PROHIBITED -> inexact: the size error, the receiver unchanged.
      *>   P * Q2, Q2 = 9876543210987654321.0 (the tail is exactly .50):
      *>     P * Q2 = 12193263113702179527312909616606462429.50
      *>       NEAREST-EVEN (429 is odd) -> ...462430; NEAREST-AWAY -> ...462430; NEAREST-TOWARD-ZERO -> ...462429;
      *>       TOWARD-LESSER -> ...462429; TOWARD-GREATER -> ...462430.
      *>   G = H = 99999999999999999999: G * H is 40 digits, past what ANY receiver holds: the size error.
      *> The receivers' digits are shown as HI = R / 10**7 and LO = R mod 10**7 (a COMP-5 value past its PICTURE
      *> would display its low 31 digits only).  Every statement is a single-receiver product (the final transfer) or
      *> MULTIPLY ... GIVING / COMPUTE with several receivers, each rounded at its own scale and mode.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1143WR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E  PIC 9(20) VALUE 12345678901234567891.
       01 F  PIC 9(20) VALUE 1234567890123456789.
       01 G  PIC 9(20) VALUE 99999999999999999999.
       01 H  PIC 9(20) VALUE 99999999999999999999.
       01 P  PIC 9(19)V9 VALUE 1234567890123456789.5.
       01 Q  PIC 9(19)V9 VALUE 9876543210987654321.5.
       01 Q2 PIC 9(19)V9 VALUE 9876543210987654321.0.
       01 R  PIC S9(31) COMP-5 VALUE 7.
       01 R2 PIC S9(31) COMP-5 VALUE 7.
       01 E5 PIC S9(31) COMP-5 VALUE 12345678901234567891.
       01 HI PIC 9(31).
       01 LO PIC 9(7).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = E * F
           PERFORM SHOW
           DISPLAY "T1 COMPUTE"
           MOVE 7 TO R
           MULTIPLY E BY F GIVING R
           PERFORM SHOW
           DISPLAY "T2 MULTIPLY GIVING"
           MOVE 7 TO R
           MULTIPLY E BY F GIVING R R2
           PERFORM SHOW
           MOVE R2 TO R
           PERFORM SHOW
           DISPLAY "T3 MULTIPLY GIVING two receivers"
           MULTIPLY F BY E5
           MOVE E5 TO R
           PERFORM SHOW
           DISPLAY "T4 MULTIPLY BY"
           MOVE 7 TO R
           MOVE 7 TO R2
           COMPUTE R R2 = E * F
           PERFORM SHOW
           MOVE R2 TO R
           PERFORM SHOW
           DISPLAY "T5 COMPUTE two receivers"
           COMPUTE R = P * Q
           PERFORM SHOW
           DISPLAY "T6 truncation"
           COMPUTE R ROUNDED = P * Q
           PERFORM SHOW
           DISPLAY "T7 rounded (nearest away, .25)"
           COMPUTE R ROUNDED MODE IS AWAY-FROM-ZERO = P * Q
           PERFORM SHOW
           DISPLAY "T8 away from zero (.25)"
           MOVE 7 TO R
           COMPUTE R ROUNDED MODE IS PROHIBITED = P * Q
               ON SIZE ERROR DISPLAY "T9 prohibited: size error"
               NOT ON SIZE ERROR DISPLAY "T9 prohibited: no size error"
           END-COMPUTE
           DISPLAY "T9 R still 7: " R
           COMPUTE R ROUNDED MODE IS NEAREST-EVEN = P * Q2
           PERFORM SHOW
           DISPLAY "T10 nearest even (.50, 429 odd)"
           COMPUTE R ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO = P * Q2
           PERFORM SHOW
           DISPLAY "T11 nearest away (.50)"
           COMPUTE R ROUNDED MODE IS NEAREST-TOWARD-ZERO = P * Q2
           PERFORM SHOW
           DISPLAY "T12 nearest toward zero (.50)"
           COMPUTE R ROUNDED MODE IS TOWARD-LESSER = P * Q2
           PERFORM SHOW
           DISPLAY "T13 toward lesser (.50)"
           COMPUTE R ROUNDED MODE IS TOWARD-GREATER = P * Q2
           PERFORM SHOW
           DISPLAY "T14 toward greater (.50)"
           MOVE 7 TO R
           COMPUTE R = G * H
               ON SIZE ERROR DISPLAY "T15 size error"
               NOT ON SIZE ERROR DISPLAY "T15 no size error"
           END-COMPUTE
           DISPLAY "T15 R still 7: " R
           STOP RUN.
       SHOW.
           COMPUTE HI = R / 10000000
           COMPUTE LO = FUNCTION MOD(R, 10000000)
           DISPLAY "HI=" HI " LO=" LO.
