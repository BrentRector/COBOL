> # ADOPTED — data-model design D21 (`docs/COBOLNET_DATA_MODEL_DESIGN.md`) is the SSOT since 2026-08-18 (DEVLOG 1322).
> This file is the historical draft D21 re-derived from (every clause re-checked with `cite.py --check`; two
> decisions changed at adoption — no per-item `FloatEditSpec` projection / static mask field, `IsFloatEdited` alone
> discriminates and `CobolEdit.FloatMask.Parse` is the one parser). Where this draft and D21 differ, D21 governs.
>
> # UNREVIEWED AGENT DRAFT — NOT A DESIGN SSOT (frozen 2026-08-09)
> Produced by the wf_fdd1492c probe-sweep fleet's PB66 design agent, unreviewed by the main line and
> NOT adopted. kb/Work/PB66.md owns the item; docs/COBOLNET_DESIGN.md §0.5 does NOT list this file.
> The draft's terminology finding stands on its own citations (the 2023 standard calls the category a
> FLOATING-POINT NUMERIC-EDITED item, §13.18.40.4 GR13 b — "external floating-point" appears nowhere
> in the spec) but every claim must be re-derived at adoption (cite.py --check).

# DESIGN — External floating-point PICTURE (floating-point numeric-edited data)

Status: **DESIGN — DRAFT, nothing implemented.** The construct is recognized and staged loud today
(`COBOLNET0899` at ≥2002, `COBOLNET0900` below) — see §2 for the literal probe evidence. This document is the
decision-complete target for the whole feature; it is the design-doc-first prerequisite `kb/Work/PB66.md`
demands ("implementing external float PICTURE … is its own feature wave with a design-doc section first").

Scope: the `E`-symbol PICTURE character-string of §13.18.40 — **PICTURE analysis · storage model · editing (store)
· de-editing (read) · MOVE and arithmetic-store semantics · SIGN / DECIMAL-POINT IS COMMA / CURRENCY SIGN /
BLANK WHEN ZERO / VALUE / INITIALIZE interactions · `FUNCTION HIGHEST-ALGEBRAIC` and `FUNCTION LOWEST-ALGEBRAIC`
· edition gating · diagnostics · the test plan.** Files: `src/Cobol.Net.Compiler/Binding/PictureAnalyzer.cs`,
`Binding/Model/PicInfo.cs`, `Binding/DataBinder.cs`, `Binding/Procedure/Verbs/IntrinsicBinder.cs`,
`CodeGen/Roslyn/RuntimeApi.cs`, `CodeGen/Emit/NumericRenderer.cs`, `CodeGen/Verbs/{Move,Arithmetic,AcceptDisplay,
String}Emitter.cs`, `CodeGen/DataDivision/ValueInitializer.cs`, `src/Cobol.Net.Runtime/Values/Numeric/CobolEdit.cs`.

Companion docs (this doc defers to them and must be merged into them on landing):
`docs/COBOLNET_DATA_MODEL_DESIGN.md` (the category/storage register — PB66 names it as the owning doc),
`docs/COBOLNET_NUMERIC_DESIGN.md` (D1 the Int128 engine, D3 the arithmetic modes, D7/D16/D18 the float lane),
`docs/rearchitecture/DESIGN-version-conformance-pipeline.md` (the one edition gate),
`docs/rearchitecture/DESIGN-edition-framework.md` (`constructs.json` rows).

> **The standard has no term "external floating-point".** It calls this construct a **floating-point
> numeric-edited item** (§13.18.40.4 GR13 b) and never uses "external floating-point" anywhere in the 2023 text
> (`grep` over `specs/ISO_COBOL.md`: 0 occurrences). Every diagnostic, comment, doc line and `constructs.json`
> field this wave touches adopts the standard's term; "external floating-point" survives only as a
> parenthetical in the `PB66` note and in the legacy diagnostic string that §12 replaces.

---

## 0. Hard invariants this design upholds

1. **Typed-native only.** The item's storage is a .NET `string` holding its character image — the SAME substrate
   every other numeric-edited item already uses (`PicInfo.ClrType`: `PicCategory.NumericEdited => "string"`).
   No byte substrate, no new CLR carrier for storage. §8.5.2.1 Table 2 makes a display-usage numeric-edited item
   **class alphanumeric**, so the string carrier is not an implementation convenience — it is the category.
   ✔ `cite.py --check 8.5.2.1 "The category of an elementary data item depends upon its description"` → OK.
2. **Spec-first, and every clause number re-derived.** Every semantic claim below carries a clause validated with
   `python scripts/spec/cite.py --check`; §16 is the ledger with the verdict for each. The **Table 10 row for
   `E` was re-derived from the RENDERED PDF page** (`scripts/render-spec-page.py 490`, printed folio 460) because
   a 24×24 OCR'd grid is exactly the artifact CLAUDE.md rule 1 warns about — the rendered row agrees with the
   Markdown transcription cell-for-cell (§1.2).
3. **One mechanism per job.** The floating-point form is a second ARM of the existing edited-store /
   edited-read dispatch, never a second pipeline: the arm lives inside `RuntimeApi.EditFormat` /
   `RuntimeApi.EditTryFormat` and inside `NumericRenderer.FieldNum`'s single edited branch (§5.3), so no call
   site can forget it. The value carrier is the EXISTING `CobolDec` (`NumX.Dec` lane), not a new struct.
4. **No deferral.** Every rule the standard states for this construct is implemented in this wave, including the
   4-digit exponent and the 36-digit significand (§3, D-EF2 proves the Annex A.3 latitude does **not** apply to
   this implementation).
5. **Four editions in one.** The construct is introduced at COBOL-2002; the §15.43.4/§15.58.4 well-formedness
   rule is stated in COBOL-2014 wording. §11 gives the per-edition behaviour and the gating diagnostic.

---

## 1. What the standard specifies

### 1.1 The construct

A **floating-point numeric-edited item** is defined by §13.18.40.4 GR13 b):

> "To define a floating-point numeric-edited item, characters-string-1 shall consist of two parts, separated
> without any spaces, by the symbol 'E'. The first part represents the significand; the second part represents
> the exponent."
> "The significand shall be a valid character-string for either a numeric item or a numeric-edited item for a
> fixed-point result. Neither floating insertion editing nor zero suppression with replacement shall be
> specified for the significand."
> "The exponent shall be '+9', '+99', '+999', '+9999', or '+9(n)' where n = 1, 2, 3, or 4."

✔ all three `--check 13.18.40.4` → OK, §13.18.40.4 GR13 (the printed sub-label is b); `cite.py`'s rule path is
approximate below two levels and prints `13)`).

Its **category is numeric-edited** (§13.18.40.4 GR3 lists the eight categories a PICTURE clause may define;
§8.5.2.13 r1: "A data item described as numeric-edited by its PICTURE character-string"). Its **class is
alphanumeric** when its usage is display and **national** when its usage is national (§8.5.2.1 Table 2).

`E` is a real character position: "The symbol 'E' represents a character position into which the character 'E'
will be inserted during editing. The symbol 'E' is counted in the size of the item." — §13.18.40.4 GR14.
✔ `--check 13.18.40.4 "The symbol 'E' is used to separate the significand and the exponent of a floating-point
numeric-edited item"` → OK.

### 1.2 The EXACT symbol alphabet — Table 10 row `E`, re-derived from the printed page

§13.18.40.3 SR2 makes the precedence tables normative for what is *allowable*: "The allowable combinations of
symbols for a PICTURE clause are specified in 13.18.40.6, Precedence rules." ✔ OK.
§13.18.40.6 then splits the string:

> "For the purposes of Table 10, character-string-1 for a numeric-edited item for a floating-point edited result
> is considered as two separate strings, the first of which begins with the first symbol and ends with the symbol
> 'E', and the second of which begins with the symbol 'E' and ends with the last symbol. The presence of symbols
> preceding the symbol 'E' has no effect on the validity of symbols following the symbol 'E'." ✔ OK.

**Table 10, row `E` (which First Symbols may precede `E`), read off the rendered PDF page 490 (folio 460) and
confirmed identical to the Markdown transcription:** `B 0 /` · `,` · `.` · `+ −` (the *leftmost fixed insertion*
column) · `9`. **Every other column is blank** — so `V`, `P`, `S`, `Z`, `*`, the currency symbol `cs`, the
floating `+ −`, `CR DB`, `A X`, `1`, `N` may **not** precede `E`.

**Table 10, row `+` (the exponent-only `+`, which the clause identifies as "The symbol '+' that appears in a
column and in a row by itself, represents its use in the exponent part of character-string-1 for a floating-point
numeric-edited item" ✔ OK): the ONLY `x` is in column `E`.** And row `9` carries an `x` in the exponent-`+`
column. So the exponent is exactly `E` `+` `9`{1..4} — the general rule and the precedence table agree.

Consequences the analyzer enforces directly (§4):

| Symbol | In a floating-point numeric-edited character-string |
|---|---|
| `9` | the significand's and the exponent's digit positions |
| `.` | the significand's decimal point — **special insertion, a real character position** (§13.18.40.4 GR14) |
| `,` `B` `0` `/` | simple insertion inside the significand |
| leading `+` / `-` | the significand's fixed-insertion sign (Table 8) |
| `E` | exactly once (§13.18.40.3 SR12, the "If the FOR phrase is not specified" list, printed item b: "Each of the symbols from the set 'CR', 'DB', 'E', 'S', 'V' '.' may appear only once in character-string-1") ✔ OK |
| `V` | **illegal** — Table 10 row `E` has no `V` column, and `V` and `.` are mutually exclusive anyway (§13.18.40.3 SR20 ✔ OK). The significand's point is always the real `.` |
| `P` | **illegal** — Table 10 row `E`; also `P`/`.` mutually exclusive (SR17 ✔ OK) |
| `S` | **illegal** — Table 10 row `E`; SR18 also pins `S` to the first symbol (✔ OK) |
| `Z` `*` | **illegal** — GR13 b bans zero suppression with replacement in the significand |
| `cs` | **illegal** — Table 10 row `E` (and GR13 b bans floating insertion) |
| `CR` `DB` | **illegal** — Table 10 row `E` |
| character-1 (`EDITING`) | **illegal** — §13.18.40.6 gives `es` "the same precedence as the 'cs' symbol in the column and row of non-floating insertion symbols", and `cs` may not precede `E`; §13.18.40.3 SR12 a also states "Extended editing sign control symbols shall not be specified for a floating-point edited item" ✔ OK |

**Two general syntax rules whose literal wording the floating-point form contradicts, resolved here (not an
owner question — the standard resolves it itself):** SR25 ("The symbol '+' or the symbol '-', when used, shall be
either the leftmost or the rightmost symbol in character-string-1" ✔ OK) and SR24 ("only one … editing sign
control symbol may be used" ✔ OK) would both reject `-9.9(5)E+99`. **SR23 states the exception explicitly** —
"The editing sign control symbols '+', '-', 'CR', and 'DB' are mutually exclusive in character-string-1 **with
the exception of a numeric-edited data item for a floating-point edited result as described in General rule
13b**" ✔ OK — and its NOTE 3 says "the significand part of the character-string may contain a '-' symbol and the
exponent part always contains a '+' symbol" ✔ OK. Together with §13.18.40.6's two-strings sentence, SR24/SR25 are
applied **per string**: the significand's sign is leftmost of the significand, the exponent's `+` is leftmost of
the exponent. The analyzer therefore validates SR24/SR25 on each half, never on the whole.

### 1.3 Digit-position capacity — SR14 does NOT apply

- §13.18.40.3 SR14: "For data items of category numeric, **and for fixed-point data items of category
  numeric-edited**, the number of digit positions described by character-string-1 shall range from 1 through 31."
  ✔ OK — by its own wording this does not reach a floating-point numeric-edited item.
- §13.18.40.3 SR15: "For floating-point data items of category numeric-edited, the number of digit positions **in
  the significand** shall range from 1 through 36." ✔ OK.
- The exponent's width is bounded by GR13 b at 1–4 digits.
- §13.18.40.3 SR4 caps the whole character-string at 63 characters ✔ OK.

**This is a live defect the wave must fix:** `DataBinder.cs:2147` today routes every
`Category: Numeric or NumericEdited` item through `EditionContext.CheckDigitCapacity`, which emits
`COBOLNET0801` citing "31 digits (ISO §8.3.3.3.2)" (the message cited the phantom §8.3.1.2 until
kb/Work PB159/PB290 repaired it). A 36-digit significand is legal; the SR14 path must be
skipped for the float form and replaced with the SR15 + GR13 b pair (§4.3).

### 1.4 Editing (the store direction)

§13.18.40.5 Table 7 pins the editing types: "Numeric-edited (floating-point edited result) | **Simple insertion,
special insertion, and fixed insertion for the significand part / None for the exponent part**" ✔ OK.

§14.6.8.4 "Floating-point numeric-edited receiving data items" is the alignment rule:

> 1) "If the algebraic value of the sending operand is not zero, the exponent and significand of the value are
>    adjusted such that the most significant digit of the significand is not zero." ✔ OK
> 2) "Alignment and zero fill or truncation take place as described in the general rules and editing rules in
>    13.18.40, PICTURE clause." ✔ OK

