## Entry 168 — 2026-03-29: PERFORM VARYING — One Fix, Three Tests (92→95)

The NC201A/NC220M/NC237A runtime hangs had been "known issues" for two sessions. All three
turned out to share the same root cause: `EmitVaryingMove` and `EmitVaryingAdd` in `Binder.cs`
only handled two expression types for the FROM and BY clauses — positive numeric literals and
simple (non-subscripted) identifiers. Any other shape silently fell through without emitting
IR instructions, so the loop variable was never initialized or incremented → infinite loop.

Three cases the code didn't handle:
1. **Negative literals** (`BY -0.2`): the parser encodes `-0.2` as `BoundBinaryExpression(0, Subtract, 0.2)`,
   not as a `BoundLiteralExpression(-0.2)`. New `TryExtractNegativeLiteral` helper detects and folds this.
2. **Subscripted identifiers** (`BY PFM-F4-24-C(S2)`): used `ResolveLocation(DataSymbol)` which lost
   subscript info. Changed to `ResolveExpressionLocation(BoundExpression)`.
3. **Arbitrary expressions**: added fallback paths using `IrComputeStore` / `IrComputeIntoAccumulator`
   for any expression type the specific handlers don't cover.

The fix was dispatched to a specialist agent. Once applied, NC201A (55 tests), NC220M (24 tests),
and NC237A (11 tests) all ran to completion. The lesson: when IR emission code has a type switch
on bound node kinds, every unhandled case MUST produce a diagnostic — silent fall-through is a
time bomb.

---

