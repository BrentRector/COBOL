// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics.CodeAnalysis;

namespace CobolNet.Binding.Model;

/// <summary>A Format-2 (table) VALUE SUBSCRIPT TUPLE — one occurrence number per dimension, in the order
/// ISO §13.18.63.3 SR20 fixes ("one subscript-1 specified for each OCCURS clause for the subject of the entry or
/// superordinate to that entry, specified in the same order as a subscripted reference to the subject of the entry
/// would be specified"): the MOST inclusive dimension first. A value type with structural equality; its
/// lexicographic order (<see cref="Compare"/>) is the odometer order a <see cref="TableValuePhrase"/> runs in.</summary>
public readonly struct Subscripts : IEquatable<Subscripts>
{
    private readonly int[] _v;

    public Subscripts(params int[] values) => _v = values;

    /// <summary>This tuple with <paramref name="next"/> appended — how an emitter descends one OCCURS level.</summary>
    public Subscripts With(int next)
    {
        var a = new int[Count + 1];
        for (int i = 0; i < Count; i++) a[i] = _v[i];
        a[Count] = next;
        return new Subscripts(a);
    }

    /// <summary>This tuple with every subscript of <paramref name="tail"/> appended, in order.</summary>
    public Subscripts With(Subscripts tail)
    {
        var r = this;
        for (int i = 0; i < tail.Count; i++) r = r.With(tail[i]);
        return r;
    }

    public int Count => _v?.Length ?? 0;

    public int this[int i] => _v[i];

    public bool Equals(Subscripts other)
    {
        if (Count != other.Count) return false;
        for (int i = 0; i < Count; i++) if (_v[i] != other._v[i]) return false;
        return true;
    }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Subscripts s && Equals(s);

    public override int GetHashCode()
    {
        var h = new HashCode();
        for (int i = 0; i < Count; i++) h.Add(_v[i]);
        return h.ToHashCode();
    }

    public override string ToString() => Count == 0 ? "()" : "(" + string.Join(" ", _v) + ")";

    /// <summary>ODOMETER (= lexicographic) ORDER, the order §13.18.63.4 GR12 initializes table elements in: the
    /// LEAST inclusive (rightmost) subscript advances first and carries into the next MOST inclusive one, so a
    /// tuple's position in the fill sequence is exactly its lexicographic rank. §13.18.63.3 SR21's "the table
    /// element associated with subscript-2 is the same occurrence or a successive occurrence of the table element
    /// associated with the corresponding subscript-1" is therefore <c>Compare(to, from) &gt;= 0</c> — and it needs
    /// no dimension maxima, which matters because a DYNAMIC dimension may not have one.</summary>
    public static int Compare(Subscripts a, Subscripts b)
    {
        int n = Math.Min(a.Count, b.Count);
        for (int i = 0; i < n; i++) if (a[i] != b[i]) return a[i].CompareTo(b[i]);
        return a.Count.CompareTo(b.Count);
    }
}

/// <summary>ONE DIMENSION of a Format-2 (table) VALUE's subscript tuple: the entry whose OCCURS clause creates it
/// (<paramref name="Owner"/> — the subject of the entry itself, or an entry superordinate to it, ISO §13.18.63.3
/// SR20), and the maximum occurrence number the odometer counts to.
/// <para><paramref name="Max"/> is §13.18.63.4 GR12's "the maximum number of occurrences, or, in the case of a
/// dynamic-capacity table, the expected number of occurrences, specified by its corresponding OCCURS clause" —
/// NULL only for a DYNAMIC table declared without an OCCURS TO (expected) capacity, where the standard gives the
/// dimension no ceiling. SR22 forbids a no-TO VALUE over such a dimension and SR23 forbids the odometer from
/// carrying past it, so a null <paramref name="Max"/> is never a modulus the fill needs.</para></summary>
/// <param name="Dynamic">True when <paramref name="Owner"/> is a Format-4 DYNAMIC-capacity table (§13.18.38).</param>
public sealed record TableValueDim(DataItem Owner, int? Max, bool Dynamic)
{
    /// <summary>The §13.18.63.3 SR22/SR23 subject: "an OCCURS clause with a DYNAMIC phrase but no TO phrase".</summary>
    public bool DynamicWithoutTo => Dynamic && Max is null;

