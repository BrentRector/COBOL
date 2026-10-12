## Entry 402 — S3 pre-flight: code-grounded implementation checklist for the first character flip (autonomous /loop tick)

An autonomous-loop tick on the record-struct substrate. The next step is **S3 — the first character flip**, a
single atomic Cecil-emission change (the review forbade dead intermediate stages). Rather than force a large
codegen change to completion in a time-boxed tick — where rushed Cecil emission would risk the owner's
commercial-quality bar — this tick read the actual emit seams and locked in a precise, code-grounded S3
implementation checklist (`docs/RECORD_STRUCT_STORAGE_DESIGN.md` §6.1) so the next focused pass executes fast and
clean. Key findings:
- **Type emission is reusable:** `CilEmitter.DefineType` (`CilEmitter.cs:870`) already emits an `IrRecordType` as a
  sealed `SequentialLayout` value-type with `public` fields → flipping `RecordLayoutBuilder.MapToIrType` to return
  `String` for a classifier-Typed `PIC X` field makes it emit a `record struct` of `string` with no other change.
- **The dead `IrLoadField`/`IrStoreField` must NOT be reused** — they are a register-model relic (load a field into
  an `IrValue`) that does not fit COBOL's location-model MOVE/COMPARE; that mismatch is *why* they are dead. S3's
  `IrDataSlot`/`TypedFieldSlot` (location-model) is the right abstraction; the register-model IR is excised in S3.
- **`CilLocationEmitter.ResolveForeignStateField` (`:320`)** iterates *Cecil* `Module.Types` by program-name for
  GLOBAL inheritance — unrelated to the dead IR record types (so they don't interfere).
- **`ElementSize` (§4):** `StorageLayoutComputer` already sets it before the Binder via the same
  `FieldSizeCalculator`, so `RecordLayoutBuilder`'s writes are redundant — but the two passes sum *group* sizes
  independently, so equivalence must be verified per-group before making `StorageLayoutComputer` the sole writer.
- **Safety:** S3 lands gated by `EnableTypedFields` (default OFF) → the whole guard stays byte-identical; a
  flag-ON `tests/conformance/2002/` test drives + verifies the typed path and keeps every new member reachable.

The 7-step checklist (type reuse → static instance → init → location fork → emit cells → ElementSize verify →
flag-gated test) is in design §6.1. Guard unchanged (docs only): **1187 unit / 481 integration / 364 NIST.**
NEXT: execute S3 per §6.1.

