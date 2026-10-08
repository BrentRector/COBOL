// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The OCCURS DEPENDING table a CELL-BACKED variable-length group holds as its TRAILING storage (kb/Work PB244):
/// ISO §13.18.38.3 SR22 lets the table's subject be followed within its record only by entries subordinate to it,
/// so the table is the last thing of every group that contains it, and the group's window reserves it at its
/// MAXIMUM (<see cref="Max"/> elements of <see cref="Elem"/> positions). The cell's group helpers
/// (<see cref="StorageCell.VarGroupAt"/> and its siblings) take this beside their component layout, because the
/// table is a component of the group's CONTIGUOUS layout (<see cref="CobolContiguousLayout"/> with
/// <c>OdoTail</c>) exactly as it is for a declared group's generated <c>CurrentImage</c> / <c>CurrentExtents</c>:
/// without it, the take step that splits a record would hand the table's positions to a dynamic-length item
/// that precedes it. <c>default</c> (<see cref="Present"/> false) is a group that holds no such table.
/// </summary>
/// <param name="Elem">One occurrence's width in character positions OF THE GROUP'S FIXED RUN (a dynamic-capacity
/// table inside an element reserves its one-element extent in the window and none in the run).</param>
/// <param name="Max">The table's maximum occurrence count (OCCURS … TO integer-2).</param>
/// <param name="Comps">How many variable-length COMPONENTS (dynamic-length items, dynamic-capacity tables) one
/// occurrence holds - zero for an element of a fixed image (kb/Work PB244). The cell numbers a table's components
/// per occurrence (<c>CellComponents</c>), and the table is the LAST thing of the group, so they are its last
/// <c>Max * Comps</c> components and the occurrences beyond the count contribute none of them.</param>
public readonly record struct CellOdoTail(int Elem, int Max, int Comps = 0)
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
}
