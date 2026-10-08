// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ TWO RUN UNITS SHARING A RELATIVE OR INDEXED FILE UNDER <c>SHARING WITH ALL OTHER</c> SEE EACH OTHER'S RECORD
/// LOCKS AND KEEP EACH OTHER'S RECORDS (kb/Work PB2660). ISO §9.1.16: <i>"While locked by a given file connector, a
/// record is not accessible to another file connector in the same or a different run unit, except by the execution
/// of a READ statement with the IGNORING LOCK phrase"</i>; §9.1.13.8 1): <i>"I-O status = 51. The input-output
/// statement is unsuccessful due to an attempt to access a record that is currently locked by another file
/// connector"</i>; §9.1.15 3): <i>"The sharing with all other mode allows concurrent access to a physical file
/// through other file connectors specifying input, I-O, or extend mode … Record locks are in effect."</i>
/// <para>Before PB2660 a keyed connector read the whole file at OPEN and rewrote it at CLOSE, and its record locks
/// lived in its own run unit's table, so a second run unit's READ WITH LOCK of a record the first held answered
/// '00', its REWRITE and WRITE answered '00', and the first run unit's CLOSE then erased them. A second
/// <see cref="FileRegistry"/> IS a second run unit here: it shares no table and no handle with the first
/// (<see cref="RunUnitFileLockDriftTests"/>), so only what the physical file and the host's byte-range locks carry
/// can pass between them.</para>
/// <para>The PB754 half: emptying a keyed store is done in ONE place, <see cref="KeyedStoreTable.AttachCreated{T}"/>,
/// which refuses a store another connector still holds — no connector clears one itself.</para>
/// </summary>
public sealed class CrossRunUnitKeyedStoreDriftTests
{
    public enum Org { Relative, Indexed }

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb2660-{tag}-{Guid.NewGuid():N}.dat");

    /// <summary>Whether this host carries the byte-range locks the cross-run-unit half rests on (Windows; Linux
    /// x86-64/arm64). Elsewhere the run unit has only its own guarantees (docs/CONFORMANCE.md DOC-A.1-75).</summary>
    private static bool HostCarriesRegionLocks => OperatingSystem.IsWindows() || OfdRegionLocks.Available;

    private static void Register(FileRegistry reg, string name, string host, Org org, bool optional = false,
        FileSharing? sharing = FileSharing.AllOther)
    {
        if (org == Org.Relative)
            reg.RegisterRelative(name, host, recordWidth: 8, optional: optional, accessMode: (int)KeyedAccess.Dynamic,
                relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
        else
            reg.RegisterIndexed(name, host, recordWidth: 8, optional: optional, accessMode: (int)KeyedAccess.Dynamic,
                primeOffset: 0, primeLength: 4, varyMin: -1, varyMax: -1);
        if (sharing is { } s) reg.RegisterSharing(name, s, FileLockMode.Manual, multiple: false);
    }

    /// <summary>Record <paramref name="n"/>'s image: its key (also its relative record number) and a payload.</summary>
    private static string Rec(int n, string payload) => $"{n:0000}{payload}";

    private static void Position(FileRegistry reg, string name, Org org, int n)
    {
        if (org == Org.Relative) reg.SetRelativeKey(name, n);
    }

    private static string ReadKeyed(FileRegistry reg, string name, Org org, int n, FileRecordLock phrase,
        out string image, bool ignoringLock = false, FileRetryKind retry = FileRetryKind.None, long amount = 0)
    {
        Position(reg, name, org, n);
        return reg.ReadKeyedShared(name, keyIndex: -1, Rec(n, "    "), phrase, ignoringLock, retry, amount, out image);
    }

    private static string Write(FileRegistry reg, string name, Org org, int n, string payload)
    {
        Position(reg, name, org, n);
        return reg.WriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
    }

    private static string Rewrite(FileRegistry reg, string name, Org org, int n, string payload)
    {
        Position(reg, name, org, n);
        return reg.RewriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0);
    }

    private static void Seed(string host, Org org)
    {
        var seed = new FileRegistry();
        Register(seed, "S", host, org, sharing: null);
        seed.OpenStatic("S", FileOpenMode.Output);
        Assert.Equal("00", Write(seed, "S", org, 1, "AAAA"));
        Assert.Equal("00", Write(seed, "S", org, 2, "BBBB"));
        seed.Close("S");
    }

    private static List<string> Records(string host, Org org)
    {
        var reader = new FileRegistry();
        Register(reader, "R", host, org, sharing: null);
        reader.OpenStatic("R", FileOpenMode.Input);
        var all = new List<string>();
        while (reader.ReadShared("R", previous: false, FileRecordLock.None, advancingOnLock: false, ignoringLock: false,
                   FileRetryKind.None, 0, out string image) == "00")
            all.Add(image);
        reader.Close("R");
        return all;
    }

