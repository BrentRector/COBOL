// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// The ONE procedure table + its builders (P7 Step 10t — the plan's `ProcedureTableBuilder`, per-unit on
/// <see cref="BinderContext.Table"/>): the pc space (`_paras ∥ _paraSection ∥ _paraMethod` in LOCKSTEP —
/// every AddParagraph appends to all three), the section map (ISO §14.4.3), the method-local scope maps
/// (a method definition is its own source element — §3.164 with §3.165 and §11.7.1 — so §8.4.6.1 confines
/// its paragraph-names to it; registered through the ambient
/// <see cref="BinderContext.CurrentMethodScope"/> collection cursor), <see cref="ResolveProcedureOperand"/> (§8.4.2.2 — explicit OF/IN → in-section → global → section-name,
/// method-confined inside a method), and the DECLARATIVES half (ISO §14.3 / §14.9.49 USE — each
/// declarative section joins the same pc space; the USE sentence binds into a <see cref="BoundDeclarative"/>
/// scope, never a bound statement; the use procedure's range IS that section's own
/// <see cref="SectionInfo.Range"/>, §14.9.49.3 SR1 + §14.4.2).
/// </summary>
internal sealed class ProcedureTableBuilder(BinderContext ctx)
{
    private readonly List<(string Cobol, string Method, Core.SentenceContext[] Sentences)> _paras = [];
    // ⛔ ProcedureNameMap, never a bare Dictionary + TryAdd: §8.4.2.2.1 asks whether a spelling is UNIQUE, and
    // TryAdd's contract is to discard the evidence (kb/Work PB466).
    private readonly ProcedureNameMap<int> _paraIndex = new();
    private readonly ProcedureNameMap<SectionInfo> _sections = new();
    private readonly List<SectionInfo?> _paraSection = [];   // per-pc owning section (parallel to _paras;
                                                              // the ambient CURRENT section lives on Ctx — 10s)


    private readonly List<OoMethodScope?> _paraMethod = [];   // per-pc owning method (parallel to _paras; the
                                                              // ambient CURRENT scope lives on Ctx — 10s)

    private readonly List<int> _paraLine = [];   // per-pc source line (parallel to _paras) — the X3.23-1985
                                                 // DEBUG-LINE register value for a debug-subject procedure (VCR 7.17)

    // ── Procedure table (paragraphs + sections, ISO §14.4.3 / §8.4.2.2) ─────────────────────────────────────

    /// <summary>Register one paragraph (name + uniquified method key + its sentences) at the next pc. Inside a
    /// METHOD body (<see cref="_currentMethodScope"/> set — the class-body collection) the name declares
    /// METHOD-LOCALLY (ISO §8.4.6.1 over the method's own source element, §3.164 — sibling methods may reuse
    /// names and cross-method resolution must FAIL), so it registers in the method's own map, never the
    /// program-global fallback.</summary>
    public void AddParagraph(string name, Core.SentenceContext[] sentences, SectionInfo? section, HashSet<string> used)
    {
        ctx.Data.DeclareUserWord(name, UserWordKind.ParagraphName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
        string baseName = "P_" + DataItem.IdentifierCharacters(name.Replace('.', '_'));   // extended letters too (PB1402)
        string method = baseName;
        for (int n = 2; !used.Add(method); n++) method = $"{baseName}_{n}";
        // ⛔ DECLARE, never TryAdd (kb/Work PB466): a repeated spelling is KEPT, so §8.4.2.2.1's "No other name
        // has the identical spelling" stays answerable at the reference. The FIRST definition is still what an
        // unambiguous lookup returns; what changed is that the map now knows there was a second.
        if (ctx.CurrentMethodScope is { } ms)
            ms.Paras.Declare(name, _paras.Count);   // method-local declaration (§8.4.6.1 / §3.164)
        else
            _paraIndex.Declare(name, _paras.Count); // the program-wide paragraph declarations
        section?.Paras.Declare(name, _paras.Count); // in-section map for qualified / same-section resolution
        _paraSection.Add(section);
        _paraMethod.Add(ctx.CurrentMethodScope);
        _paraLine.Add(sentences.Length > 0 ? ctx.SourceLine(sentences[0]) : 0);   // DEBUG-LINE source line (VCR 7.17)
        _paras.Add((name, method, sentences));
    }

    /// <summary>Add the ISO §14.4.3 paragraph-name-OMITTED paragraph — "one or more successive sentences
    /// following the procedure division header or a section header". It takes a pc like any other paragraph so
    /// the dispatcher executes it, but it is DELIBERATELY registered in NO name map: having no paragraph-name it
    /// can never be the target of PERFORM / GO TO (§8.4.2.2 resolves procedure-NAMES only), and inventing a
    /// synthetic name would make it referenceable and collide with a user word. The display name carries spaces
    /// so it is not a well-formed COBOL word; the C# method name is generated independently.</summary>
    public void AddAnonymousParagraph(Core.SentenceContext[] sentences, SectionInfo? section, HashSet<string> used)
    {
        if (sentences.Length == 0) return;
        AddUnnamedParagraph(sentences, section, used);
    }

    /// <summary>The ONE pc a declarative section with ZERO paragraphs owns (§14.9.49.3 SR1: "zero, one, or more
    /// procedural paragraphs" — the section is still SELECTED, §14.9.49.4 GR3, so it needs a pc to dispatch to). It
    /// is a paragraph-name-OMITTED paragraph with no sentences: like <see cref="AddAnonymousParagraph"/> it is named
    /// nowhere (kb/Work PB990 — it used to be registered under the SECTION's own name, so every such section declared
    /// a paragraph its source never wrote, and <c>PERFORM</c> of the section was ambiguous with it).</summary>
    public void AddEmptyAnonymousParagraph(SectionInfo section, HashSet<string> used) =>
        AddUnnamedParagraph([], section, used);

    private void AddUnnamedParagraph(Core.SentenceContext[] sentences, SectionInfo? section, HashSet<string> used)
    {
        string method = "P__Anon";
        for (int n = 2; !used.Add(method); n++) method = $"P__Anon_{n}";
        _paraSection.Add(section);
        _paraMethod.Add(ctx.CurrentMethodScope);
        _paraLine.Add(sentences.Length > 0 ? ctx.SourceLine(sentences[0]) : 0);
        // The paragraph-name-OMITTED paragraph (§14.4.3) has NO name — the empty string, never a display
        // placeholder (kb/Work PB63 / RV-15.30.3-2: EXCEPTION-LOCATION's r2b2 field printed the placeholder where
        // the standard defines an empty procedure field or the bare section-name).
        _paras.Add(("", method, sentences));
    }

    // ── Exception-checking (Format-3) PERFORM handler pc-ranges (ISO §14.9.28.4 GR17) ───────────────────────
    // The WHEN / WHEN OTHER / WHEN COMMON handler bodies (imp-2/3/4) are bound IN LEXICAL CONTEXT (in
    // EcBindExceptionPerform — correct §8.4.2.2 scope + the GR14 overlay already popped) and registered here as
    // synthetic, UNREFERENCEABLE pc-range paragraphs. They are APPENDED ABOVE the whole main pc space by
    // StatementBinder after the main bind loop (so `_paras.Count` is the frozen main count throughout binding),
    // then walled off the top-level fall-through by the dispatcher (design §9.5.3). imp-1 and imp-5 (FINALLY) stay
    // inline in the host paragraph; only imp-2/3/4 become pc-ranges (run via the reused __RunUse).
    private readonly List<BoundParagraph> _f3Handlers = [];
    private readonly List<int> _f3Owners = [];   // owning PerformId per handler (parallel to _f3Handlers)
    private readonly List<OoMethodScope?> _f3HandlerMethod = [];   // owning method scope per handler (null for a
                                                                    // program unit; parallel to _f3Handlers — the
                                                                    // per-method slice source, design SSOT §9.10)

    public IReadOnlyList<BoundParagraph> F3Handlers => _f3Handlers;
    public IReadOnlyList<int> F3HandlerOwners => _f3Owners;

    /// <summary>The owning method scope of each appended Format-3 handler (parallel to <see cref="F3Handlers"/>;
    /// null in a program unit) — the source of each method's contiguous handler sub-range (design SSOT §9.10).</summary>
    public IReadOnlyList<OoMethodScope?> F3HandlerMethods => _f3HandlerMethod;

    /// <summary>Register one already-bound Format-3 handler body (imp-2/3/4) as a synthetic pc-range paragraph and
    /// return its pc: the frozen main paragraph count (declaratives + all nondeclarative paragraphs) + the
    /// registration ordinal — dense, collision-free, and equal to its index once StatementBinder appends the
    /// side-list (the base it records as <c>BoundProgram.F3HandlerBasePc</c>). The body is a
    /// single sentence-group (a handler has no paragraph structure); an empty body still gets one no-op pc (the
    /// bounded <c>__RunUse</c>/<c>__Dispatch(pc,pc)</c> needs a range). Registered in NO name map — unreferenceable
    /// (the <see cref="AddAnonymousParagraph"/> precedent). Nesting-safe: a handler that itself binds an inner F3
    /// PERFORM registers the inner handlers first (lower ordinals); each pc still equals its final appended index.</summary>
    public int AddF3Handler(IReadOnlyList<BoundStatement> body, int performId, int line)
    {
        int pc = _paras.Count + _f3Handlers.Count;
        _f3Handlers.Add(new BoundParagraph("(exception-checking PERFORM handler)", new[] { body }, line));
        _f3Owners.Add(performId);
        _f3HandlerMethod.Add(ctx.CurrentMethodScope);   // the owning method (null in a program) — the per-method slice
        return pc;
    }

    public void CollectParagraphs(Core.ProcedureDivisionContext pd)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);

