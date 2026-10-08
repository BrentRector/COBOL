// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using Antlr4.Runtime;

using Core = CobolNet.Frontend.Generated.CobolParserCore;

namespace CobolNet.Frontend.Cst;

/// <summary>
/// One clause list whose general format prints every element in its OWN bracket with no ellipsis — and so permits
/// each element at most once (ISO §5.2.6.2: a bracket allows the element "or that portion of the general format may
/// be omitted"; §5.2.7: only an ellipsis licenses repetition) — keyed in <see cref="ClauseCardinalities.ByClauseContext"/>
/// by the parse-tree context of the alternative list the clauses are written into.
/// </summary>
/// <param name="Clause">The general format's own subclause ("13.16.2").</param>
/// <param name="FormatLabel">"Format 1 " where the subclause prints several formats; the empty string otherwise.</param>
/// <param name="Subject">The construct as the standard names it ("data description entry").</param>
/// <param name="Repeatable">The alternatives that ARE allowed to repeat: an ellipsis follows them in the figure
/// (the file control entry's ALTERNATE RECORD KEY and collating-sequence clauses, §12.4.5.1), or the alternative
/// is not a clause at all (an error production, or a meta-language term such as §13.16.2's validation-clauses that
/// stands for several clauses some of which carry an ellipsis). Every OTHER alternative of the list is once-only.</param>
/// <param name="Sequence">⛔ THE ORDER DECISION, which every row makes explicitly (kb/Work PB1508). ISO §5.2.1: "The
/// words, phrases, clauses, punctuation, and operands in each general format shall be written in the compilation
/// group in the sequence given in the general format, unless otherwise specified by the rules of that format." Null
/// when a rule of the format DOES specify otherwise — the row's comment names that syntax rule (§13.16.3 SR4 for the
/// data description entry, §13.4.5.3 SR2 for the FD, …). Otherwise the once-only elements in the sequence the figure
/// prints them, and <c>ClosedFormatPass</c> refuses an element written after one the figure prints later
/// (COBOLNET2987): the OPTIONS paragraph (§11.9.2), the CONFIGURATION SECTION (§12.3.2) and the identification division
/// (§11.2.1) print a fixed sequence of brackets, and none has a rule that frees it.
/// <c>ClauseCardinalityDriftTests</c> requires a sequence to hold exactly the list's once-only alternatives, so a
/// clause added to such a list cannot go unranked.</param>
/// <param name="KindOf">The element an alternative instance fills, when the grammar folds two figure elements into
/// one alternative (the FD's <c>IS EXTERNAL</c> and <c>IS GLOBAL</c> slots are one <c>fileGlobalExternalClause</c>);
/// null means "the alternative itself".</param>
public sealed record ClauseList(string Clause, string FormatLabel, string Subject, IReadOnlySet<Type> Repeatable,
    IReadOnlyList<Type>? Sequence, Func<ParserRuleContext, object>? KindOf = null)
{
    /// <summary>The position of <paramref name="element"/> in <see cref="Sequence"/>, or null when the list's
    /// order is free or the element is not ranked.</summary>
    public int? RankOf(object element)
    {
        if (Sequence is null || element is not Type type) return null;
        for (int i = 0; i < Sequence.Count; i++)
            if (Sequence[i] == type) return i;
        return null;
    }
}

