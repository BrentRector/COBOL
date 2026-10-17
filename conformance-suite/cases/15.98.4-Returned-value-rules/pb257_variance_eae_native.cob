      *>--- conformance-case
      *> id: pb257_variance_eae_native
      *> rules: [RV-15.98.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB257 - FUNCTION VARIANCE's three lettered arms (ISO 15.98.4 r1) on the NATIVE carrier, the
      *> lane 15.4.1 licenses as "an implementor-defined approximation of the value of that expression". The
      *> standard-decimal twin of this fixture is 2014/pb257_variance_eae_arms; this one exists because the
      *> native lane is a SECOND body with its own argument-list handling, and a rule verified in one carrier is
      *> not verified in the other - that is precisely how the two bodies came to disagree about an empty
      *> argument list (one answered a silent 0, the other would have indexed past the end).
      *>   VA1 = FUNCTION VARIANCE(5)        arm (a): (0)                                      -> 0 (DISPLAYed 000000: 9V9(5), implied point)
      *>   VA2 = FUNCTION VARIANCE(1 3)      arm (b): MEAN = 2; ((1-2)**2 + (3-2)**2)/2 = 1 (100000)
      *>   VA3 = FUNCTION VARIANCE(1 2 3)    arm (c): (1 + 0 + 1)/3 = 2/3, truncated at 9V9(5) -> 0.66666 (066666)
      *>   SD2 = FUNCTION STANDARD-DEVIATION(1 3)   15.86.4 r1: SQRT(VARIANCE(1 3)) = 1 (100000)
      *> Arms (a) and (b) are exact in binary64 (x - x is exact and the sums are small integers), so the
      *> approximation latitude does not soften the first two values.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB257VARNAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VA1 PIC 9V9(5).
       01 VA2 PIC 9V9(5).
       01 VA3 PIC 9V9(5).
       01 SD2 PIC 9V9(5).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE VA1 = FUNCTION VARIANCE(5)
           DISPLAY "VA1=" VA1
           COMPUTE VA2 = FUNCTION VARIANCE(1, 3)
           DISPLAY "VA2=" VA2
           COMPUTE VA3 = FUNCTION VARIANCE(1, 2, 3)
           DISPLAY "VA3=" VA3
           COMPUTE SD2 = FUNCTION STANDARD-DEVIATION(1, 3)
           DISPLAY "SD2=" SD2
           STOP RUN.
