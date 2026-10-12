## Entry 119 — 2026-03-20: NC140A 70/70, NC141A 9/9 — Silent Fallthrough Anti-Pattern Redux

### The anti-pattern (again)

`LowerSetIndex` had the exact silent-fallthrough anti-pattern the user flagged in an earlier
session. The UpBy and DownBy cases only handled `BoundLiteralExpression` — when the value was
any other expression type (identifier, binary expression), the case silently fell through with
no instruction emitted and no error reported.

This caused two categories of failure:
1. `SET INDEX1 UP BY TABLE2-REC(INDEX2)` — identifier expression, silently did nothing (NC141A)
2. `SET INDEX1 UP BY -5` — unary negation produced BoundBinaryExpression, silently did nothing
   (NC140A: 42 of the 70 failures)

### The fix

Rewrote `LowerSetIndex` with zero silent paths:
- Added `TryEvalConstant()` — recursively evaluates compile-time constant expressions
  (literals, unary +/-, simple binary arithmetic). Handles `-5`, `+5`, `3 + 2`, etc.
- Added identifier-expression path using `IrPicAdd`/`IrPicSubtract` for field-to-field deltas
- Every `switch` branch now either emits IR or reports a `COBOL05xx` diagnostic
- Null `targetLoc` reports `COBOL0510` instead of silent return

### AI misstep

This is the same class of bug the user explicitly asked me to sweep for and eliminate. I was
supposed to have audited ALL lowering methods for silent fallthroughs. The `LowerSetIndex`
method was overlooked because it was changed during the SET grammar expansion but the audit
didn't re-check the new code paths. The lesson: when adding new expression types to a
dispatch, re-audit ALL branches of that dispatch for the new types.

### USAGE INDEX normalization

Also added: standalone level-77 `USAGE IS INDEX` items now get normalized to `PIC S9(9) COMP`
in SemanticBuilder, matching the representation used by INDEXED BY items. This ensures
consistent storage layout and comparison behavior.

NC131A still has 1 remaining failure (9/10) — comparing a standalone USAGE INDEX item with a
table-bound INDEXED BY index. The normalized storage types should now match, but the comparison
still fails. Deeper investigation needed.

### Results

- NC140A: 28/70 → **70/70** (100%)
- NC141A: 3/9 → **9/9** (100%)
- NC131A: 9/10 (unchanged, 1 remaining INDEX comparison edge case)
- guard.sh ALL GREEN

---

