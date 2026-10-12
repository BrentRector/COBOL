## Entry 802 — 2026-07-11 17:10 PDT — P7 Step 9h1: arithmetic becomes `Verbs/ArithmeticEmitter` (mechanical split; 74 = 34 + 40)

**What (9h first half).** The arithmetic block (~330 lines: EmitInPlace/EmitGiving/EmitDivide/
EmitDivideRemainder/EmitCompute/EmitComputeBoolean + the shared `EmitArith` ON-SIZE-ERROR/EC-SIZE wrapper,
`StoreArith` store funnel, `RcvFor`, `Snapshot`, and the `Narrow`/`BwzFlag`/`ScaleOf` statics) moves to
`Verbs/ArithmeticEmitter.cs` over (ctx, num, ecState, host) — host carries the EC seam
(`EcEnabledSizeNames`/`EcEmitSizeHandling`, widened internal) and the SIZE-ERROR phrase-body dispatch
(`EmitStatementList`). The host keeps THREE service shims (`EmitArith`/`StoreArith`/`RcvFor` → `_arith`) for the
five collaborator callers + the remaining core verbs; the 11 Dispatch arithmetic visits re-point directly; the
`Narrow`/`BwzFlag` statics' references retarget (`CSharpEmitter.` → `ArithmeticEmitter.`) across the four
consuming collaborators. One genuine transcription hazard caught at compile time: the DIVIDE INTO body's LOCAL
`NumX num` collided with the ctor-injected renderer after the mechanical `_num`→`num` rewrite — renamed `q`
(compile-time name only; emitted text unchanged, snapshots prove it). Ratchet re-keyed 74 → 34 + 40 (exact).

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

