// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cldr;
using Xunit;
using Xunit.Abstractions;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The CLDR locale loader and the tailoring builder (Runtime/Collation/CLDR/, kb/Work PB105): the embedded pack
/// loads and EVERY file in it parses; the rule syntax is read faithfully; the builder turns the rules into tables
/// whose ORDER is the locale's — Spanish ñ, German phonebook umlauts, Danish caseFirst and æ/ø/å, Swedish, Canadian
/// French backwards secondaries, Czech ch, Hungarian digraphs, Vietnamese tone marks, the script reorderings of
/// Russian / Croatian / Arabic / Hebrew, Chinese pinyin, Japanese, Korean, POSIX — each pinned by a comparison a
/// user of that locale would recognize; keys and comparisons agree; the hand-derived es .tailor orders exactly like
/// the CLDR derivation (the drift test that keeps the two sources honest); the BCP 47 -u- keys resolve.
/// </summary>
public sealed class CldrLocaleLoaderTests(ITestOutputHelper output)
{
    private static Collator L(string tag) => CollationEngine.ForLocale(tag);

    private static void Less(Collator c, string a, string b)
    {
        Assert.True(c.Compare(a, b) < 0, $"expected {a} < {b} under {c.Table.Name} ({c.Options})");
        Assert.True(c.Compare(b, a) > 0, $"expected {b} > {a} under {c.Table.Name}");
        Assert.True(c.GetKey(a).CompareTo(c.GetKey(b)) < 0, $"keys disagree with Compare for {a} < {b} under {c.Table.Name}");
    }

    private static void Same(Collator c, string a, string b) =>
        Assert.True(c.Compare(a, b) == 0 && c.GetKey(a).CompareTo(c.GetKey(b)) == 0, $"expected {a} == {b} under {c.Table.Name}");

    // ---- the pack and the parser ---------------------------------------------------------------------------------

    [Fact]
    public void Pack_Loads_AndEveryFileParses()
    {
        Assert.Equal("release-48-2", CldrLocaleLoader.PackRelease);
        Assert.True(CldrLocaleLoader.PackLocales.Count >= 130, $"only {CldrLocaleLoader.PackLocales.Count} files in the pack");
        Assert.Contains("root", CldrLocaleLoader.PackLocales);
        Assert.Contains("zh_Hant", CldrLocaleLoader.PackLocales);
        int collations = 0, rules = 0, unsupported = 0;
        foreach (string name in CldrLocaleLoader.PackLocales)
        {
            var data = CldrLocaleLoader.LoadExact(name);
            Assert.NotNull(data);
            foreach (var c in data!.Collations)
            {
                collations++;
                rules += c.RuleCount;
                unsupported += c.Unsupported.Count;
                foreach (string u in c.Unsupported) output.WriteLine($"{name}/{c.Type}: {u}");
            }
        }
        output.WriteLine($"{CldrLocaleLoader.PackLocales.Count} files, {collations} collations, {rules} rules, {unsupported} unsupported");
        Assert.True(collations > 150 && rules > 30_000, $"{collations} collations / {rules} rules");
        Assert.Equal(0, unsupported);   // release-48-2: every setting and construct is recognized
    }

    [Fact]
    public void Root_HasStandardSearchEmoji_AndDefaultType()
    {
        var root = CldrLocaleLoader.Root;
        Assert.Equal("root", root.Tag);
        Assert.Equal("standard", root.EffectiveDefaultType);
        Assert.NotNull(root.Find("standard"));
        Assert.Equal(0, root.Find("standard")!.RuleCount);
        var search = root.Find("search")!;
        Assert.True(search.RuleCount > 10);
        Assert.NotNull(search.Settings.SuppressContractions);
        Assert.Contains(0x0E40, search.Settings.SuppressContractions!);   // Thai SARA E
        Assert.NotNull(root.Find("emoji"));
    }

