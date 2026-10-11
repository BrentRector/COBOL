// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;

using Core = CobolNet.Frontend.Generated.CobolParserCore;

namespace CobolNet.Frontend.Cst;

/// <summary>
/// One paragraph whose general format prints a SINGLE separator period after its entries, while the grammar
/// superset-parses an optional period after every entry so the misplaced one can be refused by name.
/// </summary>
/// <param name="Clause">The general format's subclause in ISO/IEC 1989:2023.</param>
/// <param name="Subject">The paragraph as the standard names it ("SPECIAL-NAMES").</param>
/// <param name="Printed">What the format prints, completing "the general format prints …" in the diagnostic.</param>
/// <param name="OwnSentence">The one clause the format prints as a SENTENCE OF ITS OWN, closed by its own period
/// (§12.4.6.2's APPLY COMMIT clause), or null where the paragraph is a single sentence.</param>
public sealed record ParagraphPeriod(string Clause, string Subject, string Printed, Type? OwnSentence);

/// <summary>One sentence of a paragraph: its entries and the separator period that closes it (null when the
/// paragraph ends without one).</summary>
public sealed record ParagraphSentence(IReadOnlyList<ParserRuleContext> Entries, ITerminalNode? Period);

/// <summary>
/// ⛔ THE ONE TABLE OF PARAGRAPHS WHOSE SEPARATOR PERIODS ARE SUPERSET-PARSED (kb/Work PB2727), keyed by the
/// paragraph's parse-tree context.
///
/// <para>WHY. ISO §12.4.6.2 (I-O-CONTROL), §12.3.7.2 (SPECIAL-NAMES) and §12.3.8.2 (REPOSITORY) each print ONE
/// closing separator period after the paragraph's entries (I-O-CONTROL a second one around its APPLY COMMIT
/// clause), and §5.2.1 binds the punctuation a format prints. The three grammar rules wrote <c>(entry DOT?)*</c>
/// and nothing read the periods, so a period after EVERY entry, or none after the last, compiled clean at every
/// edition — one shape in three rules. The grammar keeps the punctuation superset so the refusal can NAME the rule
/// (the design's §3.10 posture), and <c>ClosedFormatPass</c> reads every row through <see cref="Sentences"/>.
/// <c>ParagraphPeriodDriftTests</c> holds every grammar rule that writes <c>DOT?</c> to a row here, so a fourth
/// paragraph with the shape cannot parse into silence.</para>
/// </summary>
public static class ParagraphPeriods
{
    /// <summary>Every paragraph rule that writes an optional separator period, by the context type ANTLR generates
    /// for it; enumerable so the drift test can compare it with the grammar in both directions.</summary>
    public static readonly FrozenDictionary<Type, ParagraphPeriod> ByContext =
        new Dictionary<Type, ParagraphPeriod>
        {
            // §12.4.6.2, rendered (PDF p363 / folio 333): `I-O-CONTROL. [[apply-commit-clause].] [[{same-clause}…].]`.
            [typeof(Core.IoControlParagraphContext)] = new("12.4.6.2", "I-O-CONTROL",
                "one separator period after the run of SAME clauses, and one after the APPLY COMMIT clause, which is "
                + "a sentence of its own", typeof(Core.ApplyCommitClauseContext)),
            // §12.3.7.2: `SPECIAL-NAMES. [clauses] … .` — §12.3.7.3 SR31 lets one period go only when no clause is written.
            [typeof(Core.SpecialNamesParagraphContext)] = new("12.3.7.2", "SPECIAL-NAMES",
                "one separator period, after the last clause", null),
            // §12.3.8.2: `REPOSITORY. [ { specifier } … . ]` — the period printed inside the bracket, once.
            [typeof(Core.RepositoryParagraphContext)] = new("12.3.8.2", "REPOSITORY",
                "one separator period, after the last specifier", null),
        }.ToFrozenDictionary();

    /// <summary>The paragraph's sentences in source order: the entries between its header's period and each later
    /// separator period. The header is the paragraph's first two children (the paragraph name and its period); a
    /// sentence with no entries — SPECIAL-NAMES' full empty spelling <c>SPECIAL-NAMES. .</c>, §12.3.7.3 SR31 — is
    /// skipped, because it is the one spelling the format prints with a period and no clause.</summary>
    public static IReadOnlyList<ParagraphSentence> Sentences(ParserRuleContext paragraph)
    {
        var sentences = new List<ParagraphSentence>();
        var entries = new List<ParserRuleContext>();
        for (int i = 2; i < paragraph.ChildCount; i++)
        {
            switch (paragraph.GetChild(i))
            {
                case ParserRuleContext entry:
                    entries.Add(entry);
                    break;
                case ITerminalNode period when period.Symbol.Type == CobolLexer.DOT:
                    if (entries.Count > 0) sentences.Add(new ParagraphSentence(entries, period));
                    entries = [];
                    break;
            }
        }
        if (entries.Count > 0) sentences.Add(new ParagraphSentence(entries, null));
        return sentences;
    }

    /// <summary>Whether <paramref name="sentence"/> is the clause <paramref name="row"/> prints as a sentence of its
    /// own: exactly one entry, whose single child is that clause.</summary>
    public static bool IsOwnSentence(ParagraphPeriod row, ParagraphSentence sentence)
        => row.OwnSentence is { } own
           && sentence.Entries is [{ ChildCount: 1 } entry]
           && entry.GetChild(0).GetType() == own;
}
