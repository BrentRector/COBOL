*>--- conformance-case
*> id: pb115_asin_scaled_argument
*> rules: [AR-15.10.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB115 — the float lane's scaled-argument conversion must be CORRECTLY ROUNDED. ISO 15.10.3 r2
      *> admits any |argument-1| <= 1 and 15.10.4 r1 requires "the approximation of the arcsine ... greater than
      *> or equal to -pi/2 and less than or equal to +pi/2"; the defect divided by a repeated-multiplication
      *> 10^scale (one ulp low at scale >= 23), so each all-nines value below arrived ABOVE 1.0, Math.Asin
      *> returned NaN, and the reference drew the 15.3 default 0 (or a thrown EC-ARGUMENT-FUNCTION under
      *> checking) on CONFORMING source.
      *> Hand-derived: 1 - 10^-23 (and -25, -31) all sit inside double 1.0's half-ulp (1.11e-16), so the
      *> correctly-rounded double IS 1.0 and asin = pi/2 = 1.57079632679489...; the asserts are RANGES around
      *> the true values (r1 licenses an approximation, and the fixed-point landing's grain is the documented
      *> item-92 determination, deliberately not re-pinned here): pi/2 in (1.5707963, 1.5707964),
      *> asin(0.5) = 0.52359877559829... in (0.5235987, 0.5235988). Pre-fix, every S* line printed 0 - far
      *> outside its range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB115AS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X31 PIC V9(31) VALUE .9999999999999999999999999999999.
       01 X25 PIC V9(25) VALUE .9999999999999999999999999.
       01 X23 PIC V9(23) VALUE .99999999999999999999999.
       01 XHALF PIC V99 VALUE .50.
       01 R PIC 9V9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION ASIN(X31)
           IF R > 1.5707963 AND R < 1.5707964
               DISPLAY "S31 OK" ELSE DISPLAY "S31 BAD " R END-IF
           COMPUTE R = FUNCTION ASIN(X25)
           IF R > 1.5707963 AND R < 1.5707964
               DISPLAY "S25 OK" ELSE DISPLAY "S25 BAD " R END-IF
           COMPUTE R = FUNCTION ASIN(X23)
           IF R > 1.5707963 AND R < 1.5707964
               DISPLAY "S23 OK" ELSE DISPLAY "S23 BAD " R END-IF
           COMPUTE R = FUNCTION ASIN(XHALF)
           IF R > 0.5235987 AND R < 0.5235988
               DISPLAY "HALF OK" ELSE DISPLAY "HALF BAD " R END-IF
           STOP RUN.