    /// <summary>The ceiling §13.18.63.3 SR20/SR21 measure a subscript against — <see cref="Max"/>, or, for a
    /// DYNAMIC table the OCCURS clause gives no expected capacity, this implementation's §8.5.1.9.1 maximum
    /// capacity (<see cref="TableValueOdometer.MaxDynamicCapacity"/>: the standard supplies no number there).</summary>
    public int Ceiling => Max ?? TableValueOdometer.MaxDynamicCapacity;
}

/// <summary>ONE WELL-FORMED Format-2 (table) VALUE phrase, resolved: the run of table elements it initializes in
/// §13.18.63.4 GR12's odometer order, from <paramref name="From"/> (subscript-1) through <paramref name="To"/>
/// (subscript-2, or GR14's table maximum), <paramref name="Count"/> elements long
/// (<see cref="TableValueOdometer.ElementCount"/>), each taking the literal at its position in the run modulo the
/// literal list (GR13's cyclic reuse).</summary>
public sealed record TableValuePhrase(Subscripts From, Subscripts To, long Count, IReadOnlyList<string> Literals);

/// <summary>A maximal RUN of occurrences <paramref name="First"/>..<paramref name="Last"/> at one OCCURS level whose
/// element initializers REPEAT with <paramref name="Period"/>: occurrence <c>o</c> composes exactly as occurrence
/// <c>First + (o - First) % Period</c> does (<see cref="TableValueRuns"/>). A period of 1 is a uniform run.</summary>
public readonly record struct OccurrenceRun(int First, int Last, int Period)
{
    /// <summary>How many occurrences the run spans.</summary>
    public int Length => Last - First + 1;
}

/// <summary>⛔ THE RESOLVED Format-2 (table) VALUE of one entry (ISO §13.18.63.4 GR12–GR15) — its well-formed
/// PHRASES in source order, built ONCE by the binder's <c>ResolveTableValues</c> pass where the complete forest
/// makes the OCCURS chain knowable.
///
/// <para><b>Why phrases and not an element map (kb/Work PB1722).</b> One phrase can name the whole table — `05 T
/// PIC X OCCURS 10000 VALUE "A" FROM (1 1)` under `03 G OCCURS 10000` is one hundred million elements — so the
/// plan is O(phrases) and every question is answered from the phrases: <see cref="LiteralAt"/> for one element,
/// <see cref="TableValueRuns"/> for the runs of occurrences an emitter composes ONCE, and <see cref="RankForm"/>
/// for the run-time test the INITIALIZE lane emits. Its predecessor materialized a subscript-tuple dictionary
/// entry per element and every consumer walked it element by element.</para>
///
/// <para><b>Why the binder and not the emitter.</b> The plan is a pure function of the declared clause and the
/// entry's OCCURS chain — no emit context enters it — and the lanes that consume it
/// (<c>ValueInitializer.FieldInit</c> for the record-struct fields, <c>GroupImageCodec.ImageInitOf</c> for the
/// character-image backings, <c>InitializeBinder.ExpandTableValue</c> for INITIALIZE … TO VALUE) must never
/// disagree about a table element's value (kb/Work PB505).</para></summary>
public sealed class TableValuePlan
{
    /// <summary>The subject's OCCURS chain, MOST inclusive first (ISO §13.18.63.3 SR20's order). Never empty —
    /// an entry with no dimension violates SR18 and never gets a plan.</summary>
    public required IReadOnlyList<TableValueDim> Dims { get; init; }

    /// <summary>The well-formed phrases in SOURCE order (§13.18.63.4 GR15: "the value defined by the last
    /// specified FROM phrase in the VALUE clause is assigned to the table element"). A phrase the binder diagnosed
    /// (SR20/SR21/SR23) or one with no literal is absent — it must not seed any element.</summary>
    public required IReadOnlyList<TableValuePhrase> Phrases { get; init; }

    /// <summary>The literal this clause gives the table element at <paramref name="subs"/>, or null when the
    /// element is outside every FROM..TO range (or the tuple is not this plan's shape): the LAST phrase whose run
    /// holds the element (GR15), at the element's position in that run modulo the literal list (GR13).
    /// Odometer order is lexicographic order (<see cref="Subscripts.Compare"/>), so "holds" is two comparisons.</summary>
    public string? LiteralAt(Subscripts subs)
    {
        if (subs.Count != Dims.Count) return null;
        for (int i = Phrases.Count - 1; i >= 0; i--)
        {
            var p = Phrases[i];
            if (Subscripts.Compare(subs, p.From) < 0 || Subscripts.Compare(subs, p.To) > 0) continue;
            if (TableValueOdometer.ElementCount(Dims, p.From, subs) is not { } nth) continue;
            return p.Literals[(int)((nth - 1) % p.Literals.Count)];
        }
        return null;
    }

