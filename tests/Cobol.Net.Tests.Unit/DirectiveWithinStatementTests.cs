// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1384 — ISO §7.3.3 SR8 b): a compiler directive may be specified anywhere in a compilation group EXCEPT
/// "within a source text manipulation statement". The merged driver ends its text block at every directive line, so the
/// only place the violation is visible is there: the text since a COPY or REPLACE keyword has no separator period yet.
/// <c>CopyProcessor.OpenStatementAt</c> is that question, asked once for both statements; every case here runs the real
/// merged driver.
/// </summary>
public sealed class DirectiveWithinStatementTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB1384_" + Guid.NewGuid().ToString("N")[..8]);

    public DirectiveWithinStatementTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ } }

    private (string Text, DiagnosticBag Diags) Run(string mainText)
    {
        File.WriteAllText(Path.Combine(_dir, "cb1.cpy"), " DISPLAY \"CB1\".\n");
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, "t.cob", dialectLevel: 2023, permissive: false);
        string text = ConditionalCompilationProcessor.ProcessWithCopy(mainText, copy,
            CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        return (text, bag);
    }

    [Theory]
    [InlineData(" DISPLAY A.", null)]                                          // no statement at all
    [InlineData(" COPY cb1.", null)]                                           // finished by its period
    [InlineData(" COPY cb1", "COPY")]                                          // open: no period yet
    [InlineData(" COPY", "COPY")]
    [InlineData(" COPY cb1 REPLACING ==A== BY ==B==", "COPY")]                 // open after its pseudo-text
    [InlineData(" COPY cb1 REPLACING ==A. B== BY ==C==.", null)]               // a period INSIDE pseudo-text ends nothing
    [InlineData(" COPY cb1 REPLACING ==A. B== BY ==C. D==", "COPY")]           // ... so this one is still open
    [InlineData(" REPLACE ==A== BY", "REPLACE")]
    [InlineData(" REPLACE ==A== BY ==B==.", null)]
    [InlineData(" REPLACE OFF.", null)]
    [InlineData(" REPLACE ==A== BY ==B==. COPY cb1", "COPY")]                  // the LAST statement decides
    [InlineData(" COPY cb1. REPLACE ==A== BY ==B==", "REPLACE")]
    [InlineData(" DISPLAY \"COPY\".", null)]                                    // a literal is not a keyword
    public void TheStatementTheTextEndsInside_IsNamedByTheDirectiveThatInterruptsIt(string text, string? expected)
    {
        // The text, then a directive line: the driver asks whether the text so far is inside a statement.
        var (_, bag) = Run(" PROCEDURE DIVISION.\n" + text + "\n >>DEFINE VV AS 1\n");
        var found = bag.Diagnostics.Where(x => x.Code == "COBOLNET2697").ToArray();
        if (expected is null) Assert.Empty(found);
        else Assert.Contains(expected + " statement", Assert.Single(found).Message);
    }

    [Fact] // the open statement's text is carried from its KEYWORD, not from the start of the block: a finished statement
           // earlier in the same block does not make a later directive look interrupted
    public void AFinishedStatementEarlierInTheBlock_IsNotCarried()
    {
        var (_, bag) = Run(" PROCEDURE DIVISION.\n COPY cb1.\n DISPLAY A. COPY cb1\n >>DEFINE VV AS 1\n .\n >>DEFINE WW AS 2\n");
        Assert.Equal([3], bag.Diagnostics.Where(x => x.Code == "COBOLNET2697").Select(x => x.Location.Line).ToArray());   // 0-based
    }

    [Theory] // §7.3.3 SR8 b): each is COBOLNET2697, at the directive's own line
    [InlineData(" PROCEDURE DIVISION.\n COPY\n >>DEFINE VV AS 1\n cb1.\n", "COPY")]
    [InlineData(" PROCEDURE DIVISION.\n COPY cb1\n >>DEFINE VV AS 1\n .\n", "COPY")]
    [InlineData(" PROCEDURE DIVISION.\n REPLACE ==AA== BY\n >>DEFINE VV AS 1\n ==BB==.\n DISPLAY \"AA\".\n", "REPLACE")]
    [InlineData(" PROCEDURE DIVISION.\n REPLACE ==AA== BY ==BB==\n >>DEFINE VV AS 1\n .\n", "REPLACE")]
    [InlineData(" PROCEDURE DIVISION.\n REPLACE ==AA== BY\n >>TURN EC-ALL ON\n ==BB==.\n", "REPLACE")]   // a directive a downstream stage keeps
    public void DirectiveInsideAStatement_IsDiagnosedByName(string source, string keyword)
    {
        var (_, bag) = Run(source);
        var d = Assert.Single(bag.Diagnostics, x => x.Code == "COBOLNET2697");
        Assert.Contains(keyword + " statement", d.Message);
        Assert.Equal(2, d.Location.Line);   // 0-based: the directive is the third physical line
    }

    [Fact] // a statement split by TWO directives is reported at each
    public void StatementSplitByTwoDirectives_IsReportedAtEach()
    {
        var (_, bag) = Run(" PROCEDURE DIVISION.\n COPY\n >>DEFINE VV AS 1\n >>DEFINE WW AS 2\n cb1.\n");
        Assert.Equal([2, 3], bag.Diagnostics.Where(x => x.Code == "COBOLNET2697").Select(x => x.Location.Line).ToArray());
    }

    [Theory] // the converse: a directive BETWEEN statements, or after a finished one, is anywhere else a directive may be
    [InlineData(" PROCEDURE DIVISION.\n COPY cb1.\n >>DEFINE VV AS 1\n DISPLAY \"X\".\n")]
    [InlineData(" PROCEDURE DIVISION.\n REPLACE ==AA== BY ==BB==.\n >>DEFINE VV AS 1\n DISPLAY \"AA\".\n")]
    [InlineData(" PROCEDURE DIVISION.\n >>DEFINE VV AS 1\n COPY cb1.\n >>IF VV = 1\n DISPLAY \"X\".\n >>END-IF\n")]
    [InlineData(" PROCEDURE DIVISION.\n DISPLAY\n >>DEFINE VV AS 1\n \"X\".\n")]   // §7.3.3 SR8 allows it inside a DISPLAY statement
    public void DirectiveOutsideAStatement_IsNotDiagnosed(string source)
        => Assert.DoesNotContain(Run(source).Diags.Diagnostics, x => x.Code == "COBOLNET2697");
}
