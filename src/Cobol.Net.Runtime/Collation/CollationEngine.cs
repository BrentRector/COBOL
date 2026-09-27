// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Globalization;
using CobolNet.Runtime.Collation.Cldr;

namespace CobolNet.Runtime.Collation;

/// <summary>
/// Everything the engine resolved for one locale tag: the tailored table, the settings, and where each came from —
/// the CLDR collation (<see cref="Cldr"/>: which file, which type, the <c>-u-</c> keys) that
/// <see cref="CldrTailoringBuilder"/> applied FIRST, then the site's numeric <c>.tailor</c> layer
/// (<see cref="Tailoring"/>) on top. <see cref="Unsupported"/> lists what the tag or its CLDR collation asked for
/// that the engine does not honor (empty for the vast majority of locales).
/// </summary>
public sealed record ResolvedLocaleCollation(string Tag, CollationTable Table, CollationOptions Options,
    CldrCollationSelection Cldr, TailoringRules? Tailoring, IReadOnlyList<string> Unsupported, IReadOnlyList<string> Notes)
{
    /// <summary>The cached collator for the resolution's own settings.</summary>
    public Collator Collator => CollationEngine.For(Table, Options);

    /// <summary>True when the table differs from the root's (a CLDR tailoring or a <c>.tailor</c> changed something).</summary>
    public bool IsTailored => !ReferenceEquals(Table, CollationTable.Root);
}

/// <summary>
/// The static entry point of WiseOwl COBOL's collation subsystem — the configurations a COBOL program reaches, each a
/// cached, thread-safe <see cref="Collator"/> over the derived <see cref="CollationTable"/>:
/// <list type="bullet">
/// <item><see cref="Compare(string?,string?)"/> / <see cref="Root"/> — the CLDR root order (tertiary, non-ignorable),
/// the order ICU gives an untailored locale.</item>
/// <item><see cref="Standard"/> — the ISO/IEC 14651-style four-level ordering STANDARD-COMPARE's default table
/// "ISO 14651_2020_TABLE1" names: variable characters ignored through level 3 and weighted at level 4
/// (<see cref="AlternateHandling.Shifted"/>, <see cref="CollationStrength.Quaternary"/>); <see cref="Standard"/>
/// with an explicit level for its ordering-level argument.</item>
/// <item><see cref="ForLocale"/> / <see cref="ResolveLocale"/> — a locale's collation: the CLDR collation of the
/// tag (its file along the parent chain, its <c>-u-co-</c> type and <c>-u-</c> settings — <see cref="CldrLocaleLoader"/>,
/// <see cref="CldrTailoringBuilder"/>) over the root table, then the site's numeric <see cref="TailoringRules"/>
/// (<c>.tailor</c>) layered on top; the root order itself for a locale whose CLDR order IS the root order.</item>
/// <item><see cref="TryGetOrderingTable"/> — the resolution of an <c>ORDER TABLE … IS literal</c> name.</item>
/// </list>
/// ⚖ Conformance statement, verbatim (owner decision Q4, 2026-08-18): "Implements collation behavior consistent
/// with ISO/IEC 14651 through derived tables and CLDR/UCA data."
/// </summary>
public static class CollationEngine
{
    /// <summary>The name of the default ordering table STANDARD-COMPARE uses (ISO/IEC 1989:2023 §15.85.3 r5) — the
    /// spelling the standard gives; matched case-insensitively with the space and the underscore interchangeable.</summary>
    public const string DefaultOrderingTableName = "ISO 14651_2020_TABLE1";

