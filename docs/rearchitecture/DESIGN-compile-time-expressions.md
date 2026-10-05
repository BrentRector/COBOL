# DESIGN — Compile-time expression evaluation (§7.3.6 / §7.3.7 / §7.3.8)

> **Status: IMPLEMENTED (P13 Wave D, ledger C2).** Canonical deep-dive for the ONE shared compile-time
> expression evaluator (frontend conditional-compilation stage + CONSTANT-entry binder), the ONE boolean
> precedence resolver (compile-time + runtime), and the ANTLR grammar that parses every compiler-directive
> expression. Design SSOT for review-ledger item **C2** (`PHASE-13-plan-vs-spec-review.md §24`). Keep CURRENT
> (describes the compiler as built); do not narrate the doc's own revision history — that belongs in `DEVLOG.md`.

## 0. ⛔ THE MASTER CONSTRAINT — §7.3.3 SR10 (a directive is NOT a CONSTANT data entry)

**§7.3.3 SR10 (a general syntax rule over EVERY compiler directive):** *"A literal in a compiler directive shall
not be specified as a concatenation expression, a figurative constant, or a floating-point numeric literal."*
This governs every `>>DEFINE` / `>>EVALUATE` / `>>IF` operand and every literal inside a cce. Consequences the
evaluator enforces (COBOLNET1619) — and the reason the shared arithmetic core is CONSUMER-agnostic:

* **No floating-point literal** as a directive operand — even a *sole* one. (The CONSTANT data entry `01 c CONSTANT
  AS …`, §13.10.3, is NOT a compiler directive, so it DOES admit a sole floating-point literal — `EvaluateArithmeticOperand`
  keeps that behavior; the §7.3.3 SR10 bar lives in the frontend-only `EvaluateOperand`/`EvaluateDirectiveArithmetic`,
  never in the shared arithmetic core.)
* **No figurative constant** (`ZERO`/`SPACE`/`HIGH-VALUE`/`LOW-VALUE`/`QUOTE`/`ALL "literal"`) — in an arithmetic
  operand (`ZERO_ARITH`), a non-numeric operand (`figurativeConstant`), or a boolean operand. So a compile-time
  BOOLEAN operand is a boolean LITERAL only (§7.3.7.2 SR1) — the runtime §8.8.2 figurative operands (`ZERO`,
  `ALL B"…"`) are barred here, and `BitString` carries no positionless (figurative) case.
* **No concatenation expression.**

## 1. Scope and the defect being closed

A *compile-time expression* is an arithmetic (§7.3.6), boolean (§7.3.7), or constant-conditional (§7.3.8)
expression evaluated by the compiler, never at run time. Admitted in:

| Consumer | Spec | Operand kinds |
|---|---|---|
| `>>DEFINE cv AS …` | §7.3.11 | arithmetic-expr, boolean-expr, literal, PARAMETER, OFF |
| `>>EVALUATE …` / `>>WHEN …` | §7.3.13 | arithmetic-expr, boolean-expr, literal (subject/object/THRU range) |
| `>>IF cce` / `>>ELSE` / `>>END-IF` | §7.3.16 → §7.3.8 | constant-conditional-expression |
| `>>DISPLAY …` | §7.3.12 | arithmetic-expr, boolean-expr, literal, PARAMETER, repeated, then UPON (§8a) |
| `01 c CONSTANT AS arithmetic-expr` | §13.10.4 GR4 | arithmetic-expr (numeric only) |

**The defect (ledger C2, MAJOR — silent wrong value).** Two unrelated code paths: the **binder**
(`DataBinder.Constants.cs EvalConstExpr`) had a complete, battery-tested §7.3.6 arithmetic evaluator, reachable
only from CONSTANT-entry binding (numeric only). The **frontend** (`ConditionalCompilationProcessor`) resolved
each directive operand by a **single token** — `>>DEFINE X AS 1 + 2` bound `X = 1`, boolean operands were never
evaluated, no diagnostic raised.

