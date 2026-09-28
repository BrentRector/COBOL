// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The witness for Annex A.1 item 89 — "Function-identifier (object time resources that are checked)" — and the
/// determination <c>docs/CONFORMANCE.md</c> DOC-A.1-89 records for it. ISO/IEC 1989:2023 §8.4.3.2.4 6) (cite.py OK):
/// b) "the runtime system attempts to locate the function being activated ... If the function is not found, the
/// EC-FUNCTION-NOT-FOUND exception condition is set to exist, the function is not activated, and execution continues
/// as specified in General rule 6f"; c) "If the function is located but the resources necessary to execute the
/// function are not available, the EC-PROGRAM-RESOURCES exception condition is set to exist ... The runtime resources
/// that are checked in order to determine the availability of the function for execution are defined by the
/// implementor."
/// <para>DETERMINATION PINNED: the checked set is EMPTY. A function is located when a module registering it has been
/// loaded and its <c>__CobolModule.Register()</c> has run; a <c>&lt;name&gt;.dll</c> beside the application that
/// cannot be loaded (here a foreign, non-PE image) has therefore NOT been located, so the condition is
/// EC-FUNCTION-NOT-FOUND and never EC-PROGRAM-RESOURCES. Both names are enabled and both are named in the USE
/// statement, so whichever the runtime sets is the one EXCEPTION-STATUS reports (§15.33.3 1)), and RESUME AT NEXT
/// STATEMENT (§14.9.33.4 2) a)) lets the run continue past the fatal condition.</para>
/// <para>The control case compiles a REAL function module of the same shape into the same directory and shows the
/// sibling-module lookup locates and runs it, so the foreign-image result is caused by the image, not by a lookup
/// that never looks. A third case covers a module that loads but whose registrar throws: also not located.</para>
/// </summary>
public sealed class FunctionLocateResourceWitnessTests
{
    private static void CompileTo(string source, string dir, string name)
    {
        string src = Path.Combine(dir, name + ".cob");
        src = CompiledProgramCache.StageSource(src, source);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r.Success, $"compile {name}: {string.Join("; ", r.Errors)}");
    }

    /// <summary>A caller of <paramref name="fn"/> through its prototype; the prototype registers no function, so the
    /// function can only come from a sibling module.</summary>
    private static string Caller(string id, string fn) => $"""
        >>TURN EC-FUNCTION-NOT-FOUND EC-PROGRAM-RESOURCES CHECKING ON
        IDENTIFICATION DIVISION.
        FUNCTION-ID. {fn} IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-A PIC 9(4).
        01 L-R PIC 9(4).
        PROCEDURE DIVISION USING L-A RETURNING L-R.
        END FUNCTION {fn}.

        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            FUNCTION {fn}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-X PIC 9(4) VALUE 5.
        01 W-R PIC 9(4) VALUE 0.
        PROCEDURE DIVISION.
        DECLARATIVES.
        EC-H SECTION.
            USE AFTER EXCEPTION CONDITION EC-FUNCTION-NOT-FOUND EC-PROGRAM-RESOURCES.
        EC-H-P.
            DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
            RESUME AT NEXT STATEMENT.
        END DECLARATIVES.
        MAIN SECTION.
        MAIN-PARA.
            MOVE FUNCTION {fn}(W-X) TO W-R.
            DISPLAY "R=" W-R.
            STOP RUN.
        END PROGRAM {id}.
        """;

    [Fact]
    public void UnloadableSiblingModule_IsNotFound_NeverProgramResources()
    {
        string dir = CutRunner.NewTempDir("l1frs1");
        try
        {
            CompileTo(Caller("L1FRSMA", "L1FRSZ"), dir, "L1FRSMA");
            // A file with the function's module name that is not a loadable assembly (a foreign image).
            File.WriteAllText(Path.Combine(dir, "L1FRSZ.dll"), "THIS IS NOT A PE IMAGE");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "L1FRSMA.dll"), dir);
            // GR6 b) + 6 f): not located -> EC-FUNCTION-NOT-FOUND, the declarative runs, RESUME continues; W-R is
            // untouched (the function was not activated) so the next line shows its VALUE 0.
            Assert.Equal("H: EC-FUNCTION-NOT-FOUND\nR=0000", stdout);
            Assert.Equal(0, exit);
            Assert.DoesNotContain("EC-PROGRAM-RESOURCES", stdout + stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    [Fact]
    public void LoadableSiblingModule_IsLocatedAndRuns()
    {
        const string function = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. L1FRSY.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-A PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-A RETURNING L-R.
                COMPUTE L-R = L-A * 2.
                GOBACK.
            END FUNCTION L1FRSY.
            """;
        string dir = CutRunner.NewTempDir("l1frs2");
        try
        {
            CompileTo(Caller("L1FRSMB", "L1FRSY"), dir, "L1FRSMB");
            CompileTo(function, dir, "L1FRSY");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "L1FRSMB.dll"), dir);
            // GR6 d): located and activated; 5 * 2 = 10 into PIC 9(4) -> "0010"; no handler line.
            Assert.Equal("R=0010", stdout);
            Assert.Equal(0, exit);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>A sibling module that LOADS but whose registrar throws. DOC-A.1-89 lists it apart from the foreign
    /// image: "A module file that exists but cannot be loaded (a truncated or foreign image ...) or whose registrar
    /// throws has therefore NOT been located: EC-FUNCTION-NOT-FOUND". This is the case the rejected reading ("a
    /// module found on disk is located") would most naturally call EC-PROGRAM-RESOURCES, because the image loads.
    /// The library is built with Roslyn, the same way <c>NonCobolActivatorReturnTests.CompileHost</c> builds its host,
    /// and holds only a global-namespace <c>__CobolModule.Register()</c> that throws.</summary>
    [Fact]
    public void ThrowingRegistrarSiblingModule_IsNotFound_NeverProgramResources()
    {
        const string library = """
            public static class __CobolModule
            {
                public static void Register() => throw new System.InvalidOperationException("registrar fails");
            }
            """;
        string dir = CutRunner.NewTempDir("l1frs3");
        try
        {
            CompileTo(Caller("L1FRSMC", "L1FRSX"), dir, "L1FRSMC");
            var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
            var refs = tpa
                .Where(p => Path.GetFileNameWithoutExtension(p) is "System.Private.CoreLib" or "System.Runtime")
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                .ToList();
            var compilation = CSharpCompilation.Create("L1FRSX",
                [CSharpSyntaxTree.ParseText(library)], refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var emit = compilation.Emit(Path.Combine(dir, "L1FRSX.dll"));
            Assert.True(emit.Success, string.Join("\n", emit.Diagnostics));
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "L1FRSMC.dll"), dir);
            // Not located -> EC-FUNCTION-NOT-FOUND (GR6 b)), the declarative runs, RESUME continues; W-R keeps VALUE 0.
            Assert.Equal("H: EC-FUNCTION-NOT-FOUND\nR=0000", stdout);
            Assert.Equal(0, exit);
            Assert.DoesNotContain("EC-PROGRAM-RESOURCES", stdout + stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
