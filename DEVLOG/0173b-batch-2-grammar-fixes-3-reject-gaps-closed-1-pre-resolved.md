## Entry 173 — 2026-03-30: Batch 2 Grammar Fixes — 3 REJECT gaps closed (1 pre-resolved)

Implemented 2 COBOL-85 grammar gaps + confirmed 1 was already resolved:

**M400 (SET ON/OFF)**: Already complete from prior session. NC174A passes 100% clean (77 PASS,
0 FAIL). Grammar `setSwitchStatement` and binder were working. Marked complete in ledger.

**M402 (SORT Format 2 — table sort)**: Made USING/GIVING/PROCEDURE block optional in
sortStatement grammar. Made sortKeyPhrase data-name list optional (Format 2 uses table's
inherent KEY). Added null-check in BindSortKeys for optional dataReferenceList.
**Sub-gap discovered**: runtime table sort not implemented — binder resolves target as file,
silently skips for data items. Tracked as new ledger item M427.

**M408 (SYMBOLIC CHARACTERS N:N)**: Rewrote symbolicCharacterEntry from `IDENTIFIER (IS|ARE)
literal` to `cobolWord+ (IS|ARE) integerLiteral+` for N:N positional mapping. Added FOR
ALPHANUMERIC/NATIONAL phrase. Updated SemanticBuilder to iterate parallel name/ordinal arrays.

**Architectural**: cobolWord used in symbolicCharacterEntry and IN clause (consistency with
Batch 1 refactor).

**Tests**: 9 new integration tests in GrammarBatch2Tests.cs — 4 SORT Format 2 variants,
4 SYMBOLIC CHARACTERS variants, 1 SET ON/OFF regression.

**Regression**: 922 unit + 311 integration + 95 NIST = 0 failures. 9 net new integration tests.

---

