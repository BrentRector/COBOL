*>--- conformance-case
*> id: l1_integer_family_notes
*> rules: [AR-15.42.3-1, RV-15.42.4-1, RV-15.44.4-1, RV-15.49.4-1,
*>    FMT-15.42.2, FMT-15.44.2, FMT-15.49.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.44.4 r1 / §15.49.4 r1 / §15.42.4 r1 — INTEGER (the floor), INTEGER-PART (the split at zero)
      *> and FRACTION-PART (the remainder of THAT split) read off ONE argument ladder, so the three rules are
      *> forced to disagree line-by-line on exactly the arguments where the standard says they must.
      *>
      *> ⛔ `ARITHMETIC IS STANDARD-DECIMAL` IS LOAD-BEARING, NOT DECORATION. §15.49.4 r1 and §15.42.4 r1
      *> define their functions by an EQUIVALENT ARITHMETIC EXPRESSION, and §15.4.1 says that under NATIVE
      *> arithmetic "the value returned is an implementor-defined approximation of the value of that
      *> expression" — which is nothing a conformance golden is entitled to assert. Under a standard mode
      *> §15.4.1 rule 1 is "the returned value shall equal the value of the equivalent arithmetic expression",
      *> so every P-* and F-* line below is NORMATIVE rather than merely expected. §15.44.4 r1 states
      *> INTEGER's value DIRECTLY (that function has no equivalent arithmetic expression), so its I-* lines
      *> are normative in every mode — pinned again under native arithmetic by conformance:cases/15.44.3-Argument-rule/l1_integer_floor_85.
      *>
      *> THE DERIVATION, per argument a (every argument is an exact terminating decimal, so no rounding of the
      *> ARGUMENT is in play and only the three rules decide):
      *>   INTEGER(a)       §15.44.4 r1 — the greatest integer less than or equal to a.
      *>   INTEGER-PART(a)  §15.49.4 r1 — (FUNCTION SIGN(a) * FUNCTION INTEGER(FUNCTION ABS(a))), where SIGN
      *>                    is +1 / 0 / -1 by §15.81.4 r1 a) b) c) and ABS is the absolute value (§15.7.1).
      *>   FRACTION-PART(a) §15.42.4 r1 — (a - FUNCTION INTEGER-PART(a)).
      *>
      *>   a = -1.5   INTEGER  = -2      (-2 <= -1.5, and -1 is not)
      *>              IPART    = (-1) * INTEGER(1.5) = (-1) * 1 = -1   [§15.49.4 NOTE: -1.5 returns -1]
      *>              FRACTION = -1.5 - (-1) = -0.5                    [§15.42.4 NOTE: -1.5 returns -0.5]
      *>   a = +1.5   INTEGER  = +1      IPART = (+1) * INTEGER(1.5) = +1   [§15.49.4 NOTE: +1.5 returns +1]
      *>              FRACTION = 1.5 - 1 = +0.5                        [§15.42.4 NOTE: +1.5 returns +0.5]
      *>   a = 0      INTEGER  = 0       [§15.44.4 NOTE: zero returns zero]
      *>              IPART    = (0) * INTEGER(0) = 0                  [§15.49.4 NOTE: zero returns zero]
      *>              FRACTION = 0 - 0 = 0
      *>   a = -1.0   INTEGER  = -1      (a IS an integer, so the floor does not step down)
      *>              IPART    = (-1) * INTEGER(1.0) = -1              [§15.49.4 NOTE: -1.0 returns -1]
      *>              FRACTION = -1.0 - (-1) = 0
      *>   a = +1.0   INTEGER  = +1      IPART = (+1) * INTEGER(1.0) = +1  [§15.49.4 NOTE: +1.0 returns +1]
      *>              FRACTION = 1.0 - 1 = 0
      *>   a = -0.5   INTEGER  = -1      (-1 <= -0.5, and 0 is not)
      *>              IPART    = (-1) * INTEGER(0.5) = (-1) * 0 = 0
      *>              FRACTION = -0.5 - 0 = -0.5
      *>   a = -0.25  INTEGER  = -1      IPART = (-1) * INTEGER(0.25) = (-1) * 0 = 0
      *>              FRACTION = -0.25 - 0 = -0.25
      *>
      *> ⚠ THE LAST TWO ARGUMENTS REACH THE |a| < 1 REGION, AND THEY ARE WHY THIS FILE EXISTS. A FRACTION-PART
      *> written as (a - INTEGER(a)) — the FLOOR split instead of §15.42.4 r1's INTEGER-PART split — agrees
      *> with every line here on every POSITIVE argument and returns +0.5 / +0.75 where the rule requires
      *> -0.5 / -0.25. Likewise an INTEGER-PART written as a floor returns -1 where r1 requires 0.
      *> A fixture built from the §15.42.4 NOTE's ±1.5 pair ALONE already separates the two bodies at -1.5:
      *> §15.44.4 r1 makes INTEGER(-1.5) = -2 — this file asserts exactly that on its own I-M15 line — so the
      *> floor split returns -1.5 - (-2) = +0.5 where r1 requires -0.5, and a floor-bodied INTEGER-PART returns
      *> -2 where r1 requires -1 — the two INTEGER-PART bodies differ at -1.5 too. What the ±1.5 pair
      *> cannot reach is |a| < 1: the region where the INTEGER-PART subtrahend is ZERO and the argument passes
      *> into FRACTION-PART whole, so r1 requires a itself (-0.5, -0.25) while the floor split returns 1 + a
      *> (+0.5, +0.75) and INTEGER-PART must produce a magnitude-zero result whose SIGN factor is -1. Those are
      *> the a = -0.5 and a = -0.25 lines.
      *>
      *> THE RENDERING IS ITSELF ASSERTED: each result is MOVEd to a numeric-edited receiver whose leftmost
      *> symbol is the FIXED INSERTION '+', which by §13.18.40.5 rule 5, Table 8 prints '+' for a positive OR
      *> ZERO value and '-' for a negative one — so the SIGN of every line is pinned, not just its digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1INTFAM01.
       OPTIONS. ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A-M15 PIC S9V99 VALUE -1.5.
       01 A-P15 PIC S9V99 VALUE +1.5.
       01 A-ZRO PIC S9V99 VALUE 0.
       01 A-M10 PIC S9V99 VALUE -1.0.
       01 A-P10 PIC S9V99 VALUE +1.0.
       01 A-M05 PIC S9V99 VALUE -0.5.
       01 A-M25 PIC S9V99 VALUE -0.25.
       01 SI    PIC +99.
       01 SF    PIC +9.99.
       PROCEDURE DIVISION.
       MAIN.
      *> a = -1.5
           MOVE FUNCTION INTEGER(A-M15)       TO SI
           DISPLAY "I-M15=" SI
           MOVE FUNCTION INTEGER-PART(A-M15)  TO SI
           DISPLAY "P-M15=" SI
           MOVE FUNCTION FRACTION-PART(A-M15) TO SF
           DISPLAY "F-M15=" SF
      *> a = +1.5
           MOVE FUNCTION INTEGER(A-P15)       TO SI
           DISPLAY "I-P15=" SI
           MOVE FUNCTION INTEGER-PART(A-P15)  TO SI
           DISPLAY "P-P15=" SI
           MOVE FUNCTION FRACTION-PART(A-P15) TO SF
           DISPLAY "F-P15=" SF
      *> a = 0
           MOVE FUNCTION INTEGER(A-ZRO)       TO SI
           DISPLAY "I-ZRO=" SI
           MOVE FUNCTION INTEGER-PART(A-ZRO)  TO SI
           DISPLAY "P-ZRO=" SI
           MOVE FUNCTION FRACTION-PART(A-ZRO) TO SF
           DISPLAY "F-ZRO=" SF
      *> a = -1.0 — an EXACT negative integer: the floor must not step down to -2.
           MOVE FUNCTION INTEGER(A-M10)       TO SI
           DISPLAY "I-M10=" SI
           MOVE FUNCTION INTEGER-PART(A-M10)  TO SI
           DISPLAY "P-M10=" SI
           MOVE FUNCTION FRACTION-PART(A-M10) TO SF
           DISPLAY "F-M10=" SF
      *> a = +1.0
           MOVE FUNCTION INTEGER(A-P10)       TO SI
           DISPLAY "I-P10=" SI
           MOVE FUNCTION INTEGER-PART(A-P10)  TO SI
           DISPLAY "P-P10=" SI
           MOVE FUNCTION FRACTION-PART(A-P10) TO SF
           DISPLAY "F-P10=" SF
      *> a = -0.5 — the discriminator: floor -1, integer part 0, fraction -0.5.
           MOVE FUNCTION INTEGER(A-M05)       TO SI
           DISPLAY "I-M05=" SI
           MOVE FUNCTION INTEGER-PART(A-M05)  TO SI
           DISPLAY "P-M05=" SI
           MOVE FUNCTION FRACTION-PART(A-M05) TO SF
           DISPLAY "F-M05=" SF
      *> a = -0.25 — the same discriminator with a fraction the floor split would report as +0.75.
           MOVE FUNCTION INTEGER(A-M25)       TO SI
           DISPLAY "I-M25=" SI
           MOVE FUNCTION INTEGER-PART(A-M25)  TO SI
           DISPLAY "P-M25=" SI
           MOVE FUNCTION FRACTION-PART(A-M25) TO SF
           DISPLAY "F-M25=" SF
           STOP RUN.
