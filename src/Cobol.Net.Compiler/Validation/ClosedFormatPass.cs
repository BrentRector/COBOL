// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding;              // EditionContext
using CobolNet.Binding.Passes;       // GroupBindContext
using CobolNet.Editions.Diagnostics; // DiagnosticCatalog
using CobolNet.Frontend.Cst;         // ClosedFormats — the ONE table of closed general formats
using CobolNet.Frontend.Generated;   // CobolParserCore

namespace CobolNet.Validation;

/// <summary>
/// THE ONE PLACE A CLOSED GENERAL FORMAT'S RESIDUE IS REFUSED BY NAME (kb/Work PB829) — a sibling to
/// <see cref="DeclinedFacilityPass"/>, run right after it from <c>BinderDriver</c>.
///
/// <para>WHAT IT CLOSES. <c>genericClause : IDENTIFIER (IDENTIFIER|literal)*</c> matches ANY run of words, so
/// wherever it sat in an alternative list it was a TOTAL SINK for everything the named alternatives missed. The
/// grammar called it a "vendor/extension hook" and reached it from SIX sites spanning EIGHT closed general
/// formats. It is not a vendor hook: a vendor extension is admitted only under the dialect that owns it, never by
/// a catch-all, and this compiler declares no vendor dialect. What it did was swallow — at every edition and
/// every strictness, with no diagnostic anywhere, so §4.2.2's warning obligation ("An implementation shall
/// provide a warning mechanism … to indicate violations of the general formats and the explicit syntax rules of
/// standard COBOL") could not be met for a construct the compiler never represented.</para>
///
/// <para>WHY ONE PASS AND ONE TABLE, NOT EIGHT DIAGNOSTIC SITES. Closing it one site at a time is how it
/// survived: kb/Work PB487 closed §13.16.2 and its sibling sweep, reading the grammar by rule NAME, counted four
/// of the remaining five and missed the I-O-CONTROL paragraph's INLINE alternative, which is not a named
/// <c>xxxClause : genericClause</c> wrapper. The grammar now has ONE error production
/// (<c>unrecognizedClause</c>), this pass is its ONE consumer, and <see cref="ClosedFormats.ByContext"/> is its
/// ONE table; <c>ClosedFormatDriftTests</c> derives the obligation from the .g4 files and from the generated
/// parser, in BOTH directions, so a new closed format cannot parse into silence and a stale row cannot linger.
/// Adding a closed format is `| unrecognizedClause` plus a table row — no code here changes. (The pass owns one
/// more error production of the same posture, <c>misplacedSpecialNamesForPhrase</c> — a known phrase written in a
/// position no format prints; see <see cref="VisitMisplacedSpecialNamesForPhrase"/>.)</para>
///
/// <para>WHY A PARSE-TREE WALK AND NOT A BINDER HOOK — the <see cref="DeclinedFacilityPass"/> argument, and here
/// it is load-bearing rather than convenient. The §13.16.2 refusal used to live in <c>DataBinder.BindEntry</c>,
/// which is NOT reached for every written entry: the level-66 and level-88 paths return before it, so an
/// unrecognized word on <c>88 CN WIBBLE VALUE 1.</c> was dropped in silence by the very fix that closed the
/// format — MEASURED on the pre-change build, where only COBOLNET1747 fired and WIBBLE vanished. Four of the
/// other seven formats have no binder entry point at all — an unrecognized
/// CONFIGURATION SECTION or IDENTIFICATION DIVISION paragraph binds nothing by construction. A walk over the
/// parse tree sees the written syntax whether or not anything binds it.</para>
///
/// <para>SEVERITY. Error at every edition and every strictness, through <see cref="EditionContext.Error"/> and
/// not the declined/removed seam: this is not an optional element this implementation declines (§4.2.7) nor a
/// construct of another edition (§8.9) — it is source no edition of the standard admits, and there is no
/// permissive reading of it that is not "compile something the programmer did not write".</para>
/// </summary>
internal sealed class ClosedFormatPass(EditionContext edition) : CursorFollowingVisitor(edition)
{
    private readonly EditionContext _edition = edition;

    public static void Run(GroupBindContext group, EditionContext edition)
        => new ClosedFormatPass(edition).VisitPositioned(group.Tree);

