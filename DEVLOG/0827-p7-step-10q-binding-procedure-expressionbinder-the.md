## Entry 827 — 2026-07-11 21:16 PDT — P7 Step 10q: `Binding/Procedure/ExpressionBinder` — the expression-spine flip

**What (the AS-BUILT PLAN's 10q batch).** The shared operand/expression/receiving spine left the god class as
ONE `ExpressionBinder(ctx, host)` — per the plan at `Binding/Procedure/` (a sibling of
BinderContext/PhraseBlocks, deliberately NOT `Verbs/`: every verb binder consumes it). Three families:
OPERAND (LiteralOperand · NationalLiteralOperand §8.3.3.5 · BooleanLiteralOperand §8.3.3.4 ·
FigurativeOperand §8.3.3.6.4 [static] · FieldOperand with its keyword-omitted-function / LINAGE-COUNTER
§8.4.3.14 / RW-counter §8.4.3.15 / index-name §13.18.38 interception ladder · RefExpr/RefFailure/LinageFileOf
private), EXPRESSION (BindExpr · BindChain · BindPower · BindPrimary · the breadth-first BindOperandExpr ·
NumLiteral · `CheckLiteral` with its DECIMAL-POINT normalization + edition digit-cap window AS-IS — the
sanctioned binder-side pattern), RECEIVING (`ResolveReceiving` THE chokepoint with the LINE-COUNTER SR3 /
PAGE-COUNTER staged / CAPACITY-register rejections · ResolveTargets · Receivers×3 · RoundingOf §14.7.4).
The host keeps 15 forwarders (the 10o deviation-(a) pattern — one-shot flip at 10t); `Rw` bumped internal
for the host.Rw.CounterExpr edge; DataRefs/Children generic tree statics STAY on StatementBinder (not
expression-specific). Two stale-path comments fixed in the moved text (the ReportWriter-partial references).
BindSizeError×2/BuildSizeError stay HOST phrase members (entry 826's scoping note). The core is now 405
lines (was ~700 pre-10p). One build break: `CobolRounding` needed `using CobolNet.Runtime;`. No behavior
change intended.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

