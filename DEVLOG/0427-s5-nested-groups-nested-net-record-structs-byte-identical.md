## Entry 427 — S5: nested groups → nested .NET record structs (byte-identical)

A `01` group containing sub-groups now flips to a **nested** record struct — e.g. `OUTER` → `struct { INNER:
struct { A: string, B: long }, C: string }` — debuggable as `outer.inner.a`. The whole group flips only if every
descendant is flippable (char/numeric leaf or a flippable sub-group); any OCCURS / edited / byte-trigger descendant
keeps the whole group byte. Member-level access only; a whole-group operand is handled by the existing classifier
group-MOVE demotion (a contained OCCURS table still flips independently via S4).

Mechanism: `IrTypedFieldLocation` gained a **`MemberPath`** (the chain of intermediate nested-struct member names
from the static instance to the leaf's parent); `IrTypedFieldDef` gained a **`Nested`** sub-record; `IrTypedRecordDef`
made `InstanceName` nullable (nested sub-structs live inline, no static instance). `CilEmitter.DefineRecordStruct`
and the init helper recurse; the three value-access primitives (CilDataEmitter) resolve flat OR nested members by
**walking `FieldType.Fields`** along the MemberPath (so the `TypedRecords` registry simplified to just the instance
field — no member dictionary). The Binder's S3b branch became the recursive `BuildTypedRecord` local function
(returns null = stays byte if any descendant isn't flippable). No per-op cell changes — load/store/materialize all go
through the path-walking primitives, so DISPLAY / MOVE / arithmetic / COMPARE on nested leaves are byte-identical.

Verified: `_TS_OUTER` emits with an `_TS_OUTER_INNER INNER` member + `String C`; the nested struct holds `String A` +
`Int64 B`; a DISPLAY/MOVE/ADD/COMPARE probe matches the byte path. New flip test
`NestedGroup_FlipsToNestedRecordStruct_AcrossOps_ByteIdentical` — 23 flip tests, all flag-on≡flag-off. Guard **ALL
GREEN: 1196 unit / 506 integration / 364 NIST**. NEXT: Stage-4 pointers/OO, then Stage-5 Roslyn backend.