    /// <summary>Do not descend into a PROCEDURE DIVISION: <c>unrecognizedClause</c> is reachable only from the
    /// identification division, the environment division and the data division (every row of
    /// <see cref="ClosedFormats.ByContext"/> is in one of those three), so the largest part of a real compilation
    /// unit carries none. ⚠ The same verification <see cref="DeclinedFacilityPass"/> records applies and is what
    /// makes the skip safe rather than a silent hole: a nested program's divisions are SIBLINGS of
    /// <c>procedureDivision</c> in <c>programUnit</c>, a method's <c>dataDivision?</c> is a sibling of its
    /// <c>procedureDivision?</c> inside <c>methodDefinition</c>, and the FACTORY / OBJECT paragraphs spell their
    /// method list with literal tokens rather than the <c>procedureDivision</c> rule.</summary>
    public override object? VisitProcedureDivision(CobolParserCore.ProcedureDivisionContext ctx) => null;

    /// <summary>⛔ THE OTHER HALF OF A CLOSED FORMAT — an element the figure prints ONCE, written twice (kb/Work
    /// PB917). Every rule node's clause children are screened against <see cref="ClauseCardinalities"/>: the
    /// parse-tree walk reaches every written entry (level-88 and level-66 entries, which return from
    /// <c>BindEntry</c> early, included), and a new clause list is one table row.</summary>
    public override object? VisitChildren(Antlr4.Runtime.Tree.IRuleNode node)
    {
        if (node is ParserRuleContext container) ScreenClauseRepetition(container);
        return base.VisitChildren(node);
    }

    /// <summary>Count the elements <paramref name="container"/>'s clause children fill and diagnose each that is
    /// written more than once (ISO §5.2.6.2, §5.2.7), through the ONE <see cref="UnrepeatedElements"/> reader the
    /// RD entry already uses. Reported at the SECOND occurrence — the one that makes the entry non-conforming.</summary>
    private void ScreenClauseRepetition(ParserRuleContext container)
    {
        Dictionary<(string Clause, object Element), (ClauseList List, int Count, ParserRuleContext First, ParserRuleContext Second)>? seen = null;
        for (int i = 0; i < container.ChildCount; i++)
        {
            if (container.GetChild(i) is not ParserRuleContext clause
                || !ClauseCardinalities.ByClauseContext.TryGetValue(clause.GetType(), out var list)
                || ClauseCardinalities.ElementOf(list, clause) is not { } element) continue;
            seen ??= [];
            var key = (list.Clause, element);
            seen[key] = seen.TryGetValue(key, out var prior)
                ? (list, prior.Count + 1, prior.First, prior.Count == 1 ? clause : prior.Second)
                : (list, 1, clause, clause);
        }
        if (seen is null) return;
        foreach (var (list, count, first, second) in seen.Values)
        {
            if (count < 2) continue;
            using var at = _edition.At(second);
            string owner = OwnerName(container) is { Length: > 0 } n ? $" '{n}'" : "";
            UnrepeatedElements.AtMostOnce(_edition, count, $"{list.Subject}{owner}",
                $"the clause '{WrittenOpening(first)}'", list.Clause);
        }
    }

    /// <summary>The first words of a clause as the programmer wrote them (at most 32 characters), to name it in the message.</summary>
    private static string WrittenOpening(ParserRuleContext clause)
    {
        string text = clause.Start?.InputStream is { } chars && clause.Stop is { } stop
            ? chars.GetText(new Antlr4.Runtime.Misc.Interval(clause.Start.StartIndex, stop.StopIndex))
            : clause.GetText();
        text = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length <= 32 ? text : text[..32] + "…";
    }

    /// <summary>THE refusal. The format is identified by the PARENT context — the alternative list the error
    /// production was written into — so one visitor serves every closed format and a new one needs no arm.</summary>
    public override object? VisitUnrecognizedClause(CobolParserCore.UnrecognizedClauseContext ctx)
    {
        var format = ClosedFormats.Of(ctx.Parent?.GetType());
        // The drift test makes a missing row impossible; refuse anyway rather than return silently, because a
        // silent return here is precisely the defect this pass exists to end.
        string word = ctx.genericClause()?.IDENTIFIER(0)?.GetText() ?? ctx.GetText();
        string owner = OwnerName(ctx) is { Length: > 0 } n ? $" for '{n}'" : "";
        if (format is null)
        {
            _edition.Error(DiagnosticCatalog.ClosedFormatUnrecognizedClause,
                $"'{word}' is not a clause of the general format it is written in — and that format has no row "
                + "in ClosedFormats.ByContext, which ClosedFormatDriftTests exists to prevent");
            return null;
        }
        _edition.Error(format.Code == "COBOLNET1941" ? DiagnosticCatalog.DataClauseUnrecognized
                     : format.Noun == "paragraph" ? DiagnosticCatalog.ClosedFormatUnrecognizedParagraph
                     : DiagnosticCatalog.ClosedFormatUnrecognizedClause,
            $"'{word}' is not a {format.Noun} of the {format.Subject}{owner} — the ISO §{format.Clause} "
            + $"{format.FormatLabel}general format lists the {format.Noun}s that may be specified, and it is a "
            + "closed list");
        return null;   // nothing below an error production is diagnosed — one diagnostic per word run
    }