**Mandate.** ONE compile-time expression evaluator shared by both consumers (the C2 verifier correction: "*lift/
reuse that evaluator rather than build a new one*"). ONE parser — ANTLR — for all directive-expression syntax.
ONE boolean precedence resolver shared by the compile-time and runtime paths. No operand silently mis-bound;
anything unrepresentable is rejected **loudly**. No deferrals.

## 2. Two jobs, cleanly separated

The conditional-compilation stage (§7.2 text-manipulation) has two different jobs:

1. **Line selection (NOT parsing).** Walking `>>IF/>>ELSE/>>END-IF/>>EVALUATE/>>WHEN` nesting to decide which
   physical lines survive. "text-1/text-2" may be any source lines — including un-expanded `COPY` and, in omitted
   branches, non-COBOL — so it MUST be a pre-parse text stage (§7.2); the main grammar cannot own it. Stays a
   small line-inclusion state machine in `ConditionalCompilationProcessor`.
2. **Expression / condition parsing + evaluation (100% ANTLR).** Every `>>DEFINE` operand, `>>IF` cce, and
   `>>EVALUATE`/`>>WHEN` operand is **fragment-parsed by ANTLR** and **evaluated by the ONE shared evaluator**
   over the parse tree. There is no hand-rolled tokenizer or condition parser — the ANTLR grammar is the single
   source of truth for directive-expression syntax.

```
   line-inclusion state machine (text selection)
        │  hands each directive's expression/cce TEXT to ↓
        ▼
   ANTLR fragment parse (CobolLexer + CobolParserCore, DEFAULT mode, ZeroTokenRewriter applied)
        │  → compileTimeOperandFragment / constantConditionalExpressionFragment tree
        ▼
   CompileTimeExpressionEvaluator (walks CobolParserCore.*Context; boolean via BooleanExpressionResolver)
        │  injected: edition (→ arithmetic mode + literal capacity) · name resolver · code-preserving diag sink ·
        │            operand-source clause · decimalPointIsComma
        ▼
   CtValue (numeric / alphanumeric / national / boolean) — or a loud diagnostic, never a wrong value
```

**Syntax is a property of the directive line; only EVALUATION and EMISSION depend on the branch** (§7.2.1: "compiler
directives" shall be "syntactically correct in the initial source text and library text", and the false path of an IF or
EVALUATE is part of that text; kb/Work PB1363, PB1364, PB1367, PB806). So the driver keeps three questions apart:
*structure* (the `Frame` stack: kind, phase, the library text it opened in — COBOLNET2649, judged in every branch),
*operand syntax* (every directive line is asked of `CompilerDirectiveCatalog.CheckOperand` in every branch, and the
conditional-compilation arms fragment-parse their operands in an omitted branch with `CheckOperandSyntax` /
`CheckCceSyntax`, which report a malformed operand as COBOLNET1619 and never need a value) and *evaluation* (names,
categories, division, which WHEN matches — a compiled branch only, because an omitted branch's `Q` need not be a
compilation variable). A `>>DEFINE` in an omitted branch is checked against its general format and its operand is
parsed, but it applies nothing. Words whose operand a downstream stage parses (`TURN`, `FLAG-02`, `FLAG-14`,
`COBOL-WORDS`) are the exception: their stage runs on the FINAL text and never sees an omitted line, so their operand
syntax is not yet checked in an omitted branch.

## 3. Assembly layering (done)

The evaluator must live in **Frontend** so both callers reach it (`Editions ← Frontend ← Compiler`; Frontend
cannot reference Compiler). Its lexical dependencies were relocated down accordingly — the singular lexical
utilities, now reachable by both layers:

* **`CobolLiteral`** (the ONE literal codec) relocated Compiler → `Frontend/Common` (namespace `CobolNet.Common`
  unchanged; all Compiler `using`s still resolve). **Done, build-verified.**
* **`NumericLiteral.Normalize`** — the §12.3.7 GR14a numeric-literal normalizer extracted to `Frontend/Common`;
  `DataBinder.NormalizeNumericLiteral` delegates to it and keeps emitting COBOLNET0895 byte-identically. **Done,
  build-verified.**

`PicCategory` stays in Compiler — the evaluator uses its own `CtCategory`; the binder adapts at the call boundary.

**Frontend → Runtime (kb/Work PB1592).** The documented layering is `Runtime → Frontend → Compiler → Cli`
(DESIGN-edition-framework; the runtime references nothing in the solution), and the Frontend now takes that edge:
the evaluator carries every numeric value in the runtime's `CobolDec` (the SDIDI) and selects its arithmetic mode
through the runtime's `DialectBehaviors` register. The standard's own per-edition rule demands it — standard
arithmetic at 2002/2014 (§5.1) is the SDIDI engine, and a second copy of that engine in the Frontend would be two
implementations of one rule.

## 4. The ANTLR grammar (one source of truth for syntax)

Isolated fragment entry rules, reachable only from the frontend fragment-parse (referenced by nothing in
`compilationUnit` — zero blast radius, the `functionArgListFragment` precedent). They reuse existing operand
sub-rules — no duplicated expression grammar:

```antlr
compileTimeOperandFragment : compileTimeOperand EOF ;
compileTimeOperand
    : {boolExprAhead()}? booleanExpression      // a genuine boolean expression (B-operator/BOOLLIT present)
    | arithmeticExpression                       // numeric operand (single numeric literal too — GR5 in eval)
    | nonNumericLiteral                          // string / national / hex literal operand
    ;
constantConditionalExpressionFragment : constantConditionalExpression EOF ;
constantConditionalExpression : cceOr ;
cceOr      : cceXor ( OR cceXor )* ;
cceXor     : cceAnd ( xorOperator cceAnd )* ;                     // XOR / EXCLUSIVE-OR — the ONE xorOperator rule
cceAnd     : cceNot ( AND cceNot )* ;
cceNot     : NOT? ccePrimary ;                                    // Table 5: 'NOT NOT' is not permissible
ccePrimary : LPAREN constantConditionalExpression RPAREN
           | definedCondition
           | cceRelationOrBoolean ;
definedCondition : cobolWord IS? NOT? DEFINED ;                       // DEFINED: primed-lexer token (below)
cceRelationOrBoolean
    : {boolExprAhead()}? booleanExpression                            // §8.8.4.3 simple boolean condition (len-1)
    | compileTimeOperand ( comparisonOperator compileTimeOperand )? ;   // §8.8.4.2 relation
```

* **Operand-kind disambiguation** uses the existing `boolExprAhead()` predicate (the mechanism the source
  `primaryCondition` rule already uses): `booleanExpression` is entered only when a real B-operator is present or
  a grouping-paren run opens on a boolean literal — `(B"101")`, §8.8.2's "a boolean expression enclosed in
  parentheses", which carries no operator to find (kb/Work PB1370; the same predicate serves the runtime
  conditions, so `IF (B"1") = F` is recognized too); otherwise arithmetic (incl. a single numeric literal) or a
  non-numeric literal. A parenthesized boolean NAME with no operator is not token-decidable (its category is a
  binding fact).
