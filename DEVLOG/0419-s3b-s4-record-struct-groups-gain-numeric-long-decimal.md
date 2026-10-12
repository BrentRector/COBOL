## Entry 419 — S3b/S4: record-struct groups gain numeric (`long`/`decimal`) members

The S3b group→`record struct` flip was character-only; real records mix text and numbers, so it now accepts a group
whose direct children are any typed-flippable elementary — character, unsigned-integer, OR signed/scaled — and emits
each member as the matching CLR type (`string` / `long` / `decimal`). The hard work was already done: a member is
just an `IrTypedFieldLocation` with `InstanceName` set, and every numeric cell (materialize / DISPLAY / MOVE /
COMPARE / arithmetic) is already InstanceName-aware, so no cell changes were needed — only the Binder's S3b builder
(create the right `IrTypedFieldDef` per child) and CilEmitter's struct emission. Factored the field CLR-type choice
and init-value emission into shared `TypedFieldClrType` / `EmitTypedFieldInitValue` helpers used by both the flat-field
and struct-member paths (no divergence).

Verified with `--typed-fields` that `_TS_CUSTOMER` emits as a record struct with `String CUST-NAME` / `Int64
CUST-QTY` / `Decimal CUST-BAL`, and a DISPLAY/ADD/MOVE/COMPARE probe over all three member kinds is byte-for-byte
identical to the byte path. New flip test `MixedGroup_FlipsToRecordStruct_WithCharLongAndDecimalMembers_ByteIdentical`
— 16 flip tests, all flag-on≡flag-off. (A group still flips only when every numeric child has a VALUE — same
defined-init rule as standalone numerics.) Guard **ALL GREEN: 1196 unit / 497 integration / 364 NIST**. NEXT: the
OCCURS → `CobolTable<T>` table substrate, then nested groups, then Stage-4 pointers/OO.

