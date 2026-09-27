// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE LAYOUT OF A VARIABLE-LENGTH RECORD'S CONTIGUOUS IMAGE (docs/CONFORMANCE.md §3 determinations D-FRA
/// and D-KWV; kb/Work PB981, PB1025). A WRITE / RELEASE sends a variable-length record "as though it were in fact
/// contiguous with its neighbors" (ISO §8.5.1.11.2), so a record read back — or a record held in a sort store or
/// an indexed file — carries no marker of where a dynamic member ends. This object is that record type's
/// component layout, emitted ONCE per record type beside its <c>FromContiguousImage</c>, and it answers the two
/// questions every reader of such an image asks:
/// <list type="bullet">
/// <item><see cref="Decompose"/> — the whole record back into its carrier (the READ / RETURN half);</item>
/// <item><see cref="Position"/> — where a FIXED member (a SORT/MERGE key, an indexed RECORD KEY) sits in THIS
/// record, which varies from record to record when a dynamic member precedes it.</item>
/// </list>
/// Both answers read the SAME two sources, so they agree by construction and a key is found exactly where the
/// decomposition puts it: the record's own EXTENT TABLE (<see cref="RecordExtents"/>, determination D-FRA (v);
/// kb/Work PB1053) when it travelled with the record and describes it — every layout then round-trips, whatever
/// number of variable-length members sit on either side of a fixed one — and otherwise the ONE take step: walking
/// the components left to right, each takes as many whole units (one character of a dynamic-length item, one
/// element of a dynamic-capacity table) as the record holds beyond the FIXED material still to come, up to its
/// maximum.
/// </summary>
/// <param name="FixedTotal">The width of the FIXED run — the record with every variable-length component
/// collapsed to nothing (the §8.5.1.12.3 zero-length accounting).</param>
/// <param name="FixedAt">Component k's offset in the FIXED run.</param>
/// <param name="Unit">Component k's unit width in characters.</param>
/// <param name="MaxUnits">Component k's maximum size in units (§8.5.1.10.1 / the table's maximum capacity).</param>
public sealed class CobolContiguousLayout(int FixedTotal, int[] FixedAt, int[] Unit, long[] MaxUnits)
{
    /// <summary>Each variable-length component's offset in the FIXED run, in the carrier's flattened order — where
    /// <see cref="CobolVarGroup.Compare"/> interleaves the components with the fixed material (ISO §8.8.4.2.17).</summary>
    public IReadOnlyList<int> ComponentOffsets => FixedAt;

    /// <summary>The EXTENT TABLE of a record of this type (D-FRA (v); kb/Work PB1053): each variable-length
    /// component's fixed-run offset and the length, in characters, of its content in <paramref name="current"/> —
    /// the carrier the record's own <c>AsVarImage()</c> composes, whose components are exactly the ones this layout
    /// lists, in the same flattened order. What a WRITE / REWRITE / RELEASE of the record sends beside its
    /// contiguous image.</summary>
    public RecordExtents ExtentsOf(CobolVarGroup current)
    {
        var lengths = new int[FixedAt.Length];
        for (int k = 0; k < lengths.Length; k++) lengths[k] = current.Dyn(k).Length;
        return new RecordExtents(FixedAt, lengths);
    }

    /// <summary>The record decomposed into its carrier — <see cref="CobolVarGroup.FromContiguous"/>'s rule, by the
    /// record's own <paramref name="extents"/> when they describe it (<see cref="RecordExtents.Describes"/>), by the
    /// take step otherwise.</summary>
    public CobolVarGroup Decompose(string record, RecordExtents? extents = null) =>
        CobolVarGroup.FromContiguous(record, FixedTotal, FixedAt, Unit, MaxUnits, Recorded(record, extents));

    /// <summary>The character position, in <paramref name="record"/>, of the fixed material at
    /// <paramref name="fixedOffset"/> of the FIXED run: that offset plus what every variable-length component
    /// PRECEDING it took from this record — its recorded length when <paramref name="extents"/> describe the record,
    /// the take step's share otherwise, the same two sources <see cref="Decompose"/> reads, so a key is found
    /// exactly where the decomposition puts it. A component at <c>FixedAt[k] ≤ fixedOffset</c> precedes the
    /// member — a fixed member cannot start where a following component starts, because it occupies at least one
    /// position of the fixed run first.</summary>
    public int Position(string record, int fixedOffset, RecordExtents? extents = null)
    {
        var recorded = Recorded(record, extents);
        long excess = Math.Max(0, (record?.Length ?? 0) - FixedTotal);
        long at = fixedOffset;
        for (int k = 0; k < FixedAt.Length && FixedAt[k] <= fixedOffset; k++)
            at += recorded is not null ? recorded[k] : CobolVarGroup.ContiguousTake(ref excess, Unit[k], MaxUnits[k]);
        return (int)Math.Min(int.MaxValue, at);
    }

    /// <summary>The recorded component lengths, when <paramref name="extents"/> is THIS record's table under THIS
    /// layout; null otherwise — the ONE place the §8.5.1.12.2 correspondence test is asked.</summary>
    private IReadOnlyList<int>? Recorded(string? record, RecordExtents? extents) =>
        extents is not null && extents.Describes(record?.Length ?? 0, FixedTotal, FixedAt, Unit) ? extents.Lengths : null;
}
