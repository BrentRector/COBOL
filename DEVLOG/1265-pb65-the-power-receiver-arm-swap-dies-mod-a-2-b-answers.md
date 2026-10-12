## Entry 1265 — 2026-08-09 14:20 PDT — PB65: the Power receiver-arm swap dies — MOD(A ** 2, B) answers 13657001 in every receiver

RV-15.64.4-1, the refuter's overturn: `COMPUTE R = FUNCTION MOD(A ** 2, B)` printed 320612800 into
S9(9)/S9(18)/S9(28) and the owed 13657001 into S9(31) — the receiver's PICTURE alone selected the value.
PB32 fixed the receiver-LESS arm and left the receiver-bearing one forcing FloatWorkingScale (≥9) onto an
integer-exact power: the ×10⁹ landing overflowed Int128 INSIDE PowNativeInt, the double fallback saturated,
and ModScaled consumed the sentinel. The two-arm dispatch's FIFTH sighting.

The fix removes the receiver from the integer dispatch entirely: a non-negative LITERAL exponent lands the
exact Int128 at scale 0 on BOTH arms (PowNativeInt loses its landing-scale parameter — an integer power's
scale is 0 by construction); a negative or runtime-item exponent — whose SIGN is a run-time fact, so no
compile-time scale serves both regimes (9 corrupts big powers, 0 truncates reciprocals) — returns the new
PowNativeIntDec on the SDIDI carrier, same owner-decided values (2026-08-03: exact while it fits, the double
approximation past), every consumer already carrier-total over Dec. Probed: all four receivers 13657001,
DISPLAY/IF agree, REM recovered, MOD(A ** X, B) exact on the Dec arm, 2 ** −2 = 0.25 both arms, 10³⁰ exact,
`A ** X = A ** 2` compares SAME across the two arms. Sweep: ModReal/RemReal ride the exact carrier `%`
instead of the catastrophically-cancelling literal a − (b·⌊a/b⌋) form.

Golden pb65_power_receiver (15 lines, every value from the python oracle). RV-15.64.4-1 → CONFORMS.
The edge probe found the PAST-Int128 sibling still live — MOD(A ** 3, B) answers 639816141 from the
saturated Int128.MaxValue sentinel (980012199 owed; pre-existing, a lower threshold before this fix) —
ledgered as PB69 with the fix alternatives recorded, deliberately EXCLUDED from the golden so the wrong
answer is never pinned as expected.

