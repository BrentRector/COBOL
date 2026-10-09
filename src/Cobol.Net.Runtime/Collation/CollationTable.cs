// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.IO.Compression;

namespace CobolNet.Runtime.Collation;

/// <summary>
/// WiseOwl COBOL's DERIVED collation table: every Unicode code point (or contraction — a sequence of code points that
/// collates as a unit) → its sequence of <see cref="CollationElement"/>s. The <see cref="Root"/> table is generated
/// from the Unicode CLDR root collation and UCA data by <c>scripts/collation/generate-collation-table.py</c>
/// (sources, versions and hashes in <c>Data/root-collation.manifest.json</c>; the pinned inputs live under
/// <c>data/unicode/</c>) and is embedded in this assembly, so every WiseOwl COBOL program on every host orders text
/// identically — the order does not depend on the ICU build the operating system happens to ship.
/// <para>A table answers three questions the collation engine asks while walking a string: the elements of a code
/// point (<see cref="GetElements"/>; explicit mappings, plus the two computed families — Hangul syllables through
/// their conjoining jamo, and the UTS #10 Table 16 IMPLICIT weights of Han/Tangut/Nushu/Khitan ideographs and of
/// unassigned code points), the contractions that begin with a code point (Thai/Lao prevowel + consonant, tailored
/// digraphs …), and whether a code point is a NON-STARTER (canonical combining class ≠ 0 — the trigger for canonical
/// reordering and discontiguous contraction matching). It also carries the CLDR root's REORDERING GROUPS
/// (<see cref="ReorderGroups"/>: space, punct, symbol, currency, digit, then one group per script, each a contiguous
/// primary range) — what a CLDR <c>[reorder …]</c> permutes and <see cref="MaxVariable"/> reads.</para>
/// <para>Tables are IMMUTABLE. A tailoring does not mutate a table; it produces a NEW table — <see cref="WithTailoring"/>
/// for a numeric <see cref="TailoringRules"/> file, <see cref="Rebuild"/> for the CLDR rule builder — that shares
/// nothing mutable with its base, so a run unit can hold the root table and any number of locale-tailored tables side
/// by side. A tailored table's weights may be RENUMBERED relative to the root's (a CLDR relation that needs room
/// between two adjacent root weights widens that gap; a <c>[reorder]</c> moves whole groups); the table's
/// <see cref="PrimaryMap"/> / <see cref="SecondaryMap"/> / <see cref="TertiaryMap"/> record the root → this-table
/// mapping so a root-scale weight (a <c>.tailor</c> file's) can still be layered on top correctly.</para>
/// <para>⚖ No ISO/IEC 14651 text or table is embedded here or read by the generator; the data is Unicode's
/// (data/unicode/LICENSE-UNICODE.txt). WiseOwl COBOL's conformance statement, verbatim: "Implements collation behavior
/// consistent with ISO/IEC 14651 through derived tables and CLDR/UCA data."</para>
/// </summary>
public sealed class CollationTable
{
    /// <summary>The embedded resource holding the generated root table (LogicalName in Cobol.Net.Runtime.csproj).</summary>
    public const string RootResourceName = "Collation/Data/root-collation.bin";

    private const int Hangul_SBase = 0xAC00, Hangul_LBase = 0x1100, Hangul_VBase = 0x1161, Hangul_TBase = 0x11A7;
    private const int Hangul_LCount = 19, Hangul_VCount = 21, Hangul_TCount = 28;
    private const int Hangul_NCount = Hangul_VCount * Hangul_TCount, Hangul_SCount = Hangul_LCount * Hangul_NCount;

