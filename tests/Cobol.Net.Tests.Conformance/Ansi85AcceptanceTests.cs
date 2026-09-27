// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The X3.23-1985 notInGrammar 85-acceptance set (roadmap Phase 2 W3 item ④ — VCR Table 7 rows 7.15–7.18):
/// RERUN, ENTER, USE FOR DEBUGGING, and section-header segment-numbers — four obsolete '85 elements DELETED by
/// ISO/IEC 1989:2002 that formerly had no grammar at all (generic parse errors at EVERY edition, the G1
/// co-equal-diagnostic violation). These facts pin the ACCEPTED 85 leg (parse + correct run semantics — RERUN /
/// ENTER / segment-numbers are inert; USE FOR DEBUGGING is MODELED at 85, the procedure-trigger leg, VCR 7.17)
/// and the per-word §8.9 user-word continuity; the ≥2002 reject/permissive legs are pinned by the
/// four constructs.json matrix rows and the negative corpus (rerun / enter / use-for-debugging /
/// segment-numbers). No ISO-2023 § exists for any of them — the registry rows cite the §8.9 ABSENCE pinpoints.
/// </summary>
public sealed class Ansi85AcceptanceTests
{
    private static readonly ICompilerUnderTest CobolNet85 = new CobolNetCompiler();   // dialect 85 (default)

    private static void AssertRuns(string source, string expected)
    {
        var (ok, stdout, detail) = CobolNet85.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(CutRunner.Normalize(expected), stdout);
    }

    // ── RERUN (row 7.15): a checkpoint HINT — parsed-and-ignored is a conforming null rerun facility ──

