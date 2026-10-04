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
/// <param name="KindOf">The element an alternative instance fills, when the grammar folds two figure elements into
/// one alternative (the FD's <c>IS EXTERNAL</c> and <c>IS GLOBAL</c> slots are one <c>fileGlobalExternalClause</c>);
/// null means "the alternative itself".</param>
public sealed record ClauseList(string Clause, string FormatLabel, string Subject, IReadOnlySet<Type> Repeatable,
    Func<ParserRuleContext, object>? KindOf = null);

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
/// file description entry and the §12.4.5.1 file control entry. (The RD entry and its PAGE clause count their own
/// elements through <c>UnrepeatedElements</c>.) SPECIAL-NAMES, I-O-CONTROL and OBJECT-COMPUTER lists are not rows:
/// their formats repeat most elements.</para>
/// </summary>
public static class ClauseCardinalities
{
    private static readonly IReadOnlySet<Type> None = new HashSet<Type>();

    public static readonly FrozenDictionary<Type, ClauseList> ByClauseContext =
        new Dictionary<Type, ClauseList>
        {
            // §13.16.2 Format 1 prints twenty-one separate brackets, none with an ellipsis. The validation-clauses
            // meta-term (§13.16.2's own sub-figure) holds `{ INVALID WHEN condition-2 } …` and friends, and the
            // facility is declined (Annex A.4.14) — recognized and refused by name, so it is not counted.
            [typeof(Core.DataDescriptionClauseContext)] =
                new("13.16.2", "Format 1 ", "data description entry",
                    new HashSet<Type> { typeof(Core.ValidationClauseContext), typeof(Core.UnrecognizedClauseContext) }),

            // §13.15.2: sixteen brackets and one braced choice {source | sum | value}, no ellipsis.
            [typeof(Core.ReportGroupClauseContext)] =
                new("13.15.2", "", "report group description entry", None),

            // §13.4.5.2 Formats 1-3: every element in its own bracket (`IS EXTERNAL` and `IS GLOBAL` are two).
            [typeof(Core.FileDescriptionClauseContext)] =
                new("13.4.5.2", "", "file description entry",
                    new HashSet<Type> { typeof(Core.UnrecognizedClauseContext) },
                    KindOfFileDescriptionClause),

            // §13.4.6.2: `SD file-name-1 [ record-clause ] .` (and the '85 DATA RECORDS clause).
            [typeof(Core.SortMergeDescriptionClauseContext)] =
                new("13.4.6.2", "", "sort-merge file description entry",
                    new HashSet<Type> { typeof(Core.UnrecognizedClauseContext) }),

            // §12.4.5.1: only `ALTERNATE RECORD KEY … …` and `[ collating-sequence-clause ] …` carry an ellipsis.
            [typeof(Core.FileControlClausesContext)] =
                new("12.4.5.1", "", "file control entry",
                    new HashSet<Type>
                    {
                        typeof(Core.AlternateKeyClauseContext), typeof(Core.FileCollatingSequenceClauseContext),
                        typeof(Core.UnrecognizedClauseContext),
                    }),
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
