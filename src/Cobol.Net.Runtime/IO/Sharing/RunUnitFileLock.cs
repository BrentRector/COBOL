// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ ISO §9.1.15's FILE LOCK AGAINST OTHER RUN UNITS, AS TABLE 19 ITSELF (kb/Work PB833). <i>"The successful
/// opening of a file establishes a file lock for the applicable sharing rules, thereby preventing other run units
/// from opening that file with incompatible sharing rules."</i> The applicable rules are rules 1)–3), which
/// <see cref="Table19"/> prints cell by cell, and a second run unit is bound to them exactly as a second connector
/// of this one is.
/// <para><b>The mechanism.</b> A connector's OPEN <i>publishes</i> its Table 19 column — the one cell-group an
/// OPEN request is later judged against (<see cref="Table19.Column"/>) — as a shared region lock on one byte of the
/// physical file, far beyond any record (<see cref="RegionBase"/>), and then asks whether any OTHER description
/// holds a column that its own request row refuses (<see cref="Table19.Cell(OpenRequestRow, ExistingSharingColumn)"/>).
/// It holds the byte for the life of the OPEN and gives it back with its CLOSE (§9.1.15, <i>"The file lock is
/// removed by an explicit or implicit CLOSE statement"</i>); a run unit that dies loses it with its descriptors.</para>
/// <para><b>Why publish first.</b> Two run units opening at once cannot both pass: whichever publishes second
/// finds the first already there, and a request against a column it refuses is answered '61' (§9.1.13.9 1)). It
/// is the ordering that makes the test race-free without a mutex, and it needs only a lock that ignores the asker's
/// OWN description — which is what <see cref="OfdRegionLocks"/> is chosen for, because a class that refuses its
/// own kind (NO OTHER, an OUTPUT) would otherwise refuse itself.</para>
/// <para><b>What it is NOT.</b> The table is read from <see cref="Table19"/>, never rewritten: the cross-run-unit
/// answer and the in-run-unit answer are one lookup, so they cannot drift (<c>RunUnitFileLockDriftTests</c> proves
/// it over every pair). A host share mode (<see cref="FileLockPosture"/>) is still taken on the connector's own
/// handle — it is the only thing a non-COBOL process meets, and on a host whose <see cref="FileShare"/> is
/// mandatory and per-access it already carries most of the table; this lock carries the rest, on the hosts that can
/// hold it. ⚠ It binds RUN UNITS: a process that does not take part in the protocol (a text editor, another
/// language) meets only the host share mode, which §9.1.15 permits the implementor to define ("Other facilities
/// may specify some degree of file sharing, however, their interaction with COBOL file sharing is defined by the
/// implementor", Annex A.1 item 75).</para>
/// <para><b>The protocol is a contract between run units</b> — two versions of this runtime must agree on it — so
/// the byte of each column is spelled out below and not derived from an enum's ordinal.</para>
/// </summary>
internal sealed class RunUnitFileLock : IDisposable
{
    /// <summary>The offset of the first protocol byte: 2<sup>62</sup>, beyond any file a host can hold, so the
    /// regions never overlap a byte a record or another lock user could touch.</summary>
    private const long RegionBase = HostRegionLocks.RegionBase;

    /// <summary>The byte a column is published on. ⛔ A WIRE VALUE: changing it splits the run units of two
    /// runtime versions into groups that cannot see each other.</summary>
    private static long ByteOf(ExistingSharingColumn column) => RegionBase + column switch
    {
        ExistingSharingColumn.NoOtherAnyMode => 0,
        ExistingSharingColumn.ReadOnlyNonInput => 1,
        ExistingSharingColumn.ReadOnlyInput => 2,
        ExistingSharingColumn.AllOtherNonInput => 3,
        ExistingSharingColumn.AllOtherInput => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(column), $"{column} is not an ISO §14.9.27.4 Table 19 column"),
    };

    private static readonly ExistingSharingColumn[] Columns = Enum.GetValues<ExistingSharingColumn>();

    private SafeFileHandle? _region;

    private RunUnitFileLock(SafeFileHandle region) => _region = region;

    /// <summary>The outcome of asking for the lock.</summary>
    internal enum Outcome
    {
        /// <summary>The lock is held; <c>held</c> carries it.</summary>
        Held,
        /// <summary>Another run unit holds a column Table 19 refuses this request: §9.1.13.9 1)'s '61'.</summary>
        Refused,
        /// <summary>This host cannot hold the lock (<see cref="OfdRegionLocks"/>), or the file cannot be reached
        /// for it; the connector has the file lock its <see cref="FileShare"/> gives and nothing more.</summary>
        Unavailable,
    }

    /// <summary>Establish the file lock of an OPEN in (<paramref name="sharing"/>, <paramref name="mode"/>) on
    /// <paramref name="hostPath"/>. <paramref name="held"/> is non-null exactly when the outcome is
    /// <see cref="Outcome.Held"/>; a refused or unavailable lock leaves nothing behind.</summary>
    internal static Outcome Take(string hostPath, FileSharing sharing, FileOpenMode mode, out RunUnitFileLock? held)
    {
        held = null;
        var region = OfdRegionLocks.Open(hostPath);
        if (region is null) return Outcome.Unavailable;
        bool kept = false;
        try
        {
            var own = Table19.Column(sharing, mode);
            switch (OfdRegionLocks.Hold(region, ByteOf(own), exclusive: false, wait: false))
            {
                case OfdRegionLocks.Result.Held: return Outcome.Refused;   // someone holds our byte exclusively: not our protocol, not ours to override
                case OfdRegionLocks.Result.Unavailable: return Outcome.Unavailable;
            }

            var row = Table19.Row(sharing, mode);
            foreach (var column in Columns)
            {
                if (Table19.Cell(row, column) != OpenSharingOutcome.UnsuccessfulOpen) continue;
                switch (OfdRegionLocks.HeldByAnother(region, ByteOf(column)))
                {
                    case OfdRegionLocks.Result.Held: return Outcome.Refused;
                    case OfdRegionLocks.Result.Unavailable: return Outcome.Unavailable;
                }
            }
            held = new RunUnitFileLock(region);
            kept = true;
            return Outcome.Held;
        }
        finally { if (!kept) Give(region); }
    }

    /// <summary>Remove the file lock — the CLOSE's half of §9.1.15. Closing the description releases every
    /// region it holds. Idempotent.</summary>
    public void Dispose()
    {
        if (_region is { } region) Give(region);
        _region = null;
    }

    /// <summary>Unlock every protocol byte, THEN close the description (see <see cref="OfdRegionLocks.Release"/>).</summary>
    private static void Give(SafeFileHandle region)
    {
        OfdRegionLocks.Release(region, RegionBase, Columns.Length);
        region.Dispose();
    }
}
