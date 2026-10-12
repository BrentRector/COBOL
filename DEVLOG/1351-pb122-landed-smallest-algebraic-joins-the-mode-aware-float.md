## Entry 1351 — 2026-08-29 00:15 PDT — PB122 LANDED: SMALLEST-ALGEBRAIC joins the mode-aware float path — the blanket refusal leaned on r4's NATIVE latitude and a false rationale, and under STANDARD-DECIMAL it rejected legal source

R10 fixed HIGHEST/LOWEST-ALGEBRAIC's float handling (mode-aware r2/r3 usage bars, carrier-extreme folds) but
left SMALLEST-ALGEBRAIC refusing EVERY float on §15.83.3 r4's implementor latitude — which licenses usage
restrictions only "if native arithmetic is in effect". Under STANDARD-DECIMAL a native COMP-1/COMP-2
argument-1 is legal source (r2 bars only the §3.166 STANDARD BINARY usages), so the refusal was an
over-rejection there; and its stated rationale — "no smallest positive increment for IEEE floats (it is
exponent-dependent, not a PICTURE property)" — confused the ULP of a stored VALUE with what §15.83.1
actually asks for: "the smallest algebraic value that may represent the DIFFERENCE between two values
represented in argument-1", i.e. the FORMAT's smallest positive subnormal, as much a compile-time constant
as 10^(−scale) is for fixed-point.

All three functions now share ONE mode-aware float path in BindAlgebraicFold: the r2/r3 bars (citing each
function's own § via fsec3), the §15.x.4 r1 IN-ARITHMETIC-RANGE screen — new, the float-ITEM twin of the
float-EDITED screen: carrier extremes against the mode intermediate's range, passing for every carrier
declarable today and firing automatically if a wider one lands — then the per-function fold. SMALLEST's
fold: binary32 → 2^−149, binary64 → 2^−1074 (34 correctly-rounded digits, full decimal128 precision — the
MAX folds keep R10's adjudicated round-trip grain), decimal carrier → 1E−28. The A.1 item 180 determination
is REWRITTEN (CONFORMANCE.md): no restriction beyond rules 2 and 3 — removing a documented restriction whose
technical premise was false, rather than conditioning it on mode and keeping dead latitude. Strictly more
legal source is accepted; nothing that compiled before changes meaning.

Golden 2023/pb122_smallest_algebraic_float (STANDARD-DECIMAL: the subnormal folds bounded in decimal128
comparisons + the R10 HIGHEST/LOWEST arms guarded); Algebraic_SmallestKeepsTheFloatGuard_1516 REWRITTEN to
Algebraic_SmallestFloat_FoldsToCarrierSubnormal (5E-324 / 1.401298464324817E-45 through COMP-2 receivers);
new Algebraic_SmallestStandardDecimalBarsStandardBinaryFloat_1516 pins the r2 bar reaching SMALLEST with the
15.83.3 citation. RV-15.83.4-1 → CONFORMS; AR-15.83.3-2/-3/-4 → CONFORMS.

