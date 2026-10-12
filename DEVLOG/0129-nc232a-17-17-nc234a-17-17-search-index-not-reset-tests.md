## Entry 129 — 2026-03-20: NC232A 17/17, NC234A 17/17 — SEARCH Index Not Reset, Tests Rewritten

### The bug

SEARCH always reset the index to 1 before starting the loop (line 2951:
`IrPicMoveLiteralNumeric(indexLoc, 1m)`). COBOL-85 §14.9.38: SEARCH uses the CURRENT
index value. If the index exceeds the table, AT END fires immediately. The programmer
must SET the index before SEARCH.

NC232A/NC234A set the index to 4 (past a 3-element table) then SEARCH — expecting AT END.
Our code reset to 1 and found a match instead.

### The fix

Removed the index reset from `LowerSearch`. One line deleted.

### The test rewrite

7 integration tests relied on the (incorrect) implicit index reset. Rewrote all 7 to use
proper COBOL: added `INDEXED BY` to OCCURS clauses, `SET index TO 1` before each SEARCH,
and used the INDEXED BY name in WHEN conditions. The user explicitly required rewriting
the tests rather than adding a guard — the tests were wrong, not the compiler.

### Results

- NC232A: 0/17 → **17/17** (SEARCH with high index)
- NC234A: 0/17 → **17/17** (SEARCH with high index, different table structure)
- 119 unit, 176 integration (1 skip), guard ALL GREEN

---

