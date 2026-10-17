*>--- conformance-case
*> id: pb13_float_quantize_headroom
*> rules: [RV-15.4.1-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB13 - the float->fixed quantizer's saturation is REACHABLE and SILENT (FIXED; enabled). This pins the
      *> CORRECT behaviour; it was drafted before the fix so the repro was executable and
      *> spec-derived the moment the fix lands, rather than living only in a prose ledger.
      *>
      *> IntrinsicRenderer#RenderFloat quantizes through CobolIntrinsics#FromDouble at ws = max(Receiver.Scale, 9),
      *> and FromDouble saturates at |scaled| >= 1.7e38. At ws = 9 the intermediate needs
      *> (receiver integer digits + 9) decimal digits and Int128 supplies only ~38, so a 31-digit receiver is two
      *> digits short. PB5 fixed one instance and recorded the clamp as "unreachable from a declarable receiver";
      *> that premise is false twice - PictureAnalyzer caps digits at 31 (CA33), so PIC 9(31) is legal, and the
      *> clamp fires at the FUNCTION's quantization point, before any store, so it needs no receiver at all.
      *>
      *> CASE 1 - 15.34.4 r1 makes the equivalent arithmetic expression (FUNCTION E ** argument-1) and 15.4.1
      *> permits an implementor-defined APPROXIMATION of it under native arithmetic. e**70 = 2.5154386709191670E30
      *> fits a 31-digit receiver, so 14.7.4 raises nothing and the receiver must hold that approximation.
      *> Int128.MaxValue/10**9 is not an approximation of it - it is wrong by a factor of about fifteen.
      *>
      *> CASE 2 - the receiver-free half, and the sharper one. A relation operand renders under
      *> ReceiverContext.None (scale 0, so ws = 9), so BOTH sides saturate to the same Int128.MaxValue and two
      *> values a FACTOR OF TEN apart compare EQUAL. 15.35.4 r1 gives EXP10 the equivalent expression
      *> (10 ** argument-1), so 10**30 and 10**31 are distinct values and the relation is FALSE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB13QUANT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(31).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION EXP(70)
               ON SIZE ERROR DISPLAY "SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "NO-SIZE-ERROR"
           END-COMPUTE
      *> The leading digits of e**70; the tail is 15.4.1 approximation latitude, so only the magnitude and the
      *> leading digits are asserted - a saturated result differs in the FIRST digit, which is what this catches.
           IF R > 2515438670000000000000000000000
              AND R < 2515438680000000000000000000000
              DISPLAY "EXP70=IN-RANGE"
           ELSE
              DISPLAY "EXP70=WRONG"
           END-IF
           IF FUNCTION EXP10(30) = FUNCTION EXP10(31)
              DISPLAY "EXP10-DISTINCT=NO"
           ELSE
              DISPLAY "EXP10-DISTINCT=YES"
           END-IF
           STOP RUN.
