## Entry 763 — 2026-07-11 03:00 PDT — P7 §6h cont.: IntrinsicRenderer's 3 static-arg renderers onto the visitor (the "last loud-default" claim here was PREMATURE — see Entry 764)

Converted the three intentionally-partial static-argument renderers `IntrinsicRenderer.StrStatic`/`NumStatic` (over
`BoundOperand`) + `NumStaticExpr` (over `BoundExpr`) to three cached nested visitors — `StrStaticVisitor :
IBoundOperandVisitor<string>`, `NumStaticVisitor : IBoundOperandVisitor<NumX>` (separate classes: same-param/different-
return can't overload), `NumStaticExprVisitor : IBoundExprVisitor<NumX>`; the methods are now thin `op.Accept(...)`
dispatchers. These channels are deliberately partial (H3 "named uncovered channel" — most operand/expr kinds are loud
in a static-arg context), so the many loud arms became EXPLICIT `Visit`s calling a per-visitor `Loud(n)` that reproduces
the former `_ => LoudValue(… n.GetType().Name …)` byte-for-byte. Byte-exact: 32 char + 3158 conf UNCHANGED.

**Milestone (accurate this time — verified by re-running the completeness grep): no LOUD-default bound-node dispatch
survives anywhere in the compiler.** Every `_ => …GetType().Name…` fallback over a bound root is gone (emitter, all five
renderers, StoreKindOf, the analyses, BooleanRenderer, IntrinsicRenderer).

**6h remainder — a REASONED scope decision, not an oversight:**
- The emitter's `PerformControl` switch (`default:` = PerformOnce) and `StoreSetTarget`/`AugmentSetTarget` (no default)
  are kept as switches. They are `void` + closure-heavy (`body`/`inline`/`value`/`amount`), so the generic-return
  `IBound*Visitor<T>` would force a dummy `T` and a state-carrying per-call visitor — UGLIER than the switch — for a
  tiny closed root (4 and 2 leaves) with NO loud default. A hypothetical new leaf surfaces as a missed emit (caught in
  test), not silent wrong output. Converting them cleanly wants a void-visitor generator variant; not worth it now.
- `UsageCollectionPass` (Step-5) — its own whole-tree hand `Visitor` (statements+exprs+operands+conditions+places) — is
  the one remaining walker genuinely worth converting onto the generated visitors + `StatementChildren` (it is the
  original completeness-bug site). It is substantial and deserves a focused pass (task #17).

