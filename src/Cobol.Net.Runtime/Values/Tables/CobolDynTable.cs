// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// A DYNAMIC-capacity table (ISO/IEC 1989:2023 §13.18.38 Format 4 / §8.5.1.9; data-model design D9). Out-of-line
/// growable storage for a COBOL <c>OCCURS DYNAMIC</c> table: a backing array plus a current-capacity counter (=
/// the table's <b>current capacity</b> — the number of occurrences allocated now, §8.5.1.9.1). The CAPACITY register
/// is a view over <see cref="Capacity"/>. The occurrences the table opens with are seeded with the element's
/// initial state; new/intermediate occurrences a statement creates are seeded with the INITIALIZED phrase's
/// INITIALIZE recipe when it is written (§8.5.1.9.5; kb/Work PB1267). Element access returns <c>ref T</c> so a subscripted write goes through the
/// single ref every <c>Place.Write</c> relies on; <c>T</c> is the element value type (a record struct) or string.
/// </summary>
public sealed class CobolDynTable<T>
{
    private T[] _store;
    private int _count;                 // current capacity (§8.5.1.9.1)
    private int _searching;             // >0 while a SEARCH of THIS table is in progress (EC-FLOW-SEARCH guard)
    private T _scratch = default!;      // the benign out-of-range slot (COBOL-85 / checking-off policy)
    private readonly Func<int, T> _seedAt;   // occurrence-indexed seed (1-based); a Format 2 (table) VALUE varies by occurrence
    private readonly Func<int, T> _createdAt;   // the seed of an occurrence a STATEMENT creates (§8.5.1.9.5 INITIALIZED)
    private readonly int _min;
    private readonly int? _expected;    // TO integer-5 — the expected capacity (nonfatal to exceed)

    /// <summary>The implementor maximum occurrence count (§8.5.1.9.1 — resource-bounded). A growth request past it
    /// raises EC-BOUND-TABLE-LIMIT (fatal) with the current capacity left unchanged.</summary>
    public const int MaxOccurrences = 0x3FFF_FFFF;   // ~Array.MaxLength headroom

    /// <param name="seed">Produces one occurrence as the initial state has it (§14.6.2.3.2 — the element's VALUE
    /// clauses over the background): the occurrences the table OPENS with.</param>
    /// <param name="min">FROM integer-4 — the minimum / initial current capacity (§13.18.38 GR16); the table opens
    /// at this capacity, seeded.</param>
    /// <param name="expected">TO integer-5 — the expected capacity (§13.18.38 GR17), or null if unbounded.</param>
    /// <param name="initializedSeed">The INITIALIZED phrase's seed (§8.5.1.9.5 — the element "as though … the
    /// subject of a statement of the form INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT") for every
    /// occurrence a STATEMENT creates later, or null when it is the same as <paramref name="seed"/> — which it
    /// always may be when INITIALIZED is absent, since those contents are then undefined (kb/Work PB1267).</param>
    public CobolDynTable(Func<T> seed, int min, int? expected, Func<T>? initializedSeed)
        : this((Func<int, T>)(_ => seed()), min, expected,
               initializedSeed is null ? null : (Func<int, T>)(_ => initializedSeed()), min) { }

    /// <summary>The Format 2 (table) VALUE overload (ISO §13.18.63.2/GR12–GR16): a PER-OCCURRENCE seed (1-based)
    /// and an explicit initial current capacity (<paramref name="initialCapacity"/> — the GR16 value, ≥ the FROM
    /// minimum). Occurrences 1..initialCapacity take their keyed VALUE literal; growth beyond re-seeds through
    /// <paramref name="initializedSeedAt"/> when INITIALIZED is written, else through the same
    /// <paramref name="seedAt"/> (occurrences outside the VALUE range yield the element default).</summary>
    public CobolDynTable(Func<int, T> seedAt, int min, int? expected, Func<int, T>? initializedSeedAt, int initialCapacity)
    {
        _seedAt = seedAt;
        _createdAt = initializedSeedAt ?? seedAt;
        _min = min < 0 ? 0 : min;
        _expected = expected;
        int open = Math.Max(_min, initialCapacity < 0 ? 0 : initialCapacity);
        _store = new T[Math.Max(open, 4)];
        _count = 0;
        GrowTo(open, _seedAt);   // initial current capacity = FROM (§8.5.1.9.1) raised to the VALUE's §13.18.63.4 GR16 capacity
    }

