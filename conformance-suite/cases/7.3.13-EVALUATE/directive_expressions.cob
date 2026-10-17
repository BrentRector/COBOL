*>--- conformance-case
*> id: directive_expressions
*> rules: [SR-7.3.13.3-1, SR-7.3.13.3-10, GR-7.3.13.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §7.3.6 / §7.3.7 / §7.3.8 (ledger C2) — compile-time EXPRESSION evaluation in conditional-compilation
      *> directives. >>DEFINE now EVALUATES a MULTI-TOKEN arithmetic operand (was silently bound to its first
      *> token) and a boolean-expression operand; >>IF (a constant-conditional-expression) and >>EVALUATE select
      *> the surviving source lines by the evaluated values. LVL = 2*3+1 = 7; FLG = "1100" B-OR "0011" = "1111".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CCEXPR.
       PROCEDURE DIVISION.
       MAIN.
      >>DEFINE LVL AS 2 * 3 + 1
      >>DEFINE FLG AS B"1100" B-OR B"0011"
      >>IF LVL = 7
           DISPLAY "LVL-IS-7".
      >>ELSE
           DISPLAY "LVL-WRONG".
      >>END-IF
      >>IF FLG = B"1111"
           DISPLAY "FLG-ALL-ONES".
      >>END-IF
      >>EVALUATE LVL
      >>WHEN 1 THROUGH 5
           DISPLAY "LOW".
      >>WHEN 6 THROUGH 10
           DISPLAY "MID".
      >>WHEN OTHER
           DISPLAY "HIGH".
      >>END-EVALUATE
           STOP RUN.
