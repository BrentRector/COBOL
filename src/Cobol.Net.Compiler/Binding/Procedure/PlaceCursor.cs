// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>⛔ THE ONE bind-time cursor that extends an ALREADY-RESOLVED group <see cref="Place"/> member by member
/// down to its subordinate items — so a qualified / subscripted group reference expands from the access path it
/// was resolved to, never by re-resolving each member (which would lose the group's own subscripts).
/// <see cref="Child"/> returns null for a storage form not wired here (the caller decides what that means for its
/// statement).
/// <para>Two statements walk a group this way: INITIALIZE (§14.9.20.4 GR5 — "a series of implicit MOVE or SET
/// statements, each of which has an elementary data item as its receiving operand") and the comparison of two
/// strongly-typed group items (§8.8.4.2.12 — "each elementary item of the first operand is compared with the
/// corresponding elementary item of the second operand"). It was INITIALIZE's private record until kb/Work PB1469
/// needed the same walk; a second copy of the three storage-form arms would have been two readings of one layout
/// law, which is how the BIT unit went missing from three window builders at once (kb/Work PB203).</para></summary>
internal abstract record PlaceCursor(DataItem Item)
{
    /// <summary>The cursor at <paramref name="child"/>, an immediate subordinate of <see cref="Item"/>, or null for
    /// a storage form this cursor cannot reach.</summary>
    public abstract PlaceCursor? Child(DataItem child);

    /// <summary>The cursor at one occurrence of <see cref="Item"/> (an OCCURS table), <paramref name="oneBasedIndex"/>
    /// being the D10 transitional index text — a loop variable or an integer literal.</summary>
    public abstract PlaceCursor Indexed(string oneBasedIndex);

    /// <summary>The <see cref="Place"/> the cursor denotes.</summary>
    public abstract Place ToPlace();

    /// <summary>The cursor's STRUCTURAL access path, or null for a storage form that has none — the Tier-B
    /// window cursor, whose members are character windows over one string backing. It is what a
    /// dynamic-capacity child needs (kb/Work PB393): its CAPACITY register and its per-occurrence element
    /// path are both built from the table's own path, and taking it from the cursor rather than from
    /// <c>ReferenceResolver.BuildTablePath</c> is what keeps a SUBSCRIPTED or qualified group's indices on the
    /// path.</summary>
    public virtual AccessPath? StoragePath => null;

    /// <summary>The cursor at one occurrence of the dynamic-capacity table at this cursor, entered through its
    /// direction-specific accessor (§8.5.1.9.2 / §8.5.1.9.3) — a typed element of the table's
    /// <c>CobolDynTable</c> on its <see cref="StoragePath"/>, or, in a cell-backed area, the occurrence's element cell
    /// (<see cref="ViewCursor"/>; kb/Work PB1042). Null when the position has no table path.</summary>
    public virtual PlaceCursor? DynamicElement(string oneBasedIndex) =>
        StoragePath is { } table ? new DynElementCursor(table.Add(new DynTableSegment(oneBasedIndex)), Item) : null;

    /// <summary>The cursor over a resolved group place, or null for a storage form no cursor is built over.
    /// ⛔ THE SWITCH IS OVER THE STORAGE FORM, SO IT ASKS <see cref="Place.Undecorated"/> (kb/Work PB393): an
    /// <c>OdoGroupPlace</c> answers §13.18.38.4 GR8 about the group's EXTENT and a bit/national group's image
    /// view about how it is READ as one item — neither changes where its members live.</summary>
    public static PlaceCursor? Over(Place place) => place.Undecorated switch
    {
        MemberPlace mp => new MemberCursor(mp.Path, mp.MemberItem),
        // The scope's cell path rides along so a pointer-class member inside the class reaches the area's managed
        // slots (kb/Work PB231) and a variable-length component its slot (kb/Work PB1042).
        RedefViewPlace rv => ViewCursor.Over(rv),
        DynTablePlace dp => new DynElementCursor(dp.Path, dp.Item),
        _ => null,
    };
}

/// <summary>A plain member-access cursor, mirroring <c>ReferenceResolver.AccessPath</c>: <c>CsName</c> segments
/// chained with <c>.</c>, each OCCURS level routed through the ref-returning <c>CobolTable.At</c> (benign
/// subscripting, ISO §8.4.2.3.4 GR2). Entering a Tier-B (string-canonical) REDEFINES class — whose ONE stored
/// string backing lives in the containing struct (COBOLNET_DESIGN §4.2) — switches to a <see cref="ViewCursor"/>;
/// an unwired Tier-C / Rejected class yields null.</summary>
internal sealed record MemberCursor(AccessPath Path, DataItem Item) : PlaceCursor(Item)
{
    /// <inheritdoc/>
    public override PlaceCursor? Child(DataItem child)
    {
        if (child.Class is { } cls)
        {
            if (cls.Tier == RedefinesTier.StringCanonical && child.IsCanonical)
                return new ViewCursor(Path.Add(new MemberSegment(cls.BackingCsName)),
                    child.ClassOffset.ToString(), child.ClassOffset, child, "",
                    Cell: ReferenceResolver.BuildCellPath(cls));
            if (cls.Tier != RedefinesTier.Alias || !child.IsCanonical) return null;
        }
        return new MemberCursor(Path.Add(new MemberSegment(child.CsName)), child);
    }

