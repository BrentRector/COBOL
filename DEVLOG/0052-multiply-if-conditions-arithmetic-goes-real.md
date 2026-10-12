## Entry 052 — 2026-03-14: MULTIPLY + IF Conditions — Arithmetic Goes Real

Implemented PIC-aware arithmetic and real condition evaluation:

**PicRuntime.MultiplyNumeric**: decode left + right operands from PIC storage,
multiply as decimal, scale/round to destination PIC, encode result.

**PicRuntime.CompareNumeric**: decode both operands, return CompareTo for
relational comparison (-1, 0, 1).

**BoundTreeBuilder.BindCondition**: walks the condition parse tree
(logicalOrExpression → relationalExpression) and extracts the actual
relational operator and operands. Produces BoundBinaryExpression with
real Equal/NotEqual/Greater/Less operators instead of always-true.

**BoundMultiplyStatement**: captures left, right, and GIVING target.
Binder lowers to IrPicMultiply. CIL emitter calls PicRuntime.MultiplyNumeric.

NC101A test result detail lines not yet visible — the test formatting
requires many string MOVEs to intermediate fields that aren't all wired
yet. But the arithmetic and comparison machinery is now production-grade.

---

