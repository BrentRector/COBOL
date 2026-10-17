      *>--- conformance-case
      *> id: l1c05-cce-arith-division-by-zero
      *> rules: [SR-7.3.8.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.8.2 SR2 via §7.3.6.2 SR1c — see the >>IF below
      *> Rule: "An arithmetic expression in a constant conditional
      *>   expression shall be formed in accordance with 7.3.6,
      *>   Compile-time arithmetic expressions."
      *> cite.py --check 7.3.8.2 "An arithmetic expression in a constant
      *>   conditional expression shall be formed in accordance with
      *>   7.3.6" -> OK  §7.3.8.2 2)  (Syntax rules)
      *> cite.py --check 7.3.6.2 "The expression shall be specified in
      *>   such a way that a division by zero cannot occur" -> OK
      *>   §7.3.6.2 1) c)  (Syntax rules)
      *> The >>IF below is otherwise a well-formed relation cce; only
      *>   its arithmetic operand breaks §7.3.6, so the only reason to
      *>   reject is SR2 (COBOLNET1619, the directive-expression
      *>   formation diagnostic).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05F.
       PROCEDURE DIVISION.
       MAIN.
       >>IF 1 / 0 = 1
           DISPLAY "SELECTED".
       >>END-IF
           STOP RUN.