    private static readonly Lazy<CollationTable> s_root = new(LoadRoot, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The root table — the CLDR root collation order (UCA <see cref="UcaVersion"/>), loaded once per process.</summary>
    public static CollationTable Root => s_root.Value;

    /// <summary>A contraction: the code points AFTER the first one, and the element sequence the whole contraction maps to.</summary>
    internal readonly record struct Contraction(int[] Rest, int Offset, int Count);

    private readonly record struct ImplicitRange(int First, int Last, int Base, int Subtract, byte Kind);

    private readonly CollationElement[] _elements;               // the element pool (primaries already shifted)
    private readonly Dictionary<int, int> _singles;               // code point → (offset << 8) | count
    private readonly Dictionary<int, Contraction[]> _contractions; // first code point → candidates, longest first
    private readonly Dictionary<int, byte> _ccc;                  // non-starters only (ccc ≠ 0)
    private readonly ImplicitRange[] _implicit;                   // UTS #10 Table 16 rows, sorted by First
    private readonly Dictionary<int, int[]> _nfd;                 // code point → its FULL canonical decomposition (expanded, reordered)
    private readonly int _primaryShift;
    private readonly ReorderGroup[] _groups;                      // in this table's primary order

    private CollationTable(string name, string ucaVersion, string sourceTag, int primaryShift, CollationElement[] elements,
        Dictionary<int, int> singles, Dictionary<int, Contraction[]> contractions, Dictionary<int, byte> ccc,
        ImplicitRange[] implicitRanges, Dictionary<int, int[]> nfd, ReorderGroup[] groups, TailoringRules? tailoring,
        string? description, WeightMap? primaryMap, WeightMap? secondaryMap, WeightMap? tertiaryMap)
    {
        Name = name;
        UcaVersion = ucaVersion;
        SourceTag = sourceTag;
        _primaryShift = primaryShift;
        _elements = elements;
        _singles = singles;
        _contractions = contractions;
        _ccc = ccc;
        _implicit = implicitRanges;
        _nfd = nfd;
        _groups = groups;
        Tailoring = tailoring;
        Description = description ?? name;
        PrimaryMap = primaryMap;
        SecondaryMap = secondaryMap;
        TertiaryMap = tertiaryMap;
    }

    /// <summary>"root", or the tailoring's name (its <c>@locale</c>, else its source name; a CLDR collation's
    /// locale tag such as "es" or "de-u-co-phonebk").</summary>
    public string Name { get; }

    /// <summary>A one-line description of what this table is built from — for diagnostics.</summary>
    public string Description { get; }

    /// <summary>The UCA / CLDR data version the table derives from (e.g. "17.0.0").</summary>
    public string UcaVersion { get; }

    /// <summary>The generator's description of the source data.</summary>
    public string SourceTag { get; }

    /// <summary>The numeric <c>.tailor</c> rules this table was built with, or null (the root, or a CLDR-derived table
    /// with no <c>.tailor</c> layer).</summary>
    public TailoringRules? Tailoring { get; }

    /// <summary>The number of low-order bits every root primary is shifted left by (tailoring room): a root primary
    /// is <c>source &lt;&lt; PrimaryShift</c>, and the values strictly between two adjacent root primaries are free.</summary>
    public int PrimaryShift => _primaryShift;

    /// <summary>The number of explicit single-code-point mappings.</summary>
    public int MappingCount => _singles.Count;

    /// <summary>The number of contractions (multi-code-point mappings).</summary>
    public int ContractionCount { get; private init; }

    /// <summary>The longest contraction in code points (1 when there are none) — the reach of context: a code point's
    /// element sequence can depend on at most this many code points after it. <see cref="Collator"/>'s
    /// identical-prefix skip backs its boundary up by this much.</summary>
    public int MaxContractionLength { get; private init; } = 1;

    /// <summary>The number of precomposed characters <see cref="Rebuild"/> re-derived from tailored components — the
    /// canonical closure over composites (diagnostics; 0 for the root).</summary>
    public int ClosedComposites { get; private init; }

    /// <summary>The number of elements in the pool (diagnostics).</summary>
    public int ElementCount => _elements.Length;

    /// <summary>The root → this-table mapping of PRIMARY weights, or null when this table keeps the root's primaries
    /// (the root itself, and every tailoring that only inserted into free room). A tailored table whose CLDR rules
    /// needed a wider gap, or that reordered groups, renumbers — this map is how a root-scale weight (a
    /// <c>.tailor</c> entry) is translated when layered on top (<see cref="WithTailoring"/>).</summary>
    public WeightMap? PrimaryMap { get; }

    /// <summary>The root → this-table mapping of SECONDARY weights, or null (identity).</summary>
    public WeightMap? SecondaryMap { get; }

    /// <summary>The root → this-table mapping of TERTIARY weights, or null (identity).</summary>
    public WeightMap? TertiaryMap { get; }

    /// <summary>True when some weight of this table differs from the root's for the same source element — a
    /// <c>.tailor</c> weight written against the root scale must be mapped before it is applied here.</summary>
    public bool IsRenumbered => PrimaryMap is not null || SecondaryMap is not null || TertiaryMap is not null;

    /// <summary>The CLDR root's REORDERING GROUPS (UTS #35 Part 5, "Collation Reordering") in THIS table's primary
    /// order, each a contiguous primary range in this table's scale: the five special groups (<c>space</c>,
    /// <c>punct</c>, <c>symbol</c>, <c>currency</c>, <c>digit</c>) followed by one group per script, named by its
    /// ISO 15924 code (<c>Latn</c>, <c>Grek</c>, … <c>Hani</c>); a group that holds several scripts (Hiragana +
    /// Katakana → "Hira Kana") lists every code. What a CLDR <c>[reorder …]</c> permutes; the primaries below the
    /// first group (U+FFFE's) and above the last (unassigned implicit weights, the trailing U+FFFD..U+FFFF) belong to
    /// no group and never move.</summary>
    public IReadOnlyList<ReorderGroup> ReorderGroups => _groups;

    /// <summary>The group named by a CLDR reorder code — a script code (case-insensitive; <c>Hrkt</c> = the Kana group,
    /// <c>Zyyy</c>/<c>Zinh</c> name no group) or one of the special codes — or false.</summary>
    public bool TryGetReorderGroup(string code, out ReorderGroup group)
    {
        if (string.Equals(code, "Hrkt", StringComparison.OrdinalIgnoreCase)) code = "Kana";
        foreach (var g in _groups)
            foreach (string c in g.Codes)
                if (string.Equals(c, code, StringComparison.OrdinalIgnoreCase)) { group = g; return true; }
        group = default;
        return false;
    }

    /// <summary>True when some contraction starts with <paramref name="codePoint"/> — its element sequence then
    /// depends on what follows it.</summary>
    public bool StartsContraction(int codePoint) => _contractions.Count != 0 && _contractions.ContainsKey(codePoint);

    /// <summary>The primary weight the highest possible explicit or implicit primary cannot exceed — the level-4
    /// "no variable here" filler of UTS #10 Table 12 (0xFFFF in source scale, shifted like every primary).</summary>
    public int MaxPrimary => 0xFFFF << _primaryShift;

    /// <summary>The FIRST collation element of <paramref name="codePoint"/>'s sequence — the (primary, secondary,
    /// tertiary) triple that decides its coarse position. Sufficient for the single-element majority (letters, digits,
    /// punctuation, precomposed letters' base weight); use <see cref="GetElements"/> when the whole sequence matters
    /// (expansions such as æ or ß, Hangul syllables, implicit-weight ideographs).</summary>
    public CollationElement Lookup(int codePoint)
    {
        if (_singles.TryGetValue(codePoint, out int packed))
            return _elements[packed >> 8];
        if (IsHangulSyllable(codePoint))
            return Lookup(Hangul_LBase + (codePoint - Hangul_SBase) / Hangul_NCount);
        GetImplicit(codePoint, out var first, out _);
        return first;
    }

    /// <summary>The complete collation element sequence of one code point: its explicit mapping, else (Hangul
    /// syllable) the concatenated elements of its conjoining jamo, else the two-element UTS #10 Table 16 implicit
    /// weight. Never empty; a completely ignorable code point yields one all-zero element.</summary>
    public ReadOnlyMemory<CollationElement> GetElements(int codePoint)
    {
        if (TryGetSingle(codePoint, out int offset, out int count))
            return new ReadOnlyMemory<CollationElement>(_elements, offset, count);
        if (IsHangulSyllable(codePoint))
        {
            int n = DecomposeHangul(codePoint, out int l, out int v, out int t);
            var list = new List<CollationElement>(4);
            foreach (int jamo in n == 3 ? [l, v, t] : new[] { l, v })
            {
                if (TryGetSingle(jamo, out int jo, out int jc)) list.AddRange(_elements.AsSpan(jo, jc));
                else { GetImplicit(jamo, out var a, out var b); list.Add(a); list.Add(b); }
            }
            return list.ToArray();
        }
        GetImplicit(codePoint, out var first, out var second);
        return new[] { first, second };
    }

    /// <summary>The element sequence of a code point SEQUENCE — one contraction when the table has it, else the
    /// concatenated per-code-point sequences (Hangul and implicit weights included). What a rule builder needs to
    /// read the current position of a reset string.</summary>
    public CollationElement[] GetElements(ReadOnlySpan<int> codePoints)
    {
        if (codePoints.Length == 1) return GetElements(codePoints[0]).ToArray();
        if (codePoints.Length > 1 && _contractions.TryGetValue(codePoints[0], out var candidates))
        {
            var rest = codePoints[1..];
            foreach (var c in candidates)
                if (c.Rest.AsSpan().SequenceEqual(rest))
                    return _elements.AsSpan(c.Offset, c.Count).ToArray();
        }
        var list = new List<CollationElement>(codePoints.Length * 2);
        foreach (int cp in codePoints) list.AddRange(GetElements(cp).ToArray());
        return list.ToArray();
    }

    /// <summary>True when the table carries an explicit mapping for <paramref name="codePoint"/> (as opposed to a
    /// computed Hangul or implicit weight).</summary>
    public bool HasExplicitMapping(int codePoint) => _singles.ContainsKey(codePoint);

    /// <summary>True when the table maps exactly this code point sequence as a contraction.</summary>
    public bool HasContraction(ReadOnlySpan<int> codePoints)
    {
        if (codePoints.Length < 2 || !_contractions.TryGetValue(codePoints[0], out var candidates)) return false;
        var rest = codePoints[1..];
        foreach (var c in candidates)
            if (c.Rest.AsSpan().SequenceEqual(rest)) return true;
        return false;
    }

    /// <summary>True when the code point is a NON-STARTER — canonical combining class ≠ 0 (combining marks): the
    /// presence of one is what makes canonical reordering (NFD) and discontiguous contraction matching necessary.</summary>
    public bool IsNonStarter(int codePoint) => codePoint >= 0x300 && _ccc.ContainsKey(codePoint);

    /// <summary>The canonical combining class of <paramref name="codePoint"/> (0 for every starter).</summary>
    public int CombiningClass(int codePoint) => codePoint >= 0x300 && _ccc.TryGetValue(codePoint, out byte c) ? c : 0;

    /// <summary>The FULL canonical decomposition of a code point (recursively expanded and canonically ordered —
    /// what NFD maps the character to), or false for a code point that is its own NFD. Hangul syllables are
    /// algorithmic (<see cref="DecomposeHangul"/>) and answer false here.</summary>
    public bool TryGetCanonicalDecomposition(int codePoint, out ReadOnlySpan<int> decomposition)
    {
        if (codePoint >= 0xC0 && _nfd.TryGetValue(codePoint, out var d)) { decomposition = d; return true; }
        decomposition = default;
        return false;
    }

    /// <summary>The number of code points with a canonical decomposition mapping (Hangul syllables excluded).</summary>
    public int CanonicalDecompositionCount => _nfd.Count;

    /// <summary>Every code point with a canonical decomposition and its full NFD sequence — the set
    /// <see cref="Rebuild"/>'s canonical closure covers, enumerated by the drift test that checks it.</summary>
    internal IEnumerable<KeyValuePair<int, int[]>> CanonicalDecompositions() => _nfd;

    /// <summary>The contractions that begin with <paramref name="codePoint"/>, LONGEST FIRST, or null.</summary>
    internal Contraction[]? ContractionsStartingWith(int codePoint) =>
        _contractions.Count != 0 && _contractions.TryGetValue(codePoint, out var c) ? c : null;

    /// <summary>The explicit mapping of a code point as a slice of the element pool.</summary>
    internal bool TryGetSingle(int codePoint, out int offset, out int count)
    {
        if (_singles.TryGetValue(codePoint, out int packed))
        {
            offset = packed >> 8;
            count = packed & 0xFF;
            return true;
        }
        offset = count = 0;
        return false;
    }

    /// <summary>A slice of the element pool.</summary>
    internal ReadOnlySpan<CollationElement> Slice(int offset, int count) => _elements.AsSpan(offset, count);

    /// <summary>One element of the pool.</summary>
    internal CollationElement ElementAt(int index) => _elements[index];

    /// <summary>The whole element pool (read-only) — what a rule builder scans for the weights in use.</summary>
    internal ReadOnlySpan<CollationElement> Pool => _elements;

    /// <summary>Every explicit single mapping (code point → pool slice) — for builders and diagnostics.</summary>
    internal IEnumerable<(int CodePoint, int Offset, int Count)> Singles()
    {
        foreach (var (cp, packed) in _singles) yield return (cp, packed >> 8, packed & 0xFF);
    }

    /// <summary>Every contraction (full code point sequence → pool slice) — for builders and diagnostics.</summary>
    internal IEnumerable<(int[] CodePoints, int Offset, int Count)> Contractions()
    {
        foreach (var (first, list) in _contractions)
            foreach (var c in list)
            {
                var cps = new int[c.Rest.Length + 1];
                cps[0] = first;
                c.Rest.CopyTo(cps, 1);
                yield return (cps, c.Offset, c.Count);
            }
    }

    internal static bool IsHangulSyllable(int codePoint) => (uint)(codePoint - Hangul_SBase) < Hangul_SCount;

    /// <summary>Algorithmic Hangul syllable → conjoining jamo (L V [T]) decomposition (The Unicode Standard §3.12) —
    /// the NFD of a precomposed syllable, without a normalization call. Returns the jamo count (2 or 3; <paramref name="t"/>
    /// is 0 for an LV syllable).</summary>
    internal static int DecomposeHangul(int syllable, out int l, out int v, out int t)
    {
        int s = syllable - Hangul_SBase;
        l = Hangul_LBase + s / Hangul_NCount;
        v = Hangul_VBase + s % Hangul_NCount / Hangul_TCount;
        int tIndex = s % Hangul_TCount;
        if (tIndex == 0) { t = 0; return 2; }
        t = Hangul_TBase + tIndex;
        return 3;
    }

    /// <summary>The UTS #10 (rev. 53) Table 16 IMPLICIT weight of a code point with no explicit mapping: two elements
    /// [.AAAA.0020.0002][.BBBB.0000.0000] where the (AAAA, BBBB) pair is computed from the code point's script family
    /// (siniform scripts by block, core Han, other Han, everything else — including unassigned code points and, for
    /// robustness over ill-formed input, an unpaired surrogate code unit). Sorts after every explicit primary and
    /// before the U+FFFF maximum. In a table whose primaries were renumbered (<see cref="PrimaryMap"/>) the AAAA
    /// primary is mapped like every other primary, so a reordered Han group stays reordered for implicit weights too;
    /// BBBB (compared only against another BBBB) is never mapped.</summary>
    internal void GetImplicit(int codePoint, out CollationElement first, out CollationElement second)
    {
        int aaaa, bbbb;
        int lo = 0, hi = _implicit.Length - 1, found = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var r = _implicit[mid];
            if (codePoint < r.First) hi = mid - 1;
            else if (codePoint > r.Last) lo = mid + 1;
            else { found = mid; break; }
        }
        if (found >= 0)
        {
            var r = _implicit[found];
            if (r.Kind == 0)   // siniform: AAAA = base, BBBB = (CP − start) | 0x8000
            {
                aaaa = r.Base;
                bbbb = (codePoint - r.Subtract) | 0x8000;
            }
            else               // Han core/other: AAAA = base + (CP >> 15), BBBB = (CP & 0x7FFF) | 0x8000
            {
                aaaa = r.Base + (codePoint >> 15);
                bbbb = (codePoint & 0x7FFF) | 0x8000;
            }
        }
        else                   // "any other code point"
        {
            aaaa = 0xFBC0 + (codePoint >> 15);
            bbbb = (codePoint & 0x7FFF) | 0x8000;
        }
        int p = aaaa << _primaryShift;
        if (PrimaryMap is not null) p = PrimaryMap.Map(p);
        first = new CollationElement(p, 0x0020, 0x0002);
        second = new CollationElement(bbbb << _primaryShift, 0, 0);
    }

