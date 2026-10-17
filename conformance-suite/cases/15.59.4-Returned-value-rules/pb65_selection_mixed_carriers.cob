*>--- conformance-case
*> id: pb65_selection_mixed_carriers
*> rules: [RV-15.59.4-1, RV-15.61.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB65 (RV-15.59.4-1 D2). ISO §15.59.4 r1 / §15.63.4 r1: the
      *> returned value of MAX / MIN "is the CONTENT of the argument-1 having
      *> the greatest [least] value", the comparisons "made according to the
      *> rules for simple conditions"; §15.61.4 r1: MEDIAN over an odd count is
      *> "the content of the argument-1 that is the middle value" (even: the
      *> arithmetic mean of the two middle values); §15.71 / §15.72 ORD-MAX /
      *> ORD-MIN return the ordinal position. None carries an equivalent
      *> arithmetic expression, so §15.4.1's native latitude over the
      *> representation of the returned value never reaches the VALUE. A MIXED
      *> list — a FLOAT-LONG beside a fixed-point item, conforming per §8.5.2.1
      *> Table 2 (both class numeric) — used to route the WHOLE call through
      *> binary64: MAX(F1 N1) with N1 = 999999999999999999 returned 13 (1e18 →
      *> FromDouble at scale 9 → the modular store), MAX(D9 F1) corrupted the
      *> last digit, MEDIAN(F1 N1 N2) returned 0. Now the selected argument is
      *> delivered from its own carrier through the SDIDI (exact for a fixed
      *> item; a float through the §8.8.1.5.1 conversion). An all-float list
      *> keeps its double (its content), an all-fixed list its exact lane.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65MIXSEL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1  USAGE FLOAT-LONG VALUE 1.0.
       01 F2  USAGE FLOAT-LONG VALUE 2.5.
       01 N1  PIC 9(18) VALUE 999999999999999999.
       01 N2  PIC 9(18) VALUE 999999999999999998.
       01 N0  PIC 9(18) VALUE 1.
       01 D9  PIC 9(9)V9(9) VALUE 123456789.123456789.
       01 R18 PIC 9(18).
       01 R19 PIC 9(18)V9.
       01 R9  PIC 9(9)V9(9).
       01 RFL USAGE FLOAT-LONG.
       01 O   PIC 99.
       PROCEDURE DIVISION.
           MOVE FUNCTION MAX(F1 N1) TO R18.
           DISPLAY "T1 MAX(F1 N1)=" R18.
           MOVE FUNCTION MIN(F1 N1) TO R18.
           DISPLAY "T2 MIN(F1 N1)=" R18.
           MOVE FUNCTION MAX(D9 F1) TO R9.
           DISPLAY "T3 MAX(D9 F1)=" R9.
           MOVE FUNCTION MIN(D9 F2) TO R9.
           DISPLAY "T4 MIN(D9 F2)=" R9.
           COMPUTE O = FUNCTION ORD-MAX(F1 N1 N0).
           DISPLAY "T5 ORD-MAX=" O.
           COMPUTE O = FUNCTION ORD-MIN(N1 F1 N0).
           DISPLAY "T6 ORD-MIN=" O.
           MOVE FUNCTION MAX(F1 F2) TO RFL.
           DISPLAY "T7 MAX(F1 F2)=" RFL.
           MOVE FUNCTION MAX(N1 N0) TO R18.
           DISPLAY "T8 MAX(N1 N0)=" R18.
           IF FUNCTION MAX(F1 N1) = N1 DISPLAY "T9 EQ" ELSE DISPLAY "T9 NE" END-IF.
           COMPUTE R18 = FUNCTION MAX(F1 N1) - 1.
           DISPLAY "T10 MAX - 1=" R18.
           MOVE FUNCTION MEDIAN(F1 N1 N2) TO R18.
           DISPLAY "T11 MEDIAN(F1 N1 N2)=" R18.
           MOVE FUNCTION MEDIAN(F1 N1 N2 N2) TO R19.
           DISPLAY "T12 MEDIAN(F1 N1 N2 N2)=" R19.
           MOVE FUNCTION MEDIAN(N1 N2 5) TO R18.
           DISPLAY "T13 MEDIAN(N1 N2 5)=" R18.
           STOP RUN.
