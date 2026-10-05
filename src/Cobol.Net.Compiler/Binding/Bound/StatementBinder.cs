// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using Antlr4.Runtime.Tree;
using CobolNet.Runtime;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using Microsoft.CodeAnalysis.CSharp;

using CobolNet.Binding.Model;
using CobolNet.Binding.Procedure;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// Builds the <see cref="BoundProgram"/> from a parsed program unit: it resolves every reference to a
/// <see cref="Place"/>, decodes every literal, and binds every expression / condition / statement into a bound node
/// exactly once (COBOLNET_DESIGN §2). The backend then renders the bound tree — it never re-walks the parse tree.
/// </summary>
public sealed partial class StatementBinder(DataBinder data, ReferenceResolver refs)
{
    // ── The Step-10 collaborator seam (P7; the phase doc's §Step 10 AS-BUILT PLAN): ONE BinderContext per
    //    binder instance (= per unit / class roster) and ONE instance of each verb collaborator (their
    //    counters/memos are per-unit lifetime). Lazy — the primary-ctor fields are captured, not fields, so
    //    eager initializers cannot reference them. During the incremental extraction the collaborators reach
    //    sibling collaborators directly through THIS host's accessors (e.g. host.Expr.BindExpr) — the 10t
    //    final wiring: this class is now dispatch + the mark/drain wrap protocol + the composition root. ──
    private BinderContext? _binderCtx;
    private InspectBinder? _inspectBinder;
    private EvaluateBinder? _evaluateBinder;
    private StringUnstringBinder? _stringUnstringBinder;
    private MoveBinder? _moveBinder;
    private CorrespondingBinder? _corrBinder;
    private InitializeBinder? _initializeBinder;
    private BinderContext Ctx => _binderCtx ??= new BinderContext(data, refs);
    private InspectBinder Inspect => _inspectBinder ??= new InspectBinder(Ctx, this);
    private EvaluateBinder Evaluate => _evaluateBinder ??= new EvaluateBinder(Ctx, this);
    private StringUnstringBinder Strings => _stringUnstringBinder ??= new StringUnstringBinder(Ctx, this);
    internal MoveBinder Move => _moveBinder ??= new MoveBinder(Ctx, this, Corr);   // the FROM/INTO phrases bind through it (PB348)
    private ReportWriterBinder? _rwBinder;
    internal ReportWriterBinder Rw => _rwBinder ??= new ReportWriterBinder(Ctx, this);
    private FileLockBinder? _fileLockBinder;
    private FileLockBinder FileLock => _fileLockBinder ??= new FileLockBinder(Ctx, this);
    private PtrBinder? _ptrBinder;
    internal PtrBinder Ptr => _ptrBinder ??= new PtrBinder(Ctx, this);
    private KeyedIoBinder? _keyedIoBinder;
    private KeyedIoBinder KeyedIo => _keyedIoBinder ??= new KeyedIoBinder(Ctx, this, FileLock);
    private SequentialIoBinder? _seqIoBinder;
    internal SequentialIoBinder SeqIo => _seqIoBinder ??= new SequentialIoBinder(Ctx, this, KeyedIo, FileLock);
    private AcceptDisplayBinder? _acceptBinder;
    private AcceptDisplayBinder Accept => _acceptBinder ??= new AcceptDisplayBinder(Ctx, this);
    private SortBinder? _sortBinder;
    private SortBinder Sort => _sortBinder ??= new SortBinder(Ctx, this);
    private CallBinder? _callBinder;
    private CallBinder Call => _callBinder ??= new CallBinder(Ctx, this);
    private UdfBinder? _udfBinder;
    internal UdfBinder Udf => _udfBinder ??= new UdfBinder(Ctx, this);
    private ParameterConformance? _parameterConformance;
    /// <summary>The ONE argument half of ISO §14.8.2 — shared by the Format-2 CALL and the function-identifier
    /// (kb/Work PB1418) — and the activation operand screens CALL and INVOKE state in the same words (the storage
    /// section rule and the bit-alignment proof, kb/Work PB1137).</summary>
    internal ParameterConformance Params => _parameterConformance ??= new ParameterConformance(Ctx, this);
    private IntrinsicBinder? _intrinsicBinder;
    internal IntrinsicBinder Intrinsic => _intrinsicBinder ??= new IntrinsicBinder(Ctx, this);
    private ControlFlowBinder? _controlFlowBinder;
    internal ControlFlowBinder ControlFlow => _controlFlowBinder ??= new ControlFlowBinder(Ctx, this);
    private SetBinder? _setBinder;
    internal SetBinder Set => _setBinder ??= new SetBinder(Ctx, this);
    private SearchBinder? _searchBinder;
    private SearchBinder Search => _searchBinder ??= new SearchBinder(Ctx, this);
    private SetAlterBinder? _setAlterBinder;
    internal SetAlterBinder Alter => _setAlterBinder ??= new SetAlterBinder(Ctx);
    private SendingValueTemp? _sendingValue;
    /// <summary>The ONE §14.9.25.4 GR1 / §14.9.13.4 GR3 sending-value materializer (kb/Work PB394) — shared by
    /// every verb that uses one sender in more than one place.</summary>
    internal SendingValueTemp SendingValue => _sendingValue ??= new SendingValueTemp(Ctx);

    private ArgumentOrder? _argumentOrder;
    /// <summary>The ONE §8.4.3.2.4 GR2 left-to-right argument order (kb/Work PB1423) — shared by the intrinsic
    /// call and the user-defined activation.</summary>
    internal ArgumentOrder ArgOrder => _argumentOrder ??= new ArgumentOrder(Ctx, SendingValue, Oo.OoDrainPropertyGets);

    private ConditionBinder? _conditionBinder;
    internal ConditionBinder Cond => _conditionBinder ??= new ConditionBinder(Ctx, this);

    private ArithmeticBinder? _arithmeticBinder;
    internal ArithmeticBinder Arith => _arithmeticBinder ??= new ArithmeticBinder(Ctx, this);

    private ExpressionBinder? _exprBinder;
    internal ExpressionBinder Expr => _exprBinder ??= new ExpressionBinder(Ctx, this);

    private EcBinder? _ecBinder;
    internal EcBinder Ec => _ecBinder ??= new EcBinder(Ctx, this);

    private OoBinder? _ooBinder;
    internal OoBinder Oo => _ooBinder ??= new OoBinder(Ctx, this);

    /// <summary>The binder's public EC entry point (BinderDriver / the OO bind half configure the compilation
    /// group's TurnState + this unit's PROGRAM-ID per bound unit); the state lands on <c>ctx.EcState</c>.</summary>
    public void ConfigureEc(TurnState turn, IReadOnlyList<Frontend.Preprocessor.DirectiveSite> sites,
        string programName) => Ec.ConfigureEc(turn, sites, programName);

