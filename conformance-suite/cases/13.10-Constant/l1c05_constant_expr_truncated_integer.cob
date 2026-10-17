      *>--- conformance-case
      *> id: l1c05_constant_expr_truncated_integer
      *> rules: [GR-13.10.4-3, GR-13.10.4-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.10.4 GR4 — a constant AS arithmetic-expression is
      *>   truncated to an integer
      *> Rule: "If arithmetic-expression-1 is specified, it is evaluated
      *>   in accordance with 7.3.6, Compile-time arithmetic
      *>   expressions, to determine the value of constant-name-1. The
      *>   class and category of constant-name-1 is numeric.
      *>   Constant-name-1 is an integer."
      *> cite.py --check 13.10.4 "If arithmetic-expression-1 is
      *>   specified, it is evaluated in accordance with 7.3.6" -> OK
      *>   §13.10.4 4)  (General rules)
      *> cite.py --check 7.3.6.3 "The final result of the arithmetic
      *>   expression shall be truncated to the integer part of the
      *>   value" -> OK  §7.3.6.3 3)  (General rules)
      *> cite.py --check 13.10.3 "If the operand of the constant entry
      *>   consists of a single numeric literal, that operand is treated
      *>   as a literal, not as an arithmetic-expression" -> OK
      *>   §13.10.3 1)  (Syntax rules)
      *> cite.py --check 13.10.3 "If constant-name-1 is an integer, it
      *>   may also be used to specify repetition in a picture
      *>   character-string" -> OK  §13.10.3 2)  (Syntax rules)
      *> §15.49.4: INTEGER-PART = SIGN(a) * INTEGER(ABS(a)), i.e.
      *>   truncation TOWARD ZERO.
      *> Derivation (W-E is PIC -9.9, so a surviving fraction shows):
      *>   K-HALF = 7 / 2 = 3.5 -> 3                     -> "HALF= 3.0"
      *>   K-NEG = -7 / 2 = -3.5 -> -3 (toward zero, not -4) ->
      *>     "NEG=-3.0"
      *>   K-SUM = 0.5 + 0.6 = 1.1 -> 1                  -> "SUM= 1.0"
      *>   K-MUL = 0.4 * 2 = 0.8 -> 0 (truncated, not rounded) ->
      *>     "MUL= 0.0"
      *>   K-LIT = 0.5 is a single literal (SR1), kept  -> "LIT= 0.5"
      *>   K-HALF is an integer: PIC X(K-HALF) is X(3), so "ABCDE" ->
      *>     "[ABC]"
      *>   OCCURS K-HALF: 3 entries of PIC X(2), LENGTH 6 -> "TABLEN=06"
      *>   numeric: 10 * K-HALF + K-NEG = 30 - 3 = 27    -> "ARITH=27"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-HALF CONSTANT AS 7 / 2.
       01 K-NEG  CONSTANT AS -7 / 2.
       01 K-SUM  CONSTANT AS 0.5 + 0.6.
       01 K-MUL  CONSTANT AS 0.4 * 2.
       01 K-LIT  CONSTANT AS 0.5.
       01 W-E    PIC -9.9.
       01 W-TXT  PIC X(K-HALF).
       01 W-TAB.
          05 W-ENT PIC X(2) OCCURS K-HALF.
       01 W-LEN  PIC 99.
       01 W-N    PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE K-HALF TO W-E.
           DISPLAY "HALF=" W-E.
           MOVE K-NEG TO W-E.
           DISPLAY "NEG=" W-E.
           MOVE K-SUM TO W-E.
           DISPLAY "SUM=" W-E.
           MOVE K-MUL TO W-E.
           DISPLAY "MUL=" W-E.
           MOVE K-LIT TO W-E.
           DISPLAY "LIT=" W-E.
           MOVE "ABCDE" TO W-TXT.
           DISPLAY "[" W-TXT "]".
           MOVE FUNCTION LENGTH(W-TAB) TO W-LEN.
           DISPLAY "TABLEN=" W-LEN.
           COMPUTE W-N = 10 * K-HALF + K-NEG.
           DISPLAY "ARITH=" W-N.
           STOP RUN.