    [Fact]
    public void Parser_ReadsTheRuleSyntax()
    {
        var es = CldrLocaleLoader.Load("es").Find("standard")!;
        Assert.Equal(3, es.RuleCount);
        Assert.Equal("&N", es.Rules[0].ToString());
        Assert.Equal("<n\U00000303", es.Rules[1].ToString());     // es.xml spells ñ DECOMPOSED (n + COMBINING TILDE)
        Assert.Equal("<<<N\U00000303", es.Rules[2].ToString());

        var da = CldrLocaleLoader.Load("da").Find("standard")!;
        Assert.Equal(CaseFirst.Upper, da.Settings.CaseFirst);
        Assert.Contains(da.Rules, r => r is CldrReset { BeforeLevel: 1, Text: "ǀ" });
        Assert.Contains(da.Rules, r => r is CldrRelation { Strength: CldrRelationStrength.Tertiary, Text: "aa" });

        Assert.True(CldrLocaleLoader.Load("fr-CA").Find("standard")!.Settings.BackwardsSecondary);
        Assert.Equal(new[] { "Latn", "Cyrl" }, CldrLocaleLoader.Load("hr").Find("standard")!.Settings.Reorder);
        Assert.Equal(new[] { "others", "digit" }, CldrLocaleLoader.Load("cs").Find("digits-after")!.Settings.Reorder);

        var sv = CldrLocaleLoader.Load("sv").Find("standard")!;
        Assert.Contains(sv.Rules, r => r is CldrRelation { Strength: CldrRelationStrength.Tertiary, Text: "þ", Extension: "h" });

        var de = CldrLocaleLoader.Load("de");
        Assert.Null(de.Find("standard"));                       // "standard is the same as in root"
        Assert.NotNull(de.Find("phonebook"));
        Assert.NotNull(de.Find("phonebk"));                     // the -u-co- alias
        var search = de.Find("search")!;
        Assert.Equal(2, search.Imports.Count);
        Assert.Equal(new CldrImport("root", "search"), search.Imports[0]);
        Assert.Equal(new CldrImport("de", "phonebook"), search.Imports[1]);

        // Starred relations with ranges: the POSIX order.
        var posix = CldrLocaleLoader.Load("en-US-POSIX").Find("standard")!;
        Assert.True(posix.RuleCount > 90, $"POSIX rules: {posix.RuleCount}");
        Assert.Contains(posix.Rules, r => r is CldrRelation { Text: " " });
        Assert.Contains(posix.Rules, r => r is CldrRelation { Text: "/" });

        // Prefix contexts and quaternary relations: Japanese (in the private-kana collation its standard imports).
        var ja = CldrLocaleLoader.Load("ja");
        Assert.Contains(ja.Find("standard")!.Imports, i => i.Type == "private-kana");
        var kana = ja.Find("private-kana")!;
        Assert.Contains(kana.Rules, r => r is CldrRelation { Prefix: not null });
        Assert.Contains(kana.Rules, r => r is CldrRelation { Strength: CldrRelationStrength.Quaternary });
    }

    [Fact]
    public void Parser_JsonMirror_AndUnicodeSets()
    {
        var data = CldrParser.ParseJson("""
            { "locale": "xx-YY", "collations": { "defaultCollation": "standard",
              "standard": { "rules": "&N<ñ<<<Ñ # like Spanish" },
              "search": "[import und-u-co-search]" } }
            """, "test.json");
        Assert.Equal("xx-YY", data.Tag);
        Assert.Equal("xx", data.Language);
        Assert.Equal("YY", data.Territory);
        Assert.Equal(3, data.Find("standard")!.RuleCount);
        Assert.Single(data.Find("search")!.Imports);
        Assert.Equal(new[] { 0x41, 0x42, 0x43, 0x61, 0x1E00, 0x1E01 }, CldrParser.ParseUnicodeSet(@"[A-C aḀ-ḁ]"));
    }

