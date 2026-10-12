## Entry 1290 — 2026-08-17 21:30 PDT — PB76: the far-below-precision remainder marker was an exact half-tie

Found while writing the NUMVAL-F range-check tests for entry 1289 and landed at once — one line, one file, its own
golden. Under STANDARD-DECIMAL, `COMPUTE R9 ROUNDED = 10 ** -20` into `S9(9)V9(9)` stored **0.000000001**; so did
`10 ** -50` and `FUNCTION NUMVAL-F("1E-50")`. §14.7.4.3 r4 (NEAREST-AWAY-FROM-ZERO, implied by ROUNDED): "the
arithmetic value is rounded to the nearest value that can be represented in the resultant identifier. If two
such values are equally near, the value farther from zero is chosen" — 10⁻²⁰ at nine fraction digits is nowhere
near a tie, and its nearest representable value is 0.

**Mechanism.** `CobolDec.DivRemPow10(v, n)` returned the marker `(0, v == 0 ? 0 : 1, 2)` when `n > 38` — "far
below precision: quotient 0, inexact marker" — but a remainder of 1 over a denominator of 2 is EXACTLY HALF, so
`RoundFromRemainder`'s NearestAwayFromZero arm (`absRem2 >= den`) and NearestEven's tie arm both lifted the
quotient. The 34-digit significand makes the shape common, not exotic: `1 / 10²⁰` is `10³³ × 10⁻⁵³`, 44 places
below scale 9 — every SDIDI division result of small magnitude lands here. And the marker carried no sign, so
AWAY-FROM-ZERO / TOWARD-GREATER of a NEGATIVE value stepped toward +∞. Both `ToUnscaled` (every SDIDI store,
MOVE and COMPUTE) and `Clamp`'s decimal128 subnormal re-round rode it — `CobolDec.FromParsed(1, −9999)` came
back as one quantum instead of EC-SIZE-UNDERFLOW.

**Fix.** The marker is `(0, sign(v), 4)`: a signed, below-half, inexact remainder — NEAREST modes fall to 0,
AWAY-FROM-ZERO / TOWARD-* keep their one-unit step in the value's direction, TRUNCATION keeps 0, PROHIBITED still
throws. Golden `pb76_sdidi_subprecision_rounding` (11 rows: the three NEAREST legs, NEAREST-EVEN, AWAY-FROM-ZERO
both signs, TOWARD-GREATER and TOWARD-LESSER of a negative, TRUNCATION, and the at-scale tie/below-half controls
that must agree) matched its spec-derived `.out` first run; unit `CobolDecRoundingTests` (17 cases — the
seven-mode positive matrix, the six-mode negative matrix, PROHIBITED, the subnormal underflow, the controls).
Gates: Conformance 917/917 (Arithmetic|Corpus|Numeric|Standard|Rounded|Intrinsic|SpecTraceability) · Unit 41/41
(the two Dec classes) · characterization 33/33. The §14.7.4.3 inventory rows are blank (GAP), not backlog — left
for the systematic burn-down rather than adjudicated from SDIDI-only evidence (a CONFORMS on r4 would claim the
native carrier too, which this golden does not test).

