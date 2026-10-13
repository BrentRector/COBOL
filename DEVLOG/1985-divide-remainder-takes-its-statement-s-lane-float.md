## Entry 1985 — 2026-10-12 20:43 PDT — DIVIDE ... REMAINDER takes its statement's lane, float intermediates past the carrier raise, and the SDIDI operand lift answers from one predicate

Wave 1069 group A, one root: the numeric renderer's landings of an operand into the 128-bit integer carrier (PB3017,
PB3018, PB3019). Re-probed on the branch's own build; two of the three notes needed their premise corrected.

**PB3019 - the intermediate float landing raises.** The note's repro (`FUNCTION MAX` of 1E300 and 1E301 COMP-2 items) does not
reproduce: an all-float list rides the binary64 body, a mixed list the SDIDI selection body, and relations compare in
binary64, so none of them aligns a float through the saturating landing. What did reach `CobolFloat.ToScaledSaturating`
was every receiver-less `NumericRenderer.Align` / `AlignRoundedUp` consumer of a binary64 operand, observable in the
native `DIVIDE F BY 7 GIVING Q REMAINDER R` with F = 1.0E+40, which stored a quotient of the sentinel and took NOT ON SIZE
ERROR. `ToScaledSaturating` is replaced by `ToScaledIntermediate` (NaN to 0; past the carrier EC-SIZE-OVERFLOW, ISO 14.7.5
5), the twin of `CobolDec.ToUnscaledIntermediate`); the saturating form is deleted. The one consumer that wants a membership
answer, the formatted-time seconds, states its own screen in `CobolDate.SecondsOfReal` (|seconds| >= 10^19 saturates by
sign, the bound `SecondsOfDec` already used).

