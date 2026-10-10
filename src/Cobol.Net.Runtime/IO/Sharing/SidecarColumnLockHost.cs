// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE HOST OF <see cref="RunUnitFileLock"/> WHERE THE KERNEL HAS NO OPEN-FILE-DESCRIPTION LOCKS — macOS above all
/// (kb/Work PB2484). ISO §9.1.15 2): <i>"The sharing with read only mode restricts concurrent access to a physical
/// file through file connectors other than this one, to input mode"</i> binds other RUN UNITS too, and on such a host
/// the only thing a connector met was .NET's one advisory <c>flock</c> (<c>LOCK_EX</c> for
/// <see cref="FileShare.None"/>, <c>LOCK_SH</c> for everything else), which says rule 1 and nothing of rules 2 and 3.
/// <para><b>The mechanism: one lock FILE per holder, in a lock directory BESIDE the physical file.</b> An OPEN creates
/// <c>.wolk-&lt;key&gt;/&lt;column&gt;-&lt;guid&gt;</c> (the directory is named by the SHA-256 of the physical file's
/// name under the host's case rule, <see cref="HostFile.PhysicalFileKey"/>; the column is
/// <see cref="RunUnitFileLock.WireNumber"/>) and holds it open through a <see cref="FileStream"/> whose
/// <see cref="FileShare"/> makes .NET take a shared <c>flock</c> on it. A holder is TESTED by trying to open its file
/// with <see cref="FileShare.None"/> (an exclusive <c>flock</c>): the open is refused exactly while the holder lives, and
/// the kernel drops the lock with the descriptor, so a run unit that is killed holds nothing. No native call is made:
/// <c>fcntl</c> is variadic, which Apple's arm64 ABI passes differently from a fixed signature, so a P/Invoke of it
/// would be wrong on exactly the hosts this class serves, and its open-file-description commands are private there.</para>
/// <para><b>Why one file per holder and not one per column.</b> The protocol publishes first and then asks "does any
/// OTHER holder have a column my row refuses", and a request row can refuse its own column (SHARING WITH READ ONLY
/// beside a READ ONLY connector that writes; every OUTPUT). A <c>flock</c> cannot answer "anybody but me" about a file
/// the asker itself holds, but it can answer it about a file the asker does NOT hold — so every holder owns a file of
/// its own and a tester skips only its own. That keeps Linux's property, with no mutex: two OPENs at the same instant
/// cannot both pass, because whichever completes its publication second enumerates the directory and finds the first.</para>
/// <para><b>Publication is atomic.</b> A holder creates and locks <c>pending-&lt;guid&gt;</c>, checks the lock is
/// really in force, and only then RENAMES it to its column name (a rename keeps the open file description, hence the
/// lock). A tester reads only column names, so a column file it can open freely is dead (its holder was killed) or
/// being closed, never half-published — and it deletes it, which keeps a crashed run unit's litter from growing. A
/// pending file it can open freely it deletes too; a publisher that loses that race publishes again.</para>
/// <para><b>Capability is measured, not assumed (kb/Work PB795).</b> The lock .NET takes is advisory, absent on a
/// network file system and switched off by <c>DOTNET_SYSTEM_IO_DISABLEFILELOCKING</c>; right after creating its file
/// the host opens it again with <see cref="FileShare.None"/> and, if that is NOT refused, the host cannot hold the lock
/// and answers <see cref="RunUnitFileLock.Outcome.Unavailable"/> — as it does for a directory the process may not write.
/// The connector then has the file lock its <see cref="FileShare"/> gives, nothing more.</para>
/// <para><b>The CLOSE.</b> The holder's file is deleted BEFORE its descriptor closes (a <c>fork</c> for
/// <c>CALL "SYSTEM"</c> holds a copy of the description until its <c>exec</c>, and a name nobody can see cannot refuse
/// a successor OPEN for those milliseconds, the window <see cref="OfdRegionLocks.Release"/> closes on Linux), and the
/// directory goes with its last file. <b>What it is NOT:</b> a lock on the file's inode — two spellings that reach one
/// file through a symbolic or hard link name two lock directories, the identity every table here uses
/// (<see cref="HostFile.PhysicalFileComparer"/>); and a program that takes no part in the protocol is not seen.</para>
/// </summary>
internal sealed class SidecarColumnLockHost : IColumnLockHost
{
    internal static readonly SidecarColumnLockHost Instance = new();

    /// <summary>⛔ A WIRE VALUE: the lock directory is <c>.wolk-</c> + 16 hex digits of the SHA-256 of the physical
    /// file's name key, so two runtime versions find each other.</summary>
    private const string DirectoryPrefix = ".wolk-";

    /// <summary>⛔ A WIRE VALUE: a holder's file before publication. A tester reaps one it can open freely.</summary>
    private const string PendingPrefix = "pending-";

    /// <summary>How often a publication retries when a tester or a closing sibling removed what it was standing on.
    /// Each retry is a different race lost; exhausting them is a host that cannot be trusted, not a refusal.</summary>
    private const int PublishAttempts = 8;

