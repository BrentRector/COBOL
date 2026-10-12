## Entry 816 — 2026-07-11 19:52 PDT — P7 Step 10f: `Verbs/ReportWriterBinder` + the ResolveReceiving hoist

**What (the AS-BUILT PLAN's 10f batch).** INITIATE/GENERATE/TERMINATE + `RwCounterExpr` (→ `CounterExpr`)
became `Verbs/ReportWriterBinder` over `BinderContext` ONLY (the census: zero sibling-partial consumption —
no host param at all). The load-bearing move the census's INVERTED-DEPENDENCY-TRAP hazard demanded:
**`ResolveReceiving` did NOT ride along** — it is the shared receiving-side chokepoint five host pipelines
consume (SET target, ResolveTargets, the three arithmetic Receiver builders), so it HOISTED into the core
partial (final home `ExpressionBinder` at 10q); the SET-F14-reroute-upstream-of-the-1523-guard placement and
all diagnostic call order preserved verbatim. The FieldOperand/RefExpr interception chain keeps its exact
position (KeywordOmittedFunction → LINAGE_COUNTER → `Rw.CounterExpr` → IndexFieldOf → refs.Resolve). The
GENERATE report-name-before-detail-group lookup order (SR2 before SR1 — a same-named report shadows a group)
moved verbatim. DEFERRED with a note: the raw `"COBOLNET1523"` string → catalog-descriptor normalization —
no 1523 descriptor exists and the catalog is registry-generated + drift-guarded; recorded for the 10t sweep
rather than widening a mechanical batch. One shadowing catch: the original verb methods' `ctx` PARAMETER
collided with the primary-ctor context — renamed `stmt` (compile-time).

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