    [Fact]
    public void Rerun_RecordsForm_InertAt85_IoUnaffected() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85RR1.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "a85rr1" ORGANIZATION IS SEQUENTIAL.
        I-O-CONTROL.
            RERUN ON F EVERY 10 RECORDS OF F.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 R PIC X(10).
        WORKING-STORAGE SECTION.
        01 W PIC X(10).
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT F.
            MOVE "CHECKPOINT" TO R.
            WRITE R.
            CLOSE F.
            OPEN INPUT F.
            READ F INTO W.
            CLOSE F.
            DISPLAY W.
            STOP RUN.
        """, "CHECKPOINT");

    /// <summary>All three remaining X3.23-1985 EVERY forms in one paragraph: integer CLOCK-UNITS,
    /// condition-name (a SPECIAL-NAMES switch-status condition), and [END OF] REEL/UNIT OF file — plus the
    /// ON-phrase-less shape.</summary>
    [Fact]
    public void Rerun_ClockUnitsConditionAndReelForms_ParseAt85() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85RR2.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "a85rr2" ORGANIZATION IS SEQUENTIAL.
        I-O-CONTROL.
            RERUN ON F EVERY 20 CLOCK-UNITS
            RERUN ON F EVERY SW1-ON
            RERUN EVERY END OF REEL OF F.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 R PIC X(5).
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT F.
            CLOSE F.
            DISPLAY "RERUN-FORMS-OK".
            STOP RUN.
        """, "RERUN-FORMS-OK");

    /// <summary>The SQ206A adjacency pattern: a SAME clause and a RERUN clause under ONE period — the
    /// sameClause file-name loop must not swallow the RERUN head (RERUN is cobolWord-admitted; ALL(*)
    /// resolves the loop-exit on the following ON/EVERY).</summary>
    [Fact]
    public void Rerun_AfterSameClause_OnePeriod_Parses() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85RR3.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F1 ASSIGN TO "a85rr3a" ORGANIZATION IS SEQUENTIAL.
            SELECT F2 ASSIGN TO "a85rr3b" ORGANIZATION IS SEQUENTIAL.
        I-O-CONTROL.
            SAME RECORD AREA FOR F1 F2
            RERUN ON F1 EVERY 5 RECORDS OF F1.
        DATA DIVISION.
        FILE SECTION.
        FD F1.
        01 R1 PIC X(5).
        FD F2.
        01 R2 PIC X(5).
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT F1.
            MOVE "AAAAA" TO R1.
            WRITE R1.
            CLOSE F1.
            DISPLAY "SAME-RERUN-OK".
            STOP RUN.
        """, "SAME-RERUN-OK");

    // ── ENTER (row 7.16): comment-equivalent when only COBOL is supported (BoundNop) ──

    /// <summary>The classic paired idiom — ENTER LINKAGE … ENTER COBOL. Both operands are SYSTEM-names
    /// (deliberately not cobolWord): 'COBOL' is an '85 §8.9 reserved word, and a cobolWord slot would
    /// false-reject the conforming switch-back with 0901.</summary>
    [Fact]
    public void Enter_LinkageAndCobol_InertAt85() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85EN1.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W PIC 9 VALUE 7.
        PROCEDURE DIVISION.
        MAIN.
            ENTER LINKAGE.
            DISPLAY W.
            ENTER COBOL.
            DISPLAY "ENTER-OK".
            STOP RUN.
        """, "7\nENTER-OK");

    /// <summary>ENTER language-name routine-name (the two-operand form) between statements of one sentence.</summary>
    [Fact]
    public void Enter_WithRoutineName_InertAt85() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85EN2.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W PIC 99 VALUE 42.
        PROCEDURE DIVISION.
        MAIN.
            ENTER FORTRAN SUBR1
            DISPLAY W.
            STOP RUN.
        """, "42");

    // ── USE FOR DEBUGGING (row 7.17): the '85 debug module, MODELED at --std 85 (procedure-trigger leg) ──

    /// <summary>WITH DEBUGGING MODE present: the debugging section IS compiled AND, with the object-time switch ON
    /// (RunUnit.DebugMode default true — the CCVS posture), the ON ALL PROCEDURES declarative FIRES just before each
    /// nondeclarative procedure. Here M1 is the sole nondeclarative procedure (its first execution → "START
    /// PROGRAM"), so the debug section runs before M1's body.</summary>
    [Fact]
    public void UseForDebugging_SwitchPresent_AllProcedures_FiresTheDeclarative() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85UD1.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SOURCE-COMPUTER. A85BOX WITH DEBUGGING MODE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W PIC 9 VALUE 3.
        PROCEDURE DIVISION.
        DECLARATIVES.
        DBG-SEC SECTION.
            USE FOR DEBUGGING ON ALL PROCEDURES.
        DBG-PARA.
            DISPLAY "DBG " DEBUG-CONTENTS.
        END DECLARATIVES.
        MAIN SECTION.
        M1.
            DISPLAY W.
            DISPLAY "DEBUG-OK".
            STOP RUN.
        """, "DBG START PROGRAM\n3\nDEBUG-OK");

    /// <summary>WITHOUT the switch, X3.23-1985 compiles debugging sections as if they were COMMENT lines —
    /// so even DEBUG-* register references inside must compile (the DB103M shape: no switch, 95 register
    /// references, designed by NIST to run with the sections inert).</summary>
    [Fact]
    public void UseForDebugging_NoSwitch_CommentTreated_DebugRegistersCompile() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85UD2.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W PIC X(6).
        PROCEDURE DIVISION.
        DECLARATIVES.
        DBG-SEC SECTION.
            USE FOR DEBUGGING ON ALL PROCEDURES.
        DBG-PARA.
            MOVE DEBUG-LINE TO W.
            DISPLAY DEBUG-NAME.
        END DECLARATIVES.
        MAIN SECTION.
        M1.
            DISPLAY "COMMENT-TREATED".
            STOP RUN.
        """, "COMMENT-TREATED");

    /// <summary>WITH the switch, a DEBUG-* register reference is a legal '85 use of the now-MODELED facility — it
    /// resolves to the DEBUG-ITEM register (an alphanumeric view), so the program COMPILES; never a deferred
    /// COBOLNET0899 nor a false §8.9 "reserved word as user-defined word" COBOLNET0901.</summary>
    [Fact]
    public void UseForDebugging_SwitchPresent_DebugRegisters_Resolve()
    {
        var (ok, errors, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. A85UD3.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. A85BOX WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC X(6).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG-SEC SECTION.
                USE FOR DEBUGGING ON ALL PROCEDURES.
            DBG-PARA.
                MOVE DEBUG-LINE TO W.
            END DECLARATIVES.
            MAIN SECTION.
            M1.
                STOP RUN.
            """, 85);
        Assert.True(ok, string.Join("\n", errors));
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0899");
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0901");
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET1571");
    }

    /// <summary>The '85 operand forms in one declarative: ALL REFERENCES OF identifier (OF-qualified),
    /// a second bare operand in the same phrase (the DB202A shape), a file-name, and a procedure-name.</summary>
    [Fact]
    public void UseForDebugging_OperandForms_ParseAt85() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85UD4.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "a85ud4" ORGANIZATION IS SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 R PIC X(5).
        WORKING-STORAGE SECTION.
        01 G1.
           02 A1 PIC 9.
           02 A2 PIC 9.
        PROCEDURE DIVISION.
        DECLARATIVES.
        DBG-SEC SECTION.
            USE FOR DEBUGGING ON ALL REFERENCES OF A1 OF G1 A2 F M1.
        DBG-PARA.
            DISPLAY "NEVER-SEEN".
        END DECLARATIVES.
        MAIN SECTION.
        M1.
            DISPLAY "OPERANDS-OK".
            STOP RUN.
        """, "OPERANDS-OK");

    // ── Section-header segment-numbers (row 7.18): all segments resident — a conforming posture ──

    /// <summary>Fixed (10) and independent (50) segments plus a declarative-section segment-number; control
    /// flows through the sections exactly as if unsegmented (the '85 segmentation guarantee).</summary>
    [Fact]
    public void SegmentNumbers_InertAt85_ControlFlowUnchanged() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85SG1.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "a85sg1" ORGANIZATION IS SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 R PIC X(5).
        WORKING-STORAGE SECTION.
        01 W PIC 9 VALUE 5.
        PROCEDURE DIVISION.
        DECLARATIVES.
        ERR-SEC SECTION 04.
            USE AFTER STANDARD ERROR PROCEDURE ON F.
        ERR-PARA.
            DISPLAY "IO-ERROR".
        END DECLARATIVES.
        FIRST-PART SECTION 10.
        P1.
            DISPLAY W.
        SECOND-HALF SECTION 50.
        P2.
            DISPLAY "SEG-OK".
            STOP RUN.
        """, "5\nSEG-OK");

    /// <summary>SG101A's integer-named-section shape: an INTEGERLIT section NAME carrying a segment-number
    /// (`00 SECTION 00.`) — the name and the priority are distinct slots.</summary>
    [Fact]
    public void SegmentNumbers_IntegerSectionName_ParsesAt85() => AssertRuns("""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A85SG2.
        PROCEDURE DIVISION.
        00 SECTION 00.
        P1.
            DISPLAY "INT-NAME-OK".
        99 SECTION 77.
        P2.
            STOP RUN.
        """, "INT-NAME-OK");

    // ── The DB corpus witnesses (golden-less residue made compilable by this batch) ──

    [Theory]
    [InlineData("DB103M")]   // no switch + 95 DEBUG-register references → comment treatment
    [InlineData("DB301M")]   // switch + USE FOR DEBUGGING → the procedure-trigger leg is modeled; compiles
    [InlineData("DB302M")]
    [InlineData("DB305M")]
    public void DbResidue_CompilesAt85(string name)
    {
        var (ok, diagnostics) = EditionHarness.CompileNist(name, 85);
        Assert.True(ok, string.Join("\n", diagnostics));
    }

    /// <summary>DB101A (switch + active DEBUG-* register use + ON procedure-name subjects) COMPILES at 85 — the
    /// X3.23-1985 procedure-trigger debug facility (DEBUG-ITEM register + ON procedure-name / ALL PROCEDURES) is
    /// modeled (VCR Table 7 row 7.17), so its DEBUG-* references resolve and its debugging declaratives bind; no
    /// deferred COBOLNET0899, no false COBOLNET0901, and no COBOLNET1571 (its subjects are all procedure-names).</summary>
    [Fact]
    public void Db101a_CompilesAt85_DebugFacilityModeled()
    {
        var (ok, diagnostics) = EditionHarness.CompileNist("DB101A", 85);
        Assert.True(ok, string.Join("\n", diagnostics));
        EditionHarness.AssertNoDiagnostic(diagnostics, "COBOLNET0899");
        EditionHarness.AssertNoDiagnostic(diagnostics, "COBOLNET0901");
        EditionHarness.AssertNoDiagnostic(diagnostics, "COBOLNET1571");
    }

    // ── §8.9 user-word continuity: each word frees exactly at its ReservedWords.Table edition ──

    [Theory]
    [InlineData("RERUN", 2002)]
    [InlineData("ENTER", 2002)]
    [InlineData("DEBUGGING", 2014)]
    [InlineData("EVERY", 2023)]
    [InlineData("CLOCK-UNITS", 2023)]
    [InlineData("REFERENCES", 2023)]
    [InlineData("PROCEDURES", 2023)]
    public void Word_FreesAsUserWord_AtItsTableEdition(string word, int freeEdition)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. A85UW{freeEdition % 100}{word[0]}{word[^1]}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 {word} PIC 9 VALUE 4.
            PROCEDURE DIVISION.
            MAIN.
                MOVE 6 TO {word}.
                DISPLAY {word}.
                STOP RUN.
            """;
        // 0901 at 85 strict (the '85 reservation, §8.3.2.1 rule 1)…
        var (ok85, errors85, _) = EditionHarness.CompileFull(src, 85);
        Assert.False(ok85);
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0901");
        // …reserved through freeEdition-1 (the last still-reserved edition below it, when one exists)…
        int[] editions = EditionHarness.Editions;
        int lastReserved = editions.Last(e => e < freeEdition);
        if (lastReserved != 85)
        {
            var (okMid, errorsMid, _) = EditionHarness.CompileFull(src, lastReserved);
            Assert.False(okMid);
            EditionHarness.AssertHasDiagnostic(errorsMid, "COBOLNET0901");
        }
        // …and a plain user word from freeEdition on (compiles + runs).
        var (ok, stdout, detail) = new CobolNetCompiler(freeEdition).CompileAndRun(src);
        Assert.True(ok, $"'{word}' should be a user word at {freeEdition}: {detail}");
        Assert.Equal("6", stdout);
    }
}