    /// <summary>The current capacity — the number of occurrences allocated now (§8.5.1.9.1). The source-level
    /// CAPACITY register reads this; SEARCH/PERFORM VARYING bound to it.</summary>
    public long Capacity => _count;

    /// <summary>The occurrences that exist now — 1..current capacity — as one span over the store, for an operation
    /// that ranges over the whole current table in place: the Format-2 table SORT (ISO §14.9.40.4 GR20, whose
    /// "number of occurrences … determined by the rules in the OCCURS clause" is the current capacity for a
    /// dynamic-capacity table, §8.5.1.9.1; kb/Work PB1174). Never past the capacity: the store's spare slots are
    /// not occurrences.</summary>
    public Span<T> CurrentOccurrences => _store.AsSpan(0, _count);

    /// <summary>A SENDING element reference (§8.5.1.9.2): a 1-based occurrence in 1..current-capacity — "the result
    /// of the operation is the same as for a fixed-capacity table whose number of occurrences is the current
    /// capacity of the table", so an occurrence outside it is §8.4.2.3.4 2)'s out-of-range subscript and sets
    /// EC-BOUND-SUBSCRIPT exactly as <see cref="CobolTable.At{T}"/> does (kb/Work PB1268 — this arm used to return
    /// the scratch slot in silence while the fixed-table accessor raised). With checking off the reference continues
    /// benignly through a fresh scratch slot, the same policy. The subscript is an <see cref="Occurrence"/> (kb/Work
    /// PB2695), as <see cref="CobolTable.At{T}"/>'s is: the detail names the value the program holds even when it
    /// left <c>long</c>'s range.</summary>
    public ref T RefSending(Occurrence occurrence)
    {
        long occ = occurrence.Value;
        if (occ >= 1 && occ <= _count) return ref _store[(int)(occ - 1)];
        ExceptionState.SubscriptError(
            $"subscript {occurrence} is outside 1..{_count}, the current capacity (ISO 8.5.1.9.2, 8.4.2.3.4 GR2)");
        _scratch = _seedAt((int)occ);
        return ref _scratch;
    }

    /// <summary>A RECEIVING element reference (§8.5.1.9.3): an occurrence &gt; the current capacity GROWS the table to
    /// it, seeding any skipped intermediate occurrences. An occurrence &lt; 1 is not a growth case — §8.4.2.3.4 2)'s
    /// "not a positive integer" — so it sets EC-BOUND-SUBSCRIPT, as the fixed-table accessor does (kb/Work PB1268),
    /// and with checking off continues through the benign scratch slot. An implicit growth
    /// past the expected capacity (TO integer-5) raises the nonfatal EC-BOUND-OVERFLOW through
    /// <see cref="RaiseImplicitOverflow"/> — the ONE §8.5.1.9.6 1) raise every implicit capacity change shares — and
    /// the growth proceeds regardless, a declarative's RESUME AT NEXT STATEMENT included (kb/Work PB1269). Every
    /// detail text names the subscript's value as the program holds it, even past <c>long</c> (<see cref="Occurrence"/>,
    /// kb/Work PB2695).</summary>
    public ref T RefReceiving(Occurrence occurrence)
    {
        long occ = occurrence.Value;
        if (occ < 1)
        {
            ExceptionState.SubscriptError($"subscript {occurrence} is not a positive integer (ISO 8.4.2.3.4 GR2)");
            _scratch = _seedAt((int)occ);
            return ref _scratch;
        }
        if (occ > _count)
        {
            // The growth diagnostics take the requested capacity as an Int128; a subscript that saturated is named by
            // its exact text instead (null for every other, so the common growth builds no string).
            string? shown = occurrence.IsSaturated ? occurrence.ToString() : null;
            RaiseImplicitOverflow(occ, "implicit growth", shown);
            // `occ`, not `(int)occ` — the SAME narrowing the explicit path carried (kb/Work PB459). A receiving
            // reference to occurrence 5 000 000 000 wrapped to 705 032 704 and silently grew the table to it
            // instead of raising GR30's EC-BOUND-TABLE-LIMIT; the `long` widens to GrowTo's Int128 parameter.
            GrowTo(occ, _createdAt, shown);
            // ⛔ GrowTo may now DECLINE (GR30 leaves the capacity unchanged when checking is off), so the
            // occurrence it was asked for can still not exist. Falling through to `_store[occ-1]` here would be
            // an IndexOutOfRangeException — a raw .NET failure on user source, from the one path where a benign
            // scratch slot is exactly what the model already provides for an unreachable occurrence.
            if (occ > _count) { _scratch = _seedAt((int)occ); return ref _scratch; }
        }
        return ref _store[(int)(occ - 1)];
    }