    // ---- derived tables --------------------------------------------------------------------------------------

    /// <summary>A NEW table: this table's mappings with <paramref name="rules"/>' entries layered over them (an entry
    /// REPLACES the whole element sequence of its code point / contraction). The rules' weights are written against
    /// the ROOT scale; when this table renumbered (<see cref="IsRenumbered"/>) they are translated through its maps
    /// first, so a site's <c>.tailor</c> composes with a CLDR-derived table. Canonical closure is automatic and the
    /// same as for a CLDR collation (<see cref="Rebuild"/>): a tailored key's decomposed spelling maps like the key,
    /// and every precomposed character containing a tailored code point is re-derived from its components — so
    /// tailoring c moves ç with it, and the precomposed and decomposed spellings keep collating identically.</summary>
    public CollationTable WithTailoring(TailoringRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.UcaVersion is { } v && !string.Equals(v, UcaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"tailoring '{rules.Name}' was written for UCA {v} but the base table is UCA {UcaVersion} — its numeric weights are not comparable");
        if (rules.Entries.Count == 0) return this;   // a header-only tailoring (en-US, fr-FR: "the root order is valid") IS this table

        var entries = new List<(int[] CodePoints, CollationElement[] Elements)>(rules.Entries.Count);
        foreach (var e in rules.Entries)
        {
            var mapped = e.Elements;
            if (IsRenumbered)
            {
                mapped = new CollationElement[e.Elements.Length];
                for (int i = 0; i < mapped.Length; i++)
                {
                    var ce = e.Elements[i];
                    mapped[i] = ce with
                    {
                        Primary = ce.Primary != 0 && PrimaryMap is not null ? PrimaryMap.Map(ce.Primary) : ce.Primary,
                        Secondary = ce.Secondary != 0 && SecondaryMap is not null ? SecondaryMap.Map(ce.Secondary) : ce.Secondary,
                        Tertiary = ce.Tertiary != 0 && TertiaryMap is not null ? TertiaryMap.Map(ce.Tertiary) : ce.Tertiary,
                    };
                }
            }
            entries.Add((e.CodePoints, mapped));
        }
        return Rebuild(new TailoringPlan
        {
            Name = rules.Name,
            Description = $"{Description} + {rules.Source}",
            Tailoring = rules,
            Entries = entries,
            PrimaryMap = PrimaryMap,
            SecondaryMap = SecondaryMap,
            TertiaryMap = TertiaryMap,
        });
    }

