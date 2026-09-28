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
/// kb/Work PB892 — <b>an activation written as an OPERAND has the statement it was written in, and one landing
/// gives it that statement.</b> ISO §14.9.18.4 GR1 b) raises a condition an activated element propagates "in the
/// activating runtime element if checking for that exception condition is enabled in the activating runtime
/// element", and §14.9.33.4 GR2 a) 2. makes the applicable statement of a RESUME AT NEXT STATEMENT for it, "for an
/// inline invocation or a function invocation, … the statement in which the inline invocation or function
/// invocation was specified".
///
/// <para>The defect these pins keep closed had two arms on one surface. A per-evaluation function reference (a
/// PERFORM UNTIL, a SEARCH WHEN, an EVALUATE object, a short-circuited operand) was rendered by a SECOND
/// activation text that emitted no propagation pickup, so the registry discarded every condition it
/// propagated; and a HOISTED one's pickup resumed by falling through, back INTO the statement GR2 says to leave
/// (a COMPUTE completed with the function's result). The repair is ONE mechanism: the activation is marked
/// <c>OperandEvaluation</c>, its pickup throws <c>RaiseResumeSignal</c>, and the carrying statement's
/// <c>BoundActivationSite</c> lands it.</para>
/// </summary>
public sealed class OperandActivationDriftTests
{
    private static string CodeGenFile(params string[] parts) =>
        File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", "CodeGen", .. parts]));

    private static string MethodBody(string file, string signaturePrefix)
    {
        var m = Regex.Match(file, Regex.Escape(signaturePrefix) + @"[^\n]*\n    \{(?<b>.*?)\n    \}", RegexOptions.Singleline);
        Assert.True(m.Success, $"'{signaturePrefix}' not found");
        return m.Groups["b"].Value;
    }

