// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Editions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE §4.2.10 REGISTER OF NONSTANDARD EXTENSIONS IS ONE TABLE, AND EVERYTHING THAT SAYS WHAT AN EXTENSION IS READS
/// IT (kb/Work PB1525). ISO/IEC 1989:2023 §4.2.10 owes the user three things — documentation that identifies the
/// extensions for which support is claimed and specifies the reserved words added for them, and a compile-time
/// warning mechanism that flags only syntactically distinguishable ones — and
/// <see cref="NonstandardExtensionRegister"/> is where all three are answered. This class keeps the five places that
/// speak of an extension from drifting apart: the register's ids and their <see cref="ExtensionIds"/> constants; the
/// reserved-word table (<c>cobol-words.json</c> <c>extensionReserved</c>); the usage funnel, which finds a vendor
/// spelling BY ROW; the grammar's <c>usageKeyword</c> alternatives; the binder sites, which name a row by its id; and
/// <c>docs/CONFORMANCE.md</c> §3.2. A new extension is a row, and every one of these fails until it is complete.
/// </summary>
public sealed class NonstandardExtensionRegisterDriftTests
{
    private static NonstandardExtension[] Rows => [.. NonstandardExtensionRegister.Entries];

    private static Dictionary<string, string> IdConstants() =>
        typeof(ExtensionIds).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral)
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!);

    [Fact]
    public void EveryRowHasAnIdConstantAndEveryConstantARow()
    {
        var constants = IdConstants();
        var ids = Rows.Select(r => r.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.True(constants.Values.ToHashSet(StringComparer.Ordinal).SetEquals(ids),
            $"ExtensionIds constants [{string.Join(",", constants.Values.Order())}] != register ids [{string.Join(",", ids.Order())}]");
    }

    /// <summary>§4.2.10: "documentation … shall specify any reserved words added for nonstandard extensions" — the
    /// words the lexer reserves on an extension's account (<c>cobol-words.json</c> <c>extensionReserved</c>) are
    /// exactly the words the register's rows carry, each on one row.</summary>
    [Fact]
    public void TheReservedWordsOfTheRows_AreTheLexersExtensionReservedTable()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("cobol-words.json")));
        var table = doc.RootElement.GetProperty("extensionReserved").EnumerateArray()
            .Select(e => e.GetProperty("word").GetString()!).ToList();
        var carried = Rows.SelectMany(r => r.ReservedWords).ToList();
        Assert.Equal(carried.Count, carried.Distinct(StringComparer.Ordinal).Count());
        Assert.True(table.ToHashSet(StringComparer.Ordinal).SetEquals(carried),
            $"extensionReserved [{string.Join(",", table.Order())}] != the register's ReservedWords [{string.Join(",", carried.Order())}]");
    }

    /// <summary>The usage funnel (<c>PictureAnalyzer.ParseUsage</c>) recognizes a vendor spelling BY ROW, so a row's
    /// every spelling is flagged under <c>--flag-extensions</c> and none is flagged without it — adding a spelling to
    /// a row needs no edit at the funnel.</summary>
    [Fact]
    public void EveryUsageSpelling_IsFlaggedByTheOneUsageFunnel_OnlyWhenAsked()
    {
        foreach (var row in Rows.Where(r => r.Spellings.Count > 0))
            foreach (string spelling in row.Spellings)
            {
                var on = new EditionContext(2023) { FlagExtensions = true };
                PictureAnalyzer.ParseUsage(spelling, on, "data item 'T'");
                Assert.True(on.Warnings.Any(w => w.Contains("COBOLNET2894") && w.Contains(spelling)),
                    $"{spelling} ({row.Id}) was not flagged: [{string.Join(" | ", on.Warnings)}]");
                var off = new EditionContext(2023);
                PictureAnalyzer.ParseUsage(spelling, off, "data item 'T'");
                Assert.Empty(off.Warnings);
            }
    }

    /// <summary>The other direction: a vendor usage word the GRAMMAR accepts (<c>COMP_n</c> / <c>COMPUTATIONAL_n</c>
    /// alternatives of <c>usageKeyword</c>) that no row spells is an extension compiled in silence — the defect this
    /// register exists to end.</summary>
    [Fact]
    public void EveryVendorUsageTokenTheGrammarAccepts_IsASpellingOfARow()
    {
        string g4 = File.ReadAllText(TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolData.g4"));
        var rule = Regex.Match(g4, @"usageKeyword\r?\n\s*:(.*?)\r?\n\s*;\s*\r?\n", RegexOptions.Singleline);
        Assert.True(rule.Success, "CobolData.g4 lost its usageKeyword rule");
        var vendor = Regex.Matches(rule.Groups[1].Value, @"\b(COMP(?:UTATIONAL)?_\d)\b")
            .Select(m => m.Groups[1].Value.Replace('_', '-')).ToHashSet(StringComparer.Ordinal);
        var spelled = Rows.SelectMany(r => r.Spellings).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(vendor);
        Assert.True(vendor.SetEquals(spelled),
            $"usageKeyword vendor tokens [{string.Join(",", vendor.Order())}] != register Spellings [{string.Join(",", spelled.Order())}]");
    }

    /// <summary>"A dead lookup is also an unverified one": every ACCEPTED, syntactically distinguishable row that is
    /// not a usage spelling is named by a binder site (<c>EditionContext.Extension(ExtensionIds.X, …)</c>); a row that
    /// is not flaggable (not distinguishable, or refused by name) is named by none, because §4.2.10's mechanism
    /// "shall flag only extensions that are syntactically distinguishable".</summary>
    [Fact]
    public void EveryFlaggableRowIsNamedByABinderSite_AndNoOtherRowIs()
    {
        var constants = IdConstants();
        string register = Path.GetFullPath(TestRepo.Src("Cobol.Net.Editions", "NonstandardExtension.cs"));
        var references = new HashSet<string>(StringComparer.Ordinal);
        foreach (string dir in new[] { "Cobol.Net.Compiler", "Cobol.Net.Frontend", "Cobol.Net.Cli", "Cobol.Net.Editions" })
            foreach (string file in Directory.EnumerateFiles(TestRepo.Src(dir), "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFullPath(file) == register) continue;
                string rel = Path.GetRelativePath(TestRepo.Src(dir), file);
                if (rel.StartsWith("obj", StringComparison.Ordinal) || rel.StartsWith("bin", StringComparison.Ordinal)
                    || rel.StartsWith("Generated", StringComparison.Ordinal)) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(file), @"ExtensionIds\.(\w+)"))
                    references.Add(constants[m.Groups[1].Value]);
            }
        foreach (var row in Rows)
        {
            bool flaggableBySite = row.Support == ExtensionSupport.Accepted && row.Distinguishable && row.Spellings.Count == 0;
            Assert.True(references.Contains(row.Id) == flaggableBySite,
                flaggableBySite
                    ? $"{row.Id} is an accepted, distinguishable extension with no EditionContext.Extension(ExtensionIds.…) site — "
                      + "it is accepted in silence, which is what the register exists to end"
                    : $"{row.Id} is not flaggable by a binder site (usage spelling, undistinguishable or refused by name) but one names it");
        }
    }

    [Fact]
    public void ARowThatIsNotFlaggable_IsRefusedBytheSeam_NotSilentlyIgnored()
    {
        var ed = new EditionContext(2023) { FlagExtensions = true };
        foreach (string id in new[] { ExtensionIds.SourceFormatAuto, ExtensionIds.VendorStatementWords })
            Assert.Throws<InvalidOperationException>(() => ed.Extension(id, "x"));
        Assert.Throws<ArgumentException>(() => ed.Extension("no-such-extension", "x"));
        Assert.Empty(ed.Warnings);
    }

    /// <summary>§4.2.10: "Documentation associated with an implementation shall identify nonstandard extensions for
    /// which support is claimed" — <c>docs/CONFORMANCE.md</c> §3.2 has one table row per register row, first column
    /// the row's id in backticks, and no other.</summary>
    [Fact]
    public void TheDocumentation_ListsExactlyTheRegisterRows()
    {
        string conformance = File.ReadAllText(TestRepo.Docs("CONFORMANCE.md"));
        var section = Regex.Match(conformance, @"### 3\.2 Nonstandard extensions.*?(?=\r?\n## )", RegexOptions.Singleline);
        Assert.True(section.Success, "docs/CONFORMANCE.md lost its §3.2 Nonstandard extensions section (ISO §4.2.10 documentation)");
        var documented = Regex.Matches(section.Value, @"^\| `([a-z0-9-]+)` \|", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(documented.Count, documented.Distinct(StringComparer.Ordinal).Count());
        Assert.True(documented.ToHashSet(StringComparer.Ordinal).SetEquals(Rows.Select(r => r.Id)),
            $"CONFORMANCE.md §3.2 lists [{string.Join(",", documented.Order())}] != register [{string.Join(",", Rows.Select(r => r.Id).Order())}]");
    }
}