    /// <summary>The ONE derivation step every tailoring goes through: a NEW table from this one and a
    /// <see cref="TailoringPlan"/> — the base pool re-weighted through the plan's remapping (renumbering, reordering;
    /// identity when null), the plan's entries added or replacing existing mappings, contractions starting with the
    /// plan's suppressed code points removed, the result CANONICALLY CLOSED (the tailored keys' NFD spellings, and
    /// every precomposed character whose decomposition the tailoring reaches), and the plan's group ranges and
    /// root-scale maps recorded on the new table.</summary>
    internal CollationTable Rebuild(TailoringPlan plan)
    {
        var singles = new Dictionary<int, int>(_singles);
        var contractions = new Dictionary<int, Contraction[]>(_contractions);
        int contractionCount = ContractionCount;
        if (plan.SuppressContractionsStartingWith is { Count: > 0 } suppress)
        {
            foreach (int cp in suppress)
            {
                if (contractions.Remove(cp, out var removed)) contractionCount -= removed.Length;
            }
        }
        var pool = new List<CollationElement>(_elements.Length + plan.Entries.Count * 2);
        if (plan.Remap is { } remap)
            foreach (var e in _elements) pool.Add(remap(e));
        else
            pool.AddRange(_elements);

        // The code points the tailoring can change the walk of: every code point of a key it maps, and the first code
        // point of every contraction it suppresses. A composite whose decomposition holds none of them walks exactly
        // as it does in this (already canonically closed) table.
        var touched = new HashSet<int>();
        if (plan.SuppressContractionsStartingWith is { } suppressed) touched.UnionWith(suppressed);
        foreach (var (codePoints, elements) in plan.Entries)
        {
            int offset = pool.Count;
            pool.AddRange(elements);
            AddMapping(singles, contractions, codePoints, offset, elements.Length, ref contractionCount);
            touched.UnionWith(codePoints);
        }

        // CANONICAL CLOSURE (the Collator.Compare contract: canonically equivalent texts compare equal; Normalizer walks
        // a text with no non-starter as it is, trusting the table to be closed). ONE step for every front-end — a CLDR
        // rule set and a numeric .tailor file alike — as ICU's builder closes every tailoring:
        // (1) every tailored key that is not in NFD (a precomposed letter, a contraction whose first code point
        //     decomposes, marks out of canonical order) also maps its NFD spelling — what Normalizer turns the text
        //     into whenever it holds a combining mark — unless the plan maps that spelling itself;
        foreach (var (codePoints, elements) in plan.Entries)
        {
            var nfd = CanonicalForm(codePoints);
            if (nfd is null || plan.Defines(nfd)) continue;
            int offset = pool.Count;
            pool.AddRange(elements);
            AddMapping(singles, contractions, nfd, offset, elements.Length, ref contractionCount);
            touched.UnionWith(nfd);
        }
        // (2) every precomposed character the plan does not map itself, whose decomposition holds a code point the
        //     tailoring touched, takes the elements its decomposition walks to under the TAILORED mappings (longest
        //     contraction, discontiguous over unblocked marks — the engine's own walk, UTS #10 S2.1), when they differ
        //     from what it maps to now: tailoring c moves ç; tailoring a mark moves every letter carrying it; a
        //     tailored ă reaches ặ = a + dot below + breve. Hangul syllables are algorithmic and never mapped.
        var provisional = Derived(plan, pool.ToArray(), singles, contractions, contractionCount);
        var composites = new List<(int CodePoint, CollationElement[] Elements)>();
        if (touched.Count > 0)
        {
            foreach (var (cp, decomposition) in _nfd)
            {
                if (plan.Defines([cp]) || !decomposition.Any(touched.Contains)) continue;
                var walked = provisional.Walk(decomposition);
                if (!provisional.GetElements(cp).Span.SequenceEqual(walked)) composites.Add((cp, walked));
            }
        }
        if (composites.Count == 0) return provisional;
        foreach (var (cp, elements) in composites)
        {
            int offset = pool.Count;
            pool.AddRange(elements);
            AddMapping(singles, contractions, [cp], offset, elements.Length, ref contractionCount);
        }
        return Derived(plan, pool.ToArray(), singles, contractions, contractionCount, composites.Count);
    }

