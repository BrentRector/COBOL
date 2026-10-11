## Entry 172 — 2026-03-30: M001 — IrExpression Contract (Eliminating BoundExpression Leakage)

### The problem
44 occurrences of `BoundExpression` embedded in IR instructions. The IR layer should be
independent of the bound tree, but `IrPerformTimes.CountExpression`, `IrFunctionCall`,
`IrElementRef`, `IrRefModLocation` all carried `BoundExpression` references. This forced
CilEmitter to import the entire `Semantics.Bound` namespace and evaluate expressions at
emit time instead of during lowering.

### The fix (4 stages)
**Stage 1:** Created `IrExpression` hierarchy — `IrLiteral`, `IrLoadNumeric`,
`IrBinaryExpr`, `IrUnaryExpr`, `IrIntrinsicCall` — plus argument types
`IrLiteralStringArg`, `IrAlphanumericArg`, `IrNumericArg`. Added `Binder.LowerExpression`
that converts `BoundExpression` → `IrExpression` during lowering. 19 unit tests.

**Stage 2:** Migrated all 8 IR instruction types to use `IrExpression` instead of
`BoundExpression`. Added `CilEmitter.EmitIrExpression` that evaluates the IR-native
expression tree. 4 enums (`IrArithmeticOp`, `IrUnaryOp`, `IrCompareOp`,
`ClassConditionKind`) moved to IR namespace.

**Stage 3:** Deleted `EmitExpression`, `EmitIntrinsicCall`,
`PreResolveExpressionLocations`, and the entire `ResolvedLocations` parameter bundle
from CilEmitter. Zero `BoundExpression` references remain in IR or CilEmitter.

**Stage 4:** 13 reflection contract tests + 13 integration pipeline tests verifying
the IrExpression pipeline end-to-end.

Design doc: `docs/ir/IR-Expression-Contract.md`. Tests: 453 unit + 287 integration + 95 NIST.

### What I learned
The `ResolvedLocations` bag was the real code smell — it bundled pre-resolved
parameters alongside the bound expression, creating a hidden coupling between
lowering and emission. Once expressions became self-contained IR trees, the
entire resolution step vanished.

---

