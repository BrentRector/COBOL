## Entry 1322 — 2026-08-18 14:42 PDT — PB66: the floating-point numeric-edited PICTURE (symbol E) is LIVE — plus PB97 (the VALUE literal's FORM, and the numeric-edited level-88 that was silently false) and PB98 (`1,5E+3` under DECIMAL-POINT IS COMMA)

**What landed.** kb/Work **PB66** (feature, data division): `01 E1 PIC -9.9(5)E+99.` used to reject with COBOLNET0899
"recognized but not yet implemented" and the two HIGHEST/LOWEST-ALGEBRAIC returned-value rows (RV-15.43.4-1 /
RV-15.58.4-1) sat NOT-IMPLEMENTED behind it. The form is now a first-class numeric-edited category — data-model
design **D21** in `COBOLNET_DATA_MODEL_DESIGN.md` adopts (and re-derives, `cite.py --check` on every clause) the
2026-08-09 agent draft. Two defects found on the way outrank a feature and landed in the same change set: **PB97**
(a floating-point literal in a VALUE clause on a fixed-point numeric item CRASHED the backend with CS0595; a
numeric-edited conditional variable's level-88 numeric VALUEs compared the raw literal text against the edited image,
so `88 IS-TEN VALUE 10.` on `PIC ZZ9.99` was NEVER true; the SR6 form rule and the "no truncation of digits or sign"
rule for numeric-edited VALUEs were not checked at all) and **PB98** (under DECIMAL-POINT IS COMMA the floating-point
literal `1,5E+3` was a parse error in a statement and silently seeded 1 in a VALUE clause).

**The spec, derived first.** §13.18.40.4 GR13 b) defines the form — a significand character-string and an
exponent `+9{1..4}` joined by E; §13.18.40.6 Table 10 (rendered from PDF pages 489–490 to confirm the OCR) admits
before the E only `B 0 / , .`, a leading fixed `+`/`-`, and `9` — so V, P, S, Z, `*`, CR, DB and the currency symbol
are illegal in the significand (my first probe used `9V99E+99`, and so did the old skeleton row; both were
Table-10-illegal). §14.6.8.4: the store normalizes the significand so its most significant digit is nonzero, then
aligns and truncates per §13.18.40; §13.18.40.5 r8: a zero value zeroes every digit position with positive signs.
§14.9.25.4 GR6 item 4: overflow → EC-DATA-OVERFLOW (content undefined), underflow → zero. §14.6.13.2 rule 4: a
de-editing MOVE from content no editing could produce → EC-DATA-INCOMPATIBLE. §14.7.5 items 3 and 4: an
arithmetic resultant further from OR nearer to zero than permitted is the size error condition (receiver unchanged).
§13.18.63.3 SR6: numeric VALUE literals convert per the MOVE rules "such that no truncation of digits or sign is
required" — in formats 1, 2 AND 4 — fixed-point literals for fixed-point formats, floating-point literals for
floating-point formats, ZERO / the literal zero for either; SR3: a signed literal needs a sign representation.
§8.3.3.3.3 GR5: a floating-point literal's value is significand × 10^exponent — exact. §12.3.7.4 GR14 a): under
DECIMAL-POINT IS COMMA the comma is the literal's decimal separator. §15.43.4 r1 / §15.58.4 r1: the algebraic
functions' argument shall be such that its extreme passes IN-ARITHMETIC-RANGE (§8.8.4.4.4 GR3 l); §15.58.4 r2:
LOWEST of an unsigned picture is zero.

