      *>--- conformance-case
      *> id: pb228_add_standard_decimal
      *> rules: [GR-14.9.2.4-1, GR-14.9.2.4-2, GR-14.9.2.4-3]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.2.4 GR1, GR2 and GR3, SECOND PARAGRAPH of each -
      *> the ADD rules that exist only under standard arithmetic.
      *> GR1: "the result of the initial evaluation is equivalent to
      *> the result of the arithmetic expression (operand-11 +
      *> operand-12 + ... + operand-1n)" in the order written, and the
      *> receiver's sum is "(initial-evaluation + identifier-2)".
      *> GR2: "(operand-11 + operand-12 + ... + operand-1n +
      *> operand-2)", operand-2 being the TO operand: it comes LAST.
      *> GR3: per pair "(operand-1 + operand-2)", operand-1 from
      *> identifier-4, operand-2 the corresponding identifier-5 item.
      *> STANDARD-BINARY, the rules' other named mode, is a declared
      *> non-support (docs/CONFORMANCE.md A.3) and is not exercised.
      *>
      *> WHY THE ORDER IS VISIBLE HERE AND NOWHERE ELSE. Native
      *> addition is exact, so it is commutative and associative; an
      *> SDIDI is decimal128 (§8.8.1.5.2: "a maximum precision of 34
      *> decimal digits"), each addition is a correctly rounded
      *> formatOf-addition (§8.8.1.5.3), and with no INTERMEDIATE
      *> ROUNDING clause NEAREST-AWAY-FROM-ZERO is implied (§11.9.11.2
      *> GR3a). With X = 10**30 (31 digits) and Z = 0.0004, X + Z
      *> needs 35 digits and rounds to X: the 0.0004 is lost, so WHICH
      *> operands are added first decides the answer. Y = -(10**30).
      *> Unparenthesized + runs left to right (§8.8.1.2 GR3). Every
      *> store truncates (§14.7.4.3 GR2: no ROUNDED = TRUNCATION).
      *> The composite-of-operands cap of 31 digits is a NATIVE rule
      *> (§14.7.7 GR2), so these 35-digit composites are legal only
      *> because standard-decimal arithmetic is in effect.
      *>
      *> TO-1  ADD X Z Y TO R1 (R1 = 0): initial evaluation
      *>       (X + Z) + Y = X + Y = 0, then 0 + R1 = 0.0000.
      *>       Native exact arithmetic would give 0.0004.
      *> TO-2  ADD X Y Z TO R2 (R2 = 0.0004): initial evaluation
      *>       (X + Y) + Z = 0.0004, then 0.0004 + R2 = 0.0008.
      *>       Folding the RECEIVER in first, ((R2 + X) + Y) + Z,
      *>       loses R2 to the 34-digit rounding: 0.0004.
      *> GIV-1 ADD X Z Y GIVING G1: (X + Z) + Y = 0.0000.
      *> GIV-2 ADD X Y Z GIVING G2: (X + Y) + Z = 0.0004; with GIV-1
      *>       this pins "in the order in which they are specified".
      *> GIV-3 ADD X Z TO Y GIVING G3: operand-2 (Y) LAST,
      *>       (X + Z) + Y = 0.0000; taking it first, (Y + X) + Z,
      *>       would give 0.0004.
      *> CORR  ADD CORRESPONDING SRC TO DST: the pair P is
      *>       0.9996 + (10**30 + 4) = 10**30 + 4.9996, 35 digits,
      *>       rounded to 34 at the thousandths -> 10**30 + 5.000, and
      *>       truncated into PIC 9(31): ...0005. Native exact
      *>       arithmetic would truncate 10**30 + 4.9996 to ...0004.
      *>       The pair Q (0.55 + 2.2 = 2.75 -> PIC 9V9 2.7) is exact
      *>       in both modes; SRC's S has no DST partner and DST's T
      *>       none in SRC, so T keeps 9 (§14.7.6 rule 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB228ASD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC S9(31) VALUE  1000000000000000000000000000000.
       01 Y  PIC S9(31) VALUE -1000000000000000000000000000000.
       01 Z  PIC SV9(4) VALUE 0.0004.
       01 R1 PIC 9V9(4) VALUE 0.
       01 R2 PIC 9V9(4) VALUE 0.0004.
       01 G1 PIC 9V9(4).
       01 G2 PIC 9V9(4).
       01 G3 PIC 9V9(4).
       01 ED PIC 9.9999.
       01 SRC.
          05 P PIC V9(4) VALUE 0.9996.
          05 Q PIC V99 VALUE 0.55.
          05 S PIC 9 VALUE 7.
       01 DST.
          05 P PIC 9(31) VALUE 1000000000000000000000000000004.
          05 Q PIC 9V9 VALUE 2.2.
          05 T PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       MAIN.
           ADD X Z Y TO R1
           MOVE R1 TO ED
           DISPLAY "TO-1  R1=" ED
           ADD X Y Z TO R2
           MOVE R2 TO ED
           DISPLAY "TO-2  R2=" ED
           ADD X Z Y GIVING G1
           MOVE G1 TO ED
           DISPLAY "GIV-1 G1=" ED
           ADD X Y Z GIVING G2
           MOVE G2 TO ED
           DISPLAY "GIV-2 G2=" ED
           ADD X Z TO Y GIVING G3
           MOVE G3 TO ED
           DISPLAY "GIV-3 G3=" ED
           ADD CORRESPONDING SRC TO DST
           DISPLAY "CORR  P=" P OF DST
           DISPLAY "CORR  Q=" Q OF DST " T=" T
           STOP RUN.
