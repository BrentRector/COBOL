## Entry 735 — 2026-07-09 23:05 PDT — PHASE-03 Step 14g.3: the OO class/interface definition + OCCURS DYNAMIC gates → parse-arm

The third 14g commit: three more edition-introduction gates move from the binder into `VersionConformancePass`, all to
the parse-arm (recognition) — the recon (`wf_0d98d218-087`) classified all three as recognition fits, and the 14g.2
review reinforced that a data/definition gate belongs on the parse-arm unless it keys on a genuinely-resolved fact.

- **CLASS-ID / INTERFACE-ID definition** (`ClassDefinition2002` / `InterfaceDefinition2002`, ISO §11.2/§11.3 //
  §11.5/§11.6) — moved from `OoClassTable.Build` (which fired for every parse node BEFORE its dedup `continue`) to
  `ParseArm.VisitClassDefinition` / `VisitInterfaceDefinition`. A bound-arm home would UNDER-count: a duplicate/colliding
  definition is dropped with a `continue` before it enters the built table, but the gate must still name the edition. The
  name-embedding where-strings are reproduced exactly (`className(0)` / `interfaceName(0)` — the definition's own name, not
  an INHERITS base). `Build` is called once per compile (`CSharpEmitter.Call.cs:247`), so the count is byte-identical.
- **OCCURS DYNAMIC** (`OccursDynamic2014`, ISO §13.18.38 Format 4; data-model D9) — moved from `OdoBindOccursSpec` to
  `ParseArm.VisitOccursClause` (fires on the DYNAMIC alternative of the shared `occursClause` rule, matching
  `occ.DYNAMIC()`). A per-DataItem bound-arm walk would OVER-count: a TYPEDEF template's dynamic table is cloned into each
  `TYPE` reference, but the gate must fire once per SOURCE clause (the clones are DataItem objects, not parse nodes). Reuses
  the 14g.2 `InConditionOrRenamesEntry` guard (the binder skipped level-66/88 before `OdoBindOccursSpec` ran).

Byte-neutral: characterization 32 byte-exact (`char_neg_occurs_dyn85` unchanged — the content-sorted diag surface).
The three gates now reference from EXACTLY the pass + the `GateId` registry mapping; zero binder references. The `.g4`
edits (CobolParserCore.g4 compilationGroup comment + CobolData.g4 occurs comment) are comment-only → byte-identical regen.

New `OoOccursDynEditionTests` (Conformance, 6 exact-count witnesses): one 0900 per class/interface/OCCURS-DYNAMIC below
its intro (zero at/above); and the load-bearing over-count witness — OCCURS DYNAMIC inside a TYPEDEF referenced twice
gates ONCE (the template's single `occursClause` node; the two clones add none).

Battery: greenfield conformance **3137** (3131 + 6) · unit **223** · characterization **32** · INV-1-strong @ 2023
permissive **349/349** · FULL legacy guard **NIST 353 MATCH / 0 regressions** (legacy unit 1196 · integration 608).
RESUME AT Step 14g.4 (SPECIAL-NAMES-FOR + file SHARING/LOCK-MODE → bound-arm; PD RETURNING/RAISING → parse-arm).

