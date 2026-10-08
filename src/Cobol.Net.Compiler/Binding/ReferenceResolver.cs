// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// Resolves a <c>dataReference</c> parse node to a <see cref="Place"/> — the single entry point every verb uses to
/// turn a COBOL operand into a typed C# lvalue (COBOLNET_DESIGN §3.4). Two phases:
/// <list type="number">
///   <item><b>Syntactic flatten</b> — walk <c>cobolWord dataReferenceSuffix*</c> into the base name, its OF/IN
///         qualifiers, its subscript list (<c>subscriptPart</c>, one parsed item per subscript — kb/Work PB2113) and
///         its reference modifier (<c>refModPart</c>).</item>
///   <item><b>Semantic resolve</b> — resolve the (optionally qualified) name to a <see cref="DataItem"/>, interpret
///         the subscripts to C# index expressions, and build the member-access path with each subscript attached to
///         its OCCURS level (outer→inner).</item>
/// </list>
/// Returns <see langword="null"/> when the reference cannot be resolved in this slice — an unknown name, a special
/// register, a reference-modified reference (<c>(s:l)</c> — G2-1c), or a subscript form not yet handled — so the
/// caller emits a loud not-implemented guard rather than silently mis-binding.
/// </summary>
/// <summary>Which ORDINAL-POSITION rule a rendered segment is subject to. The two positions share one token
/// renderer and one integrality rule, and differ only in the Table 13 condition a non-integer value sets:
/// §8.4.2.3.4 GR1b names EC-BOUND-SUBSCRIPT for a subscript, §8.4.3.3.4 rule 5)c) names EC-BOUND-REF-MOD for a
/// leftmost-position/length (fix-queue PB41). Carried by parameter rather than a field — the renderer is
/// re-entrant through <see cref="ReferenceResolver.ReadRefMod(CobolParserCore.RefModPartContext)"/>, and ambient
/// state goes stale across a re-entrant descent (the ExpressionBinder OperandContext discipline).</summary>
internal enum SegmentPosition
{
    /// <summary>A subscript (ISO §8.4.2.3.2 <c>arithmetic-expression-1</c>) — EC-BOUND-SUBSCRIPT.</summary>
    Subscript,

    /// <summary>A reference-modifier leftmost-position or length (§8.4.3.3.3 SR4) — EC-BOUND-REF-MOD.</summary>
    RefMod,
}

/// <summary>What ONE data reference carries after its base word, read lexically by
/// <see cref="ReferenceResolver.ReadOperandSuffixes"/> for the DATA-DIVISION clauses (kb/Work PB205).</summary>
/// <param name="RefMods">How many reference-modification carriers are written. More than one violates ISO
/// §8.4.3.3.3 SR3 ("Identifier-1 shall not be a reference-modification format identifier").</param>
/// <param name="Subscripts">How many subscript carriers are written.</param>
/// <param name="Start">The leftmost-position integer literal, when exactly one ref-mod is written and both its
/// segments are integer literals; null otherwise.</param>
/// <param name="Length">The length integer literal — null both for "no readable ref-mod" and for §8.4.3.3.2's
/// omitted, bracketed length, which <see cref="RefMods"/> and <see cref="Start"/> tell apart.</param>
/// <param name="NonLiteral">Exactly one ref-mod is written and a segment of it is NOT an integer literal — the
/// shape §13.18.16.3 SR4 / §13.18.54.3 SR8 / §13.18.57.3 SR10 each reject.</param>
/// <param name="BeyondHostLimit">Exactly one ref-mod is written, both segments ARE integer literals, and one of them
/// lies beyond the host range (kb/Work PB1579). <see cref="Start"/> and <see cref="Length"/> are then null: a
/// clause that lays the slice out (the CONTROL prior-value copy, §13.18.16.4 GR3) reports COBOLNET2427 through
/// <see cref="CobolNet.Validation.IntegerOperandRules.BeyondLimitMessage"/> rather than allocate a saturated
/// bound.</param>
internal readonly record struct RefModSuffixes(
    int RefMods, int Subscripts, int? Start, int? Length, bool NonLiteral, bool BeyondHostLimit = false);

public sealed partial class ReferenceResolver(DataBinder data)
{
    /// <summary>The D18 hook that MATERIALIZES a subscript / ref-mod position the direct walk cannot read
    /// (fix-queue PB17): given the position's PARSED arithmetic expression (null when the written position is not
    /// one — the hook refuses it by name, COBOLNET2363), its written text for that message and its line, it binds the
    /// expression through the ONE <c>ExpressionBinder.BindExpr</c>, synthesizes the §15.4 temporary via
    /// <c>DataBinder.CreateCompilerTemp</c>, registers the store as a statement-scoped pending PRE-op on
    /// <see cref="DataBinder.PendingPreOps"/>, and returns the temp — which this resolver then reads as an ordinary
    /// position (<see cref="PositionRead"/>).
    /// <para>⛔ IT IS A HOOK, NOT A COLLABORATOR REFERENCE, because the binder dependency is ONE-WAY:
    /// <c>StatementBinder(DataBinder, ReferenceResolver)</c>. StatementBinder installs it in its constructor (the
    /// <c>ConditionRenderer.Calls</c> property-wire precedent). Null on the DATA-division resolution paths
    /// (<c>DataBinder.Constants</c>/<c>Ptr</c> build a throwaway resolver with no procedure binder), where the
    /// old loud posture stands — a VALUE/ADDRESS OF subscript cannot carry a function activation anyway.</para>
    /// <para>⚠ Binding happens HERE, at resolve time, NOT at the drain. That is load-bearing for nesting: the UDF
    /// precedent's own words are "a nested call registers while its consumer's arguments bind, so it precedes the
    /// consumer in the sequence". Deferring the bind to the drain would append an INNER segment's temp AFTER its
    /// consumer's — <c>W-E(FUNCTION INTEGER(W-F(FUNCTION INTEGER(2))))</c> would store in the wrong order.</para></summary>
    /// <remarks>⛔ THE POSITION RIDES THE HOOK (kb/Work PB170/PB172). It used to be <c>Func&lt;string, int,
    /// DataItem?&gt;</c>, so ONE hook served BOTH <see cref="SegmentPosition.Subscript"/> and
    /// <see cref="SegmentPosition.RefMod"/> and the binder hard-coded the index-name window context for both —
    /// but §13.18.38.3 r7's five contexts list "as a subscript" and do NOT list a reference-modification
    /// position, so an index-name in a ref-mod bound was wrongly admitted. One hook, two positions, one context
    /// was the defect; the position is now part of the hook's question.</remarks>
    internal Func<Core.ArithmeticExpressionContext, SegmentPosition, int, DataItem?>? MaterializeSegment { get; set; }

    /// <summary>⛔ THE ODO LENGTH IS EVALUATED BEFORE THE REFERENCE MODIFIER (ISO §14.6.4 steps 6 then 7 — "length
    /// evaluation for an occurs-depending group item", then "reference modification", each done in full before the
    /// next; kb/Work PB1123). A reference modifier whose position carries a function activation has that activation
    /// hoisted ahead of the statement as a PRE-op (<see cref="MaterializeSegment"/>), so it would run BEFORE the
    /// occurs-depending group's length is read at the operand site. This hook — set by the statement binder, the
    /// same one-way edge as <see cref="MaterializeSegment"/> — pins the length first: given the occurs-depending
    /// group's place, the index in the pending pre-ops where the reference modifier's own pre-ops begin, and the
    /// line, it registers a copy of the DEPENDING ON object into a compiler temp at that index and returns the place
    /// whose extent reads the temp. Null when absent (a data-division resolver): the pre-D18 answer.</summary>
    internal Func<OdoGroupPlace, int, int, Place>? FreezeOdoExtent { get; set; }
    /// <summary>A SYNTHETIC copy of <paramref name="dref"/> without its LAST <c>dataReferenceSuffix</c> — for a
    /// caller whose resolved SYMBOL has shown that the trailing word the parser attached as a qualifier belongs to
    /// the enclosing construct instead (kb/Work PB843: a THROUGH range's <c>IN alphabet-name-1</c>). The copy ADOPTS
    /// the original children (the <c>BinderDriver.Reparent</c> technique) and never mutates the parse tree, so every
    /// resolver walk — each reads <c>dataReferenceSuffix()</c> off the node it is handed — sees the reference the
    /// symbol decided, with no second resolution path to keep in step.</summary>
    internal static Core.DataReferenceContext WithoutTrailingSuffix(Core.DataReferenceContext dref)
    {
        var cut = new Core.DataReferenceContext(dref.Parent as ParserRuleContext, dref.invokingState);
        int keep = dref.ChildCount - 1;
        for (int i = 0; i < keep; i++)
            switch (dref.GetChild(i))
            {
                case ParserRuleContext rc: cut.AddChild(rc); break;
                case ITerminalNode t: cut.AddChild(t); break;
            }
        cut.Start = dref.Start;
        cut.Stop = dref.GetChild(keep - 1) switch
        {
            ParserRuleContext rc => rc.Stop,
            ITerminalNode t => t.Symbol,
            _ => dref.Start,
        };
        return cut;
    }

    /// <summary>Resolve <paramref name="dref"/> to a <see cref="Place"/>, or <see langword="null"/> if unsupported
    /// here — the DEMANDING form: a name that identifies NO declared item reports <c>COBOLNET1639</c> (kb/Work
    /// R30 — §8.4.2.1: "a statement shall contain a reference that uniquely identifies that resource"; before
    /// this, a typo in any reference position compiled clean and threw at RUN time). Every OTHER null return —
    /// a special register routed by the caller, an unsupported subscript/RENAMES/Tier-C shape — is feature-debt
    /// staging and stays silent, so the caller's loud posture is unchanged. A caller asking "IS this a data
    /// item?" with a legal alternative on no (the SET format sniffs, the INVOKE class-name receiver, the
    /// boolean/float reroutes) reads <see cref="Probe"/> instead.</summary>
    public RefResolution Resolve(Core.DataReferenceContext dref) => ResolveImpl(dref, report: true);

    /// <summary>The SPECULATIVE form of <see cref="Resolve"/>: identical resolution, but a name that identifies
    /// no item returns null SILENTLY — for type-discriminating probes whose null arm continues to a legal
    /// alternative reading (INVOKE's class-name receiver, SET's format sniffs, EXCEPTION-OBJECT, the
    /// boolean-operand predicate that is documented diagnostic-free). Never use it where a data item is
    /// REQUIRED — that silence is exactly the R30 defect.
    /// <para>⛔ IT RETURNS A SNIFF, NOT A <see cref="Place"/>, AND THE TYPE IS THE FIX (kb/Work PB221). The
    /// documented contract has always been "a probe is a TYPE-DISCRIMINATING sniff whose Place is DISCARDED
    /// after reading its Item" — but it returned a <c>Place</c>, and FOUR callers (CallBinder's BY CONTENT arm,
    /// OoBinder's INVOKE receiver and SET object-reference sender, PtrBinder's SET UP/DOWN first target) simply
    /// committed it into the bound tree. Everything <c>_probing</c> suppresses to keep a probe pure then went
    /// missing from the committed bind: the §8.8.1.1 position screen and the §13.18.38.3 r7 index-name screen
    /// never ran (<c>CALL "S" USING BY CONTENT E(XE)</c> with <c>XE PIC X(4)</c> compiled clean while the
    /// adjacent <c>BY REFERENCE</c> operand drew COBOLNET0844 — one statement, two verdicts), and the D18
    /// materializer's <c>return "1"</c> short-circuit made <c>E(FUNCTION INTEGER(2))</c> bind occurrence ONE — a
    /// WRONG ANSWER, live since PB157 landed the flag. A comment cannot hold that invariant; a return type can.
    /// A caller that has finished discriminating asks <see cref="Resolve"/> for the Place that enters the tree,
    /// which is the pattern <c>SetBinder.BindSetLocale</c> already used.</para></summary>
    public ProbeResult? Probe(Core.DataReferenceContext dref) =>
        ResolveImpl(dref, report: false).Place is { } p
            ? new ProbeResult(p.Item, p is RefModPlace rm ? rm.Category : p.Item.OperandPic?.Category,
                SectionDataItem.NonSectionKind(p))
            : null;

    /// <summary>What a <see cref="Probe"/> may tell its caller: the resolved <see cref="DataItem"/>, the
    /// operand CATEGORY of the reference (§8.4.3.3.4 GR6 for a reference-modified one — <see cref="RefModPlace"/>'s
    /// own reader — else <see cref="DataItem.OperandPic"/>, the D20 one reader, so a GROUP-USAGE group sniffs as
    /// the boolean / national operand it is), and — <paramref name="NonSectionKind"/> — what the reference is when
    /// it is NOT "a data item defined in the file, working-storage, local-storage, or linkage section" (the
    /// <see cref="SectionDataItem"/> question an activation's keyword-less argument asks BEFORE it chooses its
    /// passing mode, kb/Work PB1137). Deliberately NOT a <see cref="Place"/>: a probe is unscreened, so a Place it
    /// produced must never reach the bound tree (kb/Work PB221).</summary>
    public readonly record struct ProbeResult(DataItem Item, PicCategory? OperandCategory, string? NonSectionKind);

    /// <summary>The source nodes this resolver has already DIAGNOSED — a data reference (an undefined name,
    /// <see cref="ReportUnidentified"/>, or a rejected reference shape: SR3 ref-mod-of-ref-mod, SR1 identifier-1
    /// exclusion) or a reference modifier (an omitted leftmost-position, <see cref="ReadRefMod"/>): one report per
    /// source node even when a statement binder resolves the same node more than once, and the fact the receiving
    /// chokepoint asks (<see cref="WasDiagnosed"/>) so that a null it gets back is EITHER already reported OR
    /// reported there — never a silently dropped receiver (kb/Work PB70).</summary>
    private readonly HashSet<ParserRuleContext> _diagnosed = [];

    /// <summary>True when a diagnostic has been emitted for <paramref name="dref"/> by this resolver — the
    /// receiving chokepoint reports an undiagnosed null itself (recognized-not-implemented shape).</summary>
    public bool WasDiagnosed(Core.DataReferenceContext dref) => _diagnosed.Contains(dref);

    /// <summary>The COBOLNET1639 report for a reference no declaration identifies (kb/Work R30): "not defined"
    /// when the bare name exists nowhere; "does not uniquely identify" when it exists but the qualifiers or an
    /// ambiguity defeat resolution (§8.4.2.2 — qualification shall establish uniqueness).</summary>
    internal void ReportUnidentified(Core.DataReferenceContext dref, string name, List<string> qualifiers)
    {
        // kb/Work R32 — a name DECLARED in the SCREEN SECTION is not undefined. Since kb/Work PB260 the section
        // itself is REFUSED (COBOLNET1560), so "is not defined" would send the user hunting a declaration that is
        // right there — the reference is to a REFUSED declaration (COBOLNET2364). Before kb/Work PB1030 this arm
        // reported NOTHING, and the statement bound a run-time NotImplemented announced as a WiseOwl COBOL gap.
        if (data.ScreenNames.Contains(name))
        {
            ReportRefusedDeclaration(dref, name, "a SCREEN SECTION entry — COBOLNET1560");
            return;
        }
        if (!_diagnosed.Add(dref)) return;
        string text = DataBinder.WrittenText(dref);
        // A declared ALPHABET-NAME written where a data item is required (kb/Work R38, PB1030). ISO §8.4.2.1: "a
        // statement shall contain a reference that uniquely identifies that resource", and an alphabet-name
        // (SPECIAL-NAMES) identifies no data item — so the reference identifies no resource of the kind the
        // position needs. R38 adjudicated the vendor alphabet-operand forms (GnuCOBOL's INSPECT CONVERTING
        // alphabet) as extensions no edition admits, and the owner's 2026-08-08 decision keeps WiseOwl COBOL
        // strict-ISO permanently; this arm used to report nothing and let the statement abort at run time.
        if (data.Alphabets.ContainsKey(name) || data.NationalAlphabets.ContainsKey(name))
        {
            data.Edition.Error(DiagnosticCatalog.UndefinedReference,
                $"'{text}' is declared as an alphabet-name in the SPECIAL-NAMES paragraph, not as a data item, so "
                + "this reference identifies no data item (ISO §8.4.2.1: \"a statement shall contain a reference "
                + "that uniquely identifies that resource\").");
            return;
        }
        // ⛔ A DECLARED INDEX-NAME IS NOT UNDEFINED (kb/Work PB1266 — the resolver-level twin of SetBinder's
        // PB388 screen). The data symbol table holds no index-name, so `ADD 1 TO IX` and `CALL "P" USING IX` — an
        // identifier position an index-name cannot fill — landed in the "not defined" arm below and told the user that
        // no declaration gives 'IX', while INDEXED BY had declared it: false, and it sends the reader hunting a typo.
        // §13.18.38.3 SR7 closes the list of contexts an index-name may appear in, and this position is not one, so
        // the reference is refused for the CATEGORY — the same COBOLNET1637 DISPLAY / MOVE / COMPUTE already draw.
        if (data.Symbols.IndexCandidates(name, qualifiers, data.ActiveScope) is not null)
        {
            data.Edition.Error(DiagnosticCatalog.IndexNameContext,
                $"'{text}' is an index-name, which is not an identifier (ISO §8.4.3.1.2): §13.18.38.3 SR7 admits an "
                + "index-name only as a subscript, in PERFORM/SEARCH VARYING, in SET, or in a relation condition, and "
                + $"this position needs an identifier. SET a data item to the index first (SET data-item TO {text}) "
                + "and reference the data item");
            return;
        }
        string msg;
        // ⛔ A DECLARED CONDITION-NAME IS NOT UNDEFINED (kb/Work PB567). The data symbol table holds no level-88,
        // so a condition-name qualified by a data-name it is NOT subordinate to (`IS-A OF H1`, H1 an unrelated
        // record; `IS-A OF G1 OF S`, real ancestors in reversed order) used to land in the "not defined" arm below
        // and tell the user that no declaration gives the name — while the 88 was right there.
        if (!data.Symbols.TryResolve(name, data.ActiveScope, out var candidates)
            && data.Symbols.TryResolveCondition(name, data.ActiveScope, out _))
            msg = MisqualifiedConditionText(text, name, qualifiers);
        else if (candidates.Count == 0)
            msg = $"'{text}' is not defined — no declaration in this source element gives the name '{name}', so "
                + "the statement's reference identifies no resource (ISO §8.4.2.1: \"a statement shall contain a "
                + "reference that uniquely identifies that resource\"). Check the spelling, or declare the item.";
        else if (qualifiers.Count == 0)
            msg = $"'{text}' does not uniquely identify a data item — {candidates.Count} declarations share the "
                + "name and no qualification distinguishes them (ISO §8.4.2.2 — qualification shall establish "
                + "uniqueness).";
        else
        {
            int matches = 0;
            foreach (var c in candidates) if (data.QualifierChainMatches(c, qualifiers)) matches++;
            msg = matches > 1
                ? $"'{text}' does not uniquely identify a data item — {matches} declarations of '{name}' match "
                  + "the written qualifiers (ISO §8.4.2.2 — qualification shall establish uniqueness; write "
                  + "further qualifiers to single one out)."
                : $"'{text}' does not uniquely identify a data item — '{name}' is declared, but not under the "
                  + $"given qualifier{(qualifiers.Count > 1 ? "s" : "")} ({string.Join(" OF ", qualifiers)}) "
                  + "(ISO §8.4.2.2 — qualification shall establish uniqueness).";
        }
        data.Edition.Error(DiagnosticCatalog.UndefinedReference, msg);
    }

    /// <summary>⛔ THE ONE WORDING of a reference whose word is a DECLARED level-88 condition-name that the written
    /// qualifiers do not reach (kb/Work PB567) — every site that finds no condition-name under the qualifiers says
    /// this, and never "not defined". ISO §13.16.3 SR23: "Each condition-name is subordinate to the data-name with
    /// which it is associated", and §8.4.2.2.3 SR5: "The qualification of a condition-name may include the
    /// conditional variable with which the condition-name is associated, as well as by any name by which that
    /// conditional variable may be qualified" — in SR4's order of successively more inclusive levels. So a
    /// qualifier the conditional variable is not subordinate to, or ancestors written in reversed order, identify
    /// NO condition-name. Unqualified, the word is a condition-name written where a data item is required.</summary>
    internal static string MisqualifiedConditionText(string text, string name, IReadOnlyList<string> qualifiers) =>
        qualifiers.Count == 0
            ? $"'{text}' is a condition-name (a level-88 entry), which identifies no data item, and this "
              + "reference requires one (ISO §8.4.2.1: \"a statement shall contain a reference that uniquely "
              + "identifies that resource\")."
            : $"'{text}' does not identify a condition-name — '{name}' is declared as a condition-name, but none "
              + $"is subordinate to the given qualifier{(qualifiers.Count > 1 ? "s" : "")} "
              + $"({string.Join(" OF ", qualifiers)}): a condition-name is qualified only by its conditional "
              + "variable and that variable's containing groups, innermost first (ISO §13.16.3 SR23; "
              + "§8.4.2.2.3 SR4, SR5).";

    /// <summary>True while the CURRENT resolution is a <see cref="Probe"/> — the R30 purity flag (kb/Work
    /// PB157). A probe is a TYPE-DISCRIMINATING sniff whose Place is discarded after reading its Item, so in
    /// probe mode the resolver must be SIDE-EFFECT-FREE: no diagnostics beyond none, no OO property temp/op
    /// registration (the orphan op made OoWrapPropertyOps prepend a GET §8.4.3.9.4 GR2 forbids — or reject a
    /// WITH NO GET property), and no D18 subscript materialization (a function-bearing subscript would bind —
    /// and later ACTIVATE — twice). Save/restored, not cleared: a COMMIT resolution can nest inside hooks.</summary>
    private bool _probing;

    /// <summary>The report whose REPORT SECTION clause operands are being bound (ISO §13.18.53.3 SR4 — the sum counters a
    /// SOURCE operand may name are those "defined in the current report"), or null in the procedure division and
    /// everywhere else. Set only by <c>ReportWriterBinder</c>, for the span of its own clause binding; it narrows the
    /// candidates of a sum-counter reference to that report, so a name two reports share is unambiguous there
    /// (kb/Work PB1292).</summary>
    internal ReportModel? ReportScope { get; set; }

    /// <summary>The VARYING counters the report-section expression being bound may name, by data-name (ISO §13.18.64.3
    /// SR2 — a counter "may be referenced only within the current entry or a subordinate entry"), or null outside a
    /// report entry's clause. Set only by <c>ReportWriterBinder</c>. A counter is no data item (§13.18.64.4 GR1 — "a
    /// temporary integer data item") and lives in no name table, so every reader of a written NAME asks here: the
    /// operand and expression binders through <c>ReportWriterBinder.VaryingExpr</c>, and the subscript renderer
    /// (<see cref="ResolveSubscriptName"/>), which is how a counter subscripts a source item (GR4's NOTE).</summary>
    internal IReadOnlyDictionary<string, ReportVaryingModel>? VaryingScope { get; set; }

    /// <summary>Set when a subscript or reference-modifier SEGMENT of the current resolution failed because one of
    /// its own operands is a deferred shape (the materializer's expression came back
    /// <see cref="BoundExprError.IsUnbuilt"/>), so the segment's null is a DEFERRAL, not a refusal (kb/Work PB1030).
    /// Save/restored per resolution exactly as <see cref="_probing"/> is: the materializer can re-enter this
    /// resolver for a nested reference.</summary>
    private bool _segmentDeferred;

    /// <summary>Called by the segment materializer when a segment's expression came back UNBUILT (not refused) —
    /// see <see cref="_segmentDeferred"/>.</summary>
    internal void NoteSegmentDeferred() => _segmentDeferred = true;

    private RefResolution ResolveImpl(Core.DataReferenceContext dref, bool report)
    {
        bool savedProbing = _probing, savedSegment = _segmentDeferred;
        _probing = !report;
        _segmentDeferred = false;
        try
        {
            var answer = ResolveImplCore(dref, report);
            // ⛔ A DEFERRAL GOES ON THE LEDGER HERE, BEFORE ANY CALLER SEES IT (kb/Work PB1030): the statement
            // funnel announces it (COBOLNET1756) whatever the caller then does with the answer — a caller that
            // drops it (`.OfType<Place>()`) cannot make it silent. A probe records nothing (R30 purity).
            if (report && answer.Outcome == RefOutcome.Deferred) data.Edition.NoteUnbuilt(answer.Feature);
            return answer;
        }
        finally { _probing = savedProbing; _segmentDeferred = savedSegment; }
    }

    /// <summary>⛔ ISO §13.7.3 SR4 FOR A DATA-NAME REFERENCE (kb/Work PB1249): the procedure division may reference a
    /// linkage item "if, and only if" it is a header operand, under one, a redefinition or renaming of one, or BASED
    /// (<see cref="DataBinder.UnreferenceableLinkageRecord"/> is the decision). The reference still resolves, so the
    /// statement binds without a cascade; the compile fails here, once per written reference. The condition-name and
    /// index-name legs (e) ask <see cref="ReportUnreferenceableLinkage"/> from their own resolutions.</summary>
    private void ScreenLinkageReference(Core.DataReferenceContext dref, DataItem item)
    {
        if (data.UnreferenceableLinkageRecord(item) is not { } record || !_diagnosed.Add(dref)) return;
        ReportUnreferenceableLinkage(DataBinder.WrittenText(dref), record);
    }

    /// <summary>The ONE wording of COBOLNET2746, whichever kind of name (data-name, condition-name, index-name) the
    /// written reference is.</summary>
    internal void ReportUnreferenceableLinkage(string written, DataItem record) =>
        data.Edition.Error(DiagnosticCatalog.LinkageItemNotReferenceable,
            $"'{written}' belongs to LINKAGE SECTION record '{record.CobolName}', which the procedure division may not "
            + "reference (ISO §13.7.3 SR4): the record is not an operand of the USING or RETURNING phrase of the "
            + "procedure division header, not a redefinition or renaming of one, and not BASED. Name it in the "
            + "header, or describe it BASED (§13.18.5).");

    /// <summary>The non-place answer for a SEGMENT (subscript or reference-modifier) the renderer could not
    /// render: deferred when the resolver has no materializer (a data-division resolver) or the segment's own
    /// operand is deferred; otherwise REPORTED — the materializer reports every segment it refuses (kb/Work
    /// PB1030: a segment that is not an arithmetic expression is not a subscript, ISO §8.4.2.3.2).</summary>
    private RefResolution SegmentFailure(string written) =>
        MaterializeSegment is null ? RefResolution.Deferred(DeferredShape.SegmentWithoutStatement, written)
        : _segmentDeferred ? RefResolution.Deferred(DeferredShape.DeferredSegmentOperand, written)
        : RefResolution.Refused(written);

    /// <summary>The non-place answer when <see cref="PlaceForItem"/> built no place: its deferred shape, or —
    /// when it names none — the entry whose declaration was refused (a REJECTED REDEFINES view, Tier D
    /// <see cref="RedefinesTier.Rejected"/>; a level-66 RENAMES entry whose operands did not resolve). A reference
    /// to it is reported here, once per reference (COBOLNET2364), because the statement holding it must say why
    /// it cannot bind.</summary>
    private RefResolution ItemFailure(Core.DataReferenceContext dref, PlaceGap gap)
    {
        string written = DataBinder.WrittenText(dref);
        if (gap.Deferred is { } shape) return RefResolution.Deferred(shape, written);
        ReportRefusedDeclaration(dref, gap.Refused.CobolName ?? gap.Refused.CsName, gap.Refused.Class?.RejectReason);
        return RefResolution.Refused(written);
    }

    /// <summary>Why <see cref="PlaceForItem(DataItem, IReadOnlyList{string}, out PlaceGap)"/> built no place:
    /// the <see cref="DeferredShape"/> it has not built, or — when <paramref name="Deferred"/> is null — the
    /// item whose DECLARATION was refused. That item is carried rather than assumed because it need not be the
    /// item asked about: a level-66 RENAMES alias builds no place when data-name-2 (its no-THROUGH target) or one
    /// of its spanned leaves was refused, and the report must name that entry (kb/Work PB1380).</summary>
    private readonly record struct PlaceGap(DeferredShape? Deferred, DataItem Refused);

