// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB2097 — slice 1 of the §8.13 external repository (docs/rearchitecture/DESIGN-external-repository.md §4.5,
/// §11.2, §11.3), run as separately compiled modules beside each other, the way a run unit composed of several runtime
/// modules is deployed (ISO/IEC 1989:2023 §14.6.1, cite.py OK: "A run unit contains one or more runtime modules").
/// <list type="bullet">
///   <item>Every emitted type lives in the module's namespace <c>Cobol.&lt;S&gt;</c> and every foreign name is
///     <c>global::</c>-rooted, so modules whose assembly names are the namespaces the generated code uses
///     (<c>Cobol</c>, <c>CobolNet</c>) or a runtime type's name (<c>CobolNum</c>) compile and call each other.</item>
///   <item>A sibling module that defines an outermost program another module already registered is refused WHOLE by
///     <c>ProgramTable.RegisterModule</c>: §8.3.2.2 2) (cite.py OK) "when two or more source elements identify something
///     with the same externalized name, they refer to the same instance", so the second definition is not registered and
///     the CALL that needed the module is §14.9.4.4 GR3 b)'s EC-PROGRAM-NOT-FOUND ("If the program cannot be located",
///     cite.py OK), whose message names both modules.</item>
///   <item>A sibling module compiled against another runtime major is refused by the probe with that reason.</item>
/// </list>
/// </summary>
public sealed class ModuleRegistrationHostTests
{
    private static string Callee(string id, int add) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-N PIC 9(4).
        PROCEDURE DIVISION USING L-N.
            ADD {add} TO L-N
            GOBACK.
        END PROGRAM {id}.
        """;

    [Fact]
    public void ModulesNamedLikeTheNamespacesTheGeneratedCodeUses_CompileAndCallEachOther()
    {
        const string main = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P2097NM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-N PIC 9(4) VALUE 7.
            PROCEDURE DIVISION.
                CALL "COBOLNET" USING W-N
                DISPLAY "AFTER COBOLNET " W-N
                CALL "COBOLNUM" USING W-N
                DISPLAY "AFTER COBOLNUM " W-N
                STOP RUN.
            END PROGRAM P2097NM.
            """;
        string dir = CutRunner.NewTempDir("pb2097ns");
        try
        {
            // The sibling probe finds a program's module by the program's name (COBOLNET -> CobolNet.dll).
            NonCobolElement.CompileCobol(dir, "CobolNet", Callee("COBOLNET", 10));
            NonCobolElement.CompileCobol(dir, "CobolNum", Callee("COBOLNUM", 100));
            NonCobolElement.CompileCobol(dir, "Cobol", main);
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "Cobol.dll"), dir);
            Assert.True(exit == 0, detail);
            Assert.Equal("AFTER COBOLNET 0017\nAFTER COBOLNUM 0117", stdout);   // 7 + 10, then + 100
        }
        finally { CutRunner.TryDelete(dir); }
    }

    [Fact]
    public void ASiblingModuleRedefiningARegisteredProgram_IsRefusedWhole_AndTheCallIsNotFound()
    {
        // The main module defines P2097DM and P2097DX; the sibling module P2097DB defines P2097DB and P2097DX again.
        string main = """
            >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P2097DM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-N PIC 9(4) VALUE 1.
            PROCEDURE DIVISION.
                CALL "P2097DX" USING W-N
                DISPLAY "OWN " W-N
                CALL "P2097DB" USING W-N
                DISPLAY "NOT REACHED " W-N
                STOP RUN.
            END PROGRAM P2097DM.
            """ + "\n" + Callee("P2097DX", 1);
        string sibling = Callee("P2097DB", 1000) + "\n" + Callee("P2097DX", 500);
        string dir = CutRunner.NewTempDir("pb2097dup");
        try
        {
            NonCobolElement.CompileCobol(dir, "P2097DB", sibling);
            NonCobolElement.CompileCobol(dir, "P2097DM", main);
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "P2097DM.dll"), dir);
            Assert.Equal("OWN 0002", stdout);   // the main module's own P2097DX; the refused module ran nothing
            Assert.NotEqual(0, exit);           // EC-PROGRAM-NOT-FOUND, checked, unhandled: the run unit ends abnormally
            Assert.Contains("EC-PROGRAM-NOT-FOUND", detail, StringComparison.Ordinal);
            Assert.Contains("P2097DX", detail, StringComparison.Ordinal);
            Assert.Contains("Cobol.P2097DM.__CobolModule", detail, StringComparison.Ordinal);
            Assert.Contains("Cobol.P2097DB.__CobolModule", detail, StringComparison.Ordinal);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    [Fact]
    public void ASiblingModuleOfAnotherRuntimeMajor_IsNotFound_WithTheReason()
    {
        const string main = """
            >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P2097VM.
            PROCEDURE DIVISION.
                CALL "P2097VS"
                STOP RUN.
            END PROGRAM P2097VM.
            """;
        string other = $"{CobolNet.Runtime.RuntimeAbi.Version.Major + 1}.0.0.0";
        // A module that records a runtime major this runtime is not: the probe refuses it before invoking its registrar.
        string library = $$"""
            [assembly: CobolNet.Runtime.Repository.CobolRepository({{CobolNet.Runtime.Repository.CobolRepositoryAttribute.CurrentSchemaVersion}}, "{{other}}", {{CobolNet.Runtime.RuntimeAbi.CallAbi}}, Registrar = "Cobol.P2097VS.__CobolModule")]
            namespace Cobol.P2097VS
            {
                public static class __CobolModule
                {
                    public static void EnsureRegistered() => throw new System.InvalidOperationException("the registrar of a refused module ran");
                }
            }
            """;
        string dir = CutRunner.NewTempDir("pb2097ver");
        try
        {
            NonCobolElement.CompileCobol(dir, "P2097VM", main);
            NonCobolElement.CompileCSharp(dir, "P2097VS", library, runtimeConfigOf: null);
            var (exit, _, detail) = CutRunner.RunExit(Path.Combine(dir, "P2097VM.dll"), dir);
            Assert.NotEqual(0, exit);
            Assert.Contains("EC-PROGRAM-NOT-FOUND", detail, StringComparison.Ordinal);
            Assert.Contains(other, detail, StringComparison.Ordinal);
            Assert.Contains("major", detail, StringComparison.Ordinal);
            Assert.DoesNotContain("the registrar of a refused module ran", detail, StringComparison.Ordinal);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>kb/Work PB2839: modules <c>P2839-R</c> and <c>P2839_R</c> are two assemblies and two namespaces, because the
    /// identifier derived from a name is injective (<c>DataItem.IdentifierCharacters</c>). The old mapping wrote both
    /// <c>Cobol.P2839_R</c>, so the second module's registrar was the first's, <c>RegisterModule</c> returned at once for
    /// it, and <c>CALL "P2839_R"</c> answered EC-PROGRAM-NOT-FOUND ("does not carry the program").</summary>
    [Fact]
    public void ModulesWhoseNamesDifferOnlyInHyphenAndUnderscore_AreTwoModules()
    {
        const string main = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P2839M.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-N PIC 9(4) VALUE 7.
            PROCEDURE DIVISION.
                CALL "P2839-R" USING W-N
                DISPLAY "AFTER HYPHEN " W-N
                CALL "P2839_R" USING W-N
                DISPLAY "AFTER UNDERSCORE " W-N
                STOP RUN.
            END PROGRAM P2839M.
            """;
        string dir = CutRunner.NewTempDir("pb2839");
        try
        {
            NonCobolElement.CompileCobol(dir, "P2839-R", Callee("P2839-R", 10));
            NonCobolElement.CompileCobol(dir, "P2839_R", Callee("P2839_R", 100));
            NonCobolElement.CompileCobol(dir, "P2839M", main);
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "P2839M.dll"), dir);
            Assert.True(exit == 0, detail);
            Assert.Equal("AFTER HYPHEN 0017\nAFTER UNDERSCORE 0117", stdout);   // 7 + 10, then + 100
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>kb/Work PB2843: two modules each DECLARE an outermost <c>P2843W</c>, the sibling's <c>AS "P2843X"</c>
    /// (legal: §8.3.2.2 2) externalizes the AS literal, so the two externalized names differ). Each outermost program's
    /// run-unit path is rooted at its externalized name (<c>ProgramTable.OutermostPath</c>), so loading the sibling
    /// leaves the main module's node, and its containee, in place: §8.4.6.3 1) (cite.py OK) "that program-name may be
    /// referenced only by statements included in that containing program", and P2843W contains P2843H. With a
    /// declared-name root the sibling's node replaced the main module's, and <c>CALL "P2843H"</c> was not found.</summary>
    [Fact]
    public void ASiblingModuleDeclaringTheSameProgramNameAsAnotherName_LeavesTheFirstModulesContaineesCallable()
    {
        const string main = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P2843W.
            PROCEDURE DIVISION.
                CALL "P2843X" ON EXCEPTION DISPLAY "X NOT FOUND" END-CALL
                CALL "P2843H" ON EXCEPTION DISPLAY "H NOT FOUND" END-CALL
                CANCEL "P2843H"
                CALL "P2843H" ON EXCEPTION DISPLAY "H NOT FOUND" END-CALL
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P2843H.
            PROCEDURE DIVISION.
                DISPLAY "MAIN'S CONTAINEE"
                GOBACK.
            END PROGRAM P2843H.
            END PROGRAM P2843W.
            """;
        const string sibling = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P2843W AS "P2843X".
            PROCEDURE DIVISION.
                DISPLAY "SIBLING"
                GOBACK.
            END PROGRAM P2843W.
            """;
        string dir = CutRunner.NewTempDir("pb2843");
        try
        {
            NonCobolElement.CompileCobol(dir, "P2843X", sibling);
            NonCobolElement.CompileCobol(dir, "P2843W", main);
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "P2843W.dll"), dir);
            Assert.True(exit == 0, detail);
            Assert.Equal("SIBLING\nMAIN'S CONTAINEE\nMAIN'S CONTAINEE", stdout);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