        // DECLARATIVES first (ISO §14.2.3 GR1 — execution begins with the first NONdeclarative procedure; the
        // declarative sections share the ONE pc space, entered only via the USE dispatch or an explicit PERFORM —
        // §14.9.49.3 SR4 admits no other reference into a declarative section from outside it, and
        // ResolveProcedureOperand enforces it, kb/Work PB362). The walk records the BoundDeclarative scopes
        // (StatementBinder.Declaratives.cs).
        foreach (var dp in pd.declarativePart())
            foreach (var sec in dp.declarativeSection())
            {
                using var _ = ctx.Edition.At(sec);   // the declarative section's position (kb/Work PB82)
                DeclCollectSection(sec, used);
            }
        _entryPc = _paras.Count;

        // §14.4.3 — sentences written directly after the PROCEDURE DIVISION header, with no paragraph-name.
        // They form the first nondeclarative paragraph, so they must take the ENTRY pc (execution begins with
        // the first nondeclarative procedure, §14.2.3 GR1) — hence before the procedureUnit walk.
        AddAnonymousParagraph(pd.sentence(), null, used);

        foreach (var unit in pd.procedureUnit())
        {
            using var _ = ctx.Edition.At(unit);   // the paragraph / section header's position (kb/Work PB82)
            if (unit.paragraphDefinition() is { } para)
                AddParagraph(para.paragraphName().GetText(), para.sentence(), null, used);
            else if (unit.sectionDefinition() is { } section)
            {
                // A section's paragraphs are contiguous in the pc sequence, so the section IS a pc range:
                // GO TO section transfers to its first paragraph (ISO §14.9.17), PERFORM section runs first
                // statement of its first paragraph through last statement of its last (ISO §14.9.28).
                ctx.Data.DeclareUserWord(section.sectionName().GetText(), UserWordKind.SectionName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
                var info = new SectionInfo(section.sectionName().GetText(), _paras.Count);
                // §14.4.3 — a section header may likewise be followed directly by unnamed sentences; they are
                // the section's first paragraph, so GO TO / PERFORM <section> enters them (§14.9.17/§14.9.28).
                AddAnonymousParagraph(section.sentence(), info, used);
                foreach (var p in section.paragraphDefinition())
                    AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
                info.CloseAt(_paras.Count - 1);   // zero paragraphs ⇒ the range stays EMPTY (§14.4.2)
                _sections.Declare(info.Name, info);
            }
        }

        // X3.23-1985 USE FOR DEBUGGING (VCR Table 7 row 7.17): resolve the debug SUBJECTS now that every
        // nondeclarative procedure is in the pc space — the ALL PROCEDURES / procedure-name legs bind to real
        // trigger points; the data-name / file-name / cd-name subject kinds and the SORT/MERGE cause taxonomy
        // are staged loud (COBOLNET1571). No-op unless WITH DEBUGGING MODE collected a debug declarative.
        FinalizeDebug(pd);
    }

