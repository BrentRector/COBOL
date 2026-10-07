// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SECOND RUN UNIT IS ARBITRATED BY TABLE 19 TOO (kb/Work PB833). ISO §9.1.15: <i>"The successful opening of a
/// file establishes a file lock for the applicable sharing rules, thereby preventing other run units from opening
/// that file with incompatible sharing rules."</i> The "applicable sharing rules" are rules 1)–3), which Table 19
/// prints cell by cell, so for EVERY (sharing mode × open mode) on the existing side crossed with every one on the
/// incoming side the second run unit's OPEN shall answer exactly what the in-run-unit arbiter answers: '61' where
/// <c>Table19.Conflicts</c> refuses, a success-family status where it permits.
/// <para>A second <see cref="FileRegistry"/> IS a second run unit here: it shares nothing with the first — not the
/// <c>PhysicalFileTable</c>, so Table 19's in-memory arbitration cannot be what answers, and not a host handle,
/// so a host share mode cannot be what answers for the host that cannot express the rule — and the file lock is
/// the only thing standing between them.</para>
/// </summary>
public sealed class RunUnitFileLockDriftTests
{
    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb833-{tag}-{Guid.NewGuid():N}.dat");

    public static TheoryData<FileSharing?, FileOpenMode, FileSharing?, FileOpenMode, FileLockPostureDriftTests.Org> EveryPair()
    {
        var data = new TheoryData<FileSharing?, FileOpenMode, FileSharing?, FileOpenMode, FileLockPostureDriftTests.Org>();
        FileSharing?[] spellings = [null, FileSharing.NoOther, FileSharing.ReadOnly, FileSharing.AllOther];
        foreach (var org in Enum.GetValues<FileLockPostureDriftTests.Org>())
            foreach (var sa in spellings)
                foreach (var ma in Enum.GetValues<FileOpenMode>())
                    foreach (var sb in spellings)
                        foreach (var mb in Enum.GetValues<FileOpenMode>())
                            data.Add(sa, ma, sb, mb, org);
        return data;
    }

