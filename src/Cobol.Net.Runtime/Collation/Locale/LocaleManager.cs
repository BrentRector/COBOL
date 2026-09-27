// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Globalization;
using CobolNet.Runtime.Collation.Cldr;

namespace CobolNet.Runtime.Collation.Locale;

/// <summary>
/// The locale SELECTION system of the collation subsystem (kb/Work PB101 section D, PB105): select a locale by tag,
/// load its collation — the CLDR collation of the tag (<see cref="CldrLocaleLoader"/> →
/// <see cref="CldrTailoringBuilder"/>), then the site's numeric <c>.tailor</c> rules on top — and make the engine
/// collate by it, for the run unit that is current.
/// <para><b>Where the state lives.</b> There is exactly ONE current-locale state in WiseOwl COBOL: the run unit's
/// <see cref="LocaleState"/> (<c>RunUnit.Current.Locale</c>, ISO/IEC 1989:2023 §8.2.1 / §14.6.6 — DESIGN-locale-facility
/// §4.3). <see cref="SetLocale"/> writes it (every category, the <c>SET LOCALE LC_ALL</c> shape) and
/// <see cref="CurrentLocale"/> reads its LC_COLLATE category; the LOCALE-based collating sequence
/// (<see cref="LocaleCollation.Current"/> — an <c>ALPHABET … IS LOCALE</c> program collating sequence, a SORT/MERGE or file
/// key sequence) resolves that same state at each use, so a selection made here is what a running COBOL program's
/// locale-based comparisons see. This class adds no second store.</para>
/// <para><b>How a locale's collation is built.</b> Tables are immutable: selecting a locale never mutates the root
/// table. <see cref="CollationEngine.ResolveLocale"/> does the work once per tag and caches it: the CLDR collation
/// the tag means (its file along the parent chain, its <c>-u-co-</c> type, its settings and the tag's <c>-u-</c>
/// keys) is built into a tailored table over the root, and a <c>.tailor</c> for the tag or its language (a file in
/// <c>COBOL_COLLATION_DIR</c>, in <c>Collation/</c> or <c>Collation/Locales/</c> beside the application, or one embedded
/// in this assembly — exact tag, then language) is layered on top; the result is a table + settings per locale,
/// shared and cached, never a global mutation — a run unit may use several locales at once (a SORT under one, a file
/// under another).</para>
/// <para><b>Validation.</b> A tag is accepted when it is the root, has a CLDR collation file of its own, has
/// <c>.tailor</c> rules (its own or its language's), or is a culture .NET recognizes ("es-419", "nb-NO" — it then
/// collates by its CLDR parent chain: es.xml, no.xml; a locale no data covers, by the root order — the CLDR order for
/// English, German, Italian …); anything else is a <see cref="CultureNotFoundException"/>
/// (<see cref="CollationEngine.IsKnownLocale"/> — the ONE rule; the CLDR parent chain alone never makes a tag known,
/// or "no-Such-TABLE" would be Norwegian). Tags are normalized to the hyphen form.</para>
/// <para><b>Start-up.</b> No call is required: a run unit's <see cref="LocaleState"/> initializes itself from the L2
/// defaults (owner decision Q2 — <c>COBOL_USER_LOCALE</c>, else the process culture, else the root) when the run unit
/// is created, and <see cref="CollationRuntime"/> wires the subsystems together. A host that wants to override that
/// for the program it is about to run calls <c>LocaleManager.SetLocale("es-ES")</c> before invoking the program's
/// entry point (the compiled <c>Main</c> needs no change); the future <c>SET LOCALE</c> statement (design increment T1)
/// writes the same <see cref="LocaleState"/>.</para>
/// </summary>
public static class LocaleManager
{
    private static readonly ConcurrentDictionary<string, LocaleInfo> s_infos = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The locale currently selected for collation in the ambient run unit — its LC_COLLATE locale.</summary>
    public static LocaleInfo CurrentLocale => GetLocale(RunUnit.Current.Locale.Current(LocaleCategory.Collate));

    /// <summary>The engine collator for <see cref="CurrentLocale"/> at the locale's own settings (determination L11).</summary>
    public static Collator CurrentCollator => CurrentLocale.Collator;