    /// <summary>ONE landing decides for every operand-evaluation step (kb/Work PB1432). The fact that a step
    /// evaluates an OPERAND lives on the bound node (<c>BoundStatement.OperandEvaluation</c>, stamped at the two
    /// drain sites), the statement emitter opens the scope around such a step, and <c>DispatchState.ResumeTransfer</c>
    /// unwinds inside it — so no raise site chooses its own landing. Before PB1432 the choice was a per-emitter
    /// <c>Resume(inExpression, …)</c> helper that only activations reached: a §15.4 subscript temporary store's
    /// size-error landing wrote <c>goto __xfer</c> inside a short-circuited operand's lambda (CS0159) and, hoisted,
    /// fell back INTO the statement §14.9.33.4 GR2 a) 1. says to leave.</summary>
    [Fact]
    public void OperandEvaluationLanding_IsDecidedOnceByTheDispatchState()
    {
        string call = CodeGenFile("Verbs", "CallEmitter.cs");
        Assert.DoesNotContain("inExpression", call);
        Assert.DoesNotContain("private string Resume(", call);
        string state = CodeGenFile("EmitterState.cs");
        string resume = Regex.Match(state, @"public string ResumeTransfer\([^\n]*\n(?<b>(?:[^\n]*\n){1,3})").Groups["b"].Value;
        Assert.Contains("InOperandEvaluation", resume);
        Assert.Contains("OperandEvaluationResume(", resume);
        string stmt = CodeGenFile("StatementEmitter.cs");
        Assert.Contains("_dispatchState.EnterOperandEvaluation()", MethodBody(stmt, "internal bool EmitStatement("));
        string ec = CodeGenFile("EcEmitter.cs");
        Assert.DoesNotContain("Func<string, string>? landing", ec);
        foreach (var drain in new[] {
            File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "UdfBinder.cs"])),
            File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "OoBinder.cs"])) })
            Assert.Contains("with { OperandEvaluation = true }", drain);
    }

    /// <summary>A per-evaluation window's activation is the statement emitter's own output — there is no second
    /// activation text for expression position any more.</summary>
    [Fact]
    public void ConditionWindow_RendersActivationsThroughTheStatementEmitter()
    {
        string cond = CodeGenFile("Emit", "ConditionRenderer.cs");
        Assert.DoesNotContain("ProgramRegistry.CallProgram", cond);
        Assert.DoesNotContain("FunctionActivationText", CodeGenFile("Verbs", "CallEmitter.cs"));
        Assert.Matches(@"private string PreOpText\(BoundStatement s\) => ctx\.Writer\.CaptureText\(\(\) => Statements\.EmitStatement\(s\)\);", cond);
    }

    /// <summary>The emitted shape: a function referenced in a PERFORM UNTIL condition activates inside the
    /// condition's lambda WITH the site-handled pickup and the operand-activation throw, the PERFORM carries the
    /// landing, and no <c>goto</c> sits inside the lambda. A hoisted reference in a COMPUTE takes the same landing.
    /// </summary>
    [Fact]
    public void GeneratedCode_OperandActivationsPickUpAndLandAtTheirStatement()
    {
        string cs = Emit("""
                   >>TURN EC-USER-OAZ CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. OADRIFT1.
                   ENVIRONMENT DIVISION.
                   CONFIGURATION SECTION.
                   REPOSITORY.
                       FUNCTION OADRIFTF.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 X PIC 99 VALUE 5.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   HZ SECTION. USE AFTER EXCEPTION CONDITION EC-USER-OAZ.
                   HZ-P.
                       RESUME AT NEXT STATEMENT.
                   END DECLARATIVES.
                   MAIN SECTION.
                   MAIN-P.
                       PERFORM UNTIL FUNCTION OADRIFTF = 7
                           DISPLAY "BODY"
                       END-PERFORM.
                       COMPUTE X = FUNCTION OADRIFTF + 1.
                       STOP RUN.
                   END PROGRAM OADRIFT1.
                   IDENTIFICATION DIVISION.
                   FUNCTION-ID. OADRIFTF.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01 R PIC 9.
                   PROCEDURE DIVISION RETURNING R RAISING EC-USER-OAZ.
                   F-P.
                       MOVE 7 TO R.
                       GOBACK RAISING EXCEPTION EC-USER-OAZ.
                   END FUNCTION OADRIFTF.
            """);
        var lambda = Regex.Match(cs, @"new Func<bool>\(\(\) => \{(?<b>.*?)return ", RegexOptions.Singleline);
        Assert.True(lambda.Success, cs);
        string body = lambda.Groups["b"].Value;
        Assert.Contains("ProgramRegistry.CallProgram(", body);
        Assert.Contains("ExceptionState.TakeRaisedPropagation(", body);
        Assert.Contains("throw new RaiseResumeSignal(", body);
        Assert.DoesNotContain("goto ", body);
        // Two landings — the PERFORM's and the COMPUTE's — and every operand selection throws rather than falls
        // through: per activation, the GOBACK RAISING pickup AND the activation-failure catch (both render through
        // EcEmitter.EmitSelection since kb/Work PB1549, so they share its result-variable spelling) — 2 × 2.
        Assert.Equal(2, Regex.Matches(cs, @"catch \(RaiseResumeSignal __as\d+\)").Count);
        Assert.Equal(4, Regex.Matches(cs, @"== ResumeSignal\.NextStatement\) throw new RaiseResumeSignal\(__r\d+\);").Count);
        Assert.Equal(2, Regex.Matches(cs, @"TakeRaisedPropagation\(").Count);
    }

    /// <summary>kb/Work PB1432 — a §15.4 subscript temporary STORE in a short-circuited operand is an
    /// operand-evaluation step too: its size-error selection throws to the IF's landing from inside the lambda
    /// (it was <c>goto __xfer</c> there, CS0159), and hoisted before a first operand it throws as well rather than
    /// falling back into the IF (§14.9.33.4 GR2 a) 1. and NOTE 1: "transfer would be after the END-IF").</summary>
    [Fact]
    public void GeneratedCode_SubscriptTemporaryStoreLandsAtItsStatement()
    {
        string cs = Emit("""
                   >>TURN EC-ALL CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. OADRIFT4.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 WS-A PIC 9 VALUE 0.
                   01 Z PIC 9 VALUE 0.
                   01 ONE PIC 9 VALUE 1.
                   01 TB.
                      05 EL PIC 9 OCCURS 3 VALUE 1.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   HZ SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE.
                   HZ-P.
                       RESUME AT NEXT STATEMENT.
                   END DECLARATIVES.
                   MAIN SECTION.
                   MAIN-P.
                       IF WS-A = 1 OR EL(FUNCTION INTEGER(ONE / Z)) = 1
                           DISPLAY "T"
                       END-IF.
                       IF EL(FUNCTION INTEGER(ONE / Z)) = 1
                           DISPLAY "T"
                       END-IF.
                       STOP RUN.
            """);
        var lambda = Regex.Match(cs, @"new Func<bool>\(\(\) => \{(?<b>.*?)return ", RegexOptions.Singleline);
        Assert.True(lambda.Success, cs);
        string body = lambda.Groups["b"].Value;
        Assert.Contains("throw new RaiseResumeSignal(", body);
        Assert.DoesNotContain("goto ", body);
        // Both IFs carry a landing, and both stores' selections throw (the lambda's and the hoisted one's).
        Assert.Equal(2, Regex.Matches(cs, @"catch \(RaiseResumeSignal __as\d+\)").Count);
        Assert.Equal(2, Regex.Matches(cs, @"== ResumeSignal\.NextStatement\) throw new RaiseResumeSignal\(__r\d+\);").Count);
    }

    /// <summary>The control: a statement with no operand activation binds no landing.</summary>
    [Fact]
    public void AStatementWithoutAnOperandActivation_BindsNoLanding()
    {
        string cs = Emit("""
                   >>TURN EC-USER-OAN CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. OADRIFT2.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 X PIC 99 VALUE 5.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   HZ SECTION. USE AFTER EXCEPTION CONDITION EC-USER-OAN.
                   HZ-P.
                       RESUME AT NEXT STATEMENT.
                   END DECLARATIVES.
                   MAIN SECTION.
                   MAIN-P.
                       CALL "OADRIFT3".
                       COMPUTE X = X + 1.
                       STOP RUN.
            """);
        Assert.DoesNotContain("catch (RaiseResumeSignal __as", cs);
        Assert.DoesNotContain("throw new RaiseResumeSignal(__pr", cs);
    }

    private static string Emit(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"oadrift_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            // The >>TURN events ride frontend.Directives — without them every assertion passes for the wrong reason.
            return emitter.EmitBound(emitter.Bind(tree!, new EditionContext(2023), frontend.Directives));
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
