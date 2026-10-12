## Entry 092 — 2026-03-16: INITIALIZE Statement — Default, Group, REPLACING

Implemented INITIALIZE with category-based defaults and REPLACING clause.

**Lowering strategy**: No new IR instructions. INITIALIZE lowers to a sequence of existing MOVEs:
`IrPicMoveLiteralNumeric(loc, 0)` for numeric fields, `IrMoveFigurative(loc, Space)` for
alphanumeric. This reuses the full PIC-aware MOVE pipeline including sign handling and editing.

**Group traversal**: Recursive descent through DataSymbol.Children. REDEFINES items are skipped
(they share storage with the base item, which gets initialized).

**REPLACING**: Grammar extended with `initializeReplacingPhrase` containing
`initializeReplacingItem` alternatives for ALPHANUMERIC/NUMERIC/EDITED DATA BY value. New lexer
tokens: ALPHANUMERIC, EDITED. Category classification maps CobolCategory → InitializeCategory
for replacement matching.

4 tests: basic reset (unskipped), group with mixed children, REDEFINES, category REPLACING.

---

