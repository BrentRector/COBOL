// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime.Collation;
using Xunit;
using Xunit.Abstractions;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The CLDR-derived locale tables against the host's ICU (what .NET's culture-specific <see cref="CompareInfo"/>
/// exposes) — the external oracle for the DERIVATION of a tailoring, on a per-locale word corpus. ICU implements
/// the same CLDR rules (an older CLDR release on most hosts), so the two must agree on every pair; a disagreement
/// is either a builder defect or a documented CLDR change between the host's release and the pinned one, and the
/// test names the pair. Skipped (silently) on a host in globalization-invariant mode, where CompareInfo is ordinal.
/// </summary>
public sealed class CldrIcuCrossCheckTests(ITestOutputHelper output)
{
    private static bool HostHasIcu()
    {
        // The invariant culture: ordinal (globalization-invariant mode) puts "A" before "a"; ICU puts "a" first.
        return CultureInfo.InvariantCulture.CompareInfo.Compare("a", "A", CompareOptions.None) < 0;
    }

    public static IEnumerable<object[]> Locales()
    {
        yield return ["es-ES", "es", new[] { "nino", "niño", "ñu", "nu", "nz", "o", "n", "ñ", "Ñ", "N", "año", "ano", "anos", "años", "cañon", "canon", "canton", "Ñandú", "NANDU", "NAÑDU", "a", "b", "z", "á", "é", "ch", "ll", "ci", "cj", "lk", "lm" }];
        yield return ["da-DK", "da", new[] { "a", "A", "b", "B", "z", "Z", "æ", "Æ", "ø", "Ø", "å", "Å", "aa", "Aa", "AA", "ab", "ä", "Ä", "ö", "ő", "ü", "y", "yz", "þ", "th", "ti", "đ", "d", "ð", "e", "Åse", "Aase", "aase", "Ase", "ase", "æble", "zebra", "øl", "Øl" }];
        yield return ["sv-SE", "sv", new[] { "a", "z", "å", "ä", "ö", "æ", "ø", "ő", "œ", "ü", "y", "yz", "þ", "th", "ti", "v", "w", "va", "wa", "vb", "đ", "d", "ð", "e", "Åke", "Ake", "ärlig", "arlig", "öl", "ol", "zebra" }];
        yield return ["fr-CA", "fr-CA", new[] { "cote", "coté", "côte", "côté", "a", "b", "e", "é", "è", "ê", "pêche", "péché", "pêché", "peche", "élève", "eleve", "élevé", "resume", "résumé", "Résumé" }];
        yield return ["cs-CZ", "cs", new[] { "h", "ch", "i", "c", "č", "d", "r", "ř", "s", "š", "t", "z", "ž", "hyena", "chemie", "cyklus", "čas", "Ch", "CH", "cH", "chata", "hana", "ia" }];
        yield return ["hu-HU", "hu", new[] { "c", "cs", "d", "dz", "dzs", "e", "g", "gy", "h", "l", "ly", "m", "n", "ny", "o", "ö", "ő", "p", "s", "sz", "t", "ty", "u", "ü", "ű", "v", "z", "zs", "ccs", "cscs", "csa", "cza", "cs" + "z", "gyula", "gulya", "zsák", "zab" }];
        yield return ["vi-VN", "vi", new[] { "a", "à", "ả", "ã", "á", "ạ", "ă", "ằ", "ẳ", "ẵ", "ắ", "ặ", "â", "ầ", "ẩ", "ẫ", "ấ", "ậ", "b", "d", "đ", "e", "ê", "o", "ô", "ơ", "u", "ư", "v", "ba", "bà", "bả", "bã", "bá", "bạ" }];
        yield return ["ru-RU", "ru", new[] { "\U00000430", "\U00000431", "\U0000044F", "a", "b", "z", "1", "\U000003B1", "\U00000410", "\U00000451", "\U00000435" }];
        yield return ["hr-HR", "hr", new[] { "c", "č", "ć", "d", "dž", "đ", "e", "l", "lj", "m", "n", "nj", "o", "s", "š", "t", "z", "ž", "\U00000430", "\U000003B1", "a", "džep", "dzip", "ljubav", "lubav", "njiva", "niva" }];
        yield return ["th-TH", "th", new[] { "\U00000E01", "\U00000E40\U00000E01", "\U00000E02", "\U00000E40", "\U00000E01\U00000E32", "a", "b", "1", "\U00000E01\U00000E31\U00000E19" }];
        yield return ["ar-SA", "ar", new[] { "\U00000627", "\U00000628", "\U0000062A", "a", "b", "1", "\U00000623", "\U00000622", "\U00000629" }];
        yield return ["he-IL", "he", new[] { "\U000005D0", "\U000005D1", "\U000005D2", "a", "b", "1", "\U000005EA" }];
        yield return ["tr-TR", "tr", new[] { "c", "ç", "d", "g", "ğ", "h", "ı", "i", "İ", "I", "j", "o", "ö", "p", "s", "ş", "t", "u", "ü", "v", "ırmak", "irmak", "İrmak", "Irmak" }];
        yield return ["pl-PL", "pl", new[] { "a", "ą", "b", "c", "ć", "d", "e", "ę", "f", "l", "ł", "m", "n", "ń", "o", "ó", "p", "s", "ś", "t", "z", "ź", "ż", "łódź", "lodz", "zab", "źab", "żab" }];
        yield return ["lt-LT", "lt", new[] { "c", "č", "d", "i", "y", "j", "s", "š", "t", "z", "ž", "ą", "a", "b" }];
        yield return ["fi-FI", "fi", new[] { "a", "z", "å", "ä", "ö", "v", "w", "va", "wa", "vb", "žemė", "zemė", "š", "s", "t" }];
        yield return ["is-IS", "is", new[] { "a", "á", "b", "d", "ð", "e", "é", "i", "í", "o", "ó", "u", "ú", "y", "ý", "z", "þ", "æ", "ö", "ø", "aa", "b" }];
        yield return ["sk-SK", "sk", new[] { "a", "ä", "b", "c", "č", "d", "ď", "dz", "dž", "e", "h", "ch", "i", "l", "ĺ", "ľ", "o", "ô", "r", "ŕ", "s", "š", "t", "ť", "z", "ž", "chata", "hata", "ia" }];
        yield return ["ro-RO", "ro", new[] { "a", "ă", "â", "b", "i", "î", "j", "s", "ş", "ș", "t", "ţ", "ț", "u", "z" }];
        yield return ["nb-NO", "nb", new[] { "a", "z", "æ", "ø", "å", "aa", "ä", "ö", "ü", "y", "b", "Åse", "Aase", "ø", "Ø" }];
        yield return ["et-EE", "et", new[] { "a", "s", "š", "z", "ž", "t", "u", "v", "w", "õ", "ä", "ö", "ü", "x", "y", "b" }];
        yield return ["lv-LV", "lv", new[] { "a", "ā", "b", "c", "č", "d", "e", "ē", "g", "ģ", "i", "ī", "y", "j", "k", "ķ", "l", "ļ", "n", "ņ", "o", "s", "š", "u", "ū", "z", "ž" }];
        yield return ["sl-SI", "sl", new[] { "c", "č", "ć", "d", "đ", "e", "s", "š", "t", "z", "ž", "a", "b" }];
        yield return ["uk-UA", "uk", new[] { "\U00000430", "\U00000431", "\U00000433", "\U00000491", "\U00000434", "\U00000454", "\U00000435", "\U00000456", "\U00000438", "\U00000457", "\U00000439", "a" }];
        yield return ["el-GR", "el", new[] { "\U000003B1", "\U000003B2", "\U000003C9", "a", "b", "1", "\U00000391" }];
        yield return ["ja-JP", "ja", new[] { "\U00003042", "\U00003044", "\U000030A2", "\U000030A4", "\U00003041", "\U000030A1", "a", "\U00004E00", "\U0000304B", "\U0000304C", "\U000030AB", "\U000030AC", "\U00003042\U000030FC", "\U00003042\U00003042",
            // kb/Work PB2761: the =-chain after an extension (ja.xml: <<<う|ゝ=ぅ|ゝ=ゔ|ゝ=う|ゞ/゙=ぅ|ゞ/゙=ゔ|ゞ/゙).
            "\U00003045\U0000309E", "\U00003045\U00003094", "\U00003094\U0000309E", "\U00003094\U00003094", "\U00003046\U0000309E", "\U00003046\U00003094" }];
        yield return ["ko-KR", "ko", new[] { "\U0000AC00", "\U0000AC01", "\U0000AC02", "\U0000B098", "a", "\U00004E00", "\U00001100", "\U00003131" }];
        yield return ["zh-CN", "zh", new[] { "\U00004E00", "\U00004E01", "\U00004E03", "\U00004E07", "\U00004E2D", "\U00006587", "\U00004E2D\U00006587", "\U00006587\U00004E2D", "a", "b", "1", "\U00005B57" }];
        yield return ["de-DE", "de", new[] { "a", "ä", "ae", "af", "o", "ö", "oe", "u", "ü", "ue", "ß", "ss", "st", "Müller", "Mueller", "Muller", "z" }];
    }

