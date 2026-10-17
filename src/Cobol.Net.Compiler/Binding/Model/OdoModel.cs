// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using OdoTail = CobolNet.Runtime.OdoTail;

namespace CobolNet.Binding.Model;

/// <summary>ONE data-name-2 of an OCCURS KEY phrase (ISO §13.18.38) together with the direction word that governs
/// it: the key's name, its IN/OF qualifiers AS WRITTEN (innermost first), and the direction.
/// <para>⛔ THE QUALIFIERS ARE PART OF THE KEY (kb/Work PB1018). §13.18.38.3 SR3 confines data-name-2 to "the entry
/// containing the OCCURS clause or an entry subordinate to" it, but it does not make the name unique there: a table
/// element <c>E</c> holding <c>A.K</c> and <c>B.K</c> must write <c>KEY IS K OF B</c>, and §8.4.2.2.3 SR1 makes that
/// qualification the ONLY thing that says which K is the key. This record used to carry the bare name with a
/// comment claiming the qualifier chain "adds nothing", and the lookup took the FIRST K under the table — so a
/// legal <c>SEARCH ALL … WHEN K OF B (IX) = 2</c> was refused and the table SORT keyed on A's K.</para></summary>
public readonly record struct OccursKey(string Name, IReadOnlyList<string> Qualifiers, bool Descending)
{
    /// <summary>The key as the programmer wrote it (<c>K OF B</c>), for a diagnostic that quotes the phrase.</summary>
    public string Written => DataBinder.WrittenQualified(Name, Qualifiers);
}

/// <summary>
/// The structured OCCURS description (ISO/IEC 1989:2023 §13.18.38) attached to a table's <see cref="DataItem"/>:
/// the Format-2 occurrence bounds (<c>OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1</c>), the
/// DEPENDING ON data-name and its post-build-resolved item, and the ASCENDING/DESCENDING KEY data-names (Formats
/// 1 and 2 alike — §13.18.38 GR3, consumed by SEARCH ALL and the table SORT). The ARRAY itself is always
/// allocated at <see cref="Max"/> occurrences — §8.5.1.8: for an occurs-depending table "the physical capacity is
/// fixed at compile time; the logical capacity may vary" — so only the GR7 current-count machinery (group-operand
/// extents per GR8, the SEARCH bound per §14.9.37.4 GR4) consults this object; layout, width, and VALUE
/// initialization (§13.18.63 GR6 — as if the count were the maximum) ride on <see cref="DataItem.Occurs"/>.
/// </summary>
public sealed class OccursSpec
{
    /// <summary>integer-1 — the minimum occurrence count (<c>0 ≤ Min &lt; Max</c>, §13.18.38 SR16). Equals
    /// <see cref="Max"/> for a fixed (Format 1) table.</summary>
    public required int Min { get; init; }

    /// <summary>integer-2 — the maximum occurrence count, the allocated physical capacity (§8.5.1.8). ZERO for a
    /// DYNAMIC-capacity table, which has no allocated maximum: ask <see cref="MaxCapacity"/> for "how many occurrences
    /// can it hold".</summary>
    public required int Max { get; init; }

    /// <summary>⛔ THE ONE ANSWER TO "HOW MANY OCCURRENCES CAN THIS TABLE HOLD?" for a layout that sizes a table
    /// component by its maximum (<c>VarGroupWindow</c>, <c>CellGroupShape</c>, a declared group's contiguous layout):
    /// <see cref="Max"/> for a fixed or OCCURS DEPENDING table (§8.5.1.7, §8.5.1.8), and for a DYNAMIC-capacity table
    /// the IMPLEMENTOR'S maximum (<c>CobolDynTable.MaxOccurrences</c>, docs/CONFORMANCE.md DOC-A.1-60), because
    /// §8.5.1.9.1 2) says "it may have an unlimited capacity". ⚠ NOT <see cref="ExpectedMax"/> (<c>TO integer-5</c>):
    /// §13.18.38.4 GR17 makes that only "the expected capacity of the table", and a SET past it sets EC-BOUND-SET and
    /// still grows the table (§14.9.39.4 GR30), so it bounds nothing. Reading <see cref="Max"/> here gave every dynamic
    /// table a maximum of zero (kb/Work PB2833).</summary>
    public int MaxCapacity => IsDynamic ? CobolNet.Runtime.CobolDynTable<byte>.MaxOccurrences : Max;

