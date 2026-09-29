// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB860 — the I-O status a file connector answers when ANOTHER RUN UNIT holds the physical file
/// in a manner that excludes this request. The standard has a value for exactly this:
/// <list type="bullet">
/// <item>§9.1.13.9 1) — <i>"I-O status = 61. A file sharing conflict condition exists because an OPEN statement
///   is attempted on a physical file and that physical file is already open by another file connector in a manner
///   that conflicts with this request"</i>, sub-case a) <i>"An attempt is made to open a physical file that is
///   currently open by another file connector in the sharing with no other mode"</i>;</item>
/// <item>§9.1.13.9 2) — <i>"I-O status = 62. … a DELETE FILE statement is attempted on a physical file and that
///   physical file is currently open by another file connector"</i>.</item>
/// </list>
/// Every organization answered '30' — §9.1.13.6 1)'s <i>"no further information is available"</i> — because
/// <c>FileConnector.Open</c> mapped every <see cref="IOException"/> to it. The other file connector here is a
/// <see cref="FileShare.None"/> handle held by the test itself: a host share mode names no requester, so it is
/// exactly as foreign to the runtime's connector as another program's (<see cref="HostCapability.OutsiderCan"/>).
/// </summary>
public sealed class SharingRefusalStatusTests
{
    public enum Org { Sequential, Relative, Indexed }

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb860-{tag}-{Guid.NewGuid():N}.dat");

