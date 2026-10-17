// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §14.9.27.4 GR8's Table 20 leaves the WRITE cell BLANK where the access mode is random or dynamic and the
/// open mode is extend, and §9.1.13.7 item 8 b) names the status for that intersection — <i>"If the access mode
/// is dynamic or random, the file connector is not open in the I-O or output mode"</i> → '48'. That state is
/// UNREACHABLE from conforming source (§14.9.27.3 SR2 confines the EXTEND phrase to sequential access), so no
/// conformance golden can reach it: the reachable cells of Table 20 ride
/// conformance:cases/9.1.13.2-Successful-completion/l1_table20_seq_relative and conformance:cases/14.9.27-OPEN/l1_table20_indexed, and these four facts reach
/// the arm they cannot.
///
/// They are the regression net for kb/Work PB325: both keyed connectors opened their WRITE with
/// <c>_access == KeyedAccess.Sequential || Mode == FileOpenMode.Extend</c>, and that second disjunct routed a
/// random- or dynamic-access connector into the sequential-release branch, whose own screen accepts extend —
/// so the WRITE succeeded and appended a record. Removing it makes the runtime answer item 8 b) on its own,
/// without depending on a bind-time screen holding (see <see cref="KeyedConnector"/>).
///
/// Pinned over a PRIVATE <see cref="FileRegistry"/> instance (no shared static state, so these parallelize).
/// </summary>
public sealed class Table20WriteOpenModeTests
{
    private const int SeqAccess = 0;      // KeyedAccess.Sequential
    private const int RandomAccess = 1;   // KeyedAccess.Random
    private const int DynamicAccess = 2;  // KeyedAccess.Dynamic

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb325-{tag}-{Guid.NewGuid():N}.dat");

    /// <summary>A relative connector at <paramref name="access"/> holding one record at RRN 1, closed.</summary>
    private static (FileRegistry reg, string host) SeededRelative(int access)
    {
        var reg = new FileRegistry();
        string host = Tmp("rel");
        reg.RegisterRelative("F", host, 8, false, access, 4, -1, -1);
        reg.OpenStatic("F", FileOpenMode.Output);
        reg.SetRelativeKey("F", 1);
        Assert.Equal(FileStatusCode.Success, reg.WriteKeyed("F", "AAAAAAAA", -1));
        reg.Close("F");
        return (reg, host);
    }

    /// <summary>An indexed connector at <paramref name="access"/> holding one record keyed K001, closed.</summary>
    private static (FileRegistry reg, string host) SeededIndexed(int access)
    {
        var reg = new FileRegistry();
        string host = Tmp("idx");
        reg.RegisterIndexed("F", host, 8, false, access, 0, 4, -1, -1);
        reg.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal(FileStatusCode.Success, reg.WriteKeyed("F", "K001AAAA", -1));
        reg.Close("F");
        return (reg, host);
    }

    /// <summary>The record count reachable through a sequential-access reader over the same physical file.</summary>
    private static int CountRelative(string host)
    {
        var reg = new FileRegistry();
        reg.RegisterRelative("C", host, 8, false, SeqAccess, 4, -1, -1);
        reg.OpenStatic("C", FileOpenMode.Input);
        int n = 0;
        while (reg.ReadKeyedNext("C", out _) == FileStatusCode.Success) n++;
        reg.Close("C");
        return n;
    }

    private static int CountIndexed(string host)
    {
        var reg = new FileRegistry();
        reg.RegisterIndexed("C", host, 8, false, SeqAccess, 0, 4, -1, -1);
        reg.OpenStatic("C", FileOpenMode.Input);
        int n = 0;
        while (reg.ReadKeyedNext("C", out _) == FileStatusCode.Success) n++;
        reg.Close("C");
        return n;
    }

