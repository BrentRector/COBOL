// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Sections as procedure-name targets + qualified procedure-names (ISO §14.4.3 structure; §14.9.17 GO TO;
/// §14.9.28 PERFORM section / THRU incl. the legal INVERTED range; §8.4.2.2 <c>para OF section</c> qualification
/// and same-section implicit resolution of duplicated paragraph names), and the PERFORM … TIMES once-evaluated
/// count (§14.9.28 GR7 — body modifications of the count item must not change the iteration count; zero/negative
/// counts run zero times). Pinned to the legacy oracle (NIST-85 green across the whole PERFORM/GO TO series).
/// </summary>
public sealed class SectionDifferentialTests
{
    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    private static string Program(string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SECTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        {procedure}
        """;

    [Fact]
    public void GoToSectionName_TransfersToFirstParagraph()
        => AssertSameAsLegacy(Program("01 WS-N PIC 9 VALUE 0.", """
            MAIN-PARA.
                GO TO TARGET-SECT.
            SKIPPED-PARA.
                DISPLAY "SKIPPED".
                STOP RUN.
            TARGET-SECT SECTION.
            T-FIRST.
                DISPLAY "FIRST".
            T-SECOND.
                DISPLAY "SECOND".
                STOP RUN.
            """));

    [Fact]
    public void PerformSection_RunsWholeRangeAndReturns()
        => AssertSameAsLegacy(Program("01 WS-N PIC 9 VALUE 0.", """
            MAIN-PARA.
                PERFORM WORK-SECT.
                DISPLAY "BACK".
                STOP RUN.
            WORK-SECT SECTION.
            W-A.
                DISPLAY "A".
            W-B.
                DISPLAY "B".
            """));

    [Fact]
    public void PerformSectionThruParagraph_InvertedRange()
        => AssertSameAsLegacy(Program("01 WS-N PIC 9 VALUE 0.", """
            MAIN-PARA.
                PERFORM GO-SECT THRU EXIT-PARA.
                DISPLAY "RETURNED".
                STOP RUN.
            EXIT-PARA.
                DISPLAY "EXIT-PARA".
            AFTER-EXIT.
                DISPLAY "AFTER-EXIT".
                STOP RUN.
            GO-SECT SECTION.
            G-START.
                DISPLAY "G-START".
                GO TO EXIT-PARA.
            """));

    [Fact]
    public void QualifiedParagraph_DuplicateNamesAcrossSections()
        => AssertSameAsLegacy(Program("01 WS-N PIC 9 VALUE 0.", """
            MAIN-PARA.
                PERFORM DOIT OF SECT-ONE.
                PERFORM DOIT IN SECT-TWO.
                STOP RUN.
            SECT-ONE SECTION.
            DOIT.
                DISPLAY "ONE".
            SECT-TWO SECTION.
            DOIT.
                DISPLAY "TWO".
            """));

    /// <summary>SPEC-PINNED (not differential): a duplicated paragraph-name referenced WITHOUT qualification from
    /// within a section that contains the named paragraph resolves to the paragraph IN THAT SECTION — ISO
    /// §8.4.2.2.1 rule 6 ("the name is a paragraph-name and the section containing the reference also contains the
    /// named paragraph") + §8.4.2.2.3 SR7 ("need not be qualified when referred to from within the same section").
    /// The legacy oracle resolves this global-first (a version-invariant non-conformance — the rule is unchanged
    /// since COBOL-85), so this case pins to the spec.</summary>
    [Fact]
    public void UnqualifiedDuplicate_ResolvesWithinOwnSection()
    {
        var (ok, output, detail) = new CobolNetCompiler().CompileAndRun(Program("01 WS-N PIC 9 VALUE 0.", """
            MAIN-PARA.
                PERFORM CALLER OF SECT-TWO.
                STOP RUN.
            SECT-ONE SECTION.
            HELPER.
                DISPLAY "ONE-HELPER".
            SECT-TWO SECTION.
            CALLER.
                PERFORM HELPER.
            HELPER.
                DISPLAY "TWO-HELPER".
            """));
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal("TWO-HELPER", output);
    }

    [Fact]
    public void PerformTimes_CountDeterminedOnce()
        => AssertSameAsLegacy(Program("01 WS-CNT PIC 999 VALUE 3.\n01 WS-RUNS PIC 999 VALUE 0.", """
            MAIN-PARA.
                PERFORM BUMP WS-CNT TIMES.
                DISPLAY WS-RUNS.
                STOP RUN.
            BUMP.
                ADD 100 TO WS-CNT.
                ADD 1 TO WS-RUNS.
            """));

    [Fact]
    public void PerformTimes_ZeroAndNegativeRunZeroTimes()
        => AssertSameAsLegacy(Program("01 WS-Z PIC S999 VALUE 0.\n01 WS-M PIC S999 VALUE -3.\n01 WS-RUNS PIC 999 VALUE 0.", """
            MAIN-PARA.
                PERFORM BUMP WS-Z TIMES.
                PERFORM BUMP WS-M TIMES.
                DISPLAY WS-RUNS.
                STOP RUN.
            BUMP.
                ADD 1 TO WS-RUNS.
            """));

    // ── EXIT SECTION (ISO §14.9.14 Format 4, GR7) — SPEC-PINNED (the legacy shares the __atExit latent bug so a
    //    differential would match the WRONG output); EXIT SECTION is a 2002 introduction, so compiled at 2023 ──

    /// <summary>§14.9.14.4 GR7 — EXIT SECTION from a MID-section paragraph of a PERFORMed section transfers to the
    /// section end, triggering the PERFORM's return: the rest of W1 AND the following paragraph W2 must NOT run.
    /// This is the exact case the design's bare <c>pc = lastPara+1</c> port gets wrong — the bounded dispatch's
    /// <c>__atExit</c> tail-check is false for a mid-section pc, so it would fall through into W2. Pinned to the
    /// ISO result (the legacy is non-conforming here).</summary>
    [Fact]
    public void ExitSection_MidSectionUnderPerform_TriggersPerformReturn()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V5EXIT.
            PROCEDURE DIVISION.
            DRIVER SECTION.
            D1.
                DISPLAY "start".
                PERFORM WORK-SEC.
                DISPLAY "back".
                STOP RUN.
            WORK-SEC SECTION.
            W1.
                DISPLAY "w1".
                EXIT SECTION.
                DISPLAY "w1-tail".
            W2.
                DISPLAY "w2".
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(source);
        Assert.True(ok, detail);
        Assert.Equal("start\nw1\nback", stdout);   // EXIT SECTION skips w1-tail AND W2, returns from PERFORM WORK-SEC
    }

    /// <summary>§14.9.14.4 GR7 at top level (not PERFORMed): EXIT SECTION transfers to the section end and execution
    /// continues into the NEXT section (the implicit sequential return) — S1's tail is skipped, S2 runs.</summary>
    [Fact]
    public void ExitSection_TopLevel_FallsThroughToNextSection()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V5TOP.
            PROCEDURE DIVISION.
            S1 SECTION.
            P1.
                DISPLAY "s1".
                EXIT SECTION.
                DISPLAY "s1-tail".
            S2 SECTION.
            P2.
                DISPLAY "s2".
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(source);
        Assert.True(ok, detail);
        Assert.Equal("s1\ns2", stdout);
    }

    /// <summary>§14.9.14.3 SR9 — EXIT SECTION outside a section (a paragraph before any SECTION header) is rejected
    /// at compile time (COBOLNET0827, the EXIT-placement SR family).</summary>
    [Fact]
    public void ExitSection_OutsideSection_IsRejected()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V5SR9.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "x".
                EXIT SECTION.
                STOP RUN.
            """;
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(source);
        Assert.False(ok, "EXIT SECTION outside a section must be rejected (SR9)");
        Assert.Contains("COBOLNET0827", detail);
    }
}
