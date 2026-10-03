// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE EXTENT TABLE OF ONE VARIABLE-LENGTH RECORD — where each of its variable-length components ended when the
/// record was sent (docs/CONFORMANCE.md §3 determination D-FRA (v); kb/Work PB1053).
/// <para>A WRITE / REWRITE / RELEASE sends a variable-length group record as its contiguous image (ISO
/// §8.5.1.11.2), and that image carries no marker of where a dynamic member ends: with two or more variable-length
/// components no split of the characters alone can recover the record the program wrote (<c>"AA" "KEY" "CCCC"</c>
/// and <c>"AAKEY" "CCC" "C"</c> are the same characters). The table is the implementor information §9.1.7.2 admits
/// "on the physical storage medium" beside the record — it travels with the record through every file
/// organization that frames its records and through the sort store, and §12.4.5.11.4 GR1 keeps it out of the
/// record area and the record size: the RECORD is still exactly the contiguous image.</para>
/// <para>Component <c>k</c> is described by <see cref="FixedAt"/>[k] — its offset in the record's FIXED run, the
/// §8.5.1.12.3 zero-length accounting — and <see cref="Lengths"/>[k], its current length in characters.
/// <see cref="CobolContiguousLayout.Decompose(string, RecordExtents?)"/> honours the table only for a receiving
/// record whose components CORRESPOND to it (§8.5.1.12.2 — "Two dynamic-length elementary items correspond if they
/// start at the same relative byte positions within their groups"), and only when it describes the very
/// characters received; any other record is split by the D-FRA take step.</para>
/// </summary>
public sealed class RecordExtents
{
    /// <summary>A table of <paramref name="fixedAt"/>.Length components. The arrays are owned by the table from
    /// here on. <paramref name="layout"/> is the record type's layout, which a table a PROGRAM built (the sending
    /// side of a WRITE, <see cref="CobolContiguousLayout.ExtentsOf"/>) carries so a file of fixed-length records can
    /// ask for the record's fixed form (<see cref="CobolContiguousLayout.ToFixedForm"/>); a table read back from a
    /// frame has none, because a frame records only where each component ended.</summary>
    /// <exception cref="ArgumentException">The arrays differ in length, or a length is negative.</exception>
    public RecordExtents(int[] fixedAt, int[] lengths, CobolContiguousLayout? layout = null)
    {
        ArgumentNullException.ThrowIfNull(fixedAt);
        ArgumentNullException.ThrowIfNull(lengths);
        if (fixedAt.Length != lengths.Length)
            throw new ArgumentException("an extent table needs one length per component offset", nameof(lengths));
        foreach (int n in lengths)
            if (n < 0) throw new ArgumentException("a component length is never negative", nameof(lengths));
        FixedAt = fixedAt;
        Lengths = lengths;
        Layout = layout;
    }

    /// <summary>The layout of the record type this table was built over, or null for a table read from a frame
    /// (see the constructor).</summary>
    public CobolContiguousLayout? Layout { get; }

    /// <summary>⛔ THE MEDIUM IMAGE of the record this table was built over (<see cref="CobolContiguousLayout.MediumImage"/>;
    /// kb/Work PB1094): <paramref name="image"/> — the contiguous <c>CurrentImage()</c> — with each component a
    /// DYNAMIC LENGTH STRUCTURE lays out framed by its length field and delimiter (ISO §12.3.7.4 GR18, GR19), which is
    /// what a WRITE / REWRITE / RELEASE hands the file or sort store. A table with no layout, or whose layout has no
    /// structured component, answers the image itself.</summary>
    public string MediumImage(string image) => Layout is { HasStructure: true } layout ? layout.MediumImage(image, this) : image;

    /// <summary>The table of that medium image (<see cref="CobolContiguousLayout.MediumExtents"/>) — each structured
    /// component's length widened by its length field and delimiter; this table itself when nothing is structured.</summary>
    public RecordExtents Medium() => Layout is { HasStructure: true } layout ? layout.MediumExtents(this) : this;

    /// <summary>Component k's offset in the record's FIXED run.</summary>
    public IReadOnlyList<int> FixedAt { get; }

    /// <summary>Component k's current length in characters.</summary>
    public IReadOnlyList<int> Lengths { get; }

    /// <summary>How many variable-length components the table describes.</summary>
    public int Count => FixedAt.Count;

    /// <summary>Does this table describe <paramref name="recordLength"/> characters of a record whose FIXED run is
    /// <paramref name="fixedTotal"/> wide and whose components sit at <paramref name="fixedAt"/> in
    /// <paramref name="unit"/>-character units — the §8.5.1.12.2 correspondence (same components at the same
    /// fixed-run positions, whole units of each) AND the record actually received (its length is the fixed run plus
    /// every component). A record truncated or padded on its way, or sent through another description, fails it and
    /// is not described by this table.</summary>
    public bool Describes(int recordLength, int fixedTotal, IReadOnlyList<int> fixedAt, IReadOnlyList<int> unit)
    {
        if (fixedAt.Count != Count) return false;
        long total = fixedTotal;
        for (int k = 0; k < Count; k++)
        {
            if (FixedAt[k] != fixedAt[k]) return false;
            if (unit[k] <= 0 ? Lengths[k] != 0 : Lengths[k] % unit[k] != 0) return false;
            total += Lengths[k];
        }
        return total == recordLength;
    }
}
