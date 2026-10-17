*>--- conformance-case
*> id: pb2729_float_significand_forms
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2729 - the floating-point literal's significand is a fixed-point literal (8.3.3.3.3 rule 1)
      *> whose decimal point is anywhere but its rightmost character (8.3.3.3.2 rule 3): a point between digits
      *> (1.0E2), a leading point (.25E+1), a signed significand in an argument list (-1.5E2) and a signed
      *> exponent (25.0E-1) all stay ONE literal. Values: 1.0E2 + 5 = 105; MAX(-150, 2.5) = 2.5, stored in
      *> PIC 9(3)V9 as 0025 (V is an assumed point); 25.0E-1 * 2 = 5 (8.3.3.3.3 rule 5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2729POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(5).
       01 W PIC 9(3)V9.
       PROCEDURE DIVISION.
           COMPUTE X = 1.0E2 + 5
           DISPLAY X
           COMPUTE W = FUNCTION MAX(-1.5E2 .25E+1)
           DISPLAY W
           COMPUTE X = 25.0E-1 * 2
           DISPLAY X
           STOP RUN.
