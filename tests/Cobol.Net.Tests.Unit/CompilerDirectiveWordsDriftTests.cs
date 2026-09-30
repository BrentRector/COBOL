// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1366 — <see cref="CompilerDirectiveWords"/> is the ONE representation of ISO/IEC 1989:2023 §8.12,
/// "Compiler-directive words", and these tests are what keeps it equal to the standard: a word the table prints
/// and the code lacks is a compilation-variable-name this compiler would accept (§7.3.11.3 SR1, §7.3.8.4.3 SR1), and
/// a word the code has and the table lacks is legal source it would reject.
/// </summary>
public sealed class CompilerDirectiveWordsDriftTests
{
    /// <summary>The §8.12 table, read from the specification itself: every cell of the pipe table between the
    /// section heading and the "In addition to the above list" sentence.</summary>
    private static HashSet<string> SpecTable()
    {
        string path = TestRepo.Specs("ISO_COBOL.md");
        Assert.True(File.Exists(path), $"the ISO transcription is missing: {path}");
        string text = File.ReadAllText(path);
        int head = text.IndexOf("### 8.12 Compiler-directive words", StringComparison.Ordinal);
        Assert.True(head >= 0, "section 8.12 not found in the transcription");
        int end = text.IndexOf("In addition to the above list", head, StringComparison.Ordinal);
        Assert.True(end > head, "the 8.12 table's closing sentence was not found");
        var words = new HashSet<string>(StringComparer.Ordinal);
        foreach (string row in text[head..end].Split('\n'))
        {
            string r = row.Trim().TrimEnd('\r');
            if (!r.StartsWith('|') || r.StartsWith("|---") || r.StartsWith("| | | |")) continue;
            foreach (string cell in r.Trim('|').Split('|'))
            {
                string w = cell.Trim().Replace("\\*", "*");
                if (w.Length > 0) words.Add(w);
            }
        }

        return words;
    }

    [Fact]
    public void Table_IsEqualToTheSpecTable()
    {
        var spec = SpecTable();
        var code = CompilerDirectiveWords.All.ToHashSet(StringComparer.Ordinal);
        Assert.True(spec.Count > 90, $"the 8.12 table parse found only {spec.Count} words");
        Assert.True(spec.SetEquals(code),
            "§8.12 and CompilerDirectiveWords disagree — in the spec only: ["
            + string.Join(", ", spec.Except(code).Order(StringComparer.Ordinal)) + "]; in the code only: ["
            + string.Join(", ", code.Except(spec).Order(StringComparer.Ordinal)) + "]");
        Assert.Equal(CompilerDirectiveWords.All.Count, code.Count);   // no duplicate entry
    }

    /// <summary>Every directive the catalog recognizes heads its line with a §8.12 word (§7.3.3 SR6) — except the two
    /// Annex E.2 item 21 directives the 2023 text removed, which the 2023 table no longer lists. A directive added to
    /// <c>constructs.json</c> without its word on the list is a red test, never a silently usable variable name.</summary>
    [Fact]
    public void EveryCatalogDirectiveWord_IsReserved()
    {
        string[] removedIn2023 = ["FLAG-85", "FLAG-NATIVE-ARITHMETIC"];
        var missing = CompilerDirectiveCatalog.Words
            .Where(w => !removedIn2023.Contains(w) && !CompilerDirectiveWords.IsReserved(w)).ToList();
        Assert.True(missing.Count == 0, $"directive words not reserved: [{string.Join(", ", missing)}]");
    }

    /// <summary>"All of the exception-names specified in 14.6.13.1 are reserved in the context of compiler
    /// directives": the level-1, level-2 and level-3 names of the catalog, and the open EC-USER / EC-IMP families.</summary>
    [Theory]
    [InlineData("EC-ALL")]
    [InlineData("EC-SIZE")]
    [InlineData("EC-SIZE-TRUNCATION")]
    [InlineData("EC-I-O-AT-END")]
    [InlineData("ec-user-anything")]
    [InlineData("EC-IMP-SOMETHING")]
    public void ExceptionNames_AreReserved(string name) => Assert.True(CompilerDirectiveWords.IsReserved(name));

    [Theory]
    [InlineData("XMODE")]
    [InlineData("DEBUG-FLAG")]
    [InlineData("MYVAR")]
    [InlineData("EC-NOT-A-CONDITION")]
    public void OrdinaryNames_AreNot(string name) => Assert.False(CompilerDirectiveWords.IsReserved(name));

    [Theory]
    [InlineData("off")]
    [InlineData("Listing")]
    [InlineData("IMP")]
    public void Lookup_IsCaseInsensitive(string name) => Assert.True(CompilerDirectiveWords.IsReserved(name));
}