    /// <inheritdoc/>
    public override PlaceCursor Indexed(string oneBasedIndex) =>
        this with { Path = Path.Add(new FixedTableSegment(oneBasedIndex)) };

    /// <inheritdoc/>
    public override Place ToPlace() => new MemberPlace(Path, Item);

    /// <inheritdoc/>
    public override AccessPath? StoragePath => Path;
}

/// <summary>A cursor at one occurrence of an OCCURS DYNAMIC table (data-model D9): entered at the element level
/// with the direction-specific accessor path already applied. Children append their <c>.CsName</c>; a nested
/// FIXED OCCURS below the element wraps the path in <c>CobolTable.At</c>. Yields a <see cref="DynTablePlace"/>, so a
/// store writes through <c>RefReceiving</c> and a read through <c>RefSending</c>. A REDEFINES view under the
/// element is not wired here (null).</summary>
internal sealed record DynElementCursor(AccessPath Path, DataItem Item) : PlaceCursor(Item)
{
    /// <inheritdoc/>
    public override PlaceCursor? Child(DataItem child) =>
        child.Class is null
            ? new DynElementCursor(Path.Add(new MemberSegment(child.CsName)), child)
            : null;

    /// <inheritdoc/>
    public override PlaceCursor Indexed(string oneBasedIndex) =>
        this with { Path = Path.Add(new FixedTableSegment(oneBasedIndex)) };

    /// <inheritdoc/>
    public override Place ToPlace() => new DynTablePlace(Path, Item);

    /// <inheritdoc/>
    public override AccessPath? StoragePath => Path;
}