**Normalization is to the mask, not to `d.ddd`.** GR1 says the most significant digit of *the significand* is
nonzero. For `PIC 9.9(5)E+99` (1 integer digit position) that means `[1.00000, 9.99999]`; for `PIC 99.9(4)E+99`
(2 integer digit positions) it means `[10.0000, 99.9999]`. The design's normalizer targets **the mask's own
integer-digit-position count**, which is the only reading that leaves the leading position nonzero.

§13.18.40.5 rule 8 pins zero: "If the value to be edited into a floating-point edited item is zero, then after
editing all digit positions of the significand and all digit positions of the exponent shall be zero; the sign of
the significand, if present, shall be positive; and the sign of the exponent shall be positive." ✔ OK.
Combined with Table 8 (fixed insertion), `PIC -9.9(5)E+99` at zero renders `" 0.00000E+00"` (a `-` symbol renders
a space for a positive/zero value) and `PIC +9.9(5)E+99` renders `"+0.00000E+00"`.

### 1.5 Out-of-range stores

MOVE (§14.9.25.4 GR6 item 4, whose antecedent is "If the receiving data item is described with a standard
floating-point usage **or is a floating-point numeric-edited item**" ✔ OK):

> a. "If the algebraic value of the sending operand is farther from zero than is permitted by the usage
>    specifications of the receiving data item, the EC-DATA-OVERFLOW exception condition is set to exist, and the
>    content of the receiving data item is undefined." ✔ OK
> b. "If the algebraic value of the sending operand is nearer to zero than is permitted by the data description of
>    the receiving operand, the numeric value is treated as zero." ✔ OK

Arithmetic (§14.7.5) is deliberately **different**, and the difference is a two-arm hazard the implementation must
carry: case 3 "if, after radix point alignment … the result of an arithmetic statement is further from zero than
permitted for the associated resultant data item" ✔ OK and case 4 "if the nonzero result … is nearer to zero than
permitted for the associated resultant data item" ✔ OK — **both** raise the size error condition. So MOVE treats
underflow as zero while `ADD … GIVING` raises SIZE ERROR on the same value. §6.4 wires this.

### 1.6 De-editing (the read direction)

§14.9.25.4 GR5: "De-editing takes place only when the sending operand is a numeric-edited data item and the
receiving item is a numeric or a numeric-edited data item." ✔ OK. §3.56 defines de-editing as the "logical removal
of all editing characters from a numeric-edited data item in order to determine that item's numeric value".

§14.6.13.2 rule 4: "When a numeric-edited data item is the sending operand of a de-editing MOVE statement and the
content of that data item is not a possible result for any editing operation in that data item, the result of the
MOVE operation is undefined and an EC-DATA-INCOMPATIBLE exception condition is set to exist." ✔ OK.

A floating-point edited item is **class alphanumeric**, so it is *not* admitted as an arithmetic sending operand
(§14.9.2.3 SR2 "Identifier-1 and identifier-2 shall reference numeric data items" ✔ OK; §8.8.1.1 admits only a
numeric data item in an arithmetic expression) and it compares as an alphanumeric operand
(§8.8.4.2.3 SR2 ✔ OK — class alphanumeric; §8.8.4.2.4 governs only "operands whose class is numeric" ✔ OK).
**Therefore the only runtime path that reads its VALUE is the de-editing MOVE** — a fact §5.4 exploits.

### 1.7 Where it may appear as a receiver

§14.9.2.3 SR4 "Identifier-3 shall reference a numeric data item or a numeric-edited data item" ✔ OK — the GIVING
receiver of every arithmetic statement, and by the same shape the COMPUTE receiver. Plus MOVE (Table 16 row
"Numeric-edited" column "Numeric, Numeric-edited": Yes), `ACCEPT`, `STRING … INTO`, `INITIALIZE`, `VALUE`.

---

## 2. Current state — measured, not assumed

`PictureAnalyzer.Analyze` scans for `E`, fires `StagedNotImplemented(edition, Constructs.PicExternalFloat2002,
"Phase 6", where)` and returns `PicInfo.Recovery(len) with { SkeletonGate = Constructs.PicExternalFloat2002 }`
(`PictureAnalyzer.cs:98,105-123`). The recovery erases the category to `Alphanumeric`, which is why the
downstream errors are category errors.

**Probe `pb66d01.cob` (`PROGRAM-ID. PB66D01`), default `--std` (2023):**

```
  error COBOLNET0899: an external floating-point PICTURE (symbol E) is recognized but not yet implemented (owning roadmap phase: Phase 6) - data item 'W-EF' (ISO 13.18.40 external float; skeleton W2 (loud), full Phase 6 (IEEE float catchall); PENDING)
  error COBOLNET0819: MOVE . TO W-EF: MOVE is invalid - a noninteger numeric sending operand does not move to an alphabetic, alphanumeric or alphanumeric-edited receiver (ISO 14.9.25.3 SR10, Table 16)
```

**Same program at `--std 85` — the introduction gate fires instead:**

```
  error COBOLNET0819: MOVE . TO W-EF: MOVE is invalid - a noninteger numeric sending operand does not move to an alphabetic, alphanumeric or alphanumeric-edited receiver (ISO 14.9.25.3 SR10, Table 16)
  error COBOLNET0900: an external floating-point PICTURE (symbol E) requires COBOL-2002 (targeting COBOL-85) - data item 'W-EF' (ISO 13.18.40 external float; skeleton W2 (loud), full Phase 6 (IEEE float catchall); PENDING)
```

**Probe `pb66d02.cob` (`PROGRAM-ID. PB66D02`) — the two inventory rows this note owns:**

```
  error COBOLNET0899: an external floating-point PICTURE (symbol E) is recognized but not yet implemented (owning roadmap phase: Phase 6) - data item 'W-EF' (ISO 13.18.40 external float; skeleton W2 (loud), full Phase 6 (IEEE float catchall); PENDING)
  error COBOLNET1516: FUNCTION HIGHEST-ALGEBRAIC argument-1 shall be a category numeric or numeric-edited DATA ITEM - not a literal, an arithmetic expression, a group item, a reference-modified item, an index, or another function (ISO 15.43.3 rule 1)
  error COBOLNET1516: FUNCTION LOWEST-ALGEBRAIC argument-1 shall be a category numeric or numeric-edited DATA ITEM - not a literal, an arithmetic expression, a group item, a reference-modified item, an index, or another function (ISO 15.58.3 rule 1)
```

**Probe `pb66d05.cob` (`PROGRAM-ID. PB66D05`) — eight different `E` masks, including `+9(2).9(3)E+9999`,
`999E+99`, `-9,9(5)E+99`, `$9.99E+99`, `ZZ9.99E+99`, `9V99E+99` — every one reaches `PictureAnalyzer` intact and
draws only the single `COBOLNET0899`.** ⇒ **the lexer and grammar need NO work**: `PIC_STRING` already carries
`+ - . , ( ) E` and the repetition factor. The whole feature is binder + emitter + runtime.

**Baseline the design must not regress — `pb66d04.cob` (`PROGRAM-ID. PB66D04`), the fixed-point fold over the
standard's own example `PIC $**,**9.99BCR` (§15.43.4 / §15.58.4 NOTE tables give +99999.99 / −99999.99):**

```
HI=[0000999999I]
LO=[0000999999R]
```

(`I` = trailing over-punch for `+9`, `R` = for `−9`; i.e. `+000099999.99` and `−000099999.99` — correct.)

**Floating-point NUMERIC LITERALS already work — `pb66d03.cob` (`PROGRAM-ID. PB66D03`):**

```
D=[1500]
E=[ 12.50]
```

(`01 W-D USAGE COMP-2 VALUE 1.5E+3.` ⇒ 1500.) So §13.18.63.3 SR6's "literals for floating-point formats shall be
specified as floating-point" has a working literal channel to build the VALUE rule on (§9.5).

**Inventory fan-out this wave unblocks** (`tests/version-matrix/traceability-inventory.json`): §13.18.40.3 — 37
rows, all GAP; §13.18.40.4 — 19 rows, all GAP; §13.18.40.5 — 15 rows, all GAP; §14.6.8.4 — 2 rows, both GAP with
a blank verdict; §15.43.4 r1 and §15.58.4 r1 — the two `NOT-IMPLEMENTED` rows PB66 owns.

**Metadata defect found while writing this design.** `tests/version-matrix/constructs.json`, row
`pic-external-float-2002`, carries `"source": "… 01 W-E PIC +9V99E+99. …"`. **That picture is illegal**: Table 10
row `E` has no `V` column (§1.2). When the row flips to `status: active` the gate program must become a legal
mask (`PIC +9.99E+99`), otherwise the edition-gate test would assert the right diagnostic for the wrong reason —
the `green_gates_arent_evidence` shape.

---

## 3. Decisions

### D-EF1. Storage is the existing numeric-edited `string` image; no new `PicCategory`, no new CLR carrier.

The item's category IS numeric-edited (§8.5.2.13 r1) and its class IS alphanumeric (§8.5.2.1 Table 2). Every
character-position surface already in the tree — `DataItem.ByteWidth`/`ImageWidth`, `GroupImageCodec`,
`RecordFraming`, reference modification, `INSPECT`, `STRING`, relation conditions — therefore works unchanged the
moment `PictureAnalyzer` returns `PicCategory.NumericEdited` with the right `Length`.

*Rejected:* (a) a new `PicCategory.FloatEdited` — REJECTED: it would fork ~30 `is PicCategory.NumericEdited`
tests (the grep in §14 lists them), every one of which is correct for both forms; the standard has one category
and so must the model. (b) storing a decoded `(significand, exponent)` pair and re-rendering on read — REJECTED:
it is a byte-substrate-shaped idea in disguise, it breaks `REDEFINES`/group-image identity, and §14.6.13.2 rule 4
*requires* that an arbitrary character image be representable so its de-edit can be diagnosed.

### D-EF2. The FULL range is implemented: significand 1–36 digit positions, exponent 1–4 digits.

Annex A.3 item 1 makes "the ability to specify a significand longer than 31 digits in the PICTURE character-string
associated with a floating-point numeric-edited data item" ✔ OK (A.3 1 c) and "…an exponent longer than 3
digits…" ✔ OK (A.3 1 d) processor-dependent — **but only "When no support for any of the features standard binary
floating-point usages, standard decimal floating-point usages, standard-binary arithmetic, and standard-decimal
arithmetic is provided."** WiseOwl COBOL provides `FLOAT-BINARY-32`/`FLOAT-BINARY-64` (LIVE, `PicInfo` Usage docs) and
implements `ARITHMETIC IS STANDARD-DECIMAL` (numeric design D3, `CobolDec`). **The antecedent is false, so the
latitude is not available and the full SR15/GR13 b range is mandatory.** This single conclusion is what forces
D-EF3: a 36-digit significand cannot round-trip through binary64.

### D-EF3. The VALUE channel is EXACT DECIMAL — the existing `CobolDec (Int128 Sig, int Exp)` and the existing `NumX.Dec` lane — never `double`.

`CobolDec` is `value = Sig × 10^Exp` with an `Int128` significand (38 digits ≥ the 36 SR15 permits) and an `int`
exponent (≫ the ±9999 GR13 b permits). It already exists, it already has `From(unscaled, scale)`,
`ToUnscaled(scale, mode)` (high-order-truncating, rounding-mode aware) and `Compare`, and the emitter already has
a lane for it: `NumX(Expr, Scale, Dec: true)` — "`Scale` is then meaningless (the SDIDI carries its own
exponent)", which is precisely a floating-point-edited item's situation.

