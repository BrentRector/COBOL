## Entry 143 — 2026-03-24: Two More Bugs Hiding Behind GF-48

With the condition grammar refactored (Entry 141-142), GF-48 still failed. Traced to two
independent bugs that the compound condition exposed:

### Bug 1: `IsNumericClass` accepts signs in alphanumeric fields

`CLASS-1 NOT NUMERIC` returned FALSE for `"+1234"` stored in `PIC X(5)`. Our `IsNumericClass`
method accepted `+` and `-` characters regardless of the field's PIC category. COBOL-85 §6.3.4.1
is clear: for alphanumeric/group items, NUMERIC means digits 0-9 only. Signs and decimals are
only valid for numeric-category items.

**Root cause**: The original `IsNumericClass` was written for numeric fields and never updated
when class conditions were extended to alphanumeric fields. The PIC descriptor was passed in
but never consulted for category.

**Fix**: Check `pic.Category == CobolCategory.Numeric` before allowing sign/decimal characters.
One-line change with immediate impact: GF-48 passes, and the `IS NUMERIC` class test is now
spec-correct for all field categories.

### Bug 2: Arithmetic expressions as comparison operands

`IF A = B - 1` failed with `COBOL0504: Cannot normalize comparison operands`. The Binder's
`NormalizeOperand` had an explicit switch for identifiers, literals, figuratives, and
negative-literal patterns — but no case for `BoundBinaryExpression` with arithmetic operators.
Any comparison where one side was a computed expression (not a simple field reference) was
rejected.

**Root cause**: The comparison normalization was designed for the common case (field vs literal)
and never extended for arithmetic operands. COBOL allows any arithmetic expression as a
comparison operand: `IF A = B + C`, `IF X > Y * 2`, etc.

**Fix**: Two changes:
1. `NormalizeOperand`: New `ComparisonOperandKind.ArithmeticExpression` that carries the
   `BoundBinaryExpression`. Evaluated at emit time via `IrComputeIntoAccumulator`.
2. New `IrPicCompareAccumulator` IR instruction: compares a PIC location against a pre-evaluated
   decimal accumulator. Reuses existing `PicRuntime.CompareNumericToLiteral`.
3. `ExpandAbbreviatedConditions`: Added `IsArithmeticOp` check so arithmetic expressions in
   abbreviated chains (e.g., `IF A = B OR C - 1`) are recognized as value operands, not
   conditions.

### Also bug 2b: Abbreviated expander didn't recognize arithmetic as "bare operand"

`IF CCON-2 EQUAL TO CCON-1 OR 8 OR CCON-3 - 1` — the `CCON-3 - 1` was a
`BoundBinaryExpression(Subtract)` which the expander's bare-operand check
(`expr is BoundIdentifierExpression or BoundLiteralExpression`) didn't match. The expander
left it as a standalone expression, which the Binder then couldn't process as a condition.

**Fix**: Added `IsArithmeticOp` check in `ExpandAbbrev` so arithmetic expressions are treated
as value operands that participate in abbreviation.

**Result: NC211A 49/51** (was 47/51). Only 2 figurative constant failures remain (ALL literal
runtime issue, unrelated to conditions).

---

