// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE IMPLEMENTOR-DEFINED EXCEPTION-NAME DOCUMENTATION (docs/CONFORMANCE.md §7, Annex A.1 items 99 and 100) IS HELD TO
/// THE CATALOG IT DOCUMENTS (kb/Work PB1531). ISO §14.6.13.1.1 leaves "the action to be taken, the fatality, and when any of
/// these exceptions are raised" of an implementor-defined exception-name to the implementor, and A.1 requires what the
/// implementor provides to be documented; the catalog (<see cref="ExceptionCatalog"/>) is the one machine form of the names
/// a program can write, so a name added to or removed from it without the rows is a defect this test names. Item 99 says no
/// <c>EC-IMP-suffix</c> exists and item 100 lists the level-2 <c>-IMP</c> names Table 13 defines; both statements are asserted
/// against <see cref="ExceptionCatalog.Level3Rows"/>.
/// </summary>
public sealed class EcImplementorNamesDocumentationDriftTests
{
    private static string RowOf(string key)
    {
        string prefix = $"| {key} |";
        string? row = File.ReadLines(TestRepo.Docs("CONFORMANCE.md"))
            .FirstOrDefault(l => l.StartsWith(prefix, StringComparison.Ordinal));
        Assert.True(row is not null, $"docs/CONFORMANCE.md has no §7 row {key}");
        return row!;
    }

    /// <summary>A.1 item 100 names exactly the catalog's level-2 <c>-IMP</c> exception-names (Table 13's 22 rows), no more
    /// and no fewer, and says they are fatal because the catalog treats every one as fatal.</summary>
    [Fact]
    public void Item100_NamesExactlyTheCatalogsImplementorDefinedLevel2Names()
    {
        string row = RowOf("DOC-A.1-100");
        // The names the row writes; "EC-IMP" and "EC-IMP-suffix" never match (the pattern needs a family segment).
        var documented = Regex.Matches(row, @"EC-[A-Z0-9]+(?:-[A-Z0-9]+)*-IMP\b")
            .Select(m => m.Value).ToHashSet(StringComparer.Ordinal);

        var provided = ExceptionCatalog.Level3Rows
            .Where(i => i.Fatality == EcFatality.Imp)
            .Select(i => i.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(22, provided.Count);   // Table 13's EC-level-2-IMP rows — the population is asserted before the match
        Assert.True(provided.SetEquals(documented),
            "DOC-A.1-100 and ExceptionCatalog disagree about the implementor-defined level-2 exception-names.\n"
            + $"catalog only: {string.Join(", ", provided.Except(documented).Order())}\n"
            + $"documented only: {string.Join(", ", documented.Except(provided).Order())}");
        Assert.All(ExceptionCatalog.Level3Rows.Where(i => i.Fatality == EcFatality.Imp), i => Assert.True(i.IsFatal, i.Name));
        Assert.Contains("each is FATAL", row, StringComparison.Ordinal);
    }

    /// <summary>A.1 item 99 says this implementation provides no <c>EC-IMP-suffix</c> name, and the catalog agrees: no
    /// level-3 row starts <c>EC-IMP-</c> and the name resolves to nothing (the refusal itself is COBOLNET0711, witnessed by the
    /// <c>pb1531-ec-imp-suffix-*</c> negative goldens).</summary>
    [Fact]
    public void Item99_NoEcImpSuffixNameExists()
    {
        Assert.Contains("**Not provided.**", RowOf("DOC-A.1-99"), StringComparison.Ordinal);
        Assert.DoesNotContain(ExceptionCatalog.Level3Rows, i => i.Name.StartsWith("EC-IMP-", StringComparison.Ordinal));
        Assert.False(ExceptionCatalog.TryGet("EC-IMP-WIBBLE", out _));
        Assert.True(ExceptionCatalog.IsImplementorSuffixForm("EC-IMP-WIBBLE"));   // the SPELLING stays reserved (§8.12)
    }
}