**As built.**
- *Runtime* (`CobolEdit.Float.cs`, `CobolEdit` is now partial): `FloatMask.Parse` — the ONE parser of the form;
  `FormatFloatMove` over an Int128+scale / `CobolDec` / `double` sender (normalize → truncate → image; overflow →
  `ExceptionState.FloatOverflowError` and, unchecked, the pinned saturated image; underflow → the zero image; BLANK
  WHEN ZERO); `TryFormatFloat` (the arithmetic store — false both ways out of range); `DeEditFloat` → an exact
  `CobolDec`, raising the new EC-DATA-INCOMPATIBLE (`CheckingFlags.DataIncompatible`, `ExceptionState
  .DataIncompatibleError`, a FATAL ambient gate in `EcEmitter`, enabled by `EcBinder` on any MOVE). Rule 4's
  sibling: the FIXED-POINT `DeEdit` never raised it either — under checking it now verifies the round trip
  `Format(DeEdit(image)) == image` (Format IS the one editor, so the test is exact; the emitter passes the item's
  BLANK WHEN ZERO so an all-spaces zero passes) and raises before any receiver is written.
- *Analyzer*: `PictureAnalyzer.AnalyzeFloatEdited` validates the two strings separately (one E, `+9{1..4}`, no
  EDITING phrase, a per-symbol COBOLNET1658 message; a SIGN clause on the entry is 1658 too, §13.18.52.3 SR1) and
  returns a numeric-edited `PicInfo` with `IsFloatEdited` — the SOLE discriminator (`Scale` 0, `DigitPositions`
  0; SR14's 1..31 cap does not apply, SR15's 1..36 significand digits does). The 2002 gate keys on the flag through
  the ONE `VersionConformancePass.PictureConstructId` — the forest, report printables and the report SUM-counter
  Analyze (`ReportSumModel.SkeletonGate`) alike; `SkeletonGate` now carries only the national-edited skeleton.
- *Dispatch* (D-EF5 — the form dispatch lives in ONE place per operation, never at call sites):
  `RuntimeApi.EditFormatFor` / `EditTryFormatFloat` / `EditComposeFloat`, `NumericRenderer`'s FieldNum float arm
  (`DeEditFloat` → a Dec-lane `NumX`). MOVE (numeric / edited / alphanumeric / FLOAT senders), ACCEPT, DISPLAY,
  STRING, arithmetic GIVING / COMPUTE (`ArithmeticEmitter`: SIZE ERROR both ways under the phrase or EC-SIZE, the
  MOVE disposition without), VALUE and level-88 (below), HIGHEST/LOWEST-ALGEBRAIC (`IntrinsicBinder
  .BindAlgebraicFold`: the all-nines significand at the maximum exponent, negated for LOWEST of a signed picture,
  "0" for LOWEST of an unsigned one; COBOLNET1660 when the extreme would fail IN-ARITHMETIC-RANGE for the mode in
  effect — binary64's ±308 natively, ±6144 under standard-decimal).
- *VALUE and level-88 — PB97's ONE mechanism.* `NumericLiteral.IsFloatingPointForm` / `ExpandFloatingPoint`
  (Frontend Common) give the exact fixed-point text of a floating-point literal. `DataBinder.ValidateValueCategory`
  — already the item-VALUE + level-88 funnel — decides the literal's FORM once: a floating-point literal on a
  fixed-point NUMERIC subject is rewritten to its exact value (SR2's range / exactness checks apply downstream, so
  `PIC 9V9(8) VALUE 1.234E-5` seeds 0.00001234 and `PIC 9(3) VALUE 12.5E+1` seeds 125); on a fixed-point
  numeric-edited subject it is COBOLNET1659; a nonzero fixed-point literal on a floating-point numeric-edited subject
  is COBOLNET1659 (one descriptor, both directions). `ValidateNumericValue` now serves the numeric-edited subject —
  digit positions (P excluded) at `CobolEdit.MaskScale`, sign-bearing = an editing sign symbol (for the floating
  form: the significand's own sign, never the exponent's `+`), and for the floating form the significand's digit run
  vs `SigDigits` and the normalized exponent vs `MaxExp` (COBOLNET1625). `ValueInitializer.EditedImageOfNumericValue`
  is the ONE compose of a numeric-edited item's numeric VALUE image (fixed → `EditCompose`, floating →
  `EditComposeFloat`, figurative ZERO at ≥ 2023 — VCR 35), and `ConditionRenderer.StringMembershipValue` calls it for
  a numeric-edited conditional variable, so a level-88's singletons and THRU ranges compare EDITED images. The VCR-86
  gate (a numeric-literal VALUE on a numeric-edited item is 2023) sees the floating form too.