    [Fact]
    public void Tag_ParsesTheUnicodeExtension()
    {
        var t = CldrLocaleTag.Parse("de_at-u-co-phonebk-ka-shifted-kf-upper-ks-level2-kv-space-kb-true");
        Assert.Equal("de-AT", t.BaseTag);
        Assert.Equal("phonebook", t.CollationType);
        Assert.Equal(AlternateHandling.Shifted, t.Settings.Alternate);
        Assert.Equal(CaseFirst.Upper, t.Settings.CaseFirst);
        Assert.Equal(CollationStrength.Secondary, t.Settings.Strength);
        Assert.Equal(MaxVariable.Space, t.Settings.MaxVariable);
        Assert.True(t.Settings.BackwardsSecondary);
        Assert.Empty(t.Unsupported);
        Assert.Equal(new[] { "sr_Latn_RS", "sr_Latn", "sr", "root" }, CldrLocaleLoader.Chain("sr-Latn-RS"));   // truncation (the nonlikelyScript rule is main-component only)
        Assert.Equal(new[] { "zh_Hant_TW", "zh_Hant", "zh", "root" }, CldrLocaleLoader.Chain("zh-Hant-TW"));
        Assert.Equal(new[] { "yue", "zh_Hant", "zh", "root" }, CldrLocaleLoader.Chain("yue"));                // component="collations": yue → zh_Hant
        Assert.Equal(new[] { "nb", "no", "root" }, CldrLocaleLoader.Chain("nb"));                            // parentLocales: nb → no
        Assert.Equal(new[] { "de_AT", "de", "root" }, CldrLocaleLoader.Chain("de-AT"));
        Assert.Equal("no", CldrLocaleLoader.ResolveCollation("nb").Found!.Tag);                              // nb.xml is empty; no.xml has the rules
        Assert.Equal("root", CldrLocaleTag.FileName("und"));
        var kn = CldrLocaleTag.Parse("en-u-kn-true");
        Assert.Single(kn.Unsupported);
        var sel = CldrLocaleLoader.ResolveCollation("de-AT-u-co-phonebk");
        Assert.Equal("de-AT", sel.Found!.Tag);            // de_AT.xml defines its own phonebook
        Assert.Equal("phonebook", sel.Type);
        var zhHant = CldrLocaleLoader.ResolveCollation("zh-Hant");
        Assert.Equal("stroke", zhHant.Type);              // zh_Hant.xml: <defaultCollation>stroke</defaultCollation>
        var missing = CldrLocaleLoader.ResolveCollation("en-u-co-phonebk");
        Assert.Equal("standard", missing.Type);           // no phonebook for en anywhere in the chain: the default
        Assert.Contains(missing.Unsupported, u => u.Contains("phonebook"));
    }

    // ---- the builder: locale orders ------------------------------------------------------------------------------

    [Fact]
    public void Spanish_Enye_AfterN_AndTheHandTailoringAgrees()
    {
        var es = L("es");
        Less(es, "n", "ñ");
        Less(es, "ñ", "o");
        Less(es, "nz", "ñu");                     // a letter, not n + tilde
        Less(es, "ñ", "Ñ");                       // tertiary
        Same(es.With(strength: CollationStrength.Primary), "ñ", "Ñ");
        Same(es, "ñ", "n\U00000303");             // canonical closure: the decomposed spelling collates the same
        Less(es, "a", "b");
        // The hand-derived es-ES.tailor (Collation/Tailoring/) is a SITE override layered on the CLDR derivation; it
        // must order a Spanish corpus exactly like the CLDR-only derivation.
        var resolved = CollationEngine.ResolveLocale("es-ES");
        Assert.NotNull(resolved.Tailoring);
        Assert.NotNull(resolved.Cldr.Collation);
        var cldrOnly = CldrTailoringBuilder.Build(CldrLocaleLoader.ResolveCollation("es"), "es-cldr-only");
        var a = CollationEngine.For(cldrOnly.Table, cldrOnly.Options);
        var b = resolved.Collator;
        string[] corpus = ["nino", "niño", "nino", "ñandu", "nandu", "ñu", "nu", "nz", "o", "N", "Ñ", "n", "ñ", "n\U00000303", "año", "ano", "anos", "años", "cañon", "canon", "canton", "Ñandú", "NANDU", "NAÑDU"];
        for (int i = 0; i < corpus.Length; i++)
            for (int j = 0; j < corpus.Length; j++)
                Assert.True(Math.Sign(a.Compare(corpus[i], corpus[j])) == Math.Sign(b.Compare(corpus[i], corpus[j])),
                    $"CLDR-only vs CLDR+.tailor disagree on {corpus[i]} vs {corpus[j]}");
    }