    [Theory]
    [InlineData(Org.Relative)]
    [InlineData(Org.Indexed)]
    public void AnotherRunUnitsRecordLock_Answers51_AndItsUpdatesSurviveTheFirstClose(Org org)
    {
        if (!HostCarriesRegionLocks) return;
        string host = Tmp($"lock-{org}");
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, org);
            Register(b, "B", host, org);
            a.OpenStatic("A", FileOpenMode.IO);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));

            Assert.Equal("00", ReadKeyed(a, "A", org, 1, FileRecordLock.WithLock, out _));

            // §9.1.16 / §9.1.13.8 1) — the record A holds is not accessible to B, in another run unit.
            Assert.Equal("51", ReadKeyed(b, "B", org, 1, FileRecordLock.WithLock, out _));
            Assert.Equal("51", Rewrite(b, "B", org, 1, "ZZZZ"));
            // §14.9.30.4 GR12 — IGNORING LOCK makes it available "even if it is locked".
            Assert.Equal("00", ReadKeyed(b, "B", org, 1, FileRecordLock.None, out string ignored, ignoringLock: true));
            Assert.Equal(Rec(1, "AAAA"), ignored);
            // A record nobody holds is B's to change, and A sees the change at its next statement.
            Assert.Equal("00", Write(b, "B", org, 3, "CCCC"));
            Assert.Equal("00", Rewrite(b, "B", org, 2, "YYYY"));
            Assert.Equal("00", ReadKeyed(a, "A", org, 3, FileRecordLock.None, out string seen));
            Assert.Equal(Rec(3, "CCCC"), seen);

            // A's UNLOCK gives the record back to every run unit.
            a.Unlock("A", records: true);
            Assert.Equal("00", ReadKeyed(b, "B", org, 1, FileRecordLock.WithLock, out _));
            Assert.Equal("00", Rewrite(b, "B", org, 1, "XXXX"));

            a.Close("A");   // A's CLOSE must not write back the image A read at its OPEN
            b.Close("B");
            Assert.Equal([Rec(1, "XXXX"), Rec(2, "YYYY"), Rec(3, "CCCC")], Records(host, org));
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>A READ WITH LOCK through an <c>OPEN INPUT</c> connector — a read-only handle — sets its lock (§14.9.30.4
    /// GR11 c), §12.4.5.9.4: record locks are in effect under SHARING WITH ALL OTHER, §9.1.15 3)) and publishes it to
    /// the other run units. The read-only handle holds the record's byte SHARED and then asks whether anybody else holds
    /// it; the question must not count the asker's own hold, which on Windows (per-handle LockFileEx) an exclusive test
    /// lock through the same handle does, so the lock was refused with '51' by nobody (lander review, train 1042).</summary>
    [Theory]
    [InlineData(Org.Relative)]
    [InlineData(Org.Indexed)]
    public void AnInputConnectorsReadWithLock_Succeeds_AndIsSeenByAnotherRunUnit(Org org)
    {
        if (!HostCarriesRegionLocks) return;
        string host = Tmp($"input-lock-{org}");
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, org);
            Register(b, "B", host, org);
            a.OpenStatic("A", FileOpenMode.Input);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));

            Assert.Equal("00", ReadKeyed(a, "A", org, 1, FileRecordLock.WithLock, out string image));
            Assert.Equal(Rec(1, "AAAA"), image);
            Assert.Equal("51", ReadKeyed(b, "B", org, 1, FileRecordLock.WithLock, out _));
            Assert.Equal("00", ReadKeyed(b, "B", org, 2, FileRecordLock.WithLock, out _));

            a.Close("A");
            Assert.Equal("00", ReadKeyed(b, "B", org, 1, FileRecordLock.WithLock, out _));
            b.Close("B");
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>§14.7.9.3 — a RETRY against a holder in ANOTHER run unit waits for it to give the lock back, and the
    /// wait happens OUTSIDE the store's statement mutex: the holder can only give the lock back by executing a
    /// statement of its own, which the mutex would otherwise keep from ever running. RETRY FOREVER against such a
    /// holder is not the deadlock docs/CONFORMANCE.md DOC-A.1-109 detects: the holder can release, so the wait goes
    /// on until it does.</summary>
    [Theory]
    [InlineData(Org.Relative, FileRetryKind.Times)]
    [InlineData(Org.Indexed, FileRetryKind.Times)]
    [InlineData(Org.Relative, FileRetryKind.Forever)]
    [InlineData(Org.Indexed, FileRetryKind.Forever)]
    public void ARetryWaitsForTheOtherRunUnit_OutsideTheStoreMutex(Org org, FileRetryKind kind)
    {
        if (!HostCarriesRegionLocks) return;
        string host = Tmp($"retry-{org}");
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, org);
            Register(b, "B", host, org);
            a.OpenStatic("A", FileOpenMode.IO);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", ReadKeyed(a, "A", org, 1, FileRecordLock.WithLock, out _));

            int pauses = 0;
            FileRegistry.PauseObserver.Value = _ =>
            {
                using var probe = new FileStream(host, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                Assert.NotEqual(OfdRegionLocks.Result.Held,
                    HostRegionLocks.HeldByAnother(probe.SafeFileHandle, HostRegionLocks.StoreMutexByte));
                if (++pauses == 2) a.Unlock("A", records: true);   // the first re-attempt still meets the lock
            };
            try
            {
                Assert.Equal("00", ReadKeyed(b, "B", org, 1, FileRecordLock.WithLock, out string image,
                    retry: kind, amount: 3));
                Assert.Equal(Rec(1, "AAAA"), image);
            }
            finally { FileRegistry.PauseObserver.Value = null; }
            Assert.Equal(2, pauses);
            a.Close("A");
            b.Close("B");
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>kb/Work PB754 — Table 19 refuses an OUTPUT open beside any other connector (§9.1.13.9 1) e)), and
    /// the refusal precedes the OPEN body: the existing connector keeps every record. The OPTIONAL-absent shape is
    /// the one place a connector is attached with a status other than '00', and it is registered all the same.</summary>
    [Theory]
    [InlineData(Org.Relative)]
    [InlineData(Org.Indexed)]
    public void AnOutputOpenBesideAnAttachedConnector_IsRefused_AndEmptiesNothing(Org org)
    {
        string host = Tmp($"output-{org}");
        try
        {
            Seed(host, org);
            foreach (var mode in new[] { FileOpenMode.Input, FileOpenMode.IO })
            {
                var reg = new FileRegistry();
                Register(reg, "A", host, org);
                Register(reg, "B", host, org);
                reg.OpenStatic("A", mode);
                Assert.Equal("00", reg.Status("A"));
                reg.OpenStatic("B", FileOpenMode.Output);
                Assert.Equal("61", reg.Status("B"));
                Assert.Equal("00", ReadKeyed(reg, "A", org, 2, FileRecordLock.None, out string image));
                Assert.Equal(Rec(2, "BBBB"), image);
                reg.CloseAll();
            }

            string absent = Tmp($"absent-{org}");
            var opt = new FileRegistry();
            Register(opt, "A", absent, org, optional: true);
            Register(opt, "B", absent, org, optional: true);
            opt.OpenStatic("A", FileOpenMode.Input);
            Assert.Equal("05", opt.Status("A"));
            opt.OpenStatic("B", FileOpenMode.Output);
            Assert.Equal("61", opt.Status("B"));
            opt.CloseAll();
            try { File.Delete(absent); } catch (IOException) { }
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>The guard behind the arbitration fires when the arbitration is bypassed: a store being created while
    /// another connector holds it is a loud defect, never a silent emptying (kb/Work PB754).</summary>
    [Fact]
    public void CreatingAStoreAnotherConnectorHolds_IsADefect()
    {
        var table = new KeyedStoreTable();
        table.AttachRelative("pb754-host", _ => { });
        Assert.Throws<InvalidOperationException>(() => table.AttachCreated<RelativeStore>("pb754-host"));
        table.Detach("pb754-host");
        Assert.Empty(table.AttachCreated<RelativeStore>("pb754-host").Slots);
    }

    /// <summary>No connector empties a keyed store itself: the ONE emptying is <c>KeyedStoreTable.AttachCreated</c>,
    /// and the only other <c>Clear()</c> is the organization's <c>Fill</c> — a load or a reload from the physical
    /// file, which every attached connector must see (kb/Work PB754).</summary>
    [Fact]
    public void NoConnectorClearsASharedStore()
    {
        foreach (var file in new[] { "RelativeConnector.cs", "IndexedConnector.cs", "KeyedConnector.cs" })
        {
            string[] lines = File.ReadAllLines(TestRepo.Src("Cobol.Net.Runtime", "IO", file));
            var clears = lines.Select((l, i) => (l, i)).Where(x => x.l.Contains(".Clear()", StringComparison.Ordinal)
                && !x.l.TrimStart().StartsWith("//", StringComparison.Ordinal)).ToList();
            foreach (var (line, at) in clears)
                Assert.True(line.Trim() == "store.Clear();",
                    $"{file}:{at + 1} clears a store outside Fill: '{line.Trim()}' — emptying a shared keyed store is "
                    + "KeyedStoreTable.AttachCreated's alone (kb/Work PB754).");
        }
    }
}
