// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A WRITE WITHOUT THE ADVANCING PHRASE IS A WRITE WITH <c>AFTER ADVANCING 1 LINE</c>, IN EVERY STATE OF THE
/// DEVICE (kb/Work PB1027). ISO/IEC 1989:2023 §14.9.51.4 GR25: <i>"If the ADVANCING phrase is not used, automatic
/// advancing shall be provided by the implementor to act as if the user has specified AFTER ADVANCING 1 LINE."</i>
/// (checked: <c>cite.py --check 14.9.51.4 "If the ADVANCING phrase is not used, automatic advancing shall be
/// provided by the implementor to act as if the user has specified AFTER ADVANCING 1 LINE"</c> is
/// <c>OK  §14.9.51.4 25)</c>). The standard names no implementor choice of PLACEMENT, so the two spellings produce
/// the SAME file, byte for byte, whatever the file already holds — the property this class pins over every
/// placement mix instead of over the handful of mixes a hand-written golden would name.
///
/// <para>The defect this replaced: a plain WRITE took BEFORE's placement (the record, then its line end)
/// whenever no line was open, so after a BEFORE write it added no blank line where the spelled-out AFTER adds
/// one, on a LINAGE page the first record of a page sat one line high, and a line sequential file travelled twice
/// around an AFTER write. The two spellings are now ONE path (<c>SequentialConnector.WriteRecord</c> reroutes to
/// <c>WriteAdvancingRecord(lines: 1, before: false)</c>), and this grid is the drift test that keeps them one:
/// a new write arm that places a plain WRITE its own way fails a cell here.</para>
///
/// <para>⚖ THE GRID'S SHAPE IS THE DETERMINATION (docs/CONFORMANCE.md §4, "which files support vertical
/// positioning", Annex A.3 item 37): a file with a LINAGE clause supports vertical positioning from its OPEN, any
/// other sequential file from the first WRITE that carries an ADVANCING phrase. So the LINAGE shapes run every
/// three-write sequence over {plain, AFTER, BEFORE}, and the shapes without LINAGE run the sequences that start
/// with an explicit phrase; <see cref="PlainOnlyFile_IsNotPositioned_PlainWriteIsTheRecordAndItsLineEnd"/> pins the
/// complement (a file no phrase was ever written to has nothing to position).</para>
/// </summary>
public sealed class PlainWriteAdvancingConformanceTests
{
    private static readonly CobolNetCompiler CobolNet2023 = new(2023);

    private const string Linage = "LINAGE IS 2 LINES LINES AT TOP 1 LINES AT BOTTOM 1";

