*>--- conformance-case
*> id: pb76_sdidi_subprecision_rounding
*> rules: [GR-8.8.1.5.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB76 - the SDIDI final transfer's rounding decision for a value FAR BELOW the receiver's precision.
      *> 14.7.4.3 r4 (NEAREST-AWAY-FROM-ZERO, implied by ROUNDED): "the arithmetic value is rounded to the
      *> nearest value that can be represented in the resultant identifier. If two such values are equally
      *> near, the value farther from zero is chosen." 10**-20 into V9(9) is nowhere near a tie - its nearest
      *> representable value is 0. The defect: CobolDec.DivRemPow10's past-the-carrier marker was (0, 1, 2)
      *> - EXACTLY HALF - so every NEAREST mode lifted a sub-precision value to one unit (0.000000001), and
      *> the marker carried no sign, so AWAY-FROM-ZERO / TOWARD-GREATER of a negative value went toward +inf.
      *> The 34-digit significand makes the shape common: 1/10**20 is 10**33 x 10**-53, 44 places below scale 9.
      *> N20/N50/NF50: NEAREST (implied) -> 0.000000000. E20: NEAREST-EVEN -> 0. AZ20/AZN20: r3 AWAY-FROM-ZERO
      *> -> the nearest value farther from zero, +/-0.000000001, in the VALUE's direction. TG-N: TOWARD-GREATER
      *> of a negative sub-precision value -> 0 (r8: the nearest value greater); TL-N: TOWARD-LESSER ->
      *> -0.000000001 (r9). T20: TRUNCATION (r10) -> 0. TIE/BELOW: controls at scale - 5E-10 (an exact tie)
      *> rounds away to 0.000000001, 4E-10 to 0 - the sub-precision case must agree with the below-half case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB76SUBPREC.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9   PIC S9(9)V9(9).
       01 E9   PIC -(9)9.9(9).
       PROCEDURE DIVISION.
           COMPUTE R9 ROUNDED = 10 ** -20.
           MOVE R9 TO E9.
           DISPLAY "N20=" E9.
           COMPUTE R9 ROUNDED = 10 ** -50.
           MOVE R9 TO E9.
           DISPLAY "N50=" E9.
           COMPUTE R9 ROUNDED = FUNCTION NUMVAL-F("1E-50").
           MOVE R9 TO E9.
           DISPLAY "NF50=" E9.
           COMPUTE R9 ROUNDED MODE NEAREST-EVEN = 10 ** -20.
           MOVE R9 TO E9.
           DISPLAY "E20=" E9.
           COMPUTE R9 ROUNDED MODE AWAY-FROM-ZERO = 10 ** -20.
           MOVE R9 TO E9.
           DISPLAY "AZ20=" E9.
           COMPUTE R9 ROUNDED MODE AWAY-FROM-ZERO = 0 - 10 ** -20.
           MOVE R9 TO E9.
           DISPLAY "AZN20=" E9.
           COMPUTE R9 ROUNDED MODE TOWARD-GREATER = 0 - 10 ** -20.
           MOVE R9 TO E9.
           DISPLAY "TG-N=" E9.
           COMPUTE R9 ROUNDED MODE TOWARD-LESSER = 0 - 10 ** -20.
           MOVE R9 TO E9.
           DISPLAY "TL-N=" E9.
           COMPUTE R9 ROUNDED MODE TRUNCATION = 10 ** -20.
           MOVE R9 TO E9.
           DISPLAY "T20=" E9.
           COMPUTE R9 ROUNDED = FUNCTION NUMVAL-F("5E-10").
           MOVE R9 TO E9.
           DISPLAY "TIE=" E9.
           COMPUTE R9 ROUNDED = FUNCTION NUMVAL-F("4E-10").
           MOVE R9 TO E9.
           DISPLAY "BELOW=" E9.
           STOP RUN.
       END PROGRAM PB76SUBPREC.
