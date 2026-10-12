## Entry 131 — 2026-03-21: Grammar Tier 1 — TEST BEFORE/AFTER, EVALUATE Class, DEPENDING ON?, SEARCH ALL WHEN+

Four grammar changes for future-proofing (no new tests unblocked yet — remaining tests
have additional blockers beyond these changes):

1. **PERFORM WITH TEST BEFORE/AFTER** (COBOL-85 §14.9.21): `(WITH? TEST (BEFORE|AFTER))?`
   prefix added to `performUntil` and `performVarying`. TEST token added to lexer.

2. **EVALUATE class conditions**: `classCondition` rule (NUMERIC, ALPHABETIC, etc.) added
   as alternative in `evaluateSubject`. Required for NC223A/NC225A (which also need
   INSPECT REPLACING category support).

3. **DEPENDING ON?** (ON optional): `DEPENDING ON? dataReference` for NIST NC235A
   compatibility.

4. **SEARCH ALL WHEN+**: Multiple WHEN clauses now allowed in SEARCH ALL per COBOL-85.
   BoundTreeBuilder updated to iterate `searchAllWhenClause[]`.

Remaining 16 tests all have deeper issues: period-terminated inline PERFORM (NC201A),
INSPECT REPLACING with category keywords (NC223A, NC225A), STRING WITH POINTER (NC217A),
CURRENCY SIGN (NC108M), and various other grammar gaps.

---

