// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ <b>THE ONE "MAY THESE TWO OPERANDS OCCUPY THE SAME STORAGE?" QUESTION</b> (kb/Work PB1907). ISO §14.9.48.4 GR18
/// (Annex A.2 item 61) leaves an UNSTRING whose operands share storage undefined, and WiseOwl COBOL's documented
/// answer for that case (docs/CONFORMANCE.md D-UNS1 to D-UNS4) is GnuCOBOL's: the statement runs in place, so an
/// operand a store can reach is READ AGAIN before each receiving area. Re-reading costs a re-render of the operand,
/// so an emitter asks this predicate and pays it only where a store can reach the operand.
/// <para><b>The answer is CONSERVATIVE by construction:</b> <see langword="true"/> means "can not be shown
/// disjoint", never "does overlap". A false positive costs one redundant re-read, whose result equals the unread
/// value when the storage is in fact disjoint; a false negative would silently fall back to snapshot semantics, so
/// every storage the compiler cannot place statically is "may share":</para>
/// <list type="bullet">
/// <item>the SAME 01/77 record (any subordinate, any REDEFINES view, any subscript or reference modification: they
///   are one storage area);</item>
/// <item>two records of ONE REDEFINES class (a level-01 REDEFINES shares the area, ISO §13.18.44);</item>
/// <item>a level-66 RENAMES alias, which occupies the storage of the items it renames (§13.18.45.4 GR1/GR2);</item>
/// <item>two FILE SECTION records (the implicit redefinition of a file's records and SAME RECORD AREA, §13.18.33.4
///   GR3 / §12.4.6.4.4 GR2 — kept as "any two file records" so a new way of sharing a record area is never missed);</item>
/// <item>a LINKAGE or BASED item, whose storage is decided when the program runs (CALL BY REFERENCE, SET ADDRESS
///   OF, ALLOCATE), so it may alias anything.</item>
/// </list>
/// A synthesized temporary (<see cref="DataItem.IsCompilerTemp"/>) owns storage nothing else can name, so it shares
/// with nothing. Held by <c>UnstringLiveReadDriftTests</c>: the UNSTRING emitter asks this and no private copy.
/// </summary>
internal static class StorageSharing
{
    /// <summary>May <paramref name="a"/> and <paramref name="b"/> occupy the same storage area? (see the class
    /// summary — a conservative answer.)</summary>
    public static bool MayShare(Place a, Place b)
    {
        var ra = Roots(a.Item);
        var rb = Roots(b.Item);
        if (ra.Count == 0 || rb.Count == 0) return false;
        foreach (var x in ra)
            foreach (var y in rb)
                if (RecordsMayShare(x, y)) return true;
        return false;
    }

    /// <summary>The 01/77 records whose storage <paramref name="item"/> occupies: its own root, plus the roots of
    /// the items a level-66 alias renames (§13.18.45.4) — an alias is an owner-record sibling, not a storage child.
    /// Empty for a synthesized temporary.</summary>
    private static List<DataItem> Roots(DataItem item)
    {
        var roots = new List<DataItem>(2);
        if (item.IsCompilerTemp) return roots;
        roots.Add(RootOf(item));
        if (item.Renames is { } r)
        {
            if (r.From is { } from) roots.Add(RootOf(from));
            if (r.Thru is { } thru) roots.Add(RootOf(thru));
        }
        return roots;
    }

    private static DataItem RootOf(DataItem item)
    {
        var root = item;
        while (root.Parent is { } p) root = p;
        return root;
    }

    private static bool RecordsMayShare(DataItem x, DataItem y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x.Class is { } c && ReferenceEquals(c, y.Class)) return true;
        if (x.Section == EntrySection.File && y.Section == EntrySection.File) return true;
        return Aliasable(x) || Aliasable(y);
    }

    /// <summary>Storage whose location the compiler does not fix: a LINKAGE item (by-reference arguments, SET ADDRESS
    /// OF) or a BASED one (§13.18.5 — no storage until an address is set).</summary>
    private static bool Aliasable(DataItem root) => root.IsBased || root.Section == EntrySection.Linkage;
}