    /// <summary>The compilation group's user-function signature table (FUNCTION-ID name → RETURNING +
    /// USING descriptions), built by the run-unit emitter between the DATA and PROCEDURE bind phases.
    /// Null in unit-test direct construction and in class-unit binders — every user-function reference
    /// there fails loud (COBOLNET1505).</summary>
    public IReadOnlyDictionary<string, UserFunctionSignature>? UserFunctions { get; set; }

    /// <summary>The programs an AS NESTED call in THIS unit may name (§14.9.4.3 SR15: directly contained in
    /// the caller, or a visible COMMON program), each with its bound PD-header formals — built per unit in
    /// BinderDriver.BindUnitProcedure, after every unit's DATA has bound (the UserFunctions precedent), so
    /// GR9's the-formal-decides mode derivation is a bind-time lookup (kb/Work PB131).</summary>
    public IReadOnlyDictionary<string, CalleeSignature>? NestedCallables { get; set; }

    /// <summary>The program prototypes THIS unit may name (kb/Work PB237): every §12.3.8.2 program-specifier
    /// visible here, resolved through §12.3.8.4 GR10 against the compilation group, plus the §8.4.6.8 spelling
    /// that needs no specifier at all — "the program-name of a containing program definition". Keyed by
    /// program-prototype-name (§8.3.2 case-insensitive). It is the §14.9.4.3 SR16 / §14.9.5.3 SR3 lookup, and its
    /// hit's <c>Signature</c> is what §14.9.4.4 GR7 means by "program-prototype-name-1 is used to determine the
    /// characteristics of the called program". Built per unit in BinderDriver.BindUnitProcedure, the
    /// NestedCallables precedent; null in unit-test direct construction and in class-unit binders.</summary>
    public IReadOnlyDictionary<string, ProgramPrototype>? ProgramPrototypes { get; set; }

    /// <summary>A program-prototype-name's bound signature, through the §8.4.6.8 scope table
    /// (<see cref="ProgramPrototypes"/>) its declaration was already screened against — THE ONE reader every
    /// signature comparison asks (the SET program-pointer pairs, the address-identifier argument, a restricted
    /// program-pointer's CALL, kb/Work PB609). Null for a null or unknown name and for a §12.3.8.4 GR10 c)
    /// external-repository prototype, which <see cref="PrototypeSignatures.Same"/> treats as conforming.</summary>
    internal CalleeSignature? ProgramSignatureOf(string? prototypeName) =>
        prototypeName is not null && ProgramPrototypes?.TryGetValue(prototypeName, out var p) == true
            ? p.Signature
            : null;

    /// <summary>The unit's RECURSIVE attribute — explicit, or inherited per §11.10.4 GR4 (kb/Work PB137:
    /// §14.9.7.3 SR1 bans COMMIT/ROLLBACK in a recursive source element). Set by BinderDriver.</summary>
    public bool UnitRecursive { get; set; }

    /// <summary>The containing FUNCTION-ID unit's own function name, when this binder binds a function
    /// definition's body — §8.4.6.6: a referenced function-prototype-name shall be "the user-function-name
    /// of the containing function definition OR a function-prototype-name declared in the REPOSITORY
    /// paragraph", so self-recursion needs NO repository entry (a present self-entry is ignored per
    /// §12.3.8 GR11 — same resolution either way). Null in program units.</summary>
    public string? UdfSelfName { get; set; }

    /// <summary>The kind of source element this binder's PROCEDURE DIVISION belongs to (ISO §14.2.2 SR10) — the
    /// input every EXIT / GOBACK placement rule needs (kb/Work PB403). Set by <c>BinderDriver</c> from the same
    /// unit facts as <see cref="UdfSelfName"/>; stored on the <see cref="BinderContext"/> so the ONE placement
    /// probe (<c>ctx.Enclosing</c>) owns it rather than a second per-verb copy. A method body binds on a CLASS
    /// unit's binder and overrides this through the entered method scope.</summary>
    internal SourceElementKind UnitKind { get => Ctx.UnitKind; set => Ctx.UnitKind = value; }

    /// <summary>True when the unit being bound is a nested (contained) program — set from <c>BoundUnit.Parent</c>
    /// at binder construction. Gates FUNCTION MODULE-NAME NESTED (§15.65.3 argument rule 1 — NESTED shall be
    /// specified only within a contained program).</summary>
    public bool InNestedProgram { get; init; }
    internal CorrespondingBinder Corr => _corrBinder ??= new CorrespondingBinder(Ctx, this);
    internal InitializeBinder Init => _initializeBinder ??= new InitializeBinder(Ctx, this);

    /// <summary>The group's pass-1 class symbol table (deep-dive D1) — set by the run-unit emitter before
    /// binding so INVOKE resolves classes/methods defined anywhere in the group. Null ⇔ empty group.</summary>
    public OoClassTable? OoClasses { get; set; }

    /// <summary>The CLASS whose method bodies this binder is binding (per-binder-instance configuration, set
    /// ONCE at construction by <c>Oo/OoDriver</c> — init-only since P9 Step 6; null in a program unit) — the
    /// SELF/SUPER resolution root (§8.4.3.8: SELF resolves on the current
    /// class's chain, SUPER starts at its BASE; slice 3b).</summary>
    public OoClassSymbol? OoCurrentClass { get; init; }

    /// <summary>True when this binder binds the FACTORY roster (§11.4; per-binder-instance configuration,
    /// init-only since P9 Step 6): SELF/SUPER resolve over the FACTORY interface
    /// (§14.9.23.3 SR4f/h) and SELF|SUPER "NEW" binds the ACTIVE-CLASS creation form (§16.2.1).</summary>
    public bool OoInFactory { get; init; }


    /// <summary>True while binding a statement inside a METHOD body — the D8 context switch (GOBACK → method
    /// return; RAISE SELF / INVOKE SELF|SUPER placement, §8.4.3.8; the CALL … AS NESTED context, §14.9.4.3 SR13).
    /// <para>⛔ A NAMED READING OF THE ONE BIND-POSITION PROBE, not a second derivation (kb/Work PB403): it is
    /// exactly <c>ctx.Enclosing.SourceElement is MethodDefinition</c>. Keeping it as its own
    /// <c>CurrentMethodScope is not null</c> test is how the §14.9.14.3 SR7 arm came to answer "is this a program
    /// procedure division?" with a method/not-method bit and admit the other three §14.2.2 SR10 elements.</para></summary>
    internal bool InMethod => Ctx.SourceElement is SourceElementKind.MethodDefinition;

