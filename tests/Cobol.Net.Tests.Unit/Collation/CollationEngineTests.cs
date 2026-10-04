// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime.Collation;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The collation ENGINE (Runtime/Collation/Collator.cs, CollationKey.cs, CollationEngine.cs): the multi-level order
/// over the derived table — primary/secondary/tertiary/quaternary strengths, shifted vs non-ignorable variables,
/// expansions, contractions, canonical equivalence, Hangul, implicit weights — the agreement between the streaming
/// comparison and the materialized key, and a cross-check of the root order against the host's ICU (the CLDR root
/// order .NET exposes) on a Latin/Greek/Cyrillic corpus.
/// </summary>
[Collection("process-globals")]   // reads the process-global locale configuration (the site tailoring directory variable a
                                  // CobolLocaleTests / LocaleManagerTests case points at a temp directory it then deletes; kb/Work PB126)
public sealed class CollationEngineTests
{
    private static readonly Collator Root = CollationEngine.Root;
    private static readonly Collator Standard = CollationEngine.Standard;

    private static void Less(Collator c, string a, string b)
    {
        Assert.True(c.Compare(a, b) < 0, $"expected {Show(a)} < {Show(b)} under {c.Table.Name}/{c.Strength}/{c.Alternate}");
        Assert.True(c.Compare(b, a) > 0, $"expected {Show(b)} > {Show(a)} (antisymmetry)");
        Assert.True(c.GetKey(a).CompareTo(c.GetKey(b)) < 0, $"key order disagrees with Compare for {Show(a)} < {Show(b)}");
    }

    private static void Same(Collator c, string a, string b)
    {
        Assert.True(c.Compare(a, b) == 0, $"expected {Show(a)} == {Show(b)} under {c.Table.Name}/{c.Strength}/{c.Alternate}");
        Assert.True(c.GetKey(a).CompareTo(c.GetKey(b)) == 0, $"key equality disagrees with Compare for {Show(a)} == {Show(b)}");
    }

    private static string Show(string s) => string.Join(" ", s.EnumerateRunes().Select(r => $"U+{r.Value:X4}"));

    // ---- the root order (CLDR default: tertiary, non-ignorable) --------------------------------------------------

    /// <summary>The owner's requested cases (2026-08-18), each with the design's answer stated: ASCII order; the
    /// case question ("Z" vs "a" — letters outrank case, so "a" &lt; "Z"; the native code-unit order would say the
    /// opposite); case at tertiary; accents at secondary ("a" vs "á", "e" vs "é"); multi-character strings; equality,
    /// less-than, greater-than through <see cref="CollationEngine.Compare(string?,string?)"/>.</summary>
    [Fact]
    public void Requested_BasicOrdering_Equality_LessThan_GreaterThan()
    {
        Assert.True(CollationEngine.Compare("A", "B") < 0);
        Assert.True(CollationEngine.Compare("a", "b") < 0);
        Assert.True(CollationEngine.Compare("a", "Z") < 0);      // letters before case: a < Z (code-unit order says Z < a)
        Assert.True(CollationEngine.Compare("Z", "a") > 0);
        Assert.True(CollationEngine.Compare("a", "A") < 0);      // case is TERTIARY: lowercase first
        Assert.True(CollationEngine.Compare("A", "a") > 0);
        Assert.True(CollationEngine.Compare("a", "\U000000E1") < 0);   // accent is SECONDARY: a < á
        Assert.True(CollationEngine.Compare("e", "\U000000E9") < 0);   // e < é
        Assert.True(CollationEngine.Compare("\U000000E9", "e") > 0);
        Assert.True(CollationEngine.Compare("abc", "abd") < 0);
        Assert.True(CollationEngine.Compare("abd", "abc") > 0);
        Assert.True(CollationEngine.Compare("\U000000E1b", "aa") > 0);   // primaries decide first: (a,b) > (a,a), the accent never counts
        Assert.True(CollationEngine.Compare("aa", "\U000000E1b") < 0);
        Assert.Equal(0, CollationEngine.Compare("abc", "abc"));
        Assert.Equal(0, CollationEngine.Compare("", ""));
        Assert.True(CollationEngine.Compare("abc", "abd") < 0);
        Assert.True(CollationEngine.Compare("abd", "abc") > 0);
    }

