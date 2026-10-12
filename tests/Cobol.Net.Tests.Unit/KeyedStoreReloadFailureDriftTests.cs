// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A COHERENT STATEMENT'S RELOAD OF THE STORE HAS ONLY I-O STATUSES AS OUTCOMES — never an escaping exception
/// (kb/Work PB2842). ISO §9.1.13.6 1): <i>"A permanent error exists and no further information is available"</i>
/// is I-O status '30'. A keyed connector whose file another run unit may write (<c>SHARING WITH ALL OTHER</c>) reloads
/// its store at the start of a statement when the file's generation stamp moved
/// (<c>KeyedConnector.BeginStatement</c>); a file that has meanwhile grown past
/// <see cref="RecordFraming.MaxStoreBytes"/> is not one the connector can read, and the reload used to let that
/// <see cref="IOException"/> out of the READ, WRITE, REWRITE, DELETE or START — the OPEN's mapping to '30' had no twin
/// in the statement. The refusal is a permanent error (§9.1.13.1: <i>"The permanent error condition remains in
/// effect for all subsequent input-output operations on the file"</i>), so the statement runs its ordinary
/// permanent-error replay — position invalidation for READ and START included — and touches no record of the stale
/// store; a CLOSE ends the condition.
/// </summary>
public sealed class KeyedStoreReloadFailureDriftTests
{
    public enum Org { Relative, Indexed }

    public enum Verb { ReadNext, ReadRandom, Write, Rewrite, Delete, StartFirstLast, StartKey, PlainReadNext, PlainWrite }

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb2842-{tag}-{Guid.NewGuid():N}.dat");

    private static void Register(FileRegistry reg, string name, string host, Org org, bool shared = true)
    {
        if (org == Org.Relative)
            reg.RegisterRelative(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
        else
            reg.RegisterIndexed(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                primeOffset: 0, primeLength: 4, varyMin: -1, varyMax: -1);
        if (shared) reg.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, multiple: false);
    }

    private static string Rec(int n, string payload) => $"{n:0000}{payload}";

    private static string Write(FileRegistry reg, string name, Org org, int n, string payload)
    {
        if (org == Org.Relative) reg.SetRelativeKey(name, n);
        return reg.WriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
    }

    private static void Seed(string host, Org org)
    {
        var seed = new FileRegistry();
        Register(seed, "S", host, org, shared: false);
        seed.OpenStatic("S", FileOpenMode.Output);
        Assert.Equal("00", Write(seed, "S", org, 1, "AAAA"));
        Assert.Equal("00", Write(seed, "S", org, 2, "BBBB"));
        seed.Close("S");
    }

    /// <summary>Make the physical file longer than any store the format describes while leaving its header — and so
    /// its generation stamp — intact. Sparse where the host's file system supports it.</summary>
    private static void GrowPastTheStoreCapacity(string host)
    {
        using var handle = File.OpenHandle(host, FileMode.Open, FileAccess.Write,
            FileShare.ReadWrite | FileShare.Delete);
        RandomAccess.SetLength(handle, RecordFraming.MaxStoreBytes + 1);
    }

