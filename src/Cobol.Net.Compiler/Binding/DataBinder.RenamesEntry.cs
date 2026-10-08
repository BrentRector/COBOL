// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// The RENAMES clause's ENTRY-level syntax rules (ISO §13.18.45.3; kb/Work PB1283): what its operands may BE (SR3, SR5,
/// SR6), where its window may lie (SR11) and where the entry may stand (SR2). They were all absent because
/// data-name-2 / data-name-3 were found by name through <see cref="SubtreeCandidates"/> — whose candidate set includes
/// the record ITSELF — and then used without being asked what they are. SR4 (operands name items of THIS record), SR7
/// (not subscripted), SR8 (what the range may hold) and SR10 (whole bytes) already had their screens beside the
/// resolution in <see cref="ResolveRedefines"/>.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>§13.18.45.3 SR5, SR3 and SR6 over one resolved operand, reported against the alias
    /// <paramref name="ren"/>; false when any was violated (the alias is then left unbound, like any refused
    /// range). <paramref name="owner"/> is the record — or the group a TYPEDEF's RENAMES was cloned onto — whose scope
    /// resolved the operand; the OCCURS walk climbs past it to the true record root, because an alias inside a table is
    /// itself subject to that table's OCCURS (data-name-1 is named by SR3 as well).
    /// <para>The OCCURS test is the operand and its ANCESTORS only — never the items BETWEEN the endpoints: a table may
    /// lie inside the window (PB986 tiles it), and testing the span would re-create that over-rejection.</para></summary>
    private bool ScreenRenamesOperand(DataItem owner, DataItem ren, DataItem operand, string role)
    {
        string alias = ren.CobolName ?? "FILLER";
        string op = operand.CobolName ?? "FILLER";
        bool ok = true;

        // SR5 — "Neither data-name-2 nor data-name-3 shall refer to an entry that is described with level-number 1, 66,
        // 77, or 88." Levels 66 and 88 are off the storage tree and already unreachable (SR4); a level-1 or level-77
        // entry is on it, and the owner itself (a record, or the group a type was applied to) is such an entry.
        if (operand.Level is 1 or 77 || ReferenceEquals(operand, owner))
        {
            Edition.Error(DiagnosticCatalog.RenamesEntryRule,
                $"'{alias}' RENAMES: {role} '{op}' refers to an entry described with level-number "
                + $"{(operand.Level is 77 ? "77" : "1")} (ISO §13.18.45.3 SR5: \"Neither data-name-2 nor data-name-3 "
                + "shall refer to an entry that is described with level-number 1, 66, 77, or 88.\")");
            ok = false;
        }

        // SR3 — "Data-name-1, data-name-2 and data-name-3 shall not be subject to any OCCURS clauses": the operand's
        // own OCCURS or any ancestor's.
        for (var n = operand; n is not null; n = n.Parent)
            if (n.IsTable)
            {
                Edition.Error(DiagnosticCatalog.RenamesEntryRule,
                    $"'{alias}' RENAMES: {role} '{op}' is subject to an OCCURS clause"
                    + (ReferenceEquals(n, operand) ? "" : $" (of '{n.CobolName ?? "FILLER"}')")
                    + " (ISO §13.18.45.3 SR3: \"Data-name-1, data-name-2 and data-name-3 shall not be subject to any "
                    + "OCCURS clauses.\")");
                ok = false;
                break;
            }

        // SR6 — no operand within a CONSTANT RECORD: the alias would be a modifiable name for constant content.
        if (IsConstantRecordItem(operand))
        {
            Edition.Error(DiagnosticCatalog.RenamesEntryRule,
                $"'{alias}' RENAMES: {role} '{op}' is an entry within a record whose data description entry includes "
                + "the CONSTANT RECORD clause (ISO §13.18.45.3 SR6)");
            ok = false;
        }
        return ok;
    }

    /// <summary>§13.18.45.3 SR11 — "The beginning of the storage area described by data-name-3 shall not precede the
    /// beginning of the storage area described by data-name-2. The end of the storage area described by data-name-3
    /// shall follow the end of the storage area described by data-name-2. NOTE Data-name-3, therefore, cannot be
    /// subordinate to data-name-2." Both ends are asked (the screen used to test only that data-name-3 did not END before
    /// data-name-2 BEGAN, which is a strict subset). Extents are in BITS for a record that holds a bit item (§8.5.1.6.3
    /// packs same-level bit items into shared bytes), else in bytes — the measure the tiler below uses.</summary>
    private bool ScreenRenamesWindowOrder(DataItem ren, RenamesInfo info, Func<DataItem, (long Start, long End)> extentOf)
    {
        var (start2, end2) = extentOf(info.From!);
        var (start3, end3) = extentOf(info.Thru!);
        if (start3 >= start2 && end3 > end2) return true;
        Edition.Error(DiagnosticCatalog.RenamesEntryRule,
            $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName} THRU {info.ThruName}: "
            + (start3 < start2
                ? "the beginning of data-name-3 precedes the beginning of data-name-2"
                : "the end of data-name-3 does not follow the end of data-name-2 (data-name-3 cannot be subordinate to, "
                  + "or coincide with, data-name-2)")
            + " (ISO §13.18.45.3 SR11: \"The beginning of the storage area described by data-name-3 shall not precede the "
            + "beginning of the storage area described by data-name-2. The end of the storage area described by "
            + "data-name-3 shall follow the end of the storage area described by data-name-2.\")");
        return false;
    }

    /// <summary>§13.18.45.3 SR2 — "All RENAMES entries referring to data items within a given record shall immediately
    /// follow the last data description entry of the associated record description entry." Asked by <c>BindEntries</c>,
    /// the only place the entry ORDER exists. A level-66 entry needs an open associated record in this section (a
    /// level-77 item is no record description entry, and a constant entry ends the record before it — kb/Work PB2516),
    /// and once a record's RENAMES entries have begun no level 02-49 entry may follow before the next record.</summary>
    private void ScreenRenamesPlacement(DataItem? associatedRecord, string aliasName)
    {
        if (associatedRecord is { Level: not 77 }) return;
        Edition.Error(DiagnosticCatalog.RenamesEntryRule,
            $"'{aliasName}': the RENAMES entry follows "
            + (associatedRecord is null
                ? "no record description entry it can belong to (none precedes it in this section, or a constant entry ended it)"
                : "a level-77 item, which is not a record description entry")
            + " (ISO §13.18.45.3 SR2: \"All RENAMES entries referring to data items within a given record shall "
            + "immediately follow the last data description entry of the associated record description entry.\")");
    }

    /// <summary>SR2's other half: an entry that continues a record whose RENAMES entries have already begun.</summary>
    private void ScreenEntryAfterRenames(string? entryName)
    {
        Edition.Error(DiagnosticCatalog.RenamesEntryRule,
            $"'{entryName ?? "FILLER"}': a data description entry follows the RENAMES entries of its record (ISO "
            + "§13.18.45.3 SR2: \"All RENAMES entries referring to data items within a given record shall immediately "
            + "follow the last data description entry of the associated record description entry.\")");
    }
}
