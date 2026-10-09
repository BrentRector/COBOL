// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1010 — <b>a METHOD is its own declarative-selection scope.</b> ISO §14.2.2 SR10 admits the declaratives
/// format in a method definition, and §14.9.49.4 GR3/GR4 a) select over the USE statements of the source element
/// that contains the raising statement: the method. The emitter realizes that with ONE machinery emitter
/// (<c>DispatchEmitter.EmitUseMachinery</c>) called <c>asLocal</c> from <c>OoEmitter.EmitMethod</c>, so every
/// selection member becomes a LOCAL FUNCTION of the method that declares the USE procedures, and a method activation
/// installs its own runtime-site selector (<c>NonfatalSelectorFn</c>) and restores its invoker's.
/// <para>What is measured, over the generated C# (so a selection member added to the machinery later is covered
/// without editing this test): (1) no selection member is declared as a CLASS member of a COBOL class — a
/// class-level <c>__EcDispatch</c>/<c>__RunUse</c>/<c>__IoCheck</c>/<c>__EcObjDispatch</c> could only select one
/// method's declaratives for every method; (2) each is declared exactly once, inside the method that owns the
/// USE statements; (3) the method with declaratives installs a selector over them, the sibling without installs
/// <c>NonfatalSelectorFn.None</c> (its runtime-site raise must not select its INVOKER's declaratives), and every
/// install is paired with the restore.</para>
/// </summary>
public sealed class MethodSelectionScopeDriftTests
{
    private const string Src = """
              >>TURN EC-SIZE CHECKING ON
               IDENTIFICATION DIVISION.
               CLASS-ID. MSSEX.
               OBJECT.
               END OBJECT.
               END CLASS MSSEX.
               IDENTIFICATION DIVISION.
               CLASS-ID. MSSC.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   CLASS MSSEX.
               OBJECT.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F ASSIGN TO "mss.dat" ORGANIZATION IS SEQUENTIAL.
               DATA DIVISION.
               FILE SECTION.
               FD F.
               01 FR PIC X(4).
               PROCEDURE DIVISION.
               METHOD-ID. OWNS.
               DATA DIVISION.
               LOCAL-STORAGE SECTION.
               01 N PIC 9 VALUE 0.
               01 X USAGE OBJECT REFERENCE MSSEX.
               PROCEDURE DIVISION.
               DECLARATIVES.
               D1 SECTION.
                   USE AFTER STANDARD ERROR PROCEDURE ON F.
               D2 SECTION.
                   USE AFTER EXCEPTION CONDITION EC-SIZE.
               D2-P.
                   RESUME AT NEXT STATEMENT.
               D3 SECTION.
                   USE AFTER EXCEPTION OBJECT MSSEX.
               END DECLARATIVES.
               MAIN SECTION.
               M-P.
                   OPEN INPUT F
                   COMPUTE N = 50 * 3
                   RAISE X.
               END METHOD OWNS.
               METHOD-ID. SIBLING.
               DATA DIVISION.
               LOCAL-STORAGE SECTION.
               01 S PIC 9 VALUE 0.
               PROCEDURE DIVISION.
                   COMPUTE S = 50 * 3.
               END METHOD SIBLING.
               END OBJECT.
               END CLASS MSSC.
        """;

    private static readonly string[] SelectionMembers = ["__RunUse", "__EcDispatch", "__IoCheck", "__EcObjDispatch"];

    private static string Emit(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), $"mssdrift_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2002 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            var bound = emitter.Bind(tree!, new EditionContext(2002));
            return emitter.EmitBound(bound, "DRIFTPROBE");
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }

    /// <summary>The text of one emitted method, from its header to the header of the next method (or the end).</summary>
    private static string MethodBody(string cs, string cobolName)
    {
        int at = cs.IndexOf($"// METHOD-ID {cobolName} ", StringComparison.Ordinal);
        Assert.True(at >= 0, $"method {cobolName} was not emitted");
        int next = cs.IndexOf("// METHOD-ID ", at + 1, StringComparison.Ordinal);
        return next < 0 ? cs[at..] : cs[at..next];
    }

    [Fact]
    public void EachSelectionMemberIsLocalToTheMethodThatDeclaresTheUseProcedures()
    {
        string cs = Emit(Src);
        string owns = MethodBody(cs, "OWNS");
        string sibling = MethodBody(cs, "SIBLING");
        foreach (string member in SelectionMembers)
        {
            var decl = new Regex($@"^\s*(private\s+)?(int|void)\s+{member}\(", RegexOptions.Multiline);
            Assert.DoesNotMatch(new Regex($@"^\s*private\s+(int|void)\s+{member}\(", RegexOptions.Multiline), cs);
            Assert.Single(decl.Matches(owns));
            Assert.Empty(decl.Matches(sibling));
        }
    }

    [Fact]
    public void EveryMethodActivationInstallsItsOwnRuntimeSelectorAndRestoresTheInvokers()
    {
        string cs = Emit(Src);
        Assert.Contains("ExceptionState.NonfatalDispatcher = new NonfatalSelectorFn(", MethodBody(cs, "OWNS"));
        Assert.Contains("ExceptionState.NonfatalDispatcher = NonfatalSelectorFn.None;", MethodBody(cs, "SIBLING"));
        int saves = Regex.Matches(cs, @"var __nfM = ExceptionState\.NonfatalDispatcher;").Count;
        int restores = Regex.Matches(cs, @"ExceptionState\.NonfatalDispatcher = __nfM;").Count;
        Assert.Equal(2, saves);
        Assert.Equal(saves, restores);
    }
}
