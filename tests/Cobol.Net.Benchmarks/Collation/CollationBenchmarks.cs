// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using BenchmarkDotNet.Attributes;
using CobolNet.Runtime.Collation;

namespace CobolNet.Benchmarks.Collation;

/// <summary>
/// The performance profile of the WiseOwl COBOL collation engine (<c>src/Cobol.Net.Runtime/Collation/</c>, kb/Work
/// PB101).
/// </summary>
/// <remarks>
/// <para>
/// ⚖ WHY THIS EXISTS. The engine is on the hot path of ordinary COBOL: every relation condition between two
/// alphanumeric operands under a LOCALE program collating sequence, every key comparison in <c>SORT</c> /
/// <c>MERGE</c>, every INDEXED-file key comparison, <c>MAX</c>/<c>MIN</c>, and <c>FUNCTION STANDARD-COMPARE</c>
/// all reach it (Collation/README.md §6). A <c>SORT</c> of a million records is tens of millions of calls to
/// <see cref="Collator.Compare(string?,string?)"/>, so its cost per comparison — and, just as much, its
/// ALLOCATION per comparison — is a product characteristic, not an implementation detail.
/// </para>
/// <para>
/// The engine deliberately does not delegate to <see cref="CompareInfo"/> (README §1: the host's ICU floats with
/// the operating system, and a decade-old INDEXED file must stay in key order). That decision is only defensible
/// if the table-driven implementation is in the same performance class as the ICU it declines to use — which is
/// why <see cref="CompareInfo"/> appears here as a baseline in every comparison category, alongside
/// <see cref="string.CompareOrdinal(string,string)"/> as the floor that no culturally-correct collation can reach.
/// </para>
/// <para>
/// WHAT A REGRESSION LOOKS LIKE:
/// </para>
/// <list type="bullet">
/// <item><b>Allocated ≠ 0 in <see cref="Compare_ShortStrings"/>, <see cref="Compare_LongStrings"/> or the
/// baselines' engine counterparts.</b> README §4 says the common path is allocation-free: the comparison streams
/// collation elements level by level and normalizes only when the text holds a combining mark. A non-zero
/// Allocated column on a pure-ASCII input means something started buffering the elements, materializing a key, or
/// boxing — and a SORT would feel it as GC pressure long before it felt it as CPU.</item>
/// <item><b><see cref="Compare_LongStrings"/> growing faster than linearly in length.</b> The comparison is one
/// forward pass per level; a super-linear shape means a re-walk crept in.</item>
/// <item><b><see cref="Compare_LongStrings_CaseOnly"/> drifting far from ~3× <see cref="Compare_LongStrings"/>.</b>
/// A case-only difference forces levels 1, 2 and 3 to be walked in full where a last-character primary difference
/// costs one walk. Much more than 3× means a level is doing redundant work; much less means a level is being
/// skipped, which would be a correctness bug the ordering tests should have caught first.</item>
/// <item><b><see cref="Compare_MixedLocaleStrings"/> jumping while the ASCII paths hold still.</b> That is the
/// NFD/expansion/implicit-weight machinery — the table-driven <c>Normalizer</c>, contraction matching, Hangul
/// decomposition and the UTS #10 Table 16 implicit weights.</item>
/// <item><b>The Ratio against <see cref="CompareInfo"/> worsening by a large factor.</b> The engine is expected
/// to sit within a small multiple of the host ICU. Losing an order of magnitude would reopen README §1's
/// trade-off, so the ratio, not the absolute nanoseconds, is the number to watch across hosts.</item>
/// </list>
/// <para>
/// The COBOL layer is measured too: <see cref="Compare_ShortStrings_LocaleCollation"/> drives
/// <see cref="CobolNet.Runtime.LocaleCollation.Current"/> — the <c>CobolCollation</c> carrier a program's
/// <c>ALPHABET … IS LOCALE</c> comparisons reach (DESIGN-locale-facility §4.4): the §8.8.4.2.11 trailing-space
/// trim, the well-formedness check, the per-use resolution of the run unit's current locale, then the engine — so
/// the carrier's dispatch overhead is separable from the engine's cost.
/// </para>
/// </remarks>
[Config(typeof(BenchmarkConfig))]
public class CollationBenchmarks
{
    // ---------------------------------------------------------------------------------------------------------
    // Categories. Ratios are computed WITHIN a category (BenchmarkConfig groups by category), so a 1.5 KB walk is
    // never ratioed against a five-letter word.
    // ---------------------------------------------------------------------------------------------------------
    private const string CompareShort = "Compare-Short";
    private const string CompareLong = "Compare-Long";
    private const string CompareMixed = "Compare-Mixed";
    private const string BuildKeyShort = "BuildKey-Short";
    private const string BuildKeyLong = "BuildKey-Long";

