// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1066 — ISO §14.9.28.4 GR14: "An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of
/// imperative-statement-1. Immediately preceding the END PERFORM phrase, there is an implicit POP ALL" — reaching
/// the state the CONDITIONAL-COMPILATION driver holds, which runs before any parse: the compilation-variable table
/// (§7.3.22.4 GR3: "all instances of that directive" are pushed) and the frontend-inline FLAG options. A
/// <c>&gt;&gt;DEFINE</c> written in a WHEN or FINALLY phrase is undone at END-PERFORM, one written in
/// imperative-statement-1 (before the PUSH) survives, and the driver places a directive met inside a copybook or
/// after a REPLACE statement in the same resultant frame the parser's tokens use.
/// </summary>
public sealed class ExceptionPerformDefineScopeTests
{
    private const string After = """
                   >>IF ZZ IS DEFINED
                       DISPLAY "ZZ-DEFINED"
                   >>ELSE
                       DISPLAY "ZZ-UNDEFINED"
                   >>END-IF
        """;

    [Fact]
    public void Define_WrittenInAWhenPhrase_IsUndoneAtEndPerform()
    {
        var r = Compile("""
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                   >>DEFINE ZZ AS 1
                           DISPLAY "HANDLER"
                       END-PERFORM
            """ + "\n" + After);
        Assert.Equal("ZZ-UNDEFINED", r.Branch);
    }

    [Fact]
    public void DefineOff_WrittenInAFinallyPhrase_IsUndoneAtEndPerform()
    {
        var r = Compile("""
                   >>DEFINE ZZ AS 1
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                           DISPLAY "HANDLER"
                       FINALLY
                   >>DEFINE ZZ OFF
                           DISPLAY "FINAL"
                       END-PERFORM
            """ + "\n" + After);
        Assert.Equal("ZZ-DEFINED", r.Branch);
    }

    [Fact]
    public void Define_WrittenInImperativeStatement1_PrecedesThePushAndSurvives()
    {
        var r = Compile("""
                       PERFORM
                   >>DEFINE ZZ AS 1
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                           DISPLAY "HANDLER"
                       END-PERFORM
            """ + "\n" + After);
        Assert.Equal("ZZ-DEFINED", r.Branch);
    }

    [Fact]
    public void Define_InsideTheHandler_IsInForceThere()
    {
        // The control: the POP is at END-PERFORM, so a directive still inside the handler sees the DEFINE.
        var r = Compile("""
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                   >>DEFINE ZZ AS 1
            """ + "\n" + After + "\n" + """
                       END-PERFORM
            """);
        Assert.Equal("ZZ-DEFINED", r.Branch);
    }

    [Fact]
    public void NestedPerforms_TheInnerPopRestoresTheOuterHandlersState()
    {
        // The outer handler defines ZZ, the inner handler undefines it; the inner END-PERFORM restores the outer
        // handler's state (ZZ defined), and the outer END-PERFORM restores the program's (ZZ undefined).
        var r = Compile("""
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                   >>DEFINE ZZ AS 1
                           PERFORM
                               DISPLAY "INNER-IMP1"
                           WHEN EC-SIZE
                   >>DEFINE ZZ OFF
                               DISPLAY "INNER-HANDLER"
                           END-PERFORM
                   >>IF ZZ IS DEFINED
                           DISPLAY "BETWEEN-DEFINED"
                   >>END-IF
                       END-PERFORM
            """ + "\n" + After);
        Assert.Contains("\"BETWEEN-DEFINED\"", r.Text, StringComparison.Ordinal);
        Assert.Equal("ZZ-UNDEFINED", r.Branch);
    }

    [Fact]
    public void Define_ReadFromACopybookInTheHandler_IsUndoneAtEndPerform()
    {
        // The copybook's own lines sit in the resultant frame where its COPY statement was spliced in — the driver
        // must place the DEFINE there, not at its line within the copybook (which precedes the PERFORM).
        var r = Compile("""
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                           COPY PB1066CB.
                       END-PERFORM
            """ + "\n" + After,
            copybook: """
                       DISPLAY "CB-1"
                       DISPLAY "CB-2"
                   >>DEFINE ZZ AS 1
                       DISPLAY "CB-3"
            """);
        Assert.Equal("ZZ-UNDEFINED", r.Branch);
    }

    [Fact]
    public void Define_AfterAReplaceStatement_IsPlacedInTheResultantFrame()
    {
        // The REPLACE statement's own three lines vanish from the resultant text, so the DEFINE's resultant line is
        // three less than the driver's output line: unmapped, it would land AFTER the END-PERFORM it precedes.
        var r = Compile("""
                       REPLACE
                           ==NOTE-IT== BY
                           ==DISPLAY==.
                       PERFORM
                           NOTE-IT "IMP1"
                       WHEN EC-SIZE
                           NOTE-IT "HANDLER"
                   >>DEFINE ZZ AS 1
                       END-PERFORM
            """ + "\n" + After);
        Assert.Equal("ZZ-UNDEFINED", r.Branch);
    }

