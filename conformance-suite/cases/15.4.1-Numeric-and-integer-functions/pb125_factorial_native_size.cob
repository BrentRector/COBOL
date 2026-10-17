*>--- conformance-case
*> id: pb125_factorial_native_size
*> rules: [RV-15.4.1-1, DOC-A.1-123]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB125 — FACTORIAL's NATIVE lane past the Int128 intermediate. ISO 15.36.3 r1 admits every
      *> nonnegative integer, so 34 is a CONFORMING argument and the old EC-ARGUMENT-FUNCTION default 0 was a
      *> silent wrong answer (zero is no 15.4.1 "approximation" of 34! = 2.95E+38). The value the 15.36.4 r1c
      *> equivalent arithmetic expression names exceeds the native Int128 intermediate (item 123's documented
      *> carrier), so the honest disposition is the SIZE ERROR condition — the same class as any intermediate
      *> the carrier cannot form (CONFORMANCE.md items 70/179): ON SIZE ERROR takes it; without checking the
      *> run unit would terminate abnormally rather than fabricate a value. 33!/31! = 33 * 32 = 1056 pins the
      *> exact lane's ceiling from below (both factorials and their quotient are Int128-exact).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB125FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION FACTORIAL(33) / FUNCTION FACTORIAL(31)
           IF R = 1056 DISPLAY "EXACT OK" ELSE DISPLAY "EXACT BAD " R
           END-IF
           COMPUTE R = FUNCTION FACTORIAL(34)
               ON SIZE ERROR DISPLAY "SIZE OK"
               NOT ON SIZE ERROR DISPLAY "SIZE BAD " R
           END-COMPUTE
           STOP RUN.
