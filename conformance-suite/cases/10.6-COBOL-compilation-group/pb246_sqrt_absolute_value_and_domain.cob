*>--- conformance-case
*> id: pb246_sqrt_absolute_value_and_domain
*> rules: [SR-10.6.2-5, AR-15.84.3-2, RV-15.84.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB246 - FUNCTION SQRT on the NATIVE carrier: ISO 15.84.4 r4 and 15.84.3 r2, the two rules that
      *> meet on the one body and neither of which was implemented there.
      *>
      *> 15.84.4 r4: "When native arithmetic is in effect, the returned value is the ABSOLUTE VALUE of the
      *> approximation of the square root of argument-1." The phrase is not decoration. IEC 60559 mandates
      *> sqrt(-0) = -0 EXACTLY so that sqrt is not the absolute value there, while |-0.0| is +0.0 - so negative
      *> zero is the unique argument on which r4's "absolute value" does any work, and it is a LEGAL argument-1
      *> because 15.84.3 r2 admits zero. A COMP-2 item reaches -0.0 by ordinary native underflow: the product of
      *> a negative tiny and a positive tiny is a negative zero, which NZ below DISPLAYs with its sign (the
      *> unquantized float channel is IEEE-faithful), so the input is observable BEFORE the function sees it.
      *>   NZ  = -0      the argument, proving the probe really is negative zero and not plain zero
      *>   SQL = 0       15.84.4 r4 over the receiver-less/float arm: |sqrt(-0.0)| = |-0.0| = +0.0, DISPLAYed
      *>                 without a sign. A bare Math.Sqrt answers -0 here; that was the defect.
      *>   SQR = 0       the same value stored into a COMP-2 receiver, which keeps the signed zero bit for bit
      *>   SQ4 = 00200   15.84.4 r4 over a fixed-point receiver: sqrt(4) = 2 exactly (9(3)V99, implied point)
      *>
      *> 15.84.3 r2: "The value of argument-1 shall be zero or positive." This is a VALUE constraint, so 15.3
      *> rule 14 makes it EC-ARGUMENT-FUNCTION at run time rather than a compile-time reject - with checking off
      *> the implementor defines the result (docs/CONFORMANCE.md DOC-A.1-90: the numeric zero), and with checking
      *> on the condition is raised and a declarative sees it. The guard is now EXPLICIT at the body beside
      *> LOG's and LOG10's; before, the only detector was the NaN artifact of Math.Sqrt, which is not total -
      *> a standard-decimal operand whose exponent is at or below -324 converts to double as -0.0, on which
      *> Math.Sqrt answers -0.0 and not NaN, so a genuinely negative argument-1 was accepted in silence.
      *>   SQN = 00000   checking OFF: the 15.3 rule 14 default result, zero
      *>   "  RAISED" then SQC = 00000: checking ON, the declarative fires and RESUMEs - the branch
      *>                 nothing anywhere exercised for SQRT
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB246SQRTABS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-Y  COMP-2 VALUE -1.0E-200.
       01 W-T  COMP-2 VALUE 1.0E-200.
       01 W-Z  COMP-2.
       01 W-R  COMP-2.
       01 W-F  PIC 9(3)V99.
       01 W-N  PIC S9(4) VALUE -4.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W-Z = W-Y * W-T
           DISPLAY "NZ=" W-Z
           DISPLAY "SQL=" FUNCTION SQRT(W-Z)
           COMPUTE W-R = FUNCTION SQRT(W-Z)
           DISPLAY "SQR=" W-R
           COMPUTE W-F = FUNCTION SQRT(4)
           DISPLAY "SQ4=" W-F
           COMPUTE W-F = FUNCTION SQRT(W-N)
           DISPLAY "SQN=" W-F
           CALL "PB246SQRTON"
           STOP RUN.
       END PROGRAM PB246SQRTABS.

       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB246SQRTON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-F  PIC 9(3)V99 VALUE 0.
       01 W-N  PIC S9(4) VALUE -4.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  RAISED".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           COMPUTE W-F = FUNCTION SQRT(W-N)
           DISPLAY "SQC=" W-F
           GOBACK.
       END PROGRAM PB246SQRTON.