    /// <summary>Every cell of the grid: organization × LINAGE × a three-write sequence of P (no phrase), A (AFTER
    /// ADVANCING 1 LINE) and B (BEFORE ADVANCING 1 LINE). A shape without LINAGE needs an explicit phrase to be
    /// positioned, so its sequences do not start with P.</summary>
    public static IEnumerable<object[]> Cells()
    {
        string[] sequences = [.. from a in "PAB" from b in "PAB" from c in "PAB" select $"{a}{b}{c}"];
        foreach (string org in new[] { "SEQUENTIAL", "LINE SEQUENTIAL" })
            foreach (bool linage in new[] { false, true })
                foreach (string seq in sequences)
                    if (linage || seq[0] != 'P')
                        yield return [org, linage, seq];
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void PlainWrite_ProducesTheFileTheSpelledOutAfterPhraseProduces(string org, bool linage, string sequence)
    {
        string id = $"EQ{(org.StartsWith("LINE", StringComparison.Ordinal) ? "L" : "R")}{(linage ? "G" : "N")}{sequence}";
        var (ok, stdout, detail) = CobolNet2023.CompileAndRun(EqualityProgram(id, org, linage, sequence));
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        // "EQUAL n": the two files held the same n bytes. n is positive (a write that reached the medium) and
        // host-dependent (CR LF versus LF line ends), which is why only its sign is asserted.
        Assert.StartsWith("EQUAL ", stdout, StringComparison.Ordinal);
        Assert.NotEqual(0, int.Parse(stdout.AsSpan("EQUAL ".Length).Trim()));
    }

    /// <summary>One program that writes the same sequence to two files — F1 with every P left bare, F2 with every P
    /// spelled <c>AFTER ADVANCING 1 LINE</c> — then reads both back one byte at a time and compares them.</summary>
    private static string EqualityProgram(string id, string org, bool linage, string sequence)
    {
        string fd = linage ? " " + Linage : "";
        var proc = new StringBuilder();
        void Writes(string record, string suffix)
        {
            for (int i = 0; i < sequence.Length; i++)
            {
                string phrase = sequence[i] switch
                {
                    'A' => " AFTER ADVANCING 1 LINE",
                    'B' => " BEFORE ADVANCING 1 LINE",
                    _ => suffix,
                };
                proc.AppendLine($"    MOVE \"{new string((char)('A' + i), 4)}\" TO {record}");
                proc.AppendLine($"    WRITE {record}{phrase}");
            }
        }
        proc.AppendLine("    OPEN OUTPUT F1");
        Writes("R1", "");
        proc.AppendLine("    CLOSE F1");
        proc.AppendLine("    OPEN OUTPUT F2");
        Writes("R2", " AFTER ADVANCING 1 LINE");
        proc.AppendLine("    CLOSE F2");
        return $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F1 ASSIGN TO "{id}-1.dat" ORGANIZATION IS {org}.
            SELECT F2 ASSIGN TO "{id}-2.dat" ORGANIZATION IS {org}.
            SELECT B1 ASSIGN TO "{id}-1.dat" ORGANIZATION IS SEQUENTIAL.
            SELECT B2 ASSIGN TO "{id}-2.dat" ORGANIZATION IS SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F1{fd}.
        01 R1 PIC X(4).
        FD F2{fd}.
        01 R2 PIC X(4).
        FD B1.
        01 BY1 PIC X.
        FD B2.
        01 BY2 PIC X.
        WORKING-STORAGE SECTION.
        01 E1 PIC 9 VALUE 0.
        01 E2 PIC 9 VALUE 0.
        01 N  PIC 9(4) VALUE 0.
        01 OK-SW PIC 9 VALUE 1.
        PROCEDURE DIVISION.
        {proc}
            OPEN INPUT B1
            OPEN INPUT B2
            PERFORM UNTIL E1 = 1 OR E2 = 1
                READ B1 AT END MOVE 1 TO E1 END-READ
                READ B2 AT END MOVE 1 TO E2 END-READ
                IF E1 = 0 AND E2 = 0
                    ADD 1 TO N
                    IF BY1 NOT = BY2 MOVE 0 TO OK-SW END-IF
                END-IF
            END-PERFORM
            IF E1 NOT = E2 MOVE 0 TO OK-SW END-IF
            CLOSE B1
            CLOSE B2
            IF OK-SW = 1
                DISPLAY "EQUAL " N
            ELSE
                DISPLAY "DIFFER AT BYTE " N
            END-IF
            STOP RUN.
        """;
    }

    /// <summary>The complement of the grid: a file that has never been written with an ADVANCING phrase and has no
    /// LINAGE clause supports no vertical positioning (Annex A.3 item 37; §14.9.51.4 GR25: <i>"If the physical file
    /// does not support vertical positioning, the ADVANCING and END-OF-PAGE phrases are ignored"</i>), so its plain
    /// WRITE is the record and its own line end — no leading blank line, which is the shape GnuCOBOL writes by
    /// default and the one every ordinary text-file program relies on. A record sequential file has no line end
    /// at all: its records are fixed-width blocks.</summary>
    [Theory]
    [InlineData("LINE SEQUENTIAL", "AAAA{nl}BBBB{nl}")]
    [InlineData("SEQUENTIAL", "AAAABBBB")]
    public void PlainOnlyFile_IsNotPositioned_PlainWriteIsTheRecordAndItsLineEnd(string org, string expected)
    {
        string id = "PLO" + (org.StartsWith("LINE", StringComparison.Ordinal) ? "L" : "R");
        string file = id.ToLowerInvariant() + ".dat";
        string source = $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F1 ASSIGN TO "{file}" ORGANIZATION IS {org}.
        DATA DIVISION.
        FILE SECTION.
        FD F1.
        01 R1 PIC X(4).
        PROCEDURE DIVISION.
            OPEN OUTPUT F1
            MOVE "AAAA" TO R1
            WRITE R1
            MOVE "BBBB" TO R1
            WRITE R1
            CLOSE F1
            STOP RUN.
        """;
        var (ok, _, detail, bytes) = CobolNet2023.CompileRunAndReadFile(source, file);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.NotNull(bytes);
        Assert.Equal(expected.Replace("{nl}", Environment.NewLine, StringComparison.Ordinal), Encoding.Latin1.GetString(bytes!));
    }
}