* **Edition gate.** A compile-time boolean expression is formed per §8.8.2 OF THE TARGETED EDITION, so each
  evaluated fragment asks the ONE boolean-operator introduction gate (`BooleanOperatorGate`, shared with
  `VersionConformancePass`): a `B-SHIFT-*` below 2023 is COBOLNET0900, exactly as its runtime twin (kb/Work
  PB1370; `BooleanExpressionGateSiteDriftTests` pins both fragment sites). The evaluator
  dispatches on **which operand sub-node parsed**, not a token guess — necessary because `booleanExpression →
  valueOperand` would otherwise match every arithmetic/non-numeric operand.
* **`DEFINED`** is not reserved in the source language — a token only inside the fragment via a primed lexer flag
  `PrimeDirectiveExpr()` (`DEFINED : {_primeDirectiveExpr}? 'DEFINED' ;`), the `PrimeFunctionArgs()` pattern.
  Zero global blast radius.
* **Relops** reuse `comparisonOperator` as it stands — the printed §8.8.4.2.2 Format 1 set, each NOT part of its
  alternative (the rule used to write a free `IS? NOT?` before the operator, a second unscreened NOT that let
  `>>IF X NOT >= 3` through; kb/Work PB1034). The non-numeric `=`/`<>` restriction (§7.3.8.2 SR1a.2) is enforced in
  the evaluator. Abbreviated combined relations are not admitted (§7.3.8.2 SR1d).

### 4.1 Fragment-parse mechanics (identical lexing to the main parse)

* **DEFAULT lexer mode** — NOT `PrimeFunctionArgs`: §7.3.6 has no argument-juxtaposition, so `1 - 2` is
  subtraction.
* **`PrimeDirectiveExpr()`** — a lexer flag (the `PrimeFunctionArgs()` pattern) with two effects specific to the
  directive-expression context: it makes `DEFINED` a token (context-sensitive — reserved nowhere else), and it
  makes every `(` a grouping `LPAREN` (subscript mode is never pushed). The second is required: the subscript-vs-
  grouping lexer decision treats a `(` after any word that *could* be a data-name as a subscript, and the boolean
  operators (`B-AND` etc.) are legal data-names below 2023 — so without this flag `A B-AND (…)` mis-lexes the
  parenthesized group in SUBSCRIPT mode (confirmed by token dump). Directive operands never subscript, so every
  `(` is unambiguously a group. (The same latent subscript-vs-grouping ambiguity affects `(` after a boolean
  operator in the *main* parse — a pre-existing runtime `COMPUTE` Format-2 limitation, tracked separately.)
* **`ZeroTokenRewriter`** applied to the fragment stream (as `Frontend.LexAndParse`), so figurative `ZERO` in an
  arithmetic operand becomes `ZERO_ARITH`.
* **Edition** `EditionInfo.Of(EditionInfo.Latest)` (not literal 2023) — the operand parses at the newest edition
  (the whole-`>>`-facility introduction gate below 2002 is ledger C15). Recorded coupling: `Latest` also relaxes
  fixed-point digit capacity (§8.3.3.3.2), closed with C15 when the real edition is threaded.
