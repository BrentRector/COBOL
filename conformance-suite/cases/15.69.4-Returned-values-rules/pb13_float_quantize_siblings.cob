*>--- conformance-case
*> id: pb13_float_quantize_siblings
*> rules: [RV-15.69.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB13 (siblings) - the float->fixed quantizer's three OTHER faces. The headroom golden
      *> pb13_float_quantize_headroom pins the two cases the fix-queue entry carried; this one pins the cases the
      *> SWEEP for that entry's siblings found (CLAUDE.md rule 4 - every bug is a pattern), each of which was
      *> silently wrong on the same mechanism and none of which the entry named.
      *>
      *> THE MECHANISM, once: CobolIntrinsics#FromDouble lands a double at a WORKING scale and saturates at
      *> Int128.MaxValue. The store then rescales working->receiver scale, which DIVIDES the saturation sentinel
      *> back down and so destroys the evidence the receiver's digit-capacity check exists to see. That is why the
      *> saturation was SILENT rather than a size error. ReceiverContext.FloatWorkingScale now caps the working
      *> scale at the receiver's Int128 headroom (ws <= 38 - integer digits), which makes a value that FITS never
      *> saturate and a value that does NOT always trip the capacity check.
      *>
      *> SIBLING 1 - NumericRenderer#Power reaches the SAME quantizer with the SAME formula, so ** carried the
      *> identical defect: 10 ** 30 into PIC 9(31) stored 0170141183460469231731687303715. 8.8.1.2 r6 places no
      *> exactness requirement on a native exponentiation and 8.8.1.3 makes native arithmetic implementor-defined,
      *> so only the MAGNITUDE is asserted here - a saturated result differs in the first digit.
      *>
      *> SIBLING 2 - the receiver-less ** relation, the exact shape of the headroom golden's EXP10 case: with no
      *> receiver there is no scale to quantize to, both sides saturated to the same Int128.MaxValue, and two
      *> values a FACTOR OF TEN apart compared EQUAL. 8.8.4.2.4 makes a native comparison proceed by the rules of
      *> native arithmetic, and 15.4.1 leaves the returned value's representation to the implementor - COBOL.NET's
      *> determination is binary64, so 10**30 and 10**31 are distinct and the relation is FALSE.
      *>
      *> SIBLING 3 - a value that genuinely EXCEEDS the receiver must raise the size error condition, never store a
      *> saturated one. 14.7.5 case 5 makes an out-of-range native intermediate a size error condition when the
      *> implementor defines that range as checked; EXP(700) is about 1E+304 and cannot fit PIC 9(31).
      *>
      *> SIBLING 4 - the receiver-less TEXT channel. This printed 170141183460469231731687303715.884105727 - the
      *> saturation sentinel rendered as if it were the value. docs/CONFORMANCE.md's 15.4.1 / 14.9.11.4 GR1
      *> determination already required that a FLOAT-valued function render through the same shortest-round-trip
      *> CobolFloat.Display a COMP-2 item does; the working-scale form silently broke it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB13SIB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(31).
       01 S PIC X(10) VALUE "1E+20".
       PROCEDURE DIVISION.
      *> SIBLING 5 - the NUMVAL family carried PB5's ORIGINAL clamp, unswept: the runtime returned long and
      *> saturated at long.MaxValue, so with the >= 6 / >= 9 working floor an ordinary argument overflowed it.
      *> FUNCTION NUMVAL-F("1E+20") returned 9223372036 - ten orders of magnitude out, with NO size error - where
      *> 15.69.4 r2 requires "an approximation of the numeric value represented by argument-1". 10**20 is exact in
      *> Int128 (21 digits of 38) so the value is asserted exactly, not as a range.
           COMPUTE R = FUNCTION NUMVAL-F(S)
               ON SIZE ERROR DISPLAY "NVF=SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "NVF=NO-SIZE-ERROR"
           END-COMPUTE
           IF R = 100000000000000000000
              DISPLAY "NVF=EXACT"
           ELSE
              DISPLAY "NVF=WRONG"
           END-IF
           COMPUTE R = 10 ** 30
               ON SIZE ERROR DISPLAY "POW30=SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "POW30=NO-SIZE-ERROR"
           END-COMPUTE
           IF R > 999999999999999000000000000000
              AND R < 1000000000000001000000000000000
              DISPLAY "POW30=IN-RANGE"
           ELSE
              DISPLAY "POW30=WRONG"
           END-IF
           IF 10 ** 30 = 10 ** 31
              DISPLAY "POW-DISTINCT=NO"
           ELSE
              DISPLAY "POW-DISTINCT=YES"
           END-IF
           COMPUTE R = FUNCTION EXP(700)
               ON SIZE ERROR DISPLAY "EXP700=SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "EXP700=NO-SIZE-ERROR"
           END-COMPUTE
           DISPLAY "E31=[" FUNCTION EXP10(31) "]"
           STOP RUN.
