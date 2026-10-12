## Entry 632 — 2026-07-06 17:44 PDT — Phase 5: FUNCTION CONVERT (§15.19) — LANDED COMPLETELY (repertoire / hex / byte + the EC-DATA-CONVERSION leg)

CONVERT is the fourth and last keyword-bearing §15 string intrinsic — and the deepest: two keyword groups
after argument-1 (source-format ANY|ANUM|HEX|NAT, destination-format ANUM|NAT [HEX] | BYTE, §15.19.2), a
result category that depends on the destination (§15.19.1), conversions across three data repertoires, and a
NONFATAL EC-DATA-CONVERSION condition. Implemented in FULL — every conversion leg AND the EC leg (the design
recon workflow, wf_840f8070-fdf, produced the decision-complete design; anchors verified before implementing).

- **`BindConvert`** — argument-1 binds ordinarily; the two format keyword groups (bare `SUB_IDENTIFIER`
  segments, like TRIM's LEADING/TRAILING) parse to `BoundIntrinsicCall.ConvertSource`/`ConvertDest`/
  `ConvertDestHex`. The §15.19.3 syntax rules are enforced with a new **COBOLNET1514**: SR3 (source ≠ dest),
  SR8 (ANY ⇒ ANUM HEX / NAT HEX), SR9 (BYTE ⇒ HEX source), SR1 (non-empty argument-1). The result category
  (§15.19.1) is **National** for a NAT destination, Alphanumeric otherwise.
- **`CobolIntrinsics.Convert(arg, src, dst, dstHex)`** — the char-set model is documented and complete: the
  alphanumeric coded set is **8-bit Latin-1** (code point == byte); national is **UTF-16BE**, one char/position
  (D-N1 / §8.1.2 NOTE 2). Repertoire translation (ANUM↔NAT, r1/r3); byte/bit pathway (ANUM/ANY/HEX/NAT → bytes →
  ANUM HEX (r2) / NAT HEX (r4) / ANUM (r1) / NAT (r3) / BYTE (r5)); an untranslatable character yields the
  implementor-defined substitution char `'?'` and sets EC-DATA-CONVERSION. **Source ANY = the item's canonical
  display image** (typed-native model — §8.1.2 NOTE 2 leaves usage representation implementor-defined; always
  paired with a HEX destination per SR8), so the spec's NOTE 3(c) `"E0"` hardware bit-packing is deliberately
  NOT reproduced (documented, not residue).
- **National-result routing** — two `OperandText` arms (AsString / IsString) now accept a National-category
  intrinsic result (national is string-stored, so its image IS the char string) — forward-compatible with the
  deferred NATIONAL-OF/CHAR-NATIONAL.
- **The EC-DATA-CONVERSION leg (nonfatal, §15.19.4 r1/r3) — BUILT, not staged.** A new `DataConversionChecking`
  ambient statement gate + `DataConversionError` in `ExceptionState` (the nonfatal twin of the
  `ArgumentFunctionChecking` gate — singular pattern); the binder adds it to any intrinsic-bearing statement
  under `>>TURN EC-DATA-CONVERSION CHECKING ON`; the emitter wraps the statement with a set/finally-reset gate
  (`EcEmitChecked` refactored: `EcEmitArgOrPlain` extracted, the nonfatal gate wraps it — no catch, EC-DATA-
  CONVERSION never throws). Verified END TO END: a `>>TURN … ON` program converting an untranslatable national
  char reports `EC-DATA-CONVERSION` via FUNCTION EXCEPTION-STATUS.

Golden `tests/conformance/2023/intrinsics_convert.cob` (ANUM↔HEX / HEX→BYTE / NAT→ANUM HEX / the ANUM→NAT→ANUM
round-trip), **GreenfieldOnly**. +11 `IntrinsicFunctionDifferentialTests` (five format cases, the round-trip,
three SR violations → 1514, the 2014 gate → 1502, and the EC-DATA-CONVERSION status verification). Battery:
**1909 conformance (+11) · 216 unit · 113 corpus goldens GREEN**; greenfield-only. Phase-5 intrinsics live:
CONCAT/BASECONVERT (628), TRIM (629), FIND-STRING (630), SUBSTITUTE (631), CONVERT (632); residue =
MODULE-NAME/SMALLEST-ALGEBRAIC + the 2014 date family, each to be done in full when reached.

