*>--- conformance-case
*> id: pb99_floating_literal_extremes
*> rules: [SR-8.3.3.3.3-1, SR-8.3.3.3.3-2, SR-8.3.3.3.3-3,
*>    GR-8.3.3.3.3-5, DOC-A.1-82]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB99 - the floating-point numeric literal at its LEGAL extremes: a 36-digit significand and a four-digit
      *> exponent (8.3.3.3.3 SR2/SR3) seeding a floating-point numeric-edited item keep the EXACT value (the VALUE
      *> path composes the edited image from the literal's significand and power of ten - no binary64 on the way,
      *> so 1.0E+9999 lands in PIC 9E+9999); a literal at binary64's / binary32's edge seeds a FLOAT-LONG /
      *> FLOAT-SHORT item and evaluates in a statement (the implementor-defined exponent range, CONFORMANCE.md 7
      *> item 82); a fixed-point numeric item takes a floating-point literal at its exact value (13.18.63.3 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB99FX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC 9.9(35)E+9999 VALUE 1.23456789012345678901234567890123456E+9999.
       01 E2 PIC 9E+9999 VALUE 1.0E+9999.
       01 E3 PIC -9.9(3)E+9999 VALUE -5.0E-9999.
       01 F1 USAGE FLOAT-LONG VALUE 1.7E+308.
       01 F2 USAGE FLOAT-SHORT VALUE 3.0E+38.
       01 F3 USAGE FLOAT-LONG.
       01 N1 PIC 9(5)V9(3) VALUE 1.5E+3.
       01 N2 PIC 9V9(9) VALUE 5.0E-9.
       PROCEDURE DIVISION.
           DISPLAY "E1=[" E1 "]".
           DISPLAY "E2=[" E2 "] E3=[" E3 "]".
           MOVE 1.0E-300 TO F3
           IF F3 < 1.0E-299 AND F1 > 1.0E+308 DISPLAY "F1/F3 in range" END-IF
           IF F2 > 2.0E+38 DISPLAY "F2 in range" END-IF
           DISPLAY "N1=" N1 " N2=" N2.
           STOP RUN.