    [Fact]
    public void GermanPhonebook_UmlautsAsVowelPlusE()
    {
        var de = L("de");                          // "standard is the same as in root"
        Assert.False(CollationEngine.ResolveLocale("de").IsTailored);
        var phone = L("de-u-co-phonebk");
        Assert.True(CollationEngine.ResolveLocale("de-u-co-phonebk").IsTailored);
        Same(phone.With(strength: CollationStrength.Primary), "ä", "ae");
        Less(phone, "ae", "ä");                    // a secondary difference
        Less(phone, "ä", "af");
        Same(phone.With(strength: CollationStrength.Primary), "Müller", "Mueller");   // ü ≈ ue at level 1 …
        Less(phone, "Mueller", "Müller");                                             // … and after it at level 2
        Assert.NotEqual(0, de.With(strength: CollationStrength.Primary).Compare("Müller", "Mueller"));   // root: ü is u + diaeresis
        Less(phone, "Mue", "Mü");                  // phonebook: Mü right after Mue
        Less(de, "Mü", "Mue");                     // root: Mu < Mue at level 1 (a shorter primary sequence)
    }

    [Fact]
    public void Danish_CaseFirstUpper_AndAeOslashAring_AfterZ()
    {
        var da = L("da");
        Assert.Equal(CaseFirst.Upper, da.Options.CaseFirst);
        Less(da, "A", "a");                        // upper first
        Less(da, "a", "b");
        Less(da, "z", "æ");
        Less(da, "æ", "ø");
        Less(da, "ø", "å");
        Less(da, "Æ", "æ");
        Same(da.With(strength: CollationStrength.Secondary), "å", "aa");   // aa is a tertiary variant of å
        Less(da, "å", "aa");
        Less(da, "Aa", "aa");                      // upper first among the tertiary variants
        Less(da, "ä", "ø");                        // ä = secondary variant of æ
        Less(da, "æ", "ä");
        Less(da, "th", "þ");                       // þ tertiary after th
        Less(da, "d", "đ");
        Same(da.With(strength: CollationStrength.Primary), "d", "đ");
        // The root order, for contrast.
        Less(CollationEngine.Root, "a", "A");
        Less(CollationEngine.Root, "æ", "b");
    }

    [Fact]
    public void Swedish_AAO_AfterZ_And_ThornAsTh()
    {
        var sv = L("sv");
        Less(sv, "z", "å");
        Less(sv, "å", "ä");
        Less(sv, "ä", "ö");
        Less(sv, "æ", "ö");                        // æ = secondary variant of ä
        Same(sv.With(strength: CollationStrength.Primary), "þ", "th");
        Less(sv, "th", "þ");
        Less(sv, "v", "w");                        // standard keeps v and w apart
        Same(L("sv-u-co-trad").With(strength: CollationStrength.Primary), "v", "w");     // traditional: w is a secondary variant of v
        Less(L("sv-u-co-trad"), "v", "w");
    }

    [Fact]
    public void CanadianFrench_BackwardsSecondaries()
    {
        var fr = L("fr");
        var ca = L("fr-CA");
        Assert.False(fr.Options.BackwardsSecondary);
        Assert.True(ca.Options.BackwardsSecondary);
        // Forward (French, root): cote < coté < côte < côté.  Backwards (Canada): cote < côte < coté < côté.
        Less(fr, "cote", "coté"); Less(fr, "coté", "côte"); Less(fr, "côte", "côté");
        Less(ca, "cote", "côte"); Less(ca, "côte", "coté"); Less(ca, "coté", "côté");
        Same(ca, "côté", "co\U00000302te\U00000301");   // canonical equivalence unaffected
        Less(ca, "a", "b");
    }

    [Fact]
    public void ScriptReordering_Russian_Croatian_Arabic_Hebrew()
    {
        var root = CollationEngine.Root;
        Less(root, "a", "\U000003B1"); Less(root, "\U000003B1", "\U00000430");           // root: Latin < Greek < Cyrillic
        var ru = L("ru");
        Less(ru, "\U00000430", "a");                                    // Cyrillic before Latin
        Less(ru, "\U00000430", "\U000003B1");
        Less(ru, "1", "\U00000430");                                    // digits still first
        Less(ru, " ", "\U00000430");
        Less(ru, "\U00000430", "\U00000431");                                    // Cyrillic order itself unchanged
        Less(ru, "\U0000044F", "a");
        var hr = L("hr");
        Less(hr, "z", "\U00000430");                                    // Latin, then Cyrillic …
        Less(hr, "\U00000430", "\U000003B1");                                    // … before Greek
        Less(hr, "c", "č"); Less(hr, "č", "ć"); Less(hr, "ć", "d");
        Less(hr, "d", "dž"); Less(hr, "dž", "đ"); Less(hr, "đ", "e");
        var ar = L("ar");
        Less(ar, "\U00000627", "a");
        var he = L("he");
        Less(he, "\U000005D0", "a");
        // Reordering moves whole tiles: within Cyrillic and within Latin nothing changes.
        Less(ru, "a", "b"); Less(hr, "\U00000430", "\U00000431");
    }

