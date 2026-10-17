*>--- conformance-case
*> id: l1c32_sign_condition_format2
*> rules: [SR-8.8.4.7.3-2, GR-8.8.4.7.4-2, FMT-8.8.4.7.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.4.7.2 Format 2 — the standard-float sign condition: IS, NOT, 3 keywords
      *>
      *> THE FORMAT. §8.8.4.7.2 Format 2 (standard-float):
      *>   data-name-1 IS [ NOT ] { POSITIVE | NEGATIVE | ZERO }
      *> §8.8.4.7.3 SR2: "Data-name-1 shall be the name of a single data item in the data
      *> division described with a standard floating-point usage, and that name shall not
      *> be enclosed in parentheses."
      *> §8.8.4.7.4 GR2: a) POSITIVE true "if the sign of the content ... is positive";
      *> b) NEGATIVE true "if the sign of the content ... is negative"; c) ZERO true "if the
      *> content ... is a valid representation of the numeric value zero".
      *>   cite.py: OK  §8.8.4.7.2   (General format)  [data-name-1 IS [NOT ] ...]
      *>   cite.py: OK  §8.8.4.7.3 2)  (Syntax rules)
      *>   cite.py: OK  §8.8.4.7.4 2)  (General rules)  [a), b), c) of GR2]
      *> NOT: §8.8.4.10.1 "A condition is negated by use of the logical operator 'NOT',
      *> which reverses the truth value of the condition to which it is applied."
      *>   cite.py: OK  §8.8.4.10.1   (General)
      *> Directory 2014: FLOAT-BINARY-64 (a §3 standard floating-point usage) enters with
      *> ISO/IEC 1989:2014; Annex E records no 2014->2023 change to the sign condition.
      *> Only non-zero values are used, so no line depends on how a zero is signed (the
      *> Format 1 / Format 2 partition for +0/-0 is SR1/SR2's row, not this one).
      *>
      *> DERIVATION (FB = -2.5, then FB = 3.0; both exactly representable):
      *>   G-1 FB NEGATIVE          sign negative        -> T   (IS, NOT omitted)
      *>   G-2 FB IS POSITIVE       sign not positive    -> F
      *>   G-3 FB IS NOT ZERO       not a zero           -> T
      *>   G-4 FB NOT NEGATIVE      negative, negated    -> F
      *>   MOVE 3.0 TO FB
      *>   G-5 FB IS POSITIVE                            -> T
      *>   G-6 FB ZERO                                   -> F
      *>   G-7 FB IS NOT NEGATIVE                        -> T
      *>   G-8 FB NOT POSITIVE                           -> F
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FB           USAGE FLOAT-BINARY-64.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE -2.5 TO FB.
           IF FB NEGATIVE DISPLAY "G-1 T" ELSE DISPLAY "G-1 F".
           IF FB IS POSITIVE DISPLAY "G-2 T" ELSE DISPLAY "G-2 F".
           IF FB IS NOT ZERO DISPLAY "G-3 T" ELSE DISPLAY "G-3 F".
           IF FB NOT NEGATIVE DISPLAY "G-4 T" ELSE DISPLAY "G-4 F".
           MOVE 3.0 TO FB.
           IF FB IS POSITIVE DISPLAY "G-5 T" ELSE DISPLAY "G-5 F".
           IF FB ZERO DISPLAY "G-6 T" ELSE DISPLAY "G-6 F".
           IF FB IS NOT NEGATIVE DISPLAY "G-7 T" ELSE DISPLAY "G-7 F".
           IF FB NOT POSITIVE DISPLAY "G-8 T" ELSE DISPLAY "G-8 F".
           STOP RUN.
