// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using Antlr4.Runtime;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// ⛔ EVERY CLOSED CLAUSE LIST DECIDES "EACH ELEMENT AT MOST ONCE" EXPLICITLY (kb/Work PB917; ISO §5.2.6.2, §5.2.7).
/// A closed general format that prints each clause in its own bracket with no ellipsis allows each clause once; the
/// grammar writes the list as <c>clause*</c> (the order licence) and <c>ClosedFormatPass</c> counts the elements from
/// <see cref="ClauseCardinalities.ByClauseContext"/>. Two obligations keep that table total, both read from the
/// GENERATED parser rather than from a list somebody remembered:
/// <list type="bullet">
/// <item>every closed format of <see cref="ClosedFormats.ByContext"/> is either a clause-cardinality row or on the
/// reviewed exemption list below (a format whose elements mostly repeat, or a paragraph list) — a NEW closed format
/// cannot arrive without someone reading its printed figure for ellipses;</item>
/// <item>every <c>Repeatable</c> alternative a row names is still an alternative of its list — a dead entry would
/// silently stop exempting nothing and, worse, hide that the figure's ellipsis moved.</item>
/// </list></summary>
public sealed class ClauseCardinalityDriftTests : CobolNetTestBase
{
    /// <summary>The closed formats whose figure was read and does NOT print every element once: SPECIAL-NAMES,
    /// I-O-CONTROL and OBJECT-COMPUTER repeat most of their elements; SOURCE-COMPUTER holds a single element. (The
    /// configuration section and the identification division list paragraphs, each bracketed once, and are rows —
    /// kb/Work PB1508.)</summary>
    private static readonly IReadOnlySet<Type> ReviewedExemptions = new HashSet<Type>
    {
        typeof(Core.SpecialNameEntryContext),
        typeof(Core.IoControlClauseContext),
        typeof(Core.SourceComputerParagraphContext),
        typeof(Core.ObjectComputerClauseContext),
    };

    private static IReadOnlyList<Type> AlternativesOf(Type listContext) =>
        listContext
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length == 0
                        && typeof(ParserRuleContext).IsAssignableFrom(m.ReturnType)
                        && m.ReturnType != listContext)
            .Select(m => m.ReturnType)
            .Distinct()
            .ToList();

    [Fact]
    public void EveryClosedFormat_IsARowOrAReviewedExemption()
    {
        Assert.NotEmpty(ClosedFormats.ByContext);   // a probe that finds nothing is not a green result
        var undecided = ClosedFormats.ByContext.Keys
            .Where(t => !ClauseCardinalities.ByClauseContext.ContainsKey(t) && !ReviewedExemptions.Contains(t))
            .Select(t => t.Name).ToList();
        Assert.True(undecided.Count == 0,
            "a closed general format has no ClauseCardinalities row and is not a reviewed exemption: "
            + string.Join(", ", undecided) + ". Read its printed figure (scripts/render-spec-page.py): when every "
            + "element is in its own bracket with no ellipsis, add a row; when elements repeat, add it to "
            + "ReviewedExemptions with the reason.");
        var stale = ReviewedExemptions.Where(t => !ClosedFormats.ByContext.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.True(stale.Count == 0, "ReviewedExemptions names a context that is no longer a closed format: " + string.Join(", ", stale));
        var both = ReviewedExemptions.Where(ClauseCardinalities.ByClauseContext.ContainsKey).Select(t => t.Name).ToList();
        Assert.True(both.Count == 0, "a context is both a ClauseCardinalities row and an exemption: " + string.Join(", ", both));
    }

    [Fact]
    public void EveryRepeatableAlternative_IsStillAnAlternativeOfItsList()
    {
        foreach (var (list, row) in ClauseCardinalities.ByClauseContext)
        {
            var alternatives = AlternativesOf(list).ToHashSet();
            Assert.NotEmpty(alternatives);
            var dead = row.Repeatable.Where(t => !alternatives.Contains(t)).Select(t => t.Name).ToList();
            Assert.True(dead.Count == 0,
                $"{list.Name}'s Repeatable set names {string.Join(", ", dead)}, which the grammar no longer offers there");
        }
    }

    /// <summary>⛔ A FIXED SEQUENCE RANKS EXACTLY THE LIST'S ONCE-ONLY ALTERNATIVES (kb/Work PB1508; ISO §5.2.1). A row
    /// whose figure binds the order (<see cref="ClauseList.Sequence"/>) is read by <c>ClosedFormatPass</c> as a rank per
    /// element; an alternative the sequence does not name would be written anywhere unchecked, and one it names that
    /// the grammar no longer offers is a dead rank. Both are read from the GENERATED parser, so a clause added to the
    /// OPTIONS paragraph or the CONFIGURATION SECTION cannot arrive unranked.</summary>
    [Fact]
    public void EveryFixedSequence_RanksExactlyTheOnceOnlyAlternatives()
    {
        var ordered = ClauseCardinalities.ByClauseContext.Where(r => r.Value.Sequence is not null).ToList();
        Assert.True(ordered.Count >= 3,
            "the OPTIONS paragraph, CONFIGURATION SECTION and identification division rows must carry a Sequence");
        foreach (var (list, row) in ordered)
        {
            var onceOnly = AlternativesOf(list).Where(t => !row.Repeatable.Contains(t)).ToHashSet();
            var ranked = row.Sequence!.ToHashSet();
            Assert.True(ranked.Count == row.Sequence!.Count, $"{list.Name}'s Sequence names an element twice");
            var unranked = onceOnly.Except(ranked).Select(t => t.Name).ToList();
            Assert.True(unranked.Count == 0, $"{list.Name}'s Sequence does not rank {string.Join(", ", unranked)}: read "
                + "the printed figure (scripts/render-spec-page.py) and place it where the figure prints it");
            var dead = ranked.Except(onceOnly).Select(t => t.Name).ToList();
            Assert.True(dead.Count == 0, $"{list.Name}'s Sequence ranks {string.Join(", ", dead)}, which is not a "
                + "once-only alternative of the list");
        }
    }

    /// <summary>The OPTIONS paragraph's clause list (§11.9.2) has no error production, so the closed-format obligation
    /// cannot see it: pin that it is a row, that nothing in it repeats, and that its sequence is the figure's.</summary>
    [Fact]
    public void TheOptionsClauseRow_RanksTheSevenClausesInTheFiguresSequence()
    {
        Assert.True(ClauseCardinalities.ByClauseContext.TryGetValue(typeof(Core.OptionsClauseContext), out var row));
        Assert.Empty(row!.Repeatable);
        Assert.Equal(
            [
                typeof(Core.ArithmeticClauseContext), typeof(Core.DefaultRoundedClauseContext),
                typeof(Core.EntryConventionClauseContext), typeof(Core.FloatBinaryClauseContext),
                typeof(Core.FloatDecimalClauseContext), typeof(Core.OptionsInitializeClauseContext),
                typeof(Core.IntermediateRoundingClauseContext),
            ],
            row.Sequence!);
    }

    /// <summary>The report group description entry is the one row that is NOT a closed format with an error
    /// production, so the first obligation cannot see it: pin that its list is the table's key and its alternatives
    /// are the ones §13.15.2 prints.</summary>
    [Fact]
    public void TheReportGroupClauseRow_ExistsAndHasNoRepeatableAlternative()
    {
        Assert.True(ClauseCardinalities.ByClauseContext.TryGetValue(typeof(Core.ReportGroupClauseContext), out var row));
        Assert.Empty(row!.Repeatable);
        Assert.NotEmpty(AlternativesOf(typeof(Core.ReportGroupClauseContext)));
    }
}
