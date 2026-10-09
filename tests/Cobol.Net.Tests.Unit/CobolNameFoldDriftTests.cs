// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A COBOL WORD IS COMPARED BY ONE FOLD, ANNEX C, AND BY NOTHING ELSE (kb/Work PB1402; ISO §8.1.3.2 GR3 b) and
/// GR4 b)). Every name table and every word comparison in the compiler (Cobol.Net.Compiler), the frontend
/// (Cobol.Net.Frontend) and the edition tables (Cobol.Net.Editions, kb/Work PB1965) goes through
/// <see cref="CobolNames"/>; .NET's ordinal-ignore-case comparison may not appear there, and neither may its
/// <c>ToUpperInvariant</c> as a word's key (<see cref="CobolNames.UpperFold"/> is that key), because it upper-cases by the host's case mapping and disagrees with Annex C on the extended letters (it
/// makes final sigma the same letter as sigma and a lowercase Cherokee syllable the same as its capital, and keeps
/// U+0130 and the Kelvin sign apart from i and k). The one exception is a comparison of a string that is not a COBOL
/// word — a file-system path, a command-line option — and that line says so with the marker
/// <c>// not a COBOL word: &lt;what it is&gt;</c>, so the exception is visible where it is made.
/// The fold's own answers, and the Annex B screen's, are pinned by value below.
/// </summary>
public sealed class CobolNameFoldDriftTests
{
    private const string Marker = "// not a COBOL word:";

    /// <summary>The projects whose name tables and comparisons are COBOL words: the compiler, the front end, and the
    /// edition tables (the <c>&gt;&gt;COBOL-WORDS</c> map, the directive catalog, the §8.10 and implementor-name
    /// tables), which reference the runtime for the fold since kb/Work PB1965.</summary>
    private static readonly string[] WordProjects = ["Cobol.Net.Compiler", "Cobol.Net.Frontend", "Cobol.Net.Editions"];

