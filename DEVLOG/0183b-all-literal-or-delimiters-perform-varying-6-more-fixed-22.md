## Entry 183 — 2026-03-31: ALL-literal + OR delimiters + PERFORM VARYING — 6 more fixed (22 remaining)

**ALL-literal condition** (NC250A IF--TEST-28): `ConditionValue.FromAllString` +
`IsAllLiteral` flag. Lowerer repeats pattern string to fill parent's StorageLength
before comparison. NC250A 2→1 FAIL* (IF--TEST-26 remains: abbreviated condition issue).

**UNSTRING OR delimiters** (NC218A GF-21.03/04): Extended full pipeline from single
delimiter to delimiter list. BoundUnstringDelimiter record, IrUnstringDelimiter,
CilStringEmitter builds string[]/bool[] arrays, UnstringExtract scans all delimiters
picking earliest match. NC218A 2→0 FAIL* — all 9 original failures now fixed!
NC218A baseline created (88/95 tests with clean baselines).

**PERFORM VARYING AFTER** (NC201A): Properly implemented GR10(d) step 8 — inner
AFTER variables re-initialized to FROM values when outer loop increments.
`ResetInnerVaryingFromValues` walks Next chain emitting MOVE instructions.
NC201A 5→2 FAIL* (2 remain: COMP subscript corruption in F4-24).

Guard: 22 FAIL* across 7 tests pending fix.

---