    /// <summary>
    /// The number of short pairs walked per invocation. Declared as a constant because
    /// <c>[Benchmark(OperationsPerInvoke = …)]</c> needs one, and asserted against the corpus in
    /// <see cref="Setup"/>: if the two ever drift apart the reported per-comparison cost is silently wrong by the
    /// ratio between them, which is exactly the kind of error a benchmark cannot show you.
    /// </summary>
    private const int ShortPairCount = 64;

    /// <summary>The number of mixed-script pairs walked per invocation (same drift check).</summary>
    private const int MixedPairCount = 12;

    /// <summary>The length of the long-string stem, i.e. 1.5 KB of text before the one differing character.</summary>
    private const int LongStemLength = 1535;

    /// <summary>
    /// 64 representative ASCII pairs, in eight groups of eight, each group a distinct shape of the comparison:
    /// where the difference is, whether there is one at all, and which LEVEL decides it. Real COBOL data is
    /// overwhelmingly this — short alphanumeric keys — so this is the corpus the headline number comes from.
    /// </summary>
    /// <remarks>
    /// Written out rather than generated so a reader can see exactly what is being timed. The groups are
    /// deliberately unequal in cost: a first-character difference exits after one collation element, an equal pair
    /// walks every level to the end, and a case-only difference walks three. Averaging over the eight shapes is
    /// what makes the single reported number representative instead of a best case.
    /// </remarks>
    private static readonly (string A, string B)[] ShortPairs =
    [
        // 1 — difference at the FIRST character: the level-1 walk exits after one element. The common case in a
        //     SORT over distinct keys, and the shape any optimisation must not slow down.
        ("apple", "banana"), ("cherry", "damson"), ("elder", "fig"), ("grape", "honey"),
        ("ivory", "jasper"), ("kiwi", "lemon"), ("mango", "nectar"), ("olive", "peach"),

        // 2 — difference in the MIDDLE: a few elements of shared prefix before the level-1 decision.
        ("station", "statute"), ("balance", "balcony"), ("account", "accrual"), ("invoice", "involve"),
        ("payment", "payroll"), ("customer", "customary"), ("shipping", "shipment"), ("register", "registry"),

        // 3 — difference at the LAST character: the whole string is walked at level 1 before it decides.
        ("abc", "abd"), ("alpha", "alphb"), ("delta", "deltb"), ("gamma", "gammb"),
        ("ledger", "ledges"), ("cursor", "cursos"), ("branch", "brancg"), ("vendor", "vendos"),

        // 4 — CASE-only difference: equal at levels 1 and 2, decided at level 3, so three full walks.
        ("Apple", "apple"), ("BANANA", "banana"), ("Cherry", "cherry"), ("DELTA", "delta"),
        ("Echo", "echo"), ("FOXTROT", "foxtrot"), ("Golf", "golf"), ("HOTEL", "hotel"),

        // 5 — EQUAL: the worst case. Every configured level is walked to the end and still ties.
        ("identical", "identical"), ("account", "account"), ("balance", "balance"), ("customer", "customer"),
        ("invoice", "invoice"), ("payment", "payment"), ("shipping", "shipping"), ("register", "register"),

        // 6 — PREFIX: one operand runs out first (the end-of-input branch, "a proper prefix is less").
        ("app", "apple"), ("bal", "balance"), ("cust", "customer"), ("inv", "invoice"),
        ("pay", "payment"), ("ship", "shipping"), ("reg", "register"), ("tot", "total"),

        // 7 — DIGITS and mixed alphanumerics: a different region of the table (CLDR groups digits before letters),
        //     and the shape of a real record key.
        ("00012345", "00012346"), ("2024-01-01", "2024-01-02"), ("A1000", "A1001"), ("PO-7781", "PO-7782"),
        ("9999", "10000"), ("ITEM0001", "ITEM0002"), ("X42", "X43"), ("0.00", "0.01"),

        // 8 — PUNCTUATION and spaces: VARIABLE elements. Under Root (non-ignorable) they carry primaries; under
        //     Standard (shifted) they are ignored through level 3 and weighted at level 4 — so these eight pairs
        //     are the ones on which Compare_ShortStrings and Compare_ShortStrings_Standard do different work.
        ("a-b", "ab"), ("co-op", "coop"), ("O'Brien", "OBrien"), ("de la Cruz", "dela Cruz"),
        ("ACME, INC.", "ACME INC"), ("part_no", "part no"), ("A/B", "AB"), ("re-order", "reorder"),
    ];

