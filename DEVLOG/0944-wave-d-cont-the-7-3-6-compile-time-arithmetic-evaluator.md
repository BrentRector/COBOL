## Entry 944 — 2026-07-21 10:59 PDT — Wave D (cont.): the §7.3.6 compile-time arithmetic evaluator extracted into the ONE shared CompileTimeExpressionEvaluator; CONSTANT binder rewired onto it (ledger C2)

The C2 fix is ONE shared compile-time expression evaluator for both the frontend conditional-
compilation stage and the CONSTANT-entry binder. This lands its ARITHMETIC core and moves the binder
onto it — the "lift the battery-tested code, don't duplicate" step. The boolean fold + the frontend
fragment-parse rewire follow. Design SSOT: docs/rearchitecture/DESIGN-compile-time-expressions.md.

- New `CompileTimeExpressionEvaluator` (Cobol.Net.Frontend/Expressions): `EvaluateArithmeticOperand`
  applies §7.3.11.4 GR5 (a single numeric literal stays a literal — AS 0.25 keeps 0.25) and §7.3.6.3
  GR3 (an expression's FINAL result truncated to integer) at the PUBLIC boundary; the raw-decimal
  recursion (`EvalArith`, lifted verbatim in logic from the binder's `EvalConstExpr`) stays private so
  intermediates are un-truncated (GR1). Consumer specifics injected: numeric-name resolution, a
  CODE-preserving `ICtDiagnostics` sink (so the binder keeps its exact codes), a `CtOperandVocabulary`
  (per-consumer operand wording + citation — the binder says "constant-name"/§13.10.3, the frontend
  will say "compilation variable"/§7.3.6), and the DECIMAL-POINT IS COMMA mode. Returns a
  `CtNumber{WasSingleLiteral, Value, Text}`.
- `DataBinder.BindConstantArithmetic` rewired; `EvalConstExpr`/`ParseConstLiteral`/`SoleNumericLiteral`
  deleted. Byte-identical ConstantDef + diagnostics, three deliberate deltas: the SR2 overflow message
  "decimal128"→".NET decimal"; a fractional constant-NAME under DECIMAL-POINT IS COMMA no longer emits a
  spurious COBOLNET0895 (the old path re-normalized the already-normalized stored text — a latent-bug
  fix, same value); and the sole-literal boundary now range-validates (below).

**Adversarial verification (2-agent Workflow) caught a real regression BEFORE commit — the ultracode
pattern working.** The extraction's new sole-literal branch added a `decimal.TryParse` whose
`NumberStyles` omitted `AllowExponent`, so `01 C CONSTANT AS 1.5E3` (a FLOATLIT — a valid numericLiteral)
failed to parse and returned null SILENTLY: the constant was dropped with no diagnostic (the old binder
accepted it), and later references failed with a misleading "unknown constant-name". Both a divergence
and a silent-failure defect. Fixed: the sole-literal boundary parses with AllowExponent (a GR5 literal
keeps its own class — a floating-point literal is valid there, unlike a §7.3.6.2 SR1b arithmetic-
EXPRESSION operand which stays fixed-point), and a literal beyond the decimal range is rejected LOUDLY
(SR2), never a silent null. Two unit tests pin both.

Verified: 13 CompileTimeExpressionEvaluatorTests; the constant_entry.cob golden byte-matches through the
shared path; negative constant goldens unchanged. Guards: legacy `guard.sh` `=== ALL GREEN ===`;
greenfield Unit 392 / Characterization 33 (byte-exact) / Conformance 3784 green.

