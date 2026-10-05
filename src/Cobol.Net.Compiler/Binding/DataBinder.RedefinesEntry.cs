// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// The REDEFINES clause's ENTRY-level syntax rules (ISO §13.18.44.3; kb/Work PB1280) — the ones that ask about the
/// subject and data-name-2 as WRITTEN, before the storage-class machinery (<c>ClassifyRedefinesClasses</c>) lays the
/// overlay out: SR1 (position in the entry — <c>ScreenLeadingClausePosition</c>, DataBinder.ClausePlacement.cs, beside
/// its §13.16.3 SR4 TYPEDEF twin), SR2 (identical level-numbers), SR3 (not a level-1 file-section entry),
/// SR8 (the size screen, in BITS, with its level-1-without-EXTERNAL exception), SR13 (data-name-2 is no CONSTANT
/// RECORD) and SR15 (alignment). The clause-placement table had named <c>ResolveRedefines</c> as the home of the
/// "level and position rules" and nothing was there: data-name-2 was found by name alone, so a 77 and an 01 were
/// interchangeable and the SR8 comment ASSUMED SR3.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The entry-level rules over the RESOLVED pair (subject, data-name-2), asked once per REDEFINES clause by
    /// <see cref="ResolveRedefines"/> — the one place both entries are known.</summary>
    private void ScreenRedefinesEntry(DataItem item, DataItem target)
    {
        string name = item.CobolName ?? "FILLER";
        string tname = target.CobolName ?? "FILLER";

        // SR2 — a 77 and an 01 are both roots, so name resolution alone cannot tell them apart; the written
        // level-numbers can.
        if (item.Level != target.Level)
            Edition.Error(DiagnosticCatalog.RedefinesEntryRule,
                $"'{name}' REDEFINES '{tname}': the level-numbers of data-name-2 ({target.Level}) and the subject of "
                + $"the entry ({item.Level}) shall be identical (ISO §13.18.44.3 SR2)");

        // SR3 — first half: no REDEFINES in a level-1 FILE SECTION entry (the records of one FD already share their
        // area, §13.18.33.4 GR3, and that implicit redefinition is not this clause). The second half, an FD with a
        // FORMAT clause, belongs to the declined A.4.8 facility.
        if (item.Parent is null && item.Level == 1 && item.Section == EntrySection.File)
            Edition.Error(DiagnosticCatalog.RedefinesEntryRule,
                $"'{name}': the REDEFINES clause shall not be specified in a level-1 entry in the file section "
                + "(ISO §13.18.44.3 SR3)");

        // SR13 — a constant record cannot be redefined and then modified through the redefining name.
        if (target.IsConstantRecord)
            Edition.Error(DiagnosticCatalog.RedefinesEntryRule,
                $"'{name}' REDEFINES '{tname}': the data description entry for data-name-2 shall not contain the "
                + "CONSTANT RECORD clause (ISO §13.18.44.3 SR13)");

        // SR8 — "The storage area required for the subject of the entry shall not be larger than the storage area
        // required for the data item referenced by data-name-2, unless the data item referenced by data-name-2 has been
        // specified with level number 1 and without the EXTERNAL clause." §13.18.44.4 GR1 measures the association in
        // BITS ("the number of bits required by the data item referenced by the subject of the entry"), so a bit item
        // is measured in bits and everything else in bytes of eight (kb/Work PB231: a national position is two
        // bytes). An EXTERNAL level-1 target is NOT exempt: its area is the run-unit's one shared area, and a larger
        // redefiner would run past it.
        bool exempt = target.Level == 1 && !target.HasExternalClause;
        if (!exempt && StorageBitsOf(item) is var itemBits && StorageBitsOf(target) is var targetBits && itemBits > targetBits)
            Edition.Error("COBOLNET1539", $"'{name}' REDEFINES '{tname}': the redefining storage area "
                + $"({itemBits} bits) is larger than the redefined ({targetBits}) — permitted only when the redefined "
                + "item is level 1 and not EXTERNAL (ISO §13.18.44.3 SR8)");

        // SR15 — "The description of the subject of the entry shall be such that its required alignment is the same
        // as the alignment of the data item referenced by data-name-2." §8.5.1.6.3 puts an elementary bit item that
        // follows a same-level bit item at the NEXT BIT position and every other data item on a byte boundary (an
        // ALIGNED clause, §13.18.1.4 GR1, makes a bit item the latter too). A subject needing a byte boundary over a
        // data-name-2 that begins mid-byte has no storage association §13.18.44.4 GR1 can make: the area "starts at
        // the first bit of the data item referenced by data-name-2". A bit-aligned subject fits any starting bit.
        if (!BitLayout.IsBitItem(item) || item.IsAligned || item.AlignedAsLevelOne)
        {
            int start = TargetStartBit(target);
            if (start > 0 && start % BitLayout.BitsPerCharacter != 0)
                Edition.Error(DiagnosticCatalog.RedefinesEntryRule,
                    $"'{name}' REDEFINES '{tname}': '{name}' requires a byte boundary, but '{tname}' begins at bit "
                    + $"{start % BitLayout.BitsPerCharacter} of its byte — the required alignment of the subject shall be "
                    + "the same as the alignment of the data item referenced by data-name-2 (ISO §13.18.44.3 SR15; "
                    + "§8.5.1.6.3)");
        }
    }

    /// <summary>The storage a REDEFINES overlay measures (§13.18.44.3 SR8 / §13.18.44.4 GR1), in bits: a bit item
    /// exactly (<see cref="BitLayout.RunBits"/>, occurrences included), anything else its byte width times eight.</summary>
    private static long StorageBitsOf(DataItem d) =>
        BitLayout.IsBitItem(d) ? BitLayout.RunBits(d)
        : (long)d.ByteWidth * (d.Occurs ?? 1) * BitLayout.BitsPerCharacter;

    /// <summary>The bit offset of <paramref name="target"/> within its record (§8.5.1.6.3 placement), or -1 when an
    /// overlay chain below it cannot be resolved — the callers must not reject on -1.</summary>
    private static int TargetStartBit(DataItem target)
    {
        var root = target;
        while (root.Parent is { } p) root = p;
        return ReferenceEquals(root, target) ? 0 : BitLayout.StartBitOf(root, target);
    }
}
