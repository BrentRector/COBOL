      *> kb/Work PB1471 — the Format 1 / Format 2 partition of the simple sign condition is keyed
      *> on the STANDARD floating-point usages, not on "any float":
      *>   §8.8.4.7.3 SR1: arithmetic-expression-1 "shall be any single numeric data item
      *>     described with a usage other than a standard floating-point usage, or any form of
      *>     arithmetic expression."                       cite.py: OK  §8.8.4.7.3 1)
      *>   §8.8.4.7.3 SR2: data-name-1 "described with a standard floating-point usage, and that
      *>     name shall not be enclosed in parentheses."   cite.py: OK  §8.8.4.7.3 2)
      *>   §3.166 "usages float-binary-32, float-binary-64, and float-binary-128"; §3.167
      *>     "usages float-decimal-16 and float-decimal-34"  cite.py: OK  §3.166 / §3.167
      *>   §8.8.4.7.4 GR2 a) (Format 2): POSITIVE is "true if the sign of the content of the data
      *>     item identified by data-name-1 is positive"   cite.py: OK  §8.8.4.7.4 2)
      *>   §8.8.4.7.4 GR1 a)/b) (Format 1): POSITIVE "false if the value is zero or less than
      *>     zero"; NEGATIVE "false if it is zero or greater than zero"  cite.py: OK §8.8.4.7.4 1)
      *> Directory 2014: FLOAT-BINARY-* (and so Format 2) enter with ISO/IEC 1989:2014.
      *>
      *> DERIVATION — one +0.0 / -0.0 pair per usage:
      *>   FB (FLOAT-BINARY-64, bare)  +0.0 POSITIVE -> T (sign bit clear, GR2 a)
      *>   FB32 (FLOAT-BINARY-32)      +0.0 POSITIVE -> T
      *>   (FB) parenthesized          +0.0 POSITIVE -> F (SR1 NOTE: Format 1, GR1 a)
      *>   FL (FLOAT-LONG, bare)       +0.0 POSITIVE -> F (not standard: Format 1, GR1 a)
      *>   EVALUATE FB WHEN POSITIVE   +0.0          -> POS (GR2 a)
      *>   EVALUATE FL WHEN POSITIVE   +0.0          -> OTHER (GR1 a)
      *>   after * -1 (-0.0):
      *>   FB NEGATIVE -> T (sign bit set, GR2 b);  FB ZERO -> T (GR2 c, sign-agnostic)
      *>   FL NEGATIVE -> F (GR1 b);                FL ZERO -> T (GR1 c)
      *> The NOTE's non-finite edges — GR2 answers "regardless of whether the content of that
      *> item would evaluate to true in a NUMERIC class test or a ZERO sign test" (cite.py: OK
      *> §8.8.4.7.4 2)); SET CONTENT (§14.9.39.4 GR33/34: "otherwise the sign is positive",
      *> cite.py: OK §14.9.39.4 33)) builds them:
      *>   -Inf : POSITIVE F, NEGATIVE T, ZERO F
      *>   +NaN : POSITIVE T, NEGATIVE F, ZERO F  (sign bit clear; a NaN is no zero)
      *>   -NaN : POSITIVE F, NEGATIVE T
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1471P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FB   USAGE FLOAT-BINARY-64.
       01 FB32 USAGE FLOAT-BINARY-32.
       01 FL   USAGE FLOAT-LONG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO FB FB32 FL.
           IF FB IS POSITIVE DISPLAY "FB-P0-POS T" ELSE DISPLAY "FB-P0-POS F".
           IF FB32 IS POSITIVE DISPLAY "FB32-P0-POS T"
               ELSE DISPLAY "FB32-P0-POS F".
           IF (FB) IS POSITIVE DISPLAY "PAREN-P0-POS T"
               ELSE DISPLAY "PAREN-P0-POS F".
           IF FL IS POSITIVE DISPLAY "FL-P0-POS T" ELSE DISPLAY "FL-P0-POS F".
           EVALUATE FB
               WHEN POSITIVE DISPLAY "EV-FB-P0 POS"
               WHEN OTHER    DISPLAY "EV-FB-P0 OTHER"
           END-EVALUATE.
           EVALUATE FL
               WHEN POSITIVE DISPLAY "EV-FL-P0 POS"
               WHEN OTHER    DISPLAY "EV-FL-P0 OTHER"
           END-EVALUATE.
           COMPUTE FB = FB * -1.
           COMPUTE FL = FL * -1.
           IF FB IS NEGATIVE DISPLAY "FB-N0-NEG T" ELSE DISPLAY "FB-N0-NEG F".
           IF FB IS ZERO DISPLAY "FB-N0-ZERO T" ELSE DISPLAY "FB-N0-ZERO F".
           IF FL IS NEGATIVE DISPLAY "FL-N0-NEG T" ELSE DISPLAY "FL-N0-NEG F".
           IF FL IS ZERO DISPLAY "FL-N0-ZERO T" ELSE DISPLAY "FL-N0-ZERO F".
           SET CONTENT OF FB TO FLOAT-INFINITY SIGN NEGATIVE.
           IF FB IS POSITIVE DISPLAY "NINF-POS T" ELSE DISPLAY "NINF-POS F".
           IF FB IS NEGATIVE DISPLAY "NINF-NEG T" ELSE DISPLAY "NINF-NEG F".
           IF FB IS ZERO DISPLAY "NINF-ZERO T" ELSE DISPLAY "NINF-ZERO F".
           SET CONTENT OF FB TO FLOAT-NOT-A-NUMBER.
           IF FB IS POSITIVE DISPLAY "PNAN-POS T" ELSE DISPLAY "PNAN-POS F".
           IF FB IS NEGATIVE DISPLAY "PNAN-NEG T" ELSE DISPLAY "PNAN-NEG F".
           IF FB IS ZERO DISPLAY "PNAN-ZERO T" ELSE DISPLAY "PNAN-ZERO F".
           SET CONTENT OF FB TO FLOAT-NOT-A-NUMBER SIGN NEGATIVE.
           IF FB IS POSITIVE DISPLAY "NNAN-POS T" ELSE DISPLAY "NNAN-POS F".
           IF FB IS NEGATIVE DISPLAY "NNAN-NEG T" ELSE DISPLAY "NNAN-NEG F".
           STOP RUN.
