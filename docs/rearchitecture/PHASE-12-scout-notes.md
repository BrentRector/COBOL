# PHASE-12 — Verified spec anchors + code seams (retained reference)

> **STATUS: RETAINED REFERENCE (produced at PHASE-12 kickoff; the phase itself is NOT STARTED).** This is the
> persisted output of the P12 anchor re-scout (6 parallel read-only scouts + an adversarial verify pass,
> 2026-07-17), run per the standing P10/P11 lesson (`feedback_persist_anchor_rescout`: *every multi-wave phase
> re-scouts its anchors spec-first, persists the result, and trusts it over the drift-prone phase plan*). Each
> section is one scout's verified findings: exact ISO/IEC 1989:2023 §/GR/SR anchors as `specs/ISO_COBOL.md`
> numbers them (with line numbers), and the code seams (file:line) each feature touches in the CURRENT tree.
>
> **Why this doc exists / how to trust it.** The `PHASE-12-m3-2014-deltas.md` plan was authored 2026-07-07,
> **before P8/P9/P10/P11 landed** — so nearly every code-state claim in it is stale, and several spec anchors
> drifted. This re-scout found **one spec-faithfulness inversion** (the IEEE-754 fidelity claim, §float-family),
> **two large already-done features the plan treats as absent** (PROGRAM-POINTER, the E-picture staging), a
> **severe diagnostic-band collision** (17 of the 20 planned codes are already live), a **mis-identified feature**
> (TYPE TO is not a pointer form), and a **wrong edition span** (`>>PROPAGATE` is live-in-2023, not ≤2014). Trust
> THIS doc over the phase plan for anchors; the plan's STEP STRUCTURE is still the execution skeleton, but every
> step needs the corrections below applied before coding. Line numbers were captured at HEAD `91ff7301` and will
> drift as the tree changes — re-Grep before editing.
>
> Companion to `PHASE-12-m3-2014-deltas.md` (the step plan). Baseline battery at scout time (green):
> **3521 conformance · 301 unit · 33 characterization · legacy 1196+636 · NIST 353 MATCH** — the plan's
> "3166 conformance / 281 unit" exit-criteria figures are P7-era and stale (also stale in
> `DESIGN-codegen-backend.md:402`, `DESIGN-data-model.md:351`; sweep per `feedback_propagate_reconciliations`).

---

## 0. TL;DR — the load-bearing corrections (apply BEFORE coding each step)

Ranked by consequence. **[VERIFIED]** = an adversarial verify agent independently re-checked and UPHELD/MODIFIED
the scout (never REFUTED). **[SELF-VERIFIED]** = re-checked directly against the spec in this session.
**[SCOUT-ONLY]** = the float-family verifiers hit the Fable-5 usage limit; the finding is single-source (still a
spec/code citation, but confirm at step start).

1. **IEEE-754 fidelity claim is INVERTED — a spec-faithfulness error (Step 7). [SELF-VERIFIED]**
   The plan says "backing FLOAT-BINARY-128 by `double` and FLOAT-DECIMAL-16/34 by `System.Decimal` is a conforming
   implementor choice per §13.18.60.4 GR13". **Wrong.** GR13 (spec:22824) makes ONLY `FLOAT-SHORT/LONG/EXTENDED`
   implementor-defined; **GR14-18 (spec:22826-22867) PIN** `FLOAT-BINARY-32/64/128` to ISO/IEC 60559:2020
   binary32/64/128 and `FLOAT-DECIMAL-16/34` to decimal64/decimal128. `double`-backed binary128 / `System.Decimal`-
   backed decimals are **non-conforming representations**. The only spec escape is **Annex A.3 items 17/19**
   (spec:40154/40158): these usages are *processor-dependent* — a processor may not support them at all, but if it
   does, the format is IEEE-pinned. Honest Step-7 options: (a) implement binary32/64 natively (`float`/`double`) and
   declare binary128 + decimal16/34 **processor-dependent non-support** per A.3 17/19; or (b) implement the pinned
   formats for real. **Do NOT cite GR13/§4.2.15 as a licence for the standard usages.** (This is the P11-CONCATENATE
   class of finding — a plan premise that contradicts the spec.)

2. **PROGRAM-POINTER is fully DONE as a COBOL-2002 feature — Step 8 is largely already landed. [VERIFIED]**
   P10 Step 7 (after the plan was written) landed the whole PROGRAM-POINTER pipeline: `PROGRAM_POINTER` lexer token,
   `programPointerUsage` in `usageKeyword`, `Usage.ProgramPointer` + `PicCategory.ProgramPointer`, the
   `ProgramPointer` runtime record struct on the ONE `ProgramTable` (`EntryOf`/`CallPointer`), SET Format 9 +
   `SET … TO ENTRY`, CALL-through-pointer, a runnable `tests/conformance/2002/program_pointer.cob`, 4 negative tests,
   and **active** matrix rows `usage-program-pointer-2002` + `user-word-program-pointer-2002`. The plan's §7 table
   ("2014", row `program-pointer-2014`, test `2014/program_pointer`) and its `{is2014()}?` gate would be a
   **regression** (rejecting valid `--std 2002`). Step 8's real remaining PROGRAM-POINTER scope = only the **restricted
   `TO program-prototype-name` form** (GR25, staged loud on COBOLNET0899) and the ISO **`ADDRESS OF PROGRAM`** spelling
   (§8.4.3.13 — only the non-ISO `SET … TO ENTRY` spelling exists today).

