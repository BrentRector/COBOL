## Entry 056 — 2026-03-15: CobolCategory Lattice — Unified Type System

### The Change

Replaced the ad-hoc `PicCategory` (compiler) / `CobolType` (bound tree) dual
system with a single `CobolCategory` enum (ISO §6.1.2) shared between compiler
and runtime:

```
Numeric, NumericEdited, Alphanumeric, AlphanumericEdited, National, NationalEdited
```

Changes across 8 files:
- **Runtime**: `CobolCategory` enum + `CobolCategoryExtensions` (IsNumericLike,
  IsAlphanumericLike, IsNationalLike)
- **PicDescriptor**: `Category` property, auto-classified from flags, passed
  through CIL `newobj` (9-arg constructor)
- **TypeSystem**: `PicCategory` removed. `PicLayout.Category` is `CobolCategory`.
  `ITypeSymbol.Category` / `DataTypeSymbol.Category` added.
- **PicUsageResolver**: Classifies into full lattice (NumericEdited vs Numeric, etc.)
  using tracked char flags (hasNumericChars, hasAlphaChars, hasNationalChars)
- **PicDescriptorFactory**: Uses `symbol.ResolvedType.Category` as source of truth
- **BoundNodes**: `BoundExpression.Category` replaces `CobolType Type`. Old
  `CobolType` class removed entirely.
- **BoundTreeBuilder**: All `CobolType.*` → `CobolCategory.*`
- **CilEmitter**: `EmitLoadPicDescriptor` passes Category, uses `Category.IsNumericLike()`

### Result

NC101A: 243 lines, 20/59 pass — identical output. Pure refactor, no behavior change.

---

