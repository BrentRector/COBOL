// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1541 — ISO §9.1.13.1: a permanent error "remains in effect for all subsequent input-output operations
/// on the file unless an implementor-defined technique is invoked to correct the permanent error condition"
/// (Annex A.1 item 105; the technique this processor provides is CLOSE — docs/CONFORMANCE.md DOC-A.1-105).
/// <para>The rule is one mechanism with one entry per VERB FAMILY (the shared READ/START/REWRITE-DELETE
/// preconditions, each WRITE entry, OPEN, and the registry's UNLOCK and DELETE FILE), so the matrix here is
/// every organization x every verb: a verb that forgets to ask would answer success over a file the standard says
/// is still in the error. The end-to-end relative case rides the <c>pb1541_*</c> golden; these reach the
/// organizations and verbs a golden cannot produce a portable '30'/'34' for (a full disk), by reporting a
/// permanent error through <see cref="FileConnector.SetStatus"/> — the facade-level entry the registry itself uses.</para>
/// </summary>
public sealed class PermanentErrorInEffectTests
{
    private const string Host = "30";   // §9.1.13.6 item 1 — "a permanent error exists and no further information is available"

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb1541-{tag}-{Guid.NewGuid():N}.dat");

    // ── sequential (record and line) ─────────────────────────────────────────────────────────────────────────

    private static void AssertSequentialEveryVerbReplays(bool lineSequential)
    {
        string host = Tmp(lineSequential ? "lseq" : "seq");
        try
        {
            var c = new SequentialConnector(host, 4, lineSequential);
            Assert.Equal("00", c.Open(FileOpenMode.Output));
            Assert.Equal("00", c.Write("AAAA", -1, null));
            Assert.Equal("00", c.Write("BBBB", -1, null));
            Assert.Equal("00", c.Close());
            Assert.Equal("00", c.Open(FileOpenMode.IO));
            Assert.Null(c.PermanentErrorInEffect);

            c.SetStatus(Host);   // the host reported a permanent error on the operation that just ran
            Assert.Equal(Host, c.PermanentErrorInEffect);

            Assert.False(c.Read(previous: false, out _));
            Assert.Equal(Host, c.Status);
            Assert.Equal(Host, c.Rewrite("CCCC"));
            Assert.Equal(Host, c.Write("CCCC", -1, null));
            Assert.Equal(Host, c.WriteAdvancing("CCCC", 1, before: false, null));
            Assert.Equal(Host, c.StartFirstLast(last: false));
            Assert.Equal(Host, c.Open(FileOpenMode.IO));   // §9.1.13.1 outranks the already-open '41'
            Assert.Equal(Host, c.PermanentErrorInEffect);

            Assert.Equal("00", c.Close());                 // the correction technique
            Assert.Null(c.PermanentErrorInEffect);
            Assert.Equal("00", c.Open(FileOpenMode.Input));
            Assert.True(c.Read(previous: false, out string first));
            Assert.Equal("AAAA", first);                   // nothing was done while the condition held
            Assert.True(c.Read(previous: false, out string second));
            Assert.Equal("BBBB", second);
            Assert.Equal("00", c.Close());
        }
        finally { try { File.Delete(host); } catch { } }
    }

    [Fact]
    public void RecordSequential_EveryVerbReplaysThePermanentError_AndCloseEndsIt() =>
        AssertSequentialEveryVerbReplays(lineSequential: false);

    [Fact]
    public void LineSequential_EveryVerbReplaysThePermanentError_AndCloseEndsIt() =>
        AssertSequentialEveryVerbReplays(lineSequential: true);

