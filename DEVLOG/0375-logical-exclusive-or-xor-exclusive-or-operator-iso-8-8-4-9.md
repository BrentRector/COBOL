## Entry 375 — Logical exclusive-or (XOR / EXCLUSIVE-OR) operator (ISO §8.8.4.9)

Plan item M4-2's XOR part — though XOR is actually a logical operator from §8.8.4.9 (2002-era, not a 2023 delta;
the audit mis-tagged it). `XOR` and `EXCLUSIVE-OR` are equivalent; a combined condition `c1 XOR c2` is true iff
exactly one of `c1`, `c2` is true. Precedence is **NOT > AND > XOR > OR** (§8.8.4.11.3).

- Lexer: `XOR` and `EXCLUSIVE-OR` tokens (both corpus-safe — 0 occurrences in NIST/fixtures).
- Grammar: a new `logicalXorExpression` level between `logicalOrExpression` and `logicalAndExpression`, so
  `A OR B XOR C` parses as `A OR (B XOR C)`.
- Binder: `BindLogicalOr` now consumes `logicalXorExpression`; new `BindLogicalXor` builds a
  `BoundBinaryExpression` with the new `BoundBinaryOperatorKind.Xor`. (Fixed the two `ControlFlowBinder` EVALUATE
  navigations that walked `logicalOrExpression → logicalAndExpression` directly — they now go through
  `logicalXorExpression`; a full-codebase scan confirmed those were the only such paths.)
- Lowering/emit: `ConditionLowerer` adds a Xor case → `IrBinaryLogical(…, IrLogicalOp.Xor)`; the emit uses the CIL
  `xor` opcode (boolean operands are 0/1, so `xor` == exactly-one-true).

Verified truth values + precedence: `(A=1) XOR (B=1)`=T, `(A=1) XOR (C=1)`=F, `(B=1) XOR (B=1)`=F, and
`A=1 OR B=1 XOR C=1` = `T OR (F XOR T)` = T (the `4T` case proves XOR binds tighter than OR). Tests:
`LogicalXor_TruthValuesAndPrecedence` + conformance `tests/conformance/2002/logical_xor`.

Guard ALL GREEN: **1047 unit / 467 integration (+2) / 364 NIST**, 0 regressions.

