## Entry 088 — 2026-03-16: Fix level-88 THRU ranges — dead grammar rule removal

The `conditionEntry88` grammar rule was dead code. It expected `INTEGERLIT conditionName valueSet`,
but `dataDescriptionEntry` already consumed the level number and data name before reaching
`dataDescriptionBody`. Level-88 entries were silently routing through the generic `valueClause`
path, which had no THRU support. Single-value 88s worked by accident; THRU ranges never parsed.

Fix: removed dead `conditionEntry88`, `conditionName`, `valueSet`, `valueRange` rules. Unified
`valueClause` to use `valueItem : literal (THRU literal)?` — supports single values, multiple
values, and THRU ranges uniformly. SemanticBuilder updated to navigate the new structure.

2 integration tests: THRU range, multiple THRU ranges with grade boundaries.

---

