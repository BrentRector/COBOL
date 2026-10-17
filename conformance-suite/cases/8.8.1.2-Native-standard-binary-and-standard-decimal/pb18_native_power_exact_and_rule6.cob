*>--- conformance-case
*> id: pb18_native_power_exact_and_rule6
*> rules: [GR-8.8.1.2-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> NATIVE ** IS EXACT WHEN THE RESULT FITS, AND SS8.8.1.2 RULE 6 BINDS IT.
      *> 8.8.1.3 makes native arithmetic implementor-defined, so the TECHNIQUE is ours to choose; owner decision
      *> 2026-08-03 chose "exact Int128 while it fits, the documented double approximation past it", following
      *> the field (IBM and Micro Focus fall back to floating point past the fixed capacity; GnuCOBOL is
      *> arbitrary-precision and has no boundary). What is NOT implementor-defined is rule 6, whose own title is
      *> "Native, standard-binary, and standard-decimal arithmetic" - it binds native ** exactly as it binds the
      *> SDIDI one, and two of its three parts are mandatory 'shall' requirements.
      *>
      *> ⛔ THIS GOLDEN EXISTS BECAUSE THREE THINGS WERE WRONG AT ONCE (fix-queue PB18 + PB28 + PB32).
      *>   10 ** 30   returned 1000000000000000071935427891953  - a double artifact where Int128 holds it exactly,
      *>              contradicting our OWN documented native technique (numeric design D3).
      *>   0 ** 0     returned 1 with NO SIZE ERROR - IEEE's convention, not COBOL's (rule 6a).
      *>   -2 ** 0.5  returned 0 with NO SIZE ERROR - Math.Pow yields NaN and FromDouble quantized it to zero
      *>              (rule 6c). CobolDec.Pow had enforced both legs since it was written; every NATIVE arm
      *>              ignored them, so the same program answered differently depending only on whether an
      *>              ARITHMETIC clause was present.
      *>
      *> ⚠ AND THE FIX HAD TO NOT BREAK THE CASES THAT WERE ALREADY RIGHT. A negative exponent is rule 6's
      *> RECIPROCAL and is not an integer, so an exact-integer arm that ignores the landing scale turns
      *> 2 ** -2 into 0 - a regression the first cut introduced and probing caught. A fractional BASE keeps the
      *> approximation arm entirely, because a scale-s base to the n has scale s*n and no compile-time scale.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB18POWER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B31 PIC 9(31).
       01 R   PIC S9(9)V9(4).
       01 I   PIC S9(9).
       01 N   PIC S9(4)V9(4) VALUE -2.
       01 H   PIC S9(4)V9(4) VALUE 0.5.
       01 A   PIC S9(18)     VALUE 100000000.
       01 D   PIC S9(18)     VALUE 1000000007.
       PROCEDURE DIVISION.
      *> PB18 - exact, not an approximation. Int128 holds 10^30; the old answer ended ...071935427891953.
           COMPUTE B31 = 10 ** 30
           DISPLAY "POW-EXACT=" B31
      *> PB28 rule 6a - a zero base requires an exponent greater than zero.
           MOVE 7 TO I
           COMPUTE I = 0 ** 0 ON SIZE ERROR DISPLAY "R6A-SIZE-ERROR"
           END-COMPUTE
           DISPLAY "R6A-RECEIVER=" I
      *> PB28 rule 6c - a negative base requires an integer exponent.
           MOVE 7 TO R
           COMPUTE R = N ** H ON SIZE ERROR DISPLAY "R6C-SIZE-ERROR"
           END-COMPUTE
           DISPLAY "R6C-RECEIVER=" R
      *> ⚠ THE CASES THAT MUST BE UNAFFECTED.
           COMPUTE R = 2 ** -2
           DISPLAY "NEG-EXP=" R
           COMPUTE R = N ** 3
           DISPLAY "NEG-BASE-INT-EXP=" R
           COMPUTE R = 1.5 ** 2
           DISPLAY "FRAC-BASE=" R
           COMPUTE I = 2 ** 10
           DISPLAY "SMALL=" I
      *> PB32's remaining half: a function's value must not depend on the SHAPE of its receiver (15.4).
      *> `A ** 2` was exact under COMPUTE and binary64 under DISPLAY / an IF subject, which routed FUNCTION MOD
      *> to a DIFFERENT BODY: 930000007 against 930000008, so the relation below evaluated FALSE.
           COMPUTE I = FUNCTION MOD(A ** 2, D)
           DISPLAY "MOD-COMPUTE=" I
           DISPLAY "MOD-DISPLAY=" FUNCTION MOD(A ** 2, D)
           IF FUNCTION MOD(A ** 2, D) = 930000007
               DISPLAY "MOD-SHAPES-AGREE=YES"
           ELSE
               DISPLAY "MOD-SHAPES-AGREE=NO"
           END-IF
           STOP RUN.
