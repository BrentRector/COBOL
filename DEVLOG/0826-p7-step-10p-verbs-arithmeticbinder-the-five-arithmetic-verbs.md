## Entry 826 — 2026-07-11 21:10 PDT — P7 Step 10p: `Verbs/ArithmeticBinder` — the five arithmetic verbs

**What (the AS-BUILT PLAN's 10p batch).** BindAdd (§14.9.1 incl. the Format-3 GIVING TO-operand-is-an-addend
fix's home) · BindSubtract (§14.9.44) · BindMultiply (§14.9.26) · BindDivide (§14.9.12, all five formats +
the REMAINDER SR6 single-quotient rule) · BindCompute (§14.9.8 F1 + the F2 boolean compute, the F1→F2
sole-boolean re-route, BuildComputeBoolean shared tail) became `Verbs/ArithmeticBinder(ctx, host)`; the
dispatch arms retarget `Arith.*`. The CORR entries (`Corr.BindAddCorresponding`/`BindSubtractCorresponding`)
retarget via `host.Corr` (accessor bumped internal). Per the plan: the receiving machinery — Receivers×3
(bumped internal), RoundingOf, ResolveReceiving — and the expression spine (BindExpr, DataRefs bumped
internal static) RIDE THE HOST until 10q flips them into ExpressionBinder; BindSizeError×2/BuildSizeError
ride the host too but STAY there past 10q (a statement-block phrase family recursing into
BindBlocks→BindStatement, like BindRetry — not expression spine); boolean edges go to ConditionBinder
(Gr3Width/SoleDataRef statics DIRECT — same namespace,
no forwarder needed; BindBoolExpr via the 10o host forwarder). The 1511 F2 SR bodies stay in-binder pending
the 10t pure-lift sweep (10o deviation (b) carried). No behavior change intended anywhere.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