    /// <summary>⛔ THE ONE <c>procedure-name</c> OPERAND RESOLUTION for a STATEMENT (kb/Work PB390): resolve, or
    /// REPORT COBOLNET1639 and return null. Every statement whose general format prints procedure-name — PERFORM,
    /// GO TO (both formats), ALTER, RESUME AT, SORT/MERGE INPUT and OUTPUT PROCEDURE — enters here, so a name that
    /// resolves to nothing is a compile-time DIAGNOSTIC by construction and not a per-site habit.
    /// <para>⛔ THIS EXISTS BECAUSE THE SILENT TWIN WAS USED EIGHT TIMES. Each site turned the null into a
    /// <c>BoundUnsupported</c> — which the emitter renders as <c>NotImplemented.Run(...)</c> — so a misspelled
    /// PERFORM COMPILED, produced an assembly, and aborted the run unit claiming WiseOwl COBOL had not implemented a
    /// feature; on a path the flow skipped it said nothing at all. ISO §4.2.2 ¶2 requires the compile-time
    /// mechanism. The message itself lives in the ONE syntax-rule catalog
    /// (<see cref="Validation.StatementValidation.RejectProcedureName"/>), never here.</para>
    /// <para>⛔ THE DECLARATIVES BOUNDARY IS TESTED HERE, ONCE, FOR EVERY STATEMENT (kb/Work PB362). ISO §14.9.49.3
    /// SR3 and SR4 restrict every procedure-name REFERENCE, and this is the funnel every statement reference passes
    /// through, so a verb that takes a procedure-name inherits both rules by construction. The quiet prescan
    /// (<see cref="ResolveProcedureQuiet"/>) is not a reference of its own and is not tested.</para></summary>
    /// <param name="pn">The procedure-name as written.</param>
    /// <param name="verb">The statement or phrase for the message, e.g. "PERFORM", "SORT INPUT PROCEDURE".</param>
    /// <param name="rule">The caller's OWN syntax rule quoted with its citation (PERFORM §14.9.28.3 SR12/SR13);
    /// "" where the statement states none and §8.4.2.1 alone decides (GO TO, ALTER, RESUME AT).</param>
    /// <param name="kind">Which statement writes the reference, for the declaratives boundary (ISO §14.9.49.3
    /// SR3/SR4). Only PERFORM and RESUME pass anything but the default — see
    /// <see cref="ProcedureReferenceKind"/>.</param>
    public ResolvedProcedure? ResolveProcedureOperand(
        Core.ProcedureNameContext pn, string verb, string rule = "",
        ProcedureReferenceKind kind = ProcedureReferenceKind.Other)
    {
        var resolved = Resolve(pn);
        if (resolved.Procedure is { } procedure)
            return ctx.Validation.CheckDeclarativesBoundary(procedure, ctx.CurrentSection, pn.GetText(), verb, kind)
                ? procedure : null;
        string head = pn.GetChild(0).GetText();
        // ⛔ "IDENTIFIES MORE THAN ONE" IS NOT "IDENTIFIES NONE" (kb/Work PB466). Both fail §8.4.2.1's "a
        // reference that uniquely identifies that resource", but they are different rules with different
        // repairs, so the ambiguous case gets its own message and its own code rather than being folded into
        // COBOLNET1639's "no paragraph or section carries that name", which would be a false statement about
        // a program that carries the name twice.
        if (resolved.Ambiguity is { } ambiguity)
        {
            ctx.Validation.RejectAmbiguousProcedureName(ambiguity, verb);
            return null;
        }
        string qualifier = pn.ChildCount >= 3 ? " " + pn.GetChild(1).GetText() + " " + pn.GetChild(2).GetText() : "";
        ctx.Validation.RejectProcedureName(head + qualifier, head, verb, rule, ctx.CurrentMethodScope is not null);
        return null;
    }

    /// <summary>Resolve a procedure-name reference to its inclusive pc range (ISO §8.4.2.2): a section name is its
    /// paragraph range; a paragraph is (pc, pc). The head/qualifier are taken from the context's CHILDREN — never
    /// <c>GetText()</c> of the whole context, which concatenates <c>PAR-1A OF SEC-1</c> into an unmatchable key.
    /// Resolution order: explicit <c>OF/IN section</c> qualifier → the named section's own map; unqualified → a
    /// paragraph of the CURRENT section (implicit qualification of duplicated names), then the global first-defined
    /// paragraph, then a section name. Null when unknown.
    /// <para>⛔ QUIET, AND THE NAME SAYS SO (kb/Work PB390): this arm reports NOTHING, so it belongs only to a
    /// PRESCAN that is not the statement's own bind — the ALTER switch-field prescan and the USE FOR DEBUGGING
    /// SORT/MERGE-overlap scan, each of which runs before (or beside) the bind that will report. A STATEMENT
    /// operand goes through <see cref="ResolveProcedureOperand"/>, which cannot resolve to nothing in
    /// silence.</para>
    /// <para>An AMBIGUOUS name resolves to nothing here, exactly as an unknown one does: a prescan has no use
    /// for "some procedure with this spelling", and the statement's own operand bind reports the ambiguity and
    /// fails the compile either way (kb/Work PB466).</para></summary>
    public PcRange? ResolveProcedureQuiet(Core.ProcedureNameContext pn) => Resolve(pn).Range;

    /// <summary>⛔ THE ONE §8.4.2.2 procedure-name resolution algorithm — <b>one body, not one per scope</b>.
    /// It runs over whichever pair of name maps the reference stands in: a METHOD body's own maps when one is
    /// current, the program's otherwise. Inside a method resolution is CONFINED to the method's maps because a
    /// method definition begins with an identification division (ISO §11.7.1), so it is a contained SOURCE UNIT
    /// (§3.165) and thus its own SOURCE ELEMENT (§3.164), and §8.4.6.1 confines paragraph-names and
    /// section-names to the source element that declares them — the legacy trap-#10 cross-method reject made
    /// structural. (NOT §11.7 alone, which the old comment cited: that is the METHOD-ID paragraph, whose GR5
    /// scoping rule is about the method's DATA DIVISION words — kb/Work PB390.)
    /// <para>⛔ THE TWO SCOPES WERE TWO COPIES OF THIS ALGORITHM, and the second inherited every defect of the
    /// first — which is exactly how the method scope came to have the SAME uniqueness hole one level down
    /// (kb/Work PB466). Selecting the maps and running one body is what keeps them equal.</para>
    /// <para><b>The order, and the rule each step implements.</b> An explicit <c>IN/OF section-name</c>
    /// qualifier resolves against that section's own map (§8.4.2.2.2 format 4). Unqualified: first a paragraph
    /// of the CURRENT section — §8.4.2.2.1 rule 6, "The name is a paragraph-name and the section containing the
    /// reference also contains the named paragraph", the one excuse a duplicated paragraph-name gets — then the
    /// whole source element, where §8.4.2.2.1 rule 1 ("No other name has the identical spelling") decides: the
    /// reference is unique only if the spelling is carried by EXACTLY ONE procedure, counting paragraphs and
    /// sections together, because §14.4.1 makes a procedure-name "a word used to refer to a paragraph or
    /// section". Two or more and the reference identifies no one resource: §8.4.2.2.3 SR1 requires
    /// qualification, and this returns the ambiguity instead of an arbitrary pick.</para>
    /// <para>The head/qualifier are taken from the context's CHILDREN — never <c>GetText()</c> of the whole
    /// context, which concatenates <c>PAR-1A OF SEC-1</c> into an unmatchable key.</para></summary>
    private ProcedureResolution Resolve(Core.ProcedureNameContext pn) =>
        ResolveProcedureWord(pn.GetChild(0).GetText(),
            pn.ChildCount >= 3 ? pn.GetChild(2).GetText() : null,
            ctx.CurrentSection);

