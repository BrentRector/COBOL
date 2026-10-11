// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A HOST WITHOUT OPEN-FILE-DESCRIPTION LOCKS ARBITRATES A SECOND RUN UNIT BY TABLE 19 TOO (kb/Work PB2484).
/// ISO §9.1.15 2): <i>"The sharing with read only mode restricts concurrent access to a physical file through file
/// connectors other than this one, to input mode"</i>, and the file lock of §9.1.15 binds <i>"other run units"</i>. On
/// macOS (and every Unix whose process shape <c>OfdRegionLocks</c> was not written for) the only lock a connector met
/// was .NET's one advisory <c>flock</c>, which says rule 1 and nothing of rules 2 and 3, so a second run unit's OPEN
/// EXTEND against a file another held SHARING WITH READ ONLY answered '00' where Table 19 prints '61', and both wrote.
/// <see cref="SidecarColumnLockHost"/> publishes each holder's Table 19 column as a lock file beside the physical file.
/// <para>The class is plain managed code over <see cref="FileShare"/>, which is advisory <c>flock</c> on Unix and
/// mandatory share modes on Windows, so these tests run on EVERY host and do not need a Mac: the contract they hold it
/// to is the table itself (<see cref="Table19.Conflicts"/>), crossed over every pair, exactly as
/// <see cref="RunUnitFileLockDriftTests"/> holds the host of the process it runs on.</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class SidecarColumnLockHostDriftTests
{
    private static readonly IColumnLockHost Host = SidecarColumnLockHost.Instance;

    private static readonly Regex LockDirectoryName = new(@"^\.wolk-[0-9a-f]{16}$", RegexOptions.Compiled);
    private static readonly Regex HolderFileName = new(@"^[0-4]-[0-9a-f]{32}$", RegexOptions.Compiled);

    /// <summary>A private directory holding one physical file, so the lock directory beside it is this test's alone.</summary>
    private static (string Directory, string File) NewFile()
    {
        var dir = System.IO.Directory.CreateTempSubdirectory("pb2484-").FullName;
        string file = Path.Combine(dir, "data.dat");
        System.IO.File.WriteAllText(file, "SEED");
        return (dir, file);
    }

    private static string[] HolderFilesBeside(string directory) =>
        System.IO.Directory.EnumerateDirectories(directory, ".wolk-*")
            .SelectMany(d => System.IO.Directory.EnumerateFiles(d)).ToArray();

    private static void Clean(string directory)
    {
        try { System.IO.Directory.Delete(directory, recursive: true); } catch (IOException) { }
    }

    public static TheoryData<FileSharing, FileOpenMode, FileSharing, FileOpenMode> EveryPair()
    {
        var data = new TheoryData<FileSharing, FileOpenMode, FileSharing, FileOpenMode>();
        foreach (var sa in Table19.StandardModes)
            foreach (var ma in Enum.GetValues<FileOpenMode>())
                foreach (var sb in Table19.StandardModes)
                    foreach (var mb in Enum.GetValues<FileOpenMode>())
                        data.Add(sa, ma, sb, mb);
        return data;
    }

    /// <summary>For EVERY sharing mode × open mode on the holder crossed with every one on the requester, the second
    /// holder is refused exactly where Table 19 prints <i>Unsuccessful open</i>, and a refused requester leaves no
    /// file behind (§14.9.27.4 GR25, <i>"the file is not affected"</i>). Both orders are covered by the cross product,
    /// and so are the requests that refuse their OWN column (READ ONLY beside READ ONLY non-input, every OUTPUT),
    /// which a per-column lock could not tell from the asker itself.</summary>
    [Theory]
    [MemberData(nameof(EveryPair))]
    public void ASecondHolderIsArbitratedByTable19(FileSharing sa, FileOpenMode ma, FileSharing sb, FileOpenMode mb)
    {
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, sa, ma, out var first));
            var outcome = RunUnitFileLock.Take(Host, file, sb, mb, out var second);

            bool refused = Table19.Conflicts((sb, mb), (sa, ma));
            Assert.True(outcome == (refused ? RunUnitFileLock.Outcome.Refused : RunUnitFileLock.Outcome.Held),
                $"Table 19 {(refused ? "refuses" : "permits")} {(sb, mb)} against {(sa, ma)}, but the second holder was {outcome}.");
            Assert.Equal(refused, second is null);
            Assert.Equal(refused ? 1 : 2, HolderFilesBeside(dir).Length);

            second?.Dispose();
            first!.Dispose();
            Assert.Empty(System.IO.Directory.EnumerateDirectories(dir));   // the CLOSE removes the lock with its last file
        }
        finally { Clean(dir); }
    }

    /// <summary>§9.1.15: <i>"The file lock is removed by an explicit or implicit CLOSE statement"</i>, so the refusal
    /// is not sticky.</summary>
    [Fact]
    public void TheCloseGivesTheLockBack()
    {
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.NoOther, FileOpenMode.IO, out var first));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out _));
            first!.Dispose();
            first.Dispose();   // idempotent
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var second));
            second!.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>⛔ TWO RUN UNITS OPENING AT THE SAME INSTANT CANNOT BOTH WIN: whichever completes its publication second
    /// enumerates the lock directory and finds the first. At most one is ever <see cref="RunUnitFileLock.Outcome.Held"/>
    /// (both may be refused — each saw the other — which is a refused OPEN, not a violated lock).</summary>
    [Fact]
    public void TwoSimultaneousExclusiveTakes_NeverBothHold()
    {
        var (dir, file) = NewFile();
        try
        {
            for (int round = 0; round < 100; round++)
            {
                using var barrier = new Barrier(2);
                var outcomes = new RunUnitFileLock.Outcome[2];
                var holds = new RunUnitFileLock?[2];
                var threads = Enumerable.Range(0, 2).Select(i => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    outcomes[i] = RunUnitFileLock.Take(Host, file, FileSharing.NoOther, FileOpenMode.IO, out holds[i]);
                })).ToArray();
                foreach (var t in threads) t.Start();
                foreach (var t in threads) t.Join();

                Assert.False(outcomes[0] == RunUnitFileLock.Outcome.Held && outcomes[1] == RunUnitFileLock.Outcome.Held,
                    $"Round {round}: both holders took SHARING WITH NO OTHER on {file} — §9.1.15 1) 'specifies exclusive access'.");
                Assert.DoesNotContain(RunUnitFileLock.Outcome.Unavailable, outcomes);
                foreach (var hold in holds) hold?.Dispose();
            }
        }
        finally { Clean(dir); }
    }

    /// <summary>A run unit that was killed holds nothing — the kernel dropped its lock with its descriptor — but its
    /// file is left behind. A leftover lock file nobody holds refuses nobody, and the next tester sweeps it, so a
    /// crashed run unit's litter does not grow; a half-published (pending) file is swept the same way.</summary>
    [Fact]
    public void AKilledHoldersLeftoversRefuseNobodyAndAreSwept()
    {
        var (dir, file) = NewFile();
        try
        {
            string lockDir = SidecarLockFiles.LockDirectoryOf(file);
            System.IO.Directory.CreateDirectory(lockDir);
            string staleColumn = Path.Combine(lockDir, $"0-{Guid.NewGuid():N}");   // NO OTHER: refuses every request row
            string stalePending = Path.Combine(lockDir, $"pending-{Guid.NewGuid():N}");
            System.IO.File.WriteAllText(staleColumn, "");
            System.IO.File.WriteAllText(stalePending, "");

            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var held));

            Assert.False(System.IO.File.Exists(staleColumn) || System.IO.File.Exists(stalePending));
            Assert.Single(HolderFilesBeside(dir));
            held!.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>A dead holder's file that SEVERAL testers sweep at the same instant refuses none of them: probes ask for a
    /// SHARED lock and only a holder holds the exclusive one, so a tester never reads another tester as a holder (kb/Work
    /// PB2917, the train 1052 review: exclusive probes made the second tester's OPEN '61' for a connector that did not
    /// exist, §9.1.13.9 1)). The file is recreated every round, so every round has a dead file to race over.</summary>
    [Fact]
    public void ManyTestersSweepingOneDeadFileNeverRefuseEachOther()
    {
        var (dir, file) = NewFile();
        try
        {
            string lockDir = SidecarLockFiles.LockDirectoryOf(file);
            for (int round = 0; round < 100; round++)
            {
                System.IO.Directory.CreateDirectory(lockDir);
                System.IO.File.WriteAllText(Path.Combine(lockDir, $"0-{Guid.NewGuid():N}"), "");   // NO OTHER: refuses every request row
                using var barrier = new Barrier(4);
                var outcomes = new RunUnitFileLock.Outcome[4];
                var holds = new RunUnitFileLock?[4];
                var threads = Enumerable.Range(0, 4).Select(i => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    outcomes[i] = RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.Input, out holds[i]);
                })).ToArray();
                foreach (var t in threads) t.Start();
                foreach (var t in threads) t.Join();
                foreach (var hold in holds) hold?.Dispose();
                Assert.True(outcomes.All(o => o == RunUnitFileLock.Outcome.Held),
                    $"Round {round}: a dead holder's file was read as a live one — outcomes {string.Join(", ", outcomes)}.");
            }
        }
        finally { Clean(dir); }
    }

    /// <summary>⛔ A DEAD FILE THAT TESTERS SWEEP IS NEVER READ AS UNOBSERVABLE (train 1056b). Where share modes are
    /// mandatory, an open that races a DELETE of the file it names answers "access denied" (the file is delete-pending for
    /// that instant) — the answer of a holder this process cannot observe — so a tester's OPEN was refused '61' for no
    /// holder: round 11 of <see cref="ManyTestersSweepingOneDeadFileNeverRefuseEachOther"/> under a loaded gate, and about
    /// one probe in two hundred of eight testers sweeping one file. Five quick retries narrowed the window and could not
    /// close it. The sweeper now renames the file away before deleting it (<see cref="SidecarLockFiles.Retire"/>): an open
    /// that races a rename finds the file or finds no file, never a delete-pending one, so not one probe here may answer
    /// <see cref="HostFile.LockFileState.Unobservable"/>. Every host: a Unix unlink is already atomic.</summary>
    [Fact]
    public void TestersSweepingOneDeadFileNeverReadItAsUnobservable()
    {
        var (dir, file) = NewFile();
        try
        {
            string lockDir = SidecarLockFiles.LockDirectoryOf(file);
            System.IO.Directory.CreateDirectory(lockDir);
            int unobservable = 0;
            for (int round = 0; round < 300; round++)
            {
                string dead = Path.Combine(lockDir, $"0-{Guid.NewGuid():N}");
                System.IO.File.WriteAllText(dead, "");
                using var barrier = new Barrier(8);
                var threads = Enumerable.Range(0, 8).Select(_ => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    for (int k = 0; k < 3; k++)
                        if (SidecarLockFiles.Observe(dead) == HostFile.LockFileState.Unobservable)
                            Interlocked.Increment(ref unobservable);
                })).ToArray();
                foreach (var t in threads) t.Start();
                foreach (var t in threads) t.Join();
            }
            Assert.Equal(0, unobservable);
        }
        finally { Clean(dir); }
    }

    /// <summary>⛔ A LOCK DIRECTORY THIS USER MAY ENTER BUT NOT LIST HIDES EVERY HOLDER (the train 1056b review). A parent
    /// of mode 0711, which the lock directory copies, lets another user reach the physical file and its lock directory by
    /// name but not list it, and the listing's "access denied" was read as a lock this host cannot hold: the OPEN was
    /// granted with no file lock where a live holder may be, against §9.1.15 2). It is refused ('61', §9.1.13.9 1)) and
    /// leaves nothing behind, both for a user who may not write the directory (search only, the other user of a 0711
    /// parent) and for one who may (write and search). Unix modes only; the test witnesses nothing as root.</summary>
    [Fact]
    public void ALockDirectoryThatCannotBeListedRefusesAndLeavesNothing()
    {
        if (OperatingSystem.IsWindows()) return;
        foreach (var mode in new[] { UnixFileMode.UserExecute, UnixFileMode.UserWrite | UnixFileMode.UserExecute })
        {
            var (dir, file) = NewFile();
            try
            {
                string lockDir = SidecarLockFiles.LockDirectoryOf(file);
                System.IO.Directory.CreateDirectory(lockDir);
                System.IO.File.SetUnixFileMode(lockDir, mode);
                try
                {
                    try { _ = System.IO.Directory.EnumerateFileSystemEntries(lockDir).ToList(); return; }   // root lists it: nothing to witness
                    catch (UnauthorizedAccessException) { }

                    Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var held));
                    Assert.Null(held);
                }
                finally
                {
                    if (System.IO.Directory.Exists(lockDir))
                        System.IO.File.SetUnixFileMode(lockDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                }
                Assert.True(!System.IO.Directory.Exists(lockDir) || !System.IO.Directory.EnumerateFileSystemEntries(lockDir).Any(),
                    $"Mode {mode}: the refused request left a lock file behind.");
            }
            finally { Clean(dir); }
        }
    }

    /// <summary>⛔ A HOLDER THIS PROCESS CANNOT OBSERVE IS A HOLDER, NEVER A LEFTOVER (kb/Work PB2917). A column file the
    /// process may not open was read as "nobody holds it" and swept, so another user's live holder was deleted and the OPEN
    /// §9.1.15 requires refused was granted. The entry here is a directory named like a holder file — which no host lets
    /// a file open read, on Windows and on Unix alike — so the test needs no second account: a request row that refuses
    /// its column is refused ('61', §9.1.13.9 1)) and the entry is left where it is; a request row that does not refuse
    /// its column is not affected by it.</summary>
    [Fact]
    public void AColumnFileThatCannotBeObservedRefusesAndIsNotSwept()
    {
        var (dir, file) = NewFile();
        try
        {
            string lockDir = SidecarLockFiles.LockDirectoryOf(file);
            string unreadable = Path.Combine(lockDir, $"0-{Guid.NewGuid():N}");   // NO OTHER: refuses every request row
            System.IO.Directory.CreateDirectory(unreadable);

            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var held));
            Assert.Null(held);
            Assert.True(System.IO.Directory.Exists(unreadable), "An entry the process cannot observe was swept as a leftover.");
            Assert.Equal([unreadable], System.IO.Directory.EnumerateFileSystemEntries(lockDir).ToArray());   // the refused request left nothing behind

            // Column 4 (ALL OTHER, input only) is refused by no request row that Table 19 prints as a conflict here:
            // the same entry under that column does not touch a request it does not refuse.
            System.IO.Directory.Delete(unreadable);
            string harmless = Path.Combine(lockDir, $"4-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(harmless);
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var second));
            second!.Dispose();
            System.IO.Directory.Delete(harmless);
        }
        finally { Clean(dir); }
    }

    /// <summary>The same on a real unreadable FILE — another user's under a umask of 077, as the note describes — where
    /// the host has Unix modes and the test is not running as a user the modes do not bind (root).</summary>
    [Fact]
    public void AColumnFileWithNoReadAccessRefusesAndIsNotSwept()
    {
        if (OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            string lockDir = SidecarLockFiles.LockDirectoryOf(file);
            System.IO.Directory.CreateDirectory(lockDir);
            string unreadable = Path.Combine(lockDir, $"0-{Guid.NewGuid():N}");
            System.IO.File.WriteAllText(unreadable, "");
            System.IO.File.SetUnixFileMode(unreadable, UnixFileMode.None);
            try { using var _ = new FileStream(unreadable, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); return; }   // root reads it: nothing to witness
            catch (UnauthorizedAccessException) { }

            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out _));
            Assert.True(System.IO.File.Exists(unreadable), "A holder file the process may not read was swept as a leftover.");
        }
        finally { Clean(dir); }
    }

    /// <summary>⛔ A USER WHO MAY NOT WRITE THE LOCK DIRECTORY CAN STILL LOOK (kb/Work PB2917): it cannot publish its own
    /// column, but a live holder it can see still refuses it (§9.1.15 2), '61'), and with no conflicting holder its OPEN
    /// has the share mode alone, <see cref="RunUnitFileLock.Outcome.Unavailable"/>. Unix modes only; skipped as root.</summary>
    [Fact]
    public void AUserWhoMayNotPublishStillRefusesAgainstALiveHolder()
    {
        if (OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out var live));
            string lockDir = SidecarLockFiles.LockDirectoryOf(file);
            System.IO.File.SetUnixFileMode(lockDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            try
            {
                try { using var _ = new FileStream(Path.Combine(lockDir, "probe"), FileMode.CreateNew); return; }   // root writes it: nothing to witness
                catch (UnauthorizedAccessException) { }

                // READ ONLY non-input (column 1) refuses a request in any mode but input (Table 19): refused, though nothing was published.
                Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out _));
                // An input request is not refused by it: no lock of its own can be written, the share mode is all it has.
                Assert.Equal(RunUnitFileLock.Outcome.Unavailable, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var none));
                Assert.Null(none);
            }
            finally
            {
                System.IO.File.SetUnixFileMode(lockDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                live!.Dispose();
            }
        }
        finally { Clean(dir); }
    }

    /// <summary>⛔ THE LOCK DIRECTORY AND ITS FILES CARRY THE PERMISSIONS OF THE PHYSICAL FILE'S DIRECTORY, not of the first
    /// holder's umask (kb/Work PB2917). A directory made at the default umask 022 is 0755 and its owner's alone to write,
    /// so a second user of a group-writable shared directory could not publish into it and was granted an OPEN §9.1.15 2)
    /// requires refused; a file made under a umask of 077 is 0600 and unreadable to that user. Unix modes only.</summary>
    [Fact]
    public void TheLockDirectoryAndItsFilesTakeTheSharedDirectorysPermissions()
    {
        if (OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            const UnixFileMode shared = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            System.IO.File.SetUnixFileMode(dir, shared);   // a group-writable directory two users share (0775)
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var held));

            string lockDir = SidecarLockFiles.LockDirectoryOf(file);
            Assert.Equal(shared, System.IO.File.GetUnixFileMode(lockDir));
            string holder = Assert.Single(System.IO.Directory.EnumerateFiles(lockDir));
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead,
                System.IO.File.GetUnixFileMode(holder));
            held!.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>The names are a contract between run units (two runtime versions must find each other): the lock
    /// directory is <c>.wolk-</c> and sixteen hex digits, beside the physical file, and a holder's file is its column's
    /// <see cref="RunUnitFileLock.WireNumber"/>, a dash and a GUID.</summary>
    [Fact]
    public void TheLockIsNamedByTheWireContract()
    {
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var held));
            string lockDir = Assert.Single(System.IO.Directory.EnumerateDirectories(dir));
            Assert.Matches(LockDirectoryName, Path.GetFileName(lockDir));
            Assert.Equal(lockDir, SidecarLockFiles.LockDirectoryOf(file));
            string holder = Path.GetFileName(Assert.Single(HolderFilesBeside(dir)));
            Assert.Matches(HolderFileName, holder);
            Assert.StartsWith($"{RunUnitFileLock.WireNumber(ExistingSharingColumn.AllOtherInput)}-", holder, StringComparison.Ordinal);
            held!.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>The wire numbers are the contract Linux's byte offsets and macOS's file names share; the five columns
    /// are numbered 0 to 4 in Table 19's order and no two share a number.</summary>
    [Fact]
    public void TheFiveColumnsHaveTheFiveWireNumbers()
    {
        int[] expected = [0, 1, 2, 3, 4];
        Assert.Equal(expected, Enum.GetValues<ExistingSharingColumn>().Select(RunUnitFileLock.WireNumber).Order().ToArray());
        Assert.Equal(0, RunUnitFileLock.WireNumber(ExistingSharingColumn.NoOtherAnyMode));
        Assert.Equal(4, RunUnitFileLock.WireNumber(ExistingSharingColumn.AllOtherInput));
    }

    /// <summary>Two spellings of one file name one lock directory exactly where the host's case rule
    /// (<see cref="HostFile.PhysicalFileComparer"/>) calls them one file — on macOS <c>Cust.dat</c> and <c>cust.dat</c>
    /// are one file, so two run units spelling it differently must still meet.</summary>
    [Fact]
    public void TwoSpellingsOfOneFileShareOneLockDirectory()
    {
        string a = Path.Combine(Path.GetTempPath(), "pb2484-case", "Cust.dat");
        string b = Path.Combine(Path.GetTempPath(), "pb2484-case", "cust.dat");
        Assert.Equal(HostFile.PhysicalFileComparer.Equals(a, b),
            SidecarLockFiles.LockDirectoryOf(a) == SidecarLockFiles.LockDirectoryOf(b));
        Assert.Equal(SidecarLockFiles.LockDirectoryOf(a),
            SidecarLockFiles.LockDirectoryOf(Path.Combine(Path.GetTempPath(), "pb2484-case", "x", "..", "Cust.dat")));
        Assert.NotEqual(SidecarLockFiles.LockDirectoryOf(a),
            SidecarLockFiles.LockDirectoryOf(Path.Combine(Path.GetTempPath(), "pb2484-case", "other.dat")));
    }

    /// <summary>A host that cannot put a lock beside the file — here a directory that is really a file — holds
    /// nothing and refuses nothing: the connector keeps the file lock its <see cref="FileShare"/> gives. It is an
    /// <see cref="RunUnitFileLock.Outcome.Unavailable"/>, never an exception out of an OPEN.</summary>
    [Fact]
    public void ADirectoryThatCannotHoldTheLockIsUnavailableNotAnException()
    {
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Unavailable,
                RunUnitFileLock.Take(Host, Path.Combine(file, "inner.dat"), FileSharing.NoOther, FileOpenMode.Output, out var held));
            Assert.Null(held);
        }
        finally { Clean(dir); }
    }

    /// <summary>The host of a process is chosen once, from facts of the process: Linux open-file-description locks
    /// where <c>OfdRegionLocks</c> carries them, no lock file on Windows (mandatory share modes carry the table), and
    /// the lock files everywhere else.</summary>
    [Fact]
    public void TheHostOfTheProcessIsChosenFromTheProcessShape()
    {
        object? expected = OfdRegionLocks.Available ? OfdColumnLockHost.Instance
            : OperatingSystem.IsWindows() ? null
            : SidecarColumnLockHost.Instance;
        Assert.Same(expected, RunUnitFileLock.HostOfThisProcess);
        Assert.Equal(RunUnitFileLock.Outcome.Unavailable,
            RunUnitFileLock.Take(null, "never-opened.dat", FileSharing.NoOther, FileOpenMode.Output, out var held));
        Assert.Null(held);
    }
}
