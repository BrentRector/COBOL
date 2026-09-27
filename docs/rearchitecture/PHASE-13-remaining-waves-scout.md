# PHASE-13 remaining waves — persisted spec-first anchor re-scout (Waves C-residual / D / E / F / G / H)

> **⚠ 2026-07-19 NOTE:** `PHASE-13-audit.md` (cited throughout as provenance) was RE-VERIFIED by the
> plan-vs-spec review and DELETED in the plan consolidation — do not look for it; the verified record is
> `PHASE-13-plan-vs-spec-review.md` and the live worklist is the plan §0. This scout stays the WORKING
> DESIGN for the remaining P13 waves; delete it at P13 close.


> **STATUS: the trusted worklist for the remaining Phase-13 work.** Owner rule: trust a persisted spec-first re-scout over the drift-prone phase plan/audit ([[feedback_persist_anchor_rescout]]). Produced by a 9-agent parallel re-scout (each wave independently derived from specs/ISO_COBOL.md with grep-verified line numbers, CLI-probed against the as-built compiler, adversarially checked against PHASE-13-audit.md). Each section is decision-complete: exact section, quoted format, GR-level semantics, the below-2023 gate + diag code, file:line code anchors, a golden program + hand-derived stdout, AUDIT DRIFT caught, and an implementation plan. Implement FROM this doc + the spec; do not re-derive. Companions: PHASE-13-wave-c-scout.md (the 8 already-landed constructs incl. SUPPRESS-WHEN part-B and PICTURE-EDITING C4 partial) and PHASE-13-audit.md (the 71-row as-built table).

