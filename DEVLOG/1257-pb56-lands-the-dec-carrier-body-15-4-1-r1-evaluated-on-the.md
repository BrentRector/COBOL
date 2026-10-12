## Entry 1257 — 2026-08-09 10:32 PDT — PB56 lands: the Dec-carrier body — §15.4.1 r1 evaluated ON the SDIDI, the sweep found two sibling arms, and eleven rows close

The backlog campaign's first compiler fix, and it is the top-ranked one: under a standard arithmetic
mode, every §15.4.1 r1 function now evaluates its equivalent arithmetic expression ON the CobolDec
SDIDI (CobolIntrinsics.Dec.cs — SIGN/ABS/INTEGER/INTEGER-PART/FRACTION-PART/MOD/REM/MAX/MIN/ORD-MAX/
ORD-MIN/SUM/RANGE/MEAN/MEDIAN/MIDRANGE plus the formerly staged ANNUITY/PRESENT-VALUE/VARIANCE/
STANDARD-DEVIATION), dispatched by IntrinsicRenderer.RenderDec BEFORE any landing: arguments lift
exactly per §8.8.1.5.1/.2 r1 from the RAW operand and the result lands at the receiver ONCE. The
interim landing's fixed working scale — which turned SIGN(1e-9 − 0) into 0 — no longer touches the
family; an all-fixed argument list stays on the exact Int128 engine, whose agreement with the Dec
bodies is now a pinned theory, not a comment.

THE SWEEP EARNED ITS RULE TWICE. Probing after the core landed found the SAME defect in two sibling
arms: the R18 Exp/Exp10 arms round-tripped their exponent through the truncating landing
(DecOperand(Arg(...)) — the lift UNDID by the landing it followed), and Dbl — the float family's
entire argument intake — landed before converting, so SQRT(4e-18) probed as ZERO with the core fix
already in. Both fixed at their single sites. RenderFloat's standard-mode tail now CONVERTS the
prose family's binary64 result in per §8.8.1.5.1 instead of quantizing at a receiver-derived scale,
which also removed the double-rounding artifact: ASIN(1) is 1.570796326 by ONE final transfer at the
receiver's mode, not 1.570796327 by two roundings.

COBOLNET0899 IS RETIRED: the four inexact-EAE functions compute, the descriptor is deleted, and the
negative fixture that pinned the stage green (arith-standard-intrinsic-staged — the
green-test-holds-the-gap shape) is replaced by the positive golden pb56_dec_carrier_intrinsics:
16 lines, every expected value derived from its EAE, three of them (SG/EX/SQ) impossible before the
fix. VERIFIED: FULL greenfield Conformance 4,308/4,308 · CobolIntrinsicsDecTests 14/14 · intrinsic
unit filters 49/49 · SpecTraceability 10/10. ELEVEN inventory rows re-verdict CONFORMS+tested, each
naming its probe line; PB56's residue rows (different mechanisms) are reassigned to PB65, and
RV-15.69.4-2/-3 land with PB60's parser rewrite. D3's ledger line and the Landed remark now describe
the current dispatch. GAP 4,219 → 4,208; the defect backlog stands at 206 bad-verdict rows.