    /// <summary>A SPECIAL-NAMES FOR phrase written AFTER its clause's definition (kb/Work PB977) — the second error
    /// production this pass owns, and the same posture: source no edition admits, refused at every edition and
    /// every strictness. ISO §12.3.7.2 prints <c>FOR {ALPHANUMERIC | NATIONAL}</c> only immediately after the name
    /// the ALPHABET / CLASS clause declares, and before the first symbolic-character-1 of SYMBOLIC CHARACTERS; the
    /// trailing spelling is owned by no dialect (CLAUDE.md rule 1's precedence — GnuCOBOL takes the phrase only
    /// before IS). ONE visitor for all three clauses: the ALPHABET one used to be ACCEPTED as a "historical
    /// superset" and the CLASS one drew a bare parse error — one spelling, two answers.</summary>
    public override object? VisitMisplacedSpecialNamesForPhrase(CobolParserCore.MisplacedSpecialNamesForPhraseContext ctx)
    {
        string clause = ctx.Parent switch
        {
            CobolParserCore.AlphabetClauseContext a => $"ALPHABET {a.cobolWord().GetText()}",
            CobolParserCore.ClassDefinitionClauseContext c => $"CLASS {c.cobolWord(0).GetText()}",
            _ => "SYMBOLIC CHARACTERS",
        };
        string where = ctx.Parent is CobolParserCore.SymbolicCharactersClauseContext
            ? "before the first symbolic-character-1" : "between the name and IS";
        var phrase = ctx.specialNamesForPhrase();
        string words = string.Join(" ", Enumerable.Range(0, phrase.ChildCount).Select(i => phrase.GetChild(i).GetText()));
        _edition.Error(DiagnosticCatalog.SpecialNamesForPhraseMisplaced, $"{clause}: the phrase '{words}' "
            + $"follows the clause's definition — the FOR phrase belongs {where} (ISO §12.3.7.2 general format)");
        return null;
    }

    /// <summary>The name of the entry the offending word run sits in, for a message that points at the user's own
    /// declaration rather than at a line number alone. Four of the formats have such a name (the data
    /// description entry's data-name, the FD's and SD's file-name, the file control entry's SELECT file-name);
    /// the rest — I-O-CONTROL, SPECIAL-NAMES, SOURCE-/OBJECT-COMPUTER, the configuration section and the identification division —
    /// are unnamed paragraphs and correctly yield null, because no ancestor of theirs matches an arm below.
    /// <para>⚠ NEAREST ancestor wins, which is the point: a <c>dataDescriptionEntry</c> nested under an
    /// <c>fileDescriptionEntry</c> must report ITS data-name, not the file's.</para></summary>
    private static string? OwnerName(ParserRuleContext ctx)
    {
        for (RuleContext? p = ctx; p is not null; p = p.Parent)   // the nearest ancestor-or-self: a clause container asks for its own entry
            switch (p)
            {
                case CobolParserCore.ReportGroupEntryContext r: return r.dataName()?.GetText();
                case CobolParserCore.DataDescriptionEntryContext d: return d.dataName()?.GetText();
                case CobolParserCore.FileDescriptionEntryContext f: return f.fileName()?.GetText();
                case CobolParserCore.SortMergeDescriptionEntryContext s: return s.fileName()?.GetText();
                case CobolParserCore.FileControlClauseGroupContext g: return g.fileName()?.GetText();
                case CobolParserCore.EnvironmentDivisionContext: return null;   // bound: no named ancestor above
                case CobolParserCore.IdentificationDivisionContext: return null;
                case CobolParserCore.DataDivisionContext: return null;
            }
        return null;
    }
}
