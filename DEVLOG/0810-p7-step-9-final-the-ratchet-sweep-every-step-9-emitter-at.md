## Entry 810 — 2026-07-11 18:47 PDT — P7 Step 9-final: the RATCHET SWEEP — every Step-9 emitter at ZERO bare fragments; the whitelist = the four pre-Step-11/12 renderers only

**What (the STATUS line's "ratchet sweep of the remaining pinned fragments", executed).** Two halves:
- **Routing.** Every GENUINE emitted-text fragment in the Step-9 outputs now renders through `RuntimeApi`:
  ProgramEmitter's 14 (new façade members `ArgAdaptNum/Text/Present/StoreReturn`, `FileInit`, `FileCloseAll`,
  `PtrDeref` + existing `FileClose`/`FileOpenModeOf`/`StrStore`) · OoEmitter's 17 (new `ObjRequireNonNull`,
  `ObjNormalizeMethodName`, `FileMintInstanceKey` + existing Num/Str members) · CallEmitter's 11 emitted
  `CobolPassMode.*` (the new `PassModeText` anchor — the `RoundingText` pattern) · the DataDivision five's 24
  (new `StrRepeat`; the boolean VALUE store now renders via the existing `StrStoreBoolean` — byte-identical
  text) · SequentialIo's inline-`nameof` RegisterSharing → the existing `FileRegisterSharing`. The three
  COMPILE-TIME `CobolEdit` calls (MaskScale ×3, the §13.18.63 GR6 edited-image bake) become TYPED façade
  passthroughs (`RuntimeApi.MaskScale`/`EditCompose`) — one anchored definition, per the test's
  "façade passthrough" option.
- **The recorded counting-rule refinement** (the §5 empty-whitelist criterion's other half): (a) comment
  lines are stripped before matching — a comment can never mis-emit, and doc-`cref`s are compile-validated
  (kills the EmitCore/PhysicalModel/CallEmitter doc-comment pins); (b) `CobolRounding.`/`CobolPassMode.` are
  excluded — in CodeGen they are always TYPED enum arguments whose emitted forms route exclusively through
  the `RoundingText`/`PassModeText` anchors, so a runtime rename still breaks RuntimeApi.cs at compile time.
- **Result:** 20 baseline entries → 4. Every Step-9 emitter, the spine, EmitterState, ReceiverContext, and
  EmitCore are at ZERO. The remaining pins are exactly the four pre-Step-11/12 renderers
  (IntrinsicRenderer 47 · NumericRenderer 17 · ConditionRenderer 17 · OperandText 7 under the refined rule) —
  they restructure at Step 11 (the `Roslyn/` relocation with structural Place) and Step 12 (the
  static-channel deletion), where their fragments route; recorded in the test's doc comment.

**Verify.** Sln Debug clean; 33 characterization — the 32 snapshots BYTE-EXACT through the routing (the façade
emits the identical text by construction) + the rewritten ratchet green · 281 unit · 3166 conformance —
verdicts read as separate actions before the commit.