3. **FUNCTION-POINTER surface is DONE too; only its SEMANTICS remain (genuinely 2014). [VERIFIED]**
   Token, `functionPointerUsage` (superset-parsed at every edition), `Usage.FunctionPointer`, the 0900-below-2014
   introduction gate, the 0899 staged-loud rejection (`DiagnosticCatalog.UsageFunctionPointer`), the **pending** row
   `usage-function-pointer-2014`, and the negative test all exist. The planned new code **COBOLNET1552 is unnecessary**
   (duplicates the live 0899 gate). Remaining Step-8 work = the runtime function-address carrier + SET **Format 8** +
   **mandatory `TO`** enforcement (ISO requires `FUNCTION-POINTER TO proto`, no brackets — grammar currently parses
   `TO` optional) + `ADDRESS OF FUNCTION` (§8.4.3.12). **Compile-time function prototypes already exist**
   (`BinderDriver.BuildUserFunctionTable` name-keyed protos + REPOSITORY `UserFunctionNames` + the
   `FunctionPrototype2002` gate) — the blocker is the runtime side, not prototypes.

4. **Diagnostic band 1540-1559 COLLIDES — 17 of 20 codes are already live. [VERIFIED]**
   The plan's "1538 is the current TYPEDEF high-water" is stale; the live 15xx high-water is **1559**. `1540/1541/1545`=
   concat §8.8.3.2 (P10), `1542`=ANY LENGTH (P9), `1543/1544/1546`=P11 intrinsics, `1547/1548/1549`=CONSTANT (P10),
   `1553`=linkage, `1554`=UDF, `1555-1557`=SAME AS (P10 — the plan's own Step 10 admits this, self-contradicting its
   band table), `1558`=EXTERNAL type, `1559`=RW PRESENT WHEN/VARYING. **Only 1550/1551/1552 are free** inside the band
   (and `DiagnosticCatalog.cs:93,176` already earmark exactly those three for P12). **P12 must keep 1550/1551/1552 and
   draw the rest from 1561-1599** (39 free; the whole 16xx century is free); honor the single-code **1560 PHASE-13
   earmark**. Concretely: the plan's `COBOLNET1540/1541` (DYNAMIC LENGTH) and `COBOLNET1545` (external float) **must be
   renumbered** — those codes are live concat diagnostics with goldens pinning their text.

5. **`>>PROPAGATE` is LIVE in 2023, not removed — the "≤2014" span is wrong (Step 9). [VERIFIED]**
   §7.3.21 is a current 2023 section, listed in the §7.1 directive table and load-bearing in §14.6.13 EC default
   handling; Annex E/F carry no removal. The correct action is an **INTRODUCTION gate** (reject below its introducing
   edition, almost certainly **2002** — same EC-directive era as `>>TURN`, which is gated at 2002+ via COBOLNET0875;
   confirm against the 1989:2002 text, not in-repo) with **no top-end gate**. The code claim is also inverted: the
   directive is already recognized-and-silently-consumed via `KnownIgnoredDirectives` (a directive-name set), NOT a
   `>>DEFINE` variable. **Ownership conflict for the supervisor:** P9 close + `ISO2023_CONFORMANCE_PLAN.md` defer
   `>>PROPAGATE` SEMANTICS to P13, but the P13 doc has no owning step and its Depends-on line labels it as P12 scope.

6. **TYPE TO is NOT a pointer form — Step 10 mis-identifies it. [VERIFIED]**
   In the 2023 spec, "TYPE TO" is Format 1 of the ordinary TYPE clause: §13.18.57.2 `TYPE TO type-name-1` where `TO`
   is a plain **optional word** (i.e. `TYPE [TO] type-name-1`). A literal `TYPE TO` grep returns zero because the
   underline markup splits the phrase — that is likely how the "pointer-target form" mislabel arose. The real
   pointer-target constructs are `USAGE POINTER [TO type-name-1]` (restricted data-pointer, §13.18.60.2 / Annex
   D.9.2.2), which dovetails with Step 8. The §7 table also has swapped citations: **SAME AS = §13.18.49** (not
   §13.18.57), **TYPE = §13.18.57**, **TYPEDEF = §13.18.58** (the table inverts the last pair). Same mislabel lives in
   `ISO2023_CONFORMANCE_PLAN.md:644` and `COBOLNET_DATA_MODEL_DESIGN.md:434` — fix all three.

