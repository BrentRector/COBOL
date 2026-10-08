// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE LINE END OF A FILE'S MEDIUM IS A FUNCTION OF THE FILE'S SHAPE, IN THREE ARMS (kb/Work PB1664;
/// docs/CONFORMANCE.md DOC-A.1-146 (c′), DOC-A.1-159, DOC-A.1-114). ISO leaves the on-medium form of a record to
/// the implementor (§9.1.7.2 for a record sequential record, §13.4.5.4 GR4 for a report writer's logical record,
/// §12.4.5.10.3 GR2 for a line sequential delimiter), so CLAUDE.md rule 1's precedence decides and GnuCOBOL 3.2
/// answers:
/// <list type="bullet">
/// <item>a LINE SEQUENTIAL file, a file with a LINAGE clause (which GnuCOBOL reclassifies LINE SEQUENTIAL), and
///   every REPORT file (reclassified the same way): the C library's text-mode newline — the HOST newline, CR LF
///   on Windows and LF on Linux and macOS;</item>
/// <item>any other RECORD SEQUENTIAL print stream — a file written with an ADVANCING phrase — a binary sequential
///   file: LF on EVERY host.</item>
/// </list>
/// Before PB1664 the second arm was CR LF on every host and a LINE SEQUENTIAL report file bypassed the first
/// (<c>FileRegistry.RegisterReport</c> registers every report file as record sequential), so it was CR LF on Linux
/// where GnuCOBOL writes LF. ADVANCING 0's bare carriage return (§14.9.51.4 GR25 c), owner decision R54) and
/// ADVANCING PAGE's form feed (GR25 h)) are line CONTROLS, not line ends, and are asserted unchanged.
/// </summary>
public sealed class PrintStreamLineEndConformanceTests
{
    private static readonly CobolNetCompiler CobolNet2023 = new(2023);

    private static string Host(string s) => s.Replace("{nl}", Environment.NewLine, StringComparison.Ordinal);

    private static string Fd(string org, string fdClauses, string id, string file, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F1 ASSIGN TO "{file}" ORGANIZATION IS {org}.
        DATA DIVISION.
        FILE SECTION.
        FD F1 {fdClauses}.
        01 R1 PIC X(4).
        PROCEDURE DIVISION.
            OPEN OUTPUT F1
        {proc}
            CLOSE F1
            STOP RUN.
        """;

    private static void AssertMedium(string source, string file, string expected)
    {
        var (ok, _, detail, bytes) = CobolNet2023.CompileRunAndReadFile(source, file);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.NotNull(bytes);
        Assert.Equal(expected, Encoding.Latin1.GetString(bytes!));
    }

    private const string TwoAfterWrites = """
            MOVE "AAAA" TO R1
            WRITE R1 AFTER ADVANCING 1 LINE
            MOVE "BBBB" TO R1
            WRITE R1 AFTER ADVANCING 1 LINE
        """;

    /// <summary>Arm 2: a record sequential print stream with no LINAGE clause ends every line with LF on every
    /// host, and its controls are unchanged — a bare CR for ADVANCING 0 and a form feed for ADVANCING PAGE.</summary>
    [Fact]
    public void RecordSequentialPrintStream_EndsLinesWithLfOnEveryHost()
        => AssertMedium(Fd("SEQUENTIAL", "", "PLE1", "ple1.dat", TwoAfterWrites), "ple1.dat", "\nAAAA\nBBBB\n");

    [Fact]
    public void RecordSequentialPrintStream_KeepsItsLineControls()
        => AssertMedium(Fd("SEQUENTIAL", "", "PLE2", "ple2.dat", """
                MOVE "AAAA" TO R1
                WRITE R1 AFTER ADVANCING PAGE
                MOVE "BBBB" TO R1
                WRITE R1 AFTER ADVANCING 0 LINES
                MOVE "CCCC" TO R1
                WRITE R1 AFTER ADVANCING 1 LINE
            """), "ple2.dat", "\fAAAA\rBBBB\nCCCC\n");

    /// <summary>Arm 1: the other two shapes of a record sequential file that is a print stream by its FD, and the
    /// line sequential file itself — the host newline.</summary>
    [Theory]
    [InlineData("SEQUENTIAL", "LINAGE IS 5 LINES", "PLE3", "ple3.dat")]
    [InlineData("LINE SEQUENTIAL", "", "PLE4", "ple4.dat")]
    [InlineData("LINE SEQUENTIAL", "LINAGE IS 5 LINES", "PLE5", "ple5.dat")]
    public void LinageAndLineSequentialFiles_EndLinesWithTheHostNewline(string org, string fd, string id, string file)
        => AssertMedium(Fd(org, fd, id, file, TwoAfterWrites), file, Host("{nl}AAAA{nl}BBBB{nl}"));

    /// <summary>Arm 1 for the report writer: EVERY report file — a LINE SEQUENTIAL one included (§13.4.5.3 SR4
    /// makes it legal) — ends its lines with the host newline.</summary>
    [Theory]
    [InlineData("SEQUENTIAL", "PLE6", "ple6.rpt")]
    [InlineData("LINE SEQUENTIAL", "PLE7", "ple7.rpt")]
    public void ReportFiles_EndLinesWithTheHostNewline(string org, string id, string file)
    {
        string source = $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT PFILE ASSIGN TO "{file}" ORGANIZATION IS {org}.
        DATA DIVISION.
        FILE SECTION.
        FD PFILE REPORT IS R1.
        REPORT SECTION.
        RD R1.
        01 DL TYPE IS DETAIL LINE PLUS 1.
           05 COLUMN 1 PIC X(2) VALUE "DD".
        PROCEDURE DIVISION.
            OPEN OUTPUT PFILE
            INITIATE R1
            GENERATE DL
            GENERATE DL
            TERMINATE R1
            CLOSE PFILE
            STOP RUN.
        """;
        AssertMedium(source, file, Host("DD{nl}DD{nl}"));
    }
}
