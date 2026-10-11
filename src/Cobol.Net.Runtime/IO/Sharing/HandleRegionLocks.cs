// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE REGION LOCKS THAT ARE A PROPERTY OF THE CONNECTOR'S OWN OPEN HANDLE (kb/Work PB2660): the locks are taken
/// through the handle that IS the connector's §9.1.15 file lock (<c>KeyedConnector</c>'s store; the handle beneath a
/// <c>SequentialConnector</c>'s reader or writer, kb/Work PB2692), never through a second handle on the path: a second
/// handle would have to be admitted by the connector's own share mode, and every other open of a host path is
/// <c>HostFile</c>'s (kb/Work PB713, PB771). Both expressions are owned by the HANDLE (the open file description on
/// Linux, the file handle on Windows), so two connectors of one run unit, and two run units in one process, conflict
/// exactly as two processes do, and the locks go with the handle when it closes.
/// <list type="bullet">
/// <item><b>Linux (x86-64, arm64)</b> — <see cref="OfdRegionLocks"/>.</item>
/// <item><b>Windows</b> — <c>LockFileEx</c>/<c>UnlockFileEx</c>, per handle and MANDATORY for the bytes they cover,
/// which is harmless here because every byte <see cref="HostRegionLocks"/> names lies at 2^62 or beyond.</item>
/// </list>
/// On a host that has neither (macOS), <see cref="HostRegionLocks.HostOfThisProcess"/> is not this host, and a witness
/// that forces it there meets <see cref="RegionLockResult.Unavailable"/> from every operation.
/// </summary>
internal sealed class HandleRegionLocks(SafeFileHandle handle, bool writable) : RegionLocks(writable)
{
    /// <summary>The host of the per-handle expressions.</summary>
    internal static readonly IRegionLockHost Host = new HandleHost();

    private sealed class HandleHost : IRegionLockHost
    {
        public RegionLocks Open(SafeFileHandle handle, string hostPath, bool writable) => new HandleRegionLocks(handle, writable);
    }

    internal override bool ExclusiveNeedsWritableHandle => !OperatingSystem.IsWindows();

    internal override RegionLockResult Hold(long offset, bool exclusive, bool wait)
    {
        if (handle.IsClosed) return RegionLockResult.Unavailable;
        if (OperatingSystem.IsWindows()) return Windows.Lock(handle, offset, exclusive, wait);
        return OfdRegionLocks.Available ? OfdRegionLocks.Hold(handle, offset, exclusive, wait) : RegionLockResult.Unavailable;
    }

    internal override RegionLockResult HeldByAnother(long offset)
    {
        if (handle.IsClosed) return RegionLockResult.Unavailable;
        if (OperatingSystem.IsWindows())
        {
            var taken = Windows.Lock(handle, offset, exclusive: true, wait: false);
            if (taken == RegionLockResult.Ok) Windows.Unlock(handle, offset);
            return taken;
        }
        return OfdRegionLocks.Available ? OfdRegionLocks.HeldByAnother(handle, offset) : RegionLockResult.Unavailable;
    }

    internal override void Release(long offset)
    {
        if (handle.IsClosed) return;   // a closed handle's locks went with it
        if (OperatingSystem.IsWindows()) Windows.Unlock(handle, offset);
        else if (OfdRegionLocks.Available) OfdRegionLocks.Release(handle, offset, 1);
    }

    /// <summary>The handle's locks go with the handle, which the connector closes.</summary>
    public override void Dispose() { }

    /// <summary>The Windows expression: <c>LockFileEx</c> / <c>UnlockFileEx</c> on one byte.</summary>
    [SupportedOSPlatform("windows")]
    private static class Windows
    {
        private const uint LockfileFailImmediately = 0x1;
        private const uint LockfileExclusiveLock = 0x2;
        private const int ErrorLockViolation = 33;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool LockFileEx(SafeFileHandle file, uint flags, uint reserved, uint bytesLow,
            uint bytesHigh, ref NativeOverlapped overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnlockFileEx(SafeFileHandle file, uint reserved, uint bytesLow, uint bytesHigh,
            ref NativeOverlapped overlapped);

        private static NativeOverlapped At(long offset) => new()
        {
            OffsetLow = unchecked((int)(uint)offset),
            OffsetHigh = (int)(offset >> 32),
        };

        internal static RegionLockResult Lock(SafeFileHandle handle, long offset, bool exclusive, bool wait)
        {
            var at = At(offset);
            uint flags = (exclusive ? LockfileExclusiveLock : 0) | (wait ? 0 : LockfileFailImmediately);
            if (LockFileEx(handle, flags, 0, 1, 0, ref at)) return RegionLockResult.Ok;
            // Anything but a lock violation (an overlapped handle's ERROR_IO_PENDING, a filesystem without byte-range
            // locks) means this handle cannot carry the lock — never that someone holds it.
            return Marshal.GetLastPInvokeError() == ErrorLockViolation
                ? RegionLockResult.Held
                : RegionLockResult.Unavailable;
        }

        internal static void Unlock(SafeFileHandle handle, long offset)
        {
            var at = At(offset);
            _ = UnlockFileEx(handle, 0, 1, 0, ref at);   // ERROR_NOT_LOCKED for a byte not held — the documented no-op
        }
    }
}
