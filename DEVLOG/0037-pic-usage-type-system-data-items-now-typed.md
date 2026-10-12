## Entry 037 — 2026-03-14: PIC/USAGE Type System — Data Items Now Typed

### What Changed

User provided a concrete PIC/USAGE typing design separating "language-level type" from
"storage layout." Implemented as a clean layer between the symbol table and the binder.

**ITypeSymbol interface**: IsNumeric, IsAlphanumeric, IsBoolean, PicLayout?, UsageKind.
Carried by every DataSymbol via `ResolvedType` property.

**PicLayout**: decoded PIC string → Category (Numeric/Alphanumeric/National/Boolean/Edited),
Length, IntegerDigits, FractionDigits, IsSigned, IsEdited. First-pass decoder handles:
- `S` (sign), `9` (digit), `X` (alphanumeric), `A` (alphabetic), `N` (national)
- `V` (implied decimal), `P` (scaling)
- Repeat counts: `9(5)`, `X(120)`
- Editing symbols: `Z`, `*`, `+`, `-`, `$`, `B`, `0`, `/`, `,`, `.`, `CR`, `DB`

**PicUsageResolver**: single entry point called by SemanticBuilder for each data item.
Maps PIC string + USAGE clause → concrete DataTypeSymbol.

**UsageMapper**: keyword text → UsageKind enum (DISPLAY, COMP, COMP-1/2/3, BINARY,
PACKED-DECIMAL, INDEX, POINTER, OBJECT).

### What This Enables

The binder and CIL emitter can now query any data item's type:
- `variable.ResolvedType.IsNumeric` — arithmetic compatibility
- `variable.ResolvedType.Pic.IntegerDigits` — storage size for CIL fields
- `variable.ResolvedType.Usage` — runtime representation (packed decimal, binary, etc.)

NC101A compiles with type resolution — zero errors.

### Current State

- Grammar: 8 files, zero warnings, NC101A compiles
- Semantic: symbol table + PIC/USAGE types + two-pass analysis
- Pipeline: Preprocess → ANTLR4 Lex → Parse → SemanticBuilder (symbols + types) → ReferenceResolver → [Binder → CIL next]
- Next: binder (bound tree from parse tree + types), then CIL emitter

---