    /// <summary>Locale tailoring: the shipped Spanish tailoring (ñ a letter after n) and a synthetic tailoring
    /// placing ä after z (the Swedish/Finnish treatment) applied through <see cref="TailoringRules"/>.</summary>
    [Fact]
    public void Requested_LocaleTailoring_ChangesTheOrder()
    {
        var es = CollationEngine.ForLocale("es-ES");
        Assert.True(es.Compare("\U000000F1", "n") > 0);              // ñ after n
        Assert.True(es.Compare("\U000000F1", "o") < 0);              // …and before o
        Assert.True(es.Compare("\U000000F1u", "nz") > 0);            // a LETTER, not n + tilde
        Assert.True(Root.Compare("\U000000F1u", "nz") < 0);          // the root order: n + tilde
        // ä after z: a synthetic tailoring in the .tailor format — ä (and Ä) take a primary between z and the next root primary.
        var t = CollationTable.Root;
        int afterZ = t.Lookup('z').Primary + 1;
        var rules = TailoringRules.Parse(new StringReader(
            $"@locale xx-SV\nU+00E4 {afterZ:X} 0020 0002\nU+00C4 {afterZ:X} 0020 0008\n"), "xx-SV.tailor");
        var sv = new Collator(rules.Apply(t));
        Assert.True(sv.Compare("\U000000E4", "z") > 0);              // ä after z
        Assert.True(sv.Compare("\U000000E4", "a") > 0);
        Assert.True(sv.Compare("z\U000000E4", "zz") > 0);
        Assert.True(Root.Compare("\U000000E4", "b") < 0);            // the root order: a + diaeresis, before b
        Assert.True(sv.Compare("\U000000E4", "\U000000C4") < 0);     // ä < Ä at the tertiary level
        Assert.True(sv.Compare("a", "b") < 0);                       // untouched letters keep the root order
    }

    [Fact]
    public void Root_BasicLatinOrder()
    {
        Less(Root, "a", "b");
        Less(Root, "a", "A");            // lowercase before uppercase — a TERTIARY difference
        Less(Root, "A", "b");            // …but case never outranks the letter
        Less(Root, "apple", "Apple");
        Less(Root, "Apple", "banana");
        Less(Root, "", "a");
        Less(Root, "ab", "abc");         // a proper prefix is less
        Less(Root, "1", "a");            // digits before letters
        Less(Root, "0", "1");
        Less(Root, " ", "0");            // space (variable) before digits under non-ignorable
        Less(Root, "a b", "ab");         // the space carries its own (low) primary
        Less(Root, "a-b", "ab");         // the hyphen too
        Less(Root, "a\U000000A0b", "a-b");                     // NO-BREAK SPACE: the space primary (0209) < hyphen (020D)
        Less(Root, "a b", "a\U000000A0b");                    // …and a TERTIARY variant of the space (001B vs 0002)
        Same(Root, "abc", "abc");
        Same(Root, "", "");
        Assert.Equal(0, CollationEngine.Compare("x", "x"));
        Assert.True(CollationEngine.Compare("x", "y") < 0);
    }

    [Fact]
    public void Root_AccentsAreSecondary_CaseIsTertiary()
    {
        Less(Root, "a", "á");            // secondary difference
        Less(Root, "á", "b");            // never outranks the letter
        Less(Root, "e", "é");
        Less(Root, "é", "f");
        Less(Root, "cote", "coté");      // French forward secondaries
        Less(Root, "coté", "côte");
        Less(Root, "côte", "côté");
        Less(Root, "a", "à");
        Less(Root, "á", "à");            // acute (0024) before grave (0025) — the data-driven check below pins it
    }

    [Fact]
    public void Root_SecondaryOrderFollowsTheData()
    {
        // 00E1 á = a + [.0000.0024.0002]; 00E0 à = a + [.0000.0025.0002] -> á < à at the secondary level.
        var t = CollationTable.Root;
        int acute = t.GetElements(0x00E1).Span[1].Secondary, grave = t.GetElements(0x00E0).Span[1].Secondary;
        Assert.True(acute < grave);
        Less(Root, "á", "à");
    }

