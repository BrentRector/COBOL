// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE HOST'S BYTE-RANGE LOCKS ON A CONNECTOR'S OWN HANDLE — the one primitive by which a run unit tells
/// ANOTHER run unit about a record lock (ISO §9.1.16) or claims a keyed store for the length of one statement
/// (kb/Work PB2660). The policy that uses it — which bytes, when — is <see cref="KeyedConnector"/>'s (the store
/// mutex) and <see cref="PhysicalFileTable"/>'s (record locks); this class is only the host's words for "hold byte
/// N exclusively", "does anybody ELSE hold byte N" and "give byte N back", and the map of which byte means what
/// (<see cref="StoreMutexByte"/>, <see cref="RecordLockByte"/>).
/// <para><b>One handle, the connector's own.</b> The locks are taken through the handle that IS the connector's
/// §9.1.15 file lock (<c>KeyedConnector.Store</c>; the handle beneath a <c>SequentialConnector</c>'s reader or writer,
/// kb/Work PB2692), never through a second handle on the path: a second handle
/// would have to be admitted by the connector's own share mode, and every other open of a host path is
/// <c>HostFile</c>'s (kb/Work PB713, PB771). Both host expressions below are owned by the HANDLE (the open file
/// description on Linux, the file handle on Windows), so two connectors of one run unit, and two run units in one
/// process, conflict exactly as two processes do.</para>
/// <list type="bullet">
/// <item><b>Linux (x86-64, arm64)</b> — open-file-description locks (<see cref="OfdRegionLocks"/>, kb/Work PB833):
/// classic <c>fcntl</c> locks and <c>FileStream.Lock</c> are keyed on the process and dropped when any descriptor
/// it holds on the file closes.</item>
/// <item><b>Windows</b> — <c>LockFileEx</c>/<c>UnlockFileEx</c>, which are per-handle. They are MANDATORY for the
/// bytes they cover, which is harmless here because every byte this class names lies at 2^62 or beyond, past the
/// end of any store the format can describe (<see cref="RecordFraming.MaxStoreBytes"/>).</item>
/// <item><b>Elsewhere</b> (macOS, or a filesystem without byte-range locks) every request answers
/// <see cref="OfdRegionLocks.Result.Unavailable"/>, and the run unit keeps the guarantees it can keep alone:
/// its own record locks and its own store, as before PB2660 — the determination is docs/CONFORMANCE.md
/// <c>DOC-A.1-75</c>.</item>
/// </list>
/// </summary>
internal static class HostRegionLocks
{
    // ── The region map — every byte a run unit publishes about a physical file ──────────────────────────────

    /// <summary>The first byte of the lock region. The five Table 19 column bytes of <see cref="RunUnitFileLock"/>
    /// occupy [<see cref="RegionBase"/>, <see cref="RegionBase"/> + 5).</summary>
    internal const long RegionBase = 1L << 62;

    /// <summary>⭐ The keyed store's cross-run-unit MUTEX: held exclusively by a connector for the length of ONE
    /// statement on a store another run unit may write, and for every load and persist of the store, so no run
    /// unit reads a store another is half-way through rewriting and a statement's lock check, operation and lock
    /// action are one step to every other run unit (kb/Work PB2660).</summary>
    internal const long StoreMutexByte = RegionBase + 0x100;

    /// <summary>⭐ "Some connector of this run unit holds a record lock on this file": held SHARED by every handle
    /// that has published at least one record lock, for as long as it holds one. A run unit that wants to know
    /// whether ANY other run unit may hold a record lock asks this one byte (<see cref="HeldByAnother"/>) before it
    /// pays for a record identity and a record byte — so the common case, a shared file nobody has locked anything
    /// in, costs one host question per statement (kb/Work PB2660).</summary>
    internal const long LockPresenceByte = RegionBase + 0x101;

    /// <summary>The first byte of the record-lock region; a record's byte is <see cref="RecordLockByte"/>.</summary>
    private const long RecordRegionBase = RegionBase + (1L << 60);

    /// <summary>The record-lock region's width: 2^60 bytes, so the region ends at 2^62 + 2^61, inside a signed
    /// 64-bit file offset.</summary>
    private const long RecordRegionMask = (1L << 60) - 1;

    /// <summary>⛔ THE BYTE THAT STANDS FOR A RECORD across run units — §9.1.16's record identity (an RRN, a prime
    /// key value, a sequential ordinal: <c>FileConnector.LastReadRecordId</c>), hashed into the record region.
    /// <para>It is a HASH because a prime key may be any length and the region is 2^60 bytes; the identity must
    /// also be the same in every process, which <see cref="string.GetHashCode()"/> is not. SHA-256 of the
    /// identity's UTF-16 code units, truncated to 60 bits: two distinct records of one file share a byte with
    /// probability 2^-60 per pair, and the only consequence would be an over-cautious '51' across run units
    /// (never a missed one). Within a run unit the identity is compared exactly (<see cref="PhysicalFileTable"/>).
    /// The determination is docs/CONFORMANCE.md <c>DOC-A.1-109</c>.</para></summary>
    internal static long RecordLockByte(string recordId)
    {
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(MemoryMarshal.AsBytes(recordId.AsSpan()), digest);
        return RecordRegionBase + (BitConverter.ToInt64(digest) & RecordRegionMask);
    }

