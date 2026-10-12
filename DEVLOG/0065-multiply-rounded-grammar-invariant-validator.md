## Entry 065 — 2026-03-15: MULTIPLY ROUNDED + Grammar Invariant Validator

### MULTIPLY ROUNDED (per-item)

Added ROUNDED support to MULTIPLY BY with per-item flags:
```cobol
MULTIPLY A BY B ROUNDED C D ROUNDED.
```

Changes:
- **Lexer**: Added `ROUNDED` token
- **Grammar**: `multiplyByTarget: identifier ROUNDED?`, used in both BY and GIVING
- **BoundMultiplyTarget**: new class with `Symbol` + `IsRounded`
- **BoundMultiplyStatement**: restructured with `Operand` + `IReadOnlyList<BoundMultiplyTarget>`
  instead of `Left`/`Right`/`GivingTarget`/`IsRounded`
- **BindMultiply**: iterates `multiplyByTarget()` contexts, extracts per-item ROUNDED
- **LowerMultiply**: iterates targets, passes `target.IsRounded ? 1 : 0` per item

Initial grammar attempt had single `ROUNDED?` after `identifierList` — failed
on NC101A's multi-target MULTIPLY with mixed ROUNDED flags. Fixed to use
`multiplyByTarget+` with per-item `ROUNDED?`.

### Grammar Invariant Validator

Added `GrammarInvariants.ValidateSentenceAndStatementBoundaries` — debug-time
checker that walks the parse tree and asserts:
- Every sentence ends with DOT
- No statement ends with DOT

Wired into Compilation pipeline after parsing, before semantic analysis. Catches
grammar regressions that would reintroduce DOT into statements.

### Result

NC101A: **51/89 pass** (was 48/89). F1-2 flipped from FAIL to PASS (ROUNDED fix).
35/35 category tests still pass.

---

---