    private static readonly Lazy<Collator> s_standard = new(
        () => new Collator(CollationTable.Root, CollationStrength.Quaternary, AlternateHandling.Shifted),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly ConcurrentDictionary<string, ResolvedLocaleCollation> s_locales = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<(CollationTable Table, CollationOptions Options), Collator> s_collators = new();

    /// <summary>The CLDR root default — root table, tertiary strength, non-ignorable variables.</summary>
    public static Collator Root => Collator.Root;

    /// <summary>The ISO/IEC 14651-style default: root table, all four levels, variables shifted to level 4.</summary>
    public static Collator Standard => s_standard.Value;

    /// <summary>Compare two texts under the CLDR root default: &lt;0, 0, &gt;0.</summary>
    public static int Compare(string? a, string? b) => Root.Compare(a, b);

    /// <summary>Compare two texts under the root table at an explicit strength and alternate handling.</summary>
    public static int Compare(string? a, string? b, CollationStrength strength, AlternateHandling alternate = AlternateHandling.NonIgnorable) =>
        Cached(CollationTable.Root, strength, alternate).Compare(a, b);

    /// <summary>The 14651-style ordering at ordering level <paramref name="level"/> (1–4; STANDARD-COMPARE's
    /// argument-4): the levels above <paramref name="level"/> do not distinguish.</summary>
    public static Collator StandardAtLevel(int level)
    {
        if (level is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(level), level, "ordering level must be 1..4");
        return Cached(CollationTable.Root, (CollationStrength)level, AlternateHandling.Shifted);
    }

    /// <summary>The materialized key of <paramref name="text"/> under a locale's collation (null/empty/"root" = the
    /// root order), through that collator's <see cref="Cache.CollationKeyCache"/> — build once, compare many times.</summary>
    public static CollationKey GetKey(string? text, string? localeTag = null) =>
        Cache.CollationKeyCache.For(ForLocale(localeTag)).GetKey(text);

    /// <summary>The cached collator for a table and full settings.</summary>
    public static Collator For(CollationTable table, CollationOptions options)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(options);
        return options.IsPlain ? Cached(table, options.Strength, options.Alternate)
            : s_collators.GetOrAdd((table, options), static k => new Collator(k.Table, k.Options));
    }

    /// <summary>The collator of a locale ("es-ES", "fr_FR", "en", "de-u-co-phonebk", …): the locale's CLDR collation
    /// plus its <c>.tailor</c> layer (<see cref="ResolveLocale"/>) at the locale's own settings, or at the given
    /// strength / alternate handling where stated. Null/empty/"root" is the root order. Cached.</summary>
    public static Collator ForLocale(string? localeTag, CollationStrength? strength = null, AlternateHandling? alternate = null)
    {
        var r = ResolveLocale(localeTag);
        if (strength is null && alternate is null) return r.Collator;
        return For(r.Table, r.Options with { Strength = strength ?? r.Options.Strength, Alternate = alternate ?? r.Options.Alternate });
    }

    /// <summary>The (cached) table of a locale — the root table when the locale has no tailoring.</summary>
    public static CollationTable TableForLocale(string? localeTag) => ResolveLocale(localeTag).Table;

    /// <summary>Resolve (once per tag; cached) what a locale tag means to the engine: the CLDR collation of the tag
    /// (<see cref="CldrLocaleLoader.ResolveCollation"/> → <see cref="CldrTailoringBuilder"/>) over the root table,
    /// then the site's numeric tailoring for the tag or its language (<see cref="TailoringRules.ForLocale"/>) on top,
    /// and the settings the CLDR collation and the tag's <c>-u-</c> keys declare. Null/empty/"root"/"und" is the root
    /// order. A malformed <c>.tailor</c> file is an error, not a silent fallback.</summary>
    public static ResolvedLocaleCollation ResolveLocale(string? localeTag)
    {
        string tag = string.IsNullOrWhiteSpace(localeTag) ? "" : localeTag.Trim().Replace('_', '-');
        if (tag.Equals("root", StringComparison.OrdinalIgnoreCase) || tag.Equals("und", StringComparison.OrdinalIgnoreCase)) tag = "";
        return s_locales.GetOrAdd(tag, static t => Resolve(t));
    }

    private static ResolvedLocaleCollation Resolve(string tag)
    {
        var selection = CldrLocaleLoader.ResolveCollation(tag);
        var built = CldrTailoringBuilder.Build(selection, tag.Length == 0 ? "root" : tag);
        var table = built.Table;
        var rules = tag.Length == 0 ? null : TailoringRules.ForLocale(selection.Tag.BaseTag);
        if (rules is not null) table = table.WithTailoring(rules);
        return new ResolvedLocaleCollation(tag, table, built.Options, selection, rules, built.Unsupported, built.Notes);
    }

