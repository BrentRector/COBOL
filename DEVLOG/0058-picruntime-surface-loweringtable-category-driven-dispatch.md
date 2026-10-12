## Entry 058 — 2026-03-15: PicRuntime Surface + LoweringTable — Category-Driven Dispatch

### The Change

Restructured `PicRuntime` into a category-organized public surface matching the
compatibility matrices 1:1:

**MOVE helpers** (28 methods): Every legal (source, target) category pair has a
dedicated method. Numeric→Numeric, Numeric→NumericEdited, Numeric→Alphanumeric,
NumericEdited→Alphanumeric, Alphanumeric→Alphanumeric, plus all National variants.
Implementations delegate to core helpers (DecodeNumeric/EncodeNumeric for numeric,
Array.Copy+space-fill for alphanumeric).

**Arithmetic** (6 methods): AddNumeric, SubtractNumeric (new), MultiplyNumeric,
DivideNumeric (new), plus literal variants for Add and Multiply.

**Comparison** (3 families): CompareNumeric (decode+compare decimals),
CompareAlphanumeric (new — byte-by-byte with space padding),
CompareNational (new — delegates to alphanumeric for now).

**Status structs**: `MoveStatus` (Truncated), `ArithmeticStatus` (SizeError)
ready for ON SIZE ERROR wiring.

**LoweringTable.cs**: Central dispatch — `ResolveHelper(OperationKind, source,
target)` returns `MethodInfo?`. null = illegal combination (binder diagnostic).
Maps every legal category pair to its PicRuntime method. Binder and emitter
share this single source of truth.

### Result

NC101A: 243 lines, 20/59 pass — identical output. All infrastructure in place
for category-driven lowering. Legacy method signatures preserved for backward
compatibility during transition.

---

---

