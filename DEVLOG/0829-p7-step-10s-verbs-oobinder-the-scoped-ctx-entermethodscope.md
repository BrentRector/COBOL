## Entry 829 — 2026-07-11 21:35 PDT — P7 Step 10s: `Verbs/OoBinder` + the scoped `ctx.EnterMethodScope` — the OO half converts LAST

**What (the AS-BUILT PLAN's 10s batch — the last collaborator).** The `.Oo` partial dissolves three ways.
(1) The ride-along bound records (InvokeForm · BoundInvoke · BoundInvokeArg · BoundInvokeUniversal ·
BoundUniversalArg · BoundSetObjectRef · BoundMethodReturn) → `Binding/Bound/BoundOo.cs`, records-only
VERBATIM (the established rule — the generated visitor + VersionConformancePass key on namespace/shape).
(2) The verb machinery → `Verbs/OoBinder(ctx, host)`: OoBindInvoke §14.9.23 in every form (class/NEW ·
instance/interface · SELF/SUPER §8.4.3.8 · factory §11.4 · the D10 UNIVERSAL path) + the D6
USING/RETURNING marshaling ladder (OoBindResolvedInvoke/OoBindInvokeArg/OoContentMismatch — §14.8.2/.3) ·
OoBindSetObjectRef §14.9.39 F5 (+ OoExtractBareReference static for SetBinder's re-route) · the
§8.4.3.9.4 GR1–GR3 property desugar OoWrapPropertyOps (still invoked between the UDF and EC wraps at the
host BindStatement exit) · the D8 method returns (OoBindMethodGoback/OoBindExitMethod/OoBindMethodRaising).
(3) The roster/scope half stays HOST with the procedure table until the 10t ProcedureTableBuilder hoist:
BindMethodRoster (the class-roster entry-point twin of Bind()), the OoClasses/OoCurrentClass/OoInFactory
emitter-set properties (behind IOoBindHost until P9, per plan), InMethod/OoScopeHint, `_paraMethod`.
**The ambient ordered quadruple is GONE:** `SectionInfo` + `OoMethodScope` promoted to top-level
`Binding/Procedure/` types; `BinderContext` gains `CurrentSection`/`CurrentMethodScope` and the scoped
`EnterMethodScope(section, scope, pc)` → `BindPositionScope` token (section → method scope → §11.7 GR5
`Data.ActiveMethodScope` shadowing → cursor, set coherently, RESTORED coherently on dispose) — both bind
loops (program Bind() and the roster loop) now `using`-scope each pc; the host `_currentSection`/
`_currentMethodScope` fields are deleted (readers — AddParagraph, ResolveProcedure, the SetAlter-prepass
CurrentSection accessor — re-point to ctx). The COLLECTION cursor keeps a plain property set (no data
shadowing during collection — behavior identical). One predicted fix: the Declaratives partial needed
`using CobolNet.Binding.Procedure;` for the promoted SectionInfo.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact, OO snapshots included) · 281
unit · 3166 conformance (the OO goldens ride in the battery) — verdicts read as separate actions.

