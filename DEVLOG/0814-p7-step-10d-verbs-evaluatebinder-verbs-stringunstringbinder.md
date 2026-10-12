## Entry 814 — 2026-07-11 19:37 PDT — P7 Step 10d: `Verbs/EvaluateBinder` + `Verbs/StringUnstringBinder` — the next two pure leaves

**What (the AS-BUILT PLAN's 10d batch).** Both partials became real collaborators over `(BinderContext ctx,
StatementBinder host)`, the Step-9-style host edges reaching the shared spine (BindBlocks / BindCondition /
CheckedRelational / ConditionOf / FieldOperand / LiteralOperand / CheckLiteral / BindExpr — 11 spine members
made `internal` on the host; they flip to real collaborators at 10o/10q). The ride-along bound nodes stayed in
the Bound home as new records-only files — `BoundEvaluate.cs` (BoundEvaluate/BoundEvaluateWhen —
`StatementChildren` discoverability) and `BoundStringUnstring.cs` (the five STRING/UNSTRING records; the
BindString cref fixed). NO StatementValidation lifts from StringUnstring — its SR rejections are CONTROL FLOW
(each aborts the statement with a placeholder), exactly the convention's stay-in-binder case; Evaluate's SR
surface likewise binds through the spine checks (CheckClassConditionOperand — lifts with 10o's condition
channel, not piecemeal here). Dispatch arms `:199/:214/:215` rewired; lazy per-unit collaborator accessors on
the host. One namespace fact found by the compiler: `Place` lives in `CobolNet.Binding.Model` (the P5
relocation) — the records file needed the Model using, not the parent-namespace fallback.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

