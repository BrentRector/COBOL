// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.IO.Compression;

namespace CobolNet.Runtime.Collation.Cldr;

/// <summary>
/// The exception <see cref="CldrLocaleLoader.Load"/> throws when no CLDR collation file exists for a locale or any of
/// its parents.
/// </summary>
public sealed class CldrLocaleNotFoundException(string message) : Exception(message);

/// <summary>
/// The CLDR LOCALE LOADER: finds and parses the CLDR collation data of a locale — from a site directory
/// (<c>COBOL_CLDR_DIR</c>, then <c>Collation/CLDR/</c> beside the application; <c>.xml</c> LDML or the <c>.json</c>
/// mirror), else from the pack of CLDR release files embedded in this assembly
/// (<c>Collation/CLDR/Data/cldr-collation.zip</c>, built by <c>scripts/collation/pack-cldr-collation.py</c> from the
/// pinned <c>data/unicode/cldr/</c>) — and resolves WHICH collation of which file a locale tag means
/// (<see cref="ResolveCollation"/>: the BCP 47 <c>-u-co-</c> type, the parent chain, the <c>-u-</c> settings keys).
/// Parsed files are cached per process.
/// <para><b>Fallback chain.</b> A tag's file is looked for under its own name (<c>de_AT</c>), then along CLDR's
/// locale inheritance: the explicit parent when <c>supplementalData.xml</c>'s <c>&lt;parentLocales&gt;</c> (packed
/// with the data) names one — the <c>component="collations"</c> table (<c>yue</c> → <c>zh_Hant</c>) and the general
/// table's plain entries (<c>nb</c> → <c>no</c>), but not its <c>localeRules="nonlikelyScript"</c> entries, which
/// LDML reserves for the main component (so <c>zh_Hant</c>'s collation parent stays <c>zh</c>) — else with the last
/// subtag dropped (<c>sr_Latn_RS</c> → <c>sr_Latn</c> → <c>sr</c>), then <c>root</c>. A collation TYPE is looked for along the same chain: the
/// first file that defines it wins; a type no file defines falls back to the chain's default type; and a locale with
/// no file at all collates by the root order (which IS the CLDR order for English, French, German, Dutch, Italian,
/// Portuguese …).</para>
/// </summary>
public static class CldrLocaleLoader
{
    /// <summary>The embedded resource holding the pack (LogicalName in Cobol.Net.Runtime.csproj).</summary>
    public const string PackResourceName = "Collation/CLDR/Data/cldr-collation.zip";

    /// <summary>The environment variable naming a directory of site CLDR collation files (<c>&lt;tag&gt;.xml</c> or <c>.json</c>).</summary>
    public const string EnvDirectory = "COBOL_CLDR_DIR";

