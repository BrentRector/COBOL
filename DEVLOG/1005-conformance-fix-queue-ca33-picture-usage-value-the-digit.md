## Entry 1005 — 2026-07-23 01:52 PDT — Conformance fix-queue: CA33 (picture-usage-value) — the digit-position CAP measures DIGIT POSITIONS, not just the '9' count

First picture-usage-value fix. §13.18.40.3 SR14 caps a numeric / fixed-point numeric-edited item at 1–31 (18
pre-2002) DIGIT POSITIONS, where §13.18.40.4 GR14 makes Z and * digit-bearing positions and P "counted in the maximum
number of digit positions." `PictureAnalyzer` set a numeric-edited item's `Digits = count('9')` only, and DataBinder's
cap gate checked `pic.Digits` under a `Digits: > 0` guard — so (a) mixed Z/9 UNDERCOUNTED (`Z(11)9(8)` = 19 positions
but Digits=8, no check fired) and (b) all-suppression pictures had Digits=0, so the `> 0` guard skipped the check
entirely (`Z(35)` = 35 positions sailed through).

**Fix.** A new `PicInfo.DigitPositions` (backing field, defaults to `Digits` for pure-numeric/non-analyzer PicInfos
where they coincide). `PictureAnalyzer` computes it as `count('9','Z','*') + leadingP + trailingP + floatingExtra`,
where `floatingExtra` = (occurrences − 1) of the one floating '+'/'-'/currency symbol appearing ≥2 times — counting
SYMBOL OCCURRENCES (not run length) naturally excludes embedded simple insertions (`$$,$$9` → 4 `$` → 3 positions). It's
set on both the Numeric and NumericEdited returns. DataBinder's gate now measures `pic.DigitPositions` and drops the
`Digits: > 0` sub-pattern (gating on `DigitPositions > 0`).

**Verified (CLI):** `Z(11)9(8)` @85 → COBOLNET0802 "has 19 digit positions; COBOL-85 limits … to 18" (was accepted);
`Z(35)` @2023 → COBOLNET0801 "has 35 digit positions" (was accepted, Digits=0 skipped it); legal `Z(6)9.9(5)` (12) and
floating `$$,$$9` (4) still compile. Negative goldens `tests/conformance/negative/numedit_digits_over18_at85` (85-only)
+ `numedit_digits_over31` (all editions). Fallout swept BEFORE implementing (CA39 lesson): the only over-`18`-ish corpus
pictures (`arith_standard` `9(30)` pure numeric = Digits, `formatted_datetime` `Z(6)9.9(5)` = 12) are within cap — zero
re-baselining.

**Gate (wave-local FILTERED):** characterization 33/33, CorpusRunner+Picture+VersionMatrix+DataLevel conformance 2199/2199.

