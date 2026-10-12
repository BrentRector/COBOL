## Entry 124 — 2026-03-20: DIVIDE INTO REMAINDER — Non-GIVING Accumulator Pattern

### The bug

`DIVIDE A INTO B REMAINDER R` (non-GIVING form) failed with zero quotient and wrong remainder.
The REMAINDER computation only existed for the GIVING form — the non-GIVING path had no
accumulator and the REMAINDER check was gated by `div.Receiver != null` (null for non-GIVING).

### The fix

For `DIVIDE A INTO B REMAINDER R`:
1. Evaluate `B / A` into an accumulator (preserving B's original value)
2. Store the quotient from accumulator to B (with B's truncation)
3. Compute `R = B_original - truncated_quotient × A` using the accumulator

The key insight: the non-GIVING form's dividend IS the target field. The divide overwrites it.
Without saving the original value first, the remainder calculation reads the quotient instead
of the dividend.

---

