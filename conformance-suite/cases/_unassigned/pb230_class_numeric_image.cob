      *>--- conformance-case
      *> id: pb230_class_numeric_image
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB230 - ISO 8.8.4.4.4 GR3 n)1: the NUMERIC class condition over a numeric item whose storage is a
      *> CHARACTER WINDOW (a REDEFINES view), keyed on the item's USAGE.  A native-carrier numeric leaf folds to
      *> true at compile time (it can only hold digits); only a window can hold content that fails the test, and
      *> the rule the window is tested against is the item's own byte representation:
      *>   n)1.a (DISPLAY) "the presence or absence of an operational sign in the content ... is in agreement with
      *>          the data description ... and ... the content, except for the operational sign, consists entirely
      *>          of the characters 0, 1, 2, 3, ..., 9";
      *>   n)1.c (every other fixed-point usage) "the content ... consists entirely of a valid representation for
      *>          the usage and, if a PICTURE clause is specified, the numeric value is within the range of values
      *>          implied by the PICTURE clause".
      *> This is the SAME predicate 14.6.13.2 rule 2 tests a sending operand against ("would evaluate to false in
      *> a numeric class condition"), so it is written once and both callers ask it.
      *> DERIVATIONS for the four NO answers.  Zoned "AB1": A and B are not among 0-9 (n)1.a).  Signed S9(3)
      *> holding "1AB": with the DISPLAY default of a TRAILING over-punch (13.18.52), position 3 must be a digit
      *> or an over-punch character and "B" is one - but position 2, "A", is not a digit, so the test fails on the
      *> non-sign position.  Packed PIC 9(3) COMP-3 holding X"5A5A": the digit nibbles are 5, A and 5, and A is
      *> not a decimal digit, so the bytes are not a valid packed representation (n)1.c first half).  Binary
      *> PIC 9(3) COMP holding X"5A5A": every two's-complement pattern IS a valid binary representation, but the
      *> value 23130 is outside 0..999, the range the PICTURE implies (n)1.c second half).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB230CLASSIMG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GZ.
          05 Z PIC 9(3).
       01 XZ REDEFINES GZ PIC X(3).
       01 GS.
          05 S PIC S9(3).
       01 XS REDEFINES GS PIC X(3).
       01 GP.
          05 P PIC 9(3) COMP-3.
       01 XP REDEFINES GP PIC X(2).
       01 GB.
          05 BN PIC 9(3) COMP.
       01 XB REDEFINES GB PIC X(2).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 123 TO Z.
           IF Z IS NUMERIC
               DISPLAY "zoned 123 YES"
           ELSE
               DISPLAY "zoned 123 NO"
           END-IF.
           MOVE "AB1" TO XZ.
           IF Z IS NUMERIC
               DISPLAY "zoned AB1 YES"
           ELSE
               DISPLAY "zoned AB1 NO"
           END-IF.
           MOVE -12 TO S.
           IF S IS NUMERIC
               DISPLAY "signed -12 YES"
           ELSE
               DISPLAY "signed -12 NO"
           END-IF.
           MOVE "1AB" TO XS.
           IF S IS NUMERIC
               DISPLAY "signed 1AB YES"
           ELSE
               DISPLAY "signed 1AB NO"
           END-IF.
           MOVE 123 TO P.
           IF P IS NUMERIC
               DISPLAY "packed 123 YES"
           ELSE
               DISPLAY "packed 123 NO"
           END-IF.
           MOVE "ZZ" TO XP.
           IF P IS NUMERIC
               DISPLAY "packed ZZ YES"
           ELSE
               DISPLAY "packed ZZ NO"
           END-IF.
           MOVE 123 TO BN.
           IF BN IS NUMERIC
               DISPLAY "binary 123 YES"
           ELSE
               DISPLAY "binary 123 NO"
           END-IF.
           MOVE "ZZ" TO XB.
           IF BN IS NUMERIC
               DISPLAY "binary ZZ YES"
           ELSE
               DISPLAY "binary ZZ NO"
           END-IF.
           STOP RUN.
