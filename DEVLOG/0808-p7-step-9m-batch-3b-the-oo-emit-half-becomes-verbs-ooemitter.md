## Entry 808 — 2026-07-11 17:57 PDT — P7 Step 9m/BATCH-3b: the OO EMIT half becomes `Verbs/OoEmitter` over LIVE host accessors; the Oo partial = the bind half only

**What (the census's riskiest coupling, landed).** The OO emit half (class/factory/interface unit emission,
INVOKE typed·instance·universal, SET object-ref, the per-method `__MDispatch` local dispatcher) moves to
`Verbs/OoEmitter.cs`. The load-bearing design point: class-unit emission RE-CREATES the per-unit
context/renderer quadruple MID-RUN, so a collaborator with ctor-captured `ctx` would go stale — `OoEmitter`
reads `Ctx/Num/Cond/Refs` through LIVE host accessors instead, and the three per-unit re-new sites collapse to
the ONE `host.BeginUnit(w, data, refs)` entry (the program-class site now calls it too; the interface-unit site
gains harmless fresh renderers — output-neutral, snapshots prove it). `OoEmitter` is RUN-UNIT scoped (created
beside `NameAllocator` — it must exist before the first class unit, which precedes any program unit).
A transcription hazard caught at compile time: the original `var host = data.Files…` local SHADOWED the new
ctor param — renamed `hostFiles` (compile-time only). `CSharpEmitter.Oo.cs` now holds ONLY the OO BIND half
(the `IOoBindHost` bodies + the `_ooClasses`/`_ooIfaceData` forests, internal for the emit half) — exactly the
P6→P9 seam the host-survival deviation preserves. Ratchet: 17 = 0 + 17 exact.

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