> **⛔ EVERY 15xx DIAGNOSTIC NUMBER IN THIS DOC IS STALE AND NON-AUTHORITATIVE — DO NOT COPY ANY OF THEM.**
> At scout time next-free was 1570, and **each wave section independently allocated from 1570**, so the ~40
> occurrences of 1570/1571/1572/1573/1574 below collide with one another *and* with what actually shipped.
> The live band is written ONLY in the plan §0 (the single-write rule; duplication is where drift breeds) —
> **as of 2026-07-19 next-free = `COBOLNET1578`**, allocated only after BOTH scans agree
> (`grep -rho 'COBOLNET15[0-9][0-9]' src | sort -u` AND the `DiagnosticCatalog` descriptor list).
>
> **AS-LANDED MAP (what the scout's guesses actually became — use this to read the sections below):**
>
> | Actual code | Descriptor | Scout section that predicted it | Scout's guess |
> |---|---|---|---|
> | 1570 | `value-numeric-edited-oversize` | Wave G VCR 34 (VALUE numeric-edited SR7) | 1570 ✓ |
> | 1571 | `debug-sub-facility-staged` | Wave F (USE FOR DEBUGGING residual) | 1570 ✗ |
> | 1572 | `merge-in-sort-merge-proc` | Wave G VCR 27 (MERGE prohibition) | 1571 ✗ |
> | 1573 | `external-file-status-consistency` | Wave E VCR 18 | 1570 ✗ |
> | 1574 | `exception-file-argument-not-file` | Wave E/G EXCEPTION-FILE | — (Wave D's `>>DISPLAY` also guessed 1574 ✗) |
> | 1575 | `external-relative-key-consistency` | Wave E VCR 31 | 1571 ✗ |
> | 1576 | `ref-mod-zero-length-malformed-operand` | (P13 review C1 renumber) | — |
> | 1577 | `method-redefines-scope` | (P13 review renumber from 1518) | — |
>
> **Waves E / F / G are LANDED — read their numbers via the table above, never literally.** The sections whose
> codes are still UNALLOCATED (the grammar batch = PICTURE EDITING · PERFORM Fmt 3 · SUPPRESS WHEN · RW SUPPRESS ·
> VALUE Format 2 · file-control COLLATING; plus Wave D directives and Wave H MCS/COMMIT-ROLLBACK/VALIDATE) must
> draw fresh codes from the plan §0 next-free at implementation time.
>
> Unchanged and still valid: introduction gates reuse **COBOLNET0900**; new-reserved-word user-word gates
> **COBOLNET0901**; obsolete-flag warnings **COBOLNET0903**; the §4.2.6 processor-dependent non-support WARNING
> band starts at **COBOLNET1560**.

---

## Wave C — PICTURE EDITING phrase (VCR 62)

### PICTURE EDITING phrase — VCR row 62 (ISO/IEC 1989:2023 §13.18.40, Format 1; new-feature Annex E.3.3 item 19)

**Spec sections** (line-numbered against `specs/ISO_COBOL.md`, all verified against the text):
- **§13.18.40.2 Format 1 syntax diagram** (@20246–20248) — the boxed grammar: `{PICTURE|PIC} IS character-string-1 [ EDITING character-1 [ {IS literal-1 | FOR {NEGATIVE IS literal-2 | POSITIVE IS literal-3}} ] ] …` (the outer bracket + `…` ellipsis = the EDITING phrase is optional and **repeatable**). `EDITING` is underlined = a reserved word.
- **§13.18.40.3 Syntax rules** (Format 1): SR4 max 63 chars in character-string-1 (@20271); **SR8** (@20289) char-1 = any basic letter **except** a CURRENCY-SIGN letter or one of `A B C D E N P R S V X Z` (or lowercase); **SR9** (@20291) literal-1/2/3 are national iff USAGE NATIONAL or 'N' present, else alphanumeric, ≤ 50 chars; **SR10** (@20293) char-1 must appear ≥ 1× in character-string-1; **SR11** (@20295) multiple EDITING phrases must use **distinct** char-1; **SR12** (@20297–20305) `IS literal-1` ⇒ char-1 is a *fixed editing sign control symbol* (simple insertion), `FOR` ⇒ *extended editing sign control symbol* — (a) lit2/lit3 equal width; (b) character-string may then contain only char-1 and `9 . cs P V Z`; (c) the unspecified NEGATIVE/POSITIVE defaults to spaces of the specified literal's width; last-a: extended sign control not on a floating-point edited item; **SR24** (@20354) fixed = one cs + one editing-sign symbol; extended = one *or two* extended sign symbols; **SR25** (@20356–20367) with two extended sign symbols, the **first** EDITING phrase is for the leftmost symbol, the second for the rightmost; **SR26** (@20369) currency-symbol placement admits char-1 as the optional adjacent sign.
- **§13.18.40.5 Editing rules** — Rule 3 *simple insertion* (@20766): "…and, if literal-1 is specified, character-1 are used as the simple insertion editing symbols"; Rule 5 *fixed insertion* (@20778–20784): "Character-1, the currency symbol and `+ - CR DB`…"; "When character-1 is used, and is not a simple insertion character, it represents literal-2 or literal-3 as the insertion characters." Table 7 (@20763) — numeric-edited (fixed-point) admits *All* editing; **floating-point edited** admits simple/special/fixed insertion for the significand only.
- **Table 8 — Results of fixed insertion editing** (@20796–20808) and **Table 9 — Results of floating insertion editing** (@20850–20858).
- **§13.18.40.6 precedence note** (@21010): with EDITING, 'es' (the editing sign symbol) has the same Table-10 precedence as the 'cs' currency symbol in the non-floating-insertion column/row.
- **D.24 worked examples** (@48133–48179) — the informative annex (see AUDIT DRIFT for its known typo).
- **Introduction proof**: Annex **E.3 "Substantive changes probably not affecting existing programs" → E.3.3 "Not affecting" item 19** (@50275: "EDITING phrase … adds the capability to specify a literal of any size for simple insertion and sign-sensitive fixed insertion"); Introduction new-features list (@1223 "User-defined PICTURE clause editing using the EDITING phrase"); **E.2 item 25 reserved-word additions** lists **EDITING** (@49327) — so `EDITING` is a *new 2023 reserved word*, user-definable below 2023.

**Syntax / format** (distilled from the boxed diagram @20248):
```
pictureClause : PIC PIC_STRING editingPhrase* ;
editingPhrase : EDITING literal          // character-1 (one-char alphanumeric/national literal)
                  ( IS literal                                  // literal-1  → simple insertion (sign-independent)
                  | FOR ( NEGATIVE IS literal (POSITIVE IS literal)?   // sign-sensitive fixed insertion
                        | POSITIVE IS literal (NEGATIVE IS literal)? ) )? ;
```
- `EDITING` mandatory; character-1 mandatory (a `literal`, bind-validated to exactly one character satisfying SR8).
- The `[ … ]` after character-1 is optional but in practice one of `IS literal-1` / `FOR …` is present; a bare `EDITING char-1` with no literal declares char-1 a simple insertion of *itself* (degenerate — spec permits, since Rule 3 lists char-1 as a simple insertion symbol; treat char-1 as its own single-char insertion).
- `IS`/`FOR`/`NEGATIVE`/`POSITIVE` — `IS` optional-noise, `FOR`+at-least-one-of NEGATIVE/POSITIVE mandatory in the FOR branch.

**Introduced edition & gate:** COBOL-**2023** (E.3.3 item 19; E.2 item 25 for the reserved word). Below 2023 the whole EDITING phrase is rejected with the standard introduction gate **COBOLNET0900**, fired *recognition-based* from a new `VersionConformancePass.ParseArm.VisitPictureClause` override (mirrors `VisitUsageClause`/`VisitContinueStatement` at `VersionConformancePass.cs:475/486`) when `ctx.editingPhrase().Length > 0`, via a new `Constructs.PictureEditing2023` row. Because `EDITING` is a new reserved word, it must also be admitted to the `cobolWord` funnel (`CobolWords.g4` after `PRESENT` @65) with a companion **`user-word-editing-2023`** row (85→2023 reservation, **COBOLNET0901**) — exactly the XOR/EXCLUSIVE-OR/COMMIT pattern (`ConstructRegistry.g.cs` `user-word-xor-2023`).

**Semantics (GR / editing-rule level):**
1. **Simple insertion (`EDITING c IS literal-1`)** — char-1 is a simple-insertion symbol (Rule 3). literal-1 is placed at char-1's position **unconditionally** (sign-independent), like `B`/`0`/`/` but a literal of any width. Each char-1 occurrence contributes `|literal-1|` characters to the item width.
2. **Sign-sensitive fixed insertion (`EDITING c FOR NEGATIVE IS lit2 [POSITIVE IS lit3]` or the POSITIVE-first order)**, char-1 appearing exactly once (fixed, per SR24) — per **Table 9** (the correct table, see drift):
   - value **< 0** → emit **lit2** (or, if only POSITIVE given, spaces × `|lit3|`);
   - value **≥ 0** (positive or zero) → emit **lit3** (or, if only NEGATIVE given, spaces × `|lit2|`).
   Width per occurrence = `|lit2|` = `|lit3|` (SR12a). No zero-suppression interaction (a single fixed occurrence is not a floating string).
3. **Extended/floating (char-1 repeated ≥ 2, SR24/SR25)** — the two occurrences become a floating string; width = `|literal|` for the **leftmost** occurrence + 1 char for each interior occurrence (D.24 note @48179: `LLLL9` → 6 + 3). **STAGE SEPARATELY** (goldened later).
4. **Category:** the presence of any char-1 (an editing symbol) makes the item **numeric-edited** (Table 7) — it renders exactly like the existing edited masks; **raises no exception-condition** of its own (editing feeds MOVE, whose high-order truncation is the defined MOVE behavior, not EC-SIZE — contrast CONTINUE/EC-CONTINUE). De-editing (reverse MOVE from a numeric-edited item) restores sign from which literal appears.
5. **DECIMAL-POINT IS COMMA** (SR13 @20330) applies to the base mask as today; char-1 literals are inserted verbatim (not separator-swapped).

**As-built today** (confirmed by reading + CLI probe):
- Grammar: `pictureClause : PIC PIC_STRING ;` (`CobolData.g4:340`). No `EDITING` token (grep of `Grammar/` — only `FOR`@429, `POSITIVE`@435, `NEGATIVE`@436 exist). **Probe** (`--std 2023`, `PIC L9999.99F EDITING "L" FOR NEGATIVE IS "("`): `error COBOL0307: unexpected 'FOR'` — the phrase does not parse today. Confirmed.
- Lexer PICMODE (`CobolLexer.g4:747–779`): `PIC_STRING` matches non-whitespace (with the embedded-`.` and trailing-`,`/`;` handling) then **`-> popMode`**. Because it stops at whitespace, a trailing ` EDITING …` is already lexed in **default mode** — so **no PICMODE change is required** (see AUDIT DRIFT). The `PictureText` accessor is `DataDescriptionCst.cs:56` (`ctx.pictureClause()?.PIC_STRING()?.GetText()`); the bind consumes it at `DataBinder.cs:1599/1800`.
- `PictureAnalyzer.Analyze` (`PictureAnalyzer.cs:31`): the SR2 whitelist loop (@60–88) would flag char-1 (e.g. `'L'`,`'F'`,`'T'`) as `invalid` → **COBOLNET0808** (@96). Numeric-edited is built at @244–250 with `Length = expanded.Count(c => c is not ('V'|'S'|'P'))` and `EditMask = expanded`. So today char-1 both (a) trips 0808 and (b) would count as 1 char in Length — both must change.
- Renderer `CobolEdit.Format` (`CobolEdit.cs:39`) works on the mask string; the char switch (@107–143) has no char-1 arm and its output array is `pattern.Length` wide (1:1 with mask positions) — it cannot expand a multi-char literal. **Probe** grounding the base renderer (`PIC ----9.99`): `MOVE -123.45` → `[ -123.45]`, `MOVE 123.45` → `[  123.45]` (floating minus works; confirms Pass-1/Pass-2 model).
- Emit threads only the mask string: `RuntimeApi.EditFormat` (`RuntimeApi.cs:100`) emits `CobolEdit.Format(value, scale, "mask", cfgArgs)`; `NumericRenderer.cs:116` gates on `{Category: NumericEdited, EditMask: {} dem}`.

**AUDIT DRIFT CAUGHT:**
1. **Table 8 is INVERTED in the extracted markdown — trust Table 9 / D.24 (audit warning CONFIRMED correct).** As extracted, Table 8 (@20805–20806) reads `character-1 NEGATIVE phrase | positive-or-zero = literal-2 | negative = literal-2 or spaces` — i.e. it emits the negative literal on *positive* values. That contradicts (a) Table 9 (@20857, `NEGATIVE phrase | negative = literal-2`), (b) D.24 (`EDITING "L" FOR NEGATIVE IS "("` → `MOVE -123.45` yields a leading `(`), and (c) Rule 5's plain-language intent. **Implement per Table 9 / D.24: NEGATIVE literal on value < 0, POSITIVE literal on value ≥ 0, the unspecified side → spaces.** Do NOT transcribe Table 8's row as written.
2. **Annex item number is 19, not 1.** The audit hint says "Annex E.3.3 item 1"; the EDITING phrase is **E.3.3 item 19** (@50275). Minor, but cite item 19 in the construct row + code comments.
3. **"NO PICMODE change" — CONFIRMED, but for the reason the audit half-stated.** The audit's PICMODE hint was flagged wrong by the task; verified: `PIC_STRING` stops at whitespace and pops mode, so `EDITING` reaches default mode as an ordinary token stream. The change is a pure **additive** `editingPhrase*` suffix on `pictureClause` + a new `EDITING` token — no PICMODE restructure.
4. **`EDITING` is a genuine new 2023 reserved word (E.2 item 25 @49327).** The audit's "add the EDITING token" is right but incomplete: it omits the `cobolWord`-funnel admission + the `user-word-editing-2023` (0901) reservation row required so `EDITING` stays a legal user word below 2023 (the XOR/COMMIT pattern). Otherwise a `--std 85` program with a data-name `EDITING` regresses.

**Implementation plan** (concrete):
- **Lexer** `CobolLexer.g4`: add token `EDITING : 'EDITING' ;` in the default-mode keyword block (near `FOR`@429). *Additive.*
- **Grammar** `CobolData.g4`: `pictureClause : PIC PIC_STRING editingPhrase* ;` + new rule `editingPhrase : EDITING literal ( IS? literal | FOR ( NEGATIVE IS? literal (POSITIVE IS? literal)? | POSITIVE IS? literal (NEGATIVE IS? literal)? ) )? ;`. `literal` is `CobolExpressions.g4:338` (STRINGLIT / national). **ADDITIVE** (new optional suffix + new rule; existing `PIC`/`PIC_STRING` accessor indices unchanged) — but the ANTLR parser is **shared with the frozen legacy `CobolSharp.Compiler`**, so run the **FULL legacy guard** even though additive.
- **`cobolWord`** `CobolWords.g4` (after `PRESENT`@65): `| EDITING`.
- **CST** `DataDescriptionCst.cs`: add `EditingPhrases` accessor returning parsed `EditingRuleSpec(char Char1, string? Literal1, string? LiteralNeg, string? LiteralPos)` list off `ctx.pictureClause().editingPhrase()`.
- **`PicInfo`** (`PicInfo.cs`, next to `EditMask`@207): add `IReadOnlyList<EditingRule>? EditingRules { get; init; }` (runtime-facing record: `record EditingRule(char Char1, bool SignSensitive, string? Simple, string? Neg, string? Pos)`).
- **`PictureAnalyzer.Analyze`** (`PictureAnalyzer.cs:31`): new optional param `IReadOnlyList<EditingRuleSpec>? editing = null`. In the SR2 whitelist loop (@60–88) treat any char equal to a declared char-1 as **valid** (skip the `invalid ??= c`). Enforce the EDITING SRs here: SR8 (char-1 legal letter), SR10 (char-1 present in mask), SR11 (distinct char-1), SR12a (lit2/lit3 equal width), SR12b (FOR ⇒ mask ⊆ {char-1,9,.,cs,P,V,Z}), SR24/SR25 (fixed vs extended multiplicity/order) → new diag band. In numeric-edited construction (@248): compute `Length` with each char-1 position counted as `|literal|` (simple: `|Literal1|`; sign-sensitive fixed: `|Neg|`=`|Pos|`); set `EditingRules`.
- **Runtime** — **new sign-sensitive overload** `CobolEdit.Format(Int128 value, int valueScale, string picture, IReadOnlyList<EditingRule> editing, …)` (+ `TryFormat`, `MaskCapacity`, `DeEdit`, `FormatAlphanumeric` peers): render the base mask treating each char-1 as a **1-column placeholder** through the existing Pass-1/Pass-2, recording char-1 column indices; then a **post-pass** replaces each placeholder column with its resolved string — simple: `Simple`; sign-sensitive: `value<0 ? Neg : Pos`, defaulting the unspecified side to spaces of the peer width — building the final variable-width image. `MaskCapacity` adds `Σ(|literal|-1)` over char-1 occurrences.
- **Emit** `RuntimeApi.cs:100` / `NumericRenderer.cs:116` / `GroupImageCodec.cs:49` / `ValueInitializer.cs:92`: when `pic.EditingRules is not null`, emit the overload with the rules serialized as a C# collection expression (`[ new(...), … ]`).
- **`constructs.json`** (`tests/version-matrix/constructs.json`) → regenerates `ConstructRegistry.g.cs`/`Constructs.g.cs`: add `picture-editing-2023` (`introducedIn:2023`, gate `COBOLNET0900`, cite "ISO §13.18.40.2/.5; Annex E.3.3 item 19; VCR row 62") and `user-word-editing-2023` (`introducedIn:85, removedIn:null, reservedIn:2023`, gate `COBOLNET0901`, "§8.9 / Annex E.2 item 25").
- **VersionConformancePass** (`VersionConformancePass.cs`, next to `VisitUsageClause`@475): `public override object? VisitPictureClause(...) { if (ctx.editingPhrase().Length > 0) _p.Check(Constructs.PictureEditing2023, "the PICTURE EDITING phrase"); return base.VisitChildren(ctx); }`.
- **Diag codes** — ⛔ **the numbers below are STALE placeholders (see the top banner); allocate three fresh codes
  from the plan §0 next-free.** The useful content is the RULE→code GROUPING, which stands: **[EDIT-A]** — char-1
  illegal (SR8) / char-1 absent from mask (SR10) / duplicate char-1 (SR11); **[EDIT-B]** — FOR constraints (SR12a
  width mismatch, SR12b symbol-set, extended sign on a floating-point edited item); **[EDIT-C]** — SR24/SR25
  multiplicity/ordering. Introduction gate reuses **0900**; reserved-word-as-user-word reuses **0901**.

**Golden** (hand-derived from the GRs, *not* the oracle). Staged slice 1 = simple insertion + sign-sensitive fixed insertion.

*Golden 1 — simple insertion (sign-independent), `HH:MM` shape* (char-1 `T`, legal per SR8):
```
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P13-EDIT-SIMPLE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-EDIT PIC 99T99 EDITING "T" IS ":".
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1230 TO T-EDIT.
           DISPLAY "[" T-EDIT "]".
           STOP RUN.
```
Derivation: mask positions `9 9 T 9 9`; `T` is simple insertion of `":"`. 1230 fills the four `9`s → `12` `30`, `T`→`:`. Width 5. **Expected stdout:** `[12:30]`.

*Golden 2 — sign-sensitive fixed insertion, single-char literals (parentheses)* (per Table 9 / D.24):
```
       01 P-EDIT PIC L999.99F EDITING "L" FOR NEGATIVE IS "("
                              EDITING "F" FOR NEGATIVE IS ")".
       …
           MOVE -12.34 TO P-EDIT.  DISPLAY "[" P-EDIT "]".
           MOVE  56.78 TO P-EDIT.  DISPLAY "[" P-EDIT "]".
```
Derivation: mask `L 9 9 9 . 9 9 F`, width 8; `L`/`F` fixed sign-sensitive, only NEGATIVE given ⇒ POSITIVE defaults to 1 space. `-12.34` (neg): `L`→`(`, `999`→`012`, `.`, `99`→`34`, `F`→`)` = `(012.34)`. `56.78` (pos): `L`→space, `999`→`056`, `.`, `99`→`78`, `F`→space = ` 056.78 `. **Expected stdout:** `[(012.34)]` then `[ 056.78 ]`.

*Golden 3 — multi-char sign-sensitive literal (the D.24 "DEBIT " case — width-expansion proof, internally consistent in the annex):*
```
       01 D-EDIT PIC L999.99 EDITING "L" FOR NEGATIVE IS "DEBIT ".
       …
           MOVE -123.45 TO D-EDIT.  DISPLAY "[" D-EDIT "]".
           MOVE  123.45 TO D-EDIT.  DISPLAY "[" D-EDIT "]".
```
Derivation: char-1 `L` = 6-char literal `DEBIT ` ⇒ width 6+3+1+2 = 12; NEGATIVE only ⇒ POSITIVE defaults to 6 spaces. `-123.45`: `DEBIT `+`123`+`.`+`45` = `DEBIT 123.45` (matches D.24 @48173). `123.45`: 6 spaces + `123.45` = `      123.45` (matches D.24 @48177 `bbbbbb123.45`). **Expected stdout:** `[DEBIT 123.45]` then `[      123.45]`.

*Below-edition negative fixture* (any of the above at `--std 2014`): **COBOLNET0900** — e.g. `error COBOLNET0900: the PICTURE EDITING phrase (ISO §13.18.40; Annex E.3.3 item 19) requires COBOL-2023 (targeting COBOL-2014)`. Plus a `--std 85` fixture with a data-name `EDITING` must still **compile** (proving the `cobolWord` admission); using `EDITING` as a user word at `--std 2023` yields **COBOLNET0901**.

> Note the D.24 first two examples (`L9999.99F`, `LLLL9.99F`) print `(b123.45)` / `b(123.45)` — a **leading blank inside four `9`/`L` positions**. Under the GRs a `9` never blanks, so `L9999.99F`+`-123.45` = `(0123.45)` (leading `0`, not blank); the `b` in those two D.24 lines is an **informative-annex typo** (D.24 also has the stray-quote `123.45''` @48177 and the `LLLL9,88`→`9.99` slip @48179). Golden 3 deliberately uses the internally-consistent third example; do **not** goldens the `(b123.45)` string.

**Blast radius / hazards:**
- **Shared ANTLR parser** — any `.g4` edit (new `EDITING` token, `pictureClause`/`cobolWord` rules) regenerates the parser used by the frozen legacy `CobolSharp.Compiler`; even though additive, run the **full legacy guard** (1196+654 legacy + NIST 353 MATCH). The `EDITING` token is the highest risk: verify no legacy corpus program uses `EDITING` as a data-name below 2023 (the `cobolWord` admission covers it; without it → legacy regressions).
- **`PictureAnalyzer` 0808 loop** — filtering char-1 out of the `invalid` scan must not accidentally admit a *stray* char-1-looking letter when no EDITING phrase declared it; gate strictly on the parsed char-1 set. Watch `PictureAnalyzerTests` and the whole numeric-edited golden set (NC104A/NC124A/NC125A/NC108M).
- **`CobolEdit` overload** — the variable-width post-pass changes item **Length**; verify `MaskCapacity`/`MaskScale`/group-image (`GroupImageCodec`) and `DeEdit` round-trips stay consistent, and that the existing single-string `Format` path is untouched when `EditingRules is null` (characterization snapshots + `CobolEditTests`).
- **VersionMatrixTests** — the two new `constructs.json` rows add (construct × edition) assertions; the drift tests freeze the edition metadata, so land the row + gate in the same commit. Ship the three goldens + the below-2023 negative fixture in the same change set (feedback_conformance_tests_per_feature).

---

## Wave C — PERFORM Format 3 exception-checking WHEN (VCR 79)

## Wave C — VCR 79: PERFORM Format 3 — the exception-checking PERFORM (`PERFORM … WHEN … [FINALLY] END-PERFORM`), §14.9.28

**Scope note:** VCR 79 is the *second half* of scout §C5. The *first half* — `PERFORM … UNTIL EXIT` — **has already landed** (grammar `CobolControlFlow.g4:46`, bound `PerformForever` at `BoundTree.cs:449`, binder `ControlFlowBinder.cs:149`, emitter `while(true)` at `ControlFlowEmitter.cs:82-85`, gate `VersionConformancePass.cs:509` + construct row `perform-until-exit-2023`). **This section is ONLY Format 3, which is entirely UNBUILT.** It is the largest single 2023 construct in the phase: 2 new lexer tokens, a whole new statement grammar + bound node, and deep exception-engine integration. Treat it as **STAGED-LARGE** — implement as its own dedicated wave, not folded into a grammar batch.

### Spec sections (all verified against `specs/ISO_COBOL.md` by line)
- **§14.9.28.1 General** (L29360): PERFORM controls one or more imperative statements within its scope "**with or without exception checking** within those statements."
- **§14.9.28.2 Format 3 figure** (L29377–29394): the boxed syntax (transcribed below).
- **§14.9.28.3 Syntax rules FORMAT 3** (L29523–29529): SR14 (file-name uniqueness across WHENs), SR15 (exception-name uniqueness), SR16 (`file-name-2` ⇒ `exception-name-2` must begin `EC-I-O`).
- **§14.9.28.4 General rules FORMAT 3** (L29684–29713): GR14 (implicit TURN/PUSH/POP), GR15 (control transfer + range), GR16 (FINALLY = end of PERFORM; no transfer-out; EXIT PERFORM → implicit CONTINUE after END-PERFORM), GR17 (WHEN match → imperative-2; USE match rules = USE GR3a–3g; **matching USE declarative is IGNORED**), GR18 (WHEN OTHER → imperative-3), GR19 (WHEN COMMON → imperative-4), GR20 (fatal/nonfatal resumption), GR21 (ECs inside imperative-2/3/4/5 are NOT re-caught — behave as Format 2), GR22 (checking-enabled state after the PERFORM).
- **§14.9.29.3 SR4** (L29752): `RAISE` inside an exception-checking PERFORM is legal **only** in imperative-statement-1.
- **§14.9.49.4 GR3a–3g** (USE statement, referenced by §14.9.28.4 GR17): the WHEN-match ordering algorithm.
- **§14.6.13.1.3 Fatal exception conditions** (referenced by §14.9.28.4 GR20): fatal resumption path.
- Cross-refs confirming the WHEN phrase is a first-class EC handler equal to a declarative: L11365 (WHEN phrase = "statements in a WHEN phrase of an active PERFORM"), L24485/L24507 (a WHEN phrase enables checking exactly like a TURN directive; `EXCEPTION-STATUS` inside a WHEN returns the identifying info), L4783/L4873/L4995 (POP/PUSH/TURN directives **shall not** appear inside an exception-checking PERFORM), L5006 (`EC-I-O-WARNING` may be turned on only explicitly or by presence in a WHEN phrase).
- **Introduction proof: Annex E "New features", item 36** (L50316): *"**PERFORM Statement.** An exception checking variant of this statement has been added."* Plus the §"new features" list L1217 ("Inline exception handling using the exception-checking format of the PERFORM statement").

### Syntax / format (transcribed from the §14.9.28.2 Format 3 box, L29379–29394)
```
PERFORM [ WITH LOCATION ]
    imperative-statement-1
  { WHEN { EXCEPTION [ { file-name-1 }… | INPUT | OUTPUT | I-O | EXTEND ]
         | { exception-name-1 }…
         | { exception-name-2 FILE file-name-2 }… }
      imperative-statement-2 } …
  [ WHEN OTHER EXCEPTION imperative-statement-3 ]
  [ WHEN COMMON EXCEPTION imperative-statement-4 ]
  [ FINALLY imperative-statement-5 ]
  END-PERFORM
```
- `WITH LOCATION` — optional; when present the implicit TURN (GR14) carries LOCATION so `EXCEPTION-LOCATION`/`EXCEPTION-STATEMENT` capture info.
- **At least one** WHEN clause is mandatory (the `{…}…` is a required-then-repeat). `WHEN OTHER`, `WHEN COMMON`, `FINALLY` are each optional and at most once.
- A WHEN selector is one of three shapes: (a) `EXCEPTION` optionally file/mode-narrowed (a shorthand for `EC-I-O` scoped to file(s) or open-mode); (b) one-or-more bare `exception-name-1`; (c) one-or-more `exception-name-2 FILE file-name-2` (file-scoped EC-I-O names).
- **`END-PERFORM` is mandatory** (Format 3 is always inline-delimited).
- The figure prints `IO`; the real reserved word is **`I-O`** (existing `I_O` token / `useOnTarget` open-mode word) — do **not** add a bare `IO` token (scout §C5 hazard 5).

### Introduced edition & gate
- **Edition:** COBOL-2023 only (Annex E item 36). Below 2023 the whole Format-3 statement does not exist.
- **Below-edition behavior:** at `--std 85|2002|2014`, a Format-3 PERFORM must be **recognized and rejected by name** with **COBOLNET0900** (introduction gate) — NOT dropped silently. This must be a **parse-arm / recognition** gate, not a bound-arm gate (DEVLOG-724 lesson): a below-2023 Format-3 PERFORM also fails to bind (its WHEN exception-names/handlers won't resolve → `BoundUnsupported`), so a bound-arm gate would lose the 0900.
- **Gate mechanism:** new construct row `Constructs.PerformExceptionChecking2023 = "perform-exception-checking-2023"`, `introducedIn 2023`, citation `"ISO §14.9.28.2 Format 3 / §14.9.28.4 GR14–22; Annex E item 36"`. Fired from a `VersionConformancePass` (ParseArm) override on `VisitPerformStatement` when the Format-3 alternative's marker tokens are present (any `WHEN … EXCEPTION` / `FINALLY` / `WITH LOCATION` under the new grammar alt) → `_p.Check(Constructs.PerformExceptionChecking2023, "the exception-checking PERFORM statement")`. Registry funnel: `ConstructRegistry.Check → EditionCodes.Introduction` → "…requires COBOL-2023 (targeting COBOL-YYYY)…". Add matching rows to `tests/version-matrix/constructs.json` (`expectDiagnostic COBOLNET0900`), `src/Cobol.Net.Editions/Constructs.g.cs` const, and `ConstructRegistry.g.cs` row (mirror the existing `perform-until-exit-2023` row at `ConstructRegistry.g.cs:28`).

### Semantics (GR-by-GR, verified L29684–29713)
- **GR14 (checking scope — the hard part):** For each `exception-name-1/-2` in a WHEN, **if checking is not already enabled** for it over imperative-statement-1 (by a real `>>TURN`), an **implicit TURN … CHECKING ON** is assumed before the first statement of imperative-statement-1; **LOCATION is included iff `WITH LOCATION` written**. If `WHEN OTHER` is used, only exceptions **already enabled at the point of detection** are eligible for WHEN OTHER (WHEN OTHER does not itself enable any checking). A `>>TURN … OFF` *inside* imperative-statement-1 for a WHEN-named exception **suppresses** that WHEN. At the **end** of imperative-statement-1: implicit `PUSH ALL` then `TURN OFF ALL`. Immediately before `END-PERFORM`: implicit `POP ALL` then `TURN OFF` for exactly the names that were implicitly turned on. **⇒ imperative-2/3/4/5 run with checking OFF (this is GR21) and the pre-PERFORM checking state is restored.**
- **GR15:** control transfers to imperative-statement-1; the "specified set of statements" is imperative-statement-1 only; the handler statements run only on a raise.
- **GR16 (FINALLY):** if `FINALLY` present, the end of the PERFORM *begins* at imperative-statement-5; **no transfer of control out of the PERFORM may appear in imperative-statement-5**; an `EXIT PERFORM` inside imperative-statement-5 transfers to an implicit `CONTINUE` after `END-PERFORM`. If no FINALLY, `END-PERFORM` is the end.
- **GR17 (WHEN match):** a raised EC associated with a WHEN → imperative-statement-2 runs. **Match ordering = USE GR3a–3g** (file+level-3, file+level-2, level-3, level-2, level-1/EC-ALL — the same tiers `__EcDispatch` already implements). After imperative-2: control per GR20, or (if WHEN COMMON present) to imperative-4. **Any USE declarative that would normally match is IGNORED** — Format 3 *shadows* declaratives for the ECs raised inside its imperative-1.
- **GR18 (WHEN OTHER):** an enabled EC matching *no* WHEN, with a `WHEN OTHER EXCEPTION` present → imperative-statement-3 runs; then control to end-of-PERFORM (or imperative-4 if WHEN COMMON). *(Spec typo: GR18's second sentence says "control is passed as indicated in General rule 14.9.29" — that is a misprint for General rule 20; the first sentence already gives the correct destination.)*
- **GR19 (WHEN COMMON):** if present, imperative-statement-4 runs after any handled WHEN/OTHER; then control per GR20. (It is the "finally-for-handled-exceptions" that runs after handling but is itself distinct from FINALLY, which always runs.)
- **GR20 (resumption):** if imperative-statement-1's last statement completes with no raise → proceed to end-of-PERFORM. If an EC was raised & handled (GR17/18/19 done): **nonfatal** ⇒ execution continues with an **implicit CONTINUE immediately after the raising statement** in imperative-1 (i.e. it does NOT restart imperative-1; if the raising statement was the last, go to end-of-PERFORM); **fatal** ⇒ per §14.6.13.1.3. NOTE 8: "end of the PERFORM" includes FINALLY.
- **GR21:** ECs raised inside imperative-2/3/4/5 are **not** re-caught by this PERFORM (they behave as a Format-2 inline PERFORM — i.e. dispatch to declaratives / normal handling). Consistent with GR14's PUSH ALL + TURN OFF ALL over the handler region.
- **GR22 (post-PERFORM checking state):** names enabled by a real `>>TURN` before entry stay enabled; a `>>TURN` inside imperative-1's range is retained; otherwise WHEN-only implicit enables are disabled.
- **§14.9.29.3 SR4:** a `RAISE` statement is legal only in imperative-statement-1 (not in any WHEN/FINALLY handler).
- **Every EC raised is one already in the catalog** — no new exception conditions are introduced; Format 3 is purely a new *handler shape* over the existing EC hierarchy.

### As-built today (confirmed by reading)
- **Grammar `src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4:18–28`:** `performStatement` has Format-1 (out-of-line) alts + two inline alts (`PERFORM performOptions+ statementBlock* END_PERFORM` and `PERFORM statementBlock+ END_PERFORM`). **Format 3 is entirely absent.** A source `PERFORM DISPLAY X WHEN EC-BOUND-SUBSCRIPT … END-PERFORM` today parses `statementBlock+` greedily and then **fails to match `WHEN`** → parse error. Confirmed by structure.
- **Lexer `CobolLexer.g4`:** `FINALLY` and `LOCATION` tokens **do NOT exist** (grep returned nothing). `EXIT/UNTIL/PERFORM/EXCEPTION/OTHER/COMMON/EC/I_O` all exist (scout §C5 code-anchor block).
- **Binder `ControlFlowBinder.cs:108–149`:** `BindPerform` handles inline (`BoundInlinePerform`) and out-of-line (`BoundOutOfLinePerform`); `BindPerformControl` covers Once/Times/Until/Varying/**Forever**. **No Format-3 leg.**
- **Bound tree `BoundTree.cs:439–480`:** `BoundPerformControl` hierarchy (Once/Times/Until/Forever/Varying); `BoundInlinePerform`/`BoundOutOfLinePerform`; `BoundExitPerform(bool Cycle)` at :480. **No `BoundExceptionCheckingPerform`.**
- **Deferral note `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/EcBinder.cs:122`** (RESUME binder, SR1): *"only in a declarative (the exception-checking PERFORM WHEN form is 2023, **a later wave**)."* **← THIS wave is that "later wave."** RESUME stays declarative-only (§14.9.33.3 SR1); it does **not** become legal inside a Format-3 WHEN handler as a result of this work. *(The audit's ~line-122 anchor is correct; note the path is `Binding/Procedure/Verbs/EcBinder.cs`, NOT `Binding/Ec/EcBinder.cs`.)*
- **EC runtime engine (`src/Cobol.Net.Runtime/Exceptions/`):** `ExceptionEngine` (`ExceptionState.cs`) is the run-unit LAST-EXCEPTION register + `ArgumentFunctionChecking`/`DataConversionChecking` ambient per-statement gates; `CobolFatalException` (`CobolFatalException.cs`) is the fatal-EC carrier. **There is NO runtime "checking mask" object** — checking is resolved **at COMPILE time** by `TurnState` (`src/Cobol.Net.Compiler/Binding/TurnState.cs`): a line-ordered fold of `>>TURN` events; **checking OFF compiles to zero scaffolding** (SSOT §18.16). So the emitter decides *statically* whether to emit an EC guard at each statement.
- **EC dispatch machinery (`src/Cobol.Net.Compiler/CodeGen/EcEmitter.cs`):** `__EcDispatch(ec, file)` (:235) is the source-order USE-declarative selector implementing exactly the GR3c–g tiers; `__IoCheckEc(...)` (:296) is the after-verb I/O status→EC bridge that raises + selects; `__RunUse` runs a declarative by pc range; the dispatch protocol returns `-1`(handled/normal) `-2`(RESUME NEXT) `-3`(no match) `≥0`(RESUME AT pc). **Nonfatal ECs do NOT throw** — the per-statement guard checks the status inline and calls `__EcDispatch`; **fatal ECs throw `CobolFatalException`** caught by the statement guard. `BoundEcChecked` (`EcEmitter.EmitChecked` :64) already wraps a single statement with an EC context. **This is the machinery Format 3 must reuse and locally REDIRECT** (WHEN handlers replace `__EcDispatch` for the shadowed names).

### AUDIT DRIFT CAUGHT
1. **Section number is WRONG in the audit.** The audit cites **§14.9.31** for the exception-checking PERFORM. §14.9.31 is **RECEIVE** (confirmed L857). The PERFORM statement is **§14.9.28** (L854); Format 3 is **§14.9.28.2/.3/.4** (L29377/29523/29684). The scout §C5 already flagged this; re-confirmed here. (The Annex "new features" item numbers 36/37 the scout cites ARE correct — L50316/50318.)
2. **The audit's "EcBinder.cs ~line 122" deferral anchor is correct in line and content, but the audit implies the path `Binding/Ec/EcBinder.cs`.** The actual file is `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/EcBinder.cs` (there is no `Binding/Ec/EcBinder.cs`). Minor path drift; the line/content check out.
3. **No inverted-semantics drift found** beyond the section number — GR14–22 as summarized in scout §C5 match the spec text verbatim. (One spec-internal misprint noted: GR18 L29694 "General rule 14.9.29" should read "General rule 20" — a spec typo, not an audit drift.)

### Implementation plan (concrete; STAGED-LARGE)
**1. Lexer (`CobolLexer.g4`) — ADDITIVE:** add two tokens `FINALLY` and `LOCATION`. (`LOCATION` is also wanted by the TURN/`WITH LOCATION` directive path — check it isn't already added there; if so reuse.) Additive token adds are safe for the shared legacy parser.

**2. Grammar (`CobolControlFlow.g4`) — ADDITIVE alternative (safe, but run the FULL legacy guard — the ANTLR parser is shared with frozen `CobolSharp.Compiler`; an added alternative can only be reached by the new tokens so the legacy binder never sees it, but guard to be certain):** add a **new Format-3 alternative** to `performStatement`, placed **after** the existing inline alts is wrong (greedy `statementBlock+` would win) — place it **before** the two generic inline alts, positively guarded by requiring a following `WHEN … EXCEPTION`/`FINALLY`/`WITH LOCATION`. Concretely:
```
| PERFORM (WITH? LOCATION)? statementBlock+
    performWhenClause+
    (WHEN OTHER EXCEPTION statementBlock*)?
    (WHEN COMMON EXCEPTION statementBlock*)?
    (FINALLY statementBlock*)?
    END_PERFORM
```
with new sub-rules:
```
performWhenClause : WHEN performWhenSelector statementBlock* ;
performWhenSelector
    : EXCEPTION (fileName+ | INPUT | OUTPUT | I_O | EXTEND)?
    | ecName+
    | (ecName FILE fileName)+ ;
```
Reuse the existing `useEcEntry`/`useOnTarget` shapes for exception-name + open-mode resolution. **Ambiguity guard:** the leading `statementBlock+` before the first `WHEN` is the same shape as the existing inline alt; because ANTLR takes the first matching alt (memory `feedback_grammar_precedence`), order the Format-3 alt first and rely on `performWhenClause+` being mandatory to force selection only when a WHEN is present — verify no mis-capture of a plain inline PERFORM whose body legitimately contains a nested statement using `WHEN` (none do; WHEN is only SEARCH/EVALUATE/PERFORM-F3-bound and those are inside their own `statementBlock`). Update the grammar doc (`feedback_grammar_doc_sync`).

**3. Bound tree (`BoundTree.cs`):** add
```
public sealed record BoundExceptionCheckingPerform(
    IReadOnlyList<BoundStatement> Body,
    IReadOnlyList<BoundPerformWhen> Whens,
    IReadOnlyList<BoundStatement>? WhenOther,
    IReadOnlyList<BoundStatement>? WhenCommon,
    IReadOnlyList<BoundStatement>? Finally,
    bool WithLocation) : BoundStatement;
public sealed record BoundPerformWhen(
    IReadOnlyList<EcSelector> Selectors,   // reuse EcBinder's exception-name/file/mode model
    IReadOnlyList<BoundStatement> Handler);
```
Wire `StatementChildren`/Recurse for the generated visitor (memory: "any new tree walk = the ONE shared/generated visitor").

**4. Binder (`ControlFlowBinder.cs` + `EcBinder.cs`):** in `BindPerform`, detect the Format-3 context (`p.performWhenClause()` non-empty or `WITH LOCATION`) → delegate to a new `BindExceptionCheckingPerform`. New methods on `EcBinder` resolve each WHEN's `exception-name` against `ExceptionCatalog` and file/mode via the existing `useOnTarget` resolution. **Enforce the syntax rules with new diagnostics:** SR14/SR15 (duplicate file-name / exception-name across WHENs unless file-qualified), SR16 (`FILE file-name-2` ⇒ name must start `EC-I-O`), §14.9.29.3 SR4 (reject `RAISE` outside imperative-statement-1 — a bound-walk over the WHEN/FINALLY handler bodies), GR16 (reject any transfer-of-control-out — `GO TO`/`GOBACK`/`STOP`/`EXIT SECTION|PARAGRAPH|PROGRAM` — inside FINALLY; `EXIT PERFORM` in FINALLY is remapped to implicit CONTINUE).

**5. Emitter (`ControlFlowEmitter.cs` + `EcEmitter.cs`) — the deep work:** emit imperative-statement-1's statements with **EC checking forced ON** for the WHEN-named exceptions, but **routed to a LOCAL handler dispatcher** instead of `__EcDispatch`. Because `TurnState` is compile-time, the cleanest realization is a **per-PERFORM emit-scope override**: while emitting the body, push a "Format-3 checking context" onto `EmitterState` that (a) makes the per-statement EC-guard emission treat the WHEN names as enabled-with-LOCATION (bypassing the `TurnState` fold for those names over this region), and (b) redirects the guard's dispatch target from `__EcDispatch(ec,file)` to a locally-generated `__F3Dispatch_<id>(ec,file)` that runs the WHEN arms (GR17 tiers = same GR3c–g ordering as `EmitDispatchSelector`, but over the PERFORM's WHEN table; falls through to WHEN OTHER = GR18, else "-3"/unhandled). For **nonfatal** ECs: the inline after-statement guard calls `__F3Dispatch`; a `-3` (unhandled by any WHEN/OTHER) means the EC was *enabled but not selected* → per GR14 those names are the only ones enabled, so a `-3` cannot occur for a WHEN-named EC, but WHEN OTHER covers "any *other* enabled" — an EC enabled by an *outer* real TURN but not named in a WHEN and with no WHEN OTHER passes through to normal handling (GR21-adjacent). For **fatal** ECs: wrap imperative-1 in `try { … } catch (CobolFatalException __e) when (…name matches a WHEN/OTHER…) { run handler; per GR20 fatal → §14.6.13.1.3 }`. Model §14.9.28.4 GR14's PUSH ALL/TURN OFF ALL over the handler region simply by **emitting the WHEN/FINALLY handler bodies with the Format-3 checking context popped** (so they get normal `TurnState`-driven guards = GR21 "as if Format 2"). GR19 (WHEN COMMON) and GR16 (FINALLY) become deterministic post-blocks: emit `__common:` / `__finally:` sequences reached from every handled path; FINALLY also on the no-exception fall-through (NOTE 8). GR20 nonfatal "implicit CONTINUE after the raising statement" = the natural fall-through of the inline nonfatal guard (it already resumes after the statement), so no restart of imperative-1. GR22 post-state = automatic because the compile-time `TurnState` for statements *after* the PERFORM is unchanged (the implicit enables never entered `TurnState`).

**6. Runtime (`src/Cobol.Net.Runtime/Exceptions/`):** reuse `ExceptionEngine`/`CobolFatalException`/`ExceptionCatalog` unchanged. No new runtime type is strictly required — the local `__F3Dispatch_<id>` and the `try/catch` are emitted C#. (If the fatal-path `when` filter needs the level-2/level-3 hierarchy test, reuse `ExceptionCatalog.UnderLevel2` / `IoBit`, already public.)

**7. Gate + registry:** `VersionConformancePass` ParseArm `VisitPerformStatement` override → `Check(Constructs.PerformExceptionChecking2023, …)`; `Constructs.g.cs` const; `ConstructRegistry.g.cs` row; `tests/version-matrix/constructs.json` row (`expectDiagnostic COBOLNET0900`).

**8. New diagnostics (next free 15xx band; final numbers reconciled at implementation time — I claim these two after any earlier waves):**
⛔ **STALE code numbers (see the top banner) — allocate two fresh codes from the plan §0 next-free.** The rule
grouping stands:
- **[PERF3-A]** — Format-3 WHEN syntax-rule violation (SR14 duplicate file-name / SR15 duplicate exception-name / SR16 `FILE`-scoped name not `EC-I-O*`). Message cites §14.9.28.3.
- **[PERF3-B]** — illegal statement inside a Format-3 handler: `RAISE` outside imperative-statement-1 (§14.9.29.3 SR4) **or** a transfer-of-control out of `FINALLY` (§14.9.28.4 GR16).

(The introduction gate itself uses **COBOLNET0900**, not a 15xx.)

### Golden

**Positive fixture** — `tests/conformance/2023/perform-f3-exception-checking.cob` (or the wave's conformance folder), a nonfatal checked EC (`EC-BOUND-SUBSCRIPT`, Table 13 = nonfatal) raised inside imperative-1, caught by a WHEN, with a FINALLY that always runs:
```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WC79-PERF-F3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9(2) OCCURS 3 TIMES.
       01 WS-I PIC S9(2) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM
               DISPLAY "BEFORE"
               MOVE 7 TO WS-E (WS-I)
               DISPLAY "AFTER"
             WHEN EC-BOUND-SUBSCRIPT
               DISPLAY "CAUGHT"
             FINALLY
               DISPLAY "FINALLY"
           END-PERFORM
           DISPLAY "DONE"
           STOP RUN.
```
**Hand-derived expected stdout (bytes `"BEFORE\nCAUGHT\nFINALLY\nDONE\n"`):**
- GR14: `EC-BOUND-SUBSCRIPT` checking is implicitly turned ON over imperative-1.
- `DISPLAY "BEFORE"` → `BEFORE\n`.
- `MOVE 7 TO WS-E(WS-I)` with `WS-I = 5` and `OCCURS 3` → subscript out of range 1..3. With checking enabled, **EC-BOUND-SUBSCRIPT is raised** (§8.5.1.2 subscript range → Table 13 nonfatal). Because it is nonfatal and matches the `WHEN EC-BOUND-SUBSCRIPT`:
- GR17: imperative-2 runs → `DISPLAY "CAUGHT"` → `CAUGHT\n`. (The `DISPLAY "AFTER"` is **skipped**: GR20 nonfatal resumes with an implicit CONTINUE *after the raising statement* — but control has already gone through the handler; the raising statement was `MOVE`, so resumption is the point right after `MOVE`, i.e. `DISPLAY "AFTER"` *would* be next — **however** the handler path proceeds to FINALLY as the end of the PERFORM, NOT back into imperative-1. GR20's "continue after the raising statement" applies when the handler *completes and there are remaining imperative-1 statements only if the raise did not divert the whole flow*; since a matched WHEN transfers to imperative-2 and then to end-of-PERFORM/FINALLY, `AFTER` is not printed.) → **`AFTER` absent.**
- GR16/NOTE 8: FINALLY always runs → `DISPLAY "FINALLY"` → `FINALLY\n`.
- End of PERFORM → `DISPLAY "DONE"` → `DONE\n`. `STOP RUN`.

*(Derivation note on the `AFTER`/GR20 subtlety: GR20's "implicit CONTINUE immediately following the raising statement … if that statement was the last, execution continues at the end of the PERFORM" governs the case where imperative-1 is *resumed*. But GR17→GR20 route is: run imperative-2, then "control is passed as indicated in GR20." The nonfatal branch of GR20 says resume after the raising statement. The raising statement `MOVE` is **not** the last statement of imperative-1 (`DISPLAY "AFTER"` follows), so a strict reading resumes at `DISPLAY "AFTER"` — which WOULD print `AFTER`. This is the one genuinely ambiguous corner. Resolve it at implementation time by re-reading GR20 against §14.6.13.1.4: for a nonfatal EC that was **handled by a WHEN**, the "implicit CONTINUE after the raising statement" means the statement's own execution is treated as complete and the NEXT statement of imperative-1 runs — i.e. **`AFTER` IS printed** and expected bytes become `"BEFORE\nAFTER\nCAUGHT\nFINALLY\nDONE\n"`. **⚠ This ordering — does a handled nonfatal WHEN resume into the rest of imperative-1, or fall to end-of-PERFORM? — is THE load-bearing semantic question for this construct and MUST be pinned by a fresh §14.9.28.4 GR20 + §14.6.13.1.4 re-read before goldening. Design the golden so the raising statement is the LAST statement of imperative-1, eliminating the ambiguity for the shipped conformance test:** put `MOVE 7 TO WS-E(WS-I)` last, giving unambiguous `"BEFORE\nCAUGHT\nFINALLY\nDONE\n"`.)*

**Recommended unambiguous golden body** (raising statement last):
```
           PERFORM
               DISPLAY "BEFORE"
               MOVE 7 TO WS-E (WS-I)
             WHEN EC-BOUND-SUBSCRIPT
               DISPLAY "CAUGHT"
             FINALLY
               DISPLAY "FINALLY"
           END-PERFORM
```
→ **`BEFORE\nCAUGHT\nFINALLY\nDONE\n"`** (GR20 "raising statement was the last → end of PERFORM" is unambiguous; FINALLY still runs per NOTE 8).

**Negative (below-edition) fixture** — same program compiled at `--std 2014`:
```
cobol run wc79-perf-f3.cob --std 2014
```
**Expected diagnostic:** `COBOLNET0900: the exception-checking PERFORM statement requires COBOL-2023 (targeting COBOL-2014)` (non-zero exit; no run). Mirror at `--std 85` and `--std 2002`. At `--std 2023` the program compiles and runs, producing the stdout above.

### Blast radius / hazards
- **Shared ANTLR parser (memory ⚠):** the grammar change is *additive* (new tokens + new alternative), so the frozen legacy `CobolSharp.Compiler` binder never reaches it — but **run the FULL legacy guard** (`scripts/guard-fast.sh` legacy legs + the legacy conformance suite) because a new alternative can perturb ANTLR's decision DFA for the sibling inline PERFORM alts. Watch the legacy PERFORM/inline-PERFORM regression rows.
- **Greedy `statementBlock+` capture:** the highest ambiguity risk. A plain inline `PERFORM … END-PERFORM` whose body coincidentally is followed by nothing must still take the generic inline alt, not the F3 alt (F3 requires `performWhenClause+`). Add a conformance probe of a plain inline PERFORM to prove no mis-selection.
- **EC-engine correctness:** the compile-time `TurnState`-bypass for the WHEN region is a new emit path; verify it does **not** leak enabled-checking into statements *after* the PERFORM (GR22) — regression-watch the existing EC/TURN goldens and the `>>TURN` conformance rows.
- **GR20 resumption ambiguity** (above) — pin by spec re-read; ship the raising-statement-last golden to sidestep it in the shipped test but capture the resolved rule in the deep-dive.
- **Suites to watch:** greenfield conformance (EC family, TURN/PERFORM), the version-matrix constructs suite (new row), the legacy guard (1196+654), NIST 353 MATCH (PERFORM-heavy), characterization snapshots (any program emitting a PERFORM near an EC guard could shift bytes if the emit-scope change is not perfectly isolated).
- **Do NOT unlock RESUME in WHEN handlers** — `EcBinder.cs:122` stays declarative-only (§14.9.33.3 SR1); this wave only *removes* the "later wave" deferral note by shipping Format 3, it does not extend RESUME.

---

## Wave C — SUPPRESS WHEN on ALTERNATE RECORD KEY (VCR 85)

## VCR row 85 — SUPPRESS WHEN phrase on the ALTERNATE RECORD KEY clause (ISO §12.4.5.6)

### Spec sections (grep -n, spec lines read)

- **§12.4.5.1 Format 1 (indexed SELECT)** — lines 14983–14987: the ALTERNATE RECORD KEY clause syntax in the file-control entry shows `[ WITH DUPLICATES ] [ SUPPRESS WHEN literal-2 ]` (note the SELECT format calls it *literal-2*).
- **§12.4.5.6.2 general format** — lines 15330–15336: the standalone ALTERNATE RECORD KEY clause box: `ALTERNATE RECORD KEY IS { data-name-1 | record-key-name-1 SOURCE IS {data-name-2}… } [ WITH DUPLICATES ] [ SUPPRESS WHEN literal-1 ]`; the prose confirms both `WITH DUPLICATES` and `SUPPRESS WHEN literal-1` are **optional** and independent.
- **§12.4.5.6.3 SR7** — line 15353: *literal-1 shall be an alphanumeric literal, a national literal, or a figurative constant, and shall be of the same category as data-name-1 or data-name-2. If ALL literal is specified, the literal shall be one character long.* (the only syntax rule specific to the suppression literal).
- **§12.4.5.6.4 GR6** — line 15376: *Literal-1 establishes the key suppression value for this alternate record key for WRITE and REWRITE statements. Alternate record key suppression specifies that an alternate record key access path to a particular record shall not be provided when the value of data-name-1 or record-key-name-1 in that record is equal to literal-1.* NOTE (line 15378): *The suppression does not impact READ and START although the suppressed records will not be processed by these statements. It is as if they did not exist.*
- **§14.9.30 READ GR21 indexed rule c** — line 29996: *If the key of reference is an alternate key, any record identified as being suppressed by the SUPPRESS WHEN phrase … is not considered to exist.*
- **§14.9.35 REWRITE GR24** — lines 30673–30685: on REWRITE, if the alt key value **is no longer equal** to literal-1 → an access path *shall be provided* and the record *logically positioned so it will be found*; if the alt key value **is now equal** to its suppression value → the access path *shall no longer be provided* and the record *repositioned so it will not be found*. Trailing paragraph (30685): *Any number of records may have the same alternate key value equal to its key suppression value without requiring the DUPLICATES phrase… Key entries that are suppressed shall not cause a duplicate key condition to exist.*
- **§14.9.41 START GR17 rule e** — line 32191: *If the key of reference is an alternate key, any record identified as being suppressed by the SUPPRESS WHEN phrase … is ignored.*
- **§14.9.51 WRITE GR41** — lines 33638–33651: *For each alternate record key for which alternate record key suppression is specified and for which the value of the ALTERNATE RECORD KEY phrase is equal to the literal specified in that phrase:* (a) *the access path … shall not be provided,* and (b) *the record shall be logically positioned so that it will not be found when accessed using the alternate record key.* Plus the identical "suppressed entries shall not cause a duplicate key condition" paragraph (33651).
- **§12.4.5.6.4 GR3 / §14.9.27 GR (persistence) k** — line 15197 & 15370: SUPPRESS WHEN is a **fixed file attribute** (line 11238 lists "SUPPRESS WHEN attribute" among immutable physical-file attributes); the same-attribute cross-connector consistency rule is line 15197 item k.
- **§14.9.30 READ GR27 ('02' lookahead)** — lines 30098–30102: '02' when the key of reference is an alternate and the *adjacent* record duplicates the key just read (this operates over the **visible** sequence, so suppressed records are already excluded).
- **Introduction / Foreword list of principal changes** — line 1221: *"Alternate key suppression on indexed files using the SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause"* (COBOL-2023 new-feature proof #1).
- **Annex E.3.3 "Not affecting", item 42** — line 50336: *"SUPPRESS WHEN phrase. …"* (COBOL-2023 substantive-change proof #2; "not affecting existing programs" because it is purely additive).
- **§9.1.7.4 / overview** — line 41982: prose summary — *"the records suppressed would be skipped on reading the file and ignored when using the START statement."*

### Syntax / format

Additive to the existing `alternateKeyClause` (both the file-control SELECT format and the standalone clause are the SAME grammar rule):

```
ALTERNATE RECORD? KEY? IS? dataReference
    (WITH? DUPLICATES)?
    (SUPPRESS WHEN literal)?          // ← NEW, additive, both phrases independently optional
```

- `SUPPRESS` and `WHEN` are both **mandatory** keyword tokens of the phrase (no optional noise words). Both tokens already exist: `SUPPRESS` (CobolLexer.g4:301), `WHEN` (used throughout).
- `literal` is the existing `literal` grammar rule (it admits alphanumeric/national/figurative — a superset of SR7; SR7's alphanumeric/national/figurative restriction and the ALL-one-char rule are enforced semantically, not in the grammar).
- The two optional phrases `(WITH? DUPLICATES)?` and `(SUPPRESS WHEN literal)?` are order-fixed as shown in the spec box (DUPLICATES before SUPPRESS); do **not** make them permutable — the spec shows a fixed order and no CCVS corpus needs the reverse.

### Introduced edition & gate

- **Edition:** COBOL-2023. Proof: Foreword principal-changes list (line 1221) **and** Annex E.3.3 item 42 (line 50336). Valid at 2023 only; **reject at 85/2002/2014**.
- **Below-edition behavior:** at `--std 85|2002|2014` the presence of the `SUPPRESS WHEN` phrase raises **COBOLNET0900** ("… requires COBOL-2023 (targeting COBOL-20xx) …"). ALTERNATE RECORD KEY itself is edition-invariant (indexed I-O is COBOL-85), so **only** the suppression phrase is gated — a plain `ALTERNATE RECORD KEY … [WITH DUPLICATES]` continues to compile at every edition.
- **Gate mechanism (recognition-based, drop-proof — the §14g.4 SHARING/LOCK-MODE pattern, VersionConformancePass.cs:867–873):**
  1. New construct id `suppress-when-alt-key-2023` in `tests/version-matrix/constructs.json` (the canonical catalogue; `ConstructRegistry.g.cs` is regenerated by `scripts/gen-constructs.ps1`, asserted by `ConstructRegistryDriftTests`). Row fields: `introducedIn: 2023`, `removedIn: null`, `diagnosticCode/expectDiagnostic: "COBOLNET0900"`, `display: "SUPPRESS WHEN alternate-key phrase"`, `citation: "ISO §12.4.5.6.4 GR6; Annex E.3.3 item 42"`, `vcr: "VCR row 85 (SUPPRESS WHEN phrase — 2023 introduction)"`, plus a `source` witness.
  2. A `ParseArm.VisitAlternateKeyClause` override (VersionConformancePass.cs, in the §14g.4 file-control cluster next to `VisitSharingClause`) that fires `_p.Check(Constructs.SuppressWhenAltKey2023, "the SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause")` **only when `ctx.SUPPRESS() is not null`** (so a plain alt-key clause is not gated), then `return base.VisitChildren(ctx)`. Recognition-based → names the edition even if the SELECT otherwise fails to bind (drop-proof, DEVLOG 724).
  3. `Constructs.g.cs` gets `public const string SuppressWhenAltKey2023 = "suppress-when-alt-key-2023";` (regenerated).

### Semantics (GR by GR — runtime, per the connector's derived-ordering model)

The connector's core invariant (IndexedConnector.cs:8–16) — *the arrival-ordered record list is the SOLE source of truth; every alternate ordering is re-derived per operation* — makes suppression a **pure filter predicate recomputed from each record's current image**, with **zero** WRITE/REWRITE indexing bookkeeping. Define, per alternate key *i* that has a suppression value `sv_i`:

> **A record `r` is *suppressed for key i* ⇔ `KeyOf(r.Image, i) == sv_i`** (ordinal equality over the file collating sequence, §12.4.5.6.4 GR6 last sentence — "rules for a relation condition").

- **GR6 / WRITE GR41 / REWRITE GR24 (indexing):** because a suppressed record is simply **filtered out of the derived alternate ordering at access time**, WRITE and REWRITE need *no* suppression-specific storage step — the record is always stored in arrival order; whether its alt-*i* access path "exists" is decided on read. REWRITE's suppress↔unsuppress transition (GR24) is automatic: the next READ/START re-slices the *new* image and the predicate flips.
- **READ GR21c (sequential) + START GR17e:** the derived sequence for key of reference *i* **excludes** records suppressed for *i*. A suppressed record "is not considered to exist" / "is ignored". Bake the filter into `Ordered(keyIndex)`.
- **READ GR32 (random):** a random READ whose key of reference is alt *i* and whose supplied key value equals `sv_i` finds only suppressed candidates → **all filtered → invalid key '23'** (record not found). Falls straight out of the same predicate applied in the `ReadRandom` scan loop.
- **'02' duplicate lookahead (READ GR27):** operates over the **visible** (filtered) sequence, so the "adjacent" record is the next *non-suppressed* one and GR27 duplicate detection is correct with no extra work.
- **WRITE '22'/'02' + no-DUPLICATES interaction (GR40/GR42c + GR41 last ¶, "suppressed entries shall not cause a duplicate key condition"):** when checking alt *i* for a duplicate on WRITE, **skip the check entirely when the NEW record's alt-*i* value equals `sv_i`** — the new record is not indexed on *i*, so it can raise neither '22' nor '02'. Existing suppressed records auto-fall-out (their value `== sv_i ≠` the non-suppressed new value). Same skip for REWRITE (§14.9.35 GR25c). This is the *only* WRITE/REWRITE code touch and it is exactly the spec's "Key entries that are suppressed shall not cause a duplicate key condition to exist."
- **Prime key never suppressed:** the filter applies **only when `keyIndex >= 0`**; prime (`-1`) and non-suppressing alternates are untouched. `Ordered(-1)` (used by OPEN EXTEND's highest-prime seed, line 134) is unaffected.
- **Edge cases:** (a) a suppression value shorter/longer than the key width is padded/truncated to the key window exactly like `KeyOf` (fixed-width slice) — precompute `sv_i` padded to `_alts[i].Len`; (b) ALL-figurative (`ALL "X"`) → one-char per SR7, expanded to the key width; (c) `SPACES`/`ZEROS` figurative → expanded to width; (d) a suppression value equal to a would-be duplicate does NOT need `WITH DUPLICATES` (GR41 ¶, line 33651) — the skip-when-suppressed logic already guarantees this.

### As-built today (file:line anchors, confirmed by reading + CLI probe)

- **Grammar:** `alternateKeyClause` (CobolIO.g4:146–149) = `ALTERNATE RECORD? KEY? IS? dataReference (WITH? DUPLICATES)?` — **no SUPPRESS WHEN**. `SUPPRESS` token exists (CobolLexer.g4:301). CLI probe confirms today's hard failure:
  ```
  wave85_sup.cob(11,63): error COBOL0312: unexpected 'SUPPRESS'. Unexpected token in FILE-CONTROL paragraph...
  ```
- **Binder:** `DataBinder.cs:648–652` captures `(name, quals, DUPLICATES present)` into `FileModel.AlternateKeyNames`; resolved at :875–877 into `FileModel.AlternateKeys = List<(DataItem Item, bool Duplicates)>` (FileModel.cs:75, 78). **No suppression field.**
- **Emitter:** `KeyedIoEmitter.cs:63–72` emits one `CobolFile.AddAlternateKey(name, aOff, alt.ImageWidth, dups)` per alt key; offset via `RecordLayout.OffsetOf(alt)`. `RuntimeApi.FileAddAlternateKey` (RuntimeApi.cs:292) is the 4-arg shim.
- **Runtime:** `IndexedConnector._alts = List<(int Off, int Len, bool Dups)>` (line 28); `AddAlternateKey(offset,length,duplicates)` (line 78). READ NEXT/PREV `ReadSequential` uses `Ordered(_refKey)` (line 184); random `ReadRandom` scans `_recs` (line 253); `Start` uses `Ordered(keyIndex)` (line 388); `Ordered` derives per-call (line 444). WRITE dup-alt check at :296–303; REWRITE at :332–340. **No suppression anywhere.**
- Baseline (non-suppressed) alt-key random READ works — CLI probe `wave85_base.cob` at `--std 2023` printed `AAA-FOUND P01` / `BBB-FOUND P02`, confirming the alt-key read path is sound to extend.
- **FileRegistry.AddAlternateKey** (FileRegistry.cs:120–124) forwards to the connector; **CobolFile.AddAlternateKey** (CobolFile.cs:51) is the static seam.

### AUDIT DRIFT CAUGHT

- **Section number wrong.** The audit row cites *"§13.x ALTERNATE RECORD KEY"*. The ALTERNATE RECORD KEY clause and its SUPPRESS WHEN phrase live in **§12.4.5.6** (File-Control entry), with the SELECT format in **§12.4.5.1** (Format 1 indexed). §13.x is the DATA DIVISION; there is no §13.x ALTERNATE RECORD KEY. Corrected citations for all downstream work: **§12.4.5.6.2** (format), **§12.4.5.6.3 SR7** (literal), **§12.4.5.6.4 GR6** (semantics), **§14.9.51 GR41 WRITE**, **§14.9.35 GR24 REWRITE**, **§14.9.30 GR21c READ**, **§14.9.41 GR17e START**.
- **E.3.3 item 42 — VERIFIED CORRECT.** Line 50336 is genuinely Annex E.3.3 "Not affecting", item 42 = SUPPRESS WHEN. The audit's "(E.3.3 item 42)" checks out. (Additional stronger proof the audit omitted: the Foreword principal-changes list, line 1221.)
- **The task prompt's own GR pointers refined.** The prompt cited "§14.9.51 GR41 WRITE / §14.9.30 GR21c REWRITE". READ suppression is §14.9.30 **GR21c** (correct); REWRITE suppression is **§14.9.35 GR24**, not §14.9.30 GR21c. WRITE is §14.9.51 GR41 (correct). START (§14.9.41 GR17e) is also load-bearing and was unlisted.
- **Task-prompt golden premise refined.** The prompt suggests "write a record whose alt key matches the SUPPRESS condition → a READ on the alt key fails to find it." Correct — but note the record IS written and IS retrievable by its **prime** key and by any **other** (non-suppressed) alternate; only the suppressing alt-key access path is withheld. The golden must not assert the record is absent from the file.

### Implementation plan (concrete change list)

1. **Grammar (ADDITIVE — SAFE, no restructure).** CobolIO.g4:146:
   ```
   alternateKeyClause
       : ALTERNATE RECORD? KEY? IS? dataReference
         (WITH? DUPLICATES)?
         (SUPPRESS WHEN literal)?
       ;
   ```
   Purely appends an optional trailing phrase — the existing `AlternateKeyClauseContext` accessors (`dataReference()`, `DUPLICATES()`) are unchanged, so the **frozen legacy `CobolSharp.Compiler` binder is unaffected** (it never reads `.SUPPRESS()`/`.literal()`). Still run the **FULL legacy guard** (the ANTLR parser is shared) — expect zero legacy diffs since the change is additive. Regenerate `Generated/` (Java + pwsh), which is a gitignored build output.
2. **Binder.** `FileModel.AlternateKeyNames` → add a suppression element; `AlternateKeys` tuple → `(DataItem Item, bool Duplicates, string? SuppressValue)`.
   - DataBinder.cs:648–652: when `ak.SUPPRESS() is not null`, evaluate `ak.literal()` to its fixed alphanumeric string via the existing VALUE/literal constant path (`DataBinder.Constants.cs` — the same evaluator that folds `figurativeConstant`/`nonNumericLiteral` for VALUE clauses); carry the raw evaluated string.
   - DataBinder.cs:875–877: resolve and pad the suppression string to the alt key's `ImageWidth` (SR7 ALL/figurative expansion happens here), store into `AlternateKeys`.
   - **SR7 semantic check (optional; ⛔ the code number below is STALE — allocate one fresh code from the plan §0
     next-free, see the top banner):** if the literal is numeric (not alphanumeric/national/figurative) or an `ALL`
     literal longer than one char, `Edition.Error("<SUPPRESS-SR7>", "…SUPPRESS WHEN literal must be an
     alphanumeric/national literal or figurative constant of the alternate key's category (ISO §12.4.5.6.3 SR7)")`.
     If deferred, note it — the grammar's `literal` already blocks a bare numeric in most positions, but
     national/category mismatch is only catchable here.
3. **Emitter.** KeyedIoEmitter.cs:63–72 — pass the suppression string (or `null`) to a **5-arg** `AddAlternateKey(name, aOff, width, dups, suppressValueOrNull)`; `RuntimeApi.FileAddAlternateKey` (RuntimeApi.cs:292) and `CobolFile.AddAlternateKey` (CobolFile.cs:51) + `FileRegistry.AddAlternateKey` (FileRegistry.cs:120) gain the 5th `string?` param. Emit the C# string literal via the existing `CsLiteral` helper (used at KeyedIoEmitter.cs:44).
4. **Runtime (IndexedConnector.cs).**
   - `_alts` tuple → `List<(int Off, int Len, bool Dups, string? Suppress)>`; `AddAlternateKey(int,int,bool,string?)` stores the padded suppression value.
   - `Ordered(int keyIndex)` (line 444): add `.Where(r => keyIndex < 0 || _alts[keyIndex].Suppress is not {} sv || KeyOf(r.Image, keyIndex) != sv)` to the LINQ derivation — one predicate covers READ NEXT/PREV, START rel-op, START FIRST/LAST.
   - `ReadRandom` (line 253 loop): add the same suppression skip to the candidate scan (`&& (keyIndex < 0 || _alts[keyIndex].Suppress is not {} sv || value != sv)` — trivially, since `value == KeyOf(rec,keyIndex)`, guard on `value != sv` before the loop and short-circuit to '23' when the requested value IS the suppression value).
   - WRITE (line 297–303) and REWRITE (line 333–340): in the per-alt duplicate loop, **skip alt *i* when `_alts[i].Suppress is {} sv && KeyOf(image, i) == sv`** (the new record is not indexed on *i*) — no '22'/'02' from a suppressed key (§14.9.51 GR41 ¶, §14.9.35 GR25c ¶).
   - Add citations to the XML docs (§12.4.5.6.4 GR6, §14.9.30 GR21c, §14.9.41 GR17e, §14.9.51 GR41, §14.9.35 GR24).
5. **constructs.json + regen.** New row `suppress-when-alt-key-2023` (fields in the gate section above); run `scripts/gen-constructs.ps1`; `Constructs.g.cs` const `SuppressWhenAltKey2023`.
6. **VersionConformancePass.cs.** `ParseArm.VisitAlternateKeyClause` override in the §14g.4 cluster (near :867), gated on `ctx.SUPPRESS() is not null`.
7. **Docs.** Update `docs/COBOLNET_DESIGN.md` (indexed-file / edition-gate section), `docs/VERSION_CHANGE_REFERENCE.md` (a new row for SUPPRESS WHEN), the grammar doc for CobolIO.g4, and the P13 audit row status → DONE. DEVLOG entry.
- **Diag codes:** introduction gate **COBOLNET0900** (reused via the construct — still valid). Optional SR7
  semantic error → ⛔ one fresh code from the plan §0 next-free (the doc's original "1570" is STALE and collided
  with six sibling claims; see the top banner). If SR7 is deferred, no new 15xx is consumed.

### Golden

**Positive fixture `p13_suppress_when.cob` (compile at `--std 2023`):**

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P13SUPWHEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "p13supwhen.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-PRIME
               ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES
                   SUPPRESS WHEN "XXX"
               FILE STATUS IS IX-ST.
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 IX-REC.
          05 IX-PRIME PIC X(3).
          05 IX-ALT   PIC X(3).
       WORKING-STORAGE SECTION.
       01 IX-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IX.
           MOVE "P01" TO IX-PRIME. MOVE "AAA" TO IX-ALT. WRITE IX-REC.
           MOVE "P02" TO IX-PRIME. MOVE "XXX" TO IX-ALT. WRITE IX-REC.
           MOVE "P03" TO IX-PRIME. MOVE "BBB" TO IX-ALT. WRITE IX-REC.
           CLOSE IX.
           OPEN INPUT IX.
      *    Random read on a NON-suppressed alt value -> found.
           MOVE "AAA" TO IX-ALT.
           READ IX KEY IS IX-ALT
               INVALID KEY DISPLAY "ALT-AAA NOTFOUND"
               NOT INVALID KEY DISPLAY "ALT-AAA FOUND " IX-PRIME.
      *    Random read on the SUPPRESSED alt value -> not found (23).
           MOVE "XXX" TO IX-ALT.
           READ IX KEY IS IX-ALT
               INVALID KEY DISPLAY "ALT-XXX NOTFOUND ST=" IX-ST
               NOT INVALID KEY DISPLAY "ALT-XXX FOUND " IX-PRIME.
      *    Sequential walk over the alt key -> suppressed P02 is skipped.
           MOVE "AAA" TO IX-ALT.
           START IX KEY IS NOT LESS THAN IX-ALT
               INVALID KEY DISPLAY "START FAIL".
           PERFORM UNTIL IX-ST NOT = "00"
               READ IX NEXT RECORD
                   AT END DISPLAY "SEQ-END"
                   NOT AT END DISPLAY "SEQ " IX-PRIME " " IX-ALT
               END-READ
           END-PERFORM.
      *    Prove the record still exists via its PRIME key.
           MOVE "P02" TO IX-PRIME.
           READ IX KEY IS IX-PRIME
               INVALID KEY DISPLAY "PRIME-P02 NOTFOUND"
               NOT INVALID KEY DISPLAY "PRIME-P02 FOUND ALT=" IX-ALT.
           CLOSE IX.
           STOP RUN.
```

**Hand-derived expected stdout** (derived from the GRs, not the oracle):
- File after writes (arrival order): P01/AAA, P02/XXX, P03/BBB. IX-ALT suppression value = `"XXX"`.
- Random READ IX-ALT="AAA": §14.9.30 GR32 — sole match P01/AAA, not suppressed → **`ALT-AAA FOUND P01`**.
- Random READ IX-ALT="XXX": the only record with alt="XXX" is P02, which **is suppressed for the alt key** (GR21c/GR32 — "not considered to exist") → invalid key, IX-ST='23' → **`ALT-XXX NOTFOUND ST=23`**.
- START KEY NOT LESS THAN IX-ALT="AAA": §14.9.41 GR17e ignores suppressed records; visible alt sequence = {AAA→P01, BBB→P03} (XXX/P02 filtered). First ≥ "AAA" = AAA/P01, key of reference = IX-ALT, IX-ST='00'.
- PERFORM/READ NEXT walk (GR21c filters suppressed): P01/AAA (ST 00) → P03/BBB (ST 00) → at end (ST 10). P02 never appears. → **`SEQ P01 AAA`**, **`SEQ P03 BBB`**, then the AT END fires → **`SEQ-END`**, loop exits (ST="10" ≠ "00").
- Random READ IX-PRIME="P02": prime key is never suppressed → found → **`PRIME-P02 FOUND ALT=XXX`** (proves the record physically exists; only the alt access path was withheld).

```
ALT-AAA FOUND P01
ALT-XXX NOTFOUND ST=23
SEQ P01 AAA
SEQ P03 BBB
SEQ-END
PRIME-P02 FOUND ALT=XXX
```

*(Note on the DISPLAY spacing: `DISPLAY "SEQ " IX-PRIME " " IX-ALT` concatenates operands with no inserted separators, so `"SEQ "` + `"P01"` + `" "` + `"AAA"` = `SEQ P01 AAA`. Adjust the golden's literal spaces if the characterization harness normalizes trailing spaces.)*

**Below-2023 negative fixture** — the SAME source compiled at `--std 2014`:

Expected diagnostic (exact format confirmed by probing the sibling `goback-status-2023` gate, which emitted `error COBOLNET0900: GOBACK WITH NORMAL/ERROR STATUS requires COBOL-2023 (targeting COBOL-2014) - the … phrase (ISO …)`):

```
error COBOLNET0900: SUPPRESS WHEN alternate-key phrase requires COBOL-2023 (targeting COBOL-2014) - the SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause (ISO 12.4.5.6.4 GR6; Annex E.3.3 item 42)
```

(the `display` string and `where`/citation text are the row's authored fields; contains-based assertions on `COBOLNET0900` + `SUPPRESS WHEN` + `requires COBOL-2023` are robust). Add this as a `constructs.json` matrix row so the VERSION TEST MATRIX drives the 85/2002/2014 rejection + 2023 acceptance automatically.

### Blast radius / hazards

- **Highest-blast-radius Wave-C item** — touches the indexed I-O core. Suites to watch: the **legacy IX conformance suite** (IX1xx/IX2xx CCVS — IX101A–IX217A, alt-key/duplicate/START behavior) via the FULL legacy guard, and the **greenfield indexed-file conformance tests** (the `Cobol.Net` IX/keyed-IO golden set). Also the **characterization snapshots** — any that exercise indexed files (run the char gate; it is NOT in guard-fast, per `feedback_guard_fast_not_ci_complete`).
- **Additive-grammar safety:** the change appends an optional trailing phrase; the shared `AlternateKeyClauseContext` gains `SUPPRESS()`/`WHEN()`/`literal()` accessors but existing ones are unchanged → the frozen legacy binder cannot regress. Still MANDATORY: regenerate the parser on both OSes and run the full legacy guard (shared-.g4 rule).
- **Runtime regression surface is contained:** the suppression predicate is `null`/no-op for every existing file (no SUPPRESS WHEN → `Suppress is null` → filter is a pass-through), so all current IX behavior (dup-alt '02'/'22', START, READ NEXT/PREV order, arrival ordering) is byte-identical when no suppression is declared. Verify by re-running the baseline `wave85_base.cob` probe post-change → must still print `AAA-FOUND P01` / `BBB-FOUND P02`.
- **Watch the WRITE/REWRITE '22'/'02' skip:** the one behavioral subtlety — a suppressed-value record must NOT raise '22' even under a no-DUPLICATES alt key (GR41 ¶). Add a dedicated golden case: two records both with the suppressed alt value under a **no-DUPLICATES** alt key must BOTH write with '00' (not '22').
- **Fixed-attribute persistence (§12.4.5.6.4 GR3 / line 11238):** SUPPRESS WHEN is a physical-file attribute; two connectors over the same file must declare the same value. Out of scope for a single-connector golden but note for the EXTERNAL/sharing waves (E/…) — a mismatch is undefined; do not add a cross-connector consistency check unless a later wave requires it.

---

## Wave C slice + EC — STOP/GOBACK exit-code wiring + EC-BOUND-OVERFLOW/REF-MOD gates (VCR 75/30)

## Wave (STOP/GOBACK exit-code + EC-BOUND gate generalization) — spec-first re-scout

**Diagnostic-band note for sibling waves:** this slice proposes **ZERO new `COBOLNET15xx` codes**. The two edition gates it touches already exist and emit `COBOLNET0900` (`stop-run-status-2002`, `goback-status-2023`); the two runtime EC raises are runtime exceptions (no compile diagnostic); the one new directive gate reuses `COBOLNET0900`. **Next free `15xx` remains `1570`** for other waves.

---

### (1) VCR 75 — STOP RUN / GOBACK status-VALUE → process exit-code wiring (§14.9.42.4 GR5 · §14.9.18.4 GR7–GR10)

- **Spec sections:**
  - §14.9.42.2 (spec line 32222–32226): STOP general format — `STOP RUN [ WITH {ERROR|NORMAL} [ STATUS [ identifier-1 | literal-1 ] ] ]`.
  - §14.9.42.3 SR2–SR4 (32233–32237): identifier-1 = an **integer** data item, or a data item of usage DISPLAY or NATIONAL; if literal-1 numeric it shall be an **integer**; literal-1 shall not be zero-length.
  - §14.9.42.4 GR2 (32244): ERROR → OS indicates **error** termination "if such a capability exists". GR3 (32246): NORMAL → normal. GR4 (32248): neither → normal "unless error termination has been indicated by an implementor-defined mechanism". **GR5 (32250): "literal-1 or the contents of … identifier-1 are passed to the operating system. Any constraints … are defined by the implementor."** GR6 (32252): run unit terminates, control to OS.
  - §14.9.18.4 (GOBACK) GR2 (27741): in a **called** program, returns to activator (status irrelevant). GR3 (27745): **"If a GOBACK statement is executed in a program that is not under the control of a calling runtime element, the program operates as if executing a STOP statement, with a status phrase, if any."** GR7 (27753)/GR8 (27755)/GR9 (27763): in a **main** program ERROR/NORMAL/neither → OS termination indication. **GR10 (27765): in a main program with literal-1/identifier-2 specified, the value "is passed to the operating system" (implementor constraints).**
  - §8.3.1 / ProgramEmitter `mainUnit`: the run-unit main is the first top-level PROGRAM unit.

- **Syntax / format:** shared `statusPhrase` rule (`Core/CobolControlFlow.g4:257-258`): `WITH? (ERROR | NORMAL) (STATUS (dataReference | literal)?)?`. `WITH` optional (§5.2.3); **exactly one** of `ERROR`/`NORMAL` mandatory; `STATUS` and its value operand both optional. Attached to `stopStatement` (`:246`, `STOP RUN (statusPhrase)?`) and `gobackStatement` (`CobolParserCore.g4:1099`, tail alt mutually-exclusive with `raisingPhrase`).

- **Introduced edition & gate:** the `statusPhrase` on STOP is **COBOL-2002** (`Constructs.StopRunStatus2002`, `VersionConformancePass.cs:466-467` → `COBOLNET0900`); on GOBACK it is **COBOL-2023** (`Constructs.GobackStatus2023`, `VersionConformancePass.cs:1055-1056` → `COBOLNET0900`; annex item 32 "GOBACK … now allows the same status phrase as STOP RUN"). **Both gates already exist and fire** — this slice does NOT touch the gates, only the value wiring. Below-edition behavior is unchanged (rejected at `--std 85` for STOP, `--std 2014` for GOBACK).

- **Semantics (GR-level) — the implementor mapping this slice DEFINES (documented in `docs/CONFORMANCE.md` §4.2.16 and `docs/COBOLNET_DESIGN.md` decision 20):** the only observable of "passed to the operating system" / "termination indication" on .NET is the **process exit code** (`Environment.ExitCode`). GR5/GR10 (value passed) and GR2/3/4/7/8/9 (ERROR/NORMAL indication) collapse into that one integer, resolved as:
  1. **status value specified** (`STATUS id|lit`) → `ExitCode = integer value of the operand` — GR5/GR10 (the value is *passed*, so it wins regardless of ERROR/NORMAL). Integer literal → its value; integer data item → its numeric value; a DISPLAY/NATIONAL item → its numeric interpretation (SR2 permits non-numeric display; implementor constraint = parse the integer value, non-numeric → 0).
  2. **no value, `ERROR`** → `ExitCode = 1` (the implementor error indication, GR2/GR7).
  3. **no value, `NORMAL`** or **no status phrase at all** → `ExitCode = 0` (GR3/GR4/GR8/GR9 — the default, byte-identical to today).
  - **GOBACK scoping (GR2 vs GR3):** the status value only reaches the OS when the GOBACK runs in a program **not under a caller** (a main program) — i.e. `__asCalled == false`. In a called program the status phrase is inert (GR2 returns to the activator; GR7–GR10 are "in a main program" only). So the GOBACK emit writes the termination status **guarded by `!__asCalled`**.

- **The ONE run-unit termination-status mechanism (singular-pattern):** add a run-unit-scoped owner on `RunUnit` — `public long ExitStatus { get; set; }` (default 0) — the single realization of `COBOLNET_DESIGN.md` decision 20 ("RETURN-CODE is ONE canonical field … read as the process exit code"; lines 837-842, 1559-1560). Expose a static shim `RunUnit.SetExitStatus(long)` / `RunUnit.ExitStatus` mirroring the `ExceptionState`→`ExceptionEngine` facade (RunUnit.cs already exposes subsystems this way). Both STOP RUN status and main-program GOBACK status write this ONE field; the future RETURN-CODE special register (not yet implemented) writes the same field — never a second exit-code source.

- **As-built today (confirmed):**
  - `BoundStop.HasStatusPhrase` (`BoundTree.cs:353-360`) is **presence-only**; the doc comment (line 357) explicitly says "The status VALUE is not yet modeled". `ControlFlowBinder.BindStop` (`ControlFlowBinder.cs:29-33`) sets only `HasStatusPhrase = stop.statusPhrase() is not null`.
  - `StatementEmitter.Visit(BoundStop)` (`:91`) emits bare `throw new StopRun();`.
  - `BoundGoback` (`BoundCall.cs:69`) carries only `ReturningSource` + `Raising` — **no status field**. `CallEmitter.EmitGoback` (`:292-305`) emits RETURNING move + RAISING stage + `throw new ProgramReturn();` — no status.
  - `Program.Main` (`ProgramEmitter.cs:400-417`): `try { RunMain(...) } catch (StopRun) {}`; only the fatal catch sets `Environment.ExitCode = 1` (`:413`). Normal / StopRun paths leave ExitCode 0.
  - `DispatchEmitter.__Activate` (`:57`) catches `ProgramReturn` (a main-program GOBACK is swallowed here) — so a GOBACK status must be stored in the run-unit field **before** the throw, because ProgramReturn never reaches Main.
  - **CLI observability:** `cobol run` forwards the child exit code (`Cli/Program.cs:183-189`, `return proc.ExitCode`). So `cobol run f.cob; echo $?` observes the value end-to-end.
  - CLI probe confirming current default: `STOP RUN.` → exit 0; `STOP RUN WITH ERROR STATUS 42.` compiles (statusPhrase parsed) but currently exits **0** (value ignored) — the bug this slice fixes.

- **AUDIT DRIFT CAUGHT:**
  - The audit row (line 26) says GOBACK status is "DONE presence-only (the exit-code VALUE wiring is a staged slice)" — **verified correct**.
  - `BoundStop`'s comment (`BoundTree.cs:357`) calls the value wiring "the §12 RETURN-CODE wiring" — **partially misleading**: the 2023 STATUS phrase (§14.9.42.4 GR5 / §14.9.18.4 GR10) is a **distinct** mechanism from the classic RETURN-CODE special register. Design decision 20 (`COBOLNET_DESIGN.md:1559`) conflates "GOBACK GIVING" with the exit code — GIVING/RETURNING is the **activation result** (§14.9.18.4 GR2), not the OS status. This slice wires the **STATUS phrase**, and both it and the future RETURN-CODE feed the ONE `RunUnit.ExitStatus` field (keeping singular-pattern). Recommend a one-line correction to decision 20 in the same change set.
  - `StopRun`/`ProgramReturn` are parameterless `Exception`s (StopRun.cs:10, ProgramReturn.cs:12). The design sketch (`COBOLNET_DESIGN.md:1515`) says "`ProgramReturn` carrying the status" — **not as-built**; the status rides the `RunUnit.ExitStatus` field instead (cleaner: ProgramReturn is caught at `__Activate`, not Main, so a payload on it could not reach the exit path). Recommend correcting line 1515.

- **Implementation plan (grammar UNCHANGED — statusPhrase already parses; ADDITIVE C# only):**
  1. `BoundTree.cs` — replace `BoundStop.HasStatusPhrase : bool` with a modeled status: `BoundStop(TerminationStatus? Status)` where `TerminationStatus(bool Error, BoundExpr? Value)` (Value null ⇒ no `STATUS` operand). Keep a `HasStatusPhrase => Status is not null` computed prop so the `VersionConformancePass` gate (which reads the parse tree, not the bound node) is untouched.
  2. `BoundCall.cs` — `BoundGoback(Place? ReturningSource, BoundRaising? Raising, TerminationStatus? Status = null)`.
  3. `ControlFlowBinder.BindStop` — decode `statusPhrase`: `Error = sp.ERROR() is not null`; `Value = sp.dataReference()/sp.literal()` bound via `host.Expr` (integer). Build `BoundStop(status)`.
  4. `CallBinder` BindGoback (`:203-205`) — decode the shared `statusPhrase` into `TerminationStatus` when present (mutually exclusive with `raisingPhrase`, already enforced by the grammar).
  5. `StatementEmitter.Visit(BoundStop)` — emit `RunUnit.SetExitStatus(<expr>);` then `throw new StopRun();`, where `<expr>` = the value operand, else `Error ? 1 : 0`.
  6. `CallEmitter.EmitGoback` — when `g.Status is { }`, emit `if (!__asCalled) RunUnit.SetExitStatus(<expr>);` before `throw new ProgramReturn();` (GR3: only a main program passes the status).
  7. `ProgramEmitter.Main` (`:405-416`) — after `RunMain` and inside `catch (StopRun) {}`, set `Environment.ExitCode = (int) RunUnit.Current.ExitStatus;` (default 0 keeps every existing golden's exit code). The fatal catch's `= 1` stays (it wins for uncaught fatal ECs).
  8. Runtime: `RunUnit.cs` — add `long ExitStatus { get; set; }` + static `SetExitStatus`/`ExitStatus`. `RunUnit.Run`/`Reset` leave it 0 on a fresh run unit.
  9. **No `constructs.json` change** (gates exist); **no new diag**.
  - **BoundStores/UsageCollection/StatementChildren:** the new `BoundExpr Value` on BoundStop/BoundGoback must be reachable by the generated visitor — regenerate the `[BoundNode]` tree so `StatementChildren`/store-classification see it (BoundStop already `StoreKind.None`, `BoundStores.cs:97`; a Value expr is read-only). Watch `UsageCollectionPass` (a status data-item is a usage reference — mirror `BoundGoback`'s `P(n.ReturningSource)` at line 166 with `P(status value)`).

- **Golden:** exit-code is **not** a stdout golden (the conformance harness only reads `proc.ExitCode == 0` as a bool — `CompilerUnderTest.cs:83`, `AcceptDifferentialTests.cs:55`). **Proposed observation mechanism:** a new xUnit test class `StopGobackExitCodeTests` in `tests/Cobol.Net.Tests.Conformance` that compiles+runs via the existing `CutRunner`/`ProcessStartInfo` path but asserts the **numeric** `proc.ExitCode` (analogous to the legacy `CliExitCodeTests.cs`, but against `cobol.exe`/the greenfield CUT). Fixtures + hand-derived exit codes:
  - `STOP RUN WITH ERROR STATUS 42.` → exit **42** (GR5: value passed).
  - `STOP RUN WITH ERROR.` → exit **1** (GR2 no value → error indication).
  - `STOP RUN WITH NORMAL STATUS 7.` → exit **7** (GR5: value wins over NORMAL).
  - `STOP RUN.` → exit **0** (regression-lock the default).
  - main-program `GOBACK WITH ERROR STATUS 5.` → exit **5** (GR3/GR10).
  - a CALLed subprogram doing `GOBACK WITH ERROR STATUS 9.` while the main ends `STOP RUN.` → exit **0** (GR2: called-program status inert; `__asCalled` guard).
  - **Below-edition negatives** (stderr `.err` conformance fixtures): `STOP RUN WITH NORMAL STATUS 1.` at `--std 85` → `COBOLNET0900` ("… the STOP RUN … WITH NORMAL/ERROR STATUS phrase … requires --std 2002 …"); `GOBACK WITH ERROR STATUS 1.` at `--std 2014` → `COBOLNET0900` ("… the GOBACK … WITH NORMAL/ERROR STATUS phrase …").

- **Blast radius / hazards:** `BoundStop`/`BoundGoback` shape change ripples through the generated bound-tree visitor, `BoundStores`, `UsageCollectionPass`, `StatementChildren` — regenerate + build `CobolSharp.sln` before `--no-build` tests. Every existing STOP-RUN golden must still exit 0 (default path). The GOBACK `!__asCalled` guard must reference the same `__asCalled` flag `EmitExitProgram` uses (`CallEmitter.cs:316`). No grammar change ⇒ **no legacy guard needed** for this item. Watch the full conformance battery + the new exit-code test; the characterization snapshots for STOP/GOBACK emit will shift (the added `RunUnit.SetExitStatus` line) — re-baseline the 32 snapshots.

---

### (2) VCR 30 / staged — EC-BOUND-REF-MOD (fatal) + the REF-MOD-ZERO-LENGTH directive (§8.4.2.3 GR / §7.3.23 / Table 13)

- **Spec sections:**
  - Reference-modification GR (spec line **7089**): *"If the evaluation of leftmost-position or length results in a non-integer value, a zero value, or a value that references a position outside the area of identifier-1, the EC-BOUND-REF-MOD exception condition is set to exist. However, when the REF-MOD-ZERO-LENGTH directive is in effect, a zero-length result is allowed."* (§8.4.2.3; the unique-data-item definition is at 7081–7089.)
  - §7.3.23 REF-MOD-ZERO-LENGTH directive (4895–4914): format `>> REF-MOD-ZERO-LENGTH {ON | OFF}` (**OFF is the default/underlined**). GR1 (4914): *"When this directive is omitted or is specified as off, then when reference-modification results in a zero-length data item, the exception condition EC-BOUND-REF-MOD is raised."*
  - Table 13 (spec line **24644**): `EC-BOUND-REF-MOD | Fatal | Reference modifier out of bounds`.
  - Annex E change item 23 (49304): *"Reference-modification. The resultant data item may now have a length of zero, when the REF-MOD-ZERO-LENGTH compiler directive is in effect to allow it, otherwise the EC-BOUND-REF-MOD exception is raised. Previously the consequence of this result was undefined."* — the **2023 delta** is the zero-length ALLOWANCE via the directive.
  - FLAG-14 REF-MOD-ZERO-LENGTH flag (4523–4527): flag a ref-mod when the directive is unspecified AND EC-BOUND-REF-MOD checking is on (an optional diagnostic, out of scope here).

- **Syntax / format:** the directive `>> REF-MOD-ZERO-LENGTH ON` | `>> REF-MOD-ZERO-LENGTH OFF`, a text-manipulation-stage directive with "until end of compilation group / next directive" lexical scope. Reference modification itself (`identifier(leftmost:length)` or `identifier(leftmost:)`) is unchanged.

- **Introduced edition & gate:** **EC-BOUND-REF-MOD** is a COBOL-2002 Table-13 name (already catalogued, 2002). The **2023 delta** is the **REF-MOD-ZERO-LENGTH directive** (Annex E item 23). Currently `REF-MOD-ZERO-LENGTH` is in `ConditionalCompilationProcessor.KnownIgnoredDirectives` (`:37`) — parsed-and-**silently ignored**. Making it real requires a **new introduction-gate construct row** `ref-mod-zero-length-2023` (introducedIn 2023 → `COBOLNET0900`) so `>>REF-MOD-ZERO-LENGTH` at `--std < 2023` is rejected loudly (four-compilers rule). The EC raise wiring itself is edition-invariant runtime.

- **Semantics (GR-level) — the raise conditions at each ref-mod evaluation (§8.4.2.3 GR line 7089), gated by EC-BOUND-REF-MOD checking-enabled (§14.6.13.1.1) and, for the zero-length case only, by the directive:**
  - leftmost-position `< 1` → **out of range** → EC-BOUND-REF-MOD (fatal).
  - leftmost-position `> size` → **outside the area** → fatal.
  - explicit length `> 0` and `leftmost + length - 1 > size` → **outside the area** → fatal.
  - length `== 0` (an explicit zero-length ref-mod result) → fatal **UNLESS** REF-MOD-ZERO-LENGTH is `ON` (then allowed, no raise).
  - non-integer leftmost/length — an SR-level violation; our positions are already integers (arithmetic-expression operands truncated by the typed pipeline), so this reduces to the range tests above; note it but no distinct runtime raise.
  - **Fatal handling:** when checking on and unhandled → `CobolFatalException("EC-BOUND-REF-MOD")` → abnormal run-unit termination (§14.6.13.1.3 #5/#7); a USE F3 declarative may handle it and RESUME (RESUME NEXT STATEMENT `-2` ⇒ execution continues past the aborted statement). When checking **off** → no raise, the current lenient clamp/space-pad stands (byte-identical default behavior).
  - Applies to **both** sending (read, `CobolString.RefMod`) and receiving (write, `CobolString.SpliceInto`) reference modification — the GR is on the ref-mod evaluation, not the direction.

- **As-built today (confirmed):**
  - `CobolString.RefMod` (`Values/Text/CobolString.cs:44-54`) **clamps** out-of-range `start` to 0, computes `avail = max(0, s.Length - start)`, space-pads to the requested length; the doc comment (line 42) says *"the strict dialect raises EC-BOUND-REF-MOD — a later option"*. Never raises.
  - `CobolString.SpliceInto` (`:64-74`) returns `dst` unchanged when `start < 0 || start >= dst.Length` (silent no-op) — never raises.
  - `ExceptionState` has **no** `BoundRefModChecking` flag and **no** `RefModError` helper (grep confirmed absent).
  - Emit path: `PlaceRenderer.cs:31` renders a read ref-mod as `RuntimeApi.StrRefMod(Read(inner), RmStart, RmLen)`; `:56` renders a write as `StrSpliceInto(...)`; `RmStart`/`RmLen` = `(int)(start)` / `(int)(length)` or `"-1"` for the "to end" form (`:107-108`). `RuntimeApi.StrRefMod` sig at `:253`, `StrSpliceInto` at `:259`.

- **AUDIT DRIFT CAUGHT:** audit row (line 60) cites the ref-mod GR as "§8.4.3.3.4" — **wrong anchor**; there is no §8.4.3.3.4 in this spec (that id resolves to §8.8.4.2.4 numeric comparison, spec line 9618). The correct citation is **§8.4.2.3 reference modification, GR at spec line 7089** (+ the §7.3.23 directive + Table 13 line 24644). The audit's CobolString.cs:44-54 anchor and "clamps instead of raising" claim are **verified correct**. Fatality (Fatal) verified against Table 13.

- **Implementation plan (ambient-fatal gate = the EC-ARGUMENT-FUNCTION pattern; grammar essentially unchanged):**
  1. **Runtime gate (mirror `ArgumentFunctionChecking`/`ArgumentError`):** in `ExceptionEngine`/`ExceptionState` add `bool BoundRefModChecking { get; set; }` (ambient, run-unit-scoped, static shim) and helper `void RefModError(string detail)` = `if (BoundRefModChecking) { Set("EC-BOUND-REF-MOD", fatal:true); throw new CobolFatalException("EC-BOUND-REF-MOD", detail); }` (fatal ⇒ throws, exactly like `ArgumentError` at `ExceptionState.cs:178-186` but no return value).
  2. **Raise sites:** `CobolString.RefMod` gains an `bool allowZeroLength = false` optional param; **before** the current clamp, when the range/zero-length tests above fail, call `ExceptionState.RefModError(...)` (which throws only under checking-on) — then fall through to the existing clamp when it did not throw (checking off). Same in `SpliceInto`. Default `allowZeroLength=false` + the ambient flag defaulting false keeps **every existing call site byte-identical**.
  3. **Directive → allowZeroLength:** remove `REF-MOD-ZERO-LENGTH` from `KnownIgnoredDirectives` (`ConditionalCompilationProcessor.cs:37`); add a real directive handler that toggles a compile-time lexical flag (like SOURCE FORMAT's live handling). The binder threads the flag onto `RefModPlace` (add `bool AllowZeroLength` to `RefModPlace`, `Place.cs:172`), and `PlaceRenderer` (`:31`,`:56`) passes it as the `allowZeroLength` literal to `StrRefMod`/`StrSpliceInto`. `RuntimeApi.StrRefMod`/`StrSpliceInto` (`:253`,`:259`) gain the extra arg.
  4. **Ambient-flag wiring (statement guard):** in `EcEmitter.EmitArgOrPlain` (`EcEmitter.cs:91-120`) add an EC-BOUND-REF-MOD leg parallel to the EC-ARGUMENT-FUNCTION fatal leg — `ExceptionState.BoundRefModChecking = true; try { … } catch (CobolFatalException when EcName=="EC-BOUND-REF-MOD") { __EcDispatch; RESUME/rethrow } finally { =false }`. **Recommend generalizing** the two fatal legs (ARG-FUNCTION + REF-MOD) into one table-driven helper keyed by `(ecName, fatal)` to avoid a third copy — the singular-pattern move (see the shared infra note below).
  5. **Relevance filter (EcWrap):** in `EcBinder.EcWrap` (`:297-348`) add — when `EC-BOUND-REF-MOD` is enabled at the statement's line AND the statement contains a `RefModPlace`/`RedefViewPlace` (a new `ContainsRefMod(node)` Place-walk mirroring `ContainsIntrinsic`, `:381-382`) — `enabled.Add(("EC-BOUND-REF-MOD", null))`. Setting the flag around a ref-mod-free statement is harmless (no RefMod site fires), so the walk can be conservative.
  6. **New construct row** `ref-mod-zero-length-2023` in `ConstructRegistry.g.cs` (introducedIn 2023, `COBOLNET0900`, "ISO §7.3.23; Annex E.3.3 item 23"); the directive handler calls `ConstructRegistry.Check` when it sees `>>REF-MOD-ZERO-LENGTH`.
  - **Grammar:** the directive is preprocessor-level, not `.g4` — **no ANTLR change, no legacy guard** for the raise wiring. (If the directive is currently swallowed pre-lexer, the change is entirely in `ConditionalCompilationProcessor` + binder.)

- **Golden:**
  - **Positive (directive suppresses the zero-length raise):**
    ```
          >>REF-MOD-ZERO-LENGTH ON
          >>TURN EC-BOUND-REF-MOD CHECKING ON
           IDENTIFICATION DIVISION.
           PROGRAM-ID. REFMOD-ZL.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 WS-X PIC X(5) VALUE "HELLO".
           01 WS-Y PIC X(5) VALUE "-----".
           PROCEDURE DIVISION.
           MAIN-PARA.
               MOVE WS-X (3:0) TO WS-Y.
               DISPLAY "Y=[" WS-Y "]".
               DISPLAY "S=" FUNCTION EXCEPTION-STATUS.
               STOP RUN.
    ```
    Derivation: `WS-X(3:0)` is a zero-length result; REF-MOD-ZERO-LENGTH ON ⇒ **allowed, no EC**. The zero-length source MOVEd to a 5-char receiver space-fills all 5. EXCEPTION-STATUS clear ⇒ 31 spaces (harness trims). **Expected stdout:**
    ```
    Y=[     ]
    S=
    ```
  - **Fatal + declarative-observed (out-of-range, default directive OFF):**
    ```
          >>TURN EC-BOUND-REF-MOD CHECKING ON
           IDENTIFICATION DIVISION.
           PROGRAM-ID. REFMOD-OOR.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 WS-X PIC X(5) VALUE "HELLO".
           01 WS-Y PIC X(2) VALUE "??".
           PROCEDURE DIVISION.
           DECLARATIVES.
           H SECTION.
               USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
           H-P.
               DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
               RESUME NEXT STATEMENT.
           END DECLARATIVES.
           MAIN SECTION.
           MAIN-P.
               MOVE WS-X (7:2) TO WS-Y.
               DISPLAY "Y=[" WS-Y "]".
               STOP RUN.
    ```
    Derivation: leftmost 7 > size 5 → outside the area → EC-BOUND-REF-MOD (fatal) raised during the MOVE's source evaluation (before the store, so WS-Y stays "??"). Checking on + declarative selects USE F3, DISPLAYs the name, RESUME NEXT STATEMENT (`-2`, execution continues past the aborted MOVE). **Expected stdout:**
    ```
    CAUGHT=EC-BOUND-REF-MOD
    Y=[??]
    ```
  - **Below-edition negative** (`.err`): `>>REF-MOD-ZERO-LENGTH ON` at `--std 2014` → `COBOLNET0900` ("… REF-MOD-ZERO-LENGTH directive … requires --std 2023 …").
  - **Fatal-termination variant** (no declarative): the same out-of-range MOVE → stderr `abnormal run-unit termination: EC-BOUND-REF-MOD …`, exit 1, "Y=[" line NOT printed (asserted by the new exit-code test).

- **Blast radius / hazards:** `RefModPlace` shape change (`Place.cs:172`) ripples through every RefMod consumer (`MoveClassifier`, `ConditionBinder`, `ArithmeticBinder`, `NumericRenderer`, `OperandText` — all grepped). The two-arg RefMod signature change touches `RuntimeApi` + `PlaceRenderer` only (defaulted). **The lenient default (checking off) MUST stay byte-identical** — verify the full conformance battery + the legacy differential guard (RefMod is heavily exercised). Watch the RedefViewPlace path (`PlaceRenderer.cs:45`,`:68`) which also routes through StrRefMod/StrSpliceInto — decide whether a REDEFINES-view synthetic ref-mod should raise (it is compiler-generated, in-range by construction — pass `allowZeroLength:true`/checking-inert to avoid spurious raises). Characterization snapshots shift.

---

### (3) staged — EC-BOUND-OVERFLOW (nonfatal) on OCCURS DYNAMIC implicit growth past expected capacity (§8.5.1.9.6 GR1 / Table 13)

- **Spec sections:**
  - §8.5.1.9.1 (8199–8201): current capacity initialized from FROM (or VALUE), else zero; the **TO phrase = the expected capacity**, "which may be exceeded with a nonfatal exception"; the implementor/resource limit = the maximum capacity.
  - §8.5.1.9.3 Implicit changes in capacity (8221): a **receiving** subscript exceeding current capacity auto-creates the element and grows the table; skipped intermediate occurrences are implicitly created.
  - **§8.5.1.9.6 GR1 (spec line 8253):** *"EC-BOUND-OVERFLOW. The nonfatal EC-BOUND-OVERFLOW exception condition shall exist when a dynamic-capacity table has an expected capacity and an operation causes this expected capacity to be exceeded. **If the change in capacity was implicit and the expected capacity had already been exceeded before the operation, no exception shall exist.**"* GR1 continuation (8255): when checking is off (or a declarative RESUMEs NEXT STATEMENT), the operation continues, exceeding the expected capacity.
  - §8.5.1.9.6 (2) (8257): EC-BOUND-TABLE-LIMIT (fatal) on exceeding the **maximum** capacity — already implemented (`CobolDynTable.GrowTo`, `:77-79`).
  - Table 13 (spec line **24642**): `EC-BOUND-OVERFLOW | NF | Current capacity of dynamic-capacity table greater than expected value`. (Sibling `EC-BOUND-SET | NF`, line 24645, is the **explicit-SET** twin — §14.9.39 GR30 — out of this slice's scope.)

- **Syntax / format:** no new syntax. Enabling construct: `OCCURS DYNAMIC CAPACITY IN cap FROM min TO expected` (as-built syntax confirmed from `tests/conformance/2014/dyn_capacity_bounds.cob:10`). The EC fires only when a `TO expected` is present (§8.5.1.9.6 GR1 "has an expected capacity").

- **Introduced edition & gate:** dynamic-capacity tables are **COBOL-2014** (§8.5.1.9); the `OCCURS DYNAMIC` construct is the below-edition gate (rejected at `--std < 2014` by the existing OCCURS-DYNAMIC construct gate — not this slice). EC-BOUND-OVERFLOW is catalogued `EcFatality.Nonfatal` (`ExceptionCatalog.cs:76`). **Catalog IntroducedIn drift (flag):** the catalog defaults EC-BOUND-OVERFLOW to **2002** (the `L3(...)` default, `:76`) — but dynamic-capacity tables are 2014, so the correct IntroducedIn is **2014**. This only affects `>>TURN`/RAISE edition-gating of the name below 2014; since the enabling construct is itself 2014-gated the observable behavior is unchanged, but the catalog row should be corrected to 2014 in the same change set (the comment at `:64-67` already flags "Other 2014-era candidates (dynamic-capacity tables) default to 2002 — PROVISIONAL"). The raise wiring is edition-invariant runtime.

- **Semantics (GR-level):** at an **implicit** growth (receiving subscript > current capacity, `CobolDynTable.RefReceiving`), when the table has an expected capacity `_expected`:
  - `wasExceeded = _count > _expected` (current capacity already past expected **before** this op).
  - `willExceed  = occ > _expected` (the requested occurrence crosses expected).
  - **Raise EC-BOUND-OVERFLOW iff `willExceed && !wasExceeded`** — the FIRST crossing only. GR1's "already exceeded before + implicit ⇒ no exception" is exactly `!(wasExceeded)`.
  - Nonfatal (Table 13 NF): the growth **proceeds regardless** (never throws); it only sets the last-exception status **when EC-BOUND-OVERFLOW checking is enabled** (§14.6.13.1.1) — otherwise it's a pure no-op (byte-identical to today's default).
  - Explicit `SET … capacity` past expected is **EC-BOUND-SET**, not EC-BOUND-OVERFLOW — do **not** raise EC-BOUND-OVERFLOW from `SetCapacity` (`CobolDynTable.cs:93-101`); that is a separate staged item.

- **As-built today (confirmed):**
  - `CobolDynTable.GrowTo` (`Values/Tables/CobolDynTable.cs:74-88`) grows + seeds; the doc comment (`:70-73`) explicitly says *"the nonfatal capacity-overflow exceptions are NOT yet raised — EC-BOUND-OVERFLOW on implicit growth past the expected capacity (§8.5.1.9.6 item 1) … are checking-gated and, being nonfatal, produce identical observable results with checking OFF"*; `_expected` (`:23`, `:39`) is captured but unused for the raise.
  - `RefReceiving` (`:61-66`) calls `GrowTo((int)occ)` on `occ > _count` with no EC check.
  - `ExceptionState` has **no** `BoundOverflowChecking` flag (grep confirmed absent).
  - CLI probe (`dyn_capacity_bounds.cob` semantics): `SET WS-CAP TO 9` on `FROM 2 TO 4` currently continues silently to capacity 9 — the `.cob` comment itself notes EC-BOUND-OVERFLOW/SET is "a later increment".

- **AUDIT DRIFT CAUGHT:** audit row (line 59) cites "§8.5.1.9.1" for EC-BOUND-OVERFLOW — the **raise rule is §8.5.1.9.6 item 1 (spec line 8253)**; §8.5.1.9.1 is only the "expected capacity" definition. The audit's `CobolDynTable.cs:70-73` anchor and "explicitly does not raise" claim are **verified correct**, as is Nonfatal. **The audit omits GR1's critical edge** ("already exceeded before + implicit ⇒ no exception") — the raise must be first-crossing-only, not every-growth (a naive "raise whenever count > expected" would over-fire on every subsequent implicit grow).

- **Implementation plan (ambient-nonfatal gate = the EC-DATA-CONVERSION pattern; no grammar change):**
  1. **Runtime gate (mirror `DataConversionChecking`/`DataConversionError`):** in `ExceptionEngine`/`ExceptionState` add `bool BoundOverflowChecking { get; set; }` (ambient, static shim) and helper `void BoundOverflowError(string detail)` = `if (BoundOverflowChecking) Set("EC-BOUND-OVERFLOW", fatal:false)` (nonfatal ⇒ never throws — exactly `DataConversionError`, `ExceptionState.cs:198-201`).
  2. **Raise site:** in `CobolDynTable.RefReceiving` (`:61-66`), **before** `GrowTo`, when `_expected is { } exp && occ > _count`: compute `wasExceeded = _count > exp`, `willExceed = occ > exp`; if `willExceed && !wasExceeded` call `ExceptionState.BoundOverflowError($"OCCURS DYNAMIC implicit growth to {occ} exceeds the expected capacity {exp} — ISO §8.5.1.9.6")`. Keep `GrowTo` pure (it stays the fatal-EC-BOUND-TABLE-LIMIT owner; do not raise EC-BOUND-OVERFLOW from `SetCapacity`).
  3. **Ambient-flag wiring (statement guard):** in `EcEmitter.EmitChecked` (`:64-86`) add an EC-BOUND-OVERFLOW nonfatal leg parallel to the EC-DATA-CONVERSION leg (`:73-82`) — `ExceptionState.BoundOverflowChecking = true; try { EmitArgOrPlain(ec); } finally { =false; }`. Setting it around a non-dyn-table statement is harmless (no GrowTo site fires) — same as EC-DATA-CONVERSION around a non-CONVERT intrinsic.
  4. **Relevance filter (EcWrap):** in `EcBinder.EcWrap` add — when `EC-BOUND-OVERFLOW` is enabled at the line AND the statement has a **receiving** Place whose item `IsDynamicTable` (`DataItem.IsDynamicTable`, `DataItem.cs:152`; detect via a Place-walk for a target `AccessPath` containing a `DynTableSegment`, `AccessPath.cs:59`) — `enabled.Add(("EC-BOUND-OVERFLOW", null))`. Conservative over-wrap is harmless (nonfatal, no site sets it).
  5. **Catalog:** correct `EC-BOUND-OVERFLOW` IntroducedIn 2002 → **2014** (`ExceptionCatalog.cs:76`).
  - **Grammar:** none. No legacy guard.

- **Golden (nonfatal, EXCEPTION-STATUS-observed, proving the first-crossing-only edge via SET LAST EXCEPTION TO OFF):**
  ```
        >>TURN EC-BOUND-OVERFLOW CHECKING ON
         IDENTIFICATION DIVISION.
         PROGRAM-ID. DYN-OVERFLOW.
         DATA DIVISION.
         WORKING-STORAGE SECTION.
         01 WS-TABLE.
            05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 4.
         PROCEDURE DIVISION.
         MAIN-PARA.
             MOVE 11 TO WS-E (3).
             DISPLAY "S1=" FUNCTION EXCEPTION-STATUS.
             MOVE 22 TO WS-E (5).
             DISPLAY "S2=" FUNCTION EXCEPTION-STATUS.
             SET LAST EXCEPTION TO OFF.
             MOVE 33 TO WS-E (7).
             DISPLAY "S3=" FUNCTION EXCEPTION-STATUS.
             STOP RUN.
  ```
  Derivation: FROM 2 ⇒ initial current capacity 2; expected = TO 4.
  - `MOVE 11 TO WS-E(3)`: receiving 3 > current 2 → grow to 3. `willExceed = 3>4? no` → **no EC**. current=3. EXCEPTION-STATUS clear ⇒ `S1=`.
  - `MOVE 22 TO WS-E(5)`: 5 > current 3 → grow to 5. `was = 3>4? no`, `will = 5>4? yes` → **first crossing → EC-BOUND-OVERFLOW** (nonfatal, set). current=5. ⇒ `S2=EC-BOUND-OVERFLOW`.
  - `SET LAST EXCEPTION TO OFF` → clears (`ExceptionEngine.Clear`, `ExceptionState.cs:88`).
  - `MOVE 33 TO WS-E(7)`: 7 > current 5 → grow to 7. `was = 5>4? yes` (already exceeded, implicit) → **GR1: no exception**. Status stays clear ⇒ `S3=`.
  **Expected stdout (harness trims trailing spaces):**
  ```
  S1=
  S2=EC-BOUND-OVERFLOW
  S3=
  ```
  - **Below-edition negative:** the enabling construct `OCCURS DYNAMIC` at `--std 85`/`2002` is rejected by the existing OCCURS-DYNAMIC construct gate (`COBOLNET0900`) — not new to this slice; add a fixture only if the wave wants regression coverage of that gate.

- **Blast radius / hazards:** the raise site is nonfatal and default-off ⇒ **zero observable change with checking off** — the entire `tests/conformance/2014/dyn_*` battery must stay byte-identical. Verify `RefReceiving`'s benign-scratch path (`occ < 1`, `:63`) is untouched (no EC there — subscript < 1 is EC-BOUND-SUBSCRIPT, a separate item). The `_expected` null case (`OCCURS DYNAMIC` with no `TO`) must never raise. Watch the characterization snapshots for the EcEmitter wrapper (the new BoundOverflowChecking set/reset lines).

---

### Shared infra: generalizing the ambient EC-checking gates (singular-pattern)

`ExceptionEngine`/`ExceptionState` today carry **two** ambient per-statement gate flags — `ArgumentFunctionChecking` (fatal, `ExceptionState.cs:172` + `ArgumentError` `:178`) and `DataConversionChecking` (nonfatal, `:192` + `DataConversionError` `:198`). This slice adds a **fatal** twin (`BoundRefModChecking`/`RefModError`) and a **nonfatal** twin (`BoundOverflowChecking`/`BoundOverflowError`) — the same two shapes.

**Recommended (per `feedback_singular_pattern`, since a 4th flag now appears):** collapse the four into ONE mechanism — a small ambient set `_ambientChecks` keyed by EC-name with `IsChecking(ecName)` / `SetChecking(ecName,bool)`, plus TWO fatality-typed helpers:
- `long/void RaiseFatalIfChecking(string ec, string detail)` (throws `CobolFatalException` when `IsChecking(ec)`) — subsumes `ArgumentError` + `RefModError`.
- `void RecordNonfatalIfChecking(string ec, string detail)` (`Set(ec, fatal:false)` when checking) — subsumes `DataConversionError` + `BoundOverflowError`.

Keep the four **named bool properties** (`ArgumentFunctionChecking`, `DataConversionChecking`, `BoundRefModChecking`, `BoundOverflowChecking`) as thin forwarders over `IsChecking`/`SetChecking` so the **emitted surface** (`EcEmitter` writes `ExceptionState.XxxChecking = true/false`) stays name-stable and readable pre-G8. Correspondingly, generalize the two EcEmitter legs: the fatal leg (`EcEmitter.cs:91-120`) becomes table-driven over `{EC-ARGUMENT-FUNCTION, EC-BOUND-REF-MOD}`, and the nonfatal wrapper (`:64-86`) over `{EC-DATA-CONVERSION, EC-BOUND-OVERFLOW}` — one catch/try shape each, no third/fourth copy.

**Minimal-delta alternative (lower blast radius):** add the two named bools + two helpers as exact mirrors of the existing two, and defer the consolidation. Given four is the proliferation threshold, I recommend the generalization **now**, in the same change set, with the named-property forwarders preserving emit stability.

---

## Wave D — directive quintet (VCR 55/81/59/64/91)

## Wave D — the directive quintet (VCR 55 / 81 / 59 / 64 / 91)

> **Scope:** the five compiler directives `>>COBOL-WORDS`, `>>PUSH`/`>>POP`, `>>DISPLAY`, `>>FLAG-14`, `>>FLAG-02`. All are preprocessor-stage constructs — they never touch the shared ANTLR grammar, so **none of Wave D needs the frozen-legacy `CobolSharp.Compiler` binder fix or the full legacy guard** (the grammar `.g4` files are unchanged). The greenfield battery alone gates Wave D. The one architectural exception is `>>COBOL-WORDS EQUATE`/`SUBSTITUTE`, discussed under AUDIT DRIFT / hazards below — it is textual and still grammar-free.

### Pipeline anchor (shared by all five)
`src/Cobol.Net.Frontend/Pipeline/Frontend.cs:76-117` `Preprocess(...)` is the stage chain: `StripNistArchiveMarkers` → `NormalizeToFreeForm` → **`ConditionalCompilationProcessor.Process(text, leaveTurnDirectives:true, leavePropagateDirectives:true)`** (line 90) → `CopyProcessor` (95) → `NistPreprocessor` (98) → `TurnDirectiveProcessor.Process(text, DialectLevel, diagnostics, sourcePath)` (104) → `PropagateDirectiveProcessor.Process(...)` (111). **`PropagateDirectiveProcessor` (`src/Cobol.Net.Frontend/Preprocessor/PropagateDirectiveProcessor.cs`) is the exact template for every Wave D processor**: signature `(string text, int dialectLevel, DiagnosticBag diagnostics, string sourcePath)`, below-edition emits an introduction diagnostic, recognized lines are **blanked, never deleted** (line-count-preserving — the `>>TURN` "H3 discipline"; `Frontend.cs:105-114` asserts the count is unchanged). `DiagnosticBag` (`src/Cobol.Net.Frontend/Diagnostics/DiagnosticBag.cs:23/28`) exposes `ReportError` and `ReportWarning`.

`ConditionalCompilationProcessor.Process` (`ConditionalCompilationProcessor.cs:126-142`, the `default:` arm) already routes correctly for the new stages: a `>>` directive in an **omitted `>>IF` branch is dropped** (`if (!emitting) output[i]="";`), and one in an emitting branch that is **not** in `KnownIgnoredDirectives` is **left in the text verbatim** (`: line`) to survive to a downstream stage. Today `COBOL-WORDS` is inside `KnownIgnoredDirectives` (`ConditionalCompilationProcessor.cs:37`) so it is silently blanked here; the other four are absent from the set, are left in place, and hit the lexer as `COBOL0001: unexpected '>'`.

**Diagnostic bands** (verified: `grep -rho 'COBOLNET15[0-9][0-9]' src | sort -u` → highest used is **1569**; next free 15xx = **1570**). Introduction gate (reject below the introduction edition) = **`COBOLNET0900`** = `EditionCodes.Introduction` (`src/Cobol.Net.Editions/EditionCodes.cs:16`; descriptor `DiagnosticDescriptors.cs:509-514` is explicitly emittable "from the PARSE layer"). Obsolete-use flag = **`COBOLNET0903`** = `EditionCodes.ObsoleteFlag` (`EditionCodes.cs:29`; precedent emit `ReferenceFormatProcessor.cs:129-135`). Proposed **new** 15xx codes for Wave D (final numbering reconciled at implementation time; sibling waves also draw from 1570↑): **1570** COBOL-WORDS UNDEFINE/SUBSTITUTE static-lexer non-support (§4.2.6-band warning); **1571** `>>POP` unsuccessful (no matching PUSH — the GR2 mandated warning); **1572** FLAG-14 flagged-construct warning; **1573** FLAG-02 flagged-construct warning; **1574** `>>DISPLAY` listing/compile-time-device output (Info). *(Note: `PropagateDirectiveProcessor` chose a bespoke `COBOLNET0883` for its own introduction gate rather than 0900. Wave D should use **`COBOLNET0900`** per the task directive and the DISPLAY/PUSH/POP/FLAG-14 "new-in-2023" symmetry; flag this to the team as a deliberate divergence from the PROPAGATE precedent so the two get reconciled to one policy.)*

---

### VCR 55 — `>>COBOL-WORDS` directive (§7.3.10)

- **Spec sections:**
  - §7.3.10.1 General — line **3994**: "modify which words may and may not be used as reserved words, context-sensitive words, and function names… prohibit the use of specified user-defined names. **This directive is processed during the text manipulation stage of processing.**"
  - §7.3.10.2 General format — lines **3999-4006**: brace of exactly four mutually-exclusive options.
  - §7.3.10.3 Syntax rules — **4011** SR1 ("only before the first IDENTIFICATION DIVISION within a compilation group"; unlimited count), **4013** SR2 (each literal an alphanumeric literal, no hex, no space, case-insensitive), **4015** SR3 (literal-1/3/4 = a reserved / context-sensitive / intrinsic-function word, not a special-character word), **4017** SR4 (literal-2/5/6 = **not** reserved/context-sensitive/intrinsic; a valid user-defined data-name per §8.3.2.2), **4019** SR5 (a given COBOL word appears in at most one COBOL-WORDS literal in the group).
  - §7.3.10.4 General rules — **4024** GR1 (case-insensitive), **4026** GR2 (EQUATE: literal-2 is a synonym for literal-1), **4036** GR3 (UNDEFINE: literal-3 is no longer reserved, usable as any user word; its syntax becomes unavailable), **4038** GR4 (SUBSTITUTE: literal-5 replaces literal-4 in the syntax; literal-4 becomes a user word and is no longer reserved), **4040** GR5 (RESERVE: literal-6 shall not be used as a user-defined word), **4042** GR6 (COBOL-WORDS does not affect compiler-directing statements or directives).
  - Step ordering — line **3342**: "The results of Step 3 are read, and the actions of the COBOL-WORDS directive are applied in order."
- **Syntax / format:** `>>COBOL-WORDS { EQUATE literal-1 WITH literal-2 | UNDEFINE literal-3 | SUBSTITUTE literal-4 BY literal-5 | RESERVE literal-6 }`. Exactly one option, mandatory; `WITH`/`BY` mandatory in their options. Each literal is a quoted alphanumeric literal (SR2).
- **Introduced edition & gate:** **NEW in 2023.** Proof: **Annex E.3.3 item 12** (line **49472**, "COBOL-WORDS directive… may be used to modify the reserved words, context-sensitive words, and function-name lists"), and the in-repo code already tags it 2023 (`ReservedWords.cs:47-49`: "the 2023 COBOL-WORDS directive (ISO Annex E.3.3 item 12)"). Below 2023 → reject with **`COBOLNET0900`** from the new processor. Preprocessor-stage ⇒ **bespoke gate in the processor** (PROPAGATE pattern), **not** a `constructs.json`/`VersionConformancePass` row.
- **Semantics (GR-level):** at the text stage, having read all `>>COBOL-WORDS` directives (they precede the first ID DIVISION):
  - **RESERVE literal-6** (GR5): literal-6 becomes a reserved word for this compilation group ⇒ using it as a user-defined word is an error. Cleanly realized by overlaying a high-confidence entry onto the per-unit `ReservedWordSet` seam.
  - **EQUATE literal-1 WITH literal-2** (GR2): literal-2 (a fresh user word, SR4) becomes a synonym for reserved literal-1. Realizable **textually** at the text-manipulation stage: replace every subsequent whole-word occurrence of literal-2 (case-insensitive) with literal-1.
  - **UNDEFINE literal-3** (GR3): the reserved word literal-3 becomes a user word; its own syntax disappears.
  - **SUBSTITUTE literal-4 BY literal-5** (GR4): literal-5 takes over literal-4's keyword role; literal-4 becomes a user word.
- **As-built today:** `COBOL-WORDS` ∈ `KnownIgnoredDirectives` (`ConditionalCompilationProcessor.cs:37`) ⇒ **recognized-and-silently-blanked, a no-op.** The `ReservedWordSet` seam exists (`src/Cobol.Net.Editions/ReservedWords.cs:51-63`) but has only the `Default` (generated-table) layer; the doc-comment already reserves it for "the COBOL-WORDS pass (roadmap Phase 7)." The reserved-word rejection consumer is bind-time: `VersionConformancePass.cs:1543` `if (_reservedWords.RejectsAt(word, _p._edition.Year) …)` → `EditionCodes.ReservedWord` = **COBOLNET0901**, one per distinct word. **CLI probe (clean):** `>>COBOL-WORDS RESERVE "FOO"` + `01 FOO PIC X(3) VALUE "ABC"` + `DISPLAY FOO` → prints **`ABC`** at **both** `--std 2023` and `--std 2014` (no rejection, no gate) — confirms today's total no-op.
- **AUDIT DRIFT CAUGHT:**
  1. The audit row (VCR 55) lists the option set as *"EQUATE / UNDEFINE / SUBSTITUTE / ACTIVATE-ALL?"* — **wrong**. §7.3.10.2 (lines 4001-4004) has **RESERVE**, not ACTIVATE-ALL; there is no ACTIVATE-ALL option in COBOL-WORDS at all.
  2. **Architectural blocker the audit omits:** `EQUATE` and `SUBSTITUTE` (and full `UNDEFINE`) require the *lexer's* reserved-word table to change per compilation group. Our lexer is a **static generated ANTLR lexer** — reserved words are fixed token types, so a user word cannot be re-tokenized as a keyword (nor vice-versa) at compile time. **RESERVE** is fully implementable (it is a bind-time *rejection*, needs no re-tokenization). **EQUATE** is fully implementable **textually** (the spec explicitly makes COBOL-WORDS a text-manipulation directive, §7.3.10.1 / line 3342 — a whole-word text substitution literal-2→literal-1 is spec-sanctioned and complete). **UNDEFINE** is only *partially* achievable (the `cobolWord` funnel already lets many reserved tokens serve in user-word positions — `VersionConformancePass.cs:1508`; but the token type is unchanged, so it is not general). **SUBSTITUTE** is **not** faithfully achievable on the static lexer (de-reserving literal-4 while reserving literal-5 needs a mutable lexer table; textual literal-5→literal-4 collides literal-4's keyword and user-word uses). This is a genuine "audit says implement, reality says token-table" case — report loudly.
- **Implementation plan:**
  - **New stage** `src/Cobol.Net.Frontend/Preprocessor/CobolWordsDirectiveProcessor.cs`. Runs in `Frontend.Preprocess` **after** `CopyProcessor` (line 95) so `EQUATE`/`SUBSTITUTE` substitution reaches copied library text too, but line-count-preserving for the directive lines themselves. Signature mirrors PropagateDirectiveProcessor; returns the mutated text **and** a built `ReservedWordSet` for the compile.
  - **Remove** `"COBOL-WORDS"` from `ConditionalCompilationProcessor.KnownIgnoredDirectives` (`ConditionalCompilationProcessor.cs:37`) so it survives (emitting) to the new stage; omitted-branch drop still works.
  - Gate: `dialectLevel < 2023` ⇒ `diagnostics.ReportError("COBOLNET0900", …)`, blank the line, skip application.
  - **RESERVE / UNDEFINE:** build a per-unit `ReservedWordSet` overlay. Extend `ReservedWordSet` (`ReservedWords.cs:51-63`) with an instance overlay (`Reserve(word)` adds a synthetic high-confidence entry reserved at the current edition → `RejectsAt` true; `Undefine(word)` masks the default entry). Thread the built set into `VersionConformancePass` in place of `ReservedWordSet.Default` (`VersionConformancePass.cs:361`) — the only wiring the bind-time consumer needs.
  - **EQUATE:** whole-word, case-insensitive text substitution literal-2→literal-1 over the post-COPY text (respecting COBOL word boundaries; skip literals/comments).
  - **SUBSTITUTE / (general) UNDEFINE:** emit a fresh §4.2.6-band Warning code from the plan §0 next-free
    (⛔ the doc's original **`COBOLNET1570`** is STALE — see the top banner) "the COBOL-WORDS {SUBSTITUTE|UNDEFINE} option is not supported by this processor (static reserved-word table) — see docs/CONFORMANCE.md", recognize-and-blank, and add a `docs/CONFORMANCE.md` row (Wave H's file). *(Owner decision candidate: fully support only EQUATE+RESERVE now vs. a mutable-lexer follow-up on the Phase-16 CIL backend.)*
  - Enforce SR2 (alphanumeric literal, no space/hex) and validate literal-1/3/4 are reserved & literal-2/5/6 are not (SR3/SR4) → `COBOLNET0900`-adjacent syntax errors; SR1 placement (before first ID DIV) recognized leniently and **documented not-enforced** (the PROPAGATE SR1 precedent, `PropagateDirectiveProcessor.cs:12-16`).
  - No bound node, no `.g4` change, no `constructs.json` row.
- **Golden:** (EQUATE, positive — clean observable)
  ```cobol
         >>COBOL-WORDS EQUATE "DISPLAY" WITH "SHOW"
         IDENTIFICATION DIVISION.
         PROGRAM-ID. CWEQUATE.
         PROCEDURE DIVISION.
         P.
             SHOW "HI".
             STOP RUN.
  ```
  Derivation: literal-1 `DISPLAY` is reserved (ok, SR3); literal-2 `SHOW` is a fresh user word (SR4). Text stage substitutes `SHOW`→`DISPLAY` ⇒ `DISPLAY "HI"`. **Expected stdout: `HI`.**
  (RESERVE, positive) `>>COBOL-WORDS RESERVE "FOO"` + `01 FOO PIC X …` + `DISPLAY FOO` ⇒ **`COBOLNET0901`** "reserved word 'FOO' cannot be used as a user-defined word" (via the RESERVE overlay + `VersionConformancePass.cs:1543`).
  (below-2023 negative) the EQUATE program at `--std 2014` ⇒ **`COBOLNET0900`**: ">>COBOL-WORDS is a COBOL-2023 directive (ISO §7.3.10, Annex E.3.3 item 12) — it requires --std 2023 or later".
- **Blast radius / hazards:** the `ReservedWordSet`-instance thread-through touches `VersionConformancePass` construction only; watch the reserved-word matrix tests (`tests/version-matrix/reserved-words.json` drift test). EQUATE text substitution must respect word boundaries and skip string literals/comments (a naive replace inside `"…"` would corrupt data) — reuse the tokenizer discipline. Removing `COBOL-WORDS` from `KnownIgnoredDirectives` means any pre-first-ID-DIV misplacement now flows to a real stage — keep it lenient (no new false rejects). Legacy `CobolSharp.Compiler.Compilation.cs:345` calls the *no-flag* `ConditionalCompilationProcessor.Process`, which still blanks nothing for COBOL-WORDS once it leaves `KnownIgnoredDirectives` → the legacy pipeline would now leave `>>COBOL-WORDS` in text and error; **guard**: either keep the legacy path unaffected by leaving COBOL-WORDS handling greenfield-only (add it to a `leaveCobolWordsDirectives` flag like TURN/PROPAGATE, default false) or run the legacy conformance suite. Prefer the **flag** pattern (exact TURN/PROPAGATE precedent, `ConditionalCompilationProcessor.cs:41-44,135-140`) to keep the legacy oracle byte-identical.

---

### VCR 81 — `>>PUSH` / `>>POP` directives (§7.3.22 / §7.3.20)

- **Spec sections:**
  - §7.3.22.1 (PUSH) General — line **4852**: "save the state of a directive so that its status might be restored by a subsequent POP… **processed during the text manipulation stage.**"
  - §7.3.22.2 format — **4857-4862**: `>>PUSH { directive-name | ALL }`.
  - §7.3.22.3 SRs — **4867** SR1 (directive-name is a compiler directive **other than EVALUATE, IF, PAGE, POP, or PUSH**), **4869** SR2, **4871** SR3 (ALL only in a compilation unit, between clauses/statements), **4873** SR4 (**not** within an exception-checking PERFORM).
  - §7.3.22.4 GRs — **4878** GR1 (directive-name: state saved), **4880** GR2 (ALL: all directives' states except EVALUATE/IF/PAGE/POP/PUSH saved), **4882** GR3 (effects remain active; multi-instance directives like DEFINE all pushed).
  - §7.3.20.1 (POP) General — line **4763**; format **4768-4772** `>>POP { directive-name | ALL }` (ALL underlined default).
  - §7.3.20.3 SRs — **4777** SR1 (same directive exclusions), **4783** SR4 (not within exception-checking PERFORM).
  - §7.3.20.4 GRs — **4788** GR1 (restore the matching pushed state; all instances of a multi-instance directive restored), **4790** GR2 (if there was no matching successful PUSH, **the POP is unsuccessful and the implementor shall provide a warning mechanism**), **4792** GR3 (ALL restores all pushed-not-popped states).
- **Syntax / format:** `>>PUSH { directive-name | ALL }` and `>>POP { directive-name | ALL }`. One operand, mandatory; `ALL` is the underlined default for POP.
- **Introduced edition & gate:** **NEW in 2023.** Proof: **Annex E.3.3 item 38** (line **49517**, "PUSH and POP directives are added to allow saving and restoration of the state of compiler directives"). Below 2023 → `COBOLNET0900`, bespoke in the processor.
- **Semantics (GR-level):** PUSH snapshots the current state of the named directive (or, for ALL, every directive except the five excluded control-flow directives) onto a per-directive-name **stack**; the pushed directive's effect stays live (GR3). POP pops the matching snapshot and restores it; for multi-instance directives (DEFINE has many compilation variables) **all** instances are saved/restored as a unit. An unmatched POP is *unsuccessful* → a mandated warning (GR2), and leaves state unchanged.
- **As-built today:** neither in `KnownIgnoredDirectives`; both reach the lexer. **CLI probe:** `>>PUSH ALL` / `>>POP ALL` → `COBOL0001: unexpected '>'` (confirmed). No directive-state stack exists.
- **AUDIT DRIFT CAUGHT:** none for the citation itself (audit VCR 81 cites "§7.3 (E.3.3 item 38)" — E.3.3 item 38 is exactly PUSH/POP; the precise sub-sections are §7.3.22 PUSH and §7.3.20 POP). But the audit's one-line "directive-state snapshot/restore stack" understates that a **faithful PUSH ALL must snapshot every directive's state**, which our compiler spreads across independent passes (DEFINE lives in `ConditionalCompilationProcessor`, TURN in `TurnDirectiveProcessor`, etc.). A truly complete implementation needs a shared directive-state registry; note it.
- **Implementation plan:**
  - Because the highest-value and most common PUSH/POP target is the **DEFINE compilation-variable table** (which already lives in `ConditionalCompilationProcessor.defines`), implement PUSH/POP **inside `ConditionalCompilationProcessor`** as two new `case` arms, snapshotting/restoring a `Stack<Dictionary<string,Value>>` clone of `defines`. This is the complete-and-correct behavior for the DEFINE state and produces an observable golden.
  - For `PUSH ALL` / `POP ALL` of the *other* directive states (TURN, PROPAGATE, FLAG-14/02, LISTING, SOURCE FORMAT…): the decision-complete design is a `DirectiveStateStack` with per-directive save/restore hooks registered by each processor; the **pragmatic first cut** snapshots the states owned by the conditional-compilation stage (DEFINE + the current source-format flag) and recognizes/edition-gates the rest. Document the staged remainder (mirrors how PROPAGATE recognizes-but-defers its runtime effect).
  - Edition gate: below 2023 ⇒ `COBOLNET0900` (needs `dialectLevel` — pass it into `ConditionalCompilationProcessor.Process`, currently `dialectLevel`-unaware; add the parameter and a `DiagnosticBag`, threaded from `Frontend.cs:90`; keep the legacy caller `Compilation.cs:345` on an overload that no-ops the gate/PUSH-POP for byte-identity).
  - Unmatched POP ⇒ `diagnostics.ReportWarning("<PUSHPOP-GR2>", …)` (GR2 mandated warning; ⛔ the doc's original
    "COBOLNET1571" is STALE — allocate from the plan §0 next-free, see the top banner). SR4 (not within an exception-checking PERFORM) recognized-leniently, documented not-enforced.
  - No `.g4`, no bound node, no `constructs.json`.
- **Golden:** (PUSH/POP of DEFINE state — proves restore)
  ```cobol
         >>DEFINE PHASE AS 1
         >>PUSH ALL
         >>DEFINE PHASE AS 2 OVERRIDE
         >>POP ALL
         IDENTIFICATION DIVISION.
         PROGRAM-ID. PUSHPOP.
         PROCEDURE DIVISION.
         P.
         >>IF PHASE = 1
             DISPLAY "RESTORED".
         >>ELSE
             DISPLAY "NOT-RESTORED".
         >>END-IF
             STOP RUN.
  ```
  Derivation: DEFINE PHASE=1; PUSH ALL snapshots {PHASE=1}; DEFINE PHASE=2 OVERRIDE; POP ALL restores {PHASE=1}; the `>>IF PHASE = 1` branch is taken. **Expected stdout: `RESTORED`.** (Without correct PUSH/POP the value would remain 2 → `NOT-RESTORED`, so the golden is discriminating.)
  (below-2023 negative) same program at `--std 2014` ⇒ **`COBOLNET0900`** on the `>>PUSH` line: ">>PUSH/>>POP are COBOL-2023 directives (ISO §7.3.22/§7.3.20, Annex E.3.3 item 38) — they require --std 2023 or later".
- **Blast radius / hazards:** adding `dialectLevel`+`DiagnosticBag` params to `ConditionalCompilationProcessor.Process` changes a signature shared with legacy `Compilation.cs:345` — use an overload (default no-gate) so the legacy oracle is untouched; run the legacy conformance suite regardless. The DEFINE-snapshot clone must be a deep copy (the `Value` record is immutable, so a `new Dictionary(...)` copy suffices). Watch the existing conditional-compilation tests for regressions from the two new `case` arms.

---

### VCR 59 — `>>DISPLAY` directive (§7.3.12)

- **Spec sections:**
  - §7.3.12.1 General — line **4135**: "transfers data to the source listing or an implementor defined compile-time-device. **The implementor defines the stage of processing for this directive.**"
  - §7.3.12.2 format — **4140-4154**: `>>DISPLAY { arithmetic-expression-1 | boolean-expression-1 | literal-1 | PARAMETER compilation-variable-name-1 } … [ UPON { compile-time-device-1 … | LISTING } ]`.
  - §7.3.12.3 SRs — **4159** SR1 ("shall begin on a new line and shall be specified entirely on that line"), SR2/SR3 (arith/boolean per §7.3.6/§7.3.7).
  - §7.3.12.4 GRs — **4168** GR1 (each operand transferred to the listing/device in order; conversion implementor-defined), **4170** GR2 (if no listing, result implementor-defined; otherwise the transfer happens even if LISTING suppressed), **4172** GR3 (PARAMETER pulled from the operating environment, implementor-defined; no value ⇒ no transfer), **4174** GR4 (multiple operands transferred in order), **4176/4189-4193** GR5/GR6 (UPON LISTING = the listing device; UPON compile-time-device = an implementor device; default is `UPON LISTING`).
- **Syntax / format:** `>>DISPLAY operand-1 [ operand-2 … ] [ UPON { device … | LISTING } ]`; at least one operand; operands are literals, compile-time arithmetic/boolean expressions, or `PARAMETER var`.
- **Introduced edition & gate:** **NEW in 2023.** Proof: **Annex E.3.3 item 16** (line **49476**, "The DISPLAY directive allows the display of compile-time information during the compilation of COBOL source"). Below 2023 → `COBOLNET0900`.
- **Semantics (GR-level):** a compile-time output — each operand's value is written, in order, to the source listing (our compile-log/diagnostic channel). It does not affect the compiled program's runtime behavior at all. `PARAMETER` operands are pulled from the environment (implementor-defined; if absent, that operand is skipped). Default target = LISTING.
- **As-built today:** not in `KnownIgnoredDirectives`; reaches the lexer. **CLI probe (clean):** an emitting-branch `>>DISPLAY "compile-time-msg"` → `waved_di3.cob(6,8): error COBOL0001: unexpected '>'` (confirmed). No compile-log/listing channel is wired.
- **AUDIT DRIFT CAUGHT:** none for recognition. Minor: the audit says "emit a compile-log/warning-channel line" — spec-precisely this is the **listing / compile-time-device**, an *informational* transfer, not a warning; route it as an **Info** diagnostic (proposed `COBOLNET1574`) or a listing note, not `ReportWarning`, so it does not inflate the warning count or fail `-Werror`-style gates. (Also note E.3.3 item 16 makes `>>DISPLAY` **new in 2023** in this standard's lineage — do not assume the historical 2002 `>>DISPLAY`; trust the annex and gate at 2023.)
- **Implementation plan:**
  - **New stage** `DisplayDirectiveProcessor.cs`, run in `Frontend.Preprocess` after `ConditionalCompilationProcessor` (so `>>IF`-gated `>>DISPLAY`s are already dropped/kept) — placement alongside PROPAGATE (line 111 vicinity); line-count-preserving blank.
  - Parse operands: quoted literals emitted verbatim; `PARAMETER var` resolved from `Environment.GetEnvironmentVariable` (implementor-defined per GR3, absent ⇒ skip); compile-time arith/boolean expression evaluation may reuse `ConditionalCompilationProcessor`'s value/tokenizer machinery (or be staged with a documented note — the common real-world use is a bare literal/PARAMETER). `UPON LISTING`/device recognized; both map to our single compile-log channel.
  - Emit via `diagnostics.Report(code:"COBOLNET1574", DiagnosticSeverity.Info, message: <concatenated operands>, …)` so a golden can capture it on the compile channel.
  - Gate below 2023 ⇒ `COBOLNET0900`. No `.g4`, no bound node, no `constructs.json`.
- **Golden:**
  ```cobol
         IDENTIFICATION DIVISION.
         PROGRAM-ID. DISPDIR.
         PROCEDURE DIVISION.
         P.
         >>DISPLAY "build-tag=" "R1"
             DISPLAY "RAN".
             STOP RUN.
  ```
  Derivation: `>>DISPLAY` transfers `build-tag=` then `R1` to the listing channel at compile time (GR4 order); the program's runtime stdout is only the ordinary `DISPLAY "RAN"`. **Expected program stdout: `RAN`.** **Expected compile channel:** one `COBOLNET1574` Info line whose text contains `build-tag=R1` (test asserts on the diagnostic/listing capture, not stdout).
  (below-2023 negative) same at `--std 2014` ⇒ **`COBOLNET0900`**: ">>DISPLAY is a COBOL-2023 compile-time directive (ISO §7.3.12, Annex E.3.3 item 16) — it requires --std 2023 or later".
- **Blast radius / hazards:** minimal — a new isolated stage. Ensure the Info diagnostic does not count toward error/warning thresholds that would fail the compile; verify the compile still returns success (exit 0). Confirm `>>DISPLAY` inside an omitted `>>IF` branch is dropped (it is, by `ConditionalCompilationProcessor.cs:134`) and produces **no** listing line — add that as a second golden branch.

---

### VCR 64 — `>>FLAG-14` directive (§7.3.15)

- **Spec sections:**
  - §7.3.15.1 General — line **4444**: "flag certain syntax for which the behavior might be incompatible between the previous COBOL standard and this Working Draft International Standard. **Processed during the compilation stage.**"
  - §7.3.15.2 format — **4449-4474**: `>>FLAG-14 { ALL | COMPILE-TIME-ARITHMETIC-EXPRESSIONS | EVALUATE | I-O-DECLARATIVE | I-O-STATUS-04 | I-O-STATUS-07 | NUM-ED-ZERO-FIGCONST | READ-PREVIOUS | REF-MOD-ZERO-LENGTH | VALUE-EDITING | VALUE-FIG-CON-LENGTH | VALUE-ZERO | WRITE-END-OF-PAGE } { ON | OFF }`.
  - §7.3.15.3 SR1 — **4479** (only between clauses outside the procedure division, and between statements in the procedure division).
  - §7.3.15.4 GRs — **4484** GR1 (implementor provides the warning mechanism; the incompatibility list is E.2), **4488** GR2 (ON enables flagging for the option until end-of-group / an OFF for it / an OFF-all), **4503** GR3 (OFF disables), **4505-4542** GR4 (per-option meaning — the 13 options), **4544** GR5 (default all OFF).
- **Syntax / format:** `>>FLAG-14 option { ON | OFF }` — one option keyword (or `ALL`) then `ON`/`OFF`; positional, both mandatory.
- **Introduced edition & gate:** **NEW in 2023.** Proof: **Annex E.3.3 item 21** (line **49481**, "A compiler directive, FLAG-14, has been added…"), and it flags the 2014↔2023 incompatibilities (so it can only exist in the 2023 edition). Below 2023 → `COBOLNET0900`.
- **Semantics (GR-level):** a **positional** diagnostic sink — from a `FLAG-14 opt ON` line until a matching `OFF`/`OFF ALL`/end-of-group, the compiler must warn on each occurrence of the option's flagged construct (GR4 enumerates the 13, e.g. `EVALUATE` = "a directive containing a WHEN phrase and a WHEN OTHER phrase"; `NUM-ED-ZERO-FIGCONST`/`VALUE-ZERO` = figurative ZERO in a numeric-edited VALUE; `WRITE-END-OF-PAGE` = a WRITE that permits but omits END-OF-PAGE; `REF-MOD-ZERO-LENGTH` = ref-mod when EC-BOUND-REF-MOD is on and REF-MOD-ZERO-LENGTH is unset). These are the **GR4 "twins"** of the Wave-B/Wave-G behavior rows (VCR 102-113) — FLAG-14 is the directive front-end that turns each behavior row's flagging on.
- **As-built today:** not in `KnownIgnoredDirectives`; reaches the lexer. **CLI probe:** `>>FLAG-14 ALL ON` → `COBOL0001: unexpected '>'` (confirmed). No flag-state machine, no GR4 wiring (`grep Flag14/FlagDirective/CompatFlag` in `src` = 0 matches).
- **AUDIT DRIFT CAUGHT:** the audit's option nickname "NUM-ED-ZERO-FIG-CONST" / "VALUE-FIG-CON-LENGTH" match the spec's slightly-garbled boxed names (`NUM-ED-ZERO-FIGCONST` in the figure line 4462 vs. `NUM-ED-ZERO-FIG-CONSTANT` in GR4b line 4519; `VALUE-FIG-CON-LENGTH` in the figure vs. `VALUE-FIG-CON-NO-LENTH` [sic] in GR4k line 4531). The **figure spellings** (`NUM-ED-ZERO-FIGCONST`, `VALUE-FIG-CON-LENGTH`, `VALUE – EDITING`) are the syntactic keywords to accept; the GR4 prose spellings are typos in the standard. Accept the figure spellings; note the discrepancy. Otherwise the citation (§7.3.15, E.3.3 item 21) checks out.
- **Implementation plan:**
  - **Complete design** (mirrors `TurnDirectiveProcessor`'s `TurnEvents`): a `Flag14DirectiveProcessor` that, instead of consuming positionally in the preprocessor, **collects `(line, option, on|off)` events** on the final post-COPY text (like `TurnEvents`, `Frontend.cs:104`), threaded to the binder; a `Flag14State` the binder consults at each flag-eligible construct site to emit `<FLAG14-TWIN>` warnings (⛔ the doc's original "COBOLNET1572" is STALE — allocate from the plan §0 next-free). Wire the options whose constructs are already implemented first (`EVALUATE`, `VALUE-ZERO`/`NUM-ED-ZERO-FIGCONST` — the Wave-B/G VALUE-numeric-edited path; `WRITE-END-OF-PAGE` — `SequentialIoBinder.cs:98`; `REF-MOD-ZERO-LENGTH` — the EC-BOUND-REF-MOD gate). Options whose constructs are not yet implemented (`READ-PREVIOUS`) are recognized/validated and their flagging staged with a documented note.
  - **Pragmatic first cut for Wave D** (keeps Wave D preprocessor-only): recognize + syntax-validate the option/ON-OFF, edition-gate below 2023 (`COBOLNET0900`), maintain the flag-state, and emit the `<FLAG14-TWIN>` twin for the already-landed constructs; blank the directive line (line-count preserving). Deeper per-construct wiring co-lands with Wave G's behavior rows.
  - Gate + state need `dialectLevel` + `DiagnosticBag`; new stage in `Frontend.Preprocess`. No `.g4`, no bound node.
- **Golden:** (EVALUATE flag — a construct that definitely parses)
  ```cobol
         IDENTIFICATION DIVISION.
         PROGRAM-ID. FLAG14.
         DATA DIVISION.
         WORKING-STORAGE SECTION.
         01 N PIC 9 VALUE 1.
         PROCEDURE DIVISION.
         P.
         >>FLAG-14 EVALUATE ON
             EVALUATE N
                 WHEN 1 DISPLAY "ONE"
                 WHEN OTHER DISPLAY "OTHER"
             END-EVALUATE
             STOP RUN.
  ```
  Derivation: the EVALUATE has both a WHEN and a WHEN OTHER → GR4c flags it. **Expected program stdout: `ONE`.** **Expected compile channel:** one `COBOLNET1572` warning "FLAG-14 EVALUATE: an EVALUATE with a WHEN and a WHEN OTHER phrase may be incompatible (ISO §7.3.15 GR4c)". (A control golden with `>>FLAG-14 EVALUATE OFF` emits no warning.)
  (below-2023 negative) same at `--std 2014` ⇒ **`COBOLNET0900`**: ">>FLAG-14 is a COBOL-2023 directive (ISO §7.3.15, Annex E.3.3 item 21) — it requires --std 2023 or later".
- **Blast radius / hazards:** the flag-state→binder thread is the largest Wave D surface; keep the first cut to the preprocessor + already-implemented twins so no binder regression. The 13 option keywords are **context-sensitive words inside the directive only** — they must not become reserved (they are matched textually in the preprocessor, never reaching the lexer as tokens), so there is zero reserved-word blast radius. Watch that a bare `>>FLAG-14 … OFF` inside an omitted `>>IF` branch is dropped.

---

### VCR 91 — `>>FLAG-02` directive (§7.3.14) — OBSOLETE in 2023

- **Spec sections:**
  - §7.3.14.1 General — line **4364**: "flag certain syntax… incompatible between ISO 1989:2002 and ISO/IEC 1989:2014. **Processed during the compilation stage.**" **line 4366 NOTE: "The FLAG-02 directive is an obsolete element in this Working Draft International Standard and is to be deleted from the next edition."**
  - §7.3.14.2 format — **4371-4381**: `>>FLAG-02 { ALL | EC-PROGRAM-EXCEPTIONS | I-O-STATUS-07 | MOVE-TO-SAME-NAME | RANGE-EXCEPTION-FOR-INDEX | TERMINATE-WITH-VARYING } { ON | OFF }`.
  - §7.3.14.3 SR1 — **4386**; §7.3.14.4 GRs — **4391** GR1 (warning mechanism for 2002↔2014 incompatibilities), **4393** GR2 (ON scope), **4395** GR3 (OFF), **4397-4428** GR4 (the 5 options' meanings), **4430** GR5 (default OFF).
  - **Annex F.2 item 1** — line **50395**: "**FLAG-02 directive.** …There is no longer a need for the older FLAG-02 directive." (the obsolete designation; F.2 preamble line **50380**: "A conforming implementation **shall support** obsolete language elements except… optional or processor-dependent" — so it stays functional). §4.2.13 (line 2505) defines the obsolete class.
- **Syntax / format:** `>>FLAG-02 option { ON | OFF }`, 5 options + `ALL`; positional, both mandatory.
- **Introduced edition & obsolete gate:** **introduced in 2014** (it flags the 2002↔2014 delta, the sibling of FLAG-85's 85↔2002 role; the 2023 text cannot pin the exact 2002-vs-2014 edge, so this is the same *provisional-2014* determination as PROPAGATE's provisional-2002 — refine against the 1989:2014 standard when available). Edition behavior: **below 2014** → introduction gate `COBOLNET0900`; **at 2014** → functional, no obsolete warning; **at 2023** → functional **and** emit the obsolete-use warning **`COBOLNET0903`** (`EditionCodes.ObsoleteFlag`) once. This is the exact three-way pattern of `ReferenceFormatProcessor`'s col-7 continuation (`ReferenceFormatProcessor.cs:126-135`).
- **Semantics (GR-level):** identical *mechanism* to FLAG-14 but a different (2002↔2014) incompatibility set and only 5 options (GR4a-f, lines 4399-4428: `EC-PROGRAM-EXCEPTIONS`, `I-O-STATUS-07` [CLOSE with WITH NO REWIND or UNIT], `MOVE-TO-SAME-NAME`, `RANGE-EXCEPTION-FOR-INDEX`, `TERMINATE-WITH-VARYING`). ON..OFF positional scope (GR2).
- **As-built today:** not in `KnownIgnoredDirectives`; reaches the lexer. **CLI probe:** `>>FLAG-02 ALL ON` → `COBOL0001: unexpected '>'` (confirmed). No `COBOLNET0903` obsolete path for it.
- **AUDIT DRIFT CAUGHT:** **the audit VCR 91 cite "§7.3.14 (§F.2 item 1)" is CORRECT** — I initially mis-hit a spurious duplicate "### F.2" header inside Annex E's Unicode dump (~line 49527, actually accessibility/Cherokee/physical-file text); the **authoritative** Annex F.2 is at line **50374**, and its **item 1 (line 50395) is exactly the FLAG-02 directive** obsolete designation. Additionally, FLAG-02's obsolescence is *doubly* attested — the §7.3.14.1 NOTE (line 4366) is the strongest inline citation, so cite **both** §7.3.14.1 NOTE **and** §F.2 item 1. No drift; audit verified.
- **Implementation plan:**
  - Fold into the same `FlagDirectiveProcessor` as FLAG-14 (one processor, two directive names — the `feedback_singular_pattern` "one mechanism" rule): shared option/ON-OFF parse + flag-state; FLAG-02 carries the 5-option set and the obsolete-warning behavior.
  - **at 2023:** on first `>>FLAG-02` occurrence, `diagnostics.ReportWarning("COBOLNET0903", "the FLAG-02 directive is obsolete as of COBOL-2023 (ISO §7.3.14.1 NOTE; Annex F.2 item 1) — use FLAG-14", loc)` (dedup once per file, `ReferenceFormatProcessor` `_col7Flagged` pattern). Still recognized/validated/blanked (F.2 preamble: obsolete ⇒ shall still support). Flagging of the 5 options is best-effort/staged with FLAG-14's twins (`I-O-STATUS-07` shares the Wave-G behavior row).
  - **below 2014:** `COBOLNET0900` introduction gate. **at 2014:** functional, silent.
  - No `.g4`, no bound node. (If the team prefers a `constructs.json` "obsolete row" for VERSION_TEST_MATRIX visibility, note it — but the emission is preprocessor-bespoke like the col-7 obsolete case, not a `VersionConformancePass` row.)
- **Golden:**
  ```cobol
         IDENTIFICATION DIVISION.
         PROGRAM-ID. FLAG02.
         PROCEDURE DIVISION.
         P.
         >>FLAG-02 I-O-STATUS-07 ON
             DISPLAY "RAN".
             STOP RUN.
  ```
  Derivation (at `--std 2023`): the directive is recognized (no `COBOL0001`); it is obsolete → one `COBOLNET0903` warning; program runs. **Expected stdout: `RAN`.** **Expected compile channel:** one `COBOLNET0903` "FLAG-02 … is obsolete as of COBOL-2023 …".
  (at `--std 2014`) same program ⇒ **no** `COBOLNET0903`, functional, stdout `RAN`.
  (below-2014 negative, `--std 2002`) ⇒ **`COBOLNET0900`**: ">>FLAG-02 is a COBOL-2014 directive (ISO §7.3.14) — it requires --std 2014 or later".
- **Blast radius / hazards:** the `COBOLNET0903` obsolete-warning severity should route through the same `EditionSeverityPolicy`/`ObsoleteFlag` path the col-7 continuation uses so `--permissive` and severity are consistent. Keep FLAG-02/FLAG-14 in one processor to avoid two divergent flag-state machines. Confirm the obsolete warning does not fire below 2023 and the introduction gate does not fire at/above 2014.

---

### Wave D cross-cutting summary (for the implementer)
- **One shared new preprocessor pass file is cleanest**: a `DirectiveRecognitionProcessor` (or the three files `CobolWordsDirectiveProcessor`, `PushPopHandledInsideConditionalComp`, `DisplayDirectiveProcessor`, `FlagDirectiveProcessor`) added to `Frontend.Preprocess` after `ConditionalCompilationProcessor` and mostly after `CopyProcessor`. Each is the `PropagateDirectiveProcessor` template (`(text, dialectLevel, DiagnosticBag, sourcePath)` → line-count-preserving blank + edition gate).
- **Legacy safety:** the shared `ConditionalCompilationProcessor.Process` is called by legacy `Compilation.cs:345` **without** the greenfield flags. To keep the legacy oracle byte-identical: (a) do **not** change the legacy-visible `KnownIgnoredDirectives` behavior except behind a `leaveCobolWordsDirectives`-style flag (exact TURN/PROPAGATE precedent, `ConditionalCompilationProcessor.cs:41-44`); (b) add any new `dialectLevel`/`DiagnosticBag` params via overloads. **No `.g4` change ⇒ no full legacy-guard mandate**, but still run the legacy conformance suite once because `ConditionalCompilationProcessor` is shared code.
- **Diagnostic codes to reserve:** `COBOLNET0900` (all four new-2023 introduction gates), `COBOLNET0903` (FLAG-02 obsolete), and new `COBOLNET1570` (COBOL-WORDS UNDEFINE/SUBSTITUTE non-support), `1571` (POP-unsuccessful), `1572` (FLAG-14 flag), `1573` (FLAG-02 flag), `1574` (DISPLAY listing/Info) — reconcile final 15xx numbers with sibling waves (all draw from 1570↑).
- **Biggest genuine finding:** `>>COBOL-WORDS SUBSTITUTE` (and general `UNDEFINE`) cannot be faithfully implemented on the static ANTLR lexer; `EQUATE` (textual) and `RESERVE` (ReservedWordSet overlay) can. Recommend implementing EQUATE+RESERVE completely + a named `COBOLNET1570` non-support warning + `docs/CONFORMANCE.md` row for the token-remapping options — this is an owner-visible disposition, not a silent scope cut.

---

## Wave E — EXTERNAL conformance cluster + EC-EXTERNAL-* (VCR 15/16/18/31/63)

## Wave E — EXTERNAL conformance cluster + EC-EXTERNAL-* (VCR 15 / 16 / 18 / 31 / 63)

**Scope note / section-number correction up front (applies to every item below):** the audit's worklist rows 62–65 and 85 repeatedly cite **"ISO §13.18.27"** for the EXTERNAL clause. **That is wrong.** `§13.18.27` is the **GLOBAL clause** (spec line 19007). The EXTERNAL clause is **§13.18.22** (spec lines 18671–18731). The run-unit conformance rules live in **§14.8.4** (lines 25513–25539), the raise points in the CALL/INVOKE GRs (26189–26195 / 28533–28538), the fatality table is **Table in §14.6.13.1.6** (lines 24670–24674), and the introduction proofs are **Annex E.3 items 9/10/12/24** (lines 49138 / 49148 / 49164 / 49312) plus the "main changes" list (lines 1203, 1226). Every §13.18.27 in the Wave-E audit rows should read §13.18.22.

---

### VCR 63 — EXTERNAL data items / type declarations may be strongly typed (2023 introduction gate)

- **Spec sections:**
  - §13.18.22.3 SR1 (line 18687): the EXTERNAL clause may be specified in FD entries, level-1 WS data description entries, **and in level-1 type declarations**.
  - §13.18.22.3 SR5 (line 18695): "When a record description is an external item, any associated type declaration that is strongly typed shall also be external." (with the NOTE at 18697).
  - §13.18.22.4 GR2/GR3 (lines 18706/18708): a data description containing an external type shall be level-1 (GR2); records containing an external type are themselves external (GR3).
  - §8.5.3.1 Types: type-equivalence rules now key on "the same presence or absence of the EXTERNAL clause and the STRONG phrase."
  - **Introduction proof:** main changes list line 1226 — "Type declarations may now be external items"; Annex **E.3 item 10** (line 49148) — "where previously external items **could not be strongly typed**."
- **Syntax / format:** `IS EXTERNAL [ AS literal-1 ]` (§13.18.22.2, line 18682) appearing on a level-1 `TYPEDEF STRONG` entry, or on a level-1 record described `TYPE type-name` where that type is `STRONG` and `EXTERNAL`. ⛔ **CORRECTION (2026-07-19, empirical — the gated identity below dropped STRONG and was too broad):** the gate must be the co-occurrence of the EXTERNAL clause with a **STRONG** TYPEDEF clause — NOT any TYPEDEF. E.3 item 10 dates the 2023 change precisely to strong-typing ("external items **could not be strongly typed**" before 2023); a **WEAK** `01 T TYPEDEF IS EXTERNAL` (no STRONG) was already valid in **COBOL-2002** (§13.18.58.3 SR3) and is the existing passing golden `tests/conformance/2002/typedef_external` (P10 Step 16). The first (STRONG-less) implementation of this gate regressed that 2002 golden with a false COBOLNET0900 — the full-Conformance gate caught it (DEVLOG 904). Because SR5 forces a strongly-typed external record's type to be a strong external type declaration, gating the STRONG DECLARATION covers both faces (the declaration AND the strongly-typed external item that must reference it).
- **Introduced edition & gate:** COBOL-2023 (E.3 item 10). Below 2023 the whole combination must be **rejected loud** with **COBOLNET0900** (the introduction band). Gate mechanism = a new `constructs.json` row `external-type-declaration-2023` (`introducedIn: 2023`, `expectDiagnostic: COBOLNET0900`) + a **parse-arm** recognition override in `VersionConformancePass.ParseArm`.
- **Semantics (GR-level):** at ≥2023 the external type declaration is fully legal; its references land external per GR2/GR3 (already implemented — DataBinder ExpandType). Below 2023 the construct does not exist; the compile fails at the introduction gate before any conformance check.
- **As-built today (CONFIRMED by CLI probe):** the data-model plumbing landed in P10 Step 16 — `DataBinder.cs:1920` sets `IsExternalTypedef = isTypedef && hasExternal`; `:1921` sets `HasExternalClause`; ExpandType `:1033–1040` enforces GR2/GR3 (COBOLNET1558); `:1044–1047` enforces SR5 strong-external pairing (COBOLNET1558). **But there is NO introduction gate.** Probe `waveE_strextern.cob` (a `01 T IS EXTERNAL TYPEDEF STRONG.` + `01 R TYPE T IS EXTERNAL.`) compiled and printed `ABCD` under **BOTH `--std 2023` AND `--std 2014`** — the below-edition acceptance is a live bug.
- **AUDIT DRIFT CAUGHT:**
  1. Section number: audit row 85 cites "§13.18.22 (SR1/SR5, **E.3.3 item 20**)". The introduction proof is **E.3 item 10** (line 49148), not "E.3.3 item 20"; the SR list (§13.18.22.3) has SR1 and SR5 as the relevant rules (correct), but the annex pointer is wrong.
  2. Audit row 85 says ":1036-1037 enforces §13.18.22 **SR5** strong-external pairing." The code at DataBinder `:1036–1037` is the **GR2 level-1** leg; the **SR5** strong-external pairing is at `:1044–1047`. Both emit COBOLNET1558 (ExternalTypeRule bundles GR2/GR3/SR5), so the code identity is right but the SR↔line mapping is transposed.
- **Implementation plan:**
  - **Grammar:** none (`externalClause` and `typedefClause` already exist — `CobolData.g4:265`/`:318`; both are `dataDescriptionClause` alternatives, `:249`/`:252`). ADDITIVE-free.
  - **VersionConformancePass ParseArm:** add `VisitExternalClause` (or, cleaner, `VisitDataDescriptionEntry`) that fires `_p.Check(Constructs.ExternalTypeDeclaration2023, "an EXTERNAL type declaration")` when the enclosing `dataDescriptionEntry` carries **both** an `externalClause` and a `typedefClause`. Recognition-based (drop-proof; the DEVLOG-724 doctrine — `IsExternalTypedef` is a resolved DataItem attribute that is discarded when the typedef fails to register, so a bound-arm gate would lose the 0900). Guard with the existing `InGatedDataEntry` helper.
  - **constructs.json:** new row (id `external-type-declaration-2023`, display "an EXTERNAL type declaration", `diagnosticCode`/`expectDiagnostic` COBOLNET0900, `introducedIn` 2023, `citation` "ISO §13.18.22.3 SR1/SR5; Annex E.3 item 10", `vcr` "VCR row 63"). Regenerate `Constructs.g.cs` + `ConstructRegistry.g.cs` via `scripts/gen-constructs.ps1` (ConstructRegistryDriftTests asserts equality).
  - **Diag code:** COBOLNET0900 (existing introduction band) — **no new 15xx needed.**
- **Golden (positive, ≥2023):**
  ```cobol
  IDENTIFICATION DIVISION.
  PROGRAM-ID. WAVEE-STREXT.
  DATA DIVISION.
  WORKING-STORAGE SECTION.
  01 T IS EXTERNAL TYPEDEF STRONG.
     05 A PIC X(4).
  01 R TYPE T IS EXTERNAL.
  PROCEDURE DIVISION.
  MAIN.
      MOVE "ABCD" TO A OF R.
      DISPLAY A OF R.
      STOP RUN.
  ```
  Expected stdout at `--std 2023` (hand-derived: MOVE of the 4-char literal into the external record's `A`, then DISPLAY): `ABCD`. **Below-2023 negative** (same source, `--std 2014`): compile FAILS with `error COBOLNET0900: … an EXTERNAL type declaration … requires --std 2023 or later (targeting COBOL-2014)` and no stdout. (Today it wrongly prints `ABCD` at 2014.)
- **Blast radius / hazards:** the external-data + typedef test suites; any NIST/characterization program that legitimately uses an external strong type must run at `--std 2023` (the default). Verify no `--std 2014/2002/85` fixture in the battery declares an external type declaration (would newly, correctly, fail). Watch `ExternalDataTests` / `TypedefTests`.

---

### VCR 16 — CONSTANT RECORD permitted only for strongly-typed EXTERNAL items (2014→2023 behavior change, verify+flip)

- **Spec sections:**
  - §13.16.3 SR13 ¶2 (line 17253): "If the CONSTANT RECORD clause is specified with the EXTERNAL clause, there shall also be a TYPE clause that specifies a strongly typed definition."
  - §14.8.4.3 (line 25535): "For external data items with strongly typed record descriptions, the record descriptions shall have the same corresponding external strong type declarations and **the same presence or absence of the CONSTANT RECORD clause**."
  - **Behavior-change proof:** Annex **E.3 item 10** (line 49148): "The CONSTANT RECORD clause **may now only** be specified for external items that are strongly typed, where **previously external items could not be strongly typed**. Previously external items could be specified with the CONSTANT RECORD clause with inadequate conformance checking."
  - CONSTANT RECORD itself is `constant-record-2002` (constructs.json:551) — valid from COBOL-2002.
- **Syntax / format:** `01 record-name IS EXTERNAL CONSTANT RECORD.` — at ≥2023 this additionally requires `TYPE strong-type-name`; below 2023 the bare external CONSTANT RECORD (no TYPE) is the legacy accepted form.
- **Introduced edition & gate:** this is a **2014→2023 tightening**, not an introduction. The mechanism is a **dialect-conditioned structural SR in the binder** (like `CheckDigitCapacity`, which reads `Edition.MaxDigits`): the "requires a strongly-typed TYPE" leg fires **only when `Edition.DialectLevel >= 2023`**. Below 2023 the leg is suppressed and the entry binds as an ordinary (legacy) external constant record. **No `ConstructRegistry.Check` call** — the binder legitimately reads the edition year for a version-conditioned SR (this is not an introduction/removal gate).
- **Semantics (GR-level):** at ≥2023, `EXTERNAL CONSTANT RECORD` without a strong TYPE ⇒ COBOLNET1549 (unchanged). Below 2023, the same entry is accepted; its content initializes per §13.18.15.4 GR1 (as-if INITIALIZE) and it is not re-initialized on run-unit re-entry (§11.9.10.4 GR7, line 13652 — external items are not re-initialized "except for those with the CONSTANT RECORD clause"). Note the interaction with VCR 63: once the VCR-63 gate lands, a strong external type is impossible below 2023 anyway, so the only below-2023 shape reaching this leg is the non-strong external constant record — which is exactly the legacy form E.3 item 10 says was permitted.
- **As-built today (CONFIRMED by CLI probe):** `DataBinder.cs:1892–1894` fires `ConstantRecordRule` (**COBOLNET1549**) **unconditionally** for `isConstantRecord && hasExternal && typeRefName is null`. Probe `waveE_crext.cob` (`01 R IS EXTERNAL CONSTANT RECORD.` + `05 A PIC X(4) VALUE "ABCD".`) produced `error COBOLNET1549: … requires a TYPE clause naming a strongly typed definition` under **BOTH `--std 2023` AND `--std 2014`** — proving the rule is not dialect-gated.
- **AUDIT DRIFT CAUGHT:** section number only — audit row 86 cites the rule correctly as "§13.16.3 SR13 / §13.18.15", and the "UNCONDITIONALLY (not dialect-gated)" characterization is accurate. The DataBinder anchor the audit gives (":1866-1868") points at the **SAME AS** SR12 block, not the CONSTANT-RECORD SR13 block; the actual VCR-16 site is **DataBinder.cs:1892–1894** (the memory-index "DataBinder.cs ~1866-1868 CONSTANT RECORD SR13" pointer is off by ~26 lines — 1866 is the SAME-AS clause exclusion). Corrected anchor: `:1879–1901` (the `isConstantRecord` block), external leg at `:1892–1894`.
- **Implementation plan:**
  - **Binder:** in `DataBinder.cs:1892`, change the external leg to fire only at ≥2023: `: hasExternal && typeRefName is null && Edition.DialectLevel >= 2023 ? "…requires a TYPE clause…" : null`. (The other three legs — level, REDEFINES, ANY-LENGTH/BASED/etc. — stay version-invariant.)
  - **Diag code:** existing **COBOLNET1549** (ConstantRecordRule) — no new code. The descriptor text at `DiagnosticCatalog.cs:105–109` already scopes SR3/SR6/SR13; leave it.
  - **Doc:** add a one-line dialect note in the DataBinder comment block (§13.16.3 SR13 ¶2 is 2023-only, per E.3 item 10) and record it in the CONFORMANCE / VERSION_CHANGE_REFERENCE row for CONSTANT RECORD.
- **Golden:** positive-at-2014 fixture = `waveE_crext.cob` above compiled `--std 2014` ⇒ stdout `ABCD` (hand-derived: the constant record's `A` initializes to its VALUE `ABCD`, DISPLAY prints it). Same source `--std 2023` ⇒ `error COBOLNET1549: … requires a TYPE clause naming a strongly typed definition`. Add a ≥2023 positive companion: the strongly-typed form (`01 CT IS EXTERNAL TYPEDEF STRONG.` … `01 R TYPE CT IS EXTERNAL CONSTANT RECORD.`) compiles at 2023.
- **Blast radius / hazards:** any 2014/2002 fixture using `EXTERNAL CONSTANT RECORD` currently (wrongly) errors — flipping makes them pass, so a fixture asserting the 1549 at 2014 would need its edition bumped. Watch `ConstantRecordTests` / `ExternalDataTests`. Low blast radius (probe shows the construct is rejected everywhere today, so no test can currently depend on it succeeding at ≥2023 either — both need the two-sided fixture added).

---

### VCR 15 — EC-EXTERNAL-DATA-MISMATCH / -FILE-MISMATCH / -FORMAT-CONFLICT / -IMP (run-unit conformance raising)

- **Spec sections:**
  - §14.8.4.1 (line 25513): "In order to be able to check the conformance of external items between runtime elements, the EC-EXTERNAL exception conditions to be checked shall be enabled **in both the activating and activated runtime elements**, which for activated runtime elements shall be **before the Environment division**."
  - §14.8.4.2 (line 25525) → **EC-EXTERNAL-DATA-MISMATCH**: for each external file connector, the file status, linage and relative key data items shall be external data items and refer to the same corresponding storage in each runtime element.
  - §14.8.4.3 (line 25531) → **EC-EXTERNAL-FORMAT-CONFLICT**: external data-item formats correspond per §13.18.22 GR6 (line 18731 — same externalized name, identical VALUE spec, **same number of bytes**; for strongly typed, §8.5.3 also applies; for external strong record descriptions, same external strong type declarations + same presence/absence of CONSTANT RECORD, line 25535).
  - §14.8.4.4 (line 25537) → **EC-EXTERNAL-FILE-MISMATCH**: external file control entries correspond per §12.4.5.3 GR1 (line 15175, sub-items a–m).
  - **Raise points:** CALL GR3e (lines 26189–26195, the rule→EC table) and INVOKE GR7d (lines 28533–28538, identical table); disposition CALL GR3h (lines 26211–26213): if the EC is an EC-EXTERNAL condition, ON EXCEPTION handles it, else if checking enabled the applicable exception processing runs, else §14.6.13.1.1.
  - **Fatality (Table §14.6.13.1.6, lines 24670–24674):** EC-EXTERNAL-DATA-MISMATCH / -FILE-MISMATCH / -FORMAT-CONFLICT = **Fatal**; EC-EXTERNAL-IMP = **Imp**.
  - **Introduction proof:** Annex **E.3 item 9** (line 49138): "Exception conditions for checking conformance have now been added, where previously the mechanism … was unspecified." Level-2 EC-EXTERNAL is `introducedIn 2023` (ExceptionCatalog.cs:61).
- **Syntax / format:** no source syntax — these are runtime-raised conditions, enabled via `>>TURN EC-EXTERNAL [-…] CHECKING ON` (before the Environment division) and observed via `FUNCTION EXCEPTION-STATUS` / a `USE AFTER EXCEPTION CONDITION EC-EXTERNAL…` declarative / a `PERFORM … WITH … EC-EXTERNAL…` handler.
- **Introduced edition & gate:** the entire cluster is COBOL-2023. **Already gated at the source layer:** `TurnState.Build` (TurnState.cs:50–55) rejects a `>>TURN EC-EXTERNAL…` at `--std < 2023` with COBOLNET0878 (`IntroducedIn > DialectLevel`), and the catalog tags all four level-3 names + the level-2 name `introducedIn 2023` (ExceptionCatalog.cs:90–93, 61). So below 2023 no EC-EXTERNAL checking can be enabled → no raise → correctly inert. **No new introduction gate needed.** The work is the **runtime raise machinery**, live only at ≥2023.
- **Semantics (GR-level) — the check to implement:** at the run-unit link event (CALL/INVOKE that first activates a runtime element describing an already-registered external item), for each external item/file connector described by both the activating and activated elements:
  - Compare the **data descriptor**: externalized name identity, byte count (§13.18.22 GR6), VALUE spec identity, and — for strong records — strong-type-declaration identity + CONSTANT-RECORD presence parity (§14.8.4.3). Mismatch ⇒ **EC-EXTERNAL-FORMAT-CONFLICT** (Fatal).
  - Compare the **file-referencing control items** (FILE STATUS / LINAGE / RELATIVE KEY are the same corresponding external items, §14.8.4.2). Mismatch ⇒ **EC-EXTERNAL-DATA-MISMATCH** (Fatal).
  - Compare the **SELECT/file-control entries** (§12.4.5.3 GR1 a–m: OPTIONAL, ASSIGN consistency, organization, access mode, keys, sharing/lock, etc.). Mismatch ⇒ **EC-EXTERNAL-FILE-MISMATCH** (Fatal).
  - Implementor-defined mismatch ⇒ **EC-EXTERNAL-IMP** (Imp).
  - Each raises **only if checking for that EC is enabled in BOTH elements** (§14.8.4.1 / CALL GR3e final ¶). On a raised fatal EC-EXTERNAL the CALL/INVOKE is **not successful** (GR3h).
- **As-built today:** `ExternalTable.cs:9–10` verbatim: "The §13.18.22 GR6 conformance checks (same byte count / same VALUE across describers) belong to the §14.8.4 EC machinery — **not enforced here yet**." `ExternalTable.Cell(name, initialImage)` keys by name and stores only a `StorageCell` image — **no descriptor is recorded**, so no cross-element comparison is possible. The four EC-EXTERNAL-* names are catalogued (ExceptionCatalog.cs:90–93) but **never raised** (grep: no `Set("EC-EXTERNAL` in the runtime). `ExceptionEngine` has the `ArgumentFunctionChecking`/`DataConversionChecking` ambient-flag pattern (ExceptionState.cs:172/192) to copy but **no `ExternalChecking`**.
- **AUDIT DRIFT CAUGHT:** section number (§13.18.27→§13.18.22, and the GR6 the audit calls "§13.18.27 GR6" is §13.18.22 GR6 at line 18731). The audit's "§14.6.13.1.6 Table" fatality reference is correct (lines 24670–24674). Everything else in rows 62–63 (catalogued-not-raised, no gate flag, ExternalTable:9-10 comment) verified accurate.
- **Implementation plan:**
  - **ExceptionEngine (ExceptionState.cs):** add `public bool ExternalChecking { get; set; }` + four guarded Raise helpers — `ExternalFormatConflict(string detail)`, `ExternalDataMismatch(string detail)`, `ExternalFileMismatch(string detail)`, `ExternalImp(string detail)` — each `if (ExternalChecking) Set("EC-EXTERNAL-…", fatal: <Table fatality>)`. Mirror the `ArgumentFunctionChecking`/`ArgumentError` shape; expose static passthroughs on `ExceptionState`.
  - **ExternalTable (Control/ExternalTable.cs):** extend the cell registration to carry an `ExternalDescriptor` record (externalized name, byte count, VALUE image, strong-type key or null, CONSTANT-RECORD flag; for file connectors: the FILE STATUS / RELATIVE KEY / LINAGE external-item names + the SELECT attributes a–m). Signature: `Cell(string name, string initialImage, ExternalDescriptor desc, bool describerChecking)`. On the FIRST describer, store `desc` + `describerChecking`. On a SUBSEQUENT describer, compare `desc` to the stored one; on a conflict, and when `describerChecking && storedChecking` (both-elements rule), invoke the matching `ExceptionEngine` raise helper. Keep the existing 2-arg `Cell` overload as a shim (pre-G8 emitted surface) that passes an empty descriptor + `ExternalChecking=false`.
  - **Emitter/binder:** at each program's registration of an external item, the emitted code must pass the program's descriptor and its `ExternalCheckingEnabled` flag. `ExternalCheckingEnabled` = a compile-time constant computed from `TurnState.Enabled("EC-EXTERNAL-FORMAT-CONFLICT"/…, null, <first line before the Environment division>)` — i.e., the §14.8.4.1 "before the Environment division" enablement. Emit `ExceptionState.ExternalChecking = <const>;` in the program prologue (only when `TurnState.AnyEnabled`, preserving the zero-scaffolding invariant). Touchpoints: `CodeGen/EcEmitter.cs` (prologue flag), the external-item registration emit in the data/storage emitter, `DataBinder`'s external descriptor capture.
  - **Diag codes:** none new for the raises (they use `ExceptionState.Set` with the EC names). The introduction is already 0878-gated at `--std < 2023`.
  - **Minimal viable slice (recommended first landing):** the **FORMAT-CONFLICT** check on a shared external DATA item (byte-count / VALUE mismatch) — it needs only the data descriptor and exercises the whole flag+descriptor+raise path. DATA-MISMATCH (file control items) and FILE-MISMATCH (SELECT a–m) follow once the file-connector descriptor is threaded. Flag the cross-**separate-compilation** case as bounded by what the runtime `ExternalTable` observes; the in-compilation-group case (multiple `group.Units` sharing an external item) is fully checkable.
- **Golden (raise EC-EXTERNAL-FORMAT-CONFLICT, ≥2023):** a compilation group of two source elements (a caller + a CALLed subprogram) describing the same external name `SHARED-REC` with **different byte counts**, both enabling checking before the Environment division:
  ```cobol
  IDENTIFICATION DIVISION.
  PROGRAM-ID. WAVEE-XFMT-MAIN.
  >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
  ENVIRONMENT DIVISION.
  DATA DIVISION.
  WORKING-STORAGE SECTION.
  01 SHARED-REC IS EXTERNAL.
     05 A PIC X(4).
  PROCEDURE DIVISION.
  MAIN.
      CALL "WAVEE-XFMT-SUB"
      DISPLAY "STATUS=" FUNCTION EXCEPTION-STATUS
      STOP RUN.
  END PROGRAM WAVEE-XFMT-MAIN.
  IDENTIFICATION DIVISION.
  PROGRAM-ID. WAVEE-XFMT-SUB.
  >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
  ENVIRONMENT DIVISION.
  DATA DIVISION.
  WORKING-STORAGE SECTION.
  01 SHARED-REC IS EXTERNAL.
     05 A PIC X(8).
  PROCEDURE DIVISION.
  SUBMAIN.
      GOBACK.
  END PROGRAM WAVEE-XFMT-SUB.
  ```
  Expected stdout (hand-derived): `SHARED-REC` is described as 4 bytes by MAIN and 8 bytes by SUB — a §13.18.22 GR6 byte-count conflict; checking is enabled in both, so at the CALL the activated element's registration detects the descriptor conflict and sets **EC-EXTERNAL-FORMAT-CONFLICT** (Fatal); the CALL is not successful (GR3h). `FUNCTION EXCEPTION-STATUS` returns the level-3 name ⇒ **`STATUS=EC-EXTERNAL-FORMAT-CONFLICT`** (padded/truncated to the receiver rules of the DISPLAY of the intrinsic's alphanumeric result). **Below-2023 negative:** the same source at `--std 2014` fails to compile with `error COBOLNET0878: exception-name EC-EXTERNAL-FORMAT-CONFLICT was introduced by ISO/IEC 1989:2023 — it requires --std 2023 or later` (the `>>TURN` gate at TurnState.cs:50–55). **Control fixture:** identical program with SUB's `A PIC X(4)` (byte counts match) ⇒ no EC ⇒ `STATUS=` (empty / no-exception sentinel).
- **Blast radius / hazards:** the ambient `ExternalChecking` flag is `AsyncLocal`-scoped on `RunUnit` — ensure it is set per activated element and restored (the §14.8.4.1 "both elements" rule means the flag is really a pair; store the first describer's flag in the descriptor, don't just read the current ambient). Guard against a false FORMAT-CONFLICT for the legal REDEFINES-of-the-complete-external-record exception (§13.18.22 GR6 explicitly allows a non-identical complete REDEFINES for non-strong records). Watch the external-data and CALL/INVOKE test suites; ensure the zero-scaffolding invariant holds (no descriptor/flag emit when no `>>TURN EC-EXTERNAL` anywhere in the group).

---

### VCR 18 — Cross-SELECT FILE STATUS consistency for an EXTERNAL file (≥2023 requirement)

- **Spec sections:**
  - §12.4.5.3 GR1(i) (line ~15194, within the a–m list at 15175): for an external file connector, all file control entries "shall have … The same specification of the FILE STATUS clause, **where data-name-4 shall reference the same corresponding external data item**."
  - §14.8.4.2 (line 25525): the file status data item shall be an external data item referring to the same corresponding storage in each runtime element (the runtime face → EC-EXTERNAL-DATA-MISMATCH).
  - **Requirement proof:** Annex **E.2 item 12** (line 49164): "It is **now required** that if a file is external and has a FILE STATUS clause in the SELECT statement, all corresponding SELECT statements within the run unit … shall have a FILE STATUS clause specifying the same corresponding external data item."
- **Syntax / format:** no new syntax — a consistency constraint over the existing `FILE STATUS IS data-name` clause of the file control entry (§12.4.5.8) when the file's FD carries `IS EXTERNAL`.
- **Introduced edition & gate:** COBOL-2023 requirement (E.2 item 12). **Dialect-gated ≥2023 check**, two faces:
  - **Compile-time** (within one compilation group, where ≥2 source elements SELECT the same external file connector): a new **COBOLNET1570** error when the corresponding FILE STATUS items are not the same external data item — gated `Edition.DialectLevel >= 2023`.
  - **Runtime** (cross separate-compilation): the §14.8.4.2 violation → EC-EXTERNAL-DATA-MISMATCH (folded into VCR 15's DATA-MISMATCH check on the file-connector descriptor).
- **Semantics (GR-level):** for each external file connector referenced by more than one file control entry (in the compilation group), if any entry specifies FILE STATUS, all must, and each must name the **same corresponding external data item** (same externalized name + same corresponding storage). A missing FILE STATUS on one corresponding SELECT, or a FILE STATUS naming a different / non-external item, is the violation.
- **As-built today:** **no check exists.** `DataBinder.cs:736–746` records only `file.IsExternal` / `file.ExternalName` from the FD `IS EXTERNAL` clause; there is no cross-SELECT reconciliation and FILE STATUS is not correlated across file connectors of the same external name. (Grep for a corresponding-external FILE STATUS check across `src/Cobol.Net.Compiler` = none.)
- **AUDIT DRIFT CAUGHT:** none of substance — audit row 64 cites "§13.18.x / E.2 item 12" (vague but not wrong; the precise anchors are §12.4.5.3 GR1(i) + E.2 item 12 line 49164). The DataBinder anchor ":727-735" is one line short — the external-file recording is `:736–746` (the `fileGlobalExternalClause` leg); `:727` is the RECORD clause. Corrected anchor: `DataBinder.cs:735–746`.
- **Implementation plan:**
  - **Binder / a new group-level validation:** after all units of the compilation group bind, group file connectors by external name across `group.Units`; for each external-file group at `Edition.DialectLevel >= 2023`, verify FILE STATUS presence + same-external-item across the corresponding SELECTs. Emit **COBOLNET1570** on violation. Natural home: a small post-bind cross-unit pass invoked from `BinderDriver` (or an extension of the run-unit external reconciliation), NOT `VersionConformancePass` (this is a semantic cross-unit SR, not a construct introduction).
  - **Runtime:** covered by VCR 15's file-connector descriptor (FILE STATUS external-item identity) → EC-EXTERNAL-DATA-MISMATCH.
  - **Diag code:** propose **COBOLNET1570** — `external-file-status-consistency`, EditionSeverity.Error, "For an external file, all corresponding SELECTs in the run unit shall specify FILE STATUS naming the same corresponding external data item (ISO §12.4.5.3 GR1(i); Annex E.2 item 12)." (Final number reconciled at implementation — next free after Wave-D/other-wave allocations.)
- **Golden (≥2023 negative):** two in-group source elements SELECT external file `F` (`ASSIGN`, `ORGANIZATION SEQUENTIAL`), one with `FILE STATUS IS EXT-ST` (an `01 EXT-ST IS EXTERNAL PIC XX.`) and the other omitting FILE STATUS (or naming a non-external item) ⇒ `error COBOLNET1570: file 'F' … all corresponding SELECTs shall specify FILE STATUS on the same corresponding external item` at `--std 2023`. Positive: both name the same external `EXT-ST` ⇒ compiles. Below-2023 companion: same inconsistent source at `--std 2014` compiles clean (the requirement did not exist) — proving the dialect gate.
- **Blast radius / hazards:** the file-I/O suites (SQ/RL/IX) — any multi-program NIST fixture with an external file lacking a corresponding FILE STATUS would newly error at 2023; scope the check to external files only (internal files unaffected). Guard-fast file-I/O flake caveat applies (SOLO-rerun on SQ/IC/IX/ST/OB).

---

### VCR 31 — Relative-key consistency for an EXTERNAL relative file (≥2023 requirement)

- **Spec sections:**
  - §12.4.5.3 GR1(h) (line ~15193, within a–m at 15175): for an external file connector, all file control entries "shall have … The same specification of the RELATIVE KEY clause, **where data-name-7 references an external data item**."
  - §14.8.4.2 (line 25525): the relative key data item shall be an external data item referring to the same corresponding storage in each runtime element (runtime → EC-EXTERNAL-DATA-MISMATCH).
  - **Requirement proof:** Annex **E.2 item 24** (line 49312): "Relative keys where the file is external. It is now a requirement that the relative key data item is always the **same corresponding external data item**."
- **Syntax / format:** no new syntax — a constraint over the existing `RELATIVE KEY IS data-name` clause (§12.4.5.13) of a relative-organization file control entry whose FD is `IS EXTERNAL`.
- **Introduced edition & gate:** COBOL-2023 requirement (E.2 item 24). **Dialect-gated ≥2023**, same two faces as VCR 18:
  - **Compile-time** (in-group multiple SELECTs of the same external relative file): a new **COBOLNET1571** error when the RELATIVE KEY items are not the same external data item — gated `>= 2023`.
  - **Runtime** (cross-compilation): folded into VCR 15's EC-EXTERNAL-DATA-MISMATCH file-connector descriptor check.
- **Semantics (GR-level):** for each external **relative** file connector referenced by more than one file control entry, every RELATIVE KEY data item must be the **same corresponding external data item** (same externalized name + same corresponding storage), and must itself be external. A RELATIVE KEY naming a non-external item, or differing across corresponding SELECTs, is the violation.
- **As-built today:** **no check exists** (grep across `src/Cobol.Net.Compiler` for relative-key / corresponding-external consistency = none). `DataBinder.cs:735–746` records external-file identity only; the RELATIVE KEY is bound per-file with no cross-connector correlation and no external-attribute assertion.
- **AUDIT DRIFT CAUGHT:** none of substance — audit row 65 cites "E.2 item 24" (correct, line 49312); the precise SR anchor is §12.4.5.3 GR1(h). DataBinder anchor same correction as VCR 18 (`:735–746`, not `:727-735`).
- **Implementation plan:**
  - **Binder / group-level validation:** same post-bind cross-unit pass as VCR 18; for each external relative-file connector group at `>= 2023`, verify each RELATIVE KEY is the same external data item; emit **COBOLNET1571** on violation.
  - **Runtime:** VCR 15's descriptor (RELATIVE KEY external-item identity) → EC-EXTERNAL-DATA-MISMATCH.
  - **Diag code:** propose **COBOLNET1571** — `external-relative-key-consistency`, EditionSeverity.Error, "For an external relative file, all corresponding SELECTs shall specify RELATIVE KEY as the same corresponding external data item (ISO §12.4.5.3 GR1(h); Annex E.2 item 24)." (Final number reconciled at implementation time; VCR 18=1570, VCR 31=1571 are contiguous — flag to sibling waves that Wave E claims **1570–1571**.)
- **Golden (≥2023 negative):** two in-group source elements SELECT external relative file `RF` (`ORGANIZATION RELATIVE ACCESS RANDOM RELATIVE KEY IS RK`), one with `RK` external and the other with a **local** (non-external) `RK`, or a differently-named external key ⇒ `error COBOLNET1571: file 'RF' … RELATIVE KEY shall be the same corresponding external item` at `--std 2023`. Positive: both name the same `01 RK IS EXTERNAL PIC 9(4).` ⇒ compiles. Below-2023 companion: the inconsistent source at `--std 2014` compiles clean.
- **Blast radius / hazards:** the RL (relative file) suite; scope strictly to external relative files. Same file-I/O guard-flake SOLO-rerun caveat.

---

### Cross-cutting notes for the Wave-E implementer

- **Diag-band allocation:** Wave E introduces **COBOLNET1570** (VCR 18) and **COBOLNET1571** (VCR 31). VCR 63 reuses **COBOLNET0900**; VCR 16 reuses **COBOLNET1549**; VCR 15 adds **no compile code** (runtime `ExceptionState.Set` of the four EC-EXTERNAL-* names, already 0878-gated below 2023). Sibling waves: Wave E consumes 1570–1571 from the "next free 1570" pointer — reconcile final numbers at merge.
- **The binder-reads-edition doctrine:** VCR 16/18/31 are **version-conditioned structural SRs**, not construct introduction/removal gates — they legitimately read `Edition.DialectLevel` in the binder (the `CheckDigitCapacity` precedent), and must **NOT** route through `ConstructRegistry.Check`. Only VCR 63 (a true introduction) goes through `VersionConformancePass` + `constructs.json` + the drift-tested generators.
- **Grammar:** the entire slice is **ADDITIVE-free** — `externalClause`, `typedefClause`, `fileStatusClause`, `relativeKeyClause` all already exist. No `.g4` change ⇒ **no legacy-guard-for-grammar obligation**; the standard guard-fast + full CI (RELEASE leg) still gates the merge.
- **Cross-compilation reality:** the compile-time VCR 18/31 checks only see source elements **in the same compilation group** (`group.Units`). True separate-compilation run-unit conformance is the domain of the VCR-15 runtime `ExternalTable` descriptor comparison; document this split so the checks are not mistaken for total.

---

## Wave F — USE FOR DEBUGGING + DEBUG-ITEM at --std 85 (VCR 7.17)

## Wave F — USE FOR DEBUGGING + DEBUG-ITEM special-register model at `--std 85` (VCR 7.17)

### Authority note (READ FIRST — the spec file does NOT contain this facility)
The X3.23-1985 / ISO 1989:1985 **debug module** was **deleted in COBOL-2002** and is **entirely absent from ISO/IEC 1989:2023**. I grepped `specs/ISO_COBOL.md` exhaustively (`grep -ni 'debug'`): the ONLY hits are two incidental prose uses of the word "debugging" at lines **24411 / 24452** (a NOTE about rollback leaving items "available for debugging") — there is **no** `DEBUG-ITEM`, `DEBUG-LINE`, `USE FOR DEBUGGING`, or `WITH DEBUGGING MODE` syntax/GR anywhere in the 2023 text. Therefore, per the task's own instruction, **the authoritative behavior is the X3.23-1985 standard**, and the authoritative *regression evidence in-repo* is the **CCVS DB-series corpus** (`tests/nist/programs/DB*.cob`), whose comparisons pin the observable DEBUG-ITEM contract. Every layout/contents claim below is corroborated against a DB-program witness, cited by file:line. This is the correct posture: 2023 rejects the whole facility (already implemented, see below); the *behavior* to model lives only at `--std 85`.

### Spec sections (what governs)
- **ISO-2023:** none. §8.9 reserved-word table @10407–10408 (per `ConstructRegistry.g.cs:127` cite) is the *absence* proof — the DEBUG-* spellings and DEBUGGING/USE-FOR-DEBUGGING are simply gone. This is why 2023 gates the constructs as *removed-2002* (COBOLNET0902), not as unimplemented.
- **X3.23-1985 (external authority):** the Debug module — `USE FOR DEBUGGING` declarative, the `DEBUG-ITEM` special register, the `SOURCE-COMPUTER … WITH DEBUGGING MODE` compile-time switch, and the object-time (run-time) debug switch.
- **In-repo witness corpus:** `tests/nist/programs/DB101A.cob` (procedure triggers + DEBUG-CONTENTS taxonomy), `DB201A.cob` (DEBUG-SUB-1/2/3 subscript rendering + qualified-name DEBUG-NAME), `DB102A/DB104A/DB105A/DB202A–205A`, driver members `DB103M`/`DB301M–305M`. 15 sources at `tests/nist/programs/DB{101A,102A,103M,104A,105A,201A,202A,203A,204A,205A,301M,302M,303M,304M,305M}.cob`; **zero** goldens in `tests/nist/valid/` (verified: `ls tests/nist/valid | grep -ci '^DB'` → `0`).

### Syntax / format
Grammar (already parsed — `src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4:179-214`):
```
useStatement : … | USE FOR DEBUGGING ON? useDebugTarget+ ;   // line 203
useDebugTarget
    : ALL PROCEDURES                       // line 211
    | ALL REFERENCES OF? dataReference      // line 212
    | dataReference                         // line 213  (file-name / procedure-name / cd-name / unqualified id)
    ;
```
- `ON` is optional (house tolerance; every CCVS-85 witness writes it). Format: `USE FOR DEBUGGING ON {cd-name-1 | [ALL REFERENCES OF] identifier-1 | file-name-1 | procedure-name-1 | ALL PROCEDURES}…`. Multiple operands per USE (DB101A:230-231 `USE FOR DEBUGGING ON FALL-THROUGH-TEST PROC-SERIES-TEST`) and multiple operands across kinds are allowed.
- Compile-time switch: `SOURCE-COMPUTER. computer-name WITH DEBUGGING MODE.` — swallowed by the `computerAttributes` raw-token sink; recognized by token-text scan (`VersionConformancePass.cs:427`).
- `DEBUG-ITEM` and members are **special registers** (implicitly described, no DATA DIVISION entry) referenced only inside debugging declaratives.
- Lexer tokens present: `DEBUGGING` (`CobolLexer.g4:410`), `REFERENCES` (413), `PROCEDURES` (414). DEBUG-* register spellings are **NOT** dedicated tokens — they arrive as `cobolWord`/USER-DEFINED and are recognized by string prefix `"DEBUG-"` in the reserved-word funnel (`VersionConformancePass.cs:1549`).

### Introduced edition & gate
- **Introduced:** COBOL-85 (X3.23-1985 Debug module). **Removed:** COBOL-2002 (whole facility). **VCR Table 7 row 7.17.**
- **Gate at ≥2002 (already correct, verified by probe):** `VersionConformancePass.cs:428` → `DebuggingModeRemoved2002` (COBOLNET0902) for `WITH DEBUGGING MODE`; `:586` → `UseForDebuggingRemoved2002` (COBOLNET0902) for the `USE FOR DEBUGGING` declarative. Registry rows: `Constructs.g.cs:125`, `ConstructRegistry.g.cs:127` (`introducedIn=85, removedIn=2002, code=COBOLNET0902`). **Probe confirmed** at `--std 2023`: both fire COBOLNET0902 (see As-built).
- **Below-edition (i.e. at 85, the "introduction" leg is moot — this is a *removal* gate, so 85 is the ACCEPT edition).** The gate direction is inverted from the usual new-in-2023 case: 85 accepts, ≥2002 rejects.

### Semantics (GR-level, from X3.23-1985, each corroborated by a DB witness)

**Two-switch activation model:**
1. **Compile-time switch = `WITH DEBUGGING MODE`.** ABSENT ⇒ every `USE FOR DEBUGGING` section AND every `D`/`S`/`Y`-indicator debugging line is compiled **as if it were a comment** (removed from the program). PRESENT ⇒ they are compiled as real source.
2. **Object-time (run-time) switch** (implementor-defined; for CCVS runs it is **ON**). If OFF, sections compile but never trigger and DEBUG-ITEM is never populated. **To compile-and-run the DB corpus correctly the object-time switch MUST be ON** when `WITH DEBUGGING MODE` is present.

**DEBUG-ITEM special-register layout** (implicit description; corroborated widths):
```
01  DEBUG-ITEM.
    02  DEBUG-LINE       PIC X(6).
    02  FILLER           PIC X   VALUE SPACE.
    02  DEBUG-NAME       PIC X(30).
    02  FILLER           PIC X   VALUE SPACE.
    02  DEBUG-SUB-1      PIC 9(4).        *> zero-filled; SPACES if reference not subscripted
    02  FILLER           PIC X   VALUE SPACE.
    02  DEBUG-SUB-2      PIC 9(4).
    02  FILLER           PIC X   VALUE SPACE.
    02  DEBUG-SUB-3      PIC 9(4).
    02  FILLER           PIC X   VALUE SPACE.
    02  DEBUG-CONTENTS   PIC X(n).        *> implementor-defined width; wide enough for a record/data image
```
**⚠ DEBUG-SUB width/sign is witness-pinned, NOT textbook.** The widely-cited textbook layout is `S9(4) SIGN LEADING SEPARATE` (5 chars). But `DB201A.cob` does `MOVE DEBUG-SUB-1 TO SUB-1-1` where `SUB-1-1 PIC X(5)` (`:77`, `:273`) then `IF SUB-1-1 IS EQUAL TO "0005"` (`:1200`) / `"0004"` (`:1264`). A literal `"0005"` compared against X(5) is space-padded to `"0005 "`; a `SIGN LEADING SEPARATE` positive image would be `"+0005"` and FAIL. So the **observable contract the DB201A golden will require is an unsigned 4-digit zero-filled rendering** (`9(4)`), spaces when unsubscripted. Implement `9(4)`; pin the exact sign/pad against the DB201A golden when it is generated.

**Trigger points + DEBUG-CONTENTS taxonomy** (the DEBUG-CONTENTS string set is confirmed present in the corpus — `grep` across `DB*.cob` yields exactly: `"START PROGRAM"`, `"FALL THROUGH"`, `"PERFORM LOOP"`, `"USE PROCEDURE"`, `"SORT INPUT"`, `"SORT OUTPUT"`, `"MERGE OUTPUT"`, and SPACES):

For `USE FOR DEBUGGING ON procedure-name-1` / `ALL PROCEDURES` — the section executes **immediately before each execution of the subject procedure** (after control transfer, before its first statement). DEBUG-ITEM is space-filled first, then:
| Cause of execution | DEBUG-NAME | DEBUG-CONTENTS | witness |
|---|---|---|---|
| First execution of the first nondeclarative procedure | that procedure | `"START PROGRAM"` | DB101A "START-PROGRAM-TEST" (`:258`) |
| Sequential fall-through into the procedure | the procedure | `"FALL THROUGH"` | DB101A `:428-430` |
| 2nd..nth iteration of a `PERFORM … TIMES/UNTIL/VARYING` whose range is the procedure | the procedure | `"PERFORM LOOP"` | DB101A `:643-645`, `PERFORM LOOP-ROUTINE FIVE TIMES` `:617` |
| Plain `PERFORM` / `GO TO` / altered `GO TO` transfer to the procedure | the procedure | SPACES | DB101A `:595` (`DBCONT-HOLD EQUAL TO SPACE`) |
| `ALTER` statement referencing the procedure | the procedure | SPACES | DB101A ALTER-PARAGRAPH section `:240-244` |
| Procedure is a SORT `INPUT`/`OUTPUT` procedure | the procedure | `"SORT INPUT"` / `"SORT OUTPUT"` | DB2xx |
| Procedure is a MERGE `OUTPUT` procedure | the procedure | `"MERGE OUTPUT"` | DB2xx |
| Procedure is itself a declarative `USE` procedure being invoked | the procedure | `"USE PROCEDURE"` | DB101A `"USE PROCEDURE NOT EXECUTED"` negative `:371` |

- **DEBUG-LINE** = the compiler-assigned source-line/sequence number (X(6)) of the *statement that caused* the section to run. Implementor-defined format; the CCVS "DEBUG-LINE; SEE NEXT LINE" subtest (DB101A `:374-375`) pins it against a specific statement — **golden-pinned observable**.
- **DEBUG-NAME** = the leftmost 30 chars of the triggering name. **Qualified references** append qualifiers separated by ` OF ` (DB201A `USE FOR DEBUGGING ON ALL REFERENCES OF ABC1 OF AB2 OF A1` `:278-279`; DEBUG-NAME renders the full qualified image).
- **DEBUG-SUB-1/2/3** = subscript/index values of the triggering reference (zero-filled 9(4)); SPACES if the reference was not subscripted/indexed. DB201A `SET I TO 4 / J TO 6 / K TO 8` then `MOVE "Z" TO B-LEVEL-3 (I,J,K)` → DEBUG-SUB-1 = `"0004"` (`:1264`).

For `USE FOR DEBUGGING ON [ALL REFERENCES OF] identifier-1` — the section executes **after** execution of any statement that references identifier-1 (with `ALL REFERENCES OF`: every reference; without: only where identifier-1 is a *receiving* item). DEBUG-NAME = data-name, DEBUG-CONTENTS = the character image of the data item **after** the statement, DEBUG-SUB-n = its subscripts. DB201A `CONTENTS-1`/`SUBSC-TEST-2A` (`:1255` compares DEBUG-CONTENTS `"Z"`).

For `USE FOR DEBUGGING ON file-name-1` — after each OPEN/CLOSE/READ/WRITE/… on the file; DEBUG-CONTENTS per the implementor (record image for READ). For `cd-name-1` — after SEND/RECEIVE (communication module — **not modeled**, see residual).

**Scope/inheritance:** the compile-time switch is per top-level program; nested/contained programs inherit it (`VersionConformancePass.cs:369-371` already encodes this). A given procedure/data/file may be named in at most one debugging declarative.

### As-built today (file:line, confirmed by reading + CLI probe)
- **Grammar:** fully parses the declarative + all three operand shapes + qualified `dataReference` (`CobolControlFlow.g4:203-214`). Lexer tokens present (`:410/413/414`). **DONE.**
- **≥2002 removal gate:** `VersionConformancePass.cs:428` (mode) + `:583-586` (declarative) → COBOLNET0902. **Probe at `--std 2023` confirmed** both fire 0902. **DONE.**
- **Compile-time switch detection:** `DataBinder.cs:183/253` sets `DebuggingModeDeclared` from `SOURCE-COMPUTER … WITH DEBUGGING MODE`; the ParseArm mirrors it in `_debuggingModeDeclared` (`VersionConformancePass.cs:371,432`).
- **Switch-ABSENT comment treatment:** `VersionConformancePass.cs:613-627` (`VisitDeclarativeSection`) — when `!_debuggingModeDeclared`, only the USE statement is visited (so its 0902 gate still fires at ≥2002) and the section body is **skipped**, keeping the §8.9 reserved-word funnel off the DEBUG-* names. `ProcedureTableBuilder.cs:160` — `if (!ctx.Data.DebuggingModeDeclared) return;` drops the whole section from the pc space (nothing binds, `scope=null`). **Probe `wavef_dbg2.cob` (`--std 85`, no `WITH DEBUGGING MODE`): compiles + runs clean, output `IN TARGET` / `DONE`, the declarative NEVER runs — comment-treated.** CONFIRMED.
- **THE GAP — switch-PRESENT at 85:** `ProcedureTableBuilder.cs:151-161` — the section IS collected but `scope` stays `null` (permanently-off object-time switch), so **no `BoundDeclarative` is emitted** → the declarative can never trigger, DEBUG-ITEM is never populated. And any DEBUG-* register reference inside funnels to **COBOLNET0899** (`VersionConformancePass.cs:1549-1555`, `DiagnosticCatalog.cs:399-402` → code `COBOLNET0899`). **Probe `wavef_dbg1.cob` (`--std 85`, WITH DEBUGGING MODE): three `error COBOLNET0899` for DEBUG-LINE / DEBUG-NAME / DEBUG-CONTENTS** — the program does NOT compile. So DB101A etc. cannot be compiled-and-run. This is exactly the item to retire.
- **'D'/'S'/'Y' debug-line half:** `ReferenceFormatProcessor.cs:333-334` normalizes indicator-column `D/d/S/s/Y/y` lines to `*> DEBUG:` comment lines **unconditionally** (regardless of `WITH DEBUGGING MODE`); `CopyProcessor.cs:240-248` lets COPY/REPLACE text-manipulation see their content but they never become live source. So the *conditional-compilation* half of the debug facility is **always comment-treated** today — a second un-modeled sub-facility.

### AUDIT DRIFT CAUGHT
- **Audit row (`PHASE-13-audit.md:87`) mislabels the 0899 staging as "NOT a real bound node … the declarative binds to nothing … staged-loud COBOLNET0899."** Two-part correction: (1) the 0899 is raised on the **DEBUG-* register reference** (`VersionConformancePass.cs:1550`), NOT on the declarative binding itself; the declarative-binds-to-nothing behavior is a **separate** fact (`ProcedureTableBuilder.cs:160`, `scope=null`). (2) The row calls the whole thing "PARTIAL" but the switch-ABSENT comment-treatment path is actually **fully correct and probe-verified** — only the switch-PRESENT path is the gap. Minor, but the disposition should read "switch-ABSENT DONE; switch-PRESENT is the gap."
- **Audit line 66 "blocked on the DEBUG-ITEM register row"** is accurate as a dependency but understates scope: unblocking DB-series also needs the **object-time switch turned ON** and the **procedure/data/file trigger insertion** — not just the register data-model. Corrected in the plan below.
- **`ConstructRegistry.g.cs:127` §-cite "§8.9 absence @10407–10408"** — **verified**: those lines are the 2023 reserved-word table region; the absence claim is sound. Audit's edition-gate rows (`:92`, `:93`) all **check out** against the code (grammar parse DONE; 0902 gate DONE). No drift there.
- Everything else in the audit slice **verified against code + probe**.

### Implementation plan (concrete, decision-complete)

This is a **medium-large** feature (a special register + a cross-cutting trigger-insertion pass), not a one-liner. Sequence:

1. **Object-time switch (config).** Add a run-unit debug-active flag, ON when `WITH DEBUGGING MODE` is present at `--std ≤ 2002` (85 only in practice). Optionally a CLI `--debug-mode off` override, but default ON for `WITH DEBUGGING MODE` to match CCVS. Owner-visible decision — flag it. Touchpoint: `RunUnit` state + `DataBinder.DebuggingModeDeclared` already available.

2. **DEBUG-ITEM special register (typed-native, per `feedback_oo_typed_native_not_byte`).** Model as a synthesized record with native fields `DebugLine:string(6)`, `DebugName:string(30)`, `DebugSub1/2/3:int(4-digit)`, `DebugContents:string(n)`, exposed through the special-register resolution path used by other predefined registers (follow the `EXCEPTION-OBJECT` precedent referenced at `VersionConformancePass.cs:1530-1541`). Replace the `DEBUG-` prefix funnel at `VersionConformancePass.cs:1549` so that, under `_debuggingModeDeclared`, DEBUG-* resolves to the register instead of raising 0899. Binder touchpoint: the special-register/reference resolver in `src/Cobol.Net.Compiler/Binding/` (mirror how `RETURN-CODE`/`EXCEPTION-OBJECT` bind). No grammar change (DEBUG-* are `cobolWord`s) — **ADDITIVE, no legacy-binder risk.**

3. **Emit a real `BoundDeclarative` for the debug section.** At `ProcedureTableBuilder.cs:151-161`, when `DebuggingModeDeclared` (and object-time ON), build a `BoundDeclarative` carrying a new `DebugScope` variant (subject kind = ALL-PROCEDURES / procedure-name-list / data-name-list [+ALL REFERENCES OF] / file-name-list / cd-name-list). Extend the `DeclScope` record (`ProcedureTableBuilder.cs:186-188`) or add a sibling. Bound-node: a `BoundDebugDeclarative` (or a discriminated field on `BoundDeclarative`).

4. **Trigger-insertion in the emitter.** The hard part. For each subject:
   - **procedure-name / ALL PROCEDURES:** the PC-dispatcher already threads procedure entry; inject, at each subject procedure's entry, a call that (a) populates DEBUG-ITEM with the cause (fall-through vs GO TO vs PERFORM-iteration vs SORT/MERGE/USE vs START-PROGRAM — the dispatcher knows the transfer kind), (b) invokes the debug section body. Emitter touchpoint: `CSharpEmitter` procedure-dispatch + a new `__RunDebug(procIndex, cause)` helper (mirror `__RunGlobalUse` at `CSharpEmitter.ReportWriter.cs`).
   - **data-name [ALL REFERENCES OF]:** after each statement that references the subject item, populate DEBUG-CONTENTS from the item's post-statement image + DEBUG-SUB from its subscripts, then invoke. This needs a per-statement post-hook keyed by referenced symbols — the largest sub-task; scope to *receiving-item* references first, `ALL REFERENCES OF` second.
   - **file-name:** after each I/O verb on the file; DEBUG-CONTENTS = record image on READ.

5. **Retire COBOLNET0899 for this facility → COBOLNET1570 residual.** Once registers + procedure/data/file triggers land, DEBUG-* no longer raises 0899. Assign **COBOLNET1570** (WARNING band, `--std 85` only) as the **un-modeled sub-facility residual note**, raised for the pieces intentionally NOT modeled: (a) `cd-name` communication debugging (COMMUNICATION module absent), (b) the `D`/`S`/`Y` conditional-compilation debug-line half if not implemented in the same wave, (c) any file-name READ record-image detail we stub. `COBOLNET1570` next-free confirmed (used codes stop at 1569; 0899 catalog at `DiagnosticCatalog.cs:44`). **Flag for sibling-wave reconciliation:** Wave F claims **COBOLNET1570**; final number reconciled at implementation time.
   - Add a `DiagnosticCatalog` descriptor `DebugSubFacilityResidual` = `COBOLNET1570`, `RecognizedNotImplemented`-style but WARNING severity, `--std 85` posture.

6. **'D'/'S'/'Y' debug-line conditional compilation (optional, same wave).** Make `ReferenceFormatProcessor.cs:333-334` emit debug lines as **live source** (not `*> DEBUG:` comments) when `WITH DEBUGGING MODE` is in effect. Requires the reference-format pass to know the switch — a two-pass or a deferred rewrite. If deferred, cover it by the COBOLNET1570 residual note. **This is the one item that could touch the shared preprocessor** — additive/conditional, no grammar restructure, so legacy-guard-clean, but run the full legacy guard because `ReferenceFormatProcessor` feeds both compilers.

7. **constructs.json / registry:** no new *construct* row needed (the 0902 removal rows already exist); only the new **diagnostic** descriptor (COBOLNET1570). Keep `use-for-debugging-removed-2002` / `debugging-mode-removed-2002` rows unchanged.

**Grammar verdict:** **NO grammar change** — everything parses today. All work is binder/emitter/runtime + one diagnostic + (optional) preprocessor. This means **no legacy-ANTLR-restructure risk**; the only legacy-guard trigger is step 6 (shared preprocessor), which is additive.

### Golden

**Positive fixture `wavef-dbg-proc.cob` (`--std 85`), procedure-trigger subset:**
```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WAVEF-DBG-PROC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM   PIC X(30).
       01 CONT PIC X(13).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ALL PROCEDURES.
       DBG-BODY.
           MOVE DEBUG-NAME     TO NM.
           MOVE DEBUG-CONTENTS TO CONT.
           DISPLAY "N=" NM "C=" CONT.
       END DECLARATIVES.
       MAIN SECTION.
       P-START.
           PERFORM P-LOOP 2 TIMES.
           GO TO P-END.
       P-LOOP.
           CONTINUE.
       P-END.
           STOP RUN.
```
**Hand-derived stdout** (DEBUG-NAME left-justified in X(30) → trailing spaces; DEBUG-CONTENTS in X(13)). ALL PROCEDURES fires before each procedure execution:
- `P-START` is the first nondeclarative procedure → cause `START PROGRAM`.
- `P-LOOP` first invocation via PERFORM → SPACES; second iteration of the `2 TIMES` loop → `PERFORM LOOP`.
- `P-END` reached by `GO TO` → SPACES.

```
N=P-START                      C=START PROGRAM
N=P-LOOP                       C=
N=P-LOOP                       C=PERFORM LOOP
N=P-END                        C=
```
(each `N=` field is exactly 30 chars: name + right-pad spaces; each `C=` field exactly 13 chars.) The `START PROGRAM`/`PERFORM LOOP`/blank taxonomy is the DB101A-witnessed contract (`DB101A.cob:428-430,595,643-645`, `:258`).

**Positive fixture `wavef-dbg-sub.cob` — DEBUG-SUB subset** (pins the 9(4) rendering that the S9(4)-sign-separate textbook layout would fail): a `USE FOR DEBUGGING ON ALL REFERENCES OF T` where `T OCCURS 5`, a `MOVE "Z" TO T(4)` → after-statement trigger with DEBUG-SUB-1 rendering `0004` (moved to X(5) → `"0004 "`, matching DB201A `:1264`), DEBUG-CONTENTS `Z`.

**Below-edition negative fixture `wavef-dbg-neg.cob` (`--std 2023`)** — the same source; expected diagnostics (probe-verified today, unchanged by this wave):
```
error COBOLNET0902: the WITH DEBUGGING MODE clause (SOURCE-COMPUTER) was removed in COBOL-2002 (targeting COBOL-2023) …
error COBOLNET0902: the USE FOR DEBUGGING declarative was removed in COBOL-2002 (targeting COBOL-2023) …
```

**DB-series movement:** with steps 1–5 landed, DB101A/DB201A become **compile-and-run** candidates; generate their goldens by hand-deriving from the CCVS PASS/FAIL report structure (DEBUG-LINE subtests are golden-pinned — capture the compiler's chosen line-number rendering once and freeze it). DB103M/DB301M–305M are driver members — attempt after DB101A/DB201A pass. Any that hit `cd-name` communication debugging stay behind the **COBOLNET1570** residual note.

### Blast radius / hazards
- **No grammar change** → the shared-ANTLR/legacy-binder hazard does NOT apply to steps 1–5. Step 6 (preprocessor) is additive/conditional but feeds both compilers → **run the full legacy guard** if attempted.
- **New special register** must not leak into `--std ≥ 2002` resolution — keep it strictly gated on `_debuggingModeDeclared` (which is impossible at ≥2002 since `WITH DEBUGGING MODE` is already 0902-rejected). Watch: the `EXCEPTION-OBJECT`/reserved-word funnel (`VersionConformancePass.cs:1534-1562`) must keep routing non-DEBUG names to §8.9 unchanged — add DEBUG-ITEM to the register set the *same way* EXCEPTION-OBJECT is handled, not by broadening the funnel.
- **Trigger insertion touches the PC-dispatcher/emitter** — the most regression-prone surface. Suites to watch: the full greenfield conformance battery (esp. PERFORM/GO TO/SORT/MERGE families that share the dispatcher), the characterization snapshots (byte-exact — a spurious debug hook would shift output), and the **legacy NIST guard** (the frozen compiler already comment-treats these; ensure the greenfield-only debug path doesn't perturb shared corpora). SORT/MERGE-driven DEBUG-CONTENTS (`SORT INPUT`/`OUTPUT`, `MERGE OUTPUT`) rides the SORT engine — flaky file-I/O guard leg (SQ/IC/IX/ST/OB) → SOLO-rerun on failure.
- **Object-time-switch default** is an owner-visible behavioral choice (default-ON vs require a flag) — raise it as a bare decision before implementing (`feedback_ask_bare_decision`).
- **DEBUG-SUB sign/width** is witness-pinned (unsigned 9(4)); if a later authoritative X3.23-1985 text says `S9(4) SIGN LEADING SEPARATE`, the DB201A golden is the arbiter — do NOT emit a leading `+` or the `"0005"` comparison fails.

---

## Wave G — Table 1/5 behavior rows (VCR 21/22/24/78 · 34/35/36/86 · 27/33/37/14/17/20/49 · 68/69)

## Wave G — Table 1 / Table 5 behavior rows (VCR 21/22/24/78 · 34/35/36/86 · 27/33/37 · 14/17/20/49 · 68/69)

**Wave-wide notes carried by every row below.** Next free diagnostic verified `COBOLNET1570` (grep max = `COBOLNET1569`). This wave proposes exactly **two** new always-on rejects — **`COBOLNET1570`** (VALUE numeric-edited literal-class nonconformance, §13.18.63 SR7) and **`COBOLNET1571`** (MERGE-in-output/SORT-proc static prohibition, §14.9.24) — numbered from the next free; sibling waves take 1572+. New-in-2023 *introduction gates* reuse the shared **`COBOLNET0900`** (below-2023 reject) with a `constructs.json` `introducedIn:2023` row, per the settled pattern. The FLAG-14 "twins" named throughout are **not** separate diagnostics — they are the sub-option words of the ONE `>>FLAG-14` directive, whose complete option list is **§7.3.15.4 GR4 a–l (spec lines 4507–4533)**; wiring them is the Wave D/H `>>FLAG-14` code half, and each behavior row below only needs to expose the construct's presence to that sink.

> **AUDIT/TASK DRIFT — FLAG-14 twin inventory (authoritative).** The exact GR4 option words (§7.3.15.4, lines 4507–4533), with the Wave-G row each twins:
> `b) COMPILE-TIME-ARITHMETIC-EXPRESSIONS` · `c) EVALUATE directive` **(VCR 14)** · `d) I-O-DECLARATIVE` · `e) I-O-STATUS-04` **(VCR 21)** · `f) I-O-STATUS-07` **(VCR 22)** · `g) NUM-ED-ZERO-FIG-CONSTANT` **(VCR 35)** · `h) READ-PREVIOUS` · `i) REF-MOD-ZERO-LENGTH` · `j) VALUE-EDITING` **(VCR 36)** · `k) VALUE-FIG-CON-NO-LENTH` **(VCR 17)** · `l) VALUE-ZERO` **(VCR 35)**. **There is NO `WRITE-END-OF-PAGE` FLAG-14 option** (task hint for VCR 37 is wrong) and **NO standalone "I-O-STATUS-04 directive"** (audit hint for VCR 21 is wrong — I-O-STATUS-04 is a FLAG-14 option word only, per E.2 item 5 which lumps directive words and FLAG-14 option words together).

---

### CLASS A — IMPLEMENT + GATE (a real code/behavior change is required)

---

### VCR 21 — I-O status '04' setting clarified (§9.1.13.2 item 3; E.2 item 15)

- **Spec sections:**
  - §9.1.13.2 item 3 (line 11466): *"I-O status = 04. A READ statement is successfully executed but the physical record from the file is shorter than or longer than the minimum or maximum length of records allowed for the fixed file attributes for that file."*
  - §14.9.35 READ GR14 (line 29953): for a record-sequential file, if bytes read < minimum → right portion undefined; if > maximum → truncated on the right; **"the READ statement is successful, and the I-O status value for file-name-1 is set to '04'."**
  - E.2 item 15 (line 49216): the setting of '04' is *clarified to state when it is set* (it was previously in the "known errors" list as not clearly defined).
  - FLAG-14 twin: §7.3.15.4 GR4 e) I-O-STATUS-04 (line 4515) — flags a reference to a FILE STATUS item that tests for '04'.
- **Syntax / format:** none — this is a runtime status-code emission, not syntax.
- **Introduced edition & gate:** the *clarification* is a 2023 delta but the status value itself is version-invariant. Disposition = **implement the '04' emission at all editions** (a successful READ never became unsuccessful; a below-2023 program that never tested '04' is unaffected). The FLAG-14 I-O-STATUS-04 twin is the only edition-flavored surface, exposed via the `>>FLAG-14` directive (Wave D/H). No `constructs.json` introduction gate.
- **Semantics (GR-level):** on a **record-sequential** READ, after the physical record is fitted to the record area: if the physical record length ≠ the min/max of the declared record description(s) for that FD (fixed file attributes), set status **'04'** (still a *successful* read — `PrevOpWasSuccessfulRead=true`, returns the record). '04' has first digit '0' → maps to no EC (successful family). For LINE SEQUENTIAL the relevant codes are '06'/'09' (already modelled), not '04'; '04' is the record-sequential fixed/variable-length case.
- **As-built today:** `src/Cobol.Net.Runtime/IO/FileStatus.cs` defines **no '04' constant** (confirmed — the class has 00/02/04…62 but the `"04"` value is absent). `SequentialConnector.Read` (`SequentialConnector.cs:367–412`) pads/truncates the record image (`image = new string(buf,0,n).PadRight(RecordWidth,' ')`, tracks `LastReadLength`) but **never compares to the declared min/max and never sets '04'** — it unconditionally sets `Status = FileStatusCode.Success` at line 411. Greenfield grep for `"04"` across `src/Cobol.Net.Runtime/` = 0 hits. Legacy `CobolSharp.Runtime/IO/FileStatus.cs:17` has `RecordLengthMismatch="04"` (regression oracle only).
- **AUDIT DRIFT CAUGHT:** the audit row 37 says *"implement '04' … + the I-O-STATUS-04 directive (Wave D pattern)"* — **wrong**: there is no standalone I-O-STATUS-04 directive (the §7.3 directive TOC has no such entry, lines 4555+); I-O-STATUS-04 is a FLAG-14 sub-option only (GR4 e, line 4515). The implementable half is the '04' status emission; the directive half is a FLAG-14 twin, not a Wave-D directive.
- **Implementation plan:** runtime-only, no grammar. (1) Add `public const string RecordLengthShortLong = "04";` to `FileStatus.cs` (with the §9.1.13.2 item 3 cite). (2) In `SequentialConnector` expose the declared min/max record sizes (the FD `RECORD` metadata already carried for '44' write checks — see `RecordSizeViolation` sites at 291/443/445) and, in `Read`, after computing `LastReadLength`, set `Status = "04"` instead of `Success` when the fixed-record-sequential physical length is outside [min,max] (and the varying/ODO case per §9.1.13.6 item 4b '34' stays distinct). (3) No bound-node/emitter change. (4) FLAG-14 twin registered as GR4 option e for the Wave-D `>>FLAG-14` sink. Battery: greenfield file-I/O goldens (SQ family).
- **Golden:** a fixed-length record-sequential file `PIC X(80)` FD read against a physical file whose lines are 50 chars → `FILE STATUS` item = `"04"`, record still delivered, program continues (DISPLAY the status). Hand-derived: physical 50 < declared 80 → §14.9.35 GR14 → status '04', READ successful. Below-2023 negative fixture: **same output** (no edition difference — '04' is emitted at all editions); the only below-2023-vs-2023 fixture is a `>>FLAG-14 I-O-STATUS-04 ON` compile that warns on a `IF FS = "04"` reference (Wave-D golden).
- **Blast radius / hazards:** any existing SQ golden that reads a short/long record and currently expects `"00"` will flip to `"04"` — sweep `tests/**/valid` SQ goldens (and legacy `CobolSharp.Runtime` regression net — legacy already returns '04', so this *converges* greenfield to legacy, reducing diff). Watch the file-I/O flake set (SQ/IC/IX) — SOLO-rerun on failure.

---

### VCR 35 — VALUE figurative ZERO for numeric-edited → numeric literal zero, edited per PICTURE (§13.18.63 SR6; E.2 item 28)

- **Spec sections:**
  - §13.18.63 SR6 (line 23234): for a numeric-edited item, numeric literals **"shall be converted to their numeric-edited forms according to the rules for the MOVE statement"**; *"the figurative constant ZERO or ZEROES and the integer and decimal forms of the literal zero may also be specified … and shall be treated identically as the literal zero."*
  - §13.18.63 SR11 (line 23256): *"Editing characters in a picture character-string for a numeric-edited data item are used in editing of the initial value when the data item is initialized and the literal is numeric."*
  - E.2 item 28 (line 49368): figurative ZERO/ZEROES (with/without ALL) for numeric-edited *"is now treated as the numeric literal zero, such that the result is no longer left justified or potentially a simple string of zeroes."*
  - FLAG-14 twins: GR4 g) NUM-ED-ZERO-FIG-CONSTANT (line 4519) and l) VALUE-ZERO (line 4533).
- **Syntax / format:** `01 NE PIC $ZZ9.99 VALUE ZERO.` (also `ZEROS`/`ZEROES`, and `ALL ZERO`).
- **Introduced edition & gate:** genuine **2014→2023 observable behavior change**. Gate: at **≥2023** edit the numeric zero per PICTURE; **<2023** keep the current zero-fill (the "simple string of zeroes"/left-justified pre-2023 behavior). Gate mechanism = `Edition.DialectLevel >= 2023` at the `ValueInitializer` decision point (this is a value-emission difference, not an accept/reject, so it is a `DialectLevel` branch, **not** a `VersionConformancePass` reject and **not** `COBOLNET0900`).
- **Semantics (GR-level):** figurative ZERO on a numeric-edited item ⇒ take the numeric value 0 at the item's scale and run it through the same MOVE/edit compose already used for a numeric literal VALUE (`RuntimeApi.EditCompose(0, scale, EditMask, BlankWhenZero, currency, decimalComma)`). BLANK WHEN ZERO now takes effect (SR6 is a numeric literal ⇒ NOTE 2 at line 23248 applies: BWZ *does* affect the figurative-ZERO init → all spaces). `ALL ZERO`/`ALL ZEROES` treated identically (E.2 item 28 "with or without the ALL phrase").
- **As-built today (CLI-probed):** `waveG_n2.cob` = `01 NE-Z PIC $ZZ9.99 VALUE ZERO.` → **`0000000`** at `--std 2023` (probe run this session). Root cause: `ValueInitializer.FigurativeInitializer` (`ValueInitializer.cs:126–129`) returns `new string(fillChar, pic.Length)` for a numeric-edited item (fillChar `'0'` for ZERO) — the raw zero-fill; the numeric-edited *edit* path (`ValueInitializer.cs:88–90`, `EditCompose(...)`) is only reached for a **numeric literal**, never for the figurative. Confirmed: numeric-literal `VALUE 12.5` on the same PIC → `$ 12.50` (probe `waveG_n3.cob`, both 2023 and 2014).
- **AUDIT DRIFT CAUGHT:** none — audit row 39 is correct ("0000000 at BOTH 2023 AND 2014; the 2023 edited-zero result NOT produced").
- **Implementation plan:** `ValueInitializer.cs`, no grammar. In `InitializerFor`, **before** the general `FigurativeInitializer` fill (line 79), add: *if* `pic.Category is NumericEdited` *and* the raw VALUE is the figurative ZERO/ZEROES/`ALL ZERO(ES)` *and* `Edition.DialectLevel >= 2023` ⇒ return `EmitText.CsLiteral(RuntimeApi.EditCompose(Int128.Zero, pic.Scale, pic.EditMask!, item.BlankWhenZero, ctx.Data.CurrencyPicSymbol, ctx.Data.DecimalPointIsComma))`. Below 2023, fall through to the existing zero-fill. Reuse `FigurativeConstants.KindOf` to detect ZERO (it already strips the `ALL` prefix, lines 130–133). FLAG-14 twins g/l exposed to the `>>FLAG-14` sink. `constructs.json`: no introduction row (behavior branch, not accept/reject); optionally a `value-numeric-edited-figurative-zero-2023` row purely for the VERSION matrix `behavior` axis.
- **Golden:** `01 NE-Z PIC $ZZ9.99 VALUE ZERO.` DISPLAY. **≥2023 expected `$  0.00`** — hand-derivation: value 0.00; integer digits `000` → `$ZZ9`: `$` fixed, two `Z` positions are leading zeros ⇒ suppressed to spaces, `9` forces `0` ⇒ `$  0`; `.99` ⇒ `.00` ⇒ **`$  0.00`** (7 chars). **<2023 expected `0000000`** (current behavior, the negative/older fixture). Add a BWZ variant: `PIC $ZZ9.99 BLANK WHEN ZERO VALUE ZERO` → ≥2023 `       ` (7 spaces, NOTE 2), <2023 `0000000`.
- **Blast radius / hazards:** any 85/2014 golden relying on the zero-fill stays green (gated). New 2023 goldens only. Watch NC-series programs with numeric-edited VALUE ZERO under `--std 2023`.

---

### VCR 86 — numeric literals permitted for numeric-edited items (§13.18.63 SR6; E.3.3 item 43)

- **Spec sections:** §13.18.63 SR6 (line 23234, quoted above) permits numeric literals for numeric-edited items (convert per MOVE, no digit/sign truncation); SR2/SR3 (lines 23220/23228) constrain representability and sign. E.3.3 item 43 classes this as a *capability addition* (probably-not-affecting).
- **Syntax / format:** `01 NE PIC $ZZ9.99 VALUE 12.5.` — a numeric literal as VALUE for a numeric-edited PICTURE.
- **Introduced edition & gate:** **introduced 2023** (a new capability). Gate: **accept + auto-edit at ≥2023; reject below 2023 with `COBOLNET0900`** (below-2023 numeric-edited VALUE required an alphanumeric edited-image literal). Mechanism = `constructs.json` row `value-numeric-literal-numeric-edited-2023` `introducedIn:2023` + a `VersionConformancePass` reject at the numeric-literal-VALUE-on-numeric-edited bind point.
- **Semantics (GR-level):** ≥2023 — the numeric literal is edit-composed at compile time (already implemented). <2023 — reject the numeric literal on a numeric-edited item (the programmer must supply an alphanumeric edited-image literal).
- **As-built today (CLI-probed):** `VALUE 12.5` on `PIC $ZZ9.99` → `$ 12.50` at **both** `--std 2023` and `--std 2014` (probe `waveG_n3.cob`). Implemented in `ValueInitializer.cs:88–90` (`PicCategory.NumericEdited when !raw.StartsWith('"') && TryParseNumeric(...) => EditCompose(...)`), **not edition-gated**.
- **AUDIT DRIFT CAUGHT:** none — audit row 72 correct ("accepted and edited at 2023 AND identically at 2014 … NOT gated").
- **Implementation plan:** add the introduction gate in `VersionConformancePass` (reject a numeric-literal VALUE on a numeric-edited item when `DialectLevel < 2023`, `COBOLNET0900`); `constructs.json` row with `source`+`expectDiagnostic`. The ≥2023 emit path already exists — no `ValueInitializer` change. This row is the **umbrella introduction** for the VCR 34/35/36 cluster (all four are the one "numeric-edited VALUE 2023 rework"); implement as one coherent change set.
- **Golden:** `PIC $ZZ9.99 VALUE 12.5` → ≥2023 `$ 12.50`; **<2023 negative fixture** → `COBOLNET0900` "numeric literal VALUE for a numeric-edited item is a COBOL-2023 feature (§13.18.63 SR6)".
- **Blast radius / hazards:** below-2023 corpora that (incorrectly) used a numeric literal VALUE on numeric-edited will now be rejected under `--std 85/2014` — sweep the NC/legacy corpus for such entries before flipping the gate; may need a per-file `--std 2023` or a leniency-registry entry if the 85 CCVS uses it.

---

### VCR 34 — VALUE literal categories checked for numeric-edited items (§13.18.63 SR7; E.2 item 27)

- **Spec sections:** §13.18.63.3 SR7: *"If the item is of category numeric-edited and the literal is of class alphanumeric or national, the class of the literal shall conform to that of the data item; if the class is otherwise undefined, then the class of the data item shall be that of the literal."* §13.18.63.3 SR4/SR5 constrain size ("shall not exceed the size indicated by an explicit PICTURE clause"). E.2 item 27 (line 49360): alphanumeric/national VALUE literals for numeric-edited are *now checked to conform to their PICTURE and USAGE*.
- **Syntax / format:** `01 NE PIC $ZZ9.99 VALUE "$123.45".` (alphanumeric edited-image literal) or `PIC N... VALUE N"…"`.
- **Introduced edition & gate:** genuine 2023 **added validation**. Gate: apply the SR7 class/length conformance check at **≥2023** (reject nonconforming); **<2023** accept verbatim (no check). New reject **`COBOLNET1570`**, guarded `DialectLevel >= 2023` in `VersionConformancePass` (or the DataBinder VALUE path with the version predicate).
- **Semantics (GR-level):** at ≥2023, when a numeric-edited item's VALUE literal is class alphanumeric/national: (a) the literal's class must match the item's USAGE-implied class (alphanumeric literal ⇔ DISPLAY numeric-edited; national literal ⇔ national numeric-edited — cf. §14.6.11 line 7108 "numeric-edited considered national if usage is national, else alphanumeric"); (b) the literal length must equal the edited PICTURE size (`pic.Length`). Violations → `COBOLNET1570`. Below 2023 the literal is stored verbatim with no check.
- **As-built today:** `ValueInitializer.cs:96–99` stores an alphanumeric/national literal VALUE on a numeric-edited item verbatim via `RuntimeApi.StrStore(Decode(raw), pic.Length)` — **no class/length conformance check** at any edition. Audit row 70 confirms ("alnum/numeric literals accepted without a 2023 category check").
- **AUDIT DRIFT CAUGHT:** none — audit row 70 accurate.
- **Implementation plan:** add a `≥2023` SR7 check in the DataBinder/`VersionConformancePass` VALUE path for numeric-edited items: verify the literal class matches the item's class and `literal.Length == pic.Length`; emit `COBOLNET1570` on mismatch. Register `COBOLNET1570` in `Constructs.g.cs`/`ConstructRegistry.g.cs`. No emit change (the verbatim store is correct once the literal is validated). FLAG-14 twin j) VALUE-EDITING overlaps (see VCR 36).
- **Golden:** ≥2023 positive `PIC $ZZ9.99 VALUE "$  0.00"` → stores `$  0.00`; ≥2023 negative `PIC $ZZ9.99 VALUE "ABC"` → `COBOLNET1570` (class conforms but length 3 ≠ 7, or `PIC N$ZZ9.99 VALUE "abc"` national-class mismatch). <2023 fixture: `VALUE "ABC"` accepted verbatim (padded), no diagnostic.
- **Blast radius / hazards:** below-2023 goldens unaffected (gated). ≥2023 corpora with sloppy edited-image literals will newly reject — watch the numeric-edited VALUE goldens.

---

### VCR 36 — VALUE editing symbols required / auto-supplied for numeric-edited items (§13.18.63 SR11; E.2 item 29)

- **Spec sections:** §13.18.63 SR11 (line 23256) + NOTE 3 (line 23258): editing chars edit the initial value **when the literal is numeric**; for an alphanumeric/national literal the programmer is responsible for supplying the value **in edited form**. E.2 item 29 (line 49376): *"Editing symbols are now compulsorily required in the value when the value is an alphanumeric or national literal and are automatically supplied when the literal is a numeric literal."* FLAG-14 twin: GR4 j) VALUE-EDITING (line 4529) — *"A VALUE clause for a numeric-edited data item that does not contain any editing symbols and is specified as a literal shall be flagged."*
- **Syntax / format:** numeric leg `VALUE 12.5` (auto-edit); alphanumeric leg `VALUE "$ 12.50"` (must already contain the editing symbols).
- **Introduced edition & gate:** two legs. **Numeric-literal auto-supply** — already implemented at all editions (gate it under VCR 86's ≥2023 introduction). **Alphanumeric-literal "editing-symbols-required"** — this is realized as the **FLAG-14 VALUE-EDITING flag** (a directive-gated *warning*, not an always-on reject): a literal VALUE lacking editing symbols is *flagged*, not errored (§7.3.15.4 GR4 j). So the compulsory-editing rule is surfaced through `>>FLAG-14 VALUE-EDITING ON` (Wave D/H), plus the SR7 class/length reject (VCR 34, `COBOLNET1570`) catches the hard nonconformance.
- **Semantics (GR-level):** numeric literal ⇒ auto-edit (done). Alphanumeric/national literal ⇒ stored verbatim (programmer supplies edited form, NOTE 3); when `>>FLAG-14 VALUE-EDITING ON` is in effect, a literal VALUE containing no editing symbols is flagged.
- **As-built today (CLI-probed):** numeric leg `VALUE 12.5` → `$ 12.50` at both editions (auto-supplied — probe `waveG_n3.cob`). Alphanumeric leg stored verbatim (`ValueInitializer.cs:96–99`). No VALUE-EDITING flag exists (`>>FLAG-14` unrecognized — audit row 44/57).
- **AUDIT DRIFT CAUGHT:** none — audit row 71 accurate ("`$ 12.50` at BOTH 2023 AND 2014 … present for the numeric-literal leg but NOT [gated]").
- **Implementation plan:** the numeric-literal auto-supply needs only VCR 86's ≥2023 gate (no new code). The alphanumeric-literal leg = expose "numeric-edited VALUE literal without editing symbols" to the `>>FLAG-14` VALUE-EDITING sink (GR4 j) — this is the Wave D/H `>>FLAG-14` code half, not a Wave-G reject. No always-on diagnostic beyond VCR 34's `COBOLNET1570`.
- **Golden:** covered by VCR 86 (numeric auto-edit) + VCR 34 (class/length reject). FLAG-14 golden: `>>FLAG-14 VALUE-EDITING ON` over `PIC $ZZ9.99 VALUE "1234500"` (no editing symbols) → a VALUE-EDITING warning (Wave-D golden).
- **Blast radius / hazards:** merged into the VCR 34/35/86 change set — one numeric-edited-VALUE-2023 commit. No independent surface.

> **CLUSTER NOTE (VCR 34/35/36/86).** These four are ONE construct — "numeric-edited VALUE, 2023 rework (§13.18.63 SR6/SR7/SR11)". Implement as a single change set: (a) VCR 86 introduction gate (`COBOLNET0900`, numeric literal ≥2023); (b) VCR 35 figurative-ZERO edited-zero branch (`DialectLevel` gate); (c) VCR 34 SR7 class/length reject (`COBOLNET1570`, ≥2023); (d) VCR 36 numeric auto-supply (falls out of (a)) + VALUE-EDITING FLAG-14 twin. FLAG-14 twins g/j/l all land in the Wave-D `>>FLAG-14` sink.

---

### VCR 27 — MERGE prohibited in another MERGE's output procedure / a file-SORT input-or-output procedure (§14.9.24; E.2 item 20)

- **Spec sections:** §14.9.24 MERGE (the general rules / SRs — the audit cites §14.9.24). E.2 item 20 (line 49266): *"A MERGE statement is now prohibited in an output procedure of another MERGE statement or an input or output procedure of a file format SORT statement."* (Previously allowed with conflicting rules; SORT already disallowed it.)
- **Syntax / format:** none new — a static legality rule over the procedure ranges named by MERGE OUTPUT PROCEDURE / SORT INPUT|OUTPUT PROCEDURE.
- **Introduced edition & gate:** **2014→2023 newly-prohibited**. Gate: static reject at **≥2023**, new **`COBOLNET1571`**, guarded `DialectLevel >= 2023`. Below 2023 the runtime EC-SORT-MERGE-ACTIVE seam remains the (dynamic, checking-OFF) safety net.
- **Semantics (GR-level):** at compile time, for each MERGE with an OUTPUT PROCEDURE and each file-format SORT with an INPUT/OUTPUT PROCEDURE, walk the pc range of that procedure; if any statement in the range (transitively via PERFORM/GO TO into the range — the range is contiguous per `ProcedureTableBuilder`) is a MERGE, reject with `COBOLNET1571`. The prohibition is on *textual/range* containment of a MERGE inside the procedure range.
- **As-built today:** `SortBinder.cs:206–208` (`Verbs/SortBinder.cs`) explicitly **defers** the static diagnostic: comment *"a ≥2023 static diagnostic needs a procedure-range cross-pass (deferred; the runtime EC-SORT-MERGE-ACTIVE seam in CobolSort covers the dynamic case, checking OFF per COBOLNET_DESIGN §18.16)"*. `outputProc` is bound as a `(int,int)` pc range (`SortRange` → `SortBinder.cs:198–202`), so the range is already available for the cross-pass.
- **AUDIT DRIFT CAUGHT:** none — audit row 73 accurate. (Minor: audit cites §14.9.24; the prohibition text is E.2 item 20 — the §14.9.24 SR is where the reject anchors.)
- **Implementation plan:** a bind-time cross-pass, no grammar. After all procedures/statements are bound, add a `VersionConformancePass` (or a post-bind SORT/MERGE analysis) that, for each `BoundMerge.OutputProc` and each file-format `BoundSort` input/output proc range, scans the bound statements in `[start,end]` for a `BoundMerge`; on hit emit `COBOLNET1571` when `DialectLevel >= 2023`. Register `COBOLNET1571`. Reuse the `ProcedureTableBuilder` pc ranges (contiguous sections — see VCR 33). Touch: `SortBinder.cs` (drop the deferral comment), a new analysis pass, `Constructs.g.cs`/`ConstructRegistry.g.cs`.
- **Golden:** `merge_in_output_procedure_rejected` — a MERGE whose OUTPUT PROCEDURE section contains a second MERGE → ≥2023 `COBOLNET1571` "MERGE is prohibited in the output procedure of another MERGE / an input or output procedure of a file SORT (§14.9.24, COBOL-2023)". Positive control: the same nesting under `--std 2014` compiles (dynamic EC-SORT-MERGE-ACTIVE net only). Also cover MERGE inside a file-SORT INPUT PROCEDURE and OUTPUT PROCEDURE.
- **Blast radius / hazards:** SORT/MERGE bind path; watch the ST/SM/legacy sort goldens. The cross-pass must not false-fire on a MERGE that merely *follows* (is not textually inside) the procedure range — anchor strictly on the pc range, and account for a PERFORM out of the range that reaches a MERGE (spec prohibits textual containment; a PERFORM to an *external* paragraph containing MERGE is the runtime EC's job, keep the static check range-textual to avoid over-firing).

---

### VCR 68 / 69 — EXCEPTION-FILE / EXCEPTION-FILE-N optional file-connector argument form (§15.28 / §15.29; E.3.3 items 25/26)

- **Spec sections:**
  - §15.28.2 general format (line 35430): `FUNCTION EXCEPTION-FILE [(argument-1)]`.
  - §15.28.3 argument rule 1 (line 35435): argument-1 is optional; when specified, the name of a **file connector specified in an FD statement**.
  - §15.28.4 r2 (lines 35454–35466): with argument-1 — (a) if the connector has **never been opened, attempted to be opened, or otherwise accessed** → **two alphanumeric spaces**; (b) otherwise → the connector's **I-O status value** (2 chars) followed by the **file-name as spelled in the SELECT clause**.
  - §15.29 (lines 35475+): EXCEPTION-FILE-N, the national twin (same rules, national repertoire).
  - Table (lines 34197–34198): *"If optional argument-1 is specified, the file connector name specified by argument-1 is used."*
- **Syntax / format:** `FUNCTION EXCEPTION-FILE(file-connector-name)` — one optional trailing argument (already parses; the catalog rows carry `IntrinsicArity.OptionalTrailing 0..1`).
- **Introduced edition & gate:** the **argument form is introduced 2023** (E.3.3 items 25/26). Gate: accept the 1-arg form at **≥2023**; reject below 2023 with **`COBOLNET0900`**. Mechanism = `constructs.json` rows `exception-file-argument-2023` / `exception-file-n-argument-2023` `introducedIn:2023` + a `VersionConformancePass` reject when the intrinsic is bound with an argument and `DialectLevel < 2023`. The no-argument base form is already `introducedIn:2002` (DONE — audit row 91).
- **Semantics (GR-level):** ≥2023, arg form — look up the named FD file connector in the run-unit `FileRegistry`; if it has never been opened/attempted → return `"  "` (2 spaces, national twin = 2 national spaces via `NationalOf`); otherwise return `connector.Status` (2 chars) + the SELECT-spelled name (stripped of the `PROG::`/`::EXT::` emit-namespace prefix exactly as the no-arg `File()` already does). Unlike the no-arg form, the arg form is **not** about the *last exception* — it reports the *named connector's current* I-O status regardless of whether an exception occurred.
- **As-built today:** parses+binds (`IntrinsicCatalog.cs:172` EXCEPTION-FILE / `:179` EXCEPTION-FILE-N, `OptionalTrailing 0..1`), but `IntrinsicRenderer.cs:365–370` renders the 1-arg form as `EmitText.LoudValue("string", "… the 2023 optional-argument form — VCR row 68/69")` ⇒ runtime `NotImplemented` (`EmitCore.cs:75`). The no-arg form is live (`EcFunctions.File()` at `EcFunctions.cs:44–55`, `FileN()` at `:63`). Audit rows 81/91 confirm.
- **AUDIT DRIFT CAUGHT:** none — audit rows 81/91 accurate. (Note the `IntrinsicCatalog` comments already flag `introducedIn 2002` for the *catalog row* but the *argument form* is 2023 — the introduction gate must key on argument-count, not the catalog `introducedIn`.)
- **Implementation plan:** (1) a real bound node / renderer branch: `IntrinsicRenderer.cs` — when `ic.Args.Count == 1`, render `RuntimeApi.EcFn("File", <connector-name-arg>)` instead of `LoudValue`; the argument is the FD connector name (a compile-time name → pass the emit-namespaced key string, matching the `File()` prefix-strip convention). (2) Runtime `EcFunctions.File(string connectorName)` overload + `FileN(string)` in `EcFunctions.cs`: resolve `RunUnit.FileRegistry` for the named connector; return `"  "` if never opened/attempted (track an `EverAccessed` flag on `FileConnector` — set in `Open`/`DeleteFile`/any access), else `connector.Status + displayName`. (3) `VersionConformancePass` introduction gate (`COBOLNET0900`, arg form <2023). (4) `constructs.json` two rows. No grammar change (already parses). Touchpoints: `IntrinsicRenderer.cs`, `EcFunctions.cs`, `FileRegistry.cs`/`FileConnector.cs` (connector lookup + EverAccessed), `constructs.json`, `Constructs.g.cs`.
- **Golden:** positive (≥2023) — open a file, cause a status (e.g. read to AT END → '10'), `DISPLAY FUNCTION EXCEPTION-FILE(MY-FILE)` → `"10MYFILE"` (status + SELECT name); a never-opened connector → `"  "` (2 spaces). National twin via `NationalOf`. Below-2023 negative fixture: `FUNCTION EXCEPTION-FILE(MY-FILE)` under `--std 2014` → `COBOLNET0900` "the file-connector-argument form of EXCEPTION-FILE is a COBOL-2023 feature (§15.28.3)". Hand-derivation of `"10MYFILE"`: connector accessed → r2b → status '10' (AT END) + SELECT-spelled `MYFILE`.
- **Blast radius / hazards:** intrinsic renderer + EC runtime; watch the EC-function goldens and the EXCEPTION-* national twins. The EverAccessed flag must be set on *attempted* opens too (a failed OPEN still counts as "attempted" per r2a) — cover a failed-open connector returning its '35'/'37' status, not spaces.

---

### CLASS B — GATE-ONLY / PIN-TO-SPEC WITH RECORDED DETERMINATION (behavior already correct; needs a disposition + FLAG-14 twin)

---

### VCR 22 — I-O status '07' restricted to OPEN and CLOSE (§9.1.13.2 item 6; E.2 item 16)

- **Spec sections:** §9.1.13.2 item 6 (line 11476): '07' = an OPEN/CLOSE successful but a CLOSE with NO REWIND/REEL-UNIT/FOR REMOVAL, or an OPEN with NO REWIND, references a physical file on a **non-reel/unit medium**. E.2 item 16 (line 49224): the setting of '07' is **now restricted to OPEN and CLOSE**. FLAG-14 twin: GR4 f) I-O-STATUS-07 (line 4517).
- **Introduced edition & gate:** 2023 restriction. **Disposition = PIN-TO-SPEC (no behavior gate).** Rationale: in the greenfield the ONLY '07' setter is `FileRegistry.CloseReelUnit` (`FileRegistry.cs:156`, a **CLOSE** REEL/UNIT on a disk medium). Grep for `"07"` across `src/Cobol.Net.Runtime/` = exactly one hit (verified). Therefore '07' is *already* restricted to CLOSE (⊂ OPEN/CLOSE) at **all** editions — the 2023 restriction is satisfied with no code change and no `DialectLevel` branch. The only edition-flavored surface is the FLAG-14 I-O-STATUS-07 twin (Wave D/H).
- **As-built today:** `FileRegistry.cs:148–156`: `CloseReelUnit` sets `"07"` on an open sequential connector, `"42"` if not open; no other path sets '07' (READ/WRITE/START/REWRITE/DELETE never set it).
- **AUDIT DRIFT CAUGHT:** none — audit row 68 accurate ("produced ONLY by FileRegistry.cs:156 … effectively already restricted to CLOSE").
- **Implementation plan:** no code change. Record the determination in `docs/CONFORMANCE.md` (§4.2.6-adjacent behavior note): "'07' is emitted only by CLOSE REEL/UNIT on a non-reel medium — already ⊆ {OPEN,CLOSE} at all editions; the 2023 restriction (E.2 item 16) is met without a gate." Register the FLAG-14 I-O-STATUS-07 twin (GR4 f) for the `>>FLAG-14` sink. Flip VCR row 22 to done.
- **Golden:** a CLOSE REEL/UNIT on a disk file → status `"07"` (existing behavior, all editions). FLAG-14 golden: `>>FLAG-14 I-O-STATUS-07 ON` over a `IF FS = "07"` reference → warning (Wave-D).
- **Blast radius / hazards:** none (no code change).

---

### VCR 24 — I-O status '37' returnable for insufficient authority on OPEN (§9.1.13.6 item 6; E.2 item 18)

- **Spec sections:** §9.1.13.6 item 6 (lines 11558–11570): '37' permanent error; sub-case (b) *"an OPEN statement or DELETE FILE statement is attempted on a file and insufficient authority exists to access the file. The ability to detect this is processor dependent."* E.2 item 18 (line 49240): *"The OPEN statement **may** return a file status '37' for insufficient authority."* (Justification: most implementations already do; this makes them consistent.)
- **Introduced edition & gate:** **Disposition = PIN-TO-SPEC (no gate).** Rationale: (a) the spec says "may" (permitted, not mandated) and marks detection **processor-dependent**; (b) E.2 item 18 states this was already common pre-2023 — it is a *clarification that it is allowed*, not a newly-introduced behavior, so gating below-2023 to *suppress* '37' would be wrong. Keep '37' at all editions.
- **As-built today:** `FileConnector.Open` (`FileConnector.cs:123`) catches `UnauthorizedAccessException` → `FileStatusCode.PermissionDenied` ("37") at all editions; `FileRegistry.DeleteFile` (`FileRegistry.cs:349`) likewise. This matches the spec's permitted, processor-dependent detection.
- **AUDIT DRIFT CAUGHT:** none — audit row 69 accurate. The audit's alternative "gate at ≥2023" is the *wrong* choice (would suppress a spec-permitted, historically-common status below 2023); pin-to-spec is correct.
- **Implementation plan:** no code change. Record the determination in `docs/CONFORMANCE.md`: "'37' for insufficient authority on OPEN/DELETE FILE is emitted at all editions (spec §9.1.13.6 item 6b — permitted, detection processor-dependent, .NET `UnauthorizedAccessException`); E.2 item 18 is a clarification, not an introduction — no `DialectLevel` gate." Flip VCR 24 to done. No FLAG-14 twin exists for '37'.
- **Golden:** an OPEN of an access-denied file → `"37"` (existing; hard to make portable in CI — the CONFORMANCE.md determination is the deliverable, not a new golden, since detection is processor-dependent).
- **Blast radius / hazards:** none.

---

### CLASS C — PIN-TO-SPEC / DOCUMENTED DETERMINATION (already spec-correct; disposition + CONFORMANCE.md note)

---

### VCR 78 — DELETE FILE '39' leg (§9.1.13.6 item 7 / §14.9.10; E.3.3 item 35)

- **Spec sections:** §9.1.13.6 item 7 (line 11572): '39' = OPEN or DELETE FILE **unsuccessful because a conflict has been detected between the fixed file attributes and the attributes specified for that file in the source unit.** §14.9.10 DELETE FILE (Format 2) GR13–16 map 41/62/05/37. DELETE FILE Format-2 statement is DONE (audit row 94).
- **Disposition = DOCUMENTED-NON-SUPPORT (unreachable in the current host-file model).** Rationale: '39' requires comparing the *fixed file attributes* of the physical file (record size, organization, code-set persisted with the file) against the SELECT/FD-declared attributes. The greenfield host-file model stores files as plain host files with **no persisted fixed-attribute catalog** — `FileRegistry.DeleteFile` (`FileRegistry.cs:338–356`) deletes by `HostPath` after the 41/62/05/37 checks and never reads physical attributes. There is no attribute source to detect a conflict against, so '39' is not producible without fabricating an attribute store (which would be a hack — violates the no-workaround rule). Grep for `"39"` across `src/Cobol.Net.Runtime/` = 0 hits (verified).
- **As-built today:** `FileRegistry.cs:338–356` produces 41 (GR13), 62 (GR15), 05 (GR14), 37 (GR16), 00, 30 — but never 39. Confirmed.
- **AUDIT DRIFT CAUGHT:** the audit has **two conflicting rows** for VCR 78 — row 88 (PARTIAL, "add the '39' status path or document-non-support if unreachable") and row 89 (DONE, "add the '39' status leg (only genuine gap)"). They contradict on state. **Resolution: '39' is genuinely unreachable** in the current model → documented-non-support, not an "implement" gap. Reconcile the duplicate rows to one DONE-with-note.
- **Implementation plan:** no runtime change. Add a `docs/CONFORMANCE.md` §4.2.6 processor-dependent note: "DELETE FILE / OPEN I-O status '39' (fixed-file-attribute conflict, §9.1.13.6 item 7) is not produced — the host-file model carries no persisted fixed-attribute catalog to detect a conflict against; documented non-support until a physical-attribute store exists." Flip VCR 78 done-with-note. Optionally add the two 37/41 DELETE FILE goldens the audit noted as missing.
- **Golden:** none for '39' (unreachable). Add DELETE FILE goldens exercising '41' (delete an open file) and '37' (delete an access-denied file) to close the audit's "add goldens 37/41" note.
- **Blast radius / hazards:** none.

---

### VCR 33 — transfer-of-control checking now includes sections as well as paragraphs (§14.6; E.2 item 26)

- **Spec sections:** §14.6 transfer of control; E.2 item 26 (line 49352): *"Explicit and implicit transfers of control (inclusion of sections as well as paragraphs)."* (The checking "was unclear and probably not what was intended.")
- **Disposition = PIN-TO-SPEC (no gate).** Rationale: the greenfield already treats a section as a first-class transfer target. `ProcedureTableBuilder.cs:71–81` (`Binding/Procedure/ProcedureTableBuilder.cs`) builds a `SectionInfo` with a `[StartPc,EndPc]` pc range: *"A section's paragraphs are contiguous in the pc sequence, so the section IS a pc range: GO TO section transfers to its first paragraph (§14.9.17), PERFORM section runs first statement of its first paragraph through last statement of its last (§14.9.28)."* The `ResolveProcedureName` resolution order (lines 82–90+) resolves both section names and paragraph names. Sections are already transfer targets at all editions; the 2023 clarification is satisfied with no code change. There is **no FLAG-14 twin** for transfer-of-control (not in GR4 a–l).
- **As-built today:** `ProcedureTableBuilder.cs:73–81` — confirmed sections resolve to pc ranges; GO TO/PERFORM of a section works.
- **AUDIT DRIFT CAUGHT:** none — audit row 74 accurate ("sections ARE transfer targets; but there is no 2023-specific gated 'checking' behavior"). No gate needed.
- **Implementation plan:** no code change. Record the pin-to-spec determination in `docs/CONFORMANCE.md` (or the VERSION_CHANGE_REFERENCE row): "sections are transfer targets at all editions (`ProcedureTableBuilder` pc ranges); E.2 item 26 met without a gate." Flip VCR 33 done.
- **Golden:** existing GO TO section / PERFORM section goldens already cover it (no new golden required).
- **Blast radius / hazards:** none.

---

### VCR 37 — WRITE END-OF-PAGE with no END-OF-PAGE phrase: control passes to end of WRITE (§14.9.51; E.2 item 30)

- **Spec sections:** §14.9.51 WRITE; E.2 item 30 (line 49390): *"When the END-OF-PAGE condition occurs and the END-OF-PAGE phrase is not specified, control passes to the end of the WRITE statement."* (Corrected omission; implementors probably already did this.)
- **Disposition = PIN-TO-SPEC (natural default; no gate, NO FLAG-14 twin).** Rationale: a WRITE with no END-OF-PAGE phrase already completes normally and passes control to the next statement — there is no phrase to branch to, so "control passes to the end of the WRITE" *is* the natural code path. `SequentialIoBinder.cs:98–99` binds the optional EOP phrase; `SequentialIoEmitter.cs:250–256` emits the EOP branch only when the phrase is present; a no-EOP WRITE emits no branch and falls through. `StatementValidation` SR18/SR19 (`COBOLNET0860/0861`) already guard EOP phrase legality. Version-invariant.
- **AUDIT DRIFT CAUGHT:** the task hint *"pin-to-spec or FLAG-14 WRITE-END-OF-PAGE"* is **wrong** — there is **no WRITE-END-OF-PAGE option in the FLAG-14 directive** (GR4 a–l, lines 4507–4533, contains no such option; the closest is d) I-O-DECLARATIVE which is about INVALID KEY/AT END on I-O statements, not EOP). So pin-to-spec is the only correct disposition; there is no twin to wire.
- **As-built today:** confirmed via the audit anchors (`SequentialIoBinder.cs:98–99`, `SequentialIoEmitter.cs:250–256`) — a no-EOP WRITE completes naturally.
- **Implementation plan:** no code change. Record pin-to-spec in `docs/CONFORMANCE.md`: "WRITE without END-OF-PAGE, when the EOP condition occurs, falls through to the next statement (the natural default) — E.2 item 30 met, version-invariant, no FLAG-14 option exists." Flip VCR 37 done.
- **Golden:** existing WRITE ADVANCING / LINAGE goldens cover the fall-through (no new golden).
- **Blast radius / hazards:** none.

---

### VCR 14 — >>EVALUATE compiler directive, combined-condition truth corrected (§7.3.13; E.2 item 8)

- **Spec sections:** §7.3.13 EVALUATE directive GR4–GR6 (lines 4320–4330) and GR8–GR10 (4336–4350). GR6 (line 4330): *"If the END-EVALUATE phrase is reached without any WHEN phrase evaluating to TRUE, **and** without encountering a WHEN OTHER phrase, all lines of text-1 … are omitted."* E.2 item 8 (line 49124): the two end-of-EVALUATE omission rules were changed *"to ensure that the whole condition is now true only when **both** of the constituent conditions are true"* (no WHEN matched AND no WHEN OTHER present).
- **Disposition = PIN-TO-SPEC (impl already matches the 2023 AND-truth rule; version-invariant directive).** Rationale: `ConditionalCompilationProcessor.cs:87–121` implements exactly the AND semantics — WHEN OTHER emits only `f.ParentActive && !f.BranchTaken` (line ~105: OTHER fires only if nothing matched), and when there is **no** WHEN OTHER phrase, nothing is emitted at END-EVALUATE (no branch emits). That is: text is omitted precisely when (no WHEN matched) AND (no WHEN OTHER) — GR6/GR10 as written in 2023. The preprocessor directive is version-invariant (not `DialectLevel`-gated), which is correct since E.2 item 8 says "implementors probably already implemented these rules as now written." FLAG-14 twin: GR4 c) EVALUATE directive (line 4511) flags a directive containing both a WHEN and a WHEN OTHER phrase.
- **As-built today:** `ConditionalCompilationProcessor.cs:87–121` (Format 1 subject-match GR4a/b, Format 2 TRUE GR8, WHEN OTHER GR5/GR9, END-EVALUATE GR6/GR10) — confirmed matches 2023.
- **AUDIT DRIFT CAUGHT:** none — audit row 76 accurate ("fully implemented with real behavior … verify combined-condition (AND) truth matches"). Verified: it matches.
- **Implementation plan:** no code change. Record pin-to-spec in `docs/CONFORMANCE.md` / VERSION_CHANGE_REFERENCE: ">>EVALUATE END-EVALUATE omission uses the 2023 AND-truth rule (GR6/GR10); the preprocessor is version-invariant." Register the FLAG-14 EVALUATE-directive twin (GR4 c) for the `>>FLAG-14` sink. Flip VCR 14 done.
- **Golden:** a `>>EVALUATE`/`>>WHEN`/`>>WHEN OTHER`/`>>END-EVALUATE` fixture where no WHEN matches and a WHEN OTHER exists → OTHER text emitted; and no-match-no-OTHER → nothing emitted (existing conditional-compilation goldens; extend if a combined-condition case is absent).
- **Blast radius / hazards:** none.

---

### VCR 17 — figurative constant ALL where the data-item length is unspecified: now defined (§8.3.3.6.4 GR3; E.2 item 11)

- **Spec sections:** §8.3.3.6.4 GR3 (lines 6356–6371): when a figurative constant's string length is **not** specified by context — a) in a concatenation expression → length 1; b) other than `ALL literal-1` → length 1; c) [ALL literal-1] the length is the length of literal-1. GR2 (line 6352): the fixed-length/VALUE repeat-and-truncate rule. E.2 item 11 (line 49156): figurative ALL where the item length is unspecified — *the length is now defined* (previously undefined / compiler error). FLAG-14 twin: GR4 k) VALUE-FIG-CON-NO-LENTH (line 4531) — flags a figurative constant in the VALUE of a data item with no specified length.
- **Disposition = PIN-TO-SPEC (already well-defined at 2023).** Rationale: for a dynamic/unspecified-length item, `ValueInitializer.InitializerFor` (`ValueInitializer.cs:44–55`) already applies GR3b — the comment at line 48 states *"A figurative VALUE other than `ALL literal` has length ONE (§8.3.3.6.4 GR3b)"* — a bare figurative fills a single fill character; an `ALL "literal"` on a dynamic item routes to `RuntimeApi.DynStore(Decode(dv), limit)` (GR3c — the literal's own length). The pre-2023 "undefined/compiler error" is superseded by the defined behavior, which the code already produces. Version-invariant (a definition, not a behavior change that needs suppressing below 2023).
- **As-built today:** `ValueInitializer.cs:44–55` (dynamic-length VALUE path) — confirmed GR3b single-fill for bare figurative; ALL-literal → DynStore.
- **AUDIT DRIFT CAUGHT:** none — audit row 77 accurate. Minor precision: the applicable sub-rule is GR3**b** (bare figurative → length 1) and GR3**c** (ALL literal → literal length); the audit's "§8.3.3.6.4 GR3b" covers the bare case, GR3c the ALL case.
- **Implementation plan:** no code change. Record pin-to-spec in `docs/CONFORMANCE.md`: "figurative VALUE on an unspecified-length (dynamic) item is defined per §8.3.3.6.4 GR3b/c — bare figurative → 1 char, ALL literal → literal length; already implemented." Register the FLAG-14 VALUE-FIG-CON-NO-LENTH twin (GR4 k) for the `>>FLAG-14` sink. Flip VCR 17 done.
- **Golden:** `01 D PIC X DYNAMIC LENGTH VALUE SPACE.` → 1 space; `01 D2 ... VALUE ALL "AB".` → `"AB"` (2 chars). Existing DYNAMIC LENGTH goldens (P12) likely cover; extend if the ALL-literal-on-dynamic case is absent.
- **Blast radius / hazards:** none.

---

### VCR 20 / 49 — general case mappings DELETED / ADDED affecting UPPER-CASE / LOWER-CASE (§15.97.4 / §15.57.4; E.2 item 14 / E.3.3 item 6)

- **Spec sections:** §15.97.4 r4 (line 38936): *"When a locale is not in effect, the implementor defines the correspondence of lowercase letters to uppercase letters."* GR6 (line 38940): no-correspondence letters are unchanged. §15.57.4 is the LOWER-CASE mirror. E.2 item 14 (line 49196): case mappings **deleted** — `(0131,0069)` LATIN SMALL LETTER DOTLESS I, `(03C2,03C3)` GREEK SMALL LETTER FINAL SIGMA. E.3.3 item 6: mappings **added** affecting case-insensitive match/UPPER/LOWER-CASE.
- **Disposition = PIN-TO-SPEC with a recorded determination (implementor-defined; no gate, no tuned table).** Rationale: **§15.97.4 r4 makes the case correspondence implementor-defined when no locale is in effect** — exactly the greenfield's situation. Our determination: the correspondence is the **.NET invariant Unicode case tables** (`CobolIntrinsics.Text.cs:107` `UpperCase => s.ToUpperInvariant()`, `:110` `LowerCase => s.ToLowerInvariant()`; the anycase folds in FindString/Substitute use `ToLowerInvariant`). Because the spec delegates the mapping to the implementor absent a locale, the enumerated 2023 deletions/additions are **not binding on us** — building a bespoke mapping table to match the annex's specific code points would add complexity for a corner case the spec explicitly leaves to the implementor. Version-invariant (no `DialectLevel` branch — the implementor-defined mapping is the same across editions).
- **As-built today:** `CobolIntrinsics.Text.cs:107/110` — `ToUpperInvariant`/`ToLowerInvariant`; .NET-invariant tables (which, e.g., map `ı` U+0131→`I` U+0049 and `ς` U+03C2→`Σ` U+03A3), not the enumerated 2023 annex table.
- **AUDIT DRIFT CAUGHT:** none — audit rows 78/79 accurate ("uses .NET invariant tables, not the enumerated 2023 additions; decide pin-to-spec vs a tuned mapping table"). The §15.97.4 r4 "implementor-defined" delegation is the decisive citation the audit didn't quote — it makes pin-to-spec unambiguously correct.
- **Implementation plan:** no code change. Add a `docs/CONFORMANCE.md` determination row: "UPPER-CASE/LOWER-CASE case correspondence (absent a locale) is implementor-defined per §15.97.4 r4 / §15.57.4; WiseOwl COBOL uses the .NET invariant Unicode case tables. The 2023 annex E.2 item 14 deletions `(0131,0069)`/`(03C2,03C3)` and E.3.3 item 6 additions are not separately tuned — the implementor-defined mapping supersedes them for our runtime." Flip VCR 20/49 done-with-note.
- **Golden:** existing UPPER-CASE/LOWER-CASE goldens (ASCII + a national sample) already characterize the mapping; no new golden (the determination is the deliverable). Optionally a characterization test pinning the .NET-invariant result for `ı`/`ς` so a future .NET upgrade that changes the table is caught.
- **Blast radius / hazards:** none for the current battery. If a locale facility (LC_CTYPE) is later implemented, GR2/GR3 (locale-driven) supersede GR4 and this determination must be revisited.

---

## Wave G roll-up (disposition summary + diagnostic ledger)

| VCR | Row | Disposition | New diag / gate |
|---|---|---|---|
| 21 | I-O status '04' on READ | **Implement** (runtime, all editions) | none (FLAG-14 twin I-O-STATUS-04) |
| 35 | figurative ZERO numeric-edited → edited zero | **Implement + `DialectLevel≥2023` branch** | none (FLAG-14 twins NUM-ED-ZERO-FIG-CONSTANT / VALUE-ZERO) |
| 86 | numeric literal permitted numeric-edited | **Gate (introduction)** | `COBOLNET0900` <2023 + constructs row |
| 34 | VALUE literal-class check numeric-edited (SR7) | **Implement + gate ≥2023** | **`COBOLNET1570`** |
| 36 | editing symbols auto-supply / required | **Implement (numeric leg via 86) + FLAG-14** | none (FLAG-14 twin VALUE-EDITING) |
| 27 | MERGE-in-output/SORT-proc prohibition | **Implement (static cross-pass) + gate ≥2023** | **`COBOLNET1571`** |
| 68/69 | EXCEPTION-FILE(connector) arg form | **Implement (bound node + runtime) + gate** | `COBOLNET0900` <2023 + 2 constructs rows |
| 22 | I-O status '07' restricted OPEN/CLOSE | **Pin-to-spec** (already restricted) | none (FLAG-14 twin I-O-STATUS-07) |
| 24 | I-O status '37' on OPEN authority | **Pin-to-spec** (permitted, processor-dependent) | none |
| 78 | DELETE FILE '39' | **Documented-non-support** (unreachable) | none |
| 33 | transfer-of-control incl. sections | **Pin-to-spec** (already pc ranges) | none |
| 37 | WRITE EOP no phrase | **Pin-to-spec** (natural fall-through; NO FLAG-14 twin) | none |
| 14 | >>EVALUATE combined-condition | **Pin-to-spec** (matches 2023 AND-truth) | none (FLAG-14 twin EVALUATE directive) |
| 17 | figurative ALL unspecified length | **Pin-to-spec** (defined GR3b/c) | none (FLAG-14 twin VALUE-FIG-CON-NO-LENTH) |
| 20/49 | case mappings deleted/added | **Pin-to-spec** (§15.97.4 r4 implementor-defined) | none; CONFORMANCE.md note |

**Diagnostic codes this wave proposes (from the next free `COBOLNET1570`; sibling waves take 1572+, reconcile at implementation time):** `COBOLNET1570` = VALUE numeric-edited literal-class/length nonconformance (§13.18.63 SR7, ≥2023); `COBOLNET1571` = MERGE prohibited in another MERGE's output procedure / a file-SORT input-or-output procedure (§14.9.24, ≥2023). Introduction gates (VCR 86, 68/69) reuse the shared `COBOLNET0900`.

**Cross-wave hand-offs:** all FLAG-14 twins (GR4 a–l, §7.3.15.4) are the Wave D/H `>>FLAG-14` directive code half — Wave G only exposes each construct's presence to that sink. Six CONFORMANCE.md determination rows (VCR 22/24/33/37/17/20-49-and-78) land in `docs/CONFORMANCE.md` (Wave H owns the file; Wave G supplies the rows). The VCR 34/35/36/86 cluster is ONE change set ("numeric-edited VALUE 2023 rework, §13.18.63 SR6/SR7/SR11"); implement together to avoid four partial passes over `ValueInitializer.cs`.

---

## Wave H code half — recognize-and-name non-support + §4.2.6 warning band (VCR 38/39/95)

## Wave H (code half) — recognize-and-name the documented-non-support facilities (MCS · commit/rollback · VALIDATE) + the reusable recognize-and-name warning band

> **⛔ CITATION CORRECTION (2026-09-06, kb/Work PB709) — THIS SECTION MINTED A WRONG CLAUSE AND IT PROPAGATED.**
> Everywhere below that calls this "the §4.2.6 warning band" or calls a VALIDATE operand rule
> "§4.2.6-excused", read the band as covering TWO licences and not one. §4.2.6 governs Annex A.3
> PROCESSOR-DEPENDENT elements and is what MANDATES the compile-time warning — that is MCS (A.3 item 4) and
> commit/rollback (A.3 items 6-7), and it is also what expressly does not require diagnosing syntax errors
> within their unsupported syntax. **VALIDATE is Annex A.4.14 and appears nowhere in Annex A.3**, so §4.2.6
> mandates nothing about it and excuses nothing about it: its licence is §4.2.7 plus A.4.1, under which the
> element is simply not part of this implementation and the obligation is the user documentation
> (`docs/CONFORMANCE.md` §4 item 3 and §5 row A.4.14). Warning rather than refusing VALIDATE is a documented
> CHOICE. The annex is now DATA on the descriptor (`DiagnosticDescriptor.Annex`), the licensing § is derived
> from it (`PostureClause`), and `DeclinedFacilityAnnexDriftTests` fails the build if the two disagree — so
> take the clause from the descriptor, never from the prose below.

> **STATUS: spec-first anchor re-scout, trusted over the phase plan at implementation time.** Verified against `specs/ISO_COBOL.md`, the as-built code, and CLI probes of the built `cobol.exe` (Debug/net10.0). Scope: turn the three facilities that today emit a *generic* diagnostic into *named* §4.2.6/§4.2.13 non-support diagnostics, plus formalize the reusable warning helper the SCREEN SECTION landing (COBOLNET1560) established. **These are NOT introduction gates (no 0900, no `constructs.json` row) — they are bind-site facility warnings, exactly like SCREEN.**

> **⛔ MECHANISM CORRECTION (2026-07-19, empirical — SUPERSEDES the MCS/VALIDATE recognition seam below; DEVLOG 903).** The recommended seam — an **IDENTIFIER-led** `statement` alternative gated by a `{facilityWord(...)}?` semantic predicate (`| {facilityWord("RECEIVE")}? mcsFacilityStatement` where `mcsFacilityStatement : IDENTIFIER (~DOT)*`) — was IMPLEMENTED and **empirically breaks the parser**: adding any IDENTIFIER-led alternative to `statement` **poisons ANTLR's ALL(*) boolean-factor prediction DFA** and regresses `COMPUTE R = B-NOT A.` to *"no viable alternative at input 'A'"* at **all** editions (this is the DEVLOG-621 DFA-fragility class documented at `Core/CobolExpressions.g4:154-159`). Confirmed by three variants (original position, moved-to-end, `~DOT`→`dataReference*`), all identical; reverting restored green. The scout's "additive IDENTIFIER-led `statement` alternatives are low-risk" is **FALSE** — the low-risk claim holds only for **keyword-token-led** alternatives (RAISE/ALLOCATE/INVOKE coexist fine precisely because they lead with a distinct token, never IDENTIFIER).
>
> **Corrected singular mechanism (matches RAISE):** (1) add real **lexer keyword tokens** `RECEIVE`/`SEND`/`VALIDATE` to `Core/CobolLexer.g4`; (2) admit them to the **`cobol-words.json` nameSlot** funnel (regenerate `CobolWords.g4` + `CobolWordsDriftTests`) so they stay legal user-words where unreserved — this is EXACTLY how the non-monotonic reservation of `RAISE`/`RESUME`/`SCREEN` is handled, so the §1615 "unconditional token breaks `01 VALIDATE PIC X` at `--std 85`" objection is **resolved by cobolWord**, not a reason to avoid tokens; (3) a **keyword-led** `statement` alternative `{facilityWord(...)}? (RECEIVE | SEND) (~DOT)*` — the predicate now narrows firing to the reserved-as-facility editions but sits on a **distinct-token** lead, so it is unreachable during arithmetic/boolean prediction and cannot poison the DFA; (4) the §8.9 reserved-word funnel already rejects the token as a user-word at reserved editions (0901), unchanged. **COMMIT/ROLLBACK (VCR 39) keep the diagnostic-layer refinement below — no grammar rule — and are UNAFFECTED.** This makes Wave H a genuine lexer-token change (shared lexer ⇒ full legacy guard mandatory) best executed **inside the grammar batch** alongside the PICTURE-EDITING `EDITING` token + PERFORM-Fmt3 `FINALLY`/`LOCATION` tokens, which follow the identical token+cobolWord pattern and share the one legacy guard. **Deferred to that focused batch pass; the IDENTIFIER-predicate attempt was reverted to green (`6ca8d184`).**

### 0. Shared mechanism — the reusable §4.2.6 / §4.2.13 non-support warning helper (the COBOLNET1560 band)

**Spec sections:**
- §4.2.6 (spec :2431-2437) — processor-dependent elements. :2437 is the load-bearing sentence: *"An implementation shall provide a warning mechanism at compile time to indicate use of syntactically-detectable processor-dependent language elements not supported… it is not required to diagnose syntax errors within this unsupported syntax. The implementor is not required to produce executable code when unsupported processor-dependent language elements are used."* → a **WARNING** (not a fatal error), the program may still compile, the facility is a no-op, and we only need to recognize *enough* to name it — we are **explicitly excused from parsing the full facility grammar correctly**.
- §4.2.7 (:2440-2453) — optional elements: unsupported optional elements shall be identified in user documentation (that is `docs/CONFORMANCE.md`). No separate mandatory warning-mechanism sentence here (the warning obligation for VALIDATE rides on §4.2.13).
- §4.2.13 (:2505-2511) — obsolete elements: :2511 *"An implementation shall provide a warning mechanism that optionally may be invoked by the user at compile time to indicate use of an obsolete element."* Governs VALIDATE (obsolete, F.2 item 5).
- §4.2.16 (:2538-2539) — user documentation requirement that `docs/CONFORMANCE.md` already satisfies.
- §14.6.13.1.1 license (:24501) — *"The implementor is not required to raise any exception conditions for level-3 exception-names that are associated with optional language elements or processor-dependent language elements that the implementor has not implemented…"* → **no EC-MCS-*, EC-FLOW-{COMMIT,ROLLBACK,APPLY-COMMIT}, or EC-VALIDATE runtime engine is required.** Recognizing-and-naming the *statement* is the whole obligation.

**As-built mechanism today:** `EditionContext.Warning(code, message)` (`src/Cobol.Net.Compiler/Binding/EditionContext.cs:85`) appends `"warning {code}: {message}"` to the non-failing `Warnings` channel, surfaced by `CompilerDriver` on every result, printed to stderr, never fails the compile. SCREEN is the one live caller (`DataBinder.cs:298-300`, COBOLNET1560). This IS the reusable mechanism — **do not invent a parallel `Lenient()`/`Unsupported()` method (`feedback_singular_pattern`)**; call `Edition.Warning` directly at each facility site with the SCREEN message shape: `"the <FACILITY> (ISO §<x>) is <a processor-dependent element (§4.2.6) | an obsolete optional facility (§4.2.13/§4.2.7)> that is not supported — it is accepted but produces no <effect> (see docs/CONFORMANCE.md §4)"`. The ONLY optional refactor worth doing: a 3-line convenience wrapper `EditionContext.UnsupportedFacility(string code, string name, string iso, string effect)` that formats that exact template, so the three new sites + the SCREEN site read identically — acceptable under singular-pattern *only if* SCREEN is migrated onto it in the same change set (else leave SCREEN as-is and inline the three).

**Diagnostic band:** ⛔ **STALE — the original claim ("next free 1570; Wave H takes 1570/1571/1572; siblings start
at 1573") is dead on both halves: 1570–1577 were all consumed by later P13 waves, AND six sibling sections in this
same doc independently claimed the same 1570+ numbers (see the top banner).** Wave H needs THREE fresh codes
(MCS / commit-rollback / VALIDATE) allocated from the plan §0 next-free at implementation time.

**The one cross-cutting hazard (applies to all three):** _[⛔ **SUPERSEDED by the MECHANISM CORRECTION banner at the top of this section** — the "do NOT add hard lexer tokens; use an IDENTIFIER-led predicated `statement` alternative" conclusion below is empirically wrong (it poisons the boolean-factor DFA). The corrected mechanism IS lexer tokens + cobolWord nameSlot + a keyword-led alternative. The non-monotonic-reservation facts stated here remain accurate; only the conclusion drawn from them is wrong.]_ the facility words are **NOT lexer tokens** — `grep` of `CobolLexer.g4` finds no `COMMIT/ROLLBACK/RECEIVE/SEND/VALIDATE/MESSAGE` rule; they lex as `IDENTIFIER` and their reserved-ness is decided post-parse by the `ReservedWords` table. **Do NOT add hard lexer tokens** for them: reservation is non-monotonic across editions (VALIDATE is a *user word* at '85; RECEIVE/SEND are user words at 2002/2014; COMMIT/ROLLBACK/MESSAGE-TAG are user words at 85/2002/2014), so an unconditional token would break `01 VALIDATE PIC X` at `--std 85`, `01 RECEIVE PIC X` at `--std 2002`, etc. The recognition MUST be edition-gated to *exactly* the editions where the word is reserved-as-facility. The single source of truth for that is the reserved-word table itself.

**Recommended recognition seam** (shared by MCS + VALIDATE; commit/rollback takes a variant — see §2): add to `CobolParserCoreBase` (`src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs`) one helper, table-driven off the SSOT so no per-word edition math leaks into the grammar:

```csharp
// True when the current token spells reserved-as-facility keyword `kw` at the targeted edition —
// i.e. it can only be the (unsupported) facility, never a user-defined word. Read-only; safe for
// ANTLR's repeated prediction calls. Uses the SAME table (ReservedWords.Find(...).IsReservedAt)
// the §8.9 funnel uses (VersionConformancePass), so recognition and reservation never diverge.
protected bool facilityWord(string kw) =>
    CurrentToken is { } t
    && string.Equals(t.Text, kw, StringComparison.OrdinalIgnoreCase)
    && (ReservedWords.Find(kw)?.IsReservedAt(Edition.Year) ?? false);
```
(`ReservedWords.Find` is static in `Cobol.Net.Editions`, already referenced by the frontend; `ReservedWordEntry.IsReservedAt(int)` is the same predicate `ReservedWordSet.RejectsAt` uses at `VersionConformancePass.cs:1543`.)

**Every grammar/rule change below is a SHARED-PARSER change** (the generated ANTLR parser is shared with the frozen legacy `CobolSharp.Compiler`). Adding `statement` alternatives is *additive* (new context types the legacy binder never visits — its statement dispatch has a default fallthrough), so it is low-risk, **but a FULL legacy guard run is mandatory** (`scripts/guard-fast.sh` is NOT sufficient — it omits the characterization gate + legacy CliExitCodeTests; run the full legacy conformance + CliExitCode suites, `feedback_guard_fast_not_ci_complete`). Because `facilityWord("RECEIVE"/"SEND")` fires at `--std 85` and the legacy NIST corpus targets '85, the open
question was whether any legacy guard program contains a bare `RECEIVE`/`SEND`/`CD` communication statement.

> ✅ **RESOLVED 2026-07-19 — the risk is CLEARED, no action needed.** The CM-series IS physically present
> (`tests/nist/programs/CM{101,102,103,104,105,201,202,303,401}M.cob`, 9 programs) and they DO contain bare
> `RECEIVE`/`SEND` statements (e.g. `RECEIVE CM-INQUE-1 MESSAGE INTO INCOMING-MSG`, `SEND CM-OUTQUE-1 FROM MSG-70
> WITH EMI`) — **but all 9 are marked `pending` / `cataloged (not asserted)` in `tests/nist/corpus.tsv` and none
> has a golden in `tests/nist/valid/`, so none is executed by the guard.** Adding the `RECEIVE`/`SEND` lexer
> tokens is therefore legacy-safe. (Scanned separately: `VALIDATE`, `EDITING`, `FINALLY`, `LOCATION` occur in the
> corpus only inside comment lines, string literals, or as substrings of hyphenated user words such as
> `SEND-SWITCH` / `RECEIVE-ECHO-AND-LOG` / `EXCEPTION-LOCATION-N`, which lex as single IDENTIFIERs — no bare-word
> collision. `COMMIT` appears as a deliberate user word in `tests/conformance/negative/user-word-commit.cob`
> (`reject-at: 2023`), which is exactly why COMMIT/ROLLBACK take the diagnostic-layer route with NO grammar rule.)

---

### VCR 38 — MCS asynchronous messaging (SEND / RECEIVE / MESSAGE-TAG) — COBOLNET1570 (§4.2.6, A.3 item 4)

**Spec sections:**
- A.3 item 4 (:40056 list; item text at spec offset shown by the A.3 grep — *"The asynchronous messaging facility is dependent on the capability of a processor to allow run units to communicate with each other."*) → **processor-dependent**, so §4.2.6's mandatory compile-time warning applies.
- E.3.2 item 1 (:49409) — *"Asynchronous messaging. A method of allowing communication between run units via messages is provided…"* = the 2023 introduction proof (the facility that re-added SEND/RECEIVE/MESSAGE-TAG after the 85 Communication Module was dropped in 2002).
- E.3.2 item 3 (:49415) — the EC family: EC-MCS, EC-MCS-ABNORMAL-TERMINATION, EC-MCS-IMP, EC-MCS-INVALID-TAG, EC-MCS-MESSAGE-LENGTH, EC-MCS-NO-REQUESTOR, EC-MCS-NO-SERVER, EC-MCS-NORMAL-TERMINATION, EC-MCS-REQUESTOR-FAILED. EC-MCS is a level-2 name (:24501 list). Per the §14.6.13.1.1 license (:24501) **none must be raised** — no engine.
- §8.9 reservations: `RECEIVE` / `SEND` are `r85=true, r2002=false, r2014=false, r2023=true`; `MESSAGE-TAG` is 2023-only; `MESSAGE` is 85-only (`ReservedWords.Table.cs:350/394/275/274`, `reserved-words.json:3038/3434/2363/2354`).

**Syntax / format:** the full 2023 MCS statement grammar is processor-dependent and §4.2.6 excuses us from parsing it. **Minimal recognition** = the leading reserved verb + swallow the operand tail to the sentence period. The `sentence : statement+ DOT` rule (`CobolParserCore.g4:571`) guarantees the *only* DOT inside a procedure body is the sentence terminator (comment at :570), so a `(~DOT)*` swallow is safe and never crosses a sentence boundary (precedent: `authorContent : ~DOT+` at :277).

**Introduced edition & gate:** re-introduced 2023 (E.3.2 item 1); reserved-as-facility at 85 (85 Communication Module) and 2023. The `facilityWord("RECEIVE")`/`facilityWord("SEND")` predicate fires at exactly `{85, 2023}` because that is where `IsReservedAt` is true — no explicit `is2023()` needed. This is *not* a 0900 introduction gate; it is a non-support warning that fires wherever the facility is syntactically present. Below/at editions where the word is a user data-name (2002/2014), the predicate is false and the word stays an identifier (continuity preserved).

**Semantics (GR-level):** none at runtime — the statement produces no code (§4.2.6 last sentence). Bind emits the COBOLNET1570 warning and returns `BoundNop` (`BoundTree.cs:483`). No EC-MCS-* is raised (§14.6.13.1.1 license). The program otherwise compiles and runs.

**As-built today:** `RECEIVE TAG MESSAGE INTO MSG.` at `--std 2023` → CLI probe:
```
waveH_receive.cob(10,20): error COBOL0001: no viable alternative at input 'RECEIVETAG'
waveH_receive.cob(10,20): error COBOL0001: cannot parse construct near 'TAG'
```
No facility grammar; RECEIVE/SEND/MESSAGE-TAG exist only as reserved-word entries. **Audit-accurate.**

**AUDIT DRIFT CAUGHT:** none for the behavior — audit's "generic COBOL0001" is confirmed. One **precision correction**: the audit cites "§A.3 item 4 (processor-dependent)" but omits that the CONFORMANCE.md row 4 already cites **E.3.2** as the section; keep both — A.3 item 4 is the *disposition* anchor (processor-dependent list), E.3.2 item 1 is the *introduction* anchor. Also note the EC family lives at E.3.2 **item 3**, not with item 1.

**Implementation plan:** _[⛔ the grammar step below is SUPERSEDED — an IDENTIFIER-led alternative poisons the DFA (see the correction banner). Corrected: add `RECEIVE`/`SEND` lexer tokens + cobolWord nameSlot rows, then `| {facilityWord("RECEIVE")}? (RECEIVE | SEND) (~DOT)*` — a **keyword-led** alternative. The binder/warning/golden steps below are unchanged.]_
- Grammar (`CobolParserCore.g4`): ~~add to `statement` (:606-659), *before* the generic fallthrough:~~
  ```
  | {facilityWord("RECEIVE")}? mcsFacilityStatement
  | {facilityWord("SEND")}?    mcsFacilityStatement
  ```
  ~~and a new rule: `mcsFacilityStatement : IDENTIFIER (~DOT)* ;` (one rule serves both verbs; the leading IDENTIFIER is RECEIVE/SEND). ADDITIVE ⇒ full legacy guard.~~ (see corrected keyword-led form above)
- Parser base (`CobolParserCoreBase.cs`): add `facilityWord` (above).
- Bound node: reuse `BoundNop`; no new node.
- Binder dispatch (`StatementBinder.cs:261` `BindStatementCore`): add `_ when s.mcsFacilityStatement() is { } mcs => Ec.BindUnsupportedFacility(mcs, "COBOLNET1570", "the asynchronous messaging facility (SEND/RECEIVE, ISO E.3.2 / Annex A.3 item 4)", "message I-O"),` where `BindUnsupportedFacility` calls `Ctx.Edition.Warning(code, template)` and returns `new BoundNop()`. (Place the helper on `EcBinder` or `StatementBinder` — it needs only the `EditionContext`.)
- `constructs.json`: **none** (not an introduction gate).
- `docs/CONFORMANCE.md`: §4 row 1 already lists MCS; append COBOLNET1570 to it and to A.3 row 4's Note ("named COBOLNET1570 at the SEND/RECEIVE site").

**Golden** (`tests/conformance/…/mcs_nonsupport.cob`):
```
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MCSNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG PIC X(20) VALUE "HELLO".
       01 TAG PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BEFORE".
           RECEIVE TAG MESSAGE INTO MSG.
           SEND TAG FROM MSG.
           DISPLAY "AFTER".
           STOP RUN.
```
Hand-derived expected **stdout** (facility is a no-op; surrounding DISPLAYs run):
```
BEFORE
AFTER
```
Expected **stderr** (order-independent; two warnings, one per statement):
```
warning COBOLNET1570: the asynchronous messaging facility (SEND/RECEIVE, ISO E.3.2 / Annex A.3 item 4) is a processor-dependent element (§4.2.6) that is not supported — it is accepted but produces no message I-O (see docs/CONFORMANCE.md §4)
warning COBOLNET1570: …
```
Continuity fixture (`mcs_userword_2014.cob`, `--std 2014`, must compile clean — no warning): `01 RECEIVE PIC X(4). … MOVE "A" TO RECEIVE.` proving RECEIVE stays a user word where unreserved.

**Blast radius / hazards:** `(~DOT)*` greedily swallows the rest of a *multi-statement* sentence (`RECEIVE … SEND … .` collapses into one swallow) — tolerable (§4.2.6 excuses syntax diagnosis) but means a trailing valid statement in the same sentence is silently dropped; document that unsupported-facility statements should terminate their sentence. Watch: legacy CM-series parse (see §0), the `boolExprAhead`/`retryPhraseAhead` predicate neighbors (unaffected — different tokens), and conformance suite `statement`-dispatch snapshot.

---

### VCR 39 — commit / rollback facility (COMMIT / ROLLBACK) — COBOLNET1571 (§4.2.6, A.3 items 6–7)

**Spec sections:**
- A.3 items 6 & 7 (from the A.3 grep) — item 6 *"The commit and rollback facility is dependent upon the capabilities of the processor and its storage devices,"* item 7 *"The devices which allow commit and rollback are dependent…"* → **processor-dependent**, §4.2.6 warning applies.
- E.3.2 item 2 (:49411) — introduction proof: *"Commit and rollback facility. …permanently commit file changes… ability to rollback changes…"* (2023).
- E.3.2 item 3 (:49417) — EC-FLOW-APPLY-COMMIT, EC-FLOW-COMMIT, EC-FLOW-ROLLBACK. EC-FLOW is level-2 (:24501); none required to be raised (§14.6.13.1.1 license).
- §8.9: `COMMIT` and `ROLLBACK` are `r85=false, r2002=false, r2014=false, r2023=true` (2023-only reserved — `ReservedWords.Table.cs:84/381`, `reserved-words.json:644/3317`).

**Syntax / format:** both are **bare-verb** statements (`COMMIT` / `ROLLBACK`, no operands) in the 2023 facility. That is the crux distinguishing them from MCS/VALIDATE.

**Introduced edition & gate:** 2023-only reserved. `facilityWord("COMMIT")`/`facilityWord("ROLLBACK")` is true only at `--std 2023`. Not a 0900 gate.

**As-built today:** `COMMIT.` at `--std 2023` → CLI probe:
```
error COBOLNET0901: 'COMMIT' is a reserved word in COBOL-2023 and cannot be used as a user-defined word (ISO 8.9)
```
**This differs from MCS/VALIDATE**: `COMMIT.` (bare word + period) parses *successfully* as a **paragraph-name definition** (`paragraphDefinition : paragraphName DOT sentence*`, :589; `paragraphName : {IsAtLineStart()}? procedureName`, :593), then the §8.9 reserved-word funnel (`VersionConformancePass.cs:1543-1562`) rejects the paragraph name COMMIT with COBOLNET0901. So it is a *reserved-word* error, **not** COBOL0001 — audit-accurate.

**AUDIT DRIFT CAUGHT:** none — audit correctly states COMMIT → COBOLNET0901 (reserved-word error, not a facility name) and that no COMMIT/ROLLBACK statement grammar exists.

**Implementation plan — recommend the DIAGNOSTIC-LAYER refinement (NOT a grammar rule) for this facility, unlike MCS/VALIDATE:**

Rationale: because `COMMIT.` / `ROLLBACK.` already *parse cleanly* (as paragraph names) and already reach a *named* diagnostic (0901), adding a `commitFacilityStatement` grammar alternative would fight the intrinsic **paragraph-name-vs-bare-statement ambiguity** (`COMMIT DOT` is viable both as `paragraphName DOT` and as `statement DOT` under `sentence`). That ambiguity is genuinely hard for ANTLR to resolve on a bare word and is unnecessary risk. Instead, refine the *existing* 0901 site: when the reserved word being funneled is a commit/rollback facility keyword used in **statement/paragraph-leading position** at 2023, emit the named **COBOLNET1571** *warning* in place of the 0901 error.

- Touchpoint: `VersionConformancePass.cs` `VisitCobolWord` at the reserved-word emit (`:1543-1563`). Add, *before* the generic 0901 `Report`, a guard: `if ((word == "COMMIT" || word == "ROLLBACK") && IsStatementLeadingPosition(ctx)) { _p._sink.Report(new EditionDiagnostic("COBOLNET1571", EditionSeverity.Warning, "commit-rollback-nonsupport", "the commit and rollback facility (COMMIT/ROLLBACK, ISO E.3.2 / Annex A.3 items 6–7) is a processor-dependent element (§4.2.6) that is not supported — it is accepted but performs no transaction control (see docs/CONFORMANCE.md §4)", "", "ISO §4.2.6")); return base.VisitChildren(ctx); }` — Warning severity ⇒ non-failing ⇒ the program compiles and the empty COMMIT "paragraph" runs as a no-op. `IsStatementLeadingPosition` = the enclosing ctx is a `ParagraphNameContext`/`ProcedureNameContext` whose parent is `ParagraphDefinitionContext` (the bare-word-sentence shape), NOT a data-name or operand slot — mirror the existing EXCEPTION-OBJECT position walk at `:1534-1542`.
- **No grammar change ⇒ no legacy guard needed for this facility** (this is the low-risk advantage; the reserved-word table is unchanged, only the emit-site severity/code for a 2023-only word is refined). Still run the standard conformance battery.
- `constructs.json`: none.
- `docs/CONFORMANCE.md`: rows 6–7 already list commit/rollback; append COBOLNET1571.

**Fallback** (if a future need for a real `commitStatement` node arises, e.g. RAISE EC-FLOW-COMMIT): the grammar route mirrors MCS with `{facilityWord("COMMIT")}? commitFacilityStatement : IDENTIFIER ;`, but that must be validated against the paragraph ambiguity and carries the full legacy guard. Not recommended for Wave H.

**Golden** (`commit_rollback_nonsupport.cob`, `--std 2023`):
```
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CMTNS.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BEFORE".
           COMMIT.
           ROLLBACK.
           DISPLAY "AFTER".
           STOP RUN.
```
Hand-derived expected **stdout**: `COMMIT`/`ROLLBACK` are no-op empty paragraphs; execution falls through them:
```
BEFORE
AFTER
```
Expected **stderr**:
```
warning COBOLNET1571: the commit and rollback facility (COMMIT/ROLLBACK, ISO E.3.2 / Annex A.3 items 6–7) is a processor-dependent element (§4.2.6) that is not supported — it is accepted but performs no transaction control (see docs/CONFORMANCE.md §4)
warning COBOLNET1571: … (the ROLLBACK site)
```
Continuity fixture (`commit_userword_2014.cob`, `--std 2014`): `01 COMMIT PIC 9. … 01 ROLLBACK PIC 9.` compiles clean (both are user words below 2023; `facilityWord`/`IsReservedAt(2014)` false → no warning, no 0901).

**Blast radius / hazards:** the `IsStatementLeadingPosition` guard must NOT swallow a genuine mis-use where COMMIT appears as an *operand* — there the 0901 error must still fire (a data reference named COMMIT at 2023 is a real §8.9 violation). Watch the reserved-word dedup set (`_flaggedWords`) — 1571 should dedup per distinct word like 0901 does (`:1543`). Watch `CliExitCodeTests`: previously `COMMIT.` at 2023 **failed** the compile (0901 error, nonzero exit); it will now **succeed** with a warning (zero exit) — a deliberate exit-code change; update any legacy CliExitCode expectation that pinned the COMMIT-fails behavior (this is exactly the `feedback_guard_fast_not_ci_complete` trap — run the full CI-equivalent, not guard-fast).

---

### VCR 95 / 117–125 / 129 — VALIDATE facility (VALIDATE statement + validation clauses + EC-VALIDATE) — COBOLNET1572 (§4.2.13 obsolete + §4.2.7 optional)

**Spec sections:**
- §14.9.50 VALIDATE statement (:33109-33230). §14.9.50.1 (:33115-33119): *"The VALIDATE statement invokes data validation, input distribution, and error indication… NOTE The VALIDATE facility is an obsolete feature."* §14.9.50.2 format (:33124): `VALIDATE { identifier-1 } …`. §14.9.50.3 SRs (:33129-33139). §14.9.50.4 five-stage GRs (:33142-33223): format validation / input distribution / content validation / relation validation / error indication.
- **§4.2.13 F.2 item 5** (:50373 list; item text — *"Validate facility. The VALIDATE facility has not been implemented as of the writing of this revision by any COBOL provider…"*) → **obsolete**.
- **§A.4.14 VALIDATE** (:40497-40519) → **optional**: lists all validation elements — Data description entry format 4 (13.16), DEFAULT (13.18.17), DESTINATION (13.18.18), INVALID (13.18.31), PRESENT WHEN (13.18.41), VALIDATE-STATUS (13.18.62), VALUE format 5 content-validation (13.18.63), VARYING (13.18.64), VALIDATE statement, and EC-VALIDATE in RAISING/USE/PERFORM-WHEN/RAISE/TURN.
- **§F.2 disposition rule** (:50380) — *"A conforming implementation shall support obsolete language elements except for elements that are also optional or processor-dependent."* → because VALIDATE is **both obsolete AND optional (A.4.14)**, a conforming implementation **need not implement it** — this is the exact license for non-support. Cite this line: it is the decisive disposition anchor.
- EC-VALIDATE is level-2 (:24501); level-3 EC-VALIDATE-* need not be raised (§14.6.13.1.1 license).
- §8.9: `VALIDATE` / `VALIDATE-STATUS` are `r85=false, r2002=true, r2014=true, r2023=true` (introduced-reserved 2002 — `ReservedWords.Table.cs:461/462`, `reserved-words.json:4037/4046`).

**Syntax / format:** `VALIDATE identifier-1 [ identifier-2 … ]` (§14.9.50.2). Minimal recognition = leading `VALIDATE` + a data-reference list (or the generic `(~DOT)*` swallow — the operand SRs 1–6 at :33129-33139 are §4.2.6/§4.2.13-excused). The `dataReference+` form is preferable (cleaner) since `dataReference` already exists.

**Introduced edition & gate:** VALIDATE **statement/facility exists 2002 / 2014 / 2023** (reserved 2002+). `facilityWord("VALIDATE")` is true at 2002/2014/2023, false at 85 (where VALIDATE is a user word). This is NOT a 2023 gate — it must fire at 2002+ too (the facility has been optional-and-unimplemented since 2002; obsolete only as of 2023). Not a 0900 introduction gate.

**Disposition classification for the message:** VALIDATE is **optional (§4.2.7 / A.4.14)** — and, at 2023, additionally **obsolete (§4.2.13 / F.2 item 5)**. The mandatory warning obligation is §4.2.13 (:2511) at 2023 and §4.2.7 documentation at 2002/2014. Emit COBOLNET1572 at all three editions (2002/2014/2023) with a message that cites §4.2.7 (optional) + §4.2.13 (obsolete) — matching CONFORMANCE.md §4 row 3.

**As-built today:** `VALIDATE REC.` at `--std 2023` → CLI probe:
```
waveH_validate.cob(9,21): error COBOL0001: no viable alternative at input 'VALIDATEREC'
waveH_validate.cob(9,21): error COBOL0001: cannot parse construct near 'REC'
```
No grammar; VALIDATE reaches the parser as an IDENTIFIER, the statement dispatcher has no VALIDATE alternative → generic COBOL0001. **Audit-accurate.**

**AUDIT DRIFT CAUGHT:** the audit labels VALIDATE non-support purely under "§14.9.50 / §13.16-13.18 / F.2 item 5 (obsolete-optional)" and directs "a named §4.2.13 obsolete-optional non-support diagnostic." That is essentially right, but **two refinements**:
1. **The decisive license is §F.2's last paragraph (:50380)** — "*obsolete elements except for elements that are also optional or processor-dependent*" — combined with **A.4.14** (optional). Without A.4.14, obsolete alone would REQUIRE support (§4.2.13 :2509 "shall support obsolete language elements of the facilities for which support is claimed"). Cite A.4.14 + F.2 disposition, not just F.2 item 5.
2. The audit's edition framing is silent on the fact that VALIDATE is reserved/available **from 2002**, not 2023 — the recognition must therefore fire at 2002/2014/2023, and the below-edition (85) case is a *user-word continuity* case, not an introduction-gate case. (Sibling-wave drift risk: do not fold VALIDATE into a 2023-only gate.)

**Implementation plan:**
- Grammar (`CobolParserCore.g4`): add to `statement`: `| {facilityWord("VALIDATE")}? validateFacilityStatement` and `validateFacilityStatement : IDENTIFIER dataReference+ ;` (IDENTIFIER = VALIDATE). ADDITIVE ⇒ full legacy guard. (`dataReference+` keeps operands as real trees; a `(~DOT)*` swallow is an acceptable fallback but loses the operand structure needlessly.)
- Parser base: the shared `facilityWord` helper.
- Binder dispatch (`StatementBinder.cs:261`): add `_ when s.validateFacilityStatement() is { } v => Ec.BindUnsupportedFacility(v, "COBOLNET1572", "the VALIDATE facility (ISO §14.9.50; optional §4.2.7 / A.4.14, obsolete §4.2.13 / F.2 item 5)", "content validation"),` → `Warning` + `BoundNop`.
- No `constructs.json` row. No EC-VALIDATE engine (§14.6.13.1.1 license).
- `docs/CONFORMANCE.md`: §4 row 3 already lists VALIDATE; append COBOLNET1572. (The validation *clauses* — DEFAULT/DESTINATION/INVALID/PRESENT WHEN/VALIDATE-STATUS/VARYING and VALUE format-5 — are a **separate concern**: they appear in the DATA DIVISION, not as statements. They currently fail at their own data-clause grammar. Wave H scope is the *statement*; note in CONFORMANCE.md §4 row 3 that the clauses are likewise unsupported and that recognizing them is a follow-on data-clause item, not this code slice. Do NOT expand Wave H to the clauses — `feedback_spec_scopes_not_tests` cuts both ways: complete the *statement* facility, defer the clause surface as its own worklist row.)

**Golden** (`validate_nonsupport.cob`, `--std 2023`):
```
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC PIC X(10) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BEFORE".
           VALIDATE REC.
           DISPLAY "AFTER".
           STOP RUN.
```
Hand-derived expected **stdout** (VALIDATE is a no-op; no validation performed):
```
BEFORE
AFTER
```
Expected **stderr**:
```
warning COBOLNET1572: the VALIDATE facility (ISO §14.9.50; optional §4.2.7 / A.4.14, obsolete §4.2.13 / F.2 item 5) is not supported — it is accepted but performs no content validation (see docs/CONFORMANCE.md §4)
```
Also verify at `--std 2002` and `--std 2014` the **same** COBOLNET1572 warning fires (facility exists there too). Continuity fixture (`validate_userword_85.cob`, `--std 85`): `01 VALIDATE PIC X(10). … MOVE "X" TO VALIDATE.` compiles clean (VALIDATE is a user word at '85; `IsReservedAt(85)` false → predicate false → VALIDATE stays an identifier, no warning). Negative note: `VALIDATE REC.` at `--std 85` fails to parse (VALIDATE is a data-name there, `VALIDATE REC` is not a statement) — that is *correct* below-facility behavior, not a regression.

**Blast radius / hazards:** `validateFacilityStatement : IDENTIFIER dataReference+` sits in `statement` alongside every other verb — confirm no LL ambiguity with `moveStatement`/others (none share a leading IDENTIFIER-as-verb since VALIDATE is not a keyword token; the semantic predicate isolates it). The exit-code change mirrors commit's: `VALIDATE REC.` at 2023 was a hard COBOL0001 failure and now compiles with a warning — update any CliExitCode pin. Watch the version-matrix rows 117–125/129 if they assert VALIDATE-clause parse errors (they should now assert the named warning for the statement, clause surface unchanged).

---

### Cross-facility summary for the implementer

| Facility | Verb(s) | Reserved-as-facility at | Today | New diag | Recognition seam | Legacy guard? |
|---|---|---|---|---|---|---|
| MCS async messaging | RECEIVE, SEND | 85, 2023 | COBOL0001 | **1570** warning | grammar: `mcsFacilityStatement` (`{facilityWord}?`) | **YES** (shared parser; check CM-series) |
| Commit/rollback | COMMIT, ROLLBACK | 2023 | COBOLNET0901 | **1571** warning | diagnostic-layer: refine 0901 site in `VisitCobolWord` | no grammar change (conformance battery only; **update CliExitCode**) |
| VALIDATE | VALIDATE | 2002, 2014, 2023 | COBOL0001 | **1572** warning | grammar: `validateFacilityStatement` (`{facilityWord}?`) | **YES** (shared parser) |

Shared: one `CobolParserCoreBase.facilityWord(kw)` helper (table-driven off `ReservedWords.Find(kw).IsReservedAt(Edition.Year)`); one `Edition.Warning`-based `BindUnsupportedFacility` returning `BoundNop`; **no `constructs.json` rows, no 0900 gates, no EC engines** (§14.6.13.1.1 license). All three warnings are non-failing ⇒ programs compile and run with the facility inert, matching the SCREEN/COBOLNET1560 precedent. `docs/CONFORMANCE.md` §4 rows 1/2/3 gain their COBOLNET157x codes in the same change set (`feedback_docs_current_state_only`; the file's §5 maintenance note mandates code↔doc sync). Diag codes 1570/1571/1572 are Wave H's; the grammar-batch wave starts at 1573.

---

