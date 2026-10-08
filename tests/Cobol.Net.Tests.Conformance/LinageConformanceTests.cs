// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The LINAGE logical-page subsystem (ISO/IEC 1989:2023 §13.18.34 LINAGE clause, §8.4.3.14 LINAGE-COUNTER,
/// §14.9.51 WRITE GR25–GR28): the per-GR conformance net for the counter state machine, the GR26a/GR26b
/// end-of-page discrimination, and the GR6b data-name re-evaluation timing. Every behavioral test here is
/// SPEC-PINNED (expected values derived from the cited rules, not the former legacy engine): the legacy evaluates
/// LINAGE data-names ONLY at OPEN OUTPUT — a verified hole vs §13.18.34 GR6b2/GR6b3 (the SQ208M/SQ210M golden
/// re-baselines) — so it cannot be the authority for this subsystem.
///
/// <para>⛔ THE OBSERVABLE SURFACE IS TWO SURFACES, AND FOR YEARS THIS CLASS HAD ONLY ONE. Most tests here read
/// the LINAGE-COUNTER register and the END-OF-PAGE branches through stdout; that is blind to WHERE ON THE MEDIUM
/// a record lands, and the class comment used to state the blindness as a property of the FEATURE — "the physical
/// stream is counter-only (§13.18.34 GR8 — pages are contiguous, no margin spacing)". It is not. §13.18.34.4 GR1
/// makes the logical page the SUM of top margin + page body + bottom margin, GR4/GR5 name those margins as lines
/// OF that page, and GR8's "no additional spacing" forbids spacing BEYOND the logical page rather than deleting
/// the margins the page contains; §14.9.51.4 GR25 g) then repositions an ADVANCING PAGE write to the next logical
/// page instead of emitting a form feed. Under the counter-only reading GR4 and GR5 had no content at all, and
/// `LINES AT TOP` / `LINES AT BOTTOM` changed no byte of the output for the whole life of the connector while
/// every test here stayed green (kb/Work PB523). The <c>Bytes_*</c> tests below close that hole: they assert the
/// BYTES of the produced file through <see cref="CobolNetCompiler.CompileRunAndReadFile"/>, and a counter-only
/// implementation cannot pass one of them.</para>
/// </summary>
public sealed class LinageConformanceTests
{
    private static readonly CobolNetCompiler CobolNet = new();

    /// <summary>Compile-and-run on the greenfield compiler; assert the spec-derived stdout.</summary>
    private static void AssertSpec(string source, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(expected, stdout);
    }

