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

/// <summary>
/// Resolves a <c>dataReference</c> parse node to a <see cref="Place"/> — the single entry point every verb uses to
/// turn a COBOL operand into a typed C# lvalue (COBOLNET_DESIGN §3.4). Two phases:
/// <list type="number">
///   <item><b>Syntactic flatten</b> — walk <c>cobolWord dataReferenceSuffix*</c> into the base name, its OF/IN
///         qualifiers, and the subscript / reference-modification token group (a flat SUBSCRIPT-mode stream the
///         binding layer interprets — the same grammar shape the legacy compiler proved over 364 NIST tests).</item>
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
internal readonly record struct RefModSuffixes(int RefMods, int Subscripts, int? Start, int? Length, bool NonLiteral);

public sealed class ReferenceResolver(DataBinder data)
{
    /// <summary>The D18 hook that MATERIALIZES a subscript / ref-mod segment the token renderer cannot render
    /// (fix-queue PB17): given the segment's verbatim source text and its line, it re-parses the text through
    /// <c>SubscriptExpressionFragment</c>, binds it through the ONE <c>ExpressionBinder.BindExpr</c>, synthesizes
    /// the §15.4 temporary via <c>DataBinder.CreateCompilerTemp</c>, registers the store as a statement-scoped
    /// pending PRE-op on <see cref="DataBinder.PendingPreOps"/>, and returns the temp — which this resolver then
    /// renders as an ordinary data-name read.
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
    internal Func<string, SegmentPosition, int, DataItem?>? MaterializeSegment { get; set; }
    /// <summary>The object-property reference BINDER (ISO §8.4.3.9; deep-dive D-P2): when normal
    /// qualification fails and the single qualifier is a class-name (factory form) or a TYPED object
    /// reference (instance form) whose roster carries an accessor for the head word under the PINNED
    /// §11.7.4 GR1a names, synthesize the compiler temp, register the pending op (drained into the
    /// GR1/GR2/GR3 BoundSequence by StatementBinder once the carrying statement's polarity is known), and
    /// return the temp so the statement binds over its Place. Returns null when the shape is NOT a property
    /// reference (the caller keeps its generic unknown-name diagnosis). SR checks here: SR1 (:7376) the
    /// REPOSITORY property-specifier, SR2 (:7378) no universal receiver — both COBOLNET0843; SR3/SR4
    /// (accessor existence) belong to the drain, where the sending/receiving polarity is known.</summary>
    private DataItem? OoTryBindPropertyReference(string name, List<string> qualifiers,
        Antlr4.Runtime.RuleContext? site)
    {
        if (qualifiers.Count != 1 || data.OoClasses is not { } table) return null;
        string recv = qualifiers[0];

        // The class-name qualifier is a SOURCE reference and takes the §8.4.6.4 scope — the source element's
        // REPOSITORY, not the compilation group (kb/Work PB365; `table.Find` stood here). The lookup is the
        // funnel's non-diagnosing half because a miss is a legal alternative: the qualifier may be an
        // identifier naming a typed object reference, the instance form below.
        OoClassSymbol? cls = Compiler.Oo.OoNameResolution.Lookup(table, site, recv,
            Compiler.Oo.OoNameResolution.Want.Class).Class;
        bool factory = cls is not null;                      // prop OF Class-name → the FACTORY accessors (SR3/SR4 "or in the factory object")
        DataItem? recvItem = null;
        if (cls is null)
        {
            recvItem = data.Symbols.TryResolve(recv, data.ActiveScope, out var recvItems) ? recvItems[0] : null;
            if (recvItem?.Pic is not { Category: PicCategory.ObjectReference } rp) return null;
            var rd = rp.ObjectRef ?? ObjectRefDescriptor.Universal;
            if (rd.IsUniversal)
            {
                // The shape IS a property reference on a universal receiver — SR2 rejects it by name.
                // (Silently null under a probe — the committing resolution reports; kb/Work PB157.)
                if (!_probing)
                    data.Edition.Error("COBOLNET0843",
                        $"the object-property reference '{name}' OF '{recv}': the receiving identifier shall "
                        + "not be a universal object reference (ISO §8.4.3.9.3 SR2)");
                return null;
            }
            // A FACTORY-OF receiver names the class's FACTORY accessors, the same half a class-NAME receiver
            // selects above (§8.4.3.9.3 SR3/SR4 "or in the factory object") — kb/Work PB389.
            factory = rd.Factory;
            cls = table.Find(rd.Name!);
            if (cls is null) return null;                    // interface-typed receivers: property prototypes are a later refinement (0899 at the interface)
        }

        // §12.3.8.2's property-specifier `PROPERTY property-name-1 [ AS literal-4 ]` (kb/Work PB974): the source writes
        // property-name-1, and literal-4 — when written — is the property as the declared classes know it (§12.3.8.3
        // SR16 a)), so the accessors are looked up under THAT name. Without a specifier the reference is still
        // reported below (SR1), under the name as written.
        string known = data.OoRepositoryProperties.TryGetValue(name, out var specified) ? specified : name;
        string getName = NamingConvention.GetAccessorName(known), setName = NamingConvention.SetAccessorName(known);
        var get = factory ? cls.FindFactoryMethod(getName) : cls.FindMethod(getName);
        var set = factory ? cls.FindFactoryMethod(setName) : cls.FindMethod(setName);
        if (get is null && set is null) return null;         // not a property of the roster → generic diagnosis

        var model = get?.Binding!.Returning ?? set!.Binding!.Formals[0].Item;
        // R30 PURITY (kb/Work PB157): a PROBE gets the property's MODEL item — the accessor's own
        // description, carrying the category the sniff asks about — with NO temp, NO pending op and NO
        // diagnostics. The committing resolution that follows does all three exactly once. (The orphan
        // op a probing registration left behind classified as StoreKind.None and made OoWrapPropertyOps
        // prepend a GET that §8.4.3.9.4 GR2 says a write-only occurrence must not invoke.)
        if (_probing) return model.IsGroup ? null : model;

        if (!data.OoRepositoryProperties.ContainsKey(name))
            data.Edition.Error("COBOLNET0843",
                $"the object-property reference '{name}' OF '{recv}' requires a PROPERTY specifier in the "
                + "REPOSITORY paragraph (ISO §8.4.3.9.3 SR1; §12.3.8)");

        if (model.IsGroup)
        {
            data.Edition.Error(DiagnosticCatalog.OoGroupValuedProperty,
                $"the object-property reference '{name}' OF '{recv}': a GROUP-valued property reference "
                + "(the §8.4.3.9.4 temps over a group description) is a later refinement of the OO wave");
            return null;
        }

        var temp = data.OoCreatePropertyTemp(model, name);
        data.OoPendingPropertyOps.Add(new DataBinder.OoPendingPropertyOp(
            temp,
            recvItem is null ? null : PlaceForItem(recvItem, []),
            cls.CsName, factory, get, set, name, recv));
        return temp;
    }

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

