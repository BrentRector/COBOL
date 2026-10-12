## Entry 1301 — 2026-08-18 02:16 PDT — PB69 lands: one native `**` arm on the SDIDI, no sentinel — and the SDIDI division bug it uncovered (PB83)

**PB69.** `PowNativeInt` (the Int128-returning literal-exponent arm of native `**`) fell back past the carrier to
`FromDouble(...)`, which SATURATES to `Int128.MaxValue` — safe above a STORE's capacity check, poison at every
value-semantics consumer: `FUNCTION MOD(A ** 3, B)` answered from the sentinel (639816141; the same for A⁴), `IF A ** 4
> A ** 3` was FALSE and `A ** 3 = A ** 4` TRUE, `A ** 3 / A ** 2` was 1.7e8, and the SAME expression spelled `A ** X`
took the Dec arm and gave a THIRD number (966729007 — the Dec's modular landing into Int128 at the working scale).
The note's fix-design question is decided: alternative #1 + #3.

- **One arm, Dec-carried.** Every native integer power renders through `PowNativeIntDec`; the ⚠ the note asked to
  measure first is answered — `CobolDec.From(r, 0)` keeps the full Int128 significand (no 34-digit rounding on
  construction), so a 38-digit power is exact on the SDIDI too. `PowNativeInt` is deleted.
- **The class swept (#3).** A NATIVE arithmetic operation with a Dec operand now evaluates on the SDIDI
  (`CombineCore` — it used to land the Dec into Int128 at the operation's static scale, which truncated
  `2 ** -2 + 1` to 1 through the additive arm and did NOT COMPILE for `*` and `/` — a raw Dec into an Int128 slot,
  CS1503, on legal source; the float lane keeps precedence when a float operand or receiver is present). An
  intrinsic with a Dec RAW argument routes to its SDIDI body under native too (`RenderNum` — MOD/REM gain an
  exact-integer fast path in `ModDec`/`RemDec`, so `MOD(A ** 2, B)` over a 30–38-digit power stays exact); an
  integer argument lands at scale 0 from the RAW operand (`IntArg` — the working-scale landing ate the headroom of a
  33-digit power); and every Dec→Int128 landing that cannot hold the value — an aligned operand, an argument, a
  subscript — raises **EC-SIZE-OVERFLOW** (`CobolDec.ToUnscaledIntermediate`; §14.7.5 case 5; A.1 item 179 now
  names three "checked" places) instead of returning modular low-order digits.
- **Measured** (golden `pb69_native_power_past_the_carrier`, A = 999999999999999, B = 1000000007): T5/T6 TRUE/FALSE;
  T9 `A**3 − A**3` = 0; T10 `A**3 / A**2` = 999999999999998 (the approximation's ratio); T2/T4/T11 exact
  (13657001, 13657001, 40971003 — the product on the SDIDI is exact at 31 digits and the fast path is exact); T8 a
  SIZE ERROR into 31 digits (the checked store); and T1 = T3 — the SAME value for both spellings, the SDIDI
  equivalent arithmetic expression over the 15-significant-digit approximation of a 45-digit A³, whose residue mod
  1e9 is below the approximation's granularity: 0. **The exact 980012199 needs 45-digit arithmetic no native mode
  of this compiler has**; §15.4.1's native latitude is "an implementor-defined approximation of the value of that
  expression", and this is one — a function of the operand, consistent across spellings, never a sentinel. (IBM and
  Micro Focus fall to floating point past the fixed capacity and would answer likewise; GnuCOBOL's GMP would be
  exact.) Recorded in D19 and A.1 179.

**PB83 — found on the way, fixed in the same commit.** Dividing the 45-digit approximation by the exact 30-digit
A² gave 999.99 for a ratio of ~1E15. `CobolDec.Div` pre-scales the numerator so the quotient has 34–36 significant
digits — `scaleUp = 34 + digits(den) − digits(num) + 1`, up to 73 — but multiplied by `Pow10(min(scaleUp, 38))`
while the exponent subtracted the UNCAPPED scaleUp: every SDIDI division with `digits(den) − digits(num) > 3` was
wrong by 10^(scaleUp − 38). Under STANDARD-DECIMAL, `COMPUTE R = 100000 / D30` (a 30-digit divisor) stored **0** where
8.10000007290000066339000603685715…E-25 is the value — a silent wrong answer on ordinary source, in the standard
mode. The scale is now applied in two exact steps (inside the numerator's Int128 headroom, then through the
256-bit product — the second factor is at most 10³⁶ for a ≤39-digit divisor). `CobolDecDivisionTests` pins the
widest case (1 digit over 38), the measured case, the PB69 ratio, and the exact-integer MOD/REM fast path; golden
`pb83_standard_decimal_division_prescale` (R1 = 0.000000000000000000000000810000, R2 = 40.5000003645000033169500301,
R3 = 1.0000, R4 = 0.700000000). Registered as `kb/Work/PB83`, landed.

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|Negative|Arith|Numeric|Power|Pow|Intrinsic|Standard|Compute|SpecTraceability|Size|Subscript` **1048/1048** ·
Unit `SpecTraceabilityInventory|CobolDec|Numeric|Pow|Intrinsic|Real|Manifest|Arith` **303/303** · CLI probes.
Battery owed for the PB68+PB69 batch. Docs: COBOLNET_NUMERIC_DESIGN.md D19 (the ⛔ one-arm paragraph, the
intermediate-precision line), CONFORMANCE.md A.1 179 (the third checked place), kb/Work PB69 → landed, PB83 new
(landed), plan §0.