    /// <summary>Every occurrence number at level <c>prefix.Count</c> (under <paramref name="prefix"/>, occurrences
    /// 1..<paramref name="n"/>) where the set of phrases reaching the occurrence's subtree, or the way one reaches
    /// it, CHANGES: the first and one-past-the-last occurrence a phrase reaches, and either side of an occurrence
    /// the phrase covers only PART of (its FROM or TO lands inside that occurrence's subtree). Between two
    /// consecutive breaks every phrase covers either every element of every occurrence's subtree or none.</summary>
    internal void AddBreaks(Subscripts prefix, int n, SortedSet<int> breaks)
    {
        int level = prefix.Count;
        if (level >= Dims.Count) return;
        foreach (var p in Phrases)
        {
            int cf = ComparePrefix(prefix, null, p.From), ct = ComparePrefix(prefix, null, p.To);
            if (cf < 0 || ct > 0) continue;                    // the phrase's run lies wholly before / after this prefix
            int lo = cf == 0 ? p.From[level] : 1;
            int hi = Math.Min(ct == 0 ? p.To[level] : n, n);
            if (lo > hi) continue;
            breaks.Add(lo);
            breaks.Add(hi + 1);
            if (cf == 0 && !TailIs(p.From, level, _ => 1)) breaks.Add(lo + 1);       // FROM lands inside occurrence lo
            if (ct == 0 && !TailIs(p.To, level, j => Dims[j].Max)) breaks.Add(hi);   // TO lands inside occurrence hi
        }
    }

    /// <summary>How many occurrences at level <c>prefix.Count</c> it takes this plan's literals to repeat across
    /// the subtrees of a run that starts at <paramref name="first"/>, given that every phrase reaching the run covers
    /// whole subtrees (<see cref="AddBreaks"/>). The governing phrase is the LAST one reaching the occurrence (GR15);
    /// each occurrence advances its run by S elements (the product of every less inclusive dimension's maximum),
    /// so the literal an occurrence starts at returns after k / gcd(k, S mod k) occurrences for a list of k
    /// literals (GR13). 1 when no phrase reaches the run.</summary>
    internal long PeriodAt(Subscripts prefix, int first)
    {
        int level = prefix.Count;
        if (level >= Dims.Count) return 1;
        for (int i = Phrases.Count - 1; i >= 0; i--)
        {
            var p = Phrases[i];
            if (ComparePrefix(prefix, first, p.From) < 0 || ComparePrefix(prefix, first, p.To) > 0) continue;
            int k = p.Literals.Count;
            long sModK = 1 % k;
            for (int j = level + 1; j < Dims.Count; j++)
            {
                if (Dims[j].Max is not { } max) return 1;     // no S: SR23 confines the phrase to one occurrence
                sModK = sModK * (max % k) % k;
            }
            return k / Gcd(k, sModK);
        }
        return 1;
    }

    /// <summary>The phrase's run as a RANK RANGE, the form a run-time occurrence test needs (the INITIALIZE lane):
    /// the dimensions before <c>Split</c> are EQUAL across FROM and TO, so an element is in the run only where it
    /// matches them; from <c>Split</c> on, an element's rank is the mixed-radix sum of (subscript − 1) ×
    /// <c>Weights[i − Split]</c>, the run is ranks <c>Lo</c>..<c>Hi</c>, and an element's literal is the one at
    /// (rank − Lo) modulo the literal list (GR13). Every dimension less inclusive than <c>Split</c> has a maximum,
    /// because §13.18.63.3 SR23 requires the subscripts above a dimension without one to be equal. A rank beyond
    /// <see cref="long.MaxValue"/> saturates there; only a table with more elements than that can reach it.</summary>
    public (int Split, long[] Weights, long Lo, long Hi) RankForm(TableValuePhrase p)
    {
        int split = 0;
        while (split < Dims.Count - 1 && p.From[split] == p.To[split]) split++;
        var weights = new long[Dims.Count - split];
        long w = 1;
        for (int i = Dims.Count - 1; i >= split; i--)
        {
            weights[i - split] = w;
            if (i > split) w = SaturatingMultiply(w, Dims[i].Max ?? 1);
        }
        long lo = 0, hi = 0;
        for (int i = split; i < Dims.Count; i++)
        {
            lo = SaturatingAdd(lo, SaturatingMultiply(p.From[i] - 1, weights[i - split]));
            hi = SaturatingAdd(hi, SaturatingMultiply(p.To[i] - 1, weights[i - split]));
        }
        return (split, weights, lo, hi);
    }