* An error-flag listener (the `FunctionArgFragment.SyntaxErrorFlag` pattern) turns any syntax error into a loud
  `COBOLNET1619`; a partial parse is never evaluated.

## 5. The shared evaluator API — GR5/GR3 owned at the public boundary

```csharp
public sealed class CompileTimeExpressionEvaluator(
    EditionInfo edition,                     // selects the §7.3.6.3 GR2 mode (§5.1) + the §8.3.3.3.2 literal capacity
    Func<string, CtValue?> resolveName,     // a name → its bound value, or null if undefined
    ICtDiagnostics diag,                     // CODE-preserving sink (§5.2) — not a bare Action<string>
    CtOperandVocabulary vocab,               // per-consumer operand-source clause / noun (§5.2)
    bool decimalPointIsComma)                // §12.3.7 GR14a normalization (binder: real; frontend: false, §5.3)
{
    // Public operand boundary — applies §7.3.11.4 GR5 reclassification + §7.3.6.3 GR3 truncation ITSELF.
    public CtNumber? EvaluateArithmeticOperand(CobolParserCore.ArithmeticExpressionContext e, string where);
    // A boolean operand → its bit string (via BooleanExpressionResolver, §6). A shift count is one integer
    // literal or numeric-variable name (Table 4 shape screened first), held to the integer-literal FORM (§6).
    public BitString? EvaluateBoolean(CobolParserCore.BooleanExpressionContext e, string where);
}
public readonly record struct CtNumber(bool WasSingleLiteral, CobolDec Value, string Literal, bool IsInteger);   // GR3-truncated unless WasSingleLiteral
```

* **GR5 + GR3 live INSIDE `EvaluateArithmeticOperand`.** The raw-value recursion (`EvalArith`, the lift of
  `EvalConstExpr`) stays private — intermediates correctly un-truncated (§7.3.6.3 GR1). At the boundary: a single
  numeric literal (private `SoleNumericLiteral` probe) is kept exact (GR5 / §13.10.3 SR1 — `AS 0.25` → `0.25`)
  and is returned AS WRITTEN in `Literal` — its sign and decimal separator included, never a normalized form
  (kb/Work PB1230: §13.10.4 GR1 makes a constant-name "as if literal-1 … were written", so `AS +5` substitutes
  `+5` and `AS 1,5` under DECIMAL-POINT IS COMMA substitutes `1,5`, which the consumer's own literal chokepoint
  then reads in the active mode; the value a consumer computes with is `Value`, and `IsInteger` answers §13.10.3
  SR2's "constant-name-1 is an integer" for the integer positions). A duplicated constant-name's §13.10.3 SR9
  check compares the AS operands AS WRITTEN, through the ONE text-word equality (`TextWordSequence.Matches`,
  §7.2.3.4 9) c)) — never the folded values;
  otherwise the final result is truncated to its integer part (GR3 / INTEGER-PART §15.49). No consumer re-does
  this — the probe/truncate rule lives in one place, not copied at each operand site — and the boolean shift
  count is correct because it calls this boundary.
* **The literal capacity is enforced here too.** GR3 makes the final result "an integer numeric literal", so a
  result with more digits than a fixed-point literal may have (`EditionInfo.MaxDigits`, 31 — §8.3.3.3.2) is
  refused; so is a sole literal or a literal operand past that capacity. A sole literal is otherwise NOT bounded
  by any arithmetic mode (GR5 — it is not an expression).

### 5.1 Arithmetic semantics (§7.3.6) — one evaluator, the edition's mode

The private recursion is the existing `EvalConstExpr` walk: `+ - * /` and unary sign over the grammar precedence
tiers (§8.8.1/§7.3.6.3 GR1); SR1a exponentiation reject; SR1b operand-is-fixed-point-literal-or-numeric-name
(floating-point/E-form rejected); SR1c div-by-zero reject; overflow reported, never wrapped.

**The MODE is selected per edition (§7.3.6.3 GR2 + Annex E.2 6)/21), kb/Work PB1592).** `CompileTimeArithmetic.For
(edition)` is the ONE selection — every operand ENTERS the mode and every operation RUNS in it, so the three
consumers cannot disagree:

| Edition | Mode | Why |
|---|---|---|
| 2002, 2014 | `CompileTimeArithmetic.Standard` — the SDIDI (`CobolDec`, 34 digits, decimal128 range), NEAREST-AWAY-FROM-ZERO (§11.9.11.2 GR3 a) | E.2 6): "The previous COBOL Standard required the use of an arithmetic mode that is no longer supported"; the only mode 2023 removed is Standard Arithmetic (E.2 21). This compiler's standard arithmetic IS the SDIDI engine (`ArithmeticModes.IsDecimalEngine(Standard)`). |
| 2023 | `CompileTimeArithmetic.SystemDecimal` — .NET `System.Decimal` (96-bit, 28–29 digits, ties to even; **not** decimal128) | E.2 6) makes the mode implementor-defined; the documented choice is CONFORMANCE.md DOC-A.1-29 (kept after a GnuCOBOL survey). |

The edition edge is written once, in the runtime's behaviour register (`DialectBehavior.CompileTimeArithmeticImplementorDefined`,
VCR row 12). COBOL-85 has no compile-time arithmetic expression; an expression reached there only after its
introduction gate refused it takes the pre-2023 mode. **Values** travel in the mode-independent carrier
`CtNumeric`/`CobolDec` (exact for every valid literal and for every value either mode yields), so `CtValue`,
`CtNumber` and the consumers never see which mode produced a value; the System.Decimal mode enters a carrier value
through the same `decimal` parse a literal always used.

### 5.2 Diagnostics — code-preserving, per-consumer citations

`ICtDiagnostics.Report(CtDiagCode code, string message)` preserves the diagnostic CODE (a bare `Action<string>`
sink would drop it): the binder maps each `CtDiagCode` to its `DiagnosticCatalog` descriptor (COBOLNET0895 GR14a,
the `ConstantEntryRule` messages) → byte-identical binder codes; the frontend maps to **COBOLNET1619**
(directive-expression violation). `CtOperandVocabulary` supplies the per-consumer noun + governing citation so no
message mis-cites: the evaluator's shared text cites only the shared §7.3.6.2 SR1a/b/c and §8.8.2; the operand-
source clause ("previously defined numeric **constant-name**, §13.10.3 SR2/GR1" for the binder; "previously
defined numeric **compilation variable**, §7.3.11.4 GR1" for the frontend) is injected. The binder's rejection
text may differ by that one clause from today (CONSTANT goldens updated same commit); the code is unchanged.

### 5.3 Numeric-literal normalization

The one chokepoint is `CobolNet.Common.NumericLiteral.Normalize` (Frontend, §3) — §12.3.7 GR14a. The binder
passes its real `DecimalPointIsComma` and routes the issue to COBOLNET0895; the **frontend passes `false`** —
directives are processed before SPECIAL-NAMES is bound (§7.2), so a directive numeric literal is always
dot-decimal (a stated, spec-grounded limitation).

## 6. Boolean semantics (§7.3.7 → §8.8.2) — complete, via ONE shared precedence resolver

A compile-time boolean value is a **bit string** — an immutable `BitString` with **value equality**
(`IEquatable`, length-sensitive) so SR2 redefinition and cce `=`/`<>` compare correctly. Operands (§7.3.7.2 SR1):
boolean literals `B"1010"` (decoded via `CobolLiteral`), grouped sub-expressions, and previously-defined boolean
compilation-variable substitutions. **No figurative operands** — §7.3.7.2 SR1 admits only boolean literals, and
§7.3.3 SR10 bars the figurative `ZERO`/`ALL "literal"` the runtime §8.8.2 admits — so `BitString` carries NO
positionless (figurative) case (a figurative boolean operand is a COBOLNET1619 formation error at the leaf, §0).

**Precedence is implemented correctly, including the context-inherited shift precedence (rule 7b), in ONE shared
mechanism.** A CFG cannot express context-dependent precedence, so `BooleanExpressionResolver.Resolve<T>`
(Frontend) flattens a `booleanExpression` into its lexical operand/operator sequence and precedence-climbs:

* Binary precedence `B-AND`(3) > `B-XOR`(2) > `B-OR`(1); `B-NOT` is the unary factor level (tightest, rule 7b
  1st); parentheses recurse as a fresh level (rule 7a); equal precedence left-to-right (rule 7c).
* A **shift** takes the precedence of the OPERATION immediately preceding it in the sequence, or `B-AND` if none
  (rule 7b tail). So `A B-AND B B-SHIFT-L 2` → `(A B-AND B) B-SHIFT-L 2`; `A B-OR B B-SHIFT-L 2` →
  `(A B-OR B) B-SHIFT-L 2`; `A B-SHIFT-L 2 B-AND C` → `(A B-SHIFT-L 2) B-AND C`. **Negation is an operation in
  that ladder (rule 7b 1st)**, so a `B-NOT` operand makes the following shift take negation's precedence:
  `A B-AND B-NOT B B-SHIFT-R 1` → `A B-AND ((B-NOT B) B-SHIFT-R 1)` (kb/Work PB1370 — the resolver tracked only
  binary operators and shifted `(A B-AND B-NOT B)`). Pinned by `BooleanExpressionFormationTests` and
  `conformance/2023/boolean_expression_formation` (both lanes).
* `Resolve<T>` is **generic over the combine operations** (leaf / not / binary / shift callbacks), so the SAME
  grouping serves the compile-time evaluator (`T = BitString`, folds) and the runtime `COMPUTE` Format-2 boolean
  binder (`T = BoundBoolExpr`, builds). This is the singular fix.

**This removes the prior `COBOLNET1569` reject** in `ConditionBinder` (`ShiftMixedWithBinary`), which refused the
legal mixed shift-with-binary form and told the user to parenthesize — a conformance gap that rejected valid
source. `ConditionBinder`'s tier-walk (`BindBoolExpr/Xor/And/Shift`) is refactored onto `Resolve<T>`; the mixed
form is now accepted and evaluated per rule 7b. Existing COBOLNET1569 tests flip from reject to accept-and-verify.

`BitString`'s fold mirrors the runtime `CobolBool` kernel EXACTLY (the proven §8.8.2 implementation; the algorithm
— not the code — is shared: it was written before the Frontend → Runtime reference of §3 existed). Operator/operand semantics
(§8.8.2):