    // Table 20, Random × WRITE × Extend — blank; §9.1.13.7 8 b) → '48'. The record must NOT be released:
    // before PB325 the sequential branch appended it at the highest RRN + 1 and reported '00'.
    [Theory]
    [InlineData(RandomAccess)]
    [InlineData(DynamicAccess)]
    public void RelativeWrite_ExtendModeAtNonSequentialAccess_Is48_AndReleasesNothing(int access)
    {
        var (reg, host) = SeededRelative(access);
        try
        {
            reg.OpenStatic("F", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, reg.Status("F"));   // the OPEN itself is not what item 8 screens
            reg.SetRelativeKey("F", 9);
            Assert.Equal(FileStatusCode.WriteNotOpenForOutput, reg.WriteKeyed("F", "BBBBBBBB", -1));
            reg.Close("F");
            Assert.Equal(1, CountRelative(host));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    [Theory]
    [InlineData(RandomAccess)]
    [InlineData(DynamicAccess)]
    public void IndexedWrite_ExtendModeAtNonSequentialAccess_Is48_AndReleasesNothing(int access)
    {
        var (reg, host) = SeededIndexed(access);
        try
        {
            reg.OpenStatic("F", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, reg.Status("F"));
            Assert.Equal(FileStatusCode.WriteNotOpenForOutput, reg.WriteKeyed("F", "K009BBBB", -1));
            reg.Close("F");
            Assert.Equal(1, CountIndexed(host));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // The complement, so the fix cannot be "always 48": Table 20's Sequential × WRITE × Extend IS an X, and
    // §14.9.51.4 GR29 a) gives the released record the highest existing RRN + 1.
    [Fact]
    public void RelativeWrite_ExtendModeAtSequentialAccess_Succeeds_AndAppends()
    {
        var (reg, host) = SeededRelative(SeqAccess);
        try
        {
            reg.OpenStatic("F", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, reg.WriteKeyed("F", "BBBBBBBB", -1));
            reg.Close("F");
            Assert.Equal(2, CountRelative(host));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    [Fact]
    public void IndexedWrite_ExtendModeAtSequentialAccess_Succeeds_AndAppends()
    {
        var (reg, host) = SeededIndexed(SeqAccess);
        try
        {
            reg.OpenStatic("F", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, reg.WriteKeyed("F", "K009BBBB", -1));
            reg.Close("F");
            Assert.Equal(2, CountIndexed(host));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // Item 8 b)'s permitted pair, at both non-sequential access modes: Table 20's Random/Dynamic × WRITE × I-O
    // is an X — the arm the '48' above must not swallow.
    [Theory]
    [InlineData(RandomAccess)]
    [InlineData(DynamicAccess)]
    public void RelativeWrite_IoModeAtNonSequentialAccess_Succeeds(int access)
    {
        var (reg, host) = SeededRelative(access);
        try
        {
            reg.OpenStatic("F", FileOpenMode.IO);
            reg.SetRelativeKey("F", 9);
            Assert.Equal(FileStatusCode.Success, reg.WriteKeyed("F", "BBBBBBBB", -1));
            reg.Close("F");
            Assert.Equal(2, CountRelative(host));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    [Theory]
    [InlineData(RandomAccess)]
    [InlineData(DynamicAccess)]
    public void IndexedWrite_IoModeAtNonSequentialAccess_Succeeds(int access)
    {
        var (reg, host) = SeededIndexed(access);
        try
        {
            reg.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal(FileStatusCode.Success, reg.WriteKeyed("F", "K009BBBB", -1));
            reg.Close("F");
            Assert.Equal(2, CountIndexed(host));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // Item 8 a)'s other half, for symmetry with the arm PB325 changed: Table 20's Sequential × WRITE × I-O is
    // BLANK, so a sequential-access connector open I-O gets '48' — the access mode picks item 8's arm, and this
    // is the arm no random/dynamic connector may ever fall into.
    [Fact]
    public void RelativeWrite_IoModeAtSequentialAccess_Is48()
    {
        var (reg, host) = SeededRelative(SeqAccess);
        try
        {
            reg.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal(FileStatusCode.WriteNotOpenForOutput, reg.WriteKeyed("F", "BBBBBBBB", -1));
            reg.Close("F");
            Assert.Equal(1, CountRelative(host));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    [Fact]
    public void IndexedWrite_IoModeAtSequentialAccess_Is48()
    {
        var (reg, host) = SeededIndexed(SeqAccess);
        try
        {
            reg.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal(FileStatusCode.WriteNotOpenForOutput, reg.WriteKeyed("F", "K009BBBB", -1));
            reg.Close("F");
            Assert.Equal(1, CountIndexed(host));
        }
        finally { try { File.Delete(host); } catch { } }
    }
}