    /// <summary>⛔ THE ONE §8.5.1.9.6 1) RAISE FOR AN IMPLICIT CAPACITY CHANGE — "The nonfatal EC-BOUND-OVERFLOW
    /// exception condition shall exist when a dynamic-capacity table has an expected capacity and an operation
    /// causes this expected capacity to be exceeded. If the change in capacity was implicit and the expected
    /// capacity had already been exceeded before the operation, no exception shall exist." Every capacity change
    /// that is not a SET Format 14 (§8.5.1.9.4 makes the SET the only EXPLICIT one) is implicit: a receiving
    /// subscript past the current capacity (§8.5.1.9.3, <see cref="RefReceiving"/>) and the recreation of a
    /// receiving table by a variable-length group transfer (§14.6.9.2, <see cref="FromCurrentImage"/>). Both come
    /// here, so neither can raise the explicit SET's EC-BOUND-SET instead (kb/Work PB1144) or forget the
    /// already-exceeded exemption.
    /// <para>Called BEFORE the change, like its explicit twin in <see cref="SetCapacity"/>: a declarative that
    /// completes normally, or one that executes RESUME AT NEXT STATEMENT, returns here and the change is made —
    /// §8.5.1.9.6 1) names that continuation for exactly this condition ("the operation shall be allowed to
    /// continue, thus exceeding the receiving table's specified expected capacity"), which is why
    /// <see cref="ExceptionEngine.BoundOverflowError"/> does not unwind on the NEXT STATEMENT resume (kb/Work
    /// PB1269). A RESUME AT procedure-name still transfers control out of the statement.</para></summary>
    private void RaiseImplicitOverflow(Int128 newCapacity, string operation, string? shown = null)
    {
        if (_expected is { } exp && newCapacity > exp && _count <= exp)
            ExceptionState.BoundOverflowError(
                $"OCCURS DYNAMIC {operation} to {shown ?? newCapacity.ToString()} exceeds the expected capacity {exp} (ISO §8.5.1.9.6 1))");
    }