**PB3017 - one DIVIDE ... REMAINDER kernel per lane.** Both operand kinds of the note reproduced (an unsigned 16-byte
dividend under STANDARD-DECIMAL took ON SIZE ERROR; a float dividend past 1.7E+38 stored a sentinel quotient). The second
needed no kernel change once PB3019 landed. The first is the SDIDI lane's: `EmitDivideRemainder` now dispatches to
`SdidiSubsidiaryQuotient` (14.9.12.4 GR1 quotient by `CobolDec.Div` over each operand's `DecOperand` lift, the new
`CobolDec.SubsidiaryQuotient` for GR6 c) - truncation toward zero at the receiver's scale, then the digit cap, the
significand left at the quotient's own exponent so the 34-digit invariant holds - and the GR7 remainder through `Combine`)
or to `NativeSubsidiaryQuotient` (the exact Int128 kernel as before). The stored quotient is the same SDIDI value.
**Sibling fixed in the same method:** the subsidiary quotient's digit cap used the receiver's PICTURE digits even for a
binary-capacity receiver that holds its whole container, so `DIVIDE 123456 BY 7 GIVING Q REMAINDER R` into `Q PIC 9(4)
COMP-5` left R = 70004 and `9E18 / 1` into `PIC 9(18) COMP-5` left R = 9E18; `SubsidiaryDigitsOf` caps at the digits the
store keeps (PB129's own rationale).

**PB3018 - the observable defect discharged, the shape fixed.** A signed 16-byte container owns 39 digits as the unsigned
one does, so both already rounded to 34 digits on the native Dec lane (measured with a 36-digit value in `PIC 9(31) COMP-5`
and `PIC S9(31) COMP-5`); the signed operands that lift exactly are the DISPLAY, packed and ordinary binary ones of at most
31 digits, and no unsigned-wide operand has a bound of 38 or fewer. What was wrong was one rule written twice (`DecOperand`'s
unsigned arm and `LiftRounds`' `x.U ||`). `DecOperand` now asks `LiftRounds` for every carrier and the carrier only picks the
rounding twin. DOC-A.1-82 records the determination.

**Goldens** (all 2023, values derived by hand in the headers): `pb3017_sdidi_divide_remainder_beyond_the_int128_carrier`
(2^128 - 1 over 7, which the SDIDI quotient multiplies back exactly, so R = 0; over 10^10, R = 1768200000; 1E30 over 3,
R = 1; 1E40 over 7, the quotient's size error with Q and R unchanged; and the small ROUNDED, negative and scaled legs
that pin the subsidiary quotient as the TRUNCATED quotient: 7/2 ROUNDED Q 4 R 1, -7/2 ROUNDED Q -4 R -1, 10/3 into
`S9(5)V9` Q 3.3 R 0.1),
`pb3019_native_float_intermediate_past_the_carrier_is_a_size_error` (1E40 and -1E40 size errors, the in-range control, and
the binary-capacity cap legs R = 4 and R = 2) and
`pb3018_native_dec_lane_wide_binary_operand_signed_and_unsigned_agree`. Unit: `CarrierLandingFormTests` now expects the raise,
`CobolDateSecondsLandingTests` pins the date screen. Docs: COBOLNET_NUMERIC_DESIGN.md (the intermediate landing, the
DIVIDE REMAINDER lane paragraph), CONFORMANCE.md DOC-A.1-82 and DOC-A.1-179. Notes PB3017, PB3018 and PB3019 are landed with
`closes_rows: []` and a reason: none claimed an inventory row.

**Review round 3 (finding R3-1).** The branch review found DOC-A.1-70's `Pinned by` cell still naming the test PB3019 renamed (`..._SaturatesIntermediate_...`); no gate read a ref that only the Annex A.1 register carries (`check_pins` checks the inventory's refs against it, `UnresolvedTestRefs` resolves only the inventory's refs). The cell now names `..._RaisesIntermediate_...`, and `audit_annex_a1.py` gained `check_pin_resolution`: every `Pinned by` ref must resolve (a golden file exists; an xUnit class file exists and declares the method; a bare class pins the family), with self-test cases proving each failure reason. `pins_of` now drops a gloss written outside the backticks (item 81 carried one). `audit_annex_a1.py --check` and `--self-test` are green.

**Review round 1 of the finishing branch (findings R1-1, R1-2).** R1-1: the gloss self-test case could not fail for the reason it named, because its second (missing-golden) ref produced the expected text whether or not `pins_of` dropped the gloss on the first. It is now two single-ref cases (an xUnit class pin and a golden pin, each with a gloss outside the backticks) that expect NO finding, so a `pins_of` that keeps the gloss fails both; proven by reverting `pins_of` to the main form (both cases FAILED) and restoring it. R1-2: `check_pin_resolution`'s `source.setdefault(f, f.read_text(...))` read the file before looking up the key, and the `rglob` ran per pin; both are now real caches (a class file is read once, a class searched once). Over the real register, every .cs file is read once (679 reads, 679 distinct; the review measured 201 reads of 82 files in this function).


**The arch-oracle baseline (finisher, landing-oracle stop).** The landing refused the branch at exit 7: the recorded baseline (00435d6e90c0, then main's c41f1b82af2c after merging origin/main) predated the numeric changes. `compare_oracle.py` against c41f1b82af2c found 6 of 7,795 cases different, each explained by its class. ADDED (3): the branch's own goldens `pb3017_sdidi_divide_remainder_beyond_the_int128_carrier`, `pb3018_native_dec_lane_wide_binary_operand_signed_and_unsigned_agree` and `pb3019_native_float_intermediate_past_the_carrier_is_a_size_error`. CSHARP (3, emitted C# only, output and diagnostics identical): `l1_divide_quotient_remainder_standard_decimal` and `pb84_standard_decimal_intermediate_consumers`, whose STANDARD-DECIMAL DIVIDE ... REMAINDER now runs in its statement's CobolDec lane (`CobolDec.Div` plus `CobolDec.SubsidiaryQuotient`) instead of the Int128 `CobolNum.Divide`/`CapDigits` path (PB3017); and `pb84_sdidi_intermediate_consumers`, whose float-to-scaled intermediate is `CobolFloat.ToScaledIntermediate` (raises past the carrier) instead of `ToScaledSaturating` (PB3019). The baseline is re-recorded at 8b0941c5f232, the merge of origin/main into the branch.
