// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE REGION LOCKS OF A HOST WITH NO OPEN-FILE-DESCRIPTION LOCKS — macOS above all (kb/Work PB2936). ISO §9.1.16:
/// <i>"While locked by a given file connector, a record is not accessible to another file connector in the same or a
/// different run unit"</i>; §9.1.13.8 1): the access answers I-O status 51. Before this class a record lock and the
/// store mutex of a shared RELATIVE or INDEXED file had no expression off Linux and Windows, so two run units sharing
/// such a file under <c>SHARING WITH ALL OTHER</c> both read '00' for a record one of them held, wrote over each other's
/// store and lost updates, silently.
/// <para><b>The mechanism is <see cref="SidecarLockFiles"/>'s, with a different NAME.</b> A "byte" is held by publishing a
/// lock file in the physical file's lock directory — the one <see cref="SidecarColumnLockHost"/> publishes its columns
/// in — named <c>r&lt;offset as 16 hex digits&gt;-&lt;S|X&gt;-&lt;guid&gt;</c>: the byte, the mode (<c>S</c> shared,
/// <c>X</c> exclusive) and a name no other holder shares. (⛔ A WIRE VALUE: two runtime versions must find each other.)
/// Classic <c>fcntl</c> record locks cannot stand in: they belong to the process and are dropped when ANY descriptor the
/// process holds on the file closes, so a second connector's CLOSE in the same run unit would release the first's locks;
/// and <c>fcntl</c> is variadic, which Apple's arm64 ABI passes differently from a fixed P/Invoke signature.</para>
/// <para><b>Publish, then test, as <see cref="RunUnitFileLock"/> does.</b> A hold publishes its file and then looks for
/// another LIVE holder of the same byte that conflicts with it — any holder for an exclusive hold, an exclusive holder
/// for a shared one — skipping only its own file. Two exclusive requests at the same instant cannot both pass, because
/// whichever completes its publication second finds the first; both may be refused (each saw the other), which is a
/// refused statement and not a violated lock, and a waiting request retries after a jittered pause, so one of them
/// goes through. A refused or unavailable hold leaves no file behind.</para>
/// <para><b>Dead holders refuse nobody, and holders this process cannot observe refuse everybody.</b> A file whose
/// lock nobody holds is a killed run unit's leftover and is swept by whoever finds it; a file the process may not open
/// is <see cref="HostFile.LockFileState.Unobservable"/> and is neither (kb/Work PB2917): a non-waiting hold reports it
/// <see cref="RegionLockResult.Held"/>, and a WAITING one reports <see cref="RegionLockResult.Unavailable"/> because it
/// cannot wait for a release it cannot see — the run unit then has only its own protection for that statement, the
/// same degradation a directory it may not write has. A lock directory this user may enter but not LIST hides every
/// holder in it and is unobservable as a whole (the train 1056b review: read as a lock the host cannot hold, it gave a
/// READ WITH LOCK the record §9.1.13.8 1) refuses with '51').</para>
/// <para><b>Release and close.</b> A released byte's file is deleted before its descriptor closes
/// (<see cref="SidecarLockFiles.LockFileHolder"/>), the endpoint's disposal releases every byte it still holds, and a
/// run unit that is killed holds nothing — the kernel dropped its locks with its descriptors.</para>
/// </summary>
internal sealed class SidecarRegionLocks : RegionLocks
{
    /// <summary>The host of the lock-file expression.</summary>
    internal static readonly IRegionLockHost Host = new SidecarHost();

    private sealed class SidecarHost : IRegionLockHost
    {
        public RegionLocks Open(SafeFileHandle handle, string hostPath, bool writable) => new SidecarRegionLocks(hostPath, writable);
    }

    /// <summary>The lock directory, or null for a path no lock directory can be named beside (every operation is then
    /// <see cref="RegionLockResult.Unavailable"/>).</summary>
    private readonly string? _directory;

    private readonly Dictionary<long, Held> _holds = [];

    private bool _disposed;

    private readonly record struct Held(SidecarLockFiles.LockFileHolder Holder, bool Exclusive);

    private enum Conflict { None, Live, Unobservable }

    private SidecarRegionLocks(string hostPath, bool writable) : base(writable)
    {
        try { _directory = SidecarLockFiles.LockDirectoryOf(hostPath); }
        catch (ArgumentException) { }
        catch (NotSupportedException) { }
        catch (PathTooLongException) { }
    }

    /// <summary>A lock file's name carries no mode a lock file could not hold, so a shared hold needs no read-only
    /// handle and an exclusive one no writable handle.</summary>
    internal override bool ExclusiveNeedsWritableHandle => false;