    /// <summary>
    /// 12 pairs that leave the ASCII fast path: accents (a level-2 decision), the expansions that turn one
    /// character into several collation elements, precomposed vs. decomposed text (which forces the table-driven
    /// NFD walk), Han and Hangul (no table entry at all — implicit weights and jamo decomposition), and Spanish ñ.
    /// </summary>
    /// <remarks>
    /// The last pair is the one that MOVES between <see cref="Compare_MixedLocaleStrings"/> and
    /// <see cref="Compare_MixedLocaleStrings_SpanishTailored"/>: under the root order ñ is n with a level-2
    /// difference, under the es-ES tailoring it has a primary of its own between n and o, so "año" and "ano" are
    /// decided at a different level by the two collators.
    /// </remarks>
    private static readonly (string A, string B)[] MixedPairs =
    [
        ("résumé", "resume"),                       // accented Latin: equal at level 1, decided at level 2
        ("café", "cafe"),                           // ditto, one accent
        ("Ångström", "Angstrom"),                   // ring above + diaeresis
        ("straße", "strasse"),                      // ß EXPANDS to two elements (s + s) with a tertiary difference
        ("Æon", "Aeon"),                            // Æ expands to a + e
        ("œuvre", "oeuvre"),                        // œ expands to o + e
        ("e\U00000301critoire", "écritoire"),       // combining acute vs. precomposed: the NFD path; canonically EQUAL
        ("Cha\U00000302teau", "Château"),           // combining circumflex vs. precomposed: also canonically EQUAL
        ("中文字符", "中文字元"),                     // Han: no table entry — UTS #10 Table 16 implicit weights
        ("日本語", "日本国"),                         // Han again, deciding on the third character
        ("한국어", "한국말"),                         // Hangul syllables decomposed to conjoining jamo
        ("año", "ano"),                             // Spanish ñ: level 2 under Root, level 1 under the es-ES tailoring
    ];

    // ---------------------------------------------------------------------------------------------------------
    // State. Everything is built ONCE in [GlobalSetup] so no benchmark pays for string construction, table
    // decoding or tailoring resolution. The `null!` initialisers say "assigned in Setup" to the nullable analysis.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>The four-level, shifted configuration STANDARD-COMPARE's default ordering table names.</summary>
    private Collator _standard = null!;

    /// <summary>The es-ES tailored collator. Resolved once — <c>ForLocale</c> caches, but not for free.</summary>
    private Collator _spanish = null!;

    /// <summary>1.5 KB of ASCII text plus a final 'a'.</summary>
    private string _longA = "";

    /// <summary>The same 1.5 KB stem plus a final 'b' — a level-1 difference at the very last element.</summary>
    private string _longB = "";

    /// <summary>The same 1.5 KB stem plus a final 'A' — equal at levels 1 and 2, decided at level 3.</summary>
    private string _longCased = "";

    /// <summary>The host's ICU, through the invariant culture: the external oracle README §1 declines to depend on.</summary>
    private readonly CompareInfo _icu = CultureInfo.InvariantCulture.CompareInfo;

