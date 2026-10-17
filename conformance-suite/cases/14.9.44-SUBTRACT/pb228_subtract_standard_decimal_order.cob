      *>--- conformance-case
      *> id: pb228_subtract_standard_decimal_order
      *> rules: [GR-14.9.44.4-1, GR-14.9.44.4-2]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.4 GR1 and GR2, SECOND PARAGRAPH of each - the
      *> SUBTRACT twins of the ADD rules 2014/pb228_add_standard_decimal
      *> pins (kb/Work PB228's sibling sweep). GR1: "the result of the
      *> initial evaluation is equivalent to the result of the
      *> arithmetic expression (operand-11 + operand-12 + ... +
      *> operand-1n)", and each receiver's result is "(identifier-2 -
      *> initial-evaluation)". GR2: "(operand-2 - (operand-11 +
      *> operand-12 + ... + operand-1n))" - the subtrahends are summed
      *> FIRST, then taken from operand-2 in one subtraction.
      *> 2023/l1_subtract_format1_standard_decimal pins GR1 with values
      *> that are exact in every order; this golden is the one whose
      *> answer depends on the grouping. STANDARD-BINARY is a declared
      *> non-support (docs/CONFORMANCE.md A.3) and is not exercised.
      *>
      *> An SDIDI holds 34 decimal digits (§8.8.1.5.2), each operation
      *> is a correctly rounded decimal128 one (§8.8.1.5.3), rounding
      *> NEAREST-AWAY-FROM-ZERO when no INTERMEDIATE ROUNDING clause is
      *> written (§11.9.11.2 GR3a); stores truncate (§14.7.4.3 GR2).
      *> X = 10**30, Y = -(10**30), Z = 0.0004: X + Z needs 35 digits
      *> and rounds to X, so the sum (X + Z) + Y is 0, not 0.0004.
      *>
      *> FROM   SUBTRACT X Z Y FROM R (R = 0.0008): initial evaluation
      *>        (X + Z) + Y = 0, so R = 0.0008 - 0 = 0.0008. Native
      *>        exact arithmetic gives 0.0004, and so does taking the
      *>        operands from R one at a time, ((R - X) - Z) - Y,
      *>        whose every step fits in 34 digits and is exact.
      *> GIVING SUBTRACT X Z Y FROM W GIVING G (W = 0.0008): the
      *>        rule's grouping gives 0.0008; native arithmetic and a
      *>        left-to-right ((W - X) - Z) - Y both give 0.0004.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB228SSD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC S9(31) VALUE  1000000000000000000000000000000.
       01 Y  PIC S9(31) VALUE -1000000000000000000000000000000.
       01 Z  PIC SV9(4) VALUE 0.0004.
       01 R  PIC 9V9(4) VALUE 0.0008.
       01 W  PIC 9V9(4) VALUE 0.0008.
       01 G  PIC 9V9(4).
       01 ED PIC 9.9999.
       PROCEDURE DIVISION.
       MAIN.
           SUBTRACT X Z Y FROM R
           MOVE R TO ED
           DISPLAY "FROM   R=" ED
           SUBTRACT X Z Y FROM W GIVING G
           MOVE G TO ED
           DISPLAY "GIVING G=" ED
           STOP RUN.
