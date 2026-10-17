// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The drift guard for WiseOwl COBOL's conformance-suite profile, <c>conformance-suite/profiles/wiseowl.json</c>
/// (DESIGN-conformance-suite.md §4, §5): the Annex A.4 modules it claims are exactly the rows of
/// <c>docs/CONFORMANCE.md</c> §5 whose disposition is Claimed or Partial, the Annex A.3 elements it claims are exactly
/// the items of the §2 rows whose disposition is Claimed or Partial (a range row, <c>28–30</c>, claims each item of it),
/// and it supports all four editions. The registers are the authority and the profile restates them for a harness
/// that runs where the documentation is not (a released suite carries no <c>docs/</c>), so the rule runs one way.
///
/// <para>WHY CLAIMED OR PARTIAL: §5's preamble — "§1's conformance summary claims only what is Claimed/Partial here".
/// A partial claim is a claim of the module; the part it declines is documented non-support, whose tests are
/// WiseOwl's own (DESIGN-conformance-suite.md §6 step 1), never a suite's. A profile claiming less makes a suite test
/// NOT APPLICABLE on WiseOwl, which hides it; one claiming more makes a test of a declined module FAIL for a
/// documented decline (kb/Work PB3175).</para>
/// </summary>
public sealed class ConformanceSuiteProfileDriftTests
{
    private static readonly string[] Editions = ["85", "2002", "2014", "2023"];

    /// <summary>The §5 preamble's reading: a disposition claims support when it reads Claimed or Partial, with or
    /// without emphasis and a qualifier.</summary>
    private static bool ClaimsSupport(string disposition)
    {
        string d = disposition.TrimStart('*', ' ');
        return d.StartsWith("Claimed", StringComparison.Ordinal) || d.StartsWith("Partial", StringComparison.Ordinal);
    }

    /// <summary>An A.3 row's item cell, <c>17</c> or a range <c>28–30</c>, as the profile's names.</summary>
    private static IEnumerable<string> ItemNames(string items)
    {
        string[] ends = items.Split('–');
        int first = int.Parse(ends[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        int last = ends.Length > 1 ? int.Parse(ends[1].Trim(), System.Globalization.CultureInfo.InvariantCulture) : first;
        return Enumerable.Range(first, last - first + 1).Select(n => $"A.3-{n}");
    }

    private static (string[] Editions, SortedSet<string> Modules, SortedSet<string> Elements) Profile()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.At("conformance-suite", "profiles", "wiseowl.json")));
        var root = doc.RootElement;
        return (root.GetProperty("editions").EnumerateArray().Select(e => e.GetString()!).ToArray(),
            new SortedSet<string>(root.GetProperty("modules").EnumerateObject().Select(p => p.Name), StringComparer.Ordinal),
            new SortedSet<string>(root.GetProperty("elements").EnumerateObject().Select(p => p.Name), StringComparer.Ordinal));
    }

    /// <summary>⛔ THE OBLIGATION, with its population: both registers parse to their known size, some rows claim and
    /// some decline (so the comparison cannot pass vacuously), and the profile claims exactly the claiming rows.</summary>
    [Fact]
    public void TheWiseOwlProfile_ClaimsExactlyWhatTheConformanceRegistersClaim()
    {
        var a3 = AnnexA3RegisterDriftTests.Register();
        var a4 = ReadmeDeclinedSummaryDriftTests.AnnexA4Register();
        Assert.True(a3.Count >= 40, $"docs/CONFORMANCE.md §2 parsed to {a3.Count} row(s); the scanner is broken");
        Assert.True(a4.Count >= 13, $"docs/CONFORMANCE.md §5 parsed to {a4.Count} row(s); the scanner is broken");
        Assert.True(a3.Count(r => ClaimsSupport(r.Disposition)) >= 20 && a3.Count(r => !ClaimsSupport(r.Disposition)) >= 5,
            "too few claiming or declining §2 rows: the disposition reading is broken");

        var wantModules = new SortedSet<string>(a4.Where(r => ClaimsSupport(r.Disposition)).Select(r => r.Clause),
            StringComparer.Ordinal);
        var wantElements = new SortedSet<string>(
            a3.Where(r => ClaimsSupport(r.Disposition)).SelectMany(r => ItemNames(r.Items)), StringComparer.Ordinal);
        var (editions, modules, elements) = Profile();

        var problems = new List<string>();
        problems.AddRange(wantModules.Except(modules).Select(m => $"§5 claims {m}; the profile's modules do not"));
        problems.AddRange(modules.Except(wantModules).Select(m => $"the profile claims {m}, which §5 does not claim"));
        problems.AddRange(wantElements.Except(elements).Select(e => $"§2 claims {e}; the profile's elements do not"));
        problems.AddRange(elements.Except(wantElements).Select(e => $"the profile claims {e}, which §2 does not claim"));
        if (!editions.SequenceEqual(Editions))
            problems.Add($"the profile supports [{string.Join(", ", editions)}], not every edition [{string.Join(", ", Editions)}]");
        Assert.True(problems.Count == 0,
            "conformance-suite/profiles/wiseowl.json disagrees with docs/CONFORMANCE.md §2/§5 (the registers are the "
            + "authority; restate them in the profile):\n  " + string.Join("\n  ", problems));
    }
}
