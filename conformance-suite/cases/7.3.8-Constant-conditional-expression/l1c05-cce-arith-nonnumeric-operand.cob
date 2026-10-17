      *>--- conformance-case
      *> id: l1c05-cce-arith-nonnumeric-operand
      *> rules: [SR-7.3.8.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.8.2 SR2 via §7.3.6.2 SR1b — see the >>IF below
      *> Rule: "An arithmetic expression in a constant conditional
      *>   expression shall be formed in accordance with 7.3.6,
      *>   Compile-time arithmetic expressions."
      *> cite.py --check 7.3.8.2 "An arithmetic expression in a constant
      *>   conditional expression shall be formed in accordance with
      *>   7.3.6" -> OK  §7.3.8.2 2)  (Syntax rules)
      *> cite.py --check 7.3.6.2 "All operands shall be fixed-point
      *>   numeric literals or arithmetic expressions in which all
      *>   operands are fixed-point numeric literals" -> OK  §7.3.6.2 1)
      *>   b)  (Syntax rules)
      *> The >>IF below is otherwise a well-formed relation cce; only
      *>   its arithmetic operand breaks §7.3.6, so the only reason to
      *>   reject is SR2 (COBOLNET1619, the directive-expression
      *>   formation diagnostic).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05G.
       PROCEDURE DIVISION.
       MAIN.
       >>IF "1" + 1 = 2
           DISPLAY "SELECTED".
       >>END-IF
           STOP RUN.
