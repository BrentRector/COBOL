## Entry 173 — 2026-03-30: M002 — Binder Decomposition (4,266 → 579 lines)

### The problem
`Binder.cs` at 4,266 lines was doing everything: record layout, paragraph methods,
ALTER slots, 58-case `LowerStatement` switch, expression evaluation, control flow,
arithmetic, data movement, file I/O, and string operations. Single-responsibility
violated at industrial scale.

### The design
Decomposition plan in `docs/binder/Binder-Decomposition.md`:
- **LoweringContext**: shared mutable state (semantic model, value factory, paragraph
  maps, ALTER slots, PERFORM stacks, tracking vars)
- **8 focused lowerers**: LocationResolver (150 lines), ExpressionLowerer (120),
  ConditionLowerer (615), ControlFlowLowerer (1,154), ArithmeticLowerer (292),
  DataMovementLowerer (335), FileIoLowerer (688), StringLowerer (240)

### The execution (5 stages)
**Stage 1:** Created `CodeGen/Lowering/` with 9 files. LoweringContext with all shared
fields, lowerer references, and `LowerStatement` delegate. 76 structural tests.

**Stage 2:** Extracted LocationResolver (5 methods) and ExpressionLowerer (5 methods).
Binder retains thin forwarding wrappers.

**Stage 3:** Extracted ConditionLowerer (16 methods + 2 nested types), ArithmeticLowerer
(7 methods), DataMovementLowerer (9 methods). Binder down from 4,266 to 2,726 (-36%).

**Stage 4:** Extracted ControlFlowLowerer (23 methods), FileIoLowerer (16 methods),
StringLowerer (4 methods). Binder down to 688 lines.

**Stage 5:** Removed all 35 forwarding wrappers. `LowerStatement` dispatches directly
to `_ctx.ControlFlow.*`, `_ctx.FileIo.*`, etc. Binder at **579 lines** (86% reduction).

Tests at completion: 596 unit + 287 integration + 95 NIST. All green at every stage.

### The key insight
The `LowerStatement` delegate on LoweringContext was critical — it lets
ControlFlowLowerer recursively lower IF bodies and PERFORM inline statements without
depending on the Binder class. No circular dependencies. Clean DAG.

---

