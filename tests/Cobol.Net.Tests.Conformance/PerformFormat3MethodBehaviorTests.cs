// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The exception-checking (Format-3) PERFORM RUNTIME interceptor INSIDE AN OO METHOD (ISO/IEC 1989:2023 §14.9.28.4
/// GR14–GR22; §11.7; design SSOT <c>PHASE-13-c5-perform-format3-DESIGN.md</c> §9.10). Mirrors the program-path
/// <see cref="PerformFormat3BehaviorTests"/> imp-2..5 matrix, but the F3 PERFORM lives in a method whose handler
/// bodies reference the method's per-activation LOCAL-STORAGE — so every green case ALSO proves the method-local
/// machinery (method-local <c>__RunUse</c>/<c>__RunF3</c>, the two-range <c>__MDispatch</c>, and the frame Matcher
/// capturing the method's locals). Plus the §9.10.1-C2 cross-INVOKE frame-floor isolation. Every expected value is
/// SPEC-PINNED (the legacy oracle has no EC model, let alone in a method).
/// </summary>
public sealed class PerformFormat3MethodBehaviorTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler(2023);

    /// <summary>A driver program that creates an <c>F3MCLS</c> object and INVOKEs its <c>DOIT</c> method; the method
    /// carries a per-activation LOCAL-STORAGE <c>N</c> (PIC 9 VALUE 9) and runs <paramref name="proc"/> in its body.
    /// The method's stdout IS the whole program's stdout (the driver prints nothing), so the expected strings match
    /// <see cref="PerformFormat3BehaviorTests"/>'s program-path values exactly.</summary>
    private static string MProg(string proc) => $$"""
        >>TURN EC-ALL CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. F3MDRV.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS F3MCLS.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 O USAGE OBJECT REFERENCE F3MCLS.
        PROCEDURE DIVISION.
        MAIN.
            INVOKE F3MCLS "NEW" RETURNING O.
            INVOKE O "DOIT".
            STOP RUN.
        END PROGRAM F3MDRV.
        IDENTIFICATION DIVISION.
        CLASS-ID. F3MCLS INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        METHOD-ID. DOIT.
        DATA DIVISION.
        LOCAL-STORAGE SECTION.
        01 N PIC 9 VALUE 9.
        PROCEDURE DIVISION.
        MAIN.
        {{proc}}.
        END METHOD DOIT.
        END OBJECT.
        END CLASS F3MCLS.
        """;

    private static void AssertSpec(string proc, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(MProg(proc));
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}\nstdout:\n{stdout}");
        Assert.Equal(expected, stdout);
    }

    private static void AssertFatal(string proc, string ecName, string expectedStdout)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(MProg(proc));
        Assert.False(ok, $"expected abnormal termination on {ecName}; ran clean:\n{stdout}");
        Assert.Contains(ecName, detail);
        Assert.Equal(expectedStdout, stdout);
    }

    // ── GR17 + GR20 nonfatal (resume-in-place; imp-1 not abandoned) — in a method ────────────────────────────────

    [Fact]   // GR17 WHEN match runs imp-2; GR20 nonfatal → the statement after the raiser runs. Handler + imp-1 both
             // read the method's LOCAL-STORAGE N (proves the per-activation capture, design §9.10).
    public void Nonfatal_WhenMatch_ResumesInPlace()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-USER-DEMO
                    DISPLAY "AFTER N=" N
                WHEN EC-USER-DEMO
                    DISPLAY "WHEN N=" N
                END-PERFORM
                DISPLAY "DONE"
            """, "WHEN N=9\nAFTER N=9\nDONE");

    // ── GR20 fatal (WHEN runs, then abnormal termination unless RESUME) — the method's fatal propagates past the
    //    INVOKE to terminate the run unit (the CobolFatalException unwinds through catch(MethodReturn)/finally). ─────

    [Fact]
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

    [Fact]   // §14.9.33.4 GR2: RESUME NEXT STATEMENT in a WHEN suppresses the fatal termination and resumes past the raiser.
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

    // ── GR17 → §14.9.49.4 GR3c-g tier ordering (NOT written order) — in a method ─────────────────────────────────

    [Fact]   // A level-3 EC-BOUND-SUBSCRIPT (tier 2) outranks a level-1 EC-ALL (tier 4) though EC-ALL is written FIRST.
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

    // ── GR18 WHEN OTHER + GR19 WHEN COMMON — in a method ─────────────────────────────────────────────────────────

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

    [Fact]   // GR19: WHEN COMMON (imp-4) runs after the selected WHEN (imp-2) completes, then GR20 resumes in place.
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

    [Fact]   // §9.6 Q3: RESUME NEXT in imp-2 is a transfer OUT — WHEN COMMON does NOT run.
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

    // ── GR16 FINALLY + §14.9.14.4 GR4 EXIT PERFORM — in a method ─────────────────────────────────────────────────

    [Fact]   // GR16: FINALLY (imp-5) runs on the normal (nonfatal-resume) fall-through — inline in the method body.
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
             // the method-local ExitPerformSignal is caught at THIS PERFORM's boundary (WHEN COMMON + imp-1 skipped).
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

    // ── GR21 transparency — in a method (the handler raises a NEW EC the frame is transparent to → the fatal
    //    default terminates; if GR21 were violated, WHEN OTHER would RESUME). ─────────────────────────────────────

    [Fact]   // ⚠ Handler-local >>TURN, for the same reason as the program-level twin in PerformFormat3BehaviorTests:
             // this used to rely on the enclosing `>>TURN EC-ALL CHECKING ON` reaching into the handler, which
             // §14.9.28.4 GR14 forbids — "an implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of
             // imperative-statement-1", so the ADD raises nothing. An explicit RAISE cannot substitute (§14.9.29.3
             // SR4 bars RAISE outside imp-1, COBOLNET1611), so re-enabling inside the handler is the legal way to
             // raise there — and it proves a handler-local directive still wins over the GR14 floor.
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

    // ── The method-local per-activation capture, made explicit (design §9.10 core) ───────────────────────────────

    [Fact]   // The handler MUTATES the method's LOCAL-STORAGE N (9 − 4 = 5, no overflow); imp-1 (resumed in place)
             // reads the mutated value — proving the handler runs in the method's __MDispatch over the SAME captured
             // per-activation locals (design §9.10 core). A class member could neither reach __MDispatch nor see N.
    public void MethodLocal_HandlerMutation_VisibleToResumedImp1()
        => AssertSpec("""
                PERFORM
                    RAISE EXCEPTION EC-USER-DEMO
                    DISPLAY "IMP1 N=" N
                WHEN EC-USER-DEMO
                    SUBTRACT 4 FROM N
                    DISPLAY "HANDLER N=" N
                END-PERFORM
                DISPLAY "DONE"
            """, "HANDLER N=5\nIMP1 N=5\nDONE");

    // ── §9.10.1-C2 cross-INVOKE frame-floor isolation ────────────────────────────────────────────────────────────

    [Fact]   // An F3 method OUTER whose imp-1 INVOKEs an F3 method INNER: INNER raises EC-USER-DEMO, which INNER's own
             // WHEN (EC-SIZE-OVERFLOW) does NOT match. The entry FLOOR hides OUTER's frame from INNER, so INNER's EC is
             // NOT caught by OUTER's WHEN (it falls to INNER's default — a nonfatal RAISE simply continues). Without the
             // floor, "OUTER-CAUGHT-LEAK" would print. A method is a separate source element (§14.9.18.3 SR2/SR4a).
    public void CrossInvoke_InnerUnmatchedEc_DoesNotReachOuterWhen()
    {
        string src = """
            >>TURN EC-ALL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. F3XDRV.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS F3XCLS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE F3XCLS.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE F3XCLS "NEW" RETURNING O.
                INVOKE O "OUTER" USING O.
                STOP RUN.
            END PROGRAM F3XDRV.
            IDENTIFICATION DIVISION.
            CLASS-ID. F3XCLS INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. OUTER.
            DATA DIVISION.
            LINKAGE SECTION.
            01 SELF-REF USAGE OBJECT REFERENCE F3XCLS.
            PROCEDURE DIVISION USING SELF-REF.
            MAIN.
                PERFORM
                    INVOKE SELF-REF "INNER"
                    DISPLAY "OUTER-AFTER"
                WHEN EC-USER-DEMO
                    DISPLAY "OUTER-CAUGHT-LEAK"
                    RESUME NEXT STATEMENT
                END-PERFORM
                DISPLAY "OUTER-DONE".
            END METHOD OUTER.
            METHOD-ID. INNER.
            PROCEDURE DIVISION.
            MAIN.
                PERFORM
                    RAISE EXCEPTION EC-USER-DEMO
                    DISPLAY "INNER-CONTINUED"
                WHEN EC-SIZE-OVERFLOW
                    DISPLAY "INNER-SIZE"
                END-PERFORM.
            END METHOD INNER.
            END OBJECT.
            END CLASS F3XCLS.
            """;
        var (ok, stdout, detail) = CobolNet.CompileAndRun(src);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}\nstdout:\n{stdout}");
        Assert.Equal("INNER-CONTINUED\nOUTER-AFTER\nOUTER-DONE", stdout);
    }
}