    /// <summary>The prefix of every file holding <paramref name="offset"/>: <c>r</c> and the offset as 16 hex digits.</summary>
    private static string Prefix(long offset) => $"r{offset:x16}-";

    internal override RegionLockResult Hold(long offset, bool exclusive, bool wait)
    {
        if (_disposed || _directory is null) return RegionLockResult.Unavailable;
        try
        {
            if (_holds.TryGetValue(offset, out var mine))
            {
                if (mine.Exclusive || !exclusive) return RegionLockResult.Ok;   // held at least as strongly already
                Release(offset);                                                 // shared -> exclusive: give back, then take
            }
            for (int pause = 0; ; pause++)
            {
                // After a conflict, look before publishing again so a waiting request does not churn lock files.
                if (pause > 0)
                {
                    var plain = Look(offset, exclusive, ownName: null);
                    if (plain == Conflict.Unobservable) return wait ? RegionLockResult.Unavailable : RegionLockResult.Held;
                    if (plain == Conflict.Live) { if (!wait) return RegionLockResult.Held; Backoff(pause); continue; }
                }
                var holder = SidecarLockFiles.Publish(_directory, id => $"{Prefix(offset)}{(exclusive ? 'X' : 'S')}-{id:N}");
                if (holder is null) return RegionLockResult.Unavailable;
                switch (Look(offset, exclusive, holder.Name))
                {
                    case Conflict.None:
                        _holds[offset] = new Held(holder, exclusive);
                        return RegionLockResult.Ok;
                    case Conflict.Unobservable:
                        holder.Dispose();
                        return wait ? RegionLockResult.Unavailable : RegionLockResult.Held;
                    default:
                        holder.Dispose();
                        if (!wait) return RegionLockResult.Held;
                        Backoff(pause);
                        continue;
                }
            }
        }
        catch (IOException) { return RegionLockResult.Unavailable; }
        catch (UnauthorizedAccessException) { return RegionLockResult.Unavailable; }
        catch (ArgumentException) { return RegionLockResult.Unavailable; }
        catch (NotSupportedException) { return RegionLockResult.Unavailable; }
    }

    internal override RegionLockResult HeldByAnother(long offset)
    {
        if (_disposed || _directory is null) return RegionLockResult.Unavailable;
        try
        {
            string? own = _holds.TryGetValue(offset, out var mine) ? mine.Holder.Name : null;
            return Look(offset, exclusive: true, own) == Conflict.None ? RegionLockResult.Ok : RegionLockResult.Held;
        }
        catch (IOException) { return RegionLockResult.Unavailable; }
        catch (UnauthorizedAccessException) { return RegionLockResult.Unavailable; }
    }

    internal override void Release(long offset)
    {
        if (_holds.Remove(offset, out var held)) held.Holder.Dispose();
    }

    public override void Dispose()
    {
        _disposed = true;
        foreach (var held in _holds.Values) held.Holder.Dispose();
        _holds.Clear();
    }

    /// <summary>Is there a holder of <paramref name="offset"/>, other than the file <paramref name="ownName"/>, that a
    /// request in this mode conflicts with: any holder for an exclusive request, an exclusive holder for a shared one?
    /// A dead holder's file is swept on the way and conflicts with nothing.</summary>
    private Conflict Look(long offset, bool exclusive, string? ownName)
    {
        string prefix = Prefix(offset);
        IEnumerable<string> entries;
        try { entries = Directory.EnumerateFileSystemEntries(_directory!, prefix + "*").ToList(); }
        catch (DirectoryNotFoundException) { return Conflict.None; }   // no holder has ever published here, or the last one closed
        catch (UnauthorizedAccessException) { return Conflict.Unobservable; }   // a directory this user may enter but not list hides every holder
        Conflict found = Conflict.None;
        foreach (string path in entries)
        {
            string name = Path.GetFileName(path);
            if (string.Equals(name, ownName, StringComparison.Ordinal)) continue;
            bool otherExclusive = name.Length > prefix.Length && name[prefix.Length] == 'X';
            if (!exclusive && !otherExclusive) continue;   // shared beside shared
            switch (SidecarLockFiles.Observe(path))
            {
                case HostFile.LockFileState.Live: return Conflict.Live;
                case HostFile.LockFileState.Unobservable: found = Conflict.Unobservable; break;
            }
        }
        return found;
    }

    /// <summary>The pause between two attempts of a waiting hold: jittered, so two requests that refused each other
    /// at the same instant do not retry in step.</summary>
    private static void Backoff(int attempt) => Thread.Sleep(Random.Shared.Next(1, 3 + Math.Min(attempt, 18)));
}