    /// <summary>The <c>DEPENDING ON data-name-1</c> name as written, or <see langword="null"/> for a fixed
    /// (Format 1) table.</summary>
    public string? DependingName { get; init; }

    /// <summary>data-name-1's IN/OF qualifier words as written, innermost first (ISO §8.4.2.2.2 Format 1 — a
    /// qualified-data-name). Before kb/Work PB885 the capture was the whole reference's <c>GetText()</c>, which
    /// glued <c>CNT OF G1</c> into the undefined name <c>CNTOFG1</c> and rejected legal source.</summary>
    public IReadOnlyList<string> DependingQualifiers { get; init; } = [];

    /// <summary>The resolved data-name-1 item — set by the post-build <c>DataBinder.OdoResolve</c> pass
    /// (data-name-1 may legally be declared anywhere outside the span the table starts, §13.18.38 SR20, so
    /// resolution must wait for the complete forest).</summary>
    public DataItem? Depending { get; set; }

    /// <summary>The KEY phrase as written: EVERY ASCENDING/DESCENDING key data-name in ONE list, in the order the
    /// phrase specifies them. That order is load-bearing — ISO §13.18.38.4 GR3: "If more than one data-name-2 is
    /// specified, they are specified in descending order of significance" — and §14.9.37.3 SR11 is a rule ABOUT
    /// it ("all preceding data-names in that KEY phrase or their associated condition-names shall also be
    /// referenced"), so the ORDER is what is stored. Two per-direction lists cannot express it: a mixed phrase
    /// (<c>ASCENDING KEY IS A B DESCENDING KEY IS C</c>) loses which of A/B/C precedes which the moment it is
    /// split in two. (The two write-only per-direction lists this replaced were READ by nothing, so nothing had
    /// ever contradicted their shape — feedback_a_dead_lookup_is_also_unverified.)</summary>
    public List<OccursKey> Keys { get; } = [];

    /// <summary>The KEY phrase RESOLVED, one entry per <see cref="Keys"/> entry in the same order — set by the
    /// post-build <c>DataBinder.OccursKeyResolve</c> pass through the ONE §8.4.2.2 resolver (a TYPEDEF clone gets its
    /// own spec and is resolved against its own subtree, like <see cref="Depending"/>). An entry is
    /// <see langword="null"/> where the key identifies no unique item within the table — reported there, once.</summary>
    public List<DataItem?> ResolvedKeys { get; } = [];

    // ── Format 4: DYNAMIC-capacity table (ISO §13.18.38 Format 4, COBOL-2014; data-model D9) ──────────────────

    /// <summary>True for a Format-4 DYNAMIC-capacity table (§8.5.1.9) — its capacity varies at run time. Mutually
    /// exclusive with the Format-2 DEPENDING form. A dynamic table has no fixed <see cref="DataItem.Occurs"/>.</summary>
    public bool IsDynamic { get; init; }

    /// <summary><c>CAPACITY IN data-name-3</c> — the current-capacity register name (§13.18.38 GR15), or null.</summary>
    public string? CapacityName { get; init; }

    /// <summary>The synthetic CAPACITY register item — a view over the table's Capacity, minted for EVERY dynamic
    /// table (FUNCTION LENGTH's §15.50.4 r7c reads it; kb/Work PB61) and NAMED only under CAPACITY IN; set by the post-build
    /// <c>DataBinder.DynamicResolve</c> pass (data-name-3 is implicitly defined at the OCCURS entry, SR30).</summary>
    public DataItem? CapacityRegister { get; set; }

