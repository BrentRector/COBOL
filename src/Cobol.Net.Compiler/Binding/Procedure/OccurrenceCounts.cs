// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>⛔ THE ONE bind-time reading of a table's CURRENT number of occurrences as an <see cref="AllCount"/> — ISO
/// §14.6.9.1's three-way equivalence: "If another operand of the operation is a fixed capacity table, that table shall
/// be treated as a special case of a dynamic capacity table that has a current capacity equal to the fixed number of
/// elements. If another operand is an occurs-depending table, that table shall be treated as a special case of a
/// dynamic capacity table that has a current capacity equal to the value of the corresponding DEPENDING operand."
/// Two statements loop a table over it: INITIALIZE (§14.9.20.4 GR8 / GR10 — whose §13.18.38.4 GR8b maximum is its
/// own arm, layered over this) and the §14.6.9.2 element moves of a variable-length group MOVE (kb/Work PB1144).
/// It was INITIALIZE's private pair of helpers until the second statement needed the same three counts.</summary>
internal static class OccurrenceCounts
{
    /// <summary>The current count of <paramref name="table"/> whose structural path is <paramref name="tablePath"/>:
    /// a dynamic-capacity table's current capacity, an occurs-depending table's data-name-1 value (clamped by
    /// <see cref="AllCount.Odo"/>'s renderer, §13.18.38.4 GR7), a fixed table's occurrence count. Null when the count
    /// cannot be modelled — a dynamic table with no path, or an unresolvable data-name-1.</summary>
    public static AllCount? Current(DataItem table, AccessPath? tablePath, ReferenceResolver refs) =>
        table.IsDynamicTable ? (tablePath is null ? null : Capacity(table, tablePath))
        : table.OccursSpec is { DependingName: not null, Depending: { } dep } odo
            ? (refs.ResolveItem(dep) is { } depPlace
                ? new AllCount.Odo(depPlace, odo.Min, table.Occurs ?? odo.Max) : null)
        : table.Occurs is { } n ? new AllCount.Fixed(n)
        : null;

    /// <summary>A dynamic-capacity table's current-capacity count (ISO §13.18.38.4 GR15 — the register is minted for
    /// every Format-4 table whether or not CAPACITY IN names it). The path is the table's own, so a nested table's
    /// carries the OUTER index variables and the capacity is the right occurrence's.</summary>
    public static AllCount? Capacity(DataItem table, AccessPath tablePath) =>
        table.OccursSpec?.CapacityRegister is { } reg
            ? new AllCount.Capacity(new CapacityRegisterPlace(tablePath, reg)) : null;
}
