## Entry 718 — 2026-07-09 15:22 PDT — PHASE-03 Step 14a: the bind/emit SPLIT (byte-identical) — codegen no longer runs on an errored tree

Step 14 (the `VersionConformancePass` skeleton) is the largest architectural step of the phase, so — per a decision-complete
recon (workflow `wf_edbcd62a-d8a`: 7 parallel cluster-mappers over all ~91 `ConstructRegistry.Check` sites + the bind/emit
boundary + the verdict surfaces → a max-effort synthesis) — it lands as an ordered sequence of green, byte-identical
sub-commits **14a–14f**, not one un-bisectable big-bang. This is **14a: the structural bind/emit split, decoupled from the pass.**

- **The fused orchestrator is split.** `CSharpEmitter.CallEmitRunUnit(tree, edition, turnEvents) → string` (which BOUND the
  run unit AND rendered C# in one call — the design's anti-pattern #4, "gating discovered during emission") becomes
  `CallBindRunUnit(tree, edition, turnEvents) → BoundRunUnit` (bind only) + `CallEmitRunUnit(BoundRunUnit) → string` (render
  only). The public entry `CSharpEmitter.Emit(tree,…)` splits into `Bind(…)` + `Emit(BoundRunUnit)` on the SAME instance
  (its bind-populated fields `_turnState`/`_ooClasses`/`_ecActive` stay live).
- **New `BoundRunUnit` record** = the walkable bound group the pass will gate in 14b+: the parse-tree root, the program units
  + class units (each carrying its `.Bound`/`.Data`), and the OO symbol roster. Carrying the parse root on the GROUP record
  does **NOT** add a parse context to any bound NODE — `BoundTree.cs`'s "no raw parse context" invariant stands. (This is the
  documented resolution of the design's §2.4/`.Syntax` tension: the pass will identify constructs by bound-node TYPE +
  semantic ATTRIBUTE + a retained parse-arm over the tree root, never a per-node `.Syntax` back-ref — within the design's own
  §2.2/§6 provisions; the flat "`.Syntax` back-reference" prose is refined at phase close, Step 14f.)
- `CallUnit`/`CallBridge`/`OoClassUnit` promoted `private`→`internal` so the pass (a sibling `Validation` class, same assembly)
  can walk them.
- **Driver** now runs `parse → EditionValidator → (fail-fast) → Bind → HasErrors gate → CheckOnly? return : Emit → Roslyn`.
  Verified that gating after `Bind` is verdict-equivalent: the render half receives **no** `EditionContext` and produces zero
  diagnostics — every edition diagnostic producer (`FunctionPrototype2002`, `0885/0886/0887`, `1507/1508/1513`, `1520`) fires
  in a bind method (`CallMakeUnit`/`CallBuildUserFunctionTable`/`OoBindClassData`) reached from the bind sequence. So codegen
  **never runs on an errored tree** (rearch exit criterion 9 — delivered here as a pure refactor, decoupled from the pass),
  and `CheckOnly` genuinely skips emit (a speedup: it previously built the C# string then discarded it).
- **Byte-identity proven:** characterization **32/32**, greenfield conformance **3114/3114**, unit **227**, FULL legacy guard
  NIST **353 MATCH** (0 regressions — 14a is greenfield-only, no shared frontend/grammar touch).
- NEXT — **14b:** stand up `VersionConformancePass` (the bound-tree arm) + relocate the statement-level Checks (recon
  clusters R2/R3), adding the erased-at-bind semantic flags (`BoundCallProgram.UsedOverflowSpelling`, `BoundAccept.HasEndTerminator`, …).

