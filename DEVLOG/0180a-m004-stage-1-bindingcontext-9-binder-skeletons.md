## Entry 180 — 2026-03-30: M004 Stage 1 — BindingContext + 9 Binder Skeletons

Created `Semantics/Bound/Binding/` with 10 files: BindingContext (4 instance fields +
1 static set + 9 binder references + 2 delegates) plus 9 binder skeletons
(ProcedureNameResolver, ExpressionBinder, ConditionBinder, ArithmeticStatementBinder,
DataStatementBinder, ControlFlowBinder, FileIoBinder, CallBinder, StringStatementBinder).

BoundTreeBuilder constructor now creates `_ctx = new BindingContext(...)` and wires
all binders + `BindStatement` and `Typed` delegates. No methods moved. 56 structural
tests added.

Key difference from M003: BindingContext has only 4 instance fields (vs 18 for
EmissionContext). No per-method mutable state, no SyncToContext/SyncFromContext needed.
This will make Stages 2-4 significantly simpler.

Tests: 821 unit + 287 integration + 95 NIST. All green.

---

