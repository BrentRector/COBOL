## Entry 1310 — 2026-08-18 06:49 PDT — PB77 lands: every carrier's landing past the Int128 carrier has two forms, chosen by the landing

**PB77.** `MOVE FUNCTION NUMVAL-F("5E+30") TO PIC V9(9)` stored 884105727 and `… TO PIC 9(5)` stored 03715 — the
low-order digits of `Int128.MaxValue`, not of 5×10³⁰ (whose low digits at either scale are zeros; §14.6.8.2 r4:
a MOVE aligns by decimal point and truncates "on either end", and a MOVE has no size error). The float family did
the same past 1.7×10³⁸, and so did the NO-PHRASE arithmetic store (`COMPUTE X5 = <COMP-2 1.0E+40>` stored 03715).

- **The root cause was one landing form for two landings.** PB13 made the float quantizer and the exact family's
  rescale SATURATE past the carrier so a store's capacity check RAISES the size error — right for a CHECKED store
  (ON SIZE ERROR / EC-SIZE) and for an intermediate consumer with no check downstream, and garbage for a
  TRUNCATING store, which has no check to see the sentinel and keeps `v mod 10^digits` of it. The SDIDI carrier
  already had both forms (PB74's `CobolDec.ToUnscaledChecked` / `ToUnscaled`); the other two carriers had one.
- **Every carrier now has the pair, and the emitter names the landing.** Native exact family:
  `CobolIntrinsics.Rescaled(…, checkedLanding)` — the unchecked arm is `CobolNum.RescaleStoreCap` (the digits a
  ≤38-digit store could never use are dropped BEFORE the multiply); NUMVAL / NUMVAL-C / NUMVAL-F carry the flag.
  Float family: `CobolFloat.ToScaled` (checked, saturating) / `ToScaledUnchecked` (one `RoundScaled` inside the
  carrier — the two are ONE function there — and `CobolFloat.LowOrderDigits` past it: the double's exact ±m·2^e
  expansion at the landing scale, rounded by the ONE `CobolNum.RoundDiv` kernel over BigInteger, the runtime's
  second and last cold BigInteger path); `CobolIntrinsics.FromDouble(…, checkedLanding)` likewise (an unchecked
  ±∞ lands zero, as an unchecked NaN always did). `RuntimeApi.FloatToScaled(…, bool checkedLanding)` has NO default,
  so a new site must say which store it is; `NumericRenderer.StoreArgs(…, checkedLanding)` takes it from
  `ecState.SizeErrVar` (arithmetic — and the edited-receiver `Aligned(checkedPath)` had a Real arm that ignored its
  own flag) or `false` (`StoreExpr`: MOVE, INVOKE BY CONTENT); `Align` and the formatted-time SECONDS argument stay
  checked (an intermediate consumer wants the loud sentinel); the quantizer, native `**` and the NUMVAL renders
  carry the ONE `NumericRenderer.CheckedFlag` (`, checkedLanding: true` under `ReceiverContext.InSizeError`).
- **Measured** (golden `pb77_move_past_the_carrier`, sixteen rows, every expected value derived by exact
  arithmetic on the sending value — 1.0E+40 as a double is 10000000000000000303786028427003666890752): NUMVAL-F
  5E+30 → 000000000 / 00000; NUMVAL-F "-1234567890123456789012345678901E+3" → -01000; a 31-digit NUMVAL → 78901;
  a COMP-2 1.0E+40 → 90752, 000000000, 0000000303786028427003666890752, ZZZZ9 90752; -2.5E+40 → -73792; the same
  through no-phrase COMPUTE (a COMP-2, `FUNCTION ABS` of one — `FromDouble`'s unchecked form — an edited receiver);
  and under ON SIZE ERROR all three still fire with the receiver left alone. `CarrierLandingFormTests` (22) pins
  the kernels — the exact expansions, every rounding mode over the exact kernel, checked-vs-unchecked on all three
  runtime entry points — and greps the emitter sites. Verdicts GR-14.6.8.2-{1,2,4} and GR-14.6.13.1.3-8 →
  CONFORMS (GAP 4015 → 4011).
- **Documented, not moved:** the in-carrier float→fixed manner is the binary64 product's own rounding (a COMP-2
  8.2 → 8.2 into 9V9, 1.15 → 1.14 into 9V99 — a coin flip on the receiver's scale). It is now written on
  CONFORMANCE.md §3 (the §14.6.8.2 r1/r2 bullet) with the A.1-70 row for the no-phrase disposition; whether it
  should become the exact expansion everywhere (`LowOrderDigits` already computes it) or the shortest round-trip
  decimal is **PB90** — a GnuCOBOL survey first, per the follow-GnuCOBOL decision. The two item-92 rows are a
  legitimate several-rows-per-item case (the A.1 audit says so), not a numbering defect.

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|Numeric|Move|Float|Intrinsic|Numval|SpecTraceability|Arith|Compute|Size|Nist|Function|Edited|Rounded`
**1801/1801** · Unit `CarrierLandingForm|FloatQuantize|NumvalF|CobolIntrinsicsDec|SpecTraceabilityInventory|Registry|
Manifest|Guard|IntrinsicReal|Numeric|Float` **2955/2955** · CLI probe (the golden's sixteen rows). Docs:
COBOLNET_NUMERIC_DESIGN.md (the two-landing-forms paragraph), CONFORMANCE.md (§3 bullet, §7 row 70), kb/Work PB77 →
landed, PB90 registered, plan §0.

