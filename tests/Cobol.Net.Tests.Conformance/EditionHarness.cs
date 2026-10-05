// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// THE per-edition compile/diagnostic harness (VERSION_TEST_MATRIX_DESIGN.md Phase 1): compile a source text or a
/// NIST program AS a specific ISO edition (the four-compilers-in-one mission — <c>--std 85|2002|2014|2023</c>) and
/// inspect the outcome. Every edition-targeted test goes through here so the matrix, the continuity sweep, and the
/// (future) negative corpus share ONE compile path and ONE diagnostic-assertion idiom.
/// </summary>
public static class EditionHarness
{
    /// <summary>The supported ISO editions, in order.</summary>
    public static readonly int[] Editions = [85, 2002, 2014, 2023];

    /// <summary>Compile <paramref name="source"/> targeting <paramref name="edition"/> on either severity axis
    /// (P2.7): returns success plus BOTH channels — the failing errors and the non-failing warnings (permissive
    /// removals, 0903 flags). <paramref name="copybooks"/> (file name → library text), when given, is staged beside
    /// the program and that directory is named as a <c>--copy</c> search path, as a user names theirs (the default
    /// COBOL library never includes the source file's own directory — DOC-A.1-40, kb/Work PB1355).</summary>
    public static (bool Ok, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) CompileFull(
        string source, int edition, bool permissive = false, IReadOnlyDictionary<string, string>? copybooks = null,
        bool flagExtensions = false)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Ed_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, source, copybooks);   // copybooks staged WITH it (kb/Work PB1755)
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: edition, Permissive: permissive,
                CopyPaths: copybooks is null ? null : [Path.GetDirectoryName(src)!], SourceFormat: InitialReferenceFormat.Auto,
                FlagExtensions: flagExtensions));
            return (r.Success, r.Success ? [] : [.. r.Errors.DefaultIfEmpty($"status {r.Status}")], r.Warnings);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>Compile <paramref name="source"/> targeting <paramref name="edition"/> (strict); returns success
    /// and the diagnostics (empty on success). Delegates to <see cref="CompileFull"/>.</summary>
    public static (bool Ok, IReadOnlyList<string> Diagnostics) Compile(string source, int edition,
        IReadOnlyDictionary<string, string>? copybooks = null)
    {
        var (ok, errors, _) = CompileFull(source, edition, copybooks: copybooks);
        return (ok, errors);
    }

    /// <summary>Compile a NIST CCVS program (X-card preprocessing applied) targeting <paramref name="edition"/>,
    /// optionally on the permissive axis (the INV-1 continuity legs at ≥2002 run permissive — the §10 #1
    /// migration posture — once removal gating exists).</summary>
    public static (bool Ok, IReadOnlyList<string> Diagnostics) CompileNist(
        string testName, int edition, bool permissive = false, bool checkOnly = false)
    {
        string src = TestRepo.Nist("programs", testName + ".cob");
        Assert.True(File.Exists(src), $"NIST source not found: {src}");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Ed_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            // checkOnly = parse + edition-validate + bind (NO Roslyn backend) — the compile VERDICT is settled
            // pre-backend, so the INV-1 continuity sweep uses it (the ~29-min→<1-min speedup, DEVLOG 627).
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, testName + ".dll"), NistTestName: testName, DialectLevel: edition,
                Permissive: permissive, CheckOnly: checkOnly));
            return (r.Success, r.Success ? [] : [.. r.Errors.DefaultIfEmpty($"status {r.Status}")]);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>Compile <paramref name="source"/> at <paramref name="edition"/> AND run it, returning the program's
    /// stdout — the INV-3 behavior-variant path (does the SAME source produce different OUTPUT across editions?).
    /// Uses the shared <see cref="CutRunner"/> (the golden-runner's execution path).</summary>
    public static (bool Ok, string Stdout, string Detail) CompileAndRun(string source, int edition, bool permissive = false)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Run_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, source);
            string dll = Path.Combine(dir, "prog.dll");
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: edition, Permissive: permissive, SourceFormat: InitialReferenceFormat.Auto));
            if (!r.Success) return (false, "", $"[compile] {r.Status}: {string.Join("\n", r.Errors)}");
            return CutRunner.Run(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>The diagnostics of compiling <paramref name="source"/> at <paramref name="edition"/> (empty when it
    /// compiles clean).</summary>
    public static IReadOnlyList<string> GetDiagnostics(string source, int edition) => Compile(source, edition).Diagnostics;

    /// <summary>Assert some diagnostic contains <paramref name="expectedSubstring"/> (case-insensitive). The negative
    /// corpus asserts the QUALITY of a rejection this way — e.g. that a too-new construct's diagnostic names the
    /// required edition — per the matrix's reject cells. (Until the EditionValidator lands, grammar-gate rejections
    /// are generic parse errors; rows assert content only once their diagnostic is implemented.)</summary>
    public static void AssertHasDiagnostic(IEnumerable<string> diagnostics, string expectedSubstring)
    {
        var all = diagnostics.ToList();
        Assert.True(all.Any(d => d.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase)),
            $"expected a diagnostic containing '{expectedSubstring}'; got:\n{string.Join("\n", all.DefaultIfEmpty("(none)"))}");
    }

    /// <summary>Assert NO diagnostic contains <paramref name="substring"/> (P2.7 — e.g. a permissive compile of
    /// a construct the edition still HAS must not carry its removal warning).</summary>
    public static void AssertNoDiagnostic(IEnumerable<string> diagnostics, string substring)
    {
        var hit = diagnostics.FirstOrDefault(d => d.Contains(substring, StringComparison.OrdinalIgnoreCase));
        Assert.True(hit is null, $"expected NO diagnostic containing '{substring}' but found: {hit}");
    }
}
