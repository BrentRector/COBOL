## Entry 405 — S3b: a flipped `01` group → a real .NET `record struct` of `string` members (Option B realized, byte-identical)

The owner-chosen Option B is now concrete: an all-character `01` group is stored as a genuine .NET `record struct`
(a sealed `SequentialLayout` value type) of `string` members + one static instance on the program type, with
member access lowered to `ldsflda instance; ldfld/stfld member`. `MOVE "ACME" TO CUST-NAME`, `MOVE CUST-CITY TO
CUST-NAME`, and `DISPLAY CUST-NAME` for the members of `01 CUSTOMER` now read/write the typed struct — debuggable
as `_TI_CUSTOMER.CUST_NAME == "OHIO"` rather than a byte window. Byte-identical to the byte path and gated by
`EnableTypedFields` (default OFF), exactly as S3a.

Scope (S3b): a **flat** all-character `01` group — every direct child an elementary classifier-typed
alphanumeric/national/alphabetic item (no nested groups / OCCURS / figurative-VALUE yet) — that the classifier
marks typed (so it is never used as a whole-group operand, else trigger 4/15 byte-backs it). `Binder.
CollectTypedFields` grows a group pass that emits an `IrTypedRecordDef` (struct type + instance + members) and
records each child on `LoweringContext.TypedFieldRefs` with its instance name; `CilEmitter.EmitProgramState` emits
the struct `TypeDefinition`, the static instance, and member init in `InitializeState`; `IrTypedFieldLocation` gains
an optional `InstanceName` (null = a flat S3a static field, set = a struct member). The three typed cells in
`CilDataEmitter` (MOVE-literal, field↔field MOVE, DISPLAY) were refactored onto shared
`EmitTypedStorePrefix`/`EmitTypedLoad`/`EmitTypedStoreSuffix`/`EmitCobolStringStore` helpers that branch flat vs
struct-member (a struct store pushes the instance address first, then the value, then `stfld`).

Reused `CilEmitter.DefineType`'s value-type emission shape (sealed/SequentialLayout/`ValueType` with public fields)
for the struct, per the §6.1 pre-flight finding. Test (`TypedFieldFlipTests`): `01 CUSTOMER` with `CUST-NAME PIC
X(6) VALUE "ACME"` + `CUST-CITY PIC X(4)` — init/MOVE-literal/field-MOVE/DISPLAY across members, asserted
byte-identical to the flag-off byte path. Done in one continuous pass ([[feedback_continue_dont_wait]]). Guard
**ALL GREEN: 1187 unit / 485 integration / 364 NIST**. NEXT: the typed↔byte materialize fallback (§2.5) so a typed
field works in ANY op (removes the `EmitLocationArgs` throw), then typed COMPARE, then numeric (hard-gated on the
`CobolNum` oracle), then OCCURS / nested groups.