    public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own,
        IReadOnlyList<ExistingSharingColumn> refused, out IDisposable? hold)
    {
        hold = null;
        try
        {
            string directory = LockDirectoryOf(hostPath);
            if (Publish(directory, own) is not { } published) return RunUnitFileLock.Outcome.Unavailable;
            bool kept = false;
            try
            {
                var outcome = TestOthers(directory, published.Name, refused);
                if (outcome == RunUnitFileLock.Outcome.Held) { hold = published; kept = true; }
                return outcome;
            }
            finally { if (!kept) published.Dispose(); }
        }
        // An OPEN has only I-O statuses as outcomes (SharedExtendOpenDriftTests): a host that refuses the directory,
        // the file or the listing is a lock this host cannot hold here, never an escaping exception.
        catch (IOException) { return RunUnitFileLock.Outcome.Unavailable; }
        catch (UnauthorizedAccessException) { return RunUnitFileLock.Outcome.Unavailable; }
        catch (ArgumentException) { return RunUnitFileLock.Outcome.Unavailable; }
        catch (NotSupportedException) { return RunUnitFileLock.Outcome.Unavailable; }
    }

    /// <summary>The lock directory of the physical file: beside it, named by the hash of its name key.</summary>
    internal static string LockDirectoryOf(string hostPath)
    {
        string full = Path.GetFullPath(hostPath);
        string parent = Path.GetDirectoryName(full) ?? throw new ArgumentException("A physical file has a directory.", nameof(hostPath));
        string key = HostFile.PhysicalFileKey(Path.GetFileName(full));
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Path.Combine(parent, DirectoryPrefix + Convert.ToHexString(digest, 0, 8).ToLowerInvariant());
    }

    /// <summary>Create, lock, verify and name this holder's file, or null when the host cannot hold the lock here.</summary>
    private static Holder? Publish(string directory, ExistingSharingColumn column)
    {
        for (int attempt = 0; attempt < PublishAttempts; attempt++)
        {
            Directory.CreateDirectory(directory);
            var id = Guid.NewGuid();
            string pending = Path.Combine(directory, PendingPrefix + id.ToString("N"));
            FileStream? stream = null;
            try
            {
                stream = HostFile.OpenLockHolder(pending);
                if (!HostFile.IsHeldByAnother(pending)) { Abandon(stream, pending); return null; }   // the host takes no lock here
                string published = Path.Combine(directory, $"{RunUnitFileLock.WireNumber(column)}-{id:N}");
                File.Move(pending, published);
                return new Holder(stream, published, directory);
            }
            catch (DirectoryNotFoundException) { Abandon(stream, pending); }   // a closing sibling removed the empty directory
            catch (FileNotFoundException) { Abandon(stream, pending); }        // a tester reaped the pending file
            catch (IOException e) when (HostFile.IsSharingRefusal(e)) { Abandon(stream, pending); }   // a tester held it as we locked or renamed it
            Thread.Yield();
        }
        return null;
    }

    /// <summary>Test every column in <paramref name="refused"/> for a holder other than <paramref name="ownName"/>:
    /// <see cref="RunUnitFileLock.Outcome.Held"/> when there is none (the caller keeps its lock),
    /// <see cref="RunUnitFileLock.Outcome.Refused"/> when one lives.</summary>
    private static RunUnitFileLock.Outcome TestOthers(string directory, string ownName,
        IReadOnlyList<ExistingSharingColumn> refused)
    {
        foreach (string path in Directory.EnumerateFiles(directory))
        {
            string name = Path.GetFileName(path);
            if (string.Equals(name, ownName, StringComparison.Ordinal)) continue;
            bool pending = name.StartsWith(PendingPrefix, StringComparison.Ordinal);
            if (!pending && !IsRefused(refused, name)) continue;
            if (!HostFile.IsHeldByAnother(path)) { TryDelete(path); continue; }   // a holder that was killed (or a publisher racing us, which publishes again): nobody to refuse for
            if (HostFile.IsLockFileUnderSweep(path)) continue;                   // a killed holder's file another tester holds exclusively while it sweeps: no connector
            if (!pending) return RunUnitFileLock.Outcome.Refused;
        }
        return RunUnitFileLock.Outcome.Held;
    }

    /// <summary>Does the holder file <paramref name="name"/> (<c>&lt;column&gt;-&lt;guid&gt;</c>) carry a column the
    /// request row refuses?</summary>
    private static bool IsRefused(IReadOnlyList<ExistingSharingColumn> refused, string name)
    {
        int dash = name.IndexOf('-');
        if (dash <= 0 || !int.TryParse(name.AsSpan(0, dash), out int wire)) return false;
        for (int i = 0; i < refused.Count; i++)
            if (RunUnitFileLock.WireNumber(refused[i]) == wire) return true;
        return false;
    }

    private static void Abandon(FileStream? stream, string path)
    {
        stream?.Dispose();
        TryDelete(path);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }                    // held by a tester (Windows), or the directory is gone: the next tester reaps it
        catch (UnauthorizedAccessException) { }    // not ours to delete: left for its owner's CLOSE
    }

    /// <summary>The published column: closing it deletes the holder's file and, if it was the last, the directory.</summary>
    private sealed class Holder(FileStream stream, string path, string directory) : IDisposable
    {
        private FileStream? _stream = stream;

        internal string Name { get; } = Path.GetFileName(path);

        public void Dispose()
        {
            if (_stream is not { } held) return;
            _stream = null;
            if (!HostFile.ShareModesAreMandatory) TryDelete(path);   // the name goes first: see the class remarks
            held.Dispose();
            TryDelete(path);
            try { Directory.Delete(directory); }
            catch (IOException) { }                  // not empty: another holder's files, or a sibling's CLOSE won the race
            catch (UnauthorizedAccessException) { }
        }
    }
}
