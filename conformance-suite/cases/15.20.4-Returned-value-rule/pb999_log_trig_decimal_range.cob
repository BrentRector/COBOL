*>--- conformance-case
*> id: pb999_log_trig_decimal_range
*> rules: [RV-15.20.4-1, RV-15.55.4-1, RV-15.56.4-1, RV-15.82.4-1,
*>    RV-15.84.4-4, RV-15.89.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB999 - LOG, LOG10, SQRT, SIN, COS and TAN of a LEGAL argument outside binary64's range.
      *> A floating-point literal product is carried on the standard-decimal intermediate, whose range
      *> reaches 10**-6143 .. 10**6144 (8.8.1.5.2); binary64's stops near 10**+-308. The argument was
      *> narrowed to a double BEFORE the function body, so 10**-400 became +0.0 and 10**400 +Infinity.
      *>
      *> 15.55.3 r2 / 15.56.3 r2: "The value of argument-1 shall be greater than zero." 10**-400 IS greater
      *> than zero, so no EC-ARGUMENT-FUNCTION may be raised, and 15.56.4 r1 / 15.55.4 r1 require "the
      *> approximation of the logarithm to the base 10 [base e] of argument-1":
      *>   L10T = -0400.0000   log10(10**-400) = -400 exactly
      *>   LNT  = -0921.0340   ln(10**-400) = -400 * ln 10 = -921.03403719...; the COMPUTE truncates (14.7.4.3)
      *>   L10H =  0400.0000   log10(10**400) = 400
      *>   LNH  =  0921.0340   ln(10**400) = 921.03403719...
      *> Before the fix all four were 0 (log of +0.0 / +Infinity quantized to the 15.3 default).
      *>
      *> 15.84.4 r4: "When native arithmetic is in effect, the returned value is the absolute value of the
      *> approximation of the square root of argument-1." sqrt(10**-400) = 10**-200 and sqrt(10**400) =
      *> 10**200, both representable, so the receiver-less float channel shows them:
      *>   SQT = 1E-200   (was 0: the root of an underflowed +0.0)
      *>   SQH = 1E+200   (was Infinity)
      *>
      *> 15.82.3 / 15.20.3 / 15.89.3 restrict argument-1 only to class numeric, and 15.82.4 / 15.20.4 /
      *> 15.89.4 return "the approximation of the sine [cosine, tangent] of argument-1". 10**400 reduced
      *> modulo 2*pi (pi to 6400 digits, Python decimal - an oracle independent of the compiler) is
      *> 1.62486...; the COMPUTE truncates to eight places:
      *>   SIN  = -0000.99853823   sin(10**400)  = -0.99853823198309...
      *>   COS  = -0000.05404997   cos(10**400)  = -0.05404997010239...
      *>   TAN  =  0018.47435308   tan(10**400)  = 18.47435308644001...
      *>   SINN =  0000.99853823   sin(-10**400) = -sin(10**400)  (odd)
      *>   SIN40 = -0000.56963340  sin(10**40)   = -0.56963340095363... - inside binary64's range, but the
      *>          nearest binary64 to 10**40 is 10**40 + 3.0E23, so narrowing first answered sin of a
      *>          different argument (+0.6468); a periodic body pays the narrowing as an ABSOLUTE error.
      *> The same absolute error hits a SCALED item past ~10**16 (a 28-digit PICTURE is legal from 2002):
      *>   SINS = 0000.41564671    sin(123456789012345678901234567.5) = 0.41564671763281...
      *>          (narrowed first: -0.87510460..., the sine of the nearest binary64)
      *>   COSS = -0000.18376388   cos(-98765432109876543210) = -0.18376388743989...
      *>          (narrowed first: the cosine of -98765432109876543488)
      *> Before the fix SIN/COS/TAN(10**400) were NaN - 0, and RAISED under checking (see below).
      *>
      *> The nested program runs with EC-ARGUMENT-FUNCTION checking ON, so the screens are observed:
      *>   L10C = -0400.0000 with NO "RAISED" line - 10**-400 satisfies 15.56.3 r2
      *>   SINC = -0000.99853823 with NO "RAISED" line - SIN has no argument-value rule at all
      *>   "  RAISED" then SQC = 00000 - SQRT(-(10**-400)) violates 15.84.3 r2 "The value of argument-1
      *>          shall be zero or positive" - the ONE domain screen still decides it on the exact sign.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB999LOGTRIG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E  PIC -9(4).9(4).
       01 W-T  PIC -9(4).9(8).
       01 W-S  PIC 9(27)V9 VALUE 123456789012345678901234567.5.
       01 W-N  PIC S9(20) VALUE -98765432109876543210.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W-E = FUNCTION LOG10(1.0E-200 * 1.0E-200)
           DISPLAY "L10T=" W-E
           COMPUTE W-E = FUNCTION LOG(1.0E-200 * 1.0E-200)
           DISPLAY "LNT=" W-E
           COMPUTE W-E = FUNCTION LOG10(1.0E200 * 1.0E200)
           DISPLAY "L10H=" W-E
           COMPUTE W-E = FUNCTION LOG(1.0E200 * 1.0E200)
           DISPLAY "LNH=" W-E
           DISPLAY "SQT=" FUNCTION SQRT(1.0E-200 * 1.0E-200)
           DISPLAY "SQH=" FUNCTION SQRT(1.0E200 * 1.0E200)
           COMPUTE W-T = FUNCTION SIN(1.0E200 * 1.0E200)
           DISPLAY "SIN=" W-T
           COMPUTE W-T = FUNCTION COS(1.0E200 * 1.0E200)
           DISPLAY "COS=" W-T
           COMPUTE W-T = FUNCTION TAN(1.0E200 * 1.0E200)
           DISPLAY "TAN=" W-T
           COMPUTE W-T = FUNCTION SIN(-1.0E200 * 1.0E200)
           DISPLAY "SINN=" W-T
           COMPUTE W-T = FUNCTION SIN(1.0E20 * 1.0E20)
           DISPLAY "SIN40=" W-T
           COMPUTE W-T = FUNCTION SIN(W-S)
           DISPLAY "SINS=" W-T
           COMPUTE W-T = FUNCTION COS(W-N)
           DISPLAY "COSS=" W-T
           CALL "PB999LOGTRIGON"
           STOP RUN.
       END PROGRAM PB999LOGTRIG.

       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB999LOGTRIGON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E  PIC -9(4).9(4).
       01 W-T  PIC -9(4).9(8).
       01 W-F  PIC 9(3)V99 VALUE 0.
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
           COMPUTE W-E = FUNCTION LOG10(1.0E-200 * 1.0E-200)
           DISPLAY "L10C=" W-E
           COMPUTE W-T = FUNCTION SIN(1.0E200 * 1.0E200)
           DISPLAY "SINC=" W-T
           COMPUTE W-F = FUNCTION SQRT(-1.0E-200 * 1.0E-200)
           DISPLAY "SQC=" W-F
           GOBACK.
       END PROGRAM PB999LOGTRIGON.
