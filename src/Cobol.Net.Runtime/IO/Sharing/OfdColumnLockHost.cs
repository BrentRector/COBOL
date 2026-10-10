// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE LINUX HOST OF <see cref="RunUnitFileLock"/> (kb/Work PB833): each column is one byte of the physical file,
/// published as a SHARED open-file-description lock through a read-only descriptor of its own
/// (<see cref="OfdRegionLocks"/>), at <c>2^62 +</c> the column's <see cref="RunUnitFileLock.WireNumber"/>. Publishing
/// first and then asking <c>F_OFD_GETLK</c> — which never reports a lock the asking description holds itself — is what
/// makes two simultaneous OPENs unable to pass each other without a mutex, and the lock dies with its descriptor, so a
/// run unit that is killed releases it.
/// </summary>
internal sealed class OfdColumnLockHost : IColumnLockHost
{
    internal static readonly OfdColumnLockHost Instance = new();

    private static readonly int ColumnCount = Enum.GetValues<ExistingSharingColumn>().Length;

    /// <summary>The byte a column is published on. ⛔ A WIRE VALUE (see <see cref="RunUnitFileLock.WireNumber"/>).</summary>
    private static long ByteOf(ExistingSharingColumn column) => HostRegionLocks.RegionBase + RunUnitFileLock.WireNumber(column);

    public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own,
        IReadOnlyList<ExistingSharingColumn> refused, out IDisposable? hold)
    {
        hold = null;
        var region = OfdRegionLocks.Open(hostPath);
        if (region is null) return RunUnitFileLock.Outcome.Unavailable;
        bool kept = false;
        try
        {
            switch (OfdRegionLocks.Hold(region, ByteOf(own), exclusive: false, wait: false))
            {
                case OfdRegionLocks.Result.Held: return RunUnitFileLock.Outcome.Refused;   // someone holds our byte exclusively: not our protocol, not ours to override
                case OfdRegionLocks.Result.Unavailable: return RunUnitFileLock.Outcome.Unavailable;
            }

            foreach (var column in refused)
            {
                switch (OfdRegionLocks.HeldByAnother(region, ByteOf(column)))
                {
                    case OfdRegionLocks.Result.Held: return RunUnitFileLock.Outcome.Refused;
                    case OfdRegionLocks.Result.Unavailable: return RunUnitFileLock.Outcome.Unavailable;
                }
            }
            hold = new Held(region);
            kept = true;
            return RunUnitFileLock.Outcome.Held;
        }
        finally { if (!kept) Give(region); }
    }

    /// <summary>Unlock every protocol byte, THEN close the description (see <see cref="OfdRegionLocks.Release"/>).</summary>
    private static void Give(SafeFileHandle region)
    {
        OfdRegionLocks.Release(region, HostRegionLocks.RegionBase, ColumnCount);
        region.Dispose();
    }

    /// <summary>The published column: closing the description releases every region it holds. Idempotent.</summary>
    private sealed class Held(SafeFileHandle region) : IDisposable
    {
        private SafeFileHandle? _region = region;

        public void Dispose()
        {
            if (_region is { } held) Give(held);
            _region = null;
        }
    }
}
