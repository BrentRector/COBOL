      *>--- conformance-case
      *> id: l1_integer_floor_85
      *> rules: [AR-15.44.3-1, RV-15.44.4-1, RV-15.49.4-1, FMT-15.44.2,
      *>    FMT-15.49.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.44.4 r1 — FUNCTION INTEGER returns "the greatest integer less than or equal to the value of
      *> argument-1", exercised at --std 85 (the edition leg conformance:cases/15.42.3-Argument-rule/l1_integer_family_notes cannot
      *> reach: the OPTIONS paragraph's ARITHMETIC clause and FUNCTION FRACTION-PART are both post-85).
      *>
      *> WHY THIS ONE IS STILL NORMATIVE UNDER NATIVE ARITHMETIC, WHICH IS ALL COBOL-85 HAS. §15.4.1 makes a
      *> returned value under native arithmetic "an implementor-defined approximation" only where the function
      *> is defined BY an equivalent arithmetic expression, and closes with "When a numeric or integer function
      *> does not have an equivalent arithmetic expression, its returned value is implementor-defined unless
      *> otherwise specified in the function definition." §15.44.4 r1 has no EAE and specifies the value
      *> outright, so the I-* lines below are required at every edition and in every arithmetic mode.
      *>
      *> §15.44.4 r1 applied to each argument (all exact terminating decimals):
      *>   -1.5  -> -2   (-2 <= -1.5; -1 is not)             [§15.44.4 NOTE: -1.5 returns -2]
      *>   +1.5  -> +1                                        [§15.44.4 NOTE: +1.5 returns +1]
      *>    0    ->  0                                        [§15.44.4 NOTE: zero returns zero]
      *>   -1.0  -> -1   (an EXACT integer — the floor does not step down to -2)
      *>   +1.0  -> +1
      *>   -0.5  -> -1   (-1 <= -0.5; 0 is not — a truncate-toward-zero body returns 0 here)
      *>   -0.25 -> -1   (same, with a magnitude a truncating body would also report as 0)
      *>
      *> §15.49.4 r1's INTEGER-PART is written alongside on the SAME arguments, because §15.44.4's own closing
      *> sentence is "The INTEGER-PART function is similar but returns different values for negative numbers"
      *> — the contrast is part of what the rule says. Its EAE is
      *>   (FUNCTION SIGN(a) * FUNCTION INTEGER(FUNCTION ABS(a)))   §15.81.4 r1 a/b/c for SIGN, §15.7.1 for ABS
      *>   -1.5 -> (-1)*INTEGER(1.5)=-1   +1.5 -> +1   0 -> 0   -1.0 -> -1   +1.0 -> +1
      *>   -0.5 -> (-1)*INTEGER(0.5)=(-1)*0=0          -0.25 -> (-1)*INTEGER(0.25)=0
      *> ⚠ Under COBOL-85's native arithmetic §15.4.1 licenses an implementor-defined APPROXIMATION of that
      *> expression, so the P-* lines are a DETERMINATION PIN, not a bare assertion. COBOL.NET's §15.4.1
      *> determination for the exact family is on record in docs/CONFORMANCE.md A.1 item 92, in terms:
      *> "Sign discipline is the spec's, not the carrier's — MOD floors and REM truncates (§15.64.4 /
      *> §15.77.4), INTEGER floors and INTEGER-PART truncates (§15.44 / §15.49)". Under that determination
      *> the native approximation IS the exact §15.49.4 r1 value, which is what these seven lines measure at
      *> --std 85 (each is an integer over an exact terminating-decimal argument, so any deviation changes the
      *> integer, not its precision). The same determination is what makes the pre-existing native-arithmetic
      *> golden conformance:cases/15.7.4-Returned-value-rule/pb2_float_argument_exact_family — no OPTIONS paragraph, so native — evidence
      *> for INTEGER-PART(-3.5) = -3; item 92 names that golden as its pin. The strictly NORMATIVE pin for
      *> §15.49.4 r1 remains the STANDARD-DECIMAL golden conformance:cases/15.42.3-Argument-rule/l1_integer_family_notes, where
      *> §15.4.1 rule 1 makes equality mandatory; this file adds the 85 edition leg the row otherwise leaves
      *> unmeasured.
      *>
      *> Rendering: the receiver's leftmost symbol is the FIXED INSERTION '+', which by §13.18.40.5 rule 5,
      *> Table 8 prints '+' for a positive or zero value and '-' for a negative one, so the sign is asserted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1INTFLR85.
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
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION INTEGER(A-M15)      TO SI
           DISPLAY "I-M15=" SI
           MOVE FUNCTION INTEGER(A-P15)      TO SI
           DISPLAY "I-P15=" SI
           MOVE FUNCTION INTEGER(A-ZRO)      TO SI
           DISPLAY "I-ZRO=" SI
           MOVE FUNCTION INTEGER(A-M10)      TO SI
           DISPLAY "I-M10=" SI
           MOVE FUNCTION INTEGER(A-P10)      TO SI
           DISPLAY "I-P10=" SI
           MOVE FUNCTION INTEGER(A-M05)      TO SI
           DISPLAY "I-M05=" SI
           MOVE FUNCTION INTEGER(A-M25)      TO SI
           DISPLAY "I-M25=" SI
           MOVE FUNCTION INTEGER-PART(A-M15) TO SI
           DISPLAY "P-M15=" SI
           MOVE FUNCTION INTEGER-PART(A-P15) TO SI
           DISPLAY "P-P15=" SI
           MOVE FUNCTION INTEGER-PART(A-ZRO) TO SI
           DISPLAY "P-ZRO=" SI
           MOVE FUNCTION INTEGER-PART(A-M10) TO SI
           DISPLAY "P-M10=" SI
           MOVE FUNCTION INTEGER-PART(A-P10) TO SI
           DISPLAY "P-P10=" SI
           MOVE FUNCTION INTEGER-PART(A-M05) TO SI
           DISPLAY "P-M05=" SI
           MOVE FUNCTION INTEGER-PART(A-M25) TO SI
           DISPLAY "P-M25=" SI
           STOP RUN.