    /// <summary>Lexicographic comparison of <paramref name="prefix"/> (followed by <paramref name="o"/>, when given)
    /// against the same number of leading subscripts of <paramref name="full"/>.</summary>
    private static int ComparePrefix(Subscripts prefix, int? o, Subscripts full)
    {
        for (int i = 0; i < prefix.Count; i++)
            if (prefix[i] != full[i]) return prefix[i].CompareTo(full[i]);
        return o is { } occ ? occ.CompareTo(full[prefix.Count]) : 0;
    }

    /// <summary>Whether every subscript of <paramref name="t"/> after <paramref name="level"/> is the bound
    /// <paramref name="at"/> names for its dimension (1 for a run's first element, the maximum for its last).</summary>
    private bool TailIs(Subscripts t, int level, Func<int, int?> at)
    {
        for (int j = level + 1; j < Dims.Count; j++)
            if (at(j) is not { } bound || t[j] != bound) return false;
        return true;
    }

    internal static long Gcd(long a, long b)
    {
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    internal static long SaturatingMultiply(long a, long b) =>
        a == 0 || b == 0 ? 0 : a > long.MaxValue / b ? long.MaxValue : a * b;

    internal static long SaturatingAdd(long a, long b) => a > long.MaxValue - b ? long.MaxValue : a + b;
}

/// <summary>⛔ THE RUNS OF OCCURRENCES AN EMITTER COMPOSES ONCE (kb/Work PB1722) — the one answer to "which
/// occurrences of this table level initialize alike", asked by every lane that writes a table's initial content
/// (the record-struct arrays, the character-image seeds, the dynamic-capacity seed switch). With no table VALUE in
/// the subtree every occurrence is alike: one run of period 1 (§13.18.63.4 GR9 gives each occurrence the same
/// value). With one, the breaks are the union over every plan in the subtree (<see cref="TableValuePlan.AddBreaks"/>)
/// and a run's period is the least common multiple of each plan's (<see cref="TableValuePlan.PeriodAt"/>), so a
/// lane composes at most Period occurrences per run: O(phrases), never O(elements).</summary>
public static class TableValueRuns
{
    /// <summary>The runs covering occurrences 1..<paramref name="n"/> of <paramref name="table"/>, whose OCCURS
    /// clause is the dimension at <c>outer.Count</c> of every plan beneath it (<paramref name="outer"/> is the
    /// occurrence context the emitters thread: one subscript per OCCURS level above <paramref name="table"/>).</summary>
    public static IReadOnlyList<OccurrenceRun> Of(DataItem table, Subscripts outer, int n)
    {
        if (n <= 0) return [];
        if (!table.ContainsTableValue) return [new OccurrenceRun(1, n, 1)];
        var plans = new List<TableValuePlan>();
        Collect(table, plans);
        var breaks = new SortedSet<int> { 1, n + 1 };
        foreach (var plan in plans) plan.AddBreaks(outer, n, breaks);

        var runs = new List<OccurrenceRun>();
        int start = 1;
        foreach (int b in breaks)
        {
            if (b <= start) continue;
            long period = 1;
            foreach (var plan in plans)
            {
                long q = plan.PeriodAt(outer, start);
                period = period / TableValuePlan.Gcd(period, q) * q;
                if (period >= b - start) { period = b - start; break; }
            }
            runs.Add(new OccurrenceRun(start, b - 1, (int)period));
            start = b;
            if (start > n) break;
        }
        return runs;
    }

    private static void Collect(DataItem item, List<TableValuePlan> plans)
    {
        if (item.TableValuePlan is { } plan) plans.Add(plan);
        foreach (var c in item.Children)
            if (c.ContainsTableValue) Collect(c, plans);
    }
}

/// <summary>⛔ THE §13.18.63.4 GR12–GR16 ODOMETER, in ONE place: the pure resolution of a Format-2 (table) VALUE's
/// phrases over a dimension list. The binder's <c>ResolveTableValues</c> pass validates §13.18.63.3 SR18–SR23 and
/// then calls this; nothing else computes a table element's initial value.</summary>
public static class TableValueOdometer
{
    /// <summary>⛔ THE §8.5.1.9.1 MAXIMUM CAPACITY of a dynamic-capacity table, as this implementation defines it:
    /// "The actual limit for the current capacity imposed by the implementor and by current resource availability
    /// is referred to as the maximum capacity." A DYNAMIC table declared without an OCCURS TO phrase specifies NO
    /// maximum number of occurrences, so §13.18.63.3 SR21's ceiling sentence has no operand for that dimension —
    /// this number stands in its place, and a Format-2 VALUE whose subscript-2 exceeds it is rejected rather than
    /// silently materialized. (Without a ceiling the §13.18.63.4 GR12 fill has none either: the pre-existing
    /// single-dimension loop ran from subscript-1 to subscript-2 with no bound at all.)</summary>
    public const int MaxDynamicCapacity = 1_000_000;