    /// <summary>Install the D18 segment-materialization hook on this binder's <see cref="ReferenceResolver"/>
    /// (fix-queue PB17). Called from BOTH procedure-bind entry points — <see cref="Bind"/> and
    /// <see cref="BindMethodRoster"/> — because the primary constructor has no body to install it in, and because
    /// a resolver only needs the hook once a PROCEDURE DIVISION is actually being bound. Idempotent.
    /// <para>⛔ THESE TWO (the materializer and the EC-BOUND-REF-MOD checking query) ARE THE ONLY EDGES FROM THE
    /// PROCEDURE BINDER BACK INTO THE RESOLVER, and they are delegates, not type references: the dependency is
    /// one-way by design (<c>StatementBinder(DataBinder, ReferenceResolver)</c>), so a resolver that never gets them
    /// keeps the pre-D18 loud posture — which is exactly what the DATA-division throwaway resolvers in
    /// <c>DataBinder.Constants</c>/<c>Ptr</c> should have.</para></summary>
    private void AttachSegmentMaterializer()
    {
        refs.MaterializeSegment ??= MaterializeSubscriptSegment;
        refs.FreezeOdoExtent ??= FreezeOdoExtentBeforeRefMod;   // §14.6.4 steps 6 → 7 (kb/Work PB1123)
        // The second edge, of the same shape: the literal reference-modification range screen asks whether
        // EC-BOUND-REF-MOD checking is enabled at the reference's line (kb/Work PB1707 part 1). Read through
        // Ctx.EcState.Turn AT CALL TIME, so an exception-checking PERFORM's GR14 overlay is the state it sees.
        refs.RefModCheckingAt ??= line => Ctx.EcState.Turn.Enabled("EC-BOUND-REF-MOD", null, line);
        // The object of an object property that a word cannot spell — SELF, SUPER, a view, a function (§8.4.3.1.2
        // Format 7; kb/Work PB1425) — and the view of a qualified data item written as one chain: both are bound by the
        // OO binder, which owns the method context and the view's temporaries, through the same one-way edge.
        refs.BindPropertyObject ??= Oo.OoBindPropertyObject;
        refs.ApplyObjectView ??= Oo.OoApplyObjectView;
    }

    /// <summary>The ONE guard in front of <see cref="ReportWriterBinder.BindReportGroupClauses"/>: bind the report-section
    /// clause expressions of the reports THIS unit declares, when it declares any. A program unit and a class's method
    /// roster both ask it, so no unit kind can forget the procedure phase of its own reports.</summary>
    private void BindDeclaredReportClauses()
    {
        if (Ctx.Data.Reports.Count > 0) Rw.BindReportGroupClauses();
    }

    /// <summary>Bind a program unit's PROCEDURE DIVISION into a <see cref="BoundProgram"/>.</summary>
    public BoundProgram Bind(Core.ProgramUnitContext program)
    {
        AttachSegmentMaterializer();
        // Report-section PRESENT WHEN conditions + VARYING expressions (§13.18.41/§13.18.64) bind through the
        // procedure-phase binders (they resolve ordinary data references) — before statement binding, and even
        // for a PD-less unit (the report emission does not require a PROCEDURE DIVISION).
        BindDeclaredReportClauses();
        if (program.procedureDivision() is not { } pd) return new BoundProgram([]);
        Ec.EcCollectPdRaising(pd);   // the PD-header RAISING list (§14.2.1) — consumed by the GOBACK/EXIT SR2 check
        var table = Ctx.Table;
        table.CollectParagraphs(pd);

        var bound = new List<BoundParagraph>(table.Paragraphs.Count);
        for (int i = 0; i < table.Paragraphs.Count; i++)
        {
            // Section + cursor as ONE scoped bind position (10s; no method overlay in a program unit) —
            // §8.4.2.2 in-section resolution + the RESUME SR1/SR2 / §15.30 location anchoring.
            using var _ = Ctx.EnterMethodScope(table.ParaSections[i], null, i);
            var sentences = new List<IReadOnlyList<BoundStatement>>();
            foreach (var sentence in table.Paragraphs[i].Sentences)
                sentences.Add(sentence.statement().Select(BindStatement).ToList());
            // The paragraph's LAST executable statement source line — the X3.23-1985 DEBUG-LINE for a FALL THROUGH
            // trigger (VCR 7.17; only used when the debug facility is active). 0 for an empty paragraph.
            int lastLine = table.Paragraphs[i].Sentences
                .SelectMany(s => s.statement()).LastOrDefault() is { } last ? Ctx.SourceLine(last) : 0;
            bound.Add(new BoundParagraph(table.Paragraphs[i].Cobol, sentences, lastLine));
        }
        // Append the exception-checking (Format-3) PERFORM handler pc-ranges (imp-2/3/4) above the whole main pc
        // space (ISO §14.9.28.4 GR17; §9.1-C). handlerBase == table.HandlerBasePc == the frozen main count, so
        // bound[handlerBase + k] is the k-th handler — matching the pc AddF3Handler stored on each BoundExceptionMatch.
        int handlerBase = bound.Count;
        bound.AddRange(table.F3Handlers);
        return new BoundProgram(bound, table.EntryPc, table.Declaratives, Ctx.EcState.BuildFeatures(),
            DebugSubjects: table.DebugSubjects.Count > 0 ? table.DebugSubjects : null,
            F3HandlerBasePc: table.F3Handlers.Count > 0 ? handlerBase : null,
            F3HandlerOwners: table.F3Handlers.Count > 0 ? table.F3HandlerOwners : null,
            RaisingObjects: [.. Ctx.EcState.PdRaisingObjects]);
    }

    // ⛔ `OoScopeHint` IS GONE (kb/Work PB390). It appended "names resolve METHOD-LOCALLY (§11.7)" to the four
    //    unknown-procedure BoundUnsupported guards — a hint on a RUN-TIME abort that told the user WiseOwl COBOL had
    //    not implemented a feature. The hint survives where it belongs: on the compile-time diagnostic, composed
    //    once by StatementValidation.RejectProcedureName from the ONE resolution's own method-scope fact, so no
    //    site can carry the hint without the report or the report without the hint.