    private static void Register(FileRegistry reg, string name, string host, FileSharing? sharing,
        FileLockPostureDriftTests.Org org)
    {
        switch (org)
        {
            case FileLockPostureDriftTests.Org.Relative:
                reg.RegisterRelative(name, host, recordWidth: 4, optional: false, accessMode: 0,
                    relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
                break;
            case FileLockPostureDriftTests.Org.Indexed:
                reg.RegisterIndexed(name, host, recordWidth: 4, optional: false, accessMode: 0,
                    primeOffset: 0, primeLength: 4, varyMin: -1, varyMax: -1);
                break;
            default:
                reg.Register(name, host, recordWidth: 4, lineSequential: false, optional: false,
                    varyMin: -1, varyMax: -1);
                break;
        }
        if (sharing is { } s) reg.RegisterSharing(name, s, FileLockMode.Manual, multiple: false);
    }

    [Theory]
    [MemberData(nameof(EveryPair))]
    public void ASecondRunUnitsOpenIsArbitratedByTable19(FileSharing? sa, FileOpenMode ma, FileSharing? sb,
        FileOpenMode mb, FileLockPostureDriftTests.Org org)
    {
        string host = Tmp($"{org}-{sa}{ma}-{sb}{mb}");
        try
        {
            var seed = new FileRegistry();
            Register(seed, "S", host, null, org);
            seed.OpenStatic("S", FileOpenMode.Output);
            seed.WriteShared("S", "SEED", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
            seed.Close("S");

            var runUnit1 = new FileRegistry();
            var runUnit2 = new FileRegistry();
            Register(runUnit1, "A", host, sa, org);
            Register(runUnit2, "B", host, sb, org);

            runUnit1.OpenStatic("A", ma);
            Assert.True(runUnit1.Status("A")[0] == '0',
                $"The first run unit's OPEN answered '{runUnit1.Status("A")}' — the second one cannot be measured.");

            runUnit2.OpenStatic("B", mb);
            var existing = (sa ?? FileRegistry.ImplementorDefaultSharing(ma), ma);
            var request = (sb ?? FileRegistry.ImplementorDefaultSharing(mb), mb);
            bool refused = Table19.Conflicts(request, existing);
            string got = runUnit2.Status("B");
            if (refused)
                Assert.True(got == FileStatusCode.FileSharingConflict,
                    $"Table 19 refuses {request} against {existing} ({org}) and §9.1.15 binds OTHER RUN UNITS to it, "
                    + $"but the second run unit's OPEN answered '{got}'.");
            else
                Assert.True(got[0] == '0',
                    $"Table 19 permits {request} against {existing} ({org}), but the second run unit's OPEN "
                    + $"answered '{got}'.");

            runUnit2.CloseAll();
            runUnit1.CloseAll();
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>§9.1.15: <i>"The file lock is removed by an explicit or implicit CLOSE statement executed for that
    /// file connector"</i> — so the refusal is not sticky: the CLOSE gives the lock back and the very next OPEN of
    /// the other run unit succeeds, in every organization.</summary>
    [Theory]
    [InlineData(FileLockPostureDriftTests.Org.Sequential)]
    [InlineData(FileLockPostureDriftTests.Org.Relative)]
    [InlineData(FileLockPostureDriftTests.Org.Indexed)]
    public void TheCloseGivesTheFileLockBack(FileLockPostureDriftTests.Org org)
    {
        string host = Tmp($"close-{org}");
        try
        {
            var seed = new FileRegistry();
            Register(seed, "S", host, null, org);
            seed.OpenStatic("S", FileOpenMode.Output);
            seed.Close("S");

            var runUnit1 = new FileRegistry();
            var runUnit2 = new FileRegistry();
            Register(runUnit1, "A", host, FileSharing.NoOther, org);
            Register(runUnit2, "B", host, FileSharing.AllOther, org);

            runUnit1.OpenStatic("A", FileOpenMode.IO);
            Assert.Equal(FileStatusCode.Success, runUnit1.Status("A"));
            runUnit2.OpenStatic("B", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.FileSharingConflict, runUnit2.Status("B"));   // rule 1) exclusive access

            runUnit1.Close("A");
            runUnit2.OpenStatic("B", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, runUnit2.Status("B"));
            runUnit2.CloseAll();
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>⛔ ONLY A REGULAR FILE IS LOCKED. A device node is not a file whose bytes another run unit can
    /// damage, and the lock lives on the inode: were <c>/dev/null</c> locked, every run unit that assigns it would be
    /// arbitrated against every other on the machine (concurrent test programs among them). On a host with no
    /// device of that name — or with no region lock at all — the answer is the same: nothing is held.</summary>
    [Fact]
    public void ADeviceNodeCarriesNoRegionLock()
    {
        string device = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
        Assert.Equal(RunUnitFileLock.Outcome.Unavailable,
            RunUnitFileLock.Take(device, FileSharing.NoOther, FileOpenMode.Output, out var held));
        Assert.Null(held);
    }

    /// <summary>⛔ TWO RUN UNITS OPENING AT THE SAME INSTANT CANNOT BOTH WIN. The lock is PUBLISHED before it is
    /// tested, so whichever publishes second finds the first; with a test-then-publish order both would pass and
    /// both run units would write. Two <c>SHARING WITH NO OTHER</c> opens released by one barrier, many rounds:
    /// at most one is ever a success-family status (a loser is '61', and may be both — each saw the other's
    /// publication — which is a refused OPEN, not a violated lock).</summary>
    [Fact]
    public void TwoSimultaneousExclusiveOpens_NeverBothSucceed()
    {
        string host = Tmp("race");
        try
        {
            var seed = new FileRegistry();
            Register(seed, "S", host, null, FileLockPostureDriftTests.Org.Sequential);
            seed.OpenStatic("S", FileOpenMode.Output);
            seed.Close("S");

            for (int round = 0; round < 200; round++)
            {
                var a = new FileRegistry();
                var b = new FileRegistry();
                Register(a, "A", host, FileSharing.NoOther, FileLockPostureDriftTests.Org.Sequential);
                Register(b, "B", host, FileSharing.NoOther, FileLockPostureDriftTests.Org.Sequential);
                using var barrier = new Barrier(2);
                var opened = new string[2];
                var threads = new[]
                {
                    new Thread(() => { barrier.SignalAndWait(); a.OpenStatic("A", FileOpenMode.IO); opened[0] = a.Status("A"); }),
                    new Thread(() => { barrier.SignalAndWait(); b.OpenStatic("B", FileOpenMode.IO); opened[1] = b.Status("B"); }),
                };
                foreach (var t in threads) t.Start();
                foreach (var t in threads) t.Join();

                Assert.False(opened[0][0] == '0' && opened[1][0] == '0',
                    $"Round {round}: both run units opened {host} SHARING WITH NO OTHER ('{opened[0]}' and "
                    + $"'{opened[1]}') — §9.1.15 1) 'specifies exclusive access to a physical file'.");
                a.CloseAll();
                b.CloseAll();
            }
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }
}
