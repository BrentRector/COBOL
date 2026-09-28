// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §14.9.49.4 GR2 — THE USE-PROCEDURE RE-ENTRANCY CONDITION, EVERY ARM (kb/Work PB368).
///
/// <para>GR2 reads "During the execution of a USE procedure, if a statement raises an exception condition that
/// would cause the execution of a USE procedure that had previously been activated and had not yet returned
/// control to the activating entity, the EC-FLOW-USE exception condition is set to exist." The compiler had the
/// consequence and not the condition: the generated <c>__RunUse</c> returned quietly on its re-entrancy guard
/// and EC-FLOW-USE appeared nowhere outside <c>ExceptionCatalog</c>, so a program could not detect its own
/// declarative recursion and the §14.6.13.1.6 Table 13 Fatal default could never fire.</para>
///
/// <para><b>Why a test class and not one golden.</b> The fix lives at the ONE place that knows the guard fired,
/// and SIX generated selection paths reach it — <c>__IoCheck</c> (format-1, I-O-status driven), <c>__IoCheckEc</c>
/// (the EC-model I-O bridge), <c>__EcDispatch</c> (format 3), <c>__EcObjDispatch</c> (format 4),
/// <c>__RunGlobalUse</c> (the §14.9.49.4 GR4 b outward GLOBAL walk) and the report engine's BEFORE REPORTING
/// hook. This repo's most reproducible defect is a dispatch with two arms and one of them fixed, so the arms
/// that are separately GENERATED are separately pinned here, together with the two outcomes a positive golden
/// cannot carry (the §14.6.13.1.3 #7 abnormal termination, and the checking-off zero-scaffolding invariant) and
/// the ONE id distinction the raise site has to make: the exception-checking PERFORM's imp-2/3/4 handler ranges
/// share <c>__RunUse</c> and its guard array but are NOT USE procedures (§14.9.28.4 GR17, not §14.9.49.4 GR3),
/// so GR2 does not reach them.</para>
///
/// <para>The end-to-end counterpart is <c>conformance:2002/pb368_flow_use_reentrancy</c> (the handled arm with
/// RESUME AT NEXT STATEMENT) and <c>conformance:negative/pb368-flow-use-name-below-2002</c> (the edition gate).</para>
/// </summary>
public sealed class FlowUseReentrancyTests : CobolNetTestBase
{
    /// <summary>A program whose MAIN OPENs an absent file, whose format-1 declarative D1 OPENs the SAME file
    /// (so the failing inner OPEN would re-select D1 — GR2's exact antecedent), and whose remaining text is
    /// supplied by the caller: <paramref name="turn"/> is the leading directive line (or empty), and
    /// <paramref name="handler"/> the extra declarative sections placed after D1.</summary>
    private static string Recursive(string turn, string handler, string tail = "") => turn + $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB368U.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F1 ASSIGN TO "pb368u-absent.dat"
                       ORGANIZATION IS SEQUENTIAL
                       FILE STATUS IS FS1.
               DATA DIVISION.
               FILE SECTION.
               FD  F1.
               01  F1-REC   PIC X(10).
               WORKING-STORAGE SECTION.
               01  FS1      PIC XX VALUE "00".
               PROCEDURE DIVISION.
               DECLARATIVES.
               D1 SECTION.
                   USE AFTER STANDARD ERROR PROCEDURE ON F1.
               D1-P.
                   DISPLAY "D1-ENTER".
                   OPEN INPUT F1.
                   DISPLAY "D1-EXIT".
            {handler}   END DECLARATIVES.
               MAIN SECTION.
               MAIN-P.
            {tail}      OPEN INPUT F1.
                   DISPLAY "AFTER".
                   STOP RUN.

            """;

    private const string FlowUseDeclarative = """
               DFU SECTION.
                   USE AFTER EXCEPTION CONDITION EC-FLOW-USE.
               DFU-P.
                   DISPLAY "FLOW-USE " FUNCTION EXCEPTION-STATUS.
                   RESUME AT NEXT STATEMENT.

