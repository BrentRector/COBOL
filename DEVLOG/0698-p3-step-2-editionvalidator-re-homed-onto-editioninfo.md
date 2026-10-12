## Entry 698 — 2026-07-08 12:51 PDT — P3 Step 2 — EditionValidator re-homed onto EditionInfo + IDiagnosticSink (off the EditionContext adapter)

**PHASE 03 step 2** (exit criterion 6). The validator now runs natively on the P2 framework:
- ctor `EditionValidator(EditionInfo edition, IDiagnosticSink sink)` — fields `_edition` (EditionInfo) + `_sink`;
  the `EditionContext` field is gone, and `using CobolNet.Binding` was dropped (no `EditionContext` on the
  edition-gating path).
- The ~24 `ConstructRegistry.Check(_edition.Edition, _edition, …)` removal-gate calls → `Check(_edition, _sink, …)`.
- The two direct writes in `VisitCobolWord` — the DEBUG-register-facility 0899 and the §8.9 reserved-word 0901 —
  now emit via `_sink.Report(new EditionDiagnostic(...))`, the reserved-word severity from
  `EditionSeverityPolicy.For(ConstructAvailability.Removed, _edition)` (exactly what `EditionContext.Removed` did).
  `_edition.DialectLevel` → `_edition.Year`.
- `CompilerDriver.Compile` hook → `new EditionValidator(edition.Edition, edition)` — passing the immutable
  `EditionInfo` + the `EditionContext` collector AS its `IDiagnosticSink` (it implements the interface; P2 kept it
  as the collector per owner decision Q5). Both validator and binder still accumulate into the one collector, so
  the fail-fast (`edition.HasErrors`) and diagnostic ordering are unchanged.
- Stale doc-comment `EditionContext` crefs rewritten to the `IDiagnosticSink`/`EditionSeverityPolicy` reality
  (so the Release warnings-as-errors build has no CS1574/CS8019).

**Byte-identical by construction:** `EditionContext.Report(EditionDiagnostic)` reconstructs the exact
`"error {code}: {msg}"` / `"warning …"` strings the former `Error`/`Removed` produced; the descriptor path emits the
same `COBOLNET0899`/`0901` codes. **Battery:** Debug + Release (warnings-as-errors) builds clean; greenfield
conformance 2055 · unit 227 · characterization 32 GREEN (incl. the `EditionGateDiagnosticTests` /
`ReservedWordPositionConformanceTests` / `MoveEditionDifferentialTests` byte-identical tripwires); FULL legacy guard
NIST **353 MATCH** (0 regressions), legacy unit 1196, integration 607 GREEN. RESUME AT step 3 (enrich
`constructs.json` — `expectDiagnostic`/`variant`).

