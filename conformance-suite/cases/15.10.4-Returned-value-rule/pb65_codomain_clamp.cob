*>--- conformance-case
*> id: pb65_codomain_clamp
*> rules: [RV-15.10.4-1, RV-15.11.4-1, RV-15.75.4-1, FMT-15.10.2,
*>    FMT-15.11.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB65 / RV-15.75.4-1 — a bounded codomain clamps the QUANTIZED value; the interior is untouched.
      *> §15.75.4 r1: RANDOM "is greater than or equal to zero and less than one" — strict, so the
      *> away-from-zero quantization at the working scale may not round the top half-ulp to exactly 1.
      *> §15.10.4 r1 / §15.8.4 r1: ASIN/ACOS are bounded by ±π/2 / π — CLOSED bounds, but IRRATIONAL,
      *> so the nearest scaled value above them (1.570796327 / 3.141592654) is not "equal to the bound"
      *> and lies outside; the clamp lands on ⌊bound·10^ws⌋ (1.570796326 / 3.141592653). §15.11.4 r1:
      *> ATAN's ±π/2 is OPEN. FRACTION-PART's |v| < 1 follows from its §15.42.4 r1 EAE. SQRT has NO
      *> stated bound: a SQRT that quantizes to 1.000000000 is a legal §15.4.1 approximation and stays —
      *> the contrast line proving the clamp is per-rule, not a blanket rounding change.
      *> ⛔ RE-BASELINED AGAIN 2026-09-05 (kb/Work PB647): the NO-PHRASE legs now TRUNCATE at the resultant
      *> identifier's scale, because that is what §14.7.4.3 rule 2 ("If the ROUNDED phrase is not specified,
      *> execution is as if ROUNDED MODE IS TRUNCATION had been specified") and §14.7.4.1 ("truncation is
      *> relative to the size provided for the resultant identifier") require, and what the MOVE channel has
      *> always done (§14.6.8.2 rule 4) — one returned value, one landing, §15.4.1. So ASINH moved 776 -> 775
      *> and SQRTD 1000000000 -> 0999999999, both RE-DERIVED from the exact binary64 expansions below, not
      *> re-observed. Truncation can never leave a codomain the value is already inside, so the clamp this
      *> program exists for is now exercised by the ROUNDED legs (RASIN1/RASINM/RACOSM/RFRACP) instead —
      *> each of which would read one ulp OUTSIDE its §15.x.4 bound if the clamp were removed.
      *> ⛔ SQRTC RE-BASELINED 2026-09-05 (kb/Work PB623): it read +1000000000 while the landing formed
      *> v × 10^9 in binary64 first. sqrt(0.999999999) is the double 0.9999999995, whose EXACT value at
      *> scale 9 is 999999999.4999999586298145004548132419586181640625 — BELOW the midpoint, so
      *> NEAREST-AWAY-FROM-ZERO gives 999999999. The old answer came from the binary64 product landing on
      *> exactly 999999999.5 (within half an ulp of the true value) and the tie then rounding away — a
      *> double rounding, not the value. SQRTD keeps the contrast honestly: sqrt(0.9999999999) is
      *> 0.99999999995, exactly 999999999.94999999586… at scale 9, which DOES quantize to 1.000000000 and
      *> is left there, where ASIN/ACOS's stated bounds clamp.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65CODOMCLAMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W9    PIC S9V9(9) SIGN LEADING SEPARATE.
       01 FSRC  USAGE COMP-2.
       01 RND   PIC 9V9(9).
       01 HITS  PIC 9(6) VALUE 0.
       01 I     PIC 9(6).
       PROCEDURE DIVISION.
           COMPUTE W9 = FUNCTION ASIN(1)
           DISPLAY "ASIN1 =" W9
           COMPUTE W9 = FUNCTION ASIN(-1)
           DISPLAY "ASINM =" W9
           COMPUTE W9 = FUNCTION ACOS(-1)
           DISPLAY "ACOSM =" W9
           COMPUTE W9 = FUNCTION ATAN(1000000000000000000)
           DISPLAY "ATANB =" W9
           COMPUTE W9 = FUNCTION ASIN(0.5)
           DISPLAY "ASINH =" W9
           MOVE 5.9999999996 TO FSRC
           COMPUTE W9 = FUNCTION FRACTION-PART(FSRC)
           DISPLAY "FRACP =" W9
           COMPUTE W9 = FUNCTION SQRT(0.999999999)
           DISPLAY "SQRTC =" W9
           COMPUTE W9 = FUNCTION SQRT(0.9999999999)
           DISPLAY "SQRTD =" W9
      *> The ROUNDED legs are where the clamp is REACHABLE at all (kb/Work PB647): a no-phrase COMPUTE now
      *> TRUNCATES the returned value at the resultant identifier's scale, exactly as the MOVE channel does
      *> (§14.7.4.3 rule 2 / §14.6.8.2 rule 4), and truncation can never leave a codomain the double is
      *> already inside. ROUNDED (bare = NEAREST-AWAY-FROM-ZERO, §14.7.4.3 rule 4) is what rounds the top
      *> half-ulp OUT, so these four lines are the ones §15.10.4 r1 / §15.8.4 r1 / §15.42.4 r1 hold in.
           COMPUTE W9 ROUNDED = FUNCTION ASIN(1)
           DISPLAY "RASIN1=" W9
           COMPUTE W9 ROUNDED = FUNCTION ASIN(-1)
           DISPLAY "RASINM=" W9
           COMPUTE W9 ROUNDED = FUNCTION ACOS(-1)
           DISPLAY "RACOSM=" W9
           COMPUTE W9 ROUNDED = FUNCTION FRACTION-PART(FSRC)
           DISPLAY "RFRACP=" W9
      *> ... and the UNBOUNDED contrast, which the truncating SQRTD line above can no longer carry: SQRT has
      *> no §15.x.4 bound, so a returned value that ROUNDS to 1.000000000 is a legal §15.4.1 approximation
      *> and STAYS there, where ASIN/ACOS's stated bounds clamp. The clamp is per-rule, not a rounding change.
           COMPUTE W9 ROUNDED = FUNCTION SQRT(0.9999999999)
           DISPLAY "RSQRTD=" W9
           COMPUTE W9 ROUNDED = FUNCTION ASIN(0.5)
           DISPLAY "RASINH=" W9
           COMPUTE RND = FUNCTION RANDOM(1)
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 20000
               COMPUTE RND = FUNCTION RANDOM
               IF RND NOT < 1 ADD 1 TO HITS END-IF
           END-PERFORM
           DISPLAY "GE1HITS=" HITS
           STOP RUN.
