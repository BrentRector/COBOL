## Entry 831 — 2026-07-11 22:03 PDT — P7 Step 10t/2: the host-forwarder flip — collaborators reach siblings directly

**What (the FINAL-WIRING batch, sub-2 — the one-shot forwarder flip the 10o–10s batches deferred).** All ~40
transitional host forwarders on `StatementBinder` (the `internal X() => Collaborator.X()` thin delegations
added as each verb batch landed) are DELETED. The collaborator call sites re-point from `host.Foo(…)` to
`host.<Accessor>.Foo(…)` — `host.Expr.BindExpr` / `host.Cond.BindCondition` / `host.Ec.EcBindRaising` /
`host.Oo.OoBindSetObjectRef` / `host.Alter.SwitchBindSet` / `host.Set.SetTargetOf` /
`host.Intrinsic.IntrinsicOperand` — reaching siblings directly through the composition root's accessors (19
collaborator files re-pointed). The statics likewise: `StatementBinder.SoleDataRef`/`SoleNumLiteral`/
`MapOperator` → `ConditionBinder.*`, `StatementBinder.FigurativeOperand` → `ExpressionBinder.FigurativeOperand`,
`StatementBinder.OoExtractBareReference` → `OoBinder.*`. Core-internal callers folded: `BindRetry`'s two
`BindExpr` → `Expr.BindExpr`, the two `BuildEcFeatures()` → `Ctx.EcState.BuildFeatures()`, the roster
`EcLoadPdRaising` → `Ec.EcLoadPdRaising`; `Set` accessor bumped internal. **ONLY `ConfigureEc` survives as a
host method** — it is the binder's genuine PUBLIC API (BinderDriver + CSharpEmitter.Oo configure the EC
context per unit), not a collaborator forwarder. `StatementBinder` is now exactly what the plan names it:
dispatch switch + the mark/drain wrap protocol + the composition root (holds the ONE lazy instance of each
collaborator + the OO/UDF host surface that stays until P9). Core: 365 lines. One predicted fix: the
`MapOperator` static in KeyedIoBinder wasn't in the initial static map (added). No behavior change — every
`host.Foo` and `host.Bar.Foo` resolve to the identical method.

**Verify.** Sln Debug clean; 0 forwarders remain (grep); 33 characterization (32 snapshots byte-exact) · 281
unit · 3166 conformance — verdicts read as separate actions.

