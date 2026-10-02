// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE ONE CONSERVATIVE "MAY THESE TWO REFERENCES OCCUPY THE SAME STORAGE?" QUESTION (ISO §14.6.10 — overlapping
/// operands; kb/Work PB1907). A verb whose rules call a result undefined when two operands "occupy the same storage
/// area" (INSPECT §14.9.22.4 GR13/GR18/GR21, Annex A.2 item 21) still has to DO something when they do, and what it
/// does is a documented implementor choice (docs/CONFORMANCE.md "Implementor behavior in Annex A.2 undefined
/// cases"). The emitter that implements the choice must know whether the choice can matter, so that the statements
/// that cannot overlap keep their single-snapshot code unchanged.
/// <para>The answer is ALWAYS A SAFE OVER-APPROXIMATION: <see langword="true"/> means "cannot prove disjoint", never
/// "does overlap". A caller whose overlap behavior equals its disjoint behavior when the storage is in fact disjoint
/// (INSPECT's re-read of a counter-updated image is exactly that) therefore loses only speed on a false positive.
/// It is decided from the DECLARATIONS alone (no offsets, no subscript values), by the three facts that make two
/// items share storage: one contains the other (self, ancestor, descendant — a table element and the table, two
/// subscripts of the same item included); both reach the same storage through REDEFINES (explicit, or the file
/// section's implicit redefinition of one record area); or a reference that no declaration pins to its own
/// storage — a LINKAGE item, which the caller's actual arguments may alias, or a BASED item, which a pointer
/// places anywhere.</para>
/// </summary>
public static class StorageOverlap
{
    /// <summary>True unless the two references are provably disjoint (see the type's contract). Decorations
    /// (reference modification, occurs-depending extent, image views) never change WHERE the storage is, so each
    /// side is judged by the storage underneath; a level-66 RENAMES alias is judged by every leaf it spans.</summary>
    public static bool MayShareStorage(Place a, Place b)
    {
        foreach (var x in StorageItems(a))
            foreach (var y in StorageItems(b))
                if (MayShareStorage(x, y)) return true;
        return false;
    }

    private static IEnumerable<DataItem> StorageItems(Place place)
    {
        var core = place.Undecorated;
        if (core is not RenamesPlace renames)
        {
            yield return core.Item;
            yield break;
        }
        foreach (var leaf in renames.Leaves)
            foreach (var item in StorageItems(leaf))
                yield return item;
    }

    private static bool MayShareStorage(DataItem a, DataItem b)
    {
        var pathA = PathFromRoot(a);
        var pathB = PathFromRoot(b);
        if (IsUnpinned(pathA[0]) || IsUnpinned(pathB[0])) return true;

        // Walk the two root-to-item paths while they name the same STORAGE (an item and whatever redefines it name
        // one storage area). `crossed` records that the shared prefix was reached through a redefinition: below it
        // two different children may sit anywhere over one another, which the declarations alone cannot rule out.
        int i = 0;
        bool crossed = false;
        while (i < pathA.Count && i < pathB.Count && ReferenceEquals(Base(pathA[i]), Base(pathB[i])))
        {
            crossed |= !ReferenceEquals(pathA[i], pathB[i]);
            i++;
        }
        // One path ends inside the other: the item contains (or is) the other — they overlap.
        if (i == pathA.Count || i == pathB.Count) return true;
        // They parted at siblings (distinct storage under one parent) — disjoint unless the parent was a redefinition.
        return crossed;
    }

    /// <summary>The item's chain from its 01/77 root down to itself.</summary>
    private static List<DataItem> PathFromRoot(DataItem item)
    {
        var path = new List<DataItem>();
        for (var x = item; x is not null; x = x.Parent) path.Add(x);
        path.Reverse();
        return path;
    }

    /// <summary>The item whose storage this one occupies: itself, or the original at the end of its REDEFINES chain
    /// (SR11 — the target may itself be a redefiner; the file section's implicit redefinitions are in the chain too).</summary>
    private static DataItem Base(DataItem item)
    {
        while (item.RedefinesTarget is { } target) item = target;
        return item;
    }

    /// <summary>A root whose storage no declaration pins: a LINKAGE item (the caller's arguments may alias) or a
    /// BASED item (a pointer places it).</summary>
    private static bool IsUnpinned(DataItem root) => root.IsBased || root.Section == EntrySection.Linkage;
}
