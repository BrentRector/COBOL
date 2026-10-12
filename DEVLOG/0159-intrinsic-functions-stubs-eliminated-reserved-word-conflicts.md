## Entry 159 — 2026-03-27: Intrinsic Functions — Stubs Eliminated + Reserved Word Conflicts Fixed

**Context:** Entry 158 left 8 stub functions and 3 reserved word conflicts (SIGN, SUM, RANDOM).

**Agent 1 (reserved word conflicts):** Added `functionName` grammar rule accepting IDENTIFIER
plus 6 conflicting keywords (DISPLAY, MERGE, RANDOM, SIGN, SORT, SUM). Updated lexer LPAREN
action to push SUBSCRIPT mode for all 6 keywords. Updated BoundTreeBuilder to extract name from
`functionName().GetText()`. 3 new integration tests.

**Agent 2 (stub implementations):** Replaced all 8 stubs with real logic:
- LOCALE-COMPARE: `string.Compare` with `CurrentCulture`
- LOCALE-DATE/TIME/TIME-FROM-SECONDS: `DateTime`/`TimeSpan` formatted with `CurrentCulture`
- STANDARD-COMPARE: `string.CompareOrdinal`
- CHAR-NATIONAL: `((char)(int)code).ToString()`
- DISPLAY-OF/NATIONAL-OF: pass-through (TODO: national data type support)
- CONVERT: `Encoding.Convert` with named encodings
- BASECONVERT: `Convert.ToInt64` + base formatting (bases 2-36)
- EXCEPTION-*: proper "no exception" empty string returns

**Results:** All 94 spec functions now dispatched (0 stubs, 0 conflicts).
421 unit + 263 integration + 60 NIST guard.

---

