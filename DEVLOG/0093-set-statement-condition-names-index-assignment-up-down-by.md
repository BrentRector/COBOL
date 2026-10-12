## Entry 093 — 2026-03-16: SET Statement — Condition Names, Index Assignment, UP/DOWN BY

Implemented SET statement with three forms, all lowering to existing MOVE/arithmetic machinery.

**SET condition-name TO TRUE**: Moves the first defining value from the 88-level's ValueRanges
into the parent data item. `SET FLAG-ON TO TRUE` where `88 FLAG-ON VALUE "Y"` emits
`IrMoveStringToField(parentLoc, "Y")`.

**SET condition-name TO FALSE**: Needs a value guaranteed not to match any true value. For
alphanumeric parents, fills with spaces (via IrMoveFigurative). For numeric, tries 0, 1, -1, 99
and picks the first that isn't in the condition's true values. This is robust — it won't
accidentally satisfy the condition it's supposed to clear.

**SET identifier TO value / UP BY / DOWN BY**: Direct delegation — TO lowers to MOVE, UP BY to
ADD, DOWN BY to SUBTRACT. All reuse existing IR instructions.

Grammar already had `setToValueStatement`, `setBooleanStatement`, `setIndexStatement` — no grammar
changes needed. Binding routes through symbol resolution: if the target resolves as a
ConditionSymbol, it's a condition SET; otherwise it's an index/data SET.

4 tests: SET TO value (existing, unskipped), condition TO TRUE, condition TO FALSE, UP BY/DOWN BY.

---

