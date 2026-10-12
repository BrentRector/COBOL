## Entry 401 — Record-struct substrate S1: wire the (complete) classifier into the Binder + a permanent soundness-invariant net (runs on the whole corpus, byte-identical)

First implementation stage of the record-struct storage substrate (`docs/RECORD_STRUCT_STORAGE_DESIGN.md` S1,
owner-approved Option B, DEVLOG 400). `Binder.Bind` now runs the **complete** `RecordClassificationPass` (Phases
A+B+C) over every program — `Classify(DataItemsInOrder, categoryOf = GetStorageLocation(s)?.Pic.Category,
procedureStatements = all paragraphs→sentences→statements, layoutOf = GetStorageLocation offsets)` — stores the
result on `LoweringContext.Classification`, and immediately calls a new `RecordClassification.ValidateInvariants()`.

The classification is **not yet consumed by codegen** (the Stage-3 typed flip will consult it in
`LocationResolver`), so this is **byte-identical** — but it earns its keep two ways, satisfying zero-dead-code
without make-work: (1) running the pass on every program **exercises the Phase-B procedure-division walker across
the whole corpus** (364 NIST + 481 integration + the unit programs) — the first time the walker meets every real
bound-tree shape, isolated from the eventual flip; (2) `ValidateInvariants()` is a permanent internal-consistency
net that **consumes** the result in production every compile and fail-fasts (`InvalidOperationException`, the ICE
path) on either of the two invariants the fixpoint guarantees — a typed item's REDEFINES target is typed (byte
propagates up the class) and a typed item's parent is typed (byte propagates down to subordinates). Membership is
checked against the classified set so an item outside the map (reported byte by the conservative `Get` default)
never false-positives.

The whole corpus passed first run — the classifier (reviewed complete at DEVLOG 398) handles every real program
with no crash and no invariant violation, validating Phase B's exhaustive bound-tree walker against production
input. +3 unit tests pin `ValidateInvariants` (sound classification → no throw; typed-child-under-byte-parent and
typed-redefiner-of-byte-target → throw).

Guard **ALL GREEN: 1187 unit / 481 integration / 364 NIST** (+3 unit; byte-identical — no codegen/NIST change).
NEXT: S3 — the first character flip in one commit (introduce `IrDataSlot`/`TypedFieldSlot`/`ByteWindowSlot`/
`FieldShape`, rebuild `RecordLayoutBuilder` as the real producer with `StorageLayoutComputer` the sole `ElementSize`
writer, and flip an all-character `01` record → a `record struct` of `string` fields, gated by `EnableTypedFields`).

