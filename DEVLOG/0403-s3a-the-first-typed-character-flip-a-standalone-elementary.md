## Entry 403 — S3a: the FIRST typed character flip — a standalone elementary PIC X item → a native .NET `string` field (byte-identical, gated)

The data-model migration's inflection point: the first item is now stored as a **typed-native .NET field**, not a
byte window. Scope (S3a — the narrowest genuine flip): a **standalone elementary** alphanumeric/national/alphabetic
WORKING-STORAGE item the classifier marks typed (no OCCURS, no figurative/ALL VALUE, no triggers) → a static
`string` field. (A standalone elementary item has no enclosing record, so it maps to a bare native field per ADR
§1.1 — this is the elementary case of Option B; the `01`-group → `record struct` grouping is the next widening,
S3b.) Gated by `CompilationOptions.EnableTypedFields` (**default OFF** → the entire existing corpus stays
byte-identical); a dedicated flag-ON test drives the typed path, so every new cell is reachable (zero-dead-code).

The end-to-end path: `Binder.CollectTypedFields` selects the flippable items (gated by the flag + the classifier),
records each on `IrModule.TypedFieldDefs` + `LoweringContext.TypedFieldRefs`; `LocationResolver.ResolveWholeItem`
returns a new `IrTypedFieldLocation` (carrying the item's `PicDescriptor` so `GetPic` and the existing move/display
routing still work); `CilEmitter.EmitProgramState` emits one static `string` field per flipped item and
COBOL-correctly initializes it in `InitializeState` (padded VALUE / spaces — never `default(null)`, ADR §1.7); the
typed cells in `CilDataEmitter` handle the two ops the subset needs — MOVE-literal store (`CobolString.Store` →
`stsfld`) and DISPLAY (the `string` field → `Console`). Any other op on a typed field hits a **loud
`NotSupportedException`** in `CilLocationEmitter.EmitLocationArgs` (no silent miscompile) until its typed cell or
the materialize fallback lands.

**The byte-identity discipline immediately earned its keep.** The first flag-ON/flag-OFF differential test caught a
real divergence: the byte `DISPLAY` path (`PicRuntime.GetDisplayString`, alphanumeric arm) **trims trailing
spaces** (`.TrimEnd()`), but my typed `DISPLAY` initially emitted the full padded string — so `PIC X(5)` holding
`"AB   "` displayed `"AB   "` typed vs `"AB"` byte. Fixed by replicating the exact `.TrimEnd()` in the typed cell;
both paths now produce identical output. This is precisely the migration's invariant (the typed flip must be
observationally byte-identical), and the paired flag-ON/flag-OFF test (`TypedFieldFlipTests`) pins it.

Process note (owner directive, [[feedback_continue_dont_wait]]): implemented continuously in one pass rather than
deferring to a loop tick. Guard **ALL GREEN: 1187 unit / 483 integration (+2 flip tests) / 364 NIST** — the corpus
is byte-identical (flag OFF) and the typed path is proven byte-identical (flag ON). NEXT: S3b — widen the flip to
an all-character `01` group → a real `record struct` of `string` fields (the idiomatic record-struct form), reusing
`CilEmitter.DefineType`; then field↔field MOVE / COMPARE typed cells + the materialize fallback; then numeric
(hard-gated on the `CobolNum` oracle).

