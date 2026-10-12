## Entry 413 — S4: numeric sender-materialize + native numeric COMPARE / IS NUMERIC (byte-identical)

Implemented the cell 412 deferred. `CilLocationEmitter.EmitLocationArgsMaterializingTyped` now handles a typed
**numeric** read operand: it allocates a scratch `byte[width]`, encodes the field's `long` into it via the SAME
codec the byte field uses (`PicRuntime.EncodeNumeric`), and pushes `(scratch, 0, width)` for the byte op. Because
`decode∘encode` is an exact round-trip for an in-range value, the bytes the downstream op sees are identical to the
byte path's — byte-identical by construction. Factored the field-value load (flat `ldsfld` / record-struct
`ldsflda`+`ldfld`) into `EmitTypedFieldValueLoad`, shared by the string and numeric arms.

Wired the three numeric-COMPARE IR cells (`EmitPicCompare`, `EmitPicCompareLiteral`, `EmitPicCompareAccumulator`)
through `EmitLocationArgsWithPicMaterializingTyped` so a typed numeric operand materializes; a byte operand still
falls through to the identical byte path. The class-condition subject path (`EmitUserClassCondition`, 409) already
used the materializing variant, so **IS NUMERIC** on a typed numeric field now works too (encode → digit window →
`IsNumericClass`). This unblocks `IF WS-NUM = 42 / > 40 / < 99`, `IF WS-A = WS-B`, `IF WS-A NOT = WS-C`, and
`IF WS-NUM IS NUMERIC` — all flag-ON, all byte-identical.

New flip test `NumericField_Comparisons_ByteIdentical` (=, >, <, field-vs-field, NOT =, IS NUMERIC) — 11 flip tests
now, all flag-on≡flag-off. Guard **ALL GREEN: 1196 unit / 491 integration / 364 NIST**. NEXT: numeric **arithmetic**
(ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE via `CobolNum`) — the large byte-identity-critical increment — then
signed/scaled (`decimal`) numeric variants.