    /// <summary>A one-LINAGE-file program: <paramref name="fdClauses"/> is the FD clause text (the LINAGE
    /// clause under test), <paramref name="ws"/> extra WORKING-STORAGE, <paramref name="proc"/> the COMPLETE
    /// procedure body (including CLOSE/STOP RUN, so tests may append helper paragraphs).</summary>
    private static string Program(string fdClauses, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. LNGTST.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT LPF ASSIGN TO "LNG-OUT".
        DATA DIVISION.
        FILE SECTION.
        FD LPF
            {fdClauses}.
        01 P-REC PIC X(20).
        WORKING-STORAGE SECTION.
        01 LC-VAL PIC 9(3).
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT LPF.
            MOVE SPACE TO P-REC.
        {proc}
        """;

    // ── The BYTES on the medium (§13.18.34.4 GR1/GR4/GR5/GR8, §14.9.51.4 GR25 g)/GR26 a)) ─────────────────

    /// <summary><see cref="CobolNet"/> at the shipping default edition, for the one fixture that needs a post-85 construct:
    /// ORGANIZATION IS LINE SEQUENTIAL is COBOL-2023 (§12.4.5.10.3; COBOLNET0900 rejects it at --std 85). The
    /// LINAGE rules under test are identical across all four editions, so the arm is exercised where it exists.</summary>
    private static readonly CobolNetCompiler CobolNet2023 = new(2023);

    /// <summary>Compile-and-run, then assert the EXACT bytes the program left on the medium. The expected string
    /// is written with explicit <c>\r\n</c> for every LINE END (a margin line, a page-fill line, an ADVANCING
    /// line, a record's delimiter), which this helper maps to the bytes the file's SHAPE ends its lines with — the
    /// three-way mapping of docs/CONFORMANCE.md DOC-A.1-146 (c′) (kb/Work PB1664): a LINE SEQUENTIAL file and a
    /// file with a LINAGE clause end them with the HOST newline (DOC-A.1-114; kb/Work PB1540), and any other
    /// record sequential print stream — one written with an ADVANCING phrase — ends them with a line feed on
    /// every host. A lone <c>\r</c> (ADVANCING 0's overprint) and <c>\f</c> (ADVANCING PAGE) are controls, not
    /// line ends, and are asserted as written.</summary>
    private static void AssertBytes(string source, string fileName, string expected, int edition = 85, string org = "")
    {
        var (ok, _, detail, bytes) =
            (edition == 85 ? CobolNet : CobolNet2023).CompileRunAndReadFile(source, fileName);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.NotNull(bytes);
        bool hostNewline = org.Contains("LINE SEQUENTIAL", StringComparison.Ordinal)
            || source.Contains("LINAGE IS", StringComparison.Ordinal);
        expected = expected.Replace("\r\n", hostNewline ? Environment.NewLine : "\n", StringComparison.Ordinal);
        Assert.Equal(expected, System.Text.Encoding.Latin1.GetString(bytes!));
    }

    private const string LineSequentialOrg = "\n        ORGANIZATION IS LINE SEQUENTIAL";

    /// <summary>A LINAGE print program over one file: <paramref name="assign"/> is both the ASSIGN literal and
    /// the host file name, <paramref name="org"/> an ORGANIZATION phrase (or empty).</summary>
    private static string BytesProgram(string programId, string assign, string org, string fdClauses, string proc)
        => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {programId}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT LPF ASSIGN TO "{assign}"{org}.
        DATA DIVISION.
        FILE SECTION.
        FD LPF
            {fdClauses}.
        01 P-REC PIC X(4).
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT LPF.
        {proc}
            CLOSE LPF.
            STOP RUN.
        """;

    [Fact]
    // ⛔ THE TOP AND BOTTOM MARGINS ARE LINES OF THE LOGICAL PAGE, ON THE MEDIUM (kb/Work PB523).
    // FD: LINAGE IS 2 LINES LINES AT TOP 3 LINES AT BOTTOM 2. Derivation, entirely from §13.18.34.4:
    //   GR1  logical page size = 3 + 2 + 2 = 7 lines ("the sum of the values referenced by each phrase except
    //        the FOOTING phrase"), so page 1 is physical lines 1-7 and — GR8, "each logical page is contiguous
    //        to the next with no additional spacing provided" — page 2 begins at physical line 8.
    //   GR4  top margin = 3 lines: page-1 physical lines 1-3, page-2 lines 8-10.
    //   GR2  page body = 2 writable lines: page-1 physical 4-5 (body lines 1-2), page-2 physical 11-12.
    //   GR5  bottom margin = 2 lines: page-1 physical 6-7.
    //   GR7d counter := 1 at OPEN OUTPUT (device at body line 1).
    // W1 AFTER ADVANCING 1 (§14.9.51.4 GR25 f) advance then present, §13.18.34.4 GR7 c) 2 counter += 1): counter 1→2, so
    //    AAAA is on page-1 body line 2 = physical line 5.
    // W2: counter would be 3, past the page size — §14.9.51.4 GR26 a) page overflow, AFTER phrase, so "the
    //    device is repositioned to the first line that may be written on the next logical page and the logical
    //    record is presented on that line": page-2 body line 1 = physical line 11. GR7 c) 4 resets the counter.
    // W3: counter 1→2 ⇒ page-2 body line 2 = physical line 12.
    // Five blank lines therefore separate AAAA from BBBB (page-1 bottom margin 2 + page-2 top margin 3) — the
    // exact quantity a counter-only implementation writes as one.
    public void Bytes_TopAndBottomMarginsAreLinesOfTheLogicalPage()
        => AssertBytes(BytesProgram("LNGBY1", "lngby1.prt", "", "LINAGE IS 2 LINES LINES AT TOP 3 LINES AT BOTTOM 2", """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
                MOVE "CCCC" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
            """), "lngby1.prt",
            "\r\n\r\n\r\n\r\nAAAA\r\n\r\n\r\n\r\n\r\n\r\nBBBB\r\nCCCC\r\n");

    [Fact]
    // ⛔ ZERO MARGINS ARE THE IDENTITY — the same program with no TOP/BOTTOM phrase produces the print stream
    // it always produced. §13.18.34.4 GR1: "If the LINES AT TOP or LINES AT BOTTOM phrases are not specified,
    // the values of these items are zero", so the logical page is 0 + 2 + 0 and every page transition costs
    // exactly the one line GR8 makes it. This is the regression guard for the whole print corpus: the margin
    // model may not add a byte to a file that declared no margins.
    public void Bytes_NoMarginPhrases_LeaveThePrintStreamUnchanged()
        => AssertBytes(BytesProgram("LNGBY2", "lngby2.prt", "", "LINAGE IS 2 LINES", """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
                MOVE "CCCC" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
            """), "lngby2.prt",
            "\r\nAAAA\r\nBBBB\r\nCCCC\r\n");

    [Fact]
    // ⛔ ADVANCING PAGE ON A LINAGE FILE IS A REPOSITION, NEVER A FORM FEED. §14.9.51.4 GR25 g): "If PAGE is
    // specified and the LINAGE clause is specified in the associated file description entry, the record is
    // presented on the logical page before or after (depending on the phrase used) the device is repositioned
    // to the next logical page. The repositioning is to the first line that may be written on the next logical
    // page as specified in the LINAGE clause." The form feed is GR25 h), the arm for a file with NO LINAGE.
    // FD: LINAGE IS 3 LINES LINES AT TOP 2 LINES AT BOTTOM 1 ⇒ logical page = 6 (GR1); page 1 = physical 1-6,
    // page 2 = 7-12 with top margin 7-8 and body 9-11 (GR2/GR4/GR5/GR8).
    // W1 AFTER ADVANCING PAGE: the device leaves page-1 body line 1 (GR7 d) and is repositioned to page-2 body
    //    line 1 = physical line 9; AAAA is presented there (AFTER ⇒ reposition then present, GR25 f)/g)).
    // W2 AFTER ADVANCING 1: counter 1→2 ⇒ page-2 body line 2 = physical line 10.
    // Eight leading blank lines and not one 0x0C anywhere.
    public void Bytes_AdvancingPage_RepositionsToTheNextLogicalPage_NotAFormFeed()
        => AssertBytes(BytesProgram("LNGBY3", "lngby3.prt", "", "LINAGE IS 3 LINES LINES AT TOP 2 LINES AT BOTTOM 1", """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC AFTER ADVANCING PAGE.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
            """), "lngby3.prt",
            "\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\nAAAA\r\nBBBB\r\n");

    [Fact]
    // §14.9.51.4 GR25 g)'s OTHER half — "the record is presented on the logical page BEFORE ... the device is
    // repositioned": BEFORE ADVANCING PAGE prints on the page it is standing on and only then leaves it. Same
    // FD as above (logical page 6 = 2 + 3 + 1).
    // W1 BEFORE ADVANCING PAGE: AAAA is presented at page-1 body line 1 = physical line 3 (the top margin, GR4,
    //    reaches the medium ahead of it), then the device is repositioned to page-2 body line 1 = physical 9.
    // W2 AFTER ADVANCING 1: counter 1→2 ⇒ page-2 body line 2 = physical line 10, so six blank lines separate
    //    them — page-1 body lines 2-3 unwritten, its bottom margin (1), page-2's top margin (2).
    public void Bytes_BeforeAdvancingPage_PresentsOnTheCurrentPageThenRepositions()
        => AssertBytes(BytesProgram("LNGBY4", "lngby4.prt", "", "LINAGE IS 3 LINES LINES AT TOP 2 LINES AT BOTTOM 1", """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC BEFORE ADVANCING PAGE.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
            """), "lngby4.prt",
            "\r\n\r\nAAAA\r\n\r\n\r\n\r\n\r\n\r\n\r\nBBBB\r\n");

    [Fact]
    // §14.9.51.4 GR26 a)'s OTHER ordering, the one only a BEFORE phrase reaches: "If the BEFORE phrase is
    // specified, the logical record is presented and the device is repositioned to the first line that may be
    // written on the next logical page." The AFTER arm can never show this, because it repositions first and so
    // never presents on the LAST line of a page body.
    // FD: LINAGE IS 2 LINES LINES AT TOP 3 LINES AT BOTTOM 2 (page 1 = physical 1-7, page 2 starts at 8).
    // W1 BEFORE ADVANCING 1: AAAA is presented where the device stands — page-1 body line 1 = physical 4, behind
    //    the top margin (GR4) — then the counter/device advance to body line 2.
    // W2: BBBB is presented on page-1 body line 2 = physical 5, and only THEN does its advance overflow
    //    (GR26 a)) across the bottom margin and the next top margin.
    // W3: CCCC on page-2 body line 1 = physical 11.
    // The file ENDS at CCCC's own advance: GR25 e) presents the line and then advances, so that travel already
    // terminated physical line 11 and CLOSE adds nothing (kb/Work PB864 — CLOSE used to write one more line
    // terminator on any connector that had seen a print-control WRITE, a blank line the program never wrote;
    // this expectation carried it, with a warning that it was not derived from the standard).
    public void Bytes_OverflowWithBeforePhrase_PresentsThenRepositions()
        => AssertBytes(BytesProgram("LNGBY6", "lngby6.prt", "", "LINAGE IS 2 LINES LINES AT TOP 3 LINES AT BOTTOM 2", """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC BEFORE ADVANCING 1 LINE.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC BEFORE ADVANCING 1 LINE.
                MOVE "CCCC" TO P-REC.
                WRITE P-REC BEFORE ADVANCING 1 LINE.
            """), "lngby6.prt",
            "\r\n\r\n\r\nAAAA\r\nBBBB\r\n\r\n\r\n\r\n\r\n\r\nCCCC\r\n");

    [Fact]
    // kb/Work PB864, on a print file with NO LINAGE clause — the plain print stream, where the one-flag defect
    // was not tied to a logical page at all. §14.9.51.4 GR25 e): "If the BEFORE phrase is used, the line is
    // presented before the representation of the printed page is advanced", so each record is followed by its
    // own advance and the last advance ends the file. No trailing blank line.
    public void Bytes_BeforeAdvancing_LastLineEndedByItsOwnAdvance_CloseAddsNothing()
        => AssertBytes(BytesProgram("LNGBY7", "lngby7.prt", "", "RECORD CONTAINS 4 CHARACTERS", """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC BEFORE ADVANCING 1 LINE.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC BEFORE ADVANCING 2 LINES.
            """), "lngby7.prt",
            "AAAA\r\nBBBB\r\n\r\n");

    [Fact]
    // The other half of the split (kb/Work PB864): the LAST write is AFTER-placed (GR25 f) — advance, then
    // present), so its line is still open at CLOSE and CLOSE terminates exactly that one line. A BEFORE write
    // earlier in the file does not change the answer: the question CLOSE asks is about the CURRENT line.
    public void Bytes_AfterAdvancingLast_CloseTerminatesTheOpenLineOnce()
        => AssertBytes(BytesProgram("LNGBY8", "lngby8.prt", "", "RECORD CONTAINS 4 CHARACTERS", """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC BEFORE ADVANCING 1 LINE.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
            """), "lngby8.prt",
            "AAAA\r\n\r\nBBBB\r\n");

    // ── kb/Work PB964: a WRITE with no ADVANCING phrase after an AFTER-placed record ─────────────────────────
    // §14.9.51.4 GR25: "If the ADVANCING phrase is not used, automatic advancing shall be provided by the
    // implementor to act as if the user has specified AFTER ADVANCING 1 LINE", and GR25 f): "the line is
    // presented after the representation of the printed page is advanced". An AFTER write leaves its record on
    // an unterminated line; the plain WRITE's one advance ends that line and the record lands on the next one.
    // Before PB964 the print arm presented first (a BEFORE placement) and the line sequential arm never advanced,
    // so both welded the two records onto one physical line — invisible to a same-width COBOL read-back, which
    // is why these pin BYTES. One case per write arm: print stream, LINAGE page, line sequential, line sequential
    // with LINAGE, and PAGE (a form feed, GR25 h)) as the AFTER write. Since PB1027 the plain WRITE takes the
    // explicit AFTER path in EVERY state of the device, not only after an AFTER write, and
    // PlainWriteEqualsAfterOneDriftTests holds the two spellings byte-equal over every placement mix.

    [Theory]
    [InlineData("LNGBY9", "lngby9.prt", "", "RECORD CONTAINS 4 CHARACTERS", 85)]
    [InlineData("LNGBYA", "lngbya.prt", "", "LINAGE IS 10 LINES", 85)]
    // A LINE SEQUENTIAL file's FD carries no RECORD CONTAINS (§13.4.5.3 SR4 — "neither the BLOCK CONTAINS clause
    // nor the RECORD CONTAINS clause shall be specified"; COBOLNET2605), so its record width is the 01's alone.
    [InlineData("LNGBYB", "lngbyb.prt", "\n        ORGANIZATION IS LINE SEQUENTIAL", "", 2023)]
    [InlineData("LNGBYC", "lngbyc.prt", "\n        ORGANIZATION IS LINE SEQUENTIAL", "LINAGE IS 10 LINES", 2023)]
    public void Bytes_PlainWriteAfterAnAfterWrite_AdvancesFirst_NoWeld(
        string programId, string file, string org, string fd, int edition)
        => AssertBytes(BytesProgram(programId, file, org, fd, """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC AFTER ADVANCING 1 LINE.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC.
                MOVE "CCCC" TO P-REC.
                WRITE P-REC.
            """), file,
            "\r\nAAAA\r\nBBBB\r\nCCCC\r\n", edition, org);

    [Theory]
    [InlineData("LNGBYD", "lngbyd.prt", "", 85)]
    [InlineData("LNGBYE", "lngbye.prt", "\n        ORGANIZATION IS LINE SEQUENTIAL", 2023)]
    public void Bytes_PlainWriteAfterAdvancingPage_AdvancesFirst_NoWeld(string programId, string file, string org, int edition)
        => AssertBytes(BytesProgram(programId, file, org, org.Length == 0 ? "RECORD CONTAINS 4 CHARACTERS" : "", """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC AFTER ADVANCING PAGE.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC.
            """), file,
            "\fAAAA\r\nBBBB\r\n", edition, org);

    [Theory]
    // The OTHER placements keep their own answers. A BEFORE write after an AFTER write presents on the line the
    // device stands on (GR25 e): "the line is presented before the representation of the printed page is
    // advanced") — the same line, appended to it (GnuCOBOL's behaviour, kb/Work PB1840 holds the overprint
    // question); AFTER ADVANCING 0 LINES is an OVERPRINT (GR25 c): "no repositioning of the representation of the
    // printed page is performed"), written as a bare carriage return (docs/CONFORMANCE.md "ADVANCING 0", owner
    // decision kb/Work R54), so BBBB returns to the start of AAAA's line; a BEFORE write followed by an AFTER
    // write is two advances, so one blank line.
    // ⛔ AND THE LAST WRITE, WITH NO ADVANCING PHRASE, IS AN EXPLICIT AFTER ADVANCING 1 LINE (kb/Work PB1027;
    // GR25: "automatic advancing shall be provided by the implementor to act as if the user has specified AFTER
    // ADVANCING 1 LINE"): after a BEFORE write, whose own advance already ended the line, it advances AGAIN, so
    // CCCC follows a blank line exactly as LNGBYG's explicit AFTER does. (Before PB1027 it took BEFORE's
    // placement whenever no line was open, which left no blank line here.)
    [InlineData("LNGBYF", "lngbyf.prt", "AFTER ADVANCING 1 LINE", "BEFORE ADVANCING 1 LINE", "\r\nAAAABBBB\r\n\r\nCCCC\r\n")]
    [InlineData("LNGBYG", "lngbyg.prt", "BEFORE ADVANCING 1 LINE", "AFTER ADVANCING 1 LINE", "AAAA\r\n\r\nBBBB\r\nCCCC\r\n")]
    [InlineData("LNGBYH", "lngbyh.prt", "BEFORE ADVANCING 1 LINE", "", "AAAA\r\n\r\nBBBB\r\nCCCC\r\n")]
    [InlineData("LNGBYI", "lngbyi.prt", "AFTER ADVANCING 1 LINE", "AFTER ADVANCING 0 LINES", "\r\nAAAA\rBBBB\r\nCCCC\r\n")]
    public void Bytes_LineSequential_EveryPlacementMix(string programId, string file, string first, string second, string expected)
        => AssertBytes(BytesProgram(programId, file, LineSequentialOrg, "", $"""
                MOVE "AAAA" TO P-REC.
                WRITE P-REC {first}.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC {second}.
                MOVE "CCCC" TO P-REC.
                WRITE P-REC.
            """), file, expected, 2023, LineSequentialOrg);

    [Fact]
    // ⛔ THE SECOND ARM OF THE WRITE DISPATCH — a LINE SEQUENTIAL LINAGE file gets the SAME logical page. This is
    // the arm kb/Work PB523 found unfixed while its record-sequential twin was fixed: a plain WRITE there emitted
    // the record and a bare newline, so the margins never reached a line sequential medium. §14.9.51.4 GR25 makes
    // an omitted ADVANCING phrase a one-line advance and §13.18.34.4 GR7 c) 3 adds one to the counter, so that
    // newline IS the device travelling one line on the logical page.
    // FD: LINAGE IS 2 LINES LINES AT TOP 3 LINES AT BOTTOM 2 (page 1 = physical 1-7, page 2 starts at 8).
    // A plain WRITE is an AFTER ADVANCING 1 LINE (§14.9.51.4 GR25: "automatic advancing shall be provided by the
    // implementor to act as if the user has specified AFTER ADVANCING 1 LINE"; kb/Work PB1027), so the file is
    // BYTE FOR BYTE the one Bytes_TopAndBottomMarginsAreLinesOfTheLogicalPage derives for the same FD with the
    // phrase spelled out: AAAA lands on page-1 body line 2 = physical 5 (GR25 f): the counter was 1 at OPEN,
    // §13.18.34.4 GR7 d), and the advance makes it 2, GR7 c) 3), BBBB's advance would reach body line 3, past the
    // page size, so GR26 a) repositions to page-2 body line 1 = physical 11 and presents it there, and CCCC lands
    // on body line 2 = physical 12.
    public void Bytes_LineSequentialLinageFile_GetsTheSameMargins()
        => AssertBytes(BytesProgram("LNGBY5", "lngby5.prt", LineSequentialOrg,
            "LINAGE IS 2 LINES LINES AT TOP 3 LINES AT BOTTOM 2", """
                MOVE "AAAA" TO P-REC.
                WRITE P-REC.
                MOVE "BBBB" TO P-REC.
                WRITE P-REC.
                MOVE "CCCC" TO P-REC.
                WRITE P-REC.
            """), "lngby5.prt",
            "\r\n\r\n\r\n\r\nAAAA\r\n\r\n\r\n\r\n\r\n\r\nBBBB\r\nCCCC\r\n", edition: 2023, org: LineSequentialOrg);

    // ── GR7 counter rules (§13.18.34 GR7c1–c4 / GR7d) ─────────────────────────────────────────────────────

    [Fact]   // GR7d: LINAGE-COUNTER is set to one at OPEN OUTPUT.
    public void Gr7d_CounterIsOneAtOpenOutput()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=001");

    [Fact]   // GR7c3: a WRITE without the ADVANCING phrase increments the counter by one.
    public void Gr7c3_PlainWriteAddsOne()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=002");

    [Fact]   // GR7c2: WRITE ADVANCING n increments the counter by n.
    public void Gr7c2_AdvancingNAddsN()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING 3 LINES.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=004");

    [Fact]   // §14.9.51 GR25c: ADVANCING 0 performs no repositioning; the counter gains 0 (GR7c2).
    public void Gr25c_AdvancingZero_CounterUnchanged()
        => AssertSpec(Program("LINAGE IS 5 LINES WITH FOOTING AT 4", "", """
                WRITE P-REC AFTER ADVANCING 0 LINES.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=001");

    [Fact]   // GR7c1: WRITE ADVANCING PAGE resets the counter to one.
    public void Gr7c1_AdvancingPageResetsCounterToOne()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING 2 LINES.
                WRITE P-REC AFTER ADVANCING PAGE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=001");

    [Fact]   // GR7c4 + §14.9.51 GR26a: crossing the page body repositions to LINE ONE of the next page —
             // counter := 1, never a modulo carry (8 past a 5-line body is 1, not 3) — with an overflow EOP.
    public void Gr7c4_OverflowCrossing_ResetsToOneNotModulo()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING 3 LINES.
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP"
                    NOT AT EOP DISPLAY "NO-EOP"
                END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "EOP\nLC=001");

    // ── GR26a vs GR26b end-of-page discrimination (§14.9.51) ──────────────────────────────────────────────

    [Fact]   // GR26b + GR27b: printing within the footing area is a FOOTING end-of-page — the AT branch reads
             // the POST-advance counter of the triggering write, and the counter is NOT reset (no overflow).
    public void Gr26b_FootingEop_AtBranchReadsPostAdvanceCounter()
        => AssertSpec(Program("LINAGE IS 5 LINES WITH FOOTING AT 4", "", """
                WRITE P-REC AFTER ADVANCING 3 LINES
                    AT EOP MOVE LINAGE-COUNTER TO LC-VAL DISPLAY "EOP AT " LC-VAL
                    NOT AT EOP DISPLAY "NO-EOP"
                END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "AFTER=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "EOP AT 004\nAFTER=004");

    [Fact]   // ⚖ THE ADJUDICATED BOUNDARY — docs/CONFORMANCE.md §4 "DETERMINATION — the §14.9.51.4 GR26 a)/b)
             // boundary at LINAGE-COUNTER = page size" (kb/Work PB686). GR26a as PRINTED fires at counter ≥ page
             // size and GR26b is clamped to counter < page size; at counter == page size neither can hold with
             // §13.18.34 GR2 (all page-size lines may be written), GR3 (the footing area is [footing start, page
             // size] INCLUSIVE) or GR26's lead sentence. So a write landing exactly ON the page size is a FOOTING
             // end-of-page (counter stays), and overflow (GR26a) fires only when the positioning actually PASSES
             // the body (counter then resets to 1, GR7c4). ⛔ THIS EXPECTATION IS A DETERMINATION, NOT A LITERAL
             // RULE — if it ever goes red, read the determination before touching the connector. The corpus twin
             // 2023/pb686_linage_gr26_boundary (+ its 85 edition twin) covers the same boundary on the OTHER arm
             // of the FOOTING dispatch, where no FOOTING phrase is present at all.
    public void Gr26ab_CounterEqualsBody_IsFootingEopNotOverflow()
        => AssertSpec(Program("LINAGE IS 5 LINES WITH FOOTING AT 4", "", """
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP1" END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                WRITE P-REC
                    AT EOP DISPLAY "EOP2" END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "EOP1\nLC=005\nEOP2\nLC=001");

    [Fact]   // §13.18.34 GR1: no FOOTING phrase ⇒ no end-of-page condition independent of page overflow —
             // a write landing on the page size raises NOTHING (GR28: the NOT branch runs); only the
             // body-crossing write raises the (overflow) end-of-page.
             // ⚖ THE SECOND ARM OF THE PB686 BOUNDARY, and the one that shows the stakes: the determination
             // is not merely about which end-of-page NAME a FOOTING file gets. Under GR26a's printed "equal to
             // or exceeds the page size" this write — on a file that never mentions FOOTING — would overflow
             // and reset, so the last line of every page body would be unwritable. Fixing one arm's comparison
             // without this one is the repo's most reproducible defect shape.
    public void Gr1_NoFooting_EopIsOverflowOnly()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP1"
                    NOT AT EOP DISPLAY "NO-EOP1"
                END-WRITE.
                WRITE P-REC
                    AT EOP DISPLAY "EOP2"
                    NOT AT EOP DISPLAY "NO-EOP2"
                END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """), "NO-EOP1\nEOP2");

    // ── GR6b data-name evaluation timing (§13.18.34 GR6b1/2/3 — the legacy's verified hole) ───────────────

    [Fact]   // GR6b1: a data-name operand's value is read at the COMPLETION of OPEN OUTPUT — the pre-open
             // MOVE governs the first page; a post-open MOVE has no effect until a page transition (GR6b
             // "the value applies to the next logical page").
    public void Gr6b1_DataNamesReadAtOpenOutput_MutationWaitsForTransition()
        => AssertSpec($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LPF ASSIGN TO "LNG-OUT".
            DATA DIVISION.
            FILE SECTION.
            FD LPF
                LINAGE IS WS-SIZE LINES.
            01 P-REC PIC X(20).
            WORKING-STORAGE SECTION.
            01 LC-VAL PIC 9(3).
            01 WS-SIZE PIC 99 VALUE 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 3 TO WS-SIZE.
                OPEN OUTPUT LPF.
                MOVE SPACE TO P-REC.
                MOVE 99 TO WS-SIZE.
                WRITE P-REC AFTER ADVANCING 3 LINES
                    AT EOP DISPLAY "EOP1"
                    NOT AT EOP DISPLAY "NO-EOP1"
                END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP2"
                    NOT AT EOP DISPLAY "NO-EOP2"
                END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """,
            // Page 1 uses the open-time 3 (1+3 = 4 > 3 ⇒ overflow EOP, counter 1); the wrap re-evaluates
            // (GR6b3) ⇒ page 2 is 99 lines, so the 4-line advance stays inside it (NOT branch).
            "EOP1\nLC=001\nNO-EOP2");

    [Fact]   // GR6b3 + "applies to the next logical page": a mid-page MOVE never shrinks the CURRENT page —
             // the overflow decision is made against the OLD body, and the new values govern from the wrap on.
    public void Gr6b3_OverflowReEvaluation_AppliesToNextPage()
        => AssertSpec($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LPF ASSIGN TO "LNG-OUT".
            DATA DIVISION.
            FILE SECTION.
            FD LPF
                LINAGE IS WS-SIZE LINES.
            01 P-REC PIC X(20).
            WORKING-STORAGE SECTION.
            01 WS-SIZE PIC 99 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LPF.
                MOVE SPACE TO P-REC.
                MOVE 2 TO WS-SIZE.
                WRITE P-REC AFTER ADVANCING 2 LINES
                    AT EOP DISPLAY "EOP1"
                    NOT AT EOP DISPLAY "NO-EOP1"
                END-WRITE.
                WRITE P-REC AFTER ADVANCING 3 LINES
                    AT EOP DISPLAY "EOP2"
                    NOT AT EOP DISPLAY "NO-EOP2"
                END-WRITE.
                WRITE P-REC AFTER ADVANCING 2 LINES
                    AT EOP DISPLAY "EOP3"
                    NOT AT EOP DISPLAY "NO-EOP3"
                END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """,
            // 1+2 = 3 ≤ OLD body 5 ⇒ no EOP (the MOVE 2 did not shrink the current page); 3+3 = 6 > 5 ⇒
            // overflow against the OLD body, wrap to 1, re-evaluate ⇒ page = 2; 1+2 = 3 > 2 ⇒ overflow on
            // the NEW 2-line page.
            "NO-EOP1\nEOP2\nEOP3");

    [Fact]   // GR6b2: WRITE ADVANCING PAGE re-evaluates the data-names for the next logical page.
    public void Gr6b2_AdvancingPageReEvaluation()
        => AssertSpec($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LPF ASSIGN TO "LNG-OUT".
            DATA DIVISION.
            FILE SECTION.
            FD LPF
                LINAGE IS WS-SIZE LINES.
            01 P-REC PIC X(20).
            WORKING-STORAGE SECTION.
            01 WS-SIZE PIC 99 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LPF.
                MOVE SPACE TO P-REC.
                MOVE 2 TO WS-SIZE.
                WRITE P-REC AFTER ADVANCING PAGE.
                WRITE P-REC AFTER ADVANCING 2 LINES
                    AT EOP DISPLAY "EOP"
                    NOT AT EOP DISPLAY "NO-EOP"
                END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """,
            // The PAGE write resets the counter (GR7c1) and re-reads the operands (GR6b2) ⇒ the next page is
            // 2 lines; 1+2 = 3 > 2 ⇒ overflow EOP. Without the re-evaluation it would be 3 ≤ 5 ⇒ no EOP.
            "EOP");

    // ── LINAGE-COUNTER as an operand (§8.4.3.14 SR1 — procedure-division references) ───────────────────────

    [Fact]   // The register in IF relations and PERFORM UNTIL conditions (plus MOVE — every test above).
    public void Counter_InIfAndPerformUntil()
        => AssertSpec(Program("LINAGE IS 9 LINES", "", """
                PERFORM FILL-LINE UNTIL LINAGE-COUNTER EQUAL 4.
                IF LINAGE-COUNTER EQUAL 4
                    DISPLAY "IF-OK"
                ELSE
                    DISPLAY "IF-BAD".
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            FILL-LINE.
                WRITE P-REC AFTER ADVANCING 1 LINE.
            """), "IF-OK\nLC=004");

    [Fact]   // §8.4.3.14 SR3 / §8.4.2.2: with more than one LINAGE file the register is QUALIFIED by
             // file-name — each file carries its own counter (§13.18.34 GR7a). No NIST coverage; spec-pinned.
    public void QualifiedCounter_TwoLinageFiles()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LP1 ASSIGN TO "LNG-Q1".
                SELECT LP2 ASSIGN TO "LNG-Q2".
            DATA DIVISION.
            FILE SECTION.
            FD LP1
                LINAGE IS 5 LINES.
            01 R1 PIC X(10).
            FD LP2
                LINAGE IS 7 LINES.
            01 R2 PIC X(10).
            WORKING-STORAGE SECTION.
            01 LC-VAL PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LP1 LP2.
                MOVE SPACE TO R1.
                MOVE SPACE TO R2.
                WRITE R1 AFTER ADVANCING 1 LINE.
                WRITE R2 AFTER ADVANCING 2 LINES.
                MOVE LINAGE-COUNTER OF LP1 TO LC-VAL.
                DISPLAY "P1=" LC-VAL.
                MOVE LINAGE-COUNTER IN LP2 TO LC-VAL.
                DISPLAY "P2=" LC-VAL.
                CLOSE LP1 LP2.
                STOP RUN.
            """, "P1=002\nP2=003");

    // ── Bind-time diagnostics (§14.9.51 SR13/SR18/SR19; §8.4.3.14 SR3) ─────────────────────────────────────

    [Fact]   // SR19 — THE silent-drop bug class: an END-OF-PAGE phrase on a file whose FD has no LINAGE
             // clause is a compile-time rejection, never a dropped branch.
    public void Sr19_EopWithoutLinage_IsRejected()
    {
        var (ok, diags) = EditionHarness.Compile(Program("RECORD CONTAINS 20 CHARACTERS", "", """
                WRITE P-REC AFTER ADVANCING 1 LINE
                    AT EOP DISPLAY "X" END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "SR19");
    }

    [Fact]   // SR18: ADVANCING PAGE and END-OF-PAGE shall not both appear in one WRITE.
    public void Sr18_AdvancingPageWithEop_IsRejected()
    {
        var (ok, diags) = EditionHarness.Compile(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING PAGE
                    AT EOP DISPLAY "X" END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "SR18");
    }

    [Fact]   // SR13: a LINAGE file's ADVANCING phrase shall not name a SPECIAL-NAMES mnemonic.
    public void Sr13_MnemonicAdvancingOnLinageFile_IsRejected()
    {
        var (ok, diags) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CSP IS FEED-MN.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LPF ASSIGN TO "LNG-OUT".
            DATA DIVISION.
            FILE SECTION.
            FD LPF
                LINAGE IS 5 LINES.
            01 P-REC PIC X(20).
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LPF.
                MOVE SPACE TO P-REC.
                WRITE P-REC AFTER ADVANCING FEED-MN.
                CLOSE LPF.
                STOP RUN.
            """, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "SR13");
    }

    [Fact]   // §8.4.3.14 SR3 / §8.4.2.2: an UNQUALIFIED LINAGE-COUNTER is ambiguous when two files have
             // LINAGE clauses — rejected with a qualification diagnostic.
    public void UnqualifiedCounter_TwoLinageFiles_IsRejected()
    {
        var (ok, diags) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LP1 ASSIGN TO "LNG-Q1".
                SELECT LP2 ASSIGN TO "LNG-Q2".
            DATA DIVISION.
            FILE SECTION.
            FD LP1
                LINAGE IS 5 LINES.
            01 R1 PIC X(10).
            FD LP2
                LINAGE IS 7 LINES.
            01 R2 PIC X(10).
            WORKING-STORAGE SECTION.
            01 LC-VAL PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LP1 LP2.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LP1 LP2.
                STOP RUN.
            """, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "8.4.3.14");
    }

    // ── GR6 b)'s VALUE RULES and what a violation does (kb/Work PB525 + PB526) ────────────────────────────
    //
    // §13.18.34.4 GR6 b) 1 "The page size shall be greater than zero." and GR6 b) 2 "The footing start shall be
    // greater than zero and not greater than the page size.", then: "If the value does not conform to these two
    // rules, the EC-I-O-LINAGE exception condition is set to exist. If execution continues after processing of
    // this exception condition, it continues with the statement following the WRITE statement; the
    // LINAGE-COUNTER is set to 0 and remains at that value until the file is closed; and all subsequent WRITE
    // statements referencing the file cause the EC-I-O-LINAGE exception condition to continue to exist until the
    // file is closed."
    //
    // EC-I-O-LINAGE is Fatal (Table 13) and §14.6.13.1.3 3) routes a fatal EC-I-O condition through §9.1.13's
    // rules, which are rules about the I-O STATUS — so the violation reports one: '90', §9.1.13.11's
    // implementor-defined value (docs/CONFORMANCE.md §7 DOC-A.1-110). That is what these tests read, because it
    // is what a program with NO >>TURN can see; the exception-NAME half is
    // tests/conformance/2023/pb526_linage_ec_checking.

    /// <summary>A program whose LINAGE operands are data items, with a FILE STATUS item and a format-1 USE
    /// declarative on the file — the shape every test below needs, since the observable of a GR6 b) violation is
    /// the I-O status and the declarative it fires.</summary>
    private static string ViolationProgram(string fdClauses, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. LNGVIO.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT LPF ASSIGN TO "LNG-VIO"
                FILE STATUS IS FS.
        DATA DIVISION.
        FILE SECTION.
        FD LPF
            {fdClauses}.
        01 P-REC PIC X(4).
        WORKING-STORAGE SECTION.
        01 FS PIC XX VALUE "??".
        01 LC-VAL PIC 9(3).
        {ws}
        PROCEDURE DIVISION.
        DECLARATIVES.
        ERR-SECT SECTION.
            USE AFTER STANDARD ERROR PROCEDURE ON LPF.
        ERR-PARA.
            DISPLAY "USE FS=" FS.
        END DECLARATIVES.
        MAIN-SECT SECTION.
        MAIN-PARA.
            MOVE SPACE TO P-REC.
        {proc}
        """;

    [Fact]   // GR6 b) 1 at the OPEN: the page size is read "at the completion of an OPEN statement with the
             // OUTPUT phrase" (GR6 b) 1), so a zero there is a violation of the OPEN — status '90', counter 0 —
             // and NOT a process kill. Before kb/Work PB526 this exact program died with an unhandled
             // System.InvalidOperationException out of SequentialConnector.EvaluateLinage.
    public void Gr6b1_PageSizeZeroAtOpenOutput_IsTheLinageStatus_AndExecutionContinues()
        => AssertSpec(ViolationProgram("LINAGE IS WS-SZ LINES", "01 WS-SZ PIC 9(3) VALUE 0.", """
                OPEN OUTPUT LPF.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "OPEN FS=" FS " LC=" LC-VAL.
                CLOSE LPF.
                DISPLAY "CLOSE FS=" FS.
                STOP RUN.
            """), "USE FS=90\nOPEN FS=90 LC=000\nCLOSE FS=00");

    [Fact]   // ⛔ THE DISCRIMINATING PAIR for kb/Work PB525 — the SAME page size, the SAME write, the only
             // difference being whether the FOOTING PHRASE IS WRITTEN. With the phrase present and its data item
             // holding 0, GR6 b) 2's value rule applies and 0 does not conform. With the phrase absent, GR1's
             // "If the FOOTING phrase is not specified, no end-of-page condition independent of the page
             // overflow condition exists" is the rule and the write is ordinary. A model that encodes "absent"
             // as the value 0 cannot tell these two programs apart, which is exactly the defect: the first one
             // used to print NO-EOP / LC=005 like the second.
    public void Gr6b2_FootingPhraseHoldingZero_IsAViolation_NotAnAbsentPhrase()
        => AssertSpec(ViolationProgram("LINAGE IS 5 LINES WITH FOOTING AT WS-FT", "01 WS-FT PIC 99 VALUE 0.", """
                OPEN OUTPUT LPF.
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP"
                    NOT AT EOP DISPLAY "NO-EOP"
                END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "W1 FS=" FS " LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "USE FS=90\nUSE FS=90\nW1 FS=90 LC=000");

    [Fact]   // …the other half of the pair: NO FOOTING phrase, everything else identical. GR1 governs, the write
             // is successful, and §14.9.51.4 GR28's NOT END-OF-PAGE imperative runs on it.
    public void Gr1_NoFootingPhrase_SamePageAndWrite_IsAnOrdinarySuccessfulWrite()
        => AssertSpec(ViolationProgram("LINAGE IS 5 LINES", "", """
                OPEN OUTPUT LPF.
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP"
                    NOT AT EOP DISPLAY "NO-EOP"
                END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "W1 FS=" FS " LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "NO-EOP\nW1 FS=00 LC=005");

    [Fact]   // GR6 b) 2's LATCH, end to end: the violation arises at a page-overflow WRITE (GR6 b) 3 re-reads
             // the operands during it), then "the LINAGE-COUNTER is set to 0 and remains at that value until the
             // file is closed; and all subsequent WRITE statements referencing the file cause the EC-I-O-LINAGE
             // exception condition to continue to exist until the file is closed" — W2 and W3 each fire the
             // declarative and each leave the counter at 0 — and the CLOSE ends it: after it the connector
             // reports its own successful status, and the next OPEN OUTPUT re-determines the values (GR6 b) 1)
             // and starts a working page at counter 1 (GR7 d).
    public void Gr6b2_ViolationAtOverflow_LatchesUntilClose_AndTheCloseReleasesIt()
        => AssertSpec(ViolationProgram("LINAGE IS WS-SZ LINES", "01 WS-SZ PIC 9(3) VALUE 2.", """
                OPEN OUTPUT LPF.
                WRITE P-REC.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "W1 FS=" FS " LC=" LC-VAL.
                MOVE 0 TO WS-SZ.
                WRITE P-REC.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "W2 FS=" FS " LC=" LC-VAL.
                WRITE P-REC.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "W3 FS=" FS " LC=" LC-VAL.
                CLOSE LPF.
                DISPLAY "CLOSE FS=" FS.
                MOVE 4 TO WS-SZ.
                OPEN OUTPUT LPF.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "REOPEN FS=" FS " LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """),
            "W1 FS=00 LC=002\nUSE FS=90\nW2 FS=90 LC=000\nUSE FS=90\nW3 FS=90 LC=000\n"
            + "CLOSE FS=00\nREOPEN FS=00 LC=001");

    [Fact]   // §14.9.51.4 GR27 ("When an end-of-page condition occurs, the WRITE statement is successful") and
             // GR28 ("If, during the successful execution of a WRITE statement with the NOT END-OF-PAGE phrase,
             // the end-of-page condition does not occur …") both condition the phrase on a SUCCESSFUL write, so
             // an unsuccessful one runs NEITHER imperative. The latched write below is the case that made this
             // observable: its status is '90' and the connector's per-write end-of-page flag is not even its own.
    public void Gr27Gr28_AnUnsuccessfulWrite_RunsNeitherEndOfPageImperative()
        => AssertSpec(ViolationProgram("LINAGE IS WS-SZ LINES", "01 WS-SZ PIC 9(3) VALUE 2.", """
                OPEN OUTPUT LPF.
                MOVE 0 TO WS-SZ.
                WRITE P-REC AFTER ADVANCING 3 LINES
                    AT EOP DISPLAY "EOP1"
                    NOT AT EOP DISPLAY "NO-EOP1"
                END-WRITE.
                DISPLAY "W1 FS=" FS.
                WRITE P-REC AFTER ADVANCING 1 LINE
                    AT EOP DISPLAY "EOP2"
                    NOT AT EOP DISPLAY "NO-EOP2"
                END-WRITE.
                DISPLAY "W2 FS=" FS.
                CLOSE LPF.
                STOP RUN.
            """), "USE FS=90\nW1 FS=90\nUSE FS=90\nW2 FS=90");
}
