## Entry 1344 — 2026-08-28 23:29 PDT — PB115 LANDED: the ONE correctly-rounded scaled→double conversion — CobolFloat.ScaledToDouble replaces the emit lane's repeated-multiplication 10^scale divisor AND CobolDec.ToDouble's Math.Pow bridge; a legal ASIN(|x| ≤ 1) argument no longer evaluates NaN

The §15 close-out's sharpest find (batch 7's refuter, DEVLOG 1343), fixed at the root: `NumericRenderer.Real`
now emits `CobolFloat.ScaledToDouble(expr, scale)` for every scaled float-lane argument — fast path (|unscaled|
≤ 2^53, |scale| ≤ 22): two EXACT double operands, one IEEE rounding; slow path: the correctly-rounded decimal
parse — and `CobolDec.ToDouble` routes through the SAME conversion (its `(double)Sig * Math.Pow(10, Exp)`
overshot independently at scale 25). The dead `Pow10D` (exact only through 1e22 — at scale ≥ 23 its divisor sat
one ulp low, so §15.10.3 r2-conforming all-nines arguments arrived ABOVE 1.0 and Math.Asin returned NaN → the
§15.3 default 0, or a thrown EC-ARGUMENT-FUNCTION under checking) is deleted. Sweep: `grep "Math.Pow(10"` — the
one other hit is FUNCTION EXP10's own body (its r4 approximation licence, not this defect).

Tests: `ScaledToDoubleTests` (+19 Unit — the 31-nines ASIN witness converts to EXACTLY 1.0; every all-nines
scale 1–31 stays inside the closed domain; a 600+-pair deterministic sweep against the IEEE-parse oracle,
negative P-scales included; the CobolDec scale-25 lane); golden `2002/pb115_asin_scaled_argument` — the three
boundary scales (23/25/31) plus the asin(0.5) control, asserted as RANGES around the true values (§15.10.4 r1
licenses an approximation; the fixed-point landing's grain is the documented item-92 determination and is
deliberately not re-pinned by this golden — first drafts pinned equalities and chased the landing's
NearestEven-vs-truncation grain before recognizing it as out of scope).

