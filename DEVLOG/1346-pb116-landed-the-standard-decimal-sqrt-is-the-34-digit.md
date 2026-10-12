## Entry 1346 — 2026-08-28 23:39 PDT — PB116 LANDED: the standard-decimal SQRT is the 34-digit CORRECTLY-ROUNDED root (§15.84.4 r1/r2 — the ONE §15 function whose standard-mode value the standard fixes exactly); STANDARD-DEVIATION's EAE swept onto it

`CobolDec.Sqrt`: an EXACT integer floor square root (Newton over BigInteger) of the significand scaled to ≥ 71
digits at an even power — the 36–37-digit root carries 2–3 guard digits below the 34-digit rounding position and
the floor remainder becomes the sticky bit into the ONE `Round34` funnel, so the landing is correctly rounded in
every intermediate mode by construction (the mode-sensitive √0.1 case — 35th digit 5, nonzero tail — pinned
both ways). r1's "argument-1 is not rounded" holds: the exact fixed-point operand lifts per §8.8.1.5.2 with no
binary64 detour. `SqrtDec` carries §15.84.3 r2's zero-or-positive value screen (EC-ARGUMENT-FUNCTION).
`StdDevDec` — §15.86.4 r1's EAE = SQRT(VARIANCE) — swept off its `Math.Sqrt(…ToDouble())` detour onto the same
root (variance(1,2,3,4) → √1.25 to 34 digits). The NATIVE lane keeps binary64 under r4's approximation licence
(its argument now correctly rounded per PB115).

⚠ The golden's FIRST run caught a real dispatch subtlety: `RenderDec`'s early-out (`!alwaysDec &&
!AnyDecOrRealRaw`) returned null for a plain-operand `SQRT(2)` before the new arm — SQRT2 printed the binary64
value while alwaysDec's STANDARD-DEVIATION took the decimal root. Sqrt joined alwaysDec, with the arm's
`when num.StandardDecimal` guard keeping a NATIVE float-argument call (the one shape that reaches RenderDec
under native) on the float lane — native behavior byte-identical. Golden `2014/pb116_sqrt_standard_decimal`
(relocated after the edition gate caught ARITHMETIC IS STANDARD-DECIMAL as 2014, and a 34-digit literal draft
tripped §8.3.1.2's 31-digit cap — both course corrections logged); +4 Unit (`CobolDecSqrtTests`).
RV-15.84.4-1/-2 and RV-15.86.4-1 → CONFORMS. RV-15.84.4-3 (standard-binary's 113-bit leg) stays with the
standing owner question.

