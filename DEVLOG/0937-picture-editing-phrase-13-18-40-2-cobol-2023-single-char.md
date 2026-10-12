## Entry 937 — 2026-07-20 17:05 PDT — PICTURE EDITING phrase (§13.18.40.2, COBOL-2023) — single-char render lands, multi-char/floating is a P14 GAP

Grammar batch position 5. The PICTURE clause EDITING phrase (§13.18.40.2 Format 1; the new-in-2023 reserved word
EDITING, Annex E.3.3 item 19) is user-defined picture editing: an IS (simple insertion) form inserts a literal at
each character-1 position UNCONDITIONALLY (editing rule 3, sign-independent), and a FOR (extended sign control) form
selects the NEGATIVE literal on a negative value and the POSITIVE literal (or spaces, SR12c) otherwise.

**The sign map is Table 9 + Annex D.24, NOT the extracted Table 8** — Table 8 in the OCR'd spec is sign-INVERTED
(its character-1 NEGATIVE-phrase row puts the negative literal on positive values, contradicting Table 9 at 20572,
D.24's `EDITING "L" FOR NEGATIVE IS "("` → `MOVE -123.45` = leading `(`, and Table 8's own correct built-in rows).
The spec-grounding SSOT (`PHASE-13-grammar-batch-spec-grounding.json`) caught and adversarially confirmed this.

**Landable vs P14 render GAP.** The engine seam is `CobolEdit.Format`'s `char[pattern.Length]` 1:1 output array. A
SINGLE-character literal fits that model, so the IS form (any character-1 occurrence count) and the single-occurrence
FOR form (fixed sign control) RENDER now, threaded as a new `CobolEdit.EditRule(Char1, Neg, Pos)` (IS: Neg==Pos). A
MULTI-character literal ("DEBIT ") or a FLOATING character-1 (the same character-1 ≥2 times under a FOR phrase) needs
the variable-width position-group model — a documented P14 render GAP, staged loud (COBOLNET0899). Chose EditRule
threading over compile-time mask-baking because baking a literal char that collides with a mask metacharacter (`.`,
`+`, `Z`, …) would corrupt the mask; the EditRule resolves character-1 as a RAW insert before the metacharacter
switch, immune to re-interpretation.

**Threading.** `EditRule[]? edits` is a trailing OPTIONAL param on `Format`/`TryFormat`/`DeEdit` (default null → every
existing call byte-identical). The emitter appends an `edits:` named argument (via `RuntimeApi.EditsArg`) only when
`pic.EditingRules is not null`, so an ordinary program's generated code is unchanged. Threaded through the six edited
stores (MOVE, arithmetic GIVING, STRING, ACCEPT/DISPLAY, VALUE constant-fold, group image) + DeEdit sign recovery.

**Bind (`PictureAnalyzer.ValidateEditing`).** SR8 (character-1 a basic letter ≠ A B C D E N P R S V X Z / currency →
COBOLNET1591), SR11 (distinct character-1 → 1592), SR10 (character-1 present in the mask → 1593), SR9 (literal ≤50 →
1594), SR12a (equal NEGATIVE/POSITIVE width → 1595), SR12b (FOR picture only character-1 and 9 . cs P V Z → 1596). A
pre-scan of ALL phrases' character-1 lets SR12b admit a SECOND extended sign symbol (SR25 leftmost/rightmost, e.g. a
leftmost `L` and a rightmost `F`). char1Set feeds the §13.18.40.3 SR2 whitelist so character-1 letters are not
COBOLNET0808-rejected. The IS form's repeated character-1 is LEGAL (fork2: SR24 scopes the built-in +,-,CR,DB, not
the simple-insertion character-1).

**Grammar / gating.** `pictureClause : PIC PIC_STRING editingPhrase*` (additive; PIC_STRING pops PICMODE at
whitespace so EDITING lexes in default mode) + `editingPhrase`/`editingForPhrase` (the FOR choice indicators = one or
both of NEGATIVE/POSITIVE, either order — verified against the re-rendered Format-1 figure). EDITING is a new hard
lexer token admitted to the cobolWord nameSlot funnel (`cobol-words.json`) so `01 EDITING PIC 9` stays legal below
2023 (COBOLNET0901 at 2023; the COMMIT/VALIDATE precedent). 2023 introduction gate is recognition-fired by
`VersionConformancePass.ParseArm.VisitPictureClause` on editingPhrase presence (COBOLNET0900 below 2023). Two
`constructs.json` rows (picture-editing-2023, user-word-editing-2023).

Conformance `PictureEditingTests` (13): IS render `99T99 EDITING "T" IS ":"`→`12:30`; repeated IS `9G9G9`→`1:2:3`
(sign-independent); FOR `L999.99F EDITING "L" FOR NEGATIVE IS "(" EDITING "F" FOR NEGATIVE IS ")"` → `(012.34)` /
` 056.78 `; multi-char + floating → COBOLNET0899; SR8/11/10/12a negatives; below-2023 → COBOLNET0900; EDITING as a
data-name compiles at 85, COBOLNET0901 at 2023. Diagnostics 1591-1596 (compiler channel, raw codes — the 1542/0808
pattern); 0899 via DiagnosticCatalog.ConstructStagedNotImplemented (the split-code drift rule); reserved 1591-1602 →
1597-1602 released. Shared `.g4` ⇒ full legacy guard.

