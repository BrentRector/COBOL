// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ REPETITION IN A REPORT GROUP IS A SUBTREE REPLAY, AND EVERY CLAUSE RIDES IT FOR FREE (kb/Work PB565).
/// ISO §13.18.38.4 GR10 says a repeating entry "causes the entry to define integer-2 distinct report items" and
/// GR11 says every clause has "the same effect on each repetition as they would on a single data item without
/// the OCCURS clause" — so the ONE correct shape is to bind the entry's subtree once per repetition, not to
/// teach each clause about repetition. <c>DataBinder.BindReportEntries</c> does exactly that, which is why
/// §13.18.63.4 GR21's import of GR9 (a VALUE reaches every occurrence), GR22's OCCURS-DEPENDING suppressor and
/// §13.18.64's VARYING counters all became correct at once.
///
/// <para>Three guards keep it structural:</para>
/// <list type="number">
/// <item>the replay call is the ONLY repetition site — no clause binder may loop over an OCCURS count itself;</item>
/// <item>every <c>ReportColumnKindModel</c> is handled at BOTH placement readers (the emitter's compose switch
/// and the binder's line-width walk) — a new placement kind that reaches only one of them is this repo's
/// most reproducible defect, the two-arm dispatch with one arm fixed;</item>
/// <item>BOTH AXES ride the one replay, and the axis is decided in ONE place: §13.18.38.4 GR10/GR12 split
/// a/b (COLUMN) from c/d (LINE), so an entry's integer-3 is a horizontal interval or a vertical one but never
/// both, and a second axis test would be a second copy of that split.</item>
/// </list>
/// </summary>
public sealed class ReportRepeatingEntryDriftTests
{
    private static string BinderText() => File.ReadAllText(
        Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "Binding", "DataBinder.Reports.cs"));

    private static string EmitterText() => File.ReadAllText(
        Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "CodeGen", "Verbs", "ReportWriterEmitter.cs"));

    /// <summary>The replay is ONE call site. A second place that multiplies a report entry by an OCCURS count
    /// would be a second copy of GR10, and the two would drift the day GR11, GR12 or GR13 changed.</summary>
    [Fact]
    public void TheSubtreeReplay_IsTheOnlyRepetitionSite()
    {
        string src = BinderText();
        // Population: the replay exists and this scan can see it (a rename must fail loudly, not vacuously).
        Assert.Contains("private void BindReportEntries(", src, StringComparison.Ordinal);
        Assert.Contains("for (int rep = 0; rep < occurs.Max; rep++)", src, StringComparison.Ordinal);

        // No OTHER loop in the report binder may be driven by a repeating entry's count.
        var others = Regex.Matches(src, @"<\s*\w*[Oo]ccurs\w*\.Max\b|<=\s*\w*[Oo]ccurs\w*\.Max\b")
            .Select(m => m.Value).ToList();
        Assert.Single(others);
    }

    /// <summary>Every placement kind is handled at BOTH readers. The list is derived from the enum's own
    /// declaration, so adding a kind fails here until both arms name it.</summary>
    [Fact]
    public void EveryColumnPlacementKind_IsReadByBothPlacementReaders()
    {
        string binder = BinderText(), emitter = EmitterText();
        var kinds = Regex.Matches(
                binder[binder.IndexOf("public enum ReportColumnKindModel", StringComparison.Ordinal)..],
                @"^\s{4}(\w+),\s*$", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).ToList();
        // Population: the enum really has the four placement kinds this guard was written for.
        Assert.Equal(new[] { "Absolute", "Relative", "AnchorSeed", "AnchorStep" }, kinds);

        // The binder's line-width walk and the emitter's compose switch each name every kind — except that both
        // may spell the LAST arm as the switch default, which is exhaustive by construction.
        foreach (string kind in kinds.Take(kinds.Count - 1))
        {
            Assert.Contains($"ReportColumnKindModel.{kind}", binder, StringComparison.Ordinal);
            Assert.Contains($"ReportColumnKindModel.{kind}", emitter, StringComparison.Ordinal);
        }
        Assert.Contains("anchors[spec.AnchorId]", binder, StringComparison.Ordinal);   // the width walk's anchors
        Assert.Contains("__ra{spec.AnchorId}", emitter, StringComparison.Ordinal);     // the compose method's
    }

    /// <summary>NEITHER AXIS STAGES ANY MORE, and the retired ids stay retired. The two COBOLNET0899
    /// descriptors that refused vertical repetition — <c>report-occurs-in-group</c> and
    /// <c>report-multiple-line</c> — are gone, and nothing may re-declare them: a re-added stage would refuse
    /// §13.18.38.4 GR10c/GR10d and §13.18.35.4 GR9, both of which are conforming source this compiler now
    /// prints correctly.</summary>
    [Fact]
    public void NeitherRepetitionAxis_StagesLoud()
    {
        string catalog = File.ReadAllText(Path.Combine(
            TestRepo.Src("Cobol.Net.Editions"), "Diagnostics", "DiagnosticCatalog.cs"));
        // Population: the retirement comment is where the descriptors were, so this scan reads the right file.
        Assert.Contains("`report-occurs-in-group`", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportOccursInGroup = new(", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportMultipleLine = new(", catalog, StringComparison.Ordinal);
        string binder = BinderText();
        Assert.DoesNotContain("DiagnosticCatalog.ReportOccursInGroup", binder, StringComparison.Ordinal);
        Assert.DoesNotContain("DiagnosticCatalog.ReportMultipleLine", binder, StringComparison.Ordinal);
    }

    /// <summary>⛔ THE AXIS IS DECIDED ONCE (ISO §13.18.38.4 GR10/GR12). Every reader of a repeating entry's
    /// STEP asks <c>Shift(axis)</c> or <c>Undisplaced(axis)</c> — never <c>Spec.Step</c> directly — because
    /// integer-3 is an interval on the entry's OWN axis and a second reading of it is the two-arm dispatch
    /// this file exists to prevent.</summary>
    [Fact]
    public void TheStepDisplacement_IsReadThroughTheAxis()
    {
        string binder = BinderText();
        // Population: both placement builders exist and this scan can see them.
        Assert.Contains("private static IReadOnlyList<ReportColumnSpec> RepeatedPlacements(", binder, StringComparison.Ordinal);
        Assert.Contains("private static ReportLineModel RepeatedLine(", binder, StringComparison.Ordinal);
        Assert.Contains("st.Shift(ReportRepetitionAxis.Horizontal)", binder, StringComparison.Ordinal);
        Assert.Contains("st.Shift(ReportRepetitionAxis.Vertical)", binder, StringComparison.Ordinal);
        // The ONE place a frame's integer-3 is turned into a displacement.
        var reads = Regex.Matches(binder, @"\.Spec\.Step \?\? 0").Select(m => m.Value).ToList();
        Assert.Single(reads);
    }

    /// <summary>Every LINE placement kind is handled at BOTH readers — the binder that builds it and the
    /// runtime that places it. The enum is declared twice by design (model and runtime), so the guard is that
    /// the two declarations agree member for member.</summary>
    [Fact]
    public void EveryLinePlacementKind_IsDeclaredByBothTheModelAndTheEngine()
    {
        string binder = BinderText();
        string runtime = File.ReadAllText(Path.Combine(TestRepo.Src("Cobol.Net.Runtime"), "IO", "ReportWriter.cs"));
        // The members are read OFF the declaration — a new kind fails here until both declarations name it,
        // rather than being compared against a list this test would have to be taught about.
        var model = Regex.Matches(
                binder[binder.IndexOf("public enum ReportLineKindModel", StringComparison.Ordinal)..].Split('}')[0],
                @"\w+")
            .Select(m => m.Value)
            .Where(w => w is not ("public" or "enum" or "ReportLineKindModel"))
            .ToList();
        // Population: the enum really has the three placement kinds this guard was written for.
        Assert.Equal(new[] { "Absolute", "Relative", "Step" }, model);
        foreach (string kind in model)
        {
            Assert.Contains($"ReportLineKindModel.{kind}", binder, StringComparison.Ordinal);
            Assert.Contains($"ReportLineKind.{kind}", runtime, StringComparison.Ordinal);
        }
        // The engine reads a Step line's anchor, and it is the ONE subsequent-line rule all four group
        // presentations share (it was four copies of `LineCounter + l.Value` before PB565's vertical axis). Since
        // kb/Work PB1272 the four presentations share ONE line loop too, `PresentLines`, so the rule has exactly
        // one call site and every presentation reaches it through that loop — never a loop of its own.
        Assert.Contains("private long SubsequentTarget(ReportGroupLine l)", runtime, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(runtime, @"SubsequentTarget\(l\)"));
        foreach (string presentation in new[] { "PresentBody", "PresentPageHeading", "PresentPageFooting", "PresentHeadingFooting" })
        {
            int start = runtime.IndexOf($"private void {presentation}(", StringComparison.Ordinal);
            Assert.True(start >= 0, $"{presentation} is no longer an engine method — re-derive this guard");
            int end = runtime.IndexOf("\n    }", start, StringComparison.Ordinal);
            Assert.Contains("PresentLines(", runtime[start..end], StringComparison.Ordinal);
        }
    }

    /// <summary>A repetition's VARYING counter is the CLOSED form over the repetition ordinal, never an
    /// accumulator: a replayed entry becomes one field per repetition, and a field-local accumulator could not
    /// span them (ISO §13.18.64.4 GR3 — occurrence n holds FROM + n × BY).</summary>
    [Fact]
    public void TheVaryingCounter_IsClosedFormOverTheRepetitionOrdinal()
    {
        string emitter = EmitterText();
        Assert.Contains("f.RepetitionOrdinal + rep", emitter, StringComparison.Ordinal);
        // The accumulator shape that cannot survive a replay.
        Assert.DoesNotContain("+= {VaryValue(", emitter, StringComparison.Ordinal);
    }
}
