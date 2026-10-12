## Entry 635 — 2026-07-06 20:05 PDT — Phase 5: the COBOL-2014 DATE/TIME + NUMBER intrinsic family — LANDED COMPLETELY (a full §15.3 format engine)

Ten functions in one wave — COMBINED-DATETIME (§15.17), FORMATTED-CURRENT-DATE / FORMATTED-DATE /
FORMATTED-DATETIME / FORMATTED-TIME (§15.38–15.41), INTEGER-OF-FORMATTED-DATE (§15.48), NUMVAL-F (§15.69),
SECONDS-FROM-FORMATTED-TIME (§15.79), TEST-FORMATTED-DATETIME (§15.92), TEST-NUMVAL-F (§15.95). All fixed/
optional-trailing arity with NO phrase keyword ⇒ **generic binding** (flip Deferred→Runtime); the subsystem lives
in the renderer + runtime. The last of the six Phase-5 intrinsic families. (Design from the recon workflow
wf_840f8070-fdf; the §15.6 format model read in full.)

- **The §15.3 date/time FORMAT ENGINE** (`CobolDate`): ONE `Tokenize` feeds ONE `EmitFormatted` (the FORMATTED-*
  producers) and ONE `Analyze` (the INTEGER-OF-FORMATTED-DATE / SECONDS-FROM-FORMATTED-TIME / TEST-FORMATTED-
  DATETIME consumers) — singular pattern, one parse of a format. Calendar / ordinal / ISO-week dates (`ISOWeek`
  for the week-numbering year at Dec/Jan boundaries), basic vs extended (a colon/hyphen ⇒ the decimal separator
  appears in data; basic ⇒ phantom), UTC (`Z`) / offset (`+hhmm`) time with the §15.3.3.6.1 sign convention and
  the r2 local−offset midnight roll, fractional seconds. The fractional separator is whichever of `.`/`,` appears
  in the format itself, so DECIMAL-POINT IS COMMA (§15.3.3.2) needs no external mode (F.4 solved cleanly).
  `Analyze` does §15.92.4 **per-digit range narrowing** (the error position is the digit at which a value becomes
  provably out of range — "20051314"⇒6, "15990316"⇒2, the spec NOTE examples). LEAP-SECOND OFF (the standard
  default); the ON path is a documented one-parameter thread.
- **NUMVAL-F / TEST-NUMVAL-F** (`CobolIntrinsics.Exact`): the floating NUMVAL (signed mantissa + optional
  `E±exponent`, 1..4 exp digits) parsed exactly to (unscaled, scale) then rescaled; TEST-NUMVAL-F returns 0 / the
  first-error position (embedded-space r b.1, the 31-digit native cap r b.2) / LENGTH+1 (r c). commaMode via the
  numeric-channel `CommaFlag`.
- **Renderer**: six `RenderNum` cases + four `RenderString` cases (two via `RenderFormatted*` static helpers that
  pass the seconds as unscaled+scale so the fraction survives). SECONDS-FROM-FORMATTED-TIME's result scale is the
  format's fractional-second count, computed at EMIT time via `CobolDate.FormatFractionDigits` (the WHEN-COMPILED
  compile-time-runtime-call precedent). ⚠ **Tokenizer fix caught by the fractional test**: the fraction is an
  `s`-run AFTER the decimal separator (§15.3.3.2), not extra `s` in the seconds run — the initial "sss"-style
  model gave `hh:mm:ss.ss`→two Second fields (SFT 45296.50→45290.00); fixed with a `sawSecond` gate.
- **Spec-strict + edition**: a non-literal format → **COBOLNET1517** (§15.*.3 r1 mandates a literal); the family
  is 2014 (COBOLNET1502 below). Arity fixes vs the provisional catalog rows: FORMATTED-DATETIME 3..4 / FORMATTED-
  TIME 2..3 (one optional numeric offset arg). The National result form (a national format ⇒ national result)
  awaits the USAGE NATIONAL runtime (residue #11 — an orthogonal whole-compiler subsystem, not a leg of this
  feature); the alphanumeric surface — the entire realizable one — is complete.

Golden `tests/conformance/2014/formatted_datetime.cob` (16 lines: all six date/time forms + the two validators +
NUMVAL-F/TEST-NUMVAL-F + a FORMATTED-CURRENT-DATE round-trip), every line spec-verified, byte-exact,
**GreenfieldOnly**. +14 `IntrinsicFunctionDifferentialTests`. Battery: **1941 conformance (+14) · 216 unit ·
117 corpus goldens GREEN**; greenfield-only. ⛔🎉 **PHASE 5 INTRINSICS COMPLETE** — all six families landed:
CONCAT/BASECONVERT (628), TRIM (629), FIND-STRING (630), SUBSTITUTE (631), CONVERT (632), MODULE-NAME (633), the
ALGEBRAIC fold family (634), the 2014 date/number family (635). RESUME NEXT: the M2-OO-1h data-model residue +
M2-PRE; then Phase 6 (OCCURS DYNAMIC, TYPEDEF, floats) / 7 (2023 finalization).

