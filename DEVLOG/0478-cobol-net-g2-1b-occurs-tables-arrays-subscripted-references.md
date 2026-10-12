## Entry 478 — COBOL.NET G2-1b: OCCURS tables → arrays + subscripted references

The "tables" half of the G2 data-model checkpoint. A fixed `OCCURS n` item is a .NET array; a subscript is a
1-based occurrence number attached to its OCCURS level (COBOLNET_DESIGN §3.2/§3.4) — never byte-offset arithmetic.

- **OCCURS → `T[]` (element-initialized).** `FieldEmitter` emits `OCCURS n` as `new ElementType[] { e, …, e }` (n
  copies of the element's VALUE-or-default — so a `string[]` is never left with null elements). Element type is the
  group's `record struct` or the leaf's PIC type; nested OCCURS-of-group works (`_T_3[] ROW = new _T_3[] { new
  _T_3 { A = 0L, B = new string(' ', 3) }, … }`). `DataItem` gains `ElementType`/`FieldType` (array-aware).
- **Subscripts → `[expr - 1]` at the OCCURS level.** `ReferenceResolver` now interprets the flat SUBSCRIPT-mode
  token group (a faithful reduction of the legacy `ExpressionBinder.SplitSubscriptTokens` — proven over 364 NIST
  tests: depth-0 comma / multi-space splitting, the relative-subscript single-space rule, depth-0 `SUB_COLON` ⇒
  ref-mod). Each segment renders to a C# `long` index expression — integer literal, data-name (numeric item or
  INDEXED BY index-name), or relative `name ± k`. `AccessPath` walks the item's ancestor chain and inserts
  `[idx - 1]` at each OCCURS ancestor, outermost subscript first: `ITM(WS-I)` → `WS_TBL.ITM[WS_I - 1]`,
  `A(1)` → `WS_GRP.ROW[1 - 1].A`. A subscript count that doesn't match the table's OCCURS dimension, or an
  unhandled subscript form, returns null → loud-failure.
- **INDEXED BY** index-names are registered as distinct static `long` fields (`_IX_n`, 1-based, §3.5) and emitted;
  a subscript may name one. (SET of an index + SEARCH are later — without SET the field is just its init value.)
- Reference modification (`(s:l)`) is detected (depth-0 colon) and routed to loud-failure until **G2-1c** (it needs
  new `CobolString.RefMod`/`SpliceInto` runtime — `CobolString` currently has only `Store`/`Compare`).

Verified: full-solution build clean Debug (0/0); Conformance 53 green (6 new OCCURS differential tests:
elementary/group tables, literal/variable/relative subscripts, subscripted arithmetic, VALUE-initialized
elements) + Unit 3 green; legacy oracle untouched. Generated table C# inspected — clean idiomatic array code, output
byte-identical to the legacy. RESUME AT → G2-1c (ref-mod) or G2c (level-88) — then the formal bound tree +
decomposition (G2-2).

DEVLOG 478.

