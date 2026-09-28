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
/// kb/Work PB1352 — a compiler directive line inside the text that COPY REPLACING and REPLACE act on. §7.3.4 1): "A
/// compiler directive line is not affected by the replacing action of a COPY statement or a REPLACE statement";
/// §7.2.3.4 9) c) 5. / §7.2.4.4 8) c) 5.: "Each occurrence of a compiler directive line is treated as a single
/// space". Library text is replaced before its own <c>&gt;&gt;DEFINE</c> / <c>&gt;&gt;IF</c> lines are processed,
/// and REPLACE runs before the stages that read <c>&gt;&gt;TURN</c> / <c>&gt;&gt;PROPAGATE</c> / <c>&gt;&gt;PAGE</c>,
/// so both passes meet directive lines. Every case runs the real merged driver.
/// </summary>
public sealed class TextManipulationDirectiveLineTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB1352_" + Guid.NewGuid().ToString("N")[..8]);

    public TextManipulationDirectiveLineTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ } }

    private void Copybook(string name, string content) => File.WriteAllText(Path.Combine(_dir, name), content);

    private (string Text, DiagnosticBag Diags) Run(string mainText)
    {
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, "t.cob", dialectLevel: 2023, permissive: false);
        string text = ConditionalCompilationProcessor.ProcessWithCopy(mainText, copy,
            CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        return (text, bag);
    }

    private static void Clean(DiagnosticBag bag) => Assert.False(bag.HasErrors, string.Join("\n", bag.Diagnostics));

    [Fact] // §7.3.4 1): REPLACING ==AS 1== does not rewrite the copybook's own >>DEFINE, so its >>IF stays true.
    public void CopyReplacing_DoesNotRewriteTheCopybooksDefine()
    {
        Copybook("cbdef.cpy", " >>DEFINE VV AS 1\n >>IF VV = 1\n DISPLAY \"TRUE-ARM\"\n >>ELSE\n DISPLAY \"FALSE-ARM\"\n >>END-IF\n");
        var (text, bag) = Run(" PROCEDURE DIVISION.\n COPY cbdef REPLACING ==AS 1== BY ==AS 3==.\n");
        Clean(bag);
        Assert.Contains("TRUE-ARM", text);
        Assert.DoesNotContain("FALSE-ARM", text);
    }

    [Fact] // §7.3.16.4 1): the >>IF line's condition is not a text-word sequence REPLACING can reach.
    public void CopyReplacing_DoesNotRewriteAnIfCondition()
    {
        Copybook("cbif.cpy", " >>IF V = 1\n DISPLAY \"V-TRUE\"\n >>ELSE\n DISPLAY \"V-FALSE\"\n >>END-IF\n");
        var (text, bag) = Run(" >>DEFINE V AS 1\n PROCEDURE DIVISION.\n COPY cbif REPLACING ==V== BY ==Q==.\n");
        Clean(bag);   // a rewritten condition names the undefined Q (COBOLNET1619)
        Assert.Contains("V-TRUE", text);
    }

    [Fact] // c) 5.: the directive line is one space, so ==DV-A PIC== matches across it; the line itself survives.
    public void Matching_RunsAcrossADirectiveLine_AndKeepsIt()
    {
        var (text, bag) = Run(" REPLACE ==SHOW-IT NOW== BY ==DISPLAY W1==.\n PROCEDURE DIVISION.\n SHOW-IT\n"
            + " >>TURN EC-ALL CHECKING OFF\n NOW\n");
        Clean(bag);
        Assert.Contains("DISPLAY W1", text);
        Assert.DoesNotContain("SHOW-IT", text);
        Assert.Contains(">>TURN EC-ALL CHECKING OFF", text);
        // The directive stays alone on its line (§7.3.3 SR2) after the replacement.
        Assert.Contains(text.Split('\n'), l => l.Trim() == ">>TURN EC-ALL CHECKING OFF");
    }

    [Fact] // §7.3.4 1): a word inside a directive line left for a later stage is never a REPLACE target.
    public void Replace_DoesNotRewriteALaterStagesDirective()
    {
        var (text, bag) = Run(" REPLACE ==CHECKING== BY ==CHEKING==.\n PROCEDURE DIVISION.\n >>TURN EC-ALL CHECKING OFF\n");
        Clean(bag);
        Assert.Contains(">>TURN EC-ALL CHECKING OFF", text);
        Assert.DoesNotContain("CHEKING", text);
    }

    [Fact] // §7.3.19.4 1) + §7.2.3.4 10): COPY inside >>PAGE comment-text is documentation, not a COPY statement.
    public void CopyInPageCommentText_IsNotACopyStatement()
    {
        Copybook("cbpage.cpy", " >>PAGE COPY FOO.\n 01 PG-A PIC X.\n");
        var (text, bag) = Run(" WORKING-STORAGE SECTION.\n COPY cbpage REPLACING ==PG-A== BY ==PG-B==.\n");
        Clean(bag);
        Assert.Contains("PG-B", text);
    }

    [Fact] // §7.3.3 SR2: a '>>' that is not preceded only by spaces heads no directive line — it stays text-words.
    public void DoubleAngleAfterText_IsNotADirectiveLine()
    {
        var (text, bag) = Run(" REPLACE ==>>PAGE== BY ==ZZ==.\n PROCEDURE DIVISION.\n DISPLAY A >>PAGE\n");
        Clean(bag);
        Assert.Contains("DISPLAY A ZZ", text);
    }
}
