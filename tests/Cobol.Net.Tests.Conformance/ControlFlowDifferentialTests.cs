// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G4 checkpoint: the PC dispatcher (COBOLNET_DESIGN §5) — multi-paragraph control flow that the prior
/// sequential-paragraph stopgap got wrong. GO TO (forward / backward), GO TO … DEPENDING, fall-through between
/// paragraphs, out-of-line PERFORM (single / THRU / n TIMES / UNTIL), and EXIT PARAGRAPH — each pinned to the legacy
/// oracle. (Inline PERFORM was already correct; this is the out-of-line + branch machinery.)
/// </summary>
public sealed class ControlFlowDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new();

    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    /// <summary>Wrap a WORKING-STORAGE body + a full (multi-paragraph) PROCEDURE body into a program.</summary>
    private static string Program(string ws, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CFTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        {procedure}
        """;

    [Fact]
    public void FallThroughBetweenParagraphs()
        => AssertMatchesGolden(Program("01 FILLER PIC X.", """
            MAIN-PARA.
                DISPLAY "A".
            P2.
                DISPLAY "B".
            P3.
                DISPLAY "C".
                STOP RUN.
            """));

    [Fact]
    public void GoToForward()
        => AssertMatchesGolden(Program("01 FILLER PIC X.", """
            MAIN-PARA.
                DISPLAY "A".
                GO TO P3.
            P2.
                DISPLAY "B".
            P3.
                DISPLAY "C".
                STOP RUN.
            """));

    [Fact]
    public void GoToBackward_Loop()
        => AssertMatchesGolden(Program("01 I PIC 9 VALUE 0.", """
            MAIN-PARA.
                ADD 1 TO I.
                DISPLAY I.
                IF I < 3 GO TO MAIN-PARA END-IF.
            DONE-PARA.
                DISPLAY "DONE".
                STOP RUN.
            """));

    [Fact]
    public void Perform_SingleParagraph()
        => AssertMatchesGolden(Program("01 FILLER PIC X.", """
            MAIN-PARA.
                DISPLAY "BEFORE".
                PERFORM SUB-PARA.
                DISPLAY "AFTER".
                STOP RUN.
            SUB-PARA.
                DISPLAY "SUB".
            """));

    [Fact]
    public void Perform_Thru()
        => AssertMatchesGolden(Program("01 FILLER PIC X.", """
            MAIN-PARA.
                PERFORM P1 THRU P3.
                DISPLAY "DONE".
                STOP RUN.
            P1.
                DISPLAY "1".
            P2.
                DISPLAY "2".
            P3.
                DISPLAY "3".
            """));

    [Fact]
    public void Perform_Times_OutOfLine()
        => AssertMatchesGolden(Program("01 X PIC 9(2) VALUE 0.", """
            MAIN-PARA.
                PERFORM SUB-PARA 4 TIMES.
                DISPLAY X.
                STOP RUN.
            SUB-PARA.
                ADD 1 TO X.
            """));

    [Fact]
    public void Perform_Until_OutOfLine()
        => AssertMatchesGolden(Program("01 X PIC 9 VALUE 0.", """
            MAIN-PARA.
                PERFORM SUB-PARA UNTIL X = 3.
                DISPLAY X.
                STOP RUN.
            SUB-PARA.
                ADD 1 TO X.
            """));

    [Theory]
    [InlineData(1, "ONE")]
    [InlineData(2, "TWO")]
    [InlineData(3, "THREE")]
    public void GoTo_Depending(int sel, string _)
        => AssertMatchesGolden(Program($"01 SEL PIC 9 VALUE {sel}.", """
            MAIN-PARA.
                GO TO L1 L2 L3 DEPENDING ON SEL.
                DISPLAY "NONE".
                STOP RUN.
            L1.
                DISPLAY "ONE".
                STOP RUN.
            L2.
                DISPLAY "TWO".
                STOP RUN.
            L3.
                DISPLAY "THREE".
                STOP RUN.
            """));

    [Fact]
    public void GoTo_Depending_OutOfRange_FallsThrough()
        => AssertMatchesGolden(Program("01 SEL PIC 9 VALUE 5.", """
            MAIN-PARA.
                GO TO L1 L2 DEPENDING ON SEL.
                DISPLAY "FELLTHROUGH".
                STOP RUN.
            L1.
                DISPLAY "ONE".
                STOP RUN.
            L2.
                DISPLAY "TWO".
                STOP RUN.
            """));

    [Fact]
    // EXIT PARAGRAPH is a COBOL-2002 structured-procedure exit (ISO §14.9.14.2 Format 4), so this compiles at 2002 —
    // the DifferentialGolden default of edition 85 now correctly REJECTS it (COBOLNET0900; the CA39 introduction gate).
    public void ExitParagraph()
        => DifferentialGolden.Assert(Program("01 FILLER PIC X.", """
            MAIN-PARA.
                PERFORM SUB-PARA.
                DISPLAY "AFTER".
                STOP RUN.
            SUB-PARA.
                DISPLAY "S1".
                EXIT PARAGRAPH.
                DISPLAY "S2".
            """), edition: 2002);

    // ISO §14.9.28: the control phrase (TIMES / UNTIL) is INDEPENDENT of the THRU range (general format
    // PERFORM proc-1 [THRU proc-2] [times|until|varying]). The combination THRU + TIMES / THRU + UNTIL was
    // untested before DEVLOG 514, and the binder silently dropped the control phrase when THRU was present —
    // running the proc-1..proc-2 range ONCE instead of N times / until the condition. Spec-derived value asserted
    // on WiseOwl COBOL, then cross-checked against the committed golden.

    [Fact]
    public void Perform_Thru_Times_RunsRangeNTimes()
    {
        // §14.9.28 GR9: the range A..B (adds 1 + 10 = 11 per pass) is executed 3 times ⇒ 33, not once (11).
        string src = Program("01 X PIC 9(3) VALUE 0.", """
            MAIN-PARA.
                PERFORM A THRU B 3 TIMES.
                DISPLAY X.
                STOP RUN.
            A.
                ADD 1 TO X.
            B.
                ADD 10 TO X.
            """);
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(src);
        Assert.True(cok, cdetail);
        Assert.Equal("033", cout);   // 3 × (1 + 10) — the whole THRU range iterated 3 times
        AssertMatchesGolden(src);
    }

    [Fact]
    public void Perform_Thru_Until_RunsRangeUntilCondition()
    {
        // §14.9.28 GR10 (TEST BEFORE, default): the range A..B (adds 1 + 2 = 3 per pass) runs while X < 9 ⇒
        // X reaches 3, 6, 9; the pre-pass test then sees X = 9 and stops ⇒ 9, not 3 (a single pass).
        string src = Program("01 X PIC 9(3) VALUE 0.", """
            MAIN-PARA.
                PERFORM A THRU B UNTIL X >= 9.
                DISPLAY X.
                STOP RUN.
            A.
                ADD 1 TO X.
            B.
                ADD 2 TO X.
            """);
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(src);
        Assert.True(cok, cdetail);
        Assert.Equal("009", cout);
        AssertMatchesGolden(src);
    }
}
