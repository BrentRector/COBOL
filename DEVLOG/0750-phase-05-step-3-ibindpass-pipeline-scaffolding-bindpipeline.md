## Entry 750 — 2026-07-10 19:34 PDT — PHASE 05 Step 3: `IBindPass` pipeline scaffolding + `BindPipeline.ValidateDag` startup assert (no reorder)

Landed PHASE-05 Step 3 (DESIGN-data-model §2.5) — the DECLARED bind pass pipeline, a **no-op wrapper** over the
existing `DataBinder.BindResolve` passes: ZERO reorder, ZERO behavior change. This structurally kills the
"implicit pass-ordering" smell (the passes were ordered only by call sequence + comments, with no declared
Requires/Produces contract).

**What landed:**
- `Binding/Passes/IBindPass.cs` — the `PassPhase` enum (11 ordered milestones: `None → TypesExpanded → UsageResolved
  → SignResolved → RedefinesClassified → StrongTypeChecked → OccursResolved → FilesResolved → ProcedureBound →
  UsageCollected → StorageComputed`), the `IBindPass` interface (`Name`/`Requires`/`Produces`/`Run(DataBinder)`), and
  the canonical `internal record BindPass(Name, Requires, Produces, Body)` impl.
- `Binding/Passes/BindPipeline.cs` — `Build(program)` returns the ONE ordered pass list (the SSOT for the pass
  order): the 16 resolve passes in the EXACT pre-change order, then the 3 middle-end tail markers
  (`ProcedureBinding`/`UsageCollectionPass`/`StorageFormPass`, present for DAG completeness; their `Run` throws — they
  are driven from the emitter, which has the bound tree, not from the resolve loop). `ValidateDag(passes)` asserts a
  monotone phase chain (each pass's `Requires` ≤ the running high-water mark of `Produces`; no `Produces` regression),
  throwing `InvalidOperationException` on the first violation.
- `DataBinder.BindResolve` now: `var pipeline = BindPipeline.Build(program); if(!_dagValidated){ValidateDag; set;}
  foreach pass where Produces ≤ LastResolvePhase(FilesResolved): pass.Run(this)`. One-time static `_dagValidated`
  guard = the "startup assert" (the shape is compile-time-fixed, so once suffices; a benign race merely re-validates).
- Extracted the inline FILE whole-group loop → `internal void MarkFileRecordImageLeaves()` (the last resolve pass),
  behavior-identical (same `WholeGroupReferenced.Add` + `MarkImageLeaves` recursion + numeric-DISPLAY predicate).
- Widened the 12 resolve methods `private → internal` (design-sanctioned, "make them internal as needed") so
  `BindPipeline`'s lambdas reach them: `ExpandTypes`/`ResolveIndexItems`/`InheritUsageClauses`/`InheritSignClauses`/
  `ResolveRedefines`/`ClassifyRedefinesClasses`/`CheckStrongTypeDeclarations`/`ResolveFiles`/`GateNationalRecords`
  (DataBinder.cs), `OdoResolve`/`DynamicResolve` (OdoModel.cs), `ResolveReports` (DataBinder.Reports.cs).
  (`OoRouteMethodRedefinesBackings`/`CallBindExternalAndGlobal`/`PtrBindBasedAndAddressables` were already internal.)
- `tests/Cobol.Net.Tests.Unit/BindPipelineTests.cs` (×4): the REAL `BindPipeline.Build(null!)` is a valid monotone DAG
  reaching `StorageComputed`; a hand-built canonical chain validates; a reorder-before-prereq throws (message names the
  offending pass); a `Produces`-regression throws. `Build(null!)` is safe — `ValidateDag` reads metadata only, never
  runs the program-capturing lambdas.

**Design faithfulness:** the two program-context passes close over `program`; every other pass runs against the
`Run(DataBinder)` argument (`BindResolve` passes `this`). `Build` drops the unused `data` param (the closures use the
Run arg). The SSOT for the pass order lives in `BindPipeline` (§2.5), as designed.

**Battery:** greenfield conformance **3157** · unit **258** (+4 BindPipelineTests) · characterization **32 byte-exact**
(the key behavior-neutrality proof — the pass wrapping produced ZERO emitted-C# diff). Full legacy guard: NIST 353
MATCH (greenfield-only change — the legacy compiler + shared frontend/grammar are untouched). Adversarial 4-lens
find→verify review run (wf_93b6aecc-739). Exit criterion #5 (the pass DAG asserted at startup) is now satisfied.

**RESUME AT: PHASE 05 Step 4** — `RecordLayout` as a PARALLEL width authority + the corpus width-equivalence assert.

