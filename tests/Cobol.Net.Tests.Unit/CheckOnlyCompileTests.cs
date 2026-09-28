// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The no-emit CheckOnly compile path (CompilerDriver.Options.CheckOnly — the CLI `check-batch` / INV-1
/// continuity sweep fast path). Every edition-gating diagnostic is produced in parse + edition-validate +
/// bind/emit, BEFORE the Roslyn backend, so a check-only compile is VERDICT-EQUIVALENT to a full one for the
/// "does this compile at edition X" question while skipping the backend (the dominant cost) and writing nothing.
/// CheckOnly is a strictly LENIENT PREFIX of a full compile: it returns Success once bind succeeds, so it can
/// never FAIL where a full compile succeeds (it only differs by passing a program a full compile would reject at
/// the Roslyn stage — not an edition-continuity concern).
/// </summary>
public sealed class CheckOnlyCompileTests
{
    private static string WriteTemp(string dir, string name, string source)
    {
        string path = Path.Combine(dir, name + ".cob");
        File.WriteAllText(path, source);
        return path;
    }

    private const string GoodProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CHKOK.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-R PIC 9(4).
        PROCEDURE DIVISION.
        MAIN.
            COMPUTE WS-R = 2 + 2.
            DISPLAY WS-R.
            STOP RUN.
        """;

    // DELETE FILE (Format 2) is a 2023 construct — rejected below 2023 (COBOLNET0900).
    private const string EditionGated2023 = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CHK23.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN "F" ORGANIZATION LINE SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 F-REC PIC X(10).
        PROCEDURE DIVISION.
        MAIN.
            DELETE FILE F.
            STOP RUN.
        """;

    [Fact]
    public void CheckOnly_GoodProgram_SucceedsAndEmitsNothing()
    {
        string dir = Directory.CreateTempSubdirectory("chkonly").FullName;
        try
        {
            string src = WriteTemp(dir, "CHKOK", GoodProgram);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            // No runnable assembly and no generated C# were written (the backend + file writes are skipped).
            Assert.Empty(Directory.GetFiles(dir, "*.dll"));
            Assert.Empty(Directory.GetFiles(dir, "*.g.cs"));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Theory]
    [InlineData(2023, true)]    // DELETE FILE is valid at 2023
    [InlineData(85, false)]     // and rejected below it — CheckOnly must report the SAME verdict as a full compile
    [InlineData(2014, false)]
    public void CheckOnly_EditionGating_MatchesFullCompileVerdict(int edition, bool expectSuccess)
    {
        string dir = Directory.CreateTempSubdirectory("chkedn").FullName;
        try
        {
            string src = WriteTemp(dir, "CHK23", EditionGated2023);
            var check = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            var full = CompilerDriver.Compile(new CompilerDriver.Options(
                src, OutputPath: Path.Combine(dir, "full.dll"), DialectLevel: edition, SourceFormat: InitialReferenceFormat.Auto));
            Assert.Equal(expectSuccess, check.Success);
            // The edition-gating verdict is identical whether or not the backend runs.
            Assert.Equal(full.Success, check.Success);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    /// <summary>P6 Step 4 (exit criterion #4): an edition violation under CheckOnly returns <c>BindError</c>
    /// CARRYING the VersionConformancePass's edition band code — the verdict includes the terminal manifest
    /// pass's diagnostics, not just a boolean. (The pass runs INSIDE Bind since P6.4; a bind-only verdict
    /// would silently drop every edition diagnostic.)</summary>
    [Fact]
    public void CheckOnly_EditionViolation_BindErrorCarriesTheBandCode()
    {
        string dir = Directory.CreateTempSubdirectory("chkband").FullName;
        try
        {
            string src = WriteTemp(dir, "CHK23", EditionGated2023);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: 85, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            Assert.Equal(CompilerDriver.Outcome.BindError, r.Status);
            // The DELETE FILE introduction gate reports through the edition band (COBOLNET09xx).
            Assert.Contains(r.Errors, e => e.Contains("COBOLNET09"));
            // And nothing was emitted on the error path either.
            Assert.Null(r.GeneratedCsPath);
            Assert.Empty(Directory.GetFiles(dir, "*.g.cs"));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // Binds clean, but its emitted body contains a LOUD NotImplemented guard (ACCEPT into a COMP-1 receiver —
    // device conversion deferred): the closest thing to an "emit-side failure" this compiler produces by design
    // (bound errors surface as loud guards, never Roslyn breaks). CheckOnly must return Success — the CheckOnly
    // verdict is settled by bind + the conformance pass, and no C# text is ever built (exit criterion #4).
    private const string BindsCleanEmitsLoud = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CHKLOUD.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-F COMP-1.
        PROCEDURE DIVISION.
        MAIN.
            ACCEPT WS-F.
            DISPLAY WS-F.
            STOP RUN.
        """;

    [Fact]
    public void CheckOnly_BindCleanEmitLoudProgram_Succeeds()
    {
        string dir = Directory.CreateTempSubdirectory("chkloud").FullName;
        try
        {
            string src = WriteTemp(dir, "CHKLOUD", BindsCleanEmitsLoud);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            // No emit artifacts: the CheckOnly path returns before EmitBound (GeneratedCsPath stays null and no
            // .g.cs/.dll is written) — the observable "EmitBound is not invoked" contract.
            Assert.Null(r.GeneratedCsPath);
            Assert.Empty(Directory.GetFiles(dir, "*.g.cs"));
            Assert.Empty(Directory.GetFiles(dir, "*.dll"));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
