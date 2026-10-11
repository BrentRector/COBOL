## Entry 545 — 2026-06-10 13:20 PDT — MOVE/PICTURE editing closeout: NC104A + NC124A byte-match → 82/95 NC green

Four-bug basket in NC104A (the MOVE feature program) + one in NC124A (PICTURE), all in the editing layer:
1. **`PIC 9(5)CR` was classified PURE NUMERIC** — CR/DB weren't in PicInfo's edited-symbol scan, so the item became
   a `long` field and `MOVE -12345 TO MOVE16` lost the editing entirely (and the sign with it). CR/DB are fixed-
   insertion symbols (§13.18.40.4) → numeric-edited.
2. **GROUP-level VALUE ignored** (`01 MOVE29A VALUE "$123.45". 02 MOVE30 PIC $999.99.`): §13.18.63 — the literal
   initializes the whole area as ONE alphanumeric value. FieldEmitter now distributes the padded literal over the
   subtree POSITIONALLY at compile time (string-stored leaves, OCCURS occurrences sliced; falls back member-wise
   when a typed-numeric leaf or shared-storage member is in the subtree).
3. **BLANK WHEN ZERO** (§13.18.8): captured in the data model (DataItem.BlankWhenZero), threaded through every
   numeric-edited store (`CobolEdit.Format`/`TryFormat` optional flag — MOVE and arithmetic resultants alike).
4. **AN→NE moves copied characters**: an alphanumeric sender into a numeric-edited receiver is a LEGAL editing
   move (§14.9.25.3 Table 16) — treated as an unsigned integer and edited ("12345" → $12,345.00), now via
   `FromAlphanumeric` + `Format`.
5. **P scaling in EDITED masks** (NC124A `PIC ZZZPP`): PicInfo's P classification keyed on '9' only — an edited
   P-mask has none, so ZZZPP got scale +2 instead of −2; and `CobolEdit` rendered the P's as literal output
   characters. P positions now classify against the digit POSITIONS (9/Z/*), the mask scale carries −P
   (FractionDigits), and P strips from every render pattern (§13.18.40.3 — P holds no output position).

**82/95 NC GREEN (+NC104A, NC124A; zero lost), 562 conformance, 15 unit.** The 13 remaining: collating ×2,
DECIMAL-POINT COMMA ×2, ODO ×2, REDEFINES-tier ×1 (NC252A), runtime louds ×3 (NC105A/224A/401M), spec-pinned ×1
(NC236A), no-golden ×2 (by design). Wave-2 scout briefs (CALL, SORT/MERGE, RL/IX files, collating, ODO,
DECIMAL-POINT) are being built by 6 parallel scouts as this lands.

