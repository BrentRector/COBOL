      *>--- conformance-case
      *> id: l1c06-define-boolean-numeric-operand
      *> rules: [SR-7.3.11.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.11.3 SR4 DEFINE directive — boolean-expression-1 with
      *>   a non-boolean operand
      *> Rule: "Boolean-expression-1 shall be formed in accordance with
      *>   7.3.7, Compile-time
      *> boolean expressions."
      *>   cite.py --check 7.3.11.3 "Boolean-expression-1 shall be
      *>     formed in accordance
      *>   with 7.3.7"  -> OK  §7.3.11.3 4)  (Syntax rules)
      *>   cite.py --check 7.3.7.2 "all operands shall be boolean
      *>     literals or boolean
      *>   expressions in which all operands are boolean literals"
      *>   -> OK  §7.3.7.2 1)  (Syntax rule)
      *> B"1" B-AND 1 is a boolean expression whose second operand is
      *>   the NUMERIC literal
      *> 1, not a boolean literal, so the DEFINE violates SR4 via
      *>   §7.3.7.2 SR1 and shall be
      *> rejected (COBOLNET1619, directive-expression-violation).
      *>   Everything else is legal:
      *> removing "B-AND 1" leaves a valid DEFINE of a boolean literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06C.
       PROCEDURE DIVISION.
       MAIN-P.
       >>DEFINE BX AS B"1" B-AND 1
           DISPLAY "RAN".
           STOP RUN.