    /// <summary>True when <paramref name="dref"/> is an OBJECT-PROPERTY reference (ISO §8.4.3.9.2 —
    /// <c>property-name OF {class-name | identifier}</c>, textually a qualified data reference) rather than an
    /// ordinary data reference. The ONE such test, over the SAME <see cref="OoTryBindPropertyReference"/>
    /// resolution the committing path runs, so the two can never disagree about what a property reference is.
    /// <para>Its caller is §14.9.4.3 SR20's carve-out (kb/Work PB238): "BY CONTENT shall not be omitted when
    /// identifier-4 is an identifier that is permitted as a receiving operand, EXCEPT that BY CONTENT may be
    /// omitted when identifier-4 is an object property" — a keyword-less Format-2 CALL argument that is an
    /// object property takes §14.9.4.4 GR9 a)2's BY CONTENT (a property is not a data item defined in the
    /// file, working-storage, local-storage or linkage section, so it never meets Syntax rule 3).</para>
    /// <para>⛔ PURE — it runs the property resolution under <c>_probing</c>, so no temp is synthesized, no
    /// pending §8.4.3.9.4 op is registered and no diagnostic is reported (the R30 probe contract). The
    /// ordinary-qualification test comes FIRST for the same reason the committing path orders it that way: a
    /// genuine qualified data reference is not a property reference, whatever a class roster holds.</para></summary>
    public bool IsObjectPropertyReference(Core.DataReferenceContext dref)
    {
        if (dref.cobolWord() is not { } head) return false;
        List<string> qualifiers = [];
        foreach (var suffix in dref.dataReferenceSuffix())
            if (suffix.qualification() is { } q) qualifiers.Add(q.cobolWord().Name());
        if (qualifiers.Count != 1) return false;
        string name = head.Name();
        if (ResolveQualified(name, qualifiers) is not null) return false;
        bool saved = _probing;
        _probing = true;
        try { return OoTryBindPropertyReference(name, qualifiers, dref) is not null; }
        finally { _probing = saved; }
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

    /// <summary>The drefs this resolver has already DIAGNOSED (an undefined name — <see cref="ReportUnidentified"/> —
    /// or a rejected reference shape: SR3 ref-mod-of-ref-mod, SR1 identifier-1 exclusion): one report per source
    /// reference even when a statement binder resolves the same node more than once, and the fact the receiving
    /// chokepoint asks (<see cref="WasDiagnosed"/>) so that a null it gets back is EITHER already reported OR
    /// reported there — never a silently dropped receiver (kb/Work PB70).</summary>
    private readonly HashSet<Core.DataReferenceContext> _diagnosed = [];

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

    /// <summary>The non-place answer for a SEGMENT (subscript or reference-modifier) the renderer could not
    /// render: deferred when the resolver has no materializer (a data-division resolver) or the segment's own
    /// operand is deferred; otherwise REPORTED — the materializer reports every segment it refuses (kb/Work
    /// PB1030: a segment that is not an arithmetic expression is not a subscript, ISO §8.4.2.3.2).</summary>
    private RefResolution SegmentFailure(string written) =>
        MaterializeSegment is null ? RefResolution.Deferred(DeferredShape.SegmentWithoutStatement, written)
        : _segmentDeferred ? RefResolution.Deferred(DeferredShape.DeferredSegmentOperand, written)
        : RefResolution.Refused(written);

    /// <summary>The non-place answer when <see cref="PlaceForItem"/> built no place: its deferred shape, or —
    /// when it names none — a REJECTED REDEFINES view, whose data description entry was refused at its
    /// declaration (Tier D, <see cref="RedefinesTier.Rejected"/>). A reference to it is reported here, once per
    /// reference (COBOLNET2364), because the statement holding it must say why it cannot bind.</summary>
    private RefResolution ItemFailure(Core.DataReferenceContext dref, DataItem item, DeferredShape? deferred)
    {
        string written = DataBinder.WrittenText(dref);
        if (deferred is { } shape) return RefResolution.Deferred(shape, written);
        ReportRefusedDeclaration(dref, item.CobolName ?? item.CsName, item.Class?.RejectReason);
        return RefResolution.Refused(written);
    }

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
        // (kb/Work PB840). Qualification is by REPORT-NAME (§8.4.2.2.2 Format 1's file-report-qualifier), which
        // is what distinguishes two reports' same-named counters.
        if (SumCounterFor(dref, name, report) is { } sumReg) return Resolved(sumReg);

        // The reference AS WRITTEN, read by the ONE decomposition (kb/Work PB443 — see WrittenReference).
        var written = ReadWritten(dref);
        var qualifiers = written.Qualifiers;
        var subCtx = written.SubscriptGroup;    // the subscript group (no depth-0 colon)
        var refCtx = written.RefModGroup;       // a reference-modification group (start : length)
        var cleanRef = written.RefModPart;      // the refModPart form (parsed arithmeticExpression : ...)

        // ISO §8.4.3.3.3 SR3 — "Identifier-1 shall not be a reference-modification format identifier." The
        // grammar cannot express this (dataReferenceSuffix* and qualification's own (subscriptPart|refModPart)*
        // both admit unlimited ref-mods), so it is COUNTED (WrittenReference.RefModCount). Before that count,
        // `??=` kept the FIRST of each carrier and the DEFAULT-mode form then outranked the SUBSCRIPT-mode one,
        // so `MOVE A (3:4)(2:2)` COMPILED CLEAN and returned A(2:2) — a silent wrong value, not a composition
        // and not a rejection.
        if (written.RefModCount > 1)
        {
            if (!_probing && _diagnosed.Add(dref))   // R30 purity: a probe never diagnoses (kb/Work PB157)
                data.Edition.Error(DiagnosticCatalog.RefModOfRefMod,
                    $"'{name}' carries {written.RefModCount} reference modifications; a reference-modified item cannot itself "
                    + "be reference-modified (ISO §8.4.3.3.3 SR3). Compose the positions into one modifier instead.");
            return Refused();
        }

        DataItem? item = qualifiers.Count > 0 ? ResolveQualified(name, qualifiers) : ResolveUnqualified(name);
        // The object-property fallback (§8.4.3.9.2 — `prop OF {class-name | identifier}` is textually a
        // qualified data reference, so it legitimately FAILS normal qualification): the hook synthesizes the
        // GR1–GR3 temp and the rest of THIS method gives the temp the full normal tail (subscript rejection —
        // a temp has no OCCURS — and reference-modification, which SR5/SR6 permit on the property value).
        item ??= OoTryBindPropertyReference(name, qualifiers, dref);
        if (item is null)
        {
            // The NAME resolves to nothing — a typo or a mis-qualification, never a feature gap (kb/Work R30).
            // Every later null in this method is an unsupported-shape staging of a name that DID resolve.
            if (report) ReportUnidentified(dref, name, qualifiers);
            return Refused();
        }

        if (ReadSubscripts(dref, item, subCtx, out var indexExprs) is { } subscriptFailure) return subscriptFailure;

        // A level-66 RENAMES alias (ISO §13.18.45): one elementary-alphanumeric view COMPOSED over the spanned
        // leaves — reads concatenate their images, writes distribute slices back. This slice covers STRING-VALUED
        // leaves (X / edited / StoreAsImage); a typed-numeric leaf in the span fails loud (the image codecs for a
        // composed numeric leaf are a later slice). No subscripts: a RENAMES operand cannot have/live under OCCURS.
        if (item.Renames is { } ren)
        {
            // A level-66 entry is never a table element, so a written subscript was refused by §8.4.2.3.3 SR2
            // above (ScreenSubscriptArity) — this arm is unreachable, and the refusal ledger holds it to that.
            if (indexExprs.Count > 0) return Refused();
            // The no-THRU form is an ALIAS: §13.18.45.4 GR1 — "all of the data attributes of data-name-2 become
            // the data attributes of data-name-1 and the storage area occupied by data-name-2 becomes the storage
            // area occupied by data-name-1". Attributes AND storage forward to the renamed item's place (numeric
            // stays numeric: NC252A's ADD 3500 TO RENAME-12 over a PIC 9(4); a group forwards as the group), and
            // only the THRU form composes an alphanumeric span (GR2).
            // ⛔ BUT THE IDENTITY DOES NOT FORWARD (kb/Work PB602). GR1 shares the attributes and the storage, not
            // the NAME: data-name-1 is a data item of its OWN, and a bare forward made a reference to the 66
            // indistinguishable from a reference to the renamed item — so `START RLF KEY IS = RK-ALIAS` passed
            // §14.9.41.3 SR5's "shall be the data item specified in the RELATIVE KEY clause" that its THROUGH
            // sibling (a RenamesPlace, which keeps its own Item) was correctly refused by. `DenotesAs` records the
            // alias on the renamed item's own place, so nothing about the ACCESS changes and only the identity
            // question answers differently.
            // A RENAMES entry whose operands did not resolve was refused at its declaration (COBOLNET1655,
            // §13.18.45.3), and the data binder leaves it with no From and an empty Span.
            if (ren.Thru is null ? ren.From is null : ren.Span.Count == 0)
                return ItemFailure(dref, item, null);
            if (ren.Thru is null)
                return PlaceForItem(ren.From!, [], out var fwdDeferred) is { } fwdPlace
                    ? Resolved(fwdPlace with { DenotesAs = item }) : ItemFailure(dref, ren.From!, fwdDeferred);
            var leafPlaces = new List<Place>(ren.Span.Count);
            var widths = new List<int>(ren.Span.Count);
            foreach (var part in ren.Span)
            {
                var leaf = part.Leaf;
                if (part.Occurrence is { } occIdx)
                {
                    // ONE occurrence (or the one-and-only cell) of the leaf, possibly a partial slice of it (kb/Work
                    // PB96): the cell's place, then its ref-mod view when the part does not cover the whole cell.
                    if (PlaceForItem(leaf, leaf.Occurs is null ? [] : [occIdx.ToString()], out var cellDeferred) is not { } cellRaw)
                        return ItemFailure(dref, leaf, cellDeferred);
                    Place cell = cellRaw;
                    bool cellString = data.IsImageBackedEarly(leaf) || cell is RedefViewPlace
                        || leaf.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                            or PicCategory.National or PicCategory.Boolean;
                    if (!cellString)
                    {
                        if (leaf.Pic is not { IsCharacterFormNumeric: true })   // THE ONE character-form predicate (kb/Work PB646)
                            return Deferred(DeferredShape.RenamesNonCharacterLeaf);
                        cell = new NumericImagePlace(cell);
                    }
                    if (part.IsPartial) cell = new RefModPlace(cell, part.Start.ToString(), part.Length.ToString());
                    leafPlaces.Add(cell);
                    widths.Add(part.Length);
                    continue;
                }
                // An OCCURS leaf inside the span contributes EVERY occurrence in order (§13.18.45 — the alias
                // covers the whole fixed-size area; NC252A's RENAME-7 over TABLE-ITEM-2 OCCURS 5).
                int occ = leaf.Occurs ?? 1;
                for (int k = 1; k <= occ; k++)
                {
                    if (PlaceForItem(leaf, leaf.Occurs is null ? [] : [k.ToString()], out var leafDeferred) is not { } lpRaw)
                        return ItemFailure(dref, leaf, leafDeferred);
                    Place lp = lpRaw;
                    bool stringValued = data.IsImageBackedEarly(leaf) || lp is RedefViewPlace
                        || leaf.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                            or PicCategory.National or PicCategory.Boolean;
                    // A typed NUMERIC-DISPLAY leaf participates through its character image (the alias is an
                    // alphanumeric view of the span, §13.18.45 — NC252A's PIC 999 leaves under RENAMES-TEST-1).
                    if (!stringValued)
                    {
                        if (leaf.Pic is not { IsCharacterFormNumeric: true })   // THE ONE character-form predicate (kb/Work PB646)
                            return Deferred(DeferredShape.RenamesNonCharacterLeaf);
                        lp = new NumericImagePlace(lp);
                    }
                    widths.Add(leaf.ImageWidth);   // a whole part: every occurrence, at the leaf's width (kb/Work PB96)
                    leafPlaces.Add(lp);
                }
            }
            return Resolved(new RenamesPlace(leafPlaces, item, widths));
        }

        if (PlaceForItem(item, indexExprs, out var deferred) is not { } inner) return ItemFailure(dref, item, deferred);

        if (refCtx is null && cleanRef is null) return Resolved(inner);
        // ⛔ ISO §8.4.3.3.3 SR1 — WHAT identifier-1 MAY BE, in ONE place (kb/Work PB70). An excluded shape is a
        // bind-time rejection (COBOLNET1647), never the run-time NotImplemented a sending ref-mod used to reach nor
        // the silent drop a receiving one fell into.
        if (RefModExclusion(item) is { } why)
        {
            if (!_probing && _diagnosed.Add(dref))   // R30 purity: a probe never diagnoses (kb/Work PB157)
                data.Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted,
                    $"'{DataBinder.WrittenText(dref)}': reference modification of {why} is not permitted (ISO §8.4.3.3.3 SR1)");
            return Refused();
        }
        if ((cleanRef is not null ? ReadRefMod(cleanRef) : ReadRefMod(refCtx!)) is not { } spec)
            return SegmentFailure(DataBinder.WrittenText(dref));   // a bound the materializer refused or deferred (§8.4.3.3.3 SR4)
        return RefModView(item, inner, spec) is { } view ? Resolved(view) : Deferred(DeferredShape.NumericRefModSubstrate);
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
            if (!data.IsImageBackedEarly(item) && inner is not RedefViewPlace) inner = new NumericImagePlace(inner);
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
    /// does not admit it (the CALLER rejects that at bind with COBOLNET1647 — this is the backstop).</para></summary>
    public Place? ResolveItemRefMod(DataItem item, int start, int? length, bool allowZeroLength = false) =>
        RefModExclusion(item) is null && PlaceForItem(item, []) is { } inner
            ? RefModView(item, inner, new RefModSpec(start.ToString(), length?.ToString(), allowZeroLength))
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

    /// <summary>§8.5.1.12 — a variable-length group has a dynamic-length elementary item or a dynamic-capacity table
    /// as a subordinate (at any depth).</summary>
    internal static bool HasVariableLengthSubordinate(DataItem group)
    {
        foreach (var c in group.Children)
            if (c.IsDynamicLength || c.IsDynamicTable || (c.IsGroup && HasVariableLengthSubordinate(c))) return true;
        return false;
    }

    // ── The ONE reference-modification reader (ISO §8.4.3.3.2) ───────────────────────────────────────────────
    // A ref-mod reaches the binder through TWO source carriers, decided by the lexer at the '(' and frozen there:
    // the DEFAULT-mode PARSED form (`refModPart : (LPAREN | FNARG_LPAREN) refModSpec (RPAREN | FNARG_RPAREN)` —
    // BOTH paren flavours, because a ref-mod written directly after a ZERO-ARGUMENT function name is delimited
    // by the argument-list twins the lexer cannot rule out, §8.4.3.2.3 SR6's catalog precondition; PB48)
    // and the SUBSCRIPT-mode CAPTURED token group (a depth-0 SUB_COLON). Both reduce to the same
    // <see cref="RefModSpec"/> through the same segment renderer, so the rule "how a ref-mod's start and length
    // are read off the source" is written down ONCE. Both overloads are internal because the intrinsic binder
    // reads the SAME two carriers for a ref-modified FUNCTION RESULT (§8.4.3.3.3 SR2, fix-queue PB8) — a second
    // copy of this reader there is exactly the one-rule-two-places defect PB4 was.

    /// <summary>Read the PARSED DEFAULT-mode <c>refModPart</c> form. Null when a start/length expression uses a
    /// form the segment renderer does not handle, so the caller fails loud rather than emitting a wrong slice.</summary>
    internal RefModSpec? ReadRefMod(Core.RefModPartContext rmp)
    {
        var rmExprs = rmp.refModSpec().arithmeticExpression();
        if (rmExprs.Length == 0) return null;
        var startToks = new List<IToken>();
        CollectLeafTokens(rmExprs[0], startToks);
        if (RenderSegment(startToks, SegmentPosition.RefMod) is not { } rmStart) return null;
        string? rmLen = null;
        if (rmExprs.Length > 1)
        {
            var lenToks = new List<IToken>();
            CollectLeafTokens(rmExprs[1], lenToks);
            if (RenderSegment(lenToks, SegmentPosition.RefMod) is not { } l) return null;
            rmLen = l;
        }
        // §7.3.23 / §8.4.3.3.4 item 5c: the ref-mod allows a zero-length result iff REF-MOD-ZERO-LENGTH is ON at
        // this site's source line (the group's compile-time directive fold; OFF everywhere by default).
        return new RefModSpec(rmStart, rmLen, data.RefModZeroLength.IsOnAt(rmp.Start.Line));
    }

    /// <summary>Read the SUBSCRIPT-mode CAPTURED group form. The caller has already established the group IS a
    /// ref-mod (<see cref="HasDepth0Colon"/>); null on an unrenderable segment.</summary>
    internal RefModSpec? ReadRefMod(Core.SubscriptOrRefModContext group)
    {
        var (rm, _) = InterpretSubscripts(group);
        return rm is { Count: > 0 }
            ? new RefModSpec(rm[0], rm.Count > 1 ? rm[1] : null, data.RefModZeroLength.IsOnAt(group.Start.Line))
            : null;
    }

