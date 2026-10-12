## Entry 179 — 2026-03-30: M004 Stage 0 — BoundTreeBuilder Decomposition Plan

Scanned BoundTreeBuilder.cs (4,428 lines, ~124 methods, 4 fields). Used parallel agents
to analyze the full method inventory across the file. Produced decomposition plan at
`docs/boundtree/BoundTreeBuilder-Decomposition.md`.

### Key differences from M002/M003
The binding pass is much simpler than emission or lowering in terms of shared state —
only 4 instance fields (`_semantic`, `_diagnostics`, `_options`, `_paragraphs`) plus
1 static set (`_alphanumericFunctions`). No per-method mutable state like
ArithmeticStatusLocal or CachedLocationLocals. This means the SyncToContext/SyncFromContext
bridge pattern from M003 won't be needed — BindingContext can be trivially shared.

### Proposed architecture: 9 focused binders
- **ExpressionBinder** (24 methods, ~750 lines) — largest, handles subscripts/ref-mod
- **ControlFlowBinder** (17 methods, ~740 lines) — IF/EVALUATE/PERFORM/SEARCH/GO TO
- **FileIoBinder** (15 methods, ~630 lines) — all file operations + SORT/MERGE
- **ConditionBinder** (18 methods, ~500 lines) — conditions, abbreviated forms
- **StringStatementBinder** (12 methods, ~480 lines) — STRING/UNSTRING/INSPECT
- **ArithmeticStatementBinder** (9 methods, ~470 lines) — ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE
- **DataStatementBinder** (12 methods, ~420 lines) — MOVE/SET/INITIALIZE/DISPLAY/ACCEPT
- **ProcedureNameResolver** (4 methods, ~120 lines) — paragraph/section name resolution
- **CallBinder** (3 methods, ~120 lines) — CALL/CANCEL/ENTRY

BoundTreeBuilder target: ~200 lines (orchestrator + dispatch + visitors).

---