    [GlobalSetup]
    public void Setup()
    {
        // Drift guards: OperationsPerInvoke is a compile-time constant and the corpora are not, so an added pair
        // would silently rescale every per-operation number in the report. Fail loudly instead.
        if (ShortPairs.Length != ShortPairCount)
            throw new InvalidOperationException($"ShortPairs holds {ShortPairs.Length} pairs; OperationsPerInvoke says {ShortPairCount}.");
        if (MixedPairs.Length != MixedPairCount)
            throw new InvalidOperationException($"MixedPairs holds {MixedPairs.Length} pairs; OperationsPerInvoke says {MixedPairCount}.");

        _standard = CollationEngine.Standard;
        _spanish = CollationEngine.ForLocale("es-ES");

        // 1.5 KB of ordinary ASCII words — no combining marks, so the comparison stays on the un-normalized path
        // and the measurement is of the element walk itself rather than of NFD.
        const string Filler = "the quick brown fox jumps over the lazy dog while the ledger balances and the "
                            + "invoice register reconciles every posted payment against the shipping manifest ";
        var stem = new System.Text.StringBuilder(LongStemLength + Filler.Length);
        while (stem.Length < LongStemLength)
            stem.Append(Filler);
        stem.Length = LongStemLength;

        _longA = stem.ToString() + "a";
        _longB = stem.ToString() + "b";
        _longCased = stem.ToString() + "A";

        // Force the one-off costs out of the first measured iteration: decoding the embedded root table (~228 KB
        // deflated) and resolving the es-ES tailoring both happen lazily on first use.
        _ = CollationEngine.Compare("a", "b");
        _ = _standard.Compare("a", "b");
        _ = _spanish.Compare("a", "b");
        _ = CollationKey.Build("a");
        _ = _icu.Compare("a", "b", CompareOptions.None);
        _ = _icu.GetSortKey("a");
    }

    // =========================================================================================================
    // Compare-Short — the headline number: one comparison of two short alphanumeric operands.
    // =========================================================================================================

    /// <summary>
    /// <see cref="CollationEngine.Compare(string?,string?)"/> — the root order (tertiary strength, non-ignorable
    /// variables), which is what an untailored locale and the default program collating sequence resolve to.
    /// This is the number that multiplies by the record count in a SORT.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ShortPairCount), BenchmarkCategory(CompareShort)]
    public int Compare_ShortStrings()
    {
        int acc = 0;
        var pairs = ShortPairs;
        for (int i = 0; i < pairs.Length; i++)
            acc += CollationEngine.Compare(pairs[i].A, pairs[i].B);
        return acc;
    }

    /// <summary>
    /// The same corpus through <see cref="CollationEngine.Standard"/> — four levels, SHIFTED variable handling.
    /// Strictly more work than the root collator: punctuation and spaces are zeroed through level 3 and weighed
    /// at level 4, so group 8 of <see cref="ShortPairs"/> now needs a fourth walk to decide. The gap between this
    /// and <see cref="Compare_ShortStrings"/> is the price of STANDARD-COMPARE's default ordering table.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ShortPairCount), BenchmarkCategory(CompareShort)]
    public int Compare_ShortStrings_Standard()
    {
        int acc = 0;
        var pairs = ShortPairs;
        var collator = _standard;
        for (int i = 0; i < pairs.Length; i++)
            acc += collator.Compare(pairs[i].A, pairs[i].B);
        return acc;
    }

    /// <summary>
    /// The same corpus through the COBOL layer's LOCALE-based collating sequence
    /// (<see cref="CobolNet.Runtime.LocaleCollation.Current"/> → the current locale's collator): what a relation
    /// condition, a SORT key or an indexed-file key costs under <c>ALPHABET … IS LOCALE</c>. The difference to
    /// <see cref="Compare_ShortStrings"/> is the carrier — trailing-space trim, well-formedness scan, locale
    /// resolution (two dictionary lookups per call) — and it must stay small and allocation-free.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ShortPairCount), BenchmarkCategory(CompareShort)]
    public int Compare_ShortStrings_LocaleCollation()
    {
        int acc = 0;
        var pairs = ShortPairs;
        var loc = CobolNet.Runtime.LocaleCollation.Current;
        for (int i = 0; i < pairs.Length; i++)
            acc += loc.Compare(pairs[i].A, pairs[i].B);
        return acc;
    }

