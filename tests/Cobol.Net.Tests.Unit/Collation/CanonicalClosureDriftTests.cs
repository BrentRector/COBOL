// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cldr;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// Every collation table is CANONICALLY CLOSED, whichever front-end built it: each code point with a canonical
/// decomposition collates EQUAL to its NFD at every level but the code point tie-break, in the root table, in a table
/// a CLDR rule set derived, in a table a numeric <c>.tailor</c> file derived, and in a <c>.tailor</c> layered over a
/// CLDR table. <see cref="Collator"/> walks a text holding no combining mark as it is (a precomposed ç is looked up
/// whole) and normalizes one that holds a mark, so a table that is not closed makes two canonically equivalent texts
/// compare apart. The closure is ONE step, <c>CollationTable.Rebuild</c>, that every tailoring goes through
/// (kb/Work PB2765: the .tailor front-end once skipped it, and tailoring c left ç at its root weights).
/// </summary>
public sealed class CanonicalClosureDriftTests
{
    private static readonly CollationOptions AllLevels = CollationOptions.Default with { Strength = CollationStrength.Quaternary };

    /// <summary>The root primary of <paramref name="cp"/> plus one, as a .tailor weight (free room right after it).</summary>
    private static string After(int cp) => (CollationTable.Root.Lookup(cp).Primary + 1).ToString("X");

    /// <summary>A .tailor file that tailors a base letter (c after d), a mark (acute right after grave at level 2,
    /// where the root puts it before), and a contraction keyed by a PRECOMPOSED first code point (á + dot below
    /// after z) — the three shapes kb/Work PB2765 names.</summary>
    internal static TailoringRules SiteTailoring() => TailoringRules.Parse(new StringReader($"""
        @locale zz
        U+0063         {After('d')} 0020 0002
        U+0301         0000 {CollationTable.Root.Lookup(0x0300).Secondary + 1:X} 0002
        U+00E1 U+0323  {After('z')} 0020 0002
        """), "closure.tailor");

    public static IEnumerable<object[]> Tables()
    {
        yield return ["root"];
        foreach (string tag in new[] { "vi", "sv", "da", "cs", "hu", "es", "de-u-co-phonebk", "fr-CA", "ja" })
            yield return ["cldr:" + tag];
        yield return ["tailor"];
        yield return ["cldr:es+tailor"];
        yield return ["cldr:vi+tailor"];
    }

    private static CollationTable Build(string which) => which switch
    {
        "root" => CollationTable.Root,
        "tailor" => CollationTable.Root.WithTailoring(SiteTailoring()),
        _ when which.EndsWith("+tailor", StringComparison.Ordinal) => Build(which[..^"+tailor".Length]).WithTailoring(SiteTailoring()),
        _ => CldrTailoringBuilder.Build(CldrLocaleLoader.ResolveCollation(which["cldr:".Length..]), which).Table,
    };

    [Theory]
    [MemberData(nameof(Tables))]
    public void EveryDecomposableCodePoint_CollatesEqualToItsNfd(string which)
    {
        var table = Build(which);
        var collator = new Collator(table, AllLevels);
        var apart = new List<string>();
        int checkedCount = 0;
        foreach (var (cp, nfd) in table.CanonicalDecompositions())
        {
            checkedCount++;
            string composite = char.ConvertFromUtf32(cp);
            string decomposed = Text(nfd);
            if (collator.Compare(composite, decomposed) != 0) apart.Add($"U+{cp:X4} vs its NFD {string.Join(" ", nfd.Select(c => $"U+{c:X4}"))}");
        }
        Assert.True(checkedCount > 2000, $"{which}: only {checkedCount} decompositions enumerated");
        Assert.True(apart.Count == 0, $"{which}: {apart.Count} precomposed character(s) collate apart from their NFD:\n" + string.Join("\n", apart.Take(30)));
    }

    /// <summary>A tailored precomposed Hangul syllable keeps its canonical equivalence with its jamo spelling (train
    /// 1047 review of kb/Work PB2765): the engine decomposes a syllable to its jamo before the table, so the closure
    /// must not give the jamo spelling alone the syllable's tailored weights.</summary>
    [Fact]
    public void TailoredHangulSyllable_CollatesEqualToItsJamoSpelling()
    {
        var tailoring = TailoringRules.Parse(new StringReader($"""
            @locale zz
            U+AC00         {After('z')} 0020 0002
            U+AC01         {After('z')} 0020 0003
            """), "hangul.tailor");
        var collator = new Collator(CollationTable.Root.WithTailoring(tailoring), AllLevels);
        Assert.Equal(0, collator.Compare("가", "가"));         // 가 = L V
        Assert.Equal(0, collator.Compare("각", "각"));   // 각 = L V T
    }

    private static string Text(int[] codePoints)
    {
        var sb = new StringBuilder();
        foreach (int cp in codePoints) sb.Append(char.ConvertFromUtf32(cp));
        return sb.ToString();
    }
}
