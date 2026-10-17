*>--- conformance-case
*> id: pb116_sqrt_standard_decimal
*> rules: [GR-11.9.5.2-3, RV-15.4.1-L2.1, RV-15.84.4-1, RV-15.84.4-2, RV-15.86.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB116 — SQRT under ARITHMETIC IS STANDARD-DECIMAL (ISO 15.84.4 r2: "computed to 34 digits, and
      *> the result rounded to 34 digits according to the rules for standard-decimal arithmetic" — the ONE
      *> section-15 function whose standard-mode returned value the standard fixes EXACTLY; 15.4.1's
      *> implementor-defined licence yields to it), with r1's "argument-1 is not rounded" (the exact fixed-point
      *> operand enters). The defect routed SQRT to binary64 Math.Sqrt (~16 digits) through FromDouble.
      *> Hand-derived: sqrt(2) = 1.414213562373095048801688724209698 (34 digits; 35th is 0); the final transfer
      *> to the scale-30 receiver truncates (14.7 NOTE 1, no ROUNDED) -> 1.414213562373095048801688724209.
      *> STANDARD-DEVIATION(1 2 3 4): 15.86.4 r1's EAE = SQRT(VARIANCE) evaluated in SDIDI form end to end;
      *> variance = 1.25 exactly, sqrt(1.25) = 1.118033988749894848204586834365638 -> truncated at 30 ->
      *> 1.118033988749894848204586834365.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB116SQ.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9V9(30).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION SQRT(2)
           IF R = 1.414213562373095048801688724209
               DISPLAY "SQRT2 OK"
           ELSE
               DISPLAY "SQRT2 BAD " R
           END-IF
           COMPUTE R = FUNCTION SQRT(1.21)
           IF R = 1.1
               DISPLAY "EXACT OK"
           ELSE
               DISPLAY "EXACT BAD " R
           END-IF
           COMPUTE R = FUNCTION STANDARD-DEVIATION(1 2 3 4)
           IF R = 1.118033988749894848204586834365
               DISPLAY "STDDEV OK"
           ELSE
               DISPLAY "STDDEV BAD " R
           END-IF
           STOP RUN.