    /// <summary>The resolution algorithm over WORDS, so the one reference site that has no
    /// <c>procedureName</c> parse context — USE FOR DEBUGGING's <c>ON procedure-name</c> subject, which the
    /// grammar spells as a dataReference because the phrase also admits file-, cd- and data-names — resolves
    /// through this method and not through a second copy of it.</summary>
    /// <param name="head">The procedure-name.</param>
    /// <param name="qualifier">The <c>IN/OF section-name</c> qualifier, or null.</param>
    /// <param name="referencingSection">The section CONTAINING the reference — what §8.4.2.2.1 rule 6 is
    /// measured against. Passed in rather than read from the ambient cursor, because the debug subject is
    /// resolved after collection, when the cursor no longer stands where the reference was written.</param>
    private ProcedureResolution ResolveProcedureWord(string head, string? qualifier, SectionInfo? referencingSection)
    {
        var (paras, sections) = ctx.CurrentMethodScope is { } m
            ? (m.Paras, m.Sections)
            : (_paraIndex, _sections);

        if (qualifier is not null)
        {
            if (!sections.TryResolve(qualifier, out var q)) return default;   // no such section → COBOLNET1639
            // The QUALIFIER itself has to identify one section, and a section-name takes no qualifier of its
            // own (§8.4.2.2.2 format 4), so a duplicated one cannot be repaired by writing more.
            if (sections.IsDuplicated(qualifier))
                return Ambiguous(ProcedureAmbiguityRule.Qualification, qualifier, DescribeSections(sections, qualifier));
            if (!q.Paras.TryResolve(head, out int qpc)) return default;
            if (q.Paras.IsDuplicated(head))
                return Ambiguous(ProcedureAmbiguityRule.InSectionDuplicate, head, DescribeInSection(q, head), q.Name);
            return Found(PcRange.At(qpc), q);
        }

        // §8.4.2.2.1 rule 6 — and §8.4.2.2.3 SR7 is its other half: the excuse holds only while the containing
        // section declares the name ONCE ("If explicitly referenced, a paragraph-name shall not be duplicated
        // within a section"), which is why the SR7 test sits here, at the reference, rather than at the
        // declaration — an unreferenced duplicate is legal.
        if (referencingSection is { } cur && cur.Paras.TryResolve(head, out int local))
        {
            if (cur.Paras.IsDuplicated(head))
                return Ambiguous(ProcedureAmbiguityRule.InSectionDuplicate, head, DescribeInSection(cur, head), cur.Name);
            return Found(PcRange.At(local), cur);
        }

        int candidates = paras.CountOf(head) + sections.CountOf(head);   // §8.4.2.2.1 rule 1, over §14.4.1's
        if (candidates == 0) return default;                             // "paragraph or section"
        if (candidates > 1)
            return Ambiguous(ProcedureAmbiguityRule.Qualification, head, DescribeCandidates(paras, sections, head));
        // The owning section: for a PARAGRAPH it is the per-pc `_paraSection` entry recorded at collection —
        // the one structure that says which section a pc belongs to, and the same one StatementBinder's per-pc
        // pass and SetAlterBinder read; for a SECTION-name the section IS the answer.
        if (paras.TryResolve(head, out int pc)) return Found(PcRange.At(pc), SectionOfPc(pc));
        if (sections.TryResolve(head, out var sec)) return Found(sec.Range, sec);
        return default;
    }

    /// <summary>A resolution that SUCCEEDED, carrying the range together with the section that owns the
    /// denoted procedure — the ONE constructor for the success case, so no path can forget the section
    /// (kb/Work PB433).</summary>
    private static ProcedureResolution Found(PcRange range, SectionInfo? section) =>
        new(new ResolvedProcedure(range, section), null);

    /// <summary>The section that owns <paramref name="pc"/> — <see cref="ParaSections"/>, which
    /// <c>AddParagraph</c> and <c>AddAnonymousParagraph</c> fill in lockstep with the pc space. Out of range (a
    /// synthetic Format-3 handler pc, which is in no name map and so never resolves here) answers null.</summary>
    private SectionInfo? SectionOfPc(int pc) =>
        (uint)pc < (uint)_paraSection.Count ? _paraSection[pc] : null;

    private static ProcedureResolution Ambiguous(
        ProcedureAmbiguityRule rule, string name, IReadOnlyList<string> candidates, string? section = null) =>
        new(null, new ProcedureAmbiguity(rule, name, candidates, section));

    /// <summary>The SR7 candidates: the section's OWN repeated declarations of the name, each at its source
    /// line — the two places the user has to look, which naming the section alone does not tell them.</summary>
    private List<string> DescribeInSection(SectionInfo section, string head)
    {
        var described = new List<string>(section.Paras.CountOf(head));
        foreach (int pc in section.Paras.Definitions(head)) described.Add($"paragraph '{head}'" + AtLine(pc));
        return described;
    }

    /// <summary>Every procedure carrying <paramref name="head"/>, described for the diagnostic in declaration
    /// order — paragraphs first (each with its containing section and source line), then sections. The line
    /// numbers are what makes the message actionable: the user has to find the OTHER declaration.</summary>
    private List<string> DescribeCandidates(
        ProcedureNameMap<int> paras, ProcedureNameMap<SectionInfo> sections, string head)
    {
        var described = new List<string>(paras.CountOf(head) + sections.CountOf(head));
        foreach (int pc in paras.Definitions(head))
            described.Add($"paragraph '{head}'"
                + (_paraSection[pc] is { } s ? $" in section '{s.Name}'" : " (in no section)")
                + AtLine(pc));
        foreach (var sec in sections.Definitions(head)) described.Add(DescribeSection(sec));
        return described;
    }

    private List<string> DescribeSections(ProcedureNameMap<SectionInfo> sections, string name)
    {
        var described = new List<string>(sections.CountOf(name));
        foreach (var sec in sections.Definitions(name)) described.Add(DescribeSection(sec));
        return described;
    }

    private string DescribeSection(SectionInfo sec) =>
        $"section '{sec.Name}'" + (sec.Range.IsEmpty ? "" : AtLine(sec.StartPc));

    private string AtLine(int pc) => pc < _paraLine.Count && _paraLine[pc] > 0 ? $" at line {_paraLine[pc]}" : "";

    /// <summary>The pc space (name + uniquified method key + sentences) — the bind loops walk it; parallel
    /// in LOCKSTEP to <see cref="ParaSections"/> (owning section) and <see cref="ParaMethods"/> (owning
    /// method scope). Every <see cref="AddParagraph"/> appends to all three.</summary>
    public IReadOnlyList<(string Cobol, string Method, Core.SentenceContext[] Sentences)> Paragraphs => _paras;
    public IReadOnlyList<SectionInfo?> ParaSections => _paraSection;
    public IReadOnlyList<OoMethodScope?> ParaMethods => _paraMethod;

    private int _entryPc;
    private readonly List<BoundDeclarative> _declaratives = [];

    /// <summary>The narrow declarative surface EcBinder's RESUME SR1–SR3 checks read (10r; hoists to
    /// ProcedureTableBuilder at 10t and these host edges delete).</summary>
    public int EntryPc => _entryPc;
    public IReadOnlyList<BoundDeclarative> Declaratives => _declaratives;
    // ── The USE duplicate-operand screens (ISO §14.9.49.3 SR7, SR8, SR9, SR14) ──────────────────────────────
    // Four instances of ONE sentence — "the same X shall not be specified in more than one USE statement within
    // the same procedure division" — so ONE mechanism enforces all four (ConstructOperandRegister{TKey} holds
    // the construct-vs-operand boundary those rules draw; the USE STATEMENT is the construct here, the Format-2
    // COLLATING SEQUENCE CLAUSE is the construct for its second consumer). ⛔ PER PROCEDURE DIVISION: a
    // ProcedureTableBuilder is built per bound unit, and a CLASS unit's one builder collects EVERY method's
    // procedure division (kb/Work PB1010 — a method definition takes Format 1, §14.2.2 SR10), so
    // CollectMethodDeclaratives re-makes these four for each method's declaratives: a file named in one
    // method's USE is not "more than one USE statement within the same procedure division" as another's.
    private ConstructOperandRegister<int> _useModes = new();                                      // SR7
    private ConstructOperandRegister<string> _useFiles = new(CobolNames.Comparer);    // SR8
    private ConstructOperandRegister<ReportGroupModel> _useReportGroups = new();                   // SR9
    private ConstructOperandRegister<string> _useEcPairs = new(CobolNames.Comparer);  // SR14

