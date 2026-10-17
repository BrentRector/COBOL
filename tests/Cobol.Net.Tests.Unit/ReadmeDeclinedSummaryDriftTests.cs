// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The drift guard for the root <c>README.md</c>'s "Status" section, the summary of what the compiler declines:
/// it names, by its Annex item, EVERY row of <c>docs/CONFORMANCE.md</c> §2 (the Annex A.3 register, ISO §4.2.6)
/// and §5 (the Annex A.4 register, ISO §4.2.7) whose disposition is not Claimed (Not claimed, Partial or N/A), and
/// it names no row those tables claim. The registers are the authority and the README only summarizes them, so the
/// rule runs one way for content (the README follows the tables) and both ways for coverage.
///
/// <para>⛔ WHY THIS EXISTS (kb/Work PB3134, its review R1-1). The README's list was rewritten by hand to "name every
/// declined element" and still left out four the tables decline by name: CODE-SET with an alphabet no device encodes
/// (A.3 item 27, COBOLNET1672), a national collating sequence by literals or LOCALE on a national record key (items
/// 41–42, COBOLNET1584), CONTINUE AFTER beyond whole seconds (item 8), and the MESSAGE-TAG usage, APPLY COMMIT and
/// VALIDATE's refused halves. A summary presented as the whole set is a claim, and a hand-kept one drifts the day a
/// row changes; this test is what makes the next row automatic (CLAUDE.md rule 5).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ReadmeDeclinedSummaryDriftTests
{
    /// <summary>The literal §5 heading of <c>docs/CONFORMANCE.md</c>.</summary>
    private const string AnnexA4Heading = "## 5. Annex A.4";

    /// <summary>One row of §5: its A.4 clause (<c>A.4.11</c>) and its disposition.</summary>
    internal sealed record ModuleRow(string Clause, string Disposition);

    /// <summary>An A.3 reference in the README: <c>A.3 item 8</c>, <c>A.3 items 6–7</c>, <c>A.3 items 19 and
    /// 20–21</c>. Each number or range is one row's item cell, written as §2 writes it.</summary>
    private static readonly Regex A3Reference = new(
        @"A\.3 items? (?<list>[0-9]+(?:–[0-9]+)?(?:(?:,| and) [0-9]+(?:–[0-9]+)?)*)",
        RegexOptions.CultureInvariant);

    private static readonly Regex ItemCell = new(@"[0-9]+(?:–[0-9]+)?", RegexOptions.CultureInvariant);

    private static readonly Regex A4Reference = new(@"A\.4\.[0-9]+", RegexOptions.CultureInvariant);

    /// <summary>A disposition claims the element when it reads Claimed, with or without emphasis and a qualifier
    /// ("**Claimed** (inert)", "**Claimed — on BOTH organizations …**").</summary>
    private static bool IsClaimed(string disposition) =>
        disposition.TrimStart('*', ' ').StartsWith("Claimed", StringComparison.Ordinal);

    /// <summary>Parse §5's table, up to the next <c>## </c> heading. The one parser of the A.4 register:
    /// <see cref="ConformanceSuiteProfileDriftTests"/> reads the rows through it.</summary>
    internal static List<ModuleRow> AnnexA4Register()
    {
        var rows = new List<ModuleRow>();
        bool inSection = false;
        foreach (string line in File.ReadAllLines(TestRepo.Docs("CONFORMANCE.md")))
        {
            if (line.StartsWith(AnnexA4Heading, StringComparison.Ordinal)) { inSection = true; continue; }
            if (!inSection) continue;
            if (line.StartsWith("## ", StringComparison.Ordinal)) break;
            if (!line.StartsWith("| A.4.", StringComparison.Ordinal)) continue;
            var cells = line.Trim().Trim('|').Split('|');
            if (cells.Length < 4) continue;
            rows.Add(new ModuleRow(cells[0].Trim(), cells[2].Trim()));
        }
        return rows;
    }

    /// <summary>The README's "## Status" section, up to the next heading.</summary>
    private static string ReadmeStatusSection()
    {
        var section = new List<string>();
        bool inSection = false;
        foreach (string line in File.ReadAllLines(TestRepo.At("README.md")))
        {
            if (line == "## Status") { inSection = true; continue; }
            if (!inSection) continue;
            if (line.StartsWith("## ", StringComparison.Ordinal)) break;
            section.Add(line);
        }
        return string.Join('\n', section);
    }

    /// <summary>Every disagreement between a status text and the two registers: a declined row the text does not
    /// name, an A.3 reference that is no declined row's item cell, and an A.4 module the text names that §5
    /// claims.</summary>
    private static List<string> Disagreements(string status, IReadOnlyList<AnnexA3RegisterDriftTests.Row> a3,
        IReadOnlyList<ModuleRow> a4)
    {
        var namedA3 = A3Reference.Matches(status)
            .SelectMany(m => ItemCell.Matches(m.Groups["list"].Value).Select(c => c.Value))
            .ToHashSet(StringComparer.Ordinal);
        var namedA4 = A4Reference.Matches(status).Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
        var declinedA3 = a3.Where(r => !IsClaimed(r.Disposition)).Select(r => r.Items).ToHashSet(StringComparer.Ordinal);

        var problems = new List<string>();
        foreach (var r in a3.Where(r => !IsClaimed(r.Disposition) && !namedA3.Contains(r.Items)))
            problems.Add($"§2 item {r.Items} ({r.Element}) is '{r.Disposition}' and the README does not name "
                + $"'A.3 item {r.Items}'");
        foreach (string item in namedA3.Where(i => !declinedA3.Contains(i)).Order(StringComparer.Ordinal))
            problems.Add($"the README names 'A.3 item {item}', which is no row of §2 that declines anything "
                + "(a claimed row, or not a row's item cell as §2 writes it)");
        foreach (var r in a4.Where(r => !IsClaimed(r.Disposition) && !namedA4.Contains(r.Clause)))
            problems.Add($"§5 {r.Clause} is '{r.Disposition}' and the README does not name it");
        foreach (var r in a4.Where(r => IsClaimed(r.Disposition) && namedA4.Contains(r.Clause)))
            problems.Add($"the README names {r.Clause}, which §5 claims");
        return problems;
    }

    /// <summary>⛔ THE OBLIGATION, with its population (feedback_verdict_evidence_invariant): both registers
    /// parse to their known size, and the README names exactly their declined rows.</summary>
    [Fact]
    public void TheReadmeStatusSection_NamesEveryDeclinedRow_AndNoClaimedOne()
    {
        var a3 = AnnexA3RegisterDriftTests.Register();
        var a4 = AnnexA4Register();
        string status = ReadmeStatusSection();
        Assert.True(a3.Count >= 40, $"docs/CONFORMANCE.md §2 parsed to {a3.Count} row(s); the scanner is broken");
        Assert.True(a4.Count >= 13, $"docs/CONFORMANCE.md §5 parsed to {a4.Count} row(s); the scanner is broken");
        Assert.True(a3.Count(r => !IsClaimed(r.Disposition)) >= 10 && a4.Count(r => !IsClaimed(r.Disposition)) >= 5,
            "too few declined rows parsed: the disposition reading is broken and the check would pass vacuously");
        Assert.True(status.Length > 0, "README.md has no '## Status' section to check");

        var problems = Disagreements(status, a3, a4);
        Assert.True(problems.Count == 0,
            "README.md's Status section disagrees with docs/CONFORMANCE.md §2/§5, the registers it summarizes "
            + "(name each declined row by its Annex item, as the register writes it):\n" + string.Join("\n", problems));
    }

    /// <summary>Prove the guard can FAIL in each direction (feedback_green_gates_arent_evidence), on fabricated
    /// rows and text rather than by mutating the documents.</summary>
    [Fact]
    public void TheGuard_CanFail_InBothDirections()
    {
        var a3 = new List<AnnexA3RegisterDriftTests.Row>
        {
            new("8", "CONTINUE AFTER", "14.9.9", "Not claimed", ""),
            new("28–30", "CLOSE REEL", "14.9.6", "Partial", ""),
            new("31", "DELETE", "14.9.10", "**Claimed**", ""),
        };
        var a4 = new List<ModuleRow> { new("A.4.11", "Partial"), new("A.4.12", "**Claimed**") };

        Assert.Empty(Disagreements("A.3 items 8 and 28–30; A.4.11; Annex A.4.1 reads so.", a3, a4));
        var problems = Disagreements("A.3 items 28 and 31; A.4.12.", a3, a4);
        Assert.Contains(problems, p => p.Contains("§2 item 8 ", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("§2 item 28–30 ", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("'A.3 item 28'", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("'A.3 item 31'", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("§5 A.4.11 ", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("names A.4.12", StringComparison.Ordinal));
    }
}