    /// <summary>COBOLNET2364 — a reference to a name whose declaration the compiler REFUSED (a Tier-D REDEFINES
    /// view; a SCREEN SECTION name, the section being declined as COBOLNET1560). The compile has already failed at
    /// the declaration; this names the consequence at the statement, instead of "not defined" (the name IS
    /// declared) or "not implemented" (it is not a gap in WiseOwl COBOL).</summary>
    private void ReportRefusedDeclaration(Core.DataReferenceContext dref, string name, string? why)
    {
        if (_probing || !_diagnosed.Add(dref)) return;   // R30 purity; one report per written reference
        string text = DataBinder.WrittenText(dref);
        data.Edition.Error(DiagnosticCatalog.ReferenceToRefusedDeclaration,
            (text == name ? $"'{text}' names a data item" : $"'{text}' names '{name}'") + ", whose declaration was refused"
            + (why is null ? "" : $" ({why})") + "; see the error at the declaration. The reference cannot be bound.");
    }

    private RefResolution ResolveImplCore(Core.DataReferenceContext dref, bool report)
    {
        DataReferenceCst r = dref;
        // The three answers (kb/Work PB1030). The written text is built only on a failure path.
        RefResolution Refused() => RefResolution.Refused(DataBinder.WrittenText(dref));
        RefResolution Deferred(DeferredShape shape) => RefResolution.Deferred(shape, DataBinder.WrittenText(dref));
        static RefResolution Resolved(Place place) => RefResolution.Resolved(place, "");
        // A special register — LINAGE-COUNTER (I-O control system, ISO §8.4.3.14), LINE-/PAGE-COUNTER (Report
        // Writer control system, ISO §8.4.3.15) — is runtime-sourced, never a storage Place; the binder routes it
        // to BoundLinageCounterRef / BoundReportCounterRef (StatementBinder.ReportWriter.cs). The early return is
        // LOAD-BEARING for the QUALIFIED form (`LINAGE-COUNTER OF file`, `LINE-COUNTER OF report`): there
        // r.BaseName is the FILE-/REPORT-NAME qualifier and would otherwise mis-resolve here as a base data-name.
        if (r.Register != SpecialRegister.None) return Deferred(DeferredShape.UnroutedSpecialRegister);
        // No base word outside a special register is a parse-error recovery node: the parser reported it.
        if (r.BaseName is not { } name) return Refused();

        // The OCCURS DYNAMIC CAPACITY register (ISO §13.18.38 GR15 / §8.5.1.9.1; data-model D9): an implicitly-
        // defined VIEW over the owning dynamic table's current capacity — never a storage item, so it is not in
        // ByName and is resolved HERE (before ordinary name lookup) to a CapacityRegisterPlace whose Read() emits
        // {tablePath}.Capacity. The NAME identifies it outright (§13.18.38.3 SR30 first sentence — "Data-name-3
        // shall not be defined elsewhere in the source element"), so every other question is a predicate over the
        // reference AS WRITTEN and belongs to this one screen, not to a fall-through (kb/Work PB457).
        if (CapacityRegisterFor(dref) is { } capReg)
            return CapacityPlaceOf(dref, capReg) is { } capPlace ? Resolved(capPlace) : Refused();

        // The PREDEFINED OBJECT REFERENCE EXCEPTION-OBJECT (ISO §8.4.3.6; kb/Work PB922): §8.4.3.6.3 SR2 —
        // "EXCEPTION-OBJECT is implicitly described as class object and category object reference, as an external
        // data item, and as a universal object reference" — so the STANDARD declares this name, no data
        // description entry does, and it is resolved HERE (before ordinary name lookup, the CAPACITY / DEBUG-ITEM
        // pattern) to a read-only VIEW over the run unit's one instance (§8.4.3.6.4 GR2).
        // ⛔ IT IS RESOLVED IN THE ONE RESOLVER SO EVERY CALLER INHERITS IT. Before this, only SET asked the
        // question (OoBinder's own arm), so every OTHER reference — `IF EXCEPTION-OBJECT = NULL`, an INVOKE
        // receiver, a function argument, and even the ILLEGAL `MOVE U TO EXCEPTION-OBJECT` — drew COBOLNET1639
        // "'EXCEPTION-OBJECT' is not defined … Check the spelling, or declare the item", advice the very next
        // diagnostic (COBOLNET0901, "is a reserved word … cannot be used as a user-defined word") forbids taking.
        // The RECEIVING half of SR1 is screened at the ONE receiving chokepoint (ExpressionBinder.ResolveReceiving);
        // this arm is what makes the SENDING half legal source rather than a typo.
        // The predicate is <see cref="IsExceptionObjectRegister"/> — the ONE answer every caller asks for.
        if (IsExceptionObjectRegister(dref)) return Resolved(new ExceptionObjectPlace(data.ExceptionObjectRegister));

        // The X3.23-1985 DEBUG-ITEM special register / member (VCR Table 7 row 7.17): an IMPLICITLY-defined read-only
        // VIEW over the program-instance __dbgItem — not in ByName, so resolved HERE (before ordinary name lookup) to
        // a DebugRegisterPlace. Registered ONLY when a procedure-subject debugging declarative is active under WITH
        // DEBUGGING MODE (DebugRegisters empty otherwise → this never fires for a non-debug program). Only the plain
        // unqualified/unsubscripted form is covered — a reference-modified/qualified DEBUG-* falls through to loud.
        if (data.DebugRegisters.TryGetValue(name, out var dbg) && dref.dataReferenceSuffix().Length == 0)
            return Resolved(new DebugRegisterPlace(dbg.Item, dbg.Member));

        // A REPORT SECTION SUM COUNTER (ISO §13.18.54.4 GR5 — the data-name after the level number names the
        // COUNTER, not the printable item; GR12 permits procedure division statements to read and alter it): an
        // IMPLICITLY-defined VIEW over the report engine's counter, not in ByName, so it is resolved HERE — the
        // CAPACITY-register pattern — to a ReportSumCounterPlace whose read/write are SumValue/SetSumValue
        // (kb/Work PB840). Its qualifiers are the report group entries above it and, outermost, its REPORT-NAME
        // (§8.4.2.2.3 SR4; §8.4.2.2.2 Format 1's file-report-qualifier — kb/Work PB1454). A REPEATING entry's
        // counter is a table and takes the ordinary subscripts (kb/Work PB1271).
        if (SumCounterFor(dref, name, report) is { } sumCounter) return sumCounter;

        // The reference AS WRITTEN, read by the ONE decomposition (kb/Work PB443 — see WrittenReference).
        var written = ReadWritten(dref);
        var qualifiers = written.Qualifiers;
        var subCtx = written.SubscriptGroup;    // the subscript list
        var cleanRef = written.RefModPart;      // the reference modifier (refModSpec : leftmost : length)

        // ISO §8.4.3.3.3 SR3 — "Identifier-1 shall not be a reference-modification format identifier." The
        // grammar cannot express this (dataReferenceSuffix* and qualification's own (subscriptPart|refModPart)*
        // both admit unlimited ref-mods), so it is COUNTED (WrittenReference.RefModCount). Before that count,
        // `??=` kept the FIRST of each of the two carriers a ref-mod then had and one outranked the other, so
        // `MOVE A (3:4)(2:2)` COMPILED CLEAN and returned A(2:2) — a silent wrong value, not a composition and not
        // a rejection.
        if (!ScreenRefModCount(dref, written, name)) return Refused();

        // A reference whose chain ends in a non-word object (SELF, SUPER, a view, a function, NULL) is never a data name.
        DataItem? item = written.PropertyObject is not null ? null
            : qualifiers.Count > 0 ? ResolveQualified(name, qualifiers) : ResolveUnqualified(name);
        // §8.4.3.1.2 Format 7 (kb/Work PB1425): `property-name-1 OF identifier-3` is textually a qualified data
        // reference, so it legitimately FAILS normal qualification. ResolveObjectProperty splits the chain — identifier-3
        // takes the qualifiers and the subscripts (§8.4.3.1.4 GR1 a) before d)) — and synthesizes the §8.4.3.9.4 GR1–GR3
        // temp; the rest of THIS method gives the temp the reference-modification tail (§8.4.3.1.4 GR1 g)), which
        // §8.4.3.9.3 SR5/SR6 permit on the property value. A view of a qualified data item written `A OF G AS C` arrives
        // here too, and is that view.
        bool objectProperty = false;
        if (item is null)
        {
            if (ViewOfQualifiedItem(dref, report) is { } viewOfItem) return viewOfItem;
            var (temp, final) = ResolveObjectProperty(dref, name, written);
            if (final is not null) return final;
            objectProperty = (item = temp) is not null;
        }
        if (item is null)
        {
            // The NAME resolves to nothing — a typo or a mis-qualification, never a feature gap (kb/Work R30).
            // Every later null in this method is an unsupported-shape staging of a name that DID resolve.
            if (report) ReportUnidentified(dref, name, qualifiers);
            return Refused();
        }

        List<Position> indexExprs = [];   // a property temp has no OCCURS: the written subscripts were identifier-3's
        if (!objectProperty)
        {
            if (report) ScreenLinkageReference(dref, item);
            if (ReadSubscripts(dref, item, subCtx, out var subscripts) is { } subscriptFailure) return subscriptFailure;
            indexExprs = subscripts;
        }

        // ⛔ EVERY NAMED ITEM TAKES THE SAME TAIL — a level-66 RENAMES alias included (kb/Work PB1380). The alias's
        // place is built by PlaceForItem, the ONE item→place builder, and then meets the reference-modification
        // tail below like any other identifier. It used to be composed in an arm of its own HERE that returned
        // before that tail, so `RN(2:3)` bound the WHOLE alias — ISO §8.4.3.3.4 GR5's "unique data item that is a
        // subset of the data item referenced by identifier-1" was never created, `MOVE RN(2:3) TO X` moved every
        // character of the span, and `RN(2:N)` with N = 0 overwrote the span instead of raising EC-BOUND-REF-MOD
        // (§7.3.23.3 GR1, directive omitted). Both forms were dropped: the THROUGH form's RenamesPlace AND the
        // no-THROUGH form's forward to data-name-2's place.
        if (PlaceForItem(item, indexExprs, out var gap) is not { } inner) return ItemFailure(dref, gap);

        if (cleanRef is null) return Resolved(inner);
        // identifier-1's DESCRIPTION is the item its place stands for, not the name as written: for a no-THROUGH
        // RENAMES alias that is data-name-2, whose attributes §13.18.45.4 GR1 makes data-name-1's own; for a
        // THROUGH alias it is the alias itself (an alphanumeric group item, GR2); for every other item the two
        // are the same item. The syntactic path, the data-division path (ResolveItemRefMod) and the resolved-place
        // path (RefModOf) all read it this way, so SR1 and the view are asked of one description.
        DataItem described = inner.Item;
        int preOpMark = data.PendingPreOps.Count;   // the reference modifier's own pre-ops (function activations) start here
        if (ReadScreenedRefMod(dref, written, described, out var refusal) is not { } spec) return refusal!;
        // §14.6.4 steps 6 → 7: an occurs-depending group's length is evaluated BEFORE the reference modifier, so when
        // the modifier registered pre-ops (a function whose side effect may change the DEPENDING ON object) the length
        // is pinned ahead of them (kb/Work PB1123).
        if (inner is OdoGroupPlace odo && data.PendingPreOps.Count > preOpMark && FreezeOdoExtent is { } freeze)
            inner = freeze(odo, preOpMark, dref.Start.Line);
        return RefModView(described, inner, spec) is { } view ? Resolved(view) : Deferred(DeferredShape.NumericRefModSubstrate);
    }

    /// <summary>⛔ §8.4.3.3.3 SR3 — "Identifier-1 shall not be a reference-modification format identifier." The
    /// grammar cannot express it, so the written reference is COUNTED (<see cref="WrittenReference.RefModCount"/>);
    /// ONE screen for every entry that reads a reference-modified identifier — <see cref="Resolve"/> and
    /// <see cref="ResolveForAddressOf"/> (kb/Work PB1407). False having reported.</summary>
    private bool ScreenRefModCount(Core.DataReferenceContext dref, WrittenReference written, string name)
    {
        if (written.RefModCount <= 1) return true;
        if (!_probing && _diagnosed.Add(dref))   // R30 purity: a probe never diagnoses (kb/Work PB157)
            data.Edition.Error(DiagnosticCatalog.RefModOfRefMod,
                $"'{name}' carries {written.RefModCount} reference modifications; a reference-modified item cannot itself "
                + "be reference-modified (ISO §8.4.3.3.3 SR3). Compose the positions into one modifier instead.");
        return false;
    }