    [Fact]
    public void Strengths_CollapseTheLowerLevels()
    {
        var primary = Root.With(strength: CollationStrength.Primary);
        var secondary = Root.With(strength: CollationStrength.Secondary);
        Same(primary, "a", "A");
        Same(primary, "a", "á");
        Same(primary, "resume", "Résumé");
        Less(primary, "a", "b");
        Same(secondary, "a", "A");
        Less(secondary, "a", "á");
        Same(secondary, "á", "Á");
        Less(Root, "á", "Á");
    }

    [Fact]
    public void Expansions_SharpS_And_Ae()
    {
        // ß expands to s + (ligature secondary) + s: equal to "ss" at level 1, greater at level 2.
        Same(Root.With(strength: CollationStrength.Primary), "ß", "ss");
        Less(Root, "ss", "ß");
        Less(Root, "ß", "st");
        // æ expands to a + (ligature secondary) + e: equal to "ae" at level 1, greater at level 2.
        Same(Root.With(strength: CollationStrength.Primary), "æ", "ae");
        Less(Root, "ae", "æ");
        Less(Root, "æ", "af");
    }

    [Fact]
    public void CanonicalEquivalence_PrecomposedAndDecomposedAreEqual()
    {
        Same(Root, "é", "e\U00000301");                          // U+00E9 vs e + COMBINING ACUTE
        Same(Root, "Å", "A\U0000030A");                          // U+00C5 vs A + COMBINING RING ABOVE
        Same(Root, "ệ", "e\U00000323\U00000302");                    // U+1EC7 vs e + dot below + circumflex (NFD order)
        Same(Root, "ệ", "ê\U00000323");                          // U+00EA + dot below — needs reordering (dot 220 < circumflex 230)
        Same(Root, "e\U00000302\U00000323", "e\U00000323\U00000302");        // the marks in either order
        Same(Root, "각", "각");               // Hangul syllable vs its jamo
        Same(Root, "e\U00000301", "é");
        // Under Identical strength the NFD sequences still tie only when identical.
        var identical = Root.With(strength: CollationStrength.Identical);
        Same(identical, "é", "e\U00000301");                     // both NFD to the same sequence
        Less(identical, "a", "a\0");                     // U+0000 is completely ignorable at levels 1–4; the code point sequence decides
    }

    [Fact]
    public void Contractions_ThaiPrevowelReorders()
    {
        // 0E40 0E01 ; [.<KO KAI>.0020.0002][.<SARA E>.0020.0002] — the prevowel SARA E + KO KAI collates as KO KAI, SARA E.
        var key = Root.GetKey("เก");
        var t = CollationTable.Root;
        Assert.Equal(new[] { t.Lookup(0x0E01).Primary, t.Lookup(0x0E40).Primary }, key.Primary);
        // …so it sorts after KO KAI alone and before KO KAI + a following consonant that outranks SARA E.
        Less(Root, "ก", "เก");
        // Without the following consonant, SARA E keeps its own place.
        Assert.Equal(new[] { t.Lookup(0x0E40).Primary }, Root.GetKey("เ").Primary);
    }

    [Fact]
    public void Hangul_AndHan_Order()
    {
        Less(Root, "가", "각");                                 // LV before LVT
        Less(Root, "각", "간");
        Less(Root, "z", "一");                            // Han (implicit) after every Latin letter
        Less(Root, "一", "丁");                       // code point order within core Han
        Less(Root, "鿿", "㐀");                       // core Han (FB40) before Extension A (FB80)
        Less(Root, "一", "\U00000378");                       // Han before unassigned (FBC0)
        Less(Root, "\U00000378", "\U0000FFFF");                       // everything before U+FFFF
        Less(Root, "\U0000FFFE", "\0a");                      // U+FFFE is the lowest non-ignorable primary
        Less(Root, "\U00020000", "\U00020001");               // supplementary Han, code point order
        Less(Root, "a", "\U0001D400");                        // MATHEMATICAL BOLD CAPITAL A: an explicit tertiary variant of A
        Same(Root.With(strength: CollationStrength.Primary), "A", "\U0001D400");
    }