    private static void RegisterOrg(FileRegistry reg, string name, string host, Org org)
    {
        switch (org)
        {
            case Org.Relative:
                reg.RegisterRelative(name, host, recordWidth: 4, optional: false, accessMode: 0,
                    relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
                break;
            case Org.Indexed:
                reg.RegisterIndexed(name, host, recordWidth: 4, optional: false, accessMode: 0,
                    primeOffset: 0, primeLength: 4, varyMin: -1, varyMax: -1);
                break;
            default:
                reg.Register(name, host, recordWidth: 4, lineSequential: false, optional: false,
                    varyMin: -1, varyMax: -1);
                break;
        }
    }

    /// <summary>A physical file of <paramref name="org"/> holding one record, written and closed by a connector
    /// of its own registry — so the file the measurement opens exists in that organization's own format.</summary>
    private static string Seed(Org org)
    {
        string host = Tmp(org.ToString());
        var reg = new FileRegistry();
        RegisterOrg(reg, "S", host, org);
        reg.OpenStatic("S", FileOpenMode.Output);
        Assert.Equal("00", reg.Status("S"));
        reg.WriteShared("S", "SEED", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
        reg.Close("S");
        Assert.Equal("00", reg.Status("S"));
        return host;
    }

    public static TheoryData<Org, FileOpenMode> EveryOpen()
    {
        var data = new TheoryData<Org, FileOpenMode>();
        foreach (var org in Enum.GetValues<Org>())
            foreach (var mode in Enum.GetValues<FileOpenMode>())
                data.Add(org, mode);
        return data;
    }

    /// <summary>§9.1.13.9 1) a), for every organization and every open mode: '61', and — §14.9.27.4 GR25, <i>"If
    /// the execution of the OPEN statement is unsuccessful, the file is not affected"</i> — the physical file
    /// byte-for-byte as it was, OUTPUT's truncation included.</summary>
    [Theory]
    [MemberData(nameof(EveryOpen))]
    public void AnOpenRefusedByAnotherRunUnitsExclusiveHandle_IsTheFileSharingConflict(Org org, FileOpenMode mode)
    {
        Assert.True(HostCapability.Sharing.EnforcesExclusiveAccess, HostCapability.Sharing.Because);
        string host = Seed(org);
        try
        {
            byte[] before = File.ReadAllBytes(host);
            string status;
            var reg = new FileRegistry();
            RegisterOrg(reg, "F", host, org);
            using (new FileStream(host, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                reg.OpenStatic("F", mode);
                status = reg.Status("F");
            }
            Assert.True(status == FileStatusCode.FileSharingConflict,
                $"{org} OPEN {mode} against another run unit's exclusive handle answered '{status}'; §9.1.13.9 1) a) "
                + "requires '61' ('30' is §9.1.13.6 1)'s \"no further information is available\" — the host said "
                + "which refusal it was).");
            reg.Close("F");
            Assert.True(reg.Status("F") == FileStatusCode.FileNotOpen,
                "an unsuccessful OPEN leaves the connector in no open mode, so its CLOSE answers '42'");
            Assert.Equal(before, File.ReadAllBytes(host));   // §14.9.27.4 GR25 — the file is not affected
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>The refusal ends with the handle: the SAME connector's next OPEN, once the other run unit has let
    /// go, succeeds — '61' is a statement about the conflict, not a latched state of the connector.</summary>
    [Theory]
    [InlineData(Org.Sequential)]
    [InlineData(Org.Relative)]
    [InlineData(Org.Indexed)]
    public void TheConflictEndsWhenTheOtherHandleCloses(Org org)
    {
        string host = Seed(org);
        try
        {
            var reg = new FileRegistry();
            RegisterOrg(reg, "F", host, org);
            using (new FileStream(host, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                reg.OpenStatic("F", FileOpenMode.Input);
                Assert.Equal(FileStatusCode.FileSharingConflict, reg.Status("F"));
            }
            reg.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal("00", reg.Status("F"));
            reg.Close("F");
            Assert.Equal("00", reg.Status("F"));
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>The host classification itself, on a refusal MEASURED on this host rather than predicted for it
    /// (kb/Work PB795): an outside handle's open against an exclusive one is a sharing refusal, and a missing
    /// directory is not.</summary>
    [Fact]
    public void TheHostsOwnSharingRefusal_IsClassifiedAsOne()
    {
        string host = Tmp("classify");
        File.WriteAllText(host, "X");
        try
        {
            IOException? refusal = null;
            using (new FileStream(host, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                try { using var _ = new FileStream(host, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); }
                catch (IOException e) { refusal = e; }
            Assert.True(refusal is not null, "precondition: this host refused an outside reader of an exclusive handle. "
                + HostCapability.Sharing.Because);
            Assert.True(HostFile.IsSharingRefusal(refusal!), $"HResult 0x{refusal!.HResult:X8}: {refusal.Message}");

            IOException? other = null;
            try
            {
                using var _ = new FileStream(
                    Path.Combine(Path.GetTempPath(), $"pb860-no-dir-{Guid.NewGuid():N}", "f"), FileMode.Open);
            }
            catch (IOException e) { other = e; }
            Assert.NotNull(other);
            Assert.False(HostFile.IsSharingRefusal(other!), $"HResult 0x{other!.HResult:X8}: {other.Message}");
        }
        finally { File.Delete(host); }
    }

    /// <summary>§9.1.13.9 2)'s DELETE FILE twin: '62', and the file left in place (§14.9.10.4 — an unsuccessful
    /// DELETE FILE deletes nothing). ⛔ The file must SURVIVE on every host: a Unix unlink never consults open
    /// handles, so before <c>HostFile.IsHeldByAnother</c> the delete there succeeded over another run unit's open
    /// file, while Windows refused it and the refusal answered '30'. Both hosts were wrong, differently.</summary>
    [Theory]
    [InlineData(Org.Sequential)]
    [InlineData(Org.Relative)]
    [InlineData(Org.Indexed)]
    public void ADeleteFileRefusedByAnotherRunUnitsHandle_IsTheDeleteSharingConflict(Org org)
    {
        string host = Seed(org);
        try
        {
            var reg = new FileRegistry();
            RegisterOrg(reg, "F", host, org);
            string status;
            using (new FileStream(host, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                status = reg.DeleteFile("F");
            Assert.True(status == FileStatusCode.DeleteFileSharing,
                $"{org} DELETE FILE against another run unit's open handle answered '{status}'; §9.1.13.9 2) requires '62'.");
            Assert.True(File.Exists(host), "an unsuccessful DELETE FILE leaves the file in place");
            // …and once the other run unit lets go, the same statement deletes it.
            Assert.Equal(FileStatusCode.Success, reg.DeleteFile("F"));
            Assert.False(File.Exists(host));
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>kb/Work PB1163 — §14.7.9.3 GR3, <i>"If the FOREVER phrase is specified, the mass storage control
    /// system shall attempt to gain access to a locked resource until the input-output operation has been
    /// completed"</i>, end to end against a real handle of ANOTHER run unit that is released while the statement
    /// waits. DELETE FILE and OPEN both complete: '00', and the file deleted (or opened). Before the fix both gave up
    /// after one re-attempt — '62' with the file still present, '61' — although the holder let go moments later.
    /// The holder releases only once the statement has been refused at least once, so the test observes a real
    /// wait and times nothing.</summary>
    [Theory]
    [InlineData(Org.Sequential)]
    [InlineData(Org.Relative)]
    [InlineData(Org.Indexed)]
    public void RetryForever_WaitsForAnotherRunUnitToRelease_DeleteFileAndOpen(Org org)
    {
        Assert.True(HostCapability.Sharing.EnforcesExclusiveAccess, HostCapability.Sharing.Because);
        string host = Seed(org);
        try
        {
            var reg = new FileRegistry();
            RegisterOrg(reg, "F", host, org);

            using (var holder = HoldUntilRefused(host))
                reg.OpenShared("F", FileOpenMode.Input, hasSharingOverride: false, FileSharing.AllOther,
                    FileRetryKind.Forever, 0, OpenTapePhrase.None, host, assignDynamic: false, page: null);
            Assert.Equal(FileStatusCode.Success, reg.Status("F"));
            reg.Close("F");

            using (var holder = HoldUntilRefused(host))
                Assert.Equal(FileStatusCode.Success, reg.DeleteFile("F", FileRetryKind.Forever, 0));
            Assert.False(File.Exists(host), "DELETE FILE RETRY FOREVER completes once the other run unit lets go");
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>An exclusive handle on <paramref name="host"/> — another run unit's, as far as the runtime can tell —
    /// released on a background thread once the retry discipline has been refused and has paused at least once.
    /// Disposing the returned object releases the handle if that never happened, so a regression in which the
    /// statement gives up without waiting FAILS its assertion instead of hanging the test run.</summary>
    private static IDisposable HoldUntilRefused(string host)
    {
        var handle = new FileStream(host, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var refused = new ManualResetEventSlim();
        FileRegistry.PauseObserver.Value = interval => { refused.Set(); Thread.Sleep(interval); };
        var release = Task.Run(() => { refused.Wait(); handle.Dispose(); });
        return new Released(() =>
        {
            FileRegistry.PauseObserver.Value = null;
            refused.Set();
            release.Wait();
            refused.Dispose();
        });
    }

    private sealed class Released(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
