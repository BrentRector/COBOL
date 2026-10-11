## Entry 169 — 2026-03-29: Guard Honesty + Phase 1 P0 Fixes

**Guard methodology overhaul**: discovered that the guard was checking output MATCHES, not
correctness. 232 FAIL* results across 19 tests were locked into "expected" baselines —
real compiler bugs treated as passing. Updated guard.sh to report per-test FAIL* counts
and a total warning. NC174A improved from 72→76 PASS after IS NUMERIC fix exposed that
4 class-condition tests were incorrectly failing and the failures were accepted as baseline.

**Phase 1 P0 fixes** (from 4-agent audit team):
1. COMP-3 FractionDigits scaling: DecodeComp3/EncodeComp3 now apply pic.FractionDigits
2. IS NUMERIC: spaces no longer treated as numeric, overpunch sign chars now accepted
3. SortRuntime: stable sort via LINQ OrderBy, numeric key comparison via PicDescriptor
4. IrCheckFileAtEnd: removed `new` Result shadow
5. PERFORM method lists: replaced null! with COBOL0501 diagnostic
6. IrBinary: added Ne/Le/Ge composite CIL sequences
7. CilEmitter: removed debug Console.Error.WriteLine dumps

**232 FAIL* inventory** (bugs to fix for true 100%):
- NC218A (69): UNSTRING overflow/pointer/tallying/OR-delimiters
- NC225A (27): EVALUATE class conditions, TRUE/FALSE subjects
- NC223A (27): INITIALIZE statement
- NC217A (20): STRING pointer/overflow
- NC204M (15): ACCEPT FROM mnemonic-name
- NC246A (14): qualified condition names in tables
- NC109M (10): ACCEPT basic
- NC216A (8): INSPECT tallying series
- NC247A (8): OCCURS DEPENDING ON
- 6 more tests with 1-6 FAIL* each

---

