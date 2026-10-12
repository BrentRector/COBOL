## Entry 882 — 2026-07-17 17:06 PDT — PHASE-12 wave 3 — the IEEE-754 float USAGE family (§13.18.60.4 GR14-18, COBOL-2014); the fidelity inversion corrected

The COBOL-2014 IEEE interchange float family lands — and with it the spec-faithfulness correction the P12
re-scout forced. **The plan premised "backing FLOAT-BINARY-128 by `double` … is a conforming implementor choice
per §13.18.60.4 GR13."** Re-checked directly against the spec: GR13/GR21 make ONLY the FLOAT-SHORT/LONG/EXTENDED
trio implementor-defined; **GR14-18 PIN** FLOAT-BINARY-32/64/128 to ISO/IEC 60559:2020 binary32/64/128 and
FLOAT-DECIMAL-16/34 to decimal64/128. A `double`/`System.Decimal` backing of a pinned format is NON-conforming
(the CONCATENATE-class catch — a plan premise that contradicts the spec).

**The honest split implemented (spec-first):**
- **FLOAT-BINARY-32 → native `float`, FLOAT-BINARY-64 → native `double`** — the pinned IEEE interchange formats
  map EXACTLY, so these are conforming and LIVE. `Usage.FloatBinary32/64`, `IsFloat`/`IsSingle`/`ClrType`/
  `DefaultInitializer` arms, the picture-less float synthesis (`PicInfo.FloatItem`), and the runtime `CobolFloat`
  path all extend the FLOAT-SHORT trio. Verified: `10.5*2 → 00021` (binary32), `100.25*4 → 00401` (binary64),
  `10.5+100.25 → 00110` (mixed, truncated).
- **FLOAT-BINARY-128 / FLOAT-DECIMAL-16 / FLOAT-DECIMAL-34 → processor-dependent NON-support** (Annex A.3 items
  17/19): .NET has no IEEE binary128 or IEEE decimal64/128 type, and GR16-18 pin the formats — so `PictureAnalyzer.
  ParseUsage` rejects them LOUD with **COBOLNET1564** ("processor-dependent language element not supported …
  Annex A.3"), never a silent non-conforming approximation. The `Usage` members exist so the emit path has a valid
  synthesized Pic (no NRE on the errored compile) and the switches stay exhaustive.

**Grammar:** dedicated hyphenated tokens `FLOAT_BINARY_32/64/128`, `FLOAT_DECIMAL_16/34` (before IDENTIFIER — the
scout catch: `FLOAT-BINARY-32` lexes as ONE IDENTIFIER by maximal-munch otherwise, so the plan's
`FLOAT_BINARY integerLiteral?` primary plan could not work; the fallback was mandatory) + the five `usageKeyword` /
bare-`usageClause` alternatives. ANTLR regen clean (0 warnings). All five words were already in reserved-words.json;
the drift tests stay green.

**Matrix:** `usage-float-binary32-2014` flipped pending→active (+ the missing `expectDiagnostic` field), new active
`usage-float-binary64-2014`; new PENDING rows `usage-float-binary128-2014` / `usage-float-decimal16-2014` /
`usage-float-decimal34-2014` catalogue the processor-dependent non-support (they would activate if the pinned format
is ever implemented). The introduction gate (0900 below 2014) fires from `UsageConstructId` for binary32/64; the
non-support forms get 1564 at every edition (a redundant 0900 would only add noise).

**AI misstep caught by a CLI spot-run (feedback_transparency):** the first build compiled but `MOVE 10.5 TO WS-B32`
NRE'd in `MoveEmitter.ConvertSource` — FLOAT-BINARY-* were absent from DataBinder's picture-less float-synthesis
list (lines the FLOAT-SHORT trio owns), so `target.Pic` was null. Added all five to the synthesis + the
picture-prohibition (§13.18.60.2 → COBOLNET1521). This is why every wave RUNS a program, not just compiles it
(feedback_verify_demo_output).

**Deferred (documented residue):** the external-float `E`-symbol PICTURE (§13.18.40.4 GR13b, floating-point
numeric-edited) stays staged LOUD at COBOLNET0899 — it is a separable editing feature (rendering a float into a
`+9.99E+99` mask), not silently wrong, and does not block the float-USAGE scope. Scheduled as a follow-up increment.

Tests: `tests/conformance/2014/float_binary` (byte-compared, binary32/64 arithmetic; GreenfieldOnly — the frozen
legacy grammar has no FLOAT-BINARY-* tokens); `FloatFamilyTests` (10: binary32/64 positives + the three 1564
non-support rejects + the 4-edition gate + the §13.18.60.2 picture prohibition). Full battery green + FULL legacy
guard (NIST 353 MATCH — grammar changed). Next: wave 4 = the pointer residues (Step 8).

