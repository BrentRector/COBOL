// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// CALL / inter-program communication (ISO/IEC 1989:2023 §14.9.4 CALL, §14.9.5 CANCEL, §14.9.14 EXIT PROGRAM,
/// §14.2 parameter passing, §14.6.2.3 program state, §8.4.6.3 program-name scope): spec-derived facts pinned to
/// the former legacy engine (NIST-IC-green) at COBOL-85, multi-unit sources in the IC-suite shape (concatenated
/// top-level program units; nested units carry END PROGRAM). The EXIT-PROGRAM-in-main fact is SPEC-PINNED
/// instead — the legacy deviates from §14.9.14 GR2 there (deep-dive brief, legacy deviation #5; the spec wins).
/// </summary>
public sealed class CallDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new();

    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    /// <summary>Spec-pinned (no oracle): asserted against the ISO-derived expected output directly.</summary>
    private static void AssertSpecPinned(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(expected, cout);
    }

    /// <summary>ISO §11.4.2 — the PROGRAM-ID attribute list takes the optional IS … PROGRAM noise words
    /// (<c>PROGRAM-ID. name IS INITIAL PROGRAM.</c>); IC401M writes the IS form. The INITIAL semantics
    /// themselves (fresh state per activation, §14.6.2.3.3) are exercised by the IC suite.</summary>
    [Fact]
    public void ProgramId_IsInitialProgram_NoiseWordsParse()
        => AssertSpecPinned("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PIDNW1 IS INITIAL PROGRAM.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "OK".
                STOP RUN.
            """, "OK");

    /// <summary>§14.2.3 GR8: BY REFERENCE — "the activated runtime element operates as if the formal parameter
    /// occupies the same storage area as the argument"; the callee's mutation is visible to the caller.</summary>
    [Fact]
    public void CallByReference_CalleeMutationVisibleToCaller()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDREF1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CTR PIC 9(4) VALUE 0010.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "CDREF1S" USING WS-CTR.
                DISPLAY "AFTER=" WS-CTR.
                STOP RUN.
            END PROGRAM CDREF1.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDREF1S.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-CTR PIC 9(4).
            PROCEDURE DIVISION USING LK-CTR.
            SUB-P.
                ADD 5 TO LK-CTR.
                EXIT PROGRAM.
            """);

    /// <summary>§14.2.3 GR9: BY CONTENT — the callee receives a copy "allocated by the activating runtime element";
    /// its mutation is NOT visible to the caller.</summary>
    [Fact]
    public void CallByContent_CalleeMutationNotVisibleToCaller()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDCON1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CTR PIC 9(4) VALUE 0010.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "CDCON1S" USING BY CONTENT WS-CTR.
                DISPLAY "AFTER=" WS-CTR.
                STOP RUN.
            END PROGRAM CDCON1.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDCON1S.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-CTR PIC 9(4).
            PROCEDURE DIVISION USING LK-CTR.
            SUB-P.
                ADD 5 TO LK-CTR.
                EXIT PROGRAM.
            """);

    /// <summary>§14.9.4.4 GR5: with no phrase before the first parameter BY REFERENCE is assumed, and an
    /// explicit BY CONTENT phrase is TRANSITIVE across the parameters that follow it.</summary>
    [Fact]
    public void CallPassingMode_DefaultByReference_PhraseTransitive()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDTRN1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(2) VALUE 10.
            01 WS-B PIC 9(2) VALUE 20.
            01 WS-C PIC 9(2) VALUE 30.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "CDTRN1S" USING WS-A BY CONTENT WS-B WS-C.
                DISPLAY "A=" WS-A " B=" WS-B " C=" WS-C.
                STOP RUN.
            END PROGRAM CDTRN1.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDTRN1S.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-A PIC 9(2).
            01 LK-B PIC 9(2).
            01 LK-C PIC 9(2).
            PROCEDURE DIVISION USING LK-A LK-B LK-C.
            SUB-P.
                ADD 1 TO LK-A.
                ADD 1 TO LK-B.
                ADD 1 TO LK-C.
                EXIT PROGRAM.
            """);

    /// <summary>§14.6.2.3.3 (last-used state) / §8.6.4: a called program's internal WORKING-STORAGE persists in
    /// its last-used state across activations within one run unit.</summary>
    [Fact]
    public void CalledProgram_WorkingStorage_LastUsedAcrossCalls()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDSTA1.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "CDSTA1S".
                CALL "CDSTA1S".
                STOP RUN.
            END PROGRAM CDSTA1.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDSTA1S.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CNT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            SUB-P.
                ADD 1 TO WS-CNT.
                DISPLAY "CNT=" WS-CNT.
                EXIT PROGRAM.
            """);

    /// <summary>§14.9.5 GR3: after CANCEL, the next CALL finds the program in its INITIAL state (VALUE clauses
    /// re-applied); before that, last-used persists.</summary>
    [Fact]
    public void Cancel_NextCallFindsInitialState()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDCAN1.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "CDCAN1S".
                CALL "CDCAN1S".
                CANCEL "CDCAN1S".
                CALL "CDCAN1S".
                STOP RUN.
            END PROGRAM CDCAN1.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDCAN1S.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CNT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            SUB-P.
                ADD 1 TO WS-CNT.
                DISPLAY "CNT=" WS-CNT.
                EXIT PROGRAM.
            """);

    /// <summary>§14.2.3 GR2 / §14.9.4.4 GR2: argument↔formal correspondence is POSITIONAL, never by name — the
    /// callee's first formal receives the first argument regardless of the data-names involved.</summary>
    [Fact]
    public void CallUsing_CorrespondenceIsPositionalNotByName()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDPOS1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9 VALUE 1.
            01 WS-B PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "CDPOS1S" USING WS-A WS-B.
                STOP RUN.
            END PROGRAM CDPOS1.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDPOS1S.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-B PIC 9.
            01 LK-A PIC 9.
            PROCEDURE DIVISION USING LK-B LK-A.
            SUB-P.
                DISPLAY "P1=" LK-B " P2=" LK-A.
                EXIT PROGRAM.
            """);

    /// <summary>§14.9.4.4 GR3b: a CALL identifier target resolves the program-name from the identifier's value
    /// at CALL time (dynamic call).</summary>
    [Fact]
    public void DynamicCall_IdentifierTarget_ResolvesAtRunTime()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDDYN1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-PGM PIC X(8) VALUE "CDDYN1S".
            01 WS-CTR PIC 9(4) VALUE 0001.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL WS-PGM USING WS-CTR.
                DISPLAY "AFTER=" WS-CTR.
                STOP RUN.
            END PROGRAM CDDYN1.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDDYN1S.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-CTR PIC 9(4).
            PROCEDURE DIVISION USING LK-CTR.
            SUB-P.
                ADD 41 TO LK-CTR.
                EXIT PROGRAM.
            """);

    /// <summary>§14.2.3 GR8 over a GROUP argument: the callee's own LINKAGE record description maps the caller's
    /// storage area; subordinate-item mutations propagate back to the caller's record.</summary>
    [Fact]
    public void CallByReference_GroupArgument_SubordinateMutationsVisible()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDGRP1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-REC.
               05 WS-N PIC 9(3) VALUE 7.
               05 WS-T PIC X(5) VALUE "ABCDE".
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "CDGRP1S" USING WS-REC.
                DISPLAY "N=" WS-N " T=" WS-T.
                STOP RUN.
            END PROGRAM CDGRP1.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDGRP1S.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-REC.
               05 LK-N PIC 9(3).
               05 LK-T PIC X(5).
            PROCEDURE DIVISION USING LK-REC.
            SUB-P.
                MOVE 42 TO LK-N.
                MOVE "ZYXWV" TO LK-T.
                EXIT PROGRAM.
            """);

    /// <summary>§8.4.6.3: a contained (nested) program is callable by its directly containing program; the
    /// nested unit carries END PROGRAM markers (the IC222A+ source shape).</summary>
    [Fact]
    public void NestedProgram_ContainedProgramCallableByContainer()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDNST1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC 9(2) VALUE 5.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "CDNST1I" USING WS-X.
                DISPLAY "X=" WS-X.
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDNST1I.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-X PIC 9(2).
            PROCEDURE DIVISION USING LK-X.
            INNER-P.
                ADD 1 TO LK-X.
                EXIT PROGRAM.
            END PROGRAM CDNST1I.
            END PROGRAM CDNST1.
            """);

    /// <summary>§14.9.14 GR2 — SPEC-PINNED: "If the EXIT PROGRAM statement is executed in a program that is not
    /// under the control of a calling runtime element, the EXIT PROGRAM statement is treated as if it were a
    /// CONTINUE statement." The former legacy engine deviates (it terminates the main program — brief, legacy
    /// deviation #5), so this fact asserts the ISO-derived output directly.</summary>
    [Fact]
    public void ExitProgram_InMainProgram_IsContinue()
        => AssertSpecPinned("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDEXP1.
            PROCEDURE DIVISION.
            MAIN-P.
                DISPLAY "BEFORE".
                EXIT PROGRAM.
                DISPLAY "AFTER".
                STOP RUN.
            """,
            "BEFORE\nAFTER");

    /// <summary>§14.9.4.4 GR3h (85 surface — the ON OVERFLOW phrase): when the called program cannot be made
    /// available, the ON phrase's imperative runs and control then falls to the end of the CALL statement.</summary>
    [Fact]
    public void CallOnOverflow_UnresolvableProgram_RunsPhraseThenContinues()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDOVF1.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "CDNOSUCH" ON OVERFLOW DISPLAY "MISSING".
                DISPLAY "DONE".
                STOP RUN.
            """);
}
