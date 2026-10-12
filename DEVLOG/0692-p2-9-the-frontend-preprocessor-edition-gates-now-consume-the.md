## Entry 692 — 2026-07-08 11:01 PDT — P2.9 — the frontend preprocessor edition gates now consume the ONE EditionSeverityPolicy

**PHASE 02 step 9.** The two remaining copies of the strict/permissive severity decision — the fixed-form
continuation gates in `ReferenceFormatProcessor.EditionGates` (col-7 hyphen obsolete-2023 → COBOLNET0903;
word-continuation removed-2023 → COBOLNET0902) and the COPY REPLACING non-pseudo-text gate in
`CopyProcessor.OnNonPseudoTextOperand` (removed-2023 → COBOLNET0902) — inlined
`if (permissive) ReportWarning else ReportError`. That is the SAME decision the binder, the validator, and
`ConstructRegistry.Check` make through `EditionSeverityPolicy.For`. Per `feedback_singular_pattern` (one
canonical decision, never a parallel copy) these three sites now ask the ONE policy:
`EditionSeverityPolicy.For(ConstructAvailability.Removed, EditionInfo.Of(dialectLevel, permissive))` (and
`.Obsolete` for the col-7 gate), then dispatch `EditionSeverity.Error/Warning` → `ReportError/ReportWarning`
via a small `EditionGates.Emit` helper (inline in CopyProcessor's single site).

**Byte-neutral by construction:** `For(Removed, permissive:false)` = Error, `For(Removed, permissive:true)`
= Warning — exactly the old inline branch; `For(Obsolete, …)` = Warning always — exactly the old hard-coded
col-7 warning. The emit-vs-not decision (`dialectLevel < 2023 ⇒ return`) stays at the site: only the
column-aware pass can see the indicator, and "is this construct present at all" is a genuinely local
concern; ONLY the severity is delegated. Codes and message text are character-for-character unchanged.
`EditionInfo.Of` is only ever reached with `dialectLevel == 2023` (the `< 2023` early-return precedes it),
so its year-validation never trips.

This removes P2's problem P2 (severity policy reimplemented in the frontend twice more) — one policy now
governs the binder + validator + both preprocessor gates.

**Battery:** greenfield conformance 2055 · unit 224 · characterization 32 GREEN; FULL legacy guard NIST
**353 MATCH** (0 regressions), legacy unit 1196, integration 607 GREEN. (The NIST guard runs at edition 85,
below these ≥2023 gates, so it is structurally unaffected — the guard run is the shared-frontend-change
discipline from `feedback_guard_fast_not_ci_complete`, not a behavior probe.)

Touched: `src/Cobol.Net.Frontend/Preprocessor/ReferenceFormatProcessor.cs`,
`src/Cobol.Net.Frontend/Preprocessor/CopyProcessor.cs`. RESUME AT step 10 (DiagnosticDescriptors registry +
COBOLNET0899 split).

