      *>--- conformance-case
      *> id: pb1565_variadic_exact_argument_list
      *> rules: [RV-15.74.4-1, RV-15.86.4-1, RV-15.98.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1565 - VARIANCE / STANDARD-DEVIATION / PRESENT-VALUE
      *> over an all-EXACT argument list whose equivalent arithmetic
      *> expression cancels across the list.
      *> 15.98.4 r1 b): "For two occurrences of argument-1" the EAE is
      *> the mean of the squared deviations from FUNCTION MEAN; 15.86.4
      *> r1: (FUNCTION SQRT (FUNCTION VARIANCE (argument-list))); 15.74.4
      *> r1: argument-21 / (1 + argument-1) + argument-22 /
      *> (1 + argument-1) ** 2. 15.4.1 licenses an approximation of the
      *> RETURNED value, not of each argument: narrowing 18-digit
      *> arguments to binary64 first made both A and B 1.0E17, so the
      *> deviations cancelled to zero.
      *> Expected values: Python decimal at 80 digits (an oracle
      *> independent of the compiler):
      *>   VAR-AB  = 0.25   (was 0)
      *>   SD-AB   = 0.5    (was 0)
      *>   PV-0    = 1      (was 0)  (1E17+1) - 1E17 at rate 0
      *>   PV-HALF = 2      (was 0)  (3E17+3)/1.5 - 4.5E17/2.25
      *>   VAR-ALL = 1      (was 0)  deviations +-1 about 1E17+2
      *>   SD-ALL  = 1      (was 0)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1565VARLIST85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18)  VALUE 100000000000000001.
       01 B    PIC 9(18)  VALUE 100000000000000002.
       01 NB   PIC S9(18) VALUE -100000000000000000.
       01 P3   PIC 9(18)  VALUE 300000000000000003.
       01 N45  PIC S9(18) VALUE -450000000000000000.
       01 ZR   PIC 9      VALUE 0.
       01 HALF PIC 9V9    VALUE 0.5.
       01 TB.
          05 T PIC 9(18) OCCURS 2.
       01 W    PIC -9.9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 100000000000000001 TO T (1)
           MOVE 100000000000000003 TO T (2)
           COMPUTE W ROUNDED = FUNCTION VARIANCE (A B)
           DISPLAY "VAR-AB  =" W
           COMPUTE W ROUNDED = FUNCTION STANDARD-DEVIATION (A B)
           DISPLAY "SD-AB   =" W
           COMPUTE W ROUNDED = FUNCTION PRESENT-VALUE (ZR A NB)
           DISPLAY "PV-0    =" W
           COMPUTE W ROUNDED = FUNCTION PRESENT-VALUE (HALF P3 N45)
           DISPLAY "PV-HALF =" W
           COMPUTE W ROUNDED = FUNCTION VARIANCE (T (ALL))
           DISPLAY "VAR-ALL =" W
           COMPUTE W ROUNDED = FUNCTION STANDARD-DEVIATION (T (ALL))
           DISPLAY "SD-ALL  =" W
           STOP RUN.
