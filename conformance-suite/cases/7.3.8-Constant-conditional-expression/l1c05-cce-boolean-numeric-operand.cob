      *>--- conformance-case
      *> id: l1c05-cce-boolean-numeric-operand
      *> rules: [SR-7.3.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.8.2 SR3 via §7.3.7.2 SR1 — a numeric operand of B-AND
      *> Rule: "A boolean expression in a constant conditional
      *>   expression shall be formed in accordance with 7.3.7,
      *>   Compile-time boolean expressions."
      *> cite.py --check 7.3.8.2 "A boolean expression in a constant
      *>   conditional expression shall be formed in accordance with
      *>   7.3.7" -> OK  §7.3.8.2 3)  (Syntax rules)
      *> cite.py --check 7.3.7.2 "all operands shall be boolean literals
      *>   or boolean expressions in which all operands are boolean
      *>   literals" -> OK  §7.3.7.2 1)  (Syntax rule)
      *> The >>IF below is otherwise a well-formed relation cce; only
      *>   the non-boolean operand of B-AND breaks §7.3.7, so the only
      *>   reason to reject is SR3 (COBOLNET1619).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05J.
       PROCEDURE DIVISION.
       MAIN.
       >>IF B"1" B-AND 1 = B"1"
           DISPLAY "SELECTED".
       >>END-IF
           STOP RUN.
