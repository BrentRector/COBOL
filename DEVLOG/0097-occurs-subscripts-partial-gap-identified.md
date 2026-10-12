## Entry 097 — 2026-03-16: OCCURS + Subscripts — Partial, Gap Identified

Implemented OCCURS count on DataSymbol, storage layout accounting for OCCURS multiplier,
subscript syntax on identifiers, and constant subscript resolution in MOVE/DISPLAY.

**What works**: `MOVE 7 TO ITEM(3)`, `DISPLAY ITEM(3)`, `GO TO P1 P2 DEPENDING ON ARR(1)` —
all with constant integer subscripts. Storage layout correctly allocates `elementSize * occursCount`
bytes for both elementary and group OCCURS items.

**Critical gap identified by user**: Only 5 call sites in the Binder use the subscript-aware
`ResolveIdentifierLocation`. **39 other sites** still call `_semantic.GetStorageLocation` directly,
completely bypassing subscript resolution. This means subscripts silently break in:
- All arithmetic (ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE operands and targets)
- IF/condition evaluation
- INITIALIZE, SET, INSPECT
- File I/O (READ INTO, WRITE FROM)
- GO TO DEPENDING with variable selector
- PERFORM VARYING index

**Variable subscripts not implemented**: `ResolveIdentifierLocation` returns null for non-constant
subscripts. No caller handles this null. The `IrElementRef` IR node was defined but no emitter
or lowering code exists to use it.

**Architectural lesson**: The right fix (per user's design) is a unified `IrLocation` abstraction
that replaces `StorageLocation` in all IR instructions — either a static location or a dynamic
element reference. This avoids threading subscript awareness through 39+ individual call sites.
Two central emitter helpers (`EmitLoadLocation`, `EmitStoreLocation`) would handle both cases.

**AI failures this session**:
1. Tried to use string comparisons for ACCEPT FROM DATE instead of proper lexer tokens — caught
   by user before implementation.
2. Repeatedly oscillated between "constant only" and "dynamic" subscript approaches instead of
   committing to one architecture.
3. Said "cleanest approach" multiple times when the user wanted "production quality" — these are
   not the same thing. Clean ≠ correct. Production quality means: works for all cases, not just
   the easy ones.
4. Made scattered changes across 39+ call sites without a unified abstraction, creating exactly
   the kind of inconsistency the user warned against.

3 new tests pass (MOVE+DISPLAY subscript, multiple elements, GO TO DEPENDING with subscript).
97 integration tests total, all green. But the subscript implementation is incomplete.

---

