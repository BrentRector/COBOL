// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE LOCK-FILE PRIMITIVE OF EVERY HOST THAT HAS NO OPEN-FILE-DESCRIPTION LOCKS — macOS above all (kb/Work PB2484,
/// PB2936): ONE LOCK FILE PER HOLDER, in a lock directory BESIDE the physical file, published atomically and probed
/// without excluding any other prober. <see cref="SidecarColumnLockHost"/> (the §9.1.15 file lock) and
/// <see cref="SidecarRegionLocks"/> (the §9.1.16 record locks and the store mutex) are two POLICIES over this one
/// mechanism, so the rules that make it race-free are written here and nowhere else.
/// <para><b>The directory.</b> <c>.wolk-&lt;16 hex&gt;</c>, named by the SHA-256 of the physical file's name under the
/// host's case rule (<see cref="HostFile.PhysicalFileKey"/>), so two spellings of one file reach one directory and two
/// runtime versions find each other. It is created with the permissions of the physical file's directory
/// (<see cref="HostFile.CreateLockDirectory"/>) and its files carry the directory's read and write bits
/// (<see cref="HostFile.OpenLockHolder"/>), so a second user who may use the physical file's directory can publish into
/// it and read the first user's files (kb/Work PB2917).</para>
/// <para><b>A holder is a file whose lock is held.</b> The file is held open through a <see cref="FileStream"/> that
/// takes an exclusive advisory lock (<see cref="HostFile.OpenLockHolder"/>); a prober asks for a SHARED one
/// (<see cref="HostFile.ProbeLockFile"/>), refused exactly while the holder lives. The kernel drops the lock with the
/// descriptor, so a run unit that is killed holds nothing and its file reads <see cref="HostFile.LockFileState.Dead"/>.
/// No native call is made: <c>fcntl</c> is variadic, which Apple's arm64 ABI passes differently from a fixed signature,
/// and its open-file-description commands are private there.</para>
/// <para><b>Publication is atomic.</b> A holder creates and locks <c>pending-&lt;guid&gt;</c>, checks the lock is really
/// in force, and only then RENAMES it to its final name (a rename keeps the open file description, hence the lock). A
/// tester reads only final names, so a file it can open freely is dead or being closed, never half-published, and it
/// deletes it, which keeps a crashed run unit's litter from growing. A pending file it can open freely it deletes too;
/// a publisher that loses that race publishes again.</para>
/// <para><b>Probing never excludes a prober</b> (kb/Work PB2917): holders are exclusive and probes are shared, so two
/// run units sweeping one dead file at the same instant both open it, and neither reads the other as a holder — the
/// reverse (exclusive probes) refused a request '61' for a connector that did not exist.</para>
/// <para><b>Capability is measured, not assumed (kb/Work PB795).</b> The lock .NET takes is advisory, absent on a network
/// file system and switched off by <c>DOTNET_SYSTEM_IO_DISABLEFILELOCKING</c>; right after creating its file a holder
/// probes it and, if the probe is NOT refused, the host cannot hold the lock here and <see cref="Publish"/> answers null
/// — as it throws for a directory the process may not write. The caller maps both to
/// <see cref="RunUnitFileLock.Outcome.Unavailable"/> (or its region equivalent).</para>
/// <para><b>No name a prober reads is ever deleted in place</b> (<see cref="Retire"/>): where share modes are mandatory
/// (Windows), a file deleted while another handle has it open stays listed, DELETE-PENDING, and every open of it is
/// "access denied" — the very answer an unobservable holder gives — until the last handle closes. So a lock file there
/// is first RENAMED to <c>gone-&lt;guid&gt;</c>, a name no tester probes, and only then deleted: a tester that listed the
/// old name reads <see cref="HostFile.LockFileState.Gone"/>, and "access denied" stays a decisive answer
/// (<see cref="HostFile.ProbeLockFile"/>). A Unix file is gone the instant it is unlinked, so there it is deleted.</para>
/// <para><b>The CLOSE.</b> The holder's file loses its name BEFORE its descriptor closes (a <c>fork</c> for
/// <c>CALL "SYSTEM"</c> holds a copy of the description until its <c>exec</c>, and a name nobody can see cannot refuse a
/// successor for those milliseconds, the window <see cref="OfdRegionLocks.Release"/> closes on Linux), and the directory
/// goes with its last file. <b>What it is NOT:</b> a lock on the file's inode — two spellings that reach one file through
/// a symbolic or hard link name two lock directories, the identity every table here uses
/// (<see cref="HostFile.PhysicalFileComparer"/>); and a program that takes no part in the protocol is not seen.</para>
/// </summary>
internal static class SidecarLockFiles
{
    /// <summary>⛔ A WIRE VALUE: the lock directory is <c>.wolk-</c> + 16 hex digits of the SHA-256 of the physical
    /// file's name key, so two runtime versions find each other.</summary>
    private const string DirectoryPrefix = ".wolk-";

    /// <summary>⛔ A WIRE VALUE: a holder's file before publication. A tester reaps one it can open freely.</summary>
    private const string PendingPrefix = "pending-";

    /// <summary>⛔ A WIRE VALUE: a lock file renamed away before its deletion (<see cref="Retire"/>). No tester probes
    /// one; a tester that lists the whole directory deletes one a crash left behind.</summary>
    private const string RetiredPrefix = "gone-";

    /// <summary>How often a publication retries when a tester or a closing sibling removed what it was standing on.
    /// Each retry is a different race lost; exhausting them is a host that cannot be trusted, not a refusal.</summary>
    private const int PublishAttempts = 8;