**The one thing this widens, and it must be widened in the same change set (CLAUDE.md rule 6):** `NumX.Dec`'s
doc-comment today says "a STANDARD-DECIMAL intermediate (§8.8.1.5)". After this wave it means "a `CobolDec`-carried
exact decimal value with its own exponent — a standard-decimal intermediate (§8.8.1.5) **or** the de-edited value
of a floating-point numeric-edited item (§14.9.25.4 GR5)". The 34-digit clamping is a property of `CobolDec`'s
ARITHMETIC operations (`Round34Wide`), not of its representation, so the reuse is honest; `CobolDec.From`'s own
comment ("≤31 digits always fits the 34-digit significand") is likewise about arithmetic entry and gets a
sentence noting the exact-representation use.

*Rejected:* (a) `double` — REJECTED by D-EF2: binary64 carries ~17 significant digits and the standard demands 36;
`MOVE` of a `PIC S9(31)` sender into `PIC 9.9(30)E+99` would silently lose 14 digits. (b) a new
`readonly record struct FloatEditValue` — REJECTED: `feedback_one_mechanism_per_job`; it would be `CobolDec` with
a different name, and the `NumX` lane would have to be doubled. (c) `decimal`/`BigInteger` — owner-locked out
(numeric design D1).

### D-EF4. `PicInfo` gains ONE nullable projection, `FloatEdit`, and it is the sole discriminator.

```csharp
/// <summary>The parsed two-part structure of a FLOATING-POINT numeric-edited PICTURE (ISO §13.18.40.4 GR13 b) —
/// non-null exactly for that form, null for every fixed-point numeric-edited item.</summary>
public sealed record FloatEditSpec(
    string SigMask,      // the significand's expanded character-string, sign symbol included
    int    SigDigits,    // '9' positions in the significand — §13.18.40.3 SR15, 1..36
    int    SigScale,     // '9' positions right of the '.' (0 when there is no '.')
    bool   SigSigned,    // a leading '+' or '-' fixed-insertion symbol is present
    char   SigSign,      // '+' or '-' or '\0'
    int    ExpDigits);   // '9' positions after "E+" — §13.18.40.4 GR13 b, 1..4

public FloatEditSpec? FloatEdit { get; init; }
public bool IsFloatEdited => FloatEdit is not null;
```

`Scale` and `DigitPositions` are set to **0** on a float-edited `PicInfo` and are never consulted for it — the
scale of a floating-point item is a runtime property. `EditMask` keeps the FULL expanded character-string so
every width/length/image consumer stays correct; the FORM dispatch is `IsFloatEdited`, never a re-scan of the
mask for `'E'` at a call site.

### D-EF5. The form dispatch lives in the FOUR existing choke points, not at the call sites.

`feedback_change_the_dispatch_not_the_callers` + `feedback_two_arm_dispatch`. The five edited-store call sites
(`MoveEmitter.cs:331,337`, `ArithmeticEmitter.cs:303,307`, `AcceptDisplayEmitter.cs:181`, `StringEmitter.cs:231`)
call `RuntimeApi.EditFormat` / `RuntimeApi.EditTryFormat`; the one edited-read site is
`NumericRenderer.FieldNum`. **Change those three helpers' signatures to take the `PicInfo` (today they take a raw
mask string) and dispatch on `IsFloatEdited` inside.** A future third form is then one arm in one place, and
`RuntimeApi.EditFormat` becomes the single place that can be wrong.

*Rejected:* adding `if (pic.IsFloatEdited) …` at each of the six sites — REJECTED: that is the five-times-repeated
rule the `one_rule_one_place` feedback names, and the sixth site (added next year) would silently take the
fixed-point arm and produce a fixed-point image for a float mask — a WRONG ANSWER with no diagnostic.

### D-EF6. The runtime gains a parsed-mask value type `CobolEdit.FloatMask`, emitted once per item as a `static readonly` field.

```csharp
public readonly record struct FloatMask(string SigPattern, int SigDigits, int SigScale,
                                        char SigSign, int ExpDigits)
{ public static FloatMask Parse(string picture); }
```

The emitter emits `private static readonly CobolEdit.FloatMask __fm_<item> = CobolEdit.FloatMask.Parse("…");`
beside the existing per-item `NumProfile` field (`RecordStructEmitter.EmitProfiles`) and passes `__fm_<item>` at
each store/read. This is the shape the fixed-point `CobolEdit.Format` *should* have — it re-scans the mask string
on every single call today — and migrating `Format`/`DeEdit`/`MaskCapacity`/`MaskScale` to the same cached-mask
convention is a named **performance** follow-on (the fourth required review dimension), not deferral debt: it
changes no behaviour and no diagnostic.

### D-EF7. Store = normalize exactly, then render; NO float ever appears on the store path.

Given a sending value already in the engine's exact form `(Int128 unscaled, int scale)` — every fixed-point
sender — the store is pure integer arithmetic (§6.2). Given a `double` sender (`COMP-1`/`COMP-2`/`FLOAT-*`), the
value enters through the existing `CobolDec.FromDouble` (shortest round-trip decimal, ≤17 digits, exact) — the
documented implementor conversion the standard-decimal path already uses.

### D-EF8. The §14.9.25.4 GR6 4a "undefined" content is PINNED to the saturated extreme image.

When normalization needs an exponent larger than the mask's exponent capacity, the standard sets
EC-DATA-OVERFLOW and leaves the content undefined. WiseOwl COBOL's pinned choice: **the maximum-magnitude image —
all-nines significand at the maximum exponent — carrying the value's sign** (the sign only where the mask has a
sign symbol). Deterministic, monotone in the input, and it can never manufacture a small plausible number from a
huge one. Documented in `docs/CONFORMANCE.md` beside the other undefined-behaviour determinations.
**⚠ This is an owner-ratifiable behaviour determination — see §15 Q1.**

### D-EF9. The §15.43.4/§15.58.4 well-formedness rule is enforced AT THE FUNCTION REFERENCE, keyed on the arithmetic mode in effect.

The rule constrains argument-1's *data description entry*, not the declaration in general, so it is a diagnostic
on the `FUNCTION HIGHEST-ALGEBRAIC`/`LOWEST-ALGEBRAIC` reference — the item stays legal to declare and to
MOVE through. §10 gives the arithmetic-mode-parameterised bound.

---

## 4. PICTURE analysis

### 4.1 Where

`PictureAnalyzer.Analyze`, in place of the current `if (hasE) StagedNotImplemented(…)` at `PictureAnalyzer.cs:107`
and the `SkeletonGate` recovery at `:122-123`. The `E` case is lifted OUT of the SR2 whitelist's
"legal-but-unimplemented" trio (`N`, `1`, `E`) — `N` and `1` keep their current handling.

### 4.2 The algorithm (on `expanded`, i.e. after `ExpandRepeats` has unrolled `(n)` and uppercased)

```
1.  count 'E'. 0 → not this form (fall through unchanged).
                >1 → COBOLNET1643 (§13.18.40.3 SR12 b: 'E' may appear only once).
2.  split at the single 'E' → sig, exp.
3.  exp must match  '+' '9'{1,4}                       else COBOLNET1644 (§13.18.40.4 GR13 b).
        ExpDigits = |'9's|.
4.  sig:
      a. optional leading '+' or '-'                    → SigSigned, SigSign
      b. the remainder over { '9','B','0','/',',','.' } — any other symbol → COBOLNET1645
         naming the offending symbol and Table 10 row 'E' (§13.18.40.6 / §13.18.40.3 SR2).
         Distinct message legs for V, P, S, Z, *, cs, CR/DB and an EDITING character-1,
         each citing the rule that bans it (§12) — never one generic "invalid symbol".
      c. at most one '.'                                else COBOLNET1643 (SR12 b, symbol '.').
      d. at least one '9'                               else COBOLNET1646 (SR15: 1..36).
      e. SigDigits = |'9's| ; 1 <= SigDigits <= 36      else COBOLNET1646 (SR15).
      f. SigScale  = |'9's right of the '.'|  (0 if none).
      g. Table 10 order among the retained symbols is checked by the SAME table walk the
         fixed-point precedence validator uses, run on `sig` alone (§13.18.40.6's two-strings
         sentence). The exponent needs no walk — step 3 is exact.
5.  SR24/SR25 (one sign symbol, leftmost-or-rightmost) are applied PER STRING (§1.2), which
    steps 3 and 4a already satisfy by construction.
6.  Length = expanded.Length   (every symbol of both parts is a character position:
    §13.18.40.4 GR14 for 'E', '.', ',', 'B', '0', '/', '+', '-', '9'; 'V' and 'P' cannot occur).
    SR4: Length of the SOURCE character-string <= 63.
7.  return new PicInfo(PicCategory.NumericEdited, usage, Length, Digits: SigDigits,
                       Scale: 0, Signed: SigSigned)
        { EditMask = expanded, DigitPositions = 0,
          FloatEdit = new FloatEditSpec(sig, SigDigits, SigScale, SigSigned, SigSign, ExpDigits) };
```

