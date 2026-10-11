// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>What a region operation reported (<see cref="RegionLocks"/>).</summary>
internal enum RegionLockResult
{
    /// <summary>The operation took effect / the region is free of any other holder.</summary>
    Ok,
    /// <summary>Another holder holds the region (or, where the host cannot tell whether one does, might).</summary>
    Held,
    /// <summary>This host (kernel, filesystem or process shape) cannot carry the lock here.</summary>
    Unavailable,
}

/// <summary>
/// ⛔ A CONNECTOR'S ENDPOINT ON THE HOST'S REGION LOCKS — the one primitive by which a run unit tells ANOTHER run unit
/// about a record lock (ISO §9.1.16) or claims a keyed store for the length of one statement (kb/Work PB2660, PB2936).
/// A region is one "byte" of the map in <see cref="HostRegionLocks"/>; the endpoint says "hold byte N" (exclusively, or
/// shared, waiting or not), "does anybody ELSE hold byte N" and "give byte N back", and gives everything it holds back
/// when it is disposed. The policy that uses it — which bytes, when — is <see cref="KeyedConnector"/>'s (the store
/// mutex) and <see cref="PhysicalFileTable"/>'s (record locks).
/// <para><b>One endpoint per connector handle, made by the host of the process</b> (<see cref="IRegionLockHost"/>,
/// <see cref="HostRegionLocks.HostOfThisProcess"/>): <see cref="HandleRegionLocks"/> where the lock is a property of the
/// connector's own open handle (Windows <c>LockFileEx</c>, Linux open-file-description locks), and
/// <see cref="SidecarRegionLocks"/> where the kernel has neither (macOS and the other Unixes) and the lock is a file per
/// holder beside the physical file. Two endpoints conflict with each other exactly as two processes do, whichever host
/// made them, so two run units in one process behave as two processes.</para>
/// </summary>
internal abstract class RegionLocks(bool writable) : IDisposable
{
    /// <summary>Whether the connector's handle is open for writing — so it can hold an EXCLUSIVE lock on a host that
    /// needs one (<see cref="ExclusiveNeedsWritableHandle"/>).</summary>
    internal bool Writable { get; } = writable;

    /// <summary>Whether an EXCLUSIVE hold needs a handle open for writing. Linux refuses one through a read-only
    /// descriptor (<c>EBADF</c>), so a reader there holds shared and tests. Windows' <c>LockFileEx</c> takes an exclusive
    /// lock through any handle with read or write access, and its locks are per handle, so a shared hold there CANNOT be
    /// tested through the same handle: the test's exclusive request meets the handle's own hold and answers
    /// <see cref="RegionLockResult.Held"/> with nobody else holding the byte. A Windows reader therefore holds exclusively
    /// and never tests (lander review, train 1042: every READ WITH LOCK through an <c>OPEN INPUT</c> connector was refused
    /// that way, and its lock was never set). A lock file has no such limit.</summary>
    internal abstract bool ExclusiveNeedsWritableHandle { get; }

    /// <summary>Can this endpoint publish an exclusive hold at all (<see cref="Writable"/>, or a host that does not
    /// need it)?</summary>
    internal bool CanHoldExclusive => Writable || !ExclusiveNeedsWritableHandle;

    /// <summary>Hold <paramref name="offset"/> — exclusively, or shared when <paramref name="exclusive"/> is false —
    /// waiting for a conflicting holder to give it back when <paramref name="wait"/>; otherwise a conflicting holder
    /// answers <see cref="RegionLockResult.Held"/> at once. A SHARED hold exists because Linux refuses an exclusive lock
    /// through a descriptor that is not open for writing: a reader holds the store mutex shared (it never changes the
    /// store), which every exclusive request and every <see cref="HeldByAnother"/> test still sees.</summary>
    internal abstract RegionLockResult Hold(long offset, bool exclusive, bool wait);

    /// <summary>Does a holder OTHER than this endpoint hold <paramref name="offset"/>, in any mode? Nothing is kept.
    /// ⛔ Never asked about a byte this endpoint itself holds on a per-handle host: on Linux the answer would be "free"
    /// and on Windows "held", and the caller (<see cref="PhysicalFileTable"/>) knows its own locks exactly.</summary>
    internal abstract RegionLockResult HeldByAnother(long offset);

    /// <summary>Give back <paramref name="offset"/>. Giving back a byte this endpoint does not hold is a no-op.</summary>
    internal abstract void Release(long offset);

    /// <summary>Give back everything this endpoint holds. Idempotent. The connector's handle is not the endpoint's to
    /// close.</summary>
    public abstract void Dispose();
}

/// <summary>The host's way to make a connector's <see cref="RegionLocks"/>. One per process family
/// (<see cref="HostRegionLocks.HostOfThisProcess"/>); every host is held to the same contract by
/// <c>RegionLocksDriftTests</c>, whichever machine it runs on.</summary>
internal interface IRegionLockHost
{
    /// <summary>The endpoint for a connector whose handle <paramref name="handle"/> is open on the physical file
    /// <paramref name="hostPath"/>, <paramref name="writable"/> when it is open for writing. Never throws: a host that
    /// cannot carry the lock answers <see cref="RegionLockResult.Unavailable"/> from the endpoint's operations.</summary>
    RegionLocks Open(SafeFileHandle handle, string hostPath, bool writable);
}

