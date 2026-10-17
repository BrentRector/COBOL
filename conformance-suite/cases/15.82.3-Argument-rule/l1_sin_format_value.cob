      *>--- conformance-case
      *> id: l1_sin_format_value
      *> rules: [AR-15.82.3-1, RV-15.82.4-1, FMT-15.82.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.82.2 general format and §15.82.4 r1 — SIN's shape and
      *> its returned value, including the CODOMAIN half of the rule
      *> Format: "<u>FUNCTION</u> <u>SIN</u> ( argument-1 )" - both
      *> words underlined (required), exactly one argument in required
      *> parentheses, nothing bracketed, so no optional part exists.
      *> No conformance golden named SIN at all before this one (the
      *> only corpus occurrences were nist/programs/IF135A.cob, a
      *> regression net that cannot close a row).
      *>
      *> THE FORMAT IS EXERCISED OVER EVERY SHAPE argument-1 ADMITS.
      *> ISO 15.3 type 10: "An arithmetic expression or a numeric data
      *> item shall be specified" - so LIT is a numeric literal, ITEM
      *> a numeric data item, EXPR an arithmetic expression and NEST a
      *> nested function-identifier. If the format were silently
      *> narrowed to a bare identifier, EXPR and NEST would fail.
      *>
      *> §15.82.4 r1: "The returned value is the approximation of the
      *> sine of argument-1 and is greater than or equal to - 1 and
      *> less than or equal to +1." The rule is a CONJUNCTION and each
      *> half is pinned separately.
      *>
      *> (a) THE APPROXIMATION. Only the exact points of the sine can
      *> be pinned as digits without borrowing the implementor's own
      *> accuracy: sin(0) = 0 exactly (LIT/ITEM/EXPR/NEST), and at
      *> the quarter-turn sin is +1 / -1 and at the sixth-turn +1/2.
      *> The quarter- and sixth-turn arguments are written as the
      *> 17-digit decimal values of pi/2 and pi/6, and the receiver
      *> ROUNDS at 6 fraction digits (ISO 14.7.4), so the expected
      *> digits are the exact mathematical values and any
      *> approximation accurate to better than 5e-7 - far coarser than
      *> anything 15.4.1's implementor-defined representation would
      *> deliver - produces them. The non-zero pins are what stop a
      *> stub that returns 0 for every argument from passing.
      *>
      *> (b) THE CODOMAIN. -1 <= value <= +1 is spec-HARD and holds
      *> for every argument, with no implementor latitude, so it is
      *> measured over a swept range rather than at chosen points:
      *> 2001 arguments from -10.00 to +10.00 in steps of 0.01 - some
      *> three full turns either side of zero, so the peaks and
      *> troughs of the sine are crossed repeatedly - and OOB counts
      *> how many returned values fell outside the closed interval.
      *> The receiver carries THREE integer digits so a violating
      *> value cannot be truncated back into range before the test
      *> sees it. PEAK re-tests the bound exactly AT the maximum,
      *> which is where a quantize-then-round arm can push a bounded
      *> value past its bound (that is what pb65 found for ASIN/ACOS).
      *>
      *> WHY SIN NEEDS NO CODOMAIN CLAMP, and why QUARTER is the line
      *> that proves it: 15.82.4's bounds are CLOSED and the bounding
      *> values are +1 and -1, both exactly representable at any
      *> scale, so quantizing a value already inside [-1,+1] can at
      *> worst land ON a bound and never outside it. That is the
      *> opposite of 15.75.4 RANDOM, whose numerically identical-
      *> looking "less than one" is OPEN and therefore does need the
      *> clamp pb65 pins. If SIN were ever given RANDOM's open-unit
      *> treatment by analogy, sin at the quarter-turn would be
      *> clamped BELOW 1 and QUARTER would drop to +0999999 - which
      *> is what makes this line a drift guard and not decoration.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SINFV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S-B  PIC S9(3)V9(9) SIGN LEADING SEPARATE.
       01 S-6  PIC S9V9(6) SIGN LEADING SEPARATE.
       01 W-Z  PIC S9V9(9) VALUE 0.
       01 W-Q  PIC S9V9(16) VALUE 1.5707963267948966.
       01 W-X  PIC S9V9(16) VALUE 0.5235987755982988.
       01 LOW  PIC S9 VALUE -1.
       01 HIGH PIC S9 VALUE 1.
       01 LO   PIC S9(5) VALUE -1000.
       01 IDX  PIC S9(5) VALUE 0.
       01 ARG  PIC S9(3)V9(4) VALUE 0.
       01 OOB  PIC 9(6) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE S-B = FUNCTION SIN(0)
           IF S-B = 0
               DISPLAY "LIT=ZERO"
           ELSE
               DISPLAY "LIT=NONZERO"
           END-IF
           COMPUTE S-B = FUNCTION SIN(W-Z)
           IF S-B = 0
               DISPLAY "ITEM=ZERO"
           ELSE
               DISPLAY "ITEM=NONZERO"
           END-IF
           COMPUTE S-B = FUNCTION SIN(W-Z + 0)
           IF S-B = 0
               DISPLAY "EXPR=ZERO"
           ELSE
               DISPLAY "EXPR=NONZERO"
           END-IF
           COMPUTE S-B = FUNCTION SIN(FUNCTION SIN(0))
           IF S-B = 0
               DISPLAY "NEST=ZERO"
           ELSE
               DISPLAY "NEST=NONZERO"
           END-IF
           COMPUTE S-6 ROUNDED = FUNCTION SIN(W-Q)
           DISPLAY "QUARTER=" S-6
           COMPUTE S-6 ROUNDED = FUNCTION SIN(0 - W-Q)
           DISPLAY "NQUARTER=" S-6
           COMPUTE S-6 ROUNDED = FUNCTION SIN(W-X)
           DISPLAY "SIXTH=" S-6
           COMPUTE S-B = FUNCTION SIN(W-Q)
           IF S-B > HIGH OR S-B < LOW
               DISPLAY "PEAK=OUT"
           ELSE
               DISPLAY "PEAK=IN"
           END-IF
           PERFORM VARYING IDX FROM LO BY 1 UNTIL IDX > 1000
               COMPUTE ARG = IDX / 100
               COMPUTE S-B = FUNCTION SIN(ARG)
               IF S-B > HIGH OR S-B < LOW
                   ADD 1 TO OOB
               END-IF
           END-PERFORM
           DISPLAY "OOB=" OOB
           STOP RUN.
