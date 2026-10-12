## Entry 055 — 2026-03-15: Grammar Literal Split — Strings Out of Arithmetic

### The Problem

ANTLR grammar precedence bug: `moveSource: arithmeticExpression | literal` —
`arithmeticExpression` appears first and can match STRINGLIT through
`primaryExpression → literal`, so string literals are ALWAYS parsed as arithmetic
expressions, never as literals. This caused:
- Quote characters `"` embedded in field data (MOVE "FAIL*" stored as `"FAIL*"` with quotes)
- Figurative constants SPACE/ZERO treated as identifiers in expressions
- Cascading failures: BAIL-OUT string comparisons, header duplication, missing test lines

### The Fix

Split `literal` into `numericLiteral | nonNumericLiteral`. Restricted
`primaryExpression` to `numericLiteral | identifier | functionCall | (expr)`.
String literals and figurative constants can now only appear through `literal`
or `nonNumericLiteral` paths, never through arithmetic.

Also added `relationalOperand: arithmeticExpression | nonNumericLiteral` so
IF conditions can still compare against string literals and figurative constants.

Updated `BoundTreeBuilder`:
- `BindLiteral` → delegates to `BindNumericLiteral` / `BindNonNumericLiteral`
- `BindCondition` → uses `relationalOperand` instead of `arithmeticExpression`
- `BindRelationalOperand` → routes non-numeric literals through proper path
- `BindArithmeticExpr` simplified — no more figurative constant hacks

### Result

NC101A output: 243 lines, 20/59 pass. Quotes eliminated from all field data.
Test names show cleanly. Headers no longer corrupted. Behavior-preserving
refactor verified against test output.

---

