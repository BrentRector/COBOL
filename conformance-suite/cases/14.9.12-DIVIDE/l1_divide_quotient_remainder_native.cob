      *>--- conformance-case
      *> id: l1_divide_quotient_remainder_native
      *> rules: [GR-14.9.12.4-1, GR-14.9.12.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.12.4 GR1 — "When native arithmetic is in effect, the quotient is the result of dividing
      *> the dividend by the divisor."
      *> (cite.py --check 14.9.12.4 "When native arithmetic is in effect, the quotient is the result of
      *> dividing the dividend by the divisor" -> OK, §14.9.12.4 General rules 1.)
      *> ISO §14.9.12.4 GR7 — "When native arithmetic is in effect, the remainder is the result of
      *> multiplying the subsidiary quotient and the divisor and subtracting the product from the
      *> dividend."
      *> (cite.py --check 14.9.12.4 "the remainder is the result of multiplying the subsidiary quotient and
      *> the divisor and subtracting the product from the dividend" -> OK, §14.9.12.4 General rules 7.)
      *>
      *> DERIVED BEFORE MEASURING. GR2 fixes dividend and divisor; GR6c defines the subsidiary quotient as
      *> "derived from THE QUOTIENT by truncation of digits at the least significant end", with the same
      *> number of digits and the same decimal point location as identifier-3; §14.7.7 rule 3 NOTE 1
      *> confines ROUNDED to the final TRANSFER. Each line is hand-derived from those rules alone.
      *>   A=3333  10 / 3 = 3.33333…, truncated at PIC 9V999's three fraction digits -> 3.333.
      *>   B=13    100 / 8 = 12.5 exactly; ROUNDED with no MODE is nearest-away-from-zero -> 13
      *>           (truncation would give 12, which is what a receiver-mode mix-up prints).
      *>   C=014 002   100 / 7 = 14.2857…; identifier-3 PIC 9(3) takes 014, the subsidiary quotient is 14,
      *>           remainder = 100 − 14 × 7 = 2.
      *>   D=333 001   10 / 3 with PIC 9V99 receivers: quotient 3.33, subsidiary quotient 3.33,
      *>           remainder = 10 − 3.33 × 3 = 10 − 9.99 = 0.01 (the display of a V-scaled item carries no
      *>           point, so 3.33 prints 333 and 0.01 prints 001).
      *>   E=67 020    ⛔ THE DISCRIMINATOR FOR GR6c/GR7. 20 / 3 = 6.66666…; identifier-3 is PIC 9V9 with
      *>           ROUNDED, so the STORED quotient is 6.7 — but the subsidiary quotient is derived from
      *>           the QUOTIENT by TRUNCATION, i.e. 6.6, so the remainder is 20 − 6.6 × 3 = 0.20. An
      *>           implementation that back-multiplied the ROUNDED stored value would compute
      *>           20 − 6.7 × 3 = −0.10 and print 010 into the unsigned receiver.
      *>   F=SIZEERR 777   §14.7.5 case 2 — division by zero raises the size error condition; with the ON
      *>           SIZE ERROR phrase present, identifier-3 is left with its previous content (777).
      *>
      *> THE RULE'S THIRD MODE IS NOT SILENTLY SKIPPED. Both GR1 and GR7 name standard-decimal AND
      *> standard-binary arithmetic. The standard-decimal clause is pinned by the twin
      *> conformance:cases/14.9.12-DIVIDE/l1_divide_quotient_remainder_standard_decimal (the same six statements, the same
      *> derived values, under ARITHMETIC IS STANDARD-DECIMAL); the standard-binary clause is refused by
      *> name — conformance:cases/14.9.12-DIVIDE/l1-divide-arithmetic-standard-binary — under the §4.2.6
      *> processor-dependence decline recorded in docs/CONFORMANCE.md §2 row 2, so that clause is
      *> UNREACHABLE rather than unverified.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DIVN.
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
       01 W-QF PIC 9(3) VALUE 777.
       01 W-ZZ PIC 9 VALUE 0.
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
           DIVIDE W-ZZ INTO 5 GIVING W-QF
               ON SIZE ERROR DISPLAY "F=SIZEERR " W-QF
           END-DIVIDE
           STOP RUN.
