// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ANNEX B AND ANNEX C TABLES ARE THE STANDARD'S, LIST FOR LIST (kb/Work PB1402). The characters of a COBOL word
/// (Annex B.3's three position classes) and the case fold of a COBOL word (Annex C.2) are transcribed by
/// <c>scripts/spec/gen_annex_bc.py</c> into <c>CobolCharacterRepertoire.Table.cs</c> and <c>CobolNames.Table.cs</c>,
/// together with the Annex E lists that reverse them for COBOL 2002/2014 (E.2 items 4 and 14, E.3.3 items 4, 5 and 6).
/// This test re-reads every one of those ten lists from <c>specs/ISO_COBOL.md</c> and requires the tables to equal
/// them, so a transcription repair in the spec reaches the compiler or fails here. It also holds the two facts the
/// fold's implementation relies on: every Annex C mapping keeps a character's UTF-16 width (so a word and its fold
/// line up unit for unit), and every mapping Annex E.3.3 item 6 added at 2023 pairs a character the 2002/2014 Annex B
/// does not hold (so the ONE 2023 comparer is exact for those editions once the lexer has written E.2 item 14's two
/// letters in their spelling).
/// </summary>
public sealed class AnnexBCDriftTests
{
    private static readonly string[] Spec = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));

    private static readonly Regex Range = new(@"(?<![0-9A-Za-z])([0-9A-F]{4,5})(?:-([0-9A-F]{4,5}))?(?![0-9A-Za-z])");
    private static readonly Regex Pair = new(@"\(([0-9A-F]{4,5}),([0-9A-F]{4,5})\)");

    private static IEnumerable<string> Section(string opener, string closer)
    {
        int start = Array.FindIndex(Spec, l => l.StartsWith(opener, StringComparison.Ordinal));
        Assert.True(start >= 0, $"spec marker not found: {opener}");
        int end = Array.FindIndex(Spec, start + 1, l => l.StartsWith(closer, StringComparison.Ordinal));
        Assert.True(end > start, $"spec marker not found after '{opener}': {closer}");
        return Spec[(start + 1)..end];
    }

    private static SortedSet<int> CodePoints(IEnumerable<string> body)
    {
        var set = new SortedSet<int>();
        foreach (string line in body)
        {
            if (line.TrimStart().StartsWith("NOTE", StringComparison.Ordinal)) continue;
            foreach (Match m in Range.Matches(line))
            {
                int lo = Convert.ToInt32(m.Groups[1].Value, 16);
                int hi = m.Groups[2].Success ? Convert.ToInt32(m.Groups[2].Value, 16) : lo;
                for (int cp = lo; cp <= hi; cp++) set.Add(cp);
            }
        }
        return set;
    }

    private static SortedSet<int> CodePoints((int First, int Second)[] ranges)
    {
        var set = new SortedSet<int>();
        foreach (var (lo, hi) in ranges) for (int cp = lo; cp <= hi; cp++) set.Add(cp);
        return set;
    }

    private static List<(int, int)> Pairs(IEnumerable<string> body) =>
        [.. body.SelectMany(l => Pair.Matches(l)).Select(m => (Convert.ToInt32(m.Groups[1].Value, 16), Convert.ToInt32(m.Groups[2].Value, 16)))];

    private const string B1 = @"1\) The following characters are permitted in a user-defined word.";
    private const string B2 = @"2\) The following characters are permitted in a user-defined word except as the start character.";
    private const string B3 = @"3\) The following characters are permitted in a user-defined word except as the start or last character.";
    private const string E24 = @"4\) **Characters permitted in user-defined words.** The following character, represented";
    private const string E24Medial = "   The following character has been changed so it is not allowed for the start or last character";
    private const string E33i4 = @"4\) **Characters permitted in user-defined words.** The following characters have been changed";
    private const string E33i5 = @"5\) **Characters permitted in user-defined words.** The following characters have been added.";
    private const string E33i6 = @"6\) **General case mappings.** The following case mappings have been added.";

    public static TheoryData<string, string, string> RangeLists => new()
    {
        { nameof(CobolCharacterRepertoire.B3Anywhere), B1, B2 },
        { nameof(CobolCharacterRepertoire.B3NotFirst), B2, B3 },
        { nameof(CobolCharacterRepertoire.B3Medial), B3, "<a id=\"section-annex-c\"></a>" },
        { nameof(CobolCharacterRepertoire.E2Item4Deleted), E24, E24Medial },
        { nameof(CobolCharacterRepertoire.E2Item4MadeMedial), E24Medial, "   Justification:" },
        { nameof(CobolCharacterRepertoire.E33Item4MadeFirst), E33i4, E33i5 },
        { nameof(CobolCharacterRepertoire.E33Item5Added), E33i5, E33i6 },
    };

    [Theory]
    [MemberData(nameof(RangeLists))]
    public void AnnexBList_EqualsTheSpec(string field, string opener, string closer)
    {
        var table = (( int First, int Second)[])typeof(CobolCharacterRepertoire)
            .GetField(field, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
        Assert.Equal(CodePoints(Section(opener, closer)), CodePoints(table));
    }

    [Fact]
    public void AnnexCLists_EqualTheSpec()
    {
        Assert.Equal(Pairs(Section("### C.2 General case mappings", "<a id=\"section-annex-d\"></a>")),
            CobolNames.C2Mappings.Select(p => (p.First, p.Second)).ToList());
        Assert.Equal(Pairs(Section(@"14\) **General case mappings.** The following case mappings have been deleted.", "Justification:")),
            CobolNames.E2Item14Deleted.Select(p => (p.First, p.Second)).ToList());
        Assert.Equal(Pairs(Section(E33i6, @"7\) **Clarification** of exception handling procedures.")),
            CobolNames.E33Item6Added.Select(p => (p.First, p.Second)).ToList());
    }

    /// <summary>CobolNames compares a word and its fold unit for unit; that is exact only while no mapping changes a
    /// character's UTF-16 width.</summary>
    [Fact]
    public void EveryAnnexCMapping_KeepsTheCharacterWidth()
    {
        foreach (var (upper, lower) in CobolNames.C2Mappings.Concat(CobolNames.E2Item14Deleted))
            Assert.True(upper > 0xFFFF == lower > 0xFFFF, $"({upper:X4},{lower:X4}) changes width");
    }

    /// <summary>The ONE (2023) comparer serves COBOL 2002/2014 only because no mapping 2023 added can join two words
    /// those editions accept: one side of each is outside their Annex B, so the word is refused before any fold.</summary>
    [Fact]
    public void EveryMappingAddedIn2023_PairsACharacterOutsideThe2014Repertoire()
    {
        foreach (var (upper, lower) in CobolNames.E33Item6Added)
            Assert.True(CobolCharacterRepertoire.PositionOf(upper, 2014) == WordCharacterPosition.NotPermitted
                        || CobolCharacterRepertoire.PositionOf(lower, 2014) == WordCharacterPosition.NotPermitted,
                $"({upper:X4},{lower:X4}) joins two COBOL-2014 characters");
    }
}
