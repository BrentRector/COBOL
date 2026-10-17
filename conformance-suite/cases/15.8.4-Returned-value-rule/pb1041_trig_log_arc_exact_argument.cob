      *>--- conformance-case
      *> id: pb1041_trig_log_arc_exact_argument
      *> rules: [RV-15.8.4-1, RV-15.9.4-1, RV-15.10.4-1, RV-15.20.4-1, RV-15.55.4-1, RV-15.56.4-1, RV-15.82.4-1, RV-15.89.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1041 - SIN / COS / TAN, LOG / LOG10 and ACOS / ASIN of
      *> an EXACT argument where the function is ILL-CONDITIONED.
      *> 15.82.4 r1 / 15.20.4 r1 / 15.89.4 r1: "The returned value is the
      *> approximation of the sine [cosine / tangent] of argument-1";
      *> 15.55.4 r1 / 15.56.4 r1 and 15.8.4 r1 / 15.10.4 r1 say the same
      *> of the logarithm and the arccosine / arcsine. Argument-1 is the
      *> EXACT value the 18-digit item holds. Near a multiple of pi/2 the
      *> sine / cosine / tangent is (plus or minus) the RESIDUE x - k*pi/2
      *> or its reciprocal, ln x is x - 1 near 1, and arccos x is
      *> sqrt(2(1 - x)) near 1 - so narrowing the argument to binary64
      *> first (P becomes 3.141592653589793116, pi to 16 places) answered
      *> for a DIFFERENT argument. Expected values: Python decimal at 140
      *> digits, pi by Machin (an oracle independent of the compiler),
      *> scaled by a power of ten and ROUNDED (14.7.4.3) into the edited
      *> receiver; "was" is the value before the fix.
      *>   SIN-P   =   8.4626433832795 E-18  sin(3.14159265358979323)
      *>           = pi - P = 8.46264338327950288E-18 (was 122.46... E-18)
      *>   SIN-NP  =  -8.4626433832795 E-18  sin is odd
      *>   COS-H   =  -0.76867830836025 E-18 cos(1.57079632679489662)
      *>           = -(H - pi/2) (was +61.23... E-18)
      *>   TAN-H   =  -1.30093432990611 E+18 -1/(H - pi/2) (was 0.0163 E+18)
      *>   SIN-T   =  -1.00000000000000      3pi/2 + 2.3E-18: the control
      *>   COS-T   =   2.3060349250807 E-18  (was -183.69... E-18)
      *>   TAN-T   =  -4.33644776635369 E+17 (was 0.0544 E+17)
      *>   SIN-B   =  -0.99885284043482      sin(123456789012345678)
      *>           (was +0.45921144118165, the sine of its binary64)
      *>   LOG-L   =  10.00000000000000 E-18 ln(1.00000000000000001) (was 0)
      *>   LOG-M   = -10.00000000000000 E-18 ln(0.99999999999999999) (was 0)
      *>   L10-L   =   4.34294481903252 E-18 log10(1.00000000000000001)
      *>   ACOS-M  =   4.47213595499958 E-9  arccos(0.99999999999999999)
      *>           = sqrt(2E-17) (was 0)
      *>   ACOS-NM =   3.14159264911766      pi - 4.47E-9 (was 3.14159265358979)
      *>   ASIN-M  =   1.57079632232276      pi/2 - 4.47E-9 (was 1.57079632679490)
      *>   ASIN-NM =  -1.57079632232276
      *>   SIN-1   =   0.84147098480790      an ordinary argument: unchanged
      *>   ANN-T   =   0.08333333333333      ANNUITY(1E-17 12), 15.9.4 1) b):
      *>           1/12 + 13/24 * 1E-17 (kb/Work PB1310 - was 0: 1 + rate
      *>           was formed in binary64, where it is exactly 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1041EXACT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P    PIC 9V9(17)  VALUE 3.14159265358979323.
       01 NP   PIC S9V9(17) VALUE -3.14159265358979323.
       01 H    PIC 9V9(17)  VALUE 1.57079632679489662.
       01 T    PIC 9V9(17)  VALUE 4.71238898038468986.
       01 B    PIC 9(18)    VALUE 123456789012345678.
       01 L    PIC 9V9(17)  VALUE 1.00000000000000001.
       01 M    PIC S9V9(17) VALUE 0.99999999999999999.
       01 NM   PIC S9V9(17) VALUE -0.99999999999999999.
       01 ONE  PIC 9        VALUE 1.
       01 TR   PIC V9(17)   VALUE .00000000000000001.
       01 W13  PIC -9(3).9(13).
       01 W14  PIC -9(3).9(14).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W13 ROUNDED =
               FUNCTION SIN(P) * 1000000000 * 1000000000
           DISPLAY "SIN-P   =" W13
           COMPUTE W13 ROUNDED =
               FUNCTION SIN(NP) * 1000000000 * 1000000000
           DISPLAY "SIN-NP  =" W13
           COMPUTE W14 ROUNDED =
               FUNCTION COS(H) * 1000000000 * 1000000000
           DISPLAY "COS-H   =" W14
           COMPUTE W14 ROUNDED =
               FUNCTION TAN(H) / 1000000000 / 1000000000
           DISPLAY "TAN-H   =" W14
           COMPUTE W14 ROUNDED = FUNCTION SIN(T)
           DISPLAY "SIN-T   =" W14
           COMPUTE W13 ROUNDED =
               FUNCTION COS(T) * 1000000000 * 1000000000
           DISPLAY "COS-T   =" W13
           COMPUTE W14 ROUNDED =
               FUNCTION TAN(T) / 100000000000000000
           DISPLAY "TAN-T   =" W14
           COMPUTE W14 ROUNDED = FUNCTION SIN(B)
           DISPLAY "SIN-B   =" W14
           COMPUTE W14 ROUNDED =
               FUNCTION LOG(L) * 1000000000 * 1000000000
           DISPLAY "LOG-L   =" W14
           COMPUTE W14 ROUNDED =
               FUNCTION LOG(M) * 1000000000 * 1000000000
           DISPLAY "LOG-M   =" W14
           COMPUTE W14 ROUNDED =
               FUNCTION LOG10(L) * 1000000000 * 1000000000
           DISPLAY "L10-L   =" W14
           COMPUTE W14 ROUNDED = FUNCTION ACOS(M) * 1000000000
           DISPLAY "ACOS-M  =" W14
           COMPUTE W14 ROUNDED = FUNCTION ACOS(NM)
           DISPLAY "ACOS-NM =" W14
           COMPUTE W14 ROUNDED = FUNCTION ASIN(M)
           DISPLAY "ASIN-M  =" W14
           COMPUTE W14 ROUNDED = FUNCTION ASIN(NM)
           DISPLAY "ASIN-NM =" W14
           COMPUTE W14 ROUNDED = FUNCTION SIN(ONE)
           DISPLAY "SIN-1   =" W14
           COMPUTE W14 ROUNDED = FUNCTION ANNUITY(TR 12)
           DISPLAY "ANN-T   =" W14
           STOP RUN.
