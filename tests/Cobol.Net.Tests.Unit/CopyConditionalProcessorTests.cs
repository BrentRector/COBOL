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
/// The MERGED conditional-compilation + COPY text-manipulation driver (ISO §7.2.1) —
/// <see cref="ConditionalCompilationProcessor.ProcessWithCopy"/>: directives INSIDE copybooks are processed
/// (Step 1 incorporate → Step 2 CC over the expanded group), while a main-source <c>&gt;&gt;IF</c> still gates a
/// COPY and an omitted-branch COPY is never expanded. Design SSOT: <c>docs/rearchitecture/DESIGN-cc-in-copy.md</c>.
/// </summary>
public sealed class CopyConditionalProcessorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_CcCopy_" + Guid.NewGuid().ToString("N")[..8]);

    public CopyConditionalProcessorTests() => Directory.CreateDirectory(_dir);
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

    [Fact] // THE FIX — a copybook >>IF referencing a MAIN >>DEFINE is processed (was a stray >> to the lexer).
    public void CopybookIf_SeesMainDefine_SelectsBranch()
    {
        Copybook("inc.cpy", ">>IF PHASE = 1\nINCLUDED-ONE\n>>ELSE\nINCLUDED-TWO\n>>END-IF\n");
        var (text, diags) = Run(">>DEFINE PHASE AS 1\nCOPY inc.\n");
        Assert.False(diags.HasErrors);
        Assert.Contains("INCLUDED-ONE", text);
        Assert.DoesNotContain("INCLUDED-TWO", text);
        Assert.DoesNotContain(">>IF", text);   // the copybook directive was processed, not leaked
    }

    [Fact] // a copybook >>DEFINE is visible to a LATER main >>IF (Step-2 encounter order over the expanded group).
    public void CopybookDefine_VisibleToLaterMainIf()
    {
        Copybook("def.cpy", ">>DEFINE FEATURE AS 1\n");
        var (text, _) = Run("COPY def.\n>>IF FEATURE = 1\nFEATURE-ON\n>>ELSE\nFEATURE-OFF\n>>END-IF\n");
        Assert.Contains("FEATURE-ON", text);
        Assert.DoesNotContain("FEATURE-OFF", text);
    }

    [Fact] // a main >>IF still gates a COPY — taken branch incorporates the copybook.
    public void MainIf_GatesCopy_Taken()
    {
        Copybook("data.cpy", "COPYBOOK-CONTENT\n");
        var (text, _) = Run(">>DEFINE USEIT AS 1\n>>IF USEIT = 1\nCOPY data.\n>>END-IF\n");
        Assert.Contains("COPYBOOK-CONTENT", text);
    }

    [Fact] // a main >>IF omits a COPY — the copybook is NOT incorporated (omitted-branch COPY not expanded).
    public void MainIf_GatesCopy_Omitted()
    {
        Copybook("data.cpy", "COPYBOOK-CONTENT\n");
        var (text, _) = Run(">>DEFINE USEIT AS 0\n>>IF USEIT = 1\nCOPY data.\n>>END-IF\n");
        Assert.DoesNotContain("COPYBOOK-CONTENT", text);
    }

    [Fact] // a MISSING copybook in a false branch raises NO error (the omitted COPY is never expanded — even strict).
    public void MissingCopybook_InFalseBranch_NoError()
    {
        var (text, diags) = Run(">>DEFINE USEIT AS 0\n>>IF USEIT = 1\nCOPY nonexistent.\n>>END-IF\nMAIN-LINE\n");
        Assert.False(diags.HasErrors);
        Assert.Contains("MAIN-LINE", text);
    }

    [Fact] // a MISSING copybook in a TAKEN branch DOES error under strict (the omission guard is branch-precise).
    public void MissingCopybook_InTakenBranch_Errors()
    {
        var (_, diags) = Run(">>DEFINE USEIT AS 1\n>>IF USEIT = 1\nCOPY nonexistent.\n>>END-IF\n");
        Assert.True(diags.HasErrors);
    }

    [Fact] // a copybook >>TURN survives (leave* discipline) for the downstream TurnDirectiveProcessor stage.
    public void CopybookTurn_SurvivesForDownstreamStage()
    {
        Copybook("ec.cpy", ">>TURN EC-ALL CHECKING ON\nEC-LINE\n");
        var (text, _) = Run("COPY ec.\n");
        Assert.Contains(">>TURN EC-ALL CHECKING ON", text);   // left in place for the post-COPY TURN stage
        Assert.Contains("EC-LINE", text);
    }

    [Fact] // nested COPY: a copybook that COPYs another, whose directive is processed.
    public void NestedCopy_DirectivesProcessed()
    {
        Copybook("outer.cpy", "OUTER-LINE\nCOPY innr.\n");
        Copybook("innr.cpy", ">>IF PHASE = 2\nINNER-TWO\n>>ELSE\nINNER-OTHER\n>>END-IF\n");
        var (text, _) = Run(">>DEFINE PHASE AS 2\nCOPY outer.\n");
        Assert.Contains("OUTER-LINE", text);
        Assert.Contains("INNER-TWO", text);
        Assert.DoesNotContain("INNER-OTHER", text);
    }
}
