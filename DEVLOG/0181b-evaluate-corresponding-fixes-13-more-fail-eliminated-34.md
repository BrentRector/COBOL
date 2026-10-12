## Entry 181 — 2026-03-31: EVALUATE + CORRESPONDING Fixes — 13 More FAIL* Eliminated (34 remaining)

**EVALUATE multi-subject TRUE/FALSE** (NC225A, 6 fixed): Replaced global
`isEvaluateTrue`/`isEvaluateFalse` booleans with per-subject `SubjectKinds` array
(Value/True/False). Subject count always uses actual count instead of collapsing to 1.
Each WHEN group now bound with correct per-subject type. Also fixed latent ANY jump bug
in lowerer. BoundEvaluateStatement, ControlFlowBinder, ControlFlowLowerer,
BoundTreeValidator all updated.

**CORRESPONDING elementary↔group** (NC208A/NC209A, 5 fixed): Replaced flat
leaf-enumeration in CorrespondingMatcher with recursive level-by-level name-matching
(`MatchCorrespondingLevel`). At each level, named children compared by name; if both
groups: recurse; if either elementary: yield pair. Matches ISO spec exactly.

**CORRESPONDING target subscripts** (NC209A, 4+1 fixed): BoundCorrespondingStatement
now stores `BoundIdentifierExpression` (preserving subscripts) instead of bare
`DataSymbol`. DataMovementLowerer computes child locations relative to subscript-resolved
group base offset.

NC225A: 7→1, NC208A: 2→0, NC209A: 5→1 FAIL*. Guard: 34 FAIL* (was 47).

---

