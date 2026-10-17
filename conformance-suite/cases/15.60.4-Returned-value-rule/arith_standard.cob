*>--- conformance-case
*> id: arith_standard
*> rules: [RV-15.60.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 11.9.5 / 8.8.1.5 - ARITHMETIC IS STANDARD (the 2002 mode; Annex E.2 item 21) evaluates every
      *> arithmetic operation in the standard-decimal intermediate (SDIDI - 34 significant digits, one
      *> rounding per operation under INTERMEDIATE ROUNDING, default NEAREST-AWAY-FROM-ZERO 11.9.11 r3a).
      *> HAND-DERIVED expectations (P10 Step 12):
      *> W1: 2/7 -> 0.2857142857142857142857142857142857 (34 digits; 35th digit 1 rounds down); *7 ->
      *>     1|9999...9 (35 digits) -> round digit 9 rounds UP -> exactly 2 -> W1=2.00000. Native clips the
      *>     quotient toward the receiver scale first -> 1.99997 (the pinned divergence witness).
      *> W2: 1.000000001 ** 2 per 8.8.1.5.4 r2b = (b * b) exactly = 1.000000002000000001 (19 sig digits
      *>     < 34 -> NO rounding). Native computes ** in IEEE double (16-17 digits) and loses the last digit.
      *> W3: 9999999999 ** 3 per r2c = ((b*b)*b): b*b = 99999999980000000001 (20 digits, exact);
      *>     *b = 10^30 - 3*10^20 + 3*10^10 - 1 = 999999999700000000029999999999 (30 digits, exact).
      *> W4: (10 ** 3200) * (10 ** 3000): each power is exact in SDIDI (adjusted exponents 3200/3000);
      *>     the product's adjusted exponent 6200 exceeds decimal128's +6144 -> EC-SIZE-OVERFLOW
      *>     (8.8.1.5.2 r2) -> the size error condition -> ON SIZE ERROR; W4 stays 00000 (14.7.5).
      *> W5: a COMP-2 float operand converts into SDIDI form per 8.8.1.5.1 (the implementor-defined
      *>     conversion = the SHORTEST round-trip decimal identity of the IEEE value, so the double nearest
      *>     0.1 converts to the SDIDI 0.1 exactly) and the operations are SDIDI: 0.1 * 3 = 0.3 exactly ->
      *>     0.300000000000000000 at scale 18. Native IEEE binary64 computes 0.1d * 3 =
      *>     0.30000000000000004440892... whose scale-18 store carries the binary artifact in digits 17-18
      *>     (the mode-consumption witness for float operands).
      *> MEAN: 15.4.1 r1 + NOTE 2 - under standard arithmetic FUNCTION MEAN(a b c) = (a + b + c) / 3 is a
      *>     TRUE relation (both sides are the SAME SDIDI division 4/3); under native the function's
      *>     working-scale quotient differs from the expression's guard-scale quotient.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ARSTP10AS.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC 9V9(5).
       01 W2 PIC 9V9(18).
       01 W3 PIC 9(30).
       01 W4 PIC 9(5) VALUE 0.
       01 W5 PIC 9V9(18).
       01 FL USAGE COMP-2 VALUE 0.1.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "ARSTOK".
           COMPUTE W1 = 2 / 7 * 7.
           DISPLAY "W1=" W1.
           COMPUTE W2 = 1.000000001 ** 2.
           DISPLAY "W2=" W2.
           COMPUTE W3 = 9999999999 ** 3.
           DISPLAY "W3=" W3.
           COMPUTE W4 = (10 ** 3200) * (10 ** 3000)
               ON SIZE ERROR DISPLAY "RANGE-EC"
           END-COMPUTE.
           DISPLAY "W4=" W4.
           COMPUTE W5 = FL * 3.
           DISPLAY "W5=" W5.
           IF FUNCTION MEAN(1 1 2) = (1 + 1 + 2) / 3
               DISPLAY "MEAN-EAE-OK"
           ELSE
               DISPLAY "MEAN-EAE-DIFFERS"
           END-IF.
           STOP RUN.