    // ── The host's words ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Hold <paramref name="offset"/> through <paramref name="handle"/> — exclusively, or shared when
    /// <paramref name="exclusive"/> is false — waiting for a conflicting holder to give it back when
    /// <paramref name="wait"/>; otherwise a conflicting holder answers <see cref="OfdRegionLocks.Result.Held"/> at once.
    /// <para>⛔ A SHARED hold exists because Linux refuses an exclusive lock through a descriptor that is not open
    /// for writing (<c>EBADF</c>), and an <c>OPEN INPUT</c> connector's handle is read-only (<c>KeyedConnector</c>
    /// asks for no more access than its open mode needs, so a read-only file stays readable). A reader therefore
    /// holds the store mutex shared — it never changes the store — and publishes a record lock shared, which every
    /// exclusive request and every <see cref="HeldByAnother"/> test still sees.</para></summary>
    /// <summary>Whether an EXCLUSIVE hold needs a handle open for writing. Linux refuses one through a read-only
    /// descriptor (<c>EBADF</c>), so a reader there holds shared and tests (<see cref="HeldByAnother"/> does not count
    /// the asker's own open file description). Windows' <c>LockFileEx</c> takes an exclusive lock through any handle
    /// with read or write access, and its locks are per handle, so a shared hold there CANNOT be tested through the
    /// same handle: the test's exclusive request meets the handle's own hold and answers
    /// <see cref="OfdRegionLocks.Result.Held"/> with nobody else holding the byte. A Windows reader therefore holds
    /// exclusively and never tests (lander review, train 1042: every READ WITH LOCK through an <c>OPEN INPUT</c>
    /// connector was refused that way, and its lock was never set).</summary>
    internal static bool ExclusiveNeedsWritableHandle => !OperatingSystem.IsWindows();

    internal static OfdRegionLocks.Result Hold(SafeFileHandle handle, long offset, bool exclusive, bool wait)
    {
        if (handle.IsClosed) return OfdRegionLocks.Result.Unavailable;
        if (OperatingSystem.IsWindows()) return Windows.Lock(handle, offset, exclusive, wait);
        return OfdRegionLocks.Available ? OfdRegionLocks.Hold(handle, offset, exclusive, wait) : OfdRegionLocks.Result.Unavailable;
    }

    /// <summary>Does a handle OTHER than <paramref name="handle"/> hold <paramref name="offset"/>, in any mode?
    /// Nothing is kept. ⛔ Never asked about a byte <paramref name="handle"/> itself holds: on Linux the answer would
    /// be "free" and on Windows "held", and the caller (<see cref="PhysicalFileTable"/>) knows its own locks
    /// exactly.</summary>
    internal static OfdRegionLocks.Result HeldByAnother(SafeFileHandle handle, long offset)
    {
        if (handle.IsClosed) return OfdRegionLocks.Result.Unavailable;
        if (OperatingSystem.IsWindows())
        {
            var taken = Windows.Lock(handle, offset, exclusive: true, wait: false);
            if (taken == OfdRegionLocks.Result.Ok) Windows.Unlock(handle, offset);
            return taken == OfdRegionLocks.Result.Ok ? OfdRegionLocks.Result.Ok : taken;
        }
        return OfdRegionLocks.Available ? OfdRegionLocks.HeldByAnother(handle, offset) : OfdRegionLocks.Result.Unavailable;
    }

    /// <summary>Give back <paramref name="offset"/>, held through <paramref name="handle"/>. Giving back a byte the
    /// handle does not hold is a no-op on every host.</summary>
    internal static void Release(SafeFileHandle handle, long offset)
    {
        if (handle.IsClosed) return;   // a closed handle's locks went with it
        if (OperatingSystem.IsWindows()) Windows.Unlock(handle, offset);
        else if (OfdRegionLocks.Available) OfdRegionLocks.Release(handle, offset, 1);
    }

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

        internal static OfdRegionLocks.Result Lock(SafeFileHandle handle, long offset, bool exclusive, bool wait)
        {
            var at = At(offset);
            uint flags = (exclusive ? LockfileExclusiveLock : 0) | (wait ? 0 : LockfileFailImmediately);
            if (LockFileEx(handle, flags, 0, 1, 0, ref at)) return OfdRegionLocks.Result.Ok;
            // Anything but a lock violation (an overlapped handle's ERROR_IO_PENDING, a filesystem without byte-range
            // locks) means this handle cannot carry the lock — never that someone holds it.
            return Marshal.GetLastPInvokeError() == ErrorLockViolation
                ? OfdRegionLocks.Result.Held
                : OfdRegionLocks.Result.Unavailable;
        }

        internal static void Unlock(SafeFileHandle handle, long offset)
        {
            var at = At(offset);
            _ = UnlockFileEx(handle, 0, 1, 0, ref at);   // ERROR_NOT_LOCKED for a byte not held — the documented no-op
        }
    }
}
