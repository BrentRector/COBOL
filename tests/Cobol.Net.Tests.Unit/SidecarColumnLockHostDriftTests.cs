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
            string lockDir = SidecarColumnLockHost.LockDirectoryOf(file);
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

    /// <summary>A killed holder's leftover file that ANOTHER tester holds exclusively while it sweeps it refuses nobody
    /// either: only a tester ever takes the exclusive lock, and only on a dead file, so the request is not refused '61'
    /// for a connector that does not exist (§9.1.13.9 1); the train 1052 review). The sweeping tester is this test's own
    /// <see cref="FileShare.None"/> handle.</summary>
    [Fact]
    public void ALeftoverFileUnderAnotherTestersSweepRefusesNobody()
    {
        var (dir, file) = NewFile();
        try
        {
            string lockDir = SidecarColumnLockHost.LockDirectoryOf(file);
            System.IO.Directory.CreateDirectory(lockDir);
            string staleColumn = Path.Combine(lockDir, $"0-{Guid.NewGuid():N}");   // NO OTHER: refuses every request row
            System.IO.File.WriteAllText(staleColumn, "");
            using (new FileStream(staleColumn, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.Input, out var held));
                held!.Dispose();
            }
            // A LIVE holder of the same column still refuses the same request: the sweep arm is the tester's alone.
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.NoOther, FileOpenMode.IO, out var live));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.Input, out _));
            live!.Dispose();
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
            Assert.Equal(lockDir, SidecarColumnLockHost.LockDirectoryOf(file));
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
            SidecarColumnLockHost.LockDirectoryOf(a) == SidecarColumnLockHost.LockDirectoryOf(b));
        Assert.Equal(SidecarColumnLockHost.LockDirectoryOf(a),
            SidecarColumnLockHost.LockDirectoryOf(Path.Combine(Path.GetTempPath(), "pb2484-case", "x", "..", "Cust.dat")));
        Assert.NotEqual(SidecarColumnLockHost.LockDirectoryOf(a),
            SidecarColumnLockHost.LockDirectoryOf(Path.Combine(Path.GetTempPath(), "pb2484-case", "other.dat")));
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
