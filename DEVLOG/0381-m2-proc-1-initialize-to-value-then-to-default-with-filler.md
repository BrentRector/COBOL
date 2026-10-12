## Entry 381 — M2-PROC-1: INITIALIZE `TO VALUE` / `THEN TO DEFAULT` / `WITH FILLER` (ISO §14.9.20, COBOL-2002)

Closes the audit-flagged 🐛 "INITIALIZE phrases silently dropped". The grammar already parsed
`initializeCategoryToValue` / `initializeDefaultPhrase` / `WITH FILLER`, but `BindInitialize` captured only the
target list and the REPLACING phrase — the three COBOL-2002 phrases were thrown away, so e.g.
`INITIALIZE x TO VALUE` behaved like a plain `INITIALIZE x` (category defaults) instead of restoring the items'
VALUE clauses.

Captured them on `BoundInitializeStatement` (`WithFiller`, `ToValue`, `ToValueCategory` — null = ALL,
`ToDefault`) and reworked the per-item decision in the lowerer to the ISO precedence:
1. **TO VALUE** — an item with a VALUE clause (matching the optional category filter) is set to that VALUE.
2. **REPLACING** — `category DATA BY value` (unchanged).
3. **category default** — applied for the legacy no-phrase form OR an explicit `THEN TO DEFAULT`; **suppressed**
   for items not covered above when TO VALUE is given without TO DEFAULT (so `TO VALUE` alone leaves non-VALUE
   items untouched). The prior "unmatched REPLACING leaves the item unchanged" rule falls out of the same guard.

`WITH FILLER` flips the long-standing `if (item.IsFiller) return;` skip to `&& !stmt.WithFiller`. A new
`EmitValueClauseInit` emits the item's declared VALUE the same way program-start init would: figurative →
`IrMoveFigurative`; `ALL "x"` → `IrMoveAllLiteral` (repeat-fill); numeric → `IrPicMoveLiteralNumeric`;
alphanumeric → `IrMoveStringToField` (InitialValue is already quote-stripped/clean at bind time).
`ClassifyReplacingItem` was refactored to share `ClassifyCategory` with the TO VALUE category filter.

Verified on a group with VALUE'd (A=X"AAA", B=9 123) and VALUE-less (C, D) items plus a FILLER:
`INITIALIZE GRP TO VALUE` → A=AAA/B=123, C/D unchanged; plain `INITIALIZE GRP` → all defaulted, FILLER kept;
`ALL TO VALUE THEN TO DEFAULT` → A/B=VALUE, C/D=default; `REPLACING NUMERIC DATA BY 7` → B/D=007, A/C unchanged;
`WITH FILLER` initialized the FILLER (observed via an X(14) REDEFINES). Conformance
`tests/conformance/2002/initialize_phrases`. (Aside: an all-spaces alphanumeric field prints as empty —
CobolSharp's DISPLAY trims trailing spaces; pre-existing, orthogonal to INITIALIZE, noted for later.)

Guard ALL GREEN: **1047 unit / 473 integration / 364 NIST**, 0 regressions.

