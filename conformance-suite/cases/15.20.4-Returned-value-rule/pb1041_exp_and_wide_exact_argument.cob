*>--- conformance-case
*> id: pb1041_exp_and_wide_exact_argument
*> rules: [RV-15.20.4-1, RV-15.34.4-1, RV-15.35.4-1, RV-15.82.4-1, RV-15.89.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1041 - EXP / EXP10 of an EXACT argument, and SIN / COS /
      *> TAN of a 31-digit one (a PICTURE this wide is legal from 2002).
      *> 15.34.1: "The EXP function returns an approximation of the value
      *> of e raised to the power of the argument"; 15.35.1 the same of 10.
      *> EXP's condition number is |x|: the binary64 of a 30-digit argument
      *> near 700 is off by up to 700 * 2**-53, and e**x inherits that as a
      *> RELATIVE error - the body answered for a different argument, 150
      *> binary64 ulps away. The exact argument is split into its integer
      *> part and its exact fraction before either is narrowed.
      *> 15.89.4 r1 / 15.20.4 r1 / 15.82.4 r1 as in the 85 golden: the
      *> 31-digit H30 is pi/2 to 30 places, and its tangent is the
      *> reciprocal of H30 - pi/2 = -7.5144E-31.
      *> Expected values: Python decimal at 140 digits (an oracle independent
      *> of the compiler), scaled by a power of ten and ROUNDED (14.7.4.3):
      *>   EXP-X   =  1.14750327711538 E+304 (was 1.14750327711542)
      *>   E10-Y   =  1.32879133982907 E+300 (was 1.32879133982900)
      *>   TAN-H30 =  1.33077452259255 E+30  (was 0.00000000000002)
      *>   COS-H30 =  7.5144209858470 E-31   (was 612323399573676.6 E-31)
      *>   SIN-E30 = -0.09011690191214       sin(10**30) (was +0.00933146893118,
      *>             the sine of 10**30 + 19884624838656, its binary64)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1041EXACT02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X    PIC 9(3)V9(27) VALUE 700.123456789012345678901234567.
       01 Y    PIC 9(3)V9(27) VALUE 300.123456789012345678901234567.
       01 H30  PIC 9V9(30)    VALUE 1.570796326794896619231321691639.
       01 E30  PIC 9(31)      VALUE 1000000000000000000000000000000.
       01 W13  PIC -9(3).9(13).
       01 W14  PIC -9(3).9(14).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W14 ROUNDED = FUNCTION EXP(X) / 1.0E304
           DISPLAY "EXP-X   =" W14
           COMPUTE W14 ROUNDED = FUNCTION EXP10(Y) / 1.0E300
           DISPLAY "E10-Y   =" W14
           COMPUTE W14 ROUNDED = FUNCTION TAN(H30) / 1.0E30
           DISPLAY "TAN-H30 =" W14
           COMPUTE W13 ROUNDED = FUNCTION COS(H30) * 1.0E31
           DISPLAY "COS-H30 =" W13
           COMPUTE W14 ROUNDED = FUNCTION SIN(E30)
           DISPLAY "SIN-E30 =" W14
           STOP RUN.
