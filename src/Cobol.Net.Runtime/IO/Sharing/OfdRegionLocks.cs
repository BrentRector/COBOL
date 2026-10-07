// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE HOST PRIMITIVE UNDER ISO §9.1.15's FILE LOCK WHERE <see cref="FileShare"/> CANNOT CARRY IT — Linux
/// <b>open-file-description</b> byte-range locks (<c>fcntl</c> <c>F_OFD_SETLK</c> / <c>F_OFD_GETLK</c>, kernel 3.15),
/// and nothing else (kb/Work PB833). The policy that uses it — WHICH bytes a connector publishes and tests — is
/// <see cref="RunUnitFileLock"/>'s; this class is only the host's words for "hold a shared lock on byte N" and
/// "does anybody ELSE hold one on byte N".
/// <para><b>Why these locks and not .NET's.</b> .NET on Unix maps every <see cref="FileShare"/> but
/// <see cref="FileShare.None"/> onto ONE advisory <c>flock</c> <c>LOCK_SH</c>, which admits another run unit's
/// writer under every posture but NO OTHER; <c>FileStream.Lock</c> takes a classic <c>F_SETLK</c> lock, which the
/// kernel keys on the PROCESS and drops when ANY descriptor the process holds on the file closes — and this
/// runtime opens and closes bookkeeping handles on a connector's file all the time. An open-file-description
/// lock belongs to the one descriptor that took it: it conflicts with every other descriptor, in this process or
/// another, and it dies with that descriptor alone. That ownership is also what the test needs, because
/// <c>F_OFD_GETLK</c> never reports a lock the asking description holds itself, so a connector can publish its
/// own presence FIRST and then ask whether anybody else is there (<see cref="RunUnitFileLock"/>).</para>
/// <para><b>Capability is measured, not assumed (kb/Work PB795).</b> Nothing here switches on the platform
/// beyond the ABI constants below, which are a per-host fact asked in ONE place (the
/// <see cref="HostFile.IsSharingRefusal"/> pattern). Whether the host actually carries the locks is what the
/// first <c>F_OFD_GETLK</c> says: <c>EINVAL</c> (a kernel before 3.15, or a host whose <c>fcntl</c> number 36 is
/// something else), <c>ENOLCK</c>/<c>EOPNOTSUPP</c> (a filesystem without byte-range locks) and a missing
/// <c>libc</c> (Windows, where <see cref="FileShare"/> is mandatory and per-access already) all answer
/// <see cref="Unavailable"/>, and the connector then has exactly the file lock its <see cref="FileShare"/> gives.
/// The constants are the Linux x86-64/arm64 values and <c>struct flock</c> is the 64-bit layout; any other
/// process shape is <see cref="Unavailable"/> without a call being made.</para>
/// </summary>
internal static class OfdRegionLocks
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Flock
    {
        public short Type;
        public short Whence;
        public long Start;
        public long Length;
        public int Pid;
    }

    private const int F_OFD_GETLK = 36;
    private const int F_OFD_SETLK = 37;
    private const short F_RDLCK = 0;
    private const short F_WRLCK = 1;
    private const short F_UNLCK = 2;
    private const int O_RDONLY = 0;
    private const int O_CLOEXEC = 0x80000;
    private const int O_NONBLOCK = 0x800;
    private const int AT_EMPTY_PATH = 0x1000;
    private const uint STATX_TYPE = 1;
    private const int StatxBytes = 256;       // sizeof(struct statx); the kernel fills the first 144
    private const int StatxModeOffset = 28;   // stx_mode: after mask, blksize, attributes, nlink, uid, gid
    private const int S_IFMT = 0xF000;
    private const int S_IFREG = 0x8000;
    private const int EACCES = 13;
    private const int EAGAIN = 11;

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int NativeOpen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    private static extern int NativeStatx(int dirfd, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags,
        uint mask, byte[] buffer);

    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    private static extern int NativeFcntl(int fd, int command, ref Flock lockRequest);

    /// <summary>The ABI this binding was written for, asked once. A host where it is false never reaches a
    /// native call.</summary>
    private static readonly bool AbiMatches =
        OperatingSystem.IsLinux() && Environment.Is64BitProcess
        && RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.Arm64;

    /// <summary>What a region operation reported.</summary>
    internal enum Result
    {
        /// <summary>The operation took effect / the region is free of any other holder.</summary>
        Ok,
        /// <summary>Another description holds the region.</summary>
        Held,
        /// <summary>This host (kernel, filesystem or process shape) cannot carry the lock.</summary>
        Unavailable,
    }

    /// <summary>Open <paramref name="hostPath"/> read-only for the sole purpose of holding region locks on it,
    /// or null when this host cannot (the ABI is not Linux x86-64/arm64, the kernel lacks OFD locks, the file is
    /// absent or this process may not read it — each is the OPEN's own question to answer, never this one's).
    /// <para>⛔ The descriptor is made with <c>open(2)</c> directly and NOT through <see cref="FileStream"/>, because
    /// .NET would take a <c>flock</c> on it: a <c>LOCK_SH</c> that a sibling connector's own
    /// <see cref="FileShare.None"/> handle would then be refused by, in this very process. <c>O_CLOEXEC</c> keeps a
    /// <c>CALL "SYSTEM"</c> child from inheriting the description and so from outliving the CLOSE as a lock
    /// holder.</para>
    /// <para>⛔ ONLY A REGULAR FILE IS LOCKED. A device node (<c>/dev/null</c>), a FIFO or a terminal is not a
    /// file whose bytes a second run unit can damage, and the lock lives on the inode — every run unit that
    /// assigns <c>/dev/null</c> would be arbitrated against every other on the machine. The open is
    /// <c>O_NONBLOCK</c> so that a FIFO answers at once instead of waiting for a writer that is this very program's
    /// next statement, and the kind is read from the descriptor (<c>statx</c>, whose layout is the same on every
    /// architecture) rather than guessed from the path.</para></summary>
    internal static SafeFileHandle? Open(string hostPath)
    {
        if (!AbiMatches) return null;
        try
        {
            int fd = NativeOpen(hostPath, O_RDONLY | O_CLOEXEC | O_NONBLOCK);
            if (fd < 0) return null;
            var handle = new SafeFileHandle(fd, ownsHandle: true);
            if (IsRegularFile(fd)) return handle;
            handle.Dispose();
            return null;
        }
        catch (EntryPointNotFoundException) { return null; }   // a libc without statx (before glibc 2.28)
        catch (DllNotFoundException) { return null; }          // no libc under this name
    }

    private static bool IsRegularFile(int fd)
    {
        var buffer = new byte[StatxBytes];
        if (NativeStatx(fd, "", AT_EMPTY_PATH, STATX_TYPE, buffer) != 0) return false;
        ushort mode = BitConverter.ToUInt16(buffer, StatxModeOffset);
        return (mode & S_IFMT) == S_IFREG;
    }

    /// <summary>Hold a SHARED lock on the one byte at <paramref name="offset"/> through
    /// <paramref name="handle"/>'s description: compatible with every other shared holder of the byte,
    /// incompatible with an exclusive one.</summary>
    internal static Result HoldShared(SafeFileHandle handle, long offset)
    {
        var request = new Flock { Type = F_RDLCK, Whence = 0, Start = offset, Length = 1 };
        return Classify(handle, F_OFD_SETLK, ref request, onSuccess: Result.Ok);
    }

    /// <summary>Does a description OTHER than <paramref name="handle"/>'s hold the byte at
    /// <paramref name="offset"/>? The question is asked as an exclusive request (<c>F_OFD_GETLK</c> with
    /// <c>F_WRLCK</c>), which any foreign holder of any kind conflicts with, and nothing is taken.</summary>
    internal static Result HeldByAnother(SafeFileHandle handle, long offset)
    {
        var request = new Flock { Type = F_WRLCK, Whence = 0, Start = offset, Length = 1 };
        var result = Classify(handle, F_OFD_GETLK, ref request, onSuccess: Result.Ok);
        return result == Result.Ok && request.Type != F_UNLCK ? Result.Held : result;
    }

    /// <summary>Give back every lock <paramref name="handle"/>'s description holds in
    /// [<paramref name="offset"/>, <paramref name="offset"/> + <paramref name="length"/>) — EXPLICITLY, before the
    /// descriptor is closed. Closing alone releases an open-file-description lock only once EVERY descriptor on
    /// the description is gone, and a <c>fork</c> in another thread (a <c>CALL "SYSTEM"</c>) holds a copy of this
    /// one until its <c>exec</c> closes it: the CLOSE statement would then leave the file locked for those
    /// milliseconds and a successor OPEN be refused '61' for a lock the program had already given back.</summary>
    internal static void Release(SafeFileHandle handle, long offset, long length)
    {
        var request = new Flock { Type = F_UNLCK, Whence = 0, Start = offset, Length = length };
        _ = Classify(handle, F_OFD_SETLK, ref request, onSuccess: Result.Ok);
    }

    private static Result Classify(SafeFileHandle handle, int command, ref Flock request, Result onSuccess)
    {
        bool added = false;
        try
        {
            handle.DangerousAddRef(ref added);
            if (NativeFcntl((int)handle.DangerousGetHandle(), command, ref request) >= 0) return onSuccess;
            int errno = Marshal.GetLastPInvokeError();
            if (errno is EAGAIN or EACCES) return Result.Held;   // F_OFD_SETLK's refusal: another description holds an incompatible lock
            return Result.Unavailable;   // EINVAL (this kernel has no OFD locks), ENOLCK, EOPNOTSUPP, ENOSYS, EBADF — none of them is a conflict
        }
        finally { if (added) handle.DangerousRelease(); }
    }
}
