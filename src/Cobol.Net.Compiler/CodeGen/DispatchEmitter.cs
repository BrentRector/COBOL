// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The PC dispatcher (COBOLNET_DESIGN §5; a real collaborator since P7 Step 9n): each paragraph is a
/// <c>case</c> in one <c>Dispatch</c> method; control is by pc value (GO TO sets pc; fall-through is pc+1; an
/// out-of-line PERFORM is a recursive bounded <c>Dispatch(start, end)</c>). STOP RUN unwinds all frames via
/// <c>StopRun</c>, caught at <c>Main</c>. This realizes the legacy's proven return-address / exit-bounded
/// dispatch (DEVLOG 259–260) in idiomatic C#. Parameterized over program AND class units — the
/// <c>__MDispatch</c> swap rides <see cref="DispatchState.DispatchName"/> (the OO emit-into-a-type reuse).
/// </summary>
internal sealed class DispatchEmitter(EmitContext ctx, DispatchState dispatchState, EcState ecState,
    AlterSwitchEmitter alterSwitch, ReportWriterEmitter reportWriter, SequentialIoEmitter seqIo,
    EcEmitter ec, StatementEmitter statements)
{
    /// <summary>Emit the single program-counter dispatcher for a program class: the <c>__Activate</c> entry
    /// (file registration at first activation — the IC114A lesson), the USE machinery when declared, and the
    /// instance <c>__Dispatch</c> over the full paragraph range.</summary>
    public void EmitDispatcher(BoundProgram bound, CodeWriter w)
    {
        int n = bound.Paragraphs.Count;
        // Hooks are needed for the program's OWN declaratives (GR4a) — or, with none, for the outward GR4b walk
        // to a containing program's GLOBAL declaratives (IC233A: the contained unit has no declaratives, yet its
        // failing OPEN must fire the outer's USE GLOBAL).
        dispatchState.UseDecls = bound.Declaratives is { Count: > 0 } || dispatchState.OuterGlobalUse;
        // Exception-checking (Format-3) PERFORM handler context: the declarative count + the first appended handler
        // pc let ControlFlowEmitter derive each handler's __useActive id (DeclCount + pc − base). Null base ⇒ no F3.
        dispatchState.DeclCount = bound.Declaratives?.Count ?? 0;
        dispatchState.F3HandlerBasePc = bound.F3HandlerBasePc;
        dispatchState.UnitHasResume = bound.Ec?.HasResume ?? false;   // §14.9.33.4 GR2 b)'s PERFORM landing
        // §14.9.18.4 GR6 (kb/Work PB409): the GLOBAL declaratives' __useActive slots, so a GOBACK can ask at run
        // time whether it is executing within one's RANGE. Empty for a program with no USE … GLOBAL, which keeps
        // such a program's generated source byte-identical.
        dispatchState.GlobalDeclIds = bound.Declaratives is { } gds
            ? [.. Enumerable.Range(0, gds.Count).Where(i => gds[i].Global)]
            : [];
        // X3.23-1985 USE FOR DEBUGGING procedure-trigger facility (VCR Table 7 row 7.17): active when a
        // procedure-subject debugging declarative was collected under WITH DEBUGGING MODE. Gates all debug
        // scaffolding (zero-scaffolding invariant — a non-debug program is byte-identical).
        dispatchState.DebugActive = bound.DebugSubjects is { Count: > 0 };
        dispatchState.DebugByPc = bound.DebugSubjects?.ToDictionary(s => s.SubjectPc)
            ?? new Dictionary<int, BoundDebugSubject>();
        w.Line();
        // The dispatcher internals use a `__` prefix — COBOL data-names cannot contain a double underscore — so they
        // never collide with a program's fields (e.g. a COBOL `01 N` and the paragraph count `__N`).
        w.Line($"private const int __N = {n};   // paragraph count");
        alterSwitch.EmitFields(bound, w);   // the per-altered-paragraph mutable GO TO target fields (control-flow design D4)
        reportWriter.EmitReportMembers(w);      // per-report engine fields + line compose methods (Verbs/ReportWriterEmitter.cs)
        if (dispatchState.DebugActive)
        {
            // The X3.23-1985 DEBUG-ITEM special register + its per-trigger transfer cause (VCR Table 7 row 7.17).
            w.Line("private readonly DebugItem __dbgItem = new DebugItem();   // X3.23-1985 DEBUG-ITEM register (VCR 7.17)");
            w.Line("private DebugCause __dbgCause;   // the transfer-of-control cause of the next debug trigger (default Transfer → SPACES)");
            w.Line("private int __dbgLine;   // the CAUSING statement's source line — DEBUG-LINE for a non-START-PROGRAM trigger");
            w.Line("private bool __dbgBusy;   // a debugging declarative is not itself debugged (re-entrancy guard)");
        }
        w.Line();
        using (w.Block("public void __Activate()"))
        {
            if (ctx.Data.Classification is { } cls)
                // ISO §12.3.6.4 GR8 / §14.6.6 r2 — the classification is established at the module's activation.
                w.Line(ObjectComputerEmit.ClassificationPrologue(cls));
            // Register this program's SELECTed files at FIRST ACTIVATION (the IC114A lesson: connectors
            // belong to the program's entry, not the run-unit Main; a fresh instance after CANCEL / an
            // INITIAL activation re-registers — ISO §14.6.2.3.2). For a UnitStaticFiles unit the guard is
            // STATIC — one registration per run unit, §14.6.2.3.3 last-used across recursive activations,
            // reset only on the initial-state cases via __ResetStatics (kb/Work PB168). Run-unit CloseAll
            // lives in the runtime RunMain boundary.
            if (ctx.Data.Files.Count > 0)
            {
                // ⛔ ONE GUARD PER SCOPE (kb/Work PB168 — the review fleet caught all three riding one flag):
                // connector REGISTRATION is run-unit-scoped for a UnitStaticFiles unit (the flag is static
                // there); report-engine CONSTRUCTION assigns per-INSTANCE fields and takes its own instance
                // guard below. There is no THIRD thing to guard any more: the LINAGE operand values and the
                // ASSIGN … USING association are arguments of the OPEN/WRITE statements that read them, so
                // nothing per-activation is installed on a connector at all (kb/Work PB673).
                using (w.Block("if (!__filesRegistered)"))
                {
                    w.Line("__filesRegistered = true;");
                    seqIo.EmitFileRegistration(w);
                }
                // Report engines construct AFTER the registration block (hazard: the report FD must be
                // registered before the engine's first write — COBOLNET_REPORT_WRITER_DESIGN §4; ordering
                // holds on every path: a skipped registration block means an EARLIER activation registered).
                if (ctx.Data.Reports.Count > 0)
                    using (w.Block("if (!__reportsConstructed)"))
                    {
                        w.Line("__reportsConstructed = true;");
                        reportWriter.EmitReportConstruction(w);
                    }
            }
            // Execution begins at the first NONdeclarative procedure (ISO §14.2.3 GR1) — declarative sections
            // occupy the pcs below EntryPc, entered only via __RunUse or an explicit PERFORM (ISO §14.9.49.3 SR4 —
            // no other statement may reference them from outside their section; kb/Work PB362).
            // X3.23-1985: the FIRST execution of the first nondeclarative procedure is DEBUG-CONTENTS "START PROGRAM".
            dispatchState.EmitDebugCause(w, "StartProgram");
            // The top-level run is bounded at the last MAIN paragraph (F3HandlerBasePc − 1) when the unit has
            // appended Format-3 handler pc-ranges above it, so fall-through off the last real paragraph ENDS the run
            // unit (§14.9.18) and never runs into the synthetic handlers; a non-F3 unit renders the literal -1 (the
            // whole pc space) — byte-identical (the wall, design §9.5.3). Only this top-level call is walled; every
            // bounded __RunUse / out-of-line PERFORM passes its own exit pc.
            int topExit = bound.F3HandlerBasePc is int hb ? hb - 1 : -1;
            w.Line($"try {{ __Dispatch({bound.EntryPc}, {topExit}); }} catch (ProgramReturn) {{ }}   // GOBACK / called-program EXIT PROGRAM returns to the activator here (ISO §14.9.18.4 GR2/GR3; §14.9.14.4 GR3)");
        }
        w.Line();
        // The machinery also emits for a declarative-FREE program whose statements carry enabled EC-I-O checking
        // (__IoCheckEc needs no declaratives to bridge status→EC and apply the fatal default) — gated so an
        // EC-free program's source is unchanged.
        if (dispatchState.UseDecls || bound.Ec is { HasIoChecked: true } || bound.Ec is { HasF3Perform: true })
            EmitUseMachinery(bound.Declaratives ?? [], bound.F3HandlerBasePc is int hb0 ? bound.Paragraphs.Count - hb0 : 0,
                bound.Ec is { HasIoChecked: true }, bound.Ec is { HasF3Perform: true }, w, asLocal: false);   // an F3 PERFORM needs __RunUse/__EcPerform even with no USE declaratives (§14.9.28)
        if (dispatchState.DebugActive) EmitDebugRunner(w);
        EmitDispatchMethod(bound, w, "private int __Dispatch(int __startPc, int __exitPc)",
            0, bound.Paragraphs.Count - 1);
    }

    // The dispatch-method NAME the statement emitters call for a bounded range is DispatchState.DispatchName
    // (Step 9b — see EmitterState.cs for the __Dispatch / __MDispatch contract).

    /// <summary>Emit one dispatch-method body over the pc slice [<paramref name="fromPc"/>..<paramref name="toPc"/>]
    /// — SHARED by program classes (via <see cref="EmitDispatcher"/>: the full range as the instance
    /// <c>__Dispatch</c>) and COBOL classes (<c>OoEmitMethod</c>: each METHOD-ID's contiguous slice of the
    /// class's ONE pc space as a local function; a pc outside the slice hits <c>default:</c> and exits — the
    /// emit-into-a-type parameterization of the OO deep-dive).</summary>
    public void EmitDispatchMethod(BoundProgram bound, CodeWriter w, string header, int fromPc, int toPc,
        int handlerFromPc = -1, int handlerToPc = -1)
    {
        void EmitCase(int i)
        {
            dispatchState.CurrentPc = i;
            string paraName = bound.Paragraphs[i].CobolName;   // "" for the paragraph-name-OMITTED paragraph (§14.4.3)
            using (w.Block($"case {i}:   // {(paraName.Length == 0 ? "(sentences without a paragraph-name)" : paraName)}"))
            {
                // An appended Format-3 handler pc-range (imp-2/3/4): mark the region so an EXIT PERFORM in
                // its body throws ExitPerformSignal to the owning PERFORM boundary (§14.9.14.4 GR4), not a
                // dispatcher break. Owner = F3HandlerOwners[i − base] (the PerformId).
                bool isHandler = bound.F3HandlerBasePc is int hb && i >= hb;
                var f3saved = isHandler
                    ? dispatchState.SetF3Region(F3Region.Handler, bound.F3HandlerOwners![i - bound.F3HandlerBasePc!.Value])
                    : default;
                // X3.23-1985 USE FOR DEBUGGING (VCR Table 7 row 7.17): a debug SUBJECT procedure fires
                // its debugging declarative just BEFORE its own body — populate DEBUG-ITEM from the
                // transfer cause (__dbgCause, set by whatever transferred control here) and run the
                // section over its bounded pc range.
                if (dispatchState.DebugActive && dispatchState.DebugByPc.TryGetValue(i, out var subj))
                    w.Line($"__RunDebug({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(subj.SubjectName, true)}, "
                        + $"{subj.SourceLine}, {subj.SectionStartPc}, {subj.SectionEndPc});");
                if (!EmitParagraphBody(bound.Paragraphs[i], i))
                {
                    // Sequential fall-through into pc+1 is DEBUG-CONTENTS "FALL THROUGH"; DEBUG-LINE is
                    // this paragraph's LAST statement (the causing statement that fell through, DB101A
                    // FALL-THROUGH-TEST :403-407).
                    dispatchState.EmitDebugCause(w, "FallThrough", bound.Paragraphs[i].SourceLine);
                    w.Line($"__pc = {i + 1};");
                    w.Line("break;");
                }
                if (isHandler) dispatchState.RestoreF3Region(f3saved);
            }
        }
        // Each dispatch method owns its transfer label: a `goto` may not leave the C# member it is written in, and
        // an OO method's __MDispatch is a LOCAL FUNCTION (design SSOT §9.10), so the program's label is not in
        // scope there. Save/restore keeps a nested emission (a class unit emitted while a program is open) honest.
        var savedXfer = dispatchState.BeginTransferScope(dispatchState.DispatchName == "__Dispatch" ? "__xfer" : "__mxfer");
        using (w.Block(header))
        {
            w.Line("int __pc = __startPc;");
            using (w.Block("while ((uint)__pc < (uint)__N)"))
            {
                w.Line("bool __atExit = __pc == __exitPc;   // captured before the body overwrites __pc");
                using (w.Block("switch (__pc)"))
                {
                    for (int i = fromPc; i <= toPc; i++) EmitCase(i);
                    // The method's Format-3 handler pc-ranges (design SSOT §9.10) — a NON-contiguous second case set
                    // (appended above the whole class pc space, so outside [EntryPc..EndPc]); entered ONLY via the
                    // method-local __RunUse(id, hpc, hpc). Empty (handlerFromPc < 0) for a program's __Dispatch — whose
                    // main loop [0..Count−1] already covers its appended handlers — so the program path is byte-identical.
                    if (handlerFromPc >= 0)
                        for (int i = handlerFromPc; i <= handlerToPc; i++) EmitCase(i);
                    using (w.Block("default:")) { w.Line("__pc = __N;"); w.Line("break;"); }
                }
                // The ONE dispatcher exit (kb/Work PB405): every statement that transfers control out of a
                // paragraph body sets __pc and jumps HERE, so no lowered C# container (an inline PERFORM's loop,
                // a GO TO … DEPENDING switch) can capture the jump the way a bare `break` was captured, and the
                // paragraph's fall-through epilogue — which is BEFORE this label, inside the case — can no longer
                // overwrite a target the statement set. Planted only when a transfer was rendered, so a
                // transfer-free unit's generated source is unchanged.
                if (dispatchState.TransferUsed)
                    w.Line($"{dispatchState.TransferLabel}: ;   // a transfer of control out of a paragraph (ISO §14.9.14.4 GR6/GR7, §14.9.17.4, §14.9.19.4 GR4/GR6, §14.9.33.4 GR3)");
                w.Line("if (__atExit && __pc == __exitPc + 1) return __pc;   // a named THRU exit paragraph COMPLETED (an explicit GO TO cleared __atExit — §14.6.3 rule 1)");
            }
            w.Line("return __pc;");
        }
        dispatchState.EndTransferScope(savedXfer);
    }

    /// <summary>Emit the X3.23-1985 debug trigger runner (VCR Table 7 row 7.17): space-fill DEBUG-ITEM, set
    /// DEBUG-LINE/DEBUG-NAME/DEBUG-CONTENTS for the triggering occurrence, then run the debugging declarative body
    /// over the bounded pc range. Guarded by the OBJECT-TIME switch (<c>RunUnit.DebugMode</c>, default ON — the CCVS
    /// posture) and a re-entrancy flag (a debugging declarative is not itself debugged). Emitted ONLY for a
    /// debug-active program.</summary>
    private void EmitDebugRunner(CodeWriter w)
    {
        using (w.Block("private void __RunDebug(string __nm, int __subjLine, int __ds, int __de)"))
        {
            w.Line("if (!RunUnit.Current.DebugMode) return;   // the X3.23-1985 object-time debug switch (default ON)");
            // ⛔ THIS RE-ENTRANCY GUARD IS NOT §14.9.49.4 GR2's, and deliberately raises no EC-FLOW-USE (kb/Work
            // PB368 asked the question of both guards). The two are EDITION-DISJOINT: USE FOR DEBUGGING is the
            // X3.23-1985 declarative ISO 2002 deleted (`use-for-debugging-removed-2002`, COBOLNET0902), and
            // EC-FLOW-USE is a 2002 EC-model name — at --std 85 there is no exception-name to set, and at 2002+
            // there is no debugging declarative to re-enter. GR2's guard is EmitRunUseBody's.
            w.Line("if (__dbgBusy) return;");
            w.Line("__dbgBusy = true;");
            // DEBUG-LINE: START PROGRAM has no causing statement — it is the subject's own first executable
            // statement (DB101A START-PROGRAM-TEST :257-264); every other cause is the causing (transferring)
            // statement, carried in __dbgLine by the transfer site.
            w.Line("int __line = __dbgCause == DebugCause.StartProgram ? __subjLine : __dbgLine;");
            w.Line("__dbgItem.Populate(__line, __nm, __dbgCause);");
            w.Line("try { __Dispatch(__ds, __de); } finally { __dbgBusy = false; }");
        }
        w.Line();
    }

    /// <summary>Emit the USE-declaratives machinery (ISO §14.9.49; emitted ONLY when the program declares USE
    /// procedures — a declarative-free program's generated source is byte-identical): the per-section
    /// re-entrancy guards (GR2 — instance state, reset by a fresh activation instance §14.6.2.3.2), the
    /// <c>__RunUse</c> bounded-dispatch invoker, and the <c>__IoCheck</c> selector (GR3/GR5/GR6 + §9.1.13.1:
    /// after an unsuccessful I-O status not covered by the statement's own AT END / INVALID KEY phrase, run at
    /// most ONE declarative — file-scoped first, then the open-mode scope incl. a file in the process of being
    /// opened).
    /// <para>⛔ THE ONE SELECTION-MACHINERY EMITTER for a program AND an OO method (kb/Work PB1010): a method whose
    /// own declaratives (§14.2.2 SR10 admits them) or Format-3 PERFORM give it a selection scope calls it with
    /// <paramref name="asLocal"/> true, so every member below becomes a LOCAL FUNCTION of the emitted method —
    /// capturing the method's locals (its LINKAGE / LOCAL-STORAGE data, which its USE procedures reference) and
    /// shadowing any class-level funnel of the same name, which is how each raise site in the body reaches the
    /// declaratives of the source element containing it (§14.9.49.4 GR4 a)) and never a sibling method's.
    /// <paramref name="f3Handlers"/> is the handler slot count above the declarative slots — the unit's own
    /// appended handlers for a program, the CLASS total for a method (the ONE <c>HandlerUseId</c> formula).</para></summary>
    public void EmitUseMachinery(IReadOnlyList<BoundDeclarative> decls, int f3Handlers, bool hasIoChecked,
        bool hasF3Perform, CodeWriter w, bool asLocal)
    {
        string mod = EcEmitter.MemberMod(asLocal);
        // The exception-checking (Format-3) PERFORM handler bodies (imp-2/3/4) are appended pc-ranges invoked by the
        // SAME __RunUse; each needs a __useActive slot above the declarative slots (§14.9.28.4 GR17). H is 0 until the
        // pc-range synthesis wave, so a non-F3-PERFORM unit's array is byte-identical.
        if (decls.Count > 0 || hasF3Perform)
        {
            w.Line($"{(asLocal ? "" : "private readonly ")}bool[] __useActive = new bool[{decls.Count + f3Handlers}];   // §14.9.49.4 GR2 re-entrancy guards");
            if (ecState.Active)
                // The EC-model form: __RunUse RETURNS the declarative's resume action (the dispatch result
                // protocol — EcEmitter): a RESUME statement unwinds via ResumeSignal (§14.9.33; the
                // StopRun/ProgramReturn exception-as-control precedent) and __RunUse converts it to the
                // action; normal completion is -1 (§14.6.13.1.2). Emitted ONLY when the group uses the
                // EC model — an EC-free build keeps the void form byte-identical.
                using (w.Block($"{mod}int __RunUse(int __id, int __startPc, int __endPc)")) EmitRunUseBody(w, ecModel: true);
            else
                using (w.Block($"{mod}void __RunUse(int __id, int __startPc, int __endPc)")) EmitRunUseBody(w, ecModel: false);
            w.Line();
        }
        if (decls.Any(d => d.EcEntries is not null)) ec.EmitDispatchSelector(decls, w, asLocal);
        if (decls.Any(d => d.Eo is not null)) ec.EmitObjDispatchSelector(decls, w, asLocal);   // F4 (EC-OO)
        if (hasIoChecked) ec.EmitIoCheckEc(decls, w, asLocal);
        if (ecState.UnitHasF3Perform) ec.EmitPerformInterceptor(w, asLocal);   // __EcPerform + __RunF3 (§14.9.28 F3 interceptor)
        if (!dispatchState.UseDecls) return;   // an EC-only program (no F1/F2 declaratives) needs no plain __IoCheck hooks
        // Under the EC model __RunUse RETURNS the declarative's resume action, and __IoCheck must HAND IT BACK
        // to the verb site — the old `__RunUse(…); return;` discarded it, so a Format-1 declarative's RESUME AT
        // procedure-name never transferred on the plain (non-EC-masked) path of any I-O verb (kb/Work PB141;
        // RESUME NEXT STATEMENT survived only because discarding −1 happens to mean fall-through). An EC-free
        // build keeps the void form byte-identical.
        // ⛔ THE HOOK REPORTS WHETHER A PROCEDURE RAN (kb/Work PB993). Every explicit I-O statement treats "no
        // declarative applied" and "a declarative completed normally" alike — both fall through — but the SORT/MERGE
        // implicit transfers do not: MERGE's rules turn on "an applicable USE procedure that completes normally"
        // (§14.9.24.4 GR7 a), GR12 a)/b)). So the EC-model form answers -3 (no procedure) apart from -1 (ran,
        // completed normally), exactly as __IoCheckEc and __EcDispatch already do, and the EC-free form answers a
        // bool (a void __RunUse that returns HAS completed normally — RESUME is an EC-model statement, and every
        // other not-normal completion unwinds by its own signal). Callers that do not need the answer discard it.
        bool ecInt = ecState.Active;
        string none = ecInt ? "return -1;" : "return false;";
        using (w.Block($"{mod}{(ecInt ? "int" : "bool")} __IoCheck(string __f, bool __atEnd, bool __invKey)"))
        {
            w.Line($"string __st = {RuntimeApi.FileStatus("__f")};");
            w.Line($"if (__st.Length == 0 || {IoStatusClass.Successful("__st")}) {none}   // successful — no declarative (ISO §14.9.49.4 GR6)");
            w.Line($"if (__atEnd && {IoStatusClass.AtEnd("__st")}) {none}    // the statement's AT END phrase covers the at-end family (§9.1.13.1)");
            w.Line($"if (__invKey && {IoStatusClass.InvalidKey("__st")}) {none}   // the statement's INVALID KEY phrase covers its family (§9.1.13.1)");
            string run(int i) => ecInt
                ? $"return {dispatchState.RunUseCall(i, decls[i].Range)};"
                : $"{dispatchState.RunUseCall(i, decls[i].Range)}; return true;";
            // The GR3a/GR5 file-name tier and the GR3b/GR6b–e open-mode tier are rendered by the ONE
            // UseTierEmitter that __IoCheckEc and __RunGlobalUse also use. Both tiers are EDITION-INVARIANT —
            // the determination is written once, on UseTierEmitter itself (kb/Work PB344).
            UseTierEmitter.EmitScopeTiers(w, decls, run);
            // No local declarative qualified — walk OUTWARD to the nearest containing program with a USE GLOBAL
            // declarative (ISO §14.9.49.4 GR4b: "a qualifying declarative with the GLOBAL attribute in the next
            // inclusive directly containing source element", repeated outward). The declarative executes in the
            // DECLARING program's instance — its data (§8.4.6.2) — via the container's __RunGlobalUse (whose
            // RESUME, if any, resolves in the DECLARING program's own dispatch, never this one's pc space).
            if (dispatchState.OuterGlobalUse)
                w.Line(ecInt ? "if (__outer.__RunGlobalUse(__f)) return -1;" : "if (__outer.__RunGlobalUse(__f)) return true;");
            w.Line(ecInt ? "return -3;   // no qualifying declarative (the __EcDispatch convention)" : "return false;");
        }
        w.Line();
    }

    /// <summary>Emit the shared <c>__RunUse</c> body (bounded-dispatch invoker, ISO §14.9.49.4 GR2 re-entrancy guard).
    /// Renders <see cref="DispatchState.DispatchName"/> — <c>__Dispatch</c> as a class member (a program), <c>__MDispatch</c>
    /// as a method-local function (an OO method's F3 PERFORM, design SSOT §9.10) — so the ONE body serves both scopes
    /// (the C3 correction: the former literal <c>__Dispatch</c> hardcode would name a nonexistent method inside a class).
    /// <paramref name="ecModel"/> = the int RESUME-returning form (an EC-model group) vs the void form.
    /// <para>⛔ <b>THE GUARD IS THE RAISE SITE</b> (kb/Work PB368). §14.9.49.4 GR2's sole normative content is
    /// the condition, not the decline: "During the execution of a USE procedure, if a statement raises an
    /// exception condition that would cause the execution of a USE procedure that had previously been activated
    /// and had not yet returned control to the activating entity, the EC-FLOW-USE exception condition is set to
    /// exist." This body is the ONE place in the compiler that knows that has happened — every selection path
    /// (<c>__IoCheck</c>, <c>__IoCheckEc</c>, <c>__EcDispatch</c>, <c>__EcObjDispatch</c>, <c>__RunGlobalUse</c>
    /// and the report engine's BEFORE REPORTING hook) reaches the active procedure only through here — so the
    /// raise is written here and every path inherits it. With checking enabled the fatal condition unwinds to
    /// the RAISING statement's own guard for the §14.6.13.1.3 #5/#7 dispatch (a USE AFTER EC EC-FLOW-USE
    /// declarative, its RESUME, else abnormal termination); with checking off it is not raised
    /// (§14.6.13.1.1) and the quiet decline below is the #8 implementor answer.</para>
    /// <para>⛔ Only a DECLARATIVE id is a USE procedure. The ids at and above <see cref="DispatchState.DeclCount"/>
    /// are the exception-checking PERFORM's imp-2/3/4 handler ranges, which share this array and this invoker but
    /// are selected by §14.9.28.4 GR17 — they are not USE procedures and GR2 does not reach them. An OO method's
    /// method-local <c>__RunUse</c> is ALL handlers (DeclCount 0), so it emits the bare guard unchanged.</para></summary>
    internal void EmitRunUseBody(CodeWriter w, bool ecModel)
    {
        string ret = ecModel ? " -1" : "";
        // The void form belongs to an EC-FREE group (EmitUseMachinery picks it on !ecState.Active), and enabling
        // EC-FLOW-USE checking anywhere makes the group EC-active by construction — EcBinder.EcWrap binds a
        // BoundEcChecked for it, which is EcFeatures.HasChecked. So the void arm provably cannot need the raise,
        // and skipping it there keeps a declarative-bearing EC-free program byte-identical (SSOT §18.16).
        int decls = ecModel ? dispatchState.DeclCount : 0;
        if (decls > 0)
        {
            using (w.Block("if (__useActive[__id])"))
            {
                // The id test is emitted only where handler slots exist; with none, every id is a declarative.
                string only = dispatchState.F3HandlerBasePc is not null ? $"if (__id < {decls}) " : "";
                w.Line($"{only}ExceptionState.FlowUseError(\"USE declarative \" + __id + \" is already active"
                    + " (ISO 14.9.49.4 GR2 - a USE procedure activated during its own execution)\");");
                w.Line($"return{ret};   // not re-invoked — the §14.6.13.1.3 #8 outcome when checking is off");
            }
        }
        else
            w.Line($"if (__useActive[__id]) return{ret};   // ISO §14.9.49.4 GR2 — an active USE procedure is not re-invoked");
        w.Line("__useActive[__id] = true;");
        if (ecModel)
        {
            // The procedure's statements are OTHER source text than the statement whose raise selected it, and
            // that statement's guard flags are still standing here — so the procedure starts from all-off and each
            // of its statements sets only what its own line enables (§7.3.25.4 GR6; kb/Work PB891). This is also
            // §14.9.28.4 GR14's implicit PUSH ALL + TURN OFF ALL for the F3 handlers imp-2/3/4, which run here.
            w.Line("var __ckU = ExceptionState.PushAllCheckingOff();   // the procedure's checking baseline (§7.3.25.4 GR6)");
            w.Line($"try {{ {dispatchState.DispatchName}(__startPc, __endPc); }}");
            w.Line("catch (ResumeSignal __rs) { return __rs.TargetPc; }   // RESUME (§14.9.33) — the resume action");
            w.Line("finally { __useActive[__id] = false; ExceptionState.RestoreChecking(__ckU); }");
            w.Line("return -1;   // normal completion (§14.6.13.1.2)");
        }
        else
            w.Line($"try {{ {dispatchState.DispatchName}(__startPc, __endPc); }} finally {{ __useActive[__id] = false; }}");
    }

    /// <summary>Emit a paragraph body SENTENCE by sentence. When the paragraph contains a NEXT SENTENCE anywhere,
    /// each inter-sentence boundary gets a label (`__sentP_K:`) — the §14.9.19.4 GR4/GR6 implicit CONTINUE after the
    /// separator period; NEXT SENTENCE in the LAST sentence is the paragraph fall-through. Returns whether the
    /// body ends by transferring control out of the case.</summary>
    private bool EmitParagraphBody(BoundParagraph para, int pc)
    {
        var w = ctx.Writer;
        var sentences = para.Sentences;
        bool needLabels = sentences.Any(ContainsNextSentence);
        bool terminated = false;
        for (int k = 0; k < sentences.Count; k++)
        {
            bool last = k == sentences.Count - 1;
            dispatchState.SentenceEndLabel = needLabels && !last ? $"__sent{pc}_{k}" : null;
            // Unlike intra-sentence dead code, a LABELLED sentence boundary is reachable via NEXT SENTENCE even
            // after an unconditional transfer — so only skip remaining sentences when no labels exist.
            if (terminated && !needLabels) break;
            terminated = statements.EmitStatementList(sentences[k]);
            if (needLabels && !last) w.Line($"__sent{pc}_{k}: ;");
        }
        dispatchState.SentenceEndLabel = null;
        return terminated;
    }

    /// <summary>True when a statement list contains a <see cref="BoundNextSentence"/> anywhere — recursing over the
    /// generated <see cref="BoundStatementTree.StatementChildren"/> (PHASE-07 Step 6g), which is the ONE drift-proof
    /// enumeration of every nesting container the binder produces (IF/EVALUATE arms, inline-PERFORM & SEARCH bodies,
    /// READ/keyed/WRITE/CALL/arithmetic phrases), so a new container node is covered automatically. A missed container
    /// would emit a goto with NO label → a loud backend compile failure, never a silent misjump. (Replaces the former
    /// hand-maintained walker, which had missed EVALUATE/CALL/WRITE phrase bodies.)</summary>
    private static bool ContainsNextSentence(IReadOnlyList<BoundStatement> stmts) => stmts.Any(HasNextSentence);

    private static bool HasNextSentence(BoundStatement s) =>
        s is BoundNextSentence || s.StatementChildren().Any(HasNextSentence);
}
