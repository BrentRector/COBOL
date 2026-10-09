// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The OCCURS DEPENDING table a variable-length group holds as its TRAILING storage (kb/Work PB244, PB2497): ISO
/// §13.18.38.3 SR22 lets the table's subject be followed within its record only by entries subordinate to it, so the
/// table is the last thing of every group that contains it. ONE geometry serves both readers of it:
/// <list type="bullet">
/// <item>a CELL-BACKED group's window, which reserves the table at its MAXIMUM (<see cref="Max"/> elements of
/// <see cref="Elem"/> positions) — the cell's group helpers (<see cref="StorageCell.VarGroupAt"/> and its siblings)
/// take it beside their component layout and cut the run to the operand's count;</item>
/// <item>the CONTIGUOUS layout of a record (<see cref="CobolContiguousLayout"/>, a declared group's generated
/// <c>__contiguous</c> and a cell's alike), which must know the table to split a record read back: a table of
/// elements of a FIXED image (<see cref="Comps"/> zero) is the layout's last component, one element per unit, so the
/// record's own length says how many occurrences it holds; a table of VARIABLE-LENGTH elements (<see cref="Comps"/>
/// above zero) contributes <see cref="Comps"/> components per occurrence, and the record's extent table says how many
/// occurrences it holds by how many components it describes (<see cref="CountFrom"/>).</item>
/// </list>
/// <c>default</c> (<see cref="Present"/> false) is a group that holds no such table.
/// </summary>
/// <param name="Elem">One occurrence's width in character positions OF THE GROUP'S FIXED RUN (a dynamic-capacity
/// table inside an element reserves its one-element extent in the window and none in the run; a dynamic-length item
/// occupies none).</param>
/// <param name="Max">The table's maximum occurrence count (OCCURS … TO integer-2).</param>
/// <param name="Comps">How many variable-length COMPONENTS (dynamic-length items, dynamic-capacity tables) one
/// occurrence holds - zero for an element of a fixed image (kb/Work PB244). A table's components are numbered per
/// occurrence (<c>CellComponents</c>; <c>GroupImageCodec.VarParts</c>' flattening), and the table is the LAST thing of
/// the group, so they are its last <c>Max * Comps</c> components and the occurrences beyond the count contribute none
/// of them.</param>
public readonly record struct OdoTail(int Elem, int Max, int Comps = 0)
{
    /// <summary>True when the group holds the table.</summary>
    public bool Present => Max > 0;

    /// <summary>The table's width at its maximum — what the group's window reserves.</summary>
    public int Width => Elem * Max;

    /// <summary>How many trailing positions of the group's MAXIMUM image lie beyond <paramref name="count"/>
    /// occurrences — the current extent of the group is its maximum image less this suffix (ISO §13.18.38.4 GR8:
    /// only the part the DEPENDING item names is used). A count outside 0..<see cref="Max"/> is clamped, as the
    /// declared group's <c>__odo</c> argument already is (<c>TableOdoExtent</c>, EC-BOUND-ODO).</summary>
    public int CutAt(int count) => Present ? (Max - Math.Clamp(count, 0, Max)) * Elem : 0;

    /// <summary>How many of the group's trailing COMPONENTS belong to the occurrences beyond <paramref name="count"/>
    /// - the components the current extent drops with them (<see cref="CutAt"/> drops their fixed run). Zero for a
    /// table whose elements hold none.</summary>
    public int CutComponents(int count) => Present ? (Max - Math.Clamp(count, 0, Max)) * Comps : 0;

    /// <summary>The occurrence count a record holds when it carries <paramref name="components"/> of the
    /// <paramref name="total"/> components the group has at the table's maximum — the inverse of
    /// <see cref="CutComponents"/> (kb/Work PB2497): every occurrence the record holds brought its <see cref="Comps"/>
    /// components, the ones beyond it none. Null when no count gives that many (the record was sent through another
    /// description) or when the elements hold no component, where the record's LENGTH says the count instead.</summary>
    public int? CountFrom(int components, int total)
    {
        if (Comps <= 0) return null;
        int beyond = total - components;
        return beyond >= 0 && beyond % Comps == 0 && beyond / Comps <= Max ? Max - beyond / Comps : null;
    }
}
