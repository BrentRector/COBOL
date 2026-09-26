      *> ISO §7.3.6.3 GR2 + §7.3.6.2 SR2 (Annex A.1 item 29) — the
      *>   documented compile-time arithmetic mode and rounding
      *> Rules: §7.3.6.3 GR2 "The implementor shall define and document
      *>   which mode of arithmetic is to be used when evaluating
      *>   compile-time arithmetic." -> OK  §7.3.6.3 2)
      *>   §7.3.6.2 SR2 "The implementor shall define and document any
      *>   rules restricting the precision and/or magnitude and/or range
      *>   of permissible values for the intermediate results ... They
      *>   shall also document which intermediate rounding method is
      *>   used" -> OK  §7.3.6.2 2)
      *>   §7.3.6.3 GR3 "The final result of the arithmetic expression
      *>   shall be truncated to the integer part of the value as
      *>   specified in 15.49, INTEGER-PART function" -> OK  §7.3.6.3 3)
      *>   §13.10.4 GR4 "If arithmetic-expression-1 is specified, it is
      *>   evaluated in accordance with 7.3.6" -> OK  §13.10.4 4)
      *>   §A.1 29) "Compile-Time Arithmetic (Mode of arithmetic used
      *>   and rules for intermediate data handling). This item is
      *>   required." -> OK  §A.1 29)
      *> THE DOCUMENTED DETERMINATION (docs/CONFORMANCE.md row
      *>   DOC-A.1-29): one evaluator for constant entries and
      *>   directives; each intermediate result is a decimal number of
      *>   at most 28 digits after the point; an inexact intermediate
      *>   result is ROUNDED TO NEAREST, A TIE TO THE EVEN DIGIT
      *>   (division does not truncate); the final result is truncated
      *>   toward zero by GR3.
      *> DERIVATION of every .out line from that determination + GR3:
      *>   D1=002  2/3 -> 0.6666666666666666666666666667 (28 places,
      *>           rounded); *3 -> 2.0000000000000000000000000001;
      *>           GR3 -> 2. A truncating division gives 1.
      *>   D2=100  100/7 -> 14.285714285714285714285714286; *7 ->
      *>           100.00000000000000000000000000 (rounded to fit);
      *>           GR3 -> 100. A truncating division gives 99.
      *>   D3=000  1/3 -> 0.3333333333333333333333333333; *3 ->
      *>           0.9999999999999999999999999999; GR3 -> 0.
      *>   T1=002  5E-28/2 = 2.5E-28 is not representable in 28 places:
      *>           a TIE between 2E-28 and 3E-28 -> EVEN -> 2E-28;
      *>           *1E28 -> 2. Half-up rounding gives 3.
      *>   T2=008  15E-28/2 = 7.5E-28: a TIE -> EVEN -> 8E-28; *1E28
      *>           -> 8. Truncation or half-down gives 7.
      *>   IF-D1=2 / IF-D2=100  the same evaluator in a >>IF constant
      *>           conditional expression (7.3.8 via 7.3.6): 2/3*3 = 2
      *>           and 100/7*7 = 100 are TRUE.
      *>   Each constant is an integer (GR3), moved to PIC 999.
      *> 2023 dir ONLY: Annex E.2 6) "The mode of arithmetic used in
      *>   evaluating compile-time arithmetic expressions and the
      *>   handling of intermediate results is now explicitly
      *>   implementor defined." with "The previous COBOL Standard
      *>   required the use of an arithmetic mode that is no longer
      *>   supported." (both -> OK  §E.2 6)). Below 2023 the mode was
      *>   prescribed, so this determination cannot set 2002/2014
      *>   output.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G2M02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-D1 CONSTANT AS 2 / 3 * 3.
       01 K-D2 CONSTANT AS 100 / 7 * 7.
       01 K-D3 CONSTANT AS 1 / 3 * 3.
       01 K-T1 CONSTANT AS
             0.0000000000000000000000000005 / 2
             * 10000000000000000000000000000.
       01 K-T2 CONSTANT AS
             0.0000000000000000000000000015 / 2
             * 10000000000000000000000000000.
       01 W-N PIC 999.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE K-D1 TO W-N
           DISPLAY "D1=" W-N
           MOVE K-D2 TO W-N
           DISPLAY "D2=" W-N
           MOVE K-D3 TO W-N
           DISPLAY "D3=" W-N
           MOVE K-T1 TO W-N
           DISPLAY "T1=" W-N
           MOVE K-T2 TO W-N
           DISPLAY "T2=" W-N
       >>IF 2 / 3 * 3 = 2
           DISPLAY "IF-D1=2"
       >>ELSE
           DISPLAY "IF-D1-NOT-2"
       >>END-IF
       >>IF 100 / 7 * 7 = 100
           DISPLAY "IF-D2=100"
       >>ELSE
           DISPLAY "IF-D2-NOT-100"
       >>END-IF
           STOP RUN.
