      *> ISO 15.8.4 r1: ACOS "is the approximation of the arccosine of
      *> argument-1 and is greater than or equal to zero and less than or equal
      *> to" pi; 15.10.4 r1 bounds ASIN to [-pi/2, pi/2]. 14.7.4.3 r2: "If the
      *> ROUNDED phrase is not specified, execution is as if ROUNDED MODE IS
      *> TRUNCATION had been specified".
      *>
      *> A trailing-P receiver has a NEGATIVE scale: PIC 9PP holds hundreds, so a
      *> value below 100 lands as zero. Every returned value of ACOS / ASIN is
      *> below 4, so the correct landing in a P-trailing receiver is ZERO, in
      *> every rounding mode (3.14 is 0.0314 hundreds - nearest is zero too).
      *>
      *> kb/Work PB2633: the codomain clamp formed 10 ** (37 - scale) with a
      *> wrapped Int128 at scale -2 (10 ** 39) and clamped every non-negative
      *> value to -1, so ACOS(0.5) landed as -100 in PIC S9PP and 100 in PIC 9PP,
      *> and wrapped to +1 unit at scale -11. The positive-scale controls pin the
      *> clamp's other side: ACOS(-1) = 3.14159... lands as 3 in PIC S9 ROUNDED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2633CLAMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 HALF PIC S9V9 VALUE 0.5.
       01 NEG PIC S9V9 VALUE -1.
       01 POS PIC S9V9 VALUE 1.
       01 A-HUNDREDS PIC S9PP.
       01 A-HUNDREDS-U PIC 9PP.
       01 A-TENS PIC S9P.
       01 A-DEEP PIC S9P(11).
       01 A-UNITS PIC S9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE A-HUNDREDS = FUNCTION ACOS(HALF).
           DISPLAY "1-ACOS-TRUNC-S9PP=" A-HUNDREDS.
           COMPUTE A-HUNDREDS-U = FUNCTION ACOS(HALF).
           DISPLAY "2-ACOS-TRUNC-9PP=" A-HUNDREDS-U.
           COMPUTE A-HUNDREDS ROUNDED = FUNCTION ACOS(NEG).
           DISPLAY "3-ACOS-NEG1-ROUNDED-S9PP=" A-HUNDREDS.
           COMPUTE A-HUNDREDS-U ROUNDED = FUNCTION ACOS(NEG).
           DISPLAY "4-ACOS-NEG1-ROUNDED-9PP=" A-HUNDREDS-U.
           COMPUTE A-TENS = FUNCTION ACOS(NEG).
           DISPLAY "5-ACOS-NEG1-TRUNC-S9P=" A-TENS.
           COMPUTE A-TENS ROUNDED = FUNCTION ACOS(NEG).
           DISPLAY "6-ACOS-NEG1-ROUNDED-S9P=" A-TENS.
           COMPUTE A-DEEP = FUNCTION ACOS(NEG).
           DISPLAY "7-ACOS-NEG1-TRUNC-S9P11=" A-DEEP.
           COMPUTE A-DEEP ROUNDED = FUNCTION ASIN(POS).
           DISPLAY "8-ASIN-ROUNDED-S9P11=" A-DEEP.
           COMPUTE A-UNITS ROUNDED = FUNCTION ACOS(NEG).
           DISPLAY "9-CONTROL-ACOS-NEG1-ROUNDED-S9=" A-UNITS.
           STOP RUN.
