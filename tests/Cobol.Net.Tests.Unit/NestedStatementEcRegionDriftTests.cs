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
/// kb/Work PB441 — <b>a nested source statement list is an EC REGION BOUNDARY.</b> Enablement is keyed on the
/// statement's own position in the compilation group (ISO §7.3.25.4 GR6: "checking … is enabled for the procedure
/// division statements and procedure division headers that follow in the compilation group"), so the region an
/// emitter consults must be the region of the statement it is guarding — never the enclosing statement's.
///
/// <para><b>What leaving it ambient cost.</b> §14.9.28.4 GR14 assumes "an implicit PUSH ALL followed by TURN OFF
/// ALL … at the end of imperative-statement-1" and the matching POP "immediately preceding the END PERFORM
/// phrase", and GR16 places imperative-statement-5 (FINALLY) inside that window. The binder's GR14 floor
/// (<c>TurnState.WithAllDisabledFrom</c>) bound the FINALLY body with EC-ALL off, and the run-time
/// <c>ExceptionState.PushAllCheckingOff</c> wrapped it — yet an ADD in FINALLY still terminated the run unit,
/// because the EC-SIZE guard is decided at EMIT time from the ambient <c>EcState.Info</c> and imp-5 is emitted
/// INLINE inside the PERFORM's own region. The WHEN bodies escaped only because they are emitted as separate pc
/// cases. Two-arm dispatch: one window, two realizations, and the compiled-in gate was inside neither.</para>
///
/// <para>These pins hold the boundary where it now lives — <see cref="EcEmitter.EnterNestedStatements"/>, opened
/// by <c>StatementEmitter.EmitStatementList</c>, the ONE funnel every nested source statement list goes through —
/// and measure BOTH arms of GR14's window side by side plus a NON-F3 arm, so the rule cannot narrow back to the
/// statement that happened to be measured. Every behavioural fact is read off the GENERATED C#.</para>
/// </summary>
public sealed class NestedStatementEcRegionDriftTests
{
    /// <summary>The seam itself: the nested-list funnel opens the region scope. A refactor that emits a nested
    /// list without it silently restores the PB441 shape for every emit-time-gated family at once.</summary>
    [Fact]
    public void EmitStatementList_OpensTheNestedRegionScope()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "StatementEmitter.cs"));
        var body = Regex.Match(src, @"internal bool EmitStatementList\([^)]*\)\s*\{(?<b>.*?)\n    \}",
            RegexOptions.Singleline);
        Assert.True(body.Success, "EmitStatementList not found in StatementEmitter.cs");
        Assert.Contains("_ecEmit.EnterNestedStatements()", body.Groups["b"].Value);
        // And the scope actually clears the region (not merely saves it).
        string ec = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "EcEmitter.cs"));
        Assert.Contains("_saved = state.Info; state.Info = null;", ec);
    }

    private const string Gr14Src = """
               >>TURN EC-SIZE-TRUNCATION CHECKING ON
               IDENTIFICATION DIVISION.
               PROGRAM-ID. ECREGIONF3.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 N PIC 9 VALUE 5.
               PROCEDURE DIVISION.
               MAIN-PARA.
                   PERFORM
                       RAISE EXCEPTION EC-USER-DEMO
                   WHEN EC-USER-DEMO
                       ADD 9 TO N
                   FINALLY
                       ADD 9 TO N
                   END-PERFORM
                   ADD 9 TO N
                   STOP RUN.
        """;

    /// <summary>GR14's window, both arms, one program: the ADD in imperative-statement-2 (WHEN) and the ADD in
    /// imperative-statement-5 (FINALLY) both compile to a BARE store, while the ADD written AFTER END-PERFORM —
    /// outside the window, same statement, same line-enabled family — compiles to the full EC-SIZE guard. The
    /// control is the load-bearing half: without it a fix that simply stopped emitting the guard everywhere would
    /// pass.</summary>
    [Fact]
    public void Gr14Window_LeavesNoCompiledInSizeGuard_InEitherHandlerOrFinally()
    {
        string cs = Emit(Gr14Src);

        // CONTROL — the statement after END-PERFORM is guarded (the family IS emit-time gated in this program).
        Assert.Contains("__sizeErr", cs);

        // imp-5: between the FINALLY label and the end-of-PERFORM label.
        string finallyBody = Slice(cs, "__f3fin", "__f3end");
        Assert.DoesNotContain("__sizeErr", finallyBody);
        Assert.Contains("CobolNum.Store(", finallyBody);   // the bare store is there — the ADD was not dropped

        // imp-2: the synthetic handler pc case.
        var handler = Regex.Match(cs, @"case \d+:   // \(exception-checking PERFORM handler\)\s*\{(?<b>.*?)\n\s*\}",
            RegexOptions.Singleline);
        Assert.True(handler.Success, $"no generated F3 handler case in:\n{cs}");
        Assert.DoesNotContain("__sizeErr", handler.Groups["b"].Value);
        Assert.Contains("CobolNum.Store(", handler.Groups["b"].Value);
    }

    /// <summary>The boundary is NOT a Format-3 special case. A <c>&gt;&gt;TURN … CHECKING OFF</c> written inside an
    /// IF branch disables checking for the statements that follow it (§7.3.25.4 GR8), and the ADD after it must
    /// compile to a bare store although the enclosing IF's own line still has the family enabled.</summary>
    [Fact]
    public void ANestedBranch_BindsItsOwnRegion_NotTheEnclosingStatements()
    {
        string cs = Emit("""
                   >>TURN EC-SIZE-TRUNCATION CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. ECREGIONIF.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 N PIC 9 VALUE 5.
                   PROCEDURE DIVISION.
                   MAIN-PARA.
                       IF N = 5
                   >>TURN EC-SIZE-TRUNCATION CHECKING OFF
                           ADD 9 TO N
                       END-IF
                       STOP RUN.
            """);
        Assert.DoesNotContain("__sizeErr", cs);
        Assert.Contains("CobolNum.Store(", cs);
    }

    private static string Slice(string text, string from, string to)
    {
        int a = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(a >= 0, $"'{from}' not found in:\n{text}");
        int b = text.IndexOf(to, a, StringComparison.Ordinal);
        Assert.True(b > a, $"'{to}' not found after '{from}' in:\n{text}");
        return text[a..b];
    }

    /// <summary>Compile one fixture to C# at COBOL-2023 (Format 3 is 2023-only, Annex E.3.3 item 36).</summary>
    private static string Emit(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"ecregion_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            // ⛔ The >>TURN events ride frontend.Directives — without them the group binds under the §7.3.25.4
            // GR1 default (EC-ALL CHECKING OFF) and every assertion below passes for the wrong reason.
            return emitter.EmitBound(emitter.Bind(tree!, new EditionContext(2023), frontend.Directives), "DRIFTPROBE");
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
