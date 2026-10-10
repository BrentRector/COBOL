// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ ISO §9.1.15's FILE LOCK AGAINST OTHER RUN UNITS, AS TABLE 19 ITSELF (kb/Work PB833, PB2484). <i>"The successful
/// opening of a file establishes a file lock for the applicable sharing rules, thereby preventing other run units
/// from opening that file with incompatible sharing rules."</i> The applicable rules are rules 1)–3), which
/// <see cref="Table19"/> prints cell by cell, and a second run unit is bound to them exactly as a second connector
/// of this one is.
/// <para><b>The mechanism.</b> A connector's OPEN <i>publishes</i> its Table 19 column — the one cell-group an
/// OPEN request is later judged against (<see cref="Table19.Column"/>) — and then asks whether any OTHER holder
/// has published a column that its own request row refuses
/// (<see cref="Table19.Cell(OpenRequestRow, ExistingSharingColumn)"/>). It holds the column for the life of the OPEN and
/// gives it back with its CLOSE (§9.1.15, <i>"The file lock is removed by an explicit or implicit CLOSE
/// statement"</i>); a run unit that dies loses it with its descriptors. HOW a column is published and tested is the
/// host's (<see cref="IColumnLockHost"/>): a shared byte-range lock on the physical file on Linux
/// (<see cref="OfdColumnLockHost"/>), a lock file per holder beside it on every other Unix
/// (<see cref="SidecarColumnLockHost"/>); this class is the POLICY and is the same on both.</para>
/// <para><b>Why publish first.</b> Two run units opening at once cannot both pass: whichever publishes second
/// finds the first already there, and a request against a column it refuses is answered '61' (§9.1.13.9 1)). It
/// is the ordering that makes the test race-free without a mutex, and it needs a test that ignores the asker's
/// OWN publication — which is what both hosts are chosen for, because a class that refuses its own kind (NO OTHER,
/// an OUTPUT) would otherwise refuse itself.</para>
/// <para><b>What it is NOT.</b> The table is read from <see cref="Table19"/>, never rewritten: the cross-run-unit
/// answer and the in-run-unit answer are one lookup, so they cannot drift (<c>RunUnitFileLockDriftTests</c> proves
/// it over every pair, on each host). A host share mode (<see cref="FileLockPosture"/>) is still taken on the connector's own
/// handle — it is the only thing a non-COBOL process meets, and on a host whose <see cref="FileShare"/> is
/// mandatory and per-access it already carries most of the table; this lock carries the rest, on the hosts that can
/// hold it. ⚠ It binds RUN UNITS: a process that does not take part in the protocol (a text editor, another
/// language) meets only the host share mode, which §9.1.15 permits the implementor to define ("Other facilities
/// may specify some degree of file sharing, however, their interaction with COBOL file sharing is defined by the
/// implementor", Annex A.1 item 75).</para>
/// <para><b>The protocol is a contract between run units</b> — two versions of this runtime must agree on it — so
/// the number of each column is spelled out below (<see cref="WireNumber"/>) and not derived from an enum's ordinal.</para>
/// </summary>
internal sealed class RunUnitFileLock : IDisposable
{
    /// <summary>The number a column is published under. ⛔ A WIRE VALUE: the byte at 2<sup>62</sup> + n of the physical
    /// file (<see cref="OfdColumnLockHost"/>) and the prefix of a holder's lock file (<see cref="SidecarColumnLockHost"/>);
    /// changing it splits the run units of two runtime versions into groups that cannot see each other.</summary>
    internal static int WireNumber(ExistingSharingColumn column) => column switch
    {
        ExistingSharingColumn.NoOtherAnyMode => 0,
        ExistingSharingColumn.ReadOnlyNonInput => 1,
        ExistingSharingColumn.ReadOnlyInput => 2,
        ExistingSharingColumn.AllOtherNonInput => 3,
        ExistingSharingColumn.AllOtherInput => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(column), $"{column} is not an ISO §14.9.27.4 Table 19 column"),
    };

    private static readonly ExistingSharingColumn[] Columns = Enum.GetValues<ExistingSharingColumn>();

    /// <summary>⛔ THE ONE CHOICE OF HOST, made once: Linux's open-file-description locks where the process shape
    /// carries them (<see cref="OfdRegionLocks.Available"/>); otherwise, off Windows, the lock files of
    /// <see cref="SidecarColumnLockHost"/>; and on Windows none, because its share modes are mandatory and per-access
    /// (<see cref="HostFile.ShareModesAreMandatory"/>) and the connector's own handle already carries the table. A host
    /// is a fact of the process, so the platform question is asked HERE and never at a call site (kb/Work PB795).</summary>
    internal static IColumnLockHost? HostOfThisProcess { get; } =
        OfdRegionLocks.Available ? OfdColumnLockHost.Instance
        : HostFile.ShareModesAreMandatory ? null
        : SidecarColumnLockHost.Instance;

    private IDisposable? _hold;

    private RunUnitFileLock(IDisposable hold) => _hold = hold;

    /// <summary>The outcome of asking for the lock.</summary>
    internal enum Outcome
    {
        /// <summary>The lock is held; <c>held</c> carries it.</summary>
        Held,
        /// <summary>Another run unit holds a column Table 19 refuses this request: §9.1.13.9 1)'s '61'.</summary>
        Refused,
        /// <summary>This host cannot hold the lock (<see cref="HostOfThisProcess"/>), or the file cannot be reached
        /// for it; the connector has the file lock its <see cref="FileShare"/> gives and nothing more.</summary>
        Unavailable,
    }

    /// <summary>Establish the file lock of an OPEN in (<paramref name="sharing"/>, <paramref name="mode"/>) on
    /// <paramref name="hostPath"/> through this process's host. <paramref name="held"/> is non-null exactly when the
    /// outcome is <see cref="Outcome.Held"/>; a refused or unavailable lock leaves nothing behind.</summary>
    internal static Outcome Take(string hostPath, FileSharing sharing, FileOpenMode mode, out RunUnitFileLock? held) =>
        Take(HostOfThisProcess, hostPath, sharing, mode, out held);

    /// <summary>The same through <paramref name="host"/> (null: a host with no lock, <see cref="Outcome.Unavailable"/>).
    /// Every host is held to the same table by <c>RunUnitFileLockDriftTests</c>, whichever the process is running on.</summary>
    internal static Outcome Take(IColumnLockHost? host, string hostPath, FileSharing sharing, FileOpenMode mode,
        out RunUnitFileLock? held)
    {
        held = null;
        if (host is null) return Outcome.Unavailable;
        var row = Table19.Row(sharing, mode);
        var refused = Columns.Where(column => Table19.Cell(row, column) == OpenSharingOutcome.UnsuccessfulOpen).ToArray();
        var outcome = host.Establish(hostPath, Table19.Column(sharing, mode), refused, out var hold);
        if (outcome == Outcome.Held) held = new RunUnitFileLock(hold!);
        return outcome;
    }

    /// <summary>Remove the file lock — the CLOSE's half of §9.1.15. Idempotent.</summary>
    public void Dispose()
    {
        _hold?.Dispose();
        _hold = null;
    }
}
