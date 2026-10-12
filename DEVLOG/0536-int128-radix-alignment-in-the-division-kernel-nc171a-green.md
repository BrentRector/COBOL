## Entry 536 — 2026-06-10 09:36 PDT — Int128 radix alignment in the division kernel → NC171A green (46 total)

**The first slice of the deferred G3 Int128 work (COBOLNET_DESIGN §18 #4), scoped exactly to the failing
mechanism:** `CobolNum.Divide` aligns the dividend's radix by `a × 10^(bScale + resultScale − aScale)` — for an
18-significant-digit dividend that intermediate exceeds the long range MID-computation even though the QUOTIENT
fits the receiver (ISO §8.8.1: arithmetic operates on the algebraic values; intermediate width is the
implementor's burden). The division kernel now runs in **Int128**: `Divide`, `RoundDiv` (the rounding kernel —
all 8 modes, now Int128-typed), `DivisionLosesPrecision` (the PROHIBITED detector mirrors the same wide
alignment), with `Pow10Wide` (10^0..38). Storage stays native `long` (≤18 digits); `Rescale` keeps its long
contract over the widened kernel. **NC171A byte-matches → 46 NC locked in. 431 conformance + 15 unit green.**
The rest of the division cluster improved (NC172A/173A 136→100, NC175A 112→70, NC170A 70→40) but carries
additional causes — a 10-agent per-program diagnosis workflow is mapping every remaining DIFF row (results next
entry). Still-latent (documented): bare expression-pipeline `a*b` / aligned `+` between longs can overflow before
any helper runs — that is the full CobolInt carrier wave.