    /// <summary>
    /// <see cref="Collator.Compare(ReadOnlySpan{char},ReadOnlySpan{char})"/> — the span overload the string one
    /// forwards to. Measured separately because the COBOL layer will call it with slices of a record buffer
    /// (trailing-space truncation happens by re-slicing, not by allocating a substring): if the span path ever
    /// costs measurably more than the string path, that cheap truncation stopped being cheap.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ShortPairCount), BenchmarkCategory(CompareShort)]
    public int Compare_ShortStrings_Span()
    {
        int acc = 0;
        var pairs = ShortPairs;
        var collator = CollationEngine.Root;
        for (int i = 0; i < pairs.Length; i++)
            acc += collator.Compare(pairs[i].A.AsSpan(), pairs[i].B.AsSpan());
        return acc;
    }

    /// <summary>
    /// The FLOOR: byte-for-byte ordinal comparison, which does no collation at all. Nothing culturally correct
    /// can reach it; it is here to scale the other numbers, and as the baseline of this category.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ShortPairCount, Baseline = true), BenchmarkCategory(CompareShort)]
    public int Baseline_Ordinal_ShortStrings()
    {
        int acc = 0;
        var pairs = ShortPairs;
        for (int i = 0; i < pairs.Length; i++)
            acc += string.CompareOrdinal(pairs[i].A, pairs[i].B);
        return acc;
    }

    /// <summary>
    /// The host's ICU doing the same job the engine does — the comparison README §1's decision has to justify.
    /// This is the number that says whether declining to depend on the operating system's ICU costs anything.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ShortPairCount), BenchmarkCategory(CompareShort)]
    public int Baseline_IcuInvariant_ShortStrings()
    {
        int acc = 0;
        var pairs = ShortPairs;
        var icu = _icu;
        for (int i = 0; i < pairs.Length; i++)
            acc += icu.Compare(pairs[i].A, pairs[i].B, CompareOptions.None);
        return acc;
    }

    // =========================================================================================================
    // Compare-Long — 1.5 KB operands: the per-character cost of the element walk, isolated from call overhead.
    // =========================================================================================================

    /// <summary>
    /// Two 1.5 KB strings equal except for the LAST character, decided at level 1. One full forward walk of both
    /// texts: the cleanest measurement of the streaming iterator's per-character cost. Divide the mean by 1536 for
    /// nanoseconds per character.
    /// </summary>
    [Benchmark, BenchmarkCategory(CompareLong)]
    public int Compare_LongStrings() => CollationEngine.Compare(_longA, _longB);

    /// <summary>
    /// The same two 1.5 KB strings, differing only in the CASE of the last character: equal at level 1, equal at
    /// level 2, decided at level 3 — so all three levels are walked in full. Expected at roughly 3×
    /// <see cref="Compare_LongStrings"/>; a large deviation either way is the regression described in the class
    /// remarks.
    /// </summary>
    [Benchmark, BenchmarkCategory(CompareLong)]
    public int Compare_LongStrings_CaseOnly() => CollationEngine.Compare(_longA, _longCased);

    /// <summary>The ordinal floor at 1.5 KB — a vectorized memcmp, and this category's baseline.</summary>
    [Benchmark(Baseline = true), BenchmarkCategory(CompareLong)]
    public int Baseline_Ordinal_LongStrings() => string.CompareOrdinal(_longA, _longB);

    /// <summary>The host's ICU at 1.5 KB.</summary>
    [Benchmark, BenchmarkCategory(CompareLong)]
    public int Baseline_IcuInvariant_LongStrings() => _icu.Compare(_longA, _longB, CompareOptions.None);

    // =========================================================================================================
    // Compare-Mixed — everything off the ASCII fast path: NFD, expansions, contractions, implicit weights, jamo.
    // =========================================================================================================

    /// <summary>
    /// The mixed-script corpus under the root order. This is where the table-driven <c>Normalizer</c>, the
    /// longest-match contraction search, Hangul decomposition and the UTS #10 Table 16 implicit weights are paid
    /// for; the two canonically-equal pairs additionally walk every level and still tie.
    /// </summary>
    [Benchmark(OperationsPerInvoke = MixedPairCount), BenchmarkCategory(CompareMixed)]
    public int Compare_MixedLocaleStrings()
    {
        int acc = 0;
        var pairs = MixedPairs;
        for (int i = 0; i < pairs.Length; i++)
            acc += CollationEngine.Compare(pairs[i].A, pairs[i].B);
        return acc;
    }

