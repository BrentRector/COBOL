// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The exception-checking (Format-3) PERFORM RUNTIME interceptor (ISO/IEC 1989:2023 §14.9.28.4 GR14–GR22) — the
/// per-GR behavior net for the pc-RANGE interceptor (design SSOT <c>PHASE-13-c5-perform-format3-DESIGN.md</c> §9).
/// Every expected value is SPEC-PINNED (the former legacy engine has no EC model). Covers: GR17 tier-ordered WHEN match +
/// USE preemption; GR18 WHEN OTHER; GR19 WHEN COMMON; GR20 nonfatal resume-in-place vs fatal abnormal-termination +
/// RESUME NEXT; GR16 FINALLY; §14.9.14.4 GR4 EXIT PERFORM; the version gate (COBOLNET0900) + the staged sub-GAPs
/// (COBOLNET0899). The frame-stack unit mechanics are <c>PerformFrameStackTests</c>.
/// </summary>
public sealed class PerformFormat3BehaviorTests
{
    private static readonly CobolNetCompiler CobolNet = new(2023);

    private static void AssertSpec(string proc, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Prog(proc));
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}\nstdout:\n{stdout}");
        Assert.Equal(expected, stdout);
    }

    private static void AssertFatal(string proc, string ecName, string expectedStdout)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Prog(proc));
        Assert.False(ok, $"expected abnormal termination on {ecName}; ran clean:\n{stdout}");
        Assert.Contains(ecName, detail);
        Assert.Equal(expectedStdout, stdout);
    }

    /// <summary>A minimal program body with EC-ALL checking on (so every raised condition is enabled — GR14/GR18)
    /// and a single-digit item N (for an overflowing-ADD EC trigger).</summary>
    private static string Prog(string proc) => $"""
        >>TURN EC-ALL CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. F3B.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 N PIC 9 VALUE 9.
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    // ── GR17 + GR20 nonfatal (resume-in-place is automatic — the raise site continues after the raiser) ────────

    [Fact]   // GR17 WHEN match runs imp-2; GR20 nonfatal → the statement after the raiser runs (imp-1 not abandoned).
    public void Nonfatal_WhenMatch_ResumesInPlace()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-USER-DEMO
                    DISPLAY "AFTER"
                WHEN EC-USER-DEMO
                    DISPLAY "WHEN"
                END-PERFORM
                DISPLAY "DONE"
            """, "WHEN\nAFTER\nDONE");

    // ── GR20 fatal (WHEN runs, then §14.6.13.1.3 abnormal termination unless RESUME) ───────────────────────────

    [Fact]   // GR20 fatal: the WHEN runs, then — no RESUME — the run unit terminates abnormally (§14.6.13.1.3 #5/#7).
    public void Fatal_WhenMatch_ThenTerminates()
        => AssertFatal("""
                PERFORM
                    RAISE EXCEPTION EC-SIZE-OVERFLOW
                    DISPLAY "AFTER"
                WHEN EC-SIZE-OVERFLOW
                    DISPLAY "HANDLED"
                END-PERFORM
                DISPLAY "DONE"
            """, "EC-SIZE-OVERFLOW", "HANDLED");

    [Fact]   // §14.9.33.4 GR2: RESUME NEXT STATEMENT in a WHEN suppresses the fatal termination and resumes past the
             // raiser (a WHEN's only way to keep a FATAL condition from terminating).
    public void Fatal_ResumeNext_SuppressesTermination()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-SIZE-OVERFLOW
                    DISPLAY "AFTER"
                WHEN EC-SIZE-OVERFLOW
                    DISPLAY "HANDLED"
                    RESUME NEXT STATEMENT
                END-PERFORM
                DISPLAY "DONE"
            """, "HANDLED\nAFTER\nDONE");

    // ── GR17 → §14.9.49.4 GR3c-g tier ordering (NOT written order) ─────────────────────────────────────────────

    [Fact]   // A level-3 EC-BOUND-SUBSCRIPT (tier 2) outranks a level-1 EC-ALL (tier 4) even though EC-ALL is written
             // FIRST — the match is tier-priority (GR3c-g), source order only within a tier.
    public void Tier_SpecificBeatsEcAll_RegardlessOfSourceOrder()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-BOUND-SUBSCRIPT
                WHEN EC-ALL
                    DISPLAY "GENERIC"
                    RESUME NEXT STATEMENT
                WHEN EC-BOUND-SUBSCRIPT
                    DISPLAY "SPECIFIC"
                    RESUME NEXT STATEMENT
                END-PERFORM
                DISPLAY "DONE"
            """, "SPECIFIC\nDONE");

    [Fact]   // A level-2 parent (EC-BOUND, tier 3) matches its level-3 child EC-BOUND-SUBSCRIPT via UnderLevel2, but a
             // level-3 self-name (tier 2) written LATER still wins — tier over source order.
    public void Tier_Level3BeatsLevel2Parent()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-BOUND-SUBSCRIPT
                WHEN EC-BOUND
                    DISPLAY "PARENT"
                    RESUME NEXT STATEMENT
                WHEN EC-BOUND-SUBSCRIPT
                    DISPLAY "SELF"
                    RESUME NEXT STATEMENT
                END-PERFORM
                DISPLAY "DONE"
            """, "SELF\nDONE");

    // ── GR18 WHEN OTHER + GR19 WHEN COMMON ─────────────────────────────────────────────────────────────────────

    [Fact]   // GR18: an enabled condition named by no WHEN runs WHEN OTHER (imp-3).
    public void WhenOther_HandlesUnnamedCondition()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-BOUND-SUBSCRIPT
                    DISPLAY "AFTER"
                WHEN EC-USER-DEMO
                    DISPLAY "SPECIFIC"
                WHEN OTHER
                    DISPLAY "OTHER"
                    RESUME NEXT STATEMENT
                END-PERFORM
                DISPLAY "DONE"
            """, "OTHER\nAFTER\nDONE");

    [Fact]   // GR19: WHEN COMMON (imp-4) runs after the selected WHEN (imp-2) completes, THEN GR20 resumes in place.
    public void WhenCommon_RunsAfterWhen_ThenResumes()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-USER-DEMO
                    DISPLAY "AFTER"
                WHEN EC-USER-DEMO
                    DISPLAY "WHEN"
                WHEN COMMON
                    DISPLAY "COMMON"
                END-PERFORM
                DISPLAY "DONE"
            """, "WHEN\nCOMMON\nAFTER\nDONE");

    [Fact]   // §9.6 Q3 (chosen interpretation): RESUME NEXT in imp-2 is a transfer OUT — WHEN COMMON does NOT run.
    public void ResumeNext_SkipsWhenCommon()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-USER-DEMO
                    DISPLAY "AFTER"
                WHEN EC-USER-DEMO
                    DISPLAY "WHEN"
                    RESUME NEXT STATEMENT
                WHEN COMMON
                    DISPLAY "COMMON"
                END-PERFORM
                DISPLAY "DONE"
            """, "WHEN\nAFTER\nDONE");

    // ── GR16 FINALLY + §14.9.14.4 GR4 EXIT PERFORM ─────────────────────────────────────────────────────────────

    [Fact]   // GR16: FINALLY (imp-5) is the end of the PERFORM — it runs on the normal (nonfatal-resume) fall-through.
    public void Finally_RunsOnNormalPath()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-USER-DEMO
                    DISPLAY "AFTER"
                WHEN EC-USER-DEMO
                    DISPLAY "WHEN"
                FINALLY
                    DISPLAY "FINALLY"
                END-PERFORM
                DISPLAY "DONE"
            """, "WHEN\nAFTER\nFINALLY\nDONE");

    [Fact]   // §14.9.14.4 GR4: an EXIT PERFORM in a handler transfers to the implicit CONTINUE preceding FINALLY —
             // WHEN COMMON and the imp-1 remainder are skipped; FINALLY still runs.
    public void ExitPerform_InHandler_JumpsToFinally_SkipsCommon()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-USER-DEMO
                    DISPLAY "AFTER"
                WHEN EC-USER-DEMO
                    DISPLAY "WHEN"
                    EXIT PERFORM
                WHEN COMMON
                    DISPLAY "COMMON"
                FINALLY
                    DISPLAY "FINALLY"
                END-PERFORM
                DISPLAY "DONE"
            """, "WHEN\nFINALLY\nDONE");

    // ── GR3a/GR3b file & open-mode WHEN operands (a READ past EOF on an INPUT file raises EC-I-O-AT-END) ─────────

    /// <summary>A file-based program: creates a one-record LINE SEQUENTIAL file, reopens it INPUT, then runs
    /// <paramref name="proc"/> (whose imp-1 reads past end-of-file to raise EC-I-O-AT-END on the INPUT-mode file).</summary>
    private static string ProgFile(string name, string proc) => $"""
        >>TURN EC-ALL CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {name}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN "{name}.dat" ORGANIZATION LINE SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 R PIC X(4).
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT F
            WRITE R FROM "AAAA"
            CLOSE F
            OPEN INPUT F
        {proc}
            CLOSE F
            DISPLAY "DONE"
            STOP RUN.
        """;

    private static void AssertFile(string name, string proc, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(ProgFile(name, proc));
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}\nstdout:\n{stdout}");
        Assert.Equal(expected, stdout);
    }

    [Fact]   // GR3b open-mode scope: WHEN EXCEPTION INPUT matches any EC-I-O whose file is currently open INPUT.
    public void OpenModeWhen_MatchesByCurrentOpenMode()
        => AssertFile("F3OPENM", """
                PERFORM
                    READ F
                    READ F
                    DISPLAY "AFTER"
                WHEN EXCEPTION INPUT
                    DISPLAY "INPUT-MODE"
                    RESUME NEXT STATEMENT
                END-PERFORM
            """, "INPUT-MODE\nAFTER\nDONE");

    [Fact]   // GR3a file scope: WHEN EXCEPTION file-name matches any EC-I-O associated with that file.
    public void BareFileWhen_MatchesAnyIoConditionForTheFile()
        => AssertFile("F3BAREF", """
                PERFORM
                    READ F
                    READ F
                    DISPLAY "AFTER"
                WHEN EXCEPTION F
                    DISPLAY "BY-FILE"
                    RESUME NEXT STATEMENT
                END-PERFORM
            """, "BY-FILE\nAFTER\nDONE");

    [Fact]   // GR3 priority: the open-mode scope (GR3b, tier 1) is selected BEFORE an exception-name (GR3e/f) even
             // when the exception-name WHEN is written first — file/mode scope outranks EC-name per GR3's a→g order.
    public void OpenMode_OutranksExceptionName()
        => AssertFile("F3MVSN", """
                PERFORM
                    READ F
                    READ F
                    DISPLAY "AFTER"
                WHEN EC-I-O-AT-END
                    DISPLAY "BY-NAME"
                    RESUME NEXT STATEMENT
                WHEN EXCEPTION INPUT
                    DISPLAY "BY-MODE"
                    RESUME NEXT STATEMENT
                END-PERFORM
            """, "BY-MODE\nAFTER\nDONE");

    // ── GR21 transparency (an EC raised inside a handler is not re-caught by the SAME PERFORM) ──────────────────

    [Fact]   // GR21: an exception condition raised during imp-2 behaves as in a Format-2 PERFORM (this PERFORM's
             // WHEN/OTHER do NOT re-catch it), so the fatal EC-SIZE-OVERFLOW raised in the handler terminates. If
             // GR21 were violated, WHEN OTHER would print "OTHER" and RESUME instead.
             //
             // ⚠ The handler-local >>TURN is load-bearing, not decoration. This test used to rely on the enclosing
             // `>>TURN EC-ALL CHECKING ON` reaching into the handler — which §14.9.28.4 GR14 forbids: an "implicit
             // PUSH ALL followed by TURN OFF ALL is assumed at the end of imperative-statement-1", so no checking
             // is enabled inside a handler and §14.6.13.1.1 means the ADD raises nothing at all. The old form
             // asserted pre-GR14 behaviour and passed only because handler bodies wrongly inherited the enclosing
             // state. An explicit RAISE cannot substitute — §14.9.29.3 SR4: "Within an exception-checking PERFORM
             // statement, the RAISE statement shall not be specified in any imperative statement other than
             // imperative-statement-1" (COBOLNET1611). Re-enabling INSIDE the handler is the legal way to raise
             // there, and it doubles as the proof that a handler-local directive still wins over the GR14 floor.
    public void Gr21_ReRaiseInHandler_NotReCaught_FallsToFatal()
        => AssertFatal("""
                PERFORM
                    RAISE EXCEPTION EC-USER-DEMO
                    DISPLAY "AFTER"
                WHEN EC-USER-DEMO
                    DISPLAY "WHEN"
                >>TURN EC-SIZE-TRUNCATION CHECKING ON
                    ADD 9 TO N
                    DISPLAY "AFTER-ADD"
                WHEN OTHER
                    DISPLAY "OTHER"
                    RESUME NEXT STATEMENT
                END-PERFORM
                DISPLAY "DONE"
            """, "EC-SIZE-TRUNCATION", "WHEN");

    // ── The version gate + the staged sub-GAPs (loud, never silent) ────────────────────────────────────────────

    [Fact]   // §14.9.28 Format 3 is COBOL-2023; below 2023 it is rejected by the construct gate (COBOLNET0900). Tested
             // at 2014 (where >>TURN itself is already legal, isolating the PERFORM gate).
    public void VersionGate_Below2023_Rejected0900()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            >>TURN EC-ALL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. F3G.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM
                    CONTINUE
                WHEN EC-USER-DEMO
                    CONTINUE
                END-PERFORM
                STOP RUN.
            """, 2014), "COBOLNET0900");

    [Fact]   // The open-mode WHEN operand form (WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND) now compiles clean (GR3b
             // open-mode scope landed — the runtime matches by the raising file's current open mode); no 0899.
    public void OpenModeWhen_CompilesClean()
    {
        var (ok, diag) = EditionHarness.Compile("""
            >>TURN EC-ALL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. F3M.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "f.dat".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 R PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM
                    CONTINUE
                WHEN EXCEPTION INPUT
                    CONTINUE
                END-PERFORM
                STOP RUN.
            """, 2023);
        Assert.True(ok, string.Join("\n", diag));
        EditionHarness.AssertNoDiagnostic(diag, "COBOLNET0899");
    }
}