    /// <summary><c>FROM integer-4</c> — the minimum / initial current capacity (§13.18.38 GR16); null ⇒ 0.</summary>
    public int? InitialCap { get; init; }

    /// <summary><c>TO integer-5</c> — the expected capacity (§13.18.38 GR17); null ⇒ unlimited.</summary>
    public int? ExpectedMax { get; init; }

    /// <summary>The INITIALIZED phrase — seed each new/intermediate occurrence per §8.5.1.9.5.</summary>
    public bool Initialized { get; init; }
}

/// <summary>Where an OCCURS DEPENDING table lies in the group that holds it as its trailing storage
/// (<see cref="OdoModel.GeometryOf"/>): <paramref name="FixedUnits"/> before the table, <paramref name="ElemUnits"/> per
/// occurrence, the §13.18.38.4 GR7 bounds, in units of which <paramref name="PositionsPerCharacter"/> make one
/// character of the group's image — 1 (the unit IS a character, bytes on the physical basis) or 8 (bits, §8.5.1.6.3).
/// <paramref name="TrailingUnits"/> is the padding of the LAST occurrence that its <paramref name="ElemUnits"/> stride
/// counts and its extent does not (<see cref="BitLayout.TrailingFillerBits"/>: a bit table's with an ALIGNED subject or member, §13.18.1.4 GR2),
/// so the extent at <c>c</c> occurrences is <c>FixedUnits + c × ElemUnits − (c &gt; 0 ? TrailingUnits : 0)</c>.</summary>
public readonly record struct OdoGeometry(int FixedUnits, int ElemUnits, int MinOccurs, int MaxOccurs, int PositionsPerCharacter,
    int TrailingUnits)
{
    /// <summary>The extent is counted in BITS (a USAGE BIT leaf lies in the subtree).</summary>
    public bool InBits => PositionsPerCharacter > 1;
}

/// <summary>Pure model helpers for the OCCURS DEPENDING ON subsystem (ISO/IEC 1989:2023 §13.18.38).
/// (<c>OdoGroupPlace</c> — the GR8 group-operand decoration — lives with the rest of the <see cref="Place"/>
/// hierarchy in <c>Place.cs</c>, P5.11a.)</summary>
public static class OdoModel
{
    /// <summary>The table's OCCURS KEY phrase resolved to items, in the phrase's own significance order
    /// (ISO §13.18.38.4 GR3) — one entry per <see cref="OccursSpec.Keys"/> entry, <see langword="null"/> where the
    /// key data-name identifies no unique item within the table. Empty for a table with no KEY phrase, which is
    /// exactly the §14.9.37.3 SR7 condition ("The OCCURS clause associated with identifier-1 shall contain the KEY
    /// phrase"). The resolution itself is <c>DataBinder.OccursKeyResolve</c>'s, done ONCE after the forest is
    /// built (kb/Work PB1018); every consumer — SEARCH ALL and the table SORT — reads it here.</summary>
    public static List<DataItem?> KeyItems(DataItem table) =>
        table.OccursSpec is { } spec
            ? spec.ResolvedKeys.Count == spec.Keys.Count ? spec.ResolvedKeys : [.. spec.Keys.Select(_ => (DataItem?)null)]
            : [];

    /// <summary>The occurrence of <paramref name="table"/> in the OCCURS nesting <paramref name="item"/> sits
    /// under, counting OUTERMOST first — i.e. the position, in the subscript list a reference to
    /// <paramref name="item"/> must write, of the subscript that selects <paramref name="table"/>'s occurrence.
    /// ISO §8.4.2.3.3 SR3 fixes both the count and the order — "the number of subscripts shall equal the number of
    /// OCCURS clauses in the description of the table element being referenced … the subscripts are written in the
    /// order of successively less inclusive dimensions of the table" — and §14.9.37.3 SR8/SR9 single out the one
    /// belonging to identifier-1 ("shall be subscripted by the first index-name associated with identifier-1 along
    /// with any subscripts required to uniquely identify the data item"). −1 when <paramref name="item"/> is not
    /// within <paramref name="table"/>.</summary>
    public static int SubscriptPositionOf(DataItem item, DataItem table) =>
        item.SubscriptLevels().IndexOf(table);   // outermost first — SR3's order, from the ONE walk (kb/Work PB877)

