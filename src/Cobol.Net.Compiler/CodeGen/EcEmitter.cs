// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;
using CobolNet.Runtime;

/// <summary>
/// The EC exception-condition slice of the Roslyn backend (ISO/IEC 1989:2023 §14.6.13;
/// COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D9–D12 as-built): renders the per-statement guards a
/// <see cref="BoundEcChecked"/> wrapper carries, the RAISE/RESUME statements, and the GENERATED dispatch
/// machinery — <c>__EcDispatch</c> (the §14.9.49.4 GR3c–g Format-3 selector over the program's USE AFTER
/// EXCEPTION CONDITION declaratives) and <c>__IoCheckEc</c> (the §9.1.13.1 status→EC bridge). EVERY artifact here
/// is gated: a compilation group with no enabling TURN, no F3, no RAISE/RESUME/RAISING and no EXCEPTION-*
/// function emits byte-identical source to a pre-EC build (the zero-scaffolding invariant, SSOT §18.16).
/// <para><b>The dispatch result protocol</b> (shared by <c>__RunUse</c>/<c>__EcDispatch</c>/<c>__IoCheckEc</c>) is the
/// runtime's <see cref="DispatchResult"/>: every value is rendered by NAME (<c>DispatchResult.Normal</c>,
/// <c>ResumeNext</c>, <c>NoHandler</c>, <c>HandledNonfatal</c>, <c>NotNormal</c>) and every question a consumer asks
/// by PREDICATE (<c>IsTransfer</c>, <c>SuppressesFatal</c>, <c>TerminatesSortMerge</c>), never by a bare literal —
/// <c>DispatchResultProtocolDriftTests</c> pins that (kb/Work PB1122 Task A, PB1761). The values' meanings are
/// documented once, on <see cref="DispatchResult"/>.</para>
/// </summary>
internal sealed class EcEmitter(EmitContext ctx, EcState ecState, DispatchState dispatch)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/>: the EC↔statement
    /// cycle (<c>EmitChecked</c> re-enters <c>EmitStatement</c>; statements contain EC-checked children) is
    /// the edge the coupling census proved no ctor order can satisfy.</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>The per-statement raise-site dispatch expression. When this unit has an exception-checking
    /// (Format-3) PERFORM (§14.9.28), every raise site routes through <c>__EcPerform</c> — which consults the
    /// ambient F3-frame stack FIRST (GR17: a matching WHEN preempts the USE declaratives) and falls to
    /// <c>__EcDispatch</c> only on no-match. Otherwise the historical funnel: <c>__EcDispatch</c> when the unit has
    /// F3 declaratives, else the no-declarative constant. A non-F3-PERFORM unit emits byte-identical text.</summary>
    public string EcDispatchExpr(string ecNameExpr, string fileExpr) =>
        ecState.UnitHasF3Perform ? $"__EcPerform({ecNameExpr}, {fileExpr})"
        : ecState.UnitHasF3       ? $"__EcDispatch({ecNameExpr}, {fileExpr})"
        :                           "DispatchResult.NoHandler";

    /// <summary>Does <see cref="EcDispatchExpr"/> render a real selector for this unit (rather than the
    /// no-declarative constant)? Asked by the two emissions that must AGREE with it and with each other: the
    /// <c>INonfatalSelector.NonfatalDispatch</c> override (ProgramEmitter — the runtime raise path's entry into the
    /// selection) and the RESUME landing the same path needs (<see cref="EmitGatesOrInner"/>). If one were
    /// emitted without the other a <c>RaiseResumeSignal</c> would have no landing site, so the condition is
    /// spelled ONCE here rather than at both (kb/Work PB367b).</summary>
    public bool UnitHasDispatchFunnel => ecState.UnitHasF3 || ecState.UnitHasF3Perform;

    /// <summary>The <c>__EcObjDispatch</c> invocation (or the no-declarative constant when this unit has no
    /// Format-4 declaratives) — the §14.9.49.4 GR14 exception-OBJECT selector (the EC-OO wave).</summary>
    public string ObjDispatchExpr(string objExpr) =>
        ecState.UnitHasF4 ? $"__EcObjDispatch({objExpr})" : "DispatchResult.NoHandler";

    // ── The raise-site selection: ONE place for "select, land the RESUME, apply the fatal default" ────────────

    /// <summary>Emit the processing of an exception condition an emitted raise site has just set to exist: the
    /// §14.9.49 / §14.9.28 selection (<see cref="EcDispatchExpr"/>), the RESUME landing, and — for a FATAL
    /// condition — the default ISO §14.6.13.1.3 5) and 7) prescribe: "If execution of the declarative completes
    /// normally the execution of the run unit is terminated abnormally", and with no handler at all "If checking
    /// for the exception condition is enabled, execution of the run unit is terminated abnormally as specified in
    /// 14.6.12". Only RESUME (NOTE 2 — <c>ResumeNext</c>, or a <c>IsTransfer</c> result the landing already took) lets
    /// execution continue; a nonfatal condition continues in every case (§14.6.13.1.4 3)/4)).
    /// <para>⛔ THIS IS THE ONE PLACE THE FATAL DEFAULT IS DECIDED (kb/Work PB1549). Every emitted raise site used
    /// to spell <c>dispatch + ResumeTransfer</c> itself and was separately responsible for remembering the
    /// <c>if (r != -2) throw</c> after it; the SET … ADDRESS OF PROGRAM / FUNCTION raise sites (fatal
    /// EC-PROGRAM-NOT-FOUND, EC-FUNCTION-NOT-FOUND, EC-FUNCTION-PTR-INVALID) forgot, and a failed SET with checking
    /// on and no declarative CONTINUED the run unit while the CALL arm of the same condition terminated it.
    /// <c>EcRaiseSelectionDriftTests</c> pins that no emitter outside this class renders the selector.</para>
    /// </summary>
    /// <param name="ecNameExpr">The C# expression naming the raised level-3 exception-name.</param>
    /// <param name="terminate">The C# statement that terminates the run unit (normally <see cref="FatalTermination"/>),
    /// or <c>null</c> for a NONFATAL condition. Prefer <see cref="EmitConditionRaise"/>, which takes the fatality
    /// from Table 13 instead of from the caller, whenever the name is known at compile time.</param>
    /// <param name="fatalWhen">An extra C# condition ANDed onto the fatal default, for a site whose name — and so its
    /// fatality — is chosen at run time (the GOBACK … RAISING pickup's propagated fatal flag).</param>
    public void EmitSelection(string ecNameExpr, string? terminate, string? fatalWhen = null)
    {
        var w = ctx.Writer;
        string r = $"__r{ctx.Names.NextEc()}";
        w.Line($"int {r} = {EcDispatchExpr(ecNameExpr, "\"\"")};");
        // The ONE landing — it unwinds by itself inside an operand-evaluation step (kb/Work PB892, PB1432).
        w.Line(dispatch.ResumeTransfer(r));
        if (terminate is null)
            return;   // nonfatal: declarative completed / RESUME NEXT / no handler all continue (§14.6.13.1.4 3)/4))
        string when = fatalWhen is null ? "" : $" && {fatalWhen}";
        // §14.6.13.1.3 6) sits between 5) (a declarative ran: Normal terminates below) and 7): with NO handler (NoHandler) and
        // the element under >>PROPAGATE ON, the condition propagates as if GOBACK RAISING LAST EXCEPTION.
        if (PropagationReturn(ecNameExpr) is { } propagate)
            w.Line($"if ({r} == DispatchResult.NoHandler{when}) {propagate}   // no handler: automatic propagation (§14.6.13.1.3 6); §7.3.21.4 GR2)");
        w.Line($"if ({r} != DispatchResult.ResumeNext{when}) {terminate}"
            + "   // fatal, not resumed → abnormal run-unit termination (§14.6.13.1.3 5)/7))");
    }

    /// <summary>⛔ THE ONE RENDERING OF AUTOMATIC PROPAGATION of an unhandled fatal condition (ISO §14.6.13.1.3 6); the
    /// §7.3.21.4 GR2 effect of <c>&gt;&gt;PROPAGATE ON</c>; kb/Work PB1119): stage the condition as the LAST exception for
    /// the activator (<c>ExceptionState.StageAutomaticPropagation</c>, which excludes EC-FLOW-GLOBAL-EXIT and
    /// EC-FLOW-GLOBAL-GOBACK) and return as a GOBACK does — <c>ProgramReturn</c> from a called program or function,
    /// <c>MethodReturn</c> from a method. Null when the element being emitted is not under PROPAGATE ON (GR4's
    /// default), so an element without the directive emits byte-identical text. Its two callers are the fatal
    /// defaults: <see cref="EmitSelection"/> and the generated <c>__IoCheckEc</c> (the EC-I-O arm, which §14.6.13.1.3
    /// 3) sends on to the same rules).</summary>
    private string? PropagationReturn(string ecNameExpr) => ecState.Propagation switch
    {
        null => null,
        { InMethod: true } => $"if (ExceptionState.StageAutomaticPropagation({ecNameExpr})) throw new MethodReturn();",
        _ => $"if (__asCalled && ExceptionState.StageAutomaticPropagation({ecNameExpr})) throw new ProgramReturn();",
    };

    /// <summary>§14.6.13.1.5's EXIT/GOBACK item 3 — the ACTIVATOR's automatic propagation of an exception OBJECT that
    /// no declarative took (kb/Work PB1119): "if a PROPAGATE ON directive is in effect for the activating runtime
    /// element, the exception is propagated as if a GOBACK statement with the RAISING LAST EXCEPTION phrase were
    /// specified in this activating runtime element", or with EXCEPTION EC-OO-EXCEPTION when the object is of no
    /// class or interface this element's PROCEDURE DIVISION header RAISING phrase specifies. Null when the element is
    /// not under PROPAGATE ON; then item 4 (the caller's EC-OO-EXCEPTION conversion) applies.</summary>
    public string? ObjectPropagationReturn(string objExpr)
    {
        if (ecState.Propagation is not { } p) return null;
        // "as if a GOBACK statement with the RAISING LAST EXCEPTION phrase" — whose object status stages the object when
        // it is applicable and "EXCEPTION EC-OO-EXCEPTION, instead of LAST EXCEPTION" when no applicable class or
        // interface is specified in the header (the one SetPropagatingLast, item 1's test too).
        string applicable = ObjectApplicableTest(objExpr);
        return p.InMethod
            ? $"{{ ExceptionState.SetPropagatingLast({applicable}); throw new MethodReturn(); }}"
            : $"if (__asCalled) {{ ExceptionState.SetPropagatingLast({applicable}); throw new ProgramReturn(); }}";
    }

    /// <summary>⛔ §14.6.13.1.5 EXIT/GOBACK item 1's test, rendered ONCE for items 1 and 3: is <paramref name="objExpr"/>
    /// "a) an object whose class is specified or whose class is a subclass of a class specified in the RAISING phrase of
    /// the procedure division header … or b) an object that implements an interface specified in" it. <c>false</c> when
    /// the element's header names no class or interface (<see cref="EcState.PdRaisingObjectCsTypes"/>), because then no
    /// object is applicable (kb/Work PB1121).</summary>
    public string ObjectApplicableTest(string objExpr) =>
        ecState.PdRaisingObjectCsTypes.Count == 0
            ? "false" : $"{objExpr} is {string.Join(" or ", ecState.PdRaisingObjectCsTypes)}";

    /// <summary>The standard terminate statement of <see cref="EmitSelection"/>: a <c>CobolFatalException</c>
    /// pre-marked <c>Dispatched</c> (one dispatch per raise — kb/Work PB75 — so an enclosing statement's guard lets
    /// it pass to the §14.6.12 run-unit boundary instead of selecting a second time).</summary>
    public static string FatalTermination(string ecNameExpr, string reasonExpr) =>
        $"throw new CobolFatalException({ecNameExpr}, {reasonExpr}) {{ Dispatched = true }};";

    /// <summary>Set the compile-time-named level-3 condition <paramref name="ecName"/> to exist (§14.6.13.1.1),
    /// with its fatality taken from Table 13 (<see cref="ExceptionCatalog"/>) — never from the caller. The status
    /// half of <see cref="EmitConditionRaise"/>, alone for a site whose own conditional phrase takes the condition
    /// (§14.6.13.1.3 1) / §14.6.13.1.4 1)).</summary>
    public void EmitConditionSet(string ecName, string why)
    {
        var info = CataloguedLevel3(ecName);
        ctx.Writer.Line($"ExceptionState.Set({CsLiteral(info.Name)}, {(info.IsFatal ? "true" : "false")});   // {why}");
    }

    /// <summary>Raise a compile-time-named level-3 condition at an emitted raise site whose checking is enabled:
    /// <see cref="EmitConditionSet"/> then <see cref="EmitSelection"/>, the fatal default applied exactly when
    /// Table 13 says the condition is fatal. The form every constant-name raise site uses, so "is this condition
    /// fatal?" is answered by the catalog rather than remembered per emitter.</summary>
    public void EmitConditionRaise(string ecName, string why)
    {
        var info = CataloguedLevel3(ecName);
        EmitConditionSet(ecName, why);
        string name = CsLiteral(info.Name);
        EmitSelection(name, info.IsFatal
            ? FatalTermination(name, CsLiteral($"{info.Name} was set to exist and not resumed (ISO 14.6.13.1.3 5)/7))"))
            : null);
    }

    private static EcInfo CataloguedLevel3(string ecName) =>
        ExceptionCatalog.TryGet(ecName, out var info) && info.Level == 3
            ? info
            : throw new InvalidOperationException(
                $"'{ecName}' is not a catalogued level-3 exception-name (§14.6.13.1.6 Table 13) — an emitter bug");

    /// <summary>Emit the statement an OPERAND-EVALUATION step was specified in (kb/Work PB892, PB1432) — the landing
    /// <see cref="DispatchState.ResumeTransfer"/> unwinds to from inside such a step. ISO §14.9.33.4 GR2 a) 2. makes
    /// that statement the applicable one for a condition a function reference or an inline invocation propagates,
    /// GR2 a) 1. for one a §15.4 subscript temporary store raises, and GR2 a) 3. makes it the LOWEST such
    /// statement, which is why every statement that drained a step carries its own
    /// landing: the nearest one catches. RESUME AT procedure-name transfers (GR3); RESUME AT NEXT STATEMENT falls
    /// out after the statement (GR2).
    /// <para>Only a unit whose selection machinery can return a RESUME emits the landing — the same
    /// zero-scaffolding reasoning as the nonfatal-gate landing in <see cref="EmitGatesOrInner"/>: with no
    /// <c>__EcDispatch</c>/<c>__EcPerform</c> and no <c>__EcObjDispatch</c> every pickup answers "no qualifying
    /// declarative" and nothing is ever thrown.</para></summary>
    public bool EmitActivationSite(BoundActivationSite site)
    {
        if (!ecState.Active || !(UnitHasDispatchFunnel || ecState.UnitHasF4))
            return Statements.EmitStatement(site.Inner);
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        using (w.Block("try"))
            Statements.EmitStatement(site.Inner);
        // A `goto` out of a catch CLAUSE is legal C#, so the landing uses the dispatcher-transfer idiom (PB405).
        w.Line($"catch (RaiseResumeSignal __as{id}) {{ {dispatch.ResumeTransfer($"__as{id}.TargetPc", "")} }}"
            + "   // RESUME after a condition an operand activation propagated: the statement it was specified in "
            + "(§14.9.33.4 GR2 a) 2./3.; GR3)");
        return false;   // conservative: a RESUME may continue after a statement that otherwise transfers
    }

    /// <summary>RAISE identifier-1 (ISO §14.9.29.4 GR2; §14.6.13.1.5): set EXCEPTION-OBJECT, run the F4
    /// declarative if one matches (GR14 — GR3: F4 REPLACES the F1/F3 tiers for object raises), and in EVERY
    /// no-match/complete case continue with the next statement — a RAISE of an object is NEVER fatal by
    /// itself.</summary>
    public bool EmitRaiseObject(BoundRaiseObject ro)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        w.Line($"ExceptionState.SetObject({(ro.Source is { } roSrc ? RuntimeApi.AsExceptionObject(PlaceRenderer.Read(roSrc)) : "this")});   // §14.6.13.1.5 (1)/(2) — EXCEPTION-OBJECT + the status sentinel");
        w.Line($"int __r{id} = {ObjDispatchExpr($"ExceptionState.ExceptionObject")};");
        w.Line(dispatch.ResumeTransfer($"__r{id}"));
        w.Line($"// Normal/ResumeNext/NoHandler: declarative completed / RESUME NEXT / no match — continue after RAISE (§14.9.29.4 GR2)");
        return false;   // the continue-after-RAISE path IS the normal exit (GR2 — never fatal by itself)
    }

    // The former EcStmtLoc/EcStmtLocExpr per-site (stmt, loc) baking is DELETED (kb/Work R14): the pair now
    // travels on the runtime's AMBIENT statement context, entered once per checked statement by
    // <see cref="EmitChecked"/> with exactly the WITH-LOCATION names (per-condition, R06's rule), and every
    // raise site — emitted OR runtime-internal — reaches it through the 2-argument ExceptionState.Set. One
    // rule, one place; the sites an emitter could never thread (SEARCH's range Sets, CONTINUE AFTER,
    // CobolString/CobolDynString/CobolTiming) answered 63 spaces under WITH LOCATION for exactly as long as
    // the two mechanisms coexisted.

    // ── The BoundEcChecked wrapper (the statement EC context + the EC-ARGUMENT-FUNCTION ambient gate) ────────

    /// <summary>The NONFATAL ambient per-statement EC gates — each rides a run-unit-scoped
    /// <c>ExceptionState.XxxChecking</c> flag its runtime raise site consults, set inside the statement's checking scope (no
    /// catch, no throw — nonfatal ⇒ the raise only records the last exception status). Fixed order for
    /// byte-stability of the generated wrapper (a statement enabling one emits exactly the pre-generalization
    /// output). The fatal twins (EC-ARGUMENT-FUNCTION) stay in <see cref="EmitArgOrPlain"/> — they need a catch.</summary>
    internal static readonly (string Ec, string Flag)[] NonfatalAmbientGates =
    [
        ("EC-DATA-CONVERSION", "DataConversionChecking"),   // §15.19.4 r1/r3 — CONVERT / DISPLAY-OF / NATIONAL-OF
        ("EC-BOUND-OVERFLOW", "BoundOverflowChecking"),     // §8.5.1.9.6 GR1 — OCCURS DYNAMIC implicit growth
        ("EC-BOUND-SET", "BoundSetChecking"),               // §14.9.39.4 GR30 — OCCURS DYNAMIC explicit SET (kb/Work PB460)
        // These two USED TO CARRY THEIR OWN, SECOND enablement mechanism — the ThruMember carrier was emitted
        // only under checking, and SetSize took a `checkStorage` argument — which put their raises outside the
        // engine's (flag, name) pair and therefore outside the §14.6.13.1.4 #3 selection it runs (kb/Work PB367b).
        ("EC-RANGE-INVALID", "RangeInvalidChecking"),       // §14.7.8 r2 — an inverted alphanumeric/national THRU range
        ("EC-STORAGE-NOT-AVAIL", "StorageNotAvailChecking"),// §14.9.39 F16 GR37/GR38 — a dynamic-length resize
        // The Report Writer's NONFATAL raise sites (kb/Work PB1247 / PB1130 / PB1188), all raised while a GENERATE or
        // TERMINATE produces the report. Each resumes where §14.9.16.4 GR8 / §14.9.46.4 GR5 put it — "at the next
        // report item, line, or report group, whichever follows in logical order": CobolReport.PresentLine abandons
        // an overlapping LINE and, past the page limit, the rest of the GROUP; CobolReport.Place abandons an
        // overlapping ITEM; a page-width violation is truncated and printed as §13.18.14.4 GR5 states outright.
        ("EC-REPORT-LINE-OVERLAP", "ReportLineOverlapChecking"),// §13.18.35.4 GR3 — overlapping report lines (GENERATE / TERMINATE)
        ("EC-REPORT-PAGE-LIMIT", "ReportPageLimitChecking"),    // §13.18.35.4 GR2 — a report group split between two pages
        ("EC-REPORT-COLUMN-OVERLAP", "ReportColumnOverlapChecking"),// §13.18.14.4 GR4 — two printable items in one column
        ("EC-REPORT-PAGE-WIDTH", "ReportPageWidthChecking"),    // §13.18.14.4 GR5 — a printable item past the page width
    ];

    /// <summary>Open the EC region of a NESTED SOURCE statement list — an IF branch, an inline-PERFORM body, an
    /// ON SIZE ERROR / AT END / INVALID KEY phrase, a SEARCH or EVALUATE arm, imperative-statement-1 or the
    /// FINALLY phrase of an exception-checking PERFORM. <see cref="EcState.Info"/> is the region of the ONE
    /// statement being emitted (§7.3.25.4 GR6 keys enablement on the statement's own source LINE), and every
    /// member of such a list is a statement of its own that went through <c>StatementBinder.BindStatement</c>
    /// and therefore carries its own <see cref="BoundEcChecked"/> — or carries none, because nothing is enabled
    /// at ITS line. Leaving the enclosing statement's region ambient made "none" read as the enclosing region.
    /// <para>⛔ kb/Work PB441 measured what that cost: §14.9.28.4 GR14 puts imperative-statement-5 inside the
    /// implicit <c>PUSH ALL</c> + <c>TURN OFF ALL</c> window, the binder's GR14 floor duly bound the FINALLY
    /// body with EC-ALL off — and the ADD inside it still emitted the EC-SIZE guard, because that guard is
    /// decided at EMIT time from this ambient region and imp-5 is emitted INLINE inside the PERFORM's own. The
    /// window was realized by two mechanisms (the bind-time floor and the run-time
    /// <c>ExceptionState.PushAllCheckingOff</c>) and a COMPILED-IN gate was inside neither. The boundary is
    /// here, at the ONE funnel every nested source statement list is emitted through, so every emit-time-gated
    /// family — EC-SIZE today, the next one automatically — sees the region of the statement it guards.</para>
    /// <para>A desugar's <c>BoundSequence</c> / <c>BoundImplicitSeries</c> steps are NOT such a list: they are
    /// parts of ONE source statement, emitted through <c>EmitStatement</c> directly, and they must keep the
    /// region their statement's wrapper opened.</para></summary>
    /// <para>⛔ The same boundary holds for the RUN-TIME flags (kb/Work PB891): the list opens a
    /// <see cref="EnterCheckingBaseline"/> scope, so a nested statement that enables nothing does not inherit the
    /// enclosing statement's standing flags (§7.3.25.4 GR5 — a TURN inside a statement "applies to any succeeding
    /// statement … whether or not that succeeding statement is within the scope of the statement in which the
    /// TURN directive is specified").</para></summary>
    public EcRegionScope EnterNestedStatements() => new(ecState, EnterCheckingBaseline());

    /// <summary>The save/clear/restore of <see cref="EcState.Info"/> that <see cref="EnterNestedStatements"/>
    /// hands out, plus the run-time checking baseline it opened — a struct so the boundary costs no allocation on
    /// the statement path.</summary>
    internal readonly struct EcRegionScope : IDisposable
    {
        private readonly EcState _state;
        private readonly EcStatementInfo? _saved;
        private readonly CheckingScope _checking;
        internal EcRegionScope(EcState state, CheckingScope checking)
        {
            _checking = checking;
            _state = state; _saved = state.Info; state.Info = null;
        }
        public void Dispose() { _state.Info = _saved; _checking.Dispose(); }
    }

    // ── The ambient checking flags: ONE save/restore discipline (kb/Work PB891 / PB841) ─────────────────────
    //
    // Enablement belongs to the SOURCE TEXT of the executing statement (§7.3.25.4 GR6), and the run-time
    // `ExceptionState.<Flag>` bits are how a raise site deep in the runtime learns it. So every change to them is a
    // SCOPE with a saved value, never a set/reset pair: a statement guard SAVES, sets its own flags, and RESTORES
    // (OpenGateFlags); and wherever control reaches OTHER source statements while a guard's flags stand, those
    // statements start from ALL-OFF (EnterCheckingBaseline) — a nested statement list, a procedure range run by a
    // PERFORM / SORT / MERGE (StatementEmitter.EmitProcedureRange), a USE procedure or F3 handler (__RunUse), a
    // method body (OoEmitter), and a CALL / function activation (the runtime's ProgramTable.CallProgram). The
    // emitter tracks statically whether any guard's flags are standing (EcState.FlagsStanding), so a statement list
    // that no flag guard encloses — every paragraph, every EC-free program — emits nothing at all.

    /// <summary>Open the flag scope of ONE statement guard: save the ambient checking state, set the flags this
    /// statement's own line enables, and (on dispose) emit the <c>finally</c> that restores the saved state. The
    /// caller emits the <c>try</c> block and any <c>catch</c> clauses between the two. An empty flag list is a no-op
    /// scope (the null-flag gates — EC-OO-NULL, the EC-SIZE family — set nothing). kb/Work PB891: the former
    /// <c>finally { &lt;Flag&gt; = false; }</c> assumed the flag had been off; a guarded statement executed while an
    /// ENCLOSING statement's guard stood cleared that statement's enable, and a later raise site of the enclosing
    /// statement read "not enabled" (§14.6.13.1.1: "if checking for an exception condition is not enabled, the
    /// exception condition will not be raised" — the converse holds for an enabled one).</summary>
    internal CheckingScope OpenGateFlags(IReadOnlyList<string> flags)
    {
        if (flags.Count == 0) return default;
        var w = ctx.Writer;
        string local = $"__ck{ctx.Names.NextEc()}";
        w.Line($"var {local} = ExceptionState.SaveChecking();   // this statement's checking scope (§7.3.25.4 GR6)");
        foreach (var f in flags) w.Line($"ExceptionState.{f} = true;");
        bool saved = ecState.FlagsStanding;
        ecState.FlagsStanding = true;
        return new CheckingScope(w, ecState, local, saved, block: false);
    }

    /// <summary>Open a checking scope at the BASELINE (every flag off) around code that runs OTHER source
    /// statements — but only when a statement guard's flags are statically standing here; otherwise the run-time
    /// state already IS the baseline and nothing is emitted. The scope emits its own <c>try { … } finally</c>.
    /// §7.3.25.4 GR5/GR6 make each statement's enablement its own; §14.9.28.4 GR14's implicit PUSH ALL + TURN OFF
    /// ALL around imp-2..imp-5 is one instance of this scope, not a separate mechanism.</summary>
    internal CheckingScope EnterCheckingBaseline()
    {
        if (!ecState.FlagsStanding) return default;
        var w = ctx.Writer;
        string local = $"__ck{ctx.Names.NextEc()}";
        w.Line($"var {local} = ExceptionState.PushAllCheckingOff();   // other source statements: checking baseline (§7.3.25.4 GR5/GR6)");
        w.Line("try");
        w.Line("{");
        w.Indent();
        ecState.FlagsStanding = false;
        return new CheckingScope(w, ecState, local, saved: true, block: true);
    }

    /// <summary>The emitted-text scope <see cref="OpenGateFlags"/> / <see cref="EnterCheckingBaseline"/> hand out;
    /// <c>default</c> is the no-op scope. Disposing it closes the block (baseline form) and emits the ONE restore.</summary>
    internal readonly struct CheckingScope : IDisposable
    {
        private readonly CodeWriter? _w;
        private readonly EcState? _state;
        private readonly string? _local;
        private readonly bool _savedStanding, _block;
        internal CheckingScope(CodeWriter w, EcState state, string local, bool saved, bool block)
        { _w = w; _state = state; _local = local; _savedStanding = saved; _block = block; }
        public void Dispose()
        {
            if (_w is null) return;
            if (_block) _w.CloseBrace();
            _w.Line($"finally {{ ExceptionState.RestoreChecking({_local}); }}");
            _state!.FlagsStanding = _savedStanding;
        }
    }

    public bool EmitChecked(BoundEcChecked ec)
    {
        var prev = ecState.Info;
        ecState.Info = ec.Info;
        bool terminated;
        // The AMBIENT statement context (kb/Work R14): when any condition enabled at this statement carries
        // WITH LOCATION, the (Table-12 statement name, §15.30.3 r2 location) pair enters the runtime's ambient
        // slot together with the names it covers — §15.32.3 r1 is PER-CONDITION (R06), so a raise of an
        // uncovered name still answers spaces. Every raise site then reaches the pair through the 2-argument
        // ExceptionState.Set — SEARCH's range Sets, CONTINUE AFTER, the nonfatal gates, the runtime string /
        // storage sites — with no per-site threading. Save/restore (not set/clear), so a CALL inside the
        // statement restores this statement's context when the callee returns.
        // ⛔ FILE-SCOPED entries are EXCLUDED: WITH LOCATION on `EC-I-O-… FILE F1` is per-(name, FILE), which a
        // name set cannot express — a same-name raise on another file would wrongly stamp the pair. The I-O
        // path keeps its per-file __locMask channel through __IoCheckEc (R06), and its SetIo passes the pair
        // POSITIONALLY, which always wins over the ambient fallback.
        var locNames = ec.Info.Enabled.Where(e => e.WithLocation && e.File is null)
            .Select(e => e.Ec).Distinct().ToList();
        if (locNames.Count > 0)
        {
            var w = ctx.Writer;
            int id = ctx.Names.NextEc();
            string arr = string.Join(", ", locNames.Select(CsLiteral));
            w.Line($"var __ecs{id} = ExceptionState.EnterStatement({CsLiteral(ec.Info.StatementName)}, "
                + $"{CsLiteral(ec.Info.Location)}, new[] {{ {arr} }});");
            using (w.Block("try"))
                terminated = EmitGatesOrInner(ec);
            w.Line($"finally {{ ExceptionState.ExitStatement(__ecs{id}); }}");
        }
        else
            terminated = EmitGatesOrInner(ec);
        ecState.Info = prev;
        return terminated;
    }

    /// <summary>The nonfatal ambient gates enabled at this statement ride a save/set/restore scope around whichever
    /// inner dispatch (the fatal-gated or the plain) the statement needs — plus the RESUME landing for the
    /// selection those gates' raise sites now run.
    /// <para>A nonfatal condition raised INSIDE the runtime selects its declarative there (§14.6.13.1.4 #3,
    /// through <c>INonfatalSelector.NonfatalDispatch</c>), because the raise has no statement-level node an emitter
    /// could hang a dispatch on. A declarative that completes normally returns to the raise point and the
    /// statement finishes; a RESUME does not — §14.9.33.4 GR2/GR3 transfer control OUT of the interrupted
    /// statement, and the runtime expresses that as a <c>ResumeSignal</c> it cannot itself land. This catch is
    /// that landing, and it is emitted with the gates because those gates are exactly the statements whose
    /// runtime raise sites are armed (kb/Work PB367b).</para></summary>
    private bool EmitGatesOrInner(BoundEcChecked ec)
    {
        var gates = NonfatalAmbientGates.Where(g => ec.Info.Enabled.Any(p => p.Ec == g.Ec)).ToList();
        if (gates.Count > 0)
        {
            var w = ctx.Writer;
            int id = ctx.Names.NextEc();
            using (OpenGateFlags([.. gates.Select(g => g.Flag)]))   // save / set / RESTORE — never reset (PB891)
            {
                using (w.Block("try"))
                    EmitArgOrPlain(ec);
                // Only a unit with F3 selection machinery can produce a RESUME here at all — with none,
                // NonfatalDispatch answers "no qualifying declarative" and nothing is thrown, so the catch would be
                // dead text in every such program (the zero-scaffolding invariant applies to what CAN happen).
                if (UnitHasDispatchFunnel)
                    // A `goto` out of a catch CLAUSE is legal C# (only a finally BLOCK may not be left that way), so
                    // the resume landing uses the same dispatcher-transfer idiom as every other raise site (PB405).
                    w.Line($"catch (RaiseResumeSignal __nr{id}) {{ {dispatch.ResumeTransfer($"__nr{id}.TargetPc", "")} }}"
                        + "   // RESUME out of a runtime-site nonfatal raise: AT procedure-name transfers (§14.9.33.4 GR3), "
                        + "AT NEXT STATEMENT abandons the interrupted statement (GR2)");
            }
            return false;   // conservative: the inner dispatch may itself resume past a transfer
        }
        return EmitArgOrPlain(ec);
    }

    /// <summary>The FATAL ambient per-statement EC gates — each rides an <c>ExceptionState.XxxChecking</c> flag its
    /// runtime raise site consults; a raise throws <see cref="Runtime.Exceptions.CobolFatalException"/> which the
    /// statement guard catches for USE F3 dispatch (RESUME) else re-throws to terminate. Fixed order for
    /// byte-stability. (Nonfatal twins live in <see cref="EmitGatesOrInner"/>'s save/set/restore scope — they need no
    /// catch.) <c>ExceptionRaiseHelperDriftTests</c> reads BOTH tables: the flag named here for an exception-name
    /// is asserted to be the flag the runtime helper that raises that name actually reads (kb/Work PB676).</summary>
    internal static readonly (string Ec, string? Flag)[] FatalAmbientGates =
    [
        ("EC-ARGUMENT-FUNCTION", "ArgumentFunctionChecking"),   // §15.3 — intrinsic argument/domain error
        ("EC-BOUND-REF-MOD", "BoundRefModChecking"),            // §8.4.3.3.4 — ref-mod out of range / zero-length
        ("EC-DATA-NOT-FINITE", "FloatNotFiniteChecking"),       // §14.6.13.2 item 3 — NaN/±Inf float sending operand
        ("EC-DATA-OVERFLOW", "FloatOverflowChecking"),          // §14.9.25.4 GR6 d)4.a — MOVE overflows a float receiver / a floating-point edited one (D21/PB66)
        ("EC-DATA-INCOMPATIBLE", "DataIncompatibleChecking"),   // §14.6.13.2 rule 2 — a fixed-point numeric sending item whose content fails its numeric class condition (PB230); rule 4 — a de-editing MOVE from impossible edited content (D21/PB66)
        ("EC-RANGE-PERFORM-VARYING", "PerformVaryingChecking"), // §14.9.28.4 GR3 — index-name varied from a non-positive item
        ("EC-DATA-PTR-NULL", "DataPtrNullChecking"),            // §13.18.5.4 GR3 / §14.9.39 F10 GR18 — NULL data-address
        ("EC-BOUND-PTR", "BoundPtrChecking"),                   // §13.18.5.4 GR4 — address neither NULL nor valid
        ("EC-SIZE-ADDRESS", "SizeAddressChecking"),             // §14.9.39.4 GR19 — a SET UP/DOWN BY amount that does not evaluate to an integer
        ("EC-RANGE-PTR", "RangePtrChecking"),                   // §14.9.39.4 GR20 — the NEW ADDRESS outside the implementor data-pointer range (DOC-A.1-216; kb/Work PB465)
        ("EC-BOUND-SUBSCRIPT", "BoundSubscriptChecking"),       // §8.4.2.3.4 GR2 — subscript outside 1..highest
        ("EC-BOUND-ODO", "BoundOdoChecking"),                   // §13.18.38.4 GR7 — DEPENDING value outside int-1..int-2
        ("EC-RANGE-INSPECT-SIZE", "RangeInspectSizeChecking"),  // §14.9.22.4 GR14/GR15/GR22 — an INSPECT operand pair of unequal size (kb/Work PB1126)
        ("EC-RANGE-INDEX", "RangeIndexChecking"),               // §13.18.38.4 GR2 / §14.9.39.4 GR2 a) 1. b + GR4 a) — an index driven outside the implementor range (kb/Work PB459)
        // The *-ARG-OMITTED trio — one rule per activated-element kind, raised at the formal's guarded reference
        // by OmittedFormal (kb/Work PB971); the binder queries exactly the current element's name.
        ("EC-PROGRAM-ARG-OMITTED", "ProgramArgOmittedChecking"),// §14.9.4.4 GR12 — reference in a called program to an omitted formal (kb/Work PB133 wave C)
        ("EC-FUNCTION-ARG-OMITTED", "FunctionArgOmittedChecking"),// §8.4.3.2.4 GR8 — the same reference in an activated function
        ("EC-OO-ARG-OMITTED", "OoArgOmittedChecking"),          // §14.9.23.4 GR10 — the same reference in an invoked method
        ("EC-OO-RESOURCE", "OoResourceChecking"),               // §16.2.1.2 GR2 — BASE's New could not create the object (kb/Work PB1524)
        // ⛔ FLAG = null: these two raise sites are UNCONDITIONAL, so there is no checking flag to set. §14.9.23.4
        // GR5 ("If identifier-1 is null, the EC-OO-NULL exception condition is set to exist and execution of the
        // INVOKE statement is terminated") and GR7b (the method could not be located) describe crossings a
        // typed-native model can never proceed through — there is no lenient value to return, exactly as with a
        // null dereference. The entry exists so the statement still gets its try/catch and the condition can
        // reach a USE declarative; a flag nothing reads would be state a future maintainer has to disprove.
        // ⛔ EC-PROGRAM-NOT-FOUND OF A program-address-identifier OPERAND (kb/Work PB1453): the raise site is
        // ProgramRegistry.EntryOfOperand, an unconditional fatal throw whose only switch is the carrying statement's
        // compile-time TURN state (PtrEmitter.ProgramAddressText renders `checkNotFound: true` exactly when this name
        // is enabled at the statement), so there is no ambient flag — the row gives the statement its try/catch and
        // the condition its way to a declarative. SET … TO ENTRY and a CALL argument raise it through their own
        // shapes (EmitSetEntry's in-statement selection; CobolCallException), which this catch never sees.
        ("EC-PROGRAM-NOT-FOUND", null),                         // §8.4.3.13.4 GR4 — a program-address-identifier operand's locate miss
        ("EC-OO-NULL", null),                                   // §14.9.23.4 GR5 — INVOKE on a null receiver
        ("EC-OO-METHOD", null),                                 // §14.9.23.4 GR7b — method could not be located
        // FLAGGED, unlike its two neighbours: §14.9.23.4 GR7c raises only when checking is enabled in BOTH
        // elements, and this flag is how the ACTIVATOR's half reaches the callee's __CobolInvoke, which runs
        // synchronously inside the guard. The method's half is a compile-time literal (OoEmitter.OoUnivStop).
        ("EC-OO-UNIVERSAL", "OoUniversalChecking"),             // §14.9.23.4 GR7c — universal-INVOKE conformance
        // The EC-EXTERNAL trio (kb/Work PB1138): FLAGGED for the same reason as EC-OO-UNIVERSAL — the flag IS
        // §14.8.4.1's ACTIVATING-element half, read by every activation boundary before the activated element's
        // checking scope opens (ProgramTable.CallProgram for a CALL or function activation; the method prologue's
        // ExternalStore.DescribeAtMethodActivation for an INVOKE, §14.9.23.4 GR7 d)). The catch arm takes the
        // method arm's CobolFatalException; a CALL's failure is a CobolCallException its own partition handles.
        ("EC-EXTERNAL-FORMAT-CONFLICT", "ExternalFormatConflictChecking"),   // §14.8.4.3
        ("EC-EXTERNAL-DATA-MISMATCH", "ExternalDataMismatchChecking"),       // §14.8.4.2
        ("EC-EXTERNAL-FILE-MISMATCH", "ExternalFileMismatchChecking"),       // §14.8.4.4
        ("EC-FLOW-SEARCH", "FlowSearchChecking"),               // §14.9.39.4 GR31 — capacity SET during a SEARCH
        ("EC-FLOW-USE", "FlowUseChecking"),                     // §14.9.49.4 GR2 — a USE procedure re-entered while active (kb/Work PB368)
        // §14.9.18.4 GR6 — a GOBACK executed within the RANGE of one of THIS program's GLOBAL declaratives
        // (kb/Work PB409). Before this row the name was a catalog entry with no raise site and no gate, so
        // `>>TURN EC-FLOW-GLOBAL-GOBACK CHECKING ON` was accepted and wired nothing — a green compile that read
        // as support. Its Table 13 neighbour EC-FLOW-GLOBAL-EXIT has no row because the standard states no
        // general rule that SETS it (see ExceptionEngine.FlowGlobalGobackError).
        ("EC-FLOW-GLOBAL-GOBACK", "FlowGlobalGobackChecking"),  // §14.9.18.4 GR6 — GOBACK in a global declarative's range
        // The sort-merge flow conditions (kb/Work PB349). Before these rows the three names were catalog entries
        // with no gate, no flag and no raise site, so `>>TURN EC-FLOW-RELEASE CHECKING ON` compiled clean and
        // wired nothing. Each rides a flag CobolSort's statement entry consults against the store's phase.
        ("EC-FLOW-RELEASE", "FlowReleaseChecking"),             // §14.9.32.4 GR1 — RELEASE outside its SORT's input procedure
        ("EC-FLOW-RETURN", "FlowReturnChecking"),               // §14.9.34.4 GR1 — RETURN outside its SORT/MERGE's output procedure
        ("EC-SORT-MERGE-RETURN", "SortMergeReturnChecking"),    // §14.9.34.4 GR3 — RETURN after the at end condition in the same output procedure
        // The sort-merge STATEMENT conditions (kb/Work PB1036). Before these rows the four names were catalog
        // entries with no flag and no raise site, so `>>TURN EC-SORT-MERGE CHECKING ON` wired nothing and a SORT
        // … GIVING an open file ran on with its declarative silent. Each rides a flag CobolSort consults at the
        // point its rule names; when the SORT/MERGE itself raised it, VerbDisposes hands the disposition to the verb.
        ("EC-SORT-MERGE-ACTIVE", "SortMergeActiveChecking"),    // §14.9.40.4 GR10/GR13, §14.9.24.4 GR8 — a SORT/MERGE/RELEASE/RETURN a running procedure may not execute
        ("EC-SORT-MERGE-FILE-OPEN", "SortMergeFileOpenChecking"),// §14.9.40.4 GR9, §14.9.24.4 GR7/GR12 — a USING/GIVING file already open
        ("EC-SORT-MERGE-RELEASE", "SortMergeReleaseChecking"),  // §13.18.43.4 GR14/GR19, §14.9.40.4 GR12 b), §14.9.24.4 GR7 b) — record size outside the SD's range
        ("EC-SORT-MERGE-SEQUENCE", "SortMergeSequenceChecking"),// §14.9.24.4 GR6 — a MERGE USING file out of KEY order
        // The Report Writer's four statement-precondition conditions (kb/Work PB326). Each rides a flag its
        // runtime raise site in CobolReport consults; each is Table 13 Fatal, and each leaves the verb
        // unexecuted whether or not the raise happens (the standard states every lenient outcome outright).
        ("EC-FLOW-REPORT", "FlowReportChecking"),               // §14.9.49.4 GR10 — RWCS verb inside a BEFORE REPORTING range
        ("EC-REPORT-ACTIVE", "ReportActiveChecking"),           // §14.9.21.4 GR2  — INITIATE of an active report
        ("EC-REPORT-INACTIVE", "ReportInactiveChecking"),       // §14.9.16.4 GR7 / §14.9.46.4 GR1 — GENERATE/TERMINATE of an inactive report
        ("EC-REPORT-FILE-MODE", "ReportFileModeChecking"),      // §14.9.21.4 GR3  — INITIATE with the connector not open OUTPUT/EXTEND
        // kb/Work PB1130 — a sum counter's size error (§13.18.54.4 GR3, at the GENERATE that adds) and the presentation
        // of a counter whose size error indicator is set (GR4, at the GENERATE / TERMINATE that prints it). Unlike its
        // four neighbours the lenient outcome is not "the verb does nothing": the counter keeps the ADD-with-SIZE-ERROR
        // result and the printable item is spaces, whether or not the raise happens.
        ("EC-REPORT-SUM-SIZE", "ReportSumSizeChecking"),        // §13.18.54.4 GR3 / GR4 — overflow of a sum counter
        // kb/Work PB1305 — a VARYING FROM/BY expression that evaluates to a noninteger value, raised by the compose of
        // the line that carries the counter (the GENERATE / TERMINATE that prints it). The lenient outcome is GR5's
        // undefined print-line content: the counter takes the value's integer part.
        ("EC-REPORT-VARYING", "ReportVaryingChecking"),         // §13.18.64.4 GR5 — VARYING clause expression noninteger
        ("EC-BOUND-TABLE-LIMIT", "BoundTableLimitChecking"),    // §14.9.39.4 GR30 — growth past the implementor max
        ("EC-ORDER-NOT-SUPPORTED", "OrderNotSupportedChecking"),// §15.85.4 r2 — STANDARD-COMPARE's ordering table / level unavailable
        ("EC-LOCALE-MISSING", "LocaleMissingChecking"),        // §14.9.39.4 GR24 / §8.2.1 — a locale not available (SET LOCALE; a named IS LOCALE sequence at use)
        ("EC-LOCALE-INVALID-PTR", "LocaleInvalidPtrChecking"), // §14.9.39.4 GR21 — SET LOCALE through a pointer that holds no saved locale
        ("EC-LOCALE-INCOMPATIBLE", "LocaleIncompatibleChecking"),// §8.8.4.2.11 / L6 — a locale comparison over an ill-formed operand
        ("EC-LOCALE-INVALID", "LocaleInvalidChecking"),        // §8.2.1 — a locale operation over incomplete locale content (no culture data)
        ("EC-LOCALE-SIZE", "LocaleSizeChecking"),              // §13.18.40.5 r14 b — locale editing truncated a non-zero, non-suppressed character (PB64 T6)
        // ⛔ THE EC-SIZE FAMILY FOR NON-ARITHMETIC STATEMENTS (kb/Work PB75). §14.7.5: the size error condition "may
        // occur as a result of … the evaluation of an arithmetic expression" — a condition, a function argument, a
        // subscript, an INVOKE argument — and without a SIZE ERROR phrase the level-3 EC-SIZE-* "is set to exist,
        // and processing proceeds as specified in 14.6.13.1.3". The raise sites are unconditional throws of
        // CobolSizeError (a CobolFatalException), so FLAG = null exactly as for EC-OO-NULL; the entry gives such a
        // statement its try/catch so the condition reaches a USE declarative / PERFORM WHEN (#4/#5) or terminates
        // (#7). ARITHMETIC statements are EXCLUDED below (IArithmeticStatement): EmitArith owns their §14.7.5 shape
        // (phrase, EC-SIZE handling, fatal default) and a second guard would dispatch the same condition twice.
        ("EC-SIZE-OVERFLOW", null),                             // §14.7.5 cases 5/7 — an intermediate past its range
        ("EC-SIZE-ZERO-DIVIDE", null),                          // §14.7.5 case 2 — a zero divisor
        ("EC-SIZE-EXPONENTIATION", null),                       // §14.7.5 case 1 — the exponentiation rules violated
        ("EC-SIZE-TRUNCATION", null),                           // §14.7.4.3 r7 / §11.9.11.2 r3d — a PROHIBITED-inexact intermediate
    ];

    /// <summary>The FLAGGED gates whose flag is an ACTIVATING-ELEMENT HALF, not a raise-site switch: "enabled in both the
    /// activating and activated" (§14.8.4.1 for the EC-EXTERNAL trio, §14.9.23.4 GR7 c) for EC-OO-UNIVERSAL), so the
    /// flag is read by an ACTIVATION BOUNDARY — <c>ExceptionEngine.ExternalActivatingMask</c> at the CALL boundary and
    /// a method prologue, a generated <c>__CobolInvoke</c> — and never by an <c>ExceptionEngine …Error</c> helper.
    /// Derived from <see cref="Binding.Procedure.EcBinder.ExternalNames"/> so a fourth checkable EC-EXTERNAL name
    /// joins by construction; <c>ExceptionRaiseHelperDriftTests</c> exempts exactly these from the helper census and
    /// asserts the EC-EXTERNAL flags' boundary reader instead (kb/Work PB1138).</summary>
    internal static readonly IReadOnlySet<string> ActivatingHalfGates =
        new HashSet<string>([.. Binding.Procedure.EcBinder.ExternalNames, "EC-OO-UNIVERSAL"], StringComparer.Ordinal);

    /// <summary>The inner EC dispatch of a checked statement: the fatal ambient gates enabled at it (with USE F3
    /// dispatch on the raise) or, when none is enabled, a plain statement emission. Wrapped by
    /// <see cref="EmitGatesOrInner"/> with the nonfatal gates when needed.</summary>
    private bool EmitArgOrPlain(BoundEcChecked ec)
    {
        // The fatal ambient gates enabled at this statement: intrinsic calls / ref-mod render inline inside
        // arbitrary expressions, so the guard wraps the STATEMENT and the runtime error sites consult the flag(s).
        // An ARITHMETIC statement owns its EC-SIZE family (EmitArith — kb/Work PB75), so those gates skip it.
        bool arithmetic = ec.Inner is IArithmeticStatement;
        var gates = FatalAmbientGates
            .Where(g => ec.Info.Enabled.Any(p => p.Ec == g.Ec)
                        && !(arithmetic && g.Ec.StartsWith("EC-SIZE-", StringComparison.Ordinal)))
            .ToList();
        if (gates.Count == 0)
            return Statements.EmitStatement(ec.Inner);

        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        // One gate ⇒ the raised name is the literal (byte-identical to the pre-generalization output); two or more
        // ⇒ the actual __af.EcName drives the status/dispatch.
        string ecExpr = gates.Count == 1 ? CsLiteral(gates[0].Ec) : $"__af{id}.EcName";
        string nameTest = string.Join(" || ", gates.Select(g => $"__af{id}.EcName == {CsLiteral(g.Ec)}"));
        // save / set / RESTORE — never reset (kb/Work PB891); the null-flag gates set nothing, so they open no scope.
        using (OpenGateFlags([.. gates.Where(g => g.Flag is not null).Select(g => g.Flag!)]))
        {
            using (w.Block("try"))
                Statements.EmitStatement(ec.Inner);
            // `!Dispatched`: a condition an INNER statement's guard already processed passes through to the boundary
            // (§14.6.13.1.3 #7) — one dispatch per raise, not one per nesting level (kb/Work PB75).
            using (w.Block($"catch (CobolFatalException __af{id}) when (!__af{id}.Dispatched && ({nameTest}))"))
            {
                // §14.6.13.1.1: "If checking for an exception condition is enabled and an exception status indicator
                // is set … the last exception status is set to indicate that exception condition." The guard only
                // exists where checking IS enabled, so the status is set here unconditionally. The §15.32.3 r2 /
                // §15.30.3 r2 operands come from the AMBIENT statement context (kb/Work R14 — EmitChecked entered
                // it with exactly the WITH-LOCATION names, so an uncovered name answers r1's spaces): one channel
                // for every raise site, in place of the per-site (stmt, loc) literals this call used to bake.
                w.Line($"ExceptionState.Set({ecExpr}, true);");
                // Terminate by RETHROWING the caught condition (its runtime subtype and message intact), marked
                // dispatched so the enclosing guards let it pass. §14.6.13.1.3 2): a condition the SORT/MERGE
                // statement raised itself is the verb's to dispose of — the statement is terminated and execution
                // continues after it (VerbDisposes; kb/Work PB1036), so on that guard the fatal default applies only
                // to an unmarked condition.
                EmitSelection(ecExpr, $"{{ __af{id}.Dispatched = true; throw; }}",
                    fatalWhen: VerbDisposes(ec.Inner) ? $"!__af{id}.RaisedBySortMerge" : null);
            }
        }
        return false;   // conservative: the catch can resume past an inner transfer
    }

    /// <summary>⛔ §14.6.13.1.3 2) — "If the executed statement is a MERGE or SORT statement, then the rules for
    /// those statements apply" — precedes the declarative-then-terminate of 5) and the run-unit termination of 7).
    /// True for a SORT or MERGE statement's guard, whose catch then disposes of every condition the runtime marked
    /// <c>RaisedBySortMerge</c> by the verb: the applicable declarative runs, the statement is terminated and
    /// execution continues after it — the reading <c>__IoCheckEc(…, __verbRule: true)</c> applies to the implicit
    /// transfers' I-O statuses (docs/CONFORMANCE.md §3 D-SMA; kb/Work PB993). The mark, not the exception-name,
    /// decides, because the rule keys on the statement that EXECUTED: <c>CobolSort</c> sets it on whatever its
    /// statement entries raise (an EC-SORT-MERGE-* condition, an EC-LOCALE-* one from a locale-collated key), while a
    /// condition from a statement of an input or output procedure — or the same EC-SORT-MERGE names raised by a
    /// RELEASE or RETURN — arrives unmarked and keeps 5)/7).</summary>
    internal static bool VerbDisposes(BoundStatement inner) => inner is BoundSort or BoundMerge;

    // ── RAISE (§14.9.29) ─────────────────────────────────────────────────────────────────────────────────────

    public bool EmitRaise(BoundRaise r)
    {
        var w = ctx.Writer;
        if (!r.Enabled)
        {
            if (!r.Fatal)
            {
                // §14.6.13.1.4 first sentence + §14.6.13.1.1 (24485): checking off ⇒ the condition is not raised
                // and the last exception status is NOT set — the RAISE acts as CONTINUE (§14.9.29.4 GR1 NOTE).
                w.Line($"// RAISE {r.EcName}: checking not enabled — nonfatal, continues as if not raised (ISO §14.6.13.1.4)");
                return false;
            }
            // A FATAL exception-name raised with checking off is the §14.6.13.1.3 #8 implementor choice —
            // this implementation terminates loudly (the §1.4 doctrine; recorded in the deep-dive).
            w.Line($"throw new CobolFatalException({CsLiteral(r.EcName)}, \"raised by RAISE with checking not enabled "
                + "(ISO 14.6.13.1.3 #8 - implementor-defined; this implementation terminates)\");");
            return true;
        }
        string stmt = r.WithLocation ? "\"RAISE\"" : "null";
        string loc = r.WithLocation ? CsLiteral(r.Location) : "null";
        string name = CsLiteral(r.EcName);
        w.Line($"ExceptionState.Set({name}, {(r.Fatal ? "true" : "false")}, {stmt}, {loc});   // §14.9.29.4 GR1 — raise + EXCEPTION-OBJECT null");
        // Nonfatal: handled-or-not, execution continues after the RAISE (§14.6.13.1.4 #3/#4).
        EmitSelection(name, r.Fatal
            ? FatalTermination(name, "\"raised by RAISE and not resumed (ISO 14.6.13.1.3 #5/#7)\"")
            : null);
        return false;
    }

    /// <summary>Emit RESUME (ISO §14.9.33.4). Returns true when the statement UNCONDITIONALLY unwinds (the
    /// caller then treats it as a transfer out of the paragraph case), false when GR1 can make it a CONTINUE.
    /// <para>⛔ GR1 IS A RUN-TIME QUESTION, ASKED HERE AND ANSWERED BY THE SAME ARRAY GOBACK USES (kb/Work PB1160):
    /// "If the RESUME statement is executed within the scope of execution of a global declarative, it is the
    /// equivalent of the execution of a CONTINUE statement." A declarative may PERFORM a LOCAL declarative or any
    /// other procedure (§14.9.49.3 SR4), so a RESUME that SR2 let through statically can still be executed while a
    /// GLOBAL declarative is active — and it must then fall through. The "scope of execution of a global
    /// declarative" is exactly §14.9.18.4 GR6's "within the range of a declarative procedure whose USE statement
    /// contains the GLOBAL phrase", so the ONE test is <see cref="DispatchState.InGlobalDeclarativeRangeTest"/> (the
    /// per-program-instance <c>__useActive[global ids]</c>); a unit declaring no GLOBAL declarative emits the
    /// unconditional unwind, byte-identical to before. The RESUME was EXECUTED even when it continues, so the
    /// declaratives of THIS program that are open have not completed normally (§14.6.13.1.2 1: "a RESUME … that is
    /// specified in this … program is executed") — the no-op arm marks their activation records, and the declarative
    /// falls off its end as <see cref="DispatchResult.NotNormal"/> (kb/Work PB1122 Task B).</para></summary>
    public bool EmitResume(BoundResume r)
    {
        string signal = r.TargetPc == DispatchResult.ResumeNext
            ? "throw new ResumeSignal(DispatchResult.ResumeNext);"
            : $"throw new ResumeSignal({r.TargetPc});";
        string why = r.TargetPc == DispatchResult.ResumeNext
            ? "RESUME AT NEXT STATEMENT (§14.9.33.4 GR2)"
            : "RESUME AT procedure-name ≡ GO TO (§14.9.33.4 GR3)";
        if (dispatch.InGlobalDeclarativeRangeTest is not { } inGlobalRange)
        {
            ctx.Writer.Line($"{signal}   // {why}");
            return true;
        }
        ctx.Writer.Line($"if (!({inGlobalRange})) {signal}   // {why}");
        ctx.Writer.Line("else ExceptionState.MarkDeclarativesNotNormal(__useActive);   "
            + "// GR1: within a global declarative's scope of execution RESUME ≡ CONTINUE — executed, so not normal completion (§14.6.13.1.2 1))");
        return false;
    }

    // ── The EC-SIZE family over the checked-arithmetic shape (§14.7.5 ↔ Table 13) ───────────────────────────

    /// <summary>Is <paramref name="ec"/> enabled for the statement being emitted? The general form of
    /// <see cref="EnabledSizeNames"/> — an emitter needs it whenever a rule's SHAPE (not merely a raise) depends
    /// on checking being on, so that the checking-off output stays byte-identical. §14.7.6's CORRESPONDING
    /// deferral region is the first such caller (kb/Work PB230).</summary>
    public bool Enabled(string ec) =>
        ecState.Info?.Enabled.Any(p => string.Equals(p.Ec, ec, StringComparison.Ordinal)) ?? false;

    /// <summary>The EC-SIZE-* names the current statement has enabled (empty list when none / no wrapper).</summary>
    public List<string> EnabledSizeNames() =>
        ecState.Info?.Enabled.Where(p => p.Ec.StartsWith("EC-SIZE-", StringComparison.Ordinal)).Select(p => p.Ec).ToList()
        ?? [];

    /// <summary>Emit the post-store EC-SIZE handling: when the latched size-error name is one of the ENABLED
    /// names, set the last exception status and — unless the statement's own ON SIZE ERROR phrase takes
    /// precedence (§14.6.13.1.3 #1 / §14.6.13.1.4 #1) — run the §14.9.49 F3 selection and the fatal default
    /// (every EC-SIZE-* is fatal, Table 13).</summary>
    public void EmitSizeHandling(string flag, string ecnVar, List<string> enabled, bool hasPhrase)
    {
        var w = ctx.Writer;
        string nameTest = string.Join(" || ", enabled.Select(n => $"{ecnVar} == {CsLiteral(n)}"));
        using (w.Block($"if ({flag} && ({nameTest}))"))
        {
            // The §15.32.3 r2 pair rides the ambient statement context (kb/Work R14; EmitChecked entered it).
            w.Line($"ExceptionState.Set({ecnVar}, true);   // §14.6.13.1.1 — the last exception status");
            if (!hasPhrase)
                // The message decoration reads the statement name back from the status Set just recorded —
                // the ONE channel — rather than a second baked literal.
                EmitSelection(ecnVar, FatalTermination(ecnVar,
                    "\"size error and not resumed (ISO 14.7.5; 14.6.13.1.3 #5/#7)\" "
                    + "+ (ExceptionState.LastStatement is { } __szs ? \" in \" + __szs.TrimEnd() : \"\")"));
            // With an ON SIZE ERROR phrase the phrase handles it (§14.6.13.1.3 #1) — state is set, phrase runs below.
        }
    }

    // ── The EC-OVERFLOW family (STRING/UNSTRING, §14.9.43 GR8b / §14.9.48 GR16b) ─────────────────────────────

    /// <summary>Emit the EC-OVERFLOW-STRING/-UNSTRING raise after the kernel latched <paramref name="ovfFlag"/>:
    /// set the last exception status; without an ON OVERFLOW phrase run the F3 selection (nonfatal — execution
    /// continues either way, §14.6.13.1.4 #3/#4).</summary>
    /// <summary>Is <paramref name="ecName"/> enabled at the statement currently being emitted (§7.3.25.4 GR6, as
    /// folded at bind time into the <see cref="BoundEcChecked"/> wrapper)? The ONE question every
    /// CHECKING-GATED emission asks before writing a raise site, so "emit nothing when checking is off" is one
    /// predicate rather than a repeated null-and-Any test.</summary>
    public bool EnabledHere(string ecName) =>
        ecState.Info is { } info && info.Enabled.Any(p => p.Ec == ecName);

    public void EmitOverflow(string ovfFlag, string ecName, bool hasPhrase)
    {
        if (!EnabledHere(ecName)) return;
        using (ctx.Writer.Block($"if ({ovfFlag})"))
        {
            // The §15.32.3 r2 pair rides the ambient statement context (kb/Work R14). With an ON OVERFLOW phrase
            // the phrase takes the condition (§14.6.13.1.4 1)), so only the status is set.
            const string why = "§14.9.43.4 GR8b / §14.9.48.4 GR16b — the overflow condition";
            if (hasPhrase) EmitConditionSet(ecName, why);
            else EmitConditionRaise(ecName, why);
        }
    }

    // ── The EC-I-O bridge (the per-statement hook variant; §9.1.13.1) ────────────────────────────────────────

    /// <summary>The enabled EC-I-O (name → mask bit) pairs of the current statement for <paramref name="file"/>,
    /// or 0 when none (the caller then emits the plain F1 hook).</summary>
    public int IoMaskFor(FileModel file)
    {
        if (ecState.Info is null) return 0;
        int mask = 0;
        foreach (var (ec, f, _) in ecState.Info.Enabled)
            if (ReferenceEquals(f, file))
                mask |= ExceptionCatalog.IoBit(ec);
        return mask;
    }

    /// <summary>The WITH-LOCATION subset of <see cref="IoMaskFor"/> — same bit positions, so the generated
    /// <c>__IoCheckEc</c> answers §15.32.3 r1 PER EC-I-O name: the status→EC mapping picks the raised name at
    /// runtime, and one file's <c>EC-I-O-AT-END … WITH LOCATION</c> must not make its <c>EC-I-O-INVALID-KEY</c>
    /// raise record location information (kb/Work R06).</summary>
    public int IoLocMaskFor(FileModel file)
    {
        if (ecState.Info is null) return 0;
        int mask = 0;
        foreach (var (ec, f, withLoc) in ecState.Info.Enabled)
            if (withLoc && ReferenceEquals(f, file))
                mask |= ExceptionCatalog.IoBit(ec);
        return mask;
    }

    // ── The generated machinery (__EcDispatch / __IoCheckEc) ─────────────────────────────────────────────────

    /// <summary>Generate <c>__EcDispatch</c> — the Format-3 declarative selector (ISO §14.9.49.4 GR3c–g): the
    /// USE statements are analyzed in SOURCE order within each tier — file+level-3, file+level-2, level-3,
    /// level-2, level-1 (EC-ALL) — and the FIRST match runs (GR3: "no other declaratives are executed"). Level-2
    /// matching uses the catalog's longest-family-prefix predicate (so the open EC-USER-* names select
    /// correctly). The GR3g outward-GLOBAL continuation is realized only on the I-O path (the existing F1
    /// <c>__RunGlobalUse</c> walk) — recorded in the deep-dive.</summary>
    public void EmitDispatchSelector(IReadOnlyList<BoundDeclarative> decls, CodeWriter w, bool asLocal)
    {
        using (w.Block($"{MemberMod(asLocal)}int __EcDispatch(string __ec, string __f)"))
        {
            void Tier(string comment, Func<string, Binding.Model.FileModel?, int, string?> condition)
            {
                bool any = false;
                for (int i = 0; i < decls.Count; i++)
                {
                    foreach (var (ec, file) in decls[i].EcEntries ?? [])
                        if (condition(ec, file, i) is { } cond)
                        {
                            if (!any) { w.Line(comment); any = true; }
                            w.Line($"if ({cond}) return {dispatch.RunUseCall(i, decls[i].Range)};");
                        }
                }
            }
            bool L3(string ec) => ExceptionCatalog.TryGet(ec, out var i) && i.Level == 3;
            bool L2(string ec) => ExceptionCatalog.TryGet(ec, out var i) && i.Level == 2;

            Tier("// GR3c — file-scoped level-3 entries", (ec, f, i) =>
                f is not null && L3(ec) ? $"__f == {FileKeyExpr(f)} && __ec == {CsLiteral(ec)}" : null);
            Tier("// GR3d — file-scoped level-2 entries", (ec, f, i) =>
                f is not null && L2(ec) ? $"__f == {FileKeyExpr(f)} && ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(ec)})" : null);
            Tier("// GR3e — level-3 entries", (ec, f, i) =>
                f is null && L3(ec) ? $"__ec == {CsLiteral(ec)}" : null);
            Tier("// GR3f — level-2 entries", (ec, f, i) =>
                f is null && L2(ec) ? $"ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(ec)})" : null);
            Tier("// GR3g — the level-1 EC-ALL entry", (ec, f, i) =>
                f is null && CobolNames.Same(ec, ExceptionCatalog.EcAll) ? "true" : null);
            w.Line("return DispatchResult.NoHandler;   // no qualifying declarative (GR3g tail)");
        }
        w.Line();
    }

    /// <summary>Generate <c>__EcObjDispatch</c> — the Format-4 exception-OBJECT selector (ISO §14.9.49.4
    /// GR14): a source-order scan ("A declarative is selected for execution by analyzing the USE statements in
    /// a source element in the order in which they are specified"), first match wins.
    /// <para>⛔ GR14 IS A TWO-PASS RULE, and the emitted code is two passes: a) scans the source element's USE
    /// statements in written order for an <i>object-class-name-1</i> entry matching the raised object's class
    /// or a subclass; only if none qualifies does "all of the USE statements in the source element are analyzed
    /// again" and b) scan for an <i>interface-name-1</i> entry the object's IMPLEMENTS clause references. A
    /// single interleaved pass would let an EARLIER interface entry beat a LATER class entry, which GR14 orders
    /// the other way round (kb/Work PB365 — before it, pass b) did not exist at all and the interface
    /// alternative was rejected at bind).</para>
    /// <para>Each pass renders its alternative's OWN CENSUS of emitted C# types as one <c>is</c> or-pattern:
    /// ONE COBOL name is not ONE C# type. GR14 a) selects when the exception object "is a factory object or
    /// instance object of object-class-name-1 or of a subclass of object-class-name-1" — ONE clause naming BOTH
    /// object kinds, and a COBOL class is emitted as TWO DISJOINT C# hierarchies (the instance class and its
    /// sibling <c>…__FACTORY</c> singleton, each rooted at its base's corresponding half) — so its census is
    /// <see cref="Oo.OoClassSymbol.FactoryOrInstanceCsTypes"/>; testing only the instance name selected NO
    /// declarative for any factory exception object, silently (kb/Work PB366). GR14 b)'s "described with an
    /// IMPLEMENTS clause that references interface-name-1" is §11.8.4 GR2 (instance objects) / §11.4.4 GR2
    /// (factory objects) as a CLOSURE — the direct IMPLEMENTS, plus anything an implemented interface inherits,
    /// plus anything an inherited class implements — and the emitter renders each emitted HALF of a class with
    /// exactly that closure as its C# interface list, so the one emitted C# interface
    /// (<see cref="Oo.OoInterfaceSymbol.ImplementedCsTypes"/>) realizes all three legs for both halves.</para>
    /// <para>"Or of a subclass" rides C#'s <c>is</c> in EACH hierarchy, since both mirror INHERITS. GR15
    /// (EXCEPTION-OBJECT references the object on declarative entry) already holds — the raise site set the
    /// register before dispatching. A null object matches nothing (spec-literal: no class describes it) → -3,
    /// the caller's §14.6.13.1.5 conversion.</para></summary>
    public void EmitObjDispatchSelector(IReadOnlyList<BoundDeclarative> decls, CodeWriter w, bool asLocal)
    {
        using (w.Block($"{MemberMod(asLocal)}int __EcObjDispatch(object? __obj)"))
        {
            EmitObjDispatchPass(decls, eo => (eo as BoundEoClass)?.Symbol.FactoryOrInstanceCsTypes,
                "// GR14 a) — object-class-name-1 entries, in the order the USE statements are specified", w);
            EmitObjDispatchPass(decls, eo => (eo as BoundEoInterface)?.Symbol.ImplementedCsTypes,
                "// GR14 b) — \"all of the USE statements in the source element are analyzed again\": "
                + "the interface-name-1 entries", w);
            w.Line("return DispatchResult.NoHandler;   // no qualifying declarative (GR14 b) tail → 14.6.13.1.5)");
        }
        w.Line();
    }

    /// <summary>One GR14 pass over the declaratives in source order. <paramref name="census"/> IS the pass:
    /// it returns the emitted C# types the entry's operand selects when the entry belongs to THIS pass, and
    /// null when it belongs to the other one — the same null-means-skip shape the F3 <c>Tier</c> scans use, so
    /// the pass structure and the per-alternative census stay one decision each. A pass with no entries emits
    /// NOTHING — not even its banner — so a unit whose Format-4 declaratives are all class entries carries no
    /// interface-pass scaffolding at all (MEASURED: its whole selector is the GR14 a) banner, one <c>is</c>
    /// line and the tail).</summary>
    private void EmitObjDispatchPass(IReadOnlyList<BoundDeclarative> decls,
        Func<BoundEoOperand, IReadOnlyList<string>?> census, string banner, CodeWriter w)
    {
        bool any = false;
        for (int i = 0; i < decls.Count; i++)
        {
            if (decls[i].Eo is not { } eo || census(eo) is not { } csTypes) continue;
            if (!any) { w.Line(banner); any = true; }
            w.Line($"if (__obj is {string.Join(" or ", csTypes)}) "
                + $"return {dispatch.RunUseCall(i, decls[i].Range)};");
        }
    }

    /// <summary>Generate <c>__IoCheckEc</c> — the EC-aware after-verb hook a statement with enabled EC-I-O
    /// checking calls INSTEAD of <c>__IoCheck</c>: the same F1 behavior (phrase short-circuits §9.1.13.1; GR3a/b
    /// file/mode selection; the GR4b outward-GLOBAL walk) plus the §9.1.13.1 status→EC raise (the per-statement
    /// <c>__mask</c> gates by name — checking is per-(name, file) at COMPILE time), the F3 GR3c–g selection
    /// behind the F1 tiers, and the fatal-status default (status 3x/4x/7x/9x + enabled checking → abnormal
    /// termination unless a RESUME redirected — §9.1.13.1 / §14.9.49.4 GR12c; checking off keeps today's
    /// continue-on-error behavior, scout hazard H6), which a SORT/MERGE implicit transfer suppresses with
    /// <c>__verbRule</c> (§14.6.13.1.3 2) — kb/Work PB993). Returns the resume action, -1 when a procedure completed
    /// normally, or -3 when none applied.</summary>
    public void EmitIoCheckEc(IReadOnlyList<BoundDeclarative> decls, CodeWriter w, bool asLocal)
    {
        using (w.Block($"{MemberMod(asLocal)}int __IoCheckEc(string __f, bool __atEnd, bool __invKey, bool __onExc, int __mask, int __locMask, string? __stmt, string? __loc, bool __verbRule = false)"))
        {
            w.Line($"string __st = {RuntimeApi.FileStatus("__f")};");
            // ⛔ THE RAISED NAME COMES FROM THE RUNTIME, NOT FROM THE STATUS ALONE (kb/Work PB526). §9.1.13.1's
            // status→EC correspondence is the DEFAULT and covers every EC-I-O condition reached through a status;
            // §13.18.34.4 GR6 b) 2 NAMES EC-I-O-LINAGE, for which no status value exists, so the connector
            // carries the name and CobolFile.IoConditionName is the one place the two are combined.
            w.Line($"string? __ec = {RuntimeApi.FileIoConditionName("__f")};   // §9.1.13.1 correspondence, or the rule-named condition");
            w.Line("bool __en = __ec is not null && (__mask & ExceptionCatalog.IoBit(__ec)) != 0;");
            // §15.32.3 r1 / §15.30.3 r1 are PER-CONDITION: the location operands record only when the RAISED
            // name's own TURN carried WITH LOCATION (__locMask shares __mask's bit positions — kb/Work R06).
            w.Line("bool __wl = __ec is not null && (__locMask & ExceptionCatalog.IoBit(__ec)) != 0;");
            w.Line($"if (__en) ExceptionState.SetIo(__ec!, {IoStatusClass.Fatal("__st")}, __f, __st, __wl ? __stmt : null, __wl ? __loc : null);");
            using (w.Block($"if (__st.Length == 0 || {IoStatusClass.Successful("__st")})"))
            {
                // A successful completion: '00' raises nothing; '0x' (x≠0) is EC-I-O-WARNING — F3 may select it
                // (no F1: those fire on unsuccessful execution only, §14.9.49.4 GR6). Nonfatal — never terminates.
                // With an exception-checking PERFORM active, a matching WHEN preempts (and ignores) the USE (GR17).
                w.Line("if (!__en) return DispatchResult.Normal;");
                // §14.9.51.4 GR27 — an end-of-page condition rides a SUCCESSFUL WRITE, and the statement's own
                // END-OF-PAGE phrase takes it: b) transfers to the phrase, and c)/d) (the exception-checking
                // PERFORM's WHEN, the USE declarative) apply only "If the END-OF-PAGE phrase is not specified".
                // The condition is still SET above (a)), so EXCEPTION-STATUS names it inside the phrase. __atEnd
                // carries the statement's own condition phrase — AT END on a READ, END-OF-PAGE on a WRITE; no
                // statement has both (kb/Work PB854).
                w.Line("if (__atEnd && ExceptionCatalog.IsEndOfPage(__ec)) return DispatchResult.Normal;   // GR27 b) — the END-OF-PAGE phrase takes it");
                if (ecState.UnitHasF3Perform)
                {
                    w.Line("int __w = __EcPerform(__ec!, __f);   // GR17 — a matching WHEN preempts USE; warning is nonfatal");
                    w.Line("return DispatchResult.ForHandledWarning(__w);   // handled → §14.6.13.1.4 2)/3): no NOT phrase");
                }
                else
                {
                    w.Line($"int __w = {(decls.Any(d => d.EcEntries is not null) ? "__EcDispatch(__ec!, __f)" : "DispatchResult.NoHandler")};");
                    w.Line("return DispatchResult.ForHandledWarning(__w);   // handled → §14.6.13.1.4 2)/3): no NOT phrase");
                }
            }
            // The statement's ON EXCEPTION phrase is its own handler for EVERY unsuccessful family (§14.9.10.4
            // GR20c — DELETE FILE): the level-3 EC is ALREADY set above (GR20b — the old emitter skipped this
            // whole hook, leaving EXCEPTION-STATUS stale inside imperative-statement-3, kb/Work PB141), and
            // only the declarative dispatch and the fatal default are suppressed, exactly like the AT END /
            // INVALID KEY suppressions below.
            w.Line("if (__onExc) return DispatchResult.Normal;");
            w.Line($"if (__atEnd && {IoStatusClass.AtEnd("__st")}) return DispatchResult.Normal;    // the statement's AT END phrase covers the family (§9.1.13.1)");
            w.Line($"if (__invKey && {IoStatusClass.InvalidKey("__st")}) return DispatchResult.Normal;   // the statement's INVALID KEY phrase covers its family (§9.1.13.1)");
            w.Line("int __sel = DispatchResult.NoHandler;");
            // The F1 file/open-mode + F3 USE declarative tiers (§14.9.49.4 GR3a–g/GR4b) — byte-identical to a pre-F3
            // build. With an exception-checking PERFORM active they run ONLY when no WHEN matched (GR17: a matching
            // WHEN ignores the USE); the frame is consulted FIRST, above these tiers.
            // The F1 file-name and open-mode tiers come from the ONE UseTierEmitter that __IoCheck and
            // __RunGlobalUse also use, so the EC-model arm cannot drift from the plain one. `__sel == -3` is
            // this arm's OWN precondition (nothing selected yet), never an edition condition: both tiers are
            // edition-invariant, and the determination is written on UseTierEmitter (kb/Work PB344).
            void EmitUseTiers()
            {
                UseTierEmitter.EmitScopeTiers(w, decls,
                    i => $"__sel = {dispatch.RunUseCall(i, decls[i].Range)}; break;", "__sel == DispatchResult.NoHandler");
                if (decls.Any(d => d.EcEntries is not null))
                    w.Line("if (__sel == DispatchResult.NoHandler && __en) __sel = __EcDispatch(__ec!, __f);   // F3 tiers behind F1 (GR3c–g)");
                if (dispatch.OuterGlobalUse)
                    w.Line("if (__sel == DispatchResult.NoHandler && __outer.__RunGlobalUse(__f)) __sel = DispatchResult.Normal;   // outward GLOBAL walk (GR4b)");
            }
            if (ecState.UnitHasF3Perform)
            {
                w.Line("bool __wh = false;");
                w.Line("__sel = ExceptionState.RunTopFrame(__ec!, __f, out __wh);   // GR17 — a matching WHEN preempts the USE declaratives");
                w.Line("if (!__wh) __sel = DispatchResult.NoHandler;   // no WHEN matched → fall to the USE tiers below");
                using (w.Block("if (!__wh)")) EmitUseTiers();
            }
            else EmitUseTiers();
            w.Line("if (DispatchResult.SuppressesFatal(__sel)) return __sel;   // RESUME redirected/suppressed (§14.9.33)");
            // §14.6.13.1.3 3) sends a fatal EC-I-O condition on to "the following rules", so 6)'s automatic propagation
            // applies here exactly as at EmitSelection: no procedure qualified (NoHandler) under >>PROPAGATE ON (kb/Work PB1119).
            if (PropagationReturn("__ec!") is { } propagate)
                w.Line($"if (__sel == DispatchResult.NoHandler && __en && !__verbRule && {IoStatusClass.Fatal("__st")}) {propagate}"
                    + "   // automatic propagation (§14.6.13.1.3 6))");
            // ⛔ §14.6.13.1.3 2) PRECEDES 5) and 7): "If the executed statement is a MERGE or SORT statement, then
            // the rules for those statements apply." A SORT/MERGE implicit transfer passes __verbRule, and the fatal
            // disposition is then the verb's own (SortEmitter.EmitTransferDisposition — terminate the statement,
            // bypass the file, or continue), never the run-unit termination below (kb/Work PB993).
            w.Line($"if (__en && !__verbRule && {IoStatusClass.Fatal("__st")})");
            w.Line("    throw new CobolFatalException(__ec!, \"I-O status \" + __st + \" on \" + __f"
                + " + (__stmt is null ? \"\" : \" (\" + __stmt + \")\")) { Dispatched = true };   // §9.1.13.1 fatal classes; §14.6.13.1.3 #5/#7 (dispatched above)");
            // Normal = a qualifying procedure ran and completed normally; NoHandler = none qualified (HookNoProcedure). The two
            // are the same to every explicit I-O statement (both fall through) but not to the SORT/MERGE implicit
            // transfers, whose rules turn on "an applicable USE procedure that completes normally" (§14.9.24.4
            // GR7 a), GR12 a)/b)) — kb/Work PB993.
            w.Line("return __sel;");
        }
        w.Line();
    }

    /// <summary>Generate the exception-checking (Format-3) PERFORM interceptor plumbing (ISO §14.9.28.4 GR17-20) —
    /// emitted ONLY for a unit that contains an F3 PERFORM (<see cref="EcState.UnitHasF3Perform"/>), so a non-F3
    /// unit's source is byte-identical. <c>__EcPerform</c> consults the ambient F3-frame stack first (GR17: a
    /// matching WHEN preempts — and ignores — the USE declaratives) and falls to <c>__EcDispatch</c> (or the
    /// no-declarative <c>NoHandler</c>) only when no frame handled the condition. <c>__RunF3</c> composes a WHEN handler
    /// (imp-2/imp-3) with WHEN COMMON (imp-4, GR19): COMMON runs ONLY after the handler COMPLETES (falls off →
    /// <c>Normal</c>); a RESUME NEXT STATEMENT (<c>ResumeNext</c>) is a transfer out of the handler and short-circuits COMMON
    /// (design SSOT §9.6 Q3). Both handler bodies are bounded pc-ranges run by the reused <c>__RunUse</c>.</summary>
    public void EmitPerformInterceptor(CodeWriter w, bool asLocal)
    {
        EmitEcPerformMember(w, asLocal);
        EmitRunF3(w, asLocal);
    }

    /// <summary>The declaration modifier of a generated selection member: a class MEMBER for a program (and the
    /// class-level funnels of a COBOL class), a LOCAL FUNCTION inside an OO method whose own declaratives or
    /// Format-3 PERFORM give it method-scoped selection (kb/Work PB1010; design SSOT §9.10) — a local function
    /// of the same name shadows the class member, so every raise site in the method's body reaches the method's
    /// OWN selection (§14.9.49.4 GR3/GR4 a)) with no second spelling of the call.</summary>
    internal static string MemberMod(bool asLocal) => asLocal ? "" : "private ";

    /// <summary>Emit the class-member <c>__EcPerform</c> raise-site funnel (ISO §14.9.28.4 GR17: a matching WHEN
    /// preempts the USE declaratives). ALWAYS a class member — it reaches a handler only through
    /// <see cref="ExceptionEngine.RunTopFrame"/> → the frame's Matcher (never <c>__RunF3</c> directly), so it is
    /// class-callable even when the F3 PERFORM (and hence <c>__RunF3</c>/<c>__RunUse</c>) is METHOD-LOCAL (an OO
    /// method's F3 PERFORM, design SSOT §9.10). Emitted once per program (the interceptor) and once per class that
    /// has any method-F3 (<see cref="OoEmitter"/>, gated on <c>bound.Ec.HasF3Perform</c>).</summary>
    public void EmitEcPerformMember(CodeWriter w, bool asLocal = false)
    {
        using (w.Block($"{MemberMod(asLocal)}int __EcPerform(string __ec, string __f)"))
        {
            w.Line("int __a = ExceptionState.RunTopFrame(__ec, __f.Length == 0 ? null : __f, out bool __h);");
            w.Line($"return __h ? __a : {(ecState.UnitHasF3 ? "__EcDispatch(__ec, __f)" : "DispatchResult.NoHandler")};   "
                + "// GR17/18 win over USE; else the USE tiers / NoHandler");
        }
        w.Line();
    }

    /// <summary>Emit <c>__RunF3</c> (the WHEN handler + WHEN COMMON composer, ISO §14.9.28.4 GR19). SCOPE-PARAMETERIZED:
    /// a class MEMBER for a program's F3 PERFORM, a method-LOCAL function for an OO method's F3 PERFORM (design SSOT
    /// §9.10 — it calls <c>__RunUse</c>, which calls the method-local <c>__MDispatch</c>). Its sole caller is the frame
    /// Matcher, emitted inline where the F3 PERFORM statement is (so a method-local <c>__RunF3</c> is in scope).</summary>
    public void EmitRunF3(CodeWriter w, bool asLocal)
    {
        using (w.Block($"{MemberMod(asLocal)}int __RunF3(int __u, int __pc, int __cu, int __cpc)"))
        {
            // §14.9.28.4 GR14: "An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of
            // imperative-statement-1" — so imp-2/3/4 run with NO exception checking enabled (§14.6.13.1.1: "if
            // checking for an exception that occurs is not enabled, no exception condition is raised"). It has to
            // be done at runtime, and not only by binding the handler bodies under a disabled TurnState: the ambient
            // gates are set by the guard around the RAISING statement, and this composer is called from inside that
            // guard. It is done by __RunUse, which opens the all-off checking scope for EVERY procedure it runs
            // (kb/Work PB891) — a second push here would be a second realization of the same window.
            w.Line("int __a = __RunUse(__u, __pc, __pc);   // imp-2 / imp-3 (a single-pc synthetic handler range)");
            w.Line("if (__a == DispatchResult.Normal && __cpc >= 0) __a = __RunUse(__cu, __cpc, __cpc);   // WHEN COMMON (imp-4, GR19); ResumeNext short-circuits");
            w.Line("return __a;");
        }
        w.Line();
    }
}