/// <summary>
/// ⛔ THE ONE TABLE OF "EACH ELEMENT AT MOST ONCE" CLAUSE LISTS (kb/Work PB917) — the <see cref="ClosedFormats"/> twin
/// for the other half of what a closed general format says. <see cref="ClosedFormats"/> refuses a WORD the format
/// does not print; this refuses a clause the format prints ONCE but the source writes twice
/// (<c>01 N USAGE DISPLAY USAGE BINARY PIC 9(3).</c> compiled at every edition and the last USAGE silently won).
///
/// <para><b>Why a table and not a count in each binder.</b> The grammar can say "any order" with one <c>*</c> and
/// "each once" only by enumerating every ordering, so the grammar writes the <c>*</c> and the "each once" half is
/// read in ONE place (<c>ClosedFormatPass</c>) from this table. A count written in <c>BindEntry</c> as
/// <c>extraUsages.Count &gt; 0</c> covers USAGE and nothing else; the table covers every alternative of the list
/// and the NEXT alternative automatically, because <c>ClauseCardinalityDriftTests</c> requires every alternative
/// of every listed context to be classified once-only or repeatable — the decision cannot be skipped.</para>
///
/// <para>Rows are the lists whose printed formats were read for ellipses: the §13.16.2 data description entry,
/// the §13.15.2 report group description entry, the §13.4.5.2 file description entry, the §13.4.6.2 sort-merge
/// file description entry and the §12.4.5.1 file control entry — each free in ORDER by a syntax rule, or (the SD)
/// printing one element — and the
/// §11.9.2 OPTIONS paragraph, the §12.3.2 CONFIGURATION SECTION and the §11.2.1 identification division, whose figures
/// fix the order too (<see cref="ClauseList.Sequence"/>, kb/Work PB1508). (The RD entry and its PAGE clause count
/// their own elements through <c>UnrepeatedElements</c>.) SPECIAL-NAMES, I-O-CONTROL and OBJECT-COMPUTER lists are
/// not rows: their formats repeat most elements.</para>
/// </summary>
public static class ClauseCardinalities
{
    private static readonly IReadOnlySet<Type> None = new HashSet<Type>();