    private static string Run(FileRegistry reg, Verb verb, Org org, out string image)
    {
        image = "";
        if (org == Org.Relative && verb is not Verb.StartFirstLast) reg.SetRelativeKey("A", 2);
        switch (verb)
        {
            case Verb.ReadNext:
                return reg.ReadShared("A", previous: false, FileRecordLock.None, advancingOnLock: false,
                    ignoringLock: false, FileRetryKind.None, 0, out image);
            case Verb.ReadRandom:
                return reg.ReadKeyedShared("A", keyIndex: -1, Rec(2, "    "), FileRecordLock.None,
                    ignoringLock: false, FileRetryKind.None, 0, out image);
            case Verb.Write:
                return reg.WriteShared("A", Rec(9, "ZZZZ"), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
            case Verb.Rewrite:
                return reg.RewriteShared("A", Rec(2, "ZZZZ"), -1, FileRecordLock.None, FileRetryKind.None, 0);
            case Verb.Delete:
                return reg.DeleteShared("A", Rec(2, "    "), FileRetryKind.None, 0);
            case Verb.StartFirstLast:
                return reg.StartFirstLast("A", last: false);
            case Verb.StartKey:
                return org == Org.Relative
                    ? reg.StartRelative("A", ">=", 2)
                    : reg.StartIndexed("A", keyIndex: -1, ">=", Rec(2, "    "), StartKeyLength.OfWidth(4));
            case Verb.PlainReadNext:
                return reg.ReadKeyedNext("A", out image);
            case Verb.PlainWrite:
                return reg.WriteKeyed("A", Rec(9, "ZZZZ"), -1);
            default:
                throw new ArgumentOutOfRangeException(nameof(verb));
        }
    }

    public static TheoryData<Org, Verb, RegionHostKind> Cases()
    {
        var data = new TheoryData<Org, Verb, RegionHostKind>();
        foreach (var org in Enum.GetValues<Org>())
            foreach (var verb in Enum.GetValues<Verb>())
                data.Add(org, verb, RegionHostKind.OfThisProcess);
        // The lock-file host carries the store mutex differently; one verb per organization is enough to cross it.
        foreach (var org in Enum.GetValues<Org>())
            data.Add(org, Verb.ReadNext, RegionHostKind.LockFiles);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void AStoreReloadTheFileNoLongerAllows_IsAPermanentError30_NotAnEscapingException(
        Org org, Verb verb, RegionHostKind regionHost)
    {
        using var forced = RegionHostSeam.Use(regionHost);
        string host = Tmp($"{org}-{verb}");
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

            // Another run unit moves the file's generation, then the file outgrows the format's capacity.
            Assert.Equal("00", Write(b, "B", org, 3, "CCCC"));
            GrowPastTheStoreCapacity(host);

            string status = Run(a, verb, org, out string image);
            Assert.Equal("30", status);
            Assert.Equal("30", a.Status("A"));
            Assert.Equal("", image.Trim());   // no record was delivered (the content is undefined, §14.9.30.4 GR18)

            // §9.1.13.1 — the condition remains in effect for every later statement, and the statement is not retried
            // into success by the stale store.
            Assert.Equal("30", Run(a, Verb.ReadNext, org, out image));
            Assert.Equal("", image.Trim());   // no record was delivered (the content is undefined, §14.9.30.4 GR18)

            // A CLOSE ends the condition (docs/CONFORMANCE.md DOC-A.1-105) and a connector that changed nothing writes
            // nothing back over the other run unit's file.
            a.Close("A");
            Assert.Equal("00", a.Status("A"));
            b.Close("B");
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>The RETRY wait leaves the store mutex and re-enters it (<c>KeyedConnector.WaitOutsideStatement</c>), and
    /// the re-entry reloads like any statement's entry: a reload it cannot make is the same permanent error, not an
    /// exception out of the wait.</summary>
    [Theory]
    [InlineData(Org.Relative)]
    [InlineData(Org.Indexed)]
    public void ARetryWaitsReentryThatCannotReload_IsAPermanentError30(Org org)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
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
            if (org == Org.Relative) a.SetRelativeKey("A", 1);
            Assert.Equal("00", a.ReadKeyedShared("A", -1, Rec(1, "    "), FileRecordLock.WithLock,
                ignoringLock: false, FileRetryKind.None, 0, out _));

            FileRegistry.PauseObserver.Value = _ =>
            {
                Assert.Equal("00", Write(a, "A", org, 3, "CCCC"));   // moves the generation
                GrowPastTheStoreCapacity(host);
                a.Unlock("A", records: true);                          // the conflict is over
            };
            try
            {
                if (org == Org.Relative) b.SetRelativeKey("B", 1);
                Assert.Equal("30", b.ReadKeyedShared("B", -1, Rec(1, "    "), FileRecordLock.WithLock,
                    ignoringLock: false, FileRetryKind.Times, 3, out string image));
                Assert.Equal("", image.Trim());   // no record was delivered (the content is undefined, §14.9.30.4 GR18)
                Assert.Equal("30", b.Status("B"));
            }
            finally { FileRegistry.PauseObserver.Value = null; }
            b.Close("B");
            a.Close("A");
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }
}
