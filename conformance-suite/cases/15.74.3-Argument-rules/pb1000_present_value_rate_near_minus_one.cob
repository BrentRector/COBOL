      *>--- conformance-case
      *> id: pb1000_present_value_rate_near_minus_one
      *> rules: [AR-15.74.3-2, RV-15.74.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1000 - PRESENT-VALUE of a LEGAL rate whose binary64 image is the ILLEGAL -1.
      *> 15.74.3 r2: "The value of argument-1 shall be greater than -1." -0.99999999999999999 (17 nines,
      *> the closest a COBOL-85 18-digit item gets) IS greater than -1, so the function has a value, and
      *> 15.74.4 r1 a)'s equivalent arithmetic expression is (argument-2 / (1 + argument-1)); under
      *> native arithmetic 15.4.1 makes the returned value "an implementor-defined approximation of the
      *> value of that expression". Here 1 + argument-1 = 1E-17 and the one amount is 1E-15, so the value
      *> is 1E-15 / 1E-17 = 100, which ROUNDED into four places is 100.0000 for any approximation within
      *> 5E-5 of it - the witness observes the value, never this implementation's own digits.
      *>   SCALAR  = 100.0000   PRESENT-VALUE(NEAR-M1 AMT)
      *>   ALL     = 100.0000   PRESENT-VALUE(R(ALL)) - the rate is the table's FIRST implicit element
      *>                        (15.3 r14: "The order of the implicit specification of each
      *>                        occurrence is from left to right")
      *>   ALLTAIL = 100.0000   PRESENT-VALUE(Q(ALL) AMT) - a one-element ALL lead, then a written amount
      *> The binary64 nearest -0.99999999999999999 is -1.0, so the discount base 1 + rate formed from the
      *> NARROWED rate was 0: every arm divided by zero and answered 000.0000 (the non-finite result's
      *> EC-ARGUMENT-FUNCTION default; with checking on, the run ended), and the table(ALL) arm's domain
      *> screen - which read that binary64 too - refused the rate before that ("argument-1 shall be
      *> greater than -1"). The screen now reads the element's exact value, and the base is formed on the
      *> exact rate before it becomes a binary64.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1000PVNEAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEAR-M1  PIC S9V9(17) VALUE -0.99999999999999999.
       01 AMT      PIC 9V9(17) VALUE 0.000000000000001.
       01 T.
          05 R     PIC S9V9(17) OCCURS 2 TIMES.
       01 U.
          05 Q     PIC S9V9(17) OCCURS 1 TIMES.
       01 W-P      PIC 9(3).9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE W-P ROUNDED = FUNCTION PRESENT-VALUE(NEAR-M1 AMT)
           DISPLAY "SCALAR=" W-P
           MOVE NEAR-M1 TO R(1)
           MOVE AMT TO R(2)
           COMPUTE W-P ROUNDED = FUNCTION PRESENT-VALUE(R(ALL))
           DISPLAY "ALL=" W-P
           MOVE NEAR-M1 TO Q(1)
           COMPUTE W-P ROUNDED = FUNCTION PRESENT-VALUE(Q(ALL) AMT)
           DISPLAY "ALLTAIL=" W-P
           STOP RUN.