    /// <summary>Raise the current capacity to <paramref name="newCount"/>, seeding new occurrences [old..new)
    /// (§8.5.1.9.5). A request the table cannot reach — past <see cref="MaxOccurrences"/>, or past what the
    /// runtime's resources can hold — raises EC-BOUND-TABLE-LIMIT (fatal) with the capacity unchanged
    /// (<see cref="TableLimit"/>). This is the pure grow primitive: EC-BOUND-OVERFLOW on an implicit change
    /// (§8.5.1.9.6 1)) is raised by <see cref="RaiseImplicitOverflow"/> BEFORE calling here; EC-BOUND-SET on an
    /// explicit SET past the expected capacity (§14.9.39.4 GR30's second arm) is raised by
    /// <see cref="SetCapacity"/> BEFORE the capacity changes, as this one is (kb/Work PB460).</summary>
    /// <remarks>⛔ <paramref name="newCount"/> is <see cref="Int128"/>, and THAT IS THE POINT (kb/Work PB459):
    /// the implementor-maximum test is written ONCE, here, and it has to see the request BEFORE any narrowing.
    /// The explicit-SET path used to hand this an <c>(int)</c> cast of a <c>long</c>, so a capacity request of
    /// 5 000 000 000 WRAPPED to 705 032 704 — a small VALID capacity, silently allocated — instead of raising.</remarks>
    private void GrowTo(Int128 newCount, Func<int, T> seedAt, string? shown = null)
    {
        if (newCount <= _count) return;
        if (newCount > MaxOccurrences) { TableLimit(shown ?? newCount.ToString(), $"the implementor maximum ({MaxOccurrences})"); return; }
        int target = (int)newCount;   // ≤ MaxOccurrences by the test above
        var before = _store;
        try
        {
            if (target > _store.Length) _store = Enlarged(_store, target);
            for (int i = _count; i < target; i++) _store[i] = seedAt(i + 1);
        }
        catch (OutOfMemoryException)
        {
            // ⛔ THE MAXIMUM CAPACITY IS ALSO "CURRENT RESOURCE AVAILABILITY" (kb/Work PB1410). §8.5.1.9.1 3):
            // "The actual limit for the current capacity imposed by the implementor and by current resource
            // availability is referred to as the maximum capacity", and §8.5.1.9.6 2) makes the attempt to pass
            // it "based on the resources available at runtime" the fatal EC-BOUND-TABLE-LIMIT — never a .NET
            // OutOfMemoryException killing the process. The larger array, if one was allocated, is dropped and
            // the table keeps its old storage and capacity (the slots seeded above the count are invisible).
            _store = before;
            TableLimit(shown ?? newCount.ToString(), "the resources available at runtime");
            return;
        }
        _count = target;
    }

    /// <summary>§8.5.1.9.6 2) / §14.9.39.4 GR30 — the fatal EC-BOUND-TABLE-LIMIT with the capacity UNCHANGED. GR30
    /// states the outcome outright ("the EC-BOUND-TABLE-LIMIT exception condition is set to exist and the capacity
    /// of the table is unchanged"), so with checking off this returns and the table keeps its capacity, rather
    /// than the unconditional throw that used to abort the run unit; with checking on it throws the fatal
    /// condition for the statement's EC dispatch.</summary>
    private static void TableLimit(string requested, string limit) =>
        ExceptionState.BoundTableLimitError(
            $"OCCURS DYNAMIC growth to {requested} exceeds {limit} — ISO §8.5.1.9.6 2)");

    /// <summary>A store at least <paramref name="target"/> long holding <paramref name="store"/>'s occurrences —
    /// doubled for amortized growth, and when the doubled size cannot be allocated, exactly
    /// <paramref name="target"/>: a request the runtime CAN hold is never refused for the headroom this
    /// implementation would have liked (§8.5.1.9.6 2) is about the capacity requested, not the array's slack).
    /// Throws <see cref="OutOfMemoryException"/> only when even the exact size cannot be had.</summary>
    private static T[] Enlarged(T[] store, int target)
    {
        int cap = store.Length < 4 ? 4 : store.Length;
        while (cap < target) cap = cap >= MaxOccurrences / 2 ? MaxOccurrences : cap * 2;
        var grown = store;
        try { Array.Resize(ref grown, cap); }
        catch (OutOfMemoryException) when (cap > target) { grown = store; Array.Resize(ref grown, target); }
        return grown;
    }

