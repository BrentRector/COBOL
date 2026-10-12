## Entry 059 — 2026-03-15: ISO Category Rules Documentation + Arithmetic Fix

Created `docs/CATEGORY-RULES.md` — the authoritative reference for COBOL category
compatibility rules as implemented in the compiler. Documents the full MOVE truth
table (6×6), arithmetic operand/result rules, comparison family rules, and
collating sequence behavior. All with ISO/IEC 1989:2023 section citations.

Key correction: arithmetic operands must be **Numeric only** (not NumericEdited).
NumericEdited is a display/editing category per §6.13. Updated
`CategoryCompatibility.s_arithmeticOperand` accordingly. NumericEdited remains
legal as an arithmetic **result** (the result is formatted into the edited picture).

Also created `docs/FUTURES.md` capturing deferred design work: runtime category
tracing for empirical NIST validation, WRITE AFTER ADVANCING, ON SIZE ERROR,
and MoveKind (Group/Elementary/CORRESPONDING).

Added doc reference comments in `CategoryCompatibility.cs` and `LoweringTable.cs`.

NC101A: 243 lines, 20/59 pass — unchanged (behavior-preserving).

---

---

