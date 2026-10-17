      *>--- conformance-case
      *> id: l1c05_cce_boolean_expression
      *> rules: [SR-7.3.8.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.8.2 SR3 — a boolean expression in a >>IF cce is a
      *>   §7.3.7 expression
      *> Rule: "A boolean expression in a constant conditional
      *>   expression shall be formed in accordance with 7.3.7,
      *>   Compile-time boolean expressions."
      *> cite.py --check 7.3.8.2 "A boolean expression in a constant
      *>   conditional expression shall be formed in accordance with
      *>   7.3.7" -> OK  §7.3.8.2 3)  (Syntax rules)
      *> cite.py --check 7.3.7.2 "all operands shall be boolean literals
      *>   or boolean expressions in which all operands are boolean
      *>   literals" -> OK  §7.3.7.2 1)  (Syntax rule)
      *> cite.py --check 7.3.7.3 "The order of precedence and the rules
      *>   for evaluation of compile-time boolean expressions are shown
      *>   in 8.8.2" -> OK  §7.3.7.3 1)  (General rule)
      *> cite.py --check 8.8.2 "1st — negation (B-NOT)" -> OK  §8.8.2 7)
      *>   b)  (Boolean expressions)
      *> cite.py --check 8.8.2 "3rd — exclusive disjunction (B-XOR)" ->
      *>   OK  §8.8.2 7) b)  (Boolean expressions)
      *> The admitting half of SR3: a boolean expression of boolean
      *>   literals only is accepted in a cce and evaluated per §8.8.2
      *>   (precedence B-NOT, B-AND, B-XOR, B-OR). The rejecting half is
      *>   the negatives l1c05-cce-boolean-*.
      *> Derivation:
      *>   B"1" B-XOR B"1" = B"0": 1 xor 1 = 0; TRUE   -> "B1-TRUE"
      *>   B"1" = B"1" B-AND B"0": right side 0; FALSE -> "B2-FALSE"
      *>   B"1100" B-OR B"0011" = B"1111": TRUE       -> "B3-TRUE"
      *>   B-NOT B"0" B-AND B"0" = B"0": B-NOT first, 1 and 0 = 0, TRUE;
      *>     B-NOT over the whole would give 1, FALSE -> "B4-TRUE"
      *>   B"1" B-OR B"1" B-XOR B"1" = B"1": B-XOR first, 1 xor 1 = 0, 1
      *>     or 0 = 1, TRUE; left to right would give 0, FALSE ->
      *>     "B5-TRUE"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05H.
       PROCEDURE DIVISION.
       MAIN.
       >>IF B"1" B-XOR B"1" = B"0"
           DISPLAY "B1-TRUE".
       >>ELSE
           DISPLAY "B1-FALSE".
       >>END-IF
       >>IF B"1" = B"1" B-AND B"0"
           DISPLAY "B2-TRUE".
       >>ELSE
           DISPLAY "B2-FALSE".
       >>END-IF
       >>IF B"1100" B-OR B"0011" = B"1111"
           DISPLAY "B3-TRUE".
       >>ELSE
           DISPLAY "B3-FALSE".
       >>END-IF
       >>IF B-NOT B"0" B-AND B"0" = B"0"
           DISPLAY "B4-TRUE".
       >>ELSE
           DISPLAY "B4-FALSE".
       >>END-IF
       >>IF B"1" B-OR B"1" B-XOR B"1" = B"1"
           DISPLAY "B5-TRUE".
       >>ELSE
           DISPLAY "B5-FALSE".
       >>END-IF
           STOP RUN.