    public static readonly FrozenDictionary<Type, ClauseList> ByClauseContext =
        new Dictionary<Type, ClauseList>
        {
            // §13.16.2 Format 1 prints twenty-one separate brackets, none with an ellipsis. The validation-clauses
            // meta-term (§13.16.2's own sub-figure) holds `{ INVALID WHEN condition-2 } …` and friends, and the
            // facility is declined (Annex A.4.14) — recognized and refused by name, so it is not counted. ORDER: free,
            // §13.16.3 SR4 ("The remaining clauses may be written in any order").
            [typeof(Core.DataDescriptionClauseContext)] =
                new("13.16.2", "Format 1 ", "data description entry",
                    new HashSet<Type> { typeof(Core.ValidationClauseContext), typeof(Core.UnrecognizedClauseContext) },
                    Sequence: null),

            // §13.15.2: sixteen brackets and one braced choice {source | sum | value}, no ellipsis. ORDER: free,
            // §13.15.3 SR2 ("All other clauses may be written in any order").
            [typeof(Core.ReportGroupClauseContext)] =
                new("13.15.2", "", "report group description entry", None, Sequence: null),

            // §13.4.5.2 Formats 1-3: every element in its own bracket (`IS EXTERNAL` and `IS GLOBAL` are two). ORDER:
            // free, §13.4.5.3 SR2 ("The clauses that follow file-name-1 may appear in any order").
            [typeof(Core.FileDescriptionClauseContext)] =
                new("13.4.5.2", "", "file description entry",
                    new HashSet<Type> { typeof(Core.UnrecognizedClauseContext) },
                    Sequence: null, KindOfFileDescriptionClause),

            // §13.4.6.2: `SD file-name-1 [ record-clause ] .` (and the '85 DATA RECORDS clause, removal-gated at
            // 2002). ORDER: nothing to order — the figure prints one element.
            [typeof(Core.SortMergeDescriptionClauseContext)] =
                new("13.4.6.2", "", "sort-merge file description entry",
                    new HashSet<Type> { typeof(Core.UnrecognizedClauseContext) }, Sequence: null),

            // §12.4.5.1: only `ALTERNATE RECORD KEY … …` and `[ collating-sequence-clause ] …` carry an ellipsis.
            // ORDER: free after SELECT, §12.4.5.2 SR1 ("The clauses that follow the SELECT clause may appear in any
            // order"; the grammar puts SELECT first).
            [typeof(Core.FileControlClausesContext)] =
                new("12.4.5.1", "", "file control entry",
                    new HashSet<Type>
                    {
                        typeof(Core.AlternateKeyClauseContext), typeof(Core.FileCollatingSequenceClauseContext),
                        typeof(Core.UnrecognizedClauseContext),
                    },
                    Sequence: null),

            // §11.9.2 (RENDERED, PDF p302 / folio 272): `OPTIONS.` then seven separate brackets and an independent
            // `[.]` — no ellipsis and no choice indicators — and §11.9.3 has one syntax rule (the period), so no rule
            // frees the ORDER: §5.2.1 binds the printed sequence (kb/Work PB1508). The grammar's order-free
            // `optionsClause+` is the superset this row narrows to the figure.
            [typeof(Core.OptionsClauseContext)] =
                new("11.9.2", "", "OPTIONS paragraph", None,
                    Sequence:
                    [
                        typeof(Core.ArithmeticClauseContext), typeof(Core.DefaultRoundedClauseContext),
                        typeof(Core.EntryConventionClauseContext), typeof(Core.FloatBinaryClauseContext),
                        typeof(Core.FloatDecimalClauseContext), typeof(Core.OptionsInitializeClauseContext),
                        typeof(Core.IntermediateRoundingClauseContext),
                    ]),

            // §12.3.2 (RENDERED, PDF p313 / folio 283): four bracketed paragraphs in a fixed sequence, and §12.3.3's
            // three syntax rules place the section, never reorder it — so §5.2.1 binds the sequence and each
            // paragraph is written at most once (kb/Work PB1508's sibling arm: `SPECIAL-NAMES.` before
            // `SOURCE-COMPUTER.`, or two SPECIAL-NAMES paragraphs, compiled clean).
            [typeof(Core.ConfigurationParagraphContext)] =
                new("12.3.2", "", "CONFIGURATION SECTION",
                    new HashSet<Type> { typeof(Core.UnrecognizedClauseContext) },
                    Sequence:
                    [
                        typeof(Core.SourceComputerParagraphContext), typeof(Core.ObjectComputerParagraphContext),
                        typeof(Core.SpecialNamesParagraphContext), typeof(Core.RepositoryParagraphContext),
                    ]),

            // §11.2.1: the identification division prints `[ options-paragraph ]` ONCE after the PROGRAM-ID (or other
            // identifying) paragraph, which the grammar already places first — so a second OPTIONS paragraph, which
            // OptionsBinder never read (the first one won in silence), is a repeat (kb/Work PB1508's sibling arm).
            // The figure fixes its sequence and ranks one element here. The AUTHOR … SECURITY and REMARKS
            // comment-entry paragraphs are not elements of this figure at all: they are the COBOL-85 / COBOL-74 lists
            // removed at 2002 and refused by the removal gate, so they are not counted against a figure that does not
            // print them.
            [typeof(Core.IdentificationParagraphContext)] =
                new("11.2.1", "", "identification division",
                    new HashSet<Type>
                    {
                        typeof(Core.AuthorParagraphContext), typeof(Core.InstallationParagraphContext),
                        typeof(Core.DateWrittenParagraphContext), typeof(Core.DateCompiledParagraphContext),
                        typeof(Core.SecurityParagraphContext), typeof(Core.RemarksParagraphContext),
                        typeof(Core.UnrecognizedClauseContext),
                    },
                    Sequence: [typeof(Core.OptionsParagraphContext)]),
        }.ToFrozenDictionary();

    /// <summary>The figure element a written clause fills: its alternative's context type, refined by
    /// <see cref="ClauseList.KindOf"/> where one alternative serves two elements.</summary>
    public static object? ElementOf(ClauseList list, ParserRuleContext clause)
    {
        if (clause.ChildCount != 1 || clause.GetChild(0) is not ParserRuleContext alternative) return null;
        Type type = alternative.GetType();
        if (list.Repeatable.Contains(type)) return null;
        return list.KindOf?.Invoke(alternative) ?? type;
    }

    /// <summary>The FD's <c>fileGlobalExternalClause</c> is the figure's <c>[ IS EXTERNAL [ AS literal-1 ] ]</c> or its
    /// <c>[ IS GLOBAL ]</c> — two brackets, so GLOBAL and EXTERNAL are different elements.</summary>
    private static object KindOfFileDescriptionClause(ParserRuleContext alternative) =>
        alternative is Core.FileGlobalExternalClauseContext g ? (g.GLOBAL() is not null ? "GLOBAL" : "EXTERNAL")
                                                              : alternative.GetType();
}
