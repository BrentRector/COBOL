## Entry 126 — 2026-03-20: NC121M 39/39 — DIVIDE INTO GIVING Dropped Subscripts

### The bug

`DIVIDE 3 INTO TABLE1-NUM(INDEX1) GIVING NUM-9V9` computed 0 instead of 1. The dividend
`TABLE1-NUM(INDEX1)` was being read without its subscript — always reading element 1.

In `BindDivide`, when `DIVIDE a INTO b GIVING c` is parsed, the INTO operand `b` becomes
the dividend for the GIVING form. The code created a **new** BoundIdentifierExpression from
just the symbol, discarding subscripts:

```csharp
dividend = new BoundIdentifierExpression(targets[0].Target.Symbol, CobolCategory.Numeric);
```

Fix: `dividend = targets[0].Target;` — preserves the original expression with subscripts.

One-line fix. NC121M went from 34/41 to 39/39 + 2 inspect.

---

