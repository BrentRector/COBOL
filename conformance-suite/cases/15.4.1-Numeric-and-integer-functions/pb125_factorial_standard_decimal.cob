*>--- conformance-case
*> id: pb125_factorial_standard_decimal
*> rules: [RV-15.4.1-1, RV-15.4.1-L2.1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB125 — FACTORIAL under ARITHMETIC IS STANDARD-DECIMAL. ISO 15.36.4 r1c's equivalent
      *> arithmetic expression n * (n-1) * ... * 1 evaluates on the SDIDI (8.8.1.5.2), whose 34-digit range
      *> holds 34! = 295232799039604140847618609643520000000 EXACTLY (39 digits, 7 trailing zeros — every
      *> intermediate product's dropped digits are its own trailing zeros, so no rounding ever loses a
      *> nonzero digit). The defect: the Int128-capped native lane answered for BOTH modes, so a conforming
      *> FACTORIAL(34) returned the 15.3 default 0. Hand-derived pins: 34!/33! = 34 (both exact, exact
      *> quotient); bounds 2.95E+38 < 34! < 2.96E+38; FACTORIAL(0) = 1 (r1a). A result past decimal128
      *> (FACTORIAL(9999): the product overflows near n = 1755) raises the size error condition
      *> (8.8.1.5.2 r2) — pinned through ON SIZE ERROR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB125FD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(18).
       PROCEDURE DIVISION.
       MAIN.
           IF FUNCTION FACTORIAL(34) / FUNCTION FACTORIAL(33) = 34
               DISPLAY "RATIO OK" ELSE DISPLAY "RATIO BAD" END-IF
           IF FUNCTION FACTORIAL(34) > 2.95E+38
              AND FUNCTION FACTORIAL(34) < 2.96E+38
               DISPLAY "MAG OK" ELSE DISPLAY "MAG BAD" END-IF
           IF FUNCTION FACTORIAL(0) = 1
               DISPLAY "ZERO OK" ELSE DISPLAY "ZERO BAD" END-IF
           COMPUTE R = FUNCTION FACTORIAL(9999)
               ON SIZE ERROR DISPLAY "SIZE OK"
               NOT ON SIZE ERROR DISPLAY "SIZE BAD " R
           END-COMPUTE
           STOP RUN.
