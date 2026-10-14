## Entry 2002 — 2026-10-13 11:42 PDT — PB1616 - a literal in a compiler-directive operand is screened by the unit's own LiteralScreen

**What the note said, and what the re-probe showed.** PB1616 (filed from the PB1393 report) said a `>>DEFINE` / `>>IF`
literal is parsed by `DirectiveExpressionFragment` from text no tree walk reaches, so `>>DEFINE D AS X"414"` compiles
clean. On a fresh build it did not: PB1441 had added a second copy of the §8.3.3 screen to
`CompileTimeExpressionEvaluator.LiteralRuleViolated`, which reported COBOLNET1619 instead of the unit's
COBOLNET1635 / 0814 / 2630, and only along the path the evaluator evaluated. What still escaped was real: a literal in an
omitted branch (`>>IF 1 = 2 / >>DEFINE D AS X"414"`), a literal in an operand a failed DEFINE left undefined, and the
edition gates of a literal's FORMAT (`>>DEFINE ZL AS ""` at `--std 2002` compiled with rc 0 and no COBOLNET0900, the
sibling PB895/PB1545 added to the note on 2026-10-07).

**The root.** The screen was a Compiler pass over the unit's tree, so the directive lane (Frontend, below the compiler)
could not call it and grew its own partial copy. `LiteralScreenPass` moved to `src/Cobol.Net.Frontend/Parsing/
LiteralScreen.cs` (the `BooleanOperatorGate` precedent: PB1370 did the same for the boolean operators), takes the
layer-neutral `IDiagnosticSink`, and is called by both lanes: `BinderDriver` on the unit's tree, and the
conditional-compilation stage's three fragment funnels (`ParseOperandFragment`, `ParseCceFragment`,
`ParseDisplayFragment`) on each directive fragment's tree. A fragment with an unsound literal comes back null, as a
malformed one does, so the evaluator never decodes a literal that has no value; its private copy is deleted, and its
class doc states the contract. The §8.3.3 rules run in every branch (ISO §7.2.1: directives are syntactically correct in
the initial source text, and the false path is part of it); the format-edition gates run in a compiled branch only, the
posture of the directive's own gate and the boolean-operator gate. `LiteralScreenDriftTests` now pins
`CobolLiteral.SyntaxViolation(` to the one file and `DirectiveExpressionFragment.Parse*` to the funnels' file, so a fourth
parse site cannot skip the screen. The inventory witnesses of twelve rows were re-sited by batch
(`pb1616-literal-screen-moved`); design docs (frontend-grammar, version-conformance-pipeline, compile-time-expressions)
and CONFORMANCE DOC-A.1-9 say the same.

**A finding in passing.** An 8,192-position literal cannot be written in a legal directive at all: a directive is one
line and cannot be continued, so a free-form line holds 255 positions (COBOLNET2653 is the line's) and a fixed-form line
fewer. The length rule is therefore covered by `DirectiveLiteralScreenTests`, which builds the source and reads
COBOLNET0814 out of the errors beside the line's own, not by a checked-in golden.

**Goldens and tests.** `negative/pb1616-define-hex-odd` (COBOLNET1635), `negative/pb1616-omitted-branch-hex-odd`,
`negative/pb1616-define-zero-length-below-2014` (reject-at 85 2002, COBOLNET0900), positive
`2014/pb1616_define_zero_length_literal` (the introducing edition, DEFINE of `""` compared by §7.3.8.3 GR2); the
`w73a-directive-hex-literal-non-hex-digit` golden now fires COBOLNET2630 rather than COBOLNET1619. 28
`DirectiveLiteralScreenTests` cases cover every directive position (DEFINE, IF, EVALUATE subject, WHEN both forms,
DISPLAY, omitted branch) for length, grouping, repertoire and the format gates.

**Rows.** No GAP row closed (the literal-rule rows were already CONFORMS on the unit lane); `closes_rows: []` with the reason.

**Self-review.** Architecture: one body, two lanes, no wrapper (the old pass and the evaluator's copy are gone, not
forwarded). Full code: the funnel's `compiled` flag is the only difference between a compiled and an omitted branch;
with no diagnostic bag the verdict is still the rule's. Performance: one extra walk of a fragment tree per directive.
Duplication: `semgrep verify` raw-code-literal count went DOWN (268 -> 198). A directive that fails its DEFINE still
leaves a later `>>IF` on that name reporting "not a previously-defined variable"; that cascade is older than this change
and was left alone.

**Arch oracle.** Re-recorded at c8668abfeda9, the branch merged with main at 32657cce4 after PB2495 and then PB3109
landed first and re-recorded the baseline as dd3d1d4bb002 (`compare_oracle.py` against it: 5 of 7,799 cases differ, the
same five as against c41f1b82af2c before either landed, which change none of them). Four are ADDED, this change's own goldens: `corpus/2014/pb1616_define_zero_length_literal`
and the three `negative/pb1616-*`. One is DIAGNOSTICS, `negative/w73a-directive-hex-literal-non-hex-digit`, and both parts of it
are intended. At every edition, X"GG" in `>>DEFINE` is now refused by LiteralScreen's COBOLNET2630 (SR5 named), where the
evaluator's COBOLNET1619 refused it before. Every position also moves down one line because the golden's own comment
grew by one line.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261013T034137Z-f154db): Conformance
4,984 of 11,530, Unit 32,615 of 33,463, Characterization 6 of 36 ran in leg 1; the Linux gate is the landing's.