    private static readonly Lazy<Pack> s_pack = new(Pack.Load, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly ConcurrentDictionary<string, CldrLocaleData?> s_cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The CLDR release the embedded pack was built from ("release-48-2"), from its manifest.</summary>
    public static string PackRelease => s_pack.Value.Release;

    /// <summary>The locale names the embedded pack holds (file names without extension, CLDR spelling: "de_AT").</summary>
    public static IReadOnlyList<string> PackLocales => s_pack.Value.Locales;

    private static readonly Lazy<HashSet<string>> s_collationTypes = new(ReadCollationTypes, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The collation TYPES the BCP 47 <c>co</c> key admits (CLDR <c>bcp47/collation.xml</c> in the embedded pack —
    /// every <c>&lt;type name=…&gt;</c> and its <c>alias</c>: standard, phonebk/phonebook, trad/traditional, pinyin, stroke,
    /// zhuyin, unihan, dict/dictionary, search, eor, emoji, …), lower-case. Used by <c>LocaleIdentification.Normalize</c>
    /// (DESIGN-locale-facility L1) to decide whether a POSIX <c>@modifier</c> is a collation type.</summary>
    public static bool IsCollationType(string type) => type is not null && s_collationTypes.Value.Contains(type.ToLowerInvariant());

    private static HashSet<string> ReadCollationTypes()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var bytes = ReadPackEntry("bcp47/collation.xml");
        if (bytes is null) return set;
        var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore, XmlResolver = null };
        using var reader = System.Xml.XmlReader.Create(new MemoryStream(bytes), settings);
        var doc = System.Xml.Linq.XDocument.Load(reader);
        foreach (var key in doc.Descendants("key"))
        {
            if (key.Attribute("name")?.Value != "co") continue;
            foreach (var type in key.Elements("type"))
            {
                if (type.Attribute("name")?.Value is { } n) set.Add(n.ToLowerInvariant());
                if (type.Attribute("alias")?.Value is { } a)
                    foreach (var alias in a.Split(' ', StringSplitOptions.RemoveEmptyEntries)) set.Add(alias.ToLowerInvariant());
            }
        }
        return set;
    }

    /// <summary>Load the collation data of exactly <paramref name="localeName"/> or, when no file exists for it, of
    /// the nearest parent in its chain (README §"Fallback chain"). "root" / "" / "und" load the root file.</summary>
    /// <exception cref="CldrLocaleNotFoundException">No file exists for the locale or any parent (the root file is
    /// always in the pack, so this means the tag is malformed or the pack is missing).</exception>
    public static CldrLocaleData Load(string localeName)
    {
        foreach (string candidate in Chain(localeName))
        {
            if (LoadExact(candidate) is { } data) return data;
        }
        throw new CldrLocaleNotFoundException($"no CLDR collation data for '{localeName}' or its parents ({DescribeSearch()})");
    }

    /// <summary>The root file's data.</summary>
    public static CldrLocaleData Root => Load("root");

    /// <summary>The data of EXACTLY this locale (no parent fallback), or null when no file exists for it.</summary>
    public static CldrLocaleData? LoadExact(string localeName)
    {
        string key = CldrLocaleTag.FileName(localeName);
        return s_cache.GetOrAdd(key, static k => ReadFile(k));
    }

    /// <summary>The parent chain of a tag, most specific first, ending in "root": "sr-Latn-RS" → sr_Latn_RS, sr_Latn,
    /// sr, root; "nb" → nb, no, root (its explicit parent). Any <c>-u-</c> extension is dropped first.</summary>
    public static IEnumerable<string> Chain(string? localeName)
    {
        string baseTag = CldrLocaleTag.Parse(localeName).BaseTag;
        if (baseTag.Length == 0 || baseTag.Equals("root", StringComparison.OrdinalIgnoreCase)) { yield return "root"; yield break; }
        string current = baseTag.Replace('-', '_');
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (current.Length > 0 && !current.Equals("root", StringComparison.OrdinalIgnoreCase) && seen.Add(current))
        {
            yield return current;
            current = ParentOf(current);
        }
        yield return "root";
    }

    /// <summary>The CLDR parent of a locale (CLDR spelling): the explicit <c>&lt;parentLocales&gt;</c> parent when one
    /// is declared (the collations component's table first, then the general one), else the tag with its last
    /// subtag removed, else "root".</summary>
    public static string ParentOf(string localeFileName)
    {
        var parents = s_pack.Value.ParentLocales;
        if (parents.TryGetValue(localeFileName, out string? explicitParent)) return explicitParent;
        int i = localeFileName.LastIndexOf('_');
        return i > 0 ? localeFileName[..i] : "root";
    }

    /// <summary>Which collation a locale tag means: the <c>-u-co-</c> type (else the chain's default type) found along
    /// the parent chain, with the tag's other <c>-u-</c> settings keys applied over the collation's own settings.
    /// Never throws for a well-formed tag: a locale no file covers resolves to the root order.</summary>
    public static CldrCollationSelection ResolveCollation(string? localeName)
    {
        var tag = CldrLocaleTag.Parse(localeName);
        var chain = Chain(localeName).Select(LoadExact).Where(d => d is not null).Cast<CldrLocaleData>().ToList();
        // The requested type, else the most specific file's default type.
        string type = tag.CollationType ?? chain.Select(d => d.DefaultCollation).FirstOrDefault(d => d is not null) ?? "standard";
        CldrLocaleData? found = null;
        CldrCollation? collation = null;
        foreach (var d in chain)
        {
            if (d.Find(type) is { } c) { found = d; collation = c; break; }
        }
        bool typeFellBack = false;
        if (collation is null && !type.Equals("standard", StringComparison.OrdinalIgnoreCase))
        {
            // The type exists nowhere in the chain: fall back to the default type (ICU does the same).
            typeFellBack = true;
            foreach (var d in chain)
            {
                if (d.Find(d.EffectiveDefaultType) is { } c) { found = d; collation = c; type = c.Type; break; }
            }
        }
        var unsupported = new List<string>(tag.Unsupported);
        if (typeFellBack) unsupported.Add($"collation type '{tag.CollationType}' is not defined for '{tag.BaseTag}' or its parents — the default type '{type}' is used");
        return new CldrCollationSelection(tag, chain.FirstOrDefault(), found, collation, type, unsupported);
    }

    /// <summary>The disk directories a site file is looked for in, in precedence order, existing ones only.</summary>
    public static IEnumerable<string> SearchDirectories()
    {
        string? env = Environment.GetEnvironmentVariable(EnvDirectory);
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env)) yield return env;
        string app = Path.Combine(AppContext.BaseDirectory, "Collation", "CLDR");
        if (Directory.Exists(app)) yield return app;
    }

    /// <summary>Every locale a file exists for — the site directories' plus the pack's (CLDR spelling, distinct, sorted).</summary>
    public static IReadOnlyList<string> AvailableLocales()
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string dir in SearchDirectories())
            foreach (string f in Directory.EnumerateFiles(dir))
                if (f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    names.Add(Path.GetFileNameWithoutExtension(f));
        foreach (string n in s_pack.Value.Locales) names.Add(n);
        return names.ToArray();
    }

    /// <summary>A one-line description of the sources, for diagnostics.</summary>
    public static string DescribeSearch()
    {
        var dirs = SearchDirectories().ToList();
        return (dirs.Count == 0 ? "no site directory" : "site directories " + string.Join(", ", dirs)) + $"; embedded CLDR {PackRelease} pack ({PackLocales.Count} files)";
    }

    /// <summary>Forget every parsed file (a test that writes a site file at run time; a host that changed
    /// <c>COBOL_CLDR_DIR</c>). The engine's table cache is separate (<see cref="CollationEngine.ClearLocaleCache"/>).</summary>
    public static void ClearCache() => s_cache.Clear();

    private static CldrLocaleData? ReadFile(string fileName)
    {
        foreach (string dir in SearchDirectories())
        {
            foreach (string path in Directory.EnumerateFiles(dir))
            {
                string stem = Path.GetFileNameWithoutExtension(path);
                if (!string.Equals(stem, fileName, StringComparison.OrdinalIgnoreCase)) continue;
                if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    using var s = File.OpenRead(path);
                    return CldrParser.ParseXml(s, path);
                }
                if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    return CldrParser.ParseJson(File.ReadAllText(path), path);
            }
        }
        return s_pack.Value.Read(fileName);
    }

    /// <summary>The embedded zip: read once, entries parsed on demand.</summary>
    private sealed class Pack
    {
        private readonly byte[] _bytes;
        private readonly Dictionary<string, string> _entryByLocale;   // locale file name → entry name
        private readonly Lazy<Dictionary<string, string>> _parents;

        private Pack(byte[] bytes, string release, Dictionary<string, string> entries)
        {
            _bytes = bytes;
            Release = release;
            _entryByLocale = entries;
            Locales = entries.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
            _parents = new Lazy<Dictionary<string, string>>(ReadParentLocales, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public string Release { get; }
        public IReadOnlyList<string> Locales { get; }

        /// <summary>locale (CLDR spelling) → its explicit parent, from supplementalData.xml's parentLocales.</summary>
        public Dictionary<string, string> ParentLocales => _parents.Value;

        private Dictionary<string, string> ReadParentLocales()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var bytes = ReadRaw("supplemental/supplementalData.xml");
            if (bytes is null) return map;
            var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore, XmlResolver = null };
            using var reader = System.Xml.XmlReader.Create(new MemoryStream(bytes), settings);
            var doc = System.Xml.Linq.XDocument.Load(reader);
            // The collations component's table takes precedence over the general table.
            if (doc.Root is null) return map;
            var tables = doc.Root.Elements("parentLocales")
                .OrderBy(t => t.Attribute("component")?.Value?.Contains("collations", StringComparison.Ordinal) == true ? 0 : 1);
            foreach (var table in tables)
            {
                string? component = table.Attribute("component")?.Value;
                if (component is not null && !component.Contains("collations", StringComparison.Ordinal)) continue;
                foreach (var pl in table.Elements("parentLocale"))
                {
                    // LDML: the localeRules="nonlikelyScript" entries (zh_Hant → root, sr_Latn → root …) serve the
                    // main component only — "not used [for] components where text is not mixed, such as the
                    // collations component" — so zh_Hant's collation parent stays zh (where its stroke order lives);
                    // the plain entries (nb → no) apply to collation as they do to everything (ICU: %%Parent{"no"}).
                    if (component is null && pl.Attribute("localeRules") is not null) continue;
                    string parent = pl.Attribute("parent")?.Value ?? "root";
                    foreach (string loc in (pl.Attribute("locales")?.Value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        map.TryAdd(loc, parent);
                }
            }
            return map;
        }

        public static Pack Load()
        {
            var asm = typeof(CldrLocaleLoader).Assembly;
            using var stream = asm.GetManifestResourceStream(PackResourceName)
                ?? throw new InvalidOperationException($"embedded CLDR pack '{PackResourceName}' is missing from {asm.GetName().Name}");
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();
            var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string release = "unknown";
            using (var zip = new ZipArchive(new MemoryStream(bytes, writable: false), ZipArchiveMode.Read))
            {
                foreach (var e in zip.Entries)
                {
                    if (e.FullName.StartsWith("collation/", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal))
                        entries[e.FullName[10..^4]] = e.FullName;
                }
            }
            // The release tag travels in the manifest JSON beside the pack, embedded as a second resource.
            using (var man = asm.GetManifestResourceStream(PackResourceName.Replace(".zip", ".manifest.json", StringComparison.Ordinal)))
            {
                if (man is not null)
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(man);
                    if (doc.RootElement.TryGetProperty("release", out var rel) && rel.GetString() is { } s) release = s;
                }
            }
            return new Pack(bytes, release, entries);
        }

        public CldrLocaleData? Read(string localeFileName)
        {
            if (!_entryByLocale.TryGetValue(localeFileName, out string? entryName)) return null;
            using var zip = new ZipArchive(new MemoryStream(_bytes, writable: false), ZipArchiveMode.Read);
            var entry = zip.GetEntry(entryName)!;
            using var s = entry.Open();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            ms.Position = 0;
            return CldrParser.ParseXml(ms, "cldr:" + entryName);
        }

        /// <summary>The raw bytes of a pack entry (the bcp47 key file, for the drift test).</summary>
        public byte[]? ReadRaw(string entryName)
        {
            using var zip = new ZipArchive(new MemoryStream(_bytes, writable: false), ZipArchiveMode.Read);
            var entry = zip.GetEntry(entryName);
            if (entry is null) return null;
            using var s = entry.Open();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }

    /// <summary>The raw bytes of an entry of the embedded pack ("bcp47/collation.xml"), or null — for tests and diagnostics.</summary>
    public static byte[]? ReadPackEntry(string entryName) => s_pack.Value.ReadRaw(entryName);
}

/// <summary>What <see cref="CldrLocaleLoader.ResolveCollation"/> decided for a locale tag.</summary>
/// <param name="Tag">The parsed tag (base tag, <c>-u-co-</c> type, settings keys).</param>
/// <param name="MostSpecific">The most specific file of the chain that exists (the tag's own, a parent, or root).</param>
/// <param name="Found">The file the collation was taken from, or null when no file of the chain has any collation of the type.</param>
/// <param name="Collation">The collation, or null (→ the root order).</param>
/// <param name="Type">The type in effect.</param>
/// <param name="Unsupported">What the tag asked for that the engine does not honor (settings keys, a type that fell back).</param>
public sealed record CldrCollationSelection(CldrLocaleTag Tag, CldrLocaleData? MostSpecific, CldrLocaleData? Found,
    CldrCollation? Collation, string Type, IReadOnlyList<string> Unsupported)
{
    /// <summary>The collation's settings with the tag's <c>-u-</c> keys applied over them.</summary>
    public CldrSettings Settings => (Collation?.Settings ?? new CldrSettings()).Merge(Tag.Settings);

    /// <summary>True when the selection is a real tailoring (a collation with rules), false for the root order.</summary>
    public bool HasRules => Collation is { RuleCount: > 0 };

    public override string ToString() =>
        $"{Tag.BaseTag} → {(Found is null ? "root order" : $"{Found.Tag}/{Type}")}{(Unsupported.Count == 0 ? "" : $" ({Unsupported.Count} unsupported)")}";
}

/// <summary>
/// A locale tag as the collation subsystem reads it: the base BCP 47 tag ("de-AT", CLDR spelling normalized —
/// language lower-case, script title-case, region upper-case, "_" accepted for "-") plus the Unicode locale extension
/// keys of collation (<c>-u-co-phonebk-ka-shifted-kf-upper-ks-level2-kv-space-kb-true</c>; CLDR
/// <c>bcp47/collation.xml</c>): <c>co</c> the collation type, and the settings keys mapped onto
/// <see cref="CldrSettings"/> — <c>ka</c> alternate, <c>kb</c> backwards, <c>kc</c> caseLevel, <c>kf</c> caseFirst,
/// <c>kh</c> hiraganaQ, <c>kk</c> normalization, <c>kn</c> numeric, <c>kr</c> reorder codes, <c>ks</c> strength,
/// <c>kv</c> maxVariable. Anything the engine does not implement is listed in <see cref="Unsupported"/>.
/// </summary>
public sealed class CldrLocaleTag
{
    private CldrLocaleTag(string original, string baseTag, string? type, CldrSettings settings, IReadOnlyList<string> unsupported)
    {
        Original = original;
        BaseTag = baseTag;
        CollationType = type;
        Settings = settings;
        Unsupported = unsupported;
    }

    /// <summary>The tag as given.</summary>
    public string Original { get; }

    /// <summary>The base tag without the extension, normalized ("" for the root).</summary>
    public string BaseTag { get; }

    /// <summary>The <c>-u-co-</c> collation type in LDML spelling ("phonebook"), or null.</summary>
    public string? CollationType { get; }

    /// <summary>The settings the <c>-u-</c> keys stated.</summary>
    public CldrSettings Settings { get; }

    /// <summary>Keys the engine does not honor.</summary>
    public IReadOnlyList<string> Unsupported { get; }

    /// <summary>The CLDR file name of the base tag ("de_AT", "root").</summary>
    public string FileNameStem => BaseTag.Length == 0 ? "root" : BaseTag.Replace('-', '_');

    /// <summary>The CLDR file name stem of any tag.</summary>
    public static string FileName(string? localeName)
    {
        string t = (localeName ?? "").Trim();
        if (t.Length == 0 || t.Equals("root", StringComparison.OrdinalIgnoreCase) || t.Equals("und", StringComparison.OrdinalIgnoreCase)) return "root";
        return Parse(t).FileNameStem;
    }

    /// <summary>Parse a tag. Never throws: an unparsable extension is recorded as unsupported.</summary>
    public static CldrLocaleTag Parse(string? localeName)
    {
        string t = (localeName ?? "").Trim().Replace('_', '-');
        var unsupported = new List<string>();
        var settings = new CldrSettings();
        string? type = null;
        string baseTag = t;
        int u = IndexOfExtension(t);
        if (u >= 0)
        {
            baseTag = t[..u];
            var subs = t[(u + 3)..].Split('-', StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            while (i < subs.Length)
            {
                string key = subs[i].ToLowerInvariant();
                if (key.Length != 2) { unsupported.Add($"malformed -u- extension near '{subs[i]}'"); break; }
                var values = new List<string>();
                i++;
                while (i < subs.Length && !(subs[i].Length == 2 && char.IsLetter(subs[i][0]))) values.Add(subs[i++].ToLowerInvariant());
                string v = values.Count == 0 ? "true" : values[0];
                switch (key)
                {
                    case "co": type = CldrCollation.CanonicalType(string.Join('-', values)); break;   // "private-unihan" is two subtags
                    case "ka": settings = settings with { Alternate = v is "shifted" ? AlternateHandling.Shifted : AlternateHandling.NonIgnorable }; break;
                    case "kb": settings = settings with { BackwardsSecondary = v is "true" or "yes" }; break;
                    case "kc": settings = settings with { CaseLevel = v is "true" or "yes" }; break;
                    case "kf": settings = settings with { CaseFirst = v switch { "upper" => CaseFirst.Upper, "lower" => CaseFirst.Lower, _ => CaseFirst.Off } }; break;
                    case "kh": settings = settings with { HiraganaQuaternary = v is "true" or "yes" }; break;
                    case "kk": settings = settings with { Normalization = v is "true" or "yes" }; break;
                    case "kn": settings = settings with { NumericOrdering = v is "true" or "yes" }; break;
                    case "kr": settings = settings with { Reorder = values.Select(ReorderCode).ToArray() }; break;
                    case "ks":
                        settings = settings with
                        {
                            Strength = v switch
                            {
                                "level1" or "primary" => CollationStrength.Primary,
                                "level2" or "secondary" => CollationStrength.Secondary,
                                "level3" or "tertiary" => CollationStrength.Tertiary,
                                "level4" or "quaternary" or "quarternary" => CollationStrength.Quaternary,
                                "identic" or "identical" => CollationStrength.Identical,
                                _ => null,
                            },
                        };
                        if (settings.Strength is null) unsupported.Add($"-u-ks-{v}: not a strength");
                        break;
                    case "kv":
                        settings = settings with
                        {
                            MaxVariable = v switch { "space" => MaxVariable.Space, "punct" => MaxVariable.Punct, "symbol" => MaxVariable.Symbol, "currency" => MaxVariable.Currency, _ => null },
                        };
                        if (settings.MaxVariable is null) unsupported.Add($"-u-kv-{v}: not a maxVariable group");
                        break;
                    default:
                        unsupported.Add($"-u-{key}: not a collation key");
                        break;
                }
            }
        }
        unsupported.AddRange(settings.UnsupportedSettings());
        return new CldrLocaleTag(localeName ?? "", NormalizeBase(baseTag), type, settings, unsupported);
    }

    private static int IndexOfExtension(string t)
    {
        // "-u-" as a subtag boundary (or "u-" at the very start, which is not a valid tag anyway).
        int i = t.IndexOf("-u-", StringComparison.OrdinalIgnoreCase);
        return i;
    }

    /// <summary>A BCP 47 <c>kr</c> value → the CLDR reorder code spelling ("latn" → "Latn", "cyrl" → "Cyrl";
    /// the special codes and "others" stay lower-case).</summary>
    private static string ReorderCode(string v) =>
        v.Length == 4 && v is not ("space" or "punct" or "digit") ? char.ToUpperInvariant(v[0]) + v[1..].ToLowerInvariant() : v.ToLowerInvariant();

    /// <summary>CLDR spelling: language lower, script title-case, region upper, the rest as given; "root"/"und" → "".</summary>
    private static string NormalizeBase(string tag)
    {
        var parts = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "";
        if (parts[0].Equals("root", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("und", StringComparison.OrdinalIgnoreCase)) return "";
        var outParts = new List<string>(parts.Length) { parts[0].ToLowerInvariant() };
        for (int i = 1; i < parts.Length; i++)
        {
            string p = parts[i];
            if (p.Length == 4 && p.All(char.IsLetter)) outParts.Add(char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant());
            else if ((p.Length == 2 && p.All(char.IsLetter)) || (p.Length == 3 && p.All(char.IsDigit))) outParts.Add(p.ToUpperInvariant());
            else outParts.Add(p);
        }
        return string.Join('-', outParts);
    }

    public override string ToString() => Original;
}
