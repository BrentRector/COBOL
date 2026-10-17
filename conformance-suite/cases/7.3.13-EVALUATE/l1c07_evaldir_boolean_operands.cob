      *>--- conformance-case
      *> id: l1c07_evaldir_boolean_operands
      *> rules: [GR-7.3.13.4-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.13.4 GR3 — boolean-expression subject and object of
      *> the EVALUATE directive are evaluated per 7.3.7 (-> 8.8.2).
      *> RULE 7.3.13.4 GR3: "Boolean-expression-1 and
      *>   boolean-expression-2 are evaluated in accordance with 7.3.7,
      *>   Compile-time boolean expressions."
      *> RULE 7.3.7.3 GR1: "The order of precedence and the rules for
      *>   evaluation of compile-time boolean expressions are shown in
      *>   8.8.2, Boolean expressions."
      *> RULE 8.8.2 7) b): precedence "1st — negation (B-NOT) 2nd —
      *>   conjunction (B-AND) 3rd — exclusive disjunction (B-XOR)
      *>   4th — inclusive disjunction (B-OR)"
      *> cite.py:
      *>   OK  §7.3.13.4 3)  (General rules)
      *>   OK  §7.3.7.3 1)  (General rule)
      *>   OK  §8.8.2 7) b)  (Boolean expressions)
      *> EXPECTED OUTPUT, DERIVED (bitwise, per 8.8.2):
      *>   A-AND-0010         1010 B-AND 0110 = 0010 (not 0001).
      *>   B-NOT-0010         B-NOT 1101 = 0010.
      *>   C-PRECEDENCE-1110  1100 B-OR 1010 B-AND 0110: B-AND first ->
      *>                      1010 B-AND 0110 = 0010; 1100 B-OR 0010 =
      *>                      1110. (Left-to-right would give 0110.)
      *>   D-OBJECT-XOR-0110  object 1100 B-XOR 1010 = 0110 = subject.
      *>  E-XOR-BEFORE-OR    1100 B-OR 1010 B-XOR 1000: B-XOR first ->
      *>                     0010; 1100 B-OR 0010 = 1110. (Left-to-right
      *>                     would give 0110.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C07E.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>EVALUATE B"1010" B-AND B"0110"
       >>WHEN B"0001"
           DISPLAY "A-WRONG-0001".
       >>WHEN B"0010"
           DISPLAY "A-AND-0010".
       >>WHEN OTHER
           DISPLAY "A-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE B-NOT B"1101"
       >>WHEN B"0010"
           DISPLAY "B-NOT-0010".
       >>WHEN OTHER
           DISPLAY "B-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE B"1100" B-OR B"1010" B-AND B"0110"
       >>WHEN B"0110"
           DISPLAY "C-WRONG-LEFT-TO-RIGHT".
       >>WHEN B"1110"
           DISPLAY "C-PRECEDENCE-1110".
       >>WHEN OTHER
           DISPLAY "C-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE B"0110"
       >>WHEN B"1100" B-XOR B"1010"
           DISPLAY "D-OBJECT-XOR-0110".
       >>WHEN OTHER
           DISPLAY "D-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE B"1100" B-OR B"1010" B-XOR B"1000"
       >>WHEN B"0110"
           DISPLAY "E-WRONG-LEFT-TO-RIGHT".
       >>WHEN B"1110"
           DISPLAY "E-XOR-BEFORE-OR".
       >>WHEN OTHER
           DISPLAY "E-WRONG-OTHER".
       >>END-EVALUATE
           STOP RUN.
