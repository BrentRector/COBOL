// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Sequential file I/O (ISO/IEC 1989:2023 §14.9; COBOLNET_DESIGN §8): OPEN/CLOSE/WRITE/READ/REWRITE over a typed-native
/// connector. Each test round-trips through the file (WRITE then READ back, or query FILE STATUS) and DISPLAYs the
/// result, so the existing stdout differential harness pins WiseOwl COBOL to the legacy oracle (364-NIST-green) — the
/// file content itself is verified indirectly, through the program's own read-back. The printer WRITE … ADVANCING path
/// is exercised end-to-end by the NC101A NIST program; here the focus is the data-file verbs and the status machine.
/// </summary>
public sealed class FileIoDifferentialTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    /// <summary>For the cases whose OBSERVATION needs a COBOL-2023 construct — today the line-sequential
    /// read-back of a print stream (kb/Work PB688). The BEHAVIOR under test is edition-invariant.</summary>
    private static readonly ICompilerUnderTest CobolNet2023 = new CobolNetCompiler(2023);

    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    /// <summary>A program with a single SELECTed file; <paramref name="select"/> is the SELECT clause body (after the
    /// file-name), <paramref name="fd"/> the FD record description(s), and <paramref name="proc"/> the procedure body.
    /// A distinct ASSIGN target per test keeps the host files from colliding across the (isolated) runs.</summary>
    private static string Program(string select, string fd, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FIOTEST.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F {select}.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        {fd}
        WORKING-STORAGE SECTION.
        01 WS-EOF PIC X VALUE "N".
        {ws}
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    // The read-back fields are displayed as the LAST operand so the comparison is unaffected by the legacy's known
    // DISPLAY trailing-space non-conformance (a bracketed field would expose internal trailing spaces — see
    // CutRunner.Normalize / CompilerUnderTest); the per-line trailing trim then washes the field width out.
    [Fact]
    public void WriteThenReadBack_RecordSequential()
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT1\"", "01 F-REC PIC X(10).", "",
            """
                OPEN OUTPUT F.
                MOVE "HELLO" TO F-REC. WRITE F-REC.
                MOVE "WORLD" TO F-REC. WRITE F-REC.
                CLOSE F.
                OPEN INPUT F.
                PERFORM UNTIL WS-EOF = "Y"
                    READ F AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "R=" F-REC
                    END-READ
                END-PERFORM.
                CLOSE F.
            """));

    [Fact]
    public void WriteFrom_AndReadInto()
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT2\"", "01 F-REC PIC X(8).",
            "01 WS-SEND PIC X(8) VALUE \"ABCDEFGH\".\n01 WS-RECV PIC X(8).",
            """
                OPEN OUTPUT F.
                WRITE F-REC FROM WS-SEND.
                CLOSE F.
                OPEN INPUT F.
                READ F INTO WS-RECV AT END MOVE "Y" TO WS-EOF
                    NOT AT END DISPLAY "GOT=" WS-RECV
                END-READ.
                CLOSE F.
            """));

    [Fact]
    public void Extend_AppendsAfterExistingRecords()
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT3\"", "01 F-REC PIC X(4).", "",
            """
                OPEN OUTPUT F. MOVE "AAAA" TO F-REC. WRITE F-REC. CLOSE F.
                OPEN EXTEND F. MOVE "BBBB" TO F-REC. WRITE F-REC. CLOSE F.
                OPEN INPUT F.
                PERFORM UNTIL WS-EOF = "Y"
                    READ F AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY F-REC
                    END-READ
                END-PERFORM.
                CLOSE F.
            """));

    [Fact]
    public void FileStatus_SuccessAndEof()
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT4\" FILE STATUS IS WS-ST", "01 F-REC PIC X(3).",
            "01 WS-ST PIC XX.",
            """
                OPEN OUTPUT F. DISPLAY "OPEN=" WS-ST.
                MOVE "XYZ" TO F-REC. WRITE F-REC. DISPLAY "WRITE=" WS-ST.
                CLOSE F.
                OPEN INPUT F.
                READ F AT END CONTINUE NOT AT END CONTINUE END-READ. DISPLAY "READ1=" WS-ST.
                READ F AT END CONTINUE NOT AT END CONTINUE END-READ. DISPLAY "READ2=" WS-ST.
                CLOSE F.
            """));

    [Fact]
    public void OptionalAbsent_OpenInput_IsAtEnd()
        // SELECT OPTIONAL precedes the file-name (ISO §12.4.5.2); an OPTIONAL file absent at OPEN INPUT opens with
        // status 05 and the first READ raises AT END (ISO §9.1.13.2).
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOOPT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT OPTIONAL F ASSIGN TO "FIO-NOEXIST-XYZ".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(5).
            PROCEDURE DIVISION.
            MAIN.
                OPEN INPUT F.
                READ F AT END DISPLAY "ATEND"
                    NOT AT END DISPLAY "GOT " F-REC
                END-READ.
                CLOSE F.
                STOP RUN.
            """);

    [Fact]
    public void MultipleRecordsUnderOneFd_ShareTheArea()
        // ISO §9.1.2: two 01s under one FD occupy the same record area — MOVE into one, WRITE the other writes the
        // same bytes (the NC101A PRINT-REC / DUMMY-RECORD pattern).
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT5\"", "01 REC-A PIC X(6).\n01 REC-B PIC X(6).", "",
            """
                OPEN OUTPUT F.
                MOVE "SHARED" TO REC-A.
                WRITE REC-B.
                CLOSE F.
                OPEN INPUT F.
                READ F AT END MOVE "Y" TO WS-EOF NOT AT END DISPLAY "READ=" REC-A END-READ.
                CLOSE F.
            """));

    [Fact]
    public void MultipleRecordsUnderOneFd_TierBSharedArea()
        // Two 01s under one FD with DIFFERENT layouts (a group + an elementary X) share the area as a synthesized
        // Tier-B redefines (ISO §9.1.2; COBOLNET_DESIGN §4.2): the first record (a group) is a view over the ONE
        // backing, so WRITE writes the backing window and a READ distributes the image INTO the backing (not a struct
        // FromImage) — the EmitImageInto view path. The leaves then read it back.
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT7\"",
            "01 REC-A.\n   03 RA-1 PIC X(3).\n   03 RA-2 PIC X(3).\n01 REC-B PIC X(6).", "",
            """
                OPEN OUTPUT F.
                MOVE "FOOBAR" TO REC-B.
                WRITE REC-A.
                CLOSE F.
                OPEN INPUT F.
                READ F AT END MOVE "Y" TO WS-EOF
                    NOT AT END DISPLAY "A=" RA-1 "/" RA-2 END-READ.
                CLOSE F.
            """));

    [Fact]
    public void WriteAfterAdvancing_LineSequentialReadBack()
        // A printer-style WRITE … AFTER ADVANCING stream read back line-by-line (LINE SEQUENTIAL): the advancing
        // newline structure (a leading blank line per AFTER) is observable as the read records.
        // kb/Work PB688: ORGANIZATION LINE SEQUENTIAL is a COBOL-2023 introduction (ISO §12.4.5.10.3 GR2),
        // so this case compiles at 2023 rather than the class default 85; the golden carries an explicit NAME
        // because the default one hashes the EDITION into the file name.
        => DifferentialGolden.Assert(Program("ASSIGN TO \"FIO-RT6\" ORGANIZATION IS LINE SEQUENTIAL", "01 F-REC PIC X(5).", "",
            """
                OPEN OUTPUT F.
                MOVE "LINE1" TO F-REC. WRITE F-REC AFTER ADVANCING 1 LINES.
                MOVE "LINE2" TO F-REC. WRITE F-REC AFTER ADVANCING 1 LINES.
                CLOSE F.
                OPEN INPUT F.
                PERFORM UNTIL WS-EOF = "Y"
                    READ F AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "R=" F-REC
                    END-READ
                END-PERFORM.
                CLOSE F.
            """), edition: 2023,
            goldenName: "write_after_advancing_line_sequential_read_back");

    /// <summary>§14.9.46 — WRITE {BEFORE|AFTER} ADVANCING mnemonic-name: the positioning is IMPLEMENTOR-DEFINED
    /// for the associated feature; the feature-name CSP's rule (docs/CONFORMANCE.md §7 item 190, kb/Work PB862 —
    /// the rule the SQ207M golden encodes) is a ZERO-line advance, which is ADVANCING 0's overprint written as a
    /// carriage return (owner decision kb/Work R54, PB1667) — a BEFORE-mnemonic write returns to the start of its
    /// line, so the NEXT write overprints it. (The entry used to name PRT-CHAN, which was never a feature-name: any
    /// word was accepted.)
    /// SPEC-PINNED (not legacy-differential) because the record is ALWAYS released (§14.9.46 GR1 — the WRITE
    /// transfers the record regardless of positioning): the legacy DROPS an AFTER-mnemonic write entirely, a
    /// non-conformance its goldens fossilize (SQ207M is swept-only pending re-baseline). The last WRITE also
    /// pins §8.4.2.2 FILE-NAME qualification of a record name (<c>WRITE P-REC IN P-OUT</c> — SQ207M's shape).</summary>
    [Fact]
    public void WriteAdvancingMnemonic_ZeroLineAdvance_RecordAlwaysReleased()
    {
        // kb/Work PB688: the read-back SELECT is ORGANIZATION LINE SEQUENTIAL, a COBOL-2023 introduction
        // (ISO §12.4.5.10.3 GR2) — the positioning rule under test is edition-invariant, the OBSERVATION is not.
        var (cok, cout, cdetail) = CobolNet2023.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOMNADV1.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CSP IS MN-ADV.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT P-OUT ASSIGN TO "FIOMNADV1F".
                SELECT P-IN ASSIGN TO "FIOMNADV1F" ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD P-OUT.
            01 P-REC PIC X(8).
            FD P-IN.
            01 IN-REC PIC X(20).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC X VALUE "N".
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT P-OUT.
                MOVE "AAAA" TO P-REC. WRITE P-REC BEFORE ADVANCING MN-ADV.
                MOVE "BBBB" TO P-REC. WRITE P-REC BEFORE ADVANCING 1 LINE.
                MOVE "CCCC" TO P-REC. WRITE P-REC AFTER ADVANCING MN-ADV.
                MOVE "DDDD" TO P-REC.
                WRITE P-REC IN P-OUT BEFORE ADVANCING 1 LINE.
                CLOSE P-OUT.
                OPEN INPUT P-IN.
                PERFORM UNTIL WS-EOF = "Y"
                    READ P-IN
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "L=" IN-REC
                    END-READ
                END-PERFORM.
                CLOSE P-IN.
                STOP RUN.
            """);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        // Two lines and no third: the last WRITE is BEFORE ADVANCING 1 LINE, whose own advance terminates the line,
        // and nothing in §14.9.51.4 adds a terminator at CLOSE. This expectation used to carry a third, empty record
        // ("\nL=") - the spurious CLOSE-time line terminator kb/Work PB864 removed (its sibling
        // LinageConformanceTests.Bytes_OverflowWithBeforePhrase_PresentsThenRepositions pinned the same defect).
        // The first record is "AAAA" CR "BBBB": BEFORE ADVANCING CSP presents AAAA and then writes the zero advance's
        // carriage return (§14.9.51.4 GR25 c) — an overprint; docs/CONFORMANCE.md "ADVANCING 0"), a lone CR is DATA
        // to a line sequential read (kb/Work PB1540), and the harness's normalization shows it as a line break.
        // The second record, "CCCC" then "DDDD": AFTER ADVANCING CSP finds no open line (BBBB's own advance ended
        // it), so there is nothing to overprint and nothing is written, and DDDD is presented on CCCC's line.
        Assert.Equal("L=AAAA\nBBBB\nL=CCCCDDDD", cout);
    }

    /// <summary>The feature-name <c>C01</c> (docs/CONFORMANCE.md §7, Annex A.1 items 190 and 222 — kb/Work PB862):
    /// "skip to channel 1", the top of the next page — WiseOwl COBOL's §14.9.51.4 GR25 d rule for it is EXACTLY the
    /// <c>ADVANCING PAGE</c> advance in the same position. So the same records written once through a C01 mnemonic
    /// and once through PAGE must read back identically, BEFORE and AFTER alike. Spec-derived from the determination,
    /// not measured: the assertion is the equality, never a captured byte stream.</summary>
    [Fact]
    public void WriteAdvancingC01_IsThePageAdvance()
    {
        var (cok, cout, cdetail) = CobolNet2023.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOC01PG.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                C01 IS TOP-PAGE.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT M-OUT ASSIGN TO "FIOC01PGM".
                SELECT P-OUT ASSIGN TO "FIOC01PGP".
                SELECT M-IN ASSIGN TO "FIOC01PGM" ORGANIZATION IS LINE SEQUENTIAL.
                SELECT P-IN ASSIGN TO "FIOC01PGP" ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD M-OUT.
            01 M-REC PIC X(4).
            FD P-OUT.
            01 P-REC PIC X(4).
            FD M-IN.
            01 MI-REC PIC X(20).
            FD P-IN.
            01 PI-REC PIC X(20).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC X.
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT M-OUT P-OUT.
                MOVE "AAAA" TO M-REC P-REC.
                WRITE M-REC AFTER ADVANCING TOP-PAGE.
                WRITE P-REC AFTER ADVANCING PAGE.
                MOVE "BBBB" TO M-REC P-REC.
                WRITE M-REC BEFORE ADVANCING TOP-PAGE.
                WRITE P-REC BEFORE ADVANCING PAGE.
                MOVE "CCCC" TO M-REC P-REC.
                WRITE M-REC.
                WRITE P-REC.
                CLOSE M-OUT P-OUT.
                MOVE "N" TO WS-EOF.
                OPEN INPUT M-IN.
                PERFORM UNTIL WS-EOF = "Y"
                    READ M-IN AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "M=" MI-REC
                    END-READ
                END-PERFORM.
                CLOSE M-IN.
                MOVE "N" TO WS-EOF.
                OPEN INPUT P-IN.
                PERFORM UNTIL WS-EOF = "Y"
                    READ P-IN AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "P=" PI-REC
                    END-READ
                END-PERFORM.
                CLOSE P-IN.
                STOP RUN.
            """);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        var lines = cout.Split('\n');
        var viaC01 = lines.Where(l => l.StartsWith("M=", StringComparison.Ordinal)).Select(l => l[2..]).ToList();
        var viaPage = lines.Where(l => l.StartsWith("P=", StringComparison.Ordinal)).Select(l => l[2..]).ToList();
        Assert.NotEmpty(viaPage);
        Assert.Equal(viaPage, viaC01);
    }

    /// <summary>READ on a file connector that is NOT open: I-O status '47' (§9.1.13.7 item 7 / §14.9.30 GR2),
    /// the statement is unsuccessful, and AT END does NOT fire ('47' is not the at-end family, §9.1.13.4).
    /// SPEC-PINNED: the record area's content after an unsuccessful READ is spec-UNDEFINED (§14.9.30 GR18
    /// "unless otherwise specified…"); WiseOwl COBOL's documented refinement is that the area is UNCHANGED —
    /// extending the spec's own rule for every other unsuccessful I-O verb (REWRITE GR14 / WRITE GR15 /
    /// DELETE GR8 / START GR2: "unaffected"). The legacy LOW-VALUE-filled it (a byte-engine artifact — the
    /// ST146A golden was re-baselined over it, DEVLOG 570), so this is not legacy-differential.</summary>
    [Fact]
    public void ReadNotOpen_Status47_AtEndNotTaken_RecordAreaUnchanged()
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIORD47.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "FIORD47F" FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(10).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT F.
                MOVE "KEEPSAKE" TO F-REC.
                WRITE F-REC.
                CLOSE F.
                READ F AT END DISPLAY "AT-END TAKEN".
                DISPLAY "FS=" WS-FS.
                DISPLAY "REC=" F-REC.
                STOP RUN.
            """);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal("FS=47\nREC=KEEPSAKE", cout);
    }
}