    /// <summary>
    /// Bind a CLASS body: every method's paragraphs flatten into the class's ONE pc space (source order), each
    /// method holding its contiguous exit-bounded range — the emit-into-a-type spine's binding half. A method's
    /// DECLARATIVES (kb/Work PB1010 — §14.2.2 SR10 admits Format 1 in a method definition) are collected first,
    /// at the head of its range exactly as a program's are (§14.2.3 GR1 — execution begins with the first
    /// nondeclarative procedure), so the method's slice is [DeclStartPc..EndPc] and its entry is EntryPc.
    /// </summary>
    public BoundProgram BindMethodRoster(OoClassSymbol cls, IReadOnlyList<OoMethodSymbol> roster)
    {
        AttachSegmentMaterializer();
        // The report section of an object or factory binds its PRESENT WHEN / VARYING / SOURCE / SUM operands exactly
        // as a program unit's does, once, before any method body — a report operand is a written reference bound in the
        // PROCEDURE phase (kb/Work PB482, PB1292), and a class's methods are where that phase runs for it.
        BindDeclaredReportClauses();
        var used = new HashSet<string>(StringComparer.Ordinal);
        var methods = new List<BoundMethod>(roster.Count);
        var table = Ctx.Table;
        // scope → owning method, so the appended Format-3 handler pc-ranges (keyed by their binding-time
        // OoMethodScope) map back to each method's OoMethodBinding for the per-method slice (design SSOT §9.10).
        var scopeToMethod = new Dictionary<OoMethodScope, OoMethodSymbol>();

        foreach (var m in roster)
        {
            if (m.PropertySubject is not null)
            {
                // A PROPERTY-clause-SYNTHESIZED accessor: no COBOL body exists — the emitter renders the
                // direct field read/write (D-P1; observably identical to the §13.18.42 GR1/GR2 implicit
                // MOVE methods). It still occupies a roster slot (override/implements machinery applies).
                m.Binding!.EntryPc = table.Paragraphs.Count;
                m.Binding!.DeclStartPc = m.Binding!.EntryPc;
                m.Binding!.EndPc = table.Paragraphs.Count - 1;   // empty body by construction
                methods.Add(new BoundMethod(m.Name, m.CsName, m.Binding!.EntryPc, m.Binding!.EndPc));
                continue;
            }
            // The method's DATA (LINKAGE → params-as-locals, LOCAL-STORAGE → locals, method-WS → statics) was
            // bound by DataBinder.OoBindMethodData before any body binds; here we link its name scope so the
            // per-pc switch below activates §11.7 GR5 shadowing while this method's statements bind.
            var scope = new OoMethodScope { Data = m.DataScope, MethodName = m.Name, Formals = m.Binding!.Formals };
            scopeToMethod[scope] = m;         // §9.10 — handler pc-ranges map back to this method via its scope
            Ctx.CurrentMethodScope = scope;   // the COLLECTION cursor (AddParagraph registers method-locally)
            m.Binding!.DeclStartPc = table.Paragraphs.Count;
            m.Binding!.EntryPc = table.Paragraphs.Count;
            if (m.Ctx.procedureDivision() is { } pd)
            {
                // The method's declaratives portion (§14.2.1 Format 1; §14.2.2 SR10): the declarative sections take
                // the pcs BELOW the method's entry, reached only through its USE selection or an explicit PERFORM
                // (§14.9.49.3 SR4) — the program layout, one method at a time (kb/Work PB1010).
                m.Binding!.Declaratives = table.CollectMethodDeclaratives(pd, used);
                m.Binding!.EntryPc = table.Paragraphs.Count;
                // §14.4.3 — a method's procedure division may also open with unnamed sentences (same rule as a
                // program's; the header form is shared). They take the method's ENTRY pc, set just above.
                table.AddAnonymousParagraph(pd.sentence(), null, used);
                foreach (var unit in pd.procedureUnit())
                {
                    if (unit.paragraphDefinition() is { } para)
                        table.AddParagraph(para.paragraphName().GetText(), para.sentence(), null, used);
                    else if (unit.sectionDefinition() is { } section)
                    {
                        // A section inside a method is a method-local pc range (the legacy COBOL0116 reject is
                        // superseded: with per-method scopes the range cannot truncate or leak — trap #5).
                        var info = new SectionInfo(section.sectionName().GetText(), table.Paragraphs.Count);
                        table.AddAnonymousParagraph(section.sentence(), info, used);
                        foreach (var p in section.paragraphDefinition())
                            table.AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
                        info.CloseAt(table.Paragraphs.Count - 1);   // zero paragraphs ⇒ EMPTY (§14.4.2)
                        scope.Sections.Declare(info.Name, info);
                    }
                }
            }
            m.Binding!.EndPc = table.Paragraphs.Count - 1;
            methods.Add(new BoundMethod(m.Name, m.CsName, m.Binding!.EntryPc, m.Binding!.EndPc));
        }
        Ctx.CurrentMethodScope = null;

        var bound = new List<BoundParagraph>(table.Paragraphs.Count);
        OoMethodSymbol? raisingLoadedFor = null;
        for (int i = 0; i < table.Paragraphs.Count; i++)
        {
            // The ordered quadruple (section → method scope → §11.7 GR5 data shadowing → cursor) is ONE
            // scoped operation (10s) — set coherently, restored coherently on dispose.
            using var _ = Ctx.EnterMethodScope(table.ParaSections[i], table.ParaMethods[i], i);
            // §11.9.4 GR1 (kb/Work PB135): a method's own OPTIONS model governs ITS body only — swapped in
            // for this pc's sentences (the binder reads ctx.Options per statement; statements bind HERE, in
            // the per-pc pass, not in the roster registration loop above) and restored after.
            var savedOptions = data.Options;
            if (table.ParaMethods[i] is { } mScope && scopeToMethod.TryGetValue(mScope, out var mSym))
            {
                if (mSym.MethodOptions is { } mOpt) data.Options = mOpt;
                // A method IS a source element (§14.9.18.3 SR2/SR4, §14.9.14.3 SR3/SR5 — "the procedure division
                // header of the source element containing this … statement"): ITS OWN header RAISING phrase is
                // the per-element state while ITS statements bind. ⛔ It is loaded HERE, in the per-pc pass where
                // statements bind — it used to be loaded in the roster-registration loop above, which binds no
                // statement, so every method's GOBACK/EXIT RAISING was checked against the LAST method's header
                // (kb/Work PB815 — measured: a class whose last method raised CS815 refused the first method's
                // `GOBACK RAISING` of a FACTORY OF CE815 its own header listed).
                if (!ReferenceEquals(raisingLoadedFor, mSym))
                {
                    Ec.EcLoadPdRaising(mSym.Raising);
                    raisingLoadedFor = mSym;
                }
            }
            try
            {
                var sentences = new List<IReadOnlyList<BoundStatement>>();
                foreach (var sentence in table.Paragraphs[i].Sentences)
                    sentences.Add(sentence.statement().Select(BindStatement).ToList());
                bound.Add(new BoundParagraph(table.Paragraphs[i].Cobol, sentences));
            }
            finally { data.Options = savedOptions; }
        }
        // Append the exception-checking (Format-3) PERFORM handler pc-ranges (imp-2/3/4) above the whole class pc
        // space (ISO §14.9.28.4 GR17; design SSOT §9.10 — the SAME allocation the program path uses at Bind():163).
        // Empty until the F3-in-a-method un-reject (increment M4) — a class with no method-F3 stays byte-identical.
        int handlerBase = bound.Count;
        bound.AddRange(table.F3Handlers);
        StampMethodHandlerSlices(handlerBase, table.F3HandlerMethods, scopeToMethod, roster);
        return new BoundProgram(bound, 0, null, Ctx.EcState.BuildFeatures(), methods,
            F3HandlerBasePc: table.F3Handlers.Count > 0 ? handlerBase : null,
            F3HandlerOwners: table.F3Handlers.Count > 0 ? table.F3HandlerOwners : null);
    }