    [Fact]
    public void Czech_Hungarian_Vietnamese_Contractions_Expansions_Marks()
    {
        var cs = L("cs");
        Less(cs, "h", "ch"); Less(cs, "ch", "i");
        Less(cs, "hyena", "chemie");                           // ch is a letter after h
        Less(CollationEngine.Root, "chemie", "hyena");
        Less(cs, "c", "č"); Less(cs, "č", "d");
        Less(cs, "r", "ř"); Less(cs, "ř", "s"); Less(cs, "s", "š"); Less(cs, "š", "t");
        Less(cs, "ch", "cH"); Less(cs, "cH", "Ch"); Less(cs, "Ch", "CH");
        var hu = L("hu");
        Less(hu, "c", "cs"); Less(hu, "cs", "d");
        Less(hu, "d", "dz"); Less(hu, "dz", "dzs"); Less(hu, "dzs", "e");
        Less(hu, "z", "zs");
        Same(hu.With(strength: CollationStrength.Primary), "ccs", "cscs");   // ccs = cs + cs (a tertiary variant with an expansion)
        Less(hu, "cscs", "ccs");
        Less(hu, "o", "ö"); Less(hu, "ö", "ő"); Less(hu, "ő", "p");
        var vi = L("vi");
        // Tone marks reordered: grave < hook above < tilde < acute < dot below (root: acute < grave).
        Less(vi, "à", "ả"); Less(vi, "ả", "ã"); Less(vi, "ã", "á"); Less(vi, "á", "ạ");
        Less(CollationEngine.Root, "á", "à");
        Less(vi, "a", "ă"); Less(vi, "ă", "â"); Less(vi, "â", "b");
        Less(vi, "d", "đ"); Less(vi, "đ", "e");
        Less(vi, "u", "ư"); Less(vi, "ư", "v");
    }

    /// <summary>A collator built from a rules text alone (a one-collation file "zz", standard type) — the builder in
    /// isolation, for rule shapes no embedded locale selects.</summary>
    private static Collator FromRules(string rules)
    {
        var built = BuildRules(rules);
        Assert.Empty(built.Unsupported);
        return new Collator(built.Table, built.Options);
    }

    private static CldrBuildResult BuildRules(string rules)
    {
        string xml = "<ldml><identity><language type=\"zz\"/></identity><collations><collation type=\"standard\"><cr><![CDATA["
            + rules + "]]></cr></collation></collations></ldml>";
        var data = CldrParser.ParseXml(xml, "test rules");
        var selection = new CldrCollationSelection(CldrLocaleTag.Parse("zz"), data, data, data.Find("standard"), "standard", []);
        return CldrTailoringBuilder.Build(selection, "zz");
    }

    /// <summary>kb/Work PB2772: an <c>[import]</c> names EXACTLY a collation type. A type the imported locale's chain
    /// lacks imports nothing and is reported ("no such collation; nothing imported"); it is never replaced by the
    /// locale's default collation, which spliced es/standard in silently for a misspelt es-u-co-tradtional.</summary>
    [Fact]
    public void Import_OfAMissingType_ImportsNothing_AndIsReported()
    {
        var misspelt = BuildRules("[import es-u-co-tradtional]");
        Assert.Contains(misspelt.Unsupported, u => u.Contains("[import es-u-co-tradtional]") && u.Contains("no such collation; nothing imported"));
        var root = new Collator(misspelt.Table, misspelt.Options);
        Less(root, "ña", "nz");                                   // the root order: ñ is n + tilde, a secondary difference
        Less(root, "ch", "cz");
        var spelt = BuildRules("[import es-u-co-traditional]");
        Assert.Empty(spelt.Unsupported);
        var traditional = new Collator(spelt.Table, spelt.Options);
        Less(traditional, "nz", "ña");                            // es: &N<ñ
        Less(traditional, "cz", "ch");                            // traditional: &C<ch
        // A locale TAG with an unknown type still falls back to the default type, and says so.
        var tag = CldrLocaleLoader.ResolveCollation("es-u-co-tradtional");
        Assert.Equal("standard", tag.Type);
        Assert.Contains(tag.Unsupported, u => u.Contains("'tradtional'"));
        Assert.Null(CldrLocaleLoader.FindCollation("es", "tradtional"));
        Assert.Equal("traditional", CldrLocaleLoader.FindCollation("es", "traditional")?.Collation.Type);
        Assert.Equal("root", CldrLocaleLoader.FindCollation("zz-Nonexistent", "standard")?.Found.Tag);
    }