/// <summary>
/// ⛔ THE MAP OF EVERY BYTE A RUN UNIT PUBLISHES ABOUT A PHYSICAL FILE, AND THE CHOICE OF HOST THAT CARRIES THEM
/// (kb/Work PB2660, PB2936). The endpoint is <see cref="RegionLocks"/>; this class is the byte map
/// (<see cref="StoreMutexByte"/>, <see cref="LockPresenceByte"/>, <see cref="RecordLockByte"/>) and the one choice of
/// host, made once from facts of the process:
/// <list type="bullet">
/// <item><b>Linux (x86-64, arm64)</b> — open-file-description locks (<see cref="OfdRegionLocks"/>, kb/Work PB833):
/// classic <c>fcntl</c> locks and <c>FileStream.Lock</c> are keyed on the process and dropped when any descriptor
/// it holds on the file closes.</item>
/// <item><b>Windows</b> — <c>LockFileEx</c>/<c>UnlockFileEx</c>, which are per-handle. They are MANDATORY for the
/// bytes they cover, which is harmless here because every byte this class names lies at 2^62 or beyond, past the
/// end of any store the format can describe (<see cref="RecordFraming.MaxStoreBytes"/>).</item>
/// <item><b>Everywhere else</b> (macOS above all) — a lock FILE per holder beside the physical file
/// (<see cref="SidecarRegionLocks"/>, kb/Work PB2936): a byte offset cannot be locked there without a native call
/// whose ABI differs on Apple's arm64, but a held file can be named for the byte, the mode and the holder. The record
/// locks and the store mutex bind across run units on every host this runtime runs on.</item>
/// </list>
/// </summary>
internal static class HostRegionLocks
{
    // ── The region map — every byte a run unit publishes about a physical file ──────────────────────────────

    /// <summary>The first byte of the lock region. The five Table 19 column bytes of <see cref="OfdColumnLockHost"/>
    /// (<see cref="RunUnitFileLock.WireNumber"/>) occupy [<see cref="RegionBase"/>, <see cref="RegionBase"/> + 5).</summary>
    internal const long RegionBase = 1L << 62;

    /// <summary>⭐ The keyed store's cross-run-unit MUTEX: held exclusively by a connector for the length of ONE
    /// statement on a store another run unit may write, and for every load and persist of the store, so no run
    /// unit reads a store another is half-way through rewriting and a statement's lock check, operation and lock
    /// action are one step to every other run unit (kb/Work PB2660).</summary>
    internal const long StoreMutexByte = RegionBase + 0x100;

    /// <summary>⭐ "Some connector of this run unit holds a record lock on this file": held SHARED by every endpoint
    /// that has published at least one record lock, for as long as it holds one. A run unit that wants to know
    /// whether ANY other run unit may hold a record lock asks this one byte (<see cref="RegionLocks.HeldByAnother"/>)
    /// before it pays for a record identity and a record byte — so the common case, a shared file nobody has locked
    /// anything in, costs one host question per statement (kb/Work PB2660).</summary>
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

    // ── The host ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE ONE CHOICE OF HOST, made once: the per-handle expression where the process shape carries it
    /// (Windows; Linux with open-file-description locks, <see cref="OfdRegionLocks.Available"/>), otherwise the lock
    /// files of <see cref="SidecarRegionLocks"/>. A host is a fact of the process, so the platform question is asked HERE
    /// and never at a call site (kb/Work PB795).</summary>
    internal static IRegionLockHost HostOfThisProcess { get; } =
        OperatingSystem.IsWindows() || OfdRegionLocks.Available ? HandleRegionLocks.Host : SidecarRegionLocks.Host;

    private static readonly AsyncLocal<IRegionLockHost?> Forced = new();

    /// <summary>Make the endpoint of a connector whose <paramref name="handle"/> is open on
    /// <paramref name="hostPath"/> through the host of this process — or, in a witness, the host
    /// <see cref="Forcing"/> named.</summary>
    internal static RegionLocks Open(SafeFileHandle handle, string hostPath, bool writable) =>
        (Forced.Value ?? HostOfThisProcess).Open(handle, hostPath, writable);

    /// <summary>⛔ A WITNESS SEAM, as <see cref="RunUnitFileLock.Take(IColumnLockHost?, string, FileSharing, FileOpenMode, out RunUnitFileLock?)"/>
    /// is for the file lock: every connector opened on this async flow, until the scope is disposed, gets its endpoint
    /// from <paramref name="host"/>, so the lock-file host can be held to the same record-lock scenarios as the host of
    /// the machine on a machine that is not a Mac. Scoped to the calling flow, so parallel tests do not see each
    /// other's.</summary>
    internal static IDisposable Forcing(IRegionLockHost host)
    {
        var previous = Forced.Value;
        Forced.Value = host;
        return new Scope(previous);
    }

    private sealed class Scope(IRegionLockHost? previous) : IDisposable
    {
        private bool _done;

        public void Dispose()
        {
            if (_done) return;
            _done = true;
            Forced.Value = previous;
        }
    }
}