    /// <summary>Collect a METHOD's declaratives portion (kb/Work PB1010; ISO §14.2.2 SR10 — "Formats 1 and 2 may be
    /// specified in a source element if and only if that source element is a function definition, a function
    /// prototype definition, a method definition, a program definition, or a program prototype definition"), at
    /// the current end of the class's ONE pc space and under the method scope the caller has entered
    /// (<see cref="BinderContext.CurrentMethodScope"/>), through the SAME <see cref="DeclCollectSection"/> a
    /// program's declaratives take. Returns the method's own <see cref="BoundDeclarative"/>s in source order —
    /// §14.9.49.4 GR3 analyzes "the USE statements in the source element", and a method IS the source element
    /// that contains its statements (GR4 a)), so the emitter selects over exactly this list for them.</summary>
    public IReadOnlyList<BoundDeclarative> CollectMethodDeclaratives(Core.ProcedureDivisionContext pd, HashSet<string> used)
    {
        _useModes = new();
        _useFiles = new(CobolNames.Comparer);
        _useReportGroups = new();
        _useEcPairs = new(CobolNames.Comparer);
        int first = _declaratives.Count;
        foreach (var dp in pd.declarativePart())
            foreach (var sec in dp.declarativeSection())
            {
                using var _ = ctx.Edition.At(sec);
                DeclCollectSection(sec, used);
            }
        return _declaratives.Count == first ? [] : [.. _declaratives.Skip(first)];
    }

    /// <summary>Close the USE statement just bound: its operands become the procedure division's, so a repeat
    /// in a LATER USE statement is the violation SR7/SR8/SR9/SR14 name and a repeat inside the statement itself
    /// never was. Called from the ONE place a USE statement is bound (<see cref="DeclCollectSection"/>).</summary>
    private void DeclEndUseStatement()
    {
        _useModes.EndConstruct();
        _useFiles.EndConstruct();
        _useReportGroups.EndConstruct();
        _useEcPairs.EndConstruct();
    }