### 4.3 The SR14 capacity guard must be bypassed

`DataBinder.cs:2147` becomes
`if (pic is { Category: …, IsFloat: false, IsFloatEdited: false } && pic.DigitPositions > 0)`.
The float form's capacity was already checked in step 4e/3 against SR15/GR13 b, which are its rules. Leaving the
current call in place would reject a legal 36-digit significand with a message citing §8.3.3.3.2 (the fixed-point clause) — the
`a_real_clause_can_answer_a_different_question` shape.

### 4.4 Usage

§13.18.40.4 GR1 makes each symbol a national character position under `USAGE NATIONAL` and an alphanumeric one
under display. The national-form numeric/numeric-edited leg is **already staged loud** in `PictureAnalyzer`
(`edition.Error(DiagnosticCatalog.NationalData, …)` at `:210`) as Phase-4a residue; a float-edited picture under
`USAGE NATIONAL` inherits that existing stage unchanged and is out of this wave's scope by the SAME stage that
covers every other national-form numeric item. `USAGE BINARY`/`PACKED-DECIMAL`/`COMP-5` with a float-edited
picture take the existing §13.18.60.3 SR3 rejection at `:265` (an edited picture does not describe a numeric
item). No new usage handling is needed.

---

## 5. Storage and the two directions

### 5.1 The image

A `string` of exactly `Length` characters. Layout (a worked example, `PIC -9.9(5)E+99`, `Length` = 12):

```
 index : 0 1 2 3 4 5 6 7 8  9 10 11
 mask  : - 9 . 9 9 9 9 9 E  +  9  9
 value :   1 . 2 3 4 5 0 E  -  0  7      ← +1.23450 × 10^-7
 value : - 9 . 9 9 9 9 9 E  +  9  9      ← the LOWEST-ALGEBRAIC extreme
 value :   0 . 0 0 0 0 0 E  +  0  0      ← zero (§13.18.40.5 rule 8)
```

`DefaultInitializer` is unchanged — `new string(' ', Length)` (the numeric-edited initial state).

### 5.2 The two runtime entry points (new, in `CobolEdit`)

```csharp
public static string   FormatFloat  (Int128 value, int valueScale, in FloatMask m,
                                     bool blankWhenZero = false, bool commaMode = false,
                                     out FloatStoreOutcome outcome);
public static bool     TryFormatFloat(Int128 value, int valueScale, in FloatMask m,
                                      out string image, bool blankWhenZero = false,
                                      bool commaMode = false);
public static CobolDec DeEditFloat  (string image, in FloatMask m, bool commaMode = false);
```

`FloatStoreOutcome` ∈ `{ Ok, Overflow, Underflow }` carries §1.5's distinction to the caller so MOVE and
arithmetic can diverge (§6.4). `currency` is absent by construction — a currency symbol cannot occur in this form
(§1.2), and §12.3.7.3 SR22 b independently guarantees the currency symbol can never BE `E`
("alphabetic characters A, B, C, D, E, N, P, R, S, V, X, Z, or their lowercase equivalents; or the space" ✔ OK),
so the `E` scan can never collide with a program-defined currency symbol.

### 5.3 The dispatch (D-EF5) in one place

```csharp
// RuntimeApi.cs
public static string EditFormat(string value, string scale, PicInfo pic, string fieldMaskRef, string cfgArgs)
    => pic.IsFloatEdited
        ? $"CobolEdit.FormatFloat({value}, {scale}, in {fieldMaskRef}{cfgArgs}, out _)"
        : $"CobolEdit.Format({value}, {scale}, {CsLiteral(pic.EditMask!)}{cfgArgs})";
```

and symmetrically for `EditTryFormat`; `NumericRenderer.FieldNum`'s edited arm becomes

```csharp
p.Item.Pic is { Category: PicCategory.NumericEdited } pe
  ? pe.IsFloatEdited
      ? new NumX($"CobolEdit.DeEditFloat({read}, in {maskRef}{cfg})", 0, Dec: true)
      : new NumX($"CobolEdit.DeEdit({read}, {mask}{cfg}{edits})", CobolEdit.MaskScale(...))
  : …
```

### 5.4 Why the `Dec` lane's blast radius is small — measured, not assumed

A floating-point edited item's *value* is read on exactly one path: the de-editing MOVE (§1.6 — it is class
alphanumeric, so it is barred from arithmetic expressions and compares as characters). Every other reference
(`DISPLAY`, `STRING`, `INSPECT`, `UNSTRING`, a relation condition, an intrinsic argument classified
`CobolClass.NumericEditedDeEditing` in `IntrinsicArgumentRules.cs:319`, reference modification, the group image)
consumes the IMAGE and is already correct under D-EF1. So the `NumX.Dec` consumers this wave must verify under
NATIVE arithmetic are: `NumericRenderer.Align`, the numeric store (`CobolNum.Store`/`TryStore` via
`CobolDec.ToUnscaled`), and the edited store (a float-edited sender into a fixed-point edited receiver). Three,
not thirty — and each gets a golden in §13.

---

## 6. Editing (store) semantics

### 6.1 Sender → exact decimal

| Sender | Entry |
|---|---|
| fixed-point numeric (`DISPLAY`/`COMP`/`COMP-3`/`COMP-5`/`BINARY-*`) | already `(Int128 unscaled, int scale)` |
| numeric literal | folded to the same pair |
| floating-point usage (`COMP-1/2`, `FLOAT-SHORT/LONG/EXTENDED`, `FLOAT-BINARY-32/64`) | `CobolDec.FromDouble` |
| fixed-point numeric-edited | the existing `CobolEdit.DeEdit` → `(Int128, MaskScale)` |
| floating-point numeric-edited | `CobolEdit.DeEditFloat` → `CobolDec` |
| alphanumeric / national | §14.9.25.4 GR6 item 3 — treated as an unsigned integer (existing `CobolNum.FromAlphanumeric`) |

### 6.2 Normalize (§14.6.8.4 GR1) — exact integer arithmetic

```
value == 0 →  the §13.18.40.5 rule-8 zero image; DONE.
let  intDigits = SigDigits - SigScale          // the mask's integer digit positions, >= 0
let  d         = decimalDigitCount(|Sig|)      // significant digits of the sender
// choose the base-10 exponent E10 that puts exactly `intDigits` digits left of the point
E10 = (d - Exp_of_sender_as_power_of_ten_offset) - intDigits      // computed on the exact pair
sigDigits10 = |Sig| rescaled to exactly SigDigits digits          // integer shift, TRUNCATING
```

The rescale is `× 10^k` or `÷ 10^k` on `Int128` — **truncating**, per §14.6.8.4 GR2's reference to §13.18.40's
alignment rules, which is the MOVE truncation discipline the fixed-point `CobolEdit.Format` already implements
(`CobolNum.Rescale(..., CobolRounding.Truncation)` at `CobolEdit.cs:101`). A 36-digit significand needs at most
36 decimal digits — inside `Int128`'s 38.

Post-condition (the GR1 obligation): the leftmost of the `SigDigits` digits is nonzero.

### 6.3 Render

1. Exponent capacity: `maxExp = 10^ExpDigits - 1`. `E10 > maxExp` → `Overflow`; `E10 < -maxExp` → `Underflow`.
2. Significand: place `sigDigits10` into the `'9'` positions of `SigPattern` right-to-left; each `'.'`, `','`,
   `'B'`(→space), `'0'`, `'/'` supplies its insertion character in its own position (§13.18.40.5 rules 3 and 4,
   simple and special insertion — Table 7 admits exactly these plus fixed insertion).
3. Significand sign: `SigSign == '+'` → `'+'`/`'-'` by Table 8; `SigSign == '-'` → space/`'-'`; no sign symbol →
   the sign is not represented (the absolute value is edited — the same loss a fixed-point unsigned edited mask
   already has).
