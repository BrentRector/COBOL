## Entry 1353 — 2026-08-29 00:30 PDT — PB125 LANDED: FACTORIAL leaves the saturation class — exact on the SDIDI under the standard modes, an honest SIZE error past the Int128 intermediate under native

Batch 7's refuter found the PB13/PB5/PB60 saturation class surviving in the one function the PB13 sweep
deliberately routed AROUND the float lane: FACTORIAL was Int128-capped at 33 in BOTH arithmetic modes, so a
conforming FACTORIAL(34) — §15.36.3 r1 admits every nonnegative integer — returned the §15.3
EC-ARGUMENT-FUNCTION default 0, a silent wrong answer twice over: the EC was misapplied (the argument
conforms) and zero is no §15.4.1 "approximation" of 2.95e38.

Standard modes: FactorialDec on the RenderDec lane (alwaysDec membership + a `when num.StandardDecimal`
guard — the PB116 SQRT shape, so the native lane is untouched by the routing) evaluates §15.36.4 r1c's
product literally on the SDIDI with per-multiply §8.8.1.5.3 rounding. 34! is EXACT — every intermediate
product's dropped digits are its own trailing zeros — and a result past decimal128 raises EC-SIZE-OVERFLOW
from CobolDec.Mul's range check near n ≈ 1755, never from an iteration cap. Native: n > 33 now raises the
SIZE error condition — the value exceeds the native Int128 intermediate, exactly CONFORMANCE.md item 179's
documented class (ON SIZE ERROR takes it; without checking, item 70's fatal termination) — while a negative
argument keeps the genuine argument error. CobolDec.Pow needed no code change: its r2a–d integer arms are
exact and digit-identical to the fixed chains, and the non-integer core is the documented §8.8.1.5.4 r2e
implementor determination (exp_standard_decimal_eae pins the function/operator convergence).

Goldens 2014/pb125_factorial_standard_decimal (34!/33! = 34 exact; magnitude bounds; r1a; ON SIZE ERROR at
9999) and 2002/pb125_factorial_native_size (33!/31! = 1056 exact; ON SIZE ERROR at 34); FactorialDecTests
pins the exact 34-digit landing form, the negative screen, and both overflow raise points.
RV-15.4.1-1 → CONFORMS.

PB126, found by this very gate: its one red — RuntimeConfigTests' reads-only snapshot of
COBOL_COLLATION_CACHE — was a PRE-EXISTING parallel-suite race with CollationKeyCacheTests'
set-and-restore of the same process-global, surfaced when the five new tests shifted the xUnit schedule.
Named, reproduced in isolation (serial re-run green), fixed at the root in the same commit: both classes
share [Collection("process-environment")], and kb/Work/PB126.md records the mechanism and the sweep
(the suite's only stability-asserter/mutator pair). FULL Unit re-ran green, 4589/4589.

