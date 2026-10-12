## Entry 083 — 2026-03-16: BoundArithmeticStatement Deleted — 13 Silent Drops Eliminated

Replaced all 13 instances of `return new BoundArithmeticStatement(...)` across ADD, SUBTRACT, MULTIPLY, DIVIDE, and COMPUTE binders with `throw new InvalidOperationException(...)` that includes the source line number.

Deleted the `BoundArithmeticStatement` class entirely. Removed the `case BoundArithmeticStatement: break;` from `Binder.LowerStatement` that silently swallowed these nodes at IR lowering time.

This was the last systematic silent-wrong-behavior pattern in the compiler. With this and the earlier `IrSetBool(true)` elimination, the compiler now has zero paths where it silently produces wrong or missing code. If it can't handle a construct, it fails loudly.

---

