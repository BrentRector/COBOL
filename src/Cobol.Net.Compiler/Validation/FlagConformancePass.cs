// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;                // ParserRuleContext — the source-element walk
using CobolNet.Binding;              // FlagState / PictureAnalyzer / EditionContext
using CobolNet.Binding.Model;        // Usage / PicCategory / PicInfo
using CobolNet.Binding.Passes;       // GroupBindContext
using CobolNet.Binding.Procedure;    // ConditionBinder.SoleDataRef (the canonical bare-operand unwrap)
using CobolNet.Editions;             // IDiagnosticSink / EditionDiagnostic / EditionSeverity
using CobolNet.Editions.Diagnostics; // DiagnosticCatalog
using CobolNet.Frontend.Cst;         // DataReferenceCst / SpecialRegister
using CobolNet.Frontend.Generated;   // CobolParserCore / CobolParserCoreBaseVisitor
using CobolNet.Frontend.Preprocessor;// FlagDirective / FlagOption / FlagOptions / FlagDirectiveLine
using CobolNet.Runtime.IO;           // FileOpenMode (the USE-declarative open-mode ordinal, I-O-DECLARATIVE)
using CobolNet.Runtime;

namespace CobolNet.Validation;

/// <summary>
/// The migration-flagging pass (ISO §7.3.14 FLAG-02 / §7.3.15 FLAG-14) — a sibling to
/// <see cref="VersionConformancePass"/>, run right after it in <c>BinderDriver</c>. It emits a Warning for every
/// construct that an active <c>&gt;&gt;FLAG-02</c>/<c>&gt;&gt;FLAG-14</c> option flags (GR1: the implementor SHALL
/// provide the warning mechanism). It is a SEPARATE pass, not a bolt-on, because flagging is an orthogonal axis to
/// edition gating: it fires on the user's DIRECTIVE STATE (a <c>&gt;&gt;FLAG</c> ON at the construct's line),
/// regardless of <c>--std</c>, and is ALWAYS a Warning (never fails the compile).
///
/// It is a PARSE-TREE visitor (the source-line reason, design D2): the flag fold is line-sensitive (GR2 — a flag
/// applies to the text FOLLOWING its directive), and a bound statement carries no uniform source line, whereas
/// every parse node has <c>ctx.Start.Line</c> — anchored to the same final-text lines as the
/// <see cref="FlagEvent"/>s (<see cref="FlagDirectiveProcessor"/> collects on the FINAL text). It reuses the
/// generated ANTLR base visitor (the same traversal mechanism the <see cref="VersionConformancePass"/> parse arm uses).
/// Syntactic options decide from the parse node directly; options needing a resolved fact look it up by name in the
/// models reachable from <see cref="GroupBindContext"/>. Design SSOT: <c>docs/rearchitecture/DESIGN-flag-directives.md</c>.
/// </summary>
internal sealed partial class FlagConformancePass : CursorFollowingVisitor   // the cursor follows the walk (kb/Work PB82)
{
    private readonly FlagState _flag;
    private readonly IDiagnosticSink _sink;
    // Resolved-model lookups keyed by SOURCE NAME (the flag pass runs before the file-connector renaming, so the
    // model still carries source names — see BinderDriver): the WRITE targets (record- and file-names) whose file
    // has a LINAGE clause (FLAG-14 m), and the report-names whose description carries a VARYING clause (FLAG-02 f).
    private readonly IReadOnlySet<string> _linageWriteTargets;
    private readonly IReadOnlySet<string> _varyingReports;
    // FILE-STATUS reference tagging (FLAG-14 e I-O-STATUS-04 / f I-O-STATUS-07): the source names of the data items
    // named in a SELECT … FILE STATUS clause (a relation comparing one to '04'/'07' is the flagged reference), and
    // the level-88 condition-names defined ON such an item whose (singleton) VALUE is '04' / '07' (a reference to
    // that condition-name is likewise "a reference … that tests for '04'/'07'").
    private readonly IReadOnlySet<string> _fileStatusNames;
    private readonly IReadOnlySet<string> _fileStatus88Is04;
    private readonly IReadOnlySet<string> _fileStatus88Is07;
    // The compile-time directive states the state-coupled options read: the >>REF-MOD-ZERO-LENGTH tri-state (i) and
    // the >>TURN EC-checking model (i needs EC-BOUND-REF-MOD; e needs EC-RANGE-INDEX).
    private readonly RefModZeroLengthState _refModZl;
    private readonly TurnState _turn;
    // Per-unit name resolution for the NAME-RESOLVING detectors (d MOVE-TO-SAME-NAME, e RANGE-EXCEPTION-FOR-INDEX):
    // the map from each program unit (outermost AND contained) to its bound model, keyed by the unit's IDENTIFICATION
    // DIVISION node — the one node a nested program shares with the synthetic programUnit context its binder is
    // built over (BinderDriver.Reparent adopts the children) — and the CURRENT unit's data + resolver, set by
    // VisitUnit as the walk enters each unit's subtree so an operand resolves in ITS OWN COBOL name scope (duplicate
    // data-names across programs must not cross-resolve; an index-name is likewise unit-scoped).
    // Null outside any program unit (an OO METHOD body — a documented advisory false-NEGATIVE, never a false-positive:
    // the flag simply does not fire there, which is safe for a migration aid).
    private readonly IReadOnlyDictionary<CobolParserCore.IdentificationDivisionContext, BoundUnit> _unitByIdentification;
    private DataBinder? _currentData;
    private ReferenceResolver? _currentRefs;
    // The current unit's USE-declarative open modes (FLAG-14 d I-O-DECLARATIVE), read from the bound model: whether
    // it has ANY open-mode declarative (USE … ON INPUT/OUTPUT/I-O/EXTEND — the INVALID-KEY rule) and whether it has
    // an INPUT / I-O one (the AT-END rule). Reset per unit in VisitProgramUnit (a nested unit does not inherit).
    private bool _hasAnyModeDecl;
    private bool _hasReadModeDecl;
    // FLAG-02 b EC-PROGRAM-EXCEPTIONS (§7.3.14.4 GR4 b): the SOURCE ELEMENTS of the compilation group (§3.164 — a source
    // unit excluding any contained source units: a program, a function, a contained program, a class, an interface and
    // a method definition each), in source order, each recording whether its own text calls a function or invokes a
    // method. A >>TURN directive for an EC-PROGRAM-family exception is flagged (post-walk) when the source element it
    // belongs to calls or invokes. _activationSites is the BINDER's record of the activations that carry no syntactic
    // marker (DataBinder.ActivationSites).
    private readonly List<SourceElement> _elements = [];
    private SourceElement? _currentElement;
    private readonly IReadOnlySet<object> _activationSites;

