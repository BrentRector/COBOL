      *>--- conformance-case
      *> id: l1c07_evaldir_numeric_literal_operand
      *> rules: [GR-7.3.13.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.13.4 GR2 — a single numeric literal operand of the
      *> EVALUATE directive is a literal (keeps its fraction), not an
      *> arithmetic-expression (which is truncated to an integer).
      *> RULE 7.3.13.4 GR2: "If an operand of the EVALUATE directive
      *>   consists of a single numeric literal, that operand is treated
      *>   as a literal, not as an arithmetic-expression."
      *> RULE 7.3.6.3 GR3: "The final result of the arithmetic
      *>   expression shall be truncated to the integer part of the
      *>   value ..."
      *> cite.py:
      *>   OK  §7.3.13.4 2)  (General rules)
      *>   OK  §7.3.6.3 3)  (General rules)
      *>   OK  §7.3.13.4 4) b)  (General rules)
      *> EXPECTED OUTPUT, DERIVED:
      *>   A-LIT-1.5         subject 1.5 is a literal (GR2): not = 1,
      *>                     = 1.5.
      *>  B-EXPR-TRUNC-1    subject 1.5 + 0 is an arithmetic-expression
      *>                    truncated to 1 (7.3.6.3 GR3): not = literal
      *>                    1.5, = 1.
      *>  C-OBJ-EXPR-TRUNC  subject 3; object literal 3.7 is 3.7 (GR2)
      *>                    so not equal; object 3.7 + 0 truncates to 3.
      *>  D-RANGE-LIT       subject 1.5 not in 1 THRU 1.2, in 1.4 THRU
      *>                    1.6 (literal range ends keep fractions).
      *>  E-SIGNED-LIT      subject -1.5 (signed numeric literal): not
      *>                    = -1, = -1.5.
      *>   F-DIV-TRUNC-3     subject 7 / 2 = 3.5 truncated to 3: not =
      *>                     literal 3.5, = 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C07D.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>EVALUATE 1.5
       >>WHEN 1
           DISPLAY "A-WRONG-1".
       >>WHEN 1.5
           DISPLAY "A-LIT-1.5".
       >>WHEN OTHER
           DISPLAY "A-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE 1.5 + 0
       >>WHEN 1.5
           DISPLAY "B-WRONG-1.5".
       >>WHEN 1
           DISPLAY "B-EXPR-TRUNC-1".
       >>WHEN OTHER
           DISPLAY "B-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE 3
       >>WHEN 3.7
           DISPLAY "C-WRONG-3.7".
       >>WHEN 3.7 + 0
           DISPLAY "C-OBJ-EXPR-TRUNC".
       >>WHEN OTHER
           DISPLAY "C-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE 1.5
       >>WHEN 1 THRU 1.2
           DISPLAY "D-WRONG-LOW".
       >>WHEN 1.4 THRU 1.6
           DISPLAY "D-RANGE-LIT".
       >>WHEN OTHER
           DISPLAY "D-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE -1.5
       >>WHEN -1
           DISPLAY "E-WRONG-1".
       >>WHEN -1.5
           DISPLAY "E-SIGNED-LIT".
       >>WHEN OTHER
           DISPLAY "E-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE 7 / 2
       >>WHEN 3.5
           DISPLAY "F-WRONG-3.5".
       >>WHEN 3
           DISPLAY "F-DIV-TRUNC-3".
       >>WHEN OTHER
           DISPLAY "F-WRONG-OTHER".
       >>END-EVALUATE
           STOP RUN.