    /// <summary>Pairs on which the PINNED release (48-2) and older CLDR releases the host's ICU may carry differ —
    /// each verified against the two releases' rule files. Latvian: CLDR 42 had <c>&amp;I&lt;&lt;y&lt;&lt;&lt;Y</c> (y a
    /// secondary variant of i), release-48-2 has <c>&amp;I&lt;y&lt;&lt;&lt;Y&lt;ī&lt;&lt;&lt;Ī</c> (y and ī primary letters after i).</summary>
    private static readonly Dictionary<string, HashSet<(string, string)>> KnownReleaseDifferences = new()
    {
        ["lv"] = [("ī", "y")],
    };

    [Theory]
    [MemberData(nameof(Locales))]
    public void Locale_AgreesWithTheHostIcu(string culture, string tag, string[] corpus)
    {
        if (!HostHasIcu()) return;
        CompareInfo icu;
        try { icu = CultureInfo.GetCultureInfo(culture).CompareInfo; }
        catch (CultureNotFoundException) { return; }
        var ours = CollationEngine.ForLocale(tag);
        var known = KnownReleaseDifferences.TryGetValue(tag, out var k) ? k : [];
        var disagreements = new List<string>();
        for (int i = 0; i < corpus.Length; i++)
            for (int j = i + 1; j < corpus.Length; j++)
            {
                int mine = Math.Sign(ours.Compare(corpus[i], corpus[j]));
                int theirs = Math.Sign(icu.Compare(corpus[i], corpus[j], CompareOptions.None));
                if (mine != theirs && !known.Contains((corpus[i], corpus[j])) && !known.Contains((corpus[j], corpus[i])))
                    disagreements.Add($"{Show(corpus[i])} vs {Show(corpus[j])}: ours {mine}, ICU {theirs}");
            }
        foreach (string d in disagreements) output.WriteLine(d);
        Assert.True(disagreements.Count == 0, $"{tag}: {disagreements.Count} disagreement(s) with the host ICU ({culture}):\n" + string.Join("\n", disagreements.Take(30)));
    }

    private static string Show(string s) => s.Any(c => c > 0x7F) ? $"{s} ({string.Join(" ", s.EnumerateRunes().Select(r => $"U+{r.Value:X4}"))})" : s;
}
