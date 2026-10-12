## Entry 769 — 2026-07-11 10:22 PDT — P6 Step 4: the VersionConformancePass is the manifest's NAMED TERMINAL pass; driver Phase 2 is literally Bind → gate → CheckOnly → Emit

The SOLE edition gate is now a declared pipeline citizen. `BindPipeline.GroupTail` gains the terminal entry
`VersionConformancePass` [StorageComputed → the new `PassPhase.EditionConformanceChecked`], whose body runs P3's
two-arm pass over the `GroupBindContext` (which now carries the parse `Tree` for the parse-arm — on the GROUP context,
never a bound node; the BoundTree invariant stands) reporting through the session's edition sink.
`VersionConformancePass.Run` takes the context (its ONE caller is the manifest); `CompilerDriver` no longer references
the pass at all — Phase 2 is `Bind` (manifest incl. terminal gate) → `if (diagnostics) BindError` → `if (CheckOnly)
Success` → `EmitBound`. Exit criteria #4/#6 now hold structurally: the CheckOnly verdict includes the pass's
diagnostics because the pass runs INSIDE Bind, and grep shows the only `ConstructRegistry.Check` outside the pass is
`StatementBinder.Udf.cs:63` — the UDF-invocation recognition gate the P3 pipeline design names as the one principled
exception (syntactically identical to an intrinsic call; only the repository-resolved name set separates them).

Tests: `RealPipeline_FullChain_IsAMonotoneDag` now asserts the terminal pass IS named `VersionConformancePass`
producing `EditionConformanceChecked`; `CheckOnlyCompileTests` gains (a) an edition violation under CheckOnly returns
`BindError` CARRYING the COBOLNET09xx band code (verdict includes pass diagnostics, not just a bool) and (b) a
binds-clean/emits-LOUD program (ACCEPT into COMP-1 — the closest thing to an emit-side failure this compiler produces
by design) returns Success with `GeneratedCsPath` null + zero `.g.cs`/`.dll` files — the observable "EmitBound is not
invoked" contract. Battery: conformance 3158 identical · unit 272 (+2) · characterization 32/32 byte-identical · the
INV-1 version-continuity sweep all-OK (it drives check-batch → CheckOnly, validating the verdict rebase end-to-end) ·
legacy guard green.

