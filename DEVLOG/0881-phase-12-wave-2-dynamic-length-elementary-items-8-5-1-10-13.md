## Entry 881 — 2026-07-17 16:11 PDT — PHASE-12 wave 2 — DYNAMIC LENGTH elementary items (§8.5.1.10 / §13.18.19, COBOL-2014) LIVE end-to-end

DYNAMIC LENGTH elementary items land on the greenfield substrate — a variable-length, minimum-length-zero
`PIC X`/`PIC N` string. Typed-native, spec-first: the item IS a native .NET `string` field (no byte substrate),
its current length varies at runtime, and a receiving MOVE truncates on the right to the LIMIT with NO padding.

**The pipeline (bottom-up, all from the persisted scout anchors, not the drifted plan):**
- **Runtime** `CobolDynString.Store(value, limit)` — the §8.5.1.10.4 receiving rule (replace, truncate-right-to-limit,
  no pad; -1 = the implementor-defined maximum, §13.18.19.4 GR2). A static helper over `string`, the `CobolString`
  pattern.
- **Data model** — `StorageForm.DynamicString(Category, Limit)` (IsCharacterImage=false, ImageWidth=0 — a
  dynamic-length item makes its group a variable-length group, §8.5.1.12); `DataItem.IsDynamicLength` +
  `DynLengthLimit` facts; `ElementaryImageWidth`/`IsCharacterImage`/`IsImageCapable` short-circuit on it like
  `IsDynamicTable`; `StorageFormPass` classifies it (a new branch) and maps its element type to `string`.
- **Grammar** — `dynamicLengthClause : DYNAMIC LENGTH cobolWord? (LIMIT IS? integerLiteral)?`, a superset-parse
  alternative in `dataDescriptionClause` (the BASED/ANY-LENGTH convention — NOT a `{is2014()}?` predicate). Tokens
  DYNAMIC/LENGTH/LIMIT already existed; ANTLR regen clean (0 warnings — LL-disjoint from `occursClause`, which leads
  with OCCURS).
- **Binder** — `BindEntry` decodes the clause and enforces the shape SRs (the ANY LENGTH template): §13.18.19.3 SR1
  (PICTURE exactly one N or X — NOT the boolean '1') → **COBOLNET1561**; the dynamic-length-structure-name
  non-support (§13.18.19.3 SR2 / §12.3.7) → **COBOLNET1562**; §13.16.3 SR18 (only level-number/entry-name/PICTURE/
  USAGE/VALUE permitted) → **COBOLNET1563**. A fresh 1561-1563 band — the plan's 1540/1541 collide with live P10
  concat codes (the re-scout catch), so P12 draws from 1561+.
- **Edition gate** — `VersionConformancePass ParseArm.VisitDynamicLengthClause` → the `dynamic-length-item-2014`
  construct → COBOLNET0900 below 2014 (recognition-based, the drop-proof home).
- **Emitter** — the field is a `string` init `""` (or its VALUE, truncated to the LIMIT — §8.6.4 defines the initial
  length from VALUE, absent VALUE it is 0); a receiving MOVE emits `CobolDynString.Store` (a figurative SPACE →
  length 0, §8.5.1.10.4; a zero-length literal → length 0, §14.9.25.4 GR2 — NOT SPACE-substituted). FUNCTION LENGTH
  of a PIC X dynamic-length item returns the runtime `.Length` (§15.50.4 rule 6 — current length in bytes = its
  character positions); a NATIONAL dynamic-length LENGTH (bytes = 2× positions) and BYTE-LENGTH (no runtime body)
  stage loud by name (the ANY LENGTH discipline; §1.4).

**Spec-faithfulness detail the scout forced (verified against the spec):** §13.16.3 SR18 — NOT §13.18.19.3 — is the
co-clause exclusion authority (the plan cited the wrong §); VALUE is PERMITTED and sets a non-zero initial length
(§8.6.4), so the field is NOT unconditionally initialized to empty; and FUNCTION LENGTH of a dynamic-length item
returns BYTES (§15.50.4 rule 6), so a naive character-count for PIC N would be wrong — hence the national leg is
staged loud rather than shipped half-right.

**Verified (CLI + goldens, byte-exact on first run):** `MOVE "HELLO"` → length 5; `MOVE` a 38-char literal into a
LIMIT-20 item → truncates to 20; `MOVE SPACES` → length 0; VALUE "INIT" → initial length 4; an item with no LIMIT
→ unbounded (34-char store). All four SR/gate diagnostics fire (1561/1562/1563/0900). Two byte-compared conformance
programs (`tests/conformance/2014/dynamic_length_item`, `dynamic_length_limit`), the `DynamicLengthTests` guard suite
(10 cases: the SR negatives + well-formed positives + the 4-edition gate), and `CobolDynStringTests` (10 unit cases:
round-trip / truncation / min-0 / no-limit) all green. Matrix row `dynamic-length-item-2014` active — asserts
compile at 2014/2023, COBOLNET0900 at 1985/2002 (exit criterion 2 MET). Full battery green with the FULL legacy
guard (NIST 353 MATCH — grammar changed).

**Two AI missteps the gates caught (feedback_transparency):** (a) the new `dynamic-length-item-2014` matrix row
was authored WITHOUT the required `vcr` field — `VersionMatrixTests.LoadCatalogue`'s static ctor threw, red-lining
all 587 matrix cells at once (the catalogue is a single static). Added `vcr`, regenerated the construct files.
(b) The two new `2014/dynamic_length_*` conformance programs went RED in the FROZEN legacy `ConformanceTests` (the
byte engine has no dynamic-length model — it read `PIC X DYNAMIC LENGTH` as a fixed `PIC X`, so `1[H]` not
`1[HI]`). This is the `feedback_legacy_suite_on_shared_corpus` case: added both to the `GreenfieldOnly` exclusion
in the SAME commit (the greenfield CorpusRunnerTests byte-compares them; the legacy runner skips them until the G8
cut-over). The full legacy guard is the only gate that surfaces the second class — guard-fast is NOT optional for a
shared-corpus golden. Next: wave 3 = the IEEE float family (Step 7).

