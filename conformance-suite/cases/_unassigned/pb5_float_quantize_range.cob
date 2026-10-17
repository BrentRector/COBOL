*>--- conformance-case
*> id: pb5_float_quantize_range
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB5 - the float->fixed quantizer saturated at an ORDINARY COBOL magnitude.
      *> CobolIntrinsics.FromDouble returned a long and clamped at long.MaxValue; its caller quantizes at
      *> ws = max(Receiver.Scale, 9), so the clamp bit at |value| ~ 9.2e9. Every float-family result at or
      *> above that was silently replaced by 9223372036.85 - with NO SIZE ERROR, because 14.7.4 never saw an
      *> overflow: the value had already been clamped to something that fits. A 12-digit money field is
      *> routine COBOL, so this was silent wrong arithmetic in ordinary business ranges.
      *>
      *> 15.4.1 licenses an implementor-defined APPROXIMATION of the equivalent arithmetic expression under
      *> native arithmetic. 9223372036.85 is not an approximation of 10000000001.
      *>
      *> Expected values are exact spec arithmetic:
      *>   15.9.4 r1b with argument-2 = 1 reduces to 1 + argument-1  => ANNUITY(1e10, 1) = 10000000001
      *>     (a binary64 evaluation of the EAE's division lands a cent low at the 17th significant digit,
      *>      which IS the 15.4.1 approximation - the point of the fixture is the 8% saturation, not the cent)
      *>   15.85.4  SQRT(1e20) = 1e10 exactly
      *>   15.7.4   ABS of a COMP-2 1e10 = 1e10 exactly
      *>   15.59.4  MAX(1e10, 5) = 1e10 exactly
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5QUANT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D   USAGE COMP-2 VALUE 10000000000.
       01 R   PIC 9(12)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION ABS(D)
           DISPLAY R
           COMPUTE R = FUNCTION MAX(D 5)
           DISPLAY R
           COMPUTE R = FUNCTION SQRT(100000000000000000000)
           DISPLAY R
           COMPUTE R = FUNCTION ANNUITY(10000000000 1)
             ON SIZE ERROR DISPLAY "SIZE-ERROR"
             NOT ON SIZE ERROR DISPLAY "NO-SIZE-ERROR"
           END-COMPUTE
           STOP RUN.
