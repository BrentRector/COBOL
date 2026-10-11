## Entry 456 — Phase B1: OO typed-native object data COMPLETE — GROUPS (record struct) + OCCURS tables (array), per-instance

Completes the typed-native object-data story (DEVLOG 453 char, 454 numeric): a class's GROUP object data now flips to
a per-instance .NET `record struct` field and a fixed OCCURS table to a per-instance typed array — alongside the flat
`string`/`long`/`decimal` fields. The `classFlatOnly` scaffolding (453/454, which deferred record/array for classes)
is removed; the byte `State` is now empty on a fully-typed class.

The record-struct + array typed-field emit and access were static-only; made them per-instance on the same
`StateIsInstance` flag the flat path rides:
- `CilEmitter.EmitProgramState`: the record-struct instance field and the OCCURS array field emit per-instance on a
  class (vs static on a program); their `InitializeState` init (struct-member init via `ldarg.0; ldflda`, array
  `newarr`/fill via `ldarg.0; … stfld`/`ldfld`) runs in the instance ctor.
- `CilDataEmitter.EmitInstanceAddressChain` (record-struct member address) and `EmitTypedElementAddress` (array
  element address) route their field load through the `EmitTypedFieldOwner` chokepoint (`ldarg.0; ldflda`/`ldfld`
  on a class, `ldsflda`/`ldsfld` on a program).

**Verified (Mono.Cecil + run):** `oo_object_group` — class REC with a `PERSON` group → `instance _TS_PERSON _TI_PERSON`
and a `SLOT … OCCURS 3` table → `instance Int64[] _TA_SLOT`; two objects R1/R2 prove per-instance independence
(R1 FILLed, R2 stays `ANN`/`0`/`000`; R1 shows `BOB`/`7`/`123`). All OO + conformance + flip tests green; the legacy
corpus is unaffected (every change gates on `StateIsInstance`, false for non-class units). NEXT: subclass own typed
OBJECT data (lift COBOL0113); per-method LINKAGE (multi-method-with-args); FACTORY; then the rest of M2.

