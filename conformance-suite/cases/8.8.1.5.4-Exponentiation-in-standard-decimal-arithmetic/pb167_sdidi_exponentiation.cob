*>--- conformance-case
*> id: pb167_sdidi_exponentiation
*> rules: [GR-8.8.1.5.4-2, GR-8.8.1.5.4-3, RV-15.4.1-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB167 - owner decision D-C (2026-08-30): FUND the SDIDI exponentiation development.
      *> 8.8.1.5.4 r2e leaves the equivalent arithmetic expression for a non-integer exponent to the
      *> implementor but binds the DEVELOPMENT - "Operands used in the development of that value shall be in
      *> SDIDI form.  All additions, subtractions, multiplications and divisions performed in the development
      *> of the result shall be performed in accordance with the corresponding rules in ISO/IEC 60559:2020."
      *> The development is now exp(operand-2 x ln operand-1) carried entirely on CobolDec (decimal128)
      *> operations - no binary64 bridge anywhere - with ONE special case:
      *>
      *>   |operand-2| = 1/2  ->  FUNCTION SQRT(operand-1), and r3's 1/(operand-1 ** 1/2) at -1/2.
      *>
      *> SQRT is the one 15 function whose standard-decimal returned value the standard fixes EXACTLY
      *> (15.84.4 r2: "the absolute value of the exact square root of argument-1 rounded to 34 digits
      *> according to the rules for standard-decimal arithmetic"), so choosing it as the r2e equivalent
      *> expression makes b ** 0.5 and FUNCTION SQRT(b) equal BY CONSTRUCTION - 15.4.1 r1 consistency
      *> rather than luck.  Before D-C both were binary64 approximations and the two DISAGREED.
      *>
      *> The pinned digits: the SQRT lines are the spec-exact value (15.84.4 r2), truncated by the receiver
      *> (14.7.4 - no ROUNDED phrase).  The transcendental lines show 21 significant digits of the true value;
      *> the development carries more than that but 8.8.1.5.2's 34-digit per-operation rounding means an
      *> r2e development can never promise all 34, so the goldens deliberately stop short of the last digits
      *> (CONFORMANCE.md 7 records the determination in those terms).
      *>
      *> r3 (a NEGATIVE operand-2) is NOT implementor latitude - r2's latitude is scoped "When the value of
      *> operand-2 is greater than zero", and r3 fixes the outer expression as a DIVISION:
      *>
      *>   operand-2 < 0  ->  (1 / (operand-1 ** FUNCTION ABS (operand-2)))
      *>
      *> The reciprocal is now written ONCE, in CobolDec.Pow, over |operand-2| - so every arm below it is
      *> r3-correct by construction rather than by which arm the operands happened to reach.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB167SDX.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R10 PIC 9V9(25).
       01 R2  PIC 9V9(25).
       01 Q1  PIC 9V9(20).
       01 Q2  PIC 9V9(20).
       01 Q3  PIC 9V9(20).
       01 Q4  PIC 9V9(20).
       01 N1  PIC S9V9(15).
       01 N2  PIC S9V9(15).
       01 DLT PIC 9V9(4).
       01 U1  PIC 9V9(13).
       PROCEDURE DIVISION.
      *> r2e at 1/2 IS FUNCTION SQRT - the identity, in both directions, and against EXP10 (15.35.4 r1:
      *> EXP10's equivalent arithmetic expression is (10 ** argument-1)).
           IF 10 ** 0.5 = FUNCTION SQRT(10)
             DISPLAY "SQRT10=EQ" ELSE DISPLAY "SQRT10=NE" END-IF.
           IF FUNCTION EXP10(0.5) = FUNCTION SQRT(10)
             DISPLAY "EXP10 =EQ" ELSE DISPLAY "EXP10 =NE" END-IF.
      *> ...and the function against its OWN equivalent arithmetic expression, directly.  15.4.1 NOTE 1 item 3
      *> permits an EAE's result to be implementor-defined when a COMPOSING expression is - here 8.8.1.5.4 r2e's
      *> non-integer power - and 15.4.1 NOTE 2 demands the permission deliver ONE value.  RV-15.4.1-3 was
      *> refuted to PARTIAL on the observation that no leg asserted this identity; it does now.
           IF FUNCTION EXP10(0.5) = 10 ** 0.5
             DISPLAY "EAE10 =EQ" ELSE DISPLAY "EAE10 =NE" END-IF.
           IF 2 ** 0.5 = FUNCTION SQRT(2)
             DISPLAY "SQRT2 =EQ" ELSE DISPLAY "SQRT2 =NE" END-IF.
      *> 8.8.1.5.4 r3 - a negative exponent is 1 / (operand-1 ** ABS(operand-2)).
           IF 2 ** -0.5 = 1 / FUNCTION SQRT(2)
             DISPLAY "RECIP =EQ" ELSE DISPLAY "RECIP =NE" END-IF.
           COMPUTE R10 = 10 ** 0.5.
           DISPLAY "R10   =" R10.
           COMPUTE R2 = 2 ** 0.5.
           DISPLAY "R2    =" R2.
      *> The general r2e development.
           COMPUTE Q1 = 2 ** 0.25.
           DISPLAY "Q1    =" Q1.
           COMPUTE Q2 = 7 ** 0.125.
           DISPLAY "Q2    =" Q2.
           COMPUTE Q3 = 0.5 ** 0.75.
           DISPLAY "Q3    =" Q3.
           COMPUTE Q4 = 2 ** -0.5.
           DISPLAY "Q4    =" Q4.
      *> The past-loop-bound INTEGER escape is the SAME r2e development (it used to be a second, different
      *> binary64 one).  A negative base keeps the sign its exponent's exact PARITY gives it - the old log
      *> decomposition took log10|b| and never restored it, so the odd case answered POSITIVE.
           COMPUTE N1 = (-1.0000001) ** 600001.
           DISPLAY "ODD   =" N1.
           COMPUTE N2 = (-1.0000001) ** 600002.
           DISPLAY "EVEN  =" N2.
      *> 8.8.1.5.4 r3 IS THE SAME CONSTRUCTION FOR EVERY NEGATIVE EXPONENT, not only -1/2 (kb/Work
      *> PB266).  r3's reciprocal used to be spelled in two of the four arms, so the general non-integer
      *> arm and the past-loop-bound integer escape carried the SIGN INSIDE the exp argument and answered
      *> something else: 2 ** -0.25 gave ...4762332146 where 1 / (2 ** 0.25) is ...4762332141.  A relation
      *> between two SDIDI intermediates compares them EXACTLY, so these four lines see the 34th digit.
           IF 2 ** -0.25 = 1 / (2 ** 0.25)
             DISPLAY "R3GEN =EQ" ELSE DISPLAY "R3GEN =NE" END-IF.
           IF 7 ** -0.125 = 1 / (7 ** 0.125)
             DISPLAY "R3PW7 =EQ" ELSE DISPLAY "R3PW7 =NE" END-IF.
      *> ...and on both sides of the 500000 loop bound, where r3 used to be DISCONTINUOUS - honoured
      *> below it by the square-and-multiply arm's own division, broken above it by the escape.
           IF 1.0000001 ** -600001 = 1 / (1.0000001 ** 600001)
             DISPLAY "R3FAR =EQ" ELSE DISPLAY "R3FAR =NE" END-IF.
           IF 1.0000001 ** -500000 = 1 / (1.0000001 ** 500000)
             DISPLAY "R3NEAR=EQ" ELSE DISPLAY "R3NEAR=NE" END-IF.
      *> ...and the divergence was never confined to a relation: an SDIDI subtraction of two near-equal
      *> values is EXACT, so an ordinary cancellation lifted the 34th digit into a DISPLAY.  This line
      *> printed 05000 before the fix (a gap of 5E-34) and prints 00000 after it.
           COMPUTE DLT = (2 ** -0.25 - 1 / (2 ** 0.25)) * 10 ** 33.
           DISPLAY "R3DLT =" DLT.
      *> The NEAR-UNIT logarithm band (kb/Work PB269) and the escape's exponent (kb/Work PB267), in one
      *> line.  1 + 10**-33 is the closest SDIDI value to one; the ln reduction's three square roots used
      *> to cancel the whole of b-1 into their own rounding and return ln b = 0, and the escape used to
      *> replace an exponent past the long range with 9223372036854775807 - so this answered a flat
      *> 1.0000000000000.  The value is exp(10**20 x ln(1 + 10**-33)) = 1.0000000000001000000000000005.
           COMPUTE U1 = 1.000000000000000000000000000000001E+0 ** 1.0E+20.
           DISPLAY "NEARU =" U1.
           STOP RUN.
