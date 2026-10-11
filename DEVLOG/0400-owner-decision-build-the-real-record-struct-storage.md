## Entry 400 — Owner decision: build the REAL record-`struct` storage substrate (the ADR §9 literal intent); a staged design + an ADR correction (the dead `RecordLayoutBuilder`)

A design investigation for the first Stage-3 typed flip (3 agents) surfaced a load-bearing problem I verified
against the code: **the ADR's nominal home for typed values — "COBOL records → .NET `record struct`" via
`RecordLayoutBuilder` (§1.1/§9) — is dead code.** `IrLoadField`/`IrStoreField` (the only way to access a
record-struct field) have **zero producers**; `RecordLayoutBuilder.Build` (called from `Binder.BuildRecordTypes`)
emits an `IrRecordType` into `module.Types` that nothing consumes, with one live side-effect (`DataSymbol.
ElementSize`). `docs/ARCHITECTURE_ASSESSMENT.md` already flags it (item 17: "excise the dead `RecordLayoutBuilder`/
`IrRecordType`/`IrLoadField`/`IrStoreField`"). The *live* storage is `ProgramState`'s three `byte[]` areas addressed
by `StorageLayoutComputer`; the *live* layout pass is **not** `RecordLayoutBuilder`.

So the first typed flip forces an architectural decision the ADR left non-functional: **where do typed values
actually live?** The design workflow's only single-commit path was **dual-write / shadow fields** (a typed field
kept in lockstep with the canonical byte image) — which is exactly the transitional hack `feedback_no_transitional_
hacks` forbids, and has no place in a decades-maintained compiler. I declined to take it autonomously and surfaced
the decision to the owner (the project rule is to prompt for genuine design decisions). **Owner chose: build the
REAL record-`struct` storage substrate (the ADR §9 literal intent)** — over both the parallel-static-field shortcut
and the dual-write hack.

**The reconciling insight that makes Option B tractable (not a rip-out):** byte-backed items (file records,
REDEFINES/RENAMES islands, LINKAGE, EXTERNAL/GLOBAL, edited, and every not-yet-flipped item) **stay** in the
`ProgramState` byte areas — the permanent safety floor (ADR §1.6). Only items the (now complete) classifier marks
**Typed** move into native fields of an emitted `record struct`. The two meet only at the §2.5 `IDataSlot`
chokepoint, where any typed slot can materialize a transient byte window (`CobolString`/numeric codec) so the
fully-tested byte engine implements any op a typed fast-path doesn't yet cover — **no shadow copy, no drift, no
dual-write.** The substrate therefore grows by flipping one rule at a time with the guard green throughout.

Deliverables this entry (design only — no storage code yet, per the best-practices bar for a foundational change):
- **`docs/RECORD_STRUCT_STORAGE_DESIGN.md`** — the concrete, staged, guard-green engineering roadmap: target
  representation (record struct per 01/77; typed fields; inline byte-island members; OCCURS → `CobolTable<T>`),
  the `IrDataSlot` sum type addressing fork (byte items never re-addressed), rebuilding the dead `RecordLayoutBuilder`
  as the *real* producer (preserving the `ElementSize` side-effect), ADR §1.7 initialization, and the S0→S4+ stage
  plan with a kill-switch (`EnableTypedFields`, default OFF) that reverts to byte-identical at every step.
- **ADR §9 corrected** (`feedback_update_adr_on_design_corrections`): the layout row now records that
  `RecordLayoutBuilder` is dead / `StorageLayoutComputer` is live, the owner's Option-B decision, and the pointer to
  the new design doc.

The staged design passed the project's adversarial-review gate (3-lens / 4-agent: staging-green feasibility /
addressing & aliasing & byte-floor / dead-code-rebuild & init) — **verdict GO**, with **3 confirmed minor
clarifications** (of 26 raw findings; the other 23 refuted as "that's the implementation, not a design gap" or
duplicates) folded into the doc in this commit: (1) **de-hedge the staging** — S1 (wire the classifier, gated OFF)
is standalone and reachable, but the IR/resolver scaffolding + the `RecordLayoutBuilder` rebuild must land *with*
the first flip (S3), not as separate gated-OFF stages, else `TypedFieldSlot`/the emitted struct would be dead code
(no `[CompilerScaffold]` escape hatch); (2) **`ElementSize` sole-writer** — the rebuild deletes
`RecordLayoutBuilder`'s `ElementSize` writes (it and `StorageLayoutComputer` currently both write it, feeding
different consumers by temporal order — a real latent trap), leaving `StorageLayoutComputer` the sole writer with a
`Debug.Assert` equality guard; (3) **numeric oracle named as a hard gate** before the first numeric flip. Guard
unchanged (docs only): **1184 unit / 481 integration / 364 NIST.** NEXT: implement S1, then the S3 first flip.

