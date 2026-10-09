// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Collation;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// Locale TAILORING (Runtime/Collation/TailoringRules.cs + CollationTable.WithTailoring): the .tailor format, the
/// shipped en-US / fr-FR / es-ES / es files, locale resolution and fallback, canonical closure of a tailored code
/// point, immutability of the base table, and the version guard.
/// </summary>
[Collection("process-globals")]   // kb/Work PB126 (battery #28's find): Assert.Same on CollationEngine's
                                  // per-locale collator cache races the locale tests' ClearCache under parallel
                                  // xUnit — structurally identical collators, different instances.
public sealed class CollationTailoringTests
{
    [Fact]
    public void ShippedTailorings_AreEmbedded()
    {
        var names = TailoringRules.EmbeddedNames().ToArray();
        Assert.Contains("en-US", names);
        Assert.Contains("fr-FR", names);
        Assert.Contains("es-ES", names);
        Assert.Contains("es", names);
    }

    [Fact]
    public void ForLocale_ResolvesExactThenLanguage_ThenRoot()
    {
        Assert.NotNull(TailoringRules.ForLocale("es-ES"));
        Assert.Equal("es-ES", TailoringRules.ForLocale("es-ES")!.Locale);
        Assert.Equal("es-ES", TailoringRules.ForLocale("es_es")!.Locale);      // underscore and case tolerant
        Assert.Equal("es", TailoringRules.ForLocale("es-MX")!.Locale);         // language fallback
        Assert.Equal("es", TailoringRules.ForLocale("es")!.Locale);
        Assert.Empty(TailoringRules.ForLocale("en-US")!.Entries);              // English: the root order, explicitly
        Assert.Empty(TailoringRules.ForLocale("fr-FR")!.Entries);
        Assert.Null(TailoringRules.ForLocale("de"));                            // no file → the root order
        Assert.Null(TailoringRules.ForLocale("xx-YY"));
        Assert.Null(TailoringRules.ForLocale(null));
        Assert.Equal(["es-MX", "es"], TailoringRules.Candidates("es_MX"));
    }

    [Fact]
    public void Spanish_EnyeIsALetterAfterN()
    {
        var root = CollationEngine.Root;
        var es = CollationEngine.ForLocale("es-ES");
        Assert.NotSame(root.Table, es.Table);
        Assert.Equal("es-ES", es.Table.Name);
        // Root: ñ is n + tilde (secondary) — "ñu" sorts before "nz" because n < n·z at level 1.
        Assert.True(root.Compare("ñu", "nz") < 0);
        // Spanish: ñ is its own primary after n — "ñu" sorts after "nz" and before "o".
        Assert.True(es.Compare("ñu", "nz") > 0);
        Assert.True(es.Compare("ñ", "n") > 0);
        Assert.True(es.Compare("ñ", "o") < 0);
        Assert.True(es.Compare("ñ", "Ñ") < 0);                                       // tertiary: lowercase first
        Assert.Equal(0, es.With(strength: CollationStrength.Primary).Compare("ñ", "Ñ"));
        // Everything else is the root order.
        Assert.True(es.Compare("a", "b") < 0);
        Assert.True(es.Compare("é", "f") < 0);
        // The tailored weight sits strictly between n and the next root primary.
        var t = es.Table;
        Assert.True(t.Lookup('ñ').Primary > t.Lookup('n').Primary);
        Assert.True(t.Lookup('ñ').Primary < CollationTable.Root.Lookup(0x0274).Primary);   // LATIN LETTER SMALL CAPITAL N: the next root primary
        Assert.NotNull(t.Tailoring);
        // Locale variants share the language file, and the same collator instance is cached per (locale, strength, alternate).
        Assert.Same(es, CollationEngine.ForLocale("es_ES"));
        Assert.True(CollationEngine.ForLocale("es-MX").Compare("ñu", "nz") > 0);
    }