    /// <summary>Forget every resolved locale (a test that writes a tailoring or CLDR file at run time). Tables already
    /// handed out stay valid; the next resolution re-reads the sources.</summary>
    public static void ClearLocaleCache()
    {
        s_locales.Clear();
        CldrLocaleLoader.ClearCache();
    }

    /// <summary>Resolve an <c>ORDER TABLE ordering-name IS literal</c> name (§12.3.7 / §15.85): the standard's
    /// default table name (→ the root table), or a locale tag — one with CLDR collation data (its file, or a parent's;
    /// a <c>-u-co-</c> type is honored), one with a site <c>.tailor</c>, or one .NET recognizes whose order is the root
    /// order. Anything else is not a supported ordering table (→ EC-ORDER-NOT-SUPPORTED at the reference, §15.85.4 r2).</summary>
    public static bool TryGetOrderingTable(string? name, out CollationTable table)
    {
        table = CollationTable.Root;
        if (string.IsNullOrWhiteSpace(name)) return false;
        string n = name.Trim();
        if (IsDefaultOrderingTableName(n)) return true;
        string tag = n.Replace('_', '-');
        if (!IsKnownLocale(tag)) return false;
        table = TableForLocale(tag);
        return true;
    }

    /// <summary>Is <paramref name="localeTag"/> a locale this implementation KNOWS — one with a CLDR collation file of
    /// its own (or of its region-less form, since a site may pass "de-AT" where CLDR has "de_AT" and .NET agrees), a
    /// <c>.tailor</c> for the tag or its language, or a culture .NET recognizes? The CLDR parent CHAIN alone does not
    /// make a tag known: "NO SUCH TABLE" parses as the tag <c>no-Such-TABLE</c>, whose chain reaches Norwegian's
    /// file — an <c>ORDER TABLE</c> literal or a LOCALE name that names nothing must stay unknown
    /// (EC-ORDER-NOT-SUPPORTED; an unselectable locale). The ONE rule <see cref="TryGetOrderingTable"/>,
    /// <see cref="Locale.LocaleManager"/> and <see cref="Locale.LocaleConfig"/> share; a <c>-u-</c> extension is
    /// ignored for the test and honored by the resolution. Null/empty/"root" is known (the root order).</summary>
    public static bool IsKnownLocale(string? localeTag)
    {
        if (string.IsNullOrWhiteSpace(localeTag)) return true;
        var parsed = CldrLocaleTag.Parse(localeTag.Trim().Replace('_', '-'));
        string baseTag = parsed.BaseTag;
        if (baseTag.Length == 0) return true;
        if (CldrLocaleLoader.LoadExact(baseTag) is not null) return true;
        if (TailoringRules.ForLocale(baseTag) is not null) return true;
        return IsKnownCulture(baseTag);
    }

    private static bool IsKnownCulture(string tag)
    {
        try
        {
            _ = CultureInfo.GetCultureInfo(tag, predefinedOnly: true);
            return true;
        }
        catch (CultureNotFoundException) { return false; }
        catch (ArgumentException) { return false; }
    }

    /// <summary>"ISO 14651_2020_TABLE1" — case-insensitive, space/underscore interchangeable, any run of them one.</summary>
    public static bool IsDefaultOrderingTableName(string name)
    {
        static string Canon(string s) => string.Join('_', s.Split([' ', '_'], StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
        return Canon(name) == Canon(DefaultOrderingTableName);
    }

    /// <summary>True when the text is well-formed UTF-16 (no unpaired surrogate) — the derived table orders every
    /// well-formed text; see <see cref="Collator.IsWellFormed"/>.</summary>
    public static bool IsWellFormed(ReadOnlySpan<char> text) => Collator.IsWellFormed(text);

    private static Collator Cached(CollationTable table, CollationStrength strength, AlternateHandling alternate)
    {
        if (ReferenceEquals(table, CollationTable.Root))
        {
            if (strength == CollationStrength.Tertiary && alternate == AlternateHandling.NonIgnorable) return Collator.Root;
            if (strength == CollationStrength.Quaternary && alternate == AlternateHandling.Shifted) return Standard;
        }
        return s_collators.GetOrAdd((table, new CollationOptions(strength, alternate)), static k => new Collator(k.Table, k.Options));
    }
}
