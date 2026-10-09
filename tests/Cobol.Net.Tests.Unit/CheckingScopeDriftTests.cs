// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB891 + PB841 — <b>the run-time checking flags are a SCOPE, never a set/reset pair.</b> Enablement is a
/// property of the SOURCE TEXT of the executing statement (ISO §7.3.25.4 GR6: checking "is enabled for the
/// procedure division statements and procedure division headers that follow in the compilation group"; GR5: a
/// TURN inside a statement "applies to any succeeding statement … whether or not that succeeding statement is
/// within the scope of the statement in which the TURN directive is specified"), and the
/// <c>ExceptionState.&lt;Flag&gt;</c> bits are only how a raise site deep in the runtime learns it.
///
/// <para>The defect these pins keep closed: every statement guard emitted <c>&lt;Flag&gt; = true; try { … } finally
/// { &lt;Flag&gt; = false; }</c>. A checked statement executed while an ENCLOSING guard stood cleared that guard's
/// enable (an inline-PERFORM's UNTIL stopped raising and the loop ran off the table), an unchecked statement
/// inside it inherited <c>true</c>, and a CALLed program ran with its activator's flags. The repair is ONE
/// discipline realized at every boundary: a guard SAVES and RESTORES; other source statements reached while a
/// guard stands start from ALL-OFF. These tests hold each boundary where it lives, so a new one cannot be added
/// without the scope, and read the behavioural facts off the GENERATED C#.</para>
/// </summary>
public sealed class CheckingScopeDriftTests
{
    private static string CodeGenFile(params string[] parts) =>
        File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", "CodeGen", .. parts]));

    /// <summary>No emitter anywhere writes a flag back to <c>false</c>: "assume it was off" is the PB891 shape.
    /// The only way a guard's flags end is the scope's <c>RestoreChecking</c>.</summary>
    [Fact]
    public void NoEmitter_ResetsACheckingFlagToFalse()
    {
        string dir = Path.GetDirectoryName(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "EcEmitter.cs"))!;
        var offenders = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            // Matches both the literal form and the interpolated one the PB891 code used
            // (`$"ExceptionState.{g.Flag} = false;"` — two hits at the pre-fix tree, measured).
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"ExceptionState\.[^"";]*=\s*false;"))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            "an emitter resets a run-time checking flag to false instead of restoring the saved scope (PB891): "
            + string.Join(", ", offenders));
    }

    /// <summary>A procedure range run from inside a statement (out-of-line PERFORM, SORT/MERGE procedures) goes
    /// through the ONE emitter that opens the baseline scope around it — a new range-running verb that calls
    /// <c>DispatchCall</c> directly would let a standing guard's flags leak into the range.</summary>
    [Fact]
    public void ProcedureRangeCalls_GoThroughEmitProcedureRange()
    {
        string dir = Path.GetDirectoryName(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "EcEmitter.cs"))!;
        var callers = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) is not "EmitterState.cs")
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\.DispatchCall\(").Select(_ => Path.GetFileName(f)))
            .ToList();
        Assert.Equal(["StatementEmitter.cs"], callers);
        var body = Regex.Match(CodeGenFile("StatementEmitter.cs"),
            @"internal void EmitProcedureRange\([^)]*\)\s*\{(?<b>.*?)\n    \}", RegexOptions.Singleline);
        Assert.True(body.Success, "EmitProcedureRange not found in StatementEmitter.cs");
        Assert.Contains("EnterCheckingBaseline()", body.Groups["b"].Value);
    }

    /// <summary>The ACTIVATION boundaries: the CALL / function-activation funnel in the runtime and the generated
    /// method body both open the baseline and restore the activator's state (PB841).</summary>
    [Fact]
    public void ActivationBoundaries_SaveAndRestoreTheCheckingState()
    {
        string table = File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "Control", "ProgramTable.cs"));
        Assert.Contains("var savedChecking = exc.PushAllCheckingOff();", table);
        Assert.Contains("exc.RestoreChecking(savedChecking);", table);

        string oo = CodeGenFile("Verbs", "OoEmitter.cs");
        Assert.Contains("var __ckM = ExceptionState.PushAllCheckingOff();", oo);
        Assert.Contains("ExceptionState.RestoreChecking(__ckM);", oo);

        string dispatch = CodeGenFile("DispatchEmitter.cs");
        Assert.Contains("var __ckU = ExceptionState.PushAllCheckingOff();", dispatch);
        Assert.Contains("ExceptionState.RestoreChecking(__ckU);", dispatch);
    }

    /// <summary>The emitted shape, one program, every statement-level boundary: the guards save/restore, the nested
    /// list and the performed range inside a standing guard are re-based to all-off, and a program whose statements
    /// no flag guard encloses carries no baseline scope at all (the zero-scaffolding control).</summary>
    [Fact]
    public void GeneratedCode_SavesRestoresAndRebasesAtEveryStatementBoundary()
    {
        string cs = Emit("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CKSCOPE1.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 G.
                      05 T PIC 9(2) OCCURS 3 TIMES.
                   01 I PIC 9(2) VALUE 1.
                   01 R PIC 9(2) VALUE 0.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       MOVE 1 TO R.
                   >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
                       PERFORM VARYING I FROM 1 BY 1 UNTIL T (I) > 5
                          MOVE T (1) TO R
                       END-PERFORM
                       PERFORM P VARYING I FROM 1 BY 1 UNTIL T (I) > 5
                       STOP RUN.
                   P.
                       MOVE T (1) TO R.
            """);
        Assert.DoesNotContain("Checking = false", cs);
        Assert.Contains("ExceptionState.SaveChecking();", cs);
        Assert.Contains("ExceptionState.BoundSubscriptChecking = true;", cs);
        // Two re-basings: the inline body (a nested list) and the performed range P.
        Assert.Equal(2, Regex.Matches(cs, @"ExceptionState\.PushAllCheckingOff\(\);   // other source statements").Count);
        // Every scope opened is closed by exactly one restore of its own local.
        var opened = Regex.Matches(cs, @"var (__ck\d+) = ExceptionState\.(SaveChecking|PushAllCheckingOff)\(\);")
            .Select(m => m.Groups[1].Value).ToList();
        Assert.True(opened.Count >= 4, $"expected ≥4 scopes (two guarded PERFORMs, the inline body, the range P):\n{cs}");
        foreach (var local in opened)
            Assert.Single(Regex.Matches(cs, $@"ExceptionState\.RestoreChecking\({local}\);"));
    }

    /// <summary>The control: no TURN, no guard, no scope — an unchecked program's statements are byte-identical.</summary>
    [Fact]
    public void AnUncheckedProgram_EmitsNoCheckingScope()
    {
        string cs = Emit("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CKSCOPE2.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 G.
                      05 T PIC 9(2) OCCURS 3 TIMES.
                   01 I PIC 9(2) VALUE 1.
                   01 R PIC 9(2) VALUE 0.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                          MOVE T (I) TO R
                       END-PERFORM
                       PERFORM P
                       STOP RUN.
                   P.
                       MOVE T (1) TO R.
            """);
        Assert.DoesNotContain("SaveChecking", cs);
        Assert.DoesNotContain("PushAllCheckingOff", cs);
        Assert.DoesNotContain("RestoreChecking", cs);
    }

    private static string Emit(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"ckscope_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            // The >>TURN events ride frontend.Directives — without them every assertion passes for the wrong reason.
            return emitter.EmitBound(emitter.Bind(tree!, new EditionContext(2023), frontend.Directives), "DRIFTPROBE");
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