            """;

    /// <summary>§14.6.13.1.3 #5 — "there is an applicable USE statement in the source unit that specifies the
    /// exception-name associated with the exception condition … the associated declarative is executed", reached
    /// through the plain <c>__IoCheck</c> selector (EC-I-O checking is OFF here, so the format-1 declarative is
    /// selected by I-O status alone — §14.6.13.1.1 NOTE 1 — and EC-FLOW-USE is the only enabled condition).</summary>
    [Fact]
    public void ReenteredDeclarative_WithAnEcFlowUseDeclarative_RaisesAndDispatches()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Recursive(">>TURN EC-FLOW-USE CHECKING ON\n", FlowUseDeclarative), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal(["D1-ENTER", "FLOW-USE EC-FLOW-USE", "D1-EXIT", "AFTER"],
            stdout.Split("\r\n").Select(l => l.TrimEnd()).ToArray());
    }

    /// <summary>§14.6.13.1.3 #7 — with checking enabled and NO applicable declarative, "execution of the run
    /// unit is terminated abnormally". The outcome a positive golden cannot carry, and the whole reason the
    /// condition has to EXIST: before this fix the run completed normally and the recursion was invisible.</summary>
    [Fact]
    public void ReenteredDeclarative_WithNoHandler_TerminatesAbnormally()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Recursive(">>TURN EC-FLOW-USE CHECKING ON\n", ""), dialectLevel: 2002);
        Assert.False(ok, "§14.6.13.1.3 #7 requires abnormal termination, but the run unit completed: " + stdout);
        Assert.Contains("EC-FLOW-USE", detail, StringComparison.Ordinal);
        Assert.Equal("D1-ENTER", stdout.TrimEnd());   // the inner OPEN never returns to D1-EXIT
    }

    /// <summary>§14.6.13.1.1 — "if checking for an exception that occurs is not enabled, no exception condition
    /// is raised", and §14.6.13.1.3 #8 leaves what happens instead to the implementor: this implementation
    /// declines to re-enter the active procedure and continues. The ZERO-SCAFFOLDING half is asserted too — an
    /// EC-free program's generated source must not gain a word of this machinery.</summary>
    [Fact]
    public void ReenteredDeclarative_WithCheckingOff_DeclinesQuietlyAndEmitsNoMachinery()
    {
        string src = Recursive("", "");
        var (ok, stdout, detail) = CompileAndRun(src, dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal(["D1-ENTER", "D1-EXIT", "AFTER"], stdout.Split("\r\n").Select(l => l.TrimEnd()).ToArray());
        Assert.DoesNotContain("FlowUse", Generated(src, 2002), StringComparison.Ordinal);
    }

    /// <summary>⛔ THE ID DISTINCTION. An exception-checking PERFORM's imp-2/3/4 handlers are appended pc-ranges
    /// run by the SAME <c>__RunUse</c> over the SAME <c>__useActive</c> array (§14.9.28.4 GR17), and they are not
    /// USE procedures — GR2 does not reach them. The emitted guard must therefore raise only for ids below the
    /// declarative count, and must still raise for the declaratives in the very same program.</summary>
    [Fact]
    public void F3PerformHandlerSlots_AreExcludedFromTheRaise()
    {
        const string f3 = """
                       PERFORM
                           RAISE EXCEPTION EC-USER-PBTHREESIXEIGHT
                       WHEN EC-USER-PBTHREESIXEIGHT
                           DISPLAY "F3-HANDLER"
                       END-PERFORM.

            """;
        string src = Recursive(">>TURN EC-FLOW-USE CHECKING ON\n", FlowUseDeclarative, f3);
        var (ok, stdout, detail) = CompileAndRun(src, dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal(["F3-HANDLER", "D1-ENTER", "FLOW-USE EC-FLOW-USE", "D1-EXIT", "AFTER"],
            stdout.Split("\r\n").Select(l => l.TrimEnd()).ToArray());
        // Two declaratives (D1, DFU) + one handler slot ⇒ the guard carries the id test, naming the boundary.
        Assert.Contains("if (__id < 2) ExceptionState.FlowUseError(", Generated(src, 2023), StringComparison.Ordinal);
    }

    /// <summary>§14.9.49.4 GR4 b — the declarative is reached by the OUTWARD walk into a containing program's
    /// USE GLOBAL section (a SEPARATE generated method, <c>__RunGlobalUse</c>, on the container). The condition
    /// is raised in the DECLARING program, whose own re-entrancy array is the one that says the procedure is
    /// still active, and with no EC-FLOW-USE declarative there #7's abnormal termination is the outcome.</summary>
    [Fact]
    public void GlobalDeclarative_ReenteredThroughTheOutwardWalk_Raises()
    {
        const string src = """
            >>TURN EC-FLOW-USE CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB368UO.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT F1 ASSIGN TO "pb368uo-absent.dat"
                           ORGANIZATION IS SEQUENTIAL
                           FILE STATUS IS FS1.
                   DATA DIVISION.
                   FILE SECTION.
                   FD  F1 IS GLOBAL.
                   01  F1-REC   PIC X(10).
                   WORKING-STORAGE SECTION.
                   01  FS1      PIC XX GLOBAL VALUE "00".
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   DG SECTION.
                       USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F1.
                   DG-P.
                       DISPLAY "DG-ENTER".
                       OPEN INPUT F1.
                       DISPLAY "DG-EXIT".
                   END DECLARATIVES.
                   OUTER-MAIN SECTION.
                   OM-P.
                       CALL "PB368UI".
                       DISPLAY "OUTER-AFTER".
                       STOP RUN.

                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB368UI.
                   PROCEDURE DIVISION.
                   IN-S SECTION.
                   IN-P.
                       OPEN INPUT F1.
                       DISPLAY "INNER-AFTER".
                       GOBACK.
                   END PROGRAM PB368UI.
                   END PROGRAM PB368UO.

