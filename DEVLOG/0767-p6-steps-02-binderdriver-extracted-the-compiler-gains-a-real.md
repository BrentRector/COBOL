## Entry 767 — 2026-07-11 09:53 PDT — P6 Steps 0–2: BinderDriver extracted — the compiler gains a REAL Binder phase returning an immutable BoundCompilation; the bound model moves to Binding/Model

**EXEC STEP B (P6, the Real Binder) begins.** Step 0 preflight: baseline recorded green — 3158 conformance · 269 unit ·
32 characterization · legacy guard NIST **353 MATCH, 0 regressions** (+ legacy unit 1196 / integration 609). Step 1
(IBindPass manifest + DAG) was found ALREADY DELIVERED by PHASE-05 Step 3 in a lighter form (`Binding/Passes/`
`BindPass` records over the `PassPhase` enum + `ValidateDag` + `BindPipelineTests`) — recorded as an as-built deviation
in the PHASE-06 STATUS block rather than churning 18 wrapper classes into existence (the Requires/Produces CONTRACT is
what the phase needs; the doc itself says so).

**Step 2 (this commit) — the phase's structural core.** The second hidden pipeline (`CSharpEmitter.CallBindRunUnit`,
the ~12-step bind orchestration buried in the codegen class) is now `Binding/BinderDriver.Bind`, returning an immutable
`BoundCompilation`. What moved where:

- **`Binding/Model/BoundUnit.cs`** — the emitter's private `CallUnit` + `CallBridge`, relocated + renamed. The BINDER
  owns the bound model; the emitter consumes it.
- **`Binding/Model/OoClassUnit.cs`** — the class-unit DATA type relocated (the OO bind/emit LOGIC stays on the emitter
  until P9). Deviation from the phase doc (which said "keep OoClassUnit where it is"): keeping the TYPE in CodeGen
  would have made `BoundCompilation` (Binding) reference a CodeGen type — the exact layering inversion P6 exists to
  kill; `VersionConformancePass.Run` no longer takes a `CSharpEmitter.*` type.
- **`Binding/Model/BoundCompilation.cs`** — replaces `BoundRunUnit`: Tree (conformance parse-arm) + Units + ClassUnits
  + OoClasses + InterfaceData + Turn + EcActive + AnyFiles.
- **`Binding/BinderDriver.cs`** — TurnState.Build → CollectUnits/MakeUnit/Reparent → OO binds (via the seam) →
  two-phase per-unit bind (BindUnitData/BuildUserFunctionTable/BindUnitProcedure — all moved verbatim) → temp re-sync →
  UsageCollectionPass → MarkStoreAsImage (relocated from the emitter; Step 3 folds it into StorageFormPass) → OO
  harmonize → StorageFormPass.Compute → EC gate → **file-connector registry-key qualification + AnyFiles, MOVED from
  the emit half** (they mutate `FileModel` — a bind-phase model fact; the conformance pass reads no file names, so the
  relocation is observation-equivalent).
- **`Binding/IOoBindHost.cs` + `BindSession`** — the documented P6→P9 seam: the OO orchestration bodies stay on
  `CSharpEmitter` partials (they only mutate binder state) and the driver reaches them through the interface; the
  session carries Turn/OoClasses/Edition + the SHARED uid-band allocator (the emitter's `_callUidBand` and the
  driver's per-unit binds draw from ONE sequence, exactly as the fused pipeline did). Deviation from the doc's
  `OoBindCallbacks` delegate record: an interface is the same seam with one named contract — recorded in PHASE-06
  STATUS.
- **`CSharpEmitter`** — `Bind` is now a thin shim over `BinderDriver` (this instance as host); `EmitBound(comp)` is the
  emit-only entry: it RESTORES `_turnState`/`_ooClasses`/`_ecActive` from the immutable record and renders — the
  qualification loops + anyFiles computation are gone from the emit half. `MarkStoreAsImage` is gone from CodeGen
  (grep-clean — exit criterion #2's first half, ahead of Step 3's pass-ification).

Neutrality proof: the emitted-C# characterization snapshots are **byte-identical** (32/32, no re-baseline), conformance
3158 + unit 269 identical to the Step-0 baseline, legacy guard re-run green (the diff is entirely greenfield-side).
Tooling note (the standing PATH-A rule): zero new traversals were written — every moved walk is the verbatim original,
and the Step-3 merge will consolidate the three data-forest recursions onto the canonical enumerators.