    /// <summary>A tailored precomposed code point covers its decomposed spelling too (canonical closure on apply):
    /// n + COMBINING TILDE collates as ñ under Spanish, so the two spellings stay equal after the tailoring.</summary>
    [Fact]
    public void Spanish_CanonicalClosure_CoversTheDecomposedSpelling()
    {
        var es = CollationEngine.ForLocale("es");
        Assert.Equal(0, es.Compare("n\U00000303", "ñ"));
        Assert.True(es.Compare("n\U00000303u", "nz") > 0);
        Assert.True(es.Compare("N\U00000303", "nz") > 0);
        Assert.True(es.Table.ContractionCount > CollationTable.Root.ContractionCount);
        // The base table is untouched.
        Assert.True(CollationEngine.Root.Compare("ñu", "nz") < 0);
        Assert.Null(CollationTable.Root.Tailoring);
    }

    /// <summary>kb/Work PB2765: a site .tailor layer is canonically closed over composites, exactly as a CLDR
    /// collation is — tailoring a letter moves every precomposed letter built on it, tailoring a mark moves every
    /// letter carrying it, and a contraction keyed by a precomposed first code point matches its decomposed spelling.
    /// The defect left ç at its root weights after c was moved after d, so ç sorted before d while c + cedilla
    /// sorted after it (the two spellings of one text collating apart, contrary to §8.8.4.2.11's single LC_COLLATE
    /// order).</summary>
    [Fact]
    public void SiteTailoring_IsCanonicallyClosed_OverComposites()
    {
        var t = new Collator(CollationTable.Root.WithTailoring(CanonicalClosureDriftTests.SiteTailoring()), CollationOptions.Default);
        // c after d: ç follows c, spelled either way.
        Assert.True(t.Compare("c", "d") > 0);
        Assert.Equal(0, t.Compare("ç", "ç"));
        Assert.True(t.Compare("ç", "d") > 0);
        Assert.True(t.Compare("ç", "d") > 0);
        Assert.True(CollationEngine.Root.Compare("ç", "d") < 0);   // the root order, for contrast
        // acute after grave at level 2: é now follows è (the root puts it before), spelled either way.
        Assert.True(t.Compare("é", "è") > 0);
        Assert.True(t.Compare("é", "è") > 0);
        Assert.Equal(0, t.Compare("é", "é"));
        Assert.True(CollationEngine.Root.Compare("é", "è") < 0);
        // á + dot below (keyed precomposed) is a letter after z, however it is spelled: its NFD a + dot below + acute,
        // and ạ (a + dot below, precomposed) followed by the acute.
        Assert.True(t.Compare("ạ́", "z") > 0);
        Assert.Equal(0, t.Compare("ạ́", "ạ́"));
        Assert.Equal(0, t.Compare("ạ́", "ạ́"));
        Assert.True(t.Compare("ạ́", "z") > 0);
    }

    [Fact]
    public void Parse_TheFormat()
    {
        const string text = """
            # a comment
            @version 17.0.0
            @locale zz
            U+00F1  25718 0020 0002    # single element, U+ form
            00D1    25718 0020 0008 variable
            U+006E U+0303  25718 0020 0002          # contraction
            U+00E6  [23EC0 0020 0004] [0000 011F 0004] [24530 0020 0004]   # expansion
            U+0041 0x23EC0 0x20 0x8
            """;
        var rules = TailoringRules.Parse(new StringReader(text), "test.tailor");
        Assert.Equal("17.0.0", rules.UcaVersion);
        Assert.Equal("zz", rules.Locale);
        Assert.Equal("zz", rules.Name);
        Assert.Equal(5, rules.Entries.Count);
        Assert.Equal(new CollationElement(0x25718, 0x20, 0x02), rules.Find(0x00F1)!.Elements.Single());
        // Tertiary 0008 is an uppercase weight: the case bit follows the DUCET rule the generator applies (CollationElement.IsUpperTertiary).
        Assert.Equal(new CollationElement(0x25718, 0x20, 0x08, IsVariable: true, Case: ElementCase.Upper), rules.Find(0x00D1)!.Elements.Single());
        Assert.Equal([0x006E, 0x0303], rules.Find(0x006E, 0x0303)!.CodePoints);
        Assert.Equal(3, rules.Find(0x00E6)!.Elements.Length);
        Assert.Equal(0x011F, rules.Find(0x00E6)!.Elements[1].Secondary);
        Assert.Equal(new CollationElement(0x23EC0, 0x20, 0x8, Case: ElementCase.Upper), rules.Find(0x41)!.Elements.Single());
        Assert.True(rules.Defines([0x006E, 0x0303]));
        Assert.False(rules.Defines([0x006E]));
    }