    /// <summary>The host's upper-casing standing as a word's KEY (kb/Work PB1965): a C# identifier derived from a word
    /// (<c>Sanitize(x).ToUpperInvariant()</c>), a word looked up in the <c>&gt;&gt;COBOL-WORDS</c> map
    /// (<c>Resolve(x.ToUpperInvariant())</c>), and an item's default externalized name
    /// (<c>CobolName.ToUpperInvariant()</c>). Each is <see cref="CobolNames.UpperFold"/> (or
    /// <c>DataItem.WordIdentifier</c>) now.</summary>
    private static readonly System.Text.RegularExpressions.Regex HostUpperKey = new(
        @"Sanitize\([^;]*\)\.ToUpperInvariant\(|\bResolve\([^;]*\.ToUpperInvariant\(|\bCobolName!?\.ToUpperInvariant\(",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    [Fact]
    public void NoHostUpperCase_AsTheKeyOfACobolWord()
    {
        var hits = new List<string>();
        foreach (string project in WordProjects)
            foreach (string file in Directory.EnumerateFiles(TestRepo.Src(project), "*.cs", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(TestRepo.Root, file);
                if (rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(p => p is "obj" or "bin" or "Generated")) continue;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    if (!lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal) && HostUpperKey.IsMatch(lines[i]))
                        hits.Add($"{rel}:{i + 1}: {lines[i].Trim()}");
            }
        Assert.True(hits.Count == 0,
            "a COBOL word's upper-case key is CobolNames.UpperFold (a C# identifier: DataItem.WordIdentifier), never "
            + "the host's ToUpperInvariant, which keeps the Kelvin sign apart from K and joins final sigma with sigma:\n"
            + string.Join("\n", hits));
    }

    /// <summary>The one upper-case spelling: Annex C's fold with only the basic letters raised.</summary>
    [Theory]
    [InlineData("kup", "KUP")]
    [InlineData("KUP", "KUP")]   // K is U+212A, the KELVIN SIGN: (212A,006B)
    [InlineData("café", "CAFé")]  // an extended letter stays in its fold
    [InlineData("CAFÉ", "CAFé")]
    [InlineData("Xς", "Xς")]     // final sigma: no mapping at 2023 (E.2 item 14), kept apart from sigma
    [InlineData("Xσ", "Xσ")]
    [InlineData("ıTEM", "ıTEM")]   // dotless i: no mapping at 2023 (E.2 item 14)
    [InlineData("DİX", "DIX")]     // (0130,0069)
    public void UpperFold_IsTheFoldWithTheBasicLettersRaised(string word, string upper)
    {
        Assert.Equal(upper, CobolNames.UpperFold(word));
        Assert.True(CobolNames.Same(word, upper));
    }

    [Fact]
    public void UpperFold_ReturnsAnUpperCaseBasicWordItself()
    {
        const string basic = "WS-TOTAL-1";
        Assert.Same(basic, CobolNames.UpperFold(basic));
    }

    [Fact]
    public void NoOrdinalIgnoreCase_OverACobolWord_InTheCompilerOrTheFrontend()
    {
        var hits = new List<string>();
        foreach (string project in WordProjects)
            foreach (string file in Directory.EnumerateFiles(TestRepo.Src(project), "*.cs", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(TestRepo.Root, file);
                if (rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(p => p is "obj" or "bin" or "Generated")) continue;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string code = lines[i].TrimStart();
                    if (code.StartsWith("//", StringComparison.Ordinal)) continue;   // prose, not a comparison
                    if (lines[i].Contains("OrdinalIgnoreCase", StringComparison.Ordinal) && !lines[i].Contains(Marker, StringComparison.Ordinal))
                        hits.Add($"{rel}:{i + 1}: {code}");
                }
            }
        Assert.True(hits.Count == 0,
            "compare a COBOL word with CobolNames (Comparer / Same / StartsWith / EndsWith / Contains), never with "
            + $"OrdinalIgnoreCase; a string that is not a COBOL word carries '{Marker} <what>':\n" + string.Join("\n", hits));
    }

    [Theory]
    [InlineData("CAFÉ", "café", true)]          // (00C9,00E9)
    [InlineData("GRÜẞE", "grüße", true)]        // (1E9E,00DF) — .NET's case-insensitive comparison keeps them apart
    [InlineData("XK", "xk", true)]         // KELVIN SIGN (212A,006B)
    [InlineData("DİX", "dix", true)]       // (0130,0069)
    [InlineData("Xς", "Xσ", false)]   // final sigma: (03C2,03C3) deleted at 2023 (E.2 item 14)
    [InlineData("ıTEM", "ITEM", false)]    // dotless i: (0131,0069) deleted at 2023 (E.2 item 14)
    [InlineData("XᎠ", "Xꭰ", false)]   // Cherokee: not folded (G.2 item 2)
    [InlineData("ABC", "abcd", false)]
    public void Same_IsTheAnnexCFold(string a, string b, bool same)
    {
        Assert.Equal(same, CobolNames.Same(a, b));
        Assert.Equal(same, CobolNames.Comparer.Equals(a, b));
        if (same) Assert.Equal(CobolNames.Comparer.GetHashCode(a), CobolNames.Comparer.GetHashCode(b));
        Assert.Equal(same, CobolNames.Fold(a) == CobolNames.Fold(b));
    }

    [Fact]
    public void SpanOperations_FoldLikeSame()
    {
        Assert.True(CobolNames.StartsWith("ÄRGER-1", "ärger"));
        Assert.True(CobolNames.EndsWith("ZÄHLER", "hler"));
        Assert.True(CobolNames.Contains("A.MÖVE", ".möve"));
        Assert.False(CobolNames.StartsWith("AB", "abc"));
    }

    /// <summary>Below 2023 the lexer writes E.2 item 14's two letters in their edition's fold, so the one comparer
    /// gives that edition's answer; at 2023 and for a basic word the spelling is the word itself.</summary>
    [Fact]
    public void EditionSpelling_AppliesTheDeletedMappingsBelow2023()
    {
        Assert.Equal("iTEM", CobolNames.EditionSpelling("ıTEM", 2014));
        Assert.Equal("Xσ", CobolNames.EditionSpelling("Xς", 2002));
        Assert.Equal("ıTEM", CobolNames.EditionSpelling("ıTEM", 2023));
        const string basic = "WS-TOTAL";
        Assert.Same(basic, CobolNames.EditionSpelling(basic, 2014));
    }

    [Theory]
    [InlineData("CAFÉ", 2023, null)]
    [InlineData("A€", 2023, "U+20AC, which is not a character")]           // EURO SIGN: not in Annex B
    [InlineData("́AB", 2023, "U+0301 as its first character")]             // B.3 item 2 first
    [InlineData("CAFÉ", 2023, null)]                                      // a combining mark may be last
    [InlineData("AB・", 2023, "U+30FB as its last character")]               // B.3 item 3 last (E.2 item 4)
    [InlineData("AB・", 2014, null)]                                        // anywhere before 2023
    [InlineData("A・B", 2023, null)]
    [InlineData("ͺX", 2014, null)]                                         // deleted at 2023 (E.2 item 4)
    [InlineData("ͺX", 2023, "U+037A, which is not a character")]
    [InlineData("KX", 2023, null)]                                         // first from 2023 (E.3.3 item 4)
    [InlineData("KX", 2014, "U+212A as its first character")]
    public void Violation_IsTheEditionsAnnexB(string word, int edition, string? expected)
    {
        string? v = CobolCharacterRepertoire.Violation(word, edition);
        if (expected is null) Assert.Null(v);
        else Assert.Contains(expected, v);
    }

    /// <summary>A supplementary-plane pair (Adlam, added at 2023 by E.3.3 items 5 and 6) — built in the body, because
    /// a test-case NAME holding a surrogate pair is listed and reported in two spellings, which breaks the gate's
    /// population check.</summary>
    [Fact]
    public void SupplementaryPlaneLetters_FoldAndScreen()
    {
        string upper = char.ConvertFromUtf32(0x1E900) + char.ConvertFromUtf32(0x1E901);
        string lower = char.ConvertFromUtf32(0x1E922) + char.ConvertFromUtf32(0x1E923);
        Assert.True(CobolNames.Same(upper, lower));
        Assert.Equal(CobolNames.Comparer.GetHashCode(upper), CobolNames.Comparer.GetHashCode(lower));
        Assert.Null(CobolCharacterRepertoire.Violation(upper, 2023));
        Assert.Contains("U+1E900, which is not a character", CobolCharacterRepertoire.Violation(upper, 2014));
    }

    /// <summary>§8.3.2.1's 63 characters are counted per character: a supplementary letter is one, a combining
    /// character is one of its own (§8.1.3.2 GR4 c).</summary>
    [Fact]
    public void WordLength_CountsCharacters_NotUtf16Units()
    {
        string adlam63 = string.Concat(Enumerable.Repeat("\U0001E900", 63));
        Assert.Equal(63, CobolCharacterRepertoire.LengthInCharacters(adlam63));
        Assert.Null(CobolWordRule.LengthViolation(adlam63, 2023));
        Assert.NotNull(CobolWordRule.LengthViolation(adlam63 + "\U0001E900", 2023));
        Assert.Equal(5, CobolCharacterRepertoire.LengthInCharacters("CAFÉ"));
    }
}
