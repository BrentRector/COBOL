*>--- conformance-case
*> id: l1_exp_signed_float_literal
*> rules: [FMT-15.34.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.34.2 — the EXP general format `FUNCTION EXP ( argument-1 )`, with argument-1 a
      *> SIGNED floating-point numeric literal whose EXPONENT is also signed.
      *>
      *> --check validated:
      *>   cite.py --check 15.34.2 "<u>FUNCTION</u> <u>EXP</u> ( argument-1 )"
      *>     -> OK §15.34.2 (General format) - both words underlined (required), the parentheses
      *>        and argument-1 unbracketed, no optional or repeated element: exactly one argument.
      *>   cite.py --check 15.3 "An arithmetic expression or a numeric data item shall be
      *>     specified" -> OK §15.3 10) - argument-1 is a type-10 NUMERIC argument, so a numeric
      *>     literal is one legal instance of the format's argument-1.
      *>   §8.3.3.3.3 r2 "The literal to the left of the 'E' represents the significand. It may be
      *>     signed and shall include a decimal point."
      *>   §8.3.3.3.3 r3 "The literal to the right of the 'E' represents the exponent. It may be
      *>     signed and shall have a maximum of four digits and no decimal point."
      *>     - so BOTH signs belong to the ONE literal and `-1.5E-3` is a single argument-1, not
      *>       an operator applied to something.
      *>   §8.3.3.3.3 r5 "The value of a floating-point numeric literal is the algebraic product
      *>     of the value of its significand and the quantity derived by raising ten to the power
      *>     of the exponent." (--check OK) - which is where each value below starts.
      *>   cite.py --check 15.34.4 "The equivalent arithmetic expression is:" -> OK §15.34.4 1),
      *>     whose expression is (FUNCTION E ** (argument-1)) (--check OK on that text too).
      *>   cite.py --check 15.35.4 "The equivalent arithmetic expression is:" -> OK §15.35.4 1),
      *>     EXP10's expression being (10 ** (argument-1)).
      *>   cite.py --check 15.4.1 "the value returned is an implementor-defined approximation of
      *>     the value of that expression" -> OK - hence WINDOWS below, not printed digits.
      *>
      *> ⛔ WHY THIS LITERAL SHAPE. 2023/signed_float_literal_argument covers `-1.5E1` and
      *> `+1.5E1` - a signed SIGNIFICAND with an UNSIGNED exponent - and pins the binary64
      *> renderings, which are an implementor determination rather than a rule. A SECOND sign in
      *> the same token is exactly where a maximal-munch lexer repair can stop short, and it is
      *> the axis on which this row's verdict was re-taken. Every literal below therefore carries
      *> both signs, and all FOUR sign combinations §8.3.3.3.3 r2 and r3 jointly admit are written
      *> on EXP itself: (-,-) at line 1, (+,+) at line 2, (-,+) at line 3 and (+,-) at line 6.
      *> The (+ significand, - exponent) case is a distinct lexer path - a leading '+' has to open
      *> the signed-literal token while the float body goes on to consume an 'E-' tail - and it is
      *> written NOWHERE else in the corpus: 2023/signed_float_literal_argument has only `-1.5E1`,
      *> `+1.5E1` and `-1.5E3`, every one of them with an UNSIGNED exponent.
      *>
      *> The expected values are the equivalent arithmetic expressions' own values, hand-derived
      *> and pinned by an INCLUSIVE 1E-6 window at the receiver's scale (the general_format_
      *> intrinsics_batch1 convention: >= and <=, never > and <, because a development that lands
      *> exactly ON a window edge is one §15.4.1 expressly admits):
      *>   e ** -0.0015 = 1 - 0.0015 + 0.000001125 - 0.0000000005625 + ... = 0.9985011244377...
      *>   e ** +1.5    = 4.4816890703380...
      *>   e ** -1.5    = 0.2231301601484...
      *>   10 ** -1.5   = 1 / 31.6227766016838 = 0.0316227766016...
      *>   10 ** +2.0   = 100
      *>   e ** 0       = 1
      *>   e ** +0.0015 = 1 + 0.0015 + 0.000001125 + 0.0000000005625 + ... = 1.0015011255627...
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1EXPFLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R PIC 9V9(9) VALUE 0.
       01 W-H PIC 9(3)V9(6) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
      *> 0 - the format in the standard's own printed spacing, with a plain integer literal.
           COMPUTE W-R = FUNCTION EXP ( 0 ).
           IF W-R >= 0.999999 AND W-R <= 1.000001
               DISPLAY "0-PRINTED-FORM=IN"
           ELSE
               DISPLAY "0-PRINTED-FORM=OUT"
           END-IF.
      *> 1 - negative significand, NEGATIVE exponent.
           COMPUTE W-R = FUNCTION EXP(-1.5E-3).
           IF W-R >= 0.998501 AND W-R <= 0.998502
               DISPLAY "1-NEG-NEG=IN"
           ELSE
               DISPLAY "1-NEG-NEG=OUT"
           END-IF.
      *> 2 - positive significand, POSITIVE exponent.
           COMPUTE W-R = FUNCTION EXP(+1.5E+0).
           IF W-R >= 4.481689 AND W-R <= 4.481690
               DISPLAY "2-POS-POS=IN"
           ELSE
               DISPLAY "2-POS-POS=OUT"
           END-IF.
      *> 3 - negative significand, POSITIVE exponent.
           COMPUTE W-R = FUNCTION EXP(-1.5E+0).
           IF W-R >= 0.223130 AND W-R <= 0.223131
               DISPLAY "3-NEG-POS=IN"
           ELSE
               DISPLAY "3-NEG-POS=OUT"
           END-IF.
      *> 4 - the EXP10 sibling on the same lexer seam (§15.35.2 / §15.35.4 r1).
           COMPUTE W-R = FUNCTION EXP10(-1.5E+0).
           IF W-R >= 0.031622 AND W-R <= 0.031623
               DISPLAY "4-EXP10-NEG=IN"
           ELSE
               DISPLAY "4-EXP10-NEG=OUT"
           END-IF.
      *> 5 - and its integer-valued point, where (10 ** 2.0) is 100.
           COMPUTE W-H = FUNCTION EXP10(+2.0E+0).
           IF W-H >= 99.999999 AND W-H <= 100.000001
               DISPLAY "5-EXP10-POS=IN"
           ELSE
               DISPLAY "5-EXP10-POS=OUT"
           END-IF.
      *> 6 - POSITIVE significand with a NEGATIVE exponent, the fourth combination r2 and r3
      *> jointly admit and the one written nowhere else. §8.3.3.3.3 r5 gives +1.5E-3 the value
      *> +1.5 * 10 ** -3 = 0.0015, and §15.34.4 r1's expression (FUNCTION E ** 0.0015) is
      *> 1.0015011255627..., which the 9V9(9) receiver carries as 1.001501125.
           COMPUTE W-R = FUNCTION EXP(+1.5E-3).
           IF W-R >= 1.001501 AND W-R <= 1.001502
               DISPLAY "6-POS-NEG=IN"
           ELSE
               DISPLAY "6-POS-NEG=OUT"
           END-IF.
           STOP RUN.
