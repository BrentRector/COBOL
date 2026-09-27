// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Report Writer subsystem (ISO/IEC 1989:2023 §13.14–§13.18 report description clauses; §14.9.21 INITIATE /
/// §14.9.16 GENERATE / §14.9.46 TERMINATE; §8.4.3.15 report counters; §14.9.49 Format 2 USE BEFORE REPORTING) —
/// the spec-pinned conformance net (COBOLNET_REPORT_WRITER_DESIGN §8). Every expected value derives from the
/// cited GR, NOT from the legacy oracle: the NIST RW goldens compare only the CCVS print file, never the RWCS
/// report file, and the LEGACY'S REPORT-FILE CONTENT IS DEMONSTRABLY WRONG in two places this net pins as fixed
/// (a §13.18.53.4 GR1 numeric SOURCE byte-copied left-justified instead of MOVEd through the printable PICTURE;
/// a blank <c>SOURCE LINE-COUNTER</c>). Counter behavior surfaces through MOVE-to-stdout; report CONTENT
/// surfaces by reading the report file back through a second line-sequential SELECT on the same ASSIGN target.
/// </summary>
public sealed class ReportWriterConformanceTests
{
    /// <summary>⛔ THE WHOLE CLASS COMPILES AT --std 2023, AND THE OBSERVATION MECHANISM IS WHY (kb/Work PB688).
    /// Report Writer itself is a COBOL-85 subsystem, but every fact here is asserted by reading the report file
    /// back through a second SELECT with <c>ORGANIZATION LINE SEQUENTIAL</c> — and that organization is a
    /// COBOL-2023 introduction (ISO §12.4.5.10.3 GR2; the Foreword lists "Line Sequential file organization"
    /// among the main changes over ISO/IEC 1989:2014), so below 2023 the read-back is COBOLNET0900. It cannot be
    /// swapped for a record-sequential read either: WiseOwl COBOL frames a report file as line-delimited text
    /// whatever the file's ORGANIZATION, while the record-sequential connector reads fixed-size records — so a
    /// WiseOwl COBOL report file is not readable at all below 2023. The report-writer BEHAVIOR under test is
    /// edition-invariant, so 2023 costs nothing; what is lost is only the (unobtainable) claim that these
    /// assertions were observed at 85.</summary>
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler(2023);

    /// <summary>Compile-and-run on the greenfield compiler; assert the spec-derived stdout.</summary>
    private static void AssertSpec(string source, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(expected, stdout);
    }