    /// <summary>⛔ THE ONE ADMISSION OF A REFERENCE MODIFIER, from the written reference to the screened
    /// <see cref="RefModSpec"/> (kb/Work PB1407 extracted it from <see cref="Resolve"/> so ADDRESS OF reads the same
    /// rules): §8.4.3.3.3 SR1 — WHAT identifier-1 MAY BE (kb/Work PB70; a bind-time rejection, COBOLNET1647, never
    /// the run-time NotImplemented a sending ref-mod used to reach nor the silent drop a receiving one fell into),
    /// the spec read off whichever carrier the lexer chose, and the LITERAL out-of-range screen (kb/Work PB1707
    /// part 1, R60 — refused while checking is off, compiled with a warning while it is on, the run-time raise then
    /// being what the program asked for). <paramref name="described"/> is identifier-1's DESCRIPTION: the item its
    /// place stands for, not the name as written — for a no-THROUGH RENAMES alias that is data-name-2, whose
    /// attributes §13.18.45.4 GR1 makes data-name-1's own. Null having reported, with the answer in
    /// <paramref name="refusal"/>.</summary>
    private RefModSpec? ReadScreenedRefMod(Core.DataReferenceContext dref, WrittenReference written, DataItem described,
        out RefResolution? refusal)
    {
        refusal = null;
        if (RefModExclusion(described) is { } why)
        {
            if (!_probing && _diagnosed.Add(dref))   // R30 purity: a probe never diagnoses (kb/Work PB157)
                data.Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted,
                    $"'{DataBinder.WrittenText(dref)}': reference modification of {why} is not permitted (ISO §8.4.3.3.3 SR1)");
            refusal = RefResolution.Refused(DataBinder.WrittenText(dref));
            return null;
        }
        if (ReadRefMod(written.RefModPart!) is not { } spec)   // RefModCount > 0 ⇒ the first modifier is in hand
        {
            refusal = SegmentFailure(DataBinder.WrittenText(dref));   // a bound the materializer refused or deferred (§8.4.3.3.3 SR4)
            // A REFUSED segment was reported by whoever refused it (the materializer, or ReadRefMod's omitted
            // leftmost-position), so the reference counts as diagnosed: a caller that asks (ADDRESS OF's binder) then
            // adds no second, misleading "mis-subscripted" error of its own.
            if (refusal.Outcome == RefOutcome.Reported && !_probing) _diagnosed.Add(dref);
            return null;
        }
        if (!ScreenRefModLiterals(dref, described, spec, dref.Start.Line))
        {
            refusal = RefResolution.Refused(DataBinder.WrittenText(dref));
            return null;
        }
        return spec;
    }

    /// <summary>Reference-modify an ALREADY-RESOLVED place — §8.4.3.1.4 GR1 g)'s tail ("a reference modifier
    /// applies to the identifier on the left") over an identifier that is not a data-NAME reference. Its one
    /// caller today is the §8.4.3.4 inline method invocation, whose §8.4.3.4.4 GR1 c) temporary has no
    /// <c>dataReference</c> context of its own (kb/Work PB428). It is a thin ENTRY, not a second reading: the
    /// §8.4.3.3.3 SR1 screen is <see cref="RefModExclusion"/> and the view is <see cref="RefModView"/> — the
    /// same two the syntactic path runs, in the same order, with the same COBOLNET1647.</summary>
    internal Place? RefModOf(Place inner, Core.RefModPartContext rmp, string what)
    {
        if (RefModExclusion(inner.Item) is { } why)
        {
            data.Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted,
                $"'{what}': reference modification of {why} is not permitted (ISO §8.4.3.3.3 SR1)");
            return null;
        }
        return ReadRefMod(rmp) is { } spec ? RefModView(inner.Item, inner, spec) : null;
    }

    /// <summary>⛔ THE ONE REFERENCE-MODIFICATION VIEW BUILDER (kb/Work PB205): the SUBSTRATE wrap that turns an
    /// item's place into the §8.4.3.3.4 GR5 unique data item, written down once for BOTH the syntactic path
    /// (<see cref="Resolve"/>, where the positions come off the source) and the DATA-DIVISION path
    /// (<see cref="ResolveItemRefMod"/>, where a clause carries integer literals). Before this factoring the wrap
    /// lived INSIDE the parse-context-keyed path, so the CONTROL clause — which §13.18.16.3 SR4 expressly permits
    /// to be reference-modified — could not reach it and silently bound the WHOLE item instead.
    /// <para>Callers screen §8.4.3.3.3 SR1 (<see cref="RefModExclusion"/>) first; a null here is an item whose
    /// substrate this backend cannot slice (a numeric item that is not usage DISPLAY), never a rule violation.</para></summary>
    private Place? RefModView(DataItem item, Place inner, RefModSpec spec)
    {
        // Reference modification is over a character string. A GROUP (SR1 — "an alphanumeric group item" / "a group
        // item that is neither a strongly-typed group nor a variable-length group") is viewed through its character
        // IMAGE — the unique data item is an elementary alphanumeric item over the group's positions (§8.4.3.3.4
        // GR6); a Tier-B view (RedefViewPlace) is already a character window. A NUMERIC USAGE-DISPLAY item is viewed
        // through its character image likewise (NC224A's TEST-1-DATA(3:) over PIC 9(6)); a numeric item of usage
        // NATIONAL (SR1 admits it) has no national image channel yet — the loud stage.
        if (item.IsGroup)
        {
            // ⛔ A BIT GROUP TAKES BIT POSITIONS, NOT CHARACTER POSITIONS (kb/Work PB173). §8.4.3.3.3 SR1's last
            // sentence admits a bit group as identifier-1 "treated as [an] elementary data item", §13.18.29.4
            // GR1b makes that item PICTURE 1(m) of usage bit, and §8.4.3.3.4 GR5a then says in so many words:
            // "If the usage of identifier-1 is bit, positions used in evaluation are bit positions". So it wraps
            // as its UNPACKED boolean string, never as AsImage()'s ceil(m/8) packed characters — the units the
            // boolean channel already counts (ConditionBinder's widths read OperandPic.Length, which is
            // ExtentBits) and the units RefModPlace.Category already reports.
            // ⛔ A NATIONAL GROUP TAKES ITS OWN NATIONAL POSITIONS, THE SAME WAY (kb/Work PB327). It used to
            // keep GroupImagePlace on the premise that "a national leaf contributes ImageWidth = Length
            // character positions, never byte-doubled, so its AsImage() IS its national-position string". That
            // premise died when AsImage() became the BYTE image so a national leaf could ride a file record:
            // §13.18.60.4 GR8 leaves the size of a national character to the implementor and D-N1 pins TWO
            // bytes, UTF-16BE. §8.4.3.3.4 GR5a's "otherwise, positions used in evaluation are character
            // positions" means the item's OWN characters, so a national group wraps as its AsNat() string.
            // ⛔ A TIER-B WINDOW NEEDS NO WRAP IN EITHER UNIT, and for the BIT one that is now a derived fact
            // rather than an untested omission (kb/Work PB203 closing PB173's open "RELATED" question): a
            // RedefViewPlace over a bit member reads its BOOLEAN CARRIER — CobolBits.ReadWindow over the class
            // backing — so the RefModPlace built below slices §8.4.3.3.4 GR5a's bit positions structurally,
            // exactly as BitImagePlace's AsBits() does for a struct-stored bit group. Measured: with
            // `01 A PIC X(2). 01 BV REDEFINES A GROUP-USAGE BIT. 05 BV1 PIC 1(8). 05 BV2 PIC 1(8).` holding
            // B"0100100001001001", `BV(1:3)` is B"010" and `MOVE ALL B"0" TO BV(2:3)` zeroes positions 2-4.
            if (inner is not RedefViewPlace)
                inner = !item.IsAsIfElementary ? new GroupImagePlace(inner)
                    : item.GroupUsage is GroupUsage.Bit ? new BitImagePlace(inner)
                    : new NatImagePlace(inner);
        }
        else if (item.Pic?.Category is PicCategory.Numeric)
        {
            // ⛔ BOTH USAGES §8.4.3.3.3 SR1 ADMITS (kb/Work PB646) — the SAME pair RefModExclusion above already
            // names, and this gate named only one of them: the exclusion test admitted `PIC 9(6) USAGE NATIONAL`
            // and then this arm returned null, so the operand fell through to the Tier-C runtime loud
            // ("a COBOL feature that is not yet implemented was reached at run time: reference 'RM(2:3)'"). The
            // wrap is the same for both, and §8.4.3.3.4 GR3 is why: a usage-national item "is operated upon for
            // purposes of reference modification as if it were redefined as a data item of class and category
            // national of the SAME SIZE", and its size in national character positions is exactly the digit run
            // NumericImagePlace exposes (GR5a counts character positions; D-N1 makes one national position one
            // UTF-16 char). GR6c's category answer is already written — RefModPlace.CategoryOf — and this is
            // the arm that makes it reachable.
            if (item.Pic is not { IsCharacterFormNumeric: true }) return null;   // THE ONE character-form predicate
            // P5.7: the bind-time wrap decision reads the COLLECTED early facts (same mid-bind timing the
            // deleted flag had — MarkRefModStoreImage records the SAME item during this statement's bind).
            // (A view over engine state has no cell to be image-backed: Place.OwnsStorageCell, kb/Work PB1943.)
            if ((!inner.OwnsStorageCell || !data.IsImageBackedEarly(item)) && inner is not RedefViewPlace)
                inner = new NumericImagePlace(inner);
        }
        // National/boolean items reference-modify in their OWN character positions (§8.4.3.3 GR1/GR5a — a
        // national position is one UTF-16 char, a bit position one '0'/'1' char, under D-N1/D-B1); alphanumeric,
        // alphabetic, alphanumeric-edited and numeric-edited items are character strings already.
        return new RefModPlace(inner, spec.Start, spec.Length) { AllowZeroLength = spec.AllowZeroLength };
    }

    /// <summary>A ref-mod view of an ALREADY-RESOLVED item whose leftmost-position and length are COMPILE-TIME
    /// INTEGERS — the DATA-DIVISION form. The three data-division clauses that admit a reference-modified operand
    /// all restrict it to integer literals for the same reason (ISO §13.18.16.3 SR4 "Data-name-1 may be
    /// reference-modified. If it is, leftmost-position and length shall be integer literals."; §13.18.54.3 SR8;
    /// §13.18.57.3 SR10), so there is no expression to evaluate and no statement context to evaluate it in — the
    /// view is the SAME <see cref="RefModView"/> the procedure division builds, reached without a parse context.
    /// <para><paramref name="length"/> null is §8.4.3.3.2's bracketed, omitted length — the slice runs to the
    /// rightmost position (§8.4.3.3.4 GR5c last sentence). Null return: the item has no place, or §8.4.3.3.3 SR1
    /// does not admit it (the CALLER rejects that at bind with COBOLNET1647 — this is the backstop).</para>
    /// <para>SR1 and the view read the PLACE's item, as <see cref="Resolve"/> does — a no-THROUGH level-66 alias is
    /// described by data-name-2 (§13.18.45.4 GR1), never by its own entry (kb/Work PB1380).</para></summary>
    public Place? ResolveItemRefMod(DataItem item, int start, int? length, bool allowZeroLength = false) =>
        PlaceForItem(item, []) is { } inner && RefModExclusion(inner.Item) is null
            ? RefModView(inner.Item, inner, new RefModSpec(new PositionConstant(start), length is { } l ? new PositionConstant(l) : null, allowZeroLength))
            : null;

    /// <summary>ISO §8.4.3.3.3 SR1, read as an EXCLUSION test: the reason <paramref name="item"/> may NOT be
    /// identifier-1 of a reference modification, or null when it may. Admitted: a boolean, national, alphanumeric or
    /// alphabetic item (bullets 1–4 — this model's <see cref="PicCategory.Alphanumeric"/> covers alphabetic and
    /// alphanumeric-edited, <see cref="PicCategory.National"/> covers national-edited); a numeric-edited item and a
    /// numeric item of usage DISPLAY or NATIONAL, each "not subordinate to a strongly-typed group item" (bullets 5,
    /// 8); a group that is neither strongly typed nor variable-length (bullet 9; §8.5.1.12 — a variable-length group
    /// has a dynamic-length item or a dynamic-capacity table subordinate to it). Everything else — a numeric item of
    /// BINARY / PACKED / COMP-5 / float usage, an index item, a pointer, an object reference — is excluded. Bit and
    /// national GROUPS are "treated as elementary" by SR1's last sentence, and GROUP-USAGE IS modelled — kb/Work
    /// PB79 landed 2026-08-18 (DEVLOG 1317): <c>AsIfPic</c> / <c>OperandPic</c> / <c>IsAsIfElementary</c> carry the
    /// as-if description, so this predicate's group bullet admits them for the right reason. Their SUBSTRATE then
    /// differs by usage: a bit group wraps as a <c>BitImagePlace</c> over its unpacked boolean string, because
    /// §8.4.3.3.4 GR5a evaluates a bit item's positions as BIT positions (kb/Work PB173), and a national group
    /// wraps as a <c>NatImagePlace</c> over its <c>AsNat()</c> national-position string — §13.18.29.4 GR2b's as-if
    /// PICTURE N(m) counts national positions, which since kb/Work PB327 are no longer the same string as the
    /// group's byte image.</summary>
    internal static string? RefModExclusion(DataItem item)
    {
        if (item.IsGroup)
            return StrongTypeModel.IsStrongGroup(item) ? "a strongly-typed group item"
                : HasVariableLengthSubordinate(item)
                    ? "a variable-length group item (ISO §8.5.1.12 — a dynamic-length item or a dynamic-capacity table is subordinate to it)"
                : null;
        // A leaf subordinate to a strongly-typed group (its own class/category still decides bullets 1–4).
        bool underStrong = StrongTypeModel.IsStronglyTyped(item);
        return item.Pic switch
        {
            null => "an item without character positions",
            { Category: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } => null,
            { Category: PicCategory.NumericEdited } =>
                underStrong ? "a numeric-edited item subordinate to a strongly-typed group item" : null,
            { Category: PicCategory.Numeric, Usage: Usage.Index } => "an index data item (class index, ISO §13.18.60)",
            { Category: PicCategory.Numeric, Usage: Usage.Display or Usage.National, IsFloat: false } =>
                underStrong ? "a numeric item subordinate to a strongly-typed group item" : null,
            { Category: PicCategory.Numeric } p =>
                $"a numeric item of USAGE {p.Usage} (SR1 admits usage DISPLAY or NATIONAL only)",
            { Category: PicCategory.ObjectReference } => "an object reference",
            { Category: PicCategory.Pointer or PicCategory.ProgramPointer or PicCategory.FunctionPointer } => "a pointer",
            { } p => $"an item of category {p.Category}",
        };
    }

    /// <summary>⛔ THE ONE COMPILE-TIME RANGE SCREEN of a reference modification (kb/Work PB1707 part 1; owner decision
    /// R60) — the bind-time half of <c>CobolString.RefModOutOfRange</c>, written for LITERAL positions. ISO §8.4.3.3.4
    /// 5) b): leftmost-position is a position of the item, 1 through its size; 5) c): a specified length is a positive
    /// nonzero integer (zero only under REF-MOD-ZERO-LENGTH, §7.3.23) and "The sum of leftmost-position and length
    /// minus the value one shall be less than or equal to the number of positions in the data item referenced by
    /// identifier-1". A violation is the fatal EC-BOUND-REF-MOD; §14.6.13.1.3 8)'s last paragraph licenses the
    /// compiler to produce no code for one it detects while checking is not enabled, and GnuCOBOL (cobc/typeck.c,
    /// "offset of '%s' out of bounds" / "length of '%s' out of bounds") refuses it, so the caller reports
    /// <see cref="DiagnosticCatalog.RefModLiteralOutOfRange"/> there and the warning twin where checking is on.
    /// <para>Only an item of FIXED size is screened — the size is then a compile-time fact: a DYNAMIC LENGTH item
    /// (§8.5.1.10), an ANY LENGTH item (§13.18.2) and an item with an OCCURS DEPENDING beneath it (or a dynamic
    /// table) have a run-time size, which is the run-time range test's. <paramref name="start"/> /
    /// <paramref name="length"/> are null for a position that is not an integer literal; a bound is judged only when
    /// it is known, so <c>X(1:N)</c> is never screened and <c>X(7:N)</c> is refused on its start alone. A length
    /// literal that exceeds the whole item is refused whatever the start, because no start in 1..size can make it fit.
    /// Returns the violated clause's words, or null.</para></summary>
    internal static string? LiteralRefModRangeViolation(DataItem item, System.Numerics.BigInteger? start,
        System.Numerics.BigInteger? length, bool omittedLength, bool allowZeroLength)
    {
        if (item.IsDynamicLength || item.IsAnyLength || HasOdoOnOrBeneathOrDynamicTable(item)) return null;
        int size = RefModPlace.PositionCount(item);
        if (start is { } s && (s < 1 || s > size))
            return $"leftmost-position {s} is not a position of the {size}-position item (ISO §8.4.3.3.4 5) b))";
        if (omittedLength || length is not { } len) return null;
        if (len == 0 && !allowZeroLength)
            return "a zero length is not a positive nonzero integer (ISO §8.4.3.3.4 5) c); REF-MOD-ZERO-LENGTH is not on)";
        if (len > 0 && (start is { } s2 ? s2 + len - 1 > size : len > size))
            return $"the reference modification ({(start is { } s3 ? s3.ToString() : "…")}:{len}) extends past the "
                + $"{size}-position item (ISO §8.4.3.3.4 5) c))";
        return null;
    }

    private static bool HasOdoOnOrBeneathOrDynamicTable(DataItem item) =>
        DataItem.HasOdoOnOrBeneath(item) || item.IsDynamicTable || HasVariableLengthSubordinate(item);

    /// <summary>The decimal-integer LITERAL a rendered position is, or null when it is anything else (a data-name
    /// read, an expression, a narrowing call) — the screen's "is this position known at compile time".</summary>
    private static System.Numerics.BigInteger? LiteralPosition(Position? position) =>
        position is PositionConstant { Value: >= 0 } c ? new System.Numerics.BigInteger(c.Value) : null;

    /// <summary>The hook that answers "is checking for EC-BOUND-REF-MOD enabled at this source line?" — installed by
    /// <c>StatementBinder</c> (which owns the statement-level TURN fold, overlays included) exactly as
    /// <see cref="MaterializeSegment"/> is, and null on the DATA-division resolution paths, where no statement
    /// exists for a checking directive to govern (so the screen reads null as "not enabled").</summary>
    internal Func<int, bool>? RefModCheckingAt { get; set; }

    /// <summary>Screen the literal positions of a reference modification of <paramref name="described"/> written at
    /// <paramref name="line"/> and report a violation once per written reference: the error
    /// (<see cref="DiagnosticCatalog.RefModLiteralOutOfRange"/>, the reference is refused — the caller returns
    /// false) where EC-BOUND-REF-MOD checking is not enabled, the warning twin where it is (the reference binds).</summary>
    private bool ScreenRefModLiterals(Core.DataReferenceContext dref, DataItem described, RefModSpec spec, int line)
    {
        if (_probing) return true;   // R30 purity: a probe never diagnoses (kb/Work PB157)
        if (LiteralRefModRangeViolation(described, LiteralPosition(spec.Start), LiteralPosition(spec.Length),
                spec.Length is null, spec.AllowZeroLength) is not { } why) return true;
        bool checking = RefModCheckingAt?.Invoke(line) ?? false;
        if (_diagnosed.Add(dref))
        {
            string at = $"'{DataBinder.WrittenText(dref)}': {why}";
            if (checking) data.Edition.Warning(DiagnosticCatalog.RefModLiteralOutOfRangeChecked, at);
            else data.Edition.Error(DiagnosticCatalog.RefModLiteralOutOfRange, at);
        }
        return checking;
    }

    /// <summary>§8.5.1.12 — a variable-length group has a dynamic-length elementary item or a dynamic-capacity table
    /// as a subordinate (at any depth).</summary>
    internal static bool HasVariableLengthSubordinate(DataItem group)
    {
        foreach (var c in group.Children)
            if (c.IsDynamicLength || c.IsDynamicTable || (c.IsGroup && HasVariableLengthSubordinate(c))) return true;
        return false;
    }

    // ── The ONE reference-modification reader (ISO §8.4.3.3.2) ───────────────────────────────────────────────
    // A ref-mod reaches the binder as ONE parse node, `refModPart : (LPAREN | FNARG_LPAREN | REF_LPAREN) refModSpec
    // (RPAREN | FNARG_RPAREN | REF_RPAREN)` — three paren flavours because the lexer types the paren before it can see
    // the colon: REF_LPAREN straight after a data name (kb/Work PB2113 — this was a second carrier, a SUBSCRIPT-mode
    // token group with a depth-0 colon, until the mode was removed), FNARG_LPAREN straight after a ZERO-ARGUMENT
    // function name (§8.4.3.2.3 SR6's catalog precondition; PB48) and LPAREN after a ')'. Internal because the
    // intrinsic binder reads the SAME node for a ref-modified FUNCTION RESULT (§8.4.3.3.3 SR2, fix-queue PB8) — a
    // second copy of this reader there is exactly the one-rule-two-places defect PB4 was.

    /// <summary>Read a <c>refModPart</c>. Null when a start/length expression uses a form the segment renderer does not
    /// handle, so the caller fails loud rather than emitting a wrong slice — or on an OMITTED leftmost-position, which
    /// is reported HERE, in the one reader, so a sending operand, a receiving operand, an ADDRESS OF operand and a
    /// function result all name §8.4.3.3.2 (kb/Work PB1407, PB1458). The grammar admits the omission
    /// (<c>refModSpec : leftmost=functionArgument? COLON …</c>) precisely so that it reaches this report rather than
    /// a raw parse error.</summary>
    internal RefModSpec? ReadRefMod(Core.RefModPartContext rmp)
    {
        var spec = rmp.refModSpec();
        if (spec.leftmost is null)
        {
            // R30 purity: a probe never diagnoses (kb/Work PB157); one report per written modifier (_diagnosed).
            if (!_probing && _diagnosed.Add(rmp))
                data.Edition.Error(DiagnosticCatalog.RefModLeftmostPositionOmitted,
                    $"'{DataBinder.WrittenText(rmp).Trim()}': a reference modifier is written ( leftmost-position : "
                    + "[ length ] ) and only the length may be omitted, so the colon cannot lead (ISO §8.4.3.3.2). Write "
                    + "the leftmost position explicitly, for example (1:2).");
            return null;
        }
        if (BindPosition(new PositionSegment(LeafTokens(spec.leftmost), spec.leftmost), SegmentPosition.RefMod)
            is not { } rmStart) return null;
        Position? rmLen = null;
        if (spec.length is { } lengthExpr)
        {
            if (BindPosition(new PositionSegment(LeafTokens(lengthExpr), lengthExpr), SegmentPosition.RefMod)
                is not { } l) return null;
            rmLen = l;
        }
        // §7.3.23 / §8.4.3.3.4 item 5c: the ref-mod allows a zero-length result iff REF-MOD-ZERO-LENGTH is ON at
        // this site's source line (the group's compile-time directive fold; OFF everywhere by default).
        return new RefModSpec(rmStart, rmLen, data.RefModZeroLength.IsOnAt(rmp.Start.Line));
    }

    /// <summary>The suffixes written on ONE data reference, read LEXICALLY — how many subscript lists and
    /// reference modifiers it carries, and, for a single ref-mod whose two positions are INTEGER LITERALS, their
    /// values.
    /// <para>This is the DATA-DIVISION read: the clauses that admit a reference-modified operand all restrict it
    /// to integer literals (ISO §13.18.16.3 SR4, §13.18.54.3 SR8, §13.18.57.3 SR10), so no expression renderer and
    /// no procedure-phase state are involved. A position that is anything else — a data-name, an arithmetic
    /// expression, a figurative constant — comes back <see cref="RefModSuffixes.NonLiteral"/>, for the CLAUSE's own
    /// syntax rule to reject with its own diagnostic.</para></summary>
    internal static RefModSuffixes ReadOperandSuffixes(Core.DataReferenceContext dref)
    {
        int refMods = 0, subscripts = 0;
        Core.RefModPartContext? first = null;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            // A qualification carries its OWN (subscriptPart | refModPart)* tail — `C IN G (1:3)` hangs the
            // ref-mod off the qualification, not off the base word.
            if (suffix.qualification() is { } q)
            {
                subscripts += q.subscriptPart().Length;
                foreach (var rp in q.refModPart()) { refMods++; first ??= rp; }
            }
            else if (suffix.refModPart() is { } rmp) { refMods++; first ??= rmp; }
            else if (suffix.subscriptPart() is not null) subscripts++;
        }
        if (refMods != 1) return new RefModSuffixes(refMods, subscripts, null, null, false);

        var (start, length, ok, beyond) = ReadLiteralPositions(first!);
        return ok && beyond
            ? new RefModSuffixes(refMods, subscripts, null, null, false, BeyondHostLimit: true)
            : new RefModSuffixes(refMods, subscripts, ok ? start : null, ok ? length : null, !ok);
    }

    /// <summary>A <c>refModPart</c> as integer literals: each position shall be ONE integer literal (§13.18.16.3 SR4
    /// and its twins), so its whole text is the value or the read fails; an omitted length — §8.4.3.3.2's bracketed
    /// form — is the only omission permitted.
    /// ⛔ Read through THE ONE integer-literal reader, <see cref="CobolNet.Validation.IntegerOperandRules.TryHostValue(string, out int, out bool)"/>
    /// (kb/Work PB1579): an int.TryParse sent an 11-digit literal to the "not an integer literal" branch, a false
    /// sentence; the reader answers "integer literal, beyond the host range" (<c>Beyond</c>) instead.</summary>
    private static (int Start, int? Length, bool Ok, bool Beyond) ReadLiteralPositions(Core.RefModPartContext rmp)
    {
        var spec = rmp.refModSpec();
        if (spec.leftmost is null
            || !CobolNet.Validation.IntegerOperandRules.TryHostValue(spec.leftmost.GetText(), out int start, out bool b0))
            return (0, null, false, false);
        if (spec.length is null) return (start, null, true, b0);
        return CobolNet.Validation.IntegerOperandRules.TryHostValue(spec.length.GetText(), out int len, out bool b1)
            ? (start, len, true, b0 || b1) : (0, null, false, false);
    }

    /// <summary>
    /// Build the typed <see cref="Place"/> for an already-resolved <paramref name="item"/> with its subscript index
    /// expressions, honoring the REDEFINES machinery (COBOLNET_DESIGN §3.4 / §4.2):
    /// <list type="bullet">
    ///   <item>a Tier-B (string-canonical) view → a <see cref="RedefViewPlace"/> (offset,width) window over the
    ///         class's ONE string backing (the canonical too, so exactly one stored member);</item>
    ///   <item>a Tier-A (alias) view → the canonical's ONE stored field, reinterpreted through the view's own
    ///         Pic/scale/profile (the place carries the VIEW's <see cref="DataItem"/>);</item>
    ///   <item>a level-66 RENAMES entry → data-name-2's own place (no THROUGH) or a <see cref="RenamesPlace"/>
    ///         composed over the spanned leaves (THROUGH) — <see cref="PlaceForRenames"/>;</item>
    ///   <item>any other item → a plain <see cref="MemberPlace"/>.</item>
    /// </list>
    /// Returns <see langword="null"/> for a form not handled in this slice — a subscripted Tier-B view, a whole-OCCURS
    /// reference, a not-yet-wired Tier-C / Rejected view, or a subscript-count mismatch — so the caller fails loud.
    /// This is the ONE item→<see cref="Place"/> builder: both the syntactic <see cref="Resolve"/> path and the by-item
    /// <see cref="ResolveItem"/> path go through it, so EVERY consumer (verb operands, level-88 / SET conditional
    /// variables, FD record areas) sees identical view resolution.
    /// </summary>
    private Place? PlaceForItem(DataItem item, IReadOnlyList<Position> indexExprs) => PlaceForItem(item, indexExprs, out _);

    /// <param name="gap">On a null return, the <see cref="DeferredShape"/> this builder has not built — or none,
    /// with the entry whose declaration was refused: a REJECTED (Tier D) REDEFINES view (kb/Work PB1030), or a
    /// level-66 RENAMES entry or the refused entry behind it (kb/Work PB1380).</param>
    private Place? PlaceForItem(DataItem item, IReadOnlyList<Position> indexExprs, out PlaceGap gap)
    {
        gap = new(null, item);
        // A level-66 RENAMES entry (ISO §13.18.45) is a data item of its own that owns no storage — its place is
        // composed from the places of the items it renames (kb/Work PB1380: it is built HERE, in the one builder,
        // so every caller — a reference-modified alias above all — receives it the same way).
        if (item.Renames is { } ren) return PlaceForRenames(item, ren, indexExprs, out gap);
        // A WHOLE (unsubscripted) OCCURS DYNAMIC table reference has no element access — it would otherwise fold to a
        // MemberPlace wrapping the bare CobolDynTable<T> object, which is uncompilable in any value context. Fail
        // LOUD here (data-model D9); FUNCTION LENGTH and other whole-table operations route to a dedicated
        // DynWholeTablePlace in a later increment. A SUBSCRIPTED dynamic element (indexExprs non-empty) is inc 3's
        // access path and is NOT caught by this guard.
        if (item.IsDynamicTable && indexExprs.Count == 0) { gap = new(DeferredShape.DynamicWholeTable, item); return null; }
        if (item.Class is { Tier: RedefinesTier.StringCanonical } sc)
        {
            // The backing is emitted in the canonical's containing struct (FieldEmitter.PhysicalFields), so a NESTED
            // class's backing must be reached through that struct's access path — a bare `_redef_X` resolves only for a
            // top-level (static-field) class.
            // ⛔ A CLASS WHOSE CANONICAL LIES WITHIN AN OCCURS IS REACHED THROUGH THE SUBSCRIPTED PARENT PATH (kb/Work
            // PB1279). ISO §13.18.44.3 SR5 sentence 2 — "However, data-name-2 may be subordinate to an item whose
            // data description entry contains an OCCURS clause" — and §13.18.44.4 GR1 associates the redefining
            // item's storage with the first bit of the redefined item IN EACH OCCURRENCE, so every occurrence of the
            // enclosing table holds its own backing, in that element's struct. §8.4.2.3.3 SR3 gives the reference one
            // subscript per OCCURS clause: the LEADING ones select the enclosing element (they are the canonical
            // parent's own dimensions), the trailing ones are the in-class levels below.
            int outerCount = sc.Canonical.Parent?.SubscriptArity ?? 0;
            // A SUBSCRIPTED view: each OCCURS level on the item's path WITHIN the class displaces the window by
            // (occurrence − 1) × that level's per-occurrence width — the redefined table lays its occurrences
            // end-to-end in the ONE backing (ISO §13.18.44). ClassOffset is the occurrence-1 position; subscripts
            // map to the in-class OCCURS levels outer→inner, exactly as in AccessPath.
            var levels = SubscriptLevelsWithin(item, sc);
            if (indexExprs.Count != outerCount + levels.Count) { gap = new(DeferredShape.UnbuiltAccessPath, item); return null; }   // an item-path caller's count
            if (BuildBackingPath(sc, [.. indexExprs.Take(outerCount)]) is not { } classBacking) { gap = new(DeferredShape.NestedClassBacking, item); return null; }
            indexExprs = [.. indexExprs.Skip(outerCount)];
            if (WindowScopeOf(classBacking, BuildCellPath(sc), levels, indexExprs, CellOrdinalBase(sc)) is not { } scope) { gap = new(DeferredShape.UnbuiltAccessPath, item); return null; }
            // A dynamic-capacity table referenced by its subscript IS its element, at offset zero of the element cell.
            Position origin = new PositionConstant(item.IsDynamicTable ? 0 : item.ClassOffset);
            // A BASED class's window is displaced by the data-address pointer's runtime offset (ISO §13.18.5
            // — the view addresses wherever the pointer currently points; Phase-4b increment 2). The backing
            // property renders FIRST in both Read and Write, so the Deref null/bounds traps (GR3/GR4) fire
            // before the null-lenient OffsetOf. Only the record SCOPE is displaced: a dynamic-capacity table's element
            // cell (an area formal may hold one — kb/Work PB2094) has offsets of its own; its component ordinals are
            // displaced the same way (CellOrdinalBase, inside the scope walk).
            Position? based = null;
            if (sc.BasedPointerField is { } addr && !scope.Nested)
            {
                based = new PositionPointerOffset(addr);
                origin = new PositionBinary(based, PositionOperator.Add, origin);
            }
            var offset = new PositionOffset(origin, scope.Terms);
            // (whole-group image analysis moved OUT of resolve to the post-bind UsageCollectionPass, PHASE-05 Step 5)
            // A class-tier GROUP holding an occurs-depending table is an ODO operand exactly like a struct group
            // (kb/Work PB80: a BASED record — string-canonical — sent its MAXIMUM image; §13.18.38.4 GR8 does not
            // care how the group is stored). ONE wrap rule for both storage shapes.
            Position? dynOrdinal = scope.Cell is null ? null : scope.OrdinalAt(item.IsDynamicTable ? 0 : item.ClassDynOrdinal);
            return WrapIfOdoGroup(RedefViewPlace.For(scope.Backing, item, offset, based, scope.BitTerms, scope.Cell, dynOrdinal), item);
        }
        // A Tier-A view forwards to the canonical (a numeric view reinterprets the shared unscaled value via its own
        // scale, for free). A not-yet-wired (Tier-C) / Rejected view is loud.
        if (item.Class is { } cls && !item.IsCanonical && cls.Tier != RedefinesTier.Alias)
            return null;   // Tier D (Rejected) — refused at the declaration; the gap names the item itself
        DataItem accessItem = item.Class is { Tier: RedefinesTier.Alias } ac && !item.IsCanonical
            ? ac.Canonical : item;
        // A subscripted element whose access path crosses an OCCURS DYNAMIC level (data-model D9): the sending and
        // receiving accessors differ (RefSending vs RefReceiving — the latter grows-and-seeds), so build BOTH paths
        // and return a direction-carrying DynTablePlace. (A dynamic element is never an ODO subject, and a group
        // containing a dynamic table is not image-capable, so the ODO-wrap / whole-group paths below do not apply.)
        for (DataItem? n = accessItem; n is not null; n = n.Parent)
            if (n.IsDynamicTable)
            {
                if (BuildAccessPath(accessItem, indexExprs, OdoReferenceCheckFor) is not { } dynPath) { gap = new(DeferredShape.UnbuiltAccessPath, item); return null; }
                return new DynTablePlace(dynPath, item);
            }
        // An unsubscripted reference to an OCCURS table (whole-table op) is a later slice → AccessPath null → loud.
        if (BuildAccessPath(accessItem, indexExprs, OdoReferenceCheckFor) is not { } path) { gap = new(DeferredShape.UnbuiltAccessPath, item); return null; }
        // (Resolving a group no longer mutates WholeGroupReferenced — the "which groups are whole-image operands"
        // analysis is the post-bind UsageCollectionPass, which walks the BOUND tree and collects ONLY true
        // whole-group operands, not every RESOLVED group. PHASE-05 Step 5, §14.9.25.4 MOVE GR4.)
        return WrapIfOdoGroup(new MemberPlace(path, item), item);
    }

    /// <summary>The place of a level-66 RENAMES entry (ISO §13.18.45) — the <see cref="PlaceForItem"/> arm for an
    /// item that owns no storage of its own. The no-THROUGH form forwards to data-name-2's place (GR1); the
    /// THROUGH form composes a <see cref="RenamesPlace"/> over the spanned storage parts (GR2): string-valued leaves
    /// as they are and every numeric leaf through its storage image (<see cref="SpanLeafPlace"/>).</summary>
    private Place? PlaceForRenames(DataItem alias, RenamesInfo ren, IReadOnlyList<Position> indexExprs, out PlaceGap gap)
    {
        gap = new(null, alias);
        // A level-66 entry is never a table element, so a written subscript was refused by §8.4.2.3.3 SR2 before
        // the syntactic path got here (ScreenSubscriptArity); an item-path caller's count is the same mismatch
        // every other item reports.
        if (indexExprs.Count > 0) { gap = new(DeferredShape.UnbuiltAccessPath, alias); return null; }
        // A RENAMES entry whose operands did not resolve was refused at its declaration (COBOLNET1655,
        // §13.18.45.3), and the data binder leaves it with no From and an empty Span.
        if (ren.Thru is null ? ren.From is null : ren.Span.Count == 0) return null;
        // The no-THRU form is an ALIAS: §13.18.45.4 GR1 — "all of the data attributes of data-name-2 become the
        // data attributes of data-name-1 and the storage area occupied by data-name-2 becomes the storage area
        // occupied by data-name-1". Attributes AND storage forward to the renamed item's place (numeric stays
        // numeric: NC252A's ADD 3500 TO RENAME-12 over a PIC 9(4); a group forwards as the group), and only the
        // THRU form composes an alphanumeric span (GR2).
        // ⛔ BUT THE IDENTITY DOES NOT FORWARD (kb/Work PB602). GR1 shares the attributes and the storage, not the
        // NAME: data-name-1 is a data item of its OWN, and a bare forward made a reference to the 66
        // indistinguishable from a reference to the renamed item — so `START RLF KEY IS = RK-ALIAS` passed
        // §14.9.41.3 SR5's "shall be the data item specified in the RELATIVE KEY clause" that its THROUGH sibling
        // (a RenamesPlace, which keeps its own Item) was correctly refused by. `DenotesAs` records the alias on the
        // renamed item's own place, so nothing about the ACCESS changes and only the identity question answers
        // differently.
        if (ren.Thru is null)
            return PlaceForItem(ren.From!, [], out gap) is { } fwdPlace ? fwdPlace with { DenotesAs = alias } : null;
        var leafPlaces = new List<Place>(ren.Span.Count);
        var widths = new List<int>(ren.Span.Count);
        foreach (var part in ren.Span)
        {
            var leaf = part.Leaf;
            if (part.Occurrence is { } occIdx)
            {
                // ONE occurrence (or the one-and-only cell) of the leaf, possibly a partial slice of it (kb/Work
                // PB96): the cell's storage image, then a BYTE slice of it when the part does not cover the whole cell
                // — bytes, because the cell's image is its storage bytes (SpanLeafPlace), so a slice that splits a
                // national character is as addressable as any other (kb/Work PB2466).
                if (SpanLeafPlace(leaf, part.SubscriptsFor(leaf.Occurs is null ? null : occIdx), out gap) is not { } cell)
                    return null;
                if (part.IsPartial) cell = new RefModPlace(cell, new PositionConstant(part.StartByte), new PositionConstant(part.LengthBytes));
                leafPlaces.Add(cell);
                widths.Add(part.LengthBytes);   // STORAGE bytes — the unit the alias's image is in (kb/Work PB1665)
                continue;
            }
            // An OCCURS leaf inside the span contributes EVERY occurrence in order (§13.18.45 — the alias covers
            // the whole fixed-size area; NC252A's RENAME-7 over TABLE-ITEM-2 OCCURS 5). A leaf under an OCCURS GROUP is
            // one part per occurrence of the group (RenamesSpanPart.Outer, kb/Work PB986), so its cell is addressed by
            // those subscripts first.
            int occ = leaf.Occurs ?? 1;
            for (int k = 1; k <= occ; k++)
            {
                if (SpanLeafPlace(leaf, part.SubscriptsFor(leaf.Occurs is null ? null : k), out gap) is not { } lp) return null;
                widths.Add(leaf.ByteWidth);   // a whole part: every occurrence, at the leaf's storage width (kb/Work PB96, PB1665)
                leafPlaces.Add(lp);
            }
        }
        return new RenamesPlace(leafPlaces, alias, widths);
    }

    /// <summary>One spanned leaf cell of a RENAMES THROUGH alias, as the STORAGE image the composed alias
    /// concatenates: a string-valued leaf as it is, a typed NUMERIC leaf through its <see cref="NumericImagePlace"/>
    /// — the bytes it occupies, zoned digits for usage DISPLAY, radix-2 / BCD bytes for BINARY / PACKED, the IEEE
    /// window for a float (V59; the alias is an alphanumeric group item over the storage, §13.18.45.4 GR2 — NC252A's
    /// PIC 999 leaves under RENAMES-TEST-1). Null with the gap when the cell has no place.
    /// <para>⛔ EVERY NUMERIC USAGE, NOT ONLY THE CHARACTER FORMS (kb/Work PB1054). GR2 defines data-name-1 as "an
    /// alphanumeric group item that includes all elementary items" of the range, so a COMP member contributes its
    /// storage bytes exactly as it does to the record's own group image. This used to admit only
    /// <see cref="PicInfo.IsCharacterFormNumeric"/> leaves and DEFER the rest (a COBOLNET1756 warning and a run-time
    /// abort for <c>66 RN RENAMES RA THRU RC</c> over a <c>PIC 9(4) COMP</c> member), although the image place it
    /// declined renders every numeric usage through the ONE storage codec (<c>NumFormatImage</c> /
    /// <c>NumStoreImage</c>, and the float lane). Pointer, object and message-tag members never reach here: §13.18.45.3
    /// SR8 refuses them in the range at the declaration (<c>DataBinder.RenamesRangeFault</c>).</para>
    /// <para>⛔ A USAGE NATIONAL CELL IS ITS BYTES TOO (kb/Work PB1665, PB2466): its value carrier holds one character
    /// per national position, and the position occupies two storage bytes (D-N1), so the cell is composed through
    /// <see cref="NationalBytesPlace"/> — after the numeric image, for a usage-national numeric leaf whose image is
    /// national characters. The image is then in the unit every <see cref="RenamesSpanPart"/> is kept in, and a part
    /// that splits a national character is a byte slice of it like any other.</para></summary>
    private Place? SpanLeafPlace(DataItem leaf, IReadOnlyList<Position> indexExprs, out PlaceGap gap)
    {
        if (PlaceForItem(leaf, indexExprs, out gap) is not { } place) return null;
        bool stringValued = data.IsImageBackedEarly(leaf) || place is RedefViewPlace
            || leaf.Pic?.Category is not PicCategory.Numeric;
        var image = stringValued ? place : new NumericImagePlace(place);
        return leaf.Pic is { Usage: Usage.National } ? new NationalBytesPlace(image) : image;
    }

    /// <summary>A group whose subtree contains an occurs-depending table is an ODO operand (ISO §13.18.38 GR8): wrap
    /// it so the sending slice / receiving direction-split applies — whatever the group's storage shape (a record
    /// struct member, or a Tier-B / BASED class-tier window; kb/Work PB80). data-name-1 is resolved post-build,
    /// declared anywhere outside the table (SR20), and read at the operation site via CobolTable.Occ
    /// (storage-form-agnostic). Any other place passes through unchanged.</summary>
    private Place WrapIfOdoGroup(Place place, DataItem item) =>
        item.IsGroup && OdoModel.TableUnder(item) is { OccursSpec.Depending: { } dep } table
            && ResolveItem(dep) is { } depPlace
            ? OdoModel.WrapGroup(place, depPlace, item, table)
            : place;

    /// <summary>A <see cref="Place"/> for an already-resolved item with no subscripts (e.g. a level-88's conditional
    /// variable, a SET condition's variable, or an FD record area) — view-aware via <see cref="PlaceForItem"/>, so a
    /// REDEFINES view resolves to its window / canonical exactly as a verb operand does. <see langword="null"/> if the
    /// item is within an OCCURS table (a subscripted reference is then required) or is an unhandled view form.</summary>
    public Place? ResolveItem(DataItem item) => PlaceForItem(item, []);

    /// <summary>The <see cref="Place"/> of ONE OCCURRENCE of an already-resolved <paramref name="item"/> — one
    /// typed occurrence position per OCCURS level of its path, outermost first —
    /// through the same view-aware builder every verb operand uses. The caller states the occurrence; nothing is
    /// read from source. The Format-2 table SORT of a table in a shared-storage (REDEFINES) class sorts through
    /// these (kb/Work PB1175): each element and each key is a window the class's own offset law positions.
    /// <see langword="null"/> when the count does not match the item's <see cref="DataItem.SubscriptArity"/> or the
    /// item has no built place.</summary>
    internal Place? ResolveItemAt(DataItem item, IReadOnlyList<Position> indexExprs) => PlaceForItem(item, indexExprs);

    /// <summary>⛔ THE ONE RESOLUTION OF AN FD/SD's RECORD AREA (kb/Work PB355). ISO §13.18.33.4 GR3 — "Multiple
    /// level 1 entries subordinate to a FD or SD entry represent implicit redefinitions of the same area" — so
    /// the area is ONE place, and it is the LARGEST description's view (<see cref="FileModel.AreaRecord"/>): a
    /// shorter <c>Records[0]</c> window truncates the splice (RL106A's 56/102-char pair, ST111A's 50/75/100 SD).
    /// Null only for a file whose record area could not be resolved at all — §14.9.30.4 GR6's implied entry is
    /// materialized at bind time (kb/Work PB345), so no LEGAL file reaches a consumer with none.
    /// <para>It lives here because five consumers each wrote the same two-step —
    /// <c>file.AreaRecord is { } ar ? ResolveItem(ar) : null</c> — and START's own key comparand was built from
    /// data-name-1 instead of the area precisely because the area was not a thing a verb could simply ask
    /// for.</para></summary>
    public Place? RecordArea(FileModel file) => file.AreaRecord is { } area ? ResolveItem(area) : null;

    /// <summary>The place for an already-resolved <paramref name="item"/> using the SUBSCRIPTS of
    /// <paramref name="dref"/> — the condition-name-with-subscripts form (ISO §8.4.2.3 Format 2): a level-88
    /// reference's subscripts identify the occurrence of its CONDITIONAL VARIABLE. The same closed answer as
    /// <see cref="Resolve"/> (kb/Work PB1030), with a deferral put on the unbuilt ledger the same way.</summary>
    public RefResolution ResolveForItem(Core.DataReferenceContext dref, DataItem item)
    {
        bool savedSegment = _segmentDeferred;
        _segmentDeferred = false;
        try
        {
            // SR2 names the CONDITIONAL VARIABLE for a condition-name reference (§8.4.2.3 Format 2), which is
            // exactly the item this entry is handed — so the ONE subscript reading applies unchanged (kb/Work PB877).
            var answer = ReadSubscripts(dref, item, SubscriptGroupOf(dref), out var indexExprs)
                ?? (PlaceForItem(item, indexExprs, out var gap) is { } place
                    ? RefResolution.Resolved(place, "") : ItemFailure(dref, gap));
            if (answer.Outcome == RefOutcome.Deferred) data.Edition.NoteUnbuilt(answer.Feature);
            return answer;
        }
        finally { _segmentDeferred = savedSegment; }
    }

    /// <summary>⛔ THE WRITTEN SUBSCRIPT LIST OF <paramref name="dref"/> AGAINST <paramref name="item"/>, read ONCE for
    /// both commit entries (<see cref="Resolve"/> and <see cref="ResolveForItem"/> each carried a copy before kb/Work
    /// PB1030): §8.4.2.3.2's empty-parentheses screen, the segment renderer, §8.4.2.3.3 SR4's index-name
    /// association and the SR2/SR3/SR5 arity screen. Null when the list is acceptable (<paramref name="indexExprs"/>
    /// then holds the rendered subscripts); otherwise the non-place answer.</summary>
    /// <param name="tableSubject">True for the subject of a Format-2 table SORT (kb/Work PB1055): the table's OWN
    /// level is the one sorted and takes no subscript, so the reference writes the subscripts of the ENCLOSING
    /// tables only — <see cref="ScreenTableSubjectArity"/> — and §8.4.2.3.3 SR6's rightmost ALL, "equivalent to
    /// omitting the rightmost or only subscript in this context", is admitted and dropped.</param>
    private RefResolution? ReadSubscripts(Core.DataReferenceContext dref, DataItem item,
        Core.SubscriptPartContext? subCtx, out List<Position> indexExprs, bool tableSubject = false)
    {
        indexExprs = [];
        if (subCtx is null)
            return (tableSubject ? ScreenTableSubjectArity(dref, item, 0) : ScreenSubscriptArity(dref, item, 0))   // §8.4.2.3.3 SR5 — none written (kb/Work PB681)
                ? RefResolution.Refused(DataBinder.WrittenText(dref)) : null;
        if (ScreenEmptyParentheses(dref, subCtx))   // §8.4.2.3.2 / §8.4.3.3.2 (kb/Work PB969)
            return RefResolution.Refused(DataBinder.WrittenText(dref));
        List<IndexUse> ixNames = [];
        var e = InterpretSubscripts(subCtx, ixNames, tableSubject);
        if (e is null) return SegmentFailure(DataBinder.WrittenText(dref));   // a segment the materializer refused or deferred
        ScreenIndexNameAssociation(item, ixNames);   // §8.4.2.3.3 SR4 (kb/Work PB459)
        if (tableSubject ? ScreenTableSubjectArity(dref, item, e.Count) : ScreenSubscriptArity(dref, item, e.Count))   // §8.4.2.3.3 SR2/SR3 (kb/Work PB877)
            return RefResolution.Refused(DataBinder.WrittenText(dref));
        indexExprs = e;
        return null;
    }

    /// <summary>⛔ §8.4.2.3.3 SR3 / SR5 e) / SR6 FOR THE SUBJECT OF A FORMAT-2 TABLE SORT (kb/Work PB1055) — the
    /// written subscripts against the table's ENCLOSING levels. SR5 e) lets "the subject of a SORT statement that
    /// references a table" omit its subscripting, and SR6 spells the omission "ALL … as the rightmost or only
    /// subscript of a table in the table format of a SORT statement. This is equivalent to omitting the rightmost
    /// or only subscript in this context": what is omitted is the TABLE'S OWN level (the one the statement sorts),
    /// so the reference still writes one subscript for each enclosing table (SR3 — "the number of subscripts shall
    /// equal the number of OCCURS clauses", less the one SR5 e) lets go). Returns <see langword="true"/> when the
    /// reference is rejected. Before this screen the subscripts of the subject were never read at all, so
    /// <c>SORT E(2)</c> over a table inside <c>ROW OCCURS 2</c> bound to a deferral and an unsubscripted
    /// <c>SORT E</c> drew the same one: the legal and the illegal spelling were indistinguishable.</summary>
    private bool ScreenTableSubjectArity(Core.DataReferenceContext dref, DataItem table, int written)
    {
        int enclosing = table.SubscriptArity - 1;
        if (written == enclosing) return false;
        if (_probing) return true;   // R30 purity: a probe never diagnoses (kb/Work PB157)
        if (!_diagnosed.Add(dref)) return true;
        string subject = table.CobolName ?? table.CsName;
        data.Edition.Error(DiagnosticCatalog.SubscriptCountMismatch,
            $"'{DataBinder.WrittenText(dref)}': the table '{subject}' sits inside {enclosing} enclosing OCCURS "
            + $"clause{(enclosing == 1 ? "" : "s")}, so the SORT subject writes {enclosing} subscript"
            + $"{(enclosing == 1 ? "" : "s")}, outermost first, and none for '{subject}' itself — the level the "
            + $"statement sorts; ALL may stand as the rightmost one; {written} written (ISO §8.4.2.3.3 SR3, SR5 e), "
            + "SR6; §14.9.40.3 SR13).");
        return true;
    }

    /// <summary>The subscripts a Format-2 table SORT's data-name-2 writes for its ENCLOSING tables, one rendered
    /// index expression per level, outermost first (empty for a table that lies in no other table) — or the
    /// refusal. ISO §14.9.40.3 SR13: "Subscripting shall be specified in accordance with 8.4.2.3"; the rule that
    /// shape takes for a table subject is <see cref="ScreenTableSubjectArity"/>.</summary>
    internal RefResolution? ReadTableSubjectSubscripts(Core.DataReferenceContext dref, DataItem table,
        out List<Position> outerIndexExprs) =>
        ReadSubscripts(dref, table, SubscriptGroupOf(dref), out outerIndexExprs, tableSubject: true);

    /// <summary>⛔ A DATA REFERENCE FOLLOWED BY EMPTY PARENTHESES — <c>WS-X()</c> or <c>WS-X( )</c> — is neither
    /// form a parenthesis after a data-name can take (kb/Work PB969): §8.4.2.3.2 writes a subscript list as
    /// <c>( subscript … )</c>, at least one subscript, and §8.4.3.3.2 a reference modifier as
    /// <c>( leftmost-position : [ length ] )</c>, a required leftmost position. Returns <see langword="true"/> when
    /// the reference is rejected, so the caller returns null.
    /// <para>The GRAMMAR admits the empty group because the same parenthesis carries a keyword-omitted
    /// function-identifier's argument list, where §8.4.3.2.2 brackets the arguments inside the parentheses and
    /// <c>F()</c> is the zero-argument spelling — only the resolved symbol tells a function from a data item, and
    /// a name that reached THIS resolver resolved to a data item. Unscreened, <c>DISPLAY WS-X( )</c> compiled CLEAN
    /// and aborted at RUN time on NotImplementedCobolFeatureException while the receiving side drew COBOLNET0899's
    /// "not yet implemented". It is asked of the written GROUP, before the subscript interpreter.</para></summary>
    /// <summary>THE test for an empty written parenthesis group — no subscript list inside it (kb/Work PB969). One
    /// definition for every reader that must refuse <c>X()</c> on a data reference.</summary>
    internal static bool IsEmptyGroup(Core.SubscriptPartContext group) => group.subscriptList() is null;

    /// <summary>The COBOLNET2309 message body (the quoted reference is the caller's).</summary>
    internal const string EmptyParenthesesMessage = "parentheses with nothing inside them follow a data reference. A "
        + "subscript list writes at least one subscript (ISO §8.4.2.3.2 — \"( subscript … )\") and a reference "
        + "modifier a leftmost position (ISO §8.4.3.3.2); empty parentheses belong only to a function-identifier "
        + "with no arguments (ISO §8.4.3.2.2). Remove the parentheses, or write the subscript.";

    private bool ScreenEmptyParentheses(Core.DataReferenceContext dref, Core.SubscriptPartContext group)
    {
        if (!IsEmptyGroup(group)) return false;
        // R30 purity: a probe never diagnoses (kb/Work PB157); one report per written reference (_diagnosed).
        if (_probing) return true;
        if (!_diagnosed.Add(dref)) return true;
        data.Edition.Error(DiagnosticCatalog.EmptyParenthesesOnDataReference,
            $"'{DataBinder.WrittenText(dref)}': " + EmptyParenthesesMessage);
        return true;
    }

    /// <summary>⛔ §8.4.2.3.3 SR2 AND SR3 — THE WRITTEN SUBSCRIPT LIST AGAINST THE ITEM'S DIMENSIONS, asked ONCE
    /// per source reference and therefore on BOTH sides of every statement (kb/Work PB877). Returns
    /// <see langword="true"/> when the reference is rejected, so the caller returns null.
    /// <para><b>SR2</b> — "If a subscript is specified, the data description entry describing qualified-data-name-1
    /// or the conditional variable associated with qualified-condition-name-1 shall contain an OCCURS clause or
    /// shall be subordinate to a data description entry that contains an OCCURS clause" — is
    /// <see cref="DataItem.IsTableElement"/>. <b>SR3</b> — "the number of subscripts shall equal the number of
    /// OCCURS clauses in the description of the table element being referenced" — is
    /// <see cref="DataItem.SubscriptArity"/>.</para>
    /// <para>⛔ BOTH WERE DECIDABLE HERE AND NEITHER WAS DECIDED. The resolver returned a bare null on an arity
    /// mismatch and what the programmer saw depended on WHICH SIDE OF THE STATEMENT the reference stood on:
    /// <c>MOVE 1 TO PLAIN (1)</c> drew the receiving chokepoint's catch-all, COBOLNET0899 "a reference shape
    /// WiseOwl COBOL does not yet implement as a receiver" — a promise, about source no edition of the standard will
    /// ever admit (the PB489 shape) — while <c>DISPLAY PLAIN (1)</c> COMPILED CLEAN and aborted at run time on
    /// <c>NotImplementedCobolFeatureException</c>, where §4.2.2 requires a compile-time mechanism.</para>
    /// <para><b>SR5</b> — "Each table element reference shall be subscripted except when such reference appears"
    /// as a SEARCH subject, in a REDEFINES clause, in an OCCURS KEY IS phrase, in a SORT statement's KEY phrase or
    /// table subject, in a screen description entry's FROM/TO/USING phrase, or as a SUM clause addend. ⛔ NONE OF
    /// THOSE SEVEN CONTEXTS REACHES THIS SCREEN, which is why an OMITTED list is screened here too (kb/Work PB681):
    /// the SEARCH subject resolves through <see cref="ResolveTableOperand"/>, the SORT table subject and keys
    /// through the SORT binder's own data-description walk, and REDEFINES / OCCURS KEY / SUM through the data
    /// binder — never through <see cref="Resolve"/> or <see cref="ResolveForItem"/>, the procedure-division callers
    /// of this screen. (The third caller is the data division's constant entry, <c>CONSTANT AS LENGTH OF
    /// data-name-2</c> — DataBinder.BindConstantLength, kb/Work PB1016 — which is not an SR5 context either.)
    /// Before PB681 the omitted case was left to <see cref="PlaceForItem"/>, whose null NOBODY reported:
    /// <c>MOVE E TO B</c> over a table element compiled clean and aborted the run unit at the MOVE.</para></summary>
    internal bool ScreenSubscriptArity(Core.DataReferenceContext dref, DataItem item, int written)
    {
        // ⛔ THE SR5 BOUNDARY IS A PROPERTY OF THE ENTRY POINTS, and it is written down where they are: the seven
        // contexts that may omit a table element's subscripts resolve through their own entries (see the summary),
        // so every reference that reaches THIS screen is outside them and SR5 applies with no exception.
        int arity = item.SubscriptArity;
        if (written == arity) return false;
        // R30 purity: a probe never diagnoses (kb/Work PB157); one report per written reference (_diagnosed).
        if (_probing) return true;
        if (!_diagnosed.Add(dref)) return true;
        string text = DataBinder.WrittenText(dref);
        string subject = item.CobolName ?? item.CsName;
        if (written == 0)
            data.Edition.Error(DiagnosticCatalog.TableElementNotSubscripted,
                $"'{text}': '{subject}' is a table element — its description contains, or is subordinate to, "
                + $"{arity} OCCURS clause{(arity == 1 ? "" : "s")} — and ISO §8.4.2.3.3 SR5 requires \"Each table "
                + "element reference shall be subscripted\" outside seven contexts (a SEARCH subject, a REDEFINES "
                + "clause, an OCCURS KEY IS phrase, a SORT key or table subject, a screen FROM/TO/USING phrase, a "
                + $"SUM addend), none of which this is. Write {arity} subscript{(arity == 1 ? "" : "s")}, "
                + "outermost first.");
        else if (arity == 0)
            data.Edition.Error(DiagnosticCatalog.SubscriptOnNonTableItem,
                $"'{text}': a subscript is written on '{subject}', whose data description entry neither "
                + "contains an OCCURS clause nor is subordinate to one, so no subscript may be written on it "
                + "(ISO §8.4.2.3.3 SR2). For a character span write the reference-modification colon form "
                + $"instead — '{subject} (1:n)' (§8.4.3.3).");
        else
            data.Edition.Error(DiagnosticCatalog.SubscriptCountMismatch,
                $"'{text}': {written} subscript{(written == 1 ? "" : "s")} written, but the description of "
                + $"'{subject}' contains {arity} OCCURS clause{(arity == 1 ? "" : "s")}; ISO §8.4.2.3.3 SR3 "
                + "requires one subscript per OCCURS clause, written in the order of successively less inclusive "
                + "dimensions of the table.");
        return true;
    }

    /// <summary>The FIRST subscript group of <paramref name="dref"/> — the <c>subscriptPart</c> that carries the
    /// reference's subscript list, taken from the base word's own suffix or from a qualification's suffix tail (<c>K OF E (IX)</c> hangs it off the qualification). Shared by
    /// <see cref="ResolveForItem"/>, which renders it into index expressions, and by
    /// <see cref="SubscriptSegments"/>, which keeps it as written.</summary>
    internal static Core.SubscriptPartContext? SubscriptGroupOf(Core.DataReferenceContext dref) =>
        ReadWritten(dref).SubscriptGroup;

    /// <summary>A <c>dataReference</c> AS WRITTEN: the qualifier chain, the subscript group and the
    /// reference-modification carriers, exactly as the source spells them and before any name is looked up.
    /// <para>⛔ The <b>written</b> reference is a thing in its own right, and it is what a statement's own syntax
    /// rules are about. ISO §14.9.37.3 SR1 ("Identifier-1 shall not be reference-modified"), SR2 ("… shall not be
    /// subscripted at the level for which the SEARCH is applicable") and SR3 ("… contained within one or more
    /// other tables, for which the subscripting is still required") are three predicates over THIS, not over the
    /// resolved item and not over a rendered access path — which is why SEARCH, which read only
    /// <c>cobolWord()</c>, could enforce none of them and searched whichever table was declared first
    /// (kb/Work PB443).</para></summary>
    /// <param name="Written">The OF/IN qualifiers in written order, inner → outer, or null when the reference
    /// carries none — the common case, which therefore allocates nothing.</param>
    /// <param name="SubscriptGroup">The first <c>subscriptPart</c>: the reference's subscript list. Taken from the
    /// base word's own suffix or from a qualification's suffix tail (<c>K OF E (IX)</c> hangs it off the
    /// qualification).</param>
    /// <param name="RefModPart">The first <c>refModPart</c> (<c>leftmost : length</c> as expressions).</param>
    /// <param name="RefModCount">How many reference modifications the whole reference carries — §8.4.3.3.3 SR3
    /// admits at most one, and the count is the only way to see a second one.</param>
    /// <param name="PropertyObject">The non-word object the qualifier chain ends in (<c>OF SELF</c>, <c>OF SUPER</c>,
    /// <c>OF U AS C</c>, <c>OF FUNCTION F</c>, <c>OF NULL</c>) — §8.4.3.1.2 Format 7's identifier-3 where a word cannot
    /// spell it (kb/Work PB1425).</param>
    internal readonly record struct WrittenReference(
        List<string>? Written,
        Core.SubscriptPartContext? SubscriptGroup,
        Core.RefModPartContext? RefModPart,
        int RefModCount,
        Core.PropertyObjectContext? PropertyObject)
    {
        /// <summary>The shared empty qualifier list for an unqualified reference. Read-only by contract: every
        /// consumer (<see cref="ResolveQualified"/>, <see cref="ReportUnidentified"/>) only enumerates it.</summary>
        private static readonly List<string> NoQualifiers = [];

        /// <summary>The OF/IN qualifiers, inner → outer; empty for an unqualified reference.</summary>
        public List<string> Qualifiers => Written ?? NoQualifiers;

        /// <summary>True when the reference carries a reference modification in any of its spellings.</summary>
        public bool IsReferenceModified => RefModCount > 0;
    }

    /// <summary>⛔ THE ONE DECOMPOSITION OF A <c>dataReference</c> AS WRITTEN (kb/Work PB443). Three resolutions
    /// each wrote this walk out for themselves — <see cref="ResolveImplCore"/>, <see cref="ResolveForAddressOf"/>
    /// and <see cref="SubscriptGroupOf"/> — and a fourth, SEARCH's identifier-1, wrote none at all and took
    /// <c>dref.cobolWord()</c>, the BASE WORD, so every suffix the programmer wrote was discarded unread. Reading
    /// a written reference is ONE job; the §8.4.2.2 lookup and every shape rule are predicates over the result.
    /// <para>Mirrors the grammar exactly: <c>dataReference : cobolWord dataReferenceSuffix*</c> with
    /// <c>dataReferenceSuffix : subscriptPart | refModPart | qualification</c>, and <c>qualification</c> carrying
    /// its OWN <c>(subscriptPart | refModPart)*</c> tail. ⛔ Counts REF-MODS ONLY: a subscript followed by a
    /// ref-mod (<c>T(I) (2:3)</c>) is the legal §8.4.3.1.4 GR1 a→g order and must stay untouched.</para></summary>
    internal static WrittenReference ReadWritten(Core.DataReferenceContext dref)
    {
        List<string>? qualifiers = null;
        Core.SubscriptPartContext? subCtx = null;
        Core.RefModPartContext? cleanRef = null;
        Core.PropertyObjectContext? propertyObject = null;
        int refModCount = 0;

        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } q)
            {
                (qualifiers ??= []).Add(q.cobolWord().Name());
                if (q.subscriptPart() is [var qs, ..]) subCtx ??= qs;
                refModCount += q.refModPart().Length;
                if (q.refModPart().Length > 0) cleanRef ??= q.refModPart()[0];
            }
            else if (suffix.refModPart() is { } rmp) { refModCount++; cleanRef ??= rmp; }
            else if (suffix.subscriptPart() is { } s) subCtx ??= s;
            else if (suffix.propertyObject() is { } po)
            {
                propertyObject ??= po;
                // §8.4.3.1.4 GR1 g): a reference modifier written after a function object's arguments applies to the
                // whole identifier, last — never to the object reference the function returns (kb/Work PB1425).
                foreach (var resultRef in ResultRefModsOf(po)) { refModCount++; cleanRef ??= resultRef; }
            }
        }
        return new WrittenReference(qualifiers, subCtx, cleanRef, refModCount, propertyObject);
    }

    /// <summary>The reference's subscript list AS WRITTEN — one token segment per subscript position, outermost
    /// first, read by the ONE <see cref="SegmentsOf"/> with the same declaration-informed '(' rule
    /// <see cref="InterpretSubscripts"/> uses (kb/Work PB136), or <see langword="null"/> when the reference carries
    /// no subscript group at all.
    /// <para>⛔ The RENDERED form cannot answer the question this exists for. ISO §14.9.37.3 SR8 and SR9 are rules
    /// about the SOURCE TEXT of a SEARCH ALL subscript — "shall be subscripted by the first index-name associated
    /// with identifier-1 … The index-name subscript shall not be followed by a '+' or a '–'" — and the C# index
    /// expression <see cref="InterpretSubscripts"/> produces has already erased both facts: an index-name and an
    /// integer data item of the same value render identically, and <c>IX + 1</c> folds into the arithmetic.</para></summary>
    internal List<PositionSegment>? SubscriptSegments(Core.DataReferenceContext dref) =>
        SubscriptSegmentsOf(ReadWritten(dref).SubscriptGroup);

    /// <summary>True when any subscript of <paramref name="dref"/>, AS WRITTEN, names the index-name
    /// <paramref name="index"/> — the ONE reader for the two SEARCH rules that prohibit it: §14.9.37.3 SR5's second
    /// sentence over the Format-1 VARYING identifier-2 ("shall not be subscripted by the first or only index-name
    /// specified in the INDEXED phrase … for identifier-1", kb/Work PB211) and SR10's over a Format-2 WHEN operand.
    /// Asked of the source text for the reason <see cref="SubscriptSegments"/> gives: the rendered subscript has
    /// already erased which name was written. Index-names are user-defined words, compared case-insensitively
    /// (§8.1.3.2 GR3 a)).</summary>
    /// <para>⛔ BY DECLARATION, NOT SPELLING (kb/Work PB919): with two tables each <c>INDEXED BY IX</c>, a
    /// subscript <c>IX OF OTHER</c> names the other table's index and is legal here, so each written occurrence of
    /// the spelling is resolved — with the OF/IN qualifiers that follow it — and compared by identity.</para>
    internal bool SubscriptNamesIndex(Core.DataReferenceContext dref, IndexDeclaration index)
    {
        if (SubscriptSegments(dref) is not { } segs) return false;
        foreach (var seg in segs.Select(s => s.Tokens))
            for (int i = 0; i < seg.Count; i++)
            {
                var t = seg[i];
                if (!IsNameToken(t) || !CobolNames.Same(t.Text, index.Name)) continue;
                var quals = QualifiersAfter(seg, i, out _);
                if (ResolveIndexName(t.Text, quals, t) is { Outcome: IndexRefOutcome.Resolved } ix
                    && ReferenceEquals(ix.Decl, index)) return true;
            }
        return false;
    }

    /// <summary>The <see cref="SubscriptSegments"/> split over an ALREADY-READ subscript group (the caller has
    /// the <see cref="WrittenReference"/> in hand and need not walk the suffix tail a second time).</summary>
    private List<PositionSegment>? SubscriptSegmentsOf(Core.SubscriptPartContext? group) =>
        group is null ? null : SegmentsOf(group, CannotBeSubscripted);

    /// <summary>⛔ THE ONE DECLARATION-INFORMED <c>'('</c> PREDICATE (kb/Work PB136, corrected by PB877) — the
    /// segmenter's question "does this name own the <c>'('</c> that follows it, or does that paren open a new
    /// subscript?", answered by §8.4.2.3.3 SR2: a name that may carry a subscript owns its paren; a name that may
    /// not cannot, so the paren can only begin a parenthesized-expression subscript.
    /// <para>⛔ IT IS A NAMED MEMBER, NOT A LAMBDA AT EACH CALL SITE, AND THAT IS THE POINT. Both callers —
    /// <see cref="SubscriptSegments"/> (the §14.9.37.3 SR8/SR9 source-text reader) and
    /// <see cref="InterpretSubscripts"/> (the rendering path) — spelled the lambda out for themselves, and
    /// <see cref="SubscriptSegments"/>'s own doc-comment CLAIMED they used "the same declaration-informed '(' rule".
    /// They did, by coincidence of two identical copies; PB877 corrected one of them and the claim would have
    /// become false in silence. One rule, one place, and the claim is now structural.</para>
    /// <para>Answers true ONLY for a name that RESOLVES to a data item on which no subscript may be written.
    /// An UNRESOLVED name answers false — it may be a function reference (<c>FUNCTION INTEGER (X)</c>), whose
    /// argument list must not be split from it — and so does a table element, which owns its paren.</para>
    /// <para>⛔ ASKED OF THE WHOLE QUALIFIED NAME (kb/Work PB1455's sibling sweep). §8.4.2.3.2 hangs the subscript list
    /// off <c>qualified-data-name-1</c>, so in <c>X (E OF T (1))</c> the paren belongs to <c>E OF T</c>; asking the
    /// LAST word alone asked about <c>T</c> — a group that carries no OCCURS — split <c>(1)</c> off as a second
    /// subscript of X, and refused the legal reference COBOLNET2270 ("E OF T … shall be subscripted").</para></summary>
    internal bool CannotBeSubscripted(string name, List<string> qualifiers) =>
        (qualifiers.Count > 0 ? ResolveQualified(name, qualifiers) : ResolveUnqualified(name)) is { IsTableElement: false };

    // ── Intrinsic-argument entries (ISO §15.3; consumed by IntrinsicBinder) ─────────────────────────────────
    // The table(ALL) argument resolves its name and renders its non-ALL subscripts itself (it builds an enumeration,
    // not one place) — these thin internal entries expose the SAME private resolution (ResolveUnqualified /
    // ResolveQualified → PlaceForItem → BindPosition) so argument references see identical view/qualification/
    // subscript semantics as every verb operand (singular-pattern rule).

    /// <summary>Resolve a (possibly OF/IN-qualified) data-name to its <see cref="DataItem"/>, or null. Used by
    /// the table(ALL) expansion (§15.3) to read the OCCURS counts before building per-occurrence places.</summary>
    internal DataItem? FindItem(string name, IReadOnlyList<string> qualifiers) =>
        qualifiers.Count == 0 ? ResolveUnqualified(name)
            : ResolveQualified(name, qualifiers as List<string> ?? [.. qualifiers]);

    /// <summary>A reference written where a general format prints a <b>table</b> operand, resolved and read: the
    /// <see cref="DataItem"/> the qualifiers single out, plus the two facts the statement's own syntax rules are
    /// about — how many subscripts were written, and whether a reference modification was.</summary>
    /// <param name="Item">The item the §8.4.2.2 candidate-set match singles out.</param>
    /// <param name="SubscriptCount">Subscripts AS WRITTEN, 0 for an unsubscripted reference.</param>
    /// <param name="IsReferenceModified">True when the reference carries a reference modification.</param>
    public readonly record struct TableOperand(DataItem Item, int SubscriptCount, bool IsReferenceModified);

    /// <summary>⛔ THE ONE RESOLUTION OF A REFERENCE WRITTEN WHERE A GENERAL FORMAT PRINTS A <b>TABLE</b> OPERAND
    /// (kb/Work PB443) — SEARCH's and SEARCH ALL's identifier-1, the Format-2 SORT's data-name-2 and, for the same
    /// reason, a SORT/MERGE KEY (kb/Work PB1173): a statement whose rule is about the NAMED ITEM — whether it may be a
    /// key at all — asks what the name denotes before any occurrence of it is resolved. It is deliberately NOT <see cref="Resolve"/>: a
    /// table operand names the TABLE, not one occurrence of it, and ISO §14.9.37.3 SR2 says identifier-1 "shall
    /// not be subscripted at the level for which the SEARCH is applicable" — so there is no subscript for the
    /// searched level and therefore no <see cref="Place"/> to build. What such a statement needs is the data item
    /// plus the reference AS WRITTEN.
    /// <para>⛔ THE QUALIFICATION IS THE ORDINARY ONE, AND THAT IS THE WHOLE POINT. Before this, SEARCH reduced
    /// identifier-1 to <c>drefs[0].cobolWord()</c> — its BASE WORD — and took
    /// <c>candidates.FirstOrDefault(i =&gt; i.IsTable)</c>, so with two groups each declaring a table <c>E</c>,
    /// <c>SEARCH E IN G2</c> searched whichever <c>E</c> was declared FIRST: a wrong answer on legal, unambiguous
    /// COBOL, decided by declaration order, with the qualifier that would have settled it thrown away unread
    /// (§8.4.2.2 — qualification establishes uniqueness; §14.9.37.4 GR1 — the search varies the first index
    /// associated with identifier-1, and for a qualified reference identifier-1 is the QUALIFIED table).</para>
    /// <para>Reporting posture — the DEMANDING one of <see cref="Resolve"/>: a name that identifies no item, or
    /// that qualification cannot single out, draws COBOLNET1639 here, so a null the caller gets back is already
    /// reported (<see cref="WasDiagnosed"/> answers true). A SPECIAL REGISTER returns null having reported
    /// NOTHING — it identifies a resource, just not one with a data description, and the verdict that belongs to
    /// it is the statement's own operand rule, written in the one check catalog.</para></summary>
    public TableOperand? ResolveTableOperand(Core.DataReferenceContext dref)
    {
        DataReferenceCst r = dref;
        if (r.Register != SpecialRegister.None || r.BaseName is not { } name) return null;
        var written = ReadWritten(dref);
        if (FindItem(name, written.Qualifiers) is not { } item)
        {
            ReportUnidentified(dref, name, written.Qualifiers);
            return null;
        }
        // The subscripts AS WRITTEN, split by the ONE declaration-informed splitter (kb/Work PB136) — the same
        // reading SR8/SR9 already demand of a Format-2 key subscript, for the same reason: the RENDERED index
        // expression has erased how many operands the programmer wrote.
        return new TableOperand(item, SubscriptSegmentsOf(written.SubscriptGroup)?.Count ?? 0,
            written.IsReferenceModified);
    }

    /// <summary>Build the <see cref="Place"/> for a by-name reference with ALREADY-RENDERED C# index expressions
    /// (one per OCCURS level, outermost first). A level-66 RENAMES entry is built by the same
    /// <see cref="PlaceForItem"/> as every other item (kb/Work PB1380); it is never a table element, so a written
    /// subscript on one builds no place.</summary>
    internal Place? ResolveByName(string name, IReadOnlyList<string> qualifiers, IReadOnlyList<Position> indexExprs) =>
        FindItem(name, qualifiers) is { } item ? PlaceForItem(item, indexExprs) : null;

    /// <summary>Bind one written subscript to its typed <see cref="Position"/> (the private
    /// <see cref="BindPosition"/>), or null when it cannot be bound (caller fails loud).
    /// <para>⛔ <paramref name="indexNames"/> IS NOT OPTIONAL (kb/Work PB1472): a caller that renders a subscript
    /// owes §8.4.2.3.3 SR4's association screen (<see cref="ScreenIndexNameAssociation"/>) for the index-names it
    /// wrote, and the collector is how the screen sees them. The only caller outside the resolver, the table(ALL)
    /// intrinsic argument, passed none, so `FUNCTION SUM(E2(IXA, ALL))` with IXA declared on another table compiled
    /// clean while `E2(IXA, 1)` was refused — the second arm of one subscript reading.</para></summary>
    internal Position? BindIndexSegment(PositionSegment segment, List<IndexUse> indexNames) =>
        BindPosition(segment, SegmentPosition.Subscript, indexNames);

    /// <summary>Resolve an <c>ADDRESS OF</c> operand (ISO §8.4.3.11) to its item plus the OCCURS displacement
    /// of its subscripts — the <c>(idx − 1) × width</c> character-position terms (<see cref="OffsetTerm"/>) within
    /// the item's storage class, or a null displacement for an unsubscripted (possibly OF/IN-qualified) reference. The
    /// address of occurrence k is the class cell displaced by the SAME in-class occurrence law the Tier-B view
    /// window uses (<see cref="PlaceForItem"/> — a table lays its occurrences end-to-end in the ONE cell
    /// image), so the two share one formula, <see cref="PositionOffset"/>.
    /// <para>⛔ A REFERENCE-MODIFIED OPERAND IS LEGAL (kb/Work PB1407): §8.4.3.11.3 SR4 a) itself speaks of
    /// "subscripting and reference modification in identifier-1", and identifier-1 is a general identifier. It goes
    /// through the ONE ref-mod admission <see cref="ReadScreenedRefMod"/> that <see cref="Resolve"/> uses (SR3
    /// count, SR1 exclusion, the spec, the literal range screen); the address is that of its leftmost position,
    /// which the caller adds to the item's.</para>
    /// Null overall = an unresolvable name, a subscript-count mismatch, or a reference refused by one of those
    /// screens — the caller reports loud, never a wrong address.</summary>
    internal AddressOfOperand? ResolveForAddressOf(Core.DataReferenceContext dref)
    {
        DataReferenceCst r = dref;
        if (r.Register != SpecialRegister.None || r.BaseName is not { } name) return null;
        var written = ReadWritten(dref);                  // the ONE decomposition (kb/Work PB443)
        if (!ScreenRefModCount(dref, written, name)) return null;
        var qualifiers = written.Qualifiers;
        var subCtx = written.SubscriptGroup;
        if (FindItem(name, qualifiers) is not { } item) return null;
        // §8.4.2.3.3 SR2/SR3/SR5 through the ONE screen the other two entries use (kb/Work PB681): an ADDRESS OF
        // operand is an identifier like any other, and the omitted-list arm used to return the address of the
        // table's FIRST occurrence for `ADDRESS OF E` — no subscript, no diagnostic, a pointer nobody asked for.
        // The ONE written-subscript reading every identifier entry shares (kb/Work PB1030 extracted it).
        if (ReadSubscripts(dref, item, subCtx, out var exprs) is not null) return null;
        RefModSpec? refMod = null;
        Place? bitPlace = null;
        OdoGroupPlace? extent = null;
        if (written.IsReferenceModified || BitLayout.IsBitItem(item))
        {
            var inner = PlaceForItem(item, exprs);
            if (written.IsReferenceModified)
            {
                if (ReadScreenedRefMod(dref, written, inner?.Item ?? item, out _) is not { } spec) return null;
                refMod = spec;
                // §8.4.3.3.4 GR5 / 5): the area a position is tested against is identifier-1's CURRENT size, and an
                // occurs-depending group's is a run-time value (§13.18.38.4 GR8) — the same extent a read of the
                // reference measures through its sending image (kb/Work PB1969).
                extent = inner as OdoGroupPlace;
            }
            if (BitLayout.IsBitItem(item)) bitPlace = inner;
        }
        if (subCtx is null) return new AddressOfOperand(item, null, refMod, bitPlace, extent);
        // The in-class OCCURS levels outer→inner — the PlaceForItem Tier-B walk (same layout, same formula).
        var occursLevels = new List<DataItem>();
        bool underDynamicTable = false;
        for (DataItem? n = item; n is not null && ReferenceEquals(n.Class, item.Class); n = n.Parent)
        {
            if (n.Occurs is not null) occursLevels.Add(n);
            underDynamicTable |= n.IsDynamicTable;
        }
        // SR6's refusal is the binder's, and it needs the operand: a dynamic level has no fixed stride to render.
        if (underDynamicTable) return new AddressOfOperand(item, null, refMod, bitPlace, extent);
        occursLevels.Reverse();
        if (occursLevels.Count != exprs.Count) return null;   // wrong subscript count → loud
        // ByteWidth, for the same reason as the PlaceForItem twin above: the class backing is byte-addressed
        // and a NATIONAL element strides two bytes per position (kb/Work PB231; §13.18.60.4 GR8 / D-N1).
        var disp = occursLevels.Select((lv, k) => new OffsetTerm(exprs[k], lv.ByteWidth)).ToList();
        return new AddressOfOperand(item, disp, refMod, bitPlace, extent);
    }

    /// <summary>What an <c>ADDRESS OF identifier-1</c> operand (ISO §8.4.3.11) resolves to: the item, the OCCURS
    /// displacement of its subscripts, the reference modification when one is written, and, for a bit item, the
    /// place its §8.4.3.11.3 SR4 alignment proof walks.</summary>
    /// <param name="CurrentExtent">The occurs-depending group place whose CURRENT extent bounds a written reference
    /// modifier (§8.4.3.3.4 5) — "a position outside the area of identifier-1", the area being the group's current
    /// size under §13.18.38.4 GR8); null for every operand whose size is a compile-time fact.</param>
    /// <param name="Item">The data item the reference names (the table ELEMENT's item for a subscripted one).</param>
    /// <param name="OccursDisplacement">The <c>(idx − 1) × width</c> byte terms within the item's storage class, or null
    /// for an unsubscripted reference — and for an item at or under a dynamic-capacity table, which §8.4.3.11.3 SR6
    /// refuses before any address is formed (no occurrence of one has a fixed stride).</param>
    /// <param name="RefMod">The screened reference modifier, or null when none is written. §8.4.3.11.4 GR1's
    /// "address of identifier-1" is then the address of the unique data item reference modification creates
    /// (§8.4.3.3.4 GR5), i.e. of its leftmost position.</param>
    /// <param name="SubscriptedPlace">The item's place with its subscripts and WITHOUT the reference modification,
    /// built only for a bit item (SR4's proof is a walk over a place); null for any other item and for a shape the
    /// place builder cannot model, which the proof then accepts rather than reject what it cannot prove.</param>
    internal readonly record struct AddressOfOperand(
        DataItem Item, IReadOnlyList<OffsetTerm>? OccursDisplacement, RefModSpec? RefMod, Place? SubscriptedPlace,
        OdoGroupPlace? CurrentExtent);

    /// <summary>The STRUCTURAL access path to a Tier-B/Tier-C class's single stored backing field (the
    /// <see cref="RedefViewPlace"/> twin of the old string <c>BackingPath</c>). The backing is emitted in the
    /// canonical's containing struct, so a NESTED class reaches it through that struct's path
    /// (<c>OUTER.GROUP._redef_X</c>); a top-level class's backing is the bare static field (<c>_redef_X</c>). Returns
    /// <see langword="null"/> when the containing path is unavailable. A canonical within a FIXED table is reached
    /// through the enclosing element — <paramref name="outerIndexExprs"/> are the subscripts of the canonical
    /// parent's own dimensions, outermost first (ISO §13.18.44.3 SR5, kb/Work PB1279). A canonical within an OCCURS
    /// DYNAMIC table is reached the same way (kb/Work PB1933): the path's <see cref="DynTableSegment"/> renders its
    /// accessor from the direction <c>PlaceRenderer</c> renders the backing in — <c>RefSending</c> on a read,
    /// <c>RefReceiving</c> (grow-and-seed, §8.5.1.9.3) on a store into the window.</summary>
    private AccessPath? BuildBackingPath(RedefinesClass cls, IReadOnlyList<Position> outerIndexExprs)
    {
        if (cls.Canonical.Parent is not { } parent)
            return new AccessPath([new RootFieldSegment(cls.BackingCsName, OmittedFormalGuard.Of(cls.Canonical))]);
        // The enclosing element carries the same §13.18.38.4 GR7 OCCURS DEPENDING check every other element path does.
        return BuildAccessPath(parent, outerIndexExprs, OdoReferenceCheckFor) is { } parentPath
            ? parentPath.Add(new MemberSegment(cls.BackingCsName)) : null;
    }

    /// <summary>The STRUCTURAL access path to the <c>StorageCell</c> behind a CELL-BACKED class's backing —
    /// the second half of the one storage area, holding the MANAGED SLOTS a pointer-class member rides
    /// (kb/Work PB231; <see cref="SlotWindow"/>). Null for a plain REDEFINES class, which has no cell.
    /// <para>Always a bare root field: the three cell surfaces (EXTERNAL, ADDRESS OF, BASED) all force a
    /// LEVEL-1 record — §13.18.22.3 SR1 puts EXTERNAL only on "level 1 data description entries", the
    /// ADDRESS-OF forcer walks to the root before forcing, and a BASED entry is a 01/77 — so there is no
    /// containing struct to reach through, and a canonical with a parent would mean the cell property was
    /// never emitted.</para></summary>
    internal static AccessPath? BuildCellPath(RedefinesClass? cls) =>
        cls is { IsCellBacked: true, Canonical.Parent: null }
            // The cell is a reference-type StorageCell held in a READONLY field, so its guard passes it through
            // rather than taking it by ref (kb/Work PB971).
            ? new AccessPath([new RootFieldSegment(cls.BackingCellCsName,
                OmittedFormalGuard.Of(cls.Canonical) is { } g ? g with { CarrierPrefix = cls.BackingCellCsName } : null)])
            : null;

    /// <summary>⛔ THE COMPONENT-ORDINAL BASE OF A CELL-BACKED CLASS'S RECORD SCOPE (kb/Work PB2094), as the leading term of
    /// its ordinal displacement: a class laid over storage through its data-address pointer — a BASED item, an AREA formal
    /// (§14.2.3 GR8) — numbers its components from the pointer's <c>CellPointer.DynBase</c>, the ordinal twin of the
    /// <c>CobolPtr.OffsetOf</c> its character window is displaced by (an area formal over a subordinate variable-length
    /// group begins at that group's first component). Empty for a class that owns its cell.</summary>
    internal static Position? CellOrdinalBase(RedefinesClass cls) =>
        cls.BasedPointerField is { } addr ? new PositionPointerDynBase(addr) : null;

    /// <summary>The origin of a component ordinal: the item's static ordinal <paramref name="staticOrdinal"/> in its
    /// scope, displaced by the scope's run-time ordinal base (<see cref="CellOrdinalBase"/>) when it has one.</summary>
    internal static Position OrdinalOrigin(int staticOrdinal, Position? ordinalBase) =>
        ordinalBase is null ? new PositionConstant(staticOrdinal)
                            : new PositionBinary(new PositionConstant(staticOrdinal), PositionOperator.Add, ordinalBase);

    /// <summary>The subscript levels of a reference to <paramref name="item"/> WITHIN its Tier-B class
    /// <paramref name="cls"/>, outermost first: every fixed OCCURS level and every dynamic-capacity table level on
    /// its path (§8.4.2.3.3 SR3 — one subscript per OCCURS clause, a dynamic table's included).</summary>
    private static List<DataItem> SubscriptLevelsWithin(DataItem item, RedefinesClass cls)
    {
        var levels = new List<DataItem>();
        for (DataItem? n = item; n is not null && ReferenceEquals(n.Class, cls); n = n.Parent)
            if (n.Occurs is not null || n.IsDynamicTable) levels.Add(n);
        levels.Reverse();
        return levels;
    }

    /// <summary>Where a Tier-B class reference's window lives, and its displacements there.</summary>
    /// <param name="Cell">The scope's <c>StorageCell</c> path (null for a REDEFINES class, which has none).</param>
    /// <param name="Backing">The scope's character backing — the class backing, or an element cell's <c>Ref</c>.</param>
    /// <param name="Terms">The byte displacement terms of the fixed levels crossed in the scope.</param>
    /// <param name="BitTerms">The same displacement in bits, for a USAGE BIT member.</param>
    /// <param name="OrdinalTerms">The component-ordinal displacement of the same levels (cell only).</param>
    /// <param name="Nested">True when a dynamic-capacity table level opened an element cell's scope.</param>
    private readonly record struct WindowScope(AccessPath? Cell, AccessPath Backing, IReadOnlyList<OffsetTerm> Terms,
                                               IReadOnlyList<OffsetTerm> BitTerms, Position? OrdinalBase,
                                               IReadOnlyList<OffsetTerm> OrdinalTerms, bool Nested)
    {
        /// <summary>The component ordinal of an item whose static ordinal in the scope is <paramref name="staticOrdinal"/>,
        /// displaced by the scope's run-time ordinal base and its levels.</summary>
        public PositionOffset OrdinalAt(int staticOrdinal) => new(OrdinalOrigin(staticOrdinal, OrdinalBase), OrdinalTerms);
    }

    /// <summary>⛔ THE ONE WALK OF A TIER-B REFERENCE'S SUBSCRIPT LEVELS (kb/Work PB1042), outermost first. A fixed
    /// level displaces the window by <c>(index − 1) ×</c> its per-occurrence STORAGE extent — never its
    /// character-position count (kb/Work PB231: a NATIONAL element occupies two bytes per position, §13.18.60.4 GR8 /
    /// D-N1) — its BIT twin by the level's bit stride (kb/Work PB203: `PIC 1(4) USAGE BIT OCCURS 6` strides 4 bits;
    /// ALIGNED strides whole bytes, §13.18.1.4 GR2), and, in a cell, the component ordinal by the level's components
    /// per occurrence (<see cref="CellComponents.PerOccurrence"/>). A dynamic-capacity table level — ISO §8.5.1.9.1
    /// 3): it "may be defined in any place, other than the file section, in which a fixed-capacity table may be
    /// defined" — opens the SCOPE of its element: the occurrence's element cell (<see cref="CellTableSegment"/> then
    /// <see cref="DynTableSegment"/>, whose accessor is the reference's direction, §8.5.1.9.2 / §8.5.1.9.3), whose
    /// <c>Ref</c> is the backing and whose own offsets and ordinals start again at zero. Null when such a level lies in
    /// a class with no cell.</summary>
    private static WindowScope? WindowScopeOf(AccessPath backing, AccessPath? cell, IReadOnlyList<DataItem> levels,
                                              IReadOnlyList<Position> indexExprs, Position? ordinalBase)
    {
        List<OffsetTerm> terms = [], bitTerms = [], ordinalTerms = [];
        bool nested = false;
        for (int k = 0; k < levels.Count; k++)
        {
            var level = levels[k];
            if (level.IsDynamicTable)
            {
                if (cell is null) return null;
                cell = cell.Add(CellTableSegment.Of(level, new PositionOffset(OrdinalOrigin(level.ClassDynOrdinal, ordinalBase), [.. ordinalTerms])))
                           .Add(new DynTableSegment(indexExprs[k]));
                backing = cell.Add(new MemberSegment(nameof(CobolNet.Runtime.StorageCell.Ref)));
                (terms, bitTerms, ordinalBase, ordinalTerms, nested) = ([], [], null, [], true);
                continue;
            }
            terms.Add(new OffsetTerm(indexExprs[k], level.ByteWidth));
            bitTerms.Add(new OffsetTerm(indexExprs[k], BitLayout.StrideBits(level)));
            if (cell is not null) ordinalTerms.Add(new OffsetTerm(indexExprs[k], CellComponents.PerOccurrence(level)));
        }
        return new WindowScope(cell, backing, terms, bitTerms, ordinalBase, ordinalTerms, nested);
    }

    /// <summary>⛔ THE ONE ROOT SEGMENT OF AN ITEM'S ACCESS PATH (kb/Work PB971): the item's field, carrying the
    /// *-ARG-OMITTED guard when the item is a formal parameter (<see cref="OmittedFormalGuard.Of"/>), so every
    /// path builder — element, whole table, Tier-B backing — checks a reference to an omitted formal the same
    /// way, and a new builder that roots through here cannot forget it.</summary>
    private static RootFieldSegment RootOf(DataItem root) => new(root.CsName, OmittedFormalGuard.Of(root));

    // ── Name resolution ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The item an unqualified name resolves to (first match; COBOL requires qualification to
    /// disambiguate) — through the ONE scope-aware <see cref="Model.SymbolTable"/> (P7 Step 10a, the DEVLOG-773
    /// pickup): the §11.7 GR5 method-overlay-first precedence (a method-local name shadows object data; sibling
    /// methods' names are invisible) lives in <c>TryResolve</c>, no longer duplicated here.</summary>
    private DataItem? ResolveUnqualified(string name)
    {
        // §8.4.6.2.1 3) — a contained program's own declaration hides a container's global one, and a nearer
        // container's hides a farther one (kb/Work PB1047 / PB1243): the unit map holds every tier, and the ONE
        // nearest-tier rule narrows it before ambiguity is asked.
        if (!data.Symbols.TryResolveUnqualified(name, data.ActiveScope, out var list)) return null;
        if (list.Count > 1)
        {
            // §8.4.2.2.1 (kb/Work R33): "Qualification of a user-defined name is required unless … 1) No
            // other name has the identical spelling." TryResolve returns ONE namespace — the §8.4.6.2.1
            // rule-3a method overlay OR the unit map, never a mix — and NearestDeclaring has narrowed the unit map
            // to one source element, so a plural list is genuine same-element ambiguity, not legal scope
            // shadowing. Measured before enforcing: ZERO of 762 corpus+NIST
            // programs hit this (the note's blast-radius sweep). Strict: null → ReportUnidentified's
            // N-declarations arm (dead until now — R30 built it, this makes it reachable).
            // --permissive: the traditional first-declared match, warned (the DA6/R29 disposition shape).
            if (!data.Edition.Permissive) return null;
            if (_probing) return list[0];   // R30 purity: the committing resolution warns (kb/Work PB157)
            data.Edition.Warning(DiagnosticCatalog.UndefinedReference,
                $"'{name}' is declared {list.Count} times and referenced without qualification — "
                + "ISO §8.4.2.2.1 requires qualification when spellings collide; --permissive resolves to "
                + "the first declaration");
        }
        return list[0];
    }

    /// <summary>⛔ THE DECLARATION AN UNQUALIFIED DATA-NAME NAMES, IN THE REFERENCE'S OWN SCOPE (kb/Work PB467) —
    /// for the verbs whose operand is a <i>data-name</i> and whose rule is a predicate over the DECLARATION
    /// rather than over a storage place (SET ADDRESS OF / ALLOCATE's §14.9.39.3 SR18 and §14.9.3.3 SR1 based-item
    /// screen). It is <see cref="ResolveUnqualified"/> plus this resolver's own §8.4.2.1 report, so such a verb
    /// inherits EVERY scoping rule the resolver knows — now and later — instead of restating one.
    /// <para>⛔ THE BYPASS THIS REPLACED ANSWERED A SCOPED QUESTION FROM THE FLAT MAP. <c>PtrResolveBased</c> read
    /// <c>ctx.Data.ByName</c> directly, which has no notion of a reference's scope, and the based-item verdict
    /// went wrong in BOTH directions inside a method: a legal method-local <c>01 MB BASED</c> was refused as
    /// not-BASED (the method's own declaration was invisible), and a method-local NON-based item that legally
    /// shadows an object-level BASED one was ACCEPTED for rebasing (the object declaration was visible when it
    /// must not be). Moving the same declaration between the object's data division and the method's flipped the
    /// verdict — the proof that scope, not the declaration, was deciding. §11.7.4 GR5 is the rule: "If a given
    /// user-defined word is defined in the data division of this method definition and in the data division of
    /// the containing object definition, the use of that word in this method refers to the declaration in this
    /// method. The declaration in the containing object definition is inaccessible to this method" — which
    /// <see cref="Model.SymbolTable.TryResolve"/> already implements, one file away.</para>
    /// <para>Returns null having REPORTED (COBOLNET1639) when no declaration in scope carries the name or when
    /// several do — §8.4.2.2.1's ambiguity is the ordinary qualification question and gets the ordinary
    /// diagnostic, never the silent first-wins the bypass took. A caller distinguishes "reported" from "resolved
    /// to the wrong kind of item" with <see cref="WasDiagnosed"/>.</para></summary>
    internal DataItem? DeclarationOf(Core.DataReferenceContext dref, string name)
    {
        if (ResolveUnqualified(name) is { } item) return item;
        if (!_probing) ReportUnidentified(dref, name, []);
        return null;
    }

    /// <summary>
    /// Resolve a qualified reference <c>name OF q[0] OF q[1] …</c> (ISO §8.4.2.2) by CANDIDATE-SET matching:
    /// every in-scope declaration of <paramref name="name"/> whose ancestor chain carries each written
    /// qualifier in order (inner → outer, §8.4.2.2.2 Format 1 — qualifiers need not be consecutive levels),
    /// the OUTERMOST qualifier optionally the owning FILE's name (§8.4.2.2 — the FD/SD is the highest
    /// permissible qualifier; SQ207M's <c>WRITE PRINT-REC IN PRINT-FILE</c>). Exactly ONE survivor resolves;
    /// zero or several is a resolution failure the caller reports (kb/Work R30).
    /// <para>⛔ THE UNIQUENESS DEMANDED IS OF THE MATCH, NEVER OF A QUALIFIER NAME IN ISOLATION (kb/Work
    /// R31): §8.4.2.2.1 — "Identical user-defined names may be specified in a source unit; however,
    /// uniqueness shall be established through qualification". The prior walk resolved the outermost
    /// qualifier as a unique unqualified name FIRST, so legal <c>Z IN X</c> — two Xs, exactly one holding a
    /// Z — was rejected (the differential's syn_definition:931 flip, GnuCOBOL's own "Unique reference with
    /// ambiguous qualifiers" case). Level-66 RENAMES aliases are registered names with the owning record as
    /// <see cref="DataItem.Parent"/>, so <c>HARRY OF A-GLOB</c> (NC209A) matches through the same chain.</para>
    /// </summary>
    /// <para>⛔ THE MATCHING ITSELF LIVES ON <see cref="DataBinder.QualifiedCandidates"/> — ONE §8.4.2.2 resolver
    /// for the procedure division AND the file-description / file-control clauses, which used to carry a weaker
    /// private copy of it (kb/Work PB489). What stays here is only this format's decision about the survivors:
    /// exactly one resolves.</para>
    private DataItem? ResolveQualified(string name, List<string> qualifiers)
    {
        return data.QualifiedCandidates(name, qualifiers, data.ActiveScope).Single;
    }

    // ── Access-path construction (subscripts attach to OCCURS levels, outer→inner) ───────────────────────

    /// <summary>Why a written reference that NAMES a CAPACITY register still has no place (kb/Work PB457). Each arm
    /// is a syntax rule, not a hole: the register's name identifies it outright (§13.18.38.3 SR30 first sentence),
    /// so a reference that names it is never "not defined".</summary>
    internal enum CapacityRefFault
    {
        /// <summary>A legal reference — <see cref="CapacityRef.Place"/> is non-null.</summary>
        None,
        /// <summary>§13.18.38.3 SR31 — "Data-name-3 shall not be subscripted."</summary>
        Subscripted,
        /// <summary>§8.4.3.3.3 SR1 — the register is not an identifier-1 reference modification may name.</summary>
        RefModified,
        /// <summary>§13.18.38.3 SR30 second sentence with §8.4.2.2.3 SR4 — the written OF/IN qualifiers do not name
        /// successively more inclusive context of the register's implied position.</summary>
        Qualifiers,
        /// <summary>The register's table is itself subordinate to a table, so SR30 puts the register inside that
        /// outer table: §8.4.2.3.3 SR3/SR5 then REQUIRE a subscript that §13.18.38.3 SR31 FORBIDS, and no reference
        /// form is writable. ⚠ DETERMINATION — see the diagnostic's own note.</summary>
        UnderATable,
    }

    /// <summary>A written reference matched against the source element's named CAPACITY registers: the owning
    /// dynamic-capacity <paramref name="Table"/>, the <paramref name="Register"/> view item, the
    /// <paramref name="Place"/> when the reference is legal, and the <paramref name="Fault"/> otherwise.</summary>
    internal readonly record struct CapacityRef(
        DataItem Table, DataItem Register, CapacityRegisterPlace? Place, CapacityRefFault Fault);

    /// <summary>⛔ THE ONE MATCH OF A WRITTEN REFERENCE AGAINST THE NAMED OCCURS DYNAMIC CAPACITY REGISTERS
    /// (kb/Work PB457) — a PURE check over <see cref="DataBinder.CapacityRegisters"/> with NO side effects (unlike
    /// the full <see cref="Resolve"/> pipeline, which routes an unresolved qualified name through the
    /// property-reference hook and enqueues a pending op). The SET Format 14 reroute peek uses this so it never
    /// mints a spurious property temp/op for a non-capacity target (data-model D9; OCCURS DYNAMIC review #7).
    /// <see langword="null"/> means the base name names NO register; a non-null result with a null
    /// <see cref="CapacityRef.Place"/> is a rule violation the reporting caller states.
    /// <para>⛔ IT MATCHES THE REFERENCE, NOT A BARE NAME. The prior shape was
    /// <c>r.HasNoSuffix &amp;&amp; CapacityRegisters.TryGetValue(...) &amp;&amp; BuildTablePath(table) is { }</c> —
    /// two conjuncts that each DELETED a reference form instead of judging it. <c>HasNoSuffix</c> is a test on the
    /// PARSE shape and <c>dataReferenceSuffix</c> carries QUALIFICATION as well as subscripts, so the legal
    /// <c>SET WS-CAP OF WS-TABLE TO 7</c> (§13.18.38.3 SR30 with §8.4.2.2.3 SR2 — "A name may be qualified even
    /// though it does not need qualification") failed it; the zero-index <c>BuildTablePath</c> returned null for
    /// every table with a table ancestor. Because the register is off <c>ByName</c>, failing either conjunct was
    /// not a fallback but the END of resolution, and the general resolver then said COBOLNET1639 "is not defined"
    /// — false about a name the OCCURS clause declares.</para>
    /// <para>The qualifier test is the ONE §8.4.2.2 matcher (<see cref="DataBinder.QualifierChainMatches"/>), read
    /// off the register's real <see cref="DataItem.Parent"/> — SR30's "treated as though implicitly defined at the
    /// same level as the entry containing the OCCURS clause", set where the register is minted
    /// (<c>DataBinder.Odo.cs</c>). Within ONE source element the candidate set has at most one member, by SR30's
    /// first sentence, so there is no survivor count to take.</para>
    /// <para>⛔ ACROSS SOURCE ELEMENTS IT IS §8.4.6.2.1 (kb/Work PB1674). A contained program also sees every
    /// container's GLOBAL record's registers, nearest container first after its own (<see cref="DataBinder.CapacityRegisters"/>),
    /// and rule 1 applies "the normal rules for qualification" over that whole set before rule 3 picks the nearest
    /// declaring element: the first register the written qualifiers reach is the candidate (the nearest one when
    /// none does, so the qualifier fault is stated about it), and an ordinary data-name of the spelling in a NEARER
    /// element takes the reference instead (<see cref="Model.SymbolTable.NearerDataNameHides"/> — the rule the
    /// index-name class already obeys), handing it back to the ordinary lookup.</para></summary>
    internal CapacityRef? CapacityRegisterFor(Core.DataReferenceContext dref)
    {
        DataReferenceCst r = dref;
        if (r.BaseName is not { } name || !data.CapacityRegisters.TryGetValue(name, out var tables)) return null;
        var written = ReadWritten(dref);
        DataItem table = tables.Find(t => t.OccursSpec?.CapacityRegister is { } cand
                                          && data.QualifierChainMatches(cand, written.Qualifiers))
            ?? tables[0];
        if (table.OccursSpec?.CapacityRegister is not { } reg
            || data.Symbols.NearerDataNameHides(name, written.Qualifiers, data.DeclaringDepth(table))) return null;
        var path = BuildTablePath(table);
        var fault =
              written.SubscriptGroup is not null                     ? CapacityRefFault.Subscripted
            : written.RefModCount > 0                                ? CapacityRefFault.RefModified
            : !data.QualifierChainMatches(reg, written.Qualifiers)   ? CapacityRefFault.Qualifiers
            : path is null                                           ? CapacityRefFault.UnderATable
            : CapacityRefFault.None;
        return fault is CapacityRefFault.None && path is not null
            ? new CapacityRef(table, reg, new CapacityRegisterPlace(path, reg), CapacityRefFault.None)
            : new CapacityRef(table, reg, null, fault);
    }

    /// <summary>⛔ <b>THE ONE TEST for "this written reference IS the predefined object reference
    /// EXCEPTION-OBJECT"</b> (ISO §8.4.3.6; kb/Work PB922) — spelling, FORM and EDITION together, so no caller can
    /// hold a different opinion about what the name is. It sits beside <see cref="CapacityRegisterFor"/> because it
    /// answers the same shape of question for the other implicitly-declared name, and because the RESOLVER is where
    /// the answer belongs: §8.4.3.6.3 SR2 declares the name, no data description entry does, and a caller that does
    /// not ask gets "not defined" about a name the standard itself declares.
    /// <para>⛔ THE EDITION IS PART OF THE QUESTION, AND LEAVING IT OUT OF ONE ARM REJECTED LEGAL SOURCE. The
    /// object-orientation facility — and with it §8.9's reservation of the word — arrived in COBOL-2002, so at
    /// <c>--std 85</c> EXCEPTION-OBJECT is an ORDINARY user-defined word: <c>01 EXCEPTION-OBJECT PIC X(4).</c>
    /// followed by <c>MOVE "ABCD" TO EXCEPTION-OBJECT</c> is conforming '85 source. When only the resolver arm
    /// carried the gate and the receiving screen compared the spelling by itself, that program drew
    /// COBOLNET2196 — SR1 quoted at a program that never referenced the register
    /// (feedback_two_arm_dispatch, feedback_edition_gate_sweep). From 2002 the reservation makes the name
    /// undeclarable, so there is nothing left for the interception to shadow.</para>
    /// <para>Only the plain unqualified / unsubscripted / unreference-modified form is the register:
    /// §8.4.3.6.2's general format is the bare word, so a suffixed occurrence is some other (undefined)
    /// reference and must keep the ordinary resolution.</para></summary>
    internal bool IsExceptionObjectRegister(Core.DataReferenceContext dref) =>
        data.Edition.Edition.Has(2002)
        && dref.dataReferenceSuffix().Length == 0
        && Procedure.OoBinder.OoIsExceptionObject(dref.GetText());

    /// <summary>The place a matched <see cref="CapacityRef"/> resolves to, REPORTING its fault when it has one —
    /// the single reporting site for every ill-formed reference to a named CAPACITY register (kb/Work PB457). Every
    /// arm names the rule the reference breaks; none of them can be "not defined", because the name IS defined.
    /// <para>⚠ <b>DETERMINATION (<see cref="CapacityRefFault.UnderATable"/>)</b> — a dynamic-capacity table nested
    /// within another table is legal to DEFINE (§8.5.1.9.1 item 3: it "may be nested in any combination to the same
    /// number of levels as a fixed-capacity table") and its register may be NAMED, but no reference to that register
    /// is writable. §13.18.38.3 SR30 puts the register at the same level as the OCCURS entry — inside the outer
    /// table, and deliberately unlike the occurs-depending item, which §13.18.38.3 SR20 forces OUTSIDE the table —
    /// so §8.4.2.3.3 SR3 ("the number of subscripts shall equal the number of OCCURS clauses in the description of
    /// the table element being referenced") and SR5 ("Each table element reference shall be subscripted except
    /// when such reference appears" — seven contexts, none of them this) require one subscript per enclosing table,
    /// while §13.18.38.3 SR31 forbids any. The reading REJECTED was "the bare name designates the capacity of every
    /// occurrence", the occurs-depending analogy: it has no textual support, and it contradicts the per-occurrence
    /// capacity model §15.3's table(ALL) enumeration already rests on (kb/Work PB62) — each outer occurrence holds
    /// its own <c>CobolDynTable</c> with its own capacity, so one SET cannot mean all of them.</para></summary>
    private Place? CapacityPlaceOf(Core.DataReferenceContext dref, CapacityRef cap)
    {
        if (cap.Place is { } place) return place;
        if (_probing || !_diagnosed.Add(dref)) return null;   // R30 purity: a probe never diagnoses (kb/Work PB157)
        string text = DataBinder.WrittenText(dref);
        string subject = cap.Register.CobolName ?? cap.Register.CsName;
        string table = cap.Table.CobolName ?? cap.Table.CsName;
        switch (cap.Fault)
        {
            case CapacityRefFault.Subscripted:
                data.Edition.Error(DiagnosticCatalog.CapacityRegisterSubscripted,
                    $"'{text}': '{subject}' is the CAPACITY register of the dynamic-capacity table '{table}' and "
                    + "ISO §13.18.38.3 SR31 says \"Data-name-3 shall not be subscripted\". Write the register's "
                    + "name alone (optionally qualified), and subscript the TABLE's elements instead.");
                return null;
            case CapacityRefFault.RefModified:
                // §8.4.3.3.3 SR1 lives in ONE place — RefModExclusion — and the register answers it like any item.
                data.Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted,
                    $"'{text}': reference modification of {RefModExclusion(cap.Register) ?? "a CAPACITY register"} "
                    + "is not permitted (ISO §8.4.3.3.3 SR1)");
                return null;
            case CapacityRefFault.Qualifiers:
                string innermost = cap.Register.Parent is { CobolName: { } p }
                    ? $"; the innermost permitted qualifier is '{p}'" : "";
                data.Edition.Error(DiagnosticCatalog.CapacityRegisterQualifier,
                    $"'{text}': the written qualifiers do not name context of '{subject}'. ISO §13.18.38.3 SR30 "
                    + "treats a CAPACITY register \"as though implicitly defined at the same level as the entry "
                    + $"containing the OCCURS clause\", so '{subject}' stands beside '{table}' and its qualifiers "
                    + $"are the group names above it{innermost}. ISO §8.4.2.2.3 SR4 requires them \"in the order "
                    + "of successively more inclusive levels in the hierarchy\".");
                return null;
            case CapacityRefFault.UnderATable:
                data.Edition.Error(DiagnosticCatalog.CapacityRegisterUnderTable,
                    $"'{text}': '{subject}' is the CAPACITY register of '{table}', which is itself subordinate to a "
                    + "table, so ISO §13.18.38.3 SR30 places the register inside that outer table. ISO §8.4.2.3.3 "
                    + "SR3 and SR5 then require one subscript per enclosing OCCURS clause, and ISO §13.18.38.3 SR31 "
                    + "forbids subscripting data-name-3 — no reference form is writable. Name the register only on "
                    + "a dynamic-capacity table that has no table ancestor, or read the capacity with FUNCTION "
                    + "LENGTH over the subscripted inner table instead.");
                return null;
            default:
                // CapacityRefFault.None cannot reach here (its Place is non-null by construction), and every other
                // member has a case above — CapacityRegisterReferenceDriftTests pins that, so a new fault cannot
                // ship diagnostic-less. This arm is an internal-error backstop, never a user-reachable path.
                throw new InvalidOperationException(
                    $"CapacityRefFault.{cap.Fault} carries no diagnostic (kb/Work PB457).");
        }
    }

    /// <summary>The <see cref="ReportSumCounterPlace"/> for a reference to a REPORT SECTION sum counter (ISO
    /// §13.18.54.4 GR5 — the data-name written after the level number of an entry containing a SUM clause "is the
    /// name of the sum counter, not the name of the associated printable item"; GR12 — "It is permissible for
    /// procedure division statements to alter the content of sum counters"), or null when the name is not a sum
    /// counter. kb/Work PB840.
    /// <para>⛔ A SUM COUNTER HAS ITS REAL HIERARCHY (kb/Work PB1454). §8.4.2.2.3 SR4 — "Each data-name-2 shall be the
    /// name associated with a level number to which the item being qualified is subordinate" — and a SUM entry is
    /// always subordinate to its 01 report group (and to any group entry between), so the covered spelling is
    /// <c>counter [ OF group-entry ]… [ OF report-name ]</c>: every named level above the entry, inner → outer
    /// with gaps allowed, the report-name last as §8.4.2.2.2 Format 1's file-report-qualifier. The walk is the ONE
    /// <see cref="DataBinder.QualifierWalk"/> a data item's own ancestors go through, over
    /// <see cref="ReportSumModel.Qualification"/>. (The model it replaced — "a level-01-free item whose only
    /// available qualifier is its report" — was false, so `TOT OF CF1` was refused and two same-named counters
    /// of one report could not be told apart.) A reference-modified counter is a counter reference like any other
    /// and takes the ordinary reference-modification tail after its subscripts (kb/Work PB1943).</para>
    /// <para>⛔ A REPEATING ENTRY'S COUNTER IS A TABLE (kb/Work PB1271). Its occurrences are a
    /// <see cref="ReportSumFamily"/> whose register carries one OCCURS level per repetition vehicle, so the written
    /// subscripts go through the ONE <see cref="ReadSubscripts"/> — §8.4.2.3.3 SR3's count, SR5's "Each table
    /// element reference shall be subscripted" (an unsubscripted reference to a multiple COLUMN counter used to
    /// alter all of its occurrences at once) and SR4's index-name association — and the place carries the rendered
    /// subscripts the engine turns into the occurrence's counter id at run time.</para>
    /// <para>⛔ TWO ENTRIES MAY LEGALLY CARRY ONE NAME (GR1, kb/Work PB882): §8.4.2.2.1's uniqueness requirement
    /// is about a REFERENCE, so the collision is diagnosed HERE, where a reference exists, and never by dropping
    /// a declaration.</para></summary>
    private RefResolution? SumCounterFor(Core.DataReferenceContext dref, string name, bool report)
    {
        if (!data.SumCounters.ContainsKey(name)) return null;
        var written = ReadWritten(dref);
        if (SumCounterFamilyFor(dref, name, written.Qualifiers, report) is not ({ } rep, { } family)) return null;
        // §8.4.3.3.3 SR3 — the count screen every identifier entry runs (ScreenRefModCount), asked of the counter too.
        if (!ScreenRefModCount(dref, written, name)) return RefResolution.Refused(DataBinder.WrittenText(dref));
        if (ReadSubscripts(dref, family.Register, written.SubscriptGroup, out var indexExprs) is { } refusal)
            return refusal;
        var counter = SumCounterPlace(rep, family, indexExprs);
        if (written.RefModCount == 0) return RefResolution.Resolved(counter, "");
        // ⛔ A REFERENCE-MODIFIED COUNTER TAKES THE ONE REFERENCE-MODIFICATION TAIL (kb/Work PB1943), after its
        // subscripts like every item: §8.4.3.3.3 SR1 admits "a numeric data item of usage display or national", and the
        // counter's register IS one (PicInfo.SumCounterItem — GR1 states no usage, so DISPLAY is the implementor's
        // determination, docs/CONFORMANCE.md A.4.11). The same admission (ReadScreenedRefMod) and the same view
        // (RefModView: the counter's DISPLAY character image, GR1's digits) as every other numeric item. This used
        // to return null here, which sent the reference to ordinary resolution, where a sum counter is not in
        // ByName: COBOLNET1639 "'CF-T(1:2)' is not defined", naming a rule the program had not broken.
        if (ReadScreenedRefMod(dref, written, counter.Item, out var refModRefusal) is not { } spec) return refModRefusal;
        return RefModView(counter.Item, counter, spec) is { } view
            ? RefResolution.Resolved(view, "")
            : RefResolution.Deferred(DeferredShape.NumericRefModSubstrate, DataBinder.WrittenText(dref));
    }

    /// <summary>The place of one counter occurrence of <paramref name="family"/>: its rendered one-based subscripts
    /// (outermost first; empty for a non-repeating entry, whose one counter is <see cref="ReportSumFamily.BaseId"/>).
    /// The ONE construction of a procedure-division sum counter place — the ordinary reference and the table(ALL)
    /// intrinsic argument (§15.3) both build through it.</summary>
    internal ReportSumCounterPlace SumCounterPlace(ReportModel report, ReportSumFamily family, IReadOnlyList<Position> indexExprs) =>
        new(report.CsIndex, family.BaseId, family.Register, data.ReportDepth(report), indexExprs);

    /// <summary>The CURRENT range of level <paramref name="level"/> (outermost first) of a repeating sum counter, for
    /// a table(ALL) argument (ISO §15.3): an enclosing repetition with the DEPENDING phrase ranges over
    /// §13.18.38.4 GR13's count ("the range of values is determined by the object of the OCCURS DEPENDING ON
    /// clause"); every other level over its extent. The twin of <see cref="CurrentOccurrenceCount"/>, whose data
    /// division OCCURS DEPENDING has GR7's clamp and EC-BOUND-ODO instead (kb/Work PB1271). Null when data-name-1
    /// cannot be addressed.</summary>
    internal AllCount? SumCounterOccurrenceCount(ReportSumFamily family, int level) =>
        level < family.Repetitions.Count && family.Repetitions[level] is { DependingItem: { } dn } spec
            ? (ResolveItem(dn) is { } depending ? new AllCount.ReportDepending(depending, spec.Min, spec.Max) : null)
            : new AllCount.Fixed(family.Extents[level]);

    /// <summary>The SUM ENTRY a reference names — <see cref="SumCounterFor"/>'s name-and-qualifier half, shared with
    /// the table(ALL) intrinsic argument so both ask it in the same order relative to ordinary lookup (a counter
    /// first, unless the qualifiers reach no counter and an ordinary item answers them). Null when the name is no
    /// counter's, when an ordinary item answers the qualifiers, or when the reference was diagnosed (no counter
    /// is subordinate to the qualifiers; two counters are).</summary>
    internal (ReportModel Report, ReportSumFamily Family)? SumCounterFamilyFor(
        Core.DataReferenceContext dref, string name, List<string> qualifiers, bool report)
    {
        if (!data.SumCounters.TryGetValue(name, out var homonyms)) return null;
        // §13.18.53.3 SR4 — inside a report's own clauses the only sum counters a SOURCE operand may name are those "defined in
        // the current report", so a homonym of another report is no candidate: the reference is unambiguous by the rule
        // and never needs a qualifier to say so (kb/Work PB1292).
        if (ReportScope is { } scope) homonyms = [.. homonyms.Where(h => ReferenceEquals(h.Report, scope))];
        if (homonyms.Count == 0) return null;
        // §8.4.6.2.1 rule 3 — a counter of a report this source element declares hides a same-named counter of a
        // container's GLOBAL report; only candidates of the NEAREST declaring element can be ambiguous.
        var matches = data.NearestInScope(
            homonyms.Where(h => DataBinder.QualifierWalk(h.Family.Qualification, qualifiers,
                q => CobolNames.Same(q, h.Report.Name))),
            h => h.Report);
        if (matches.Count == 0)
        {
            // The qualifiers reach no counter of this name. When an ORDINARY item of the name does answer them
            // (`TOT OF WS-GROUP`, the counter's name reused as a data item), the reference is that item's and
            // resolution carries on; otherwise the name IS a sum counter and the ordinary resolver would only say
            // "not defined", so name the real rule (§8.4.2.2.2 / §8.4.2.2.3 SR4).
            if (ResolveQualified(name, qualifiers) is not null) return null;
            if (report && _diagnosed.Add(dref))
                data.Edition.Error(DiagnosticCatalog.ReportSumCounterReference, $"'{DataBinder.WrittenText(dref)}': '{name}' is a sum counter "
                    + $"(ISO §13.18.54.4 GR5), but none is subordinate to the qualifiers written — a sum counter is "
                    + "qualified by the data-names of the report group description entries above it, innermost "
                    + "first, and by the report-name last (ISO §8.4.2.2.3 SR4, §8.4.2.2.2 Format 1).");
            return null;
        }
        if (matches.Count > 1)
        {
            if (report && _diagnosed.Add(dref))
                data.Edition.Error(DiagnosticCatalog.ReportSumCounterReference, $"'{DataBinder.WrittenText(dref)}': the reference is ambiguous — "
                    + $"{matches.Count} entries establish a sum counter named '{name}' (ISO §13.18.54.4 GR1 gives "
                    + "each entry its own counter). A reference shall uniquely identify one resource (ISO "
                    + "§8.4.2.2.1); qualify it by report group entry or report-name, or give the entries distinct data-names.");
            return null;
        }
        return matches[0];
    }

    /// <summary>The STRUCTURAL access path for an item — the <see cref="MemberPlace"/>/<see cref="DynTablePlace"/>
    /// twin of the string <see cref="AccessPath"/>: each chain node is a field segment, each OCCURS level a fixed or
    /// dynamic table segment carrying its typed index position. Null on a subscript-count mismatch.</summary>
    private static AccessPath? BuildAccessPath(DataItem item, IReadOnlyList<Position> indexExprs,
        Func<DataItem, OdoReferenceCheck?>? odo = null)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = item; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();
        // §8.4.2.3.3 SR3's count, from THE one place it is written down — the string twin below reads the same
        // property, so the two access-path builders cannot disagree about arity (kb/Work PB877).
        if (item.SubscriptArity != indexExprs.Count) return null;   // wrong number of subscripts
        var segs = new List<AccessSegment>();
        int si = 0;
        bool first = true;
        foreach (var seg in chain)
        {
            segs.Add(first ? RootOf(seg) : new MemberSegment(seg.CsName));
            first = false;
            if (seg.Occurs is not null) segs.Add(new FixedTableSegment(indexExprs[si++], odo?.Invoke(seg)));   // fixed OCCURS → CobolTable.At
            else if (seg.IsDynamicTable) segs.Add(new DynTableSegment(indexExprs[si++]));         // dynamic OCCURS → RefSending/RefReceiving
        }
        return new AccessPath(segs);
    }

    /// <summary>The STRUCTURAL whole-table path (no subscript wraps) — the ONE whole-table path: the
    /// <see cref="CapacityRegisterPlace"/>'s table, the SEARCH bound and EC-FLOW-SEARCH bracket, and the base of a
    /// whole-dynamic-table INITIALIZE element path (its string twin was deleted with kb/Work PB1042, which found it
    /// blind to a cell-backed table). Null when an ancestor is itself a table (an ambiguous whole-table
    /// reference).</summary>
    internal static AccessPath? BuildTablePath(DataItem table) => BuildTablePath(table, []);

    /// <summary>⛔ THE ONE MODEL OF A TABLE LEVEL'S CURRENT OCCURRENCE COUNT (the <see cref="AllCount"/> the backend
    /// renders through <c>PlaceRenderer.OccurrenceCount</c>): a fixed table's OCCURS integer (§13.18.38.4 GR4); an
    /// occurs-depending table's data-name-1, which "represents the current number of occurrences of the subject of
    /// the entry" (GR7 — clamped to [integer-1, integer-2] with EC-BOUND-ODO outside); a dynamic-capacity table's
    /// current capacity (§8.5.1.9.1), whose register view carries the OUTER index expressions of a nested table.
    /// Every statement that ranges over the CURRENT occurrences of a table it sends asks HERE — a table(ALL)
    /// argument (§15.3) and the Format-2 table SORT (§14.9.40.4 GR20, kb/Work PB1174, whose sort used to reorder
    /// the whole PHYSICAL array). INITIALIZE keeps its own <c>InitializeBinder.TableCount</c> because its operand is
    /// a RECEIVING one: §13.18.38.4 GR8b gives a receiving group holding its own data-name-1 the MAXIMUM, a question
    /// this sending-count model does not ask. <see langword="null"/> when the count
    /// cannot be addressed (an unresolvable data-name-1, or a dynamic table with no reachable register) — the caller
    /// reports it in its own words.
    /// <para>The three-way switch itself is <see cref="Procedure.OccurrenceCounts.Current"/> — the ONE reading INITIALIZE and the
    /// §14.6.9.2 element moves also take; this entry only supplies the whole-table path a dynamic level's capacity
    /// register needs (the two used to be two copies of the switch, each documented as the one).</para></summary>
    internal AllCount? CurrentOccurrenceCount(DataItem table, IReadOnlyList<Position> outerIndexExprs) =>
        Procedure.OccurrenceCounts.Current(table, table.IsDynamicTable ? BuildTablePath(table, outerIndexExprs) : null, this);

    /// <summary>The §13.18.38.4 GR7 check a subscripted reference through <paramref name="level"/> carries
    /// (<see cref="OdoReferenceCheck"/>, kb/Work PB1268): data-name-1's place and integer-1/integer-2 for an OCCURS
    /// DEPENDING level, when the compilation group can enable EC-BOUND-ODO at all; <see langword="null"/> otherwise
    /// (a fixed level, or no enabling >>TURN — the zero-scaffolding invariant). The bounds are the SAME the extent
    /// model uses (<see cref="CurrentOccurrenceCount"/>), so the superordinate and the element checks agree.</summary>
    private OdoReferenceCheck? OdoReferenceCheckFor(DataItem level) =>
        data.OdoReferenceChecking && CurrentOccurrenceCount(level, []) is AllCount.Odo o
            ? new OdoReferenceCheck(o.Depending, o.MinOccurs, o.MaxOccurs) : null;

    /// <summary>The STRUCTURAL whole-table path to a table that may itself lie under OTHER tables — one index
    /// position per enclosing table level, outermost first: the
    /// <see cref="CapacityRegisterPlace"/> of a NESTED dynamic-capacity table for a table(ALL) enumeration (ISO §15.3
    /// — each outer occurrence has its own capacity; kb/Work PB62). Null when fewer indices are supplied than there
    /// are enclosing tables — the zero-index form is exactly the whole-table ambiguity the one-argument overload
    /// reports.</summary>
    internal static AccessPath? BuildTablePath(DataItem table, IReadOnlyList<Position> outerIndexExprs)
    {
        // A dynamic-capacity table of a CELL-BACKED class is a component of its scope's cell (kb/Work PB1042): the
        // enclosing levels are walked by THE ONE scope walk, and the table is that cell's component.
        if (table is { IsDynamicTable: true, Class: { IsCellBacked: true } cc } && BuildCellPath(cc) is { } rootCell)
        {
            var outer = SubscriptLevelsWithin(table, cc);
            outer.RemoveAt(outer.Count - 1);   // the table itself
            if (outerIndexExprs.Count < outer.Count) return null;   // an enclosing table with no index — ambiguous
            return WindowScopeOf(rootCell, rootCell, outer, outerIndexExprs, CellOrdinalBase(cc)) is { Cell: { } scopeCell } s
                ? scopeCell.Add(CellTableSegment.Of(table, s.OrdinalAt(table.ClassDynOrdinal)))
                : null;
        }
        var chain = new List<DataItem>();
        for (DataItem? n = table; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();
        var segs = new List<AccessSegment>();
        bool first = true;
        int oi = 0;
        foreach (var n in chain)
        {
            segs.Add(first ? RootOf(n) : new MemberSegment(n.CsName));
            first = false;
            if (ReferenceEquals(n, table)) break;
            if (!n.IsTable) continue;
            if (oi >= outerIndexExprs.Count) return null;   // an enclosing table with no index — ambiguous
            segs.Add(n.Occurs is not null
                ? new FixedTableSegment(outerIndexExprs[oi++])
                : new DynTableSegment(outerIndexExprs[oi++]));
        }
        return new AccessPath(segs);
    }

    // ── Subscript interpretation (the parsed subscript list, kb/Work PB2113) ───────────────────────────────

    /// <summary>True when one subscript segment is the bare word <c>ALL</c> (§8.4.2.3.3 SR6) — the ONE test, asked by
    /// the reference reader and the table(ALL) intrinsic argument alike.</summary>
    internal static bool IsAllSegment(PositionSegment segment) => segment.Tokens is [{ Type: Core.ALL }];

    /// <param name="indexNames">§8.4.2.3.3 SR4's collector — the index-names used as subscripts, for
    /// <see cref="ScreenIndexNameAssociation"/> at the caller, which knows the table being referenced.</param>
    /// <param name="admitRightmostAll">True only for a Format-2 SORT table subject (§8.4.2.3.3 SR6): a rightmost
    /// <c>ALL</c> is "equivalent to omitting the rightmost or only subscript", so the segment is dropped from the
    /// result rather than bound (<see cref="ReadSubscripts"/>).</param>
    /// <summary>Bind each subscript of the written list to its typed <see cref="Position"/>, outermost first; a
    /// segment that cannot be bound yields null (→ the caller fails loud).</summary>
    private List<Position>? InterpretSubscripts(
        Core.SubscriptPartContext ctx, List<IndexUse>? indexNames = null, bool admitRightmostAll = false)
    {
        var positions = new List<Position>();
        // kb/Work PB136 — declaration-informed '(' splitting, through the ONE predicate (kb/Work PB877): an
        // inline `IsTable` lambda stood here and answered only §8.4.2.3.3 SR2's FIRST half, so a name SUBORDINATE
        // to an OCCURS — legally subscripted, and a legal arithmetic-expression-1 subscript under §8.4.2.3.2 +
        // §8.8.1.1 + §8.4.3.1.2 Format 2 — had its own '(' split off as an EXTRA subscript.
        var segments = SegmentsOf(ctx, CannotBeSubscripted);
        if (admitRightmostAll && segments.Count > 0 && IsAllSegment(segments[^1])) segments.RemoveAt(segments.Count - 1);
        foreach (var seg in segments)
        {
            if (BindPosition(seg, SegmentPosition.Subscript, indexNames) is not { } p) return null;
            positions.Add(p);
        }
        return positions;
    }

    /// <summary>The parse tree's terminals under <paramref name="node"/>, in source order.</summary>
    internal static void CollectLeafTokens(IParseTree node, List<IToken> tokens)
    {
        if (node is ITerminalNode term) { tokens.Add(term.Symbol); return; }
        for (int i = 0; i < node.ChildCount; i++) CollectLeafTokens(node.GetChild(i), tokens);
    }

    private static List<IToken> LeafTokens(IParseTree node)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(node, tokens);
        return tokens;
    }

    /// <summary>A name as the subscript reader reads one: a word that can name a data item, an index-name or a
    /// constant (the lexer's own data-name word set, <c>CobolLexer.SubscriptTriggerTokens</c> — IDENTIFIER plus every
    /// context-sensitive word that can be user-defined). Whether it names one HERE is the resolver's question.</summary>
    internal static bool IsNameToken(IToken t) => CobolLexer.SubscriptTriggerTokens.Contains(t.Type);

    /// <summary>The OF/IN qualifiers written after the name at <paramref name="at"/> in <paramref name="tokens"/>
    /// (ISO §8.4.2.2.2), in written order; <paramref name="last"/> is the index of the last token they cover.</summary>
    private static List<string> QualifiersAfter(List<IToken> tokens, int at, out int last)
    {
        var qualifiers = new List<string>();
        last = at;
        while (last + 2 < tokens.Count && tokens[last + 1].Type is Core.OF or Core.IN && IsNameToken(tokens[last + 2]))
        {
            qualifiers.Add(tokens[last + 2].Text);
            last += 2;
        }
        return qualifiers;
    }

    /// <summary>ONE written position — a subscript of a subscript list, or a reference modifier's leftmost position or
    /// length — as the PARSE gives it (kb/Work PB2151). <paramref name="Tokens"/> are its terminals, for the readers
    /// whose syntax rules are about the written text (§14.9.37.3 SR8/SR9's index-name subscript, §13.10.3 SR3's
    /// literal subscripts). <paramref name="Operand"/> is the node the position binds from: the subscript item's or
    /// the modifier's <c>functionArgument</c>; null for <c>ALL</c> and for a piece of a written item the tree cannot
    /// give a subscript reading (<see cref="SegmentsOf"/>), which the binder refuses by name.
    /// <paramref name="Cut"/> is the reference inside <paramref name="Operand"/> whose subscript list the
    /// declarations gave to the NEXT subscript (kb/Work PB136): it is read without that list.
    /// <paramref name="FromCutList"/> marks that next subscript — the list's single item, written in its own
    /// parentheses.</summary>
    internal sealed record PositionSegment(
        List<IToken> Tokens, Core.FunctionArgumentContext? Operand,
        Core.DataReferenceContext? Cut = null, bool FromCutList = false);

    /// <summary>⛔ THE WRITTEN SUBSCRIPT LIST AS SEGMENTS, ONE PER SUBSCRIPT, read off the PARSE (kb/Work PB2113).
    /// The grammar already split the list — each <c>subscriptItem</c> is one operand, separated by §8.3.5's space or
    /// comma/semicolon-plus-space — so a segment is an item. The one decision the grammar cannot make is kept, and it
    /// is asked of the tree:
    /// <para>⛔ kb/Work PB136 — the '(' after a name, at an item's own level, is the name's subscript list when the
    /// name can carry one and the start of a NEW subscript when it cannot (§8.4.2.3.3 SR2): Annex D.3.5.3's
    /// <c>DOG (XCOUNTER (- YCOUNTER))</c> is two subscripts, <c>DOG (BAKER (I) 3)</c> two with BAKER(I) the first. The
    /// parser always reads the first way (the lexer typed the paren REF_LPAREN after the name), so a name that
    /// <paramref name="parenSplitsAfterName"/> says cannot be subscripted gives its list to a new subscript — the cut
    /// the declarations decide, which §8.4.2.3.2 leaves to them. With no predicate (none is known) nothing is cut.</para>
    /// <para>⚠ A cut has a subscript reading only when its list CLOSES the item: the item without the list is the
    /// first subscript and the list's single item the second. A list followed by more of the item
    /// (<c>T (A + X (- Y) + 1)</c>, where the tree joins <c>+ 1</c> to <c>X (- Y)</c>), or a second cut in one item,
    /// has none the tree can give; its pieces carry no operand and the binder refuses them by name (COBOLNET2363)
    /// rather than re-cut the expression out of its tokens (DESIGN-binder-bound-tree.md §3.9.2).</para></summary>
    internal static List<PositionSegment> SegmentsOf(Core.SubscriptPartContext group,
        Func<string, List<string>, bool>? parenSplitsAfterName)
    {
        var segments = new List<PositionSegment>();
        if (group.subscriptList() is not { } list) return segments;
        foreach (var item in list.subscriptItem())
        {
            var tokens = LeafTokens(item);
            var cuts = new List<(Core.DataReferenceContext Ref, Core.SubscriptPartContext List)>();
            if (parenSplitsAfterName is not null) CollectCuts(item, parenSplitsAfterName, cuts);
            if (cuts.Count == 0) { segments.Add(new PositionSegment(tokens, item.functionArgument())); continue; }
            int at = tokens.IndexOf(cuts[0].List.Start);
            var head = tokens.GetRange(0, at);
            var tail = tokens.GetRange(at, tokens.Count - at);
            var cutList = cuts[0].List;
            bool closes = cuts.Count == 1 && ReferenceEquals(tokens[^1], cutList.Stop);
            if (!closes)
            {
                segments.Add(new PositionSegment(head, null));
                segments.Add(new PositionSegment(tail, null));
                continue;
            }
            segments.Add(new PositionSegment(head, item.functionArgument(), Cut: cuts[0].Ref));
            var only = cutList.subscriptList()?.subscriptItem() is [{ } single] ? single.functionArgument() : null;
            segments.Add(new PositionSegment(tail, only, FromCutList: true));
        }
        return segments;
    }

    /// <summary>The cut points of one subscript item (see <see cref="SegmentsOf"/>): every subscript list hung on a
    /// name that cannot be subscripted, at the item's OWN level — inside a nested paren (another reference's
    /// subscripts or reference modifier, a function's arguments, a parenthesized expression) the split question
    /// belongs to that paren's owner, so the walk does not descend there.</summary>
    private static void CollectCuts(IParseTree node, Func<string, List<string>, bool> cannotBeSubscripted,
        List<(Core.DataReferenceContext Ref, Core.SubscriptPartContext List)> cuts)
    {
        switch (node)
        {
            case Core.DataReferenceContext dref:
                if (dref.cobolWord() is not { } head) return;
                var qualifiers = new List<string>();
                foreach (var suffix in dref.dataReferenceSuffix())
                {
                    Core.SubscriptPartContext? sp = suffix.subscriptPart();
                    if (suffix.qualification() is { } q)
                    {
                        qualifiers.Add(q.cobolWord().GetText());
                        if (q.subscriptPart() is [var qsp, ..]) sp = qsp;
                    }
                    if (sp is null) continue;
                    // §8.4.2.3.2 hangs the list off the whole qualified name (kb/Work PB1455's sweep)
                    if (cannotBeSubscripted(head.GetText(), qualifiers)) cuts.Add((dref, sp));
                    return;   // the first list decides; what follows it is that list's own reading
                }
                return;
            case Core.SubscriptPartContext or Core.RefModPartContext or Core.FunctionCallContext:
                return;
            case Core.PrimaryExpressionContext p when p.LPAREN() is not null:
                return;   // GROUPING-PAREN-ONLY: a parenthesized expression's inside is its own level (FNARG_ / REF_ parens belong to functionCall / subscriptPart, handled above)
        }
        for (int i = 0; i < node.ChildCount; i++) CollectCuts(node.GetChild(i), cannotBeSubscripted, cuts);
    }

    /// <summary>Bind one subscript / ref-mod position to its typed <see cref="Position"/> (kb/Work PB2151), or
    /// <see langword="null"/> if neither the direct walk nor the D18 materialization route can bind it (so the
    /// caller fails loud). The direct walk reads the PARSE NODE through the arithmetic grammar and keeps the shapes
    /// that map to an ordinal term by term — integer literals, data-name / index-name references, <c>+ - *</c>,
    /// unary minus and written parentheses, i.e. the relative-subscript and simple-index forms. <b>EVERYTHING ELSE
    /// routes through <see cref="MaterializePosition"/></b>, which binds the same node through the ONE expression
    /// binder; the walk is an optimization over that route, never the arbiter of what is legal in the position
    /// (fix-queue PB42).
    /// <para>⚠ THAT INVARIANT WAS A CLAIM, NOT A FACT, until kb/Work PB170. A name the walk CAN read never reached
    /// the binder, so nothing ever applied §8.8.1.1 to it and <c>T(XE)</c> with <c>XE PIC X(4)</c> compiled clean
    /// under STRICT — the walk WAS deciding the position's legality, by omission. It holds now because
    /// <see cref="ScreenPositionOperandClass"/> asks the same classifier the binder would have asked, so the direct
    /// walk and the D18 route reach the same verdict rather than two different ones.</para>
    /// <para>⛔ THE WALK'S SCREENS ARE DEFERRED TO ITS COMMIT POINT (kb/Work PB220). Its exits to D18 are
    /// ORDER-DEPENDENT — a later operand with no direct form (<c>**</c>), an unresolvable name, or a scaled operand in
    /// a compound abandons the walk AFTER an earlier name was already screened, and the D18 route then screens the
    /// same operand again through the expression binder. Measured: <c>MOVE E(XE ** 2) TO R</c> with <c>XE PIC X(4)</c>
    /// emitted COBOLNET0844 TWICE. So the screens are QUEUED and flushed only when the walk returns a position; every
    /// D18 reroute discards the queue, which makes the deduplication a property of the control flow.</para></summary>
    /// <param name="indexNames">§8.4.2.3.3 SR4's collector — see
    /// <see cref="ResolveSubscriptName(string,List{string},SegmentPosition,ref List{PendingScreen},out bool,ValueTuple{IToken,List{IndexUse}})"/>.</param>
    private Position? BindPosition(PositionSegment seg, SegmentPosition position, List<IndexUse>? indexNames = null)
    {
        var tokens = seg.Tokens;
        // A cut list's position is written in its OWN parentheses, the reference's: it is never a direct term.
        if (seg.FromCutList || seg.Operand?.arithmeticExpression() is not { } expr)
            return MaterializePosition(seg, position);
        // kb/Work PB136: a QUOTIENT-bearing position routes to D18 UNCONDITIONALLY — the direct form would be C#
        // integer division over long reads, truncating where §8.4.2.3.4 GR1b evaluates the exact result of the whole
        // expression and requires EC-BOUND-SUBSCRIPT on a non-integer (`E((W-A + W-B) / 2)` with the sum 7 silently
        // selected occurrence 3).
        if (tokens.Any(t => t.Type is Core.SLASH)) return MaterializePosition(seg, position);
        // ⛔ A COMPOUND position (one carrying an arithmetic operator) whose operands include a SCALED item cannot be
        // read operand by operand, because §8.4.2.3.4 GR1b tests the integrality of THE RESULT of the whole
        // expression, not of each operand: `W-E(W-P + W-Q)` with W-P = W-Q = 1.5 has the integral result 3.0 and is
        // a legal subscript, while de-scaling each operand first yields 1 + 1 = 2 AND raises the condition twice on
        // source that never violated it. Such a position routes to D18, which evaluates the expression at full
        // precision into the §15.4 temp and applies the integrality rule exactly once — to the result. A SINGLE
        // scaled operand needs no such detour: it IS the result, so the direct read is equivalent and cheaper.
        bool compound = tokens.Any(t => t.Type is Core.PLUS or Core.MINUS or Core.STAR or Core.SLASH or Core.POWER);
        List<PendingScreen>? pending = null;
        bool functionChecked = false;
        if (Additive(expr.additiveExpression()) is not { } bound) return MaterializePosition(seg, position);
        if (pending is not null && !_probing)   // R30 purity: a probe never diagnoses (kb/Work PB157)
            foreach (var ps in pending)
                if (ps.Item is { } it) ScreenPositionOperandClass(it, ps.Name, position);
                else IndexNameInPositionError(ps.Name, position);
        return bound;

        Position? Additive(Core.AdditiveExpressionContext a)
        {
            var mults = a.multiplicativeExpression();
            var ops = a.addOp();
            if (Multiplicative(mults[0]) is not { } acc) return null;
            for (int i = 0; i < ops.Length; i++)
            {
                if (Multiplicative(mults[i + 1]) is not { } right) return null;
                acc = new PositionBinary(acc, ops[i].MINUS() is null ? PositionOperator.Add : PositionOperator.Subtract, right);
            }
            return acc;
        }

        Position? Multiplicative(Core.MultiplicativeExpressionContext m)
        {
            var powers = m.powerExpression();
            if (Power(powers[0]) is not { } acc) return null;
            for (int i = 1; i < powers.Length; i++)
            {
                if (Power(powers[i]) is not { } right) return null;   // `/` never reaches here (above)
                acc = new PositionBinary(acc, PositionOperator.Multiply, right);
            }
            return acc;
        }

        // `**` has no direct form (fix-queue PB42): the first operand is read, then the operator abandons the walk.
        Position? Power(Core.PowerExpressionContext p) =>
            Unary(p.unaryExpression(0)) is { } first && p.POWER().Length == 0 ? first : null;

        Position? Unary(Core.UnaryExpressionContext u)
        {
            if (u.addOp() is { } sign)
                return Unary(u.unaryExpression()) is { } operand
                    ? sign.MINUS() is null ? operand : new PositionNegate(operand) : null;
            return Primary(u.primaryExpression());
        }

        Position? Primary(Core.PrimaryExpressionContext p)
        {
            // GROUPING-PAREN-ONLY: primaryExpression's LPAREN is a written grouping parenthesis; a subscript list's
            // REF_ parens and a function's FNARG_ parens belong to dataReference / functionCall, which route to D18.
            if (p.LPAREN() is not null)
                return Additive(p.arithmeticExpression().additiveExpression()) is { } inner ? new PositionGroup(inner) : null;
            if (p.numericLiteral() is { } lit) return IntegerLiteral(lit);
            if (p.dataReference() is not { } dref || !IsNameToken(dref.Start)) return null;
            // A FUNCTION-BEARING position cannot be read term by term (the head word is a function name, not a
            // data-name), so the WHOLE position routes to D18. Asked once, at the first name, over the whole position.
            if (!functionChecked)
            {
                functionChecked = true;
                if (IsFunctionBearing(tokens)) return null;
            }
            // A reference carrying its own subscripts, reference modifier or object qualifier is not a bare name —
            // unless its list is the one the declarations gave to the next subscript (kb/Work PB136).
            var written = ReadWritten(dref);
            if (!ReferenceEquals(dref, seg.Cut) && (written.SubscriptGroup is not null || written.IsReferenceModified))
                return null;
            if (written.PropertyObject is not null) return null;
            // ⛔ AN UNRESOLVABLE NAME ROUTES TO D18 — IT IS NOT A VERDICT (fix-queue PB50): the expression binder
            // reports a genuinely undefined name at the operand itself.
            if (ResolveSubscriptName(dref.cobolWord().GetText(), written.Qualifiers, position, ref pending,
                    out bool scaled, (dref.Start, indexNames)) is not { } read) return null;
            // A scaled operand inside a compound position — evaluate the whole expression instead (above).
            return scaled && compound ? null : read;
        }
    }

    /// <summary>An integer literal (a sign before it is a unary operator of the expression, ISO §8.3.3.3.2) as a
    /// <see cref="PositionConstant"/>; null for any other numeric literal (a decimal or floating-point literal — the
    /// D18 route applies §8.4.2.3.4 GR1 b)'s integrality rule to it) and for one beyond the host's <c>long</c>.</summary>
    private static Position? IntegerLiteral(Core.NumericLiteralContext lit)
    {
        var signed = lit.signedNumericLiteral();
        var core = signed.numericLiteralCore();
        if (core.ChildCount != 1 || core.Start.Type is not (Core.INTEGERLIT or Core.SIGNED_INTEGERLIT)) return null;
        // THE ONE integer-literal reader (kb/Work PB1058 / PB1579), at the position intakes' long width: a literal past
        // it saturates and stays out of every range.
        if (!CobolNet.Validation.IntegerOperandRules.TryHostPosition(core.Start.Text, out long value, out _)) return null;
        Position constant = new PositionConstant(value);
        return signed.MINUS() is null ? constant : new PositionNegate(constant);
    }

    /// <summary>A screen the fast path OWES once it commits to rendering the segment — either the §8.8.1.1 class
    /// screen over a resolved item (<see cref="ScreenPositionOperandClass"/>) or the §13.18.38.3 r7 index-name
    /// screen (<see cref="IndexNameInPositionError"/>, <c>Item</c> null). Queued rather than emitted so an exit
    /// to D18 later in the token loop cannot leave a diagnostic behind for the D18 route to duplicate — see
    /// <see cref="BindPosition"/>.</summary>
    private readonly record struct PendingScreen(DataItem? Item, string Name);

    /// <summary>⛔ §8.4.2.3.3 SR4 — THE ONE CONSUMER OF THE INDEX→TABLE ASSOCIATION on the reference side:
    /// "Index-name-1 shall correspond to a data description entry in the hierarchy of the table being referenced
    /// that contains an INDEXED BY phrase specifying that index-name."
    /// <para>kb/Work PB459 measured the hole: §14.9.39.4 GR1 builds the association ("Index-names are associated
    /// with a given table by being specified in the INDEXED BY phrase of the OCCURS clause for that table") and
    /// <c>DataItem.IndexNames</c> holds it, but outside SEARCH nothing read it — so
    /// <c>MOVE 77 TO E2(IX1)</c>, with IX1 the index of an unrelated table, compiled clean and wrote through that
    /// other table's occurrence number, with no diagnostic at any stage.</para>
    /// <para>The rule is applied EXACTLY as written: the index-name must appear in SOME entry of the referenced
    /// item's own hierarchy — SR4 says "in the hierarchy of the table being referenced", not "on the OCCURS level
    /// this subscript position selects", so a multi-dimensional reference that names its own table's indexes out
    /// of dimension order is NOT refused here. Reading the extra restriction in would reject source the standard
    /// admits.</para>
    /// <para>A REDEFINES / RENAMES view resolves to its own entry, and an index-name declared on the redefining
    /// entry is found through that entry's own hierarchy — no special case.</para></summary>
    internal void ScreenIndexNameAssociation(DataItem item, List<IndexUse> indexNames)
    {
        if (indexNames.Count == 0 || _probing) return;   // R30 purity: a probe never diagnoses (kb/Work PB157)
        foreach (var (t, decl) in indexNames)
        {
            if (DeclaredInHierarchy(item, decl)) continue;
            data.Edition.Error(DiagnosticCatalog.IndexNameNotInTable,
                $"'{item.CobolName}' subscripted by the index-name '{t.Text}', which is not in the INDEXED BY "
                + "phrase of any OCCURS clause in this item's hierarchy (ISO §8.4.2.3.3 SR4)");
        }
    }

    /// <summary>True when the RESOLVED declaration <paramref name="decl"/> belongs to an entry of
    /// <paramref name="item"/>'s own hierarchy — the item itself or any containing entry (§8.4.2.3.3 SR4's
    /// "hierarchy of the table being referenced"). ⛔ BY DECLARATION IDENTITY, NOT SPELLING (kb/Work PB919): with
    /// two tables each <c>INDEXED BY IX</c>, <c>EB(IX OF EA)</c> names the index of the OTHER table, and a
    /// spelling test would pass it because EB's hierarchy also declares an IX.</summary>
    private static bool DeclaredInHierarchy(DataItem item, IndexDeclaration decl)
    {
        for (DataItem? e = item; e is not null; e = e.Parent)
            if (ReferenceEquals(e, decl.Table)) return true;
        return false;
    }

    /// <summary>One index-name written as a subscript and the declaration it resolved to — §8.4.2.3.3 SR4's
    /// collector entry (<see cref="ScreenIndexNameAssociation"/>).</summary>
    internal readonly record struct IndexUse(IToken Token, IndexDeclaration Decl);

    /// <summary>What a written name resolved to as an INDEX-NAME (kb/Work PB919).</summary>
    internal enum IndexRefOutcome
    {
        /// <summary>The spelling names no index-name visible here — resolve it as a data-name.</summary>
        NotAnIndexName,
        /// <summary>Exactly one declaration survives the qualification — <see cref="IndexRef.Decl"/>.</summary>
        Resolved,
        /// <summary>An index-name reference that identifies no single declaration: already REPORTED (or, in a
        /// probe, deliberately not). The caller must not re-resolve it as a data-name, which would report a
        /// second, false "not defined".</summary>
        Failed,
    }

    /// <summary>The outcome of <see cref="ResolveIndexName"/>.</summary>
    internal readonly record struct IndexRef(IndexRefOutcome Outcome, IndexDeclaration? Decl)
    {
        /// <summary>The C# read of the reference: the declaration's cell, or — for a failed reference whose error
        /// already fails the compile — an inert occurrence number, so the bind continues without a cascade.</summary>
        public string Cell => Decl?.Cell ?? "1";
    }

    /// <summary>Written index-name references already reported, keyed by the written node (a
    /// <see cref="Core.DataReferenceContext"/> or the subscript's name <see cref="IToken"/>): several binders ask
    /// about the same reference (SET classifies its operand, then binds it).</summary>
    private readonly HashSet<object> _indexRefsReported = new(ReferenceEqualityComparer.Instance);

    /// <summary>⛔ THE ONE RESOLUTION OF A WRITTEN INDEX-NAME REFERENCE — <c>index-name-1 [OF|IN qualifier] …</c>
    /// (ISO §8.4.2.2.2 Format 3) — for EVERY position that admits one: a subscript (the position walk) and the
    /// identifier positions of SET, PERFORM / SEARCH VARYING and the relation condition (through
    /// <c>ExpressionBinder.IndexFieldOf</c>). kb/Work PB919: both used to look the bare spelling up in a
    /// <c>name → cell</c> map, so a qualified reference was "not defined" and a duplicated one silently read the
    /// cell the two tables shared.
    /// <para>The candidate set is <see cref="Model.SymbolTable.IndexCandidates"/>'s; the count is §8.4.2.2.3 SR1's
    /// ("For each non unique user-defined name that is explicitly referenced, uniqueness shall be established
    /// through a sequence of qualifiers that precludes any ambiguity of reference"), judged by the ONE verdict
    /// <see cref="DataBinder.UniqueOrReportAmbiguous{T}"/> — the data-name's own diagnostic (COBOLNET1639) and
    /// its <c>--permissive</c> disposition. A spelling that IS an index-name but whose qualifiers name no table
    /// declaring it is the same diagnostic's "not defined" arm, citing SR6.</para></summary>
    internal IndexRef ResolveIndexName(string name, IReadOnlyList<string> qualifiers, object writtenAt)
    {
        if (data.Symbols.IndexCandidates(name, qualifiers, data.ActiveScope) is not { } cands)
            return new IndexRef(IndexRefOutcome.NotAnIndexName, null);
        if (cands.Single is { } one)
        {
            ScreenLinkageIndex(one, name, qualifiers, writtenAt);
            return new IndexRef(IndexRefOutcome.Resolved, one);
        }
        // R30 purity (kb/Work PB157): a probe never diagnoses, and the committing resolution reports once.
        if (_probing || !_indexRefsReported.Add(writtenAt)) return new IndexRef(IndexRefOutcome.Failed, null);
        string written = DataBinder.WrittenQualified(name, qualifiers);
        if (cands.Count == 0)
        {
            data.Edition.Error(DiagnosticCatalog.UndefinedReference, $"'{written}' is not defined — '{name}' is an "
                + "index-name, but no table that declares it is named by the written qualifiers (ISO §8.4.2.2.3 SR6: "
                + "\"The qualification of an index-name may include the name of the table with which the index-name is "
                + "associated, as well as any name by which that table may be qualified.\")");
            return new IndexRef(IndexRefOutcome.Failed, null);
        }
        return data.UniqueOrReportAmbiguous(cands, "the index-name reference", written, out _) is { } first
            ? new IndexRef(IndexRefOutcome.Resolved, first)   // --permissive: warned, first declaration
            : new IndexRef(IndexRefOutcome.Failed, null);
    }

    /// <summary>The written index-name references already reported as §13.7.3 SR4 e) violations.</summary>
    private readonly HashSet<object> _linkageIndexReported = new(ReferenceEqualityComparer.Instance);

    /// <summary>§13.7.3 SR4 e): an index-name of a linkage table is referenceable exactly when the table is — the
    /// decision <see cref="ScreenLinkageReference"/> asks of a data-name, asked once per written reference (a probe
    /// never reports, R30).</summary>
    private void ScreenLinkageIndex(IndexDeclaration decl, string name, IReadOnlyList<string> qualifiers, object writtenAt)
    {
        if (_probing || data.UnreferenceableLinkageRecord(decl.Table) is not { } record
            || !_linkageIndexReported.Add(writtenAt)) return;
        ReportUnreferenceableLinkage(DataBinder.WrittenQualified(name, qualifiers), record);
    }

    /// <summary>True when this segment contains a FUNCTION-IDENTIFIER (ISO §8.4.3.1.2 Format 1) and therefore
    /// belongs to the D18 materialization route rather than the direct position walk: either the explicit
    /// <c>FUNCTION</c> keyword, or the §8.4.3.2.3 SR2 keyword-omitted form — a REPOSITORY-declared intrinsic or a
    /// user-function name, immediately followed by its parenthesis, that is NOT shadowed by a declared data item (a declared item always wins, exactly as in
    /// <c>IntrinsicBinder.KeywordOmittedFunction</c>; the two must not drift apart, which is why both ask the
    /// question the same way).</summary>
    private bool IsFunctionBearing(List<IToken> tokens)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type == Core.FUNCTION) return Activation(tokens[i]);
            if (!IsNameToken(tokens[i])) continue;
            string w = tokens[i].Text;
            // This arm detects the KEYWORD-OMITTED form `name(args)`, which by definition has no FUNCTION token
            // before the name — so its '(' is never retyped FNARG_LPAREN (the lexer's mark keys on exactly that
            // token): it is the reference paren REF_LPAREN the lexer gives every '(' after a name (kb/Work PB2113).
            // The explicit-keyword form is caught by the FUNCTION test above and never reaches here.
            if (i + 1 >= tokens.Count || tokens[i + 1].Type != Core.REF_LPAREN) continue;
            if (data.Symbols.TryResolve(w, data.ActiveScope, out _)) continue;   // a declared item wins
            // The REPOSITORY half is the ONE membership the declaration screen and KeywordOmittedFunction ask
            // (DataBinder.IsRepositoryIntrinsic — >>COBOL-WORDS and the edition window included; kb/Work PB1083).
            // An IDENTIFIER keeps its WRITTEN word (the lexer retypes only keyword-token words), so the directive is
            // applied to it once, here.
            if (data.UserFunctionNames.Contains(w)
                || data.IsRepositoryIntrinsic(FunctionWord.OfWrittenWord(w, data.CobolWords)))
                return Activation(tokens[i]);
        }
        return false;

        // The function-identifier's head token IS an activation site (ISO §7.3.14.4 GR4 b reads it back from
        // DataBinder.ActivationSites — the subscript has no FunctionCall parse node of its own).
        bool Activation(IToken head)
        {
            data.ActivationSites.Add(head);
            return true;
        }
    }

    /// <summary>The D18 route (fix-queue PB17, widened by PB42): materialize ANY position the direct walk cannot
    /// read into the §15.4 temporary, and read the position as that temp's ordinary position read. The position's
    /// PARSE NODE — its <c>arithmeticExpression</c> — is handed to the <see cref="MaterializeSegment"/> hook, which
    /// binds it through the ONE expression binder (kb/Work PB2151: the node is bound where the grammar parsed it; the
    /// verbatim-text re-parse through an isolated fragment rule is gone).
    /// <para>⛔ IT IS DELIBERATELY NOT GATED ON A TOKEN LIST. The ARITHMETIC-EXPRESSION GRAMMAR decides
    /// admissibility: a position that did not parse as an arithmetic expression — the word ALL outside §8.4.2.3.3
    /// SR6's two contexts, a nonnumeric literal, a piece of an item the declarations cut that has no subscript
    /// reading (<see cref="SegmentsOf"/>) — is refused here by name (COBOLNET2363, kb/Work PB1030) and never reaches
    /// the hook. That is what lets a function-identifier, <c>**</c>, a decimal literal and every
    /// future arithmetic form share ONE route with no per-token edit.</para>
    /// <para>Null when the hook is absent (a data-division resolver builds a throwaway resolver with no procedure
    /// binder) or the hook refuses or defers the position — in every such case the caller's loud posture stands.</para></summary>
    private Position? MaterializePosition(PositionSegment seg, SegmentPosition position)
    {
        // R30 PURITY (kb/Work PB157): a probe must not BIND the position — the hook registers a §15.4 pre-op and a
        // function-bearing position would activate TWICE (once per probe+commit). The probe's Place is discarded
        // after its Item is read, so the dummy occurrence is never emitted.
        if (_probing) return new PositionConstant(1);
        var tokens = seg.Tokens;
        if (MaterializeSegment is null || tokens.Count == 0) return null;
        // A reference the declarations cut (kb/Work PB136) is read without its list only by the direct walk; inside an
        // expression the walk cannot read, the tree has no reading of the cut, so the position is refused by name.
        if ((seg.Cut is null ? seg.Operand?.arithmeticExpression() : null) is not { } expr)
        {
            // ⛔ A POSITION THAT IS NOT AN ARITHMETIC EXPRESSION IS NOT A SUBSCRIPT (kb/Work PB1030). §8.4.2.3.2 writes a
            // subscript as ALL, arithmetic-expression-1, or index-name-1 [{+|-} integer-1] (the last is read directly
            // and never arrives here), and §8.4.3.3.3 SR4 makes both reference-modifier bounds arithmetic expressions.
            // What did not parse as one is the word ALL outside the two places §8.4.2.3.3 SR6 admits it (an
            // intrinsic-function argument or a SORT table's rightmost subscript, neither of which resolves through
            // here), or not a subscript at all (`E("A")`). It used to be returned unreported, and the caller bound a
            // run-time NotImplemented that aborted the run unit.
            string text = tokens[0].InputStream is { } stream
                ? stream.GetText(new Antlr4.Runtime.Misc.Interval(tokens[0].StartIndex, tokens[^1].StopIndex))
                : string.Join(" ", tokens.Select(t => t.Text));
            data.Edition.Error(DiagnosticCatalog.NotASubscript, IllegalPositionMessage(text.Trim(), position));
            return null;
        }
        return MaterializeSegment(expr, position, tokens[0].Line) is { } temp ? PositionRead(temp, position) : null;
    }

    /// <summary>The COBOLNET2363 text for a position that is not an arithmetic expression (kb/Work PB1030) — the
    /// word ALL in a subscript names §8.4.2.3.3 SR6's two contexts; anything else names the position's own rule.</summary>
    private static string IllegalPositionMessage(string segment, SegmentPosition position) =>
        position == SegmentPosition.RefMod
            ? $"'{segment}' is written as a reference-modification bound, but a leftmost-position and a length shall "
              + "be arithmetic expressions (ISO §8.4.3.3.3 SR4)."
        : CobolNames.Same(segment, "ALL")
            ? "the subscript ALL is written where it is not permitted: ISO §8.4.2.3.3 SR6 — \"The subscript ALL may "
              + "be used only\" when the subscripted identifier is an intrinsic function argument, or as the rightmost "
              + "or only subscript of a table in the table format of a SORT statement. Write the occurrence you mean."
        : $"'{segment}' is written as a subscript, but a subscript is ALL, an arithmetic expression, or an index-name "
          + "optionally followed by + or - and an integer (ISO §8.4.2.3.2).";

    /// <summary>A subscript data-name → its C# read expression: an INDEXED BY index-name (a <c>long</c> field), or
    /// a (possibly OF/IN-qualified, ISO §8.4.2.3.2) numeric data item read; <see langword="null"/> if unresolvable.
    /// A data-item read is wrapped in <c>CobolTable.Occ(…)</c> — overload resolution converts a STRING-stored
    /// occurrence number (a leaf the post-bind whole-group analysis flags <see cref="DataItem.StoreAsImage"/>, a
    /// decision NOT yet made when this bind-time text is produced) exactly as a native <c>long</c>.</summary>
    /// <param name="scaled">Set when the resolved operand carries a nonzero PICTURE scale, so the caller can send a
    /// COMPOUND segment to the D18 materializer instead (the §8.4.2.3.4 GR1b result-vs-operand distinction).</param>
    /// <param name="nameToken">The name's own token, and the collector it is recorded into when it resolves to an
    /// index-name IN A SUBSCRIPT position: the caller is the only frame that knows WHICH table is being
    /// referenced, so §8.4.2.3.3 SR4 is applied there (<see cref="ScreenIndexNameAssociation"/>). Threaded rather
    /// than kept on the resolver because the D18 materializer can re-enter this resolver for a nested
    /// reference (kb/Work PB459).</param>
    private Position? ResolveSubscriptName(string name, List<string> qualifiers, SegmentPosition position,
        ref List<PendingScreen>? pending, out bool scaled, (IToken Token, List<IndexUse>? Into) nameToken)
    {
        scaled = false;
        // An index-name is an occurrence number by construction (§13.18.38) and a constant-name substitutes an
        // INTEGER literal — neither can be scaled, so both keep the fast path.
        // ⛔ BUT ONLY IN THE POSITION r7 LISTS (kb/Work PB170). §13.18.38.3 r7's five contexts include "as a
        // subscript" and do NOT include a reference-modification position, and this line returned the index
        // field regardless of `position` — so `W(IX:2)` compiled clean. The R16 screen for exactly this rule
        // already existed (ExpressionBinder.ScreenIndexNameOperand); it simply was not applied here.
        // The name may be QUALIFIED (§8.4.2.2.2 Format 3 / §8.4.2.2.3 SR6 — `E(IX OF T)`), and a duplicated one
        // must be (SR1): the ONE resolution decides both (kb/Work PB919).
        if (ResolveIndexName(name, qualifiers, nameToken.Token) is { Outcome: not IndexRefOutcome.NotAnIndexName } ix)
        {
            if (position == SegmentPosition.Subscript)
            {
                // §8.4.2.3.3 SR4 — screened by the caller, which has the table
                if (ix.Decl is { } decl) nameToken.Into?.Add(new IndexUse(nameToken.Token, decl));
                return IndexCellOf(ix);
            }
            (pending ??= []).Add(new PendingScreen(null, name));
            return IndexCellOf(ix);   // keep reading: a null here would re-route to D18 and screen the operand twice
        }
        // An INTEGER constant-name in a subscript position substitutes its integer literal (ISO §13.10.3 SR2 /
        // §13.10.4 GR1/GR3 — a subscript is a literal position, §8.4.2.3.2) — its VALUE is the position, read by THE ONE
        // integer-literal reader (kb/Work PB1579).
        if (qualifiers.Count == 0
            && data.FindConstant(name) is { Category: PicCategory.Numeric, IntegerText: { } integer })
            return CobolNet.Validation.IntegerOperandRules.TryHostPosition(integer, out long value, out _) ? new PositionConstant(value) : null;
        // A report VARYING counter in scope (§13.18.64.4 GR4's NOTE: "data-name-1 [may] be used … as a subscript to a source
        // data item") is the compose-local integer the placement declares; it is no data item and has no scale.
        if (qualifiers.Count == 0 && VaryingScope is { } counters && counters.TryGetValue(name, out var counter))
            return new PositionLocal(counter.CsName, AsLong: true);
        DataItem? item = qualifiers.Count == 0 ? ResolveUnqualified(name) : ResolveQualified(name, qualifiers);
        if (item is null) return null;
        if (!IntrinsicArgumentRules.IsArithmeticOperandClass(item)) (pending ??= []).Add(new PendingScreen(item, name));
        scaled = item.Pic?.Scale > 0;
        return PositionRead(item, position);
    }

    /// <summary>⛔ §13.18.38.3 r7 AT THE TOKEN RENDERER'S REF-MOD BOUND — <b>ONE RULE, ONE LANE POSTURE, DECIDED
    /// BY THE SLOT KIND</b> (kb/Work PB219). r7 is enforced at four sites and they were not all on the same
    /// disposition; the axis that settles each one is what the slot IS, never which route reached it:
    /// <list type="bullet">
    ///   <item><b>An ARITHMETIC-expression position</b> — <c>ExpressionBinder.IndexNameExpr</c> (R29) and THIS
    ///   site. §8.4.3.3.3 SR4 makes a reference-modification leftmost-position/length an arithmetic expression,
    ///   so the ref-mod bound is R29's family, not R16's. Posture: strict REJECTS with the r7 citation;
    ///   <c>--permissive</c> WARNS and computes the occurrence number (the documented GnuCOBOL coercion). The
    ///   emit floor is identical either way — this method's caller renders <c>field</c> in both lanes.</item>
    ///   <item><b>An IDENTIFIER slot</b> — <c>ExpressionBinder.ScreenIndexNameOperand</c> (R16: DISPLAY, MOVE,
    ///   STRING, the STOP RUN/GOBACK status operand) and <c>InspectBinder</c>. Posture: an unconditional Error in
    ///   BOTH lanes, and the written reason is that THERE IS NO COERCION TO OFFER — the slot needs an identifier,
    ///   and an occurrence number is not one. That is a leniency this compiler declines to invent, not an
    ///   oversight; <c>dialect_two_axes</c> constrains the leniencies you implement, it does not require one.</item>
    /// </list>
    /// Before this, the ref-mod fast path carried the R16 posture while its OWN D18 route carried R29's, so
    /// under <c>--permissive</c> <c>W(IX:2)</c> was a hard error and <c>W(IX / 1:2)</c> — the same rule, the same
    /// position — warned and compiled, keyed on nothing but whether the direct walk could read the
    /// bound.</summary>
    private void IndexNameInPositionError(string name, SegmentPosition position)
    {
        string what = $"the index-name '{name}' is not an identifier (ISO §8.4.3.1.2) and a "
            + "reference-modification leftmost-position/length is not one of §13.18.38.3 r7's five contexts (a "
            + "subscript, PERFORM/SEARCH VARYING, SET, a relation condition) — §8.4.3.3.3 SR4 makes both bounds "
            + "arithmetic expressions";
        if (data.Edition.Permissive)
            data.Edition.Warning(DiagnosticCatalog.IndexNameContext,
                $"{what}; accepted under --permissive, computing the occurrence number");
        else
            data.Edition.Error(DiagnosticCatalog.IndexNameContext,
                $"{what}. SET a data item to the index first (SET data-item TO {name})");
    }

    /// <summary>⛔ THE §8.8.1.1 CLASS SCREEN FOR THE TOKEN RENDERER'S FAST PATH (kb/Work PB170) — the funnel entry
    /// that never reached the funnel.
    /// <para><b>The chain, every link cite.py-checked.</b> §8.4.2.3.2's subscript general format is exactly three
    /// alternatives — <c>ALL | arithmetic-expression-1 | index-name-1 [{+|-} integer-1]</c> — so a bare data-name
    /// subscript is admitted ONLY as arithmetic-expression-1; §8.8.1.1 then admits "an identifier referencing a
    /// NUMERIC data item, a numeric literal, the figurative constant ZERO"; §8.5.2.1 Table 2 puts category
    /// alphanumeric and alphanumeric-edited in class ALPHANUMERIC. §8.4.3.3.3 SR4 ("leftmost-position and length
    /// shall be arithmetic expressions") carries the identical rule to the ref-mod bounds. So <c>T(XE)</c> and
    /// <c>W(XE:2)</c> with <c>XE PIC X(4)</c> are illegal, and <c>T(IDX)</c> with an index DATA item is illegal
    /// twice over (§13.18.60.3 SR10's closed reference list has no subscript entry).</para>
    /// <para><b>Why the screen has to be HERE.</b> <see cref="ResolveSubscriptName"/> renders the position read
    /// straight from the token — <c>ExpressionBinder.OperandRef</c> never runs, because the operand never enters
    /// the expression binder at all. Measured on 9a89fbd1: <c>E(XE)</c> compiled clean and digit-decoded "0002"
    /// to occurrence 2, and so did the COMPOUND <c>E(XE + 1)</c> — the renderer routes to the screened D18 path
    /// only for a slash, a function, an unresolvable name, a SCALED operand inside a compound, or a token with no
    /// case arm, and plain <c>+</c> over an unscaled alphanumeric name is none of those.</para>
    /// <para>⚠ THE VERDICT IS <see cref="IntrinsicArgumentRules"/>'s, deliberately: a category switch written here
    /// would have been the fifth place answering "is this operand class numeric". Routing the segment to D18
    /// instead — which would reuse OperandRef's screen with no new code — was REJECTED: it materializes a §15.4
    /// temp and a PendingPreOp for every permissive alphanumeric subscript, changing permissive-lane emitted text
    /// and adding an integrality check where <c>CobolTable.Occ(string)</c> has none. Screening in place keeps the
    /// emit floor byte-identical.</para>
    /// <para>⚠ AND THE RENDERER'S OWN INVARIANT IS RESTORED, not abandoned: <see cref="BindPosition"/> documents
    /// itself as "an optimization over that route, never the arbiter of what is legal in the position", and this
    /// defect falsified it. Asking the ONE classifier the question the binder would have asked is what makes the
    /// sentence true again — the fast path now reaches the same verdict, not a different one.</para>
    /// <para>⚠ PRECONDITION: the class question is asked in <see cref="ResolveSubscriptName"/>
    /// (<c>IntrinsicArgumentRules.IsArithmeticOperandClass</c>) and only a REJECTED item is queued, so this
    /// method composes the message and nothing else. The <c>_probing</c> purity guard lives at the queue's flush
    /// in <see cref="BindPosition"/> — ONE place, since that is also where the ordering fix lives
    /// (kb/Work PB220).</para></summary>
    private void ScreenPositionOperandClass(DataItem item, string name, SegmentPosition position)
    {
        // ⛔ NO GROUP / POINTER / OBJECT-REFERENCE ARM, AND THAT IS A REACHABILITY FACT, NOT AN OMISSION
        // (kb/Work PB201). This method runs only when the fast path COMMITS to rendering the segment, and
        // <see cref="HasPositionOverload"/> lets it commit only for a carrier the position read declares a
        // parameter for — <c>long</c> or <c>string</c> for a non-numeric operand (numeric ones add the wide tiers). A group's
        // carrier is its per-program <c>record struct</c> and a pointer's is <c>ManagedPointer</c>, so
        // both now route to D18 and are screened by <c>ExpressionBinder.OperandRef</c> instead — the same
        // COBOLNET0844 over the same §8.8.1.1, minus this method's position phrase.
        // ⚠ THE INTERSECTION IS NARROWER THAN THAT LIST, and it is the second precondition that narrows it: only
        // an item <c>IntrinsicArgumentRules.IsArithmeticOperandClass</c> REJECTED is ever queued, and the
        // numeric-only carriers belong to class NUMERIC items, which it accepts. So what reaches HERE is
        // exactly: an index DATA item (an <c>IndexCell</c> is a <c>long</c>) and the string-carrier categories
        // — alphanumeric, national, boolean, numeric-edited and their edited forms.
        string what = item.Pic is { Usage: Usage.Index }
                ? $"item '{name}', an index data item (class index, ISO §8.5.2.1 Table 2; §13.18.60.3 SR10 admits "
                  + "an index data item only in SEARCH/SET, a relation condition, or an intrinsic argument)"
            : $"item '{name}' of class "
              + $"{IntrinsicArgumentRules.ClassOfItem(item)?.ToString().ToLowerInvariant() ?? "unknown"} "
              + "(ISO §8.5.2.1 Table 2)";
        string where = position == SegmentPosition.Subscript
            ? "a subscript is arithmetic-expression-1 (ISO §8.4.2.3.2)"
            : "a reference-modification leftmost-position/length is an arithmetic expression (ISO §8.4.3.3.3 SR4)";
        // The SAME dialect gate the expression-binder screen carries (dialect_two_axes — every leniency is
        // dialect-gated): --permissive keeps the CobolTable.Occ(string) digit decode, which is the leniency
        // already implemented by that overload, and the emitted text is unchanged either way.
        if (data.Edition.Permissive)
            data.Edition.Warning("COBOLNET0844", $"{what} is not a numeric operand — {where}, and ISO §8.8.1.1 "
                + "admits only an identifier referencing a NUMERIC data item, a numeric literal, or the "
                + "figurative constant ZERO; accepted under --permissive, decoding its digit characters as an "
                + "unsigned integer");
        else
            data.Edition.Error("COBOLNET0844", $"{what} is not a numeric operand: {where}, and ISO §8.8.1.1 "
                + "admits only an identifier referencing a NUMERIC data item, a numeric literal, or the "
                + "figurative constant ZERO. --permissive accepts it as a digit-decoding extension");
    }

    /// <summary>⛔ THE ONE ORDINAL-POSITION READ (fix-queue PB41): an already-resolved numeric item → the C#
    /// <c>long</c> expression for the POSITION it denotes, in either position kind.
    /// <para>A WiseOwl COBOL numeric item stores UNSCALED — <c>PIC 9V9 VALUE 2.0</c> is the field <c>20L</c> at scale
    /// 1 — so the item's VALUE and its STORAGE are different numbers whenever the PICTURE has a <c>V</c>. Both
    /// position clauses are about the VALUE: §8.4.2.3.4 GR1b makes the subscript "the result of the evaluation of
    /// arithmetic-expression-1", and §8.4.3.3.4 rule 5)c) says the same for a leftmost-position/length. Reading the
    /// storage instead is what made <c>W-E(W-S)</c> with <c>W-S = 2.0</c> index occurrence 20 and return the
    /// out-of-range scratch.</para>
    /// <para>⛔ A NUMERIC item reads through its OWN PROFILE, at every scale and in both positions:
    /// <c>CobolTable.Occ(path, _P_n)</c> / <c>CobolString.RefModPosition(path, _P_n)</c>. The profile carries the
    /// scale (GR1b's integrality, and a trailing-P item's multiplier), the sign, and the byte form §14.6.13.2 rule
    /// 2's check needs when the post-bind whole-group analysis stores the item as its character image — a position
    /// is ITEM IDENTIFICATION, which rule 1 names as checked even inside a class condition (kb/Work PB1117). The
    /// profile-less form this replaced decoded that image through a tolerant digit scan: EC-DATA-INCOMPATIBLE was
    /// unreachable and a signed image's sign was dropped. Only a NON-numeric operand — the two <c>--permissive</c>
    /// carriers COBOLNET0844 admits — keeps the bare <c>CobolTable.Occ(path)</c> digit decode.</para>
    /// <para>The read is a typed <see cref="PositionItemRead"/> (kb/Work PB2151): the item, its structural path
    /// and the overload form; <c>CodeGen.PositionRenderer</c> renders it through <c>RuntimeApi</c>.</para></summary>
    private PositionItemRead? PositionRead(DataItem item, SegmentPosition position)
    {
        bool numeric = item.Pic is { Category: PicCategory.Numeric };
        // ⛔ THE BET ON OVERLOAD RESOLUTION IS ONLY GOOD FOR THE CARRIERS THAT HAVE AN OVERLOAD (kb/Work PB201).
        if (!HasPositionOverload(item, numeric)) return null;
        // An unsubscripted read: an item inside a table has no path without its subscripts (§8.4.2.3.3 SR3's count).
        if (BuildAccessPath(item, []) is not { } path) return null;
        return new PositionItemRead(path, item,
            !numeric ? PositionReadForm.Digits
            : position == SegmentPosition.Subscript ? PositionReadForm.Occurrence
            : PositionReadForm.RefModPosition);
    }

    /// <summary>An index-name reference as a position: its declaration's cell, or — for a failed reference whose
    /// error already fails the compile — an inert occurrence number, so the bind continues without a cascade.</summary>
    private static Position IndexCellOf(IndexRef ix) =>
        ix.Decl is { } decl ? new PositionIndexCell(decl.Cell) : new PositionConstant(1);

    /// <summary>⛔ THE FAST PATH'S ADMISSION TEST (kb/Work PB201): can the C# text <see cref="PositionRead"/> is
    /// about to emit actually BIND against the operand's generated field? The bind-time renderer names the field
    /// and lets C# overload resolution supply the conversion — the deliberate design that lets ONE text serve a
    /// carrier the post-bind whole-group analysis has not chosen yet — but that bet is good ONLY for the carrier
    /// types the emitted helper declares a parameter for.
    /// <para><b>The overload sets are the whole rule.</b> A NUMERIC operand renders the profile arity —
    /// <c>CobolTable.Occ(x, in NumProfile)</c> / <c>CobolString.RefModPosition(x, in NumProfile)</c> — which takes
    /// <c>long | string | Int128 | ulong | UInt128</c> at every scale (the profile carries the scale); a
    /// NON-numeric <c>--permissive</c> operand renders <c>CobolTable.Occ(x)</c>, which takes <c>long | string</c>.
    /// The remaining carriers <see cref="DataItem.ElementType"/> can produce — <c>double</c>/<c>float</c> (a COMP-1/COMP-2
    /// leaf), <c>ManagedPointer</c>/<c>ProgramPointer</c>, an object reference, and a GROUP's <c>record struct</c>
    /// name — have none, and the emitted text was therefore not C# that compiles.
    /// ⚠ The FLOAT exclusion is a RULE, not a missing overload: a <c>double</c> operand can be
    /// fractional and §8.4.2.3.4 GR1b sets EC-BOUND-SUBSCRIPT when the expression "does not result in an
    /// integer", a test the scale-less overload does not perform — so a float belongs on the D18
    /// route, where the §15.4 temp applies the rule once, to the result.
    /// MEASURED, six shapes, before this fix: <c>TE(FD1)</c> with <c>FD1 USAGE COMP-2</c>,
    /// <c>TE(BIG)</c> with <c>BIG PIC 9(20) COMP</c> and <c>TE(W-U)</c> with
    /// <c>W-U USAGE BINARY-DOUBLE UNSIGNED</c> each failed the BACKEND with
    /// <c>CS1503 cannot convert from 'double'/'System.Int128'/'ulong' to 'long'</c> — in the DEFAULT lane, on source
    /// §8.8.1.1 and §8.4.2.3.4 GR1b make perfectly legal — <c>W(FD1:2)</c> the same at a ref-mod bound, and a
    /// GROUP subscript under <c>--permissive</c> emitted the record struct (<c>CS1503 '_T_0' to 'long'</c>) or, for
    /// a class-tier BASED group, a name with no C# field at all (<c>CS0103</c>).</para>
    /// <para><b>Why a route and not only wider overloads.</b> The three INTEGER carriers above genuinely were
    /// missing overloads and got them (they are also needed by the second emitter, <c>PlaceRenderer.CountRead</c>,
    /// which renders the OCCURS DEPENDING current count at CODEGEN time and has no route to fall back to). But
    /// overloads alone can never close this: a group's carrier can never have a runtime overload, being a
    /// per-program generated type, and a pointer has no numeric value to convert at all. The
    /// route already exists and is the documented posture: <see cref="BindPosition"/> is "an optimization over
    /// that route, never the arbiter of what is legal in the position", so an operand it cannot render is
    /// <see cref="MaterializePosition"/>'s, where <c>ExpressionBinder</c> screens it under §8.8.1.1 and
    /// <c>NumericRenderer.FieldNum</c> — THE ONE numeric read — supplies the carrier-correct decode (the float
    /// sending check, the wide tier, and a group's §8.5.2.1 alphanumeric IMAGE, which is exactly the digit decode
    /// the <c>--permissive</c> message promises and the fast path never performed).</para>
    /// <para>⚠ Returning FALSE is always sound and never a verdict: the segment reroutes to D18, whose loud
    /// posture on failure is the caller's pre-existing one. The pre-promotion reading of <c>ElementType</c> is
    /// likewise safe in the one direction it can be wrong — promotion only ever moves a leaf TO
    /// <c>CharImage</c>/<c>string</c>, which is in the set, so a leaf admitted here stays admitted.</para></summary>
    private static bool HasPositionOverload(DataItem item, bool numeric)
    {
        // A Tier-B class member — an EXTERNAL or BASED item, an ADDRESS-OF-taken record, a REDEFINES view — is a
        // window over the class backing and owns no field of its own name, so the fast path's bare name was
        // `CS0103` (`E (EXT-I)` with `EXT-I EXTERNAL`: found by kb/Work PB1250's golden repair). The D18 route
        // reads it through its place.
        if (item.Class is { Tier: RedefinesTier.StringCanonical }) return false;
        string carrier = item.ElementType;
        foreach (string admitted in numeric ? NumericPositionCarriers : NonNumericPositionCarriers)
            if (admitted == carrier) return true;
        return false;
    }

    /// <summary>The carrier types the NUMERIC position read — <c>CobolTable.Occ(<i>x</i>, in NumProfile)</c> and
    /// <c>CobolString.RefModPosition(<i>x</i>, in NumProfile)</c> — declares a parameter for: the ONE list
    /// <see cref="HasPositionOverload"/> reads for a numeric operand, exposed so
    /// <c>PositionCarrierOverloadDriftTests</c> can compare it to both runtime methods' ACTUAL overloads by
    /// reflection. Adding an overload without widening this list leaves the fast path routing a carrier it could
    /// now render; widening this list without the overload puts the CS1503 back. The test fails on either.
    /// <c>Int128</c> must be here for a second reason: the D18 segment temp is a 19-digit integer position item —
    /// the wide tier — and <see cref="MaterializePosition"/> reads it back through <see cref="PositionRead"/>.</summary>
    internal static readonly string[] NumericPositionCarriers =
        ["long", "string", "Int128", "ulong", "UInt128"];

    /// <summary>The carrier types <c>CobolTable.Occ(<i>x</i>)</c> declares a parameter for — a NON-numeric
    /// position operand, which only <c>--permissive</c> admits (COBOLNET0844): an index data item's <c>long</c>
    /// and a character item's <c>string</c>. Held to the runtime overloads by the same drift test.</summary>
    internal static readonly string[] NonNumericPositionCarriers = ["long", "string"];
}