    /// <summary>A table over this one's Unicode data with the given mappings and the plan's names, groups and maps.</summary>
    private CollationTable Derived(TailoringPlan plan, CollationElement[] pool, Dictionary<int, int> singles,
        Dictionary<int, Contraction[]> contractions, int contractionCount, int closedComposites = 0)
    {
        int longest = 1;
        foreach (var list in contractions.Values)
            foreach (var c in list) longest = Math.Max(longest, c.Rest.Length + 1);
        return new CollationTable(plan.Name, UcaVersion, SourceTag, _primaryShift, pool, singles, contractions,
            _ccc, _implicit, _nfd, plan.Groups ?? _groups, plan.Tailoring, plan.Description, plan.PrimaryMap, plan.SecondaryMap, plan.TertiaryMap)
        { ContractionCount = contractionCount, MaxContractionLength = longest, ClosedComposites = closedComposites };
    }

    /// <summary>The NFD of a code point sequence under this table's Unicode data, or null when the sequence is already
    /// in NFD.</summary>
    private int[]? CanonicalForm(int[] codePoints)
    {
        string text = TextOf(codePoints);
        if (!Normalizer.NeedsNfd(text, this, forIdentical: true)) return null;
        string nfd = Normalizer.ToNfd(text, this);
        var result = new List<int>(nfd.Length);
        for (int i = 0; i < nfd.Length; i++)
        {
            if (char.IsHighSurrogate(nfd[i]) && i + 1 < nfd.Length && char.IsLowSurrogate(nfd[i + 1])) { result.Add(char.ConvertToUtf32(nfd[i], nfd[i + 1])); i++; }
            else result.Add(nfd[i]);   // a BMP character, or an unpaired surrogate code unit as itself
        }
        return result.SequenceEqual(codePoints) ? null : result.ToArray();
    }

