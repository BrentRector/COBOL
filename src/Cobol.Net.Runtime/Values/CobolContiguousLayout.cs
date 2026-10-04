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
/// <param name="StructureCodes">Component k's DYNAMIC LENGTH STRUCTURE layout (§12.3.7.4 GR18/GR19) as
/// <see cref="CobolDynStructure.Code"/>, 0 for a component the clause names no structure for (§13.18.19.3 SR3 —
/// the implementor's structure, which is the bare data and the extent table), or null when no component has one.
/// <para>⛔ THE LAYOUT HAS TWO FORMS OF THE ONE RECORD (kb/Work PB1094). The CONTIGUOUS image is what a procedural
/// operation on the group sees (§8.5.1.11.2) — the data alone. The MEDIUM image is what a file or a sort store holds:
/// the contiguous image with each structured component replaced by its length field, data and delimiter
/// (<see cref="CobolDynStructure.Frame"/>). A WRITE / REWRITE / RELEASE converts once on the way out
/// (<see cref="MediumImage"/> + <see cref="MediumExtents"/>); a READ / RETURN converts back once in
/// <see cref="Decompose"/>. An unstructured layout's two forms are the same string, so nothing else changes.</para></param>
/// <param name="OdoTail">True when the LAST component is the group's OCCURS DEPENDING table (kb/Work PB244): the
/// record's trailing storage (§13.18.38.3 SR22), one element per unit up to the table's maximum. The group's
/// activation-boundary carrier (<c>AsVarImage</c>) holds that table in its FIXED run, so this layout converts at its
/// two doors — <see cref="ExtentsOf"/> and <see cref="Decompose"/> — and its <see cref="ComponentOffsets"/> list
/// only the components the carrier has.</param>
public sealed class CobolContiguousLayout(int FixedTotal, int[] FixedAt, int[] Unit, long[] MaxUnits,
    IReadOnlyList<int>? StructureCodes = null, bool OdoTail = false)
{
    private readonly CobolDynStructure?[]? _structure = CobolDynStructure.FromCodes(StructureCodes);

    /// <summary>Component k's DYNAMIC LENGTH STRUCTURE, or null when it has none (see the type summary).</summary>
    public CobolDynStructure? StructureOf(int k) => _structure?[k];

    /// <summary>Does any component of this layout have a DYNAMIC LENGTH STRUCTURE — is its MEDIUM image different
    /// from its contiguous one.</summary>
    public bool HasStructure => _structure is not null;

    /// <summary>The most characters component k can occupy in the MEDIUM image — its maximum data
    /// (§8.5.1.10.1) plus its length field and delimiter. What the fixed form (<see cref="ToFixedForm"/>) pads it to.</summary>
    private long MaxExtent(int k) => MaxUnits[k] * Unit[k] + (_structure?[k]?.Overhead ?? 0);

    /// <summary>⛔ THE MEDIUM IMAGE of a record this layout's type sent — the contiguous <paramref name="image"/> with
    /// each structured component framed by its DYNAMIC LENGTH STRUCTURE (§12.3.7.4 GR18 length field before the data,
    /// GR19 delimiter after it). <paramref name="extents"/> is the table that was built over the same image
    /// (<see cref="ExtentsOf"/>); an unstructured layout returns the image itself.</summary>
    /// <exception cref="InvalidOperationException">The table does not describe the image — a record sent through its
    /// own layout always does, so this is a compiler defect, never a program's.</exception>
    public string MediumImage(string image, RecordExtents extents)
    {
        if (_structure is null) return image;
        var lengths = Recorded(image, extents)
            ?? throw new InvalidOperationException("the extent table does not describe the record it was built over");
        var sb = new System.Text.StringBuilder(image.Length + 16);
        int at = 0, fixedDone = 0;
        for (int k = 0; k < FixedAt.Length; k++)
        {
            int lead = FixedAt[k] - fixedDone;
            sb.Append(image, at, lead);
            at += lead;
            fixedDone = FixedAt[k];
            string data = image.Substring(at, lengths[k]);
            sb.Append(_structure[k] is { } st ? st.Frame(data) : data);
            at += lengths[k];
        }
        sb.Append(image, at, image.Length - at);
        return sb.ToString();
    }

    /// <summary>The extent table of the MEDIUM image: <paramref name="extents"/> with each structured component's
    /// length increased by its length field and delimiter, so the table describes the characters a file holds
    /// (<see cref="RecordExtents.Describes"/>) and a key behind a structured member is found where it really is
    /// (<see cref="Position"/>). An unstructured layout returns the table itself.</summary>
    public RecordExtents MediumExtents(RecordExtents extents)
    {
        if (_structure is null) return extents;
        var lengths = new int[extents.Count];
        for (int k = 0; k < lengths.Length; k++) lengths[k] = extents.Lengths[k] + (_structure[k]?.Overhead ?? 0);
        return new RecordExtents(FixedAt, lengths, this);
    }

    /// <summary>Each variable-length component's offset in the FIXED run, in the carrier's flattened order — where
    /// <see cref="CobolVarGroup.Compare"/> interleaves the components with the fixed material (ISO §8.8.4.2.17).</summary>
    public IReadOnlyList<int> ComponentOffsets => OdoTail ? FixedAt[..^1] : FixedAt;

    /// <summary>The EXTENT TABLE of a record of this type (D-FRA (v); kb/Work PB1053): each variable-length
    /// component's fixed-run offset and the length, in characters, of its content in <paramref name="current"/> —
    /// the carrier the record's own <c>AsVarImage()</c> composes, whose components are exactly the ones this layout
    /// lists, in the same flattened order. What a WRITE / REWRITE / RELEASE of the record sends beside its
    /// contiguous image.</summary>
    public RecordExtents ExtentsOf(CobolVarGroup current)
    {
        if (OdoTail) current = current.SplitTail(FixedAt[^1]);
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
    /// shares — each member at the position it has when it holds its MAXIMUM size (§8.5.1.10.1 — the number
    /// §13.18.43.4 GR8 b) sums for the record), padded with spaces — and <see cref="Decompose(string, RecordExtents?, bool)"/> takes each member back at that width and
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
            sb.Append(' ', (int)Math.Min(Math.Max(0, MaxExtent(k) - lengths[k]), int.MaxValue));
        }
        sb.Append(image, at, image.Length - at);
        return sb.ToString();
    }

    /// <summary>The record decomposed into its carrier — <see cref="CobolVarGroup.FromContiguous"/>'s rule, by the
    /// record's own <paramref name="extents"/> when they describe it (<see cref="RecordExtents.Describes"/>), by the
    /// take step otherwise. <paramref name="fixedForm"/> says the record is the fixed form of a file of
    /// FIXED-LENGTH records (<see cref="ToFixedForm"/>): the members then take their maximum widths and each drops
    /// the space padding that fills its field.</summary>
    public CobolVarGroup Decompose(string record, RecordExtents? extents = null, bool fixedForm = false)
    {
        var carrier = CobolVarGroup.FromContiguous(record, FixedTotal, FixedAt, Unit, MaxUnits,
            Recorded(record, extents), fixedForm, _structure);
        return OdoTail ? carrier.JoinTail(FixedAt[^1]) : carrier;
    }

    /// <summary>The character position, in <paramref name="record"/> (the MEDIUM image — the record as the file holds
    /// it), of the fixed material at <paramref name="fixedOffset"/> of the FIXED run: that offset plus what every
    /// variable-length component PRECEDING it occupies in this record — its recorded extent when
    /// <paramref name="extents"/> describe the record, the take step's share (or its own structure's) otherwise, the
    /// same sources <see cref="Decompose"/> reads through <see cref="CobolVarGroup.ComponentTake"/>, so a key is found
    /// exactly where the decomposition puts it. A component at <c>FixedAt[k] ≤ fixedOffset</c> precedes the
    /// member — a fixed member cannot start where a following component starts, because it occupies at least one
    /// position of the fixed run first.</summary>
    public int Position(string record, int fixedOffset, RecordExtents? extents = null)
    {
        record ??= "";
        var recorded = Recorded(record, extents);
        long excess = Math.Max(0, record.Length - FixedTotal);
        long at = fixedOffset;
        for (int k = 0; k < FixedAt.Length && FixedAt[k] <= fixedOffset; k++)
            at += CobolVarGroup.ComponentTake(k, record, (int)Math.Min(int.MaxValue, at - fixedOffset + FixedAt[k]), ref excess,
                recorded, fixedForm: false, Unit[k], MaxUnits[k], _structure, out _);
        return (int)Math.Min(int.MaxValue, at);
    }

    /// <summary>The recorded component lengths, when <paramref name="extents"/> is THIS record's table under THIS
    /// layout; null otherwise — the ONE place the §8.5.1.12.2 correspondence test is asked.</summary>
    private IReadOnlyList<int>? Recorded(string? record, RecordExtents? extents) =>
        extents is not null && extents.Describes(record?.Length ?? 0, FixedTotal, FixedAt, Unit) ? extents.Lengths : null;
}