    /// <summary>One source element's own line span (its contained source units' text is inside it but not part of it —
    /// the innermost element containing a line owns that line) and whether it calls or invokes.</summary>
    private sealed class SourceElement(int firstLine, int lastLine)
    {
        public int FirstLine { get; } = firstLine;
        public int LastLine { get; } = lastLine;
        public bool CallsOrInvokes { get; set; }
    }

    /// <summary>The EC-PROGRAM-family exception-names whose <c>&gt;&gt;TURN</c> directive FLAG-02 b flags
    /// (§7.3.14.4 GR4 b) — EC-ALL plus the three EC-PROGRAM level-3 names.</summary>
    private static readonly IReadOnlySet<string> EcProgramFamily =
        new HashSet<string>(CobolNames.Comparer)
        { "EC-ALL", "EC-PROGRAM", "EC-PROGRAM-ARG-OMITTED", "EC-PROGRAM-NOT-FOUND" };
    // A discard EditionContext so the reused PictureAnalyzer (the ONE picture-category mechanism) can classify a
    // parse-tree PICTURE string WITHOUT re-emitting its bind-time diagnostics to the real sink. At 2023 (the
    // superset) so no legal symbol is spuriously rejected; the picture was already validated during binding.
    private readonly EditionContext _discard = new(2023);

    private FlagConformancePass(FlagState flag, IDiagnosticSink sink,
        IReadOnlySet<string> linageWriteTargets, IReadOnlySet<string> varyingReports,
        IReadOnlySet<string> fileStatusNames, IReadOnlySet<string> fileStatus88Is04, IReadOnlySet<string> fileStatus88Is07,
        RefModZeroLengthState refModZl, TurnState turn,
        IReadOnlyDictionary<CobolParserCore.IdentificationDivisionContext, BoundUnit> unitByIdentification,
        IReadOnlySet<object> activationSites) : base(sink)
    {
        _flag = flag;
        _sink = sink;
        _linageWriteTargets = linageWriteTargets;
        _varyingReports = varyingReports;
        _fileStatusNames = fileStatusNames;
        _fileStatus88Is04 = fileStatus88Is04;
        _fileStatus88Is07 = fileStatus88Is07;
        _refModZl = refModZl;
        _turn = turn;
        _unitByIdentification = unitByIdentification;
        _activationSites = activationSites;
    }

    /// <summary>Flag every construct an active FLAG option covers. A no-op (no walk) when no FLAG directive is
    /// present — the zero-overhead invariant: a source with no <c>&gt;&gt;FLAG</c> line is byte-identical to a
    /// build without this pass.</summary>
    public static void Run(GroupBindContext group, FlagState flag, IDiagnosticSink sink)
    {
        if (!flag.Any) return;

        // Build the two source-name lookups the statement detectors need (m WRITE-END-OF-PAGE / f
        // TERMINATE-WITH-VARYING). Files/reports live in program units (not OO class data), so units suffice.
        var linage = new HashSet<string>(CobolNames.Comparer);
        var varying = new HashSet<string>(CobolNames.Comparer);
        // FILE-STATUS reference tagging (I-O-STATUS-04/07): the FILE STATUS data-item names, and the level-88
        // condition-names on such an item whose singleton VALUE is '04' / '07'.
        var fileStatus = new HashSet<string>(CobolNames.Comparer);
        var fs88_04 = new HashSet<string>(CobolNames.Comparer);
        var fs88_07 = new HashSet<string>(CobolNames.Comparer);
        foreach (var unit in group.Units)
        {
            foreach (var file in unit.Data.Files)
            {
                if (file.Linage is not null)
                {
                    linage.Add(file.CobolName);
                    foreach (var rec in file.Records)
                        if (rec.CobolName is { } rn) linage.Add(rn);
                }
                if (file.FileStatusItem is { } fs)
                {
                    if (fs.CobolName is { } fsn) fileStatus.Add(fsn);
                    foreach (var c in fs.Own88s)
                        foreach (var v in c.Values)
                            if (v.High is null)   // a singleton VALUE tests for exactly that status
                                switch (StripLiteral(v.Low))
                                {
                                    case "04": fs88_04.Add(c.Name); break;
                                    case "07": fs88_07.Add(c.Name); break;
                                }
                }
            }
            foreach (var report in unit.Data.Reports)
                if (report.HasVarying)
                    varying.Add(report.Name);
        }

        // Map each program unit (outermost AND contained) to its bound model so VisitUnit can select the current
        // unit's data + resolver for the name-resolving detectors (d/e). OO class method bodies have no entry —
        // their MOVE/SET operands are not resolved (the documented advisory edge; _current* stays null there).
        var unitByIdentification = new Dictionary<CobolParserCore.IdentificationDivisionContext, BoundUnit>();
        foreach (var unit in group.Units)
            if (unit.Ctx.identificationDivision() is { } identification) unitByIdentification[identification] = unit;

        // The binder's record of the function activations and property references that look like data references
        // (DataBinder.ActivationSites) — every forest of the group, class and program alike.
        var activationSites = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var binder in group.AllBindersAndInterfaces()) activationSites.UnionWith(binder.ActivationSites);

