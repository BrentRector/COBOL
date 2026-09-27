// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The X3.23-1985 USE FOR DEBUGGING / DEBUG-ITEM special-register facility at <c>--std 85</c> (VCR Table 7 row
/// 7.17). The '85 debug module was deleted by ISO/IEC 1989:2002 and is absent from ISO/IEC 1989:2023, so its
/// authoritative behavior is the 1985 standard; WiseOwl COBOL models it (accepted-and-ACTIVE) only at <c>--std 85</c>
/// and rejects it ≥2002 (COBOLNET0902). Implemented: the ON procedure-name / ALL PROCEDURES trigger leg with the
/// DEBUG-CONTENTS transfer-cause taxonomy (START PROGRAM / SPACES / PERFORM LOOP / FALL THROUGH — DB101A witness)
/// and DEBUG-NAME. STAGED loud (COBOLNET1571): the data-name / file-name / cd-name subject kinds and the
/// SORT/MERGE-procedure cause.
/// </summary>
public sealed class DebugFacilityTests
{
    private const string AllProcedures = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DBGALLPROC.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 NM   PIC X(30).
        01 CONT PIC X(13).
        PROCEDURE DIVISION.
        DECLARATIVES.
        DBG SECTION.
            USE FOR DEBUGGING ON ALL PROCEDURES.
        DBG-BODY.
            MOVE DEBUG-NAME     TO NM.
            MOVE DEBUG-CONTENTS TO CONT.
            DISPLAY "N=" NM "C=" CONT.
        END DECLARATIVES.
        MAIN SECTION.
        P-START.
            PERFORM P-LOOP 2 TIMES.
            GO TO P-END.
        P-LOOP.
            CONTINUE.
        P-END.
            STOP RUN.
        """;

    /// <summary>ALL PROCEDURES fires before each nondeclarative procedure; the DEBUG-CONTENTS taxonomy is the
    /// DB101A-witnessed START PROGRAM / SPACES / PERFORM LOOP set (hand-derived stdout, §X3.23-1985).</summary>
    [Fact]
    public void AllProcedures_At85_FiresWithTheDebugContentsTaxonomy()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(AllProcedures, 85);
        Assert.True(ok, detail);
        // CutRunner normalizes (per-line trailing-space trim), so trim the expected lines' trailing spaces too;
        // NM's INTERNAL padding (before "C=") survives the trim, the DEBUG-CONTENTS taxonomy is the observable.
        string[] lines = stdout.Split('\n');
        Assert.Equal(4, lines.Length);
        // NM is X(30): name left-justified, right-space-padded. CONT is X(13).
        Assert.Equal(("N=" + "P-START".PadRight(30) + "C=START PROGRAM").TrimEnd(), lines[0]);
        Assert.Equal(("N=" + "P-LOOP".PadRight(30) + "C=").TrimEnd(), lines[1]);          // PERFORM iter 1 -> SPACES
        Assert.Equal(("N=" + "P-LOOP".PadRight(30) + "C=PERFORM LOOP").TrimEnd(), lines[2]); // iter 2 -> PERFORM LOOP
        Assert.Equal(("N=" + "P-END".PadRight(30) + "C=").TrimEnd(), lines[3]);            // GO TO -> SPACES
    }

    /// <summary>DEBUG-LINE identifies the CAUSING statement (X3.23-1985; DB101A): START PROGRAM → the subject's
    /// first executable statement (here P-START's <c>PERFORM P-LOOP</c>); a PERFORM trigger → the PERFORM
    /// statement's line on EVERY iteration (DB101A PERF-ITERATION-TEST :611-617); a GO TO trigger → the GO TO
    /// statement's line (DB101A GO-TO-TEST :482-489). Asserted relationally so the absolute source offset is
    /// irrelevant: P-START's PERFORM is the subject's first statement, so all three PERFORM-related DEBUG-LINEs are
    /// equal; the GO TO is exactly one line below.</summary>
    [Fact]
    public void DebugLine_At85_IdentifiesTheCausingStatement()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGLINE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 LN PIC X(6).
            01 CT PIC X(13).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL PROCEDURES.
            DBG-BODY.
                MOVE DEBUG-LINE     TO LN.
                MOVE DEBUG-CONTENTS TO CT.
                DISPLAY "L=[" LN "]C=[" CT "]".
            END DECLARATIVES.
            MAIN SECTION.
            P-START.
                PERFORM P-LOOP 2 TIMES.
                GO TO P-END.
            P-LOOP.
                CONTINUE.
            P-END.
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        string[] lines = stdout.Split('\n');
        Assert.Equal(4, lines.Length);
        static (int Line, string Cont) Parse(string s)
        {
            int lb = s.IndexOf("L=[", System.StringComparison.Ordinal) + 3;
            int rb = s.IndexOf(']', lb);
            int cb = s.IndexOf("C=[", System.StringComparison.Ordinal) + 3;
            int cr = s.IndexOf(']', cb);
            return (int.Parse(s[lb..rb].Trim()), s[cb..cr].TrimEnd());
        }
        var p0 = Parse(lines[0]);   // P-START, START PROGRAM
        var p1 = Parse(lines[1]);   // P-LOOP iter 1, SPACES
        var p2 = Parse(lines[2]);   // P-LOOP iter 2, PERFORM LOOP
        var p3 = Parse(lines[3]);   // P-END, GO TO (SPACES)
        Assert.Equal("START PROGRAM", p0.Cont);
        Assert.Equal("", p1.Cont);
        Assert.Equal("PERFORM LOOP", p2.Cont);
        Assert.Equal("", p3.Cont);
        // The PERFORM line = P-START's first statement (START PROGRAM subject line) = the PERFORM cause line on
        // both iterations; the GO TO is exactly the next line (the causing-statement semantics — NOT the subject
        // procedure's own body line, which would make them all equal).
        Assert.Equal(p0.Line, p1.Line);
        Assert.Equal(p0.Line, p2.Line);
        Assert.Equal(p0.Line + 1, p3.Line);
    }

    /// <summary>DEBUG-SUB-1/2/3 render SPACES for a procedure trigger (an unsubscripted reference) at the S9(4)
    /// SIGN LEADING SEPARATE width of FIVE characters; the whole DEBUG-ITEM group image is 86 characters
    /// (6 + 1 + 30 + 1 + 5 + 1 + 5 + 1 + 5 + 1 + 30). Bracketed so CutRunner's trailing-space trim does not eat
    /// the widths.</summary>
    [Fact]
    public void DebugSubAndGroupWidth_At85_ArePinned()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGSUB.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GI PIC X(86).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL PROCEDURES.
            DBG-BODY.
                MOVE DEBUG-ITEM TO GI.
                DISPLAY "S1=[" DEBUG-SUB-1 "]".
                DISPLAY "GI=[" GI "]".
            END DECLARATIVES.
            MAIN SECTION.
            P1.
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        string[] lines = stdout.Split('\n');
        Assert.Equal("S1=[     ]", lines[0]);   // DEBUG-SUB-1 = five SPACES (unsubscripted, S9(4) SIGN LEADING SEPARATE width)
        // The whole DEBUG-ITEM group image is exactly 86 characters between the brackets.
        int lb = lines[1].IndexOf('[') + 1, rb = lines[1].LastIndexOf(']');
        Assert.Equal(86, rb - lb);
    }

    /// <summary>The removal gate: WITH DEBUGGING MODE + USE FOR DEBUGGING are rejected COBOLNET0902 at ≥2002 (the
    /// '85 debug module deleted 2002 — §8.9 absence).</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void DebugFacility_AtOrAbove2002_RejectsWith0902(int edition)
    {
        var diags = EditionHarness.GetDiagnostics(AllProcedures, edition);
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0902");
    }

    /// <summary>A bare procedure-name subject (ON P-LOOP) fires ONLY for that procedure — P-START / P-END are not
    /// subjects, so only P-LOOP's two PERFORM iterations produce output.</summary>
    [Fact]
    public void OnProcedureName_At85_FiresOnlyForThatProcedure()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGONEPROC.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NM PIC X(30).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON P-LOOP.
            DBG-BODY.
                MOVE DEBUG-NAME TO NM.
                DISPLAY "N=" NM.
            END DECLARATIVES.
            MAIN SECTION.
            P-START.
                PERFORM P-LOOP 2 TIMES.
                GO TO P-END.
            P-LOOP.
                CONTINUE.
            P-END.
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        string[] lines = stdout.Split('\n');
        Assert.Equal(2, lines.Length);
        // Only P-LOOP is a subject; DEBUG-NAME left-justified in X(30), trailing spaces trimmed by CutRunner.
        Assert.Equal("N=P-LOOP", lines[0]);
        Assert.Equal("N=P-LOOP", lines[1]);
    }

    /// <summary>WITHOUT WITH DEBUGGING MODE the debugging section is comment-treated (compiled as if comment lines):
    /// it never fires, and the program runs its ordinary body (X3.23-1985 — the conforming switch-absent posture).</summary>
    [Fact]
    public void SwitchAbsent_At85_CommentTreatsTheSection()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGABSENT.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NM PIC X(30).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL PROCEDURES.
            DBG-BODY.
                MOVE DEBUG-NAME TO NM.
                DISPLAY "DBG " NM.
            END DECLARATIVES.
            MAIN SECTION.
            P1.
                DISPLAY "IN TARGET".
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.Equal("IN TARGET", stdout.Replace("\r", "").TrimEnd('\n'));
    }

    /// <summary>The data-name subject leg (ON ALL REFERENCES OF identifier) is STAGED — rejected loud COBOLNET1571
    /// (its after-statement data trigger + DEBUG-SUB rendering are not modeled).</summary>
    [Fact]
    public void OnDataName_At85_StagedLoud1571()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGDATA.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC X(5).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF T.
            DBG-BODY.
                DISPLAY DEBUG-NAME.
            END DECLARATIVES.
            MAIN SECTION.
            P1.
                MOVE "Z" TO T.
                STOP RUN.
            """;
        var (ok, diags) = EditionHarness.Compile(src, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET1571");
    }

    /// <summary>A SORT/MERGE INPUT/OUTPUT procedure that is also a debug subject is STAGED — rejected loud
    /// COBOLNET1571 (the SORT INPUT/OUTPUT / MERGE OUTPUT DEBUG-CONTENTS cause is not modeled; reject rather than
    /// emit a stale cause).</summary>
    [Fact]
    public void SortProcedureSubject_At85_StagedLoud1571()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGSORT.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SF ASSIGN TO "sf.tmp".
            DATA DIVISION.
            FILE SECTION.
            SD SF.
            01 SR PIC X(5).
            WORKING-STORAGE SECTION.
            01 NM PIC X(30).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL PROCEDURES.
            DBG-BODY.
                MOVE DEBUG-NAME TO NM.
            END DECLARATIVES.
            MAIN SECTION.
            P1.
                SORT SF ASCENDING KEY SR
                    INPUT PROCEDURE IS FEED
                    OUTPUT PROCEDURE IS DRAIN.
                STOP RUN.
            FEED.
                CONTINUE.
            DRAIN.
                CONTINUE.
            """;
        var (ok, diags) = EditionHarness.Compile(src, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET1571");
    }
}
