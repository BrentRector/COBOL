// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1005 — the ALL form of <c>&gt;&gt;PUSH</c> / <c>&gt;&gt;POP</c> has a POSITION rule:
/// <list type="bullet">
/// <item>ISO §7.3.22.3 SR3 — "If ALL is specified, the PUSH directive shall be specified only in a compilation unit
/// between clauses in divisions other than the procedure division and between statements in the procedure
/// division."</item>
/// <item>ISO §7.3.20.3 SR3 — the same for POP.</item>
/// </list>
/// Both arms (outside a unit; inside a clause or statement) draw the §4.2.2 warning COBOLNET2344, and each legal
/// position is a control that draws nothing — the rule cannot be satisfied by warning everywhere.
/// </summary>
public sealed class PushPopAllPlacementTests
{
    private const string Code = "COBOLNET2344";

    [Theory]
    [InlineData("PUSH", "§7.3.22.3 SR3")]
    [InlineData("POP", "§7.3.20.3 SR3")]
    public void BeforeTheFirstUnit_IsOutsideEveryCompilationUnit(string word, string rule)
    {
        var w = Warnings($"""
                   >>{word} ALL
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1005A.
                   PROCEDURE DIVISION.
                       DISPLAY "X"
                       STOP RUN.
            """);
        Assert.Contains(w, x => x.Contains(Code) && x.Contains("outside every compilation unit") && x.Contains(rule));
    }

    [Fact]
    public void AfterEndProgram_IsOutside_ButAfterTheLastStatementOfAnOpenProgram_IsInside()
    {
        var closed = Warnings("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1005B.
                   PROCEDURE DIVISION.
                       DISPLAY "X"
                       STOP RUN.
                   END PROGRAM PB1005B.
                   >>POP ALL
            """);
        Assert.Contains(closed, x => x.Contains(Code) && x.Contains("outside every compilation unit"));
        var open = Warnings("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1005C.
                   PROCEDURE DIVISION.
                   >>PUSH ALL
                       DISPLAY "X"
                       STOP RUN.
                   >>POP ALL
            """);
        Assert.DoesNotContain(open, x => x.Contains(Code));
    }

    [Fact]
    public void InsideAStatement_Warns_BetweenNestedStatements_DoesNot()
    {
        var w = Warnings("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1005D.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 A PIC 9 VALUE 1.
                   PROCEDURE DIVISION.
                       IF A = 1
                   >>PUSH ALL
                           DISPLAY "IN"
                   >>POP ALL
                       END-IF
                       MOVE A
                   >>PUSH ALL
                           TO A
                   >>POP ALL
                       STOP RUN.
            """);
        var mine = w.Where(x => x.Contains(Code)).ToList();
        Assert.Single(mine);
        Assert.Contains("inside the MOVE statement", mine[0]);
    }

    [Fact]
    public void InsideAClause_Warns_BetweenClauses_DoesNot()
    {
        var w = Warnings("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1005E.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 A PIC 9(3)
                   >>PUSH ALL
                       VALUE 5.
                   >>POP ALL
                   01 B PIC X(3) VALUE
                   >>PUSH ALL
                       "ABC".
                   >>POP ALL
                   PROCEDURE DIVISION.
                       STOP RUN.
            """);
        var mine = w.Where(x => x.Contains(Code)).ToList();
        Assert.Single(mine);
        Assert.Contains("inside a clause (valueClause)", mine[0]);
    }

    [Fact]
    public void NamedForm_HasNoPositionRule()
    {
        // SR3 governs ALL only; a named PUSH/POP answers to SR2 ("where directive-name must not be specified").
        var w = Warnings("""
                   >>PUSH TURN
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1005F.
                   PROCEDURE DIVISION.
                       DISPLAY "X"
                       STOP RUN.
                   >>POP TURN
            """);
        Assert.DoesNotContain(w, x => x.Contains(Code));
    }

    private static IReadOnlyList<string> Warnings(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb1005_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var edition = new EditionContext(2023);
            new CSharpEmitter().Bind(tree!, edition, frontend.Directives);
            return [.. edition.Warnings];
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }
}
