// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ TWO RUN UNITS SHARING A SEQUENTIAL FILE UNDER <c>SHARING WITH ALL OTHER</c> SEE EACH OTHER'S RECORD LOCKS, NAME
/// EACH RECORD ALIKE AND READ THE RECORDS THE FILE HOLDS NOW (kb/Work PB2692, the sequential arm of PB2660). ISO
/// §9.1.16: <i>"While locked by a given file connector, a record is not accessible to another file connector in the
/// same or a different run unit, except by the execution of a READ statement with the IGNORING LOCK phrase"</i>;
/// §9.1.13.8 1): <i>"I-O status = 51. The input-output statement is unsuccessful due to an attempt to access a record
/// that is currently locked by another file connector"</i>.
/// <para>Before PB2692 the sequential connector published no record lock (it had no <c>RecordLockHandle</c>), so a
/// second run unit's READ WITH LOCK of a record the first held answered '00'; its reader served a record another run
/// unit had rewritten from its read-ahead buffer; and a sharing writer numbered its released records from a mint only
/// its own run unit advanced, so after another run unit's append its lock guarded a different record. A second
/// <see cref="FileRegistry"/> IS a second run unit here (<see cref="CrossRunUnitKeyedStoreDriftTests"/>).</para>
/// </summary>
public sealed class CrossRunUnitSequentialLockDriftTests
{
    public enum Framing { Fixed, Line, Varying }

    private static bool HostCarriesRegionLocks => OperatingSystem.IsWindows() || OfdRegionLocks.Available;

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb2692-{tag}-{Guid.NewGuid():N}.dat");

    private static void Register(FileRegistry reg, string name, string host, Framing framing, bool shared = true)
    {
        reg.Register(name, host, recordWidth: 4, lineSequential: framing == Framing.Line, optional: false,
            varyMin: framing == Framing.Varying ? 1 : -1, varyMax: framing == Framing.Varying ? 4 : -1);
        if (shared) reg.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, multiple: false);
    }

    private static string Read(FileRegistry reg, string name, FileRecordLock phrase, out string image,
        bool ignoringLock = false) =>
        reg.ReadShared(name, previous: false, phrase, advancingOnLock: false, ignoringLock, FileRetryKind.None, 0, out image);

    private static string Write(FileRegistry reg, string name, string image, FileRecordLock phrase = FileRecordLock.None) =>
        reg.WriteShared(name, image, -1, phrase, FileRetryKind.None, 0, page: null);

    private static void Seed(string host, Framing framing, params string[] records)
    {
        var seed = new FileRegistry();
        Register(seed, "S", host, framing, shared: false);
        seed.OpenStatic("S", FileOpenMode.Output);
        foreach (string r in records) Assert.Equal("00", Write(seed, "S", r));
        seed.Close("S");
    }

    private static List<string> Records(string host, Framing framing)
    {
        var reader = new FileRegistry();
        Register(reader, "R", host, framing, shared: false);
        reader.OpenStatic("R", FileOpenMode.Input);
        var all = new List<string>();
        while (Read(reader, "R", FileRecordLock.None, out string image) == "00") all.Add(image);
        reader.Close("R");
        return all;
    }

    /// <summary>A record one run unit holds is '51' to the other's READ WITH LOCK and REWRITE, IGNORING LOCK still
    /// reads it, and the holder's UNLOCK gives it to the other run unit — which then rewrites the record the FILE
    /// holds, and the first run unit's next READ sees that rewrite rather than its own read-ahead.</summary>
    [Theory]
    [InlineData(Framing.Fixed)]
    [InlineData(Framing.Line)]
    [InlineData(Framing.Varying)]
    public void AnotherRunUnitsRecordLock_Answers51_AndItsRewriteIsReadFromTheFile(Framing framing)
    {
        if (!HostCarriesRegionLocks) return;
        string host = Tmp($"lock-{framing}");
        try
        {
            Seed(host, framing, "AAAA", "BBBB", "CCCC");
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, framing);
            Register(b, "B", host, framing);
            a.OpenStatic("A", FileOpenMode.IO);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));

            Assert.Equal("00", Read(a, "A", FileRecordLock.WithLock, out string first));   // A's buffer now holds the file
            Assert.Equal("AAAA", first);
            // §9.1.16 / §9.1.13.8 1) — record 1 is not accessible to B, in another run unit.
            Assert.Equal("51", Read(b, "B", FileRecordLock.WithLock, out _));
            // §14.9.30.4 GR12 — IGNORING LOCK makes it available "even if it is locked".
            Assert.Equal("00", Read(b, "B", FileRecordLock.None, out string ignored, ignoringLock: true));
            Assert.Equal("AAAA", ignored);
            // Record 2 nobody holds: B rewrites it, and A's next READ delivers the record the file holds now.
            Assert.Equal("00", Read(b, "B", FileRecordLock.WithLock, out _));
            Assert.Equal("00", b.RewriteShared("B", "YYYY", -1, FileRecordLock.None, FileRetryKind.None, 0));
            Assert.Equal("00", Read(a, "A", FileRecordLock.None, out string second));
            Assert.Equal("YYYY", second);

            a.Close("A");   // the CLOSE gives A's locks back to every run unit
            b.Close("B");
            var c = new FileRegistry();
            Register(c, "C", host, framing);
            c.OpenStatic("C", FileOpenMode.IO);
            Assert.Equal("00", Read(c, "C", FileRecordLock.WithLock, out _));
            c.Close("C");
            Assert.Equal(["AAAA", "YYYY", "CCCC"], Records(host, framing));
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>A sharing writer's WRITE WITH LOCK guards the record at its PHYSICAL place: after another run unit
    /// has appended, the record is the file's fourth, so another run unit's READ WITH LOCK of record 4 answers '51'
    /// and of record 3 (the other appender's) '00'. A mint advanced only by the writer's own run unit called it 3.</summary>
    [Theory]
    [InlineData(Framing.Fixed)]
    [InlineData(Framing.Line)]
    [InlineData(Framing.Varying)]
    public void ASharingWritersLock_GuardsTheRecordAtItsPhysicalPlace(Framing framing)
    {
        if (!HostCarriesRegionLocks) return;
        string host = Tmp($"write-{framing}");
        try
        {
            Seed(host, framing, "AAAA", "BBBB");
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, framing);
            Register(b, "B", host, framing);
            a.OpenStatic("A", FileOpenMode.Extend);
            b.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));
            Assert.Equal("00", Write(a, "A", "CCCC"));                            // record 3, A's run unit
            Assert.Equal("00", Write(b, "B", "DDDD", FileRecordLock.WithLock));   // record 4, locked by B

            var c = new FileRegistry();
            Register(c, "C", host, framing);
            c.OpenStatic("C", FileOpenMode.IO);
            for (int n = 1; n <= 3; n++) Assert.Equal("00", Read(c, "C", FileRecordLock.WithLock, out _));
            Assert.Equal("51", Read(c, "C", FileRecordLock.WithLock, out _));

            b.Close("B");
            Assert.Equal("00", Read(c, "C", FileRecordLock.WithLock, out string fourth));
            Assert.Equal("DDDD", fourth);
            a.Close("A");
            c.Close("C");
            Assert.Equal(["AAAA", "BBBB", "CCCC", "DDDD"], Records(host, framing));
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }
}
