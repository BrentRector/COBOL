*>--- conformance-case
*> id: l1_divide_quotient_remainder_standard_decimal
*> rules: [GR-14.9.12.4-1, GR-14.9.12.4-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.12.4 GR1 second sentence — "When standard-decimal arithmetic, or standard-binary
      *> arithmetic is in effect, the quotient … is the result of the arithmetic expression
      *> (dividend / divisor)". GR7's second sentence says the same for
      *> (dividend − (subsidiary-quotient * divisor)).
      *> (cite.py --check 14.9.12.4 "When native arithmetic is in effect, the quotient is the result of
      *> dividing the dividend by the divisor" -> OK, General rules 1; --check 14.9.12.4 "the remainder is
      *> the result of multiplying the subsidiary quotient and the divisor and subtracting the product
      *> from the dividend" -> OK, General rules 7. Both rules carry BOTH mode sentences.)
      *>
      *> WHY THE TWIN EXISTS. GR1 and GR7 each state the operation TWICE — once for native arithmetic and
      *> once for the standard modes — and the two sentences are separate obligations: under a standard
      *> mode the quotient is the value of an ARITHMETIC EXPRESSION evaluated per §8.8.1.5 on the standard
      *> decimal intermediate data item, not a division performed at the receiver's own scale. Testing
      *> only the native sentence leaves half of each rule unwitnessed, which is exactly what left these
      *> two inventory rows PARTIAL.
      *>
      *> DERIVED VALUES — IDENTICAL TO THE NATIVE TWIN, AND THAT IS THE POINT. §8.8.1.5's intermediate
      *> carries 34 significant digits, so for these operands the standard-mode expression value and the
      *> native quotient agree to far beyond the receivers' precision, and §14.7.7 rule 3 then applies the
      *> SAME truncation or ROUNDED transfer. A difference on any line would mean the mode had changed the
      *> ANSWER, which neither sentence permits:
      *>   A=3333   10 / 3 truncated at three fraction digits.
      *>   B=13     100 / 8 = 12.5, ROUNDED nearest-away-from-zero.
      *>   C=014 002   quotient 14, remainder 100 − 14 × 7 = 2.
      *>   D=333 001   quotient 3.33, remainder 10 − 3.33 × 3 = 0.01.
      *>   E=67 020    stored quotient ROUNDED to 6.7; subsidiary quotient TRUNCATED to 6.6 (GR6c);
      *>               remainder 20 − 6.6 × 3 = 0.20.
      *> The native twin is conformance:85/l1_divide_quotient_remainder_native, which also carries GR1's
      *> §14.7.5 division-by-zero leg; the composite-expression consumers of the standard-decimal
      *> intermediate are pinned separately by conformance:2023/pb84_standard_decimal_intermediate_consumers.
      *>
      *> EDITION. ARITHMETIC IS STANDARD-DECIMAL is the 2014 clause of the OPTIONS paragraph
      *> (arithmetic-standard-decimal-2014); the version matrix owns the gate below it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DIVSD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-QA PIC 9V999.
       01 W-QB PIC 9(2).
       01 W-QC PIC 9(3).
       01 W-RC PIC 9(3).
       01 W-QD PIC 9V99.
       01 W-RD PIC 9V99.
       01 W-QE PIC 9V9.
       01 W-RE PIC 9V99.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE 3 INTO 10 GIVING W-QA
           DISPLAY "A=" W-QA
           DIVIDE 8 INTO 100 GIVING W-QB ROUNDED
           DISPLAY "B=" W-QB
           DIVIDE 7 INTO 100 GIVING W-QC REMAINDER W-RC
           DISPLAY "C=" W-QC " " W-RC
           DIVIDE 3 INTO 10 GIVING W-QD REMAINDER W-RD
           DISPLAY "D=" W-QD " " W-RD
           DIVIDE 3 INTO 20 GIVING W-QE ROUNDED REMAINDER W-RE
           DISPLAY "E=" W-QE " " W-RE
           STOP RUN.