    /// <summary>The suffixes written on ONE data reference, read LEXICALLY — how many subscript and
    /// reference-modification carriers it carries, and, for a single ref-mod whose two segments are INTEGER
    /// LITERALS, their values.
    /// <para>⛔ THE CARRIER KNOWLEDGE LIVES HERE, beside the two <see cref="ReadRefMod"/> overloads, because a
    /// ref-mod reaches the binder through TWO source carriers frozen at lex time — the DEFAULT-mode parsed
    /// <c>refModPart</c> and the SUBSCRIPT-mode captured token group — and a clause that walked only one of them
    /// would drop the other SILENTLY (kb/Work PB4's one-rule-two-places defect, and PB205's dropped ref-mod).</para>
    /// <para>This is the DATA-DIVISION read: the clauses that admit a reference-modified operand all restrict it
    /// to integer literals (ISO §13.18.16.3 SR4, §13.18.54.3 SR8, §13.18.57.3 SR10), so no expression renderer and
    /// no procedure-phase state are involved. A segment that is anything else — a data-name, an arithmetic
    /// expression, a figurative constant — comes back <see cref="RefModSuffixes.NonLiteral"/>, for the CLAUSE's own
    /// syntax rule to reject with its own diagnostic.</para></summary>
    internal static RefModSuffixes ReadOperandSuffixes(Core.DataReferenceContext dref)
    {
        int refMods = 0, subscripts = 0;
        Core.SubscriptOrRefModContext? captured = null;
        Core.RefModPartContext? parsed = null;

        void TakeSubscriptPart(Core.SubscriptPartContext sp)
        {
            if (sp.subscriptOrRefMod() is not { } s) return;
            if (HasDepth0Colon(s)) { refMods++; captured ??= s; } else subscripts++;
        }

        foreach (var suffix in dref.dataReferenceSuffix())
        {
            // A qualification carries its OWN (subscriptPart | refModPart)* tail — `C IN G (1:3)` hangs the
            // ref-mod off the qualification, not off the base word.
            if (suffix.qualification() is { } q)
            {
                foreach (var sp in q.subscriptPart()) TakeSubscriptPart(sp);
                foreach (var rp in q.refModPart()) { refMods++; parsed ??= rp; }
            }
            else if (suffix.refModPart() is { } rmp) { refMods++; parsed ??= rmp; }
            else if (suffix.subscriptPart() is { } sp2) TakeSubscriptPart(sp2);
        }
        if (refMods != 1) return new RefModSuffixes(refMods, subscripts, null, null, false);

        var (start, length, ok) = parsed is not null ? ReadParsedLiterals(parsed) : ReadCapturedLiterals(captured!);
        return new RefModSuffixes(refMods, subscripts, ok ? start : null, ok ? length : null, !ok);
    }

    /// <summary>The DEFAULT-mode <c>refModPart</c> as integer literals: each arithmetic-expression segment shall be
    /// ONE integer literal (§13.18.16.3 SR4 and its twins), so its whole text is the value or the read fails.</summary>
    private static (int Start, int? Length, bool Ok) ReadParsedLiterals(Core.RefModPartContext rmp)
    {
        var exprs = rmp.refModSpec().arithmeticExpression();
        if (exprs.Length == 0 || !int.TryParse(exprs[0].GetText(), out int start)) return (0, null, false);
        if (exprs.Length == 1) return (start, null, true);
        return int.TryParse(exprs[1].GetText(), out int len) ? (start, len, true) : (0, null, false);
    }