    [Fact]
    public void Shifted_VariablesIgnoredThroughLevel3_WeighedAtLevel4()
    {
        // The ISO/IEC 14651-style default: "a-b" and "ab" are equal through level 3 and differ at level 4.
        Same(CollationEngine.StandardAtLevel(3), "a-b", "ab");
        Same(CollationEngine.StandardAtLevel(3), "a b", "ab");
        Same(CollationEngine.StandardAtLevel(1), "di Silva", "diSilva");
        Less(Standard, "a-b", "ab");                          // level 4: the hyphen's position weight < "no variable"
        Less(Standard, "a b", "a-b");                         // space (0209) < hyphen at level 4
        Less(Standard, "ab", "ac");                           // primaries still decide first
        Less(Standard, "a-b", "ac");                          // …before any level-4 consideration ("a-b" ≈ "ab" < "ac")
        Less(Standard, "ab", "a-c");                          // and "a-c" ≈ "ac" > "ab" — the hyphen never rescues it
        Assert.Equal(0, Standard.Compare("", ""));
        // A primary-ignorable (accent) FOLLOWING a variable is dropped everywhere (UTS #10 Table 12).
        Same(Standard, "a-\U00000301b", "a-b");
        Less(Standard, "a-b", "a\U00000301-b");                   // …but an accent on a letter still counts at level 2
        // Non-ignorable keeps the hyphen as a low primary.
        Less(Root, "a-b", "ab");                                // the hyphen keeps its own (low) primary
    }

    [Fact]
    public void QuaternaryStrength_UnderNonIgnorable_BehavesAsTertiary()
    {
        var q = Root.With(strength: CollationStrength.Quaternary);
        Same(q, "a\0", "a");                              // completely ignorable stays ignorable
        Less(q, "a", "A");
        Assert.Equal(3, q.GetKey("a").LevelCount);
        Assert.Equal(4, Standard.GetKey("a").LevelCount);
    }

    [Fact]
    public void Keys_LevelsAndByteImage()
    {
        var k = Root.GetKey("Ab");
        var t = CollationTable.Root;
        Assert.Equal(new[] { t.Lookup('a').Primary, t.Lookup('b').Primary }, k.Primary);
        Assert.Equal(new[] { 0x20, 0x20 }, k.Secondary);
        Assert.Equal(new[] { 0x08, 0x02 }, k.Tertiary);
        Assert.Empty(k.Quaternary);
        Assert.Equal(k.Primary, k.Level(1).ToArray());
        // The byte image orders like CompareTo through the last weight level.
        var words = new[] { "b", "a", "ab", "A", "aa", "" };
        var byKey = words.OrderBy(w => Root.GetKey(w)).ToArray();
        var byBytes = words.OrderBy(w => Root.GetKey(w).ToByteArray(), Comparer<byte[]>.Create((x, y) => x.AsSpan().SequenceCompareTo(y))).ToArray();
        Assert.Equal(byKey, byBytes);
        Assert.Equal(new[] { "", "a", "A", "aa", "ab", "b" }, byKey);
        // Keys from different collators refuse to compare.
        Assert.Throws<ArgumentException>(() => Root.GetKey("a").CompareTo(Standard.GetKey("a")));
        Assert.Equal(CollationKey.Build("x"), Root.GetKey("x"));
    }

    [Fact]
    public void IllFormedInput_OrdersDeterministically()
    {
        Assert.False(CollationEngine.IsWellFormed("\uD800"));
        Assert.False(CollationEngine.IsWellFormed("a\uDC00b"));
        Assert.True(CollationEngine.IsWellFormed("a\U0001F600b"));
        // No exception, antisymmetric, and the same answer twice.
        int c1 = Root.Compare("\uD800", "a"), c2 = Root.Compare("a", "\uD800");
        Assert.True(c1 > 0 && c2 < 0);                        // an unpaired surrogate takes an implicit weight above letters
        Assert.Equal(c1, Root.Compare("\uD800", "a"));
    }

