// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The EC exception-condition model (ISO/IEC 1989:2023 §14.6.13 + §7.3.25 TURN + §14.9.29 RAISE + §14.9.33
/// RESUME + §14.9.49 Format-3 USE + §14.9.18/§14.9.14 RAISING + §15.28–15.33 EXCEPTION-* functions;
/// COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D9–D12): the per-GR conformance net. Every behavioral test is
/// SPEC-PINNED (the legacy oracle has NO EC model — COBOL-2002+; expected values derive from the cited rules).
/// All EC programs compile at <c>--std 2023</c>; the per-edition gating facts compile at 85 and assert the
/// targeted not-in-this-edition diagnostic (the four-compilers rule).
/// </summary>
public sealed class ExceptionConditionConformanceTests
{
    private static readonly CobolNetCompiler CobolNet = new(2023);

    /// <summary>Compile-and-run on the greenfield compiler at 2023; assert the spec-derived stdout.</summary>
    private static void AssertSpec(string source, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}\nstdout:\n{stdout}");
        Assert.Equal(expected, stdout);
    }

    /// <summary>Compile-and-run; assert ABNORMAL run-unit termination (nonzero exit — the §14.6.13.1.3 #5/#7
    /// fatal default / the settled §18.16 implementor choice) whose stderr names the exception condition.</summary>
    private static void AssertFatal(string source, string ecName, string? expectedStdout = null)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.False(ok, $"expected abnormal termination on {ecName}; ran clean with stdout:\n{stdout}");
        Assert.Contains(ecName, detail);
        if (expectedStdout is not null) Assert.Equal(expectedStdout, stdout);
    }

    /// <summary>A declaratives-bearing program skeleton (sections after END DECLARATIVES per §14.3).</summary>
    private static string Prog(string name, string turn, string env, string ws, string decls, string proc) => $"""
        {turn}
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {name}.
        {env}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        DECLARATIVES.
        {decls}
        END DECLARATIVES.
        MAIN SECTION.
        MAIN-PARA.
        {proc}
        """;

    // ── RAISE + TURN scoping (§14.9.29 / §7.3.25 / §14.6.13.1.1) ─────────────────────────────────────────────

    [Fact]   // §14.9.29 GR1 + §14.6.13.1.4: enabled nonfatal RAISE sets the last exception status and the F3
             // declarative handles it; RESUME AT NEXT STATEMENT continues past; the status persists after.
    public void Raise_Enabled_DeclarativeHandles_ResumeNextStatement_StatusPersists()
        => AssertSpec(Prog("ECT001", ">>TURN EC-USER-DEMO CHECKING ON", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-DEMO.
            EC-H-P.
                DISPLAY "HANDLER: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                RAISE EXCEPTION EC-USER-DEMO.
                DISPLAY "AFTER: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """), "HANDLER: EC-USER-DEMO\nAFTER: EC-USER-DEMO");

    [Fact]   // §14.6.13.1.1 (default OFF, §7.3.25.4 GR1) + §14.6.13.1.4 first sentence: with checking not
             // enabled a NONFATAL raise acts as if the exception did not occur — no status, execution continues.
    public void Raise_CheckingOff_Nonfatal_ActsAsContinue()
        => AssertSpec($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT002.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-DEMO.
                DISPLAY "S=[" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """, $"S=[{new string(' ', 31)}]");

    [Fact]   // §7.3.25.4 GR5/GR6: a TURN applies to SUCCEEDING statements until the next toggle — ON…OFF scopes
             // by source order within one paragraph.
    public void Turn_OffMidSource_ScopesBySourceOrder()
        => AssertSpec("""
            >>TURN EC-USER-A CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT003.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-A.
                DISPLAY "S1: " FUNCTION EXCEPTION-STATUS.
                SET LAST EXCEPTION TO OFF.
            >>TURN EC-USER-A CHECKING OFF
                RAISE EXCEPTION EC-USER-A.
                DISPLAY "S2: [" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """, $"S1: EC-USER-A\nS2: [{new string(' ', 31)}]");

    [Fact]   // §7.3.25.4 GR2: EC-ALL enables checking for every exception-name (here an EC-USER-* level-3).
    public void Turn_EcAll_CoversEveryName()
        => AssertSpec("""
            >>TURN EC-ALL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT004.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-X.
                DISPLAY "S: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """, "S: EC-USER-X");

    [Fact]   // §7.3.25.4 GR3: a level-2 name enables checking for its level-3 children.
    public void Turn_Level2_CoversItsChildren()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT005.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-Y.
                DISPLAY "S: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """, "S: EC-USER-Y");

    [Fact]   // §14.6.13.1.3 #8 (checking NOT enabled, fatal category): implementor-defined — this implementation
             // terminates the run unit loudly (the §1.4 doctrine; recorded in the deep-dive).
    public void Raise_Fatal_CheckingOff_TerminatesLoudly()
        => AssertFatal("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT006.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                RAISE EXCEPTION EC-SIZE-OVERFLOW.
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-SIZE-OVERFLOW", "BEFORE");

    [Fact]   // §14.6.13.1.3 #5 NOTE 2 + §14.9.33.4 GR2: a declarative's RESUME AT NEXT STATEMENT suppresses the
             // FATAL termination — execution continues after the RAISE.
    public void Raise_Fatal_DeclarativeResumes_SuppressesTermination()
        => AssertSpec(Prog("ECT007", ">>TURN EC-SIZE CHECKING ON", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW.
            EC-H-P.
                DISPLAY "HANDLER".
                RESUME AT NEXT STATEMENT.
            """, """
                RAISE EXCEPTION EC-SIZE-OVERFLOW.
                DISPLAY "SURVIVED".
                STOP RUN.
            """), "HANDLER\nSURVIVED");

    [Fact]   // §14.9.33.4 GR3: RESUME AT procedure-name transfers control as if a GO TO.
    public void Resume_AtProcedureName_TransfersControl()
        => AssertSpec(Prog("ECT008", ">>TURN EC-USER CHECKING ON", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-J.
            EC-H-P.
                RESUME AT ELSEWHERE.
            """, """
                RAISE EXCEPTION EC-USER-J.
                DISPLAY "SKIPPED".
                STOP RUN.
            ELSEWHERE.
                DISPLAY "RESUMED-AT".
                STOP RUN.
            """), "RESUMED-AT");

    [Fact]   // §14.9.49.4 GR3e before GR3g: the level-3 entry is selected over the EC-ALL entry regardless of
             // source order; an unmatched name falls to the EC-ALL tier.
    public void UseF3_SelectionTiers_Level3BeatsEcAll()
        => AssertSpec(Prog("ECT009", ">>TURN EC-USER CHECKING ON", "", "", """
            ALL-H SECTION. USE AFTER EXCEPTION CONDITION EC-ALL.
            ALL-H-P.
                DISPLAY "ALL-HANDLER".
                RESUME AT NEXT STATEMENT.
            SPEC-H SECTION. USE AFTER EC EC-USER-T.
            SPEC-H-P.
                DISPLAY "SPECIFIC-HANDLER".
                RESUME AT NEXT STATEMENT.
            """, """
                RAISE EXCEPTION EC-USER-T.
                RAISE EXCEPTION EC-USER-OTHER.
                DISPLAY "DONE".
                STOP RUN.
            """), "SPECIFIC-HANDLER\nALL-HANDLER\nDONE");

    [Fact]   // §14.9.49.3 SR1 + §14.4.2: the use procedure is the WHOLE remainder of the declarative section, so
             // this tail-shaped one runs H1 AND H2 — a handler end derived from paragraph SHAPE stopped at D-EX and
             // printed H1 alone (kb/Work PB367). §14.9.14.4 GR1 makes the bare EXIT a no-op and GR7 sends D-LAST's
             // EXIT SECTION to the return mechanism after the section's LAST paragraph, so D-TAIL never runs — which
             // is why the run does NOT stop there. It ends instead through §14.9.49.4 GR13 b): EC-BOUND-SUBSCRIPT is
             // fatal (Table 13), the declarative completed normally, and §14.6.13.1.3 #5 terminates the run unit
             // abnormally. Both halves are load-bearing: the stdout pins the truncation, the fatality pins GR13b.
    public void UseF3_UseProcedureIsTheWholeSection_ThenGr13bTerminates()
        => AssertFatal(Prog("ECT055", ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON", "", """
            01 G.
               05 T PIC 9 OCCURS 3 TIMES.
            01 IDX PIC 9 VALUE 5.
            01 R PIC 9 VALUE 0.
            """, """
            D SECTION. USE AFTER EC EC-BOUND-SUBSCRIPT.
            D-P1.
                DISPLAY "H1".
            D-EX.
                EXIT.
            D-P2.
                DISPLAY "H2".
            D-LAST.
                EXIT SECTION.
            D-TAIL.
                DISPLAY "TAIL-MUST-NOT-RUN".
                STOP RUN.
            """, """
                MOVE T (IDX) TO R.
                DISPLAY "MUST-NOT-REACH".
                STOP RUN.
            """), "EC-BOUND-SUBSCRIPT", "H1\nH2");

    // ── The EXCEPTION-* functions + WITH LOCATION (§15.28–15.33 / §7.3.25.4 GR7) ─────────────────────────────

    [Fact]   // §15.32.3 r2 + §15.30.3 r2: WITH LOCATION captures the statement name and the three-part location.
    public void WithLocation_CapturesStatementAndLocation()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun("""
            >>TURN EC-USER CHECKING ON WITH LOCATION
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT010.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-L.
                DISPLAY "STMT: " FUNCTION EXCEPTION-STATEMENT.
                DISPLAY "LOC: " FUNCTION EXCEPTION-LOCATION.
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Contains("STMT: RAISE", stdout);
        Assert.Contains("LOC: ECT010; MAIN-PARA;", stdout);   // "element; paragraph; line-id" (§15.30.3 r2)
    }

    [Fact]   // §15.32.3 r3: the recorded name comes from Table 12's 'Statement name' column — "GO TO", never the
             // spelled leading token. A subscripted DEPENDING operand raises EC-BOUND-SUBSCRIPT INSIDE the GO TO
             // (§8.4.2.3.4 GR2); the F3 declarative reads the name and RESUMEs past (kb/Work R04).
    public void ExceptionStatement_GoTo_ReturnsTheTable12Name()
        => AssertSpec(Prog("ECT014", ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON WITH LOCATION", "", """
            01 WS-TAB.
               05 WS-CELL PIC 9 OCCURS 3 TIMES VALUE 1.
            01 WS-I PIC 9 VALUE 5.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
            EC-H-P.
                DISPLAY "STMT=[" FUNCTION EXCEPTION-STATEMENT "]".
                RESUME AT NEXT STATEMENT.
            """, """
                GO TO TGT-A TGT-B DEPENDING ON WS-CELL(WS-I).
                DISPLAY "PAST".
                STOP RUN.
            TGT-A.
            TGT-B.
                DISPLAY "WRONG-TARGET".
                STOP RUN.
            """), $"STMT=[{"GO TO",-63}]\nPAST");

    [Fact]   // §15.32.3 r3 + the optional word TO (goToStatement : GO TO? …): `GO PARA.` is the SAME statement
             // kind with no TO token anywhere — the name must still be "GO TO". This spelling is the one that
             // defeats any token-derived name (kb/Work R04: first-token gave "GO"; longest-token-match also
             // gives "GO" because the tokens never spell TO).
    public void ExceptionStatement_GoWithoutTo_StillReturnsGoTo()
        => AssertSpec(Prog("ECT015", ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON WITH LOCATION", "", """
            01 WS-TAB.
               05 WS-CELL PIC 9 OCCURS 3 TIMES VALUE 1.
            01 WS-I PIC 9 VALUE 5.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
            EC-H-P.
                DISPLAY "STMT=[" FUNCTION EXCEPTION-STATEMENT "]".
                RESUME AT NEXT STATEMENT.
            """, """
                GO TGT-A TGT-B DEPENDING ON WS-CELL(WS-I).
                DISPLAY "PAST".
                STOP RUN.
            TGT-A.
            TGT-B.
                DISPLAY "WRONG-TARGET".
                STOP RUN.
            """), $"STMT=[{"GO TO",-63}]\nPAST");

    [Fact]   // §15.32.3 r1 / §15.30.3 r1: WITHOUT the LOCATION phrase no location information is saved —
             // EXCEPTION-STATEMENT is all spaces, EXCEPTION-LOCATION a single space.
    public void WithoutLocation_StatementAndLocationAreSpaces()
        => AssertSpec($"""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT011.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-L.
                DISPLAY "T=[" FUNCTION EXCEPTION-STATEMENT "]".
                DISPLAY "L=[" FUNCTION EXCEPTION-LOCATION "]".
                STOP RUN.
            """, $"T=[{new string(' ', 63)}]\nL=[ ]");

    [Fact]   // §14.9.39 Format 13: SET LAST EXCEPTION TO OFF — the status indicates no exception condition.
    public void SetLastExceptionToOff_ClearsTheStatus()
        => AssertSpec($"""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT012.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-C.
                SET LAST EXCEPTION TO OFF.
                DISPLAY "S=[" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """, $"S=[{new string(' ', 31)}]");

    // ── The EC-SIZE bridge (§14.7.5 ↔ Table 13; arithmetic without the phrase) ───────────────────────────────

    [Fact]   // §14.7.5 case 2 + Table 13: a zero divisor under enabled EC-SIZE checking raises the PRECISE
             // level-3 name EC-SIZE-ZERO-DIVIDE; the receiver is unchanged; RESUME AT NEXT STATEMENT continues.
    public void SizeError_ZeroDivide_DeclarativeSeesPreciseName_ReceiverUnchanged()
        => AssertSpec(Prog("ECT013", ">>TURN EC-SIZE CHECKING ON", "", """
            01 WS-A PIC 9(3) VALUE 5.
            01 WS-B PIC 9(3) VALUE 0.
            01 WS-C PIC 9(3) VALUE 111.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                DIVIDE WS-A BY WS-B GIVING WS-C.
                DISPLAY "C=" WS-C.
                STOP RUN.
            """), "H: EC-SIZE-ZERO-DIVIDE\nC=111");

    [Fact]   // Table 13 "EC-SIZE-TRUNCATION — significant digits truncated in store": a store overflow under
             // enabled checking latches the truncation name; the receiver keeps its prior value (§14.7.5).
    public void SizeError_StoreTruncation_LatchesTruncationName()
        => AssertSpec(Prog("ECT014", ">>TURN EC-SIZE CHECKING ON", "", """
            01 WS-A PIC 9(3) VALUE 900.
            01 WS-B PIC 9(3) VALUE 200.
            01 WS-C PIC 9(3) VALUE 111.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                ADD WS-A WS-B GIVING WS-C.
                DISPLAY "C=" WS-C.
                STOP RUN.
            """), "H: EC-SIZE-TRUNCATION\nC=111");

    [Fact]   // §14.6.13.1.3 #5/#7: enabled EC-SIZE, no phrase, no qualifying declarative — the run unit
             // terminates abnormally on the fatal condition.
    public void SizeError_NoPhraseNoDeclarative_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT015.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(3) VALUE 5.
            01 WS-B PIC 9(3) VALUE 0.
            01 WS-C PIC 9(3) VALUE 111.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                DIVIDE WS-A BY WS-B GIVING WS-C.
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-SIZE-ZERO-DIVIDE", "BEFORE");

    [Fact]   // §14.6.13.1.3 #1 / §14.6.13.1.4 #1: the statement's own ON SIZE ERROR phrase handles the
             // condition — the declarative does NOT run; the last exception status IS set (checking enabled).
    public void SizeError_OnSizeErrorPhrase_WinsOverDeclarative()
        => AssertSpec(Prog("ECT016", ">>TURN EC-SIZE CHECKING ON", "", """
            01 WS-A PIC 9(3) VALUE 900.
            01 WS-B PIC 9(3) VALUE 200.
            01 WS-C PIC 9(3) VALUE 111.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE.
            EC-H-P.
                DISPLAY "DECL".
                RESUME AT NEXT STATEMENT.
            """, """
                ADD WS-A WS-B GIVING WS-C
                    ON SIZE ERROR DISPLAY "PHRASE"
                END-ADD.
                DISPLAY "S: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """), "PHRASE\nS: EC-SIZE-TRUNCATION");

    // ── The EC-OVERFLOW bridge (STRING §14.9.43 GR8b / UNSTRING §14.9.48 GR16b) ──────────────────────────────

    [Fact]   // §14.9.43 GR8b: a STRING overflow without the ON OVERFLOW phrase raises EC-OVERFLOW-STRING
             // (nonfatal — execution continues either way, §14.6.13.1.4 #3/#4); the receiver holds the
             // transferred prefix.
    public void StringOverflow_NoPhrase_RaisesEcOverflowString()
        => AssertSpec(Prog("ECT017", ">>TURN EC-OVERFLOW CHECKING ON", "", """
            01 WS-R PIC X(3) VALUE SPACES.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-OVERFLOW.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
            """, """
                STRING "ABCDE" DELIMITED BY SIZE INTO WS-R.
                DISPLAY "R=" WS-R.
                STOP RUN.
            """), "H: EC-OVERFLOW-STRING\nR=ABC");

    // ── The EC-I-O bridge (§9.1.13.1 status → EC) ────────────────────────────────────────────────────────────

    private const string IoEnv = """
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT TF ASSIGN TO "EC-TF".
        """;

    private const string IoFd = """
        FD TF.
        01 TF-REC PIC X(10).
        """;

    /// <summary>An I-O program skeleton: declaratives + FILE SECTION (the Prog skeleton has no file slot).</summary>
    private static string IoProg(string name, string turn, string decls, string proc) => $"""
        {turn}
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {name}.
        {IoEnv}
        DATA DIVISION.
        FILE SECTION.
        {IoFd}
        PROCEDURE DIVISION.
        DECLARATIVES.
        {decls}
        END DECLARATIVES.
        MAIN SECTION.
        MAIN-PARA.
            OPEN OUTPUT TF.
            MOVE "HELLO" TO TF-REC.
            WRITE TF-REC.
            CLOSE TF.
            OPEN INPUT TF.
        {proc}
        """;

    [Fact]   // §9.1.13.1: I-O status 10 with enabled EC-I-O checking raises EC-I-O-AT-END (nonfatal '1x'); the
             // F3 declarative selects it; §15.28.4 r1c — EXCEPTION-FILE is the status + the SELECT file-name.
    public void IoAtEnd_NoPhrase_RaisesEcIoAtEnd_ExceptionFile()
        => AssertSpec(IoProg("ECT018", ">>TURN EC-I-O CHECKING ON", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                DISPLAY "F: " FUNCTION EXCEPTION-FILE.
                RESUME AT NEXT STATEMENT.
            """, """
                READ TF.
                READ TF.
                DISPLAY "AFTER-READS".
                CLOSE TF.
                STOP RUN.
            """), "H: EC-I-O-AT-END\nF: 10TF\nAFTER-READS");

    [Fact]   // §15.29.4 r1 — EXCEPTION-FILE-N, the national twin of EXCEPTION-FILE (P10 Step-11 EC-N wave):
             // r1a two national zeros before any EC-I-O condition; r1c the I-O status in national characters +
             // the SELECT file-name converted to the runtime national character set (the ONE NationalOf
             // repertoire projection of the same File() rendering). §15.31.3 r1 — EXCEPTION-LOCATION-N is one
             // national space when the enabling TURN lacked WITH LOCATION. FUNCTION LENGTH (§15.50) pins the
             // character-position counts of the national results.
    public void IoAtEnd_ExceptionFileN_NationalTwin()
        => AssertSpec($"""
            >>TURN EC-I-O CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT018N.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            WORKING-STORAGE SECTION.
            01 WS-L1 PIC 9.
            01 WS-L2 PIC 9.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END.
            EC-H-P.
                DISPLAY "FN: " FUNCTION EXCEPTION-FILE-N.
                MOVE FUNCTION LENGTH(FUNCTION EXCEPTION-FILE-N) TO WS-L1.
                MOVE FUNCTION LENGTH(FUNCTION EXCEPTION-LOCATION-N) TO WS-L2.
                DISPLAY "L1: " WS-L1 " L2: " WS-L2.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                OPEN OUTPUT TF.
                MOVE "HELLO" TO TF-REC.
                WRITE TF-REC.
                CLOSE TF.
                OPEN INPUT TF.
                DISPLAY "P: " FUNCTION EXCEPTION-FILE-N.
                READ TF.
                READ TF.
                CLOSE TF.
                STOP RUN.
            """, "P: 00\nFN: 10TF\nL1: 4 L2: 1");

    [Fact]   // §9.1.13.1 / §14.6.13.1.4 #1: the statement's AT END phrase covers the '1x' family — the EC
             // declarative is NOT selected.
    public void IoAtEnd_AtEndPhrase_SuppressesDeclarative()
        => AssertSpec(IoProg("ECT019", ">>TURN EC-I-O CHECKING ON", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END.
            EC-H-P.
                DISPLAY "DECL".
            """, """
                READ TF.
                READ TF AT END DISPLAY "ATEND".
                CLOSE TF.
                STOP RUN.
            """), "ATEND");

    [Fact]   // §9.1.13.1 fatal classes ('3x') + §14.9.49.4 GR12c: an enabled fatal I-O status with no handler
             // terminates the run unit abnormally (checking OFF keeps the historical continue-on-error).
    public void IoPermanentError_Enabled_NoHandler_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-I-O CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT020.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT TF ASSIGN TO "EC-NO-SUCH-FILE".
            DATA DIVISION.
            FILE SECTION.
            FD TF.
            01 TF-REC PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                OPEN INPUT TF.
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-I-O-PERMANENT-ERROR", "BEFORE");

    // ── The EC-PROGRAM bridge (CALL/CANCEL, §14.9.4.4 GR3b/GR3f) ─────────────────────────────────────────────

    [Fact]   // §14.9.4.4 GR3b: CALL of an unknown program under enabled EC-PROGRAM checking raises
             // EC-PROGRAM-NOT-FOUND; the declarative's RESUME AT NEXT STATEMENT suppresses the fatal default.
    public void CallNotFound_Enabled_DeclarativeResumes()
        => AssertSpec(Prog("ECT021", ">>TURN EC-PROGRAM CHECKING ON", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                CALL "NOSUCHPROG".
                DISPLAY "SURVIVED".
                STOP RUN.
            """), "H: EC-PROGRAM-NOT-FOUND\nSURVIVED");

    [Fact]   // §14.6.13.1.3 #5/#7: enabled EC-PROGRAM, no phrase, no declarative — fatal termination (every
             // EC-PROGRAM-* is fatal, Table 13).
    public void CallNotFound_Enabled_NoHandler_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-PROGRAM CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT022.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                CALL "NOSUCHPROG".
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-PROGRAM-NOT-FOUND", "BEFORE");

    [Fact]   // §14.9.4.4 GR3b names a condition DISTINCT from EC-PROGRAM-NOT-FOUND for the NULL case: "If the data
             // item referenced by identifier-1 contains the predefined address NULL, the EC-PROGRAM-PTR-NULL
             // exception condition is set to exist." (GR3g's "invalid program address … undefined" is the NON-null
             // bad-address case.) Fatal per Table 13, so with no handler the run unit terminates — the arm the
             // corpus golden ec_program_ptr_null cannot express, since it must run clean and RESUMEs instead.
    public void CallPointerNull_Enabled_NoHandler_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-PROGRAM-PTR-NULL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT022P.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PPTR USAGE PROGRAM-POINTER.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                SET PPTR TO NULL.
                CALL PPTR.
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-PROGRAM-PTR-NULL", "BEFORE");

    [Fact]   // The FUNCTION-POINTER twin (kb/Work PB847). §8.4.3.2.4 GR6c: "If function-pointer-name-1 is NULL, the
             // EC-FUNCTION-PTR-NULL exception condition is set to exist, no function is activated" — Fatal per
             // Table 13, so with checking on and no declarative the run unit terminates and the statement carrying
             // the function-identifier never completes. The declarative arm is the corpus golden
             // 2014/pb847_function_pointer_call.
    public void FunctionPointerNull_Enabled_NoHandler_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-FUNCTION-PTR-NULL CHECKING ON
            IDENTIFICATION DIVISION.
            FUNCTION-ID. ECT847F.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-ARG PIC S9(4).
            01 L-RES PIC S9(9).
            PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
                COMPUTE L-RES = L-ARG * 2
                GOBACK.
            END FUNCTION ECT847F.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT847.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION ECT847F.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FP USAGE FUNCTION-POINTER TO ECT847F.
            01 R PIC 9(9).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                SET FP TO NULL.
                COMPUTE R = FP(5).
                DISPLAY "NEVER".
                STOP RUN.
            END PROGRAM ECT847.
            """, "EC-FUNCTION-PTR-NULL", "BEFORE");

    // ── §14.9.4.4 GR3h's three-way partition and GR3i's "the phrase is ignored" (kb/Work PB233) ──────────────
    // GR3h routes a FAILED ACTIVATION three ways, and each arm below pins one of them. The three facts the arms
    // turn on are exactly the three the emitted catch used to conflate: WHICH phrase is written (only ON
    // EXCEPTION diverts), WHICH family the condition belongs to (only EC-PROGRAM/EC-EXTERNAL reach
    // imperative-statement-1), and WHETHER control had already been transferred (GR3i).

    [Fact]   // §14.9.4.4 GR3h item 1 admits imperative-statement-1 only when "an ON EXCEPTION phrase is
             // specified in the CALL statement", and §14.6.13.1.3 #1 says the same from the other side — only
             // "a conditional phrase without the NOT phrase" processes a fatal condition. So a CALL carrying
             // ONLY NOT ON EXCEPTION is governed by item 2 or item 3 exactly as a phrase-free CALL is; checking
             // is off here, so item 3 → §14.6.13.1.3 #8, this implementation's abnormal termination. Before
             // PB233 the mere presence of the NOT phrase emitted an unconditional catch and the program ran to
             // completion with the failed activation discarded.
    public void CallNotOnExceptionOnly_ActivationFails_TerminatesLikeAPhraselessCall()
        => AssertFatal("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT233A.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                CALL "NOSUCHPROG"
                    NOT ON EXCEPTION DISPLAY "NEVER"
                END-CALL.
                DISPLAY "ALSO-NEVER".
                STOP RUN.
            """, "EC-PROGRAM-NOT-FOUND", "BEFORE");

    [Fact]   // §14.9.4.4 GR3h item 2, FIRST disjunct: checking enabled, an EC-PROGRAM condition, "and an ON
             // EXCEPTION phrase is not specified" — the applicable exception processing statements run. "If
             // control is returned from these statements, control is then transferred to the end of the CALL
             // statement", so the NOT ON EXCEPTION phrase does NOT run: GR3i gives imperative-statement-2 only
             // a program that "was successfully called". Before PB233 the NOT phrase captured the failure and
             // the declarative never ran at all.
    public void CallNotOnExceptionOnly_Enabled_RunsTheDeclarativeAndSkipsTheNotPhrase()
        => AssertSpec(Prog("ECT233B", ">>TURN EC-PROGRAM CHECKING ON", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                CALL "NOSUCHPROG"
                    NOT ON EXCEPTION DISPLAY "NEVER"
                END-CALL.
                DISPLAY "SURVIVED".
                STOP RUN.
            """), "H: EC-PROGRAM-NOT-FOUND\nSURVIVED");

    [Fact]   // §14.9.4.4 GR3i: "If the program was successfully called, after control is returned from the
             // called program the ON EXCEPTION phrase, if specified, is ignored." ECT233CM IS successfully
             // called — it displays before it fails — so the outer CALL's phrase is not a party to its inner
             // CALL's failure, which (no phrase, checking off) takes GR3h item 3 → §14.6.13.1.3 #8. Before
             // PB233 the outer catch spanned the whole activation, so ANY unhandled failure inside a callee
             // ran the ACTIVATOR's imperative-statement-1 and the run unit continued as if nothing was wrong.
    public void CallOnException_IsIgnoredWhenTheFailureIsTheCalleesOwn()
        => AssertFatal("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT233C.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "MAIN-BEFORE".
                CALL "ECT233CM"
                    ON EXCEPTION DISPLAY "NEVER"
                END-CALL.
                DISPLAY "ALSO-NEVER".
                STOP RUN.
            END PROGRAM ECT233C.

            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT233CM.
            PROCEDURE DIVISION.
            MID-PARA.
                DISPLAY "MID-BEFORE".
                CALL "NOSUCHPROG".
                DISPLAY "MID-NEVER".
                GOBACK.
            END PROGRAM ECT233CM.
            """, "EC-PROGRAM-NOT-FOUND", "MAIN-BEFORE\nMID-BEFORE");

    [Fact]   // §14.9.4.4 GR3h item 1's FAMILY restriction: imperative-statement-1 takes "any of the EC-PROGRAM
             // or EC-EXTERNAL exception conditions" and nothing else. EC-FUNCTION-NOT-FOUND (§8.4.3.2.4 GR6b —
             // a user-defined-function locate miss) is neither, so with checking off it takes GR3h item 3 →
             // §14.6.13.1.3 #8. The unfiltered catch used to route it into the phrase.
    public void CallOnException_IsNotEnteredByFunctionNotFound()
        => AssertFatal("""
            IDENTIFICATION DIVISION.
            FUNCTION-ID. ECT233FN IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-A PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-A RETURNING L-R.
            END FUNCTION ECT233FN.

            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT233D.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION ECT233FN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-X PIC 9(4) VALUE 5.
            01 W-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "MAIN-BEFORE".
                CALL "ECT233DM"
                    ON EXCEPTION DISPLAY "NEVER"
                END-CALL.
                DISPLAY "ALSO-NEVER".
                STOP RUN.

            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT233DM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 M-X PIC 9(4) VALUE 5.
            01 M-R PIC 9(4).
            PROCEDURE DIVISION.
            MID-PARA.
                DISPLAY "MID-BEFORE".
                MOVE FUNCTION ECT233FN(M-X) TO M-R.
                DISPLAY "MID-NEVER".
                GOBACK.
            END PROGRAM ECT233DM.
            END PROGRAM ECT233D.
            """, "EC-FUNCTION-NOT-FOUND", "MAIN-BEFORE\nMID-BEFORE");

    [Fact]   // §14.9.4.4 GR3h item 2's SECOND disjunct — "or if the exception condition is not one of the
             // EC-PROGRAM exception conditions, any applicable exception processing statements are executed" —
             // and the general §14.6.13.1.3 #5 that stands behind it. EC-FUNCTION-NOT-FOUND had NO handling arm
             // of any kind before PB233: the CALL emitter's enabled-name filter selected only the
             // EC-PROGRAM/EC-EXTERNAL families, so an enabled EC-FUNCTION-NOT-FOUND could reach no declarative.
             // RESUME AT NEXT STATEMENT suppresses the fatal default, exactly as it does for an EC-PROGRAM name.
    public void FunctionNotFound_Enabled_RunsTheDeclarative()
        => AssertSpec("""
            >>TURN EC-FUNCTION-NOT-FOUND CHECKING ON
            IDENTIFICATION DIVISION.
            FUNCTION-ID. ECT233GN IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-A PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-A RETURNING L-R.
            END FUNCTION ECT233GN.

            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT233E.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION ECT233GN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-X PIC 9(4) VALUE 5.
            01 W-R PIC 9(4).
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-FUNCTION-NOT-FOUND.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                MOVE FUNCTION ECT233GN(W-X) TO W-R.
                DISPLAY "SURVIVED".
                STOP RUN.
            """, "H: EC-FUNCTION-NOT-FOUND\nSURVIVED");

    [Fact]   // §14.9.18.4 GR1b is CONDITIONAL: "If the RAISING phrase is specified, an exception condition is
             // raised in the activating runtime element IF CHECKING FOR THAT EXCEPTION CONDITION IS ENABLED in
             // the activating runtime element". V58MAIN enables none, so the callee's GOBACK RAISING raises
             // NOTHING in it and execution continues after the CALL. GR3 agrees for the main-program half: a
             // GOBACK with no activator "operates as if executing a STOP statement … A RAISING phrase, if
             // specified, is ignored."
             //
             // ⚠ SEPARATELY COMPILED, which exercises the arm where the activator is in another ASSEMBLY — but
             // it is NOT "the only way this rule is reachable", as this comment used to claim. §7.3.25.4 GR6/GR8
             // scope a directive to the statements "that follow in the compilation group", so two elements of ONE
             // file can sit in different checking states; the PB408 tests below measure exactly that, in both
             // directions, and it was the axis this test holds fixed (feedback_reachability_is_measured_not_deduced).
             //
             // Two defects were fixed on this path. ProgramTable's boundary default (removed by PB892) used to throw for a
             // staged FATAL condition citing §14.6.13.1.3 #8 — a misapplication, since #8's latitude governs what
             // may happen once a fatal condition EXISTS and GR1b stops it existing in an unchecked activator at
             // all. The staging side then made the same mistake one element further in (kb/Work PB408).
    public void GobackRaising_IntoUncheckedActivator_IsNotRaisedThere()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunWith("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V58MAIN.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "V58SUB".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            """, """
            >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V58SUB.
            PROCEDURE DIVISION.
            SUB-P.
                DISPLAY "IN-SUB".
                GOBACK RAISING EXCEPTION EC-BOUND-SUBSCRIPT.
            """);
        Assert.True(ok, $"the unchecked activator should continue past the CALL: {detail}" + stdout);
        Assert.Equal("IN-SUB\nAFTER-CALL", stdout);
    }

    [Fact]   // §14.9.23.4 GR7c raises EC-OO-UNIVERSAL only "if checking for it is enabled in BOTH the activated
             // method and the activating runtime element". Here the >>TURN is switched OFF before the CLASS, so
             // only the ACTIVATOR has it enabled — the condition is therefore NOT set, the declarative must not
             // select, and NOTHING may be attributed to EC-OO-UNIVERSAL.
             //
             // The nonconforming crossing still cannot proceed (a PIC 9(4) returning item delivered into a PIC 9(6)
             // one: §9.3.6 match rule 7 BINDS the method — a MOVE could deliver it — and §14.8.3.3's same-PICTURE
             // rule is then GR7c's, which only a universal receiver defers to run time; kb/Work PB1500 — an ARGUMENT
             // mismatch resolves no method at all and is EC-OO-METHOD), so it stops as a CobolImplementorFatalException
             // — §14.6.13.1.1 NOTE 3 latitude, carrying NO exception-name. The assertion that the message does
             // NOT contain the EC name is the whole point of the test: attributing it would be the bug.
             // Its twin, where checking IS enabled in both, is the corpus golden ec_oo_universal_both.
    public void OoUniversal_EnabledInActivatorOnly_StopsWithoutAttributingTheCondition()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun("""
            >>TURN EC-OO-UNIVERSAL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V55NOTB.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CUNIVN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE.
            01 C USAGE OBJECT REFERENCE CUNIVN.
            01 W PIC 9(6) VALUE 000007.
            PROCEDURE DIVISION.
            DECLARATIVES.
            H SECTION.
                USE AFTER EXCEPTION CONDITION EC-OO-UNIVERSAL.
            H-P.
                DISPLAY "HANDLED".
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                INVOKE CUNIVN "NEW" RETURNING C.
                SET O TO C.
                INVOKE O "TAKE" RETURNING W.
                DISPLAY "AFTER".
                STOP RUN.
            END PROGRAM V55NOTB.

            >>TURN EC-OO-UNIVERSAL CHECKING OFF
            IDENTIFICATION DIVISION.
            CLASS-ID. CUNIVN INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. TAKE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK PIC 9(4).
            PROCEDURE DIVISION RETURNING LK.
            MAIN-P.
                DISPLAY "IN-TAKE".
            END METHOD TAKE.
            END OBJECT.
            END CLASS CUNIVN.
            """);
        Assert.False(ok, $"the nonconforming universal crossing must not proceed; stdout:\n{stdout}");
        Assert.DoesNotContain("EC-OO-UNIVERSAL", detail);
        Assert.DoesNotContain("HANDLED", stdout);
        Assert.DoesNotContain("AFTER", stdout);
    }

    [Fact]   // §14.9.4.4 GR3h + §14.6.13.1.4 #1: the CALL's ON EXCEPTION phrase handles the condition — no
             // termination; the last exception status IS set (checking enabled).
    public void CallNotFound_OnExceptionPhrase_WinsAndStatusSet()
        => AssertSpec("""
            >>TURN EC-PROGRAM CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT023.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "NOSUCHPROG"
                    ON EXCEPTION DISPLAY "PHRASE"
                END-CALL.
                DISPLAY "S: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """, "PHRASE\nS: EC-PROGRAM-NOT-FOUND");

    // ── The EC-ARGUMENT-FUNCTION bridge (§15.3 / Table 13) ───────────────────────────────────────────────────

    [Fact]   // §15.3: with checking DISABLED an intrinsic argument violation yields the default result 0.
    public void ArgumentFunction_CheckingOff_DefaultResultZero()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT024.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC 9(3) VALUE 999.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-X = FUNCTION LOG(0).
                DISPLAY "X=" WS-X.
                STOP RUN.
            """, "X=000");

    [Fact]   // Table 13 (EC-ARGUMENT-FUNCTION, fatal): with checking ENABLED the same violation raises and —
             // with no handler — terminates the run unit abnormally.
    public void ArgumentFunction_Enabled_NoHandler_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT025.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC 9(3) VALUE 999.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                COMPUTE WS-X = FUNCTION LOG(0).
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-ARGUMENT-FUNCTION", "BEFORE");

    [Fact]   // §14.6.13.1.3 #5 NOTE 2: the declarative's RESUME AT NEXT STATEMENT suppresses the fatal
             // termination; the aborted statement's receiver is unchanged.
    public void ArgumentFunction_Enabled_DeclarativeResumes_ReceiverUnchanged()
        => AssertSpec(Prog("ECT026", ">>TURN EC-ARGUMENT-FUNCTION CHECKING ON", "", """
            01 WS-X PIC 9(3) VALUE 999.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                COMPUTE WS-X = FUNCTION NUMVAL("12X34").
                DISPLAY "X=" WS-X.
                STOP RUN.
            """), "H: EC-ARGUMENT-FUNCTION\nX=999");

    // ── RAISING propagation (GOBACK / EXIT PROGRAM, §14.9.18 / §14.9.14) ─────────────────────────────────────

    [Fact]   // §14.9.18 GR: GOBACK RAISING stages the condition for the ACTIVATOR — raised at the end of the
             // CALL; the caller's F3 declarative selects it and RESUME continues.
    public void GobackRaising_PropagatesToActivator_DeclarativeHandles()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT027.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-PROP.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT027S".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT027.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT027S.
            PROCEDURE DIVISION RAISING EC-USER-PROP.
            SUB-PARA.
                GOBACK RAISING EXCEPTION EC-USER-PROP.
            """, "CAUGHT: EC-USER-PROP\nAFTER-CALL");

    [Fact]   // §14.9.18.4 GR1b3a first sentence: "If an exception condition is currently raised, that exception
             // condition is set to exist in the activating runtime element." The callee's PD header DOES specify
             // EC-USER-Q, so no substitution applies and the activator's matching declarative selects.
             //
             // ⚠ THE FIXTURE MOVED, TWICE OVER NON-CONFORMING BEFORE kb/Work PB408. (a) The GOBACK sat in an
             // ordinary paragraph, which §14.9.18.3 SR5 forbids for the LAST phrase ("The LAST phrase may be
             // specified only in a declarative procedure or WHEN phrase of a PERFORM statement") — it is now in
             // the callee's own declarative. (b) The PD header carried no RAISING phrase, which is GR1b3a's
             // SUBSTITUTION case, not this one; that half is now its own test below. The old fixture asserted the
             // substituted answer never happened, so a GREEN test held the gap open.
    public void GobackRaisingLastException_PropagatesTheLastStatus()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT028.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Q.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT028S".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT028.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT028S.
            PROCEDURE DIVISION RAISING EC-USER-Q.
            DECLARATIVES.
            SUB-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Q.
            SUB-H-P.
                GOBACK RAISING LAST EXCEPTION.
            END DECLARATIVES.
            SUB SECTION.
            SUB-PARA.
                RAISE EXCEPTION EC-USER-Q.
                DISPLAY "NEVER".
            """, "CAUGHT: EC-USER-Q\nAFTER-CALL");

    // ── §14.9.18.4 GR1 b): THE ENABLEMENT TEST IS THE ACTIVATOR'S (kb/Work PB408) ───────────────────────
    // GR1 b) names ONE runtime element twice: "an exception condition is raised in the activating runtime
    // element if checking for that exception condition is enabled in the activating runtime element". The
    // compiler used to fold the CALLEE's own >>TURN state into the RAISING phrase at bind time and act on that,
    // so the answer was a different element's. Each pair below flips the axis the old tests held fixed — they
    // all had checking enabled in BOTH elements, the one configuration in which the locus is invisible.
    //
    // The two elements sit in ONE compilation group: §7.3.25.4 GR6 enables checking "for the procedure division
    // statements and procedure division headers that follow in the compilation group" and GR8 disables it the
    // same way, so a directive written between the elements separates their states.

    [Fact]   // ACTIVATOR ON, CALLEE OFF → raised HERE: the activator's declarative selects (§14.6.13.1.4 #3).
             // Measured WRONG before the fix: "IN-SUB / AFTER-CALL" — the declarative never ran.
    public void GobackRaising_ActivatorEnabled_CalleeDisabled_RaisesInTheActivator()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408A.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Z.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT408AS".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT408A.
            >>TURN EC-USER CHECKING OFF
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408AS.
            PROCEDURE DIVISION RAISING EC-USER-Z.
            SUB-PARA.
                DISPLAY "IN-SUB".
                GOBACK RAISING EXCEPTION EC-USER-Z.
            """, "IN-SUB\nCAUGHT: EC-USER-Z\nAFTER-CALL");

    [Fact]   // The MIRROR — ACTIVATOR OFF, CALLEE ON → not raised here at all (§14.6.13.1.1: "if checking for an
             // exception that occurs is not enabled, no exception condition is raised"), so the activator's
             // declarative must NOT run even though it names the condition. Measured WRONG before the fix: it ran.
    public void GobackRaising_ActivatorDisabled_CalleeEnabled_IsNotRaised()
        => AssertSpec("""
            >>TURN EC-USER CHECKING OFF
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408B.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Z.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT408BS".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT408B.
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408BS.
            PROCEDURE DIVISION RAISING EC-USER-Z.
            SUB-PARA.
                DISPLAY "IN-SUB".
                GOBACK RAISING EXCEPTION EC-USER-Z.
            """, "IN-SUB\nAFTER-CALL");

    [Fact]   // The FATAL arm of the same pair. ACTIVATOR ON, CALLEE OFF: the condition comes into existence in
             // the ACTIVATOR, so its declarative runs (§14.6.13.1.3 #5) and RESUME AT NEXT STATEMENT continues.
             // Before the fix the callee threw a CobolFatalException at the GOBACK citing §14.6.13.1.3 #8, so the
             // run unit died inside the element that had checking turned OFF.
    public void GobackRaising_Fatal_ActivatorEnabled_CalleeDisabled_RunsTheActivatorsDeclarative()
        => AssertSpec("""
            >>TURN EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408C.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT408CS".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT408C.
            >>TURN EC-SIZE CHECKING OFF
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408CS.
            PROCEDURE DIVISION.
            SUB-PARA.
                DISPLAY "IN-SUB".
                GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW.
            """, "IN-SUB\nCAUGHT: EC-SIZE-OVERFLOW\nAFTER-CALL");

    [Fact]   // Its mirror, and the sharpest one: a FATAL condition the callee has enabled and the activator has
             // not. GR1b stops it coming into existence in the activator at all, so §14.6.13.1.3 — whose every
             // rule is conditioned on checking being enabled — has nothing to dispose of and execution simply
             // continues after the CALL. Before the fix the run unit terminated abnormally.
    public void GobackRaising_Fatal_ActivatorDisabled_CalleeEnabled_ExecutionContinues()
        => AssertSpec("""
            >>TURN EC-SIZE CHECKING OFF
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408D.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "ECT408DS".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT408D.
            >>TURN EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408DS.
            PROCEDURE DIVISION.
            SUB-PARA.
                DISPLAY "IN-SUB".
                GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW.
            """, "IN-SUB\nAFTER-CALL");

    [Fact]   // The EXIT PROGRAM twin: §14.9.14.4 GR3 sends an EXIT PROGRAM under a caller into the GOBACK rules,
             // and both verbs stage through the one EmitRaisingStage — so the locus fix has to hold for both or
             // it is a one-armed fix (feedback_two_arm_dispatch).
    public void ExitProgramRaising_ActivatorEnabled_CalleeDisabled_RaisesInTheActivator()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408E.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Z.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT408ES".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT408E.
            >>TURN EC-USER CHECKING OFF
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408ES.
            PROCEDURE DIVISION RAISING EC-USER-Z.
            SUB-PARA.
                DISPLAY "IN-SUB".
                EXIT PROGRAM RAISING EXCEPTION EC-USER-Z.
            """, "IN-SUB\nCAUGHT: EC-USER-Z\nAFTER-CALL");

    [Fact]   // The INVOKE arm: a method GOBACK … RAISING binds through the same EcBindRaising and is picked up at
             // the INVOKE site, so the ACTIVATING element is the invoker and the same locus applies (D-EO6).
    public void MethodGobackRaising_ActivatorEnabled_MethodDisabled_RaisesInTheInvoker()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408F.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS ECT408FC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 S USAGE OBJECT REFERENCE ECT408FC.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Z.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                INVOKE ECT408FC "NEW" RETURNING S.
                INVOKE S "WORK".
                DISPLAY "AFTER-INVOKE".
                STOP RUN.
            END PROGRAM ECT408F.

            >>TURN EC-USER CHECKING OFF
            IDENTIFICATION DIVISION.
            CLASS-ID. ECT408FC INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. WORK.
            PROCEDURE DIVISION RAISING EC-USER-Z.
            MAIN.
                DISPLAY "IN-METHOD".
                GOBACK RAISING EXCEPTION EC-USER-Z.
            END METHOD WORK.
            END OBJECT.
            END CLASS ECT408FC.
            """, "IN-METHOD\nCAUGHT: EC-USER-Z\nAFTER-INVOKE");

    [Fact]   // §14.9.18.4 GR1b3a, THE SUBSTITUTION: "If the exception condition is a level-3 exception for EC-USER
             // and that exception condition is not specified in the RAISING phrase of the procedure division
             // header of the source element in which this EXIT statement is contained, the EC-RAISING-NOT-SPECIFIED
             // exception condition is set to exist in the activating runtime element instead of the EC-USER
             // exception condition." ECT408GS's header has no RAISING phrase, so the activator's EC-USER-Q
             // declarative must NOT select; EC-RAISING-NOT-SPECIFIED is Fatal (Table 13, §14.6.13.1.6, whose third
             // column names this very case) and has no applicable declarative → §14.6.13.1.3 #7.
             //
             // This was the whole of the rule and none of it was implemented: the fixture below and its twin
             // above differ ONLY in the callee's PD-header RAISING phrase and produced byte-identical output.
    public void GobackRaisingLast_UserConditionNotInThePdHeader_BecomesRaisingNotSpecified()
        => AssertFatal("""
            >>TURN EC-USER CHECKING ON
            >>TURN EC-RAISING CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408G.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Q.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT408GS".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT408G.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408GS.
            PROCEDURE DIVISION.
            DECLARATIVES.
            SUB-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Q.
            SUB-H-P.
                DISPLAY "IN-DECL".
                GOBACK RAISING LAST EXCEPTION.
            END DECLARATIVES.
            SUB SECTION.
            SUB-PARA.
                RAISE EXCEPTION EC-USER-Q.
                DISPLAY "NEVER".
            """, "EC-RAISING-NOT-SPECIFIED", "IN-DECL");

    [Fact]   // GR1b3a's substitute is itself subject to GR1 b)'s leading condition — the condition that "is set
             // to exist in the activating runtime element" is EC-RAISING-NOT-SPECIFIED, so the enablement test is
             // taken on THAT name. Here the activator enables EC-USER but not EC-RAISING, so nothing is raised:
             // the EC-USER-Q declarative must not run (the substitution replaced the condition) and neither must
             // the run unit terminate (EC-RAISING-NOT-SPECIFIED is not enabled here).
    public void GobackRaisingLast_SubstitutedConditionNotEnabledInTheActivator_IsNotRaised()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408H.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Q.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT408HS".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT408H.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT408HS.
            PROCEDURE DIVISION.
            DECLARATIVES.
            SUB-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Q.
            SUB-H-P.
                DISPLAY "IN-DECL".
                GOBACK RAISING LAST EXCEPTION.
            END DECLARATIVES.
            SUB SECTION.
            SUB-PARA.
                RAISE EXCEPTION EC-USER-Q.
                DISPLAY "NEVER".
            """, "IN-DECL\nAFTER-CALL");

    [Fact]   // §14.9.14 GR2: EXIT PROGRAM in a program NOT under the control of a calling runtime element is
             // CONTINUE — "no exception condition is raised even if the RAISING phrase is specified".
    public void ExitProgramRaising_InMainProgram_IsContinue()
        => AssertSpec("""
            >>TURN EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT029.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EXIT PROGRAM RAISING EXCEPTION EC-SIZE-OVERFLOW.
                DISPLAY "FELL-THROUGH".
                STOP RUN.
            """, "FELL-THROUGH");

    [Fact]   // §14.9.14 GR3 (the GOBACK rules): EXIT PROGRAM RAISING in a CALLED program stages the condition
             // for the activator exactly as GOBACK RAISING does.
    public void ExitProgramRaising_InCalledProgram_Propagates()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT030.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-EXIT.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT030S".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            END PROGRAM ECT030.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT030S.
            PROCEDURE DIVISION RAISING EC-USER-EXIT.
            SUB-PARA.
                EXIT PROGRAM RAISING EXCEPTION EC-USER-EXIT.
            """, "CAUGHT: EC-USER-EXIT\nAFTER-CALL");

    [Fact]   // §14.9.18.4 GR3 (the P13 review C13 fix — this test previously pinned the PRE-fix behavior): a
             // GOBACK in a program NOT under the control of a calling runtime element "operates as if executing
             // a STOP statement … A RAISING phrase, if specified, is ignored" — even with checking enabled and a
             // FATAL name. Normal termination, no condition raised (the earlier "run-unit boundary activator"
             // reading has no GR — GR1b's staging applies only to a CALLED program's activator).
    public void GobackRaisingFatal_InMainProgram_IsIgnored_NormalTermination()
        => AssertSpec("""
            >>TURN EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT031.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW.
            """, "BEFORE");

    // ── Diagnostics (the COBOLNET07xx/08xx band) ─────────────────────────────────────────────────────────────

    [Fact]   // §14.9.29.3 SR1: only a LEVEL-3 exception-name may be raised.
    public void Raise_Level2Name_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT032.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-SIZE.
                STOP RUN.
            """, 2023), "COBOLNET0710");

    [Fact]   // §14.6.13.1 / §7.3.25.3 SR2: an unknown exception-name is rejected.
    public void Raise_UnknownName_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT033.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-BOGUS-NAME.
                STOP RUN.
            """, 2023), "COBOLNET0711");

    [Fact]   // §14.9.33.3 SR1: RESUME only in a declarative.
    public void Resume_OutsideDeclaratives_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT034.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RESUME AT NEXT STATEMENT.
                STOP RUN.
            """, 2023), "COBOLNET0712");

    [Fact]   // §14.9.33.3 SR2: RESUME shall not appear in a GLOBAL-phrase declarative.
    public void Resume_InGlobalDeclarative_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT035.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            PROCEDURE DIVISION.
            DECLARATIVES.
            G-H SECTION. USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON TF.
            G-H-P.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0713");

    [Fact]   // §14.9.49.3 SR13: FILE only with an exception-name beginning EC-I-O.
    public void UseF3_FileWithNonIoName_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT036.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE FILE TF.
            EC-H-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0715");

    // ── §14.9.49.3 SR7/SR8/SR9/SR14: the STATEMENT is the boundary (kb/Work PB364) ───────────────────────────
    // Four rules share one sentence — "the same X shall not be … in more than one USE statement within the same
    // procedure division" — and the compiler enforces all four through the ONE ConstructOperandRegister. These
    // hold BOTH arms of that boundary: a repeat ACROSS statements is the violation, and a repeat INSIDE one
    // statement is legal source that must compile. The accepting arms are the drift guard — a screen that
    // regresses to per-operand registration turns them red at once.

    [Fact]   // §14.9.49.3 SR14: the same PAIR of exception-name-2 and file-name-2 in two USE statements of one
             // procedure division is rejected. The pair is the rule's key, so the FILE phrase is essential to
             // the test: two BARE names are exception-name-1 and outside SR14 (see the accepting arm below).
    public void UseF3_DuplicatePairAcrossSections_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT037.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            PROCEDURE DIVISION.
            DECLARATIVES.
            H1 SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END FILE TF.
            H1-P.
                CONTINUE.
            H2 SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END FILE TF.
            H2-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0716");

    [Theory]   // §14.9.49.3 SR14 is scoped to PAIRS. A bare exception-name in a Format-3 USE is
    [InlineData(2002)]   // exception-name-1 (§14.9.49.2's figure puts exception-name-2 only in the FILE
    [InlineData(2014)]   // alternative; §14.9.49.4 GR3 c)/d) against e)/f)/g) split on the same axis), so
    [InlineData(2023)]   // repeating it across USE statements is legal — GR3 gives it its outcome (the FIRST
                         // declarative runs) instead of forbidding it. Every edition that HAS Format 3.
    public void UseF3_RepeatedBareExceptionName_IsAccepted(int edition)
    {
        var (ok, diagnostics) = EditionHarness.Compile($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT037B.
            PROCEDURE DIVISION.
            DECLARATIVES.
            H1 SECTION. USE AFTER EXCEPTION CONDITION EC-USER-D.
            H1-P.
                CONTINUE.
            H2 SECTION. USE AFTER EXCEPTION CONDITION EC-USER-D.
            H2-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, edition);
        Assert.True(ok, $"[--std {edition}] a repeated exception-name-1 is legal: {string.Join("\n", diagnostics)}");
    }

    [Theory]   // §14.9.49.3 SR14 forbids the pair in "more than one USE statement"; ONE statement writing it
    [InlineData(2002)]   // twice is not more than one, and §14.9.49.2's inner `{ FILE file-name-2 } …` ellipsis
    [InlineData(2014)]   // permits the repetition outright.
    [InlineData(2023)]
    public void UseF3_SamePairTwiceInOneStatement_IsAccepted(int edition)
    {
        var (ok, diagnostics) = EditionHarness.Compile($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT037C.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            PROCEDURE DIVISION.
            DECLARATIVES.
            H1 SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END FILE TF FILE TF.
            H1-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, edition);
        Assert.True(ok, $"[--std {edition}] one statement is not more than one: {string.Join("\n", diagnostics)}");
    }

    [Theory]   // §14.9.49.3 SR8: "The same file-name shall not appear in more than one USE AFTER EXCEPTION
    [InlineData(85)]     // statement within the same procedure division." Format 1 exists at every edition, so
    [InlineData(2002)]   // the rejection must hold at all four — and so must its complement below.
    [InlineData(2014)]
    [InlineData(2023)]
    public void UseF1_SameFileInTwoStatements_Diagnosed(int edition)
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT037D.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            PROCEDURE DIVISION.
            DECLARATIVES.
            H1 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON TF.
            H1-P.
                CONTINUE.
            H2 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON TF.
            H2-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, edition), "COBOLNET0897");

    [Theory]   // §14.9.49.3 SR8's complement: ONE Format-1 USE naming the same file twice. The figure writes
    [InlineData(85)]     // `{ file-name-1 } …` with nothing making the repetition illegal, and §14.9.49.4
    [InlineData(2002)]   // GR6 a) associates the one procedure with the file however often it is written.
    [InlineData(2014)]
    [InlineData(2023)]
    public void UseF1_SameFileTwiceInOneStatement_IsAccepted(int edition)
    {
        var (ok, diagnostics) = EditionHarness.Compile($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT037E.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            PROCEDURE DIVISION.
            DECLARATIVES.
            H1 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON TF TF.
            H1-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, edition);
        Assert.True(ok, $"[--std {edition}] one statement is not more than one: {string.Join("\n", diagnostics)}");
    }

    [Theory]   // ONE violation is ONE diagnostic — the two arms above COMBINED, which neither of them reaches.
    [InlineData(85)]     // TF is named by a first USE statement and then TWICE by a second: the second statement
    [InlineData(2002)]   // violates SR8 once, and its own repeat of TF is the legal in-statement repetition. The
    [InlineData(2014)]   // register therefore remembers the LAST construct that registered a key, not the first
    [InlineData(2023)]   // — with a first-seen ordinal BOTH writes in the second statement compare against
                         // statement 0 and the rule is reported twice (kb/Work PB703, which shares the register).
    public void UseF1_SameFileRepeatedInTheSecondStatement_DiagnosedOnce(int edition)
    {
        var (ok, diagnostics) = EditionHarness.Compile($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT037F.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            PROCEDURE DIVISION.
            DECLARATIVES.
            H1 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON TF.
            H1-P.
                CONTINUE.
            H2 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON TF TF.
            H2-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, edition);
        Assert.False(ok, $"[--std {edition}] the second USE statement repeats TF across statements");
        Assert.Equal(1, diagnostics.Count(d => d.Contains("COBOLNET0897", StringComparison.Ordinal)));
    }

    [Fact]   // §14.9.18.3 SR2 (¶27403): an EC-USER exception-name in GOBACK/EXIT RAISING shall be specified in
             // the RAISING phrase of the procedure division header.
    public void GobackRaising_EcUserNotInPdHeader_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT047.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GOBACK RAISING EXCEPTION EC-USER-UNDECLARED.
            """, 2023), "COBOLNET0717");

    [Fact]   // §7.3.25.3 SR3: a duplicated (exception-name, file-name) combination within one TURN directive.
    public void Turn_DuplicateName_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            >>TURN EC-SIZE EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT038.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0718");

    [Fact]   // §7.3.25.3 SR4: a file-name only with an EC-I-O… exception-name.
    public void Turn_FileWithNonIoName_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            >>TURN EC-SIZE SOME-FILE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT039.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0719");

    // ── Per-edition gating (the four-compilers rule: 2002+ constructs diagnosed BY NAME at --std 85) ─────────

    [Fact]
    public void Turn_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            >>TURN EC-ALL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT040.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 85), "COBOLNET0900");   // the ONE introduction band (kb/Work PB725 retired the bespoke 0875)

    [Fact]
    public void Raise_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT041.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-A.
                STOP RUN.
            """, 85), "COBOLNET0876");

    [Fact]
    public void UseF3_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT042.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-A.
            EC-H-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, 85), "COBOLNET0877");

    [Fact]
    public void SetLastException_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT043.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET LAST EXCEPTION TO OFF.
                STOP RUN.
            """, 85), "COBOLNET0879");

    [Fact]
    public void GobackRaising_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT044.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GOBACK RAISING EXCEPTION EC-USER-A.
            """, 85), "COBOLNET0879");

    private const string EcWordsProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ECT045.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 RAISE PIC 9 VALUE 1.
        01 RESUME PIC 9 VALUE 2.
        01 EC PIC 9 VALUE 3.
        01 CONDITION PIC 9 VALUE 4.
        01 STATEMENT PIC 9 VALUE 5.
        01 RAISING PIC 9 VALUE 6.
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY RAISE RESUME EC CONDITION STATEMENT RAISING.
            STOP RUN.
        """;

    /// <summary>The EC words are legal USER-DEFINED words at COBOL-85 only — the edition that predates the
    /// exception model. (The pre-validator posture said "legal at every edition"; the P2.4 mechanical tables
    /// corrected it TWICE: DEVLOG 578 proved them §8.9-reserved at 2023, and the per-standard source lists
    /// prove them reserved since 2002 — they are NOT among the Annex E.2 item-25 words newly reserved in 2023,
    /// so 2023-only was under-inclusive. DEVLOG 585.)</summary>
    [Fact]
    public void EcWords_RemainUserDefinedWords_At85()
    {
        var (ok, diags) = EditionHarness.Compile(EcWordsProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", diags)}");
    }

    /// <summary>At 2002/2014/2023 the EC words (except the context-sensitive STATEMENT) ARE reserved words —
    /// the P2.4 EditionValidator funnel enforces it per edition: strict rejects with COBOLNET0901.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void EcWords_AsUserWords_2002Plus_Rejected0901(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(EcWordsProgram, edition);
        Assert.False(ok, $"--std {edition} strict must reject reserved EC words used as user-defined words");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'RAISE'");
    }

    // ── The zero-scaffolding invariant (SSOT §18.16 / deep-dive D9–D10) ──────────────────────────────────────

    [Fact]   // An EC-free program's generated source carries NO exception machinery — no ExceptionState, no
             // __EcDispatch, no Runtime.Exceptions using (checking OFF compiles to NOTHING, §7.3.25.4 GR1).
    public void ZeroScaffolding_EcFreeProgram_EmitsNoExceptionMachinery()
    {
        string dir = CutRunner.NewTempDir("ec0");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. ECT046.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 WS-A PIC 9(3) VALUE 5.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    ADD 1 TO WS-A.
                    DISPLAY WS-A.
                    STOP RUN.
                """);
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            string generated = File.ReadAllText(Path.Combine(dir, "prog.g.cs"));
            Assert.DoesNotContain("ExceptionState", generated);
            Assert.DoesNotContain("__EcDispatch", generated);
            Assert.DoesNotContain("__IoCheckEc", generated);
            Assert.DoesNotContain("Runtime.Exceptions", generated);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    // ── The §15.33 width collision (kb/Work R05 — Phase-B F6): r1 fixes EXCEPTION-STATUS at 31 characters
    //    while COBOL-2023 words run to 63 (§8.3.2.1) and the §14.6.13.1.1 open-family suffixes are unbounded.
    //    The r1 width is implemented AS WRITTEN; COBOLNET1636 (Warning) makes the collision visible. ──────────

    /// <summary>44 characters — legal at 2023 only (the 31-char COBOL-2002 word limit rejects it below).</summary>
    private const string LongEc = "EC-USER-ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Fact]   // §15.33.3 r1 IS the width: a 44-character legal name reads back as its 31-character prefix —
             // the truncation is the rule's own, not an implementation choice.
    public void ExceptionStatus_LongUserName_ReturnsThe31CharPrefix()
        => AssertSpec($"""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT048.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION {LongEc}.
                DISPLAY "S=[" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """, $"S=[{LongEc[..31]}]");

    [Fact]   // COBOLNET1636 fires ONCE per spelling even when the name appears at TURN + RAISE + USE (the ONE
             // resolution funnel dedupes), and the program still compiles clean — legal source stays legal.
    public void LongUserName_AdvisedOnce_CompilesClean()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Prog("ECT049",
            $">>TURN {LongEc} CHECKING ON", "", "", $"""
            EC-H SECTION. USE AFTER EXCEPTION CONDITION {LongEc}.
            EC-H-P.
                RESUME AT NEXT STATEMENT.
            """, $"""
                RAISE EXCEPTION {LongEc}.
                STOP RUN.
            """), 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.Equal(1, warnings.Count(w => w.Contains("COBOLNET1636")));
    }

    [Fact]   // The advisory rides the FUNNEL, not one verb: a name spelled ONLY in >>TURN — never RAISEd —
             // is still advised (the arm a single-site fix would have missed; feedback_two_arm_dispatch).
    public void LongUserName_TurnOnlySpelling_StillAdvised()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull($"""
            >>TURN {LongEc} CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT050.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.Contains(warnings, w => w.Contains("COBOLNET1636"));
    }

    [Fact]   // Below 2023 the collision cannot arise: the 44-character word itself exceeds the COBOL-2002
             // 31-character word limit — INCLUDING in a >>TURN directive, where the word never reaches the
             // tree-walk funnel. ⛔ This fact FAILED when first written: the evidence ledger's "correctly
             // rejected with COBOLNET1567" was measured on the RAISE spelling only, and the directive path
             // compiled the same word clean (CobolWordRule closed that hole — TURN operands now share the
             // §8.3.2.1 ceiling). No advisory either: 1636 is a 2023-collision message, not a length error.
    public void LongUserName_At2002_TheWordItselfIsRejected()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull($"""
            >>TURN {LongEc} CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT051.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a 44-character word must not compile at --std 2002");
        Assert.Contains(errors, e => e.Contains("COBOLNET1567"));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1636"));
    }

    [Fact]   // §15.32.3 r1 / §15.30.3 r1 are PER-CONDITION (kb/Work R06): one statement checked for EC-SIZE
             // (WITH LOCATION) *and* EC-BOUND-SUBSCRIPT (without). The subscript raise must answer 63 spaces /
             // one space — under the old per-statement bool, the sibling's WITH LOCATION contaminated it and
             // EXCEPTION-STATEMENT returned "DIVIDE". The zero-divide raise keeps its location operands.
    public void WithLocation_IsPerCondition_NotPerStatement()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun("""
            >>TURN EC-SIZE CHECKING ON WITH LOCATION
            >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT053.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TAB.
               05 WS-CELL PIC 9 OCCURS 3 TIMES VALUE 1.
            01 WS-I PIC 9 VALUE 5.
            01 WS-D PIC 9 VALUE 0.
            01 WS-R PIC 9 VALUE 7.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-ALL.
            EC-H-P.
                DISPLAY "S=[" FUNCTION EXCEPTION-STATEMENT "] L=[" FUNCTION EXCEPTION-LOCATION "]".
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                DIVIDE WS-CELL(WS-I) BY WS-D GIVING WS-R.
                MOVE 2 TO WS-I.
                DIVIDE WS-CELL(WS-I) BY WS-D GIVING WS-R.
                STOP RUN.
            """);
        Assert.True(ok, detail);
        var lines = stdout.Split('\n');
        Assert.Equal(2, lines.Length);
        // Raise 1 — EC-BOUND-SUBSCRIPT, enabled WITHOUT location: r1's answers, uncontaminated.
        Assert.Equal($"S=[{new string(' ', 63)}] L=[ ]", lines[0]);
        // Raise 2 — EC-SIZE-ZERO-DIVIDE, enabled WITH LOCATION: the Table-12 name + the three-part location.
        Assert.StartsWith($"S=[{"DIVIDE",-63}] L=[ECT053; MAIN-PARA OF MAIN;", lines[1]);
    }

    [Fact]   // kb/Work R07 + §15.32.3 r2 / §15.30.3 r2: GOBACK … RAISING under WITH LOCATION records the
             // Table 12 name ("GOBACK") and the three-part location. SetPropagating's Set was TWO-ARG, so both
             // the returning element's status and what the activator reads answered 63 spaces / one space even
             // with LOCATION on — a silent wrong answer (BoundRaising had no location fields at all, unlike
             // its sibling BoundRaise).
             //
             // §7.3.25.4 GR7 keys the operands on the directive governing THE SOURCE STATEMENT — "all information
             // necessary to identify a source statement … is made available to the run unit" — and that statement
             // is the CALLEE's GOBACK, so WITH LOCATION stays on the callee's directive here and the activator's
             // is a plain CHECKING ON. The activator's ON is what kb/Work PB408 added: §14.9.18.4 GR1 b) raises the
             // condition in the ACTIVATING element only if checking is enabled THERE, and §14.6.13.1.1 sets the
             // run unit's last exception status only for a condition that was RAISED — so with the activator
             // enabling nothing (as this fixture used to have it) there is no status for EXCEPTION-STATEMENT to
             // report, and the old expectation was reading a status the callee wrote for a raise that the
             // standard puts in the other element.
    public void GobackRaising_WithLocation_RecordsStatementAndLocation()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunWith("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT054.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "ECT054S".
                DISPLAY "S=[" FUNCTION EXCEPTION-STATEMENT "]".
                DISPLAY "L=[" FUNCTION EXCEPTION-LOCATION "]".
                STOP RUN.
            """, """
            >>TURN EC-USER CHECKING ON WITH LOCATION
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT054S.
            PROCEDURE DIVISION RAISING EC-USER-R07.
            SUB-P.
                GOBACK RAISING EXCEPTION EC-USER-R07.
            """);
        Assert.True(ok, detail + stdout);
        var lines = stdout.Split('\n');
        Assert.Equal($"S=[{"GOBACK",-63}]", lines[0]);
        Assert.StartsWith("L=[ECT054S; SUB-P;", lines[1]);
    }

    [Fact]   // The complement, and the one that makes the pair EVIDENCE rather than a single reading: with the
             // activator enabling NOTHING, §14.9.18.4 GR1 b) raises nothing in it, so §14.6.13.1.1's run-unit last
             // exception status — "set to indicate the last level-3 exception condition that was RAISED in the run
             // unit" — has nothing to record and both functions answer their no-information values. Before
             // kb/Work PB408 the RETURNING element wrote the status on its way out, so an activator that had
             // turned the condition OFF could still read it back (§15.32.3 r1's spaces / §15.30.3 r1's one space).
    public void GobackRaising_IntoUncheckedActivator_LeavesNoLastExceptionStatus()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunWith("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT054B.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "ECT054BS".
                DISPLAY "S=[" FUNCTION EXCEPTION-STATEMENT "]".
                DISPLAY "T=[" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """, """
            >>TURN EC-USER CHECKING ON WITH LOCATION
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT054BS.
            PROCEDURE DIVISION RAISING EC-USER-R07.
            SUB-P.
                GOBACK RAISING EXCEPTION EC-USER-R07.
            """);
        Assert.True(ok, detail + stdout);
        var lines = stdout.Split('\n');
        Assert.Equal($"S=[{new string(' ', 63)}]", lines[0]);
        Assert.Equal($"T=[{new string(' ', 31)}]", lines[1]);
    }

    // ── kb/Work PB892 Arm B — a staged RAISING condition names the ACTIVATION it was staged for ─────────────────
    // The class module is compiled ALONE so the element that INVOKEs it can be EC-free: within one compilation
    // group a RAISING phrase makes every element EC-active (every activation site then emits a pickup), so the
    // defect — an activator that emits NO pickup leaving the staged condition for SOMEONE ELSE's pickup — is only
    // reachable across a compilation boundary. P892K hands out the object; K892.RAISE1 ends GOBACK RAISING.

    private const string Pb892ClassModule = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P892K.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS K892.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-OBJ USAGE OBJECT REFERENCE K892.
        LINKAGE SECTION.
        01 L-OBJ USAGE OBJECT REFERENCE.
        PROCEDURE DIVISION RETURNING L-OBJ.
        MAIN-P.
            INVOKE K892 "NEW" RETURNING W-OBJ.
            SET L-OBJ TO W-OBJ.
            GOBACK.
        END PROGRAM P892K.
        IDENTIFICATION DIVISION.
        CLASS-ID. K892 INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        METHOD-ID. QUIET.
        PROCEDURE DIVISION.
        Q-P.
            DISPLAY "IN-QUIET".
            GOBACK.
        END METHOD QUIET.
        METHOD-ID. RAISE1.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LR PIC 9.
        PROCEDURE DIVISION RETURNING LR RAISING EC-USER-BZ.
        R-P.
            DISPLAY "IN-RAISE1".
            MOVE 1 TO LR.
            GOBACK RAISING EXCEPTION EC-USER-BZ.
        END METHOD RAISE1.
        END OBJECT.
        END CLASS K892.
        """;

    [Fact]   // §14.9.18.4 GR1 b): "an exception condition is raised in the activating runtime element if checking for
             // that exception condition is enabled in the activating runtime element". The activator of RAISE1 is the
             // EC-FREE main, which enables nothing — so EC-USER-BZ is raised NOWHERE. P892X is a DIFFERENT activation
             // (and not RAISE1's activator); its INVOKE of QUIET, which raises nothing, must not run its declarative.
             // Before PB892 Arm B the staged slot was anonymous and run-unit-wide: the main's INVOKE left it standing
             // and P892X's pickup took it, printing DECL-IN-X after IN-QUIET.
    public void GobackRaising_StagedForAnUncheckedActivator_IsNotTakenByALaterActivation()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunWith("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P892A.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OBJ USAGE OBJECT REFERENCE.
            01 RV PIC 9.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "P892K" RETURNING OBJ.
                INVOKE OBJ "RAISE1" RETURNING RV.
                DISPLAY "A-AFTER-RAISE1".
                CALL "P892X".
                DISPLAY "A-END".
                STOP RUN.
            """, Pb892ClassModule, """
            >>TURN EC-USER-BZ CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P892X.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OBJ USAGE OBJECT REFERENCE.
            PROCEDURE DIVISION.
            DECLARATIVES.
            HB SECTION. USE AFTER EXCEPTION CONDITION EC-USER-BZ.
            HB-P.
                DISPLAY "DECL-IN-X".
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                CALL "P892K" RETURNING OBJ.
                INVOKE OBJ "QUIET".
                DISPLAY "X-AFTER-QUIET".
                GOBACK.
            """);
        Assert.True(ok, detail + stdout);
        Assert.Equal("IN-RAISE1\nA-AFTER-RAISE1\nIN-QUIET\nX-AFTER-QUIET\nA-END", stdout);
    }

    [Fact]   // The same rule one level deeper, and its positive control. P892Y enables EC-USER-BZ and CALLs the
             // EC-free P892E, which INVOKEs RAISE1: RAISE1's activator is P892E (unchecked), so the condition is not
             // raised there — and P892Y's CALL pickup, running in a DIFFERENT activation, must not take it either
             // (a fence at the CALL would not help: the staging happened INSIDE the call). Then P892Y INVOKEs
             // RAISE1 itself: now P892Y IS the activator and checking is enabled there, so GR1 b) raises the
             // condition in P892Y and its declarative runs (normal completion → execution continues, §14.6.13.1.3).
    public void GobackRaising_IsRaisedOnlyInItsOwnActivator_AcrossAnUncheckedIntermediate()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunWith("""
            >>TURN EC-USER-BZ CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P892Y.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OBJ USAGE OBJECT REFERENCE.
            01 RV PIC 9.
            PROCEDURE DIVISION.
            DECLARATIVES.
            HY SECTION. USE AFTER EXCEPTION CONDITION EC-USER-BZ.
            HY-P.
                DISPLAY "DECL-IN-Y".
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                CALL "P892E".
                DISPLAY "Y-AFTER-CALL".
                CALL "P892K" RETURNING OBJ.
                INVOKE OBJ "RAISE1" RETURNING RV.
                DISPLAY "Y-AFTER-RAISE1".
                STOP RUN.
            """, Pb892ClassModule, """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P892E.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OBJ USAGE OBJECT REFERENCE.
            01 RV PIC 9.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "P892K" RETURNING OBJ.
                INVOKE OBJ "RAISE1" RETURNING RV.
                DISPLAY "E-AFTER-RAISE1".
                GOBACK.
            """);
        Assert.True(ok, detail + stdout);
        Assert.Equal("IN-RAISE1\nE-AFTER-RAISE1\nY-AFTER-CALL\nIN-RAISE1\nDECL-IN-Y\nY-AFTER-RAISE1", stdout);
    }

    [Fact]   // The hole the funnel closed: USE AFTER EC with a LEVEL-2 name of a 2023-only family (EC-MCS) at
             // --std 2002. The old USE-site copy guarded the introduction gate with Level == 3, so the level-2
             // spelling slipped through un-gated; the funnel gates every level (COBOLNET0878).
    public void UseAfterEc_Level2NameOf2023Family_At2002_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(Prog("ECT052",
            "", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-MCS.
            EC-H-P.
                RESUME AT NEXT STATEMENT.
            """, """
                STOP RUN.
            """), 2002), "COBOLNET0878");
}
