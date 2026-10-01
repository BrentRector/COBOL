// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The V59 migration guard: a FIXED-LENGTH record-sequential file whose byte length is not a whole multiple of
/// its record length is PROVABLY inconsistent with the description about to read it, and the user is told at
/// OPEN rather than left to compute on misaligned records.
/// <para>
/// Why the standard's own detection is not enough: §14.9.30.4 GR14 makes a record whose byte count falls outside
/// the description's min/max a SUCCESSFUL read with I-O status '04' — which fires only on the trailing partial
/// record, only when the total is not a multiple, and only if the program inspects FILE STATUS. A layout
/// migration needs the arithmetic fact stated up front. The notice therefore does NOT change the I-O status:
/// OPEN still succeeds, '04' still arrives at the short read, and nothing about the conforming behaviour moves.
/// </para>
/// </summary>
[Collection("process-globals")]   // kb/Work PB126: `Console.Error` is process-wide and `RecordLayoutNotice` writes to it on OPEN — a class that swaps the error stream and a class that opens a mismatched fixed file must not run in parallel (LineSequential_IsNeverReported captured another class's notice)
public sealed class RecordLayoutNoticeTests : IDisposable
{
    private readonly List<string> _paths = [];
    private readonly TextWriter _stderr = Console.Error;

    private string TempFile(int bytes)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cobolnet_v59_{Guid.NewGuid():N}.dat");
        File.WriteAllText(path, new string('7', bytes));
        _paths.Add(path);
        return path;
    }

    /// <summary>Capture stderr for exactly the duration of <paramref name="act"/> — restored in a finally, so a
    /// throwing assertion cannot leave the process's error stream pointed at a dead buffer for every later test.</summary>
    private string CaptureStderr(Action act)
    {
        var buf = new StringWriter();
        Console.SetError(buf);
        try { act(); } finally { Console.SetError(_stderr); }
        return buf.ToString();
    }

    public void Dispose()
    {
        Console.SetError(_stderr);
        foreach (string p in _paths) try { File.Delete(p); } catch (IOException) { }
    }

    /// <summary>13 bytes described as 5-byte records: 2 whole records and 3 trailing bytes. The notice names the
    /// file, both lengths and the remainder, because a user acting on it needs to know WHICH file and by how much
    /// it disagrees.</summary>
    [Fact]
    public void NonMultipleFixedLengthFile_IsReportedAtOpen()
    {
        RecordLayoutNotice.ResetForTests();
        string path = TempFile(13);
        var c = new SequentialConnector(path, recordWidth: 5, lineSequential: false);
        string err = CaptureStderr(() => c.Open(FileOpenMode.Input));
        Assert.Contains("not a whole multiple", err);
        Assert.Contains("13 bytes", err);
        Assert.Contains("5-byte record", err);
        Assert.Contains(path, err);
        Assert.Equal(FileStatusCode.Success, c.Status);   // ⛔ a notice, never a status change
        c.Close();
    }

    /// <summary>The clean case stays SILENT — 15 bytes of 5-byte records. A notice that fired on well-formed
    /// files would be noise, and noise is how a real warning gets ignored.</summary>
    [Fact]
    public void MultipleFixedLengthFile_IsSilent()
    {
        RecordLayoutNotice.ResetForTests();
        string path = TempFile(15);
        var c = new SequentialConnector(path, recordWidth: 5, lineSequential: false);
        Assert.Equal("", CaptureStderr(() => c.Open(FileOpenMode.Input)));
        c.Close();
    }

    /// <summary>LINE SEQUENTIAL is excluded by construction: its records are DELIMITED, so the physical length
    /// has no arithmetic relationship to the record width at all (§9.1.13.2).</summary>
    [Fact]
    public void LineSequential_IsNeverReported()
    {
        RecordLayoutNotice.ResetForTests();
        string path = TempFile(13);
        var c = new SequentialConnector(path, recordWidth: 5, lineSequential: true);
        Assert.Equal("", CaptureStderr(() => c.Open(FileOpenMode.Input)));
        c.Close();
    }

    /// <summary>RECORD IS VARYING is excluded too: every record carries its own length prefix, so a disagreement
    /// is detected per record by the §14.9.30.4 GR14 '04' path rather than by file arithmetic.</summary>
    [Fact]
    public void VaryingRecords_AreNeverReported()
    {
        RecordLayoutNotice.ResetForTests();
        string path = TempFile(13);
        var c = new SequentialConnector(path, recordWidth: 5, lineSequential: false, varyMin: 1, varyMax: 5);
        Assert.Equal("", CaptureStderr(() => c.Open(FileOpenMode.Input)));
        c.Close();
    }

    /// <summary>An empty file is silent — 0 bytes is a whole number of records (none), and an OPTIONAL file that
    /// does not exist yet is the ordinary start of a program's first run.</summary>
    [Fact]
    public void EmptyFile_IsSilent()
    {
        RecordLayoutNotice.ResetForTests();
        string path = TempFile(0);
        var c = new SequentialConnector(path, recordWidth: 5, lineSequential: false);
        Assert.Equal("", CaptureStderr(() => c.Open(FileOpenMode.Input)));
        c.Close();
    }

    /// <summary>OPEN I-O reads the existing bytes too, so it carries the same proof as INPUT.</summary>
    [Fact]
    public void IoMode_IsAlsoReported()
    {
        RecordLayoutNotice.ResetForTests();
        string path = TempFile(13);
        var c = new SequentialConnector(path, recordWidth: 5, lineSequential: false);
        Assert.Contains("not a whole multiple", CaptureStderr(() => c.Open(FileOpenMode.IO)));
        c.Close();
    }

    /// <summary>OPEN EXTEND is reported, and the stakes are HIGHER than for INPUT: appending to a file whose
    /// existing layout disagrees produces a file interleaving two record layouts — permanent corruption rather
    /// than a wrong computation. This is the sibling site the first cut of the notice missed.</summary>
    [Fact]
    public void ExtendMode_IsAlsoReported()
    {
        RecordLayoutNotice.ResetForTests();
        string path = TempFile(13);
        var c = new SequentialConnector(path, recordWidth: 5, lineSequential: false);
        Assert.Contains("not a whole multiple", CaptureStderr(() => c.Open(FileOpenMode.Extend)));
        c.Close();
    }

    /// <summary>OPEN OUTPUT is SILENT even on a misaligned file — it truncates, so whatever the file held is
    /// discarded and nothing can disagree with the description. Excluding it is principled, not an oversight.</summary>
    [Fact]
    public void OutputMode_IsNeverReported()
    {
        RecordLayoutNotice.ResetForTests();
        string path = TempFile(13);
        var c = new SequentialConnector(path, recordWidth: 5, lineSequential: false);
        Assert.Equal("", CaptureStderr(() => c.Open(FileOpenMode.Output)));
        c.Close();
    }

    /// <summary>⛔ kb/Work PB677 — a REPORT FILE has no record description: its connector's width is the widest RD's
    /// LINE WIDTH and its bytes are print lines with terminators and spacing, so "not a whole multiple of the
    /// record length" is a statement about nothing and the notice's "reads will be misaligned" is false. The
    /// emitter's one record-less arm registers it through <see cref="FileRegistry.RegisterReport"/>, and OPEN
    /// EXTEND over an existing 11-byte report (the shape the PB326 golden leaves behind: 11 bytes against a 3-byte
    /// line width) must print nothing. The control — a file that DOES have a record description, over the same
    /// bytes and width — is still reported, so the silence is the registration's and not the notice's removal.</summary>
    [Fact]
    public void ReportFile_ExtendOverAnExistingReport_IsSilent_ButARecordFileOverTheSameBytesIsNot()
    {
        RecordLayoutNotice.ResetForTests();
        string report = TempFile(11);
        var reg = new FileRegistry();
        reg.RegisterReport("RPT", report, lineWidth: 3, optional: false);
        Assert.Equal("", CaptureStderr(() => reg.OpenStatic("RPT", FileOpenMode.Extend)));
        Assert.Equal(FileStatusCode.Success, reg.Status("RPT"));
        reg.Close("RPT");

        string records = TempFile(11);
        reg.Register("REC", records, 3, lineSequential: false, optional: false, -1, -1);
        Assert.Contains("not a whole multiple", CaptureStderr(() => reg.OpenStatic("REC", FileOpenMode.Extend)));
        reg.Close("REC");
    }

    /// <summary>One notice per file, however many times the program OPENs it — a loop that opens and closes must
    /// not produce a wall of identical lines.</summary>
    [Fact]
    public void RepeatedOpens_ReportOnce()
    {
        RecordLayoutNotice.ResetForTests();
        string path = TempFile(13);
        var c = new SequentialConnector(path, recordWidth: 5, lineSequential: false);
        string first = CaptureStderr(() => { c.Open(FileOpenMode.Input); c.Close(); });
        string second = CaptureStderr(() => { c.Open(FileOpenMode.Input); c.Close(); });
        Assert.Contains("not a whole multiple", first);
        Assert.Equal("", second);
    }
}
