      *>--- conformance-case
      *> id: l1c19_multiply_standard_decimal
      *> rules: [GR-14.9.26.4-3]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.26.4 3) — MULTIPLY under STANDARD-DECIMAL: product =
      *>   expression
      *> "When format 1 or 2 is used and standard-decimal, or
      *>   standard-binary
      *> arithmetic is in effect, the product equals the result of the
      *>   arithmetic
      *> expression (multiplier * multiplicand) where the values for
      *>   multiplier
      *> and multiplicand are as defined in General rules 1 and 2".
      *> OK  §14.9.26.4 3)  (General rules)
      *> 11.9.5.2 3) STANDARD-DECIMAL selects 8.8.1.5 standard-decimal
      *>   arithmetic
      *> (34-digit decimal intermediate; every product below is exact
      *>   within it).
      *> The STANDARD-BINARY half is declined (docs/CONFORMANCE.md,
      *>   PB198); this
      *> golden pins the STANDARD-DECIMAL half.  OK  §11.9.5.2 3)
      *> Transfer into the resultant: no ROUNDED = TRUNCATION
      *>   OK  §14.7.4.3 2)  (General rules)
      *> ROUNDED without MODE: 11.9.6.3 2) "DEFAULT ROUNDED MODE IS
      *> NEAREST-AWAY-FROM-ZERO is implied"; 14.7.4.3 4) nearest, tie
      *>   away
      *> from zero.  OK  §14.7.4.3 4)  (General rules)
      *> Needs the 2014 OPTIONS paragraph (lowest edition 2014).
      *> Derivation (exact products by decimal arithmetic):
      *>  P1  12345678901234.5678 * 98765432.1234
      *>      = 1219326311537174198512.63526652 -> 4 decimals,
      *>        truncated:
      *>      1219326311537174198512.6352
      *>  P2  same, ROUNDED: 5th decimal is 6 -> .6353
      *>  F1  format 1: 1.1 * 1.25 = 1.375 stored in K (9V99), truncated
      *>    1.37
      *>  F1R same, ROUNDED: 1.375 -> 1.38 (nearest, away from zero on a
      *>    tie)
      *>  NEG format 1: 3.3 * -2.5 = -8.25 exactly
      *>  CAP V9(12) .5 * 9(10) 2 GIVING 9(20) = 1. Under NATIVE this
      *>    statement
      *>      is refused (14.9.26.3 SR4 composite 32 > 31); SR4 applies
      *>        only
      *>      "When native arithmetic is in effect", so here it is legal
      *>        and
      *>      the product is the expression value 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19F.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(14)V9(4) VALUE 12345678901234.5678.
       01 B PIC 9(8)V9(4) VALUE 98765432.1234.
       01 R1 PIC 9(22).9(4).
       01 R2 PIC 9(22).9(4).
       01 K PIC 9V99 VALUE 1.25.
       01 K2 PIC 9V99 VALUE 1.25.
       01 S PIC S9V99 VALUE -2.5.
       01 E PIC -9.99.
       01 C1 PIC V9(12) VALUE .5.
       01 C2 PIC 9(10) VALUE 2.
       01 C3 PIC 9(20).
       PROCEDURE DIVISION.
           MULTIPLY A BY B GIVING R1
           DISPLAY "P1=" R1
           MULTIPLY A BY B GIVING R2 ROUNDED
           DISPLAY "P2=" R2
           MULTIPLY 1.1 BY K
           MOVE K TO E
           DISPLAY "F1=" E
           MULTIPLY 1.1 BY K2 ROUNDED
           MOVE K2 TO E
           DISPLAY "F1R=" E
           MULTIPLY 3.3 BY S
           MOVE S TO E
           DISPLAY "NEG=" E
           MULTIPLY C1 BY C2 GIVING C3
           DISPLAY "CAP=" C3
           STOP RUN.