4. Literal `'E'` (§13.18.40.4 GR14).
5. Exponent sign: `'+'` when `E10 >= 0`, `'-'` when negative (Table 8, fixed insertion on the mask's `'+'`).
6. Exponent digits: `|E10|` zero-filled to `ExpDigits`.
7. `blankWhenZero && value == 0` → all spaces (§9.4).

**Zero suppression never runs** — GR13 b bans `Z`/`*`/floating insertion from the significand, so `CobolEdit`'s
pass-2 suppression loop is structurally absent from this path. This is why `FormatFloat` is a separate method and
not a flag on `Format`: half of `Format`'s body is unreachable here, and a flag would leave that half looking
live.

### 6.4 The MOVE / arithmetic divergence (§1.5) — the two-arm hazard, wired explicitly

| Outcome | MOVE (§14.9.25.4 GR6 4) | arithmetic GIVING/COMPUTE (§14.7.5) |
|---|---|---|
| `Ok` | store the image | store the image |
| `Overflow` | EC-DATA-OVERFLOW set; content = the pinned saturated image (D-EF8) | size error condition (case 3) — receiver **unchanged** (§14.7.5 storing rule 2) |
| `Underflow` | value **treated as zero** ⇒ the rule-8 zero image; **no exception** | size error condition (case 4) — receiver **unchanged** |

`MoveEmitter` calls `FormatFloat` (+ the EC raise on `Overflow`); `ArithmeticEmitter` calls `TryFormatFloat` and
routes `false` to its existing `onFail` (`ArithmeticEmitter.cs:303`). The asymmetric UNDERFLOW row is the arm most
likely to be missed — §13 gives it its own golden in both directions.

---

## 7. De-editing (read) semantics

`DeEditFloat(image, mask)`:

```
walk SigPattern against image[0..|SigPattern|):
    '9'                  → accumulate the digit (a non-digit contributes 0 and marks INCOMPATIBLE)
    '.' ',' 'B' '0' '/'  → must hold its insertion character, else INCOMPATIBLE
    sign position        → '+'/'-'/space per Table 8, else INCOMPATIBLE; sets `negative`
'E' position             → must hold 'E' (§13.18.40.4 GR15: "The symbol 'E' represents a character position
                            that will be checked to contain the character 'E'"), else INCOMPATIBLE
exponent sign            → '+' or '-', else INCOMPATIBLE
exponent digits          → digits, else INCOMPATIBLE
return new CobolDec(negative ? -sig : sig, expValue - SigScale)
```

`INCOMPATIBLE` ⇒ **EC-DATA-INCOMPATIBLE** (§14.6.13.2 rule 4) and a deterministic result (the value assembled
from whatever digits were readable, non-digits contributing zero — the same tolerant direction
`CobolNum.Image.cs`'s decoder already takes for incompatible zoned/packed content). The design DOES check —
today's fixed-point `CobolEdit.DeEdit` silently contributes zero for any non-digit and never raises the EC, which
is a pre-existing conformance gap in the fixed-point path; **this wave adds the check to the float path and files
the fixed-point twin as its own `kb/Work` note rather than silently fixing an unrelated path.**

The receiver store is the existing `CobolDec.ToUnscaled(receiverScale, mode)`, which already implements the
§14.6.8.2 fixed-point receiving rule ("aligned by decimal point … with zero fill or truncation on either end"
✔ `--check 14.6.8.2` OK) including high-order truncation of a value far beyond the receiver.

---

## 8. Statement-by-statement inventory

| Construct | Behaviour | Where |
|---|---|---|
| `MOVE x TO float-edited` | §6 | `MoveEmitter` via `RuntimeApi.EditFormat` |
| `MOVE float-edited TO numeric` | §7 → `CobolDec.ToUnscaled` | `NumericRenderer.FieldNum` (`Dec` lane) |
| `MOVE float-edited TO numeric-edited` | §7 then the receiver's own editing | ditto + `EditFormat` |
| `MOVE float-edited TO alphanumeric` | the IMAGE moves (Table 16 row Numeric-edited → Alphanumeric: Yes) | unchanged |
| `MOVE float-edited TO alphabetic` | **invalid** (Table 16: No) | `MoveTable16.cs` — already correct via the category |
| `ADD/SUBTRACT/MULTIPLY/DIVIDE … GIVING float-edited`, `COMPUTE float-edited = …` | §6.4 arithmetic column | `ArithmeticEmitter` |
| float-edited as an arithmetic SENDING operand | **rejected** — class alphanumeric (§14.9.2.3 SR2, §8.8.1.1) | `ExpressionBinder.NonNumericOperandKind` (`:400`) already rejects any `NumericEdited` operand, citing §8.5.2.13 + §8.5.2.1 Table 2 — correct for both forms, no change |
| relation condition | alphanumeric comparison of the image (§8.8.4.2.3 SR2) | unchanged |
| `IS NUMERIC` class test | admitted by usage display (§8.8.4.4.3 SR8); the category is not numeric, so §8.8.4.4.4 GR3 n 2 makes it "all characters 0–9" — always FALSE for a real image (it contains `.`/`E`) | unchanged; a golden pins it |
| `IS POSITIVE/NEGATIVE/ZERO` sign condition | §8.8.4.7 requires a numeric operand — rejected | unchanged |
| `INSPECT`, `STRING`, `UNSTRING`, ref-mod | operate on the image | unchanged |
| `DISPLAY` | the image | unchanged |
| `ACCEPT … FROM` a device | `AcceptDisplayEmitter.cs:181` currently edits the accepted value into the mask — takes the float arm via `EditFormat` | D-EF5 |
| `INITIALIZE` | figurative `ZEROES` (§14.9.20.4 GR6 c table row "Numeric-edited") ⇒ the rule-8 zero image | `InitializeBinder` (category already `NumericEdited`) |
| `VALIDATE` format validation | §13.18.40.4 GR15's per-symbol checks, `E` "checked to contain the character 'E'" | the VALIDATE wave; this design supplies the per-symbol table |
| `REDEFINES`, group image, file record, SORT key | `Length` character positions of a `string` leaf — unchanged (D-EF1) | unchanged |
| `FUNCTION HIGHEST-ALGEBRAIC` / `LOWEST-ALGEBRAIC` | §10 | `IntrinsicBinder.BindAlgebraicFold` |
| `FUNCTION SMALLEST-ALGEBRAIC` | **rejected** — §15.83.3 r1 admits only category numeric ✔ OK; the existing arm already rejects numeric-edited | unchanged |

---

## 9. Clause interactions

### 9.1 SIGN clause — a documented NO-OP, and a rejection when written on the entry

§13.18.52.3 SR1: "The SIGN clause may be specified only for: — a numeric data or screen description entry whose
picture character-string contains the symbol 'S' …" ✔ OK. A floating-point edited picture cannot contain `S`
(§1.2), so a SIGN clause **on the entry** is an SR1 violation (the existing SIGN validation must see the
float-edited category and reject, not silently accept). A **group-level** SIGN clause is inherited only by
"numeric items whose picture character-string contains the symbol 'S'" (§13.18.52.4 GR1), so it never reaches
this item: `PicInfo.SignKindFor` must not be consulted, and `SignKind` stays at its default and unused.
The significand's sign is a *fixed insertion editing symbol*, governed by Table 8 — a different mechanism from
the operational sign entirely.

### 9.2 DECIMAL-POINT IS COMMA

§13.18.40.3 SR13: "When the DECIMAL-POINT IS COMMA clause is specified, the symbol comma is the decimal separator
and the symbol period is the grouping separator. The rules for the symbol period apply to the symbol comma, and
the rules for the symbol comma apply to the symbol period." ✔ OK — and §13.18.40.6 adds "the precedence rules for
the symbols comma and period are interchanged".

The mechanism already exists and is reused verbatim: `CobolEdit`'s public entries canonicalise by swapping
`.`↔`,` on the way in and swapping the rendered output back (`CobolEdit.SwapSeparators`, `cs:36-44`). `FloatMask.Parse`
takes `commaMode` and canonicalises `SigPattern` the same way, so `PIC -9,9(5)E+99` under
`DECIMAL-POINT IS COMMA` is byte-identically the `-9.9(5)E+99` computation with `,` rendered where `.` was.
**The exponent is untouched** — it contains no separator symbol, and `E`/`+` have no comma-mode role.
The analyzer (§4.2 step 4b) accepts BOTH `.` and `,` as the significand's decimal-point symbol and resolves which
is which from the program's `DECIMAL-POINT` clause, exactly as `PictureAnalyzer` already does for fixed-point
masks ("Analyze sees the RAW picture — DECIMAL-POINT IS COMMA … swaps the ROLES … not the symbols themselves",
`PictureAnalyzer.cs:34-36`).

### 9.3 CURRENCY SIGN

Structurally inapplicable: a currency symbol may not precede `E` (Table 10 row `E`), GR13 b bans floating
insertion in the significand, and §12.3.7.3 SR22 b forbids `E` itself from ever being a currency symbol ✔ OK.
`FormatFloat`/`DeEditFloat` therefore take no `currency` parameter — the absence is a proof obligation discharged,
not an omission.

### 9.4 BLANK WHEN ZERO

Legal: §13.18.8.3 SR1 admits any "elementary item described by its picture character-string as category
numeric-edited" ✔ OK, and SR22's `S`/`*` ban is vacuous here. §13.18.8.4 GR1: "the content of the data item is set
to all spaces when the item is a receiving operand and the value being stored is zero" ✔ OK.

**This collides with §13.18.40.5 rule 8 (zero ⇒ all-zero digits).** The design resolves it as **BLANK WHEN ZERO
wins** — spaces — on three grounds: (a) GR1 is unconditional and specific to the clause's presence; (b) the
standard states exactly this precedence for the sibling case, §13.18.40.5 rule 10, "A BLANK WHEN ZERO clause takes
precedence over locale editing"; (c) it matches what `CobolEdit.Format` already does for every fixed-point edited
mask (`cs:64`). **⚠ Interpretive — see §15 Q2.**

§13.18.63.3 SR8 and its NOTE 2 govern the VALUE interaction: an alphanumeric/national VALUE literal makes
BLANK WHEN ZERO have no effect on initialization; a numeric VALUE literal (including `ZERO`) does trigger it.

### 9.5 VALUE clause

§13.18.63.3 SR6 is the load-bearing rule and it is specific to this construct:

> "…Literals for fixed-point formats shall be specified as fixed-point, while literals for floating-point formats
> shall be specified as floating-point, though the figurative constant ZERO or ZEROES and the integer and decimal
> forms of the literal zero may also be specified for either format and shall be treated identically as the
> literal zero." ✔ OK

So `VALUE` on a floating-point edited item admits: a **floating-point numeric literal** (§8.3.3.3.3 — "formed from
two fixed-point numeric literals separated by the letter 'E' without intervening spaces" ✔ OK; exponent "a maximum
of four digits and no decimal point" ✔ OK), `ZERO`/`ZEROES`/`0`/`0.0`, or an alphanumeric literal of exactly
`Length` characters (SR7). A **fixed-point** numeric literal other than zero is an SR6 violation →
`COBOLNET1647`. A numeric literal initialises through the editing rules (§13.18.63.3 SR11: "Editing characters in
a picture character-string for a numeric-edited data item are used in editing of the initial value when the data
item is initialized and the literal is numeric" ✔ OK), i.e. through `FormatFloat` at compile time
(`ValueInitializer.cs:159` gains the float arm). SR2's "shall be representable exactly … without truncation of
leading or trailing nonzero digits" makes a literal that would truncate an error, not a silent truncation.

Probe `pb66d03` proves the floating-point literal channel already works end to end (§2).

### 9.6 The 2023 `PICTURE … EDITING` phrase

Banned for this form (§1.2 / §13.18.40.3 SR12 a). `PictureAnalyzer.ValidateEditing` runs BEFORE the `E` scan
today; the float analyzer rejects an `EDITING` phrase on a float-edited picture with `COBOLNET1645`'s
character-1 leg before `ValidateEditing`'s SR10 ("character-1 shall appear at least once") can produce a
confusing secondary error.

---

## 10. `FUNCTION HIGHEST-ALGEBRAIC` / `LOWEST-ALGEBRAIC` — the two inventory rows PB66 owns

### 10.1 What the rules say

§15.43.3 r1 / §15.58.3 r1: "Argument-1 shall be a data item of category numeric or numeric-edited and shall not be
an integer function or numeric function." ✔ OK — a floating-point edited item **is** admitted.

§15.43.4 r1 (**RV-15.43.4-1**): "When argument-1 is a floating-point numeric-edited item, the data description
entry with which argument-1 is described shall be such that, if argument-1 contained the positive value farthest
from zero that is permitted according to that data description entry, an IN-ARITHMETIC-RANGE test of argument-1
would return a true value." ✔ OK.
§15.58.4 r1 (**RV-15.58.4-1**): the same for a **signed** floating-point edited item and the negative extreme.
✔ OK.
§15.43.4 r2 / §15.58.4 r2 give the returned value: the greatest / lowest finite magnitude representable. ✔ OK.

The test they name is defined by §8.8.4.4.4 GR3 l): "If IN-ARITHMETIC-RANGE Is specified, the condition is true if
the numeric content of the data item referenced by identifier-1 is neither farther from zero nor closer to zero
than is permitted for the form of an intermediate data item appropriate to the mode of arithmetic in effect."
✔ OK (`cite.py` prints an approximate path `3) d) 2.` — the printed label is `l)`).

⚠ **The rules are HYPOTHETICAL, not a runtime test.** §8.8.4.4.3 SR6 restricts an actual `IN-ARITHMETIC-RANGE`
class condition to a **category numeric** operand ✔ OK — a numeric-edited item could never be its subject. The
rules say the entry shall be such that the test *would* return true; that makes them a **compile-time
well-formedness condition on the data description entry**, evaluated at the function reference. This is exactly
the distinction the `validate_the_premise_not_only_the_rule` feedback exists for, and it is why the *class
condition* `IN-ARITHMETIC-RANGE` is NOT a prerequisite of this wave: PB66's inventory note observes that
`IN-ARITHMETIC-RANGE` exists in WiseOwl COBOL only as a `ReservedWords.Table` row, and the note is right — but the
rule can be honoured in full without it. (Implementing the class condition itself remains its own item; it is
2014+ and belongs with the FLOAT-INFINITY / FARTHEST-FROM-ZERO / NEAREST-TO-ZERO package.)

### 10.2 The item's extremes

For `FloatEditSpec(SigDigits d, SigScale f, SigSigned s, ExpDigits e)`, with `intDigits = d - f` and
`maxExp = 10^e - 1`:

```
HIGHEST-ALGEBRAIC = (10^d - 1) × 10^(-f) × 10^(+maxExp)            // all-nines significand, max exponent
LOWEST-ALGEBRAIC  = -HIGHEST-ALGEBRAIC   when SigSigned            // §15.58.4 r2
                  =  0                   when not SigSigned        // an unsigned mask cannot hold a negative
```

(The unsigned case matches the standard's own fixed-point NOTE table, where `$**,**9.99` — no sign symbol —
returns `0` for LOWEST-ALGEBRAIC. §15.58.4 r1 is worded for a *signed* item precisely because the unsigned answer
is trivially zero.)

The smallest nonzero magnitude, needed for the "closer to zero" half:
`minNonZero = 10^(intDigits - 1) × 10^(-maxExp)`.

### 10.3 The IN-ARITHMETIC-RANGE bound, per arithmetic mode

"the form of an intermediate data item appropriate to the mode of arithmetic in effect" — WiseOwl COBOL's modes
(numeric design D3):

| Mode | Intermediate form | `farthest` | `closest (nonzero)` |
|---|---|---|---|
| **NATIVE** (default) | §8.8.1.3 makes native arithmetic "an implementor-defined method" ✔ OK; WiseOwl COBOL's documented native technique is the exact `Int128` fixed-point engine (D1) for fixed-point operands and **IEEE binary64** for any float-valued expression (D7/D16/D18). A floating-point numeric-edited item's value is float-valued, so its intermediate form is binary64. | `1.7976931348623157E+308` | `4.9406564584124654E-324` |
| **STANDARD-DECIMAL** (implemented, `CobolDec`) | §8.8.1.5.2 SDIDI — "with a maximum precision of 34 decimal digits; the smallest positive nonzero value is 1.0E-6176" ✔ OK; the magnitude ceiling is decimal128's ≈ `9.99…E+6144` | ≈ `1E+6145` | `1.0E-6176` |
| **STANDARD-BINARY** | documented-unsupported (D3; the mode is obsolete per §8.8.1.4.1's NOTE) — the existing `ArithmeticMode.StandardBinary` rejection covers it | — | — |

**Rule (D-EF9):** at the function reference, if `HIGHEST-ALGEBRAIC(item) > farthest` or
`minNonZero < closest`, emit **`COBOLNET1648`** naming §15.43.4 r1 / §15.58.4 r1, the mode in effect, and the
bound.

**Both halves are live — worked out for `intDigits = 1`:**

| Mask | farthest ≈ | closest ≈ | NATIVE (binary64: 1.797E+308 / 4.94E−324) | STANDARD-DECIMAL (≈1E+6145 / 1.0E−6176) |
|---|---|---|---|---|
| `9.9(5)E+9` | `1E+10` | `1E-9` | pass | pass |
| `9.9(5)E+99` | `1E+100` | `1E-99` | pass | pass |
| `9.9(5)E+999` | `1E+1000` | `1E-999` | **fail — both halves** | pass |
| `9.9(5)E+9999` | `1E+10000` | `1E-9999` | **fail — both halves** | **fail — both halves** |

So under the default NATIVE mode an `E+9`/`E+99` mask is a legal ALGEBRAIC argument and an `E+999`/`E+9999` mask
is not; under STANDARD-DECIMAL the line moves to `E+9999`. The mode-dependence is the whole point of GR3 l's
"appropriate to the mode of arithmetic in effect", and §13.6 tests both sides of it.

### 10.4 The returned value

Follow the existing float-usage arm at `IntrinsicBinder.cs` (`BindAlgebraicFold`), which already returns
E-notation text: `return new BoundNumLiteral("1.7976931348623157E+308")`. The float-edited arm returns
`BoundNumLiteral` of the §10.2 extreme in E notation, which the renderer carries on its float lane — and D-EF9
has just guaranteed the value fits that lane. Under STANDARD-DECIMAL the same literal enters the `CobolDec`
lane. The fold sits beside the existing `edited` branch (`IntrinsicBinder.cs:1698`, which calls
`CobolEdit.MaskCapacity` — a fixed-point-only computation that "has no exponent concept", exactly as the
inventory note records) and is selected by `pic.IsFloatEdited` **before** that branch.

### 10.5 Inventory outcome

`RV-15.43.4-1` and `RV-15.58.4-1` move `NOT-IMPLEMENTED` → `CONFORMS` with a `test-ref`, and their `notes` are
rewritten to describe the landed mechanism. The `editions` field stays `2014,2023` for the rule AS WORDED; §11
covers what happens at `--std 2002`.

---

## 11. Edition gating

| `--std` | Behaviour |
|---|---|
| **85** | `COBOLNET0900` — "a floating-point numeric-edited PICTURE (the symbol E) requires COBOL-2002 (targeting COBOL-85)". Unchanged mechanism: the `constructs.json` row `pic-external-float-2002` with `introducedIn: 2002`, fired by `VersionConformancePass`'s `GateData` enumerator. **The `SkeletonGate` carrier is DELETED** — it exists only because `PicInfo.Recovery` erased the category; once the analyzer returns a real `PicCategory.NumericEdited` the enumerator keys on `Pic.IsFloatEdited`, exactly as the National/Boolean rows key on `Pic.Category`. (`PicInfo.SkeletonGate`'s doc-comment names external float as one of its two users; after this wave only `NationalEdited2002` remains, and the property's comment must say so.) |
| **2002** | Fully live. §15.43.4/§15.58.4 r1 in the 2014 wording did not yet exist, so `COBOLNET1648` is issued citing **§8.8.1.3 + Annex A.3** (the implementor-defined intermediate and the processor-dependent capability) rather than §15.43.4 r1. The *test* is identical — the constraint is physical, not editorial: the compiler cannot return a value its intermediate cannot hold. |
| **2014 / 2023** | Fully live; `COBOLNET1648` cites §15.43.4 r1 / §15.58.4 r1. |

`constructs.json` row changes: `status: pending → active`, `diagnosticCode`/`expectDiagnostic` unchanged
(`COBOLNET0900`), `description` rewritten (drop "silent-misbinds"/"Phase 6"), `display` → "a floating-point
numeric-edited PICTURE (the symbol E)", `citation` → "ISO §13.18.40.4 GR13 b", and **`source` corrected from the
illegal `PIC +9V99E+99` to `PIC +9.99E+99`** (§2).

⛔ **Edition-gate sweep obligation** (`feedback_edition_gate_sweep`): every golden added by this wave compiles at
2002/2014/2023 and draws `COBOLNET0900` at 85. The `new-construct` skill's matrix row is mandatory.

---

## 12. Diagnostics

Next free code is **`COBOLNET1643`** (`scripts/session-probe.ps1`: "src-grep max COBOLNET1642 · catalog max
COBOLNET1641 · next free = COBOLNET1643").

| Code | Condition | Citation in the message |
|---|---|---|
| `COBOLNET1643` | `E` (or `.`) appears more than once in character-string-1 | §13.18.40.3 SR12 b |
| `COBOLNET1644` | the exponent part is not `+9(n)`, n = 1..4 | §13.18.40.4 GR13 b |
| `COBOLNET1645` | a symbol appears in the significand that may not precede `E`. **Separate legs**, each naming the symbol and its own rule: `V`/`P` (Table 10 row E; SR17/SR20 point exclusivity), `S` (Table 10 row E; SR18), `Z`/`*` (GR13 b "Neither floating insertion editing nor zero suppression with replacement shall be specified for the significand"), the currency symbol (GR13 b + Table 10 row E), `CR`/`DB` (Table 10 row E), an `EDITING` character-1 (SR12 a + §13.18.40.6's `es`≡`cs` precedence) | §13.18.40.6 Table 10 + the named rule |
| `COBOLNET1646` | significand digit positions outside 1..36 | §13.18.40.3 SR15 |
| `COBOLNET1647` | a `VALUE` literal for a floating-point format that is a non-zero **fixed-point** numeric literal | §13.18.63.3 SR6 |
| `COBOLNET1648` | `HIGHEST-`/`LOWEST-ALGEBRAIC` argument-1's extreme value is outside the intermediate data item's range for the mode in effect | §15.43.4 r1 / §15.58.4 r1 (≥2014); §8.8.1.3 + Annex A.3 (2002) |
| existing `COBOLNET0900` | below COBOL-2002 | `constructs.json` row |
| existing SIGN validation | a `SIGN` clause on the entry | §13.18.52.3 SR1 |

`COBOLNET0899` for this construct is **retired** — the whole point of the wave. The `DiagnosticCatalog` /
`docs/DIAGNOSTICS.md` regeneration (`scripts/gen-diagnostics-doc.ps1`) runs in the same change set.

---

## 13. Test plan

Corpus conventions: positive goldens under the greenfield conformance corpus with a manifest entry, negative
fixtures for every diagnostic, one unique `PROGRAM-ID` per test (`feedback_unique_programid_per_test`), and the
edition matrix row per `new-construct`.

### 13.1 PICTURE analysis (negative fixtures — one per diagnostic leg)

`PIC 9.9E+9E+9` (1643) · `PIC 9.9.9E+99` (1643) · `PIC 9.9E99` / `PIC 9.9E-99` / `PIC 9.9E+9(5)` / `PIC 9.9E+` (1644) ·
`PIC 9V99E+99` / `PIC 9PP9E+99` / `PIC S9.9E+99` / `PIC Z9.9E+99` / `PIC *9.9E+99` / `PIC $9.9E+99` /
`PIC 9.9CRE+99` / `PIC 9.9E+99 EDITING L IS ':'` (1645, one fixture each) · `PIC 9(37).9E+99` and `PIC .E+99` (1646).
Each fixture asserts the EXACT code and that the message names the symbol and the rule.

### 13.2 PICTURE analysis (positive)

`PIC 9.9(5)E+99` · `PIC -9.9(5)E+99` · `PIC +9.9(5)E+99` · `PIC 999E+9` (no point — an all-integer significand) ·
`PIC -9(2).9(34)E+9999` (SR15's 36-digit boundary AND GR13 b's 4-digit boundary) · `PIC -9,9(5)E+99` under
`DECIMAL-POINT IS COMMA` · `PIC 9B9.9E+99` (simple insertion in the significand) ·
`PIC 9.9(5)E+99 BLANK WHEN ZERO`. Each program `DISPLAY`s the item and `FUNCTION LENGTH` of it, so the
character-position count is asserted, not eyeballed.

### 13.3 Store (MOVE) — value goldens

| Sender | Mask | Expected image | Rule |
|---|---|---|---|
| `+123.45` | `-9.9(5)E+99` | `" 1.23450E+02"` | §14.6.8.4 GR1 |
| `-123.45` | `-9.9(5)E+99` | `"-1.23450E+02"` | Table 8 |
| `-123.45` | `+9.9(5)E+99` | `"-1.23450E+02"` | Table 8 |
| `+123.45` | `+9.9(5)E+99` | `"+1.23450E+02"` | Table 8 |
| `0` | `-9.9(5)E+99` | `" 0.00000E+00"` | §13.18.40.5 rule 8 |
| `0` | `+9.9(5)E+99` | `"+0.00000E+00"` | rule 8 ("the sign … shall be positive") |
| `+0.000123` | `-9.9(5)E+99` | `" 1.23000E-04"` | GR1 + Table 8 on the exponent sign |
| `+123.45` | `-99.9(4)E+99` | `" 12.3450E+01"` | GR1 normalizes to the MASK's integer digit count |
| `+1.999999` | `-9.9(3)E+99` | `" 1.999E+00"` | truncation, not rounding (§14.6.8.4 GR2 → §13.18.40) |
| `PIC S9(31)` all-nines | `-9.9(30)E+99` | 31 nines, exponent `+30` | D-EF2/D-EF3 — **the test that fails if the value channel is `double`** |
| `+1.5` | `9.9(5)E+99` (unsigned) with sender `-1.5` | `"1.50000E+00"` | an unsigned mask does not represent the sign |
| `+123` | `9.9(3)E+9` | `Ok` | boundary |
| `+123456789012` | `9.9(3)E+9` | Overflow ⇒ EC-DATA-OVERFLOW + the pinned saturated image | §14.9.25.4 GR6 4a, D-EF8 |
| `+1E-12` (from `COMP-2`) | `9.9(3)E+9` | Underflow ⇒ the rule-8 zero image, **no exception** | §14.9.25.4 GR6 4b |
| `+123.45` under `DECIMAL-POINT IS COMMA` | `-9,9(5)E+99` | `" 1,23450E+02"` | §13.18.40.3 SR13 |
| `0` with `BLANK WHEN ZERO` | `-9.9(5)E+99` | 12 spaces | §13.18.8.4 GR1, §15 Q2 |

### 13.4 Arithmetic store — the divergence goldens (§6.4)

The SAME two out-of-range values as §13.3, but through `COMPUTE F-ED = …` / `ADD … GIVING F-ED`:
overflow ⇒ SIZE ERROR taken, receiver **unchanged**; **underflow ⇒ SIZE ERROR taken, receiver unchanged** (not
zeroed). Plus the `ON SIZE ERROR`-absent form asserting EC-SIZE-TRUNCATION under a `TURN` directive.

### 13.5 De-edit

Round-trip: `MOVE x TO F-ED` then `MOVE F-ED TO PIC S9(18)V9(9)` for each §13.3 row, asserting the recovered
value. Plus: a huge exponent de-edited into a small receiver (high-order truncation per §14.6.8.2 r4);
a hand-injected malformed image via `REDEFINES`/alphanumeric MOVE (`"1.2345XE+02"`, `"1.23450F+02"`) asserting
EC-DATA-INCOMPATIBLE (§14.6.13.2 rule 4) and the deterministic value.

### 13.6 The two owned inventory rows

`FUNCTION HIGHEST-ALGEBRAIC (PIC -9.9(5)E+99)` → `+9.99999E+99`;
`FUNCTION LOWEST-ALGEBRAIC` of the same → `-9.99999E+99`;
`FUNCTION LOWEST-ALGEBRAIC (PIC 9.9(5)E+99)` (unsigned) → `0`;
`FUNCTION HIGHEST-ALGEBRAIC (PIC 9.9E+999)` → `COBOLNET1648` under NATIVE, **accepted** under
`ARITHMETIC IS STANDARD-DECIMAL` (the mode-parameterised half of D-EF9 — the probe that flips the axis the
subject holds fixed, `feedback_probe_the_shape_the_subject_hides`);
`FUNCTION HIGHEST-ALGEBRAIC (PIC 9.9E+9999)` → `COBOLNET1648` under BOTH modes (both halves of the range test).
`FUNCTION SMALLEST-ALGEBRAIC` of a float-edited item → the existing §15.83.3 r1 rejection (a NEGATIVE test that
must keep failing).

### 13.7 Category / class goldens

`MOVE F-ED TO PIC X(12)` (image moves) · `MOVE F-ED TO PIC A(12)` (rejected, Table 16) ·
`IF F-ED = "1.23450E+02"` (alphanumeric comparison) · `IF F-ED IS NUMERIC` (false) ·
`COMPUTE N = F-ED + 1` (rejected — class alphanumeric) · `FUNCTION LENGTH(F-ED)` = 12 ·
`F-ED(1:3)` ref-mod · a group containing it, moved whole.

### 13.8 Edition matrix

Every §13.2/§13.3 program at `--std 85` ⇒ `COBOLNET0900`; at 2002/2014/2023 ⇒ green.
`COBOLNET1648`'s citation text differs at 2002 vs ≥2014 — asserted.

### 13.9 Drift / structural tests

- **`FloatEditDispatchTests`** — for every `PicInfo` with `IsFloatEdited`, assert that
  `RuntimeApi.EditFormat`/`EditTryFormat` and `NumericRenderer.FieldNum` emit the `FormatFloat`/`DeEditFloat`
  form. This is the mechanical enforcement of D-EF5, and it must be made to FAIL once before it is trusted
  (`feedback_green_gates_arent_evidence`).
- **`FloatEditMaskRoundTripTests`** — `FloatMask.Parse` ∘ render ∘ `DeEditFloat` is the identity on the value for
  a generated sweep of masks × values, with the significand digit count swept 1..36 and the exponent 1..4.
- **`AlgebraicFoldContainerAgreementTests`** (existing) extended so the float-edited fold's bound agrees with
  what `FormatFloat` will actually store — the same discipline the binary-capacity fold already has.

### 13.10 GnuCOBOL differential

External floating-point is a construct GnuCOBOL supports; the differential harness gains the §13.3 programs.
⚠ GPL — read verdicts only, never sources (`feedback_gnucobol_differential`). Where GnuCOBOL diverges, the spec
decides and the divergence is recorded, not adopted.

---

## 14. Touchpoint map (verified by grep, file:line as of this draft)

| File | Change |
|---|---|
| `Binding/PictureAnalyzer.cs:98,105-123` | replace the `hasE` stage with the §4.2 analyzer; remove `E` from the `N`/`1`/`E` staged trio |
| `Binding/Model/PicInfo.cs` | add `FloatEditSpec` + `FloatEdit` + `IsFloatEdited`; narrow `SkeletonGate`'s doc to `NationalEdited2002` |
| `Binding/DataBinder.cs:2147` | exclude `IsFloatEdited` from the SR14 capacity check |
| `Binding/DataBinder.cs:2165,2173` | the VALUE-literal legs gain the SR6 fixed-vs-floating check |
| `Binding/Procedure/Verbs/IntrinsicBinder.cs:1640,1698` | the float-edited fold arm ahead of the `edited` branch |
| `Binding/Validation/*` (SIGN) | reject a `SIGN` clause on a float-edited entry (§13.18.52.3 SR1) |
| `Binding/Passes/VersionConformancePass` `GateData` | key the 2002 row on `IsFloatEdited`, drop the `SkeletonGate` carrier |
| `CodeGen/Roslyn/RuntimeApi.cs:143-146,269-272` | `EditFormat`/`EditTryFormat` take `PicInfo` and dispatch (D-EF5) |
| `CodeGen/Emit/NumericRenderer.cs:177` | the `DeEditFloat` arm on the `NumX.Dec` lane |
| `CodeGen/Emit/EmitCore.cs:71` | widen `NumX.Dec`'s doc-comment (D-EF3) |
| `CodeGen/Verbs/MoveEmitter.cs:331,337` · `ArithmeticEmitter.cs:303,307` · `AcceptDisplayEmitter.cs:181` · `StringEmitter.cs:231` | pass `PicInfo` instead of the raw mask (no per-site branching) |
| `CodeGen/DataDivision/ValueInitializer.cs:143,159` · `GroupValueSlicer.cs:46` · `GroupImageCodec.cs:47,51` | the float VALUE arm; the image arms need no change (D-EF1) |
| `CodeGen/DataDivision/RecordStructEmitter` | emit the per-item `static readonly CobolEdit.FloatMask` (D-EF6) |
| `Runtime/Values/Numeric/CobolEdit.cs` | `FloatMask`, `FormatFloat`, `TryFormatFloat`, `DeEditFloat`, `FloatStoreOutcome` |
| `Runtime/Values/Numeric/CobolDec.cs:23` | note the exact-representation use beside the arithmetic-entry comment |
| `tests/version-matrix/constructs.json` | `pic-external-float-2002` → active; **fix the illegal `source` picture** |
| `tests/version-matrix/traceability-inventory.json` | `RV-15.43.4-1`, `RV-15.58.4-1` → CONFORMS + `test-ref`; the §13.18.40.3/.4/.5 and §14.6.8.4 rows become adjudicable |
| `docs/COBOLNET_DATA_MODEL_DESIGN.md` · `docs/COBOLNET_NUMERIC_DESIGN.md` · `docs/CONFORMANCE.md` · `docs/DIAGNOSTICS.md` · `docs/DOC_INDEX.md` | merge this design; record D-EF8's pinned undefined-behaviour choice |
| `kb/Work/PB66.md` | `status: open → closed` in the landing commit |

---

## 15. Open questions

**Q1 — OWNER-RESERVED. The §14.9.25.4 GR6 4a "undefined" content.** The standard sets EC-DATA-OVERFLOW and leaves
the receiving item's content undefined when a MOVE's value exceeds the mask's exponent capacity. This design
proposes the **saturated extreme image** (D-EF8). The alternatives are *leave the item unchanged* (matches the
arithmetic SIZE ERROR arm, so one rule instead of two — but MOVE has no "unchanged" tradition and it hides the
overflow from a program that ignores the EC) and *the rule-8 zero image* (symmetric with the underflow arm — but
it turns a huge value into zero, the worst possible wrong answer). A behaviour determination that ships in
`docs/CONFORMANCE.md` is an owner call.

**Q2 — INTERPRETIVE, recommendation given. `BLANK WHEN ZERO` vs §13.18.40.5 rule 8.** Both are stated
unconditionally for a zero value; the standard states the precedence only for the *locale* format (rule 10).
Recommendation: BLANK WHEN ZERO wins (§9.4's three grounds). If the owner disagrees, the alternative is to reject
`BLANK WHEN ZERO` on a floating-point edited entry — but §13.18.8.3 SR1 plainly admits it, so rejecting legal
source would need its own justification.

**Q3 — OWNER-RESERVED. Does `COBOLNET1648` (D-EF9) also fire at the DECLARATION?** As designed it fires only at a
`HIGHEST-`/`LOWEST-ALGEBRAIC` reference, because that is the only place the rules constrain. An `E+9999` mask is
otherwise perfectly usable (nothing can produce a value that large, so no store can overflow it). The
alternative — warn at declaration that the item's declared range exceeds the implementation's intermediate — is
friendlier but rejects/flags source the standard permits.

**Q4 — INTERPRETIVE, recommendation given. Is `PIC 999E+99` (a significand with no `.`) legal?** GR13 b requires
the significand to be "a valid character-string for either a numeric item or a numeric-edited item for a
fixed-point result"; `999` is a valid numeric character-string, Table 10 row `E` admits `9` before `E`, and
nothing requires a decimal point (unlike a floating-point *literal*, where §8.3.3.3.3 r2 explicitly says the
significand "shall include a decimal point"). Recommendation: **legal**, `SigScale = 0`. The asymmetry with the
literal rule is deliberate in the standard and this design follows it. Flagged because it is the kind of
asymmetry a reader will assume is a mistake.

**Q5 — SCOPE, needs a `kb/Work` note, not a decision here. The fixed-point `CobolEdit.DeEdit` never raises
EC-DATA-INCOMPATIBLE.** §14.6.13.2 rule 4 applies to every numeric-edited sender, and the current implementation
silently contributes zero for any non-digit (`CobolEdit.cs:284`). §7 adds the check on the float path only.
Fixing the fixed-point twin is a separate, larger change (it touches every existing edited golden) and belongs in
its own note — recording it here so it is not lost, per CLAUDE.md rule 8's "a newly found defect becomes a note
before it becomes a DEVLOG paragraph".

**Q6 — SCOPE. `USAGE NATIONAL` + a floating-point edited picture.** §13.18.40.4 GR1 makes every symbol a national
character position, and §8.5.2.1 Table 2 makes the item class *national*. WiseOwl COBOL already stages the whole
national-form numeric/numeric-edited family loud (`PictureAnalyzer.cs:210`, Phase-4a residue). This design
inherits that stage rather than fixing it, which is **not** a new deferral — it is the existing, already-tracked
one — but the wave must confirm the float form takes that stage and does not slip through it.

---

## 16. Citation ledger

Every clause below was validated with `python scripts/spec/cite.py --check <clause> "<text>"` in the session that
produced this draft. `OK` = the quoted text was found inside that clause's own region.

| Clause | Quoted text (head) | Verdict |
|---|---|---|
| §13.18.40.4 GR13 b | "To define a floating-point numeric-edited item, characters-string-1 shall consist of two parts…" | OK |
| §13.18.40.4 GR13 b | "The exponent shall be '+9', '+99', '+999', '+9999', or '+9(n)' where n = 1, 2, 3, or 4" | OK |
| §13.18.40.4 GR13 b | "The significand shall be a valid character-string for either a numeric item or a numeric-edited item…" | OK |
| §13.18.40.4 GR13 b | "Neither floating insertion editing nor zero suppression with replacement shall be specified for the significand" | OK |
| §13.18.40.4 GR14 | "The symbol 'E' is used to separate the significand and the exponent…" | OK |
| §13.18.40.4 GR15 | "The symbol 'E' represents a character position that will be checked to contain the character 'E'" | OK |
| §13.18.40.4 GR3 | "A PICTURE clause defines the subject of the entry to fall into one of the following categories of data" | OK |
| §13.18.40.3 SR15 | "For floating-point data items of category numeric-edited, the number of digit positions in the significand shall range from 1 through 36" | OK |
| §13.18.40.3 SR14 | "For data items of category numeric, and for fixed-point data items of category numeric-edited…1 through 31" | OK |
| §13.18.40.3 SR12 (FOR-not-specified) b | "Each of the symbols from the set 'CR', 'DB', 'E', 'S', 'V' '.' may appear only once in character-string-1" | OK |
| §13.18.40.3 SR12 a | "Extended editing sign control symbols shall not be specified for a floating-point edited item" | OK |
| §13.18.40.3 SR13 | "When the DECIMAL-POINT IS COMMA clause is specified, the symbol comma is the decimal separator…" | OK |
| §13.18.40.3 SR17 | "The symbol 'P' and the symbol '.' are mutually exclusive in character-string-1" | OK |
| §13.18.40.3 SR18 | "The symbol 'S', if present, shall be the first symbol in character-string-1" | OK |
| §13.18.40.3 SR20 | "The symbol 'V' and the symbol '.' are mutually exclusive in character-string-1" | OK |
| §13.18.40.3 SR23 | "…mutually exclusive in character-string-1 with the exception of a numeric-edited data item for a floating-point edited result…" | OK |
| §13.18.40.3 SR23 NOTE 3 | "For a floating-point edited result, the significand part of the character-string may contain a '-' symbol…" | OK |
| §13.18.40.3 SR24 | "For fixed insertion with editing sign control symbols, only one currency symbol and only one editing sign control symbol…" | OK |
| §13.18.40.3 SR25 | "The symbol '+' or the symbol '-', when used, shall be either the leftmost or the rightmost symbol…" | OK |
| §13.18.40.3 SR4 | "The maximum number of characters allowed in character-string-1 is 63" | OK |
| §13.18.40.3 SR2 | "The allowable combinations of symbols for a PICTURE clause are specified in 13.18.40.6, Precedence rules" | OK |
| §13.18.40.3 SR7 | "If the symbol ',' or the symbol '.' is the last symbol of character-string-1…" | OK |
| §13.18.40.5 rule 2 (Table 7) | "Simple insertion, special insertion, and fixed insertion for the significand part" | OK |
| §13.18.40.5 rule 5 | "Fixed insertion editing results in the insertion character(s) occupying the same character position(s)…" | OK |
| §13.18.40.5 rule 8 | "If the value to be edited into a floating-point edited item is zero, then after editing all digit positions…" | OK |
| §13.18.40.6 | "character-string-1 for a numeric-edited item for a floating-point edited result is considered as two separate strings" | OK |
| §13.18.40.6 | "The symbol '+' that appears in a column and in a row by itself, represents its use in the exponent part…" | OK |
| §14.6.8.2 r1 | "If the sending operand is an intermediate data item or a data item described with a standard floating-point usage…" | OK |
| §14.6.8.3 | "A floating-point numeric data item is a data item described with the FLOAT-SHORT usage…" | OK |
| §14.6.8.4 r1 | "If the algebraic value of the sending operand is not zero, the exponent and significand of the value are adjusted…" | OK |
| §14.6.8.4 r2 | "Alignment and zero fill or truncation take place as described in the general rules and editing rules in 13.18.40" | OK |
| §14.9.25.4 GR5 | "De-editing takes place only when the sending operand is a numeric-edited data item…" | OK |
| §14.9.25.4 GR6 4 | "If the receiving data item is described with a standard floating-point usage or is a floating-point numeric-edited item" | OK |
| §14.9.25.4 GR6 4a | "…farther from zero than is permitted…the EC-DATA-OVERFLOW exception condition is set to exist" | OK |
| §14.9.25.4 GR6 4b | "…nearer to zero than is permitted…the numeric value is treated as zero" | OK |
| §14.6.13.2 r4 | "When a numeric-edited data item is the sending operand of a de-editing MOVE statement…" | OK |
| §14.7.5 case 3 | "if, after radix point alignment…further from zero than permitted for the associated resultant data item" | OK |
| §14.7.5 case 4 | "if the nonzero result…is nearer to zero than permitted for the associated resultant data item" | OK |
| §14.9.2.3 SR4 | "Identifier-3 shall reference a numeric data item or a numeric-edited data item" | OK |
| §14.9.20.4 GR6 c | Table row "Numeric-edited | Figurative constant ZEROES" | OK |
| §15.43.3 r1 | "Argument-1 shall be a data item of category numeric or numeric-edited and shall not be an integer function…" | OK |
| §15.43.4 r1 | "When argument-1 is a floating-point numeric-edited item, the data description entry…shall be such that" | OK |
| §15.43.4 r2 | "The value returned is equal to the positive algebraic value of greatest finite magnitude…" | OK |
| §15.58.4 r1 | "When argument-1 is a signed floating-point numeric-edited item, the data description entry…shall be such that" | OK |
| §15.58.4 r2 | "The value returned is equal to the lowest finite algebraic value that may be represented in argument-1" | OK |
| §15.83.3 r1 | "Argument-1 shall be a data item of category numeric" | OK |
| §8.8.4.4.4 GR3 l | "If IN-ARITHMETIC-RANGE Is specified, the condition is true if the numeric content…" | OK (path printed approximate) |
| §8.8.4.4.3 SR6 | "If FARTHEST-FROM-ZERO, IN-ARITHMETIC-RANGE, or NEAREST-TO-ZERO is specified, identifier-1 shall reference a data item whose category is numeric" | OK |
| §8.8.4.2.3 SR2 | "All identifiers shall be of class alphabetic, alphanumeric, index, national, or numeric…" | OK |
| §8.8.4.2.4 | "For operands whose class is numeric, a comparison is made with respect to the algebraic value…" | OK |
| §8.5.2.13 r1 | "A data item described as numeric-edited by its PICTURE character-string" | OK |
| §8.5.2.1 | "The category of an elementary data item depends upon its description" (Table 2) | OK |
| §8.8.1.3 | "Native arithmetic is an implementor-defined method of evaluating an arithmetic expression" | OK |
| §8.8.1.5.2 | "with a maximum precision of 34 decimal digits" / "the smallest positive nonzero value is 1.0E-6176" | OK |
| §8.3.3.3.3 r1 | "A floating-point numeric literal is formed from two fixed-point numeric literals separated by the letter 'E'…" | OK |
| §8.3.3.3.3 r3 | "The literal to the right of the 'E' represents the exponent…a maximum of four digits and no decimal point" | OK |
| §13.18.63.3 SR6 | "Literals for fixed-point formats shall be specified as fixed-point, while literals for floating-point formats…" | OK |
| §13.18.63.3 SR11 | "Editing characters in a picture character-string for a numeric-edited data item are used in editing of the initial value…" | OK |
| §13.18.52.3 SR1 | "The SIGN clause may be specified only for" | OK |
| §13.18.8.3 SR1 | "The BLANK WHEN ZERO clause may be specified only for an elementary item described by its picture character-string as category numeric-edited…" | OK |
| §13.18.8.4 GR1 | "the content of the data item is set to all spaces when the item is a receiving operand and the value being stored is zero" | OK |
| §12.3.7.3 SR22 b | "alphabetic characters A, B, C, D, E, N, P, R, S, V, X, Z, or their lowercase equivalents; or the space" | OK |
| Annex A.3 1 c | "The ability to specify a significand longer than 31 digits in the PICTURE character-string…" | OK |
| Annex A.3 1 d | "The ability to specify an exponent longer than 3 digits in the PICTURE character-string…" | OK |

**One citation was REFUTED and corrected while drafting:** `--check 12.3.7 "alphabetic characters A, B, C, D, E,
N, P, R, S, V, X, Z…"` → **FAIL** ("§12.3.7 exists but does NOT contain…"); `--find` places it at **§12.3.7.3
SR22 b**. The clause is one level deeper than the natural guess — the same inherited-citation shape that
`RV-15.58.4-1`'s own note records for `13.18.40.3` vs `13.18.40.4 GR13 b`.

**Rendered-page verifications** (CLAUDE.md rule 1's diagram obligation, applied to the dense precedence grid):
`scripts/render-spec-page.py 472` (folio 442 — SR8–SR12, confirming the printed standard's own duplicated `a)`
label under SR12, which the transcription reproduces faithfully), `473` (folio 443 — SR12 b through SR25),
`488` (folio 458 — the two-strings paragraph), `489`/`490` (folios 459/460 — **Table 10 in full; row `E` and the
exponent-`+` row read directly off the page and found identical to the Markdown transcription**).
