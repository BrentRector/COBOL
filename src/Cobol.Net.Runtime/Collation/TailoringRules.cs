// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime.Collation;

/// <summary>
/// A locale (or user) TAILORING of the derived collation table: a set of code point → collation element overrides read
/// from a <c>.tailor</c> file. Applying the rules to a table (<see cref="Apply"/> / <see cref="CollationTable.WithTailoring"/>)
/// yields a NEW table; the base is never mutated.
/// <para><b>File format</b> — one mapping per line, <c>#</c> starts a comment, blank lines are ignored, every number is
/// HEXADECIMAL and every weight is in the DERIVED scale the table reports (<see cref="CollationTable.Lookup"/> —
/// a root primary is the source value shifted left by <see cref="CollationTable.PrimaryShift"/>, so the values strictly
/// between two adjacent root primaries are free for a tailoring):</para>
/// <code>
/// @version 17.0.0                 # optional — refused when it differs from the base table's UCA version
/// @locale es-ES                   # optional — names the tailoring
/// # code point   primary secondary tertiary [variable] [upper]   one element (the owner's minimal form; U+ or 0x prefix)
/// U+00F1         25718 0020 0002                            # ñ — right after n at level 1
/// U+00D1         25718 0020 0008                            # Ñ — its tertiary 0008 is an uppercase weight (case bit set)
/// # several code points (a CONTRACTION) — every code point then needs its U+ prefix
/// U+006E U+0303  25718 0020 0002
/// # several elements (an EXPANSION) — bracket each element
/// U+00E6         [23EC0 0020 0004] [0000 011F 0004] [24530 0020 0004]
/// </code>
/// <para>Weights are in the ROOT scale even when the file is layered over a CLDR-derived table that renumbered
/// (<see cref="CollationTable.WithTailoring"/> maps them). The case bit (<see cref="CaseFirst"/>) of an element is
/// the DUCET tertiary rule (<see cref="CollationElement.IsUpperTertiary"/>) unless the element ends in <c>upper</c>,
/// <c>mixed</c> or <c>lower</c>.</para>
/// <para><b>Locale lookup</b> (<see cref="ForLocale"/>): <c>&lt;tag&gt;.tailor</c> is searched in the directory named by
/// the <c>COBOL_COLLATION_DIR</c> environment variable, then in <c>Collation/</c> and <c>Collation/Locales/</c> beside
/// the running application (<see cref="SearchDirectories"/>), then among the tailorings embedded in this assembly
/// (<c>Collation/Tailoring/*.tailor</c>: en-US, fr-FR, es-ES, es); an exact tag first, then the language alone. A
/// locale with no file collates by the root order — which is the CLDR order for English, French, German and most
/// European languages.</para>
/// </summary>
public sealed class TailoringRules
{
    /// <summary>One override: the code point sequence (length 1, or a contraction) and its element sequence.</summary>
    public sealed record Entry(int[] CodePoints, CollationElement[] Elements, int Line);

    /// <summary>The environment variable naming a directory of site <c>.tailor</c> files (searched first;
    /// <see cref="SearchDirectories"/>). Registered in <see cref="RuntimeConfig"/>.</summary>
    public const string EnvDirectory = "COBOL_COLLATION_DIR";
    private const string ResourcePrefix = "Collation/Tailoring/";
    private const string FileExtension = ".tailor";

    private readonly Dictionary<CodePointKey, Entry> _byKey;

    private TailoringRules(string source, string? locale, string? ucaVersion, List<Entry> entries)
    {
        Source = source;
        Locale = locale;
        UcaVersion = ucaVersion;
        Entries = entries;
        _byKey = new Dictionary<CodePointKey, Entry>(entries.Count);
        foreach (var e in entries) _byKey[new CodePointKey(e.CodePoints)] = e;
    }

    /// <summary>Where the rules came from (a path or an embedded resource name) — for diagnostics.</summary>
    public string Source { get; }

