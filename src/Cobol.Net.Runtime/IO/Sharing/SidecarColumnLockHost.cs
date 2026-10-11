// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE HOST OF <see cref="RunUnitFileLock"/> WHERE THE KERNEL HAS NO OPEN-FILE-DESCRIPTION LOCKS — macOS above all
/// (kb/Work PB2484). ISO §9.1.15 2): <i>"The sharing with read only mode restricts concurrent access to a physical
/// file through file connectors other than this one, to input mode"</i> binds other RUN UNITS too, and on such a host
/// the only thing a connector met was .NET's one advisory <c>flock</c> (<c>LOCK_EX</c> for
/// <see cref="FileShare.None"/>, <c>LOCK_SH</c> for everything else), which says rule 1 and nothing of rules 2 and 3.
/// <para><b>The mechanism: one lock FILE per holder</b> (<see cref="SidecarLockFiles"/> — the directory, the atomic
/// publication, the probe, the CLOSE). An OPEN publishes <c>&lt;column&gt;-&lt;guid&gt;</c>
/// (<see cref="RunUnitFileLock.WireNumber"/> is the column) and then looks for a holder of a column its request row
/// refuses, other than itself.</para>
/// <para><b>Why one file per holder and not one per column.</b> The protocol publishes first and then asks "does any
/// OTHER holder have a column my row refuses", and a request row can refuse its own column (SHARING WITH READ ONLY
/// beside a READ ONLY connector that writes; every OUTPUT). A <c>flock</c> cannot answer "anybody but me" about a file
/// the asker itself holds, but it can answer it about a file the asker does NOT hold — so every holder owns a file of
/// its own and a tester skips only its own. That keeps Linux's property, with no mutex: two OPENs at the same instant
/// cannot both pass, because whichever completes its publication second enumerates the directory and finds the first
/// (both may be refused — each saw the other — which is a refused OPEN, not a violated lock).</para>
/// <para><b>A holder this process cannot observe is a holder (kb/Work PB2917).</b> A column file the process may not
/// open — another user's, under a umask that gave this user no read access, or an entry that is not a file — is neither
/// free nor swept: <see cref="HostFile.LockFileState.Unobservable"/> refuses a request row that refuses its column, as a
/// live holder does. The alternative, to read "may not open" as "nobody holds it", sweeps another user's live holder and
/// grants the OPEN §9.1.15 requires refused. The price is a leftover this process can neither read nor remove (a killed
/// run unit's file in a sticky directory, owned by someone else) refusing OPENs until its owner or an administrator
/// deletes the <c>.wolk-*</c> directory; a refusal is loud and a silent grant is not (docs/CONFORMANCE.md DOC-A.1-75).
/// A user who may not WRITE the lock directory cannot publish, but can still look: a live or unobservable conflicting
/// holder refuses, and with none the OPEN has no file lock of its own beyond the share mode (<see cref="RunUnitFileLock.Outcome.Unavailable"/>).
/// A user who may enter the lock directory but not LIST it cannot look either: every holder is unobservable to it, so
/// its request is refused whenever its row refuses a column.</para>
/// </summary>
internal sealed class SidecarColumnLockHost : IColumnLockHost
{
    internal static readonly SidecarColumnLockHost Instance = new();

    public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own,
        IReadOnlyList<ExistingSharingColumn> refused, out IDisposable? hold)
    {
        hold = null;
        try
        {
            string directory = SidecarLockFiles.LockDirectoryOf(hostPath);
            SidecarLockFiles.LockFileHolder? published;
            try { published = SidecarLockFiles.Publish(directory, id => $"{RunUnitFileLock.WireNumber(own)}-{id:N}"); }
            catch (UnauthorizedAccessException)
            {
                // This user may not write the lock directory (PB2917): it cannot publish, but it can still refuse.
                return TestOthers(directory, ownName: null, refused) == RunUnitFileLock.Outcome.Refused
                    ? RunUnitFileLock.Outcome.Refused : RunUnitFileLock.Outcome.Unavailable;
            }
            if (published is null) return RunUnitFileLock.Outcome.Unavailable;
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

    /// <summary>Test every column in <paramref name="refused"/> for a holder other than <paramref name="ownName"/>
    /// (null: this process published nothing):
    /// <see cref="RunUnitFileLock.Outcome.Held"/> when there is none (the caller keeps its lock),
    /// <see cref="RunUnitFileLock.Outcome.Refused"/> when one lives or cannot be observed. ⛔ A LOCK DIRECTORY THIS USER
    /// MAY ENTER BUT NOT LIST hides every holder in it, so it is unobservable as a whole and refuses a request row that
    /// refuses any column (the train 1056b review: a <c>0711</c> parent, whose mode the lock directory copies, made the
    /// listing throw, which read as a lock this host cannot hold and granted the OPEN §9.1.15 2) refuses).</summary>
    private static RunUnitFileLock.Outcome TestOthers(string directory, string? ownName,
        IReadOnlyList<ExistingSharingColumn> refused)
    {
        List<string> entries;
        try { entries = Directory.EnumerateFileSystemEntries(directory).ToList(); }
        catch (UnauthorizedAccessException) { return refused.Count == 0 ? RunUnitFileLock.Outcome.Held : RunUnitFileLock.Outcome.Refused; }
        foreach (string path in entries)
        {
            string name = Path.GetFileName(path);
            if (string.Equals(name, ownName, StringComparison.Ordinal)) continue;
            if (SidecarLockFiles.IsRetired(name)) { SidecarLockFiles.TryDelete(path); continue; }   // a crash's leftover between rename and delete
            bool pending = SidecarLockFiles.IsPending(name);
            if (!pending && !IsRefused(refused, name)) continue;
            switch (SidecarLockFiles.Observe(path))
            {
                case HostFile.LockFileState.Live:
                case HostFile.LockFileState.Unobservable:
                    if (!pending) return RunUnitFileLock.Outcome.Refused;   // a pending file is a publisher still racing us, which tests us after its rename
                    break;
            }
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
}
