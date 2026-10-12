## Entry 672 — 2026-07-07 20:58 PDT — Rearchitecture PHASE 02 step 4-first — `EditionContext` = `EditionInfo` + `IDiagnosticSink` adapter

Landed the doc's step 4 BEFORE step 3 (see the P2.2 sequencing note): the registry move needs `EditionContext` to
already be sink-ready, since once `ConstructRegistry.Check` drops into Editions it can no longer take an `EditionContext`.
Rewrote `EditionContext` (still `CobolNet.Binding`, still the same sealed primary-ctor class) as a thin ADAPTER over what
it always really was — an immutable `EditionInfo` value **plus** a stringly-typed diagnostic sink. It now
`: IDiagnosticSink`; `DialectLevel`/`Permissive`/`MaxDigits` DELEGATE to the single `Edition` (an `EditionInfo`
constructed with the non-validating record ctor to preserve the historical any-int tolerance — new code uses the
validating `EditionInfo.Of`), which ends the triple-sourcing of `DialectLevel` (P5). Added `Edition`, `Sink => this`, and
the `Report(in EditionDiagnostic)` bridge that renders a structured diagnostic back onto the legacy `Error`/`Warning`
string channels with byte-identical text — the mechanism by which the layer-neutral `ConstructRegistry.Check` (P2.4) will
produce exactly the diagnostics the old direct `edition.Error/Removed/Warning` calls did. All 10 public members
unchanged, so the ~475 member-access sites across 36 files compile untouched (adapter guarantee; retirement is P7).

Compiler-only, byte-stable: conformance **2036** · unit **222** (both unchanged); legacy guard invariant (legacy
references Frontend, not Compiler). Build 0/0.

