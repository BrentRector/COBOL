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
/// <param name="Odo">The group's OCCURS DEPENDING table (<see cref="OdoTail"/>; kb/Work PB244, PB2497), the record's
/// trailing storage (§13.18.38.3 SR22), or <c>default</c> when it holds none. Its two shapes are two layouts:
/// <list type="bullet">
/// <item>ELEMENTS OF A FIXED IMAGE (<c>Comps</c> zero): the table is the LAST component, one element per unit up to
/// its maximum, so the record's own length says how many occurrences it holds. The group's activation-boundary
/// carrier (<c>AsVarImage</c>) holds that table in its FIXED run, so this layout converts at its two doors —
/// <see cref="ExtentsOf"/> and <see cref="Decompose"/>.</item>
/// <item>VARIABLE-LENGTH ELEMENTS (<c>Comps</c> above zero): the components are listed at the table's MAXIMUM (where
/// a comparison and the activation boundary look for them) and <paramref name="FixedTotal"/> holds every occurrence's
/// fixed run. A record holding N occurrences is laid out by the first <c>Comps</c> × N of the table's components and
/// N occurrences' fixed runs (<see cref="AtCount"/>). The record's length cannot say N, because the elements have no
/// fixed width, so N is the count the record's EXTENT TABLE states by the number of components it describes
/// (<see cref="OdoTail.CountFrom"/>), the maximum for the fixed form of a file of fixed-length records, and otherwise
/// the count the READ / RETURN supplies (<see cref="Decompose"/>, <see cref="StatesCount"/>; determination D-FRA
/// (viii), docs/CONFORMANCE.md §3).</item>
/// </list></param>
public sealed class CobolContiguousLayout(int FixedTotal, int[] FixedAt, int[] Unit, long[] MaxUnits,
    IReadOnlyList<int>? StructureCodes = null, OdoTail Odo = default)
{
    private readonly CobolDynStructure?[]? _structure = CobolDynStructure.FromCodes(StructureCodes);

    /// <summary>The OCCURS DEPENDING table of fixed-image elements is this layout's LAST component (see <c>Odo</c>).</summary>
    private bool TableIsLastComponent => Odo.Present && Odo.Comps == 0;

    /// <summary>The OCCURS DEPENDING table's elements hold components, listed at its maximum (see <c>Odo</c>): a
    /// record's layout is then <see cref="AtCount"/> at the count the record holds.</summary>
    private bool ElementsHoldComponents => Odo.Present && Odo.Comps > 0;

    /// <summary>The layout of a record whose table of variable-length elements holds <paramref name="count"/>
    /// occurrences (clamped to 0..the maximum, as <see cref="OdoTail.CutAt"/> clamps): the components of the
    /// occurrences beyond it and their fixed runs left out. The table is the record's trailing storage
    /// (§13.18.38.3 SR22), so they are the layout's last ones. A plain layout: it holds no table.</summary>
    private CobolContiguousLayout AtCount(int count)
    {
        int m = FixedAt.Length - Odo.CutComponents(count);
        return new(FixedTotal - Odo.CutAt(count), FixedAt[..m], Unit[..m], MaxUnits[..m],
            StructureCodes is null ? null : [.. StructureCodes.Take(m)]);
    }

    /// <summary>For a table of variable-length elements: the layout at the count <paramref name="extents"/> states
    /// (<see cref="OdoTail.CountFrom"/>), when that table describes <paramref name="record"/> under it — the record's
    /// own statement of how many occurrences it holds; null when it does not.</summary>
    private CobolContiguousLayout? Stated(string? record, RecordExtents? extents) =>
        extents is not null && Odo.CountFrom(extents.Count, FixedAt.Length) is int n && AtCount(n) is var at
            && at.Recorded(record, extents) is not null ? at : null;

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
        if (ElementsHoldComponents)
            return (Stated(image, extents)
                ?? throw new InvalidOperationException("the extent table does not describe the record it was built over"))
                .MediumImage(image, extents);
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
        return new RecordExtents([.. extents.FixedAt], lengths, this);
    }

    /// <summary>The EXTENT TABLE of a record of this type (D-FRA (v); kb/Work PB1053): each variable-length
    /// component's fixed-run offset and the length, in characters, of its content in <paramref name="current"/> —
    /// the carrier the record's own <c>AsVarImage()</c> composes, whose components are exactly the ones this layout
    /// lists, in the same flattened order. What a WRITE / REWRITE / RELEASE of the record sends beside its
    /// contiguous image. A carrier holding a table of variable-length elements holds the components of the occurrences
    /// it was composed with (<c>AsVarImage(__odo)</c>), so its table describes exactly those: the count a READ finds
    /// again (<see cref="OdoTail.CountFrom"/>; kb/Work PB2497).</summary>
    public RecordExtents ExtentsOf(CobolVarGroup current)
    {
        if (TableIsLastComponent) current = current.SplitTail(FixedAt[^1]);
        var lengths = new int[ElementsHoldComponents ? Math.Min(FixedAt.Length, current.Dynamic.Length) : FixedAt.Length];
        for (int k = 0; k < lengths.Length; k++) lengths[k] = current.Dyn(k).Length;
        return ExtentsFrom(lengths);
    }

    /// <summary>The extent table of a record whose component lengths are already known — one per component of this
    /// layout, the OCCURS DEPENDING table of fixed-image elements last; for a table of variable-length elements, one per
    /// component of the occurrences the record holds (this layout's first <paramref name="lengths"/>.Length). The door
    /// for a record held in a storage cell, which reads each component's length off its own slot rather than off a
    /// composed carrier (kb/Work PB244).</summary>
    /// <exception cref="ArgumentException">More lengths than this layout has components.</exception>
    public RecordExtents ExtentsFrom(int[] lengths) =>
        lengths.Length == FixedAt.Length ? new(FixedAt, lengths, this)
        : lengths.Length < FixedAt.Length ? new(FixedAt[..lengths.Length], lengths, this)
        : throw new ArgumentException("an extent table has at most one length per component of its layout", nameof(lengths));

    /// <summary>⛔ THE FIXED FORM OF A VARIABLE-LENGTH RECORD — the record as a file of FIXED-LENGTH records holds it
    /// (determination D-FRA (vi); kb/Work PB1562). ISO §13.18.43.4 GR6 makes every record of a Format 1 file the same
    /// size and §9.1.6 makes the record type and size fixed file attributes that every program using the file
    /// shares, so such a file can neither frame its records as variable-length ones nor carry an extent table: a
    /// second program describing the same file as <c>RECORD CONTAINS 20</c> over <c>PIC X(20)</c> must read the
    /// same bytes. A record with variable-length members therefore occupies the one shape every record of the file
    /// shares — each member at the position it has when it holds its MAXIMUM size (§8.5.1.10.1 — the number
    /// §13.18.43.4 GR8 b) sums for the record), padded with spaces — and <see cref="Decompose"/> takes each member back at that width and
    /// drops the padding: a space in a fixed-size field is padding, never data.
    /// <para>The characters of a member are its content character for character (a national item holds UTF-16
    /// characters, DOC-A.1-63), so the padding is the space character. Returns <paramref name="image"/> unchanged
    /// when <paramref name="extents"/> do not describe it (a record sent through another description, a table read
    /// from a frame) — there is then no member boundary to pad at.</para></summary>
    /// <param name="image">The record's contiguous image (<c>CurrentImage()</c>).</param>
    /// <param name="extents">The table that was sent with it (<see cref="ExtentsOf"/>).</param>
    /// <para>A table of variable-length elements is padded through the layout at the count the table states
    /// (<see cref="AtCount"/>): the occurrences the record does not hold lie beyond its end, and the file's fixed
    /// length fills them with spaces, which is exactly their fixed form at the maximum, where a READ decomposes
    /// them.</para>
    public string ToFixedForm(string image, RecordExtents? extents)
    {
        if (ElementsHoldComponents) return Stated(image, extents)?.ToFixedForm(image, extents) ?? image;
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
    /// the space padding that fills its field.
    /// <para>⛔ A TABLE OF VARIABLE-LENGTH ELEMENTS (kb/Work PB2497; determination D-FRA (viii)) is decomposed at the
    /// count the record states: its extent table's (<see cref="StatesCount"/>), or the MAXIMUM for the fixed form,
    /// where every occurrence sits at its fixed-form position. Otherwise it is decomposed at <paramref name="count"/>,
    /// the count the READ / RETURN supplies (clamped to the table's bounds; the maximum by default).</para></summary>
    public CobolVarGroup Decompose(string record, RecordExtents? extents = null, bool fixedForm = false,
                                   int count = int.MaxValue)
    {
        if (ElementsHoldComponents)
            return (Stated(record, extents) ?? AtCount(fixedForm ? Odo.Max : count)).Decompose(record, extents, fixedForm);
        var carrier = CobolVarGroup.FromContiguous(record, FixedTotal, FixedAt, Unit, MaxUnits,
            Recorded(record, extents), fixedForm, _structure);
        return TableIsLastComponent ? carrier.JoinTail(FixedAt[^1]) : carrier;
    }

    /// <summary>Does <paramref name="record"/> itself state how many occurrences of the OCCURS DEPENDING table it
    /// holds, so that <see cref="Decompose"/> ignored its <c>count</c>? Always, unless the table's elements are
    /// variable-length groups: the record's length then says nothing, and only its own extent table (when it travelled
    /// and describes the record) or the fixed form of a file of fixed-length records states the count. When it does
    /// not, the READ / RETURN decomposes the record again at the count data-name-1 gives (kb/Work PB2497;
    /// determination D-FRA (viii)).</summary>
    public bool StatesCount(string record, RecordExtents? extents, bool fixedForm) =>
        !ElementsHoldComponents || fixedForm || Stated(record, extents) is not null;

    /// <summary>The character position, in <paramref name="record"/> (the MEDIUM image — the record as the file holds
    /// it), of the fixed material at <paramref name="fixedOffset"/> of the FIXED run: that offset plus what every
    /// variable-length component PRECEDING it occupies in this record — its recorded extent when
    /// <paramref name="extents"/> describe the record, the take step's share (or its own structure's) otherwise, the
    /// same sources <see cref="Decompose"/> reads through <see cref="CobolVarGroup.ComponentTake"/>, so a key is found
    /// exactly where the decomposition puts it. A component at <c>FixedAt[k] ≤ fixedOffset</c> precedes the
    /// member — a fixed member cannot start where a following component starts, because it occupies at least one
    /// position of the fixed run first.</summary>
    /// <para>A table of variable-length elements lays the record out at the count its extent table states, and at the
    /// table's maximum when no table describes it (§13.18.38.4 GR8 b: a receiving group takes its maximum length) —
    /// the layout the READ's first decomposition uses (<see cref="Decompose"/>).</para>
    public int Position(string record, int fixedOffset, RecordExtents? extents = null)
    {
        if (ElementsHoldComponents)
            return (Stated(record, extents) ?? AtCount(Odo.Max)).Position(record, fixedOffset, extents);
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