    /// <summary>§13.18.63.4 GR14 — "If the TO phrase is not specified, it is as if the TO phrase were specified
    /// with each subscript-2 as the maximum number of occurrences, or, in the case of a dynamic-capacity table,
    /// the expected number of occurrences, of the table associated with each corresponding subscript-1." Returns
    /// null when a dimension has no such number (a DYNAMIC table with no OCCURS TO) — SR22 rejects that source, so
    /// the caller has already diagnosed it.</summary>
    public static Subscripts? DefaultTo(IReadOnlyList<TableValueDim> dims)
    {
        var a = new int[dims.Count];
        for (int i = 0; i < dims.Count; i++)
        {
            if (dims[i].Max is not { } m) return null;
            a[i] = m;
        }
        return new Subscripts(a);
    }

    /// <summary>ONE odometer step (ISO §13.18.63.4 GR12): "Consecutive table elements are referenced by
    /// incrementing by 1 the subscript that represents the least inclusive dimension of the table. When any
    /// reference to a subscript, prior to incrementing it, is equal to the maximum number of occurrences … that
    /// subscript is set to 1 and the subscript for the next most inclusive dimension of the table is incremented
    /// by 1." Mutates <paramref name="cur"/>; false when the whole odometer has run past its last element.
    /// <para>A dimension with no ceiling (a DYNAMIC table with no OCCURS TO) simply increments and never carries —
    /// SR23 requires every MORE inclusive subscript to be equal across FROM and TO, so no carry out of it is ever
    /// needed to reach subscript-2 (and <see cref="ElementCount"/> refuses a phrase that would need one).</para></summary>
    public static bool Step(int[] cur, IReadOnlyList<TableValueDim> dims)
    {
        for (int k = cur.Length - 1; k >= 0; k--)
        {
            if (dims[k].Max is not { } max) { cur[k]++; return true; }
            if (cur[k] < max) { cur[k]++; return true; }
            cur[k] = 1;   // exhausted this dimension — carry into the next most inclusive one
        }
        return false;
    }

    /// <summary>Resolve the phrases into the plan's §13.18.63.4 GR12–GR15 runs, in source order; GR15 is applied
    /// by the readers (the LAST phrase holding an element gives it its literal). Each phrase runs from its
    /// subscript-1 tuple through its subscript-2 tuple (GR14 supplies a missing one) and names exactly the
    /// <see cref="ElementCount"/> elements counted before anything is read. A phrase that names no well-formed run
    /// contributes nothing: the caller has already diagnosed it (SR20/SR21/SR23), and a mis-shaped phrase must not
    /// silently seed the wrong element. Nothing here is per element (kb/Work PB1722).</summary>
    public static List<TableValuePhrase> Resolve(IReadOnlyList<TableValueDim> dims, IReadOnlyList<TableValueSpec> specs)
    {
        var phrases = new List<TableValuePhrase>();
        foreach (var spec in specs.OrderBy(s => s.Ordinal))
        {
            if (spec.Literals.Count == 0) continue;
            var from = new Subscripts([.. spec.From]);
            var to = spec.To is { } tl ? new Subscripts([.. tl]) : DefaultTo(dims);
            if (to is not { } stop || ElementCount(dims, from, stop) is not { } count) continue;
            phrases.Add(new TableValuePhrase(from, stop, count, spec.Literals));
        }
        return phrases;
    }

    /// <summary>§13.18.63.3 SR23's antecedent at one LEVEL: the index of the first dimension LESS inclusive than
    /// <paramref name="level"/> whose OCCURS clause has "a DYNAMIC phrase but no TO phrase", or null when there is
    /// none. When it is not null, <paramref name="level"/> is one of "all levels higher than that of the OCCURS
    /// clause", where "the values of subscript-1 and subscript-2 … shall be equal". The rule's shape is written
    /// here once, and read by both the binder's SR23 diagnostic and <see cref="ElementCount"/>.</summary>
    public static int? UnboundedBelow(IReadOnlyList<TableValueDim> dims, int level)
    {
        for (int j = level + 1; j < dims.Count; j++)
            if (dims[j].DynamicWithoutTo) return j;
        return null;
    }