    /// <summary>Stamp each method's contiguous Format-3 handler sub-range onto its <see cref="OoMethodBinding"/>
    /// (design SSOT §9.10). The class handler pc-space partitions into per-method runs in method order — a method's
    /// handlers are all appended while its paragraphs bind (the pc-order second bind loop). Asserts that invariant
    /// LOUDLY (a future reorder that breaks contiguity must fail here, never emit a mis-sliced dispatch).</summary>
    private static void StampMethodHandlerSlices(int handlerBase, IReadOnlyList<OoMethodScope?> handlerMethods,
        Dictionary<OoMethodScope, OoMethodSymbol> scopeToMethod, IReadOnlyList<OoMethodSymbol> roster)
    {
        if (handlerMethods.Count == 0) return;
        int k = 0;
        while (k < handlerMethods.Count)
        {
            var sc = handlerMethods[k];
            int start = k;
            while (k < handlerMethods.Count && ReferenceEquals(handlerMethods[k], sc)) k++;
            if (sc is null || !scopeToMethod.TryGetValue(sc, out var m)) continue;   // a program-unit handler (no method)
            if (m.Binding!.HandlerCount != 0)
                throw new InvalidOperationException(
                    $"Format-3 handler contiguity invariant violated: method '{m.Name}' has a non-contiguous handler run "
                    + "(design SSOT §9.10 assumes per-method handler pc-ranges are appended consecutively)");
            m.Binding!.HandlerStartPc = handlerBase + start;
            m.Binding!.HandlerCount = k - start;
        }
    }

    // ── Statements ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind one statement, then apply the compile-time TurnState fold (StatementBinder.Exceptions.cs):
    /// a statement under enabled EC checking wraps in <see cref="BoundEcChecked"/>; checking-off binds the bare
    /// node — the zero-scaffolding gate (ISO §7.3.25.4 GR1 default OFF; deep-dive D10).</summary>
    private BoundStatement BindStatement(Core.StatementContext s)
    {
        // The diagnostic cursor (kb/Work PB82): every diagnostic reported while THIS statement binds — its own
        // verb binder, its operands, its conditions — carries the statement's source position; a nested statement
        // positions its own and restores this one on exit.
        using var _ = data.Edition.At(s);
        // Object-property references (D-P2) and user-function activations (M2-UDF-1): mark-on-entry /
        // drain-own-suffix — a reference resolved while THIS statement bound (including in its condition)
        // belongs to THIS statement's wrap; one resolved inside a nested statement was already drained by
        // that statement's own BindStatement. The UDF wrap is the INNER sequence (function activations are
        // always pre-ops, §8.4.3.2.3 SR1), so a property argument's GET — a pre-op of the OUTER property
        // wrap — still runs before the activation that consumes its temp.
        var marks = Udf.Mark;
        // The operand-activation scope (kb/Work PB892): THIS statement's line is the one whose §7.3.25 profile an
        // operand activation carries, and THIS statement counts only the activations it drains itself — both are
        // saved around a nested statement's bind and restored after it.
        int savedLine = StatementLine, savedActivations = data.OperandEvaluations;
        StatementLine = s.Start.Line;
        data.OperandEvaluations = 0;
        int errorMark = data.Edition.ErrorsRecorded, refusalMark = data.Edition.RefusalsBound;
        int unbuiltMark = data.Edition.UnbuiltMark;
        int controlStoreMark = _controlStoreReports;
        var core = BindStatementCore(s);
        // ISO §14.9.49.3 SR11 (kb/Work PB363) — asked HERE, of every statement bound in a USE BEFORE REPORTING
        // procedure, because this is the one funnel every statement passes through. A nested statement binds (and
        // is asked) first, at its own position; the enclosing statement is asked only when none of its nested
        // statements already reported, so one store is one diagnostic.
        if (Ctx.Enclosing.InBeforeReportingProcedure && _controlStoreReports == controlStoreMark
            && Rw.CheckControlDataStores(core))
            _controlStoreReports++;
        bool drewError = data.Edition.ErrorsRecorded != errorMark;
        // ⛔ THE REFUSAL LEDGER'S CHECK (kb/Work PB1029). A refusal node — a BoundRejected, or a refused operand or
        // condition — says the SOURCE is wrong and that its site reported why. If this statement bound one and
        // drew no error, the site lied, and the node would compile clean and abort the run unit when reached: fail
        // the compile instead. The factories' own backstop only sees "no error ANYWHERE", which an unrelated
        // earlier error satisfies; this is the per-statement answer.
        if (!drewError && data.Edition.RefusalsBound != refusalMark)
            data.Edition.Error(DiagnosticCatalog.UnreportedRefusal,
                EditionContext.UnreportedRefusalMessage($"a {s.Start.Text.ToUpperInvariant()} statement"));
        // An UNBUILT operand is the operand-level BoundUnsupported: announced (below) unless the statement drew an
        // error, and always taken off the ledger here so an enclosing statement does not answer for it again.
        string? unbuiltOperand = data.Edition.TakeUnbuiltSince(unbuiltMark);
        // ⛔ THE DEFERRAL ANNOUNCES ITSELF (kb/Work PB236). `BoundUnsupported` was the carrier for three
        // incompatible jobs — a feature WiseOwl COBOL has not built, an ill-formed OPERAND, and an illegal
        // PLACEMENT — and the emitter rendered all three as the same run-time `NotImplemented.Run(...)`. The two
        // user-error jobs have moved to bind-time diagnostics at their own sites; what is left here is job ONE,
        // and it is reported so it can never again be invisible: before this, a program carrying an
        // unimplemented statement on a path the flow skipped compiled clean, ran to normal completion and said
        // NOTHING, at any stage, ever.
        // ⛔ THIS IS THE INVARIANT, AND IT IS HERE BECAUSE HERE IS THE ONLY PLACE IT CANNOT BE FORGOTTEN. Every
        // statement the grammar accepted funnels through this method, which already positions the diagnostic
        // cursor (kb/Work PB82), so a NEW `BoundUnsupported` written anywhere in any verb binder inherits the
        // report by construction — never a per-site habit, which is exactly what produced the asymmetry PB236
        // measured (two of START's five refusals carried a diagnostic and three did not). A WARNING, not an
        // error: the gap is the COMPILER's, so it must not fail a compile the standard gives a meaning to —
        // §1.4's staged-loud posture is unchanged, it merely stopped being silent.
        // ⛔ AND ONLY FOR A STATEMENT NOTHING REFUSED (kb/Work PB909). A site that knows the SOURCE is wrong builds
        // a BoundRejected, which reported its own error; but an operand the resolver returned null for is the one
        // case a site cannot classify — the null is either REPORTED (an undefined name, a subscript on a
        // non-table item) or a shape the resolver has not built — and the error count since this statement began
        // is what tells the two apart. A statement whose bind already drew an error is a refused statement: the
        // compile fails anyway, and announcing a gap in WiseOwl COBOL on top of it (`INSPECT A(1) …` on a non-table
        // item used to draw COBOLNET2096 AND "not implemented") sends the user after the wrong party.
        if (!drewError && (core is BoundUnsupported { Feature: var f } ? f : unbuiltOperand) is { } gap)
            data.Edition.Warning(DiagnosticCatalog.StatementNotImplemented,
                $"{gap} — not implemented; reaching this statement aborts the run unit "
                + "(COBOLNET_DESIGN §1.4)");
        // ⛔ THROUGH Rewrap, NOT AROUND: a multi-operand CLOSE/FREE/INITIALIZE/INITIATE/OPEN/TERMINATE/VALIDATE
        // binds to a BoundImplicitSeries — N separate statements per the seven identically-worded general rules
        // (ISO §14.9.20.4 GR3 and siblings) — and EcWrap distributes the checked wrapper over its members to give
        // each one the statement site a RESUME … NEXT STATEMENT resumes AFTER. A desugar wrap that enclosed the
        // series in a BoundSequence would hide it from EcWrap and silently restore the PB419 defect for every
        // such statement carrying a function-identifier or an object-property reference; Rewrap therefore lands
        // the hoist on the FIRST implicit statement — the one whose operands it evaluates — and leaves the series
        // outermost. A non-series statement goes through untouched.
        core = BoundImplicitSeries.Rewrap(core, n => Udf.UdfWrapCalls(n, marks.PreOps));
        core = BoundImplicitSeries.Rewrap(core, n => Oo.OoWrapPropertyOps(n, marks.PropertyOps));
        // ISO §14.9.33.4 GR2 a) 2./3. — the statement an operand activation was specified in is where a RESUME AT
        // NEXT STATEMENT for the condition it propagates lands. Through Rewrap like the hoist above, so a
        // multi-operand statement's landing is the implicit statement the activation belongs to.
        if (data.OperandEvaluations > 0) core = BoundImplicitSeries.Rewrap(core, n => new BoundActivationSite(n));
        StatementLine = savedLine;
        data.OperandEvaluations = savedActivations;
        return Ec.EcWrap(s, core);
    }