            """;
        var (ok, stdout, detail) = CompileAndRun(src, dialectLevel: 2002);
        Assert.False(ok, "§14.6.13.1.3 #7 requires abnormal termination, but the run unit completed: " + stdout);
        Assert.Contains("EC-FLOW-USE", detail, StringComparison.Ordinal);
        Assert.Equal("DG-ENTER", stdout.TrimEnd());
    }

    /// <summary>The EC-model I-O bridge (<c>__IoCheckEc</c>) is a DIFFERENT generated selector from
    /// <c>__IoCheck</c>, chosen when the statement has an EC-I-O name enabled. Same rule, same raise: the
    /// format-3 EC-I-O declarative re-entered on its own failing OPEN raises EC-FLOW-USE, the GR3 e) tier
    /// selects the EC-FLOW-USE declarative, and its RESUME lets D1 finish — after which the OUTER OPEN's own
    /// fatal EC-I-O-PERMANENT-ERROR, its declarative having completed normally, takes #5's termination.</summary>
    [Fact]
    public void EcModelIoSelector_ReenteredDeclarative_Raises()
    {
        const string src = """
            >>TURN EC-I-O EC-FLOW-USE CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB368UE.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT F1 ASSIGN TO "pb368ue-absent.dat"
                           ORGANIZATION IS SEQUENTIAL
                           FILE STATUS IS FS1.
                   DATA DIVISION.
                   FILE SECTION.
                   FD  F1.
                   01  F1-REC   PIC X(10).
                   WORKING-STORAGE SECTION.
                   01  FS1      PIC XX VALUE "00".
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   D1 SECTION.
                       USE AFTER EXCEPTION CONDITION EC-I-O-PERMANENT-ERROR.
                   D1-P.
                       DISPLAY "D1-ENTER".
                       OPEN INPUT F1.
                       DISPLAY "D1-EXIT".
                   DFU SECTION.
                       USE AFTER EXCEPTION CONDITION EC-FLOW-USE.
                   DFU-P.
                       DISPLAY "FLOW-USE " FUNCTION EXCEPTION-STATUS.
                       RESUME AT NEXT STATEMENT.
                   END DECLARATIVES.
                   MAIN SECTION.
                   MAIN-P.
                       OPEN INPUT F1.
                       DISPLAY "AFTER".
                       STOP RUN.

            """;
        var (ok, stdout, detail) = CompileAndRun(src, dialectLevel: 2002);
        Assert.False(ok, "the outer EC-I-O-PERMANENT-ERROR is fatal and its declarative completed normally "
            + "(§14.6.13.1.3 #5), so the run unit must terminate abnormally: " + stdout);
        Assert.Contains("EC-I-O-PERMANENT-ERROR", detail, StringComparison.Ordinal);
        Assert.Equal(["D1-ENTER", "FLOW-USE EC-FLOW-USE", "D1-EXIT"],
            stdout.Split("\r\n").Select(l => l.TrimEnd()).ToArray());
    }

    /// <summary>Compile <paramref name="source"/> and return the generated C# — the shape assertions above read
    /// the emitted <c>__RunUse</c> body rather than inferring it from behaviour.</summary>
    private string Generated(string source, int dialectLevel)
    {
        string srcPath = Path.Combine(TempDir, "gen.cob");
        File.WriteAllText(srcPath, source);
        var r = CompilerDriver.Compile(
            new CompilerDriver.Options(srcPath, Path.Combine(TempDir, "gen.dll"), DialectLevel: dialectLevel, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r.Success, string.Join("\n", r.Errors));
        return File.ReadAllText(r.GeneratedCsPath!);
    }
}
