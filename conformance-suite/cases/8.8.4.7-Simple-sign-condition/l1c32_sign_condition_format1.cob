      *>--- conformance-case
      *> id: l1c32_sign_condition_format1
      *> rules: [FMT-8.8.4.7.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.8.4.7.2 Format 1 — every path of the sign-condition diagram (IS, NOT, 3 keywords)
      *>
      *> THE FORMAT. §8.8.4.7.2 Format 1 (expression):
      *>   arithmetic-expression-1 IS [ NOT ] { POSITIVE | NEGATIVE | ZERO }
      *> IS is not underlined (optional word); NOT is optional; exactly one keyword.
      *> §8.8.4.7.3 SR1: "Arithmetic-expression-1 shall be any single numeric data item
      *> described with a usage other than a standard floating-point usage, or any form of
      *> arithmetic expression."
      *> §8.8.4.7.4 GR1: a) POSITIVE "true if the value is greater than zero";
      *> b) NEGATIVE "true if the value is less than zero"; c) ZERO "true if the value is
      *> zero". NOT: §8.8.4.10.1 "A condition is negated by use of the logical operator
      *> 'NOT', which reverses the truth value of the condition to which it is applied."
      *>   cite.py: OK  §8.8.4.7.2   (General format)  [arithmetic-expression-1 IS [ NOT ] ...]
      *>   cite.py: OK  §8.8.4.7.3 1)  (Syntax rules)
      *>   cite.py: OK  §8.8.4.7.4 1) a)  (General rules)  [a), and b) and c): cite.py
      *>            labels all three list items "1) a)" - PB1554]
      *>   cite.py: OK  §8.8.4.10.1   (General)
      *> (cite.py ignores operator symbols, PB1554; the expressions below are quoted here
      *> from the source, not from a citation.)
      *>
      *> DERIVATION (N = -5, Z = 0, P = 7):
      *>   F-1  N NEGATIVE          -5 < 0          -> T   (IS omitted, NOT omitted)
      *>   F-2  N IS POSITIVE       -5 > 0 false    -> F
      *>   F-3  N IS NOT ZERO       -5 = 0 false    -> T
      *>   F-4  Z ZERO              0 = 0           -> T
      *>   F-5  Z NOT POSITIVE      0 > 0 false     -> T   (NOT without IS)
      *>   F-6  Z IS NOT NEGATIVE   0 < 0 false     -> T
      *>   F-7  P POSITIVE          7 > 0           -> T
      *>   F-8  N + 5 IS ZERO       0               -> T   (expression operand)
      *>   F-9  N * -2 POSITIVE     10              -> T
      *>   F-10 - N IS NOT NEGATIVE 5 < 0 false     -> T   (unary minus)
      *>   F-11 P - 10 NEGATIVE     -3              -> T
      *>   F-12 (P - 7) IS POSITIVE 0 > 0 false     -> F
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N            PIC S9(3) VALUE -5.
       01 Z            PIC S9(3) VALUE 0.
       01 P            PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF N NEGATIVE DISPLAY "F-1 T" ELSE DISPLAY "F-1 F".
           IF N IS POSITIVE DISPLAY "F-2 T" ELSE DISPLAY "F-2 F".
           IF N IS NOT ZERO DISPLAY "F-3 T" ELSE DISPLAY "F-3 F".
           IF Z ZERO DISPLAY "F-4 T" ELSE DISPLAY "F-4 F".
           IF Z NOT POSITIVE DISPLAY "F-5 T" ELSE DISPLAY "F-5 F".
           IF Z IS NOT NEGATIVE DISPLAY "F-6 T" ELSE DISPLAY "F-6 F".
           IF P POSITIVE DISPLAY "F-7 T" ELSE DISPLAY "F-7 F".
           IF N + 5 IS ZERO DISPLAY "F-8 T" ELSE DISPLAY "F-8 F".
           IF N * -2 POSITIVE DISPLAY "F-9 T" ELSE DISPLAY "F-9 F".
           IF - N IS NOT NEGATIVE DISPLAY "F-10 T"
               ELSE DISPLAY "F-10 F".
           IF P - 10 NEGATIVE DISPLAY "F-11 T" ELSE DISPLAY "F-11 F".
           IF (P - 7) IS POSITIVE DISPLAY "F-12 T"
               ELSE DISPLAY "F-12 F".
           STOP RUN.
