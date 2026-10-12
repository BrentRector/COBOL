## Entry 181 — 2026-03-30: M004 Stage 2 — ExpressionBinder + ProcedureNameResolver

Extracted 24 expression methods to ExpressionBinder (~780 lines) and 4 procedure name
methods to ProcedureNameResolver (~120 lines). BoundTreeBuilder reduced from 4,335 to
3,590 lines.

### The BindArithmeticTargets incident
Python script for bulk forwarding wrapper replacement accidentally deleted
`BindArithmeticTargets` (a Stage 3 arithmetic method) because its doc comment was
adjacent to `BindArithmeticExpr` (a Stage 2 expression method). The doc_comment_start
detection consumed the wrong method's body. Caught immediately at build time (8 CS0103
errors). Restored the method from git diff. Lesson: multi-line signature methods and
adjacent methods are the fragile cases for automated extraction.

### The Typed delegate pattern
`BoundTreeBuilder.Typed<T>()` is a generic static method that attaches `ResultType` to
expressions. The extracted ExpressionBinder calls it via `_ctx.Typed(expr)` — a
`Func<BoundExpression, BoundExpression>` delegate. This loses the generic type parameter
(always returns `BoundExpression`), but the agent handled it correctly by splitting
construction and typing into separate statements where the concrete type was needed.

No SyncToContext needed — BindingContext's fields are set once in the constructor.

Tests: 849 unit + 287 integration + 95 NIST. All green.

---

