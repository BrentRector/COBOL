      *> kb/Work PB1471 (rewrites CA8). A sign condition on a bare IMPLEMENTOR-DEFINED float name
      *> (FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED) is ISO §8.8.4.7 FORMAT 1, not Format 2:
      *>   §8.8.4.7.3 SR1: "Arithmetic-expression-1 shall be any single numeric data item described
      *>     with a usage other than a standard floating-point usage, or any form of arithmetic
      *>     expression."                                  cite.py: OK  §8.8.4.7.3 1)
      *>   §8.8.4.7.3 SR2: data-name-1 "described with a standard floating-point usage"
      *>                                                   cite.py: OK  §8.8.4.7.3 2)
      *>   §3.166 standard binary float = "usages float-binary-32, float-binary-64, and
      *>     float-binary-128"; §3.167 standard decimal float = "usages float-decimal-16 and
      *>     float-decimal-34"                             cite.py: OK  §3.166 / §3.167
      *> so FLOAT-LONG is outside both, and §8.8.4.7.4 GR1's ALGEBRAIC test applies:
      *>   GR1 a) POSITIVE "false if the value is zero or less than zero"
      *>   GR1 b) NEGATIVE "false if it is zero or greater than zero"
      *>   GR1 c) ZERO "true if the value is zero"         cite.py: OK  §8.8.4.7.4 1)
      *> The earlier CA8 golden expected the Format-2 sign-BIT answer (+0.0 POSITIVE, -0.0
      *> NEGATIVE) on FLOAT-LONG; that pinned the defect. Format 2 itself (FLOAT-BINARY-64) is
      *> pinned at 2014 by conformance:2014/pb1471_sign_condition_float_partition.
      *>
      *> DERIVATION (every value is +0.0, then -0.0, then 3.5 — all exact):
      *>   FL/FS/FX = +0.0 : IS POSITIVE -> NOTPOS (zero), IS ZERO -> ZERO
      *>   FL = -0.0       : IS NEGATIVE -> NOTNEG (zero), IS ZERO -> ZERO
      *>   EVALUATE FL WHEN POSITIVE on -0.0 / +0.0 -> OTHER (zero is not > 0)
      *>   FL = 3.5        : IS POSITIVE -> NORMAL-POS
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FL USAGE FLOAT-LONG.
       01 FS USAGE FLOAT-SHORT.
       01 FX USAGE FLOAT-EXTENDED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO FL FS FX
           IF FL IS POSITIVE
               DISPLAY "FL-PZERO-POS"
           ELSE
               DISPLAY "FL-PZERO-NOTPOS"
           END-IF
           IF FS IS POSITIVE
               DISPLAY "FS-PZERO-POS"
           ELSE
               DISPLAY "FS-PZERO-NOTPOS"
           END-IF
           IF FX IS POSITIVE
               DISPLAY "FX-PZERO-POS"
           ELSE
               DISPLAY "FX-PZERO-NOTPOS"
           END-IF
           IF (FL) IS POSITIVE
               DISPLAY "PAREN-PZERO-POS"
           ELSE
               DISPLAY "PAREN-PZERO-NOTPOS"
           END-IF
           IF FL IS ZERO
               DISPLAY "FL-PZERO-ZERO"
           ELSE
               DISPLAY "FL-PZERO-NOTZERO"
           END-IF
           EVALUATE FL
               WHEN POSITIVE DISPLAY "EV-PZERO-POS"
               WHEN OTHER    DISPLAY "EV-PZERO-OTHER"
           END-EVALUATE
           COMPUTE FL = FL * -1
           IF FL IS NEGATIVE
               DISPLAY "FL-NZERO-NEG"
           ELSE
               DISPLAY "FL-NZERO-NOTNEG"
           END-IF
           IF FL IS ZERO
               DISPLAY "FL-NZERO-ZERO"
           ELSE
               DISPLAY "FL-NZERO-NOTZERO"
           END-IF
           EVALUATE FL
               WHEN NEGATIVE DISPLAY "EV-NZERO-NEG"
               WHEN OTHER    DISPLAY "EV-NZERO-OTHER"
           END-EVALUATE
           MOVE 3.5 TO FL
           IF FL IS POSITIVE
               DISPLAY "NORMAL-POS"
           END-IF
           STOP RUN.
