## Entry 822 — 2026-07-11 20:39 PDT — P7 Step 10l: `Verbs/ControlFlowBinder` — ONE class (recorded deviation from the three-class sketch)

**What (the AS-BUILT PLAN's 10l batch).** STOP/GO TO/EXIT + IF + the PERFORM family became ONE
`ControlFlowBinder` over `(ctx, host)` — a RECORDED deviation from the plan sketch's
PerformBinder+IfBinder+ControlFlowBinder split: the emitter-side mirror is ONE `ControlFlowEmitter`, and
the singular-pattern rule prefers matching class topology over honoring a three-name list (IfBinder would
have been a 12-line class). Host edges per plan: GO TO's alterable forms keep delegating to the
.AlterSwitches partial (`AlterGoTo`/`AlterBindBareGoTo` internal — 10n retargets); EXIT's method/function
dispatches ride `host.OoBindExitMethod` (10s) / `host.Udf` (landed) / `host.EcBindRaising` (10r);
`SetTargetOf`/`OoScopeHint` internal for the VARYING induction targets and the method-scope error hints.
`BindBlocks`/`BindSizeError`/`BuildSizeError` STAY host spine (carved AROUND, between BindIf and
BindPerform). The one control resolver covering both PERFORM grammar shapes (the NC106A/NC176A lesson)
moved verbatim.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

