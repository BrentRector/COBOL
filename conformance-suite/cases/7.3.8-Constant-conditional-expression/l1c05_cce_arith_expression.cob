      *>--- conformance-case
      *> id: l1c05_cce_arith_expression
      *> rules: [SR-7.3.8.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.8.2 SR2 — an arithmetic expression in a >>IF cce is a
      *>   §7.3.6 expression
      *> Rule: "An arithmetic expression in a constant conditional
      *>   expression shall be formed in accordance with 7.3.6,
      *>   Compile-time arithmetic expressions."
      *> cite.py --check 7.3.8.2 "An arithmetic expression in a constant
      *>   conditional expression shall be formed in accordance with
      *>   7.3.6" -> OK  §7.3.8.2 2)  (Syntax rules)
      *> cite.py --check 7.3.6.3 "The final result of the arithmetic
      *>   expression shall be truncated to the integer part of the
      *>   value" -> OK  §7.3.6.3 3)  (General rules)
      *> The admitting half of SR2: a well-formed §7.3.6 expression is
      *>   accepted in a cce and evaluated by §7.3.6 (§8.8.1 precedence,
      *>   GR3 final truncation). The rejecting halves (§7.3.6.2 SR1 a,
      *>   b, c) are the negatives l1c05-cce-arith-*.
      *> Derivation:
      *>   7 / 2 = 3.5, truncated to 3; 3 = 3 TRUE     -> "A1-TRUE"
      *>   1.5 + 0 = 1.5, truncated to 1; 1 = 1 TRUE   -> "A2-TRUE"
      *>   1 + 2 * 3: * binds first (§8.8.1) = 7; TRUE -> "A3-TRUE"
      *>   -7 / 2 = -3.5, integer part -3 (toward zero); -3 < -3 is
      *>     FALSE (INTEGER would give -4 < -3, TRUE) -> "A4-FALSE"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05D.
       PROCEDURE DIVISION.
       MAIN.
       >>IF 7 / 2 = 3
           DISPLAY "A1-TRUE".
       >>ELSE
           DISPLAY "A1-FALSE".
       >>END-IF
       >>IF 1.5 + 0 = 1
           DISPLAY "A2-TRUE".
       >>ELSE
           DISPLAY "A2-FALSE".
       >>END-IF
       >>IF 1 + 2 * 3 = 7
           DISPLAY "A3-TRUE".
       >>ELSE
           DISPLAY "A3-FALSE".
       >>END-IF
       >>IF -7 / 2 < -3
           DISPLAY "A4-TRUE".
       >>ELSE
           DISPLAY "A4-FALSE".
       >>END-IF
           STOP RUN.