    // ── indexed ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Indexed_EveryVerbReplaysThePermanentError_AndCloseEndsIt()
    {
        string host = Tmp("idx");
        try
        {
            var c = new IndexedConnector(host, 8, KeyedAccess.Dynamic, 0, 4, -1, -1);
            Assert.Equal("00", c.Open(FileOpenMode.Output));
            Assert.Equal("00", c.Write("AAAA0001"));
            Assert.Equal("00", c.Write("BBBB0002"));
            Assert.Equal("00", c.Close());
            Assert.Equal("00", c.Open(FileOpenMode.IO));

            c.SetStatus(Host);
            Assert.Equal(Host, c.PermanentErrorInEffect);

            Assert.Equal(Host, c.ReadNext(out _));
            Assert.Equal(Host, c.ReadPrevious(out _));
            Assert.Equal(Host, c.ReadRandom(-1, "AAAA0001", out _));
            Assert.Equal(Host, c.Write("CCCC0003"));
            Assert.Equal(Host, c.Rewrite("AAAA9999"));
            Assert.Equal(Host, c.Delete("AAAA0001"));
            Assert.Equal(Host, c.Start(-1, "==", "AAAA0001", StartKeyLength.OfWidth(4)));
            Assert.Equal(Host, c.StartFirstLast(last: true));
            Assert.Equal(Host, c.Open(FileOpenMode.IO));

            Assert.Equal("00", c.Close());
            Assert.Null(c.PermanentErrorInEffect);
            Assert.Equal("00", c.Open(FileOpenMode.IO));
            Assert.Equal("00", c.ReadRandom(-1, "AAAA0001", out string a));
            Assert.Equal("AAAA0001", a);                   // neither the stuck REWRITE nor DELETE changed it
            Assert.Equal("23", c.ReadRandom(-1, "CCCC0003", out _));   // the stuck WRITE stored nothing
            Assert.Equal("00", c.Close());
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // ── relative, through the registry: a REAL producer, and the two registry-level verbs ────────────────────────

    [Fact]
    public void Relative_RegistryVerbs_ReplayTheBoundaryError_UnlockAndDeleteFileToo_AndCloseEndsIt()
    {
        var reg = new FileRegistry();
        string host = Tmp("rel");
        reg.RegisterRelative("F", host, 4, false, (int)KeyedAccess.Dynamic, 4, -1, -1);
        try
        {
            reg.OpenStatic("F", FileOpenMode.Output);
            reg.SetRelativeKey("F", 1);
            Assert.Equal("00", reg.WriteKeyed("F", "AAAA", -1));
            reg.Close("F");
            reg.OpenStatic("F", FileOpenMode.IO);

            reg.SetRelativeKey("F", 0);
            Assert.Equal("34", reg.WriteKeyed("F", "BBBB", -1));   // §14.9.51.4 GR29 b) — the real producer
            reg.SetRelativeKey("F", 2);
            Assert.Equal("34", reg.WriteKeyed("F", "CCCC", -1));   // would be '00': the condition is in effect
            Assert.Equal("34", reg.ReadKeyedNext("F", out _));
            Assert.Equal("34", reg.RewriteKeyed("F", "DDDD", -1));
            Assert.Equal("34", reg.DeleteRecord("F", ""));
            Assert.Equal("34", reg.StartRelative("F", "==", 1));
            Assert.Equal("34", reg.StartFirstLast("F", last: false));
            reg.Unlock("F", records: false);
            Assert.Equal("34", reg.Status("F"));
            Assert.Equal("34", reg.DeleteFile("F"));               // §14.9.10.4 GR13's '41' is outranked
            Assert.True(File.Exists(host));

            reg.Close("F");
            Assert.Equal("00", reg.Status("F"));
            reg.OpenStatic("F", FileOpenMode.IO);
            reg.SetRelativeKey("F", 1);
            Assert.Equal("00", reg.ReadKeyed("F", -1, "", out string one));
            Assert.Equal("AAAA", one);
            reg.SetRelativeKey("F", 2);
            Assert.Equal("23", reg.ReadKeyed("F", -1, "", out _));
            reg.Close("F");
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // ── what is NOT a permanent error in effect ──────────────────────────────────────────────────────────────

    /// <summary>A failed OPEN leaves the connector closed, which already is the corrected state (DOC-A.1-105): the
    /// next OPEN of the same connector starts fresh. A '35' (file absent) is the canonical case — the idiom of
    /// creating the file and opening again must keep working.</summary>
    [Fact]
    public void FailedOpen_LeavesNoConditionInEffect_TheNextOpenStartsFresh()
    {
        string host = Tmp("open");
        var c = new SequentialConnector(host, 4, lineSequential: false);
        Assert.Equal("35", c.Open(FileOpenMode.Input));
        Assert.Null(c.PermanentErrorInEffect);
        Assert.Equal("00", c.Open(FileOpenMode.Output));
        try { Assert.Equal("00", c.Write("AAAA", -1, null)); }
        finally { c.Close(); try { File.Delete(host); } catch { } }
    }

    /// <summary>Only a '3x' starts the condition: an invalid key ('22'/'23'), a logic error ('4x') and a
    /// successful-with-information '0x' leave the connector usable (§9.1.13.5, §9.1.13.7, §9.1.13.2).</summary>
    [Fact]
    public void OnlyAClassThreeStatusStartsTheCondition()
    {
        string host = Tmp("cls");
        var c = new SequentialConnector(host, 4, lineSequential: false);
        try
        {
            Assert.Equal("00", c.Open(FileOpenMode.Output));
            foreach (string s in new[] { "02", "10", "22", "23", "43", "47", "51", "61", "71", "91" })
            {
                c.SetStatus(s);
                Assert.Null(c.PermanentErrorInEffect);
            }
            Assert.Equal("00", c.Write("AAAA", -1, null));
        }
        finally { c.Close(); try { File.Delete(host); } catch { } }
    }
}
