## Entry 079 — 2026-03-16: Phase B — SIGN, Figuratives, MOVE Matrix, DIVIDE, and an ANTLR Landmine

**Session**: #10 (continued, Phase B branch)

### B3: SIGN Clause — All Four Variants

Implemented SIGN clause end-to-end in three slices:
1. **Trailing Separate**: Grammar already parsed it; wired SemanticBuilder → DataSymbol.ExplicitSignStorage → PicDescriptorFactory → PicRuntime decode/encode.
2. **Trailing Overpunch (default)**: IBM overpunch tables ({ABCDEFGHI / }JKLMNOPQR), changed COBOL default from LeadingSeparate to TrailingOverpunch per spec. Fixed ComputeFieldSize to not add extra byte for overpunch.
3. **Grammar fixes**: `signClause` expanded to allow bare `LEADING`/`TRAILING` without `SIGN` keyword, `CHARACTER` made optional after `SEPARATE`. `usageClause` expanded for bare `COMP`/`DISPLAY`/`COMPUTATIONAL` without `USAGE` prefix. Added `COMPUTATIONAL` lexer token.

NC116A went from compile-fail to 65/67 (82%). NC118A from compile-fail to 17/30.

### B2: Figurative Constants — Production-Grade

`FigurativeKind` enum shared between compiler and runtime. `BoundFigurativeExpression` as first-class bound node (not string hack). `IrMoveFigurative` / `IrMoveAllLiteral` IR instructions. `MoveFigurativeToField` fills entire destination with figurative byte. `MoveAllLiteralToField` repeats pattern. VALUE clause initialization via `DataSymbol.FigurativeInit`. Conditions handle `IF A = SPACES` etc.

### B1: MOVE Matrix + EditPattern

Implemented the full numeric MOVE matrix:
- `MoveNumericToNumeric` as single canonical path (DecodeNumeric→EncodeNumeric) for all USAGE combos
- `MoveAlphanumericToNumeric`, `MoveNumericEditedToNumeric`, `MoveAlphanumericToNumericEdited` — three new runtime methods
- `MoveNumericToAlphanumeric` — sign stripped per ISO §14.19.4
- `EmitMoveWithStandardSignature` helper in CilEmitter to avoid code duplication
- `ExpandEditPattern`: converts `"-9(9).9(9)"` to `"-999999999.999999999"` for FormatByEditPattern
- `FormatByEditPattern`: two-pass pattern-driven formatter (right-to-left digit fill, left-to-right zero suppression)
- Group SIGN clause propagation: `PropagateGroupSignClauses` walks data tree, inherits parent SIGN to elementary children
- COMP field sizing fixed: `ComputeFieldSize` dispatches on Usage (binary size for COMP, BCD for COMP-3)
- COMP overflow: based on PIC digit count, not binary capacity

10 new unit tests for MOVE + formatting. NC116A at 65/67.

### DIVIDE: Spec-True, No Vendor Extensions

The DIVIDE grammar saga consumed significant time. Three attempts to add `literal` after `INTO` (for NC117A's non-standard `DIVIDE A INTO 864.36 GIVING B`) all failed — any mention of `literal` after `INTO` poisons ANTLR4's LL(*) prediction for ALL statements.

**Root cause found**: ISO COBOL (all editions 1985-2023) never allows a literal after INTO. NC117A uses a NIST test card error. Decision: keep grammar ISO-pure. NC117A's parse error is acceptable.

DIVIDE GIVING now uses `IrComputeStore` with synthetic `BoundBinaryExpression(Divide)` — handles all operand combos through the COMPUTE expression evaluator. REMAINDER uses `BoundBinaryOperatorKind.Remainder` + `decimal.Remainder`.

### The Enum Landmine

Adding `Remainder` to `BoundBinaryOperatorKind` between `Divide` and `Equal` shifted all comparison operator enum values by 1. `EmitCompareResultToBool` used hardcoded `case 4:` / `case 5:` for Equal/NotEqual — the shift made Equal match NotEqual's case. Every EVALUATE and condition silently produced wrong results. 10 integration tests broke.

**Fix**: Replaced all hardcoded integer cases with proper enum casts (`case BoundBinaryOperatorKind.Equal:`). Enum members can now be freely reordered.

**Lesson**: Never use hardcoded integer values for enum members. Always use the enum name.

### Build System Fix

`CobolParserCoreBase.cs` (hand-maintained parser base class with `IsAtLineStart()` predicate) was in `Generated/` and got clobbered by ANTLR regeneration. Moved to `Parsing/`. Full clean rebuild now works.

### Test Counts

- Unit tests: 109 (was 99)
- Integration tests: 41 (was 40)
- NIST: 4 byte-for-byte (NC101A, NC171A, NC106A, NC176A)
- NC116A: 65/67, NC118A: 17/30

---