    /// <summary>The collation elements a (canonically ordered) code point sequence walks to in this table.</summary>
    private CollationElement[] Walk(int[] codePoints)
    {
        var elements = new List<CollationElement>(codePoints.Length + 2);
        var it = new CollationElementIterator(TextOf(codePoints), this);
        while (it.TryNext(out var e)) elements.Add(e);
        return elements.ToArray();
    }

    /// <summary>The UTF-16 text of a code point sequence; an unpaired surrogate code point stays one code unit.</summary>
    private static string TextOf(int[] codePoints)
    {
        var text = new System.Text.StringBuilder(codePoints.Length + 2);
        foreach (int cp in codePoints)
        {
            if (cp is >= 0xD800 and <= 0xDFFF) text.Append((char)cp);
            else text.Append(char.ConvertFromUtf32(cp));
        }
        return text.ToString();
    }

    private static void AddMapping(Dictionary<int, int> singles, Dictionary<int, Contraction[]> contractions,
        int[] codePoints, int offset, int count, ref int contractionCount)
    {
        if (codePoints.Length == 1)
        {
            singles[codePoints[0]] = (offset << 8) | count;
            return;
        }
        int[] rest = codePoints[1..];
        var existing = contractions.TryGetValue(codePoints[0], out var c) ? c : [];
        int i = Array.FindIndex(existing, e => e.Rest.AsSpan().SequenceEqual(rest));
        Contraction[] updated;
        if (i >= 0)
        {
            updated = (Contraction[])existing.Clone();
            updated[i] = new Contraction(rest, offset, count);
        }
        else
        {
            updated = new Contraction[existing.Length + 1];
            existing.CopyTo(updated, 0);
            updated[^1] = new Contraction(rest, offset, count);
            Array.Sort(updated, (a, b) => b.Rest.Length.CompareTo(a.Rest.Length));   // longest first
            contractionCount++;
        }
        contractions[codePoints[0]] = updated;
    }

    // ---- loading -------------------------------------------------------------------------------------------------