    /// <summary>The source line of the statement now binding — the §7.3.25 line an operand activation's checking
    /// profile is taken at (<c>UdfBinder.DrainPending</c>; kb/Work PB892). Saved and restored around a nested
    /// statement by <see cref="BindStatement"/>.</summary>
    internal int StatementLine { get; private set; }

    /// <summary>How many §14.9.49.3 SR11 violations <see cref="BindStatement"/> has reported — compared across a
    /// statement's bind so an enclosing statement does not report again the store its nested statement reported.</summary>
    private int _controlStoreReports;

    private BoundStatement BindStatementCore(Core.StatementContext s) => s switch
    {
        _ when s.displayStatement() is { } d => Accept.BindDisplay(d),
        _ when s.moveStatement() is { } m => Move.Bind(m),
        _ when s.addStatement() is { } a => Arith.BindAdd(a),
        _ when s.subtractStatement() is { } sub => Arith.BindSubtract(sub),
        _ when s.multiplyStatement() is { } mul => Arith.BindMultiply(mul),
        _ when s.divideStatement() is { } div => Arith.BindDivide(div),
        _ when s.computeStatement() is { } c => Arith.BindCompute(c),
        _ when s.ifStatement() is { } iff => ControlFlow.BindIf(iff),
        _ when s.performStatement() is { } p => ControlFlow.BindPerform(p),
        _ when s.setStatement() is { } set => Set.BindSet(set),
        _ when s.searchStatement() is { } se => Search.BindSearch(se),
        _ when s.evaluateStatement() is { } ev => Evaluate.Bind(ev),
        _ when s.inspectStatement() is { } ins => Inspect.Bind(ins),
        _ when s.searchAllStatement() is { } sa => Search.BindSearchAll(sa),
        _ when s.goToStatement() is { } g => ControlFlow.BindGoTo(g),
        _ when s.alterStatement() is { } al => Alter.BindAlter(al),   // 85-only; rejected ≥2002 inside the pass gate (deleted by ISO/IEC 1989:2002)
        _ when s.exitStatement() is { } e => ControlFlow.BindExit(e),
        _ when s.openStatement() is { } o => SeqIo.BindOpen(o),
        _ when s.closeStatement() is { } c => SeqIo.BindClose(c),
        _ when s.writeStatement() is { } w => SeqIo.BindWrite(w),
        _ when s.readStatement() is { } r => SeqIo.BindRead(r),
        _ when s.rewriteStatement() is { } rw => SeqIo.BindRewrite(rw),
        _ when s.startStatement() is { } st => KeyedIo.BindStart(st),
        _ when s.deleteStatement() is { } del => KeyedIo.BindDelete(del),
        _ when s.deleteFileStatement() is { } dfs => KeyedIo.BindDeleteFile(dfs),
        _ when s.unlockStatement() is { } ul => FileLock.BindUnlock(ul),
        _ when s.stringStatement() is { } sstr => Strings.BindString(sstr),
        _ when s.unstringStatement() is { } suns => Strings.BindUnstring(suns),
        _ when s.acceptStatement() is { } ac => Accept.BindAccept(ac),
        _ when s.initializeStatement() is { } ini => Init.Bind(ini),
        // ── Wave H — recognize-and-name the unsupported facilities. ISO §4.2.6 ¶3 makes the compile-time
        //    warning mechanism MANDATORY for the Annex A.3 halves (MCS, A.3 item 4; COMMIT/ROLLBACK, A.3 items
        //    6-7); the A.4.14 VALIDATE statement is an OPTIONAL element §4.2.7 requires us only to DOCUMENT,
        //    and naming it is the choice docs/CONFORMANCE.md §4 item 3 records (kb/Work PB709). Each binds
        //    to BoundNop: the program compiles, runs, and the facility is inert — never a silent wrong answer,
        //    and never an EC (licensed off by §14.6.13.1.1). ──
        _ when s.mcsReceiveStatement() is not null || s.mcsSendStatement() is not null
            => BindUnsupportedFacility(DiagnosticCatalog.McsFacilityUnsupported),
        _ when s.commitFacilityStatement() is not null || s.rollbackFacilityStatement() is not null
            => BindCommitRollback(s.commitFacilityStatement() is not null),
        _ when s.validateFacilityStatement() is not null
            => BindUnsupportedFacility(DiagnosticCatalog.ValidateFacilityUnsupported),
        _ when s.continueStatement() is { } cont => ControlFlow.BindContinue(cont),
        // NEXT SENTENCE is a PHRASE, not a statement: the grammar parses it as one so `statementBlock` spells the
        // printed `{ statement | NEXT SENTENCE }` braces, and THIS is where its position is decided — admitted only
        // as a whole brace alternative (IsNextSentenceArm), refused everywhere else (kb/Work PB446).
        _ when s.nextSentenceStatement() is not null => IsNextSentenceArm(s.Parent as Core.StatementBlockContext)
            ? new BoundNextSentence(Ctx.SourceLine(s))
            : BoundRejected.Report(data.Edition, DiagnosticCatalog.StatementFormatShape, "NEXT SENTENCE: it is a "
                + "phrase of exactly three general formats — IF Format 2 (§14.9.19.2), as the whole of the THEN or "
                + "ELSE phrase, and both SEARCH formats (§14.9.37.2), as the whole of a WHEN phrase's body — and "
                + "this position is none of them: not an AT END or other conditional phrase, not an inline PERFORM "
                + "or EVALUATE body, not a sentence of its own, and never beside another statement in the same "
                + "phrase. Use CONTINUE, or restructure with a scope terminator"),
        // STOP RUN vs STOP literal (X3.23-1985 Format 2 — communicate to the operator, then CONTINUE): the
        // literal form no longer silently binds as STOP RUN (the DEVLOG-578 mis-bind; edition-gated ≥2002 by
        // the validator, its 85 semantics implemented via BoundStopLiteral).
        _ when s.stopStatement() is { } stop => ControlFlow.BindStop(stop),
        _ when s.gobackStatement() is { } gb => Call.BindGoback(gb),   // §14.9.18 — called-program return; 2002+ gated
        _ when s.invokeStatement() is { } inv => Oo.OoBindInvoke(inv),   // §14.9.23 — OO method invocation (2002+ grammar-gated)
        _ when s.callStatement() is { } call => Call.BindCall(call),
        _ when s.cancelStatement() is { } cancel => Call.BindCancel(cancel),
        // ENTRY: ISO/IEC 1989 defines no ENTRY statement — a vendor extension, which this implementation admits under
        // no dialect (kb/Work PB909; the COBOLNET1941/1970 posture). It used to be a DEFERRAL (a COBOLNET1756
        // warning and a run-time abort), which is a promise to build something the standard does not contain.
        _ when s.entryStatement() is not null => BoundRejected.Report(data.Edition, DiagnosticCatalog.StatementFormatShape, "ENTRY: ISO/IEC 1989 defines no ENTRY statement — a secondary entry point is a vendor extension, and "
            + "this implementation admits none (ISO §4.2.2). Make the entry point a separate program, or a "
            + "nested program, and CALL it"),
        // ENTER language-name [routine-name] (X3.23-1985 Nucleus, deleted by ISO 2002 — 0902-gated ≥2002 by
        // the version-conformance pass, VCR Table 7 row 7.16): comment-equivalent when only COBOL is supported — the
        // conforming '85 posture; accepted-inert as a no-op.
        _ when s.enterStatement() is not null => new BoundNop(),
        // USE (ISO §14.9.49.3 SR1 — kb/Work PB361). The ONE legal position, the first sentence of a declarative
        // section, is consumed by ProcedureTableBuilder.DeclCollectSection and never reaches this binder, so a USE
        // statement bound HERE is misplaced by construction: in a nondeclarative paragraph, in a declarative
        // paragraph, or after the section's first sentence. It used to fall to the bottom arm and be announced as
        // a feature WiseOwl COBOL had not implemented.
        _ when s.useStatement() is not null => BoundRejected.Report(data.Edition, DiagnosticCatalog.UseStatementPlacement,
            "USE: a USE statement shall immediately follow a section header in the declaratives portion of the "
            + "procedure division and shall appear in a sentence by itself (ISO §14.9.49.3 SR1); this one is "
            + (Ctx.Enclosing.InDeclarative
                ? "inside a declarative section, after its first sentence"
                : "in the nondeclarative portion of the procedure division")),
        _ when s.sortStatement() is { } srt => Sort.BindSort(srt),
        _ when s.mergeStatement() is { } mrg => Sort.BindMerge(mrg),
        _ when s.releaseStatement() is { } rls => Sort.BindRelease(rls),
        _ when s.returnStatement() is { } ret => Sort.BindReturn(ret),
        _ when s.initiateStatement() is { } rwi => Rw.BindInitiate(rwi),     // Report Writer (ISO §14.9.21)
        _ when s.generateStatement() is { } rwg => Rw.BindGenerate(rwg),     // Report Writer (ISO §14.9.16)
        _ when s.terminateStatement() is { } rwt => Rw.BindTerminate(rwt),   // Report Writer (ISO §14.9.46)
        _ when s.suppressStatement() is { } rws => Rw.BindSuppress(rws),     // Report Writer (ISO §14.9.45)
        _ when s.raiseStatement() is { } ra => Ec.BindRaise(ra),               // EC model (ISO §14.9.29; 2002+ gated)
        _ when s.resumeStatement() is { } rs => Ec.BindResume(rs),             // EC model (ISO §14.9.33; 2002+ gated)
        _ when s.allocateStatement() is { } al => Ptr.BindAllocate(al),      // dynamic storage (ISO §14.9.3; Phase-4b inc 2)
        _ when s.freeStatement() is { } fr => Ptr.BindFree(fr),              // dynamic storage (ISO §14.9.15; Phase-4b inc 2)
        _ => new BoundUnsupported($"statement '{FirstToken(s)}'"),
    };