    [Theory]
    [InlineData("U+00F1 25718 0020", "an element is")]                       // too few weights
    [InlineData("U+00F1 25718 0020 0002 0004", "an element is")]             // too many
    [InlineData("U+00F1 xyz 0020 0002", "primary weight")]
    [InlineData("U+110000 0001 0020 0002", "not a code point")]
    [InlineData("006E 0303 25718 0020 0002", "an element is")]               // a contraction without U+ prefixes reads as one code point + 4 tokens
    [InlineData("@strength 3", "unknown directive")]
    [InlineData("U+00F1 [25718 0020 0002", "unterminated")]
    [InlineData("U+00F1 25718 0020 0002\nU+00F1 25718 0020 0003", "duplicate mapping")]
    public void Parse_RejectsMalformedLines_NamingTheLine(string text, string message)
    {
        var ex = Assert.Throws<FormatException>(() => TailoringRules.Parse(new StringReader(text), "bad.tailor"));
        Assert.Contains("bad.tailor(", ex.Message);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void Apply_RefusesAVersionMismatch()
    {
        var rules = TailoringRules.Parse(new StringReader("@version 16.0.0\nU+00F1 25718 0020 0002\n"), "old.tailor");
        var ex = Assert.Throws<InvalidOperationException>(() => rules.Apply(CollationTable.Root));
        Assert.Contains("16.0.0", ex.Message);
        Assert.Contains(CollationTable.Root.UcaVersion, ex.Message);
    }

    [Fact]
    public void Apply_HeaderOnlyRules_AreTheBaseTable()
    {
        var rules = TailoringRules.Parse(new StringReader("@locale en-US\n"), "en-US.tailor");
        Assert.Same(CollationTable.Root, rules.Apply(CollationTable.Root));
        Assert.Same(CollationTable.Root, CollationEngine.TableForLocale("en-US"));
        Assert.Same(CollationEngine.Root, CollationEngine.ForLocale("fr-FR"));
    }

    [Fact]
    public void Load_FromDisk_AndAppliedOverrides_AreIndependentOfTheRoot()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cobolnet-collation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            // Put 'z' before 'a' (a nonsense tailoring that is easy to observe): z takes a primary just below a's.
            int aPrimary = CollationTable.Root.Lookup('a').Primary;
            string path = Path.Combine(dir, "zz-ZZ.tailor");
            File.WriteAllText(path, $"@locale zz-ZZ\nU+007A {aPrimary - 1:X} 0020 0002\nU+005A {aPrimary - 1:X} 0020 0008\n");
            var rules = TailoringRules.Load(path);
            Assert.Equal("zz-ZZ", rules.Locale);
            var table = rules.Apply(CollationTable.Root);
            var c = new Collator(table);
            Assert.True(c.Compare("z", "a") < 0);
            Assert.True(c.Compare("Z", "a") < 0);
            Assert.True(c.Compare("z", "Z") < 0);
            Assert.True(CollationEngine.Root.Compare("z", "a") > 0);   // the root is untouched
            Assert.Equal(CollationTable.Root.MappingCount, table.MappingCount);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void OrderingTableNames_ResolveTheStandardsDefault_AndLocales()
    {
        Assert.True(CollationEngine.TryGetOrderingTable("ISO 14651_2020_TABLE1", out var t1));
        Assert.Same(CollationTable.Root, t1);
        Assert.True(CollationEngine.TryGetOrderingTable("iso_14651_2020_table1", out _));
        Assert.True(CollationEngine.TryGetOrderingTable("ISO  14651 2020 TABLE1", out _));
        Assert.True(CollationEngine.TryGetOrderingTable("es-ES", out var es));
        Assert.Equal("es-ES", es.Name);
        Assert.True(CollationEngine.TryGetOrderingTable("de-DE", out var de));   // a known locale, root order
        Assert.Same(CollationTable.Root, de);
        Assert.False(CollationEngine.TryGetOrderingTable("NO SUCH TABLE", out _));
        Assert.False(CollationEngine.TryGetOrderingTable("", out _));
        Assert.True(CollationEngine.IsDefaultOrderingTableName("ISO 14651_2020_TABLE1"));
        Assert.False(CollationEngine.IsDefaultOrderingTableName("ISO 14651_2016_TABLE1"));
    }
}