    /// <summary>Select <paramref name="localeName"/> for the ambient run unit — every category (LC_ALL). Validates the
    /// tag, loads and applies its collation (cached), and records it on <c>RunUnit.Current.Locale</c>. Null, "" or
    /// "root" selects the root order.</summary>
    /// <exception cref="CultureNotFoundException">The tag has no CLDR data, no tailoring, and is not a culture .NET recognizes.</exception>
    /// <exception cref="FormatException">The locale's tailoring file is malformed (the message names file and line).</exception>
    public static void SetLocale(string? localeName)
    {
        var info = GetLocale(localeName);        // validates + loads (throws on an unknown tag / a malformed file)
        RunUnit.Current.Locale.Set(LocaleCategory.All, info.Name.Length == 0 ? "" : info.Name);
    }

    /// <summary>Restore the run unit's L2 user default (the locale it started with).</summary>
    public static void ResetLocale() => RunUnit.Current.Locale.Set(LocaleCategory.All, null);

    /// <summary>The <see cref="LocaleInfo"/> of a tag WITHOUT selecting it (validated, collation built, cached).</summary>
    /// <exception cref="CultureNotFoundException">The tag has no CLDR data, no tailoring, and is not a culture .NET recognizes.</exception>
    public static LocaleInfo GetLocale(string? localeName)
    {
        string tag = Normalize(localeName);
        return s_infos.GetOrAdd(tag, static t =>
        {
            if (t.Length == 0) return new LocaleInfo("", CollationEngine.ResolveLocale(t), recognizedCulture: true);
            // The ONE known-locale rule (CollationEngine.IsKnownLocale): its own CLDR file, its own / its language's
            // .tailor, or a culture .NET recognizes — never the CLDR parent chain alone.
            if (!CollationEngine.IsKnownLocale(t))
                throw new CultureNotFoundException(nameof(localeName), t,
                    $"locale '{t}' is not selectable: no CLDR collation data of its own ({CldrLocaleLoader.DescribeSearch()}), no tailoring file ({TailoringRules.DescribeSearch(t)}) and not a culture .NET recognizes");
            var resolved = CollationEngine.ResolveLocale(t);
            bool known = LocaleConfig.IsKnownCulture(resolved.Cldr.Tag.BaseTag);
            // The canonical spelling: the tailoring's own @locale when it names this very tag ("es_es" → "es-ES");
            // else the CLDR-normalized base tag with the -u- extension as given.
            string name = resolved.Tailoring?.Locale is { } declared && declared.Equals(resolved.Cldr.Tag.BaseTag, StringComparison.OrdinalIgnoreCase)
                ? declared + t[resolved.Cldr.Tag.BaseTag.Length..]
                : t;
            return new LocaleInfo(name, resolved, known);
        });
    }

    /// <summary>Try-form of <see cref="GetLocale"/>: false (and null) for an unselectable tag.</summary>
    public static bool TryGetLocale(string? localeName, out LocaleInfo? info)
    {
        try { info = GetLocale(localeName); return true; }
        catch (CultureNotFoundException) { info = null; return false; }
    }

    /// <summary>The selectable tags this system knows by name (root + CLDR + tailored); see
    /// <see cref="LocaleConfig.IsSupported"/> for the recognized-culture rule that admits the rest.</summary>
    public static IReadOnlyList<string> SupportedLocales => LocaleConfig.SupportedLocales;

    /// <summary>Forget the cached <see cref="LocaleInfo"/>s and the engine's resolved locales (a test that writes a
    /// tailoring or CLDR file at run time, or a host that changes <c>COBOL_COLLATION_DIR</c> / <c>COBOL_CLDR_DIR</c>).</summary>
    public static void ClearCache()
    {
        s_infos.Clear();
        CollationEngine.ClearLocaleCache();
    }

    private static string Normalize(string? localeName)
    {
        if (localeName is null) return "";
        string tag = localeName.Trim().Replace('_', '-');
        return tag.Equals("root", StringComparison.OrdinalIgnoreCase) || tag.Equals("und", StringComparison.OrdinalIgnoreCase) ? "" : tag;
    }
}
