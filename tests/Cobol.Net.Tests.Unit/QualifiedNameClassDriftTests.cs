// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB919 — ISO §8.4.2.2.3 SR1's uniqueness obligation covers EVERY user-defined name, and it was
/// enforced per name class with one class (index-names) never done. <see cref="QualifiedNameClasses"/> names the
/// resolution of each §8.4.2.2.2 qualified-name format; these tests hold it to the standard's own format list, in
/// both directions, and hold each named member to existence — so a name class added later cannot arrive unrouted.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class QualifiedNameClassDriftTests
{
    private static readonly Regex FormatHeading =
        new(@"^Format \d+ \((qualified-[a-z-]+)\):\s*$", RegexOptions.Compiled);

    /// <summary>The §8.4.2.2.2 formats as the standard prints them.</summary>
    private static List<string> SpecFormats()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(lines, l => l.StartsWith("###### 8.4.2.2.2 General format", StringComparison.Ordinal));
        Assert.True(start >= 0, "specs/ISO_COBOL.md has no §8.4.2.2.2 heading — the scan measured nothing");
        var formats = new List<string>();
        for (int i = start + 1; i < lines.Length && !lines[i].StartsWith("###### ", StringComparison.Ordinal); i++)
            if (FormatHeading.Match(lines[i]) is { Success: true } m) formats.Add(m.Groups[1].Value);
        Assert.True(formats.Count >= 8, $"§8.4.2.2.2 yielded only {formats.Count} formats — the scan measured nothing");
        return formats;
    }

    [Fact]
    public void EveryPrintedQualifiedNameFormat_HasExactlyOneRow()
    {
        var spec = SpecFormats();
        var rows = QualifiedNameClasses.All.Select(r => r.Format).ToList();
        Assert.True(rows.Distinct(StringComparer.Ordinal).Count() == rows.Count, "a format has two rows");
        var missing = spec.Except(rows, StringComparer.Ordinal).ToList();
        var extra = rows.Except(spec, StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0 && extra.Count == 0,
            $"QualifiedNameClasses disagrees with ISO §8.4.2.2.2 — unrouted: [{string.Join(", ", missing)}]; not "
            + $"in the standard: [{string.Join(", ", extra)}]. Route each qualified-name format's references through "
            + "ONE resolution that counts candidates (§8.4.2.2.3 SR1), and name it here (kb/Work PB919).");
    }

    [Fact]
    public void EveryRow_NamesAMemberThatExists()
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var dead = QualifiedNameClasses.All
            .Where(r => r.Owner.GetMember(r.Member, all).Length == 0)
            .Select(r => $"{r.Format} → {r.Owner.Name}.{r.Member}")
            .ToList();
        Assert.True(dead.Count == 0, "a QualifiedNameClasses row names a member that does not exist: "
            + string.Join("; ", dead));
    }

    /// <summary>The index-name row's resolution returns a COUNTABLE set — the shape that makes SR1 enforceable.</summary>
    [Fact]
    public void IndexNameResolution_ReturnsTheCountedCandidateSet()
    {
        var m = typeof(SymbolTable).GetMethod(nameof(SymbolTable.IndexCandidates));
        Assert.NotNull(m);
        Assert.Equal(typeof(NameCandidates<IndexDeclaration>?), m!.ReturnType);
    }
}