    /// <summary>STOP RUN [WITH {NORMAL|ERROR} [STATUS …]] / STOP literal (ISO §14.9.42). The status phrase is a
    /// COBOL-2002 introduction — bind-time introduction gate (rearch bind-time migration Cluster 4; the parse-time
    /// {is2002()}? predicate is gone). The phrase has no runtime effect in this compiler, so the gate is its only
    /// binder obligation.</summary>
    // ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ───────────────────────────────────────────────────────────────

    /// <summary>Bind a RETRY phrase (ISO §14.7.9) — the FORM and, for the two that carry one, its arithmetic
    /// expression. NOTHING about the outcome is decided here: §14.7.9 has no Syntax rules clause at all, so a
    /// non-integer or out-of-range amount is legal source that §14.7.9.3 GR1/GR2/GR4 a) define at RUN time, and
    /// the one place that executes them is <c>FileRegistry.RetryLoop</c> (docs/COBOLNET_FILES_DESIGN.md "D8").
    /// The three forms stay three: GR1's count is a bounded loop, GR2's timeout period is clamped to this
    /// implementation's maximum meaningful value of zero, and GR3's unbounded wait is the only one whose
    /// failure is reported as the §9.1.13.8 item 2 deadlock — collapsing any two of them upstream is the defect
    /// kb/Work PB346 names.</summary>
    internal RetrySpec BindRetry(Core.RetryPhraseContext rp) =>
        rp.FOREVER() is not null ? new RetrySpec(RetryKind.Forever, null)
        : rp.SECONDS() is not null ? new RetrySpec(RetryKind.Seconds, Expr.BindExpr(rp.arithmeticExpression()))
        : new RetrySpec(RetryKind.Times, Expr.BindExpr(rp.arithmeticExpression()));

