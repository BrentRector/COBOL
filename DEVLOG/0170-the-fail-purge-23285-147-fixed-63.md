## Entry 170 — 2026-03-29: The FAIL* Purge — 232→85 (147 fixed, 63%)

Discovered that the guard was locking 232 FAIL* results into "expected" baselines — real
compiler bugs masked as passing tests. Session pivoted from compiler upgrade to elimination.

**Diagnostic agent team** (4 parallel read-only agents) identified root causes across all 14
failing tests. Key fixes implemented:

- UNSTRING: no-delimiter field-length, overflow preservation, figurative/identifier delimiters,
  TALLYING existing value (NC218A 75→17)
- EVALUATE: WHEN NOT inversion, EVALUATE FALSE support, class condition subjects
  (NC225A 27→7)
- SET SWITCH ON/OFF binding (NC174A 1→0)
- Figurative constants in condition VALUE clauses (NC250A 5→4)
- Phase 1 P0: COMP-3 scaling, IS NUMERIC, SortRuntime, IR/CIL bugs

85 FAIL* remain across 14 tests. Largest: NC218A(17), NC246A(14), NC225A(7), NC216A(8).

---