    /// <summary>kb/Work PB2761: the extension of <c>x/y</c> is appended to x's elements only; the next relation in
    /// the chain is placed after x's OWN relation elements (UTS #35 Part 5 "Expansions"; ICU's builder restores the
    /// element count it had before the extension). The defect carried y into the position, so every later link of
    /// the chain gained y as an extra element: Finnish traditional Þ became t h h, the Hungarian geminate chain's ccS
    /// became cs cs cs, and an <c>=</c> relation after an extension repeated it.</summary>
    [Fact]
    public void Extension_IsNotCarriedIntoTheNextRelationsPosition()
    {
        // fi.xml (traditional): &T<<þ/h<<<Þ/h — þ = [T'][h], Þ = [T''][h]: two primaries t h each.
        var fi = L("fi-u-co-traditional");
        Less(fi, "þa", "thb"); Less(fi, "Þa", "thb");          // t h a < t h b (the defect gave Þa t h h a)
        Less(fi, "þa", "Þa");                                   // Þ is a tertiary variant of þ
        Less(fi, "tha", "þa");                                  // þ is a secondary variant of t
        // hu.xml's geminate digraph chain (alt="proposed" there, so built from its text): every link is cs + cs.
        var hu = FromRules("&C<cs<<<cS<<<Cs<<<CS &cs<<<ccs/cs<<<ccS/cS<<<cCs/Cs<<<cCS/CS");
        Less(hu, "meccsa", "meccsb"); Less(hu, "meccSa", "meccsb"); Less(hu, "mecCsa", "meccsb"); Less(hu, "mecCSa", "meccsb");
        Less(hu, "meccs", "meccS"); Less(hu, "meccS", "mecCs"); Less(hu, "mecCs", "mecCS");
        Same(hu.With(strength: CollationStrength.Primary), "mecCSa", "mecscsa");
        // An identity relation after an extension copies the position WITHOUT the extension, then adds its own.
        var id = FromRules("&a<x/y=z/y");
        Same(id, "x", "z");
        Less(id, "xa", "xyb");
    }

    [Fact]
    public void Thai_ShiftedAndContractionsKept()
    {
        var th = L("th");
        Assert.Equal(AlternateHandling.Shifted, th.Options.Alternate);
        Less(th, "ก", "เก");                                     // the prevowel contraction still reorders
        Less(th, "ก", "a");                                     // [reorder Thai]: Thai before Latin
        Same(th, "a-b", "ab");                                  // shifted: hyphen ignored through level 3 …
        Assert.NotEqual(0, th.With(strength: CollationStrength.Quaternary).Compare("a-b", "ab"));   // … and weighed at level 4
    }

    [Fact]
    public void Posix_AsciiOrder()
    {
        var posix = L("en-US-POSIX");
        Less(posix, "B", "a");                                  // all capitals before all lowercase
        Less(posix, "Z", "a");
        Less(posix, "A", "B");
        Less(posix, "a", "b");
        Less(posix, " ", "0");
        Less(posix, "0", "A");
        Less(posix, "[", "a");                                  // '['-'`' between the two letter runs
    }