    /// <summary>The occurs-depending table among <paramref name="group"/>'s STRICT descendants, or
    /// <see langword="null"/>. At most one exists in a legal program: §13.18.38 SR22 makes it the unique trailing
    /// variable part of its record, and SR1(b)/SR10 forbid nesting it under another OCCURS — both validated by
    /// <c>DataBinder.OdoResolve</c>, so a first-match scan is exact.</summary>
    public static DataItem? TableUnder(DataItem group)
    {
        foreach (var c in group.Children)
        {
            if (c.OccursSpec is { DependingName: not null }) return c;
            if (TableUnder(c) is { } nested) return nested;
        }
        return null;
    }

    /// <summary>True when <paramref name="item"/> is <paramref name="ancestor"/> itself or lies within its
    /// subtree (the GR8a/GR8b "data item that describes the group" containment test).</summary>
    public static bool IsWithin(DataItem item, DataItem ancestor)
    {
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (ReferenceEquals(n, ancestor)) return true;
        return false;
    }

    /// <summary>Wrap a resolved GROUP place whose subtree contains the occurs-depending <paramref name="table"/>
    /// (ISO §13.18.38 GR8). The fixed prefix is everything before the table in the group's emitted image — exact
    /// because the table is the record's trailing storage (SR22, enforced at bind).
    /// <para>⛔ <b>THE EXTENT IS COUNTED IN THE GROUP'S OWN POSITION UNIT, WHICH IS BITS WHEN THE SUBTREE HOLDS A
    /// USAGE BIT LEAF</b> (kb/Work PB173). <c>fixed + occ × elem</c> is only an identity in a unit where each
    /// occurrence starts where the previous one ended, and §8.5.1.6.3's shared-byte runs break that in CHARACTERS:
    /// for <c>05 BT PIC 1(3) OCCURS 1 TO 4 DEPENDING ON N</c> the per-occurrence CHARACTER width rounds 3 bits up
    /// to 1, so three occurrences measured as 3 characters occupy 9 bits = 2 — and the fixed prefix computed as
    /// <c>PhysicalWidth − elem × max</c> came out NEGATIVE (an 8-bit group: 2 − 4 = −2), which made the whole
    /// group operand render as the EMPTY string. In BIT positions the same arithmetic is exact, because
    /// <see cref="BitLayout.ExtentBits"/> advances its cursor by <c>WidthBits × Occurs</c> in ONE contiguous step
    /// and every alignment round-up happens BEFORE the trailing table (SR22). <see cref="OdoGroupPlace.PositionsPerCharacter"/>
    /// then records the unit so the CHARACTER channel (<c>AsImage</c>/<c>FromImage</c>) takes the ceiling — the same
    /// ceiling <see cref="BitLayout.Characters"/> gives every other bit width — while the BIT channel
    /// (<c>AsBits</c>/<c>FromBits</c>, §13.18.29.4 GR1b's as-if PICTURE 1(m)) uses the positions directly.</para></summary>
    public static OdoGroupPlace WrapGroup(Place inner, Place depending, DataItem group, DataItem table)
    {
        var g = GeometryOf(group, table);
        return new OdoGroupPlace(inner, depending, g.FixedUnits, g.ElemUnits, g.MinOccurs, g.MaxOccurs,
            IsWithin(depending.Item, group), g.PositionsPerCharacter, g.TrailingUnits);
    }

