// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;                             // ProcessObserver — the ONE child-process observer

namespace CobolNet.Tests.Unit;

/// <summary>
/// Base for WiseOwl COBOL end-to-end tests: compile a COBOL source string with <see cref="CompilerDriver"/> (the
/// greenfield COBOL→C#→Roslyn pipeline) into a per-test temp directory, run the produced assembly, and return its
/// captured stdout. Each test gets an isolated temp dir (disposed afterwards).
/// </summary>
public abstract class CobolNetTestBase : IDisposable
{
    protected readonly string TempDir;

    protected CobolNetTestBase()
    {
        TempDir = Path.Combine(Path.GetTempPath(), "CobolNet_Tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(TempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(TempDir))
            try { Directory.Delete(TempDir, recursive: true); } catch { /* best-effort cleanup */ }
        GC.SuppressFinalize(this);
    }

    /// <summary>Compile <paramref name="source"/> and run it; returns whether it ran and its trimmed stdout.</summary>
    protected (bool ok, string stdout, string detail) CompileAndRun(string source, int dialectLevel = 85)
    {
        string srcPath = Path.Combine(TempDir, "prog.cob");
        string dllPath = Path.Combine(TempDir, "prog.dll");
        File.WriteAllText(srcPath, source);

        var result = CompilerDriver.Compile(new CompilerDriver.Options(srcPath, dllPath, DialectLevel: dialectLevel, SourceFormat: InitialReferenceFormat.Auto));
        if (!result.Success)
            return (false, "", $"{result.Status}: {string.Join("\n", result.Errors)}");

        // The ONE child-process observer (tests/_shared/ProcessObservation.cs) — a run that does not complete
        // raises rather than returning an empty stdout for the caller to compare. See plan §11 A12.
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"") { WorkingDirectory = TempDir };
        var obs = ProcessObserver.ObserveOrThrow(psi);
        // Canonicalize newlines so multi-line expectations are platform-independent (a compiled program's DISPLAY
        // uses the platform newline: \r\n on Windows, \n on Linux/CI). See the legacy EndToEndTestBase note.
        string stdout = obs.Stdout.ReplaceLineEndings("\r\n").TrimEnd();
        return (obs.ExitCode == 0, stdout, obs.Stderr.Trim());
    }
}