* **`B-NOT`** — complement, length preserved.
* **Binary `B-AND`/`B-OR`/`B-XOR`** (rules 9/10) — bit-by-bit from the left; unequal length ⇒ shorter
  right-extended with boolean zeros; result length = the larger operand; zero-length ⇒ zero-length (rule 9
  NOTE 2).
* **Shift `-L/-R/-LC/-RC`** (rule 8) — the second operand shall be an **integer operand** (rule 5). Its SHAPE is
  Table 4's: after a shift operator only a single identifier or literal may appear, so a compound (`1 + 1`),
  parenthesized (`(1)`) or separately-signed (`- 1`) count is refused by the SHARED formation screen
  (`ArithmeticFormationRules.ShiftCountNotSoleOperand`, which the runtime `ExpressionFormationPass` runs too —
  COBOLNET1719 there, COBOLNET1619 here). The single operand is then held to §5.5 2) a)'s INTEGER LITERAL by FORM
  (`NumericLiteral.IsIntegerLiteralForm` — "contains no decimal point", §8.3.3.3.2): a literal as written, or a
  numeric compilation variable's literal (§7.3.11.4 GR1), so `1.0` and a variable holding `1.5` are refused
  rather than truncated (kb/Work PB1413). Rule 8 specifies a single shift, repeated `count` times when `count` is greater than 1; a
  `count==0` is identity (the shift repeated zero times leaves the operand unchanged). §8.8.2 assigns no meaning to
  a **negative** repetition count, so — a directive value must be determinate, never a silently wrong value — a
  negative count is rejected loudly (COBOLNET1619). Logical (zero-fill) vs circular (wrap); result length = first
  operand; `count ≥ length` degenerates correctly.
* **Rules 4/5 ALL-adjacency are moot for the compile-time fold** — §7.3.3 SR10 rejects any figurative operand at
  the leaf UPSTREAM, so no `ALL literal` operand ever reaches a binary/shift combine. (The runtime `ConditionBinder`
  keeps its COBOLNET1511 rule-4/5 checks — it DOES admit the §8.8.2 figuratives; only the frontend fold does not.)

The **`boolExprAhead()` predicate** (`CobolParserCoreBase`) was completed to also detect the four shift operators
`B-SHIFT-L/R/LC/RC` (never legal user words), so a shift-only boolean expression (`A B-SHIFT-L 2`) is recognized —
both in the directive fragment (`compileTimeOperand`) and, as a latent-gap fix, in the main-parse `primaryCondition`.

## 7. The constant-conditional-expression (§7.3.8) — evaluated over the ANTLR tree

`EvaluateCce(constantConditionalExpression)` walks the cce tree:

* **`cceOr`/`cceXor`/`cceAnd`/`cceNot`** — logical combination (§8.8.4.9, precedence NOT > AND > XOR > OR per
  §8.8.4.11.3); every operand evaluated **unconditionally** (a formation error in any operand is always reportable
  per §7.3.8, regardless of branch truth). Test-pinned. §7.3.8.2 SR1 d) makes the operand "A complex condition as
  specified in 8.8.4.9", so these tiers are the runtime condition tiers' connectives over a compile-time leaf:
  ANTLR has no parameterized rules, so they are a second spelling, and `ConditionTierConnectiveDriftTests` requires
  each cce tier's connective shape (operand rule names masked) to equal its runtime tier's. The copy had drifted —
  no XOR tier and a self-recursive NOT, so `>>IF 1 = 2 XOR 1 = 1` was COBOLNET1619 and `>>IF NOT NOT 1 = 1`
  compiled (kb/Work PB1371). The XOR connective's 2023 introduction gate is `LogicalOperatorGate`, asked per
  fragment by `EvaluateCceText` (the `BooleanOperatorGate` precedent), so below 2023 it is COBOLNET0900. No
  abbreviated relation: SR1 d) — "Abbreviated combined relation conditions shall not be specified".