- *PB98*: `fragment FLOAT_COMMA_BODY` with the full twin set the `SignedLiteralShapeDriftTests` scrape demands
  (`COMMA_FLOATLIT`, `FN_SIGNED_COMMA_FLOATLIT`, `SUB_SIGNED_COMMA_FLOATLIT` / `SUB_COMMA_FLOATLIT`), joined to
  `numericLiteralCore` / `subToken`, known to `IsBoolOperandTerm` / `retryPhraseAhead` / the §7.3.3 SR10 directive
  bar; `NormalizeIfNumericLiteral` admits the E-form so the significand normalizes; without DECIMAL-POINT IS COMMA
  the token is COBOLNET0895 (no dot-mode program legally spells `<digits>,<digits>E<exp>`).
- Registry row `pic-external-float-2002` → active (`PIC +9.99E+99`); the E symbol left the DataSkeleton /
  RepositoryPrototype / LoudGuard skeleton pins for positive facts. Goldens `2023/pb66_float_edited_picture`,
  `pb66_float_edited_comma_mode`, `pb66_float_edited_algebraic_and_checking`,
  `pb97_value_literal_form_and_88_edited`; negatives `pb66-float-edited-at-85`, `pb66-float-edited-picture-symbols`,
  `pb66-value-form-float-edited`, `pb66-value-float-edited-truncation`, `pb66-algebraic-float-edited-range`,
  `pb97-value-form-fixed-edited`, `pb97-value-edited-truncation`, `pb98-comma-float-literal-without-comma-mode`.
  CONFORMANCE.md §3: the pinned dispositions (overflow unchecked = the saturated image, checked = the receiver
  unchanged; unchecked incompatible content reads digit-for-digit with a non-digit as zero). Codes 1658–1660
  claimed; DIAGNOSTICS.md regenerated.

**Missteps, recorded.** (1) The apply script's idempotency test (`old` absent AND `new` present) is blind when
`new` CONTAINS `old` — re-running it after an anchor failure double-applied three patches (duplicate members,
CS0111 / CS0128); the recovery removed the second copies. (2) The Bash tool's heredoc mangles `\\n`, `\\'` AND `\\0`
— a `'\0'` char literal became a raw NUL byte in DataBinder.cs, and the sweep found TWO more already committed in
`FigurativeConstants.cs` (git had been treating the file as binary; both are `'\0'` now). Every scripted patch is
now written with the Write tool. (3) Two expectations in my first probe were mine, not the spec's — `9V99E+99` is
Table-10-illegal, and the DISPLAY of a signed DISPLAY item is its overpunched image — the probe corrected me, not
the compiler. (4) The design's E-picture citation was inherited as "§13.18.40.3 SR13 b)"; `cite.py --check` puts it
at §13.18.40.4 GR13 b) and the zero rule at §13.18.40.5 r8 — corrected in every new comment, golden and note.

**Verdicts.** RV-15.43.4-1 / RV-15.58.4-1 NOT-IMPLEMENTED → CONFORMS; GR-13.18.40.4-13, SR-13.18.40.3-15/-23,
GR-14.6.8.4-1/-2, GR-14.9.25.4-5 / -L2.4, GR-14.6.13.2-4, SR-13.18.63.3-3/-6, GR-14.7.5-3/-4 → CONFORMS: GAP 3954 →
**3940**.

**Gate.** Wave-local: characterization 33/33; unit (literal / lexer / catalog / registry / LoudGuard drift +
the traceability inventory) 146/146; conformance filtered to the corpus runner + manifests + version matrix +
skeleton / prototype / picture / edited / VCR drift **2907/2907** (10 m 41 s). Battery #18 follows this commit.

