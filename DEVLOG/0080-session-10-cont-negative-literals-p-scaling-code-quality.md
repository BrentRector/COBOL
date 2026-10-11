## Entry 080 — 2026-03-16: Session 10 (cont.) — Negative Literals, P-Scaling, Code Quality Audit

**Session**: #10 (continued)

### NC116A: 67/67 — Fixed via Negative Literal Comparison

Root cause of NC116A GF-10.02/GF-10.04: `IF field NOT EQUAL TO -8036` silently returned TRUE because negative literals like `-8036` were parsed as `BoundBinaryExpression(Subtract, 0, 8036)`, not `BoundLiteralExpression(-8036m)`. `LowerCondition` didn't recognize this pattern and fell through to `IrSetBool(result, true)` — always TRUE.

Fix: added pattern match in `LowerCondition` for the `(0 - literal)` shape, negating the literal and routing to the existing `IrPicCompareLiteral` path. No grammar change needed — the `NOT(EQUAL)` parse works correctly as long as the inner comparison is right.

### NC106A: 127/127 — Fixed via Trailing P Scaling

The negative literal fix unmasked a latent arithmetic bug: `SUBTRACT 99 FROM WRK-DS-0201P ROUNDED` (PIC S99P) gave -90 instead of -100. `ApplyScalingAndRounding` handled FractionDigits and LeadingScaleDigits but completely ignored TrailingScaleDigits.

For PIC S99P (TrailingScaleDigits=1): field stores multiples of 10. To store -99 with ROUNDED: divide by 10 → -9.9, round → -10, multiply back → -100. Fix: added trailing P branch in `ApplyScalingAndRounding`.

### AI Misstep: "Cleanest Fix" vs Production-Quality Fix

Three failed attempts to fix the negative literal issue:
1. **Constant-folding hack in BindRelationalOperand** — user correctly rejected this as papering over the root cause instead of fixing `LowerCondition`'s architectural limitation.
2. **`IrExpressionCompare` general fallback** — caused NC106A regression because `EmitExpression` for identifier fields decoded differently in the expression evaluation context.
3. **Grammar change** (remove `NOT` from `logicalNotExpression`) — also caused NC106A regression via ANTLR parser regeneration changes.

The correct fix was the simplest: extend `LowerCondition`'s pattern match for the specific `(0 - literal)` shape. No grammar change, no new IR instruction, no architectural change. The lesson: when the binder produces a known pattern (`0 - literal` for unary minus), recognize that pattern in the lowering instead of changing the binder or the grammar.

### Code Quality Audit

Identified and fixed three critical silent-wrong-behavior patterns:
1. `IrSetBool(result, true)` fallback → `InvalidOperationException` (fatal on unrecognized conditions)
2. Magic casts `(BoundBinaryOperatorKind)20/21/22` → proper `Or/And/Not` enum members
3. Magic cast `(BoundBinaryOperatorKind)99` → proper `Power` enum member

Remaining audit items recorded in PROJECT_PLAN.md with phase assignments.

### Unified PIC Pipeline

Eliminated the "two pipelines disagree" class of bugs: `PicUsageResolver.ParsePic` now delegates to `Runtime.PicDescriptorFactory.FromPicBody`. `CompilerPicDescriptorFactory` uses the runtime factory for ALL fields with PIC strings. PicLayout is a thin view, not an independent semantic engine. -187 lines deleted.

### Test Counts
- 119 unit, 42 integration, 5 NIST at 100% (NC101A, NC171A, NC106A, NC176A, NC116A)

---