    /// <summary>Collect one declarative section into the pc space: the USE sentence (SR1 — the section's first
    /// sentence), an anonymous paragraph for any further leading sentences (the CCVS handler-before-the-first-
    /// paragraph shape, e.g. SQ103A), then the named paragraphs.</summary>
    private void DeclCollectSection(Core.DeclarativeSectionContext sec, HashSet<string> used)
    {
        string name = sec.sectionName().GetText();
        ctx.Data.DeclareUserWord(name, UserWordKind.SectionName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
        // isDeclarative: THE fact §14.9.28.3 SR11 (and GO TO's / ALTER's analogous constraints) asks about,
        // recorded where it is known rather than re-derived from pc arithmetic later.
        var info = new SectionInfo(name, _paras.Count, isDeclarative: true);

        // SR1: the first sentence consists of exactly one USE statement.
        var leading = sec.sentence();
        DeclScope? scope = null;
        if (leading.Length == 0
            || leading[0].statement() is not { Length: 1 } first
            || first[0].useStatement() is not { } use)
            ctx.Edition.Error("COBOLNET0897", $"declarative section '{name}': the first sentence shall consist "
                + "of a single USE statement (ISO §14.3 / §14.9.49.3 SR1)");
        else if (use.DEBUGGING() is not null)
        {
            // X3.23-1985 USE FOR DEBUGGING (the '85 debug facility, deleted by ISO 2002 — 0902-gated ≥2002 by
            // the version-conformance pass, VCR Table 7 row 7.17). Accepted-inert at 85 per the '85 rules: WITHOUT
            // SOURCE-COMPUTER … WITH DEBUGGING MODE the whole debugging section is compiled as if it were
            // comment lines (skip it — nothing binds, its names leave the pc space). WITH the switch the section
            // IS compiled and, with the object-time switch ON (RunUnit.DebugMode default true — the CCVS posture),
            // its ON procedure-name / ALL PROCEDURES triggers fire (the DEBUG-ITEM register family is modeled).
            // The section body is collected below (an ordinary pc range invoked by the emitted __RunDebug, not a
            // BoundDeclarative — scope stays null); FinalizeDebug records the subjects once every procedure is in.
            if (!ctx.Data.DebuggingModeDeclared) return;
            ctx.Data.ActivateDebugRegisters();   // make DEBUG-* references resolvable while this section binds
            _pendingDebug.Add((info, use));      // info.EndPc is filled after the body is collected below
            // fall through to collect the section body into the pc space (scope stays null — no BoundDeclarative)
        }
        else
        {
            scope = DeclBindUse(use, name);
            // The USE statement is COMPLETE — fold its operands into the procedure division's registers. Until
            // this point a repeated operand belongs to the statement being bound, and §14.9.49.3 SR7/SR8/SR9/
            // SR14 each forbid the repeat only across "more than one USE statement".
            DeclEndUseStatement();
        }

        // Leading sentences past the USE form an anonymous paragraph at the section start (handler bodies that
        // CCVS writes directly under the section header) — §14.4.3's paragraph-name-OMITTED paragraph, named
        // NOWHERE. ⛔ It used to be registered through AddParagraph under the SECTION's own name (kb/Work PB990), which
        // declared a paragraph the source never wrote: a legal `PERFORM handler-section` drew COBOLNET2121 "declared 2
        // times (paragraph 'H' in section 'H'; section 'H')", and §8.3.2.2's one-type-per-word census found the section
        // name used as a paragraph-name.
        AddAnonymousParagraph(leading.Skip(1).ToArray(), info, used);
        foreach (var p in sec.declarativeParagraph())
            AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
        // A declarative section with ZERO paragraphs is legal (§14.9.49.3 SR1 — "zero, one, or more procedural
        // paragraphs"; §14.4.2), and it is still SELECTED: §14.9.49.4 GR3 says "the first declarative that
        // satisfies the selection criteria is executed and no other declaratives are executed", so the search
        // stops at it and nothing else may run. Giving it ONE no-op pc keeps that a plain bounded dispatch
        // rather than a second selector shape — and it is the invariant DispatchState.RunUseCall asserts when it
        // refuses an empty PcRange. Relax this and the SELECTOR ARMS, not the renderer, are what must learn to
        // say "selected, ran nothing".
        if (_paras.Count == info.StartPc)
            AddEmptyAnonymousParagraph(info, used);

        info.CloseAt(_paras.Count - 1);
        // A METHOD's declarative section is a name in the METHOD's procedure division (§8.3.2.2.28 — "A section-name
        // identifies a section in the procedure division"), declared where the method's other sections are — the
        // same split AddParagraph makes for paragraph-names (kb/Work PB1010).
        (ctx.CurrentMethodScope?.Sections ?? _sections).Declare(info.Name, info);

        if (scope is { } s)
            // ISO §14.9.49.3 SR1: "The remainder of the section shall consist of zero, one, or more procedural
            // paragraphs that define the procedures to be used" — so the use procedure IS the section, whose
            // extent §14.4.2 fixes at the next section header or END DECLARATIVES. The range is taken whole;
            // nothing inspects paragraph shape to shorten it (kb/Work PB367).
            _declaratives.Add(new BoundDeclarative(
                name, info.Range, s.Files, s.ModeIndex, s.Global, s.Report, s.EcEntries, s.Eo));
    }

    // ── X3.23-1985 USE FOR DEBUGGING (VCR Table 7 row 7.17) ────────────────────────────────────────────────
    // The debug declaratives collected under WITH DEBUGGING MODE (section info + the USE statement), pending
    // subject resolution once every nondeclarative procedure is in the pc space (FinalizeDebug).
    private readonly List<(SectionInfo Section, Core.UseStatementContext Use)> _pendingDebug = [];
    private readonly List<BoundDebugSubject> _debugSubjects = [];

    /// <summary>The X3.23-1985 debug-facility trigger subjects (VCR Table 7 row 7.17) — each a nondeclarative
    /// procedure whose entry the emitter instruments to populate DEBUG-ITEM and run the debugging declarative;
    /// empty unless a procedure-subject USE FOR DEBUGGING was collected under WITH DEBUGGING MODE.</summary>
    public IReadOnlyList<BoundDebugSubject> DebugSubjects => _debugSubjects;

    /// <summary>Resolve the collected debug declaratives to trigger SUBJECTS now that the whole pc space exists
    /// (X3.23-1985 debug module, VCR Table 7 row 7.17). <c>ALL PROCEDURES</c> and a bare procedure-name bind to
    /// real trigger points at NONdeclarative procedures (a debugging declarative is never debugged — the '85
    /// ALL PROCEDURES exclusion, DB101A "USE PROCEDURE NOT EXECUTED"). The data-name (incl. ALL REFERENCES OF),
    /// file-name, and cd-name subject kinds are STAGED — rejected loud COBOLNET1571 (their after-statement /
    /// after-I-O trigger insertion and the DEBUG-SUB subscript rendering are not modeled). A SORT/MERGE INPUT/
    /// OUTPUT procedure that is also a debug subject is likewise staged (the SORT INPUT/OUTPUT/MERGE OUTPUT
    /// DEBUG-CONTENTS cause is not modeled — rejecting avoids a silent wrong cause).</summary>
    private void FinalizeDebug(Core.ProcedureDivisionContext pd)
    {
        if (_pendingDebug.Count == 0) return;
        foreach (var (section, use) in _pendingDebug)
        {
            foreach (var t in use.useDebugTarget())
            {
                if (t.PROCEDURES() is not null)          // ALL PROCEDURES → every nondeclarative procedure
                {
                    for (int pc = _entryPc; pc < _paras.Count; pc++)
                        AddDebugSubject(pc, section);
                }
                else if (t.REFERENCES() is not null)     // ALL REFERENCES OF identifier-1 → data-name subject (staged)
                {
                    ctx.Edition.Error("COBOLNET1571", $"declarative section '{section.Name}': USE FOR DEBUGGING "
                        + "ON ALL REFERENCES OF a data item (the after-statement data trigger with DEBUG-CONTENTS/"
                        + "DEBUG-SUB rendering) is recognized but not modeled — only ON procedure-name / ALL "
                        + "PROCEDURES is implemented (X3.23-1985 debug module; VCR Table 7 row 7.17)");
                }
                else if (t.dataReference() is { } dr)    // bare name: procedure-name, else file/data/cd (staged)
                {
                    string nm = dr.cobolWord()?.GetText() ?? dr.GetText();
                    // ⛔ THE SAME §8.4.2.2 RESOLUTION AS EVERY OTHER PROCEDURE-NAME REFERENCE, through the same
                    // method (kb/Work PB466) — USE FOR DEBUGGING ON procedure-name is an explicit reference, so
                    // an ambiguous spelling owes the same diagnostic here as it does at GO TO. The referencing
                    // section is THIS declarative section, which is what §8.4.2.2.1 rule 6 is measured against.
                    // A range covers both cases the leg needs: a paragraph is (pc, pc), a section is its whole
                    // paragraph range, and every pc in the range becomes a trigger point either way.
                    var subject = dr.dataReferenceSuffix().Length == 0
                        ? ResolveProcedureWord(nm, null, section)
                        : default;
                    if (subject.Ambiguity is { } dambiguity)
                        ctx.Validation.RejectAmbiguousProcedureName(dambiguity, "USE FOR DEBUGGING ON");
                    else if (subject.Range is { } drange)
                        for (int pc = drange.Start; pc <= drange.End; pc++)
                            AddDebugSubject(pc, section);
                    else
                        ctx.Edition.Error("COBOLNET1571", $"declarative section '{section.Name}': USE FOR DEBUGGING "
                            + $"ON '{nm}' names a file-name, cd-name, or data item (not a procedure-name of this "
                            + "program) — only the ON procedure-name / ALL PROCEDURES leg is modeled; the file / "
                            + "data / communication debug triggers are not (X3.23-1985 debug module; VCR Table 7 row 7.17)");
                }
            }
        }

        // A SORT/MERGE INPUT/OUTPUT procedure that is also a debug subject would trigger with a stale cause — the
        // SORT INPUT/OUTPUT / MERGE OUTPUT DEBUG-CONTENTS taxonomy is not modeled. Reject loud rather than emit a
        // silent wrong cause (X3.23-1985; the DB2xx SORT/MERGE witnesses are staged).
        if (_debugSubjects.Count > 0)
        {
            var subjectPcs = _debugSubjects.Select(s => s.SubjectPc).ToHashSet();
            foreach (var (name, rng) in SortMergeProcedureRanges(pd))
                if (Enumerable.Range(rng.Start, rng.End - rng.Start + 1).Any(subjectPcs.Contains))
                {
                    ctx.Edition.Error("COBOLNET1571", $"the SORT/MERGE {name} PROCEDURE is also a USE FOR "
                        + "DEBUGGING subject: the SORT INPUT/OUTPUT / MERGE OUTPUT DEBUG-CONTENTS cause is not "
                        + "modeled (only the plain-transfer / fall-through / PERFORM-loop / START-PROGRAM causes "
                        + "are) — X3.23-1985 debug module; VCR Table 7 row 7.17");
                    break;
                }
        }
    }

    /// <summary>Record one debug trigger subject (the subject pc's name + source line + the debugging section's
    /// invokable pc range) — deduplicated so a procedure named twice does not double-fire.</summary>
    private void AddDebugSubject(int pc, SectionInfo section)
    {
        if (pc < 0 || pc >= _paras.Count) return;
        if (_debugSubjects.Any(s => s.SubjectPc == pc)) return;
        _debugSubjects.Add(new BoundDebugSubject(
            pc, _paras[pc].Cobol, _paraLine[pc], section.StartPc, section.EndPc));
    }

    /// <summary>The pc range of each SORT/MERGE INPUT/OUTPUT PROCEDURE in the procedure division (for the debug
    /// SORT/MERGE-overlap staging check). Resolves the phrase's procedure-name(s) via the ordinary procedure
    /// table (THRU forms span first..last).</summary>
    private List<(string Kind, PcRange Range)> SortMergeProcedureRanges(
        Core.ProcedureDivisionContext pd)
    {
        var results = new List<(string, PcRange)>();
        void Add(string kind, Core.ProcedureNameContext[] pns)
        {
            if (pns.Length == 0) return;
            if (ResolveProcedureQuiet(pns[0]) is not { } first) return;
            var last = pns.Length > 1 ? ResolveProcedureQuiet(pns[^1]) : first;
            if (last is { } l) results.Add((kind, first.Through(l)));
        }
        void Walk(Antlr4.Runtime.Tree.IParseTree node)
        {
            switch (node)
            {
                case Core.SortInputProcedurePhraseContext ip: Add("INPUT", ip.procedureName()); return;
                case Core.SortOutputProcedurePhraseContext op: Add("OUTPUT", op.procedureName()); return;
                case Core.MergeOutputProcedurePhraseContext mo: Add("OUTPUT", mo.procedureName()); return;
            }
            for (int i = 0; i < node.ChildCount; i++) Walk(node.GetChild(i));
        }
        Walk(pd);
        return results;
    }

    /// <summary>One USE statement's bound trigger scope: Format 1's files/mode (+GLOBAL), Format 2's report
    /// group, Format 3's (exception-name, file) entries, or Format 4's single operand (ISO §14.9.49).</summary>
    private readonly record struct DeclScope(
        IReadOnlyList<FileModel> Files, int? ModeIndex, bool Global, ReportGroupModel? Report,
        IReadOnlyList<(string Ec, FileModel? File)>? EcEntries = null, BoundEoOperand? Eo = null);

    /// <summary>Bind the USE statement's trigger scope (ISO §14.9.49): Format 1's file list or open mode; the
    /// GLOBAL phrase drives the cross-program GR4b dispatch (the emitter's <c>__RunGlobalUse</c> containment
    /// walk). <c>ON file-name</c> resolves against <c>FilesByName</c>, which includes containers' GLOBAL FDs
    /// (§13.18.27.4 GR1 — merged by <c>CallBindUnit</c>; IC234A's contained USE names the outer's GLOBAL file).
    /// Format 2 (BEFORE REPORTING, SR9) names a report group — the section becomes the group's
    /// before-reporting hook, invoked by the report engine just before the group is produced (GR8; wired in
    /// <c>CSharpEmitter.ReportWriter.cs</c>). The same group shall not appear in two such statements (SR9).</summary>
    private DeclScope? DeclBindUse(Core.UseStatementContext use, string sectionName)
    {
        bool global = use.GLOBAL() is not null;
        if (use.useEcEntry() is { Length: > 0 } ecEntries)
            return DeclBindUseF3(ecEntries, sectionName);
        if (use.OBJECT() is not null || use.EO() is not null)
        {
            // Format 4 (§14.9.49.2 — a brace group requiring exactly one of TWO alternatives,
            // {object-class-name-1 | interface-name-1}; SR15 EO ≡ EXCEPTION OBJECT). GR3: for an OBJECT raise,
            // F4 selection REPLACES the F1/F3 tiers (the generated __EcObjDispatch, D-EO7).
            // use-after-exception-object-2002: the pass owns the edition gate (Exec Step E).
            ctx.EcState.F3 = true;   // the ONE "EC declaratives present" feature bit — F4 rides the same group gate
            string cname = use.cobolWord().GetText();
            // SR16 and SR17 are ONE rule with two operand kinds, and both scope the name to the REPOSITORY
            // paragraph — never to the compilation group, which is what OoClassTable holds. Both halves go
            // through the §8.4.6.4 funnel; before kb/Work PB365 this site called `OoClasses.Find` directly, so a
            // program with NO REPOSITORY compiled clean (SR16 unenforced) and the interface alternative had no
            // resolution path at all and died on SR16's diagnostic (SR17 unimplemented).
            var r = Compiler.Oo.OoNameResolution.Resolve(ctx.Data.OoClasses, ctx.Edition, use, cname,
                Compiler.Oo.OoNameResolution.Want.Either,
                $"declarative section '{sectionName}': USE AFTER EXCEPTION OBJECT",
                DiagnosticCatalog.UseExceptionObjectName.Code, "ISO §14.9.49.3 SR16/SR17");
            if (!r.Ok) return null;
            // The bound operand carries the RESOLVED SYMBOL, never a C# type name: how many emitted types one
            // COBOL name selects is GR14's question and the emitter's answer (kb/Work PB366 — a class is TWO).
            return new DeclScope([], null, global, null, Eo: r.Interface is { } ifc
                ? new BoundEoInterface(ifc)          // SR17 / GR14 b) — the IMPLEMENTS test
                : new BoundEoClass(r.Class!));       // SR16 / GR14 a) — class-or-subclass, factory or instance
        }
        if (use.REPORTING() is not null)
        {
            // Format 2: USE [GLOBAL] BEFORE REPORTING identifier-1 — identifier-1 references a report group
            // (SR9), optionally qualified by its report-name (§8.4.2.2.2 Format 1's file-report-qualifier).
            // Resolution is the ONE funnel's: it collects EVERY candidate and diagnoses an ambiguous reference
            // (§8.4.2.2.1 / §8.4.2.2.3 SR1) where this arm used to `return` inside the loop on the first match
            // and therefore could never observe a second (kb/Work PB365).
            var (head, qualifier) = ReportGroupResolution.Parts(use.reportGroupReference());
            var match = ReportGroupResolution.Resolve(ctx.Edition, ctx.Data.VisibleReports, head, qualifier,
                $"declarative section '{sectionName}': USE BEFORE REPORTING", out _, out var group, ctx.Data);
            if (match == ReportGroupResolution.Match.None || group is null)
            {
                ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE BEFORE REPORTING "
                    + $"'{head}' does not name a report group (ISO §14.9.49.3 SR9)");
                return null;
            }
            // SR9: the same identifier-1 shall not appear in more than one USE BEFORE REPORTING
            // statement within the same procedure division. Format 2 names exactly one group per
            // statement, so only the Duplicate verdict is reachable here.
            if (_useReportGroups.Register(group) == ConstructOperand.Duplicate)
                ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': report group "
                    + $"'{head}' already has a USE BEFORE REPORTING procedure in this procedure "
                    + "division (ISO §14.9.49.3 SR9)");
            return new DeclScope([], null, global, group);
        }
        var target = use.useOnTarget();
        if (target is null) return null;

