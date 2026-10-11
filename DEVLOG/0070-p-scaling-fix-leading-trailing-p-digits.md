## Entry 070 — 2026-03-15: P Scaling Fix — Leading/Trailing P Digits

### The Bug

`PIC P(4)9` (value .00001) was decoded as 10000 instead of .00001. The runtime's
P scaling logic had the direction inverted: leading P should DIVIDE (more fraction
positions), not MULTIPLY.

### Root Cause Chain

1. **PicUsageResolver**: P digits were counted as regular integerDigits/fractionDigits
   instead of tracked separately. Fixed: added `leadingPScaling`/`trailingPScaling`
   tracking with `hasRealDigits` flag to distinguish P before vs after 9's.

2. **PicLayout**: Added `LeadingPScaling`/`TrailingPScaling` fields, threaded through
   to PicDescriptor via PicDescriptorFactory.

3. **DecodeDisplay/DecodeCompBinary**: Leading P was applying `× Pow10(leading)` —
   wrong direction. Fixed: combined formula `totalFractionScale = FractionDigits + LeadingPScaling`,
   then `result /= Pow10(totalFractionScale)`. Trailing P correctly multiplies.

4. **EncodeDisplay/EncodeCompBinary**: Same inversion fixed. Trailing P divides to
   remove implied integer positions; total scale = FractionDigits + LeadingPScaling.

5. **WouldOverflow**: Updated DISPLAY overflow check to use combined scale.

### Result

**NC101A: 79/90 pass** (was 78/90). F1-11 (COMP SV9 × S99P) flipped to PASS.
Remaining 11 failures: F1-6, F1-12 (COMP issues), F1-17/19/21/23 .01/.03/.05
(multi-target MULTIPLY with P-scaled multiplier).

---

