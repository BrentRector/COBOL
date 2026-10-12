## Entry 422 — S4: OCCURS slice 1 — a fixed char OCCURS table → a typed .NET `string[]` (byte-identical)

Implemented §9 slice 1 atomically (the first *cross-cutting* typed increment — 18 dispatch sites). A fixed `OCCURS n`
over a character element flips to a static `string[]`; element access `ARR(i)` works across MOVE-literal, DISPLAY
(constant **and** variable subscript), COMPARE, and element→element MOVE — all byte-identical.

The crux (per §9.2): an abstract **`IrTypedLocation`** base (common `Width`/`Pic`/`IsDecimalNumeric`) with
`IrTypedFieldLocation` (existing) + a new **`IrTypedElementLocation`** (`ArrayFieldName` + lowered subscript). The
**three value-access primitives** (`EmitTypedLoad`/`EmitTypedStorePrefix`/`EmitTypedStoreSuffix`, now in CilDataEmitter,
shape-aware: flat field / record member / `ldsfld array; index; ldelem|stelem`) are the *only* places that know the
shape; the ~18 numeric/char dispatch sites changed `is IrTypedFieldLocation` → `is IrTypedLocation` and otherwise work
on an element **unchanged** (materialize, COMPARE, arithmetic prologue/epilogue, DISPLAY, MOVE). New `IrTypedArrayDef`
+ `TypedArrays`/`TypedArrayRefs` registries; CilEmitter emits `static T[]` + `newarr`/per-slot fill init; the resolver
turns a subscripted typed-array ref into an `IrTypedElementLocation` (subscript decimal→`Convert.ToInt32`→−1 for
0-based); the Binder flips OCCURS *children* (OCCURS is illegal on 01, so a table is always nested) ahead of the
top-level-only skip.

Safety (§9.3): the classifier `ProcedureScanner` demotes a fixed OCCURS table to byte when it — or a containing group
— is referenced as a **whole** operand (a `T[]` has no whole-table byte home); a belt-and-suspenders loud guard in
`ResolveWholeItem` refuses any whole-operand over a still-flipped table/group rather than read stale bytes. Verified:
`_TA_WS-T` emits as `String[]`; a subscripted probe is byte-identical, and a whole-group probe correctly stays byte.
Two flip tests (`CharOccursTable_…`, `WholeTableOperand_DemotesTableToByte_…`) — 19 flip tests, all flag-on≡flag-off.
Guard **ALL GREEN: 1196 unit / 500 integration / 364 NIST**. NEXT (slice 2): numeric OCCURS elements (`long[]`/
`decimal[]` — mostly falls out of the generalized primitives), then PERFORM VARYING/SEARCH over a typed table.