    /// <summary>SET Format 14 <c>… TO n</c> (§14.9.39 GR29): set the current capacity to n (raise OR lower), clamped
    /// to ≥ the minimum. Lowering frees the highest occurrences. Illegal during a SEARCH of this same table
    /// (EC-FLOW-SEARCH, GR31). Growth beyond the implementor max raises EC-BOUND-TABLE-LIMIT (capacity unchanged).</summary>
    public void SetCapacity(Int128 n)
    {
        if (_searching > 0)
        {
            // §14.9.39.4 GR31 states the outcome outright — "the EC-FLOW-SEARCH exception condition is set to
            // exist AND THE SET STATEMENT IS NOT EXECUTED" — so with checking off this returns having done
            // nothing, rather than the unconditional throw that used to abort the run unit.
            ExceptionState.FlowSearchError(
                "SET of a dynamic-capacity table's capacity during a SEARCH of that same table "
                + "(ISO §14.9.39.4 GR31)");
            return;   // GR31: the SET statement is not executed
        }
        // ⛔ THE NEW CAPACITY ARRIVES AND TRAVELS AS Int128 (kb/Work PB459). GR30 computes it from
        // arithmetic-expression-4 — "the new capacity is obtained by adding … to the current capacity" for UP BY,
        // subtracting for DOWN BY — so the UP/DOWN twins below form `_count ± n` WIDER than the carrier: in the
        // `long` it could overflow before this call was even made. The implementor-maximum test that GR30 states
        // next is NOT repeated here: it is written once, in GrowTo, which the IMPLICIT-growth path needs too. A
        // new capacity above the maximum can never be clamped to the minimum (min ≤ max), so reaching GrowTo
        // through the growth arm below applies GR30's two tests in their stated order anyway.
        // ⛔ GR30's SECOND ARM (kb/Work PB460): "… the EC-BOUND-TABLE-LIMIT exception condition is set to exist and
        // the capacity of the table is unchanged; otherwise, if an expected maximum capacity is specified for the
        // table and the new capacity of the table exceeds that expected maximum capacity, the EC-BOUND-SET
        // exception condition is set to exist." "Otherwise" is the request NOT above the implementor maximum
        // (GrowTo raises the first arm for that one). Unlike implicit growth's EC-BOUND-OVERFLOW (§8.5.1.9.6 GR1,
        // first crossing only), nothing here exempts a table ALREADY past its expected capacity: every explicit
        // SET whose NEW capacity exceeds it raises — a TO / UP BY / DOWN BY that leaves it above as much as one
        // that takes it there. The rule does not make the capacity unchanged (contrast its first arm), so the
        // change is made. ⚠ ORDER — the SAME shape as the implicit twin in RefReceiving, and §8.5.1.9.6's model
        // of an exceeded expected capacity ("the operation shall be allowed to continue"): the condition is
        // raised FIRST and the change follows. With checking off, or a declarative that completes normally
        // (§14.6.13.1.4 3)), the change is made; a RESUME that leaves the statement abandons it with the rest of
        // the statement. Nonfatal (Table 13) — it records the status only while checking is enabled.
        if (n <= MaxOccurrences && _expected is { } exp && n > exp)
            ExceptionState.BoundSetError(
                $"SET of an OCCURS DYNAMIC capacity to {n} exceeds the expected capacity {exp} (ISO §14.9.39.4 GR30)");
        if (n > _count) { GrowTo(n, _createdAt); return; }               // grow: n > _count ≥ _min, so no clamp can apply
        // GR30's minimum clamp — "If the new capacity of the table is less than the minimum capacity defined in
        // the corresponding OCCURS clause, the new capacity of the table shall be the minimum capacity" — over a
        // shrink, where n < _count ≤ MaxOccurrences makes the narrowing exact.
        if (n < _count)
        {
            // §8.5.1.9.4: "the appropriate number of higher occurrences is deleted and any resources they were
            // using are freed" — the slots are cleared so the deleted occurrences hold no references; the array
            // itself is kept, as that clause's NOTE permits.
            int kept = n < _min ? _min : (int)n;
            if (kept < _count) { Array.Clear(_store, kept, _count - kept); _count = kept; }
        }
    }