/// <summary>A cursor inside a Tier-B REDEFINES class: every member is a (offset, width) character window over
/// the class's ONE string backing. The window offset = the entry place's offset expression + (this item's
/// in-class offset − the entry item's) + Σ (index − 1) × per-occurrence storage width for each OCCURS level crossed
/// (ISO §13.18.44 — a redefined table lays its occurrences end-to-end in the one backing; the same arithmetic
/// as <c>ReferenceResolver.WindowScopeOf</c>).</summary>
/// <param name="Cell">The backing <c>StorageCell</c> path of the cursor's SCOPE, for the three cell-backed surfaces
/// — what a pointer-class member's managed slot and a variable-length component's slot address (kb/Work PB231,
/// PB1042; <see cref="SlotWindow"/>, <see cref="CellComponents"/>). Null for a plain REDEFINES class, which cannot
/// hold such a member.</param>
internal sealed record ViewCursor(
    AccessPath Backing, string BaseExpr, int BaseOffset, DataItem Item, string OccursTerms,
    string OccursBitTerms = "", AccessPath? Cell = null) : PlaceCursor(Item)
{
    /// <summary>The component-ordinal displacement of the cursor's position (kb/Work PB1042): the entry place's
    /// run-time ordinal less its item's static one, plus <c>(index − 1) ×</c> the components per occurrence of each
    /// fixed level crossed since — the ordinal twin of <see cref="OccursTerms"/>.</summary>
    public string OrdinalTerms { get; init; } = "";

    /// <summary>The dynamic-capacity table whose ELEMENT CELL is the cursor's scope (kb/Work PB1042), or null for
    /// the class cell's. The table item itself, positioned at one occurrence, is its element at offset zero.</summary>
    public DataItem? ScopeTable { get; init; }

    /// <summary>The cursor over the window <paramref name="rv"/> denotes — re-anchored on its scope's cell, and
    /// positioned at the element when the window is one occurrence of a dynamic-capacity table.</summary>
    public static ViewCursor Over(RedefViewPlace rv)
    {
        var item = rv.ViewItem;
        bool element = item.IsDynamicTable;
        return new ViewCursor(rv.Backing, rv.OffsetExpr, element ? 0 : item.ClassOffset, item, "", Cell: rv.Cell)
        {
            ScopeTable = element ? item : null,
            OrdinalTerms = rv.DynOrdinal is { } o ? $" + ({o}) - {(element ? 0 : item.ClassDynOrdinal)}" : "",
        };
    }

    /// <inheritdoc/>
    public override PlaceCursor? Child(DataItem child) =>
        ReferenceEquals(child.Class, Item.Class) ? this with { Item = child } : null;

    // The byte stride and its BIT twin, accumulated together so a USAGE BIT member's occurrences are displaced
    // by §8.5.1.6.3's "next bit position" rather than by its byte ceiling (kb/Work PB203). The byte stride is the
    // level's STORAGE extent (ByteWidth), never its character-position count: a NATIONAL element occupies two bytes
    // per position (§13.18.60.4 GR8 / D-N1, kb/Work PB231), the stride the resolver's window walk uses.
    /// <inheritdoc/>
    public override PlaceCursor Indexed(string oneBasedIndex) =>
        Item.IsDynamicTable && Cell is { } cell
            // ONE OCCURRENCE OF A CELL-BACKED dynamic-capacity table (kb/Work PB1042): its element cell is a scope
            // of its own — its members' offsets and ordinals start at zero there.
            ? this with
            {
                Backing = ElementCell(cell, oneBasedIndex).Add(new MemberSegment(nameof(CobolNet.Runtime.StorageCell.Ref))),
                Cell = ElementCell(cell, oneBasedIndex),
                BaseExpr = "0", BaseOffset = 0, OccursTerms = "", OccursBitTerms = "", OrdinalTerms = "",
                ScopeTable = Item,
            }
            : this with
            {
                OccursTerms = $"{OccursTerms} + ({oneBasedIndex} - 1) * {Item.ByteWidth}",
                OccursBitTerms = $"{OccursBitTerms} + ({oneBasedIndex} - 1) * {BitLayout.StrideBits(Item)}",   // §13.18.1.4 GR2
                OrdinalTerms = Cell is null ? OrdinalTerms
                    : $"{OrdinalTerms} + ({oneBasedIndex} - 1) * {CellComponents.PerOccurrence(Item)}",
            };

    /// <summary>The cell-backed dynamic-capacity table at the cursor (kb/Work PB1042) — what its CAPACITY register,
    /// its per-occurrence INITIALIZE loop and its element cursor are built from; null for any other position.</summary>
    public override AccessPath? StoragePath =>
        Item.IsDynamicTable && !ReferenceEquals(Item, ScopeTable) && Cell is { } cell
            ? cell.Add(CellTableSegment.Of(Item, Ordinal(Item)))
            : null;

    /// <inheritdoc/>
    public override PlaceCursor? DynamicElement(string oneBasedIndex) => StoragePath is null ? null : Indexed(oneBasedIndex);

    /// <summary>The cursor at the cell-backed dynamic-capacity table <paramref name="table"/> referenced whole (kb/Work
    /// PB1042) — a level the whole-table INITIALIZE enters only through <see cref="DynamicElement"/>, so the class
    /// backing it starts from is never read. Null outside a cell-backed class.</summary>
    public static ViewCursor? AtCellTable(DataItem table) =>
        ReferenceResolver.BuildCellPath(table.Class) is { } cell
            ? new ViewCursor(cell, "0", 0, table, "", Cell: cell) { OrdinalTerms = ReferenceResolver.CellOrdinalBase(table.Class!) }
            : null;

    private AccessPath ElementCell(AccessPath cell, string oneBasedIndex) =>
        cell.Add(CellTableSegment.Of(Item, Ordinal(Item))).Add(new DynTableSegment(oneBasedIndex));

    /// <summary>The component ordinal of <paramref name="item"/> at the cursor's position.</summary>
    private string Ordinal(DataItem item) =>
        $"{(ReferenceEquals(item, ScopeTable) ? 0 : item.ClassDynOrdinal)}{OrdinalTerms}";

    /// <inheritdoc/>
    public override Place ToPlace()
    {
        int delta = (ReferenceEquals(Item, ScopeTable) ? 0 : Item.ClassOffset) - BaseOffset;
        // ⛔ THROUGH THE ONE WINDOW BUILDER (kb/Work PB203). `BaseExpr - BaseOffset` is the entry place's
        // RUNTIME displacement with its static in-class offset removed — exactly what the builder needs,
        // because a bit member's own position is already carried in BITS by DataItem.ClassBitOffset and
        // re-deriving it from a byte expression would round a sub-byte member down to its containing byte.
        return RedefViewPlace.For(Backing, Item,
            $"{BaseExpr}{(delta != 0 ? $" + {delta}" : "")}{OccursTerms}",
            BaseExpr == BaseOffset.ToString() ? null : $"{BaseExpr} - {BaseOffset}", OccursBitTerms, Cell,
            Cell is null ? null : Ordinal(Item));
    }
}