    [Fact]
    public void Chinese_Japanese_Korean_Build()
    {
        var sw = Stopwatch.StartNew();
        var zh = L("zh");
        output.WriteLine($"zh built in {sw.ElapsedMilliseconds} ms: {CollationEngine.ResolveLocale("zh").Cldr}; notes: {string.Join(" | ", CollationEngine.ResolveLocale("zh").Notes)}; unsupported: {string.Join(" | ", CollationEngine.ResolveLocale("zh").Unsupported)}");
        // No wall-clock ceiling (kb/Work PB1590): a loaded CI runner breaches any fixed limit; the build time is
        // reported above for a human to read, and the orderings below are the property under test.
        Less(zh, "丁", "一");                                   // pinyin: dīng < yī (code point order says the reverse)
        Less(zh, "一", "a");                                    // [reorder Hani Bopo]: Han before Latin
        Less(zh, "1", "一");
        Less(CollationEngine.Root, "一", "丁");
        Less(CollationEngine.Root, "a", "一");
        sw.Restart();
        var ja = L("ja");
        output.WriteLine($"ja built in {sw.ElapsedMilliseconds} ms; unsupported: {string.Join(" | ", CollationEngine.ResolveLocale("ja").Unsupported)}");
        Assert.Contains(CollationEngine.ResolveLocale("ja").Unsupported, u => u.Contains("quaternary"));
        Less(ja, "あ", "い");
        Less(ja, "a", "あ");                                    // [reorder Latn Kana Hani]
        sw.Restart();
        var ko = L("ko");
        output.WriteLine($"ko built in {sw.ElapsedMilliseconds} ms");
        Less(ko, "가", "각");
        Less(ko, "각", "一");                                   // [reorder Hang Hani]
    }

    [Fact]
    public void Compare_And_Keys_Agree_ForTailoredLocales()
    {
        string[] corpus = ["a", "A", "á", "à", "ä", "æ", "Æ", "aa", "Aa", "å", "ø", "ö", "z", "Z", "þ", "th", "n", "ñ", "Ñ", "o", "ch", "h", "i", "cs", "d", "dz", "dzs", "e",
            "cote", "coté", "côte", "côté", "1", " ", "-", "a-b", "ab", "\U00000430", "\U00000431", "\U0000044F", "\U000003B1", "\U00000627", "一", "丁", "あ", "가", "", "resume", "résumé", "Résumé"];
        foreach (string tag in new[] { "es", "da", "sv", "fr-CA", "ru", "hr", "cs", "hu", "vi", "th", "de-u-co-phonebk", "en-US-POSIX" })
        {
            var c = L(tag);
            var keys = corpus.Select(c.GetKey).ToArray();
            for (int i = 0; i < corpus.Length; i++)
                for (int j = 0; j < corpus.Length; j++)
                {
                    int cmp = Math.Sign(c.Compare(corpus[i], corpus[j]));
                    Assert.True(cmp == Math.Sign(keys[i].CompareTo(keys[j])), $"{tag}: Compare and keys disagree on '{corpus[i]}' vs '{corpus[j]}'");
                    Assert.Equal(-cmp, Math.Sign(c.Compare(corpus[j], corpus[i])));
                }
        }
    }

    [Fact]
    public void OrderingTableNames_ResolveThroughCldr()
    {
        Assert.True(CollationEngine.TryGetOrderingTable("es", out var es));
        Assert.True(es.MappingCount > 0);
        Assert.True(CollationEngine.TryGetOrderingTable("de-u-co-phonebk", out var phone));
        Assert.NotSame(CollationTable.Root, phone);
        Assert.True(CollationEngine.TryGetOrderingTable("ISO 14651_2020_TABLE1", out var iso));
        Assert.Same(CollationTable.Root, iso);
        Assert.False(CollationEngine.TryGetOrderingTable("qq-XX-nonsense", out _));
        // A table NAME that happens to parse as a locale tag whose CLDR parent chain reaches a real file must stay
        // unknown: "MY_TABLE" → my-TABLE → my.xml (Burmese); "NO-SUCH-TABLE" → no-Such-TABLE → no.xml (Norwegian).
        Assert.False(CollationEngine.TryGetOrderingTable("MY_TABLE", out _));
        Assert.False(CollationEngine.TryGetOrderingTable("NO-SUCH-TABLE", out _));
        Assert.False(CollationEngine.IsKnownLocale("no-Such-TABLE"));
        Assert.True(CollationEngine.IsKnownLocale("nb-NO"));          // a culture .NET recognizes; collates by no.xml
        Assert.True(CollationEngine.IsKnownLocale("es-419"));
        Assert.True(CollationEngine.IsKnownLocale("de-u-co-phonebk"));
        Assert.True(CollationEngine.TryGetOrderingTable("nb_NO", out var nb));
        Assert.True(nb.MappingCount > 0 && CollationEngine.ForLocale("nb-NO").Compare("z", "æ") < 0);
    }
}