    private static CollationTable LoadRoot()
    {
        using var stream = typeof(CollationTable).Assembly.GetManifestResourceStream(RootResourceName)
            ?? throw new InvalidOperationException($"embedded collation table '{RootResourceName}' is missing from {typeof(CollationTable).Assembly.GetName().Name}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Decode(ms.ToArray(), "root");
    }

    /// <summary>Decode a table blob written by <c>generate-collation-table.py</c> ("CNCT", u32 raw length, raw-deflate
    /// payload; format 1, or format 2 = format 1 + the reordering groups and the element case bits). Public so a
    /// test can round-trip a regenerated file without touching the resource.</summary>
    public static CollationTable Decode(ReadOnlySpan<byte> blob, string name)
    {
        if (blob.Length < 8 || blob[0] != (byte)'C' || blob[1] != (byte)'N' || blob[2] != (byte)'C' || blob[3] != (byte)'T')
            throw new InvalidDataException("collation table: bad magic");
        int rawLength = BinaryPrimitives.ReadInt32LittleEndian(blob[4..]);
        var raw = new byte[rawLength];
        using (var inflate = new DeflateStream(new MemoryStream(blob[8..].ToArray()), CompressionMode.Decompress))
            inflate.ReadExactly(raw, 0, rawLength);

        var r = new Reader(raw);
        int format = r.U16();
        if (format is not (1 or 2)) throw new InvalidDataException($"collation table: unsupported format {format}");
        int shift = r.U8();
        string version = r.Str();
        string sourceTag = r.Str();

        int elementCount = r.I32();
        var elements = new CollationElement[elementCount];
        for (int i = 0; i < elementCount; i++)
        {
            int p = r.U16(), s = r.U16(), t = r.U8(), flags = r.U8();
            var elementCase = format >= 2
                ? ((flags & 4) != 0 ? ElementCase.Mixed : (flags & 2) != 0 ? ElementCase.Upper : ElementCase.Lower)
                : (CollationElement.IsUpperTertiary(t) ? ElementCase.Upper : ElementCase.Lower);
            elements[i] = new CollationElement(p << shift, s, t, (flags & 1) != 0, elementCase);
        }
        int singleCount = r.I32();
        var singles = new Dictionary<int, int>(singleCount);
        for (int i = 0; i < singleCount; i++)
        {
            int cp = r.I32(), offset = r.I32(), count = r.U8();
            singles.Add(cp, (offset << 8) | count);
        }
        int contractionCount = r.I32();
        var byFirst = new Dictionary<int, List<Contraction>>();
        for (int i = 0; i < contractionCount; i++)
        {
            int n = r.U8();
            var cps = new int[n];
            for (int k = 0; k < n; k++) cps[k] = r.I32();
            int offset = r.I32(), count = r.U8();
            if (!byFirst.TryGetValue(cps[0], out var list)) byFirst[cps[0]] = list = [];
            list.Add(new Contraction(cps[1..], offset, count));
        }
        var contractions = new Dictionary<int, Contraction[]>(byFirst.Count);
        foreach (var (first, list) in byFirst)
        {
            list.Sort((a, b) => b.Rest.Length.CompareTo(a.Rest.Length));   // longest first
            contractions[first] = list.ToArray();
        }
        int cccCount = r.I32();
        var ccc = new Dictionary<int, byte>(cccCount);
        for (int i = 0; i < cccCount; i++)
        {
            int cp = r.I32();
            ccc[cp] = (byte)r.U8();
        }
        int rangeCount = r.U16();
        var ranges = new ImplicitRange[rangeCount];
        for (int i = 0; i < rangeCount; i++)
        {
            int first = r.I32(), last = r.I32(), @base = r.U16(), subtract = r.I32(), kind = r.U8();
            ranges[i] = new ImplicitRange(first, last, @base, subtract, (byte)kind);
        }
        Array.Sort(ranges, (a, b) => a.First.CompareTo(b.First));
        int nfdCount = r.I32();
        var nfd = new Dictionary<int, int[]>(nfdCount);
        for (int i = 0; i < nfdCount; i++)
        {
            int cp = r.I32(), n = r.U8();
            var seq = new int[n];
            for (int k = 0; k < n; k++) seq[k] = r.I32();
            nfd.Add(cp, seq);
        }
        var groups = Array.Empty<ReorderGroup>();
        if (format >= 2)
        {
            int groupCount = r.U16();
            groups = new ReorderGroup[groupCount];
            for (int i = 0; i < groupCount; i++)
            {
                string codes = r.Str();
                int lo = r.U16(), hi = r.U16();
                groups[i] = new ReorderGroup(codes.Split(' ', StringSplitOptions.RemoveEmptyEntries), lo << shift, hi << shift);
            }
        }
        if (!r.AtEnd) throw new InvalidDataException("collation table: trailing bytes");
        int longest = 1;
        foreach (var list in contractions.Values)
            foreach (var c in list) longest = Math.Max(longest, c.Rest.Length + 1);
        return new CollationTable(name, version, sourceTag, shift, elements, singles, contractions, ccc, ranges, nfd, groups,
            tailoring: null, description: $"{name} ({sourceTag})", primaryMap: null, secondaryMap: null, tertiaryMap: null)
            { ContractionCount = contractionCount, MaxContractionLength = longest };
    }

    private ref struct Reader(byte[] data)
    {
        private readonly byte[] _d = data;
        private int _pos;
        public bool AtEnd => _pos == _d.Length;
        public int U8() => _d[_pos++];
        public int U16() { int v = BinaryPrimitives.ReadUInt16LittleEndian(_d.AsSpan(_pos)); _pos += 2; return v; }
        public int I32() { int v = BinaryPrimitives.ReadInt32LittleEndian(_d.AsSpan(_pos)); _pos += 4; return v; }
        public string Str() { int n = U8(); string s = System.Text.Encoding.UTF8.GetString(_d, _pos, n); _pos += n; return s; }
    }
}

/// <summary>One CLDR reordering group of a <see cref="CollationTable"/>: its reorder code(s) and the contiguous
/// primary range (that table's scale) its members occupy.</summary>
/// <param name="Codes">The CLDR reorder codes naming the group — one script code (<c>Latn</c>), several for a group
/// that holds several scripts (<c>Hira Kana</c>), or a special code (<c>space</c>, <c>punct</c>, <c>symbol</c>,
/// <c>currency</c>, <c>digit</c>).</param>
/// <param name="FirstPrimary">The lowest primary weight of a member.</param>
/// <param name="LastPrimary">The highest primary weight of a member.</param>
public readonly record struct ReorderGroup(string[] Codes, int FirstPrimary, int LastPrimary)
{
    /// <summary>The group's first (canonical) reorder code.</summary>
    public string Code => Codes[0];

    /// <summary>True for one of the five special (non-script) groups.</summary>
    public bool IsSpecial => Code is "space" or "punct" or "symbol" or "currency" or "digit";

    public override string ToString() => $"{string.Join('/', Codes)} [{FirstPrimary:X}..{LastPrimary:X}]";
}

/// <summary>
/// A mapping of ONE weight level from the root table's scale to a tailored table's — how a tailored table records
/// that it renumbered (a CLDR relation that needed more room than the free values between two adjacent root weights
/// widens the gap and shifts every higher weight) or reordered (a <c>[reorder]</c> moves whole groups of primaries).
/// Defined at every distinct root weight of the level by an explicit pair. A value strictly between two root weights
/// maps by integer interpolation between its neighbours' images when those are in order (distinct inputs stay
/// distinct because a gap is only ever widened), else right after the lower neighbour's image — so a root-scale
/// weight that is not itself a root weight (a <c>.tailor</c> file's inserted primary) keeps its place relative to its
/// neighbours.
/// </summary>
public sealed class WeightMap
{
    private readonly int[] _from, _to;