    /// <summary>
    /// The same corpus through <see cref="CollationEngine.ForLocale"/>("es-ES") — the root table with the Spanish
    /// tailoring layered on. A tailoring is a NEW table, not an indirection over the root one (README §5), so this
    /// should track <see cref="Compare_MixedLocaleStrings"/> closely: a persistent gap would mean tailored lookups
    /// are taking a slower path than root ones, which would tax every locale that has a tailoring at all.
    /// </summary>
    [Benchmark(OperationsPerInvoke = MixedPairCount), BenchmarkCategory(CompareMixed)]
    public int Compare_MixedLocaleStrings_SpanishTailored()
    {
        int acc = 0;
        var pairs = MixedPairs;
        var collator = _spanish;
        for (int i = 0; i < pairs.Length; i++)
            acc += collator.Compare(pairs[i].A, pairs[i].B);
        return acc;
    }

    /// <summary>The ordinal floor on the mixed corpus — UTF-16 code-unit order, which is not an ordering anyone
    /// asked for, but is the baseline that scales this category.</summary>
    [Benchmark(OperationsPerInvoke = MixedPairCount, Baseline = true), BenchmarkCategory(CompareMixed)]
    public int Baseline_Ordinal_MixedStrings()
    {
        int acc = 0;
        var pairs = MixedPairs;
        for (int i = 0; i < pairs.Length; i++)
            acc += string.CompareOrdinal(pairs[i].A, pairs[i].B);
        return acc;
    }

    /// <summary>The host's ICU on the mixed corpus — the fairest like-for-like comparison in this file, since
    /// this is the corpus where ICU also has real work to do.</summary>
    [Benchmark(OperationsPerInvoke = MixedPairCount), BenchmarkCategory(CompareMixed)]
    public int Baseline_IcuInvariant_MixedStrings()
    {
        int acc = 0;
        var pairs = MixedPairs;
        var icu = _icu;
        for (int i = 0; i < pairs.Length; i++)
            acc += icu.Compare(pairs[i].A, pairs[i].B, CompareOptions.None);
        return acc;
    }

    // =========================================================================================================
    // BuildKey — materializing a sort key instead of comparing. What an INDEXED file's key column stores, and
    // what a SORT should use when one record is compared many times. Unlike Compare, this MUST allocate; the
    // question the MemoryDiagnoser answers here is how much, per key.
    // =========================================================================================================

    /// <summary>
    /// <see cref="CollationKey.Build(string?)"/> over the 64 short A-side operands. A key costs strictly more
    /// than one comparison (every level is materialized, none can exit early), so it only pays off when a value
    /// is compared more than a couple of times — which is exactly the INDEXED-file and SORT case. The Allocated
    /// column is the number that decides whether keying a million-record file is affordable.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ShortPairCount), BenchmarkCategory(BuildKeyShort)]
    public int BuildKey_ShortStrings()
    {
        int acc = 0;
        var pairs = ShortPairs;
        for (int i = 0; i < pairs.Length; i++)
            acc += CollationKey.Build(pairs[i].A).LevelCount;
        return acc;
    }

    /// <summary>ICU's own sort key over the same operands — the direct analogue, and this category's baseline.</summary>
    [Benchmark(OperationsPerInvoke = ShortPairCount, Baseline = true), BenchmarkCategory(BuildKeyShort)]
    public int Baseline_IcuSortKey_ShortStrings()
    {
        int acc = 0;
        var pairs = ShortPairs;
        var icu = _icu;
        for (int i = 0; i < pairs.Length; i++)
            acc += icu.GetSortKey(pairs[i].A).KeyData.Length;
        return acc;
    }

    /// <summary>
    /// A key for a 1.5 KB operand. Every level is materialized for the whole text, so both time and allocation
    /// scale with length here where <see cref="Compare_LongStrings"/> can stop early — the reason a key is the
    /// wrong tool for a one-shot comparison and the right one for an index.
    /// </summary>
    [Benchmark, BenchmarkCategory(BuildKeyLong)]
    public int BuildKey_LongStrings() => CollationKey.Build(_longA).LevelCount;

    /// <summary>ICU's sort key for the same 1.5 KB operand — this category's baseline.</summary>
    [Benchmark(Baseline = true), BenchmarkCategory(BuildKeyLong)]
    public int Baseline_IcuSortKey_LongStrings() => _icu.GetSortKey(_longA).KeyData.Length;
}