    /// <summary>The <c>@locale</c> tag, if declared.</summary>
    public string? Locale { get; }

    /// <summary>The <c>@version</c> the weights were written against, if declared.</summary>
    public string? UcaVersion { get; }

    /// <summary>The overrides, in file order.</summary>
    public IReadOnlyList<Entry> Entries { get; }

    /// <summary>The tailoring's name: its locale, else its source's file name without extension.</summary>
    public string Name => Locale ?? Path.GetFileNameWithoutExtension(Source);

    /// <summary>True when the rules define an override for exactly this code point sequence.</summary>
    public bool Defines(int[] codePoints) => _byKey.ContainsKey(new CodePointKey(codePoints));

    /// <summary>The override for a code point sequence, or null.</summary>
    public Entry? Find(params int[] codePoints) => _byKey.TryGetValue(new CodePointKey(codePoints), out var e) ? e : null;

    /// <summary>The base table with these rules layered over it — a new table.</summary>
    public CollationTable Apply(CollationTable baseTable) => baseTable.WithTailoring(this);

    // ---- loading -------------------------------------------------------------------------------------------------

    /// <summary>Read a <c>.tailor</c> file from disk.</summary>
    public static TailoringRules Load(string path)
    {
        using var reader = new StreamReader(path, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return Parse(reader, path);
    }

    /// <summary>The tailoring for a locale tag ("es-ES", "es_ES", "es"), searched as the class summary describes;
    /// null when the locale collates by the root order. A malformed file is an error, not a silent fallback.</summary>
    public static TailoringRules? ForLocale(string? localeTag)
    {
        if (string.IsNullOrWhiteSpace(localeTag)) return null;
        foreach (string candidate in Candidates(localeTag))
        {
            foreach (string dir in SearchDirectories())
                if (FromDirectory(dir, candidate) is { } fromDisk) return fromDisk;
            if (FromResource(candidate) is { } embedded) return embedded;
        }
        return null;
    }

    /// <summary>The disk directories a tailoring is looked for in, in precedence order, existing ones only: the
    /// <c>COBOL_COLLATION_DIR</c> directory, then <c>Collation/</c> and <c>Collation/Locales/</c> beside the running
    /// application. The embedded tailorings come after all of them.</summary>
    public static IEnumerable<string> SearchDirectories()
    {
        string? env = Environment.GetEnvironmentVariable(EnvDirectory);
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env)) yield return env;
        string app = Path.Combine(AppContext.BaseDirectory, "Collation");
        if (Directory.Exists(app)) yield return app;
        string locales = Path.Combine(app, "Locales");
        if (Directory.Exists(locales)) yield return locales;
    }

    /// <summary>The tailoring names (file names without the <c>.tailor</c> extension) a directory holds.</summary>
    public static IEnumerable<string> TailoringsIn(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*" + FileExtension).Select(f => Path.GetFileNameWithoutExtension(f) ?? "").Where(n => n.Length > 0)
            : [];

    /// <summary>A one-line description of where <see cref="ForLocale"/> looked for a tag — for diagnostics.</summary>
    public static string DescribeSearch(string localeTag)
    {
        var dirs = SearchDirectories().ToList();
        string where = dirs.Count == 0 ? "no tailoring directory exists" : "searched " + string.Join(", ", dirs);
        return $"{string.Join(" / ", Candidates(localeTag).Select(c => c + FileExtension))}: {where}, then the embedded tailorings ({string.Join(", ", EmbeddedNames())})";
    }

    /// <summary>The embedded tailorings' names (the shipped locale set), for diagnostics and tests.</summary>
    public static IEnumerable<string> EmbeddedNames() =>
        typeof(TailoringRules).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && n.EndsWith(FileExtension, StringComparison.Ordinal))
            .Select(n => n[ResourcePrefix.Length..^FileExtension.Length])
            .OrderBy(n => n, StringComparer.Ordinal);

    /// <summary>The lookup chain for a tag: the normalized tag, then its language subtag ("es-MX" → "es").</summary>
    internal static IEnumerable<string> Candidates(string localeTag)
    {
        string tag = localeTag.Trim().Replace('_', '-');
        yield return tag;
        int dash = tag.IndexOf('-');
        if (dash > 0) yield return tag[..dash];
    }

    private static TailoringRules? FromDirectory(string? directory, string tag)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return null;
        // Case-insensitive file match, so "es-es" finds "es-ES.tailor" on a case-sensitive file system too.
        foreach (string path in Directory.EnumerateFiles(directory, "*" + FileExtension))
            if (string.Equals(Path.GetFileNameWithoutExtension(path), tag, StringComparison.OrdinalIgnoreCase))
                return Load(path);
        return null;
    }

    private static TailoringRules? FromResource(string tag)
    {
        var asm = typeof(TailoringRules).Assembly;
        string? name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => string.Equals(n, ResourcePrefix + tag + FileExtension, StringComparison.OrdinalIgnoreCase));
        if (name is null) return null;
        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return Parse(reader, "resource:" + name);
    }

    /// <summary>Parse the <c>.tailor</c> format (documented in the class summary). Every error names the line.</summary>
    public static TailoringRules Parse(TextReader reader, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(reader);
        string? locale = null, version = null;
        var entries = new List<Entry>();
        var seen = new Dictionary<CodePointKey, int>();
        int lineNo = 0;
        while (reader.ReadLine() is { } raw)
        {
            lineNo++;
            string line = raw;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            line = line.Trim();
            if (line.Length == 0) continue;
            if (line[0] == '@')
            {
                int sp = line.IndexOfAny([' ', '\t']);
                string directive = sp < 0 ? line : line[..sp];
                string value = sp < 0 ? "" : line[(sp + 1)..].Trim();
                switch (directive.ToLowerInvariant())
                {
                    case "@version": version = value; break;
                    case "@locale": locale = value; break;
                    default: throw Error(sourceName, lineNo, $"unknown directive '{directive}' (known: @version, @locale)");
                }
                continue;
            }
            var entry = ParseMapping(line, sourceName, lineNo);
            var key = new CodePointKey(entry.CodePoints);
            if (seen.TryGetValue(key, out int first))
                throw Error(sourceName, lineNo, $"duplicate mapping for {Describe(entry.CodePoints)} (first at line {first})");
            seen[key] = lineNo;
            entries.Add(entry);
        }
        return new TailoringRules(sourceName, locale, version, entries);
    }

    private static Entry ParseMapping(string line, string source, int lineNo)
    {
        // Tokenize; '[' and ']' delimit one element each.
        var tokens = new List<string>();
        var groups = new List<(int Start, int End)>();
        int i = 0;
        while (i < line.Length)
        {
            char c = line[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '[')
            {
                int close = line.IndexOf(']', i);
                if (close < 0) throw Error(source, lineNo, "unterminated '['");
                int start = tokens.Count;
                tokens.AddRange(line[(i + 1)..close].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                groups.Add((start, tokens.Count));
                i = close + 1;
                continue;
            }
            int j = i;
            while (j < line.Length && !char.IsWhiteSpace(line[j]) && line[j] != '[') j++;
            tokens.Add(line[i..j]);
            i = j;
        }
        if (tokens.Count == 0) throw Error(source, lineNo, "empty mapping");

        // Code points: the first token always (U+, 0x or bare hex), then every FURTHER token that carries the U+
        // prefix (a contraction) — 0x is admitted on the first code point only, so a 0x-prefixed WEIGHT after it
        // still reads as a weight.
        if (groups.Count > 0 && groups[0].Start == 0) throw Error(source, lineNo, "a mapping starts with its code point, not an element");
        var cps = new List<int> { ParseCodePoint(tokens[0], source, lineNo) };
        int t = 1;
        while (t < tokens.Count && tokens[t].StartsWith("U+", StringComparison.OrdinalIgnoreCase))
            cps.Add(ParseCodePoint(tokens[t++], source, lineNo));
        if (cps.Count > 1 && !tokens[0].StartsWith("U+", StringComparison.OrdinalIgnoreCase))
            throw Error(source, lineNo, "the code points of a contraction must each carry the U+ prefix");

        // Elements: bracket groups, else the rest of the line as one element.
        var elements = new List<CollationElement>();
        if (groups.Count > 0)
        {
            if (groups[0].Start != t) throw Error(source, lineNo, "unexpected token before the first [element]");
            int expectEnd = t;
            foreach (var (start, end) in groups)
            {
                if (start != expectEnd) throw Error(source, lineNo, "unexpected token between elements");
                elements.Add(ParseElement(tokens, start, end, source, lineNo));
                expectEnd = end;
            }
            if (expectEnd != tokens.Count) throw Error(source, lineNo, "unexpected token after the last [element]");
        }
        else
        {
            elements.Add(ParseElement(tokens, t, tokens.Count, source, lineNo));
        }
        return new Entry(cps.ToArray(), elements.ToArray(), lineNo);
    }

    private static CollationElement ParseElement(List<string> tokens, int start, int end, string source, int lineNo)
    {
        int n = end - start;
        bool variable = false;
        ElementCase? elementCase = null;
        // Trailing markers, in any order: 'variable' / '*' and 'upper' / '^' / 'mixed' / 'lower'.
        while (n > 3)
        {
            string last = tokens[start + n - 1];
            if (last is "*" || last.Equals("variable", StringComparison.OrdinalIgnoreCase)) variable = true;
            else if (last is "^" || last.Equals("upper", StringComparison.OrdinalIgnoreCase)) elementCase = ElementCase.Upper;
            else if (last.Equals("mixed", StringComparison.OrdinalIgnoreCase)) elementCase = ElementCase.Mixed;
            else if (last.Equals("lower", StringComparison.OrdinalIgnoreCase)) elementCase = ElementCase.Lower;
            else break;
            n--;
        }
        if (n != 3) throw Error(source, lineNo, "an element is 'primary secondary tertiary [variable] [upper|mixed|lower]' (hexadecimal)");
        int p = ParseWeight(tokens[start], "primary", 0xFFFFF, source, lineNo);
        int s = ParseWeight(tokens[start + 1], "secondary", 0xFFFF, source, lineNo);
        int tt = ParseWeight(tokens[start + 2], "tertiary", 0xFFFF, source, lineNo);
        // The case bits: stated, else the DUCET tertiary rule the generator applies to every root element.
        return new CollationElement(p, s, tt, variable, elementCase ?? (CollationElement.IsUpperTertiary(tt) ? ElementCase.Upper : ElementCase.Lower));
    }

    private static int ParseCodePoint(string token, string source, int lineNo)
    {
        string hex = token.StartsWith("U+", StringComparison.OrdinalIgnoreCase) || token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? token[2..] : token;
        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int cp) || cp < 0 || cp > 0x10FFFF)
            throw Error(source, lineNo, $"'{token}' is not a code point (U+0000..U+10FFFF, hexadecimal)");
        return cp;
    }

    private static int ParseWeight(string token, string what, int max, string source, int lineNo)
    {
        string hex = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? token[2..] : token;
        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int w) || w < 0 || w > max)
            throw Error(source, lineNo, $"'{token}' is not a {what} weight (hexadecimal, 0..{max:X})");
        return w;
    }

    private static string Describe(int[] cps) => string.Join(" ", cps.Select(c => $"U+{c:X4}"));

    private static FormatException Error(string source, int lineNo, string message) =>
        new($"{source}({lineNo}): {message}");
}