    /// <summary>⛔ HOW MANY table elements one phrase names: the length of the §13.18.63.4 GR12 fill from the
    /// <paramref name="from"/> tuple through the <paramref name="to"/> tuple in odometer order, known BEFORE any
    /// element is filled, so the fill is bounded by the phrase and never by a cap (kb/Work PB1716: a 64,000,000-
    /// element defensive cap was the only bound, and a phrase violating SR23 ran all the way to it — about 30 s
    /// to reject a 26-line program).
    /// <para>The count is the MIXED-RADIX distance between the tuples: one occurrence at dimension k spans the
    /// product of every less inclusive dimension's maximum. That product does not exist above a dimension with no
    /// ceiling, which is exactly why §13.18.63.3 SR23 requires "the values of subscript-1 and subscript-2
    /// corresponding to all levels higher than that of the OCCURS clause" to be equal.</para>
    /// <para>Null when the phrase names no well-formed run: a subscript outside
    /// 1..<see cref="TableValueDim.Ceiling"/> (SR20/SR21), a TO tuple preceding the FROM tuple (SR21), or unequal
    /// subscripts at a level SR23 constrains. The binder diagnoses each of those; the null keeps a diagnosed phrase
    /// (which still reaches the fill when <c>--permissive</c> demotes its diagnostic) from seeding any element.
    /// A count beyond <see cref="long.MaxValue"/> saturates there.</para></summary>
    public static long? ElementCount(IReadOnlyList<TableValueDim> dims, Subscripts from, Subscripts to)
    {
        if (from.Count != dims.Count || to.Count != dims.Count) return null;                  // SR20 / SR21 count
        for (int k = 0; k < dims.Count; k++)
        {
            int ceiling = dims[k].Ceiling;
            if (from[k] < 1 || from[k] > ceiling || to[k] < 1 || to[k] > ceiling) return null;   // SR20 / SR21 range
            if (from[k] != to[k] && UnboundedBelow(dims, k) is not null) return null;             // SR23
        }
        if (Subscripts.Compare(to, from) < 0) return null;                                          // SR21 order

        // TO minus FROM, digit by digit from the least inclusive dimension, borrowing from the next more inclusive
        // one. Every resulting digit is non-negative, so the weighted sum only grows and saturates honestly. No
        // borrow reaches a dimension without a ceiling: SR23 (above) made every level more inclusive than it equal,
        // and TO >= FROM then leaves nothing to borrow at that dimension itself — the loop stops there.
        long distance = 0, weight = 1;
        int borrow = 0;
        for (int k = dims.Count - 1; k >= 0; k--)
        {
            int digit = to[k] - from[k] - borrow;
            borrow = 0;
            if (digit < 0) { digit += dims[k].Ceiling; borrow = 1; }
            distance = SaturatingAdd(distance, SaturatingMultiply(digit, weight));
            if (dims[k].Max is not { } max) break;   // every more inclusive digit is zero (SR23)
            weight = SaturatingMultiply(weight, max);
        }
        return SaturatingAdd(distance, 1);
    }

    private static long SaturatingMultiply(long a, long b) => TableValuePlan.SaturatingMultiply(a, b);

    private static long SaturatingAdd(long a, long b) => TableValuePlan.SaturatingAdd(a, b);

    /// <summary>§13.18.63.4 GR16 — the INITIAL CAPACITY a Format-2 VALUE gives the dynamic-capacity table at
    /// dimension <paramref name="dim"/>: (a) with a TO phrase, "the initial capacity is increased, if necessary,
    /// to the value of the corresponding subscript-2, provided that this value does not lie outside the range
    /// defined by the minimum and expected capacity specified in the OCCURS clause. If the value of subscript-2
    /// lies outside this range, the initial capacity is unchanged"; (b) with no TO phrase, "the initial capacity
    /// is set equal to the expected capacity specified in the OCCURS clause". Null when this phrase raises
    /// nothing.</summary>
    public static int? InitialCapacity(TableValueSpec spec, int dim, int min, int? expected)
    {
        if (spec.To is not { } to)
            return expected;                       // GR16b
        if (dim >= to.Count) return null;
        int want = to[dim];
        if (want < min) return null;               // GR16a proviso — outside [min, expected]: unchanged
        if (expected is { } e && want > e) return null;
        return want;                               // GR16a
    }
}