7. **DYNAMIC LENGTH: the real work is genuinely unlanded, but the plan's SEMANTIC anchors drift (Steps 4/5). [VERIFIED]**
   No `DynamicLength`/`CobolDynString` handling exists anywhere in `src/` or `tests/` — Steps 4/5 are real. But: (a) the
   master exclusion rule is **§13.16.3 SR18** (only level-number/entry-name/PICTURE/USAGE/**VALUE** permitted), not the
   plan's §13.18.19.3; (b) **VALUE is permitted and sets a NON-ZERO initial length** (§8.6.4) — the plan's "emit a field
   initialized to empty" is wrong for VALUE'd entries; (c) truncation-on-overflow is **§8.5.1.10.4**, not GR2; (d)
   `FUNCTION LENGTH` on a dynamic-length item returns the current length **in BYTES** (§15.50.4 rule 6) — a PIC N
   naive character count would be wrong.

8. **VersionMatrixTests method names in the plan are fictional. [VERIFIED]**
   The plan names `ActiveConstruct_CompilesIffEditionAllows` and `PendingRow_HasActivationContract` — neither exists.
   Real: `[Theory] Construct_MatchesEditionExpectation` (VersionMatrixTests.cs:78) and `[Fact]
   PendingRows_AreCataloguedWithActivationContracts` (:65). A **pending row requires the literal string "PENDING" in
   its `description`** + a non-whitespace `vcr` + a non-whitespace `source` (so a new `type-to` pending row needs all
   three). Class-name filters (`--filter VersionMatrixTests`) still work.

9. **Corpus reality (Steps 1/3). [VERIFIED]** `tests/conformance/2002/` EXISTS with a manifest and already has **nine
   enabled float programs** incl. `float_usage` (all three trio usages) — Step 3's "add float_trio to fill a gap" is
   additive, not a gap-fill. `tests/conformance/2014/` has 18 enabled programs (all 10 `dyn_*` present). `CorpusRunnerTests`
   hard-codes the four dirs `2002/2014/2023/negative`; there is no `1985/`. `FloatFamilyTests` does NOT exist (Step 7's
   filter matches nothing until created). `scripts/guard.ps1` does NOT exist — use `bash scripts/guard-fast.sh` only.

---

## 1. `spec:dynamic-length` — DYNAMIC LENGTH elementary items (Steps 4/5/6)

### Verified anchors

| § | Title | spec line | verdict |
|---|---|---|---|
| 8.5.1.10 | Dynamic-length elementary items (.1 General 8265, .2 Structure 8278, .3 Location, .4 Operations 8292) | 8260 | CONFIRMED |
| 13.18.19 | DYNAMIC LENGTH clause (.2 format 18551, .3 SR 18556, .4 GR 18567) | 18541 | CONFIRMED |
| 13.16.2 | Format 1 data-description entry (DYNAMIC LENGTH is one clause here — ANY elementary level) | 17118 | CONFIRMED |
| 13.16.3 SR18 | **The master exclusion**: with DYNAMIC LENGTH the only other clauses permitted are level-number, entry-name, PICTURE, USAGE, VALUE | 17265 | CONFIRMED |
| 13.18.19.3 SR1 | PICTURE shall be exactly one `N` or one `X` | 18556 | CONFIRMED |
| 13.18.19.3 SR4 | LIMIT ≤ the structure max when a structure-name is given | 18562 | CONFIRMED |
| 13.18.19.4 GR1 | **MINIMUM** length is zero; the picture symbol determines the class (NOT "initial length 0") | 18567 | PARTIAL (plan misattributes) |
| 13.18.19.4 GR2 | Without LIMIT the max is implementor-defined (NOT truncation) | 18569 | PARTIAL (plan misattributes) |
| 8.6.4 | VALUE defines initial length; absent VALUE the initial length is zero | 8784 | CONFIRMED |
| 8.5.1.10.4 | Truncation-on-overflow ("truncated on the right as necessary"); receiving/sending semantics | 8298 | CONFIRMED |
| 8.5.1.10.1 | Max size = smallest of {LIMIT, largest integer storable in the PREFIXED usage, implementor max} | 8267 | CONFIRMED |
| 15.50.4 rule 6 | FUNCTION LENGTH returns the current length **in bytes** (prefix/delimiter excluded) | 36492 | CONFIRMED |
| 15.14.4 rule 5 | BYTE-LENGTH likewise current length in bytes | 34668 | CONFIRMED |
| 12.3.7 | SPECIAL-NAMES `DYNAMIC LENGTH STRUCTURE … IS [SIGNED][SHORT] PREFIXED\|DELIMITED\|physical-name` | 14081 | CONFIRMED |
| 14.9.39.2 Format 16 | **2023-only** `SET [SIZE OF] data-name TO …` (SR33/34, GR37-39; EC-STORAGE-NOT-AVAIL) — DEFER to P13 | 31248 | CONFIRMED |
| E.3.3 item 17 | The 2014→2023 delta ("SET enhanced to set its length") = VCR row 60 | 50271 | CONFIRMED |
| A.4.5 | DYNAMIC LENGTH is an OPTIONAL language-element module | 40339 | CONFIRMED |

### Drift the executor must correct

- **[VERIFIED]** Plan Step 12 "reject REDEFINES/OCCURS per §13.18.19.3" points at the wrong §. The rejection authority
  is **§13.16.3 SR18** (whole-clause allowlist). Reinforced by: §13.18.32.3 SR4 (JUSTIFIED, 19266), §13.18.44.3 SR17
  (REDEFINES, 21533), §13.18.5.3 SR2 (BASED, 17718), §13.16.3 SR13 (CONSTANT RECORD, 17251).
- **[VERIFIED]** Plan Step 5 "emit a field initialized to empty (min length 0, GR1)" is wrong for VALUE'd entries.
  VALUE is permitted (SR18) and sets a non-zero initial length (§8.6.4). Initial length is 0 only absent VALUE (always
  in the FILE SECTION per §13.4.4 GR1:16189; INITIALIZE §14.9.20.4 GR7:28011 and program init §14.6.2.3.2 rule 7:24104
  set it to 0).
- **[VERIFIED]** Truncation is §8.5.1.10.4, not GR2. GR2 is only the limit definition.
- Lexer tokens exist (line numbers drifted +22 from the plan): `DYNAMIC` CobolLexer.g4:388, `LIMIT` :432, `LENGTH`
  :466. **LL-disjoint** from OCCURS DYNAMIC (occursClause CobolData.g4:414-423 always leads with `OCCURS`; `DYNAMIC`
  appears nowhere else in CobolData.g4) — no lexer change needed. Watch the optional structure-name slot not swallowing
  a following clause keyword.

### Interaction inventory an implementation must cover (from §8.5.1.10.4 + siblings)
MOVE §14.9.25.4 GR2/GR3 (zero-length literals NOT space/zero-substituted for dynamic-length receivers, 28895);
INITIALIZE §14.9.20.4 GR7 (length→0); intrinsics LENGTH/BYTE-LENGTH (bytes); UNSTRING §14.9.43.4 (current length);
group comparison §8.8.4.2.17 / §8.5.1.12 (variable-length group); parameter conformance §14.2.3 (same category AND
same structure-name); prohibitions: ADDRESS OF §8.4.3.11.3 SR6, FILE STATUS §12.4.5.8.3 SR3, CONSTANT §13.10.3 SR12,
SORT/MERGE keys §14.9.40.3 / §14.9.24.3.

---

## 2. `spec:float-family` — FLOAT usages + external-float `E` PICTURE (Steps 2/3/7)

### Verified anchors

| § | Title | spec line | verdict |
|---|---|---|---|
| 13.18.60 | **USAGE clause** (all eight float usages in the .2 general format) | 22636 | DRIFT — plan cites §13.18.59 |
| 13.18.59 | UNDERLINE clause (the plan's wrong citation) | 22605 | — |
| 13.18.60.4 GR13 | float-short/long/extended = "format suitable for the machine … defined by the implementor" | 22824 | CONFIRMED |
| 13.18.60.4 GR14 | **FLOAT-BINARY-32 = binary32 per ISO/IEC 60559:2020, 3.4** (PINNED) | 22826 | SELF-VERIFIED |
| 13.18.60.4 GR15/16 | FLOAT-BINARY-64 = binary64; FLOAT-BINARY-128 = binary128 (PINNED) | 22843/22847 | SELF-VERIFIED |
| 13.18.60.4 GR17/18 | FLOAT-DECIMAL-16 = decimal64; FLOAT-DECIMAL-34 = decimal128 (PINNED) | 22855/22867 | SELF-VERIFIED |
| 13.18.60.4 GR21 | implementor-defined representation lists ONLY BINARY-* and the FLOAT trio (NOT the standard IEEE usages) | 22906 | SCOUT-ONLY |
| A.3 items 17/19 | FLOAT-BINARY-* and FLOAT-DECIMAL-* are **processor-dependent** (may be absent; if present, IEEE-pinned) | 40154/40158 | SCOUT-ONLY |
| 4.2.15 | "Limits" — program/operand size limits, NOT float precision (a WEAK anchor the plan over-reads) | 2523/2525 | CONFIRMED |
| 13.18.40 | PICTURE clause; external float = **"floating-point numeric-edited item"** (category numeric-edited, NOT a "Display external-float category") | 20234 | PARTIAL |
| 13.18.40.4 GR13b | the significand`E`exponent form; exponent must be `+9`..`+9(4)` (required leading `+`) | 20495 | PARTIAL |
| 13.18.40.3 SR15/SR23 | significand 1-36 digits; sign exclusivity (exponent always `+`) | 20334/20352 | PARTIAL |

### Drift the executor must correct

- **[SELF-VERIFIED] The IEEE-754 fidelity inversion (TL;DR #1).** GR13/GR21 cover only the trio; GR14-18 pin the
  standard usages to ISO/IEC 60559:2020. `double`/`System.Decimal` backing of binary128/decimal is non-conforming.
  Route unimplementable formats through Annex A.3 17/19 processor-dependent non-support. The wrong `§13.18.59` citation
  also lives in `constructs.json` rows and `PicInfo.cs:103` comments — sweep them.
- **[SCOUT-ONLY] FLOAT-BINARY-32 lexes as ONE IDENTIFIER.** `caseInsensitive` (CobolLexer.g4:7) + `NAME_BODY` alpha
  alt (`[a-z][a-z0-9-]*[a-z0-9]`, :638) matches the full 15-char `FLOAT-BINARY-32` by maximal munch, while
  `FLOAT_BINARY` (:197) matches only 12. Step 7's primary plan (`FLOAT_BINARY integerLiteral?`) CANNOT work — take the
  plan's **fallback**: explicit `FLOAT_BINARY_32/64/128`, `FLOAT_DECIMAL_16/34` tokens declared before `IDENTIFIER`
  (same length → first-rule-wins, the existing hyphenated-keyword pattern). Probe tokenization before choosing.
- **[SCOUT-ONLY] The E-picture is already staged LOUD — nothing silent remains.** `PictureAnalyzer.Analyze` `case 'E'`
  (PictureAnalyzer.cs:82) → `StagedNotImplemented` COBOLNET0899 at ≥2002 (:91) + `SkeletonGate = PicExternalFloat2002`
  → COBOLNET0900 below 2002; pinned by `LoudGuardTests.cs:106` + `DataSkeletonEditionTests.cs:47`. Step 7 replaces the
  0899 staging with a live floating-point numeric-edited implementation. The `constructs.json` row description that
  still says "silent-misbind" is stale.
- **[SCOUT-ONLY] Row-id collision:** the plan's planned NEW row `usage-external-float-pic-2002` collides with the
  EXISTING `pic-external-float-2002` (constructs.json:808, pending) — **flip that row**, don't add a duplicate.
- **[SCOUT-ONLY] No VCR/Annex E rows for any float introduction** (Annex E is 2014→2023 only). Cite
  `reserved-words.json` (`FLOAT-BINARY-*` "added 2014") + `constructs.json` `introducedIn` tags, NOT VCR rows.
  Editions themselves hold: trio + E-pic = 2002 (repo-provisional), FLOAT-BINARY/DECIMAL = 2014.

### Code seams (current line numbers — drifted heavily from the plan)
- `PicInfo.cs`: `Usage.FloatShort/Long/Extended` = **106/108/111** (plan said 93-97). Switches to extend: **`ClrType`**
  (238-267; there is **NO `CsTypeName`**), `IsFloat` (271), `IsSingle` (276), **`DefaultInitializer`** (279-298; there
  is **NO `ZeroLiteral`**). Every switch must gain an arm per new `Usage.*` member (missed arm = silent mis-type,
  `feedback_scan_all_similar`).
- `ParseUsage` is in **`PictureAnalyzer.cs:291-358`** (NOT PicInfo.cs); trio arms 344-349, exhaustive loud default
  350-356 (new keywords MUST add arms).
- `usageKeyword` = **CobolData.g4:357-387**; `FLOAT_SHORT/LONG/EXTENDED` at 371-373; `FLOAT_BINARY/FLOAT_DECIMAL`
  absent; already carries `programPointerUsage`/`functionPointerUsage`/`objectReferenceUsage`.
- `DataBinder.cs` float routing = 1760/1779 (plan said 1158/1176). Runtime carrier = `CobolFloat.cs` (a **static
  helper class**, `Display`/`ToScaled`; no binary128/decimal formats today).
- Matrix rows (all **pending**): `usage-float-short-2002` 343, `usage-float-binary32-2014` 356 (**missing the
  `expectDiagnostic` field**), `pic-external-float-2002` 808, `usage-float-long-2002` 859, `usage-float-extended-2002`
  872. `usage-float-decimal-2014` genuinely absent (real add). `tests/conformance/2002/` already has 9 float programs;
  `FloatFamilyTests` does not exist.

---

## 3. `spec:pointers` — PROGRAM-POINTER / FUNCTION-POINTER data (Step 8)

### Verified anchors

| § | Title | spec line | verdict |
|---|---|---|---|
| 8.5.2.15 | Program-pointer category | 8602 | CONFIRMED |
| 8.5.2.7 | Function-pointer category | 8504 | CONFIRMED |
| 13.18.60.2 | USAGE general format: `PROGRAM-POINTER [TO proto]` (TO optional); `FUNCTION-POINTER TO proto` (**TO REQUIRED**) | 22684-22686 | CONFIRMED (plan sketch wrong) |
| 13.18.60.3 SR14 | pointer phrases only on an elementary item at level 1 or under a STRONG type | 22752 | CONFIRMED |
| 13.18.60.4 GR24/25/26 | program-pointer / restricted program-pointer / function-pointer semantics | 22945/22949/22960 | CONFIRMED |
| 14.9.39 | **SET statement** (plan's "§14.9.31" is RECEIVE); Format 8 function-ptr-assign 31179, Format 9 program-ptr-assign 31183 | 31107 | DRIFT |
| 14.9.39.3 SR20/21 | Format 8/9 operand-category rules (signature conformance for function-pointers) | 31414 | CONFIRMED |
| 8.4.3.13 | `ADDRESS OF PROGRAM {identifier\|literal\|program-prototype-name}` (unimplemented; only `SET … TO ENTRY` exists) | 7568 | CONFIRMED |
| 8.4.3.12 | `ADDRESS OF FUNCTION {function-prototype-name\|identifier}` (unimplemented) | — | CONFIRMED |

### Reality vs the plan (the plan's Step-8 premise "data absent" is stale post-P10)

- **[VERIFIED] PROGRAM-POINTER is a 2002 feature, fully live.** `PROGRAM_POINTER` CobolLexer.g4:307; `programPointerUsage`
  CobolData.g4:393-395; `Usage.ProgramPointer` PicInfo.cs:98 + `PicCategory.ProgramPointer` :46; runtime
  `ProgramPointer` record struct `Control/ProgramPointer.cs:14` (`Null` 17, `SameTarget` 24); resolution on the ONE
  `ProgramTable` (`EntryOf` 173, `CallPointer` 191); SET Format 9 + `SET … TO ENTRY` in `SetBinder.cs` (104/150),
  emit `PtrEmitter.cs:119` / `SetEmitter.cs:46`; test `tests/conformance/2002/program_pointer.cob` + 4 negatives;
  rows `usage-program-pointer-2002` (active, constructs.json:394) + `user-word-program-pointer-2002`. **Row
  `program-pointer-2014` exists nowhere but the plan.** Residues: restricted GR25 form (staged loud 0899) + ISO
  `ADDRESS OF PROGRAM` spelling.
- **[VERIFIED] FUNCTION-POINTER surface done; semantics genuinely 2014.** Token :308; `functionPointerUsage`
  CobolData.g4:399-401 (TO **optional** — ISO requires it, enforce at implement time); `Usage.FunctionPointer`
  PicInfo.cs:102; reject `PictureAnalyzer.cs:324` → `DiagnosticCatalog.UsageFunctionPointer` (0899, catalog:138);
  gate via `VersionConformancePass.cs:279` → `Constructs.UsageFunctionPointer2014`; **pending** row
  `usage-function-pointer-2014` (constructs.json:407) + `user-word-function-pointer-2014` (active); negative test
  `function-pointer-staged.cob`. **COBOLNET1552 is unnecessary** (0899 already covers it). Remaining: runtime
  function-address carrier + run-unit function table + SET Format 8 + mandatory-TO + `ADDRESS OF FUNCTION`.
- **[VERIFIED] Function prototypes already bindable at compile time:** `BinderDriver.BuildUserFunctionTable` (375-418,
  name-keyed `defs`/`protos`, GR11(a) merge), `BoundUnit.IsPrototype` (165), REPOSITORY `DataBinder.UserFunctionNames`
  (199), gate `Constructs.FunctionPrototype2002` (VersionConformancePass.cs:92). Re-scope Step 8 from "prototypes may
  not exist" to "compile-time prototypes exist; build the runtime side".

### Code-comment §-errors to fix while in the area (P10 residue)
`ProgramPointer.cs:6` and `PicInfo.cs:41/96` cite **§8.5.2.7** (function-pointer category) for PROGRAM-pointer — should
be **§8.5.2.15**. The stale grammar comment moved to `CobolData.g4:529-531` and now (falsely) says pointer tokens are
"not yet defined" — it belongs to `initializeCategory`; the real residue there is that **INITIALIZE REPLACING** still
lacks the 2002+ category names (DATA-POINTER/FUNCTION-POINTER/PROGRAM-POINTER/OBJECT-REFERENCE/BOOLEAN/NATIONAL,
spec §14.9.20) — a separate gap, note it.

---

## 4. `spec:propagate-condexpr` — `>>PROPAGATE` + conditional-expr + increased limits (Step 9)

### Verified anchors

| § | Title | spec line | verdict |
|---|---|---|---|
| 7.3.21 | PROPAGATE directive — **LIVE in 2023** (`>> PROPAGATE ON\|OFF`, OFF default; SR1 between compilation units; GR1 lexically scoped over the compilation group; GR2 propagate as GOBACK RAISING LAST) | 4803 | PARTIAL — plan's "≤2014" is DRIFT |
| 7.1 | directives/stage table lists `PROPAGATE \| Compilation` (still current) | 3272 | CONFIRMED |
| 14.6.13.1.3 rule 6 | EC default handling references an applicable PROPAGATE ON directive | 24546 | CONFIRMED |
| 8.8.4 | Conditional expressions | 9478 | CONFIRMED |
| 8.8.4.2.17 | variable-length-group comparison (the ONLY plausibly-2014 §8.8.4 delta — rides DYNAMIC LENGTH, belongs to Steps 4/5) | 9777 | PARTIAL |
| 8.8.4.12 | abbreviated combined relation conditions — already FULLY implemented edition-invariant (`ConditionBinder.cs:274-495`) | — | CONFIRMED |
| 4.2.15 | "Limits" — delegates size limits to the implementor → "increased limits" is a safe no-op | 2523 | CONFIRMED |

### Drift the executor must correct

- **[VERIFIED] `>>PROPAGATE` is not removed-in-2023.** Do an INTRODUCTION gate (likely 2002; confirm vs 1989:2002),
  NOT a "≤2014" span (which would reject valid 2023 programs). Mechanism: the proven `>>TURN` pattern —
  `Frontend.cs:89` `ConditionalCompilationProcessor.Process(text, leaveTurnDirectives:true)` then `Frontend.cs:103`
  `TurnDirectiveProcessor.Process(text, DialectLevel, diagnostics, sourcePath)` with the edition gate at
  `TurnDirectiveProcessor.cs:48-53` (COBOLNET0875 below 2002). Remove `PROPAGATE` from `KnownIgnoredDirectives`
  (ConditionalCompilationProcessor.cs:36) or add a leave-flag, and add a dedicated gated stage.
- **[VERIFIED] The plan's code claim is inverted.** `ConditionalCompilationProcessor.cs:36` lists `PROPAGATE` in
  `KnownIgnoredDirectives` (a **directive**-name set, doc-comment :29-30), consumed at :136 at every edition — NOT a
  `>>DEFINE` variable. The only other trace: `CallEmitter.cs:153` `// rule 3 PROPAGATE ON: directive not implemented
  (residue)`.
- **[VERIFIED] Ownership conflict** (supervisor decision): P9 close (`PHASE-09-*.md:32,82`) + `ISO2023_CONFORMANCE_PLAN.md:683`
  defer `>>PROPAGATE` SEMANTICS to P13; the P13 doc has NO owning step (its Depends-on line even labels it P12 scope).
  Decide the semantics owner (P12 Step 9 is viable — the directive is live-in-2023 and the EC engine is DONE — or add
  the missing P13 step) and fix the edition span in whichever doc owns it.
- **[VERIFIED] The conditional-expression half of Step 9 is unsatisfiable as written.** The VCR has ZERO 2014
  conditional-expression rows (no 2002→2014 introduction table exists in-repo; its scope note says so). §8.8.4.12
  abbreviated relations are already edition-invariant; boolean conditions landed 2002-gated in P10/P11. The only
  plausible 2014 §8.8.4 delta is §8.8.4.2.17 (variable-length-group comparison), which belongs to the DYNAMIC LENGTH
  work. Naming anything more requires the 1989:2014 text.
- **[VERIFIED] "Increased limits" drop is sound** (§4.2.15). WiseOwl COBOL imposes no artificial translator limits;
  nothing concrete is skipped. One-line DEVLOG note per the plan.

---

## 5. `spec:typedef-occurs` — TYPEDEF / SAME AS / TYPE TO edges + OCCURS DYNAMIC (Steps 1/10)

### Verified anchors

| § | Title | spec line | verdict |
|---|---|---|---|
| 13.18.57 | **TYPE clause** (Format 1 `TYPE TO type-name` — `TO` is an OPTIONAL word, i.e. `TYPE [TO]`) | 22321 | DRIFT — plan calls TYPE TO a pointer form |
| 13.18.58 | **TYPEDEF clause** | 22557 | CONFIRMED (plan §7 table inverts 57/58) |
| 13.18.49 | **SAME AS clause** (plan §7 table wrongly cites §13.18.57) | 21731 | CONFIRMED |
| 13.18.60.2 / D.9.2.2 | the REAL pointer-target form: `USAGE POINTER [TO type-name]` (restricted data-pointer) | 22684 / 44426 | CONFIRMED |
| 13.18.38.2 Format 4 | OCCURS DYNAMIC `[CAPACITY IN dn][FROM i][TO i][INITIALIZED]` | 19854 | CONFIRMED |
| 8.5.1.9 | Dynamic-capacity tables | 8184 | CONFIRMED |

### Reality vs the plan

- **[VERIFIED] TYPE TO is not a pointer form** (TL;DR #6). Re-anchor the Step-10 `type-to` row to either (a) the TYPE
  clause's optional word `TO` (§13.18.57.2 Format 1 — a trivial grammar addition, arguably already part of the live
  TYPE clause; current `typeClause : TYPE IS? IDENTIFIER` at CobolData.g4:299 even accepts an `IS` the 2023 Format 1
  doesn't show), or (b) what the plan likely meant — restricted pointers `USAGE POINTER [TO type-name]` (§13.18.60.2),
  which belongs with Step 8. No `TYPE TO`/restricted-pointer handling exists in `src/`. Fix the same mislabel in
  `ISO2023_CONFORMANCE_PLAN.md:644` + `COBOLNET_DATA_MODEL_DESIGN.md:434`.
- **[VERIFIED] SAME AS is DONE** (P10 Step 16): `ExpandSameAs` DataBinder.cs:1210 on the ONE `CloneItem` (:1118);
  `sameAsClause` CobolData.g4:317-319; SRs COBOLNET1555/1556/1557 (DiagnosticCatalog.cs:173-192); golden
  `tests/conformance/2002/typedef_same_as.cob`; `SameAsTests.cs:14`; row `same-as-clause-2002` active. The plan's §7
  "same-as-2014 (pending)" row does not exist and is not needed. Step 10's commit template "catalogue SAME AS as
  pending" is stale — it's active.
- **[VERIFIED] TYPEDEF/TYPE/OCCURS DYNAMIC rows all effectively active** (a missing `status` field defaults to
  `active` per `VersionMatrixTests.cs:42`): `typedef-def-2002` (constructs.json:318), `type-clause-2002` (:306),
  `same-as-clause-2002` (explicit active, :331), `occurs-dynamic-2014` (:294). No `type-to` row exists — Step 10
  creates it (with `"PENDING"` in the description + `vcr` + `source`).
- **[VERIFIED] OCCURS DYNAMIC corpus complete:** 10 `dyn_*.cob` in `tests/conformance/2014/`, all enabled; test
  classes `OccursDynamicGuardTests` (:14), `OccursDifferentialTests` (:15), `OdoDifferentialTests` (:15),
  `VersionMatrixTests` (:22) all exist. Step 1 is a verify-and-lock, no new code expected.

---

## 6. `spec:diag-band` — diagnostic band + registry + corpus/battery baseline (cross-cutting)

### Live 15xx inventory (the collision — TL;DR #4) **[VERIFIED]**

`1501-1549` and `1553-1559` are ALL live in compiler source; the live high-water is **1559** (not the plan's 1538).
The plan's `1540-1559` sub-allocation collides wholesale:

| plan sub-band | plan's intent | ACTUAL live owner | file:line |
|---|---|---|---|
| 1540/1541 | DYNAMIC LENGTH | concat §8.8.3.2 SR1/ALL-figurative (P10) | ConcatFolder.cs:28/30 |
| 1542 | (DYNAMIC LENGTH) | ANY LENGTH §13.18.2 SR (P9) | DataBinder.cs:1923 |
| 1543/1544/1546 | float/E-symbol | P11 intrinsic validators | IntrinsicBinder.cs:96/808/494 |
| 1545 | external float | concat result >8191 §8.8.3.2 SR2-4 (P10) | ConcatFolder.cs:32 |
| 1547/1548/1549 | (float) | CONSTANT entry / CONSTANT RECORD (P10) | VersionConformancePass.cs:633/645, DataBinder.Constants.cs:33 |
| 1550/1551/1552 | pointers | **FREE — earmarked P12** | (catalog comments :93,176) |
| 1553/1554 | pointers | linkage BY VALUE / UDF | DataBinder.Linkage.cs:177, UdfBinder.cs:137 |
| 1555/1556/1557 | conditional-expr | SAME AS §13.18.49 (P10 — plan Step 10 admits this) | DiagnosticCatalog.cs:178/185/192 |
| 1558 | (conditional-expr) | EXTERNAL type §13.18.22 | DiagnosticCatalog.cs:212 |
| 1559 | (conditional-expr) | RW PRESENT WHEN/VARYING (P10) | VersionConformancePass.cs:686 |

**Correction:** keep **1550/1551/1552** (already earmarked P12 in `DiagnosticCatalog.cs:93,176`); take the rest from
**1561-1599** (39 free; 16xx century fully free); honor the single-code **1560 PHASE-13 earmark** (`PHASE-13-*.md:160`).
Renumber the plan's `COBOLNET1540/1541` (Step 5) and `COBOLNET1545` (Step 7) — those are live concat codes with goldens
pinning their text.

### Registry mechanism (plan line 77 is drift) **[VERIFIED]**

There is **no `src/Cobol.Net.Compiler/Diagnostics/`** dir and no "P-test rewrite" anywhere in the repo. The live
registry (since P2.10a, landed the day AFTER the plan was written) is **`src/Cobol.Net.Editions/Diagnostics/
DiagnosticCatalog.cs`** (public static `DiagnosticDescriptor` fields; `All` reflects them) + `DiagnosticDescriptor.cs`.
A new catalog-routed code touches THREE things: the descriptor in `DiagnosticCatalog.cs`, the emit site
(`Edition.Error(DiagnosticCatalog.X, …)`), and the regenerated **`docs/DIAGNOSTICS.md`** (`pwsh
scripts/gen-diagnostics-doc.ps1` or `COBOLNET_WRITE_DIAGNOSTICS_DOC=1`). Enforced by
`tests/Cobol.Net.Tests.Unit/DiagnosticRegistryDriftTests.cs` (unique Ids :26; no bare literal for split codes
0899/1533 :37; doc-in-sync :55). `EditionCodes.cs` holds ONLY 0900-0903. Bare-string emit
(`Edition.Error("COBOLNETnnnn", …)`) is still legal for non-split codes, but the P10/P11 pattern for new rule-FAMILY
codes is catalog descriptors. Frontend parse layer has its own catalogue (`src/Cobol.Net.Frontend/Diagnostics/
DiagnosticDescriptors.cs`). **Also fix `PHASE-13-*.md:597`** which carries the same stale "band head 1538" claim.

### Corpus + battery baseline **[VERIFIED]**

Four corpus dirs, each with a manifest: `2002/` (108 enabled), `2014/` (18 enabled — all `dyn_*` present), `2023/`
(13), `negative/` (102). No `1985/`. `CorpusRunnerTests` hard-codes those four (`InlineData` :35-38) + enumerates
`*.cob` (:43); a NEW corpus dir needs an `InlineData` row + `EnabledPositive` wiring. `scripts/guard-fast.sh` EXISTS;
`scripts/guard.ps1` does NOT (a future P14/P15 deliverable — reword the plan's clause, don't delete the reference).
Battery figures in the plan's exit criteria (3166/281) are P7-era stale; current = 3521/301/33/NIST 353 (also stale in
`DESIGN-codegen-backend.md:402`, `DESIGN-data-model.md:351`). `constructs.json` = 149 rows (134 active / 15 pending).
`VersionMatrixTests` real contracts: `Construct_MatchesEditionExpectation` (:78, active rows only, missing status→active
:42), `PendingRows_AreCataloguedWithActivationContracts` (:65 — pending needs literal `"PENDING"` in description + `vcr`
+ `source`).

---

## 7. Verification status of this re-scout

The workflow ran 6 scouts (all completed) + an adversarial verify pass on each DRIFT/PARTIAL claim. **13 verify agents
completed; all returned UPHELD or MODIFIED — not one scout claim was REFUTED** (the phase plan drifted in every
checked case). **17 verify agents failed on the Fable-5 usage limit** — those were mostly the float-family and some
pointer/propagate/diag-band claims. To close that gap:

- The single most consequential unverified finding — the **IEEE-754 fidelity inversion** — was **re-checked directly
  against the spec in this session** (GR13 vs GR14-18 at spec lines 22824-22867) and CONFIRMED. It is marked
  `[SELF-VERIFIED]`.
- The pointer, `>>PROPAGATE`, TYPE TO, diagnostic-band, DYNAMIC LENGTH semantics, VersionMatrixTests, and battery
  findings all have a completed `[VERIFIED]` verdict.
- The remaining `[SCOUT-ONLY]` float items (FLOAT-BINARY-32 tokenization, PicInfo member-name drift, the E-picture
  staging state, the row-id collision, GR21/A.3 anchors) are single-source citations with file:line / spec-line
  evidence but no independent verify pass. **Confirm each at the start of Step 7** before relying on it — the same
  spec-first discipline every wave owes its own anchors.

---

## 8. Open decisions for the supervisor (surfaced by the re-scout)

1. **`>>PROPAGATE` semantics owner** — P9/conformance-plan say P13; the P13 doc has no step and points back at P12.
   Pick one (P12 Step 9 is viable) and reconcile all four docs.
2. **IEEE float unimplementable formats** — (a) implement binary32/64 natively + declare binary128 + decimal16/34
   processor-dependent non-support (Annex A.3 17/19), or (b) implement the pinned IEEE formats for real. Option (a)
   is the honest minimal-scope path and matches the "no unsourced conformance claim" rule (`feedback_bare_end`).
3. **`type-to` row re-anchor** — the TYPE-clause optional-word `TO` (trivial) vs restricted `USAGE POINTER [TO
   type-name]` (a real feature that belongs with Step 8's pointer work). Likely the latter is what the plan meant.
