## Entry 945 — 2026-07-21 12:06 PDT — Wave D (cont.): the C2 frontend DEFECT closed — boolean fold + ANTLR directive-expression grammar + frontend rewire (Tokenize/CondParser deleted), COBOLNET1619

The ledger-C2 finish: the frontend conditional-compilation stage now evaluates EVERY directive operand /
constant-conditional-expression through the ONE shared `CompileTimeExpressionEvaluator` + ANTLR, closing the
silent-wrong-value defect (`>>DEFINE X AS 1 + 2` bound `X = 1`; boolean operands were never evaluated). The
hand-rolled `Tokenize`/`CondParser`/`Value`/`Relate` engine is DELETED (singular-pattern). Design SSOT:
`docs/rearchitecture/DESIGN-compile-time-expressions.md` (updated same change set for the §7.3.3 SR10 correction).

- **The load-bearing spec correction — §7.3.3 SR10** (surfaced by a spec re-scout): *"A literal in a compiler
  directive shall not be specified as a concatenation expression, a figurative constant, or a floating-point
  numeric literal."* A blanket rule over EVERY directive. So a directive operand differs from a CONSTANT DATA
  ENTRY (§13.10.3, which DOES admit a sole float): the §7.3.3 SR10 bar lives ONLY in the frontend `EvaluateOperand`/
  `EvaluateDirectiveArithmetic`, never in the shared arithmetic core (the CONSTANT binder keeps float acceptance,
  byte-identical). Figuratives are barred, so a compile-time BOOLEAN operand is a boolean LITERAL only (§7.3.7.2
  SR1) — the runtime §8.8.2 figuratives (`ZERO`/`ALL "literal"`) never reach the fold, and `BitString` carries no
  positionless case. This corrected the design's §6 (which had described figurative-length handling for the fold).
- **Boolean fold** (`BitString` + `CompileTimeExpressionEvaluator.EvaluateBoolean`): folds constant bit strings via
  the SAME `BooleanExpressionResolver.Resolve<T>` the runtime `ConditionBinder` uses (`T = BitString`), so the
  context-inherited rule-7b shift precedence is shared, not re-derived. `BitString`'s And/Or/Xor/Not/Shift mirror the
  runtime `CobolBool` kernel exactly (Frontend can't reference Runtime — algorithm shared, not code): right-zero-
  extension, result length = larger operand, zero-length→zero-length, shift zero-fill vs circular, count-0 identity.
- **cce walk** (§7.3.8): `EvaluateCce` over `cceOr/And/Not/Primary/definedCondition/cceRelationOrBoolean`. §7.3.8.2
  SR1a.1 category match, SR1a.2 non-numeric relations = / <> only, §7.3.8.3 GR2 non-numeric equality LENGTH-SENSITIVE
  (unequal length ⇒ unequal, NOT space-extended), §7.3.8.4.4 defined-condition, §8.8.4.3 SR1 bare-boolean length-1,
  §8.8.4.13 formation-error-reported-regardless-of-short-circuit (every AND/OR operand evaluated). A `CtValue`
  (category-tagged, hand-written Equals) models numeric/alnum/national/boolean; the name resolver was unified to
  `Func<string, CtValue?>` so a bare name substitutes its value of ANY category (an alnum compilation variable like
  SpecFixTests' SYS was the bug that drove this — a name forced through the numeric arithmetic path SR1b-rejected).
- **ANTLR grammar** (`CobolExpressions.g4` + `CobolLexer.g4`): the isolated `compileTimeOperandFragment` /
  `constantConditionalExpression*` fragment rules (reachable only from the frontend fragment-parse — zero blast
  radius, the `functionArgListFragment` precedent); the `PrimeDirectiveExpr()` lexer flag (DEFINED becomes a token,
  every `(` groups — no subscript-mode leak on `A B-AND (…)`); `DirectiveExpressionFragment` re-parse helper.
- **`boolExprAhead()` completed**: it detected `B-AND/B-OR/B-XOR/B-NOT` but not the shift operators, so a shift-only
  boolean expression (`A B-SHIFT-L 2`) wasn't recognized. Added `B_SHIFT_L/R/LC/RC` (never legal user words — safe on
  the SHARED main-parse predicate); a latent-gap fix (a shift-only boolean condition now parses in the main grammar).
- **Frontend rewire** (`ConditionalCompilationProcessor`): the line-inclusion state machine STAYS (it's a §7.2 text
  stage — text-1/text-2 may be un-expanded COPY / non-COBOL); only the expression/cce/operand EVALUATION moved to
  ANTLR + the shared evaluator. A small directive-syntax splitter (`SplitDefine`) still extracts name/AS/OFF/
  PARAMETER/OVERRIDE (directive keywords, not expression syntax). All formation violations → COBOLNET1619 (a new
  `DiagnosticCatalog` descriptor; `DIAGNOSTICS.md` regenerated). EVALUATE SR11 (same-category object) enforced.
- **Legacy-oracle reality** (corrects the design's earlier "greenfield-only path" posture): the FROZEN legacy
  `Compilation.cs:345` shares `Process` and IS exercised with directives (`SpecFixTests` CC1–CE3 end-to-end). The
  shared evaluator reproduces the old single-token behavior EXACTLY, so those 9 stay green; the golden
  `directive_expressions` (2002) passes BOTH the greenfield `CorpusRunnerTests` and the legacy auto-discovering
  `ConformanceTests` (its cce folds to a DISPLAY-only surviving program), so no `GreenfieldOnly` needed.
- ⚠ AI missteps: (1) drafted `BitString` WITH figurative machinery before the spec re-scout surfaced §7.3.3 SR10 —
  removed it. (2) First `EvaluateOperand` forced a bare name through the numeric arithmetic path, SR1b-rejecting an
  ALPHANUMERIC compilation variable (`SYS = "type A"`); fixed to resolve a bare name to its full `CtValue`. (3) A
  test used an UNDERSCORE compilation-variable name the deleted tokenizer wrongly accepted (ANTLR/§8.3.1.2 forbid
  underscores) — corrected the test to a hyphenated name (invalid source, not a compiler bug).
- ⚔ A 5-dimension adversarial-review WORKFLOW (refute-verify) over the delicate pieces caught THREE major defects the
  392-test suite missed — all fixed + regression-tested (compile-time AND runtime): (a) `BooleanExpressionResolver`
  deferred a POSTFIX shift on the operator stack, so a higher-precedence FOLLOWING operator folded its right operand
  INTO the shift's operand (`A B-OR B B-SHIFT-L 1 B-AND C` mis-grouped) — the shift is now applied IMMEDIATELY;
  since the runtime `ConditionBinder` shares the resolver, COMPUTE-F2 was mis-folding too (`boolean_shift_mixed`
  golden extended with the case). (b) The boolean cce relation used length-sensitive equality; §8.8.4.2.8 RIGHT-
  extends the shorter operand (§7.3.8.3 GR2's length-sensitivity is for non-numeric-non-boolean) — added
  `CtValue.RelationalEquals` / `BitString.EqualExtended` (SR2 redefinition keeps length-sensitive). (c) A shift
  count &gt; `long.MaxValue` overflowed the `(long)` cast — the count is now reduced (logical cap-at-length, circular
  mod-length) before the cast.
- Tests: 13 `BitString`-fold + cce added in `CompileTimeBooleanCceTests` (30 cases), 6 defect-fix/reject cases in
  `ConditionalCompilationDefineTests`, the `directive_expressions` 2002 golden. Full legacy guard green.