    /// <summary>Build from parallel arrays: <paramref name="from"/> strictly increasing root-scale weights,
    /// <paramref name="to"/> their images.</summary>
    public WeightMap(int[] from, int[] to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (from.Length != to.Length || from.Length == 0) throw new ArgumentException("a weight map needs parallel, non-empty arrays");
        for (int i = 1; i < from.Length; i++)
            if (from[i] <= from[i - 1]) throw new ArgumentException($"weight map sources are not increasing at {i}");
        _from = from;
        _to = to;
    }

    /// <summary>The number of defined root weights.</summary>
    public int Count => _from.Length;

    /// <summary>True when every root weight maps to itself.</summary>
    public bool IsIdentity
    {
        get
        {
            for (int i = 0; i < _from.Length; i++) if (_from[i] != _to[i]) return false;
            return true;
        }
    }

    /// <summary>The image of a root-scale weight.</summary>
    public int Map(int weight)
    {
        int i = Array.BinarySearch(_from, weight);
        if (i >= 0) return _to[i];
        i = ~i;   // first index with _from[i] > weight
        if (i == 0) return weight + (_to[0] - _from[0]);
        if (i == _from.Length) return weight + (_to[^1] - _from[^1]);
        int a = _from[i - 1], b = _from[i], na = _to[i - 1], nb = _to[i];
        if (nb > na && nb - na >= b - a) return na + (int)((long)(weight - a) * (nb - na) / (b - a));
        return na + (weight - a);   // across a reordering seam: stay right after the lower neighbour
    }
}

/// <summary>
/// The specification a derived <see cref="CollationTable"/> is built from (<see cref="CollationTable.Rebuild"/>) — the
/// one shape both tailoring front-ends produce: a numeric <c>.tailor</c> file (entries in root scale, no remapping)
/// and the CLDR rule builder (entries in the new scale, plus the remapping of every base element and the resulting
/// group ranges and root-scale maps).
/// </summary>
internal sealed class TailoringPlan
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public TailoringRules? Tailoring { get; init; }
    /// <summary>The re-weighting of every element of the base pool (renumbering + reordering), or null for identity.</summary>
    public Func<CollationElement, CollationElement>? Remap { get; init; }
    /// <summary>The mappings to add or replace, elements in the NEW table's scale.</summary>
    public required List<(int[] CodePoints, CollationElement[] Elements)> Entries { get; init; }
    /// <summary>First code points whose contractions are removed (CLDR <c>[suppressContractions]</c>).</summary>
    public HashSet<int>? SuppressContractionsStartingWith { get; init; }
    /// <summary>The reordering groups of the new table (its scale), or null to keep the base's.</summary>
    public ReorderGroup[]? Groups { get; init; }
    public WeightMap? PrimaryMap { get; init; }
    public WeightMap? SecondaryMap { get; init; }
    public WeightMap? TertiaryMap { get; init; }

    private HashSet<CodePointKey>? _keys;

    /// <summary>True when the plan itself defines this code point sequence (canonical closure must not override it).</summary>
    public bool Defines(ReadOnlySpan<int> codePoints)
    {
        _keys ??= new HashSet<CodePointKey>(Entries.Select(e => new CodePointKey(e.CodePoints)));
        return _keys.Contains(new CodePointKey(codePoints));
    }
}
