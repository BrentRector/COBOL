## Entry 116 — 2026-03-20: NC136A, NC173A 100% — Multi-dim Stride Bug, DIVIDE GIVING Overwrite

### NC136A: 3D table subscript test (3/8 → 8/8)

**Root cause**: `ComputeMultipliers` accumulated strides from the innermost OCCURS element size
upward by multiplying by OCCURS counts. For `E2(2,1)` under `GRP1 OCCURS 10 → E1(5) + GRP2 OCCURS 10 → E2(11)`,
the outer multiplier was computed as `10 * 11 = 110` (inner count × element size). But the
correct stride is `GRP1.ElementSize = 115` (which includes E1's 5 bytes). Writing to `E2(2,1)`
at offset `base + 1*110` overflowed backward into E1(2).

**Fix**: Each multiplier should be the `ElementSize` of the OCCURS group at that dimension —
not an accumulation from the innermost level. Changed `ComputeMultipliers` from:
```
acc = elementSize; for each level: multipliers[i] = acc; acc *= count;
```
to:
```
for each level: multipliers[i] = level.sym.ElementSize;
```

### NC173A: DIVIDE BY GIVING (86/102 → 102/102)

**Root cause**: DIVIDE BY GIVING with multiple targets where the dividend is also a target.
`DIVIDE WRK-DU-2V0-1 BY WRK-DU-1V1-2 GIVING WRK-DU-2V1-1, WRK-DU-2V0-1 ROUNDED, ...`
The lowering emitted one `IrComputeStore(dividend/divisor, target)` per target. After target 2
stored the quotient into `WRK-DU-2V0-1` (overwriting the dividend), subsequent evaluations
read the modified dividend. Result: targets 3-6 computed `modified_dividend / divisor` instead
of `original_dividend / divisor`.

**Fix**: Added `IrComputeIntoAccumulator` IR instruction. DIVIDE BY GIVING now evaluates the
quotient ONCE into an accumulator, then stores from the accumulator to each target via
`IrMoveAccumulatedToTarget`. The dividend is never re-read after the first evaluation.

**Pattern check**: Reviewed MULTIPLY GIVING — it already uses the accumulator pattern (safe).
ADD GIVING and SUBTRACT GIVING also use accumulators (safe). COMPUTE with multiple targets
re-evaluates per target but COMPUTE expressions don't typically reference their own targets.
Flagged for future review if a NIST test surfaces it.

---

