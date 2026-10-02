// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>⛔ THE COMPILE-TIME STORAGE EXTENT OF AN OPERAND — which storage area it lies in, where it starts and how
/// many bytes it spans — and the ONE answer to "do these two operands STATICALLY overlap?" (kb/Work PB1907;
/// docs/CONFORMANCE.md §3 D-OVL1).
/// <para>ISO §14.6.10 1): "if the operands … are not described by the same data description entry … and the
/// storage areas overlap, the result of the statement is undefined" (Annex A.2 item 36). The standard leaves the
/// result open, so WiseOwl COBOL documents what it does, and a rule that depends on overlap needs the overlap
/// to be a FACT the binder can read, not a guess the emitter makes. This is that fact. It is known only where the
/// compiler can prove it: both operands live in ONE storage area (a REDEFINES class, §13.18.44, or one record) and
/// every offset and length is a constant. An operand under OCCURS, a dynamic or variable-length item, a USAGE
/// NATIONAL or BIT operand, a reference modification with a run-time bound, or a RENAMES span answers
/// <see langword="null"/>, and a null is "not provably overlapping" — never "disjoint".</para>
/// <para>The geometry is <see cref="RecordLayout.OffsetInRecord"/>'s — the ONE offset walk the SORT/MERGE key
/// windows already ride — so the answer cannot disagree with the emitted record layout.</para></summary>
/// <param name="Area">The storage area's identity: the REDEFINES class's canonical item when the record belongs to
/// one (a secondary 01 that redefines the first shares its area), otherwise the 01 record itself.</param>
/// <param name="Start">The 0-based byte offset of the operand in the area.</param>
/// <param name="Length">The operand's extent in bytes.</param>
public readonly record struct StorageExtent(DataItem Area, int Start, int Length)
{
    /// <summary>True when the two extents lie in ONE area and share at least one byte.</summary>
    public bool Overlaps(StorageExtent other) =>
        ReferenceEquals(Area, other.Area) && Start < other.Start + other.Length && other.Start < Start + Length;

    /// <summary>The static extent of <paramref name="place"/>, or null when it is not provable (see the type's
    /// summary). A reference modification narrows its inner extent by its literal bounds (§8.4.3.3.4 GR5);
    /// the image decorators keep their inner item's extent.</summary>
    public static StorageExtent? Of(Place place) => place switch
    {
        MemberPlace m => OfItem(m.MemberItem),
        RedefViewPlace { Coding: null } v => OfItem(v.ViewItem),
        NumericImagePlace n => Of(n.Inner),
        GroupImagePlace g => Of(g.Inner),
        RefModPlace r => OfRefMod(r),
        _ => null,
    };

    private static StorageExtent? OfRefMod(RefModPlace r)
    {
        // Positions are CHARACTER positions (§8.4.3.3.4 GR5a); they are bytes only for a DISPLAY-usage operand,
        // which is the only kind whose extent this answers (USAGE NATIONAL / BIT operands answer null above).
        if (Of(r.Inner) is not { } inner || !int.TryParse(r.Start, out int start) || start < 1) return null;
        if (r.StaticLength(inner.Length) is not { } len || len < 1 || start - 1 + len > inner.Length) return null;
        return new StorageExtent(inner.Area, inner.Start + start - 1, len);
    }

    private static StorageExtent? OfItem(DataItem item)
    {
        if (item.IsDynamicLength || item.IsAnyLength || VariableLengthCompatibility.IsVariableLength(item)) return null;
        if (item.Pic is { Usage: Usage.National or Usage.Bit } || item.HasBitDescendant) return null;
        DataItem root = item;
        while (root.Parent is { } parent) root = parent;
        DataItem area = root.Class?.Canonical ?? root;
        return RecordLayout.OffsetInRecord(area, item) is { } off
            ? new StorageExtent(area, off, RecordLayout.PhysicalOccurrenceWidth(item))
            : null;
    }
}
