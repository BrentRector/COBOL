## Entry 085 — 2026-03-16: SUBTRACT GIVING Fixed — Complete GIVING Family

Same bug as ADD GIVING: `SUBTRACT A FROM B GIVING C` lowered as `C = C - A` (subtract from target's current value) instead of `C = B - A` (subtract from the FROM operand).

Fix: `BoundSubtractStatement` gets `IsGiving` flag and `GivingMinuend` (the FROM operand). Lowering uses `IrComputeStore` with a synthetic expression `minuend - sum(operands)` for the GIVING form. Multi-operand `SUBTRACT 10 20 FROM B GIVING C` → `C = B - (10 + 20) = 70`.

All four arithmetic GIVING forms now verified:
- ADD GIVING: `IrMoveAccumulatedToTarget` (target = sum)
- SUBTRACT GIVING: `IrComputeStore(minuend - accumulated)` (target = FROM - sum)
- MULTIPLY GIVING: already worked (different binding path)
- DIVIDE GIVING: `IrComputeStore(dividend / divisor)` (fixed earlier)

---

