## Entry 1001 — 2026-07-23 01:10 PDT — Conformance fix-queue: CA24 (intrinsics) — EXP/EXP10 overflow is a legal saturating result, not a domain error; LOG/LOG10 domain guard moves to the body

First of the intrinsics batch (and the first fix gated the CORRECTED way — wave-local FILTERED, not the full suite).

**The bug (§15.34.3 r1 / §15.35.3 r1).** EXP/EXP10's only argument rule is "class numeric" — no upper bound — so
EXP(710) is a fully LEGAL argument whose value e**710 ≈ 2.25e308 merely overflows binary64 to +∞. `FromDouble` (the one
double→scaled-long funnel) mapped BOTH NaN AND ±∞ to `ArgumentError`, so a legal EXP overflow returned the §15.3
default 0 (checking off) — NOT ON SIZE ERROR wrongly fired, R stayed 0 — or threw a spurious fatal EC-ARGUMENT-FUNCTION
(checking on), where the spec sets no such condition. The code already correctly saturates FINITE over-range results
(EXP10(19)→1e19→long.MaxValue); ±∞ was the sole leak.

**Fix.** (1) `FromDouble` splits the guard: `IsNaN` → ArgumentError (a genuine domain result, ACOS|x|>1 / SQRT-neg);
`IsInfinity` → saturate `d>0 ? long.MaxValue : long.MinValue` (a legal overflow, letting the receiver store raise
EC-SIZE-* / truncate per §14.7.4). (2) The one coupling that broke: LOG(0)/LOG10(0) = −∞ WAS caught by FromDouble's
old ∞→ArgumentError; now that ∞ saturates, those genuine §15.55.3 r2 / §15.56.3 r2 domain violations are guarded at
the FUNCTION BODY instead — `CobolIntrinsics.Float.Log/Log10` now `x <= 0 ? ArgumentError(...) : Math.Log(x)` (the
`long` default 0 widens to the `double` return; throws when checking on). Both edits are required together.

**Verified (CLI):** `COMPUTE R = FUNCTION EXP(710) ON SIZE ERROR "SIZE" NOT ON SIZE ERROR "OK"` → SIZE / R=00007
(unchanged; was OK / 00000). LOG(0) checking-off → 0 (the §15.3 default, unchanged). Golden
`tests/conformance/2002/intrinsic_exp_overflow`.

**Gate (wave-local FILTERED — the corrected per-commit discipline):** characterization 33/33, intrinsic conformance
(`~Intrinsic|Exp|Log|Float`) **853/853**, intrinsic unit 3/3 — ~15 s total. The full Conformance runs ONCE for the
whole intrinsics batch (CA24 · V54 · CA23 · CA25) before merge.

