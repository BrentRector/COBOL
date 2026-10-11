// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE RECORD-LOCK CEILING IS THE LAST CHECK OF A WRITE OR REWRITE, IN EVERY ORGANIZATION (kb/Work PB2848).
/// <para>§12.4.5.9.4 GR7 — <i>"Any I-O statement that attempts to obtain a record lock that would exceed either limit is
/// unsuccessful"</i> — judges the lock a statement REQUESTS, and that lock is the statement's effect on success
/// (§14.9.51.4 GR11; §14.9.35.4 GR12), so docs/CONFORMANCE.md DOC-A.1-104 (2) puts it after the statement's own rules:
/// a record that breaks GR14's / GR20's size rule is '44' however many locks the connector holds. The registry used to
/// ask the ceiling BEFORE calling the organization, and four of the eight places that change the file therefore
/// answered '54' for a record they would have refused. The mechanism is <see cref="FileConnector.CommitGate"/>,
/// asked by each organization's WRITE and REWRITE through <c>RefusedAtCommit()</c> at the instant before it first
/// changes anything; this matrix reaches every one of those places (the sequential record, line and print arms, the
/// relative and indexed WRITE and REWRITE) with a gate that always refuses, so it needs no 15 or 255 locks to stand
/// at a ceiling. The end-to-end case rides the <c>pb2848_lock_ceiling_after_record_rules</c> golden.</para>
/// <para>For each place: a record the statement accepts is refused with the gate's status and changes nothing; a record
/// that breaks the statement's own size rule is that rule's '44'; and with no gate the same accepted record succeeds, so
/// the refusal did not merely break the verb.</para>
/// </summary>
public sealed class CommitGateDriftTests
{
    private const string Ceiling = "54";   // §9.1.13.8 item 4 — this file connector holds the maximum number of locks

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb2848-{tag}-{Guid.NewGuid():N}.dat");

    private static void Gate(FileConnector c, bool refusing) => c.CommitGate = refusing ? () => Ceiling : null;

    private static void AssertSequential(bool lineSequential)
    {
        string host = Tmp(lineSequential ? "lseq" : "seq");
        try
        {
            var c = new SequentialConnector(host, 8, lineSequential, varyMin: 4, varyMax: 8);
            Assert.Equal("00", c.Open(FileOpenMode.Output));
            Gate(c, refusing: true);
            Assert.Equal(Ceiling, c.Write("AAAAAA", -1, null));
            Assert.Equal("44", c.Write("AA", -1, null));                                       // GR14 first
            Assert.Equal(Ceiling, c.WriteAdvancing("AAAAAA", 1, before: false, null));
            Assert.Equal("44", c.WriteAdvancing("AA", 1, before: false, null));
            Gate(c, refusing: false);
            Assert.Equal("00", c.Write("AAAAAA", -1, null));
            Assert.Equal("00", c.Close());

            // A REWRITE needs the immediately-previous operation to be a successful READ (GR5 / '43'), and a refused
            // REWRITE is an operation, so each attempt below re-opens and reads.
            void Reread()
            {
                if (c.IsOpen) Assert.Equal("00", c.Close());
                Assert.Equal("00", c.Open(FileOpenMode.IO));
                Assert.True(c.Read(previous: false, out string record));
                Assert.Equal("AAAAAA", record.TrimEnd());
            }
            Reread();
            Gate(c, refusing: true);
            Assert.Equal(Ceiling, c.Rewrite("BBBBBB"));
            Reread();
            // Record sequential: GR16's equal-size rule; line sequential: GR17 b)'s "more bytes than the line replaced".
            Assert.Equal("44", c.Rewrite(lineSequential ? "BBBBBBBB" : "BB"));
            Reread();
            Gate(c, refusing: false);
            Assert.Equal("00", c.Rewrite("BBBBBB"));
            Assert.Equal("00", c.Close());

            Assert.Equal("00", c.Open(FileOpenMode.Input));
            Assert.True(c.Read(previous: false, out string again));
            Assert.Equal("BBBBBB", again.TrimEnd());
            Assert.False(c.Read(previous: false, out _));   // the refused WRITEs released nothing
            Assert.Equal("00", c.Close());
        }
        finally { try { File.Delete(host); } catch { } }
    }

    [Fact]
    public void RecordSequential_WriteAndRewrite_AskTheGateAfterTheirOwnRules() => AssertSequential(lineSequential: false);

    [Fact]
    public void LineSequential_WriteAndRewrite_AskTheGateAfterTheirOwnRules() => AssertSequential(lineSequential: true);

    [Fact]
    public void Relative_WriteAndRewrite_AskTheGateAfterTheirOwnRules()
    {
        string host = Tmp("rel");
        try
        {
            var c = new RelativeConnector(host, 8, KeyedAccess.Sequential, 4, varyMin: 4, varyMax: 8);
            Assert.Equal("00", c.Open(FileOpenMode.Output));
            Gate(c, refusing: true);
            Assert.Equal(Ceiling, c.Write("AAAAAA"));
            Assert.Equal("44", c.Write("AA"));
            Gate(c, refusing: false);
            Assert.Equal("00", c.Write("AAAAAA"));
            Assert.Equal("00", c.Close());

            void Reread()
            {
                if (c.IsOpen) Assert.Equal("00", c.Close());
                Assert.Equal("00", c.Open(FileOpenMode.IO));
                Assert.Equal("00", c.ReadNext(out _));
            }
            Reread();
            Gate(c, refusing: true);
            Assert.Equal(Ceiling, c.Rewrite("BBBBBB"));
            Reread();
            Assert.Equal("44", c.Rewrite("BB"));
            Reread();
            Gate(c, refusing: false);
            Assert.Equal("00", c.Rewrite("BBBBBB"));
            Assert.Equal("00", c.Close());

            Assert.Equal("00", c.Open(FileOpenMode.Input));
            Assert.Equal("00", c.ReadNext(out string kept));
            Assert.Equal("BBBBBB", kept.TrimEnd());
            Assert.Equal("10", c.ReadNext(out _));          // the refused WRITEs stored nothing
            Assert.Equal("00", c.Close());
        }
        finally { try { File.Delete(host); } catch { } }
    }

    [Fact]
    public void Indexed_WriteAndRewrite_AskTheGateAfterTheirOwnRules()
    {
        string host = Tmp("idx");
        try
        {
            var c = new IndexedConnector(host, 8, KeyedAccess.Dynamic, 0, 4, varyMin: 6, varyMax: 8);
            Assert.Equal("00", c.Open(FileOpenMode.Output));
            Gate(c, refusing: true);
            Assert.Equal(Ceiling, c.Write("AAAA01"));
            Assert.Equal("44", c.Write("AAAA"));
            Gate(c, refusing: false);
            Assert.Equal("00", c.Write("AAAA01"));
            Assert.Equal("00", c.Close());

            Assert.Equal("00", c.Open(FileOpenMode.IO));
            Gate(c, refusing: true);
            Assert.Equal(Ceiling, c.Rewrite("AAAA99"));
            Assert.Equal("44", c.Rewrite("AAAA"));
            Gate(c, refusing: false);
            Assert.Equal("00", c.Rewrite("AAAA99"));
            Assert.Equal("00", c.Close());

            Assert.Equal("00", c.Open(FileOpenMode.Input));
            Assert.Equal("00", c.ReadRandom(-1, "AAAA", out string kept));
            Assert.Equal("AAAA99", kept.TrimEnd());
            Assert.Equal("00", c.Close());
        }
        finally { try { File.Delete(host); } catch { } }
    }
}