    /// <summary>SET Format 14 <c>… UP BY n</c> (§14.9.39.4 GR30 b): raise the current capacity by n.</summary>
    public void CapacityUpBy(long n) => SetCapacity((Int128)_count + n);

    /// <summary>SET Format 14 <c>… DOWN BY n</c> (§14.9.39.4 GR30 c): lower the current capacity by n.</summary>
    public void CapacityDownBy(long n) => SetCapacity((Int128)_count - n);

    /// <summary>INITIALIZE of the whole dynamic table (§14.9 INITIALIZE GR10): re-seed occurrences [1..current];
    /// the current capacity is unchanged.</summary>
    public void InitializeAll() { for (int i = 0; i < _count; i++) _store[i] = _seedAt(i + 1); }

    /// <summary>The table's CURRENT-EXTENT character image: every occurrence up to the current capacity,
    /// rendered by <paramref name="imageOf"/> and concatenated in occurrence order (kb/Work PB164 — the
    /// §14.9.11.4 GR7 documented DISPLAY format for a variable-length group renders each dynamic-capacity
    /// table "at its current capacity", the same extent §15.50.4 r7c counts for FUNCTION LENGTH).</summary>
    public string CurrentImage(Func<T, string> imageOf)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _count; i++) sb.Append(imageOf(_store[i]));
        return sb.ToString();
    }

    /// <summary>Each occurrence up to the current capacity, mapped by <paramref name="map"/>, in occurrence order — the
    /// occurrence carriers of a table whose elements are variable-length groups (kb/Work PB2496; ISO §14.6.9.2 moves it
    /// "Correspondingly numbered elements … according to the rules of the MOVE statement", so each occurrence travels as
    /// its own §8.5.1.12 carrier).</summary>
    public TOut[] CurrentOccurrencesAs<TOut>(Func<T, TOut> map)
    {
        var outp = new TOut[_count];
        for (int i = 0; i < _count; i++) outp[i] = map(_store[i]);
        return outp;
    }

    /// <summary>The write half of <see cref="CurrentImage"/> — distribute a carried current-extent image back
    /// into the table (kb/Work PB204). The capacity BECOMES the number of whole
    /// <paramref name="elementWidth"/>-wide occurrences the content holds (raised to the FROM minimum by
    /// space-filled elements), which is sound only because
    /// §8.5.1.12.3 admits corresponding tables at an activation boundary only "when the byte length of their
    /// elements is equal" — the bind-time compatibility check is what makes this division meaningful.
    /// <paramref name="store"/> distributes one occurrence's image into a freshly seeded element, so a group
    /// element keeps whatever storage its initializer allocated.</summary>
    /// <remarks>⛔ THIS IS §14.6.9.2's RECREATION, NOT A SET (kb/Work PB1144). "The operation recreates or overwrites
    /// the receiving table with a copy of the sending table, after freeing, if applicable, all the resources
    /// previously occupied by the receiving table", and then "If the receiving table is a dynamic-capacity table
    /// specifying a minimum capacity that is higher than its current capacity, further elements are created and
    /// filled with spaces until the current capacity of the table is equal to its minimum capacity" (§14.6.9.4's
    /// fill: each element space-filled). This used to route through <see cref="SetCapacity"/>, the explicit SET
    /// Format 14 primitive, and that was wrong three ways: its minimum clamp KEPT the receiver's stale occurrences
    /// between the sender's count and the minimum instead of creating space-filled ones; it raised the SET's
    /// EC-BOUND-SET, whose checking only a SET statement enables, so the implicit change's EC-BOUND-OVERFLOW
    /// (§8.5.1.9.6 1)) was never reported; and it applied the SET's no-exemption rule where §8.5.1.9.6 1) exempts
    /// an implicit change to a table already past its expected capacity. The new table is built whole and swapped
    /// in, so a table that cannot be recreated (EC-BOUND-TABLE-LIMIT, <see cref="TableLimit"/>) is left exactly as
    /// it was.</remarks>
    public void FromCurrentImage(string content, int elementWidth, Func<T, string, T> store) =>
        Recreate(CobolVarGroup.Occurrences(content, elementWidth), new string(' ', Math.Max(0, elementWidth)), store);

    /// <summary>⛔ §14.6.9.2's RECREATION, over whatever one occurrence travels as (kb/Work PB2496): the table becomes
    /// <paramref name="parts"/>.Count occurrences, each a freshly seeded element <paramref name="store"/> distributes one
    /// part into, raised to the FROM minimum by elements filled from <paramref name="blank"/> ("further elements are
    /// created and filled with spaces"). <see cref="FromCurrentImage"/> recreates from an occurrence IMAGE; a table whose
    /// elements are variable-length groups recreates from each occurrence's own §8.5.1.12 carrier, because "Correspondingly
    /// numbered elements are moved according to the rules of the MOVE statement" — a group MOVE per element. See
    /// <see cref="FromCurrentImage"/>'s remarks for why this is a recreation and not a SET.</summary>
    public void Recreate<TPart>(IReadOnlyList<TPart> parts, TPart blank, Func<T, TPart, T> store)
    {
        int target = Math.Max(parts.Count, _min);   // §14.6.9.2: the minimum-capacity fill
        RaiseImplicitOverflow(target, "recreation by a variable-length group transfer");
        if (target > MaxOccurrences) { TableLimit(target.ToString(), $"the implementor maximum ({MaxOccurrences})"); return; }
        T[] fresh;
        try
        {
            fresh = new T[Math.Max(target, 4)];
            for (int i = 0; i < target; i++)
                fresh[i] = store(_seedAt(i + 1), i < parts.Count ? parts[i] : blank);
        }
        catch (OutOfMemoryException)
        {
            TableLimit(target.ToString(), "the resources available at runtime");   // §8.5.1.9.6 2): the table is unchanged
            return;
        }
        _store = fresh;
        _count = target;
    }

    /// <summary>ISO §14.6.9.4, Space filling a dynamic table (kb/Work PB393): "If a dynamic table is subordinate
    /// to a variable-length group that is to be space filled as part of the execution of a MOVE statement, the
    /// current capacity of the dynamic table is unaffected, and each element of the dynamic table is
    /// space-filled." It is §14.9.25.4 GR9b step 2 — the receiving group's EXCESS part — so it is deliberately
    /// NOT <see cref="FromCurrentImage"/> with an empty content: that one recreates the table at capacity zero,
    /// which is §14.6.9.2's rule for a sender that really did carry an empty table. The two cases arrive as the
    /// same zero-length string and are told apart by <c>CobolVarGroup.HasDyn</c>.
    /// <para>A NUMERIC element holds those spaces because the compiler stores it as its character image — a
    /// <c>CobolDynTable&lt;string&gt;</c> whose <paramref name="store"/> passes the image through (kb/Work PB1939);
    /// a native carrier would decode them to zero.</para>
    /// <para><paramref name="blank"/> is a space-filled occurrence in the form <paramref name="store"/> distributes: an
    /// image of spaces, or — for a table whose elements are variable-length groups (kb/Work PB2496) — the empty carrier,
    /// which §14.9.25.4 GR9b's own steps space-fill (a dynamic-length item at length zero, a nested table §14.6.9.4 again,
    /// everything else spaces).</para></summary>
    public void SpaceFillElements<TPart>(TPart blank, Func<T, TPart, T> store)
    {
        for (int i = 0; i < _count; i++) _store[i] = store(_seedAt(i + 1), blank);
    }

    /// <summary>Mark the start of a SEARCH of this table (a SET Format 14 on it while active raises EC-FLOW-SEARCH,
    /// §14.9.39 GR31). Nestable (re-entrant SEARCH).</summary>
    public void EnterSearch() => _searching++;

    /// <inheritdoc cref="EnterSearch"/>
    public void ExitSearch() { if (_searching > 0) _searching--; }
}
