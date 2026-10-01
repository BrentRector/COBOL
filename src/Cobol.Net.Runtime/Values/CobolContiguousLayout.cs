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
        return new RecordExtents(FixedAt, lengths, this);
    }

    /// <summary>⛔ THE FIXED FORM OF A VARIABLE-LENGTH RECORD — the record as a file of FIXED-LENGTH records holds it
    /// (determination D-FRA (vi); kb/Work PB1562). ISO §13.18.43.4 GR6 makes every record of a Format 1 file the same
    /// size and §9.1.6 makes the record type and size fixed file attributes that every program using the file
    /// shares, so such a file can neither frame its records as variable-length ones nor carry an extent table: a
    /// second program describing the same file as <c>RECORD CONTAINS 20</c> over <c>PIC X(20)</c> must read the
    /// same bytes. A record with variable-length members therefore occupies the one shape every record of the file
    /// shares — each member at the position it has when it holds its MAXIMUM size (§13.18.43.4 GR8 b), padded with
    /// spaces) — and <see cref="Decompose(string, RecordExtents?, bool)"/> takes each member back at that width and
    /// drops the padding: a space in a fixed-size field is padding, never data.
    /// <para>The characters of a member are its content character for character (a national item holds UTF-16
    /// characters, DOC-A.1-63), so the padding is the space character. Returns <paramref name="image"/> unchanged
    /// when <paramref name="extents"/> do not describe it (a record sent through another description, a table read
    /// from a frame) — there is then no member boundary to pad at.</para></summary>
    /// <param name="image">The record's contiguous image (<c>CurrentImage()</c>).</param>
    /// <param name="extents">The table that was sent with it (<see cref="ExtentsOf"/>).</param>
    public string ToFixedForm(string image, RecordExtents? extents)
    {
        if (Recorded(image, extents) is not { } lengths) return image;
        var sb = new System.Text.StringBuilder(image.Length);
        int at = 0, fixedDone = 0;
        for (int k = 0; k < FixedAt.Length; k++)
        {
            int lead = FixedAt[k] - fixedDone;
            sb.Append(image, at, lead + lengths[k]);
            at += lead + lengths[k];
            fixedDone = FixedAt[k];
            sb.Append(' ', (int)Math.Min(Math.Max(0, MaxUnits[k] * Unit[k] - lengths[k]), int.MaxValue));
        }
        sb.Append(image, at, image.Length - at);
        return sb.ToString();
    }

    /// <summary>The record decomposed into its carrier — <see cref="CobolVarGroup.FromContiguous"/>'s rule, by the
    /// record's own <paramref name="extents"/> when they describe it (<see cref="RecordExtents.Describes"/>), by the
    /// take step otherwise. <paramref name="fixedForm"/> says the record is the fixed form of a file of
    /// FIXED-LENGTH records (<see cref="ToFixedForm"/>): the members then take their maximum widths and each drops
    /// the space padding that fills its field.</summary>
    public CobolVarGroup Decompose(string record, RecordExtents? extents = null, bool fixedForm = false) =>
        CobolVarGroup.FromContiguous(record, FixedTotal, FixedAt, Unit, MaxUnits, Recorded(record, extents), fixedForm);

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