* **`definedCondition`** — `IS [NOT] DEFINED` per §7.3.8.4.4.
* **Relation** (§8.8.4.2 / §7.3.8.2 SR1a) — evaluate both operands to `CtValue`s: **SR1a.1** reject a
  category mismatch; **SR1a.2** for non-numeric operands only `=`/`<>` are valid; **comparison** via
  `CtValue.RelationalEquals` (NOT the SR2-redefinition `Equals`): numeric by value; **boolean per §8.8.4.2.8 —
  the shorter operand RIGHT-zero-extended (so `B"1" = B"10"` is TRUE)**; alphanumeric/national per §7.3.8.3 GR2 —
  binary character value, LENGTH-sensitive (unequal ⇒ not equal), no collating. (§7.3.8.3 GR2's length-sensitivity
  is explicitly for operands "not numeric or **boolean**"; a boolean relation right-extends — the same equality
  the EVALUATE Format-1 GR4a match uses.)
* **Bare boolean condition** (§8.8.4.3) — enforce SR1 (length 1, else reject) + GR1 (true iff the single bit is
  `1`); leading `NOT` per GR2. The bit-string→truth bridge.

## 8. EVALUATE directive rules (§7.3.13)

`>>EVALUATE` Format 1/2 run in the line state machine; operands via the shared evaluator; formation rules
enforced: **SR11** all subjects/objects same category; **SR12** THROUGH ⇒ every subject/object numeric; **GR4**
selection — without THRU `subject = object`, with THRU the inclusive numeric range `[object, object3]` (GR4b);
Format 2 evaluates each WHEN's cce (§7.3.8). Single-numeric-literal reclassification (GR2) is automatic (§5).

## 8a. DISPLAY directive (§7.3.12; kb/Work PB807, PB1538)