    /// <summary>⛔ THE ONE GEOMETRY OF AN OCCURS DEPENDING TABLE IN THE GROUP THAT HOLDS IT AS ITS TRAILING STORAGE
    /// (§13.18.38.3 SR22; kb/Work PB3030, PB546): the units before the table, one occurrence's units, the bounds, and
    /// the unit. Asked by <see cref="WrapGroup"/> for a group OPERAND and by the variable-length group codec
    /// (<c>GroupImageCodec</c>: the current image, the carrier's fixed run, the receiving splice and the record layout)
    /// for a group MEMBER, so the operand and the member cannot measure one table two ways — they did: the codec took
    /// one occurrence's <see cref="DataItem.ImageWidth"/>, a national element's POSITIONS, where the image holds its
    /// BYTES, so a variable-length group's current image carried two bytes too many per unused national occurrence.
    /// <para>The extent is counted in the group's own position unit, BITS when the subtree holds a USAGE BIT leaf (see
    /// <see cref="WrapGroup"/>); <see cref="OdoGeometry.PositionsPerCharacter"/> records it.</para></summary>
    public static OdoGeometry GeometryOf(DataItem group, DataItem table)
    {
        // Bit-granular whenever the subtree holds a USAGE BIT leaf — not merely when the group is GROUP-USAGE BIT:
        // an ordinary alphanumeric group with a trailing bit ODO table has exactly the same sub-byte arithmetic.
        // The prefix is the table's own placement offset (never `total − elem × max`, which would charge rule 4's
        // trailing filler to the prefix); -1 means an unmodelled overlay chain, and the character path stands in.
        bool bitUnits = group.HasBitDescendant && BitLayout.StartBitOf(group, table) >= 0;
        int max = table.Occurs ?? 1;                          // the allocated capacity = integer-2 (§8.5.1.8)
        // integer-1 — the LOWER bound §13.18.38.4 GR7 requires the control value to fall within. Carried so the
        // extent computation can raise EC-BOUND-ODO below it; the runtime floor used to be hardcoded 0, which
        // made a below-minimum DEPENDING value clamp silently instead of setting the condition.
        int min = table.OccursSpec?.Min ?? 0;
        // per-occurrence STRIDE (§13.18.1.4 GR2 for ALIGNED). ⛔ In the SAME UNIT as the total it is subtracted
        // from and the image it slices: bits for a bit-bearing subtree, otherwise the PHYSICAL (byte) basis the
        // record codec lays out — never ImageWidth's character positions, which count a national leaf once where
        // the image counts it twice (kb/Work PB943: the prefix came out as the difference, so a zero-occurrence
        // national table sent half its maximum image and every count below the maximum sent too much).
        int elem = bitUnits ? BitLayout.StrideBits(table) : Model.RecordLayout.PhysicalOccurrenceWidth(table);
        int fixedUnits = bitUnits
            ? BitLayout.StartBitOf(group, table)
            : Model.RecordLayout.PhysicalWidth(group) - elem * max;            // SR22 — the variable tail is trailing
        return new OdoGeometry(fixedUnits, elem, min, max, bitUnits ? BitLayout.BitsPerCharacter : 1,
            bitUnits ? BitLayout.TrailingFillerBits(table) : 0);
    }

    /// <summary>The member of <paramref name="group"/> whose image holds <paramref name="table"/> as its trailing
    /// storage: the outermost image-capable item on the table's chain below <paramref name="group"/> (the table itself
    /// when it is a direct member). The table's geometry is measured in THAT member's unit (<see cref="GeometryOf"/>),
    /// never the whole group's: a USAGE BIT leaf elsewhere in a variable-length group would count it in bits.</summary>
    public static DataItem ImageHolder(DataItem group, DataItem table)
    {
        var holder = table;
        for (var a = table.Parent; a is not null && !ReferenceEquals(a, group); a = a.Parent)
            if (a.IsImageCapable) holder = a;
        return holder;
    }