    /// <summary>The SUBSCRIPT-mode captured group as integer literals: split at the depth-0 colon and require each
    /// side (whitespace apart) to be exactly ONE integer-literal token. An omitted length — §8.4.3.3.2's bracketed
    /// form — is the empty right side, and is the only empty side permitted.</summary>
    private static (int Start, int? Length, bool Ok) ReadCapturedLiterals(Core.SubscriptOrRefModContext group)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(group, tokens);
        int colon = -1;
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == Core.SUB_LPAREN) d++;
            else if (tt == Core.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == Core.SUB_COLON && d == 0) { colon = i; break; }
        }
        if (colon < 0) return (0, null, false);
        if (SoleIntegerLiteral(tokens, 0, colon) is not { } start) return (0, null, false);
        int after = colon + 1;
        bool empty = true;
        for (int i = after; i < tokens.Count; i++) if (tokens[i].Type != Core.SUB_WS) { empty = false; break; }
        if (empty) return (start, null, true);
        return SoleIntegerLiteral(tokens, after, tokens.Count) is { } len ? (start, len, true) : (0, null, false);
    }

    /// <summary>The one integer-literal token in <c>tokens[from, to)</c>, whitespace ignored; null when the range
    /// holds anything else (a data-name, an operator, more than one token) — i.e. not an integer literal.</summary>
    private static int? SoleIntegerLiteral(List<IToken> tokens, int from, int to)
    {
        IToken? only = null;
        for (int i = from; i < to; i++)
        {
            if (tokens[i].Type == Core.SUB_WS) continue;
            if (only is not null) return null;
            only = tokens[i];
        }
        return only is not null
            && only.Type is Core.SUB_INTEGERLIT or Core.SIGNED_INTEGERLIT or Core.INTEGERLIT
            && int.TryParse(only.Text, out int v) ? v : null;
    }

    /// <summary>
    /// Build the typed <see cref="Place"/> for an already-resolved <paramref name="item"/> with its subscript index
    /// expressions, honoring the REDEFINES machinery (COBOLNET_DESIGN §3.4 / §4.2):
    /// <list type="bullet">
    ///   <item>a Tier-B (string-canonical) view → a <see cref="RedefViewPlace"/> (offset,width) window over the
    ///         class's ONE string backing (the canonical too, so exactly one stored member);</item>
    ///   <item>a Tier-A (alias) view → the canonical's ONE stored field, reinterpreted through the view's own
    ///         Pic/scale/profile (the place carries the VIEW's <see cref="DataItem"/>);</item>
    ///   <item>any other item → a plain <see cref="MemberPlace"/>.</item>
    /// </list>
    /// Returns <see langword="null"/> for a form not handled in this slice — a subscripted Tier-B view, a whole-OCCURS
    /// reference, a not-yet-wired Tier-C / Rejected view, or a subscript-count mismatch — so the caller fails loud.
    /// This is the ONE item→<see cref="Place"/> builder: both the syntactic <see cref="Resolve"/> path and the by-item
    /// <see cref="ResolveItem"/> path go through it, so EVERY consumer (verb operands, level-88 / SET conditional
    /// variables, FD record areas) sees identical view resolution.
    /// </summary>
    private Place? PlaceForItem(DataItem item, IReadOnlyList<string> indexExprs) => PlaceForItem(item, indexExprs, out _);

    /// <param name="deferred">On a null return, the <see cref="DeferredShape"/> this builder has not built — or null
    /// when the item is a REJECTED (Tier D) REDEFINES view, refused at its own declaration (kb/Work PB1030).</param>
    private Place? PlaceForItem(DataItem item, IReadOnlyList<string> indexExprs, out DeferredShape? deferred)
    {
        deferred = null;
        // A WHOLE (unsubscripted) OCCURS DYNAMIC table reference has no element access — it would otherwise fold to a
        // MemberPlace wrapping the bare CobolDynTable<T> object, which is uncompilable in any value context. Fail
        // LOUD here (data-model D9); FUNCTION LENGTH and other whole-table operations route to a dedicated
        // DynWholeTablePlace in a later increment. A SUBSCRIPTED dynamic element (indexExprs non-empty) is inc 3's
        // access path and is NOT caught by this guard.
        if (item.IsDynamicTable && indexExprs.Count == 0) { deferred = DeferredShape.DynamicWholeTable; return null; }
        if (item.Class is { Tier: RedefinesTier.StringCanonical } sc)
        {
            // The backing is emitted in the canonical's containing struct (FieldEmitter.PhysicalFields), so a NESTED
            // class's backing must be reached through that struct's access path — a bare `_redef_X` resolves only for a
            // top-level (static-field) class. Fail loud if the parent path is unavailable (e.g. it is itself within an
            // OCCURS), rather than emit an unqualified reference that does not exist in scope.
            if (BuildBackingPath(sc) is not { } backing) { deferred = DeferredShape.NestedClassBacking; return null; }
            // A SUBSCRIPTED view: each OCCURS level on the item's path WITHIN the class displaces the window by
            // (occurrence − 1) × that level's per-occurrence width — the redefined table lays its occurrences
            // end-to-end in the ONE backing (ISO §13.18.44). ClassOffset is the occurrence-1 position; subscripts
            // map to the in-class OCCURS levels outer→inner, exactly as in AccessPath.
            var occursLevels = new List<DataItem>();
            for (DataItem? n = item; n is not null && ReferenceEquals(n.Class, sc); n = n.Parent)
                if (n.Occurs is not null) occursLevels.Add(n);
            occursLevels.Reverse();
            if (occursLevels.Count != indexExprs.Count) { deferred = DeferredShape.UnbuiltAccessPath; return null; }   // an item-path caller's count
            string offset = item.ClassOffset.ToString();
            // The BIT twin of the same displacement, for a USAGE BIT member (kb/Work PB203): a bit item's
            // occurrences lie at successive BIT positions (§8.5.1.6.3's "next bit position in storage"; the same
            // stride GroupImageCodec.EmitRunMemberFromBits distributes a run's occurrences at), so the per-level
            // stride is its bit extent — `PIC 1(4) USAGE BIT OCCURS 6` strides 4 bits, not the 1 byte its
            // ImageWidth ceiling reports.
            string bitTerms = "";
            for (int k = 0; k < occursLevels.Count; k++)
            {
                // The BYTE stride is the level's STORAGE extent, never its character-position count (kb/Work
                // PB231): the backing is byte-addressed and a NATIONAL element occupies two bytes per position
                // (§13.18.60.4 GR8 / D-N1), so `05 T PIC N(2) OCCURS 3` strides FOUR bytes. ByteWidth is
                // ImageWidth for every other leaf kind, so this is byte-identical elsewhere — the same
                // relationship the bit twin below has to it.
                offset += $" + ({indexExprs[k]} - 1) * {occursLevels[k].ByteWidth}";
                bitTerms += $" + ({indexExprs[k]} - 1) * {BitLayout.StrideBits(occursLevels[k])}";   // ALIGNED strides whole bytes (§13.18.1.4 GR2)
            }
            // A BASED class's window is displaced by the data-address pointer's runtime offset (ISO §13.18.5
            // — the view addresses wherever the pointer currently points; Phase-4b increment 2). The backing
            // property renders FIRST in both Read and Write, so the Deref null/bounds traps (GR3/GR4) fire
            // before the null-lenient OffsetOf.
            string? based = null;
            if (sc.BasedPointerField is { } addr)
            {
                based = $"CobolPtr.OffsetOf({addr})";
                offset = $"{based} + {offset}";
            }
            // (whole-group image analysis moved OUT of resolve to the post-bind UsageCollectionPass, PHASE-05 Step 5)
            // A class-tier GROUP holding an occurs-depending table is an ODO operand exactly like a struct group
            // (kb/Work PB80: a BASED record — string-canonical — sent its MAXIMUM image; §13.18.38.4 GR8 does not
            // care how the group is stored). ONE wrap rule for both storage shapes.
            return WrapIfOdoGroup(RedefViewPlace.For(backing, item, offset, based, bitTerms, BuildCellPath(sc)), item);
        }
        // A Tier-A view forwards to the canonical (a numeric view reinterprets the shared unscaled value via its own
        // scale, for free). A not-yet-wired (Tier-C) / Rejected view is loud.
        if (item.Class is { } cls && !item.IsCanonical && cls.Tier != RedefinesTier.Alias)
            return null;   // Tier D (Rejected) — refused at the declaration; `deferred` stays null
        DataItem accessItem = item.Class is { Tier: RedefinesTier.Alias } ac && !item.IsCanonical
            ? ac.Canonical : item;
        // A subscripted element whose access path crosses an OCCURS DYNAMIC level (data-model D9): the sending and
        // receiving accessors differ (RefSending vs RefReceiving — the latter grows-and-seeds), so build BOTH paths
        // and return a direction-carrying DynTablePlace. (A dynamic element is never an ODO subject, and a group
        // containing a dynamic table is not image-capable, so the ODO-wrap / whole-group paths below do not apply.)
        for (DataItem? n = accessItem; n is not null; n = n.Parent)
            if (n.IsDynamicTable)
            {
                if (BuildAccessPath(accessItem, indexExprs) is not { } dynPath) { deferred = DeferredShape.UnbuiltAccessPath; return null; }
                return new DynTablePlace(dynPath, item);
            }
        // An unsubscripted reference to an OCCURS table (whole-table op) is a later slice → AccessPath null → loud.
        if (BuildAccessPath(accessItem, indexExprs) is not { } path) { deferred = DeferredShape.UnbuiltAccessPath; return null; }
        // (Resolving a group no longer mutates WholeGroupReferenced — the "which groups are whole-image operands"
        // analysis is the post-bind UsageCollectionPass, which walks the BOUND tree and collects ONLY true
        // whole-group operands, not every RESOLVED group. PHASE-05 Step 5, §14.9.25.4 MOVE GR4.)
        return WrapIfOdoGroup(new MemberPlace(path, item), item);
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
                ?? (PlaceForItem(item, indexExprs, out var deferred) is { } place
                    ? RefResolution.Resolved(place, "") : ItemFailure(dref, item, deferred));
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
    private RefResolution? ReadSubscripts(Core.DataReferenceContext dref, DataItem item,
        Core.SubscriptOrRefModContext? subCtx, out List<string> indexExprs)
    {
        indexExprs = [];
        if (subCtx is null)
            return ScreenSubscriptArity(dref, item, 0)   // §8.4.2.3.3 SR5 — none written (kb/Work PB681)
                ? RefResolution.Refused(DataBinder.WrittenText(dref)) : null;
        if (ScreenEmptyParentheses(dref, subCtx))   // §8.4.2.3.2 / §8.4.3.3.2 (kb/Work PB969)
            return RefResolution.Refused(DataBinder.WrittenText(dref));
        List<IndexUse> ixNames = [];
        // A subscript group never carries a depth-0 colon (WrittenReference), so the ref-mod arm is not reachable.
        var (e, _) = InterpretSubscripts(subCtx, ixNames);
        if (e is null) return SegmentFailure(DataBinder.WrittenText(dref));   // a segment the materializer refused or deferred
        ScreenIndexNameAssociation(item, ixNames);   // §8.4.2.3.3 SR4 (kb/Work PB459)
        if (ScreenSubscriptArity(dref, item, e.Count))   // §8.4.2.3.3 SR2/SR3 (kb/Work PB877)
            return RefResolution.Refused(DataBinder.WrittenText(dref));
        indexExprs = e;
        return null;
    }

    /// <summary>⛔ A DATA REFERENCE FOLLOWED BY EMPTY PARENTHESES — <c>WS-X()</c> or <c>WS-X( )</c> — is neither
    /// form a parenthesis after a data-name can take (kb/Work PB969): §8.4.2.3.2 writes a subscript list as
    /// <c>( subscript … )</c>, at least one subscript, and §8.4.3.3.2 a reference modifier as
    /// <c>( leftmost-position : [ length ] )</c>, a required leftmost position. Returns <see langword="true"/> when
    /// the reference is rejected, so the caller returns null.
    /// <para>The GRAMMAR admits the empty group because the same capture carries a keyword-omitted
    /// function-identifier's argument list, where §8.4.3.2.2 brackets the arguments inside the parentheses and
    /// <c>F()</c> is the zero-argument spelling — only the resolved symbol tells a function from a data item, and
    /// a name that reached THIS resolver resolved to a data item. The whitespace-only spelling was the same group
    /// before the grammar admitted the empty one, and it compiled CLEAN on the sending side (<c>DISPLAY WS-X( )</c>
    /// aborted at RUN time on NotImplementedCobolFeatureException) while the receiving side drew COBOLNET0899's
    /// "not yet implemented" — so this closes a compile-clean-then-abort as well as keeping the new spelling out.
    /// It is asked of the written GROUP, before the subscript interpreter, because the interpreter gives up on a
    /// whitespace-only group silently.</para></summary>
    /// <summary>THE test for an empty written parenthesis group — nothing but separators inside it (kb/Work PB969).
    /// One definition for every reader that must refuse <c>X()</c> on a data reference.</summary>
    internal static bool IsEmptyGroup(Core.SubscriptOrRefModContext group) =>
        group.subToken().All(t => t.SUB_WS() is not null);

    /// <summary>The COBOLNET2309 message body (the quoted reference is the caller's).</summary>
    internal const string EmptyParenthesesMessage = "parentheses with nothing inside them follow a data reference. A "
        + "subscript list writes at least one subscript (ISO §8.4.2.3.2 — \"( subscript … )\") and a reference "
        + "modifier a leftmost position (ISO §8.4.3.3.2); empty parentheses belong only to a function-identifier "
        + "with no arguments (ISO §8.4.3.2.2). Remove the parentheses, or write the subscript.";

    private bool ScreenEmptyParentheses(Core.DataReferenceContext dref, Core.SubscriptOrRefModContext group)
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

    /// <summary>The FIRST subscript group of <paramref name="dref"/> — the <c>(…)</c> that carries the reference's
    /// subscript list rather than a reference modification (no depth-0 colon), taken from the base word's own
    /// suffix or from a qualification's suffix tail (<c>K OF E (IX)</c> hangs it off the qualification). Shared by
    /// <see cref="ResolveForItem"/>, which renders it into index expressions, and by
    /// <see cref="SubscriptSegments"/>, which keeps it as written.</summary>
    internal static Core.SubscriptOrRefModContext? SubscriptGroupOf(Core.DataReferenceContext dref) =>
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
    /// <param name="SubscriptGroup">The first SUBSCRIPT-mode group with no depth-0 colon: the reference's
    /// subscript list. Taken from the base word's own suffix or from a qualification's suffix tail
    /// (<c>K OF E (IX)</c> hangs it off the qualification).</param>
    /// <param name="RefModGroup">The first SUBSCRIPT-mode group that IS a reference modification (a depth-0 colon).</param>
    /// <param name="RefModPart">The first parsed <c>refModPart</c> form (<c>start : length</c> as expressions).</param>
    /// <param name="RefModCount">How many reference modifications the whole reference carries — §8.4.3.3.3 SR3
    /// admits at most one, and the count is the only way to see a second one.</param>
    internal readonly record struct WrittenReference(
        List<string>? Written,
        Core.SubscriptOrRefModContext? SubscriptGroup,
        Core.SubscriptOrRefModContext? RefModGroup,
        Core.RefModPartContext? RefModPart,
        int RefModCount)
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
        Core.SubscriptOrRefModContext? subCtx = null;
        Core.SubscriptOrRefModContext? refCtx = null;
        Core.RefModPartContext? cleanRef = null;
        int refModCount = 0;
        void Classify(Core.SubscriptOrRefModContext s)
        {
            if (HasDepth0Colon(s)) { refModCount++; refCtx ??= s; } else subCtx ??= s;
        }

        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } q)
            {
                (qualifiers ??= []).Add(q.cobolWord().Name());
                foreach (var sp in q.subscriptPart()) if (sp.subscriptOrRefMod() is { } qs) Classify(qs);
                refModCount += q.refModPart().Length;
                if (q.refModPart().Length > 0) cleanRef ??= q.refModPart()[0];
            }
            else if (suffix.refModPart() is { } rmp) { refModCount++; cleanRef ??= rmp; }
            else if (suffix.subscriptPart()?.subscriptOrRefMod() is { } s) Classify(s);
        }
        return new WrittenReference(qualifiers, subCtx, refCtx, cleanRef, refModCount);
    }

    /// <summary>The reference's subscript list AS WRITTEN — one token segment per subscript position, outermost
    /// first, split by the ONE <see cref="SplitSubscriptTokens"/> splitter with the same declaration-informed
    /// '(' rule <see cref="InterpretSubscripts"/> uses (kb/Work PB136), or <see langword="null"/> when the
    /// reference carries no subscript group at all.
    /// <para>⛔ The RENDERED form cannot answer the question this exists for. ISO §14.9.37.3 SR8 and SR9 are rules
    /// about the SOURCE TEXT of a SEARCH ALL subscript — "shall be subscripted by the first index-name associated
    /// with identifier-1 … The index-name subscript shall not be followed by a '+' or a '–'" — and the C# index
    /// expression <see cref="InterpretSubscripts"/> produces has already erased both facts: an index-name and an
    /// integer data item of the same value render identically, and <c>IX + 1</c> folds into the arithmetic.</para></summary>
    internal List<List<IToken>>? SubscriptSegments(Core.DataReferenceContext dref) =>
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
        foreach (var seg in segs)
            for (int i = 0; i < seg.Count; i++)
            {
                var t = seg[i];
                if (t.Type != Core.SUB_IDENTIFIER
                    || !string.Equals(t.Text, index.Name, StringComparison.OrdinalIgnoreCase)) continue;
                List<string> quals = [];
                for (int j = i + 1; ; )
                {
                    while (j < seg.Count && seg[j].Type == Core.SUB_WS) j++;
                    if (j >= seg.Count || seg[j].Type is not (Core.SUB_OF or Core.SUB_IN or Core.OF or Core.IN)) break;
                    int k = j + 1;
                    while (k < seg.Count && seg[k].Type == Core.SUB_WS) k++;
                    if (k >= seg.Count || seg[k].Type is not (Core.SUB_IDENTIFIER or Core.IDENTIFIER)) break;
                    quals.Add(seg[k].Text);
                    j = k + 1;
                }
                if (ResolveIndexName(t.Text, quals, t) is { Outcome: IndexRefOutcome.Resolved } ix
                    && ReferenceEquals(ix.Decl, index)) return true;
            }
        return false;
    }

    /// <summary>The <see cref="SubscriptSegments"/> split over an ALREADY-READ subscript group (the caller has
    /// the <see cref="WrittenReference"/> in hand and need not walk the suffix tail a second time).</summary>
    private List<List<IToken>>? SubscriptSegmentsOf(Core.SubscriptOrRefModContext? group)
    {
        if (group is null) return null;
        var tokens = new List<IToken>();
        CollectLeafTokens(group, tokens);
        return SplitSubscriptTokens(tokens, CannotBeSubscripted);
    }

    /// <summary>⛔ THE ONE DECLARATION-INFORMED <c>'('</c> PREDICATE (kb/Work PB136, corrected by PB877) — the
    /// splitter's question "does this name own the <c>'('</c> that follows it, or does that paren open a new
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
    /// argument list must not be split from it — and so does a table element, which owns its paren.</para></summary>
    internal bool CannotBeSubscripted(string name) => ResolveUnqualified(name) is { IsTableElement: false };

    // ── Intrinsic-argument entries (ISO §15.3; consumed by StatementBinder.Intrinsics.cs) ─────────────────
    // The function-argument mini-parser resolves identifiers from flat SUBSCRIPT-mode tokens, where no
    // dataReference parse context exists — these thin internal entries expose the SAME private resolution
    // (ResolveUnqualified/ResolveQualified → PlaceForItem → RenderSegment) so argument references see identical
    // view/qualification/subscript semantics as every verb operand (singular-pattern rule).

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
    /// (kb/Work PB443) — SEARCH's and SEARCH ALL's identifier-1. It is deliberately NOT <see cref="Resolve"/>: a
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
    /// (one per OCCURS level, outermost first). A level-66 RENAMES alias stays null (loud) — its composition
    /// lives only on the parse-context path.</summary>
    internal Place? ResolveByName(string name, IReadOnlyList<string> qualifiers, IReadOnlyList<string> indexExprs) =>
        FindItem(name, qualifiers) is { Renames: null } item ? PlaceForItem(item, indexExprs) : null;

    /// <summary>Render one subscript token segment to a C# index expression (the private
    /// <see cref="RenderSegment"/>), or null when the segment uses an unhandled form (caller fails loud).</summary>
    internal string? RenderIndexSegment(List<IToken> tokens) =>
        RenderSegment(tokens, SegmentPosition.Subscript);

    /// <summary>Resolve an <c>ADDRESS OF</c> operand (ISO §8.4.3.11) to its item plus the OCCURS displacement
    /// of its subscripts — <c>(idx − 1) × width [+ …]</c> character positions within the item's storage class,
    /// or a null displacement for an unsubscripted (possibly OF/IN-qualified) reference. The address of
    /// occurrence k is the class cell displaced by the SAME in-class occurrence arithmetic the Tier-B view
    /// window uses (<see cref="PlaceForItem"/> — a table lays its occurrences end-to-end in the ONE cell
    /// image), so the two share one formula; the displacement string is the D10 transitional index carrier
    /// (a rendered expression, like <see cref="FixedTableSegment.OneBasedIndex"/>). Null overall = an
    /// unresolvable name, a subscript-count mismatch, or a reference-modified operand (ref-mod addresses a
    /// character SPAN, not a data item — a named residue) — the caller reports loud, never a wrong address.</summary>
    internal (DataItem Item, string? OccursDisplacement)? ResolveForAddressOf(Core.DataReferenceContext dref)
    {
        DataReferenceCst r = dref;
        if (r.Register != SpecialRegister.None || r.BaseName is not { } name) return null;
        var written = ReadWritten(dref);                  // the ONE decomposition (kb/Work PB443)
        if (written.IsReferenceModified) return null;     // ref-mod → loud (a span, not an item)
        var qualifiers = written.Qualifiers;
        var subCtx = written.SubscriptGroup;
        if (FindItem(name, qualifiers) is not { } item) return null;
        // §8.4.2.3.3 SR2/SR3/SR5 through the ONE screen the other two entries use (kb/Work PB681): an ADDRESS OF
        // operand is an identifier like any other, and the omitted-list arm used to return the address of the
        // table's FIRST occurrence for `ADDRESS OF E` — no subscript, no diagnostic, a pointer nobody asked for.
        // The ONE written-subscript reading every identifier entry shares (kb/Work PB1030 extracted it).
        if (ReadSubscripts(dref, item, subCtx, out var exprs) is not null) return null;
        if (subCtx is null) return (item, null);
        // The in-class OCCURS levels outer→inner — the PlaceForItem Tier-B walk (same layout, same formula).
        var occursLevels = new List<DataItem>();
        for (DataItem? n = item; n is not null && ReferenceEquals(n.Class, item.Class); n = n.Parent)
            if (n.Occurs is not null) occursLevels.Add(n);
        occursLevels.Reverse();
        if (occursLevels.Count != exprs.Count) return null;   // wrong subscript count → loud
        // ByteWidth, for the same reason as the PlaceForItem twin above: the class backing is byte-addressed
        // and a NATIONAL element strides two bytes per position (kb/Work PB231; §13.18.60.4 GR8 / D-N1).
        string disp = string.Join(" + ", occursLevels.Select((lv, k) => $"({exprs[k]} - 1) * {lv.ByteWidth}"));
        return (item, disp);
    }

    /// <summary>The STRUCTURAL access path to a Tier-B/Tier-C class's single stored backing field (the
    /// <see cref="RedefViewPlace"/> twin of the old string <c>BackingPath</c>). The backing is emitted in the
    /// canonical's containing struct, so a NESTED class reaches it through that struct's path
    /// (<c>OUTER.GROUP._redef_X</c>); a top-level class's backing is the bare static field (<c>_redef_X</c>). Returns
    /// <see langword="null"/> when the containing path is unavailable (the canonical is within an OCCURS table).</summary>
    private static AccessPath? BuildBackingPath(RedefinesClass cls) =>
        cls.Canonical.Parent is not { } parent
            ? new AccessPath([new RootFieldSegment(cls.BackingCsName, OmittedFormalGuard.Of(cls.Canonical))])
            : BuildAccessPath(parent, []) is { } parentPath ? parentPath.Add(new MemberSegment(cls.BackingCsName)) : null;

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
        if (!data.Symbols.TryResolve(name, data.ActiveScope, out var list)) return null;
        if (list.Count > 1)
        {
            // §8.4.2.2.1 (kb/Work R33): "Qualification of a user-defined name is required unless … 1) No
            // other name has the identical spelling." TryResolve returns ONE namespace tier — the §8.4.6.2.1
            // rule-3a method overlay OR the unit map, never a mix — so a plural list is genuine same-tier
            // ambiguity, not legal scope shadowing. Measured before enforcing: ZERO of 762 corpus+NIST
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

    /// <summary>
    /// The C# member-access path for an item: a static field at the root, else <c>Parent.Child</c> chained, with
    /// each <paramref name="indexExprs"/> entry inserted as <c>[expr - 1]</c> at its OCCURS level (outermost first).
    /// Returns <see langword="null"/> if the subscript count does not match the table's OCCURS dimension.
    /// </summary>
    /// <summary>The C# field path to a WHOLE table's field (the bare <c>.CsName</c> chain, NO subscript wrap) — for
    /// the OCCURS DYNAMIC CAPACITY-register view (<c>{path}.Capacity</c>), and (later increments) FUNCTION LENGTH,
    /// the SEARCH bound, and INITIALIZE of a dynamic table. Returns <see langword="null"/> when an ancestor is
    /// ITSELF a table (fixed or dynamic): a subscript would be required, so a whole-table reference is ambiguous and
    /// the caller fails loud (ISO §13.18.38; data-model D9).</summary>
    internal string? TablePath(DataItem table)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = table; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();
        for (int i = 0; i < chain.Count - 1; i++)   // any table STRICTLY above → ambiguous whole-table reference
            if (chain[i].IsTable) return null;
        return string.Join(".", chain.Select((n, i) => i == 0 ? RootText(n) : n.CsName));
    }

    /// <summary>The D10 transitional STRING twin of <see cref="RootOf"/>: the root item's field text, through the
    /// guard's one rendering when the item is a formal parameter (kb/Work PB971).</summary>
    private static string RootText(DataItem root) =>
        OmittedFormalGuard.Of(root) is { } g ? g.Render(root.CsName) : root.CsName;

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
    /// (<c>DataBinder.Odo.cs</c>). The candidate SET has exactly one member by SR30's first sentence, so there is
    /// no survivor count to take.</para></summary>
    internal CapacityRef? CapacityRegisterFor(Core.DataReferenceContext dref)
    {
        DataReferenceCst r = dref;
        if (r.BaseName is not { } name
            || !data.CapacityRegisters.TryGetValue(name, out var table)
            || table.OccursSpec?.CapacityRegister is not { } reg) return null;
        var written = ReadWritten(dref);
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
    /// <para>The covered spelling is the whole one a general format admits here: the bare name, or the name with
    /// ONE report-name qualifier (§8.4.2.2.2 Format 1's file-report-qualifier — a sum counter is a level-01-free
    /// report-section item whose only available qualifier is its report). A sum counter is never an OCCURS item
    /// in this compiler's model and GR1 makes it a numeric conceptual item, so a subscript or reference
    /// modification on it is not a sum-counter reference at all — the name falls through to ordinary resolution,
    /// which names the rule it breaks.</para>
    /// <para>⛔ TWO ENTRIES MAY LEGALLY CARRY ONE NAME (GR1, kb/Work PB882): §8.4.2.2.1's uniqueness requirement
    /// is about a REFERENCE, so the collision is diagnosed HERE, where a reference exists, and never by dropping
    /// a declaration.</para></summary>
    private ReportSumCounterPlace? SumCounterFor(Core.DataReferenceContext dref, string name, bool report)
    {
        var suffixes = dref.dataReferenceSuffix();
        if (suffixes.Any(s => s.qualification() is null)) return null;   // subscripted / reference-modified
        if (suffixes.Length > 1) return null;                            // more than one qualifier — not a counter reference
        if (!data.SumCounters.TryGetValue(name, out var homonyms)) return null;
        string? qualifier = suffixes.Length == 1 ? suffixes[0].qualification().cobolWord().GetText() : null;
        // §8.4.6.2.1 rule 3 — a counter of a report this source element declares hides a same-named counter of a
        // container's GLOBAL report; only candidates of the NEAREST declaring element can be ambiguous.
        var matches = data.NearestInScope(qualifier is null
            ? homonyms
            : homonyms.Where(h => h.Report.Name.Equals(qualifier, StringComparison.OrdinalIgnoreCase)), h => h.Report);
        if (matches.Count == 0)
        {
            // A qualifier that names no report carrying this counter: the name IS a sum counter, so the ordinary
            // resolver would only report "not defined". Name the real rule instead (§8.4.2.2.2 / §8.4.2.2.3 SR1).
            if (report && _diagnosed.Add(dref))
                data.Edition.Error(DiagnosticCatalog.ReportSumCounterReference, $"'{DataBinder.WrittenText(dref)}': '{name}' is a sum counter "
                    + $"(ISO §13.18.54.4 GR5), but no report description entry named '{qualifier}' contains one — a "
                    + "sum counter may be qualified only by the report-name of the report that defines it "
                    + "(ISO §8.4.2.2.2 Format 1).");
            return null;
        }
        if (matches.Count > 1)
        {
            if (report && _diagnosed.Add(dref))
                data.Edition.Error(DiagnosticCatalog.ReportSumCounterReference, $"'{DataBinder.WrittenText(dref)}': the reference is ambiguous — "
                    + $"{matches.Count} entries establish a sum counter named '{name}' (ISO §13.18.54.4 GR1 gives "
                    + "each entry its own counter). A reference shall uniquely identify one resource (ISO "
                    + "§8.4.2.2.1); qualify it by report-name, or give the entries distinct data-names.");
            return null;
        }
        var (rep, sum) = matches[0];
        return new ReportSumCounterPlace(rep.CsIndex, sum.Id, sum.Register, data.ReportDepth(rep));
    }

    /// <summary>The STRUCTURAL access path for an item — the <see cref="MemberPlace"/>/<see cref="DynTablePlace"/>
    /// twin of the string <see cref="AccessPath"/>: each chain node is a field segment, each OCCURS level a fixed or
    /// dynamic table segment carrying its (D10 transitional) index string. Null on a subscript-count mismatch.</summary>
    private static AccessPath? BuildAccessPath(DataItem item, IReadOnlyList<string> indexExprs)
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
            if (seg.Occurs is not null) segs.Add(new FixedTableSegment(indexExprs[si++]));       // fixed OCCURS → CobolTable.At
            else if (seg.IsDynamicTable) segs.Add(new DynTableSegment(indexExprs[si++]));         // dynamic OCCURS → RefSending/RefReceiving
        }
        return new AccessPath(segs);
    }

    /// <summary>The STRUCTURAL whole-table path (no subscript wraps) — the <see cref="CapacityRegisterPlace"/> twin of
    /// the string <see cref="TablePath"/> (also the base of a whole-dynamic-table INITIALIZE element path). Null when
    /// an ancestor is itself a table (an ambiguous whole-table reference).</summary>
    internal static AccessPath? BuildTablePath(DataItem table) => BuildTablePath(table, []);

    /// <summary>The STRUCTURAL whole-table path to a table that may itself lie under OTHER tables — one index
    /// expression per enclosing table level, outermost first (the D10 transitional string carrier): the
    /// <see cref="CapacityRegisterPlace"/> of a NESTED dynamic-capacity table for a table(ALL) enumeration (ISO §15.3
    /// — each outer occurrence has its own capacity; kb/Work PB62). Null when fewer indices are supplied than there
    /// are enclosing tables — the zero-index form is exactly the whole-table ambiguity the one-argument overload
    /// reports.</summary>
    internal static AccessPath? BuildTablePath(DataItem table, IReadOnlyList<string> outerIndexExprs)
    {
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

    private static string? AccessPath(DataItem item, IReadOnlyList<string> indexExprs,
        AccessDir dir = AccessDir.Sending)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = item; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();   // root-first

        // §8.4.2.3.3 SR3's count, from THE one place it is written down (DataItem.SubscriptArity, kb/Work PB877):
        // ANY table level, fixed OR dynamic — a dynamic table IS an OCCURS dimension though its Occurs is null.
        if (item.SubscriptArity != indexExprs.Count) return null;   // wrong number of subscripts

        string path = "";
        int si = 0;
        foreach (var seg in chain)
        {
            // The root through THE one guarded-root rendering (kb/Work PB971): a subscript or reference-
            // modification position that names an omitted formal is a reference to it, like any other.
            path += path.Length == 0 ? RootText(seg) : "." + seg.CsName;
            // A FIXED OCCURS routes through the ref-returning CobolTable.At (ISO §8.4.2.3.4 GR2): an out-of-range
            // occurrence continues benignly with subscript checking off (COBOL-85 semantics — conditions and FAIL
            // paths legally evaluate one-past-the-end references), instead of a raw CLR IndexOutOfRangeException.
            if (seg.Occurs is not null) path = $"CobolTable.At({path}, {indexExprs[si++]})";
            // A DYNAMIC OCCURS (§8.5.1.9.2/.9.3, D9) has direction-specific accessors: RefSending on a read (benign
            // scratch on OOB), RefReceiving on a write (grows-and-seeds past the current capacity). The direction is
            // fixed at build time and carried by the DynTablePlace's two paths.
            else if (seg.IsDynamicTable)
                path = $"{path}.{(dir == AccessDir.Sending ? "RefSending" : "RefReceiving")}({indexExprs[si++]})";
        }
        return path;
    }

    // ── Subscript interpretation (the flat SUBSCRIPT-mode token stream) ──────────────────────────────────

    /// <summary>
    /// Interpret the flat subscript/ref-mod token sequence. Returns (index expressions, isRefMod): a depth-0
    /// <c>SUB_COLON</c> marks reference modification (handled in a later slice, so the C# list is null). Otherwise
    /// each comma- or multi-space-separated segment is rendered to a C# <c>long</c> index expression; a segment that
    /// cannot be rendered yields a null list (→ the caller fails loud).
    /// </summary>
    /// <summary>True if the flat token stream has a depth-0 <c>SUB_COLON</c> — i.e. it is a reference modification
    /// (<c>start:length</c>) rather than a subscript list. Internal because the keyword-omitted FUNCTION path
    /// asks the SAME question of a captured group (fix-queue PB8): with the FUNCTION keyword omitted,
    /// <c>CURRENT-DATE (1:8)</c> captures its ref-mod in SUBSCRIPT mode exactly as a data reference would, and
    /// only this test separates it from an argument list.</summary>
    internal static bool HasDepth0Colon(Core.SubscriptOrRefModContext ctx)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == Core.SUB_LPAREN) d++;
            else if (tt == Core.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == Core.SUB_COLON && d == 0) return true;
        }
        return false;
    }

    /// <param name="indexNames">§8.4.2.3.3 SR4's collector — the index-names used as subscripts, for
    /// <see cref="ScreenIndexNameAssociation"/> at the caller, which knows the table being referenced.</param>
    private (List<string>? Exprs, bool IsRefMod) InterpretSubscripts(
        Core.SubscriptOrRefModContext ctx, List<IndexUse>? indexNames = null)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);

        int colonIdx = -1;
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == Core.SUB_LPAREN) d++;
            else if (tt == Core.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == Core.SUB_COLON && d == 0) { colonIdx = i; break; }
        }
        if (colonIdx >= 0)   // reference modification: start [: length]
        {
            if (RenderSegment(tokens.GetRange(0, colonIdx), SegmentPosition.RefMod) is not { } start)
                return (null, true);
            var result = new List<string> { start };
            var lengthTokens = tokens.GetRange(colonIdx + 1, tokens.Count - colonIdx - 1);
            if (lengthTokens.Any(t => t.Type != Core.SUB_WS))
            {
                if (RenderSegment(lengthTokens, SegmentPosition.RefMod) is not { } len) return (null, true);
                result.Add(len);
            }
            return (result, true);
        }

        var exprs = new List<string>();
        // kb/Work PB136 — declaration-informed '(' splitting, through the ONE predicate (kb/Work PB877): an
        // inline `IsTable` lambda stood here and answered only §8.4.2.3.3 SR2's FIRST half, so a name SUBORDINATE
        // to an OCCURS — legally subscripted, and a legal arithmetic-expression-1 subscript under §8.4.2.3.2 +
        // §8.8.1.1 + §8.4.3.1.2 Format 2 — had its own '(' split off as an EXTRA subscript.
        foreach (var seg in SplitSubscriptTokens(tokens, CannotBeSubscripted))
        {
            if (RenderSegment(seg, SegmentPosition.Subscript, indexNames) is not { } e) return (null, false);
            exprs.Add(e);
        }
        return (exprs, false);
    }

    // Internal (not private): the intrinsic-argument binder (StatementBinder.Intrinsics.cs) flattens and splits
    // the SAME SUBSCRIPT-mode token streams for FUNCTION argument lists (ISO §15.3) — one splitter, not two.
    internal static void CollectLeafTokens(IParseTree node, List<IToken> tokens)
    {
        if (node is ITerminalNode term) { tokens.Add(term.Symbol); return; }
        for (int i = 0; i < node.ChildCount; i++) CollectLeafTokens(node.GetChild(i), tokens);
    }

    /// <summary>Split a flat token list into subscript segments on depth-0 comma / multi-space boundaries (a faithful
    /// reduction of the legacy <c>ExpressionBinder.SplitSubscriptTokens</c>: a single space inside a relative
    /// subscript such as <c>I + 1</c> does not split; a separator space before a new operand does).</summary>
    internal static List<List<IToken>> SplitSubscriptTokens(List<IToken> tokens, Func<string, bool>? parenSplitsAfterName = null)
    {
        var segments = new List<List<IToken>>();
        var current = new List<IToken>();
        int depth = 0;

        // kb/Work PB136: does a depth-0 '(' BEGIN A NEW SEGMENT? After ')' or a literal, always — neither can
        // take a subscript, so the paren can only open a parenthesized-expression subscript (the spec's own
        // NOTE 2 form, `DOG (XCOUNTER (- YCOUNTER))`, was rejected with a wrong-count error). After an
        // IDENTIFIER it is AMBIGUOUS with the identifier's own subscript (`DOG (BAKER (I) 3)`), so the split
        // is DECLARATION-INFORMED: a name that carries no OCCURS cannot be subscripted, so its '(' starts a
        // new segment; with no lookup (the intrinsic-argument caller) the old join stands.
        bool LParenStartsNew()
        {
            var lastNonWs = current.FindLast(x => x.Type != Core.SUB_WS);
            if (lastNonWs is null) return false;
            return lastNonWs.Type switch
            {
                Core.SUB_RPAREN or Core.SUB_INTEGERLIT or Core.SIGNED_INTEGERLIT or Core.SUB_DECIMALLIT
                    or Core.SIGNED_DECIMALLIT or Core.SUB_STRINGLIT or Core.SUB_HEXLIT => true,
                // The predicate is ReferenceResolver.CannotBeSubscripted — §8.4.2.3.3 SR2's admission test, and
                // the ONE copy of it (kb/Work PB877). It answers true ONLY for a name that RESOLVES to a data
                // item NO subscript may be written on (neither carrying an OCCURS clause nor subordinate to
                // one): an unresolved name may be a function reference (`FUNCTION INTEGER (X)` — the first cut
                // split a function from its own argument list and six goldens went red on "0 given"), and a
                // TABLE ELEMENT owns its paren as its own subscript list. Unknown → no split → the D18 loud
                // names the operand.
                Core.SUB_IDENTIFIER when parenSplitsAfterName is not null
                    && !lastNonWs.Text.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase)
                    => parenSplitsAfterName(lastNonWs.Text),
                _ => false,
            };
        }

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == Core.SUB_LPAREN)
            {
                if (depth == 0 && current.Count > 0 && LParenStartsNew())
                {
                    segments.Add(current);
                    current = [];
                }
                depth++; current.Add(t); continue;
            }
            if (t.Type == Core.SUB_RPAREN) { if (depth > 0) depth--; current.Add(t); continue; }

            if (depth == 0 && (t.Type == Core.SUB_COMMA || t.Type == Core.SUB_SEMICOLON))
            {
                if (current.Count > 0) { segments.Add(current); current = []; }
                continue;
            }
            if (depth == 0 && t.Type == Core.SUB_WS)
            {
                int next = i + 1;
                while (next < tokens.Count && tokens[next].Type == Core.SUB_WS) next++;
                if (next < tokens.Count && current.Count > 0)
                {
                    var lastNonWs = current.FindLast(x => x.Type != Core.SUB_WS);
                    // A trailing operator OR a trailing OF/IN continues the SAME segment (`I + 1` relative
                    // subscripts; `SUB1 OF GRP` qualified subscripts, ISO §8.4.2.3.2) — and a pending OF/IN also
                    // continues into its qualifier identifier. The FUNCTION keyword (a plain SUB_IDENTIFIER in
                    // SUBSCRIPT mode) also continues: the following name belongs to a nested intrinsic call in a
                    // FUNCTION argument list — `SQRT(FUNCTION SQRT(F))` is ONE argument (ISO §15.3; the legacy
                    // splitter's endsWithFunction rule, dropped in the original subscript-only reduction).
                    bool continues = lastNonWs is not null &&
                        (lastNonWs.Type is Core.SUB_PLUS or Core.SUB_MINUS or Core.SUB_STAR or Core.SUB_SLASH
                            or Core.SUB_POWER or Core.SUB_OF or Core.SUB_IN
                         || (lastNonWs.Type == Core.SUB_IDENTIFIER
                             && lastNonWs.Text.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase)));
                    int nextType = tokens[next].Type;
                    // The new-segment starters: every token that can BEGIN an operand — identifiers, all four
                    // numeric-literal shapes, string literals (intrinsic arguments may be space-separated,
                    // ISO §15's general formats), and the ALL subscript word (§15.3 table(ALL) arguments).
                    if (!continues && (nextType is Core.SIGNED_INTEGERLIT or Core.SIGNED_DECIMALLIT
                            or Core.SUB_IDENTIFIER or Core.SUB_INTEGERLIT or Core.SUB_DECIMALLIT
                            or Core.SUB_STRINGLIT or Core.SUB_HEXLIT or Core.SUB_ALL
                        // kb/Work PB136 — the spaced NOTE 2 form: a '(' after this WS opens a NEW subscript
                        // under the same declaration-informed rule as the unspaced arm above.
                        || (nextType == Core.SUB_LPAREN && LParenStartsNew())))
                    {
                        segments.Add(current);
                        current = [];
                        i = next - 1;   // skip consumed WS
                        continue;
                    }
                    // A following OF/IN never splits — `name OF qualifier` stays one segment.
                }
                current.Add(t);
                continue;
            }
            current.Add(t);
        }
        if (current.Count > 0) segments.Add(current);
        return segments;
    }

    /// <summary>Render one subscript / ref-mod segment to a C# <c>long</c> position expression, or
    /// <see langword="null"/> if it uses a form neither the token renderer nor the D18 materialization route
    /// handles (so the caller fails loud). The renderer proper is a fast path for the shapes that map to C# text
    /// one token at a time — integer literals, data-name / index-name references, <c>+ - * /</c> and parentheses,
    /// i.e. the relative-subscript and simple-index forms. <b>EVERYTHING ELSE routes through
    /// <see cref="MaterializeViaFragment"/></b>, which re-parses and binds it properly; the renderer is an
    /// optimization over that route, never the arbiter of what is legal in the position (fix-queue PB42).
    /// <para>⚠ THAT INVARIANT WAS A CLAIM, NOT A FACT, until kb/Work PB170. A name the renderer CAN render never
    /// reached the binder, so nothing ever applied §8.8.1.1 to it and <c>T(XE)</c> with <c>XE PIC X(4)</c>
    /// compiled clean under STRICT — the renderer WAS deciding the position's legality, by omission. It holds now
    /// because <see cref="ScreenPositionOperandClass"/> asks the same classifier the binder would have asked, so
    /// the fast path and the D18 route reach the same verdict rather than two different ones.</para>
    /// <para>⛔ THE FAST PATH'S SCREENS ARE DEFERRED TO ITS COMMIT POINT (kb/Work PB220). They used to fire
    /// INSIDE the token loop, and the loop's five exits to D18 are ORDER-DEPENDENT — a later token with no case
    /// arm (<c>**</c>), an unresolvable name, or a scaled operand in a compound abandons the whole fast path
    /// AFTER an earlier name was already screened, and the D18 route then screens the same operand again through
    /// the expression binder. Measured: <c>MOVE E(XE ** 2) TO R</c> with <c>XE PIC X(4)</c> emitted COBOLNET0844
    /// TWICE (and Error+Warning under <c>--permissive</c>, which is worse than either lane alone). A
    /// <c>_diagnosed</c>-style dedupe cannot fix it — the second diagnostic comes from a different class over a
    /// different bound operand — so the screens are QUEUED here and flushed only when this method actually
    /// returns a rendered segment. Every D18 reroute discards the queue, which makes the deduplication a
    /// property of the control flow rather than of a set, and makes the NEXT late exit automatic.</para></summary>
    /// <param name="indexNames">§8.4.2.3.3 SR4's collector — see
    /// <see cref="ResolveSubscriptName(string,List{string},SegmentPosition,ref List{PendingScreen},out bool,ValueTuple{IToken,List{IndexUse}})"/>.</param>
    private string? RenderSegment(List<IToken> tokens, SegmentPosition position, List<IndexUse>? indexNames = null)
    {
        var sb = new System.Text.StringBuilder();
        List<PendingScreen>? pending = null;
        // ⛔ A COMPOUND segment (one carrying an arithmetic operator) whose operands include a SCALED item cannot
        // be rendered operand-by-operand, because §8.4.2.3.4 GR1b tests the integrality of THE RESULT of the whole
        // expression, not of each operand: `W-E(W-P + W-Q)` with W-P = W-Q = 1.5 has the integral result 3.0 and
        // is a legal subscript, while de-scaling each operand first yields 1 + 1 = 2 AND raises the condition
        // twice on source that never violated it. Such a segment routes to the D18 materializer, which evaluates
        // the expression at full precision into the §15.4 temp and applies the integrality rule exactly once — to
        // the result, where the standard applies it. A SINGLE scaled operand needs no such detour: it IS the
        // result, so the direct read below is equivalent and cheaper.
        bool compound = tokens.Any(t => t.Type is Core.SUB_PLUS or Core.SUB_MINUS or Core.SUB_STAR
            or Core.SUB_SLASH or Core.SUB_POWER or Core.PLUS or Core.MINUS or Core.STAR or Core.SLASH);
        // kb/Work PB136: a QUOTIENT-bearing segment routes to D18 UNCONDITIONALLY — the token splice would be
        // C# integer division over long reads, truncating where §8.4.2.3.4 GR1b evaluates the exact result of
        // the whole expression and requires EC-BOUND-SUBSCRIPT on a non-integer (`E((W-A + W-B) / 2)` with the
        // sum 7 silently selected occurrence 3). The scaled-operand routing above (PB41) caught only segments
        // whose OPERANDS are scaled; an all-integer quotient is exactly the case it could not see.
        if (tokens.Any(t => t.Type is Core.SUB_SLASH or Core.SLASH))
            return MaterializeViaFragment(tokens, position);
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            switch (t.Type)
            {
                case Core.SUB_WS: sb.Append(' '); break;
                case Core.SUB_INTEGERLIT or Core.SIGNED_INTEGERLIT or Core.INTEGERLIT: sb.Append(t.Text); break;
                case Core.SUB_PLUS or Core.PLUS: sb.Append(" + "); break;
                case Core.SUB_MINUS or Core.MINUS: sb.Append(" - "); break;
                case Core.SUB_STAR or Core.STAR: sb.Append(" * "); break;
                case Core.SUB_SLASH or Core.SLASH: sb.Append(" / "); break;
                // GROUPING-PAREN-ONLY (fix-queue PB48): the argument-list twins FNARG_LPAREN/FNARG_RPAREN are
                // deliberately absent. A segment carrying them is function-bearing, and the `default:` arm below
                // routes ANY unrenderable token to D18 — which is where such a segment belongs anyway — so
                // adding them here would render a function call's parens into a token-by-token string that the
                // rest of this switch cannot complete. PB42's rule ("can the renderer render it") is what makes
                // the omission safe rather than lucky.
                case Core.SUB_LPAREN or Core.LPAREN: sb.Append('('); break;
                case Core.SUB_RPAREN or Core.RPAREN: sb.Append(')'); break;
                case Core.SUB_IDENTIFIER or Core.IDENTIFIER:
                {
                    // Gather `name (OF|IN qualifier)*` — a QUALIFIED data-name subscript (ISO §8.4.2.3.2).
                    string name = t.Text;
                    var qualifiers = new List<string>();
                    int j = i;
                    while (true)
                    {
                        int k = j + 1;
                        while (k < tokens.Count && tokens[k].Type == Core.SUB_WS) k++;
                        if (k >= tokens.Count || tokens[k].Type is not (Core.SUB_OF or Core.SUB_IN or Core.OF or Core.IN)) break;
                        int m = k + 1;
                        while (m < tokens.Count && tokens[m].Type == Core.SUB_WS) m++;
                        if (m >= tokens.Count || tokens[m].Type is not (Core.SUB_IDENTIFIER or Core.IDENTIFIER)) break;
                        qualifiers.Add(tokens[m].Text);
                        j = m;
                    }
                    i = j;
                    // A FUNCTION-BEARING segment cannot be rendered token-by-token (the head word is a function
                    // name, not a data-name), so the WHOLE segment routes to D18 rather than this arm failing.
                    if (IsFunctionBearing(tokens)) return MaterializeViaFragment(tokens, position);
                    // ⛔ AN UNRESOLVABLE NAME ROUTES TO D18 — IT IS NOT A VERDICT (fix-queue PB50). This arm used
                    // to `return null`, which is the caller's LOUD posture, and that made it the one place in
                    // this renderer that decided a segment was unrenderable without asking D18 — contradicting
                    // the rule stated ten lines below it ("EVERY token the renderer cannot render ROUTES TO
                    // D18"). SUBSCRIPT mode has no ZERO token, so the figurative arrives as a plain
                    // SUB_IDENTIFIER, resolves to no data item, and `E(ZERO + 1)` ABORTED AT RUN TIME —
                    // §8.8.1.1 admits "the figurative constant ZERO" as an arithmetic operand and §8.4.2.3.2
                    // makes a subscript an arithmetic expression, so that is legal source.
                    // ⚠ A GENUINELY undefined name keeps the SAME posture, verified rather than assumed:
                    // `E(NOSUCHNAME + 1)` aborted at run time before this change and aborts at run time after
                    // it. Only the message improved — it now names `NOSUCHNAME` instead of the whole reference
                    // text, because the fragment binder reaches the individual operand. (That an undefined
                    // subscript name is a RUN-TIME abort at all is a separate, pre-existing wrong-stage
                    // defect; it is recorded in PB50's note, not fixed here.)
                    if (ResolveSubscriptName(name, qualifiers, position, ref pending, out bool scaled, (t, indexNames)) is not { } readExpr)
                        return MaterializeViaFragment(tokens, position);
                    // A scaled operand inside a compound segment — evaluate the whole expression instead (above).
                    if (scaled && compound) return MaterializeViaFragment(tokens, position);
                    sb.Append(readExpr);
                    break;
                }
                // ⛔ EVERY token the renderer cannot render ROUTES TO D18 — the gate asks "can this be rendered",
                // never "is this one of a listed set" (fix-queue PB42). The listed-set version shipped for one
                // commit and dropped two shapes of plain legal arithmetic on the floor: `W-E(W-I ** 2)` and
                // `W-E(2.0)` each compiled clean and threw at RUN TIME, because `**` had no case arm and a
                // decimal literal had none either — while §8.8.1.1 admits "a numeric literal … separated by
                // arithmetic operators" and §8.3.2.4.2 lists `**` as one, so both are arithmetic-expression-1
                // under §8.4.2.3.2 and legal in the position.
                // ⚠ THIS IS NOT THE UNAUDITED-TABLE MISTAKE PB1 TAUGHT, and the reason is structural: the
                // materializer re-parses the segment through `subscriptExpressionFragment : arithmeticExpression
                // EOF`, so THE ARITHMETIC-EXPRESSION GRAMMAR IS THE ADJUDICATOR. A shape §8.8.1.1 does not admit
                // — an alphanumeric literal, an ALL figurative constant (legal only in the §8.4.2.3.3 r6
                // positions) — cannot parse as an arithmetic expression, so the fragment returns null and the
                // caller keeps the exact loud posture it had. Nothing is admitted by assertion; it is admitted by
                // parsing, which is why the NEXT arithmetic token needs no edit here at all.
                default: return MaterializeViaFragment(tokens, position);
            }
        }
        string expr = sb.ToString().Trim();
        if (expr.Length == 0) return null;   // the caller's loud posture — nothing was rendered, so nothing is screened
        if (pending is not null && !_probing)   // R30 purity: a probe never diagnoses (kb/Work PB157)
            foreach (var ps in pending)
                if (ps.Item is { } it) ScreenPositionOperandClass(it, ps.Name, position);
                else IndexNameInPositionError(ps.Name, position);
        return expr;
    }

    /// <summary>A screen the fast path OWES once it commits to rendering the segment — either the §8.8.1.1 class
    /// screen over a resolved item (<see cref="ScreenPositionOperandClass"/>) or the §13.18.38.3 r7 index-name
    /// screen (<see cref="IndexNameInPositionError"/>, <c>Item</c> null). Queued rather than emitted so an exit
    /// to D18 later in the token loop cannot leave a diagnostic behind for the D18 route to duplicate — see
    /// <see cref="RenderSegment"/>.</summary>
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
    private void ScreenIndexNameAssociation(DataItem item, List<IndexUse> indexNames)
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
    /// (ISO §8.4.2.2.2 Format 3) — for EVERY position that admits one: a subscript (the token renderer) and the
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
        if (cands.Single is { } one) return new IndexRef(IndexRefOutcome.Resolved, one);
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

    /// <summary>True when this segment contains a FUNCTION-IDENTIFIER (ISO §8.4.3.1.2 Format 1) and therefore
    /// belongs to the D18 materialization route rather than the token renderer: either the explicit
    /// <c>FUNCTION</c> keyword (a plain <c>SUB_IDENTIFIER</c> in SUBSCRIPT mode), or the §8.4.3.2.3 SR2
    /// keyword-omitted form — a REPOSITORY-declared intrinsic or a user-function name, immediately followed by a
    /// left parenthesis, that is NOT shadowed by a declared data item (a declared item always wins, exactly as in
    /// <c>IntrinsicBinder.KeywordOmittedFunction</c>; the two must not drift apart, which is why both ask the
    /// question the same way).</summary>
    private bool IsFunctionBearing(List<IToken> tokens)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type is not (Core.SUB_IDENTIFIER or Core.IDENTIFIER)) continue;
            string w = tokens[i].Text;
            if (w.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase)) return true;
            int k = i + 1;
            while (k < tokens.Count && tokens[k].Type == Core.SUB_WS) k++;
            // GROUPING-PAREN-ONLY (fix-queue PB48): this arm detects the KEYWORD-OMITTED form `name(args)`,
            // which by definition has no FUNCTION token before the name — so its '(' is never retyped
            // FNARG_LPAREN (the lexer's mark keys on exactly that token). The explicit-keyword form is caught by
            // the `w == "FUNCTION"` test above and never reaches here.
            if (k >= tokens.Count || tokens[k].Type is not (Core.SUB_LPAREN or Core.LPAREN)) continue;
            if (data.Symbols.TryResolve(w, data.ActiveScope, out _)) continue;   // a declared item wins
            if (data.UserFunctionNames.Contains(w)
                || ((data.RepositoryAllIntrinsic || data.RepositoryIntrinsics.Contains(w))
                    && IntrinsicCatalog.TryGet(w, out _)))
                return true;
        }
        return false;
    }

    /// <summary>The D18 route (fix-queue PB17, widened by PB42): materialize ANY segment the token renderer
    /// cannot render into the §15.4 temporary, and render the segment as that temp's ordinary position read. The
    /// segment's VERBATIM source text is recovered from the char stream (the <c>IntrinsicBinder.ReparseArgs</c>
    /// idiom — spacing is significant, e.g. <c>A - 4</c> vs <c>A -4</c>) and handed to the
    /// <see cref="MaterializeSegment"/> hook.
    /// <para>⛔ IT IS DELIBERATELY NOT GATED ON A TOKEN LIST. The hook re-parses the text through
    /// <c>subscriptExpressionFragment : arithmeticExpression EOF</c>, so the ARITHMETIC-EXPRESSION GRAMMAR decides
    /// admissibility: a shape §8.8.1.1 does not admit cannot parse, the hook returns null, and the caller keeps
    /// its loud posture unchanged. That is what lets a function-identifier, <c>**</c>, a decimal literal and every
    /// future arithmetic form share ONE route with no per-token edit — and why widening the gate cannot repeat
    /// PB1's unaudited-table mistake.</para>
    /// <para>Null when the hook is absent (a data-division resolver builds a throwaway resolver with no procedure
    /// binder) or the fragment fails to parse or bind — in every such case the caller's loud posture is exactly
    /// what it was before D18.</para></summary>
    private string? MaterializeViaFragment(List<IToken> tokens, SegmentPosition position)
    {
        // R30 PURITY (kb/Work PB157): a probe must not BIND the segment — the hook registers a §15.4 pre-op
        // and a function-bearing segment would activate TWICE (once per probe+commit). The probe's Place is
        // discarded after its Item is read, so a dummy occurrence expression is never emitted.
        if (_probing) return "1";
        if (MaterializeSegment is null || tokens.Count == 0) return null;
        var first = tokens[0];
        if (first.InputStream is not { } stream) return null;
        string text = stream.GetText(
            new Antlr4.Runtime.Misc.Interval(first.StartIndex, tokens[^1].StopIndex));
        return MaterializeSegment(text, position, first.Line) is { } temp ? PositionRead(temp, position) : null;
    }

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
    private string? ResolveSubscriptName(string name, List<string> qualifiers, SegmentPosition position,
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
                return ix.Cell;
            }
            (pending ??= []).Add(new PendingScreen(null, name));
            return ix.Cell;   // keep rendering: a null here would re-route to D18 and screen the operand twice
        }
        // An INTEGER constant-name in a subscript position substitutes its integer literal (ISO §13.10.3 SR2 /
        // §13.10.4 GR1/GR3 — a subscript is a literal position, §8.4.2.3.2) — its canonical integer text IS the C#
        // read (IntegerText, not the literal as written: `+5` is a COBOL integer literal, not a C# operand spelling).
        if (qualifiers.Count == 0
            && data.FindConstant(name) is { Category: PicCategory.Numeric, IntegerText: { } integer })
            return integer;
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
    /// position — warned and compiled, keyed on nothing but whether the token renderer could render the
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
    /// <para>⚠ AND THE RENDERER'S OWN INVARIANT IS RESTORED, not abandoned: <see cref="RenderSegment"/> documents
    /// itself as "an optimization over that route, never the arbiter of what is legal in the position", and this
    /// defect falsified it. Asking the ONE classifier the question the binder would have asked is what makes the
    /// sentence true again — the fast path now reaches the same verdict, not a different one.</para>
    /// <para>⚠ PRECONDITION: the class question is asked in <see cref="ResolveSubscriptName"/>
    /// (<c>IntrinsicArgumentRules.IsArithmeticOperandClass</c>) and only a REJECTED item is queued, so this
    /// method composes the message and nothing else. The <c>_probing</c> purity guard lives at the queue's flush
    /// in <see cref="RenderSegment"/> — ONE place, since that is also where the ordering fix lives
    /// (kb/Work PB220).</para></summary>
    private void ScreenPositionOperandClass(DataItem item, string name, SegmentPosition position)
    {
        // ⛔ NO GROUP / POINTER / OBJECT-REFERENCE ARM, AND THAT IS A REACHABILITY FACT, NOT AN OMISSION
        // (kb/Work PB201). This method runs only when the fast path COMMITS to rendering the segment, and
        // <see cref="HasPositionOverload"/> lets it commit only for a carrier <c>CobolTable.Occ</c> declares a
        // parameter for — <c>long</c>, <c>string</c>, <c>Int128</c>, <c>ulong</c>, <c>UInt128</c>. A group's
        // carrier is its per-program <c>record struct</c> and a pointer's is <c>ManagedPointer</c>, so
        // both now route to D18 and are screened by <c>ExpressionBinder.OperandRef</c> instead — the same
        // COBOLNET0844 over the same §8.8.1.1, minus this method's position phrase.
        // ⚠ THE INTERSECTION IS NARROWER THAN THAT LIST, and it is the second precondition that narrows it: only
        // an item <c>IntrinsicArgumentRules.IsArithmeticOperandClass</c> REJECTED is ever queued, and the three
        // wide/unsigned carriers belong to class NUMERIC items, which it accepts. So what reaches HERE is
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
    /// <para>A scale-0 item (the overwhelming majority) keeps the EXACT previous text — the bare
    /// <c>CobolTable.Occ(path)</c> — so the generated C# for ordinary subscripts is byte-identical and no
    /// de-scaling division is emitted where none is needed. A scaled item passes its scale to the overload that
    /// de-scales and raises the position's own Table 13 condition on a fractional value.</para>
    /// <para>⚠ The runtime call is spelled out rather than routed through <c>RuntimeApi</c>: this text is produced
    /// at BIND time (the D10 transitional string carrier) and the binder cannot reference the CodeGen assembly.
    /// When PHASE 15 CUT 2.5 removes the SUBSCRIPT lexer mode and the carrier becomes <c>BoundExpr</c>, this
    /// rendering moves to the renderer with the rest of it.</para></summary>
    private string? PositionRead(DataItem item, SegmentPosition position)
    {
        // Scale 0 — an integer item — has no integrality question to answer, so neither position can raise and the
        // position kind is irrelevant: keep the ONE historical text unchanged for both.
        int scale = item.Pic?.Scale ?? 0;
        // ⛔ THE BET ON OVERLOAD RESOLUTION IS ONLY GOOD FOR THE CARRIERS THAT HAVE AN OVERLOAD (kb/Work PB201).
        if (!HasPositionOverload(item, scale)) return null;
        if (AccessPath(item, []) is not { } path) return null;
        if (scale <= 0) return $"CobolTable.Occ({path})";
        return position == SegmentPosition.Subscript
            ? $"CobolTable.Occ({path}, {scale})"
            : $"CobolString.RefModPosition({path}, {scale})";
    }

    /// <summary>⛔ THE FAST PATH'S ADMISSION TEST (kb/Work PB201): can the C# text <see cref="PositionRead"/> is
    /// about to emit actually BIND against the operand's generated field? The bind-time renderer names the field
    /// and lets C# overload resolution supply the conversion — the deliberate design that lets ONE text serve a
    /// carrier the post-bind whole-group analysis has not chosen yet — but that bet is good ONLY for the carrier
    /// types the emitted helper declares a parameter for.
    /// <para><b>The overload sets are the whole rule.</b> <c>CobolTable.Occ</c> takes
    /// <c>long | string | Int128 | ulong | UInt128</c> unscaled and <c>long | string | Int128</c> with a scale;
    /// <c>CobolString.RefModPosition</c> takes the scaled three. The remaining carriers
    /// <see cref="DataItem.ElementType"/> can produce — <c>double</c>/<c>float</c> (a COMP-1/COMP-2
    /// leaf), <c>ManagedPointer</c>/<c>ProgramPointer</c>, an object reference, and a GROUP's <c>record struct</c>
    /// name — have none, and the emitted text was therefore not C# that compiles.
    /// ⚠ The FLOAT exclusion is a RULE, not a missing overload: a <c>double</c> operand can be
    /// fractional and §8.4.2.3.4 GR1b sets EC-BOUND-SUBSCRIPT when the expression "does not result in an
    /// integer", a test the scale-less overload does not perform — so a float belongs on the D18
    /// route, where the §15.4 temp applies the rule once, to the result. The unsigned-binary carriers are
    /// admitted UNSCALED only for the same reason from the other side: there is no scaled overload for them, and
    /// a scaled one has an integrality question to answer.
    /// MEASURED, six shapes, before this fix: <c>TE(FD1)</c> with <c>FD1 USAGE COMP-2</c>,
    /// <c>TE(BIG)</c> with <c>BIG PIC 9(20) COMP</c> and <c>TE(W-U)</c> with
    /// <c>W-U USAGE BINARY-DOUBLE UNSIGNED</c> each failed the BACKEND with
    /// <c>CS1503 cannot convert from 'double'/'System.Int128'/'ulong' to 'long'</c> — in the DEFAULT lane, on source
    /// §8.8.1.1 and §8.4.2.3.4 GR1b make perfectly legal — <c>W(FD1:2)</c> the same at a ref-mod bound, and a
    /// GROUP subscript under <c>--permissive</c> emitted the record struct (<c>CS1503 '_T_0' to 'long'</c>) or, for
    /// a class-tier BASED group, a name with no C# field at all (<c>CS0103</c>).</para>
    /// <para><b>Why a route and not only wider overloads.</b> The three INTEGER carriers above genuinely were
    /// missing overloads and got them (they are also needed by the second emitter, <c>RuntimeApi.TableOcc</c>,
    /// which renders the OCCURS DEPENDING current count at CODEGEN time and has no route to fall back to). But
    /// overloads alone can never close this: a group's carrier can never have a runtime overload, being a
    /// per-program generated type, and a pointer has no numeric value to convert at all. The
    /// route already exists and is the documented posture: <see cref="RenderSegment"/> is "an optimization over
    /// that route, never the arbiter of what is legal in the position", so an operand it cannot render is
    /// <see cref="MaterializeViaFragment"/>'s, where <c>ExpressionBinder</c> screens it under §8.8.1.1 and
    /// <c>NumericRenderer.FieldNum</c> — THE ONE numeric read — supplies the carrier-correct decode (the float
    /// sending check, the wide tier, and a group's §8.5.2.1 alphanumeric IMAGE, which is exactly the digit decode
    /// the <c>--permissive</c> message promises and the fast path never performed).</para>
    /// <para>⚠ Returning FALSE is always sound and never a verdict: the segment reroutes to D18, whose loud
    /// posture on failure is the caller's pre-existing one. The pre-promotion reading of <c>ElementType</c> is
    /// likewise safe in the one direction it can be wrong — promotion only ever moves a leaf TO
    /// <c>CharImage</c>/<c>string</c>, which is in the set, so a leaf admitted here stays admitted.</para></summary>
    private static bool HasPositionOverload(DataItem item, int scale)
    {
        string carrier = item.ElementType;
        foreach (string admitted in scale > 0 ? ScaledPositionCarriers : UnscaledPositionCarriers)
            if (admitted == carrier) return true;
        return false;
    }

    /// <summary>The carrier types <c>CobolTable.Occ(<i>x</i>)</c> declares a parameter for — the ONE list
    /// <see cref="HasPositionOverload"/> reads for a scale-0 operand, exposed so
    /// <c>PositionCarrierOverloadDriftTests</c> can compare it
    /// to the runtime method's ACTUAL overloads by reflection. Adding an overload without widening this list
    /// leaves the fast path routing a carrier it could now render; widening this list without the overload puts
    /// the CS1503 back. The test fails on either.</summary>
    internal static readonly string[] UnscaledPositionCarriers =
        ["long", "string", "Int128", "ulong", "UInt128"];

    /// <summary>The carrier types <c>CobolTable.Occ(<i>x</i>, int)</c> and
    /// <c>CobolString.RefModPosition(<i>x</i>, int)</c> declare a parameter for. It is NOT a superset of the
    /// unscaled list: the unsigned-binary carriers have no scaled overload (a scaled operand has an integrality
    /// question, so D18 is the right route for them), while <c>Int128</c> must be here because the D18 §15.4
    /// segment temp is a 30-digit / scale-9 item — the wide tier — and
    /// <see cref="MaterializeViaFragment"/> reads it back through <see cref="PositionRead"/>, so dropping it
    /// would break the very route this guard sends work to.</summary>
    internal static readonly string[] ScaledPositionCarriers = ["long", "string", "Int128"];
}