    /// <summary>The lock directory of the physical file: beside it, named by the hash of its name key.</summary>
    internal static string LockDirectoryOf(string hostPath)
    {
        string full = Path.GetFullPath(hostPath);
        string parent = Path.GetDirectoryName(full) ?? throw new ArgumentException("A physical file has a directory.", nameof(hostPath));
        string key = HostFile.PhysicalFileKey(Path.GetFileName(full));
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Path.Combine(parent, DirectoryPrefix + Convert.ToHexString(digest, 0, 8).ToLowerInvariant());
    }

    /// <summary>Is <paramref name="name"/> a holder that has not finished publishing?</summary>
    internal static bool IsPending(string name) => name.StartsWith(PendingPrefix, StringComparison.Ordinal);

    /// <summary>Is <paramref name="name"/> a lock file renamed away for deletion (<see cref="Retire"/>)?</summary>
    internal static bool IsRetired(string name) => name.StartsWith(RetiredPrefix, StringComparison.Ordinal);

    /// <summary>Create, lock, verify and name this holder's file, or null when the host cannot hold the lock here.
    /// <paramref name="finalName"/> maps the publication's unique id to the file's published name. Throws what a file
    /// operation throws for a directory the process may not write; the caller maps it.</summary>
    internal static LockFileHolder? Publish(string directory, Func<Guid, string> finalName)
    {
        for (int attempt = 0; attempt < PublishAttempts; attempt++)
        {
            HostFile.CreateLockDirectory(directory);
            var id = Guid.NewGuid();
            string pending = Path.Combine(directory, PendingPrefix + id.ToString("N"));
            FileStream? stream = null;
            try
            {
                stream = HostFile.OpenLockHolder(pending);
                switch (HostFile.ProbeLockFile(pending))
                {
                    case HostFile.LockFileState.Live: break;
                    case HostFile.LockFileState.Gone: Abandon(stream, pending); Thread.Yield(); continue;   // a tester reaped it before we locked it
                    default: Abandon(stream, pending); return null;   // the host takes no lock here (or will not say)
                }
                string published = Path.Combine(directory, finalName(id));
                File.Move(pending, published);
                return new LockFileHolder(stream, published, directory);
            }
            catch (DirectoryNotFoundException) { Abandon(stream, pending); }   // a closing sibling removed the empty directory
            catch (FileNotFoundException) { Abandon(stream, pending); }        // a tester reaped the pending file
            catch (IOException e) when (HostFile.IsSharingRefusal(e)) { Abandon(stream, pending); }   // a tester held it as we locked or renamed it
            Thread.Yield();
        }
        return null;
    }

    /// <summary>Probe the holder file <paramref name="path"/>, and delete it when its holder is dead. The returned state
    /// is what the caller acts on: <see cref="HostFile.LockFileState.Live"/> and <see cref="HostFile.LockFileState.Unobservable"/>
    /// are holders (or what might be one) that nobody may sweep; <see cref="HostFile.LockFileState.Dead"/> and
    /// <see cref="HostFile.LockFileState.Gone"/> are nobody.</summary>
    internal static HostFile.LockFileState Observe(string path)
    {
        var state = HostFile.ProbeLockFile(path);
        if (state == HostFile.LockFileState.Dead) Retire(path);   // a holder that was killed (or a publisher racing us, which publishes again)
        return state;
    }

    internal static void Abandon(FileStream? stream, string path)
    {
        stream?.Dispose();
        Retire(path);
    }

    /// <summary>Remove the lock file <paramref name="path"/> so that no prober ever meets it DELETE-PENDING (class
    /// remarks): where share modes are mandatory it is renamed to a retired name first, which keeps every open handle
    /// (a holder's own, a tester's probe) and takes the name out of every tester's reach, and the retired name is then
    /// deleted; elsewhere it is deleted. A file another tester retired first is simply gone.</summary>
    internal static void Retire(string path)
    {
        if (!HostFile.ShareModesAreMandatory) { TryDelete(path); return; }
        string retired = Path.Combine(Path.GetDirectoryName(path)!, RetiredPrefix + Guid.NewGuid().ToString("N"));
        try { File.Move(path, retired); }
        catch (IOException) { return; }                    // gone already: another tester retired it, or the directory went
        catch (UnauthorizedAccessException) { return; }    // not ours to rename: left for its owner's CLOSE
        TryDelete(retired);                                // delete-pending while a handle lives, under a name nobody probes
    }

    internal static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }                    // held by a tester (Windows), or the directory is gone: the next tester reaps it
        catch (UnauthorizedAccessException) { }    // not ours to delete: left for its owner's CLOSE
    }

    /// <summary>The published holder: closing it deletes the holder's file and, if it was the last, the directory.</summary>
    internal sealed class LockFileHolder(FileStream stream, string path, string directory) : IDisposable
    {
        private FileStream? _stream = stream;

        /// <summary>The file's published name — what a tester skips when it asks for holders other than itself.</summary>
        internal string Name { get; } = Path.GetFileName(path);

        public void Dispose()
        {
            if (_stream is not { } held) return;
            _stream = null;
            Retire(path);    // the name goes first: see the class remarks
            held.Dispose();
            Retire(path);    // a name the first Retire could not take (the host refused the rename) goes once nothing holds it
            try { Directory.Delete(directory); }
            catch (IOException) { }                  // not empty: another holder's files, or a sibling's CLOSE won the race
            catch (UnauthorizedAccessException) { }
        }
    }
}