    internal List<BoundStatement> BindBlocks(IEnumerable<Core.StatementBlockContext> blocks) =>
        blocks.SelectMany(b => b.statement()).Select(BindStatement).ToList();

    /// <summary>⛔ THE ONE READING OF THE PRINTED <c>{ statement | NEXT SENTENCE }</c> BRACES (kb/Work PB446). True
    /// when <paramref name="block"/> is exactly the NEXT SENTENCE alternative of one of the three general formats
    /// that print it — IF Format 2's THEN or ELSE phrase (ISO §14.9.19.2) and a WHEN phrase of either SEARCH
    /// format (§14.9.37.2) — and so the WHOLE of that phrase: a brace offers alternatives, one of which is
    /// written (§5.2.6.3), never both. Read from the parse tree's own shape, so the funnel that binds the
    /// statement and the SEARCH SR4 screen (<c>StatementValidation.CheckSearchEndSearchNextSentence</c>) ask the
    /// same question. Anywhere else the grammar lets NEXT SENTENCE through — an AT END phrase (whose operand is
    /// an imperative-statement), an inline PERFORM or EVALUATE body, a sentence of its own, beside another
    /// statement — no format prints it. ⚠ IF Format 2's arms under an END-IF are admitted here per the PB396
    /// DETERMINATION recorded on <c>ifStatement</c> in CobolControlFlow.g4.</summary>
    internal static bool IsNextSentenceArm(Core.StatementBlockContext? block) =>
        block is { Parent: Core.IfStatementContext or Core.SearchWhenClauseContext or Core.SearchAllWhenClauseContext }
        && block.statement() is [{ } only] && only.nextSentenceStatement() is not null;

    // ── ON SIZE ERROR phrase (ISO §14.7.5) ───────────────────────────────────────────────────────────────────

    internal SizeErrorPhrase? BindSizeError(Core.ArithmeticOnSizeErrorContext? ctx) =>
        ctx is null ? null : BuildSizeError(ctx.statementBlock(), PhraseBlocks.StartsWithNot(ctx));

    internal SizeErrorPhrase? BindSizeError(Core.ComputeOnSizeErrorContext? ctx) =>
        ctx is null ? null : BuildSizeError(ctx.statementBlock(), PhraseBlocks.StartsWithNot(ctx));

    /// <summary>Build the phrase from the (1 or 2) statement blocks — the shared two-branch shape via the ONE
    /// <see cref="PhraseBlocks.Split"/> extractor (P7 Step 10b).</summary>
    private SizeErrorPhrase BuildSizeError(Core.StatementBlockContext[] blocks, bool notFirst)
    {
        var (onErr, notErr) = PhraseBlocks.Split(blocks, notFirst, b => BindBlocks([b]));
        return new SizeErrorPhrase(onErr, notErr);
    }

    // ── Conditions ─────────────────────────────────────────────────────────────────────────────────────────

    internal static IEnumerable<Core.DataReferenceContext> DataRefs(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is Core.DataReferenceContext dref) yield return dref;
            else foreach (var inner in DataRefs(child)) yield return inner;
        }
    }

    internal static IEnumerable<IParseTree> Children(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++) yield return node.GetChild(i);
    }

    private static string FirstToken(IParseTree node) =>
        node.ChildCount > 0 ? node.GetChild(0).GetText() : node.GetText();

    /// <summary>Recognize-and-name an ACCEPT-INERT unsupported facility (§4.2.6 ¶3 for an Annex A.3 element,
    /// §4.2.7 for an Annex A.4 one — the descriptor's own <c>Annex</c> datum says which, and its
    /// <c>PostureClause</c> derives the §; plus §4.2.13 where the facility is also obsolete): name it once
    /// per site and bind to <see cref="BoundNop"/>, so the program still compiles and runs with the facility
    /// inert. The severity decision is NOT here — it is the descriptor's, read by the ONE declined-element
    /// seam <see cref="EditionContext.Declined"/> (feedback_one_mechanism_per_job); this helper only supplies
    /// the statement-shaped half (bind to a no-op), which is why it is reachable solely from
    /// Warning-severity descriptors. A REFUSE-severity decline (COBOLNET1705/1706) calls
    /// <c>Declined</c> directly at its own site and yields <see cref="BoundUnsupported"/>, not a no-op.</summary>
    private BoundStatement BindUnsupportedFacility(DiagnosticDescriptor d)
    {
        Ctx.Edition.Declined(d);
        return new BoundNop();
    }

    /// <summary>COMMIT / ROLLBACK (kb/Work PB137): the §4.2.6 named warning keeps the documented A.3
    /// non-support posture, §14.9.7.3/§14.9.36.3 SR1 rejects the statement in a RECURSIVE source element
    /// at bind (a function and a method are ALWAYS recursive, §8.6.6), and the IDENTITY node lets the
    /// VersionConformancePass enforce SR2's SORT/MERGE procedure ban.</summary>
    private BoundStatement BindCommitRollback(bool isCommit)
    {
        string verb = isCommit ? "COMMIT" : "ROLLBACK";
        string cite = isCommit ? "§14.9.7.3 SR1" : "§14.9.36.3 SR1";
        Ctx.Edition.Declined(DiagnosticCatalog.CommitRollbackUnsupported);
        if (UnitRecursive || InMethod || UdfSelfName is not null)
        {
            return BoundRejected.Report(Ctx.Edition, DiagnosticCatalog.CommitRollbackContext,
                $"{verb} shall not be specified in a recursive source element (ISO {cite}; a function or "
                + "method is always recursive, §8.6.6)");
        }
        return new BoundCommitRollback(isCommit);
    }
}
