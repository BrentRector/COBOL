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
/// ⛔ A REPORT-GROUP REFERENCE RESOLVES THROUGH ONE FUNNEL (<c>ReportGroupResolution</c> — kb/Work PB365), AND
/// THIS KEEPS IT TRUE. Two statements name a report group by name — <c>GENERATE data-name-1</c> (ISO
/// §14.9.16.3 SR1) and <c>USE BEFORE REPORTING identifier-1</c> (§14.9.49.3 SR9) — and both were written as
/// <c>foreach (report) if (report.Groups.FirstOrDefault(name-matches) is {} g) …</c>, a loop that returns on
/// the first match where §8.4.2.2 requires exactly one. Both arms were wrong; both were fixed by DELETING the
/// search and calling the funnel (feedback_two_arm_dispatch, feedback_one_rule_one_place).
///
/// <para>The guard is source-form on the shape that was the bug: no compiler source may search
/// <c>.Groups</c> for a name itself. A third statement that needs a report-group reference — or a "small"
/// re-inlining of the search into one of these two — fails here until it goes through the funnel or is
/// adjudicated WITH its reason.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ReportGroupResolutionDriftTests
{
    private const string FunnelRel = @"Binding\ReportGroupResolution.cs";

    /// <summary>Adjudicated files that may search <c>ReportModel.Groups</c> by name: file → reason. Only the
    /// funnel itself qualifies today. Adding one is an adjudication, not a formality.</summary>
    private static readonly Dictionary<string, string> Adjudicated = new(StringComparer.Ordinal)
    {
        [Path.Combine("Binding", "ReportGroupResolution.cs")] = "the funnel itself",
    };

    /// <summary>The <c>Groups</c>-by-name search shape: any use of <c>.Groups</c> in the same expression as a
    /// name comparison (<c>Equals(</c> — the OrdinalIgnoreCase form §8.3.2.2 requires). Object-identity uses
    /// such as <c>Groups.Contains(group)</c> are NOT a name search and do not match.</summary>
    private static readonly Regex NameSearch = new(
        @"\.Groups\b[^;]{0,200}?\bEquals\(", RegexOptions.Singleline | RegexOptions.Compiled);

    private static IEnumerable<(string Rel, string Text)> CompilerSources()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains("Generated", StringComparison.Ordinal)) continue;
            yield return (Path.GetRelativePath(root, f), File.ReadAllText(f));
        }
    }

    [Fact]
    public void EveryReportGroupNameLookup_IsTheFunnel_OrAdjudicated()
    {
        var sources = CompilerSources().ToList();

        // Population: the funnel exists and this scan can see it. A rename must fail loudly, not vacuously
        // (feedback_green_gates_arent_evidence — a guard that looked at nothing passes for the wrong reason).
        Assert.True(sources.Any(s => s.Rel == Path.Combine("Binding", "ReportGroupResolution.cs")),
            $"{FunnelRel} is gone or moved — this guard must follow it");

        var offenders = sources.Where(s => NameSearch.IsMatch(s.Text))
            .Select(s => s.Rel)
            .Where(rel => !Adjudicated.ContainsKey(rel))
            .ToList();
        Assert.True(offenders.Count == 0,
            "report-group name search(es) outside the funnel — resolve through "
            + "ReportGroupResolution.Resolve so the §8.4.2.2.1 / §8.4.2.2.3 SR1 ambiguity rule and the "
            + "IN/OF report-name qualifier are written down ONCE (kb/Work PB365):\n  "
            + string.Join("\n  ", offenders));
    }

    [Fact]   // The consumers named in the funnel's doc comment really do call it — the complement of the
             // scan above, which can only prove that nobody ELSE searches (feedback_measure_the_selectors_complement).
    public void EveryConsumer_CallsTheFunnel()
    {
        foreach (string rel in new[]
                 {
                     Path.Combine("Binding", "Procedure", "ProcedureTableBuilder.cs"),
                     Path.Combine("Binding", "Procedure", "Verbs", "ReportWriterBinder.cs"),
                     // The DATA-division site: the SUM clause's UPON data-name-2 (ISO §13.18.54.3 SR7),
                     // resolved in ResolveReports once every report group is described (kb/Work PB482).
                     Path.Combine("Binding", "DataBinder.Reports.cs"),
                 })
        {
            string text = File.ReadAllText(Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), rel));
            Assert.True(text.Contains("ReportGroupResolution.Resolve(", StringComparison.Ordinal),
                $"{rel} no longer binds its report-group reference through ReportGroupResolution.Resolve "
                + "(kb/Work PB365 / PB482 — GENERATE, USE BEFORE REPORTING and SUM … UPON are the funnel's "
                + "three consumers)");
        }
    }
}
