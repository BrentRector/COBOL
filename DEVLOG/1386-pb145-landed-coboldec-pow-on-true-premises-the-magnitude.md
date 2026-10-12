## Entry 1386 — 2026-08-29 09:32 PDT — PB145 LANDED: CobolDec.Pow on true premises — the magnitude-aware escape, exact parity, the range-guarded r2e core, one name per direction — and EC-SIZE-UNDERFLOW joins the SizeNames mask

Four false premises, one exponentiation (kb/Work PB145). THE ESCAPE: past the 500000 loop bound the
disposition is now t = n·log10|b| — Log10Abs keeps precision for a near-unit base (the exact SDIDI
difference d = |b|−1, where the old guard comment's |n·log10|b|| ≥ |n|/34 was false within 10⁻³³ of 1 and
1.00001 ** 1000000 raised a spurious size error on the in-range value ≈ 22025.36) and for an
out-of-binary64 base (significand/exponent decomposition); an in-range t computes 10^frac(t) ON THE SDIDI
EXPONENT FIELD, and the out-of-range directions carry their OWN §8.8.1.5.2 r2 names — 0.5 ** 600000 is
EC-SIZE-UNDERFLOW (was OVERFLOW). PARITY: TryIntegerValue carries sign and EXACT evenness out on the Int128
(even = exp ≥ 1 or sig even) — the ±1 shortcut read the parity of the SATURATED long, and long.MaxValue is
odd, so (−1) ** 10²⁰ answered −1 in both exponent signs. THE r2e CORE: a base outside binary64 routes
through the log decomposition — 1.0E−400 ** 0.5 = 1.0E−200 EXACTLY (was a silent 0), 2.0E+400 ** 0.5 ≈
1.4142E+200 (was a spurious overflow); the in-range arm keeps the documented binary64 approximation, now a
CONFORMANCE.md determination BOUNDED to binary64's range, with the r2e development shall itself filed as
kb/Work PB167 (status: owner — fund the 34-digit exp/ln core or ratify). ONE NAME PER CONDITION: Clamp's
below-range landing keeps EC-SIZE-UNDERFLOW under INTERMEDIATE ROUNDING IS PROHIBITED (was §14.7.4.3 r7's
TRUNCATION), settled beside the tiny-and-inexact determination (r2's too-small = the round-to-zero test).
AND THE RECORDING HALF: EC-SIZE-UNDERFLOW was MISSING from EcBinder.SizeNames — the same missing-member
shape as PB141's DELETE FILE in QueryFor — so >>TURN never reached a statement's mask and
EXCEPTION-STATUS read a STALE name inside ON SIZE ERROR (the probe showed the prior leg's TRUNCATION).
PREMISE CORRECTED: NUMVAL-F is NOT a third inexact producer — NvfScan rejects past the settled r1b
34-digit cap, so the mode goldens pin the literal and computed-intermediate producers instead.

Verdicts: GR-8.8.1.5.2-1/-3 PARTIAL → CONFORMS, GR-8.8.1.5.2-2 PARTIAL → CONFORMS, GR-8.8.1.5.4-2/-3
DIVERGES → PARTIAL (the r2e shall rides PB167). Goldens pb145_pow_range + the pb145_round_{away,even,
trunc,prohib} family (the NEAREST-EVEN tie discrimination at last); 15 CobolDecPowRangeTests facts.