    /// <summary>A one-report program: <paramref name="rdAndGroups"/> is the REPORT SECTION text after
    /// <c>REPORT SECTION.</c>, <paramref name="ws"/> extra WORKING-STORAGE, <paramref name="proc"/> the
    /// PROCEDURE DIVISION body (the program supplies OPEN; tests append GENERATE/TERMINATE/read-back). A second
    /// SELECT (<c>RBACK</c>, line sequential) reads the report file back so tests can assert report CONTENT —
    /// the surface the NIST goldens never check.</summary>
    private static string Program(string rdAndGroups, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWTST.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT RPT ASSIGN TO "RPTF".
            SELECT RBACK ASSIGN TO "RPTF" ORGANIZATION LINE SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD RPT
            REPORT IS R-1.
        FD RBACK.
        01 RB-REC PIC X(40).
        WORKING-STORAGE SECTION.
        01 LC-V PIC 9(3).
        01 PC-V PIC 9(3).
        {ws}
        REPORT SECTION.
        {rdAndGroups}
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT RPT.
        {proc}
        """;

    /// <summary>The read-back tail: skip blank (spacing) records, DISPLAY the first 4 characters of each
    /// non-blank report record (markers placed at columns 1–4 by the tests; a page-advance FORM FEED trails the
    /// page's last record, so 4-character markers keep it past the slice — <c>Normalize</c> would otherwise
    /// split on it).</summary>
    private const string ReadBack = """
            OPEN INPUT RBACK.
        RB-LOOP.
            READ RBACK AT END GO TO RB-DONE.
            IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:4).
            GO TO RB-LOOP.
        RB-DONE.
            CLOSE RBACK.
            STOP RUN.
        """;

    /// <summary>The 5-character-slice read-back (sum-counter content "CF 03"; the tests using it produce a
    /// single page — no form-feed hazard).</summary>
    private const string ReadBack5 = """
            OPEN INPUT RBACK.
        RB-LOOP.
            READ RBACK AT END GO TO RB-DONE.
            IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:5).
            GO TO RB-LOOP.
        RB-DONE.
            CLOSE RBACK.
            STOP RUN.
        """;

    // ── INITIATE (§14.9.21.4 GR1) ────────────────────────────────────────────────────────────────────────────

    [Fact]   // GR1b/GR1c: INITIATE sets LINE-COUNTER to 0 and PAGE-COUNTER to 1.
    public void Initiate_Gr1_CountersZeroAndOne()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "BODY".
            """, "", """
                INITIATE R-1.
                MOVE LINE-COUNTER TO LC-V.
                MOVE PAGE-COUNTER TO PC-V.
                DISPLAY "LC=" LC-V " PC=" PC-V.
                TERMINATE R-1.
                CLOSE RPT.
                STOP RUN.
            """), "LC=000 PC=001");

    // ── GENERATE placement + page advance (§14.9.16.4 GR4–GR6 / §13.18.35.4 GR4–GR6) ───────────────────────

    [Fact]   // §13.18.35.4 GR5b3 + GR4 exemption: the chronologically FIRST body group takes NO page-fit test
             // and, relative, lands at FIRST DETAIL (default = HEADING default = 1) — its PLUS 2 is IGNORED.
    public void FirstGenerate_Gr5b3_LandsAtFirstDetail_RelativeValueIgnored()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 5 LINES.
            01 DET-1 TYPE DE LINE PLUS 2.
                03 COLUMN 1 PIC X(4) VALUE "BODY".
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                MOVE LINE-COUNTER TO LC-V.
                DISPLAY "LC=" LC-V.
                TERMINATE R-1.
                CLOSE RPT.
                STOP RUN.
            """), "LC=001");

    [Fact]   // §13.18.35.4 GR4c page-fit (trial = LINE-COUNTER + Σ relative values vs LAST DETAIL) + §14.9.16.4
             // GR6d/e + GR5b3: PAGE 5 (LAST DETAIL defaults 5), LINE PLUS 2 ⇒ lines 1,3,5; the fourth GENERATE
             // (trial 5+2=7 > 5) page-advances: PAGE-COUNTER 2, the body lands at FIRST DETAIL 1.
    public void PageFit_Gr4c_TrialSumOverflow_AdvancesAndResets()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 5 LINES.
            01 DET-1 TYPE DE LINE PLUS 2.
                03 COLUMN 1 PIC X(4) VALUE "BODY".
            """, "", """
                INITIATE R-1.
                PERFORM GEN-SHOW 4 TIMES.
                TERMINATE R-1.
                CLOSE RPT.
                STOP RUN.
            GEN-SHOW.
                GENERATE DET-1.
                MOVE LINE-COUNTER TO LC-V.
                MOVE PAGE-COUNTER TO PC-V.
                DISPLAY "LC=" LC-V " PC=" PC-V.
            """), "LC=001 PC=001\nLC=003 PC=001\nLC=005 PC=001\nLC=001 PC=002");

    // ── §13.18.53.4 GR1 content (the two legacy report-file bugs, pinned FIXED) ─────────────────────────────

    [Fact]   // GR1: SOURCE is the sending operand of an implicit MOVE to the printable item — a PIC 9(3) item
             // sourcing a PIC 9(6) value 1 prints "001" (right-aligned numeric MOVE), NOT the legacy's "000"
             // (a left-justified byte copy); and SOURCE LINE-COUNTER prints the line number, NOT blank.
    public void Source_Gr1_ImplicitMoveEditsThroughPicture()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC 9(3) SOURCE IS WS-COUNTER.
            """, "01 WS-COUNTER PIC 9(6) VALUE 1.", """
                INITIATE R-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "001");

    [Fact]   // §13.18.35.4 GR6 + §13.18.53.4 GR3 (the load-bearing ordering): LINE-COUNTER is set to the line's
             // number BEFORE the line composes, so a detail's SOURCE LINE-COUNTER prints its OWN line number.
    public void Source_Gr6_LineCounterComposesAfterCounterUpdate()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES FIRST DETAIL 3.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC 99 SOURCE IS LINE-COUNTER.
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "03\n04");

    [Fact]   // §13.18.35.4 GR6 on a PAGE HEADING (the RW103A shape): the PH's SOURCE LINE-COUNTER prints the
             // PH's own absolute line, and the first detail of each page lands at FIRST DETAIL.
    public void PageHeading_Gr6_SourcesItsOwnLine()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 6 LINES HEADING 1 FIRST DETAIL 3 LAST DETAIL 4.
            01 PH-1 TYPE PH LINE 2.
                03 COLUMN 1 PIC X(2) VALUE "PH".
                03 COLUMN 4 PIC 9 SOURCE IS LINE-COUNTER.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
                03 COLUMN 4 PIC 9 SOURCE IS LINE-COUNTER.
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                GENERATE DET-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "PH 2\nDE 3\nDE 4\nPH 2\nDE 3");

    // ── First-GENERATE sequence + heading/footing presentation (§14.9.16.4 GR4/GR6; §13.18.57.4 GR6) ───────

    [Fact]   // §14.9.16.4 GR4a/GR4b + GR6f: the report heading prints ONCE, before the first page's heading;
             // a page advance reprints the PH (GR6f) but never the RH.
    public void FirstGenerate_Gr4_RhOncePhPerPage()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 6 LINES HEADING 1 FIRST DETAIL 3 LAST DETAIL 4.
            01 RH-1 TYPE RH LINE 1.
                03 COLUMN 1 PIC X(4) VALUE "RH-1".
            01 PH-1 TYPE PH LINE 2.
                03 COLUMN 1 PIC X(4) VALUE "PH-1".
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "DE-1".
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                GENERATE DET-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "RH-1\nPH-1\nDE-1\nDE-1\nPH-1\nDE-1");

    [Fact]   // §14.9.16.4 GR6a + §13.18.35.4 GR5b4: the page footing prints at the page advance (before the
             // physical advance), placed at FOOTING + integer-2 (relative) — and again on the LAST page at
             // TERMINATE (§13.18.57.4 GR6f), immediately followed by the report footing (§14.9.46.4 GR3c).
    public void PageAdvance_Gr6a_PfThenNewPage_TerminatePfRf()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 8 LINES HEADING 1 FIRST DETAIL 2 LAST DETAIL 4 FOOTING 6.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "DE-1".
            01 PF-1 TYPE PF LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "PF-1".
            01 RF-1 TYPE RF LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "RF-1".
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                GENERATE DET-1.
                GENERATE DET-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "DE-1\nDE-1\nDE-1\nPF-1\nDE-1\nPF-1\nRF-1");

    // ── TERMINATE (§14.9.46.4 GR2/GR3) ───────────────────────────────────────────────────────────────────────

    [Fact]   // GR2: with NO GENERATE between INITIATE and TERMINATE, NO report group is processed — the report
             // file stays empty (PH/RF defined but never printed); the sole effect is active → inactive.
    public void Terminate_Gr2_NoGenerate_NothingPrints()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 PH-1 TYPE PH LINE 1.
                03 COLUMN 1 PIC X(2) VALUE "PH".
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
            01 RF-1 TYPE RF LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "RF".
            """, "", """
                INITIATE R-1.
                TERMINATE R-1.
                CLOSE RPT.
                OPEN INPUT RBACK.
                READ RBACK AT END DISPLAY "EMPTY" GO TO RB-DONE.
                DISPLAY "NOT-EMPTY".
            RB-DONE.
                CLOSE RBACK.
                STOP RUN.
            """), "EMPTY");

    // ── CONTROL hierarchy (§13.18.16.4 GR3/GR4; §14.9.16.4 GR4c/GR5a; §14.9.46.4 GR3a/b) ────────────────────

    [Fact]   // §14.9.16.4 GR4c (CH on the first GENERATE) + GR5a (break: CF then CH) + §13.18.16.4 GR4a (the CF
             // composes with the PRIOR control value restored; the CH with the new) + §14.9.46.4 GR3b (TERMINATE
             // prints the final CF as a most-major break, with the last group's values).
    public void ControlBreak_Gr4a_CfSeesPriorValue_ChSeesNew()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 20 LINES CONTROL IS WS-KEY.
            01 CH-1 TYPE CH WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CH".
                03 COLUMN 4 PIC 9 SOURCE IS WS-KEY.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
                03 COLUMN 4 PIC 9 SOURCE IS WS-KEY.
            01 CF-1 TYPE CF WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CF".
                03 COLUMN 4 PIC 9 SOURCE IS WS-KEY.
            """, "01 WS-KEY PIC 9 VALUE 1.", """
                INITIATE R-1.
                GENERATE DET-1.
                MOVE 2 TO WS-KEY.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "CH 1\nDE 1\nCF 1\nCH 2\nDE 2\nCF 2");

    // ── SUM counters (§13.18.54.4 GR2/GR3/GR7) ──────────────────────────────────────────────────────────────

    [Fact]   // GR7c1 (accumulate on every GENERATE) + GR2 (reset at the end of the group it prints in) + the
             // GR7 ordering (the CF printed at a break shows the ENDED group's total — accumulation of the
             // breaking GENERATE's addend happens after the break processing): amounts 1,2 then 4 across a
             // break print totals 03 then 04.
    public void Sum_Gr7_AccumulatesPerGenerate_ResetsWherePrinted()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 20 LINES CONTROL IS WS-KEY.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
            01 CF-1 TYPE CF WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CF".
                03 COLUMN 4 PIC 99 SUM WS-AMT.
            """, """
        01 WS-KEY PIC 9 VALUE 1.
        01 WS-AMT PIC 9 VALUE 1.
        """, """
                INITIATE R-1.
                GENERATE DET-1.
                MOVE 2 TO WS-AMT.
                GENERATE DET-1.
                MOVE 2 TO WS-KEY.
                MOVE 4 TO WS-AMT.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack5), "DE\nDE\nCF 03\nDE\nCF 04");

    [Fact]   // GR7c2: with an UPON phrase the addend accumulates ONLY on a GENERATE of the named detail.
    public void Sum_Gr7c2_UponRestrictsToNamedDetail()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 20 LINES CONTROL IS WS-KEY.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "D1".
            01 DET-2 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "D2".
            01 CF-1 TYPE CF WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CF".
                03 COLUMN 4 PIC 99 SUM WS-AMT UPON DET-1.
            """, """
        01 WS-KEY PIC 9 VALUE 1.
        01 WS-AMT PIC 9 VALUE 5.
        """, """
                INITIATE R-1.
                GENERATE DET-1.
                MOVE 7 TO WS-AMT.
                GENERATE DET-2.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack5), "D1\nD2\nCF 05");

    // ── PRESENT WHEN (§13.18.41 Format 1; P10 Step 13 — a COBOL-2002 clause) ────────────────────────────────
    // These two used to compile at --std 2002 through their own AssertSpec2002 helper, to say "PRESENT WHEN is
    // 2002 surface". They now share the class's 2023 compiler, because the READ-BACK the whole class observes
    // through is a 2023 organization (see the CobolNet field): a program carrying it is not expressible at 2002
    // at all, so the helper's name was the only thing asserting the edition. 2002-and-later coverage of PRESENT
    // WHEN itself is the constructs.json (construct × edition) matrix's job, not this net's.

    [Fact]   // §13.18.41.4 GR3g / §13.18.54.4 GR10: a SUM entry absent under its PRESENT WHEN is neither
             // printed nor reset for that instance of the report group — the counter carries its accumulation
             // forward; the next PRESENT instance prints the carried total and resets (no RESET phrase ⇒
             // reset where printed, §13.18.54.4 GR2). Addends 5 (absent), 3, 2 print blank, 08, 02.
    public void PresentWhen_Gr3g_AbsentSumNeitherPrintsNorResets()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 20 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "D:".
                03 COLUMN 4 PIC 99 SUM WS-AMT
                   PRESENT WHEN WS-SHOW = 1.
            """, """
        01 WS-AMT PIC 9 VALUE 5.
        01 WS-SHOW PIC 9 VALUE 0.
        """, """
                INITIATE R-1.
                GENERATE DET-1.
                MOVE 3 TO WS-AMT.
                MOVE 1 TO WS-SHOW.
                GENERATE DET-1.
                MOVE 2 TO WS-AMT.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack5), "D:\nD: 08\nD: 02");

    // ── USE BEFORE REPORTING (§14.9.49 Format 2 GR8) ────────────────────────────────────────────────────────

    [Fact]   // GR8: the declarative is invoked just before the named report group is produced — a MOVE in it is
             // visible to the group's SOURCE items (the PD stores 0; the declarative overrides to 7).
    public void UseBeforeReporting_Gr8_RunsBeforeEachProduction()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWTSTU.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT ASSIGN TO "RPTF".
                SELECT RBACK ASSIGN TO "RPTF" ORGANIZATION LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD RPT
                REPORT IS R-1.
            FD RBACK.
            01 RB-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01 WS-FLAG PIC 9 VALUE 0.
            REPORT SECTION.
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
                03 COLUMN 4 PIC 9 SOURCE IS WS-FLAG.
            PROCEDURE DIVISION.
            DECLARATIVES.
            BR-SEC SECTION. USE BEFORE REPORTING DET-1.
            BR-PARA.
                MOVE 7 TO WS-FLAG.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                OPEN OUTPUT RPT.
                INITIATE R-1.
                MOVE 0 TO WS-FLAG.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
                OPEN INPUT RBACK.
            RB-LOOP.
                READ RBACK AT END GO TO RB-DONE.
                IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:4).
                GO TO RB-LOOP.
            RB-DONE.
                CLOSE RBACK.
                STOP RUN.
            """, "DE 7");

    [Fact]   // §14.9.49.4 GR8 invokes THE DECLARATIVE, and §14.9.49.3 SR1 makes that "the remainder of the
             // section": BR-P1 and BR-P2 both run, so the group sees 7. The Format-2 hook is a SEPARATE selection
             // path from the F1/F3 dispatchers and it took the same truncated range — a handler end derived from
             // paragraph shape stopped at BR-EX and the group would see 3 (kb/Work PB367). §14.9.14.4 GR7 sends
             // BR-LAST's EXIT SECTION to the return mechanism after the section's last paragraph, so BR-TAIL —
             // which would set 9 and STOP the run — never runs.
    public void UseBeforeReporting_UseProcedureIsTheWholeSection()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWTSTUB.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT ASSIGN TO "RPTFB".
                SELECT RBACK ASSIGN TO "RPTFB" ORGANIZATION LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD RPT
                REPORT IS R-1.
            FD RBACK.
            01 RB-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01 WS-FLAG PIC 9 VALUE 0.
            REPORT SECTION.
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
                03 COLUMN 4 PIC 9 SOURCE IS WS-FLAG.
            PROCEDURE DIVISION.
            DECLARATIVES.
            BR-SEC SECTION. USE BEFORE REPORTING DET-1.
            BR-P1.
                MOVE 3 TO WS-FLAG.
            BR-EX.
                EXIT.
            BR-P2.
                MOVE 7 TO WS-FLAG.
            BR-LAST.
                EXIT SECTION.
            BR-TAIL.
                MOVE 9 TO WS-FLAG.
                STOP RUN.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                OPEN OUTPUT RPT.
                INITIATE R-1.
                MOVE 0 TO WS-FLAG.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
                OPEN INPUT RBACK.
            RB-LOOP.
                READ RBACK AT END GO TO RB-DONE.
                IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:4).
                GO TO RB-LOOP.
            RB-DONE.
                CLOSE RBACK.
                STOP RUN.
            """, "DE 7");

    [Fact]   // §14.9.49.4 GR8 — "The declarative is invoked just before the named report group is produced" —
             // for ALL SEVEN report-group types, not just the detail one (kb/Work PB367b: the engine installs the
             // hook per group and invokes it as the first act of every presentation path, but only the DETAIL
             // path had a witness, so the RH / PH / CH / CF / PF / RF invocations were unverified in both
             // directions). Each declarative writes its OWN digit into WS-F and each group SOURCEs WS-F, so a
             // group can only print its own digit if ITS declarative ran immediately before IT was produced —
             // a hook wired to the wrong group, or invoked once for the whole page, prints a neighbour's digit.
             // The ORDER is the standard's own: §14.9.16.4 GR4 a)–d) (the chronologically first GENERATE prints
             // the report heading, then a page heading, then each control heading, then the detail) followed by
             // §14.9.46.4 GR3 b)/c) (TERMINATE prints each control footing, then the report footing) with
             // §13.18.57.4 GR6 f) placing the page footing as "the last report group on each page" and adding
             // that "If a report footing is defined and is not on a page by itself, the page footing on the last
             // page is immediately followed by the report footing".
    public void UseBeforeReporting_Gr8_EveryGroupTypeInvokesItsOwnDeclarative()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWTSTU7.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT ASSIGN TO "RPTF7".
                SELECT RBACK ASSIGN TO "RPTF7" ORGANIZATION LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD RPT
                REPORT IS R-1.
            FD RBACK.
            01 RB-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01 WS-F PIC 9 VALUE 0.
            01 WS-KEY PIC 9 VALUE 1.
            REPORT SECTION.
            RD R-1 CONTROL IS WS-KEY PAGE LIMIT IS 20 LINES
                HEADING 1 FIRST DETAIL 4 LAST DETAIL 14 FOOTING 16.
            01 RH-1 TYPE RH LINE 1.
                03 COLUMN 1 PIC X(2) VALUE "RH".
                03 COLUMN 4 PIC 9 SOURCE IS WS-F.
            01 PH-1 TYPE PH LINE 2.
                03 COLUMN 1 PIC X(2) VALUE "PH".
                03 COLUMN 4 PIC 9 SOURCE IS WS-F.
            01 CH-1 TYPE CH WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CH".
                03 COLUMN 4 PIC 9 SOURCE IS WS-F.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
                03 COLUMN 4 PIC 9 SOURCE IS WS-F.
            01 CF-1 TYPE CF WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CF".
                03 COLUMN 4 PIC 9 SOURCE IS WS-F.
            01 PF-1 TYPE PF LINE 17.
                03 COLUMN 1 PIC X(2) VALUE "PF".
                03 COLUMN 4 PIC 9 SOURCE IS WS-F.
            01 RF-1 TYPE RF LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "RF".
                03 COLUMN 4 PIC 9 SOURCE IS WS-F.
            PROCEDURE DIVISION.
            DECLARATIVES.
            BR-RH SECTION. USE BEFORE REPORTING RH-1.
            BR-RH-P.
                MOVE 1 TO WS-F.
            BR-PH SECTION. USE BEFORE REPORTING PH-1.
            BR-PH-P.
                MOVE 2 TO WS-F.
            BR-CH SECTION. USE BEFORE REPORTING CH-1.
            BR-CH-P.
                MOVE 3 TO WS-F.
            BR-DE SECTION. USE BEFORE REPORTING DET-1.
            BR-DE-P.
                MOVE 4 TO WS-F.
            BR-CF SECTION. USE BEFORE REPORTING CF-1.
            BR-CF-P.
                MOVE 5 TO WS-F.
            BR-PF SECTION. USE BEFORE REPORTING PF-1.
            BR-PF-P.
                MOVE 6 TO WS-F.
            BR-RF SECTION. USE BEFORE REPORTING RF-1.
            BR-RF-P.
                MOVE 7 TO WS-F.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                OPEN OUTPUT RPT.
                INITIATE R-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
                OPEN INPUT RBACK.
            RB-LOOP.
                READ RBACK AT END GO TO RB-DONE.
                IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:4).
                GO TO RB-LOOP.
            RB-DONE.
                CLOSE RBACK.
                STOP RUN.
            """, "RH 1\nPH 2\nCH 3\nDE 4\nCF 5\nPF 6\nRF 7");

    [Fact]   // §14.9.49.4 GR9 a) and b) — the two logical points that fix WHEN the declarative runs relative to
             // the rest of a GENERATE, and which had no witness at all (kb/Work PB367b).
             // GR9 a) "After any control break processing, if the associated report group is a detail and the
             // associated report description has a CONTROL clause": BR-DE writes 09 into WS-SEEN and no control
             // heading ever shows it — the first shows 00 (no declarative has run yet) and the one printed during
             // the break shows 07, the value BR-CF wrote moments earlier. A detail declarative that ran before
             // the break processing would put 09 on the second control heading.
             // GR9 b) "After incrementing sum counters defined in the report group": BR-CF changes the ADDEND
             // (WS-AMT 1 → 5), which is observable precisely because of the ordering GR9 b) asserts. The control
             // footing printed at the break still shows 01 — the increments belonging to that instance were all
             // applied before its declarative ran — and the breaking GENERATE's own increment, which
             // §14.9.16.4 GR5 a) places after the break processing, therefore picks up the 5 the declarative
             // wrote, so the TERMINATE footing (§14.9.46.4 GR3 b) shows 05. Had the declarative run before the
             // increments of its own instance, the break footing would read 05 as well.
             // ⚠ The DIRECT observable for GR9 b) — a declarative reading its group's own sum counter
             // (§13.18.54.4 GR5 + GR12) — is not available: a named sum counter is rejected as undefined in the
             // procedure division (COBOLNET1639). That is a separate defect, reported with kb/Work PB367b.
    public void UseBeforeReporting_Gr9ab_AfterBreakProcessingAndAfterSumIncrement()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWTSTU9.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT ASSIGN TO "RPTF9".
                SELECT RBACK ASSIGN TO "RPTF9" ORGANIZATION LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD RPT
                REPORT IS R-1.
            FD RBACK.
            01 RB-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01 WS-KEY  PIC 9 VALUE 1.
            01 WS-AMT  PIC 9 VALUE 1.
            01 WS-SEEN PIC 99 VALUE 0.
            REPORT SECTION.
            RD R-1 PAGE LIMIT IS 20 LINES CONTROL IS WS-KEY.
            01 CH-1 TYPE CH WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CH".
                03 COLUMN 4 PIC 99 SOURCE IS WS-SEEN.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
            01 CF-1 TYPE CF WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CF".
                03 COLUMN 4 PIC 99 SUM WS-AMT.
            PROCEDURE DIVISION.
            DECLARATIVES.
            BR-CF SECTION. USE BEFORE REPORTING CF-1.
            BR-CF-P.
                MOVE 7 TO WS-SEEN.
                MOVE 5 TO WS-AMT.
            BR-DE SECTION. USE BEFORE REPORTING DET-1.
            BR-DE-P.
                MOVE 9 TO WS-SEEN.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                OPEN OUTPUT RPT.
                INITIATE R-1.
                GENERATE DET-1.
                MOVE 2 TO WS-KEY.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
                OPEN INPUT RBACK.
            RB-LOOP.
                READ RBACK AT END GO TO RB-DONE.
                IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:5).
                GO TO RB-LOOP.
            RB-DONE.
                CLOSE RBACK.
                STOP RUN.
            """, "CH 00\nDE\nCF 01\nCH 07\nDE\nCF 05");

    [Fact]   // THE DETERMINATION kb/Work PB367b records: a USE BEFORE REPORTING declarative runs BEFORE the
             // group's PRESENT WHEN conditions are evaluated, so it can decide its own group's presence.
             // The standard fixes neither event relative to the other — §14.9.49.4 GR9 d) performs the
             // declarative "Before the processing of any LINE clauses defined for the report group" and
             // §13.18.41.4 GR2 evaluates condition-1 "before the processing of any LINE clauses for the report
             // group", the SAME boundary — and both precede the page fit test (§14.9.49.4 GR9 c) and
             // §13.18.41.4 GR3 d), which disregards absent lines). The order is therefore the implementor's, and
             // this one is chosen because the reverse makes a declarative's execution depend on data the
             // declarative is itself there to set: condition-1 is any condition (§13.18.41.2), typically over
             // exactly the items GR8's "just before the named report group is produced" exists to let the
             // procedure prepare. Under the reverse order a level-01 PRESENT WHEN would silently suppress the
             // procedure that would have made the group present, and no rule would let the program say so.
             // WS-ON starts 0 — absent, "as though the entire report group description were omitted"
             // (§13.18.41.4 GR2 b) — and only BR-DE makes it 1, so the printed line IS the determination.
    public void UseBeforeReporting_PresentWhen_DeclarativeRunsBeforeTheConditionIsEvaluated()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWTSTUPW.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT ASSIGN TO "RPTFPW".
                SELECT RBACK ASSIGN TO "RPTFPW" ORGANIZATION LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD RPT
                REPORT IS R-1.
            FD RBACK.
            01 RB-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01 WS-ON PIC 9 VALUE 0.
            01 WS-F  PIC 9 VALUE 0.
            REPORT SECTION.
            RD R-1 PAGE LIMIT IS 20 LINES.
            01 DET-1 TYPE DE LINE PLUS 1 PRESENT WHEN WS-ON = 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
                03 COLUMN 4 PIC 9 SOURCE IS WS-F.
            PROCEDURE DIVISION.
            DECLARATIVES.
            BR-DE SECTION. USE BEFORE REPORTING DET-1.
            BR-DE-P.
                MOVE 1 TO WS-ON.
                MOVE 4 TO WS-F.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                OPEN OUTPUT RPT.
                INITIATE R-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
                OPEN INPUT RBACK.
            RB-LOOP.
                READ RBACK AT END GO TO RB-DONE.
                IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:4).
                GO TO RB-LOOP.
            RB-DONE.
                CLOSE RBACK.
                STOP RUN.
            """, "DE 4");

    // ── Counter referencing rules (§8.4.3.15) ───────────────────────────────────────────────────────────────

    [Fact]   // SR3: LINE-COUNTER shall not be referenced as a receiving operand — a bind-time rejection, never a
             // silently-dropped receiver.
    public void LineCounter_Sr3_ReceivingOperandRejected()
    {
        var (ok, _, detail) = CobolNet.CompileAndRun(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "BODY".
            """, "", """
                INITIATE R-1.
                MOVE 5 TO LINE-COUNTER.
                TERMINATE R-1.
                CLOSE RPT.
                STOP RUN.
            """));
        Assert.False(ok, "MOVE … TO LINE-COUNTER must be rejected (ISO §8.4.3.15.3 SR3)");
        Assert.Contains("LINE-COUNTER", detail);
    }
}
