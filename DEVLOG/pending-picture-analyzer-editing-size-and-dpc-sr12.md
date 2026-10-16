## PB3220 + PB3219: the EDITING FOR-form size reads the renderer's fixed-or-floating decision, and SR13's comma/period swap has one asker

Two R2 findings on `PictureAnalyzer` (PB1754), one file pair, both re-probed first on the wave's own build and both
reproduced exactly as filed.

**PB3220 (GR14 `es` size).** `PIC L999L EDITING L FOR NEGATIVE IS "DB "` rendered `DB 123DB ` (9 characters) while
`FUNCTION LENGTH` said 7 and its group 8: `EditingPositions` multiplied by the occurrence count only for the IS form
(`r.SimpleInsertion ? occ : 1`), so every FOR rule was sized as floating even when `EditRule.Floating` (rule 6's
adjacency test, decided by `PictureComposition.HasFloatingString` and already read by the renderer) said fixed. PB855
made two fixed occurrences of one character-1 legal and fixed the renderer; the size count never asked. It now reads
`r.Floating`: a floating string is one literal-wide occurrence plus one character per repetition (GR14 `es`, "one
occurrence of literal-2 or literal-3 is counted in the size of the item plus one character for each repetition"), and
every other rule counts the literal at each occurrence ("each occurrence of the character(s) specified in the
associated literal are counted"). Annex D.24's own `LLLL9.99 ... "DEBIT "` = 13 still holds.

**PB3219 (SR13 under DECIMAL-POINT IS COMMA).** SR12 b names "the symbol period" twice (the FOR-form symbol list, and
"may appear only once"), SR13 transfers each rule to the comma, and two sites read a literal period: `ValidateEditing`'s
FOR screen (refused the legal comma, COBOLNET1596, and accepted the grouping period) and `AnalyzeFloatEdited` (refused
`+9.9.9E+99`, two legal grouping periods, COBOLNET1658, and accepted `+9,9,9E+99`, printing `+1,5,0E+00`, two decimal
points). The runtime renderer already swapped (`CobolEdit.FloatMask.Parse` commaMode), so there was no emit change.
Fix shape: `PictureComposition.DecimalSeparator` / `GroupingSeparator` are the ONE asker of the swap; the validator, the
floating-string detector (`FloatingString` / `HasFloatingString` now take the flag instead of an ordered pair of
characters), the FOR screen and the significand all read them.

**Design choice recorded.** The R2 architecture and duplication variants proposed routing the float-edited string
through `PictureComposition.Validate`. The data-model design (the "Where it runs" paragraph) records the opposite
decision (GR13 b pins the float-edited form more narrowly than Table 10, and `AnalyzeFloatEdited` is that rule's one
place); I kept it, and bought the agreement the finding wanted with a test instead of a second code path: SR13 is now a
CLOSURE property (`PictureCategoryDriftTests.DecimalPointIsComma_InterchangesTheRolesOfPeriodAndComma_InEveryRule`: a
string under the clause is accepted, and binds the same category, size, digits and diagnostic codes, exactly when its
comma/period-interchanged twin does without it, over a plain walk, float-edited shapes and the EDITING walk), and
`PictureSeparatorDriftTests` forbids a literal separator ternary in the two picture files. The design doc is updated.

**Goldens.** `2023/pb3220_editing_for_fixed_occurrences_size_2023` (L999L = 9, LLLL9.99 = 13, IS form 9, L$999L = 8);
`2023/pb3219_decimal_point_comma_sr12_point_2023` (FOR with the comma, a significand with two grouping periods and with a
comma); negatives `pb3219-decimal-comma-for-form-period` (COBOLNET1596) and `pb3219-decimal-comma-float-two-points`
(COBOLNET1658). PB3220 adds no refusal, so it has no negative. Drift: `EveryAcceptedEditingPicture_HasTheSizeOfItsRenderedImage`
(GR4 size = the width of the rendered image over a 5-symbol walk x form x literal width x DECIMAL-POINT mode); both walks
were mutation-checked (each fix reverted, each walk red).

**Rows.** None claimed, none closed (`closes_rows_reason` on both notes). **Self-review** (architecture, full code,
performance, duplication; test-coverage, silent-failure and comment lenses): the doc comments of `EditingPositions` and
`AnalyzeFloatEdited` were stale about the FOR form and the significand symbols and are corrected; `AnalyzeLocaleEdited`
keeps its literal period on purpose (DECIMAL-POINT IS COMMA has no effect on format 2, §12.3.7.4 GR14); the remaining
separator ternaries in the repo are date-time format and intrinsic NUMVAL parsing, not PICTURE rules.

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` — Conformance 6,057/11,774 · Unit 33,248/33,696 ·
Characterization 6/36 cases ran, run 20261016T132841Z-567fe7; the four added goldens and the three added Unit tests Passed
in leg 1.

**Review round 1 (R1-1, PB3220's mixed arm).** The first review found the sibling arm the fix above did not touch: one
FOR-form character-1 holding BOTH a floating string and a fixed occurrence (`PIC LL9L EDITING L FOR NEGATIVE IS "DB "`,
legal: SR24 counts the string as one extended symbol and the trailing L as the second, Table 10 leaves a floating
extended string outside the matrix, SR25/SR26 own the order). `EditRule.Floating` is one flag per mask character, so the
rule was read as floating everywhere: LENGTH 6 instead of 8, the trailing L a third digit position (-125 displayed
`DB 125`), its literal never rendered. My own walk had passed LL9L because its oracle, the renderer, shared the reading;
the sweep above listed IS / FOR fixed / FOR floating and not this arm. Fix at the root: `SeparateFixedFromFloating`
runs after `PictureComposition.Validate` and gives such a letter's fixed occurrences a key of their own (a private-use
mask character plus a twin rule with `Floating: false`), so GR14's size, the digit anchors, the renderer and the
de-editor each read one kind per key and no second fixed-or-floating decision exists. Golden
`2023/pb3220_editing_for_mixed_floating_fixed_2023` (LL9L = 8: `DB 25DB `, ` DB 5DB `; LLL9L = 7); Unit
`AFloatingStringAndAFixedOccurrenceOfOneCharacter1_AreSizedEachByItsOwnKind` carries GR14's arithmetic per occurrence
(independent of the renderer) and the size walk gains a `mixed > 0` population guard. Float-edited pictures refuse FOR
(COBOLNET1658) and cs/+/- cannot mix (Table 10), so this is the only arm.