        var pass = new FlagConformancePass(flag, sink, linage, varying, fileStatus, fs88_04, fs88_07,
            group.Session.RefModZeroLength, group.Session.Turn, unitByIdentification, activationSites);
        pass.VisitPositioned(group.Tree);
        pass.FlagEcProgramDirectives();   // b EC-PROGRAM-EXCEPTIONS — cross-ref >>TURN lines with the source elements that call/invoke
    }

    /// <summary>FLAG-02 b EC-PROGRAM-EXCEPTIONS (§7.3.14.4 GR4 b) — flag EACH <c>&gt;&gt;TURN</c> directive that names an
    /// EC-PROGRAM-family exception when the source element it belongs to calls a function or invokes a method. Runs
    /// after the walk: the directive lines come from <see cref="TurnState"/> (a frontend event, not a parse node), and
    /// the elements' call facts were collected during the walk. Each warning is positioned AT its directive's line, so
    /// two qualifying directives in one element are two located warnings, never one that names neither.</summary>
    private void FlagEcProgramDirectives()
    {
        foreach (int line in _turn.DirectiveLinesNaming(EcProgramFamily).Distinct())
            if (SourceElementOf(line) is { CallsOrInvokes: true })
                FlagAtLine(FlagOption.Flag02EcProgramExceptions, line,
                    "the >>TURN for an EC-PROGRAM-family exception in a source element that calls a function or invokes a method");
    }

    /// <summary>The source element a directive written on <paramref name="line"/> belongs to: the INNERMOST element whose
    /// span contains the line (a contained program, a method, … is its own source element — §3.164 excludes it from
    /// its container — so its call/invoke does not count for a containing element's directive, and vice versa); a
    /// directive written BEFORE an element's IDENTIFICATION DIVISION (it is in no element's span yet) belongs to the
    /// element that follows it, the one whose text it governs (§7.3.25.4 GR6: a TURN governs the statements that follow
    /// it) — the earliest-starting element after the line, the OUTERMOST when a container and its first contained
    /// element start together (they are recorded outermost first). Null after the last element.</summary>
    private SourceElement? SourceElementOf(int line)
    {
        SourceElement? inner = null, next = null;
        foreach (var e in _elements)
        {
            if (e.FirstLine <= line && line <= e.LastLine)
            {
                if (inner is null || e.LastLine - e.FirstLine <= inner.LastLine - inner.FirstLine) inner = e;
            }
            else if (e.FirstLine > line && next is null)
                next = e;
        }
        return inner ?? next;
    }

    // ── The source-element walk (§3.164). Every definition that is a source unit opens an element; a program unit
    //    (outermost, contained, or a function) also selects its name-resolution scope. ──

    public override object? VisitProgramUnit(CobolParserCore.ProgramUnitContext ctx) => VisitUnit(ctx, ctx.identificationDivision());

    public override object? VisitNestedProgram(CobolParserCore.NestedProgramContext ctx) => VisitUnit(ctx, ctx.identificationDivision());

    public override object? VisitClassDefinition(CobolParserCore.ClassDefinitionContext ctx) => VisitElement(ctx);

    public override object? VisitInterfaceDefinition(CobolParserCore.InterfaceDefinitionContext ctx) => VisitElement(ctx);

    public override object? VisitMethodDefinition(CobolParserCore.MethodDefinitionContext ctx) => VisitElement(ctx);

    /// <summary>Open the source element <paramref name="ctx"/> for the walk of its subtree and close it on exit.</summary>
    private object? VisitElement(ParserRuleContext ctx)
    {
        var element = new SourceElement(ctx.Start.Line, ctx.Stop?.Line ?? ctx.Start.Line);
        _elements.Add(element);
        var enclosing = _currentElement;
        _currentElement = element;
        try { return base.VisitChildren(ctx); }
        finally { _currentElement = enclosing; }
    }

    /// <summary>Select the current program unit's data + resolver for the walk of its subtree (the name-resolving
    /// detectors d/e resolve operands in THIS unit's COBOL name scope), with save/restore so a nested program
    /// restores its container's scope on exit.</summary>
    private object? VisitUnit(ParserRuleContext ctx, CobolParserCore.IdentificationDivisionContext? identification)
    {
        var saved = (_currentData, _currentRefs, _hasAnyModeDecl, _hasReadModeDecl);
        if (identification is not null && _unitByIdentification.TryGetValue(identification, out var unit))
        {
            _currentData = unit.Data;
            _currentRefs = unit.Refs;
            // The unit's Format-1 open-mode USE declaratives (BoundDeclarative.ModeIndex = the FileOpenMode ordinal;
            // null for a file-name-targeted or Format-3/4 declarative — which GR4 d does not consider).
            var modes = unit.Bound?.Declaratives;
            _hasAnyModeDecl = modes is not null && modes.Any(d => d.ModeIndex is not null);
            _hasReadModeDecl = modes is not null && modes.Any(d =>
                d.ModeIndex == (int)FileOpenMode.Input || d.ModeIndex == (int)FileOpenMode.IO);
        }
        try { return VisitElement(ctx); }
        finally { (_currentData, _currentRefs, _hasAnyModeDecl, _hasReadModeDecl) = saved; }
    }

    // ── FLAG-02 b EC-PROGRAM-EXCEPTIONS (§7.3.14.4 GR4 b) — record that the current source element calls a function
    //    (a FUNCTION activation, a keyword-omitted function-identifier §8.4.3.2.3 SR2) or invokes a method (INVOKE,
    //    the §8.4.3.4 inline method invocation, or a §8.4.3.9 property reference, whose accessor is a method). The
    //    >>TURN directives are flagged post-walk (FlagEcProgramDirectives), keyed on the source element they belong to. ──
    public override object? VisitFunctionCall(CobolParserCore.FunctionCallContext ctx)
    {
        NoteActivation();
        return base.VisitChildren(ctx);
    }

    public override object? VisitInvokeStatement(CobolParserCore.InvokeStatementContext ctx)
    {
        NoteActivation();
        return base.VisitChildren(ctx);
    }

    public override object? VisitInlineMethodInvocation(CobolParserCore.InlineMethodInvocationContext ctx)
    {
        NoteActivation();
        return base.VisitChildren(ctx);
    }

    /// <summary>A data reference the BINDER resolved to a keyword-omitted function-identifier or a property reference
    /// (<see cref="DataBinder.ActivationSites"/>): the two activations whose text looks like a data reference.</summary>
    public override object? VisitDataReference(CobolParserCore.DataReferenceContext ctx)
    {
        if (IsActivation(ctx)) NoteActivation();
        return base.VisitChildren(ctx);
    }

    private void NoteActivation()
    {
        if (_currentElement is not null) _currentElement.CallsOrInvokes = true;
    }

    /// <summary>Emit the option's Warning if it is flagging at <paramref name="line"/>, positioned where the sink's
    /// cursor stands (the walk follows the construct). The Code is per-directive; the ConstructId (suppress-key),
    /// Message, and Citation are per-option (spec-faithful — each names its own GR4 sub-rule + Annex-E item).</summary>
    private void Flag(FlagOption option, int line, string where)
    {
        if (!_flag.IsOnAt(line, option)) return;
        var info = FlagOptions.Info(option);
        bool f02 = info.Directive == FlagDirective.Flag02;
        string code = (f02 ? DiagnosticCatalog.Flag02Warning : DiagnosticCatalog.Flag14Warning).Code;
        string constructId = $"flag-{(f02 ? "02" : "14")}-{info.Word.ToLowerInvariant()}";
        _sink.Report(new EditionDiagnostic(code, EditionSeverity.Warning, constructId,
            $"{info.Change} — flagged by >>{FlagDirectiveLine.DirectiveWord(info.Directive)} {info.Word}",
            where, info.Citation));
    }

    /// <summary><see cref="Flag"/> for a construct that is NOT a parse node of the walk — a compiler directive (the
    /// <c>&gt;&gt;TURN</c> of FLAG-02 b), known only by its resultant-text line: the warning is positioned AT that line
    /// (column 0, the directive's own start — the form <c>TurnState.Build</c> positions its directive diagnostics
    /// in). Without a position the warning carried no location and two identical unlocated warnings collapsed into one
    /// (kb/Work PB1376).</summary>
    private void FlagAtLine(FlagOption option, int line, string where)
    {
        using var _ = _sink.At(line, 0);
        Flag(option, line, where);
    }

    // ── FLAG-14 h READ-PREVIOUS (§7.3.15.4 GR4 h) — a READ … PREVIOUS (sequential or keyed; the parse rule is
    //    common). Purely syntactic. ──
    public override object? VisitReadStatement(CobolParserCore.ReadStatementContext ctx)
    {
        if (ctx.readDirection()?.PREVIOUS() is not null)
            Flag(FlagOption.Flag14ReadPrevious, ctx.Start.Line, "the READ PREVIOUS statement");

        // d I-O-DECLARATIVE — a READ retrieving SEQUENTIALLY (a NEXT/PREVIOUS direction, or the file's access mode is
        // sequential) is AT-END-capable: flag it lacking AT END when an INPUT/I-O declarative is present. Otherwise
        // it is a random (keyed) read → INVALID-KEY-capable: flag it lacking INVALID KEY when any open-mode
        // declarative is present. A file that does not resolve stays unflagged (no organization ⇒ neither branch fires).
        var file = FileByName(ctx.fileName()?.GetText());
        bool sequential = ctx.readDirection() is not null || file?.AccessMode == FileAccessMode.Sequential;
        if (sequential)
        {
            if (ctx.readAtEnd() is null && _hasReadModeDecl)
                FlagIoDeclarative(ctx.Start.Line, "a READ without an AT END phrase");
        }
        else
            IoDeclarativeInvalidKey(InvalidKeyVerb.Read, ctx.readInvalidKey() is not null, file, ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    // ── FLAG-02 c I-O-STATUS-07 (§7.3.14.4 GR4 c) — a CLOSE specifying WITH NO REWIND or the REEL/UNIT phrase
    //    (§14.9.6.3 SR2 makes REEL and UNIT one phrase). Purely syntactic; one flag per CLOSE statement. ──
    public override object? VisitCloseStatement(CobolParserCore.CloseStatementContext ctx)
    {
        foreach (var phrase in ctx.closeFilePhrase())
        {
            var opt = phrase.closeOption();
            if (opt is null) continue;
            // §7.3.14.4 GR4 c names "the WITH NO REWIND phrase or the UNIT phrase" — and §14.9.6.3 SR2 makes
            // REEL the SAME phrase as UNIT ("The words REEL and UNIT are equivalent"), so a CLOSE written with
            // REEL specifies the UNIT phrase and flags identically. The old predicate deliberately excluded
            // REEL on a misreading of GR4 c — a second consumer of one parse node breaking a spelling
            // equivalence the semantic consumer kept (kb/Work PB141).
            if (opt.REEL() is not null || opt.UNIT() is not null || (opt.NO() is not null && opt.REWIND() is not null))
            {
                Flag(FlagOption.Flag02IoStatus07, ctx.Start.Line, "the CLOSE WITH NO REWIND / UNIT statement");
                break;   // GR4 c flags the CLOSE statement once, however many such phrases it carries
            }
        }
        return base.VisitChildren(ctx);
    }

    // ── FLAG-14 i REF-MOD-ZERO-LENGTH (§7.3.15.4 GR4 i; E.2 item 23) — a reference modification flagged ONLY when
    //    the >>REF-MOD-ZERO-LENGTH directive is UNSPECIFIED (neither explicit ON nor OFF) at the site AND
    //    EC-BOUND-REF-MOD checking is on there (a zero-length result would then raise the exception). Every ref-mod
    //    is ONE parse node, `refModSpec` (kb/Work PB2113 retired the second, a SUBSCRIPT-mode group with a colon).
    //    GR4 i flags "a reference modification of a data-item" — the reference modification of a FUNCTION's result
    //    (`FUNCTION UPPER-CASE (X) (1:2)`, or the keyword-omitted form) is not one. ──
    public override object? VisitRefModSpec(CobolParserCore.RefModSpecContext ctx)
    {
        if (!ModifiesAFunctionResult(ctx)) FlagRefMod(ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    /// <summary>Whether the reference modification <paramref name="refMod"/> modifies the RESULT of a function rather
    /// than a data item: its host — the nearest enclosing function call, or data reference — is a FUNCTION activation (an
    /// explicit <c>functionCall</c>, or a data reference the binder resolved to a keyword-omitted function,
    /// <see cref="DataBinder.ActivationSites"/>).</summary>
    private bool ModifiesAFunctionResult(ParserRuleContext refMod)
    {
        for (var host = refMod.Parent; host is not null; host = host.Parent)
            switch (host)
            {
                case CobolParserCore.FunctionCallContext: return true;
                case CobolParserCore.DataReferenceContext dref: return IsActivation(dref);
            }
        return false;
    }

    /// <summary>Whether the binder resolved <paramref name="dref"/> to a keyword-omitted function activation: the
    /// reference itself, or — for one written inside a subscript, which the subscript renderer binds from its tokens —
    /// its head token (ReferenceResolver.IsFunctionBearing records that).</summary>
    private bool IsActivation(CobolParserCore.DataReferenceContext dref)
        => _activationSites.Contains(dref) || _activationSites.Contains(dref.Start);

    private void FlagRefMod(int line)
    {
        if (_refModZl.IsUnspecifiedAt(line) && _turn.Enabled("EC-BOUND-REF-MOD", null, line))
            Flag(FlagOption.Flag14RefModZeroLength, line,
                "the reference modification (>>REF-MOD-ZERO-LENGTH unspecified and EC-BOUND-REF-MOD checking on)");
    }

    // ── The VALUE-clause data options — anchored at the VALUE clause's source line (the flaggable syntax). k
    //    reaches ANY real data item; g/l/j reach numeric-edited items (a PICTURE-string property, §13.18.40, via the
    //    ONE PictureAnalyzer). FILLER-safe (no name lookup). ──
    public override object? VisitDataDescriptionEntry(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        var (picture, value, usage, blankWhenZero) = Clauses(ctx);
        if (value is not null)
        {
            int line = value.Start.Line;
            var fig = FirstDescendant<CobolParserCore.FigurativeConstantContext>(value);

            // k VALUE-FIG-CON-LENGTH (§7.3.15.4 GR4 k; E.2 item 11) — a figurative constant VALUE on a data item
            // with NO SPECIFIED LENGTH: no PICTURE, no length-implying USAGE (DISPLAY/absent gives none without a
            // PICTURE; COMP-*/INDEX/POINTER/… imply one), and NOT a group (a group's figurative VALUE is filled to
            // the subordinates' length, §13.18.63 SR13). Applies to any real data item (levels 1-49, 77); a
            // level-88 condition-name reaches here via valueClause and is excluded.
            if (fig is not null && picture is null && UsageGivesNoLength(usage)
                && IsRealDataLevel(ctx) && !HasSubordinates(ctx))
                Flag(FlagOption.Flag14ValueFigConLength, line,
                    "a figurative constant in the VALUE clause of a data item with no specified length");

            // g/l/j — numeric-edited items only.
            if (picture is not null && IsNumericEditedPicture(picture, blankWhenZero))
            {
                if (fig is not null)
                {
                    // g NUM-ED-ZERO-FIGCONST + l VALUE-ZERO — the figurative constant ZERO (ZERO/ZEROS/ZEROES, with
                    // or without ALL). One condition, two independently-toggled options.
                    if (fig.zeroWord() is not null)
                    {
                        Flag(FlagOption.Flag14NumEdZeroFigconst, line, "the figurative constant ZERO in the VALUE clause of a numeric-edited item");
                        Flag(FlagOption.Flag14ValueZero, line, "the figurative constant ZERO in the VALUE clause of a numeric-edited item");
                    }
                }
                else if (LiteralHasNoEditingSymbols(value))
                {
                    // j VALUE-EDITING — the VALUE is a LITERAL (numeric or nonnumeric, NOT a figurative constant)
                    // carrying no editing symbols. §13.18.63 SR6/SR11 + E.2 item 29: at 2023 editing is auto-supplied
                    // for a numeric literal and compulsory for an alphanumeric/national literal (both changed 2014→2023).
                    Flag(FlagOption.Flag14ValueEditing, line, "a numeric-edited VALUE literal that contains no editing symbols");
                }
            }
        }
        return base.VisitChildren(ctx);
    }

    // ── FLAG-14 b COMPILE-TIME-ARITHMETIC-EXPRESSIONS (§7.3.15.4 GR4 b) at the CONSTANT entry — arithmetic-expression-1
    //    of `CONSTANT AS …` is a compile-time arithmetic expression (§7.3.6.1; "may be specified in the DEFINE and
    //    EVALUATE directives, in a constant conditional expression, and in a constant entry"). The directive operands
    //    are flagged at the conditional-compilation stage; this is the one site that stage never sees. Both ask
    //    FlagOptions.IsFlaggableCompileTimeArithmetic. ──
    public override object? VisitConstantEntryBody(CobolParserCore.ConstantEntryBodyContext ctx)
    {
        if (ctx.constantValue()?.arithmeticExpression() is { } expression
            && FlagOptions.IsFlaggableCompileTimeArithmetic(expression))
            Flag(FlagOption.Flag14CompileTimeArithmeticExpressions, ctx.Start.Line,
                "the arithmetic expression of a CONSTANT entry");
        return base.VisitChildren(ctx);
    }

    // ── FLAG-14 m WRITE-END-OF-PAGE (§7.3.15.4 GR4 m) — a WRITE that ALLOWS an END-OF-PAGE phrase (its file has a
    //    LINAGE clause, §14.9.51) but does not specify it. "The END-OF-PAGE phrase" is the positive AT END-OF-PAGE
    //    phrase: §14.9.51.3 SR19 names it and the NOT END-OF-PAGE phrase separately, so a WRITE written with only the
    //    NOT phrase does not specify it. The "allows EOP" fact is the file's LINAGE, resolved by name from the model;
    //    anchored at the WRITE. ──
    public override object? VisitWriteStatement(CobolParserCore.WriteStatementContext ctx)
    {
        if (ctx.writeAtEndOfPage() is not { } eop
            || !PhraseBlocks.HasOnBranch(eop.statementBlock(), PhraseBlocks.StartsWithNot(eop)))
        {
            // recordName (a dataReference — unqualified for a WRITE record in practice; a qualified record name is a
            // rare false-negative for this advisory flag) or the FILE fileName form.
            string? target = ctx.recordName()?.GetText() ?? ctx.fileName()?.GetText();
            if (target is not null && _linageWriteTargets.Contains(target))
                Flag(FlagOption.Flag14WriteEndOfPage, ctx.Start.Line,
                    "the WRITE without an END-OF-PAGE phrase (the file has a LINAGE clause)");
        }
        // d I-O-DECLARATIVE — a WRITE to a keyed file (INVALID-KEY-capable) without an INVALID KEY phrase.
        IoDeclarativeInvalidKey(InvalidKeyVerb.Write, ctx.writeInvalidKey() is not null,
            FileByRecordOrName(ctx.recordName()?.GetText(), ctx.fileName()?.GetText()), ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    // ── FLAG-14 d I-O-DECLARATIVE (§7.3.15.4 GR4 d; E.2 item 19) — the remaining INVALID-KEY-capable statements
    //    (REWRITE / DELETE / START on a keyed file). Each is flagged, lacking its INVALID KEY phrase, when the unit
    //    has an INPUT/OUTPUT/I-O/EXTEND USE declarative (which now executes on the exception at 2023). ──
    public override object? VisitRewriteStatement(CobolParserCore.RewriteStatementContext ctx)
    {
        IoDeclarativeInvalidKey(InvalidKeyVerb.Rewrite, ctx.rewriteInvalidKeyPhrase() is not null,
            FileByRecordOrName(ctx.recordName()?.GetText(), ctx.fileName()?.GetText()), ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    public override object? VisitDeleteStatement(CobolParserCore.DeleteStatementContext ctx)
    {
        IoDeclarativeInvalidKey(InvalidKeyVerb.Delete, ctx.deleteInvalidKeyPhrase() is not null,
            FileByName(ctx.fileName()?.GetText()), ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    public override object? VisitStartStatement(CobolParserCore.StartStatementContext ctx)
    {
        IoDeclarativeInvalidKey(InvalidKeyVerb.Start, ctx.startInvalidKeyPhrase() is not null,
            FileByName(ctx.fileName()?.GetText()), ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    /// <summary>Rule 1 of GR4 d — flag a statement that CAN be specified with an INVALID KEY phrase
    /// (<see cref="AdmitsInvalidKey"/>) but lacks it, when the unit has ANY open-mode USE declarative.</summary>
    private void IoDeclarativeInvalidKey(InvalidKeyVerb verb, bool phrasePresent, FileModel? file, int line)
    {
        if (!phrasePresent && _hasAnyModeDecl && AdmitsInvalidKey(verb, file))
            FlagIoDeclarative(line, "an I-O statement without an INVALID KEY phrase");
    }

    /// <summary>The statements GR4 d reads for an INVALID KEY phrase.</summary>
    private enum InvalidKeyVerb { Read, Write, Rewrite, Delete, Start }

    private void FlagIoDeclarative(int line, string what)
        => Flag(FlagOption.Flag14IoDeclarative, line,
            $"{what} while an INPUT/OUTPUT/I-O/EXTEND USE declarative is in effect (it now executes on the exception)");

    /// <summary>Whether <paramref name="verb"/> on <paramref name="file"/> "can be specified with an INVALID KEY phrase"
    /// (§7.3.15.4 GR4 d) — the syntax rules of each statement decide, not the file's organization alone. A file with
    /// SEQUENTIAL / LINE SEQUENTIAL organization never raises an invalid-key condition (§12.4.5.10 — only a RELATIVE or
    /// INDEXED, i.e. keyed, file does); then WRITE takes the phrase for every keyed file (§14.9.51.3 SR3: format 2),
    /// START and a random READ likewise, but REWRITE shall not specify it "for a file with sequential organization or
    /// a file with relative organization and sequential access mode" (§14.9.35.3 SR2) and DELETE RECORD shall not
    /// when it "references a file that is in sequential access mode" (§14.9.10.3 SR2). Flagging a statement the
    /// phrase cannot be written on is a false warning no source change can silence.</summary>
    private static bool AdmitsInvalidKey(InvalidKeyVerb verb, FileModel? file)
        => file is { Organization: FileOrganization.Relative or FileOrganization.Indexed } && verb switch
        {
            InvalidKeyVerb.Rewrite => !(file.Organization == FileOrganization.Relative && file.AccessMode == FileAccessMode.Sequential),
            InvalidKeyVerb.Delete => file.AccessMode != FileAccessMode.Sequential,
            _ => true,
        };

    /// <summary>The current unit's <see cref="FileModel"/> named <paramref name="name"/> (source name; the flag pass
    /// runs before file-connector renaming), or null.</summary>
    private FileModel? FileByName(string? name)
        => name is null || _currentData is null ? null
            : _currentData.Files.FirstOrDefault(f => CobolNames.Same(f.CobolName, name));

    /// <summary>The <see cref="FileModel"/> a WRITE/REWRITE targets: the explicit <c>FILE file-name</c> when written,
    /// else the file whose record descriptions include <paramref name="recordName"/>.</summary>
    private FileModel? FileByRecordOrName(string? recordName, string? fileName)
    {
        if (fileName is not null) return FileByName(fileName);
        if (recordName is null || _currentData is null) return null;
        return _currentData.Files.FirstOrDefault(f =>
            f.Records.Any(r => CobolNames.Same(r.CobolName, recordName)));
    }

    // ── FLAG-02 f TERMINATE-WITH-VARYING (§7.3.14.4 GR4 f) — a TERMINATE of a report whose description contains a
    //    VARYING clause (§13.18.64). Flagged once per TERMINATE when any named report carries a VARYING. ──
    public override object? VisitTerminateStatement(CobolParserCore.TerminateStatementContext ctx)
    {
        foreach (var report in ctx.reportName())
            if (_varyingReports.Contains(report.GetText()))
            {
                Flag(FlagOption.Flag02TerminateWithVarying, ctx.Start.Line,
                    "the TERMINATE of a report whose description contains a VARYING clause");
                break;
            }
        return base.VisitChildren(ctx);
    }

    // ── FLAG-02 d MOVE-TO-SAME-NAME (§7.3.14.4 GR4 d) — a MOVE whose sending and a receiving operand are described
    //    by the SAME data description entry (symbol identity: both resolve to the one <see cref="DataItem"/>, so
    //    differing subscripts / ref-mod of one item still count — §14.9.39 "same data description entry"), when that
    //    DDE is (1) of category alphanumeric-edited — the category of the OPERANDS, so a reference-modified operand
    //    (alphanumeric, §8.4.3.3.4 GR6 a) does not meet it — or (2) has a subordinate OCCURS…DEPENDING whose DEPENDING
    //    item is subordinate to it. Resolved in the CURRENT unit's scope (VisitUnit). ──
    public override object? VisitMoveStatement(CobolParserCore.MoveStatementContext ctx)
    {
        var (send, recvs) = MoveOperands(ctx);
        if (send is not null && ResolveItem(send) is { } sendItem)
            foreach (var recv in recvs)
                if (ResolveItem(recv) is { } recvItem && ReferenceEquals(sendItem, recvItem)
                    && MoveToSameNameFlaggable(sendItem, operandsRefModified: IsRefModified(send) || IsRefModified(recv)))
                {
                    Flag(FlagOption.Flag02MoveToSameName, ctx.Start.Line,
                        "the MOVE whose sending and receiving operands are the same data description entry");
                    break;   // GR4 d flags the MOVE once, however many receivers share the sender's DDE
                }
        return base.VisitChildren(ctx);
    }

    /// <summary>The MOVE's sending operand (null when it is a literal / function activation — never the same DDE as
    /// a receiver) and its receiving operands, across BOTH forms: <c>MOVE CORRESPONDING a TO b</c> (the CORR keyword
    /// sits on the statement) and the plain <c>MOVE a TO b…</c> (one or more receivers).</summary>
    private static (CobolParserCore.DataReferenceContext? Send, IReadOnlyList<CobolParserCore.DataReferenceContext> Recvs)
        MoveOperands(CobolParserCore.MoveStatementContext ctx)
    {
        if (ctx.CORRESPONDING() is not null || ctx.CORR() is not null)   // MOVE CORRESPONDING dref TO dref
        {
            var d = ctx.dataReference();
            return d.Length >= 2 ? (d[0], [d[1]]) : (null, []);
        }
        var send = ctx.sendingOperand()?.dataReference();
        IReadOnlyList<CobolParserCore.DataReferenceContext> recvs =
            ctx.moveReceivingPhrase()?.dataReferenceList()?.dataReference() ?? [];
        return (send, recvs);
    }

    /// <summary>Resolve a MOVE operand's <c>dataReference</c> to its <see cref="DataItem"/> in the current unit's
    /// scope — the DESCRIPTION ENTRY the operand names (subscripts / ref-mod ignored; those select an occurrence /
    /// span of the same DDE). Null for a special register, a name-less reference, or outside a resolvable unit.</summary>
    private DataItem? ResolveItem(CobolParserCore.DataReferenceContext dref)
    {
        if (_currentRefs is null) return null;
        DataReferenceCst r = dref;
        if (r.Register != SpecialRegister.None || r.BaseName is not { } name) return null;
        var quals = new List<string>();
        foreach (var suffix in dref.dataReferenceSuffix())
            if (suffix.qualification()?.cobolWord()?.GetText() is { } q) quals.Add(q);
        return _currentRefs.FindItem(name, quals);
    }

    /// <summary>Whether a same-DDE MOVE operand triggers GR4 d: (1) the OPERANDS are category alphanumeric-edited (the
    /// ONE established test — <see cref="PicCategory.Alphanumeric"/> storage carrying an edit mask, §13.18.40 — on an
    /// operand that is not reference-modified: for a reference-modified unique data item "the category
    /// alphanumeric-edited is considered class and category alphanumeric", §8.4.3.3.4 GR6 a, so the operand's category
    /// is not the DDE's), or (2) the DDE includes a subordinate OCCURS…DEPENDING clause whose DEPENDING item is
    /// subordinate to it (§13.18.38 — a group moved to itself whose length depends on a count inside the moved region;
    /// a property of the entry, which a reference modification does not change).
    /// <para>⛔ NATIONAL-EDITED IS DELIBERATELY ABSENT, and it is not the PB492 oversight it looks like: §7.3.14.4
    /// GR4 d) 1. names "category alphanumeric-edited" and stops, while §14.9.25.4 GR6 b) 1. — the rule about the
    /// same overlap — names "alphanumeric-edited or national-edited". The FLAG is narrower than the undefined
    /// behaviour it flags, in the printed standard, so this reads <see cref="PicInfo.EditMask"/> beside the
    /// ALPHANUMERIC category rather than <c>PicInfo.IsCharacterEdited</c>. Widening it would flag conforming
    /// source under a directive whose own rule does not cover it.</para></summary>
    private static bool MoveToSameNameFlaggable(DataItem item, bool operandsRefModified)
        => (!operandsRefModified && item.Pic is { Category: PicCategory.Alphanumeric, EditMask: not null })
        || (OdoModel.TableUnder(item) is { OccursSpec.Depending: { } dep } && OdoModel.IsWithin(dep, item));

    /// <summary>Whether the reference carries a reference modification (§8.4.3.3): a <c>refModPart</c> suffix, on the
    /// base word or on a qualifier.</summary>
    private static bool IsRefModified(CobolParserCore.DataReferenceContext dref)
        => dref.dataReferenceSuffix().Any(s => s.refModPart() is not null
            || (s.qualification() is { } q && q.refModPart().Length > 0));   // `X OF G (1:3)`

    // ── FLAG-02 e RANGE-EXCEPTION-FOR-INDEX (§7.3.14.4 GR4 e) — a Format-1 index-assignment (SET … TO) or Format-2
    //    index-arithmetic (SET … UP/DOWN BY) whose receiving field is an INDEX-NAME, flagged when EC-RANGE-INDEX
    //    checking is enabled. **Only an index-NAME receiver range-checks** (§14.9.39.4 Format-1 GR2a / Format-2 GR4a):
    //    a class-index DATA item (USAGE INDEX) receiver copies its value UNCHANGED (Format-1 GR2b) and never raises
    //    EC-RANGE-INDEX, so it is NOT flagged. A receiver is an index-name iff its base name is in the current unit's
    //    INDEXED BY registry (DataBinder.IndexNames); a data-name / pointer / capacity / dynamic-length receiver of
    //    the SHARED SET-TO / SET-UP/DOWN grammar (Formats 5/10/14/16) is intrinsically excluded (never an index-name). ──
    public override object? VisitSetToValueStatement(CobolParserCore.SetToValueStatementContext ctx)
    {
        FlagIndexSet(ctx.dataReference(), ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    public override object? VisitSetIndexStatement(CobolParserCore.SetIndexStatementContext ctx)
    {
        FlagIndexSet(ctx.dataReference(), ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    /// <summary>Flag the SET once (GR4 e) when a receiving operand is an index-name of the current unit AND
    /// EC-RANGE-INDEX checking is enabled at the statement line — the same <see cref="TurnState"/> read i uses for
    /// EC-BOUND-REF-MOD; the fold honours the exception hierarchy, so an enabling <c>&gt;&gt;TURN EC-RANGE</c> /
    /// <c>EC-ALL</c> also counts. No-op outside a resolvable program unit (an OO method body — the advisory edge).</summary>
    private void FlagIndexSet(IReadOnlyList<CobolParserCore.DataReferenceContext> receivers, int line)
    {
        if (_currentData is null) return;
        foreach (var recv in receivers)
        {
            DataReferenceCst r = recv;
            if (r.Register != SpecialRegister.None || r.BaseName is not { } name
                || !_currentData.IndexNames.Declares(name)) continue;
            if (_turn.Enabled("EC-RANGE-INDEX", null, line))
                Flag(FlagOption.Flag02RangeExceptionForIndex, line,
                    "the SET of an index-name while EC-RANGE-INDEX checking is enabled");
            break;   // GR4 e flags the SET once, however many receivers are index-names
        }
    }

    /// <summary>The entry's PICTURE string, VALUE clause, and USAGE clause (each null when absent), and whether it
    /// carries a BLANK WHEN ZERO clause — read once from the data-description clauses.</summary>
    private static (string? Picture, CobolParserCore.ValueClauseContext? Value, CobolParserCore.UsageClauseContext? Usage,
        bool BlankWhenZero) Clauses(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        var list = ctx.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause();
        string? pic = null;
        CobolParserCore.ValueClauseContext? value = null;
        CobolParserCore.UsageClauseContext? usage = null;
        bool blankWhenZero = false;
        if (list is not null)
            foreach (var c in list)
            {
                if (c.pictureClause()?.PIC_STRING() is { } ps) pic = ps.GetText();
                if (c.valueClause() is { } vc) value = vc;
                if (c.usageClause() is { } uc) usage = uc;
                if (c.blankWhenZeroClause() is not null) blankWhenZero = true;
            }
        return (pic, value, usage, blankWhenZero);
    }

    /// <summary>Whether the entry's PICTURE classifies as <see cref="PicCategory.NumericEdited"/> via the ONE
    /// <see cref="PictureAnalyzer"/> (discard sink; §13.18.40) — the category of the ITEM, not of the character-string
    /// alone: a BLANK WHEN ZERO clause on a numeric picture "defines the item as numeric-edited" (§13.18.8.4 GR2), so
    /// the clause is threaded to the analyzer that applies that rule. The unit's CURRENCY SIGN set is threaded too
    /// (a picture editing with a non-default currency symbol is numeric-edited all the same), and so is
    /// DECIMAL-POINT IS COMMA, because the §13.18.40.3 composition validator reads it: without it a grouped
    /// comma-mode picture (`9.999,99`) fails composition here and recovers to Alphanumeric, which would turn this
    /// classifier's answer from a rare false-negative into a systematic one (ISO §13.18.40.3 SR13).</summary>
    private bool IsNumericEditedPicture(string picture, bool blankWhenZero)
        => PictureAnalyzer.Analyze(picture, Usage.Display, _discard, "a flagged VALUE clause",
                currencies: _currentData?.CurrencySigns, blankWhenZero: blankWhenZero,
                decimalPointIsComma: _currentData?.DecimalPointIsComma ?? false).Category
            == PicCategory.NumericEdited;

    /// <summary>Whether an item with NO PICTURE has no length from its USAGE either: DISPLAY (explicit or absent —
    /// the default) has no length without a PICTURE, while every other usage (COMP-*, INDEX, POINTER family, the
    /// float/binary families, …) implies a fixed length. Detected from the usage keyword text.</summary>
    private static bool UsageGivesNoLength(CobolParserCore.UsageClauseContext? usage)
        => usage is null || usage.GetText().ToUpperInvariant().EndsWith("DISPLAY", StringComparison.Ordinal);

    /// <summary>A real data-item level (1–49 or the independent 77) — excludes 66 (RENAMES), 78 (CONSTANT), and
    /// 88 (condition-name), none of which is a length-bearing data item.</summary>
    private static bool IsRealDataLevel(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        int lvl = Level(ctx);
        return (lvl >= 1 && lvl <= 49) || lvl == 77;
    }

    private static int Level(CobolParserCore.DataDescriptionEntryContext ctx)
        => int.TryParse(ctx.levelNumber()?.GetText(), out int n) ? n : 0;

    /// <summary>Whether the entry is a GROUP item — the immediately-following sibling entry is a real subordinate
    /// data item (level 2–49, deeper than this entry). A following 66/88 entry is NOT a subordinate.</summary>
    private static bool HasSubordinates(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        if (NextEntry(ctx) is not { } next) return false;
        int nl = Level(next);
        return nl > Level(ctx) && nl is >= 2 and <= 49;
    }

    /// <summary>The next sibling <c>dataDescriptionEntry</c> in the same container (entries are a FLAT list — data
    /// nesting is by level number, not parse structure), or null.</summary>
    private static CobolParserCore.DataDescriptionEntryContext? NextEntry(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        if (ctx.Parent is not { } parent) return null;
        bool found = false;
        for (int i = 0; i < parent.ChildCount; i++)
        {
            var child = parent.GetChild(i);
            if (found && child is CobolParserCore.DataDescriptionEntryContext next) return next;
            if (ReferenceEquals(child, ctx)) found = true;
        }
        return null;
    }

    /// <summary>Whether a numeric-edited VALUE clause is a plain LITERAL (numeric or nonnumeric — the caller has
    /// already excluded figurative constants) that contains NO editing symbols (j VALUE-EDITING). A numeric literal
    /// never carries editing symbols (flagged); a nonnumeric STRINGLIT/NATLIT is scanned for the unambiguous
    /// numeric-editing insertion characters. Only a '0'- or 'B'-only insertion (ambiguous with a digit / a letter)
    /// escapes the scan — a rare false-negative for this advisory flag. A concatenation / boolean / hex literal is
    /// not analyzed (not a numeric-editing value) and is not flagged.</summary>
    private static bool LiteralHasNoEditingSymbols(CobolParserCore.ValueClauseContext value)
    {
        if (FirstDescendant<CobolParserCore.NonNumericLiteralContext>(value) is { } nn)
        {
            string? text = nn.STRINGLIT()?.GetText() ?? nn.NATLIT()?.GetText();
            return text is not null && !ContainsEditingSymbol(StripLiteral(text));
        }
        return FirstDescendant<CobolParserCore.NumericLiteralContext>(value) is not null;
    }

    // The unambiguous numeric-editing INSERTION characters as they appear in an edited value literal (§13.18.40.3).
    // '0' (zero insertion) and 'B' (space insertion) are omitted — indistinguishable from a digit / a letter in the
    // literal text without re-deriving the picture mask.
    private static readonly char[] EditingChars = [' ', '/', ',', '.', '+', '-', '$', '*'];

    private static bool ContainsEditingSymbol(string content)
    {
        if (content.IndexOfAny(EditingChars) >= 0) return true;
        string trimmed = content.TrimEnd();   // the CR / DB trailing sign insertions
        return CobolNames.EndsWith(trimmed, "CR")
            || CobolNames.EndsWith(trimmed, "DB");
    }

    /// <summary>The content of a STRINGLIT / NATLIT token — a leading national <c>N</c> prefix and the surrounding
    /// quotes stripped — for the editing-symbol scan.</summary>
    private static string StripLiteral(string token)
    {
        string s = token.Length > 0 && token[0] is 'N' or 'n' ? token[1..] : token;
        return s.Replace("\"", "").Replace("'", "");
    }

    /// <summary>The first descendant of type <typeparamref name="T"/> in <paramref name="node"/>'s subtree (pre-order),
    /// or null. A small generic walk — the flag detectors reach into a construct's operand subtree without threading
    /// the exact (edition-varying) grammar path.</summary>
    private static T? FirstDescendant<T>(Antlr4.Runtime.Tree.IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T hit) return hit;
            if (FirstDescendant<T>(child) is { } deeper) return deeper;
        }
        return null;
    }
}
