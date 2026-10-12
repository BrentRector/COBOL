## Entry 813 — 2026-07-11 19:31 PDT — P7 Step 10c: `BinderContext` + `StatementValidation` + the FIRST verb collaborator (`Verbs/InspectBinder`)

**What (the AS-BUILT PLAN's pattern-proving batch, on the census's cleanest file).** Three new homes:
- **`Binding/Procedure/BinderContext`** — the collaborators' shared spine, as-built shape per the plan
  block: `Data`/`Refs` core + derived `Edition` (the ONE sink AND edition surface — no
  IDiagnosticSink/EditionInfo split), `Symbols` (the P6 per-binder table — the sketch's SymbolTableBuilder
  was superseded), `ActiveScope` (recomputed per READ, never captured), `Options`, and the `Validation`
  handle. ONE per binder instance, LAZY on the host (the primary-ctor captures can't feed eager
  initializers).
- **`Binding/Validation/StatementValidation`** — the pure-check catalog with the convention FIXED here:
  every `Check*` reports byte-identical text to the sink and returns the verdict; the verb binder owns all
  control flow. Seeded with the five INSPECT SR checks (SR5 counter-numeric, SR7 one-char CHARACTERS
  replacement, SR6/SR9 equal-size — called on the no-expansion path, preserving the original else-if
  shape — and SR2 operand-usage).
- **`Binding/Procedure/Verbs/InspectBinder`** — the five methods moved verbatim over `ctx` +
  `StatementBinder host` edges (NationalLiteralOperand/BooleanLiteralOperand — the shared operand spine,
  made internal; they land on ExpressionBinder at 10q). The SR6/SR9 figurative-expansion REWRITE stays in
  the binder (bind logic, not validation); the 0845 BACKWARD gate moved VERBATIM (Exec Step E folds it).
  The old partial became `Binding/Bound/BoundInspect.cs` — records/enums ONLY (the generated visitor and
  the emitter key on the namespace; the Tally/Replace ordinals are runtime ABI).
- Host wiring: lazy `Ctx`/`Inspect` accessors + the `:189` dispatch arm → `Inspect.Bind(ins)`.

**Two hazards caught in-flight (transparency).** (a) The new `CobolNet.Binding.Validation` namespace
SHADOWS `CobolNet.Validation` for relative `Validation.` references inside `CobolNet.Binding.*` —
BindPipeline's pass-manifest row broke at compile time; fully qualified with a comment. (b) My first
transcription of `InspectFigurativeChar` emitted the LOW-VALUE char as a literal NUL BYTE in the source
(and HIGH-VALUE as a raw 'ÿ') — the EXACT ConditionRenderer ripgrep-invisibility hazard DEVLOG 788
recorded, and a semantic near-miss; restored the backslash-u0000 / backslash-u00ff ESCAPE spellings before any test ran.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance
(diagnostic text asserted by the corpus — the lifted checks emit at the identical traversal points) —
verdicts read as separate actions.

