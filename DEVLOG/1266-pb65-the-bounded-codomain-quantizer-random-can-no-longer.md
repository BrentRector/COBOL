## Entry 1266 — 2026-08-09 14:45 PDT — PB65: the bounded-codomain quantizer — RANDOM can no longer round to 1, ASIN(1) lands inside pi/2

RV-15.75.4-1's mechanism, closed at the one quantization site: FromDouble's away-from-zero rounding — which
must stay, it recovers SQRT(10) ** 2 = 10 from the binary64 artifact (NIST IF136A) — rounded the top
half-ulp of a bounded codomain OUT of it. RANDOM's [0,1) reached exactly 1.000000000 in a PIC 9V9(9)
receiver (§15.75.4 r1's "less than one" is strict); the deterministic witness on the shared path was
SQRT(0.999999999) → 1.000000000.

The family was enumerated MECHANICALLY from the §15.x.4 rules, not assumed: RANDOM [0,1) OPEN · ATAN
(−pi/2, pi/2) OPEN · ASIN/ACOS CLOSED-but-IRRATIONAL — rounding asin(1) at scale 9 gave 1.570796327,
ABOVE pi/2, outside even a closed bound because the bound is not a decimal — · FRACTION-PART's |v| < 1
by its EAE. SIN/COS's closed RATIONAL ±1 is deliberately absent: rounding cannot exceed an
exactly-representable bound the double already respects.

The catalog row carries the bound (IntrinsicCodomain on IntrinsicSig — one field, five rows), the
RenderFloat tail dispatches once, and FromDoubleBounded clamps the QUANTIZED value against Int128 scale-37
constants (pi to 37 fraction digits, derived independently via Decimal; the per-scale limit is one exact
integer division — never a double computation that loses the bound past 15 digits). The Dec/standard-mode
arm needs no clamp: the un-quantized double never exits its codomain.

Probed: ASIN(±1) → ±1.570796326 = ⌊pi/2·10^9⌋, ACOS(−1) → 3.141592653, ATAN(1e18) → 1.570796326,
FRACTION-PART(5.9999999996 COMP-2) → 0.999999999, interior ASIN(0.5) untouched, SQRT(0.999999999) STILL
1.000000000 (no stated bound — the legal-approximation contrast line), 20k seeded RANDOM draws zero >= 1.
Golden pb65_codomain_clamp. RV-15.75.4-1 PARTIAL → CONFORMS; RV-15.10.4-1/RV-15.11.4-1 gain the
native-path clamp beside their PB56 Dec-carrier record. THE THREE PB65 SINGLES ARE LANDED — the queue
moves to PB59.