    /// <summary>⛔ THE ONE <see cref="OdoTail"/> OF A TABLE OF FIXED-IMAGE ELEMENTS (kb/Work PB3030): <see cref="GeometryOf"/>
    /// over the <paramref name="holder"/> (<see cref="ImageHolder"/>), as the runtime states it — one occurrence's
    /// width and the maximum count in the holder's unit, and for a table counted in BITS the bits of fixed material
    /// sharing its first character and the unit (§8.5.1.6.3: a bit occurrence may share a byte, so the table's
    /// characters are the ceiling of its bits). Read by the variable-length group codec (<c>GroupImageCodec</c>: the
    /// member's current image, the receiving splice and the record layout) and by a cell-backed group's window
    /// (<c>VarGroupWindow</c>), so a declared group and a cell cannot measure one table two ways.
    /// <para>⛔ A HOLDER THAT CONTINUES A §8.5.1.6.3 RUN SHARES THE RUN'S CHARACTERS (kb/Work PB2921 review R1-1): "an
    /// elementary bit data item immediately following an elementary bit data item or bit group item of the same level"
    /// is placed "at the next bit position in storage", so the run is ONE slice of the group's image (the leader's
    /// field) and the holder starts <see cref="BitLayout.BitsBeforeInRun"/> bits into it. Those bits are fixed material
    /// sharing the table's first character exactly as the holder's own leading bits are, so they join
    /// <see cref="OdoTail.Lead"/>. Measured from the holder alone, the table of <c>05 B PIC 1 USAGE BIT. 05 TB PIC 1
    /// USAGE BIT OCCURS 1 TO 16 DEPENDING ON N.</c> was two characters wide inside a three-character run.</para>
    /// <para>⛔ THE LAST OCCURRENCE'S TRAILING FILLER TRAVELS WITH IT (kb/Work PB3167 review R1-1): a table whose
    /// occurrences stride whole bytes but begins inside a character ends <see cref="OdoGeometry.TrailingUnits"/> short of
    /// its stride count, a character earlier. Without <see cref="OdoTail.Trailing"/> the tail of <c>05 ABA PIC 1(2) USAGE
    /// BIT. 05 ASUB GROUP-USAGE BIT OCCURS 0 TO 3 DEPENDING ON N. 10 ASA PIC 1 USAGE BIT. 10 ATA PIC 1(5) USAGE BIT
    /// ALIGNED.</c> was 7 characters inside a 6-character member: the member's image at N = 0 lost its first character
    /// and a receiving splice started one character early.</para></summary>
    public static OdoTail FixedImageTail(DataItem holder, DataItem table)
    {
        var g = GeometryOf(holder, table);
        if (!g.InBits) return new OdoTail(g.ElemUnits, g.MaxOccurs);
        int lead = BitLayout.BitsBeforeInRun(holder) + g.FixedUnits;
        return new OdoTail(g.ElemUnits, g.MaxOccurs, 0, lead % g.PositionsPerCharacter, g.PositionsPerCharacter,
            g.TrailingUnits);
    }

    /// <summary>The SEARCH / SEARCH ALL depending item for an OCCURS DEPENDING table (ISO §14.9.37.4 GR4/GR9 →
    /// §13.18.38 GR7): the <see cref="Place"/> of data-name-1, whose CURRENT count bounds the scan (the backend
    /// renders <c>CobolTable.Occ(place)</c>, storage-form agnostic). <see langword="null"/> for a fixed table (the
    /// caller uses the compile-time maximum) or a DYNAMIC table (a runtime <c>Capacity</c>, rendered from the table
    /// path — mutually exclusive with the Format-2 DEPENDING form). data-name-1 is resolved post-build (SR20).</summary>
    public static Place? SearchDepending(DataItem table, ReferenceResolver refs) =>
        table.OccursSpec is { Depending: { } dep } ? refs.ResolveItem(dep) : null;
}