The directive's operand list is a fragment of the SAME grammar (`displayDirectiveFragment` in `CobolExpressions.g4`):
`displayDirectiveOperand+ displayUponPhrase? EOF`, each operand a `compileTimeOperand` — so §7.3.12.3 SR2/SR3 ("formed in
accordance with 7.3.6 / 7.3.7") and §7.3.3 SR10 are the evaluator's, not a third copy — or `PARAMETER` + a word
(`parameterPhraseAhead()` in `CobolParserCoreBase`: PARAMETER is no lexer token, so it is told apart by its text). `ConditionalCompilationProcessor.Display.cs` evaluates the operands in order (GR4), looks a PARAMETER up through the
ONE environment lookup the DEFINE PARAMETER phrase uses (`ParameterText`, DOC-A.1-49 / DOC-A.1-55), resolves the UPON words,
and **transfers** (GR1): `DiagnosticBag.Transfer(stream, line)`, one line per destination — the images of the operands joined
by one space (DOC-A.1-53) — which `CompilerDriver.Result.CompileOutput` hands to the CLI, which writes each line to the compiler
stream it names, before the diagnostics. UPON (GR5, GR6; DOC-A.1-54): no phrase is as if UPON LISTING; LISTING and the
output device-names CONSOLE and SYSOUT are the compiler's standard output, SYSERR its standard error; a device-name is a row of
the ONE `ImplementorNames` table (which therefore lives in `Cobol.Net.Editions`, beside the directive catalog, where the
front end and the binder both read it); §5.2.6.4's choice indicators — the device group and LISTING each at most once, in any
order — are checked once per phrase (COBOLNET2698). A PARAMETER value that the environment does not supply makes the whole
directive transfer nothing (GR3). A violation reports and transfers nothing; a directive in an omitted branch does not run.
Presence of an operand is the catalog's (`directiveOperand.operandRequired`, COBOLNET1911), content is the stage's.

## 9. Compilation-variable value model

```csharp
enum CtCategory { Numeric, Alphanumeric, National, Boolean }
sealed record CtValue(CtCategory Category, CobolDec Number, string Text, BitString? Bits);   // Number: the CtNumeric carrier
```

Member-wise record equality is **replaced by a hand-written `Equals`** dispatching on `Category` (Numeric →
`Number` by VALUE — `CobolDec.Compare`, with `CtNumeric.ValueHash` for the hash, never the carrier's member-wise
`(Sig, Exp)` — so `AS 1` / `AS 01` / `AS 1.0` are the same value and SR2 does not fire on spelling;
Alnum/National → `Text`; Boolean → `Bits` value-equality). `AS PARAMETER` (GR4, landed) and the SR2/COBOLNET1618
redefinition check (landed) use this model.

## 10. Legacy-oracle safety — ONE mechanism, behavior-preserving

`ConditionalCompilationProcessor.Process` has two callers: greenfield `Frontend.cs` and the legacy differential
oracle `Compilation.cs:345` (frozen until G8). Both route through the SAME shared evaluator — the hand-rolled
`Tokenize`/`CondParser`/`Value`/`Relate` engine is **deleted** (the singular-pattern rule: one mechanism, the best
one). The design's earlier "greenfield-only path" posture is superseded by reality: the legacy caller IS exercised
with directives (`tests/CobolSharp.Tests.Integration/SpecFixTests.cs` CC1–CE3 run `>>DEFINE`/`>>IF`/`>>EVALUATE`
end-to-end through `Compilation.cs`). The shared evaluator REPRODUCES the old single-token-operand behavior
(single literal, defined-condition, relation, THROUGH range, compound cce) exactly, so those tests stay green;
the rewrite only ADDS correct multi-token evaluation (the closed defect) and loud COBOLNET1619 rejects. The
`directive_expressions` 2002 conformance golden passes BOTH the greenfield `CorpusRunnerTests` and the legacy
`ConformanceTests` (its cce directives fold to a DISPLAY-only surviving program both pipelines compile
identically), so no `GreenfieldOnly` exclusion is needed. **Gate:** the full legacy guard (`scripts/guard.sh`)
`=== ALL GREEN ===` before commit (not guard-fast — the shared `.g4` changed). One behavior note: the deleted
tokenizer accepted `_` in a compilation-variable name and ANTLR does not, so a test using an underscore
name was written hyphenated. ⛔ **THE LEXER IS THE ONE THAT IS WRONG, AND THE OLD WORDING HERE CALLED IT
CORRECT while citing a clause that does not exist (§8.3.1.2 — kb/Work PB159/PB290).** §8.3.2.1 reads
"Each character of a COBOL word that is not a special character word shall be selected from the set of
basic letters, basic digits, extended letters, and the basic special characters hyphen and underscore",
so an underscore-bearing user-defined word is legal from COBOL-2002 on; `CobolLexer.g4`'s `NAME_BODY`
admits no underscore in any alternative. Tracked as a REJECTS-LEGAL-SOURCE defect, not a doc note.

## 11. Test plan (ships in the change set)

* **Unit — `BooleanExpressionResolver`**: rule-7b groupings (`A B-AND B B-SHIFT-L 2`, `A B-OR B B-SHIFT-L 2`,
  `A B-SHIFT-L 2 B-AND C`, isolated shift, consecutive shifts) via both the BitString and BoundBoolExpr
  instantiations.
* **Unit — shared evaluator** (`CompileTimeBooleanCceTests`): arithmetic (`2+3*4`, `(2+3)*4-6/2`, unary sign,
  div-by-0, `**` reject, non-literal reject, GR5 `0.25` no-truncation, GR3 truncation); boolean
  (`B-AND`/`B-OR`/`B-XOR` bit results, unequal-length extension, `B-NOT`, shift logical + circular L/R,
  `count==0`/`count≥length`, fractional/negative-count reject, rule-7b context-inherited shift precedence,
  §7.3.3 SR10 figurative/float reject, non-literal reject); cce (numeric/alnum length-sensitive/boolean relations,
  AND/OR/NOT, grouping, defined-condition, name substitution of any category, SR1a.1/SR1a.2 rejects, bare-boolean
  SR1, formation-error-in-short-circuited-branch).
* **Unit — frontend directive** (extend `ConditionalCompilationDefineTests`): `>>DEFINE X AS 1 + 2` ⇒ 3;
  `>>EVALUATE 1 + 1` selects `WHEN 2`; `>>IF A + 1 = B`; boolean DEFINE + `>>IF`; `>>IF NAME = "ABC"`;
  `A IS NOT = 1`; `((A = 1))`; category-mismatch reject; THRU-non-numeric reject; loud COBOLNET1619; existing
  PARAMETER/OFF/OVERRIDE/SR2-1618 still green.
* **Runtime COMPUTE-F2** — the mixed shift-with-binary forms now accepted and evaluated per rule 7b (was
  COBOLNET1569); goldens for the grouping.
* **Conformance** — a 2002 `>>DEFINE`/`>>IF`/`>>EVALUATE` program (arithmetic + boolean operands), `.out` golden.
* **Regression** — CONSTANT-entry goldens (`constant_entry.cob`) for the operand-source-clause wording;
  characterization; full legacy guard green.