    [Theory]
    [InlineData(false, 0)]   // FLAG-14 EVALUATE ON in the handler: undone at END-PERFORM, nothing flagged after
    [InlineData(true, 1)]    // the control: written in imperative-statement-1, it survives and flags the >>EVALUATE
    public void FrontendInlineFlagOption_FollowsTheSameImplicitPopAll(bool inImperative1, int expectedFlags)
    {
        string flag = "           >>FLAG-14 EVALUATE ON";
        var r = Compile($"""
                       PERFORM
            {(inImperative1 ? flag : "")}
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
            {(inImperative1 ? "" : flag)}
                           DISPLAY "HANDLER"
                       END-PERFORM
                   >>EVALUATE TRUE
                   >>WHEN ZZ IS DEFINED
                       DISPLAY "ZZ-DEFINED"
                   >>WHEN OTHER
                       DISPLAY "ZZ-UNDEFINED"
                   >>END-EVALUATE
            """);
        Assert.Equal(expectedFlags, r.Diagnostics.Count(d => d.Code == DiagnosticCatalog.Flag14Warning.Code
            && d.Message.Contains(">>FLAG-14 EVALUATE", StringComparison.Ordinal)));
    }

    [Fact]
    public void RerunDiagnostics_AreReportedOnce()
    {
        // A handler DEFINE forces the driver to re-run; the FLAG-14 EVALUATE warning on the >>EVALUATE after it is
        // the converged run's, reported once — not once per run.
        var r = Compile("""
                   >>FLAG-14 EVALUATE ON
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                   >>DEFINE ZZ AS 1
                           DISPLAY "HANDLER"
                       END-PERFORM
                   >>EVALUATE TRUE
                   >>WHEN ZZ IS DEFINED
                       DISPLAY "ZZ-DEFINED"
                   >>WHEN OTHER
                       DISPLAY "ZZ-UNDEFINED"
                   >>END-EVALUATE
            """);
        Assert.Equal("ZZ-UNDEFINED", r.Branch);
        Assert.Single(r.Diagnostics, d => d.Code == DiagnosticCatalog.Flag14Warning.Code
            && d.Message.Contains(">>FLAG-14 EVALUATE", StringComparison.Ordinal));
    }

    [Fact]
    public void KeyImplicitOps_KeysToTheNextEncounter_AndDropsBracketsEnclosingNoStateChange()
    {
        // Encounters on lines 2 (a DEFINE), 10 (an >>IF — reads, changes nothing) and 20 (a DEFINE). The first
        // bracket (5..12) encloses only the >>IF, so restoring what it saved changes nothing and it is dropped —
        // which is what lets an ordinary source converge on the first pass; the second (15..25) encloses the
        // DEFINE on line 20 and is kept, its PUSH keyed before encounter 2 and its POP after the last encounter.
        var result = new CobolNet.Frontend.Preprocessor.ConditionalCompilationResult(
            CobolNet.Frontend.Common.MappedText.Identity("", "t"),
            [new(2, true), new(10, false), new(20, true)]);
        var push15 = new CobolNet.Frontend.Preprocessor.DirectiveStackOp(15, CobolNet.Frontend.Preprocessor.DirectiveStackKind.Push, null);
        var pop25 = new CobolNet.Frontend.Preprocessor.DirectiveStackOp(25, CobolNet.Frontend.Preprocessor.DirectiveStackKind.Pop, null);
        var keyed = result.KeyImplicitOps([
            new(5, CobolNet.Frontend.Preprocessor.DirectiveStackKind.Push, null),
            new(12, CobolNet.Frontend.Preprocessor.DirectiveStackKind.Pop, null),
            push15, pop25]);
        Assert.Equal([new(2, push15), new(3, pop25)], keyed);
    }

    // ── fixture ─────────────────────────────────────────────────────────────────────────────────────────────

    private sealed record Result(string Text, string Branch, IReadOnlyList<Diagnostic> Diagnostics);

    /// <summary>Parse <paramref name="body"/> as a 2023 procedure division (with an optional copybook PB1066CB)
    /// and report which of the two <see cref="After"/> branches the conditional-compilation driver kept.</summary>
    private static Result Compile(string body, string? copybook = null)
    {
        string source = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1066T.
                   PROCEDURE DIVISION.
            """ + "\n" + body + "\n" + "           STOP RUN.\n";
        string dir = Path.Combine(Path.GetTempPath(), $"pb1066_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "pb1066.cob");
            File.WriteAllText(path, source);
            if (copybook is not null) File.WriteAllText(Path.Combine(dir, "PB1066CB.cpy"), copybook + "\n");
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            frontend.AddCopySearchPath(dir);
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            string text = tree.GetText();
            bool defined = text.Contains("\"ZZ-DEFINED\"", StringComparison.Ordinal);
            bool undefined = text.Contains("\"ZZ-UNDEFINED\"", StringComparison.Ordinal);
            string branch = defined == undefined ? "NEITHER-OR-BOTH" : defined ? "ZZ-DEFINED" : "ZZ-UNDEFINED";
            return new Result(text, branch, diags.Diagnostics);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { /* best-effort */ } }
    }
}
