## Entry 828 — 2026-07-11 21:25 PDT — P7 Step 10r: `Verbs/EcBinder` + `EcBindState` on ctx — the EC half leaves the god class

**What (the AS-BUILT PLAN's 10r batch).** `StatementBinder.Exceptions.cs` is DELETED; its members became
`Verbs/EcBinder(ctx, host)` — BindRaise §14.9.29 (incl. the exception-OBJECT leg) · EcResolveLevel3 ·
BindResume §14.9.33 (SR1–SR3) · BindSetLastException §14.9.39 F13 · EcBindRaising §14.9.18/§14.9.14 (incl.
the EC-OO identifier leg + SR4a superclass walk) · EcCollectPdRaising/EcLoadPdRaising/EcAddPdRaisingWord
§14.2.1–2 · `EcWrap` (the per-statement TurnState fold, D10 — still invoked at the host BindStatement EXIT,
per the plan) · EcLocation §15.30.3 r2 · the intrinsic-presence walks (ContainsIntrinsic/DirectIntrinsic +
the operand/bool/expr/cond walkers) moved VERBATIM — their generated-visitor conversion stays FLAGGED as a
behavior-sensitive follow-up. State: new `EcBindState` on `BinderContext.EcState` (Turn · ProgramName ·
PdRaising · PdRaisingClasses · DeclEcPairs · the 7 EcFeatures bits with `BuildFeatures()`), and
`_currentBindPc` → `ctx.BindCursor` (writes re-pointed in the core bind loop AND the Oo-partial method
loop). The Declaratives partial re-points `_ecF3`/`_declEcPairs` to ctx.EcState and gains the narrow
`EntryPc`/`Declaratives` internal accessors (RESUME SR1 reads; hoist to ProcedureTableBuilder at 10t).
Host keeps forwarders (ConfigureEc PUBLIC — BinderDriver/CSharpEmitter.Oo re-point at 10t with everything
else, the deviation-(a) pattern; BuildEcFeatures reads ctx.EcState directly; EcNoteFunction /
BindSetLastException / EcBindRaising / EcLoadPdRaising internal). EcLocation's table reads go through the
EXISTING host surface (`Paragraphs`/`ParaSections`). Transform misstep (transparency): the first script run
tripped its own residual-state assert — `_pdRaisingClasses.Contains` (the SR4a walk) had no rule; asserts
fired BEFORE any write, tree untouched, rule added, second run clean. Plus the recurring lesson: TurnState
lives in `CobolNet.Binding`, not `Runtime.Exceptions` (parent-namespace lookup covers it unqualified).

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

