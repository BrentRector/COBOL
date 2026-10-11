## Entry 786 — 2026-07-11 14:23 PDT — P7 Step 1: the `ICodeGenBackend` seam materialized (`RoslynBackend : ICodeGenBackend`) — pure indirection

**EXEC STEP D opens** (PHASE-07 Steps 1–5, 7–12; Step 6 was Exec A). A 9-agent premise-audit workflow
(`wf_8ace7f29-a1d`) is auditing every remaining step against the current tree in parallel (the doc's anchors
predate P5/P6); Steps 1–2 were fully scoped by direct reading and executed without waiting.

**What (Step 1, DESIGN-codegen-backend §2.2).** `CodeGen/ICodeGenBackend.cs` NEW: `BackendId {Roslyn, Cil}`,
`BackendOptions(OutputPath, AssemblyName, Edition, EmitPdb, WriteSource)`, `BackendArtifact(Success, Diagnostics,
GeneratedSourcePath, AssemblyPath)`, `ICodeGenBackend { Id; Emit(BoundCompilation, BackendOptions) }`,
`BackendFactory`. `RoslynBackend` static → `internal sealed class RoslynBackend : ICodeGenBackend`; its `Emit`
wraps the pre-seam pipeline VERBATIM (EmitBound → `.g.cs` write [WriteSource] → the existing `Compile`).
`CompilerDriver` Phase 2b+3 → ONE `BackendFactory.For(BackendId.Roslyn, emitter).Emit(bound, options)` call.
**Deviations (recorded here + the phase ledger):** (1) the factory takes the BIND-HOST `CSharpEmitter` — until P9
relocates the OO bind bodies (the documented `IOoBindHost` P6→P9 seam), `EmitBound` must run on the SAME instance
that hosted `Bind`; the parameter disappears at P9. (2) The seam is INTERNAL, not public — `BoundCompilation` is
internal by P6 design and the only consumer is the in-assembly driver (P16's `CilBackend` is in-assembly too).
(3) `EmitPdb` is declared per the design contract but RESERVED — honoring it would violate Step 1's
no-behavior-change bar (today's backend emits no symbols); a later phase wires it.

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 32 characterization byte-exact (pure
indirection — zero snapshot drift).

