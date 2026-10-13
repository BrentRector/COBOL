## PB2017 - the arithmetic operators' spacing (ISO 8.7.1) is asked, and COBOLNET2631 stops advising a meaning-changing space

**What the note said, and what the re-probe showed.** PB2017 (found by the adjudication of the battery #88 differential flip
`run_misc:156`) said two things: `COMPUTE X=1*2` compiles with no diagnostic although 8.7.1 requires an arithmetic operator
to be "preceded by a space and followed by a space", and under DECIMAL-POINT IS COMMA `COMPUTE X = 1 *,1` drew COBOLNET2631
("write a space after it"), advice that yields `1 *, 1`, which is conforming, compiles clean and computes 1 times 1 instead
of 1 times 0,1. Both reproduced on the wave-1071 tree (`77f10dfb2`); the controls `1 * ,1`, `1 + ,1`, `1,5 * 2` and
`(1 + ,5) * 2` behaved. `cite.py --check` OK for 8.7.1 (with its unary exception), 8.3.5 2) and 8.3.3.3.2 3).

**The root.** 8.7.1 was the one operator clause nothing asked. Its siblings, 8.7.3 and 8.7.4, are decided from the token run
(PB1430), but a `+` or `-` cannot be: whether it is a binary operator, a unary operator or no operator (the sign that is a
numeric literal's leftmost character, 8.3.3.3.2 2); `LINE PLUS`'s `+`) is a question of grammar. So
`SeparatorRule.ArithmeticOperatorViolations(text, tree)` reads the parse tree: `addOp` under `additiveExpression`, `mulOp`
under `multiplicativeExpression` and `POWER` under `powerExpression` are binary; `addOp` under `unaryExpression` is unary
unless `ArithmeticFormationRules.SignIsPartOfLiteral` (the one contiguity test) makes the sign the literal's. The
characters beside each go through the one `OperatorSpacingMessage` verdict, which now serves all three clauses; a unary
operator may touch a left parenthesis where the space would be, and a comma or semicolon followed by a space counts as the
space (8.3.5 2)). The report is COBOLNET2993, whose description and ISO section widen to the three clauses, so no new code was
spent (the three allocated, 3647-3649, are returned).

It runs once per unit tree in `Frontend.LexAndParse` after the parse, and once per directive fragment: the conditional
compilation stage's `DirectiveDiag.ScreenLiterals` became `ScreenFragment` (literals and operator spacing, in each of the
three fragment funnels), so `>>DEFINE X AS 2*3` is held to the rule too, in a compiled branch and an omitted one.
The second defect is `IsDecimalComma`: a comma that begins a literal right after `*`, `/`, `=`, `<` or `>` (beside the existing
`+ - (`) is the decimal point, so 2631 no longer fires on `*,1` and the operator's missing space is the one report.

**Sibling sweep.** The other arm is the directive lane (done); the other operators are the same arm (`+ - * / **`, unary).
The relational symbols state no spacing rule in 8.7.5, so they are not asked. Compiling every enabled conformance program,
every NIST program and the 25 others found three genuine violations in test sources, corrected to the conforming spelling:
`2023/pb2966_sdidi_unsigned_wide_operand_and_landing` (`-W2`), `2023/pb1641_function_result_beside_decimal_operands`
(`(-FUNCTION SQRT(16))`) and NIST `IF117A.cob:783` (`(A * G)/ 1000`, one padding space moved so the sequence column stays;
the same precedent as the PB528 edits to IX109A and IX110A). D-ARGEXPR's `USING N -1` is still the expression `N - 1` and is
now refused for its missing space (docs/CONFORMANCE.md updated).

**The sweep's hole, found at the landing.** The compiled sweep covered the corpus, not the COBOL embedded in xUnit
sources, and the implementer's leg-1 gate did not reach `WideRefModDiagnosticTests`; the landing's Linux whole-population
conformance leg did (2 of 11,554 red, deterministic, red on Windows too). Its rows E4 `X(-A:1)` (a unary operator touching
its operand) and K5 `X(1:-(N))` (one touching the reference modifier's colon, a separator under 8.3.5 7) but not the space
8.7.1 requires before the operator) are non-conforming, so they are respelled `X(- A:1)` and `X(1: -(N))`, which keeps each
row's arm. The colon case had no witness: `OperatorSpacingDiagnosticTests` now pins the reference modifier's three unary
violations (before, after, both) and its four conforming spellings. The whole Linux population (unit, conformance,
characterization) was otherwise green, so no other embedded source is affected.

**Goldens.** Positive `85/pb2017_arithmetic_operator_spacing` (every conforming spelling; the expected values were computed
from 8.8.1.2 by hand before the run). Negatives `pb2017-binary-operator-no-spaces`, `-no-space-after`,
`pb2017-unary-operator-no-space-after`, `-operator-in-subscript` and `-operator-touches-comma-decimal-point`, each at
85/2002/2014/2023 (the clause carries no edition qualifier). Unit `SeparatorRuleTests` (the tree rule, the message, the
comma); Conformance `OperatorSpacingDiagnosticTests` (one diagnostic at every edition, no COBOLNET2631, the conforming
comma spellings compile, the directive lane). The 8.7.1 clause has no traceability-inventory row, so the note closes none
(`closes_rows_reason`).
