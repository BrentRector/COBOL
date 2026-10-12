## Entry 537 — 2026-06-10 09:41 PDT — SEARCH ALL + literal-vs-group comparisons → FOUR new NC greens — ★ 50 NC PROGRAMS BYTE-MATCH ★

- **SEARCH ALL (ISO §14.9.37 Format 2):** GR9 makes the technique implementor-specified and the initial index
  setting IGNORED — a from-occurrence-1 scan is conformant for the key-ordered tables Format 2 requires (SR7).
  Bound onto the SAME `BoundSearch` machinery with a `FromStart` flag (the emitter prepends `index = 1`); the
  OCCURS KEY metadata capture (for a real binary search + the ordered-table EC) stays a later refinement, the
  semantics are right today.
- **Sole-numeric-literal comparison operands (§8.8.4.2.1):** `IF group-item = 090109` wrapped the literal in a
  COMPUTED operand → the string-comparison path failed loud. A sole numeric literal now stays a LITERAL operand,
  participating against an alphanumeric/group operand as its WRITTEN character form — leading zeros intact,
  which a computed wrapper would lose.

**NC233A, NC238A, NC237A, NC103A byte-match → 50 NC programs locked in (the half-hundred mark; the session
started at 15). 432 conformance + 15 unit green.** NC104A advanced to a 34-line diff. The 10-program DIFF
diagnosis workflow is still running; its synthesis drives the next picks.