    /// <summary>The streaming comparison and the materialized keys are ONE order: over every pair of a mixed corpus
    /// (Latin, accents, ligatures, punctuation, digits, Greek, Cyrillic, Thai contraction, Hangul, Han, supplementary,
    /// empty), sign(Compare) == sign(key.CompareTo) under four representative collators.</summary>
    [Fact]
    public void Compare_And_Keys_Agree_OverACorpus()
    {
        var corpus = Corpus();
        foreach (var c in new[] { Root, Standard, Root.With(strength: CollationStrength.Primary), Root.With(strength: CollationStrength.Identical, alternate: AlternateHandling.Shifted) })
        {
            var keys = corpus.Select(c.GetKey).ToArray();
            for (int i = 0; i < corpus.Length; i++)
                for (int j = 0; j < corpus.Length; j++)
                {
                    int viaCompare = Math.Sign(c.Compare(corpus[i], corpus[j]));
                    int viaKeys = Math.Sign(keys[i].CompareTo(keys[j]));
                    Assert.True(viaCompare == viaKeys, $"{Show(corpus[i])} vs {Show(corpus[j])} under {c.Strength}/{c.Alternate}: Compare {viaCompare}, keys {viaKeys}");
                    Assert.Equal(-viaCompare, Math.Sign(c.Compare(corpus[j], corpus[i])));
                }
        }
    }

    /// <summary>The derived root order agrees with the host's ICU root collation (what .NET's invariant CompareInfo
    /// exposes) on a Latin-1 / Greek / Cyrillic word corpus — the external oracle for the table's DERIVATION. Skipped
    /// (with a reason) on a host running in globalization-invariant mode, where CompareInfo is ordinal, not UCA.</summary>
    [Fact]
    public void Root_AgreesWithTheHostIcuRootCollation_OnACorpus()
    {
        var icu = CultureInfo.InvariantCulture.CompareInfo;
        if (icu.Compare("a", "A", CompareOptions.None) >= 0)
            return;   // ordinal (invariant-globalization) host — nothing to cross-check against
        var corpus = IcuCorpus();
        var disagreements = new List<string>();
        for (int i = 0; i < corpus.Length; i++)
            for (int j = i + 1; j < corpus.Length; j++)
            {
                int ours = Math.Sign(Root.Compare(corpus[i], corpus[j]));
                int theirs = Math.Sign(icu.Compare(corpus[i], corpus[j], CompareOptions.None));
                if (ours != theirs) disagreements.Add($"{corpus[i]} vs {corpus[j]}: ours {ours}, ICU {theirs}");
            }
        Assert.True(disagreements.Count == 0, $"{disagreements.Count} disagreement(s):\n" + string.Join("\n", disagreements.Take(40)));
    }

    private static string[] Corpus() =>
    [
        "", "a", "A", "b", "B", "z", "Z", "aa", "ab", "Ab", "aB", "abc", "á", "à", "â", "ä", "Á", "æ", "ae", "ß", "ss", "st",
        "é", "e\U00000301", "ệ", "ê\U00000323", "e\U00000323\U00000302", "cote", "coté", "côte", "côté", "resume", "résumé", "Résumé",
        "0", "1", "9", "10", "2", "01", " ", "  ", "a b", "a-b", "ab", "a\0", "a\U000000A0b", "di Silva", "diSilva", "a.b", "a,b",
        "α", "β", "Α", "ω", "а", "б", "Я", "я", "ก", "เก", "เ", "가", "각", "간", "각",
        "一", "丁", "㐀", "鿿", "\U00000378", "\U0000FFFE", "\U0000FFFF", "\U00020000", "\U0001D400", "\uD800", "a\uD800",
        "München", "Munchen", "Muenchen", "Zürich", "zurich", "naïve", "NAIVE", "Ærø", "øre", "ore", "þ", "th",
    ];

    private static string[] IcuCorpus() =>
    [
        "a", "A", "b", "B", "z", "Z", "aa", "ab", "Ab", "aB", "abc", "á", "à", "â", "ä", "Á", "æ", "ae", "ß", "ss", "st",
        "é", "cote", "coté", "côte", "côté", "resume", "résumé", "Résumé", "0", "1", "9", "10", "2", "01",
        "α", "β", "Α", "ω", "а", "б", "Я", "я", "München", "Munchen", "Muenchen", "Zürich", "zurich", "naïve", "NAIVE",
        "Ærø", "øre", "ore", "þ", "th", "apple", "Apple", "banana", "cherry", "Éclair", "eclair", "Ñandu", "nandu", "ñandu",
        "ola", "olá", "ölà", "Øst", "ost", "Łódź", "lodz", "Ǆ", "dz", "ĳ", "ij", "ﬁ", "fi", "Ω", "ω", "ǅ",
    ];
}
