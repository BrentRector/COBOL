## Entry 087 — 2026-03-16: Class Conditions — IS NUMERIC, IS ALPHABETIC

Grammar: added NUMERIC, ALPHABETIC, ALPHABETIC_LOWER, ALPHABETIC_UPPER lexer tokens. `relationalExpression` now has class condition as first alternative (before relational operator) to prevent `IS NUMERIC` from matching as a relational operator prefix.

`BoundClassConditionExpression` carries subject, ClassConditionKind, and IsNegated. `IrClassCondition` IR instruction dispatches to PicRuntime class predicate methods.

Runtime helpers: `IsNumericClass` (digits, sign, decimal point, spaces), `IsAlphabeticClass` (letters and spaces), `IsAlphabeticLowerClass`, `IsAlphabeticUpperClass`.

IS NOT form handled via `IsNegated` flag → `IrBinaryLogical(Not)` inversion.

2 integration tests: IS NUMERIC (positive/negative/NOT), IS ALPHABETIC/ALPHABETIC-UPPER/ALPHABETIC-LOWER/NOT ALPHABETIC.

Phase B5 status: level-88 ✅, class conditions ✅, abbreviated relations deferred (requires binder rewrite).

---