        // Mode scope (GR3b/GR6b–e) — the index IS the runtime FileOpenMode ordinal (the compiler references
        // the runtime enum; both sides stay aligned by construction).
        int? mode = target.INPUT() is not null ? (int)Runtime.IO.FileOpenMode.Input
            : target.OUTPUT() is not null ? (int)Runtime.IO.FileOpenMode.Output
            : target.EXTEND() is not null ? (int)Runtime.IO.FileOpenMode.Extend
            : target.I_O() is not null ? (int)Runtime.IO.FileOpenMode.IO
            : null;
        if (mode is { } m)
        {
            // SR7: the INPUT, OUTPUT, I-O and EXTEND phrases may each be specified only once in the
            // declaratives portion of a given procedure division. Format 1's brace group takes exactly one
            // alternative per statement, so only the Duplicate verdict is reachable here.
            if (_useModes.Register(m) == ConstructOperand.Duplicate)
                ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': this open mode already "
                    + "has a USE procedure in this procedure division (ISO §14.9.49.3 SR7)");
            return new DeclScope([], m, global, null);
        }

        var files = new List<FileModel>();
        foreach (var fn in target.fileName())
        {
            string fname = fn.GetText();
            if (!ctx.Data.FilesByName.TryGetValue(fname, out var file))
            {
                ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE names unknown "
                    + $"file '{fname}' (ISO §14.9.49)");
                continue;
            }
            if (file.IsSortMerge)
            {
                ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE may not name the "
                    + $"sort/merge file '{fname}' (ISO §14.9.49.3 SR2)");
                continue;
            }
            // SR8: "The same file-name shall not appear in more than one USE AFTER EXCEPTION statement within
            // the same procedure division." ONE statement writing the name twice is not more than one — the
            // Format-1 figure's `{ file-name-1 } …` permits the repetition and GR6 a) associates the one
            // procedure with the file however many times it is written — so the repeat binds ONCE and is
            // silent. It must not bind twice: the emitted `switch (__f)` would carry two identical case
            // labels (DispatchEmitter / EcEmitter EmitUseTiers).
            switch (_useFiles.Register(fname))
            {
                case ConstructOperand.Repeated:
                    continue;
                case ConstructOperand.Duplicate:
                    ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': file '{fname}' "
                        + "already has a USE procedure in this procedure division (ISO §14.9.49.3 SR8)");
                    continue;   // §14.9.49.4 GR3 recovery — the FIRST declarative keeps the file
            }
            files.Add(file);
        }
        return new DeclScope(files, null, global, null);
    }

    /// <summary>Bind a Format-3 USE statement's scope (ISO §14.9.49.2 — <c>USE AFTER {EXCEPTION CONDITION | EC}
    /// {exception-name-1 | exception-name-2 {FILE file-name-2}…}…</c>): validate every exception-name against
    /// the §14.6.13.1 catalog (level 1/2/3 all legal — the §14.9.49.4 GR3c–g tiers select by level), SR13 (a file-scoped
    /// name shall begin EC-I-O), SR14 (the same (exception-name-2, file-name-2) PAIR in more than one USE
    /// statement of this procedure division — a bare exception-name-1 is outside that rule and is bound as
    /// written), and the per-name edition window. The whole format is 2002+ (the EC model's introduction).</summary>
    private DeclScope? DeclBindUseF3(Core.UseEcEntryContext[] entries, string sectionName)
    {
        // use-after-exception-condition-2002: the pass owns the edition gate (Exec Step E).
        ctx.EcState.F3 = true;
        var pairs = new List<(string Ec, FileModel? File)>();
        foreach (var entry in entries)
        {
            string raw = entry.cobolWord().GetText();
            // Resolution + the 0711/0878/1636 diagnostics live in the ONE funnel (kb/Work R05). ⚠ The funnel
            // gates the introduction edition at EVERY level where this site's former copy guarded Level == 3 —
            // a LEVEL-2 name of a 2023-only family (EC-MCS) in a USE entry slipped the old gate at 2002/2014.
            if (!EcNameResolution.TryResolve(ctx.Edition, raw,
                    $"declarative section '{sectionName}'", out var info)) continue;
            var fileNames = entry.fileName();
            if (fileNames.Length > 0 && !Runtime.Exceptions.ExceptionCatalog.IsIoName(info.Name))
            {
                ctx.Edition.Error("COBOLNET0715", $"declarative section '{sectionName}': FILE may be specified "
                    + $"only with an exception-name beginning 'EC-I-O' — '{info.Name}' does not (ISO §14.9.49.3 SR13)");
                continue;
            }
            if (fileNames.Length == 0)
            {
                // A BARE name is exception-name-1, not exception-name-2 (§14.9.49.2's Format-3 figure puts
                // exception-name-2 only in the `exception-name-2 { FILE file-name-2 } …` alternative, and
                // §14.9.49.4 GR3 c)–d) against e)–g) split on the same axis). SR14 governs PAIRS, so nothing
                // screens a repeated exception-name-1 — GR3 gives it its OUTCOME instead: "The first
                // declarative that satisfies the selection criteria is executed and no other declaratives are
                // executed." It therefore never enters the SR14 register.
                pairs.Add((info.Name, null));
                continue;
            }
            foreach (var fn in fileNames)
            {
                string fname = fn.GetText();
                if (!ctx.Data.FilesByName.TryGetValue(fname, out var file))
                {
                    ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE names unknown "
                        + $"file '{fname}' (ISO §14.9.49)");
                    continue;
                }
                if (file.IsSortMerge)
                {
                    ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE may not name "
                        + $"the sort/merge file '{fname}' (ISO §14.9.49.3 SR2)");
                    continue;
                }
                AddPair(info.Name, file);
            }
        }
        return new DeclScope([], null, Global: false, null, pairs);

        void AddPair(string ec, FileModel file)
        {
            // SR14: "The same pair of exception-name-2 and file-name-2 shall not be specified in more than one
            // USE statement within the same procedure division." The pair is the key (a bare exception-name-1
            // never gets here) and the STATEMENT is the boundary: the figure's inner `{ FILE file-name-2 } …`
            // and outer `…` both let ONE statement write a pair twice, which is not more than one statement.
            // The '|' separator is unambiguous — neither part can contain it: every character of a COBOL word
            // "shall be selected from the set of basic letters, basic digits, extended letters, and the basic
            // special characters hyphen and underscore" (§8.3.2.1).
            switch (_useEcPairs.Register(ec + "|" + file.CobolName))
            {
                case ConstructOperand.Repeated:
                    return;   // written twice in this one statement — legal, and bound once
                case ConstructOperand.Duplicate:
                    ctx.Edition.Error("COBOLNET0716", $"declarative section '{sectionName}': the exception-name/"
                        + $"file pair '{ec} FILE {file.CobolName}' is already specified in another USE statement "
                        + "of this procedure division (ISO §14.9.49.3 SR14)");
                    return;   // §14.9.49.4 GR3 recovery — the FIRST declarative keeps the pair
            }
            pairs.Add((ec, file));
        }
    }

    // ⛔ NO HANDLER-END HEURISTIC LIVES HERE (kb/Work PB367 — the shape-derived `DeclHandlerEndPc` /
    // `DeclIsTrivialExit` / `DeclTerminatesRunUnit` trio is DELETED, not disabled). It walked the section's
    // paragraphs backwards and, whenever ANY later paragraph terminated the run unit, ended the bounded USE
    // dispatch at the last trivial EXIT/CONTINUE paragraph before it — so a user declarative written in that
    // ordinary shape (a common exit point, then a paragraph that STOPs the run) executed only in part, on every
    // invocation path (§14.9.49.4 GR7/GR12/GR13 and the Format-2 BEFORE REPORTING hook alike). The standard
    // leaves no room for it: §14.9.49.3 SR1 makes the use procedure the WHOLE remainder of the section, §14.4.2
    // ends the section only at the next section header or END DECLARATIVES, §14.6.3 rule 1 puts the implied
    // transfer back to the controlling USE at "the last statement" of "the last procedure in the range", and
    // §14.9.14.4 GR7's NOTE names USE among the return mechanisms that sit AFTER the section's last paragraph.
}
