// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet;                                          // CompilerDriver (the greenfield compiler)
using CobolNet.Tests.Shared;                             // ProcessObserver — the ONE child-process observer
using LegacyCompilation = CobolSharp.Compiler.Compilation;
using LegacyState = CobolSharp.Runtime.ProgramState;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// A compiler the differential harness can drive uniformly: compile a COBOL source string to a runnable assembly
/// and run it, returning its stdout. Two implementations — <see cref="LegacyCompiler"/> (the byte-engine oracle,
/// 364-NIST-green) and <see cref="CobolNetCompiler"/> (the greenfield typed-native compiler) — let a test assert
/// that WiseOwl COBOL produces <b>byte-identical stdout to the legacy</b> for a program (COBOLNET_DESIGN §2 / §18 #7).
/// This is the verification backbone the G2 checkpoint demands ("binds and DISPLAYs its data byte-identically to
/// the legacy"); hand-typed expected strings would re-derive the very semantics under test (padding, scale,
/// edited/overpunch images).
/// </summary>
public interface ICompilerUnderTest
{
    /// <summary>A short label for assertion messages (e.g. <c>"legacy"</c> / <c>"cobolnet"</c>).</summary>
    string Name { get; }

    /// <summary>
    /// Compile <paramref name="source"/> to a console assembly and run it in an isolated temp directory.
    /// Returns whether it compiled-and-ran cleanly, its stdout (newline-canonicalized to <c>\r\n</c> then
    /// trailing-trimmed — matching both engines' test bases so the two are compared apples-to-apples), and a
    /// human-readable detail string for a failure.
    /// </summary>
    (bool ok, string stdout, string detail) CompileAndRun(string source);
}

/// <summary>Shared process-run plumbing for the two compiler-under-test implementations.</summary>
internal static class CutRunner
{
    /// <summary>
    /// Canonicalize a compiled program's stdout to the <b>NIST acceptance basis</b> — exactly the guard's
    /// <c>normalize()</c> (<c>scripts/guard.sh</c>): drop CR, then strip trailing spaces <i>per line</i>. This is
    /// the criterion the legacy oracle's 364-NIST-green status was validated against, so comparing on this basis
    /// makes the legacy a sound differential oracle. It also neutralizes the legacy's one known DISPLAY
    /// non-conformance — it trims trailing spaces off alphanumeric operands (so <c>DISPLAY WS-X</c> of a
    /// <c>PIC X(10)</c> holding <c>"HI"</c> emits <c>"HI"</c>, not <c>"HI        "</c>), contradicting ISO
    /// §14.9.11.4 GR1/GR6 ("the content of each operand … the size … is the sum of the sizes of the operands").
    /// WiseOwl COBOL emits the spec-correct full field; a single-/trailing-operand DISPLAY then matches the legacy once
    /// per-line trailing spaces are stripped. (A case that exposes <i>internal</i> trailing spaces — e.g.
    /// <c>DISPLAY WS-X "]"</c> — is pinned to the spec value instead, since the legacy is non-conforming there.)
    /// </summary>
    public static string Normalize(string s)
    {
        var lines = s.ReplaceLineEndings("\n").Split('\n').Select(line => line.TrimEnd(' '));
        return string.Join("\n", lines).TrimEnd('\n');
    }

    /// <summary>Run <c>dotnet &lt;dll&gt;</c> in <paramref name="workDir"/> and capture normalized stdout.
    /// <paramref name="stdinFile"/> (when given) is piped to the program's stdin — ACCEPT device input (ISO
    /// §14.9.1 F1); stdin is always redirected and closed so an over-reading ACCEPT sees EOF, never the console.</summary>
    public static (bool ok, string stdout, string detail) Run(string dllPath, string workDir, string? stdinFile = null,
        IReadOnlyDictionary<string, string>? env = null)
    {
        var (code, stdout, detail) = RunExit(dllPath, workDir, stdinFile, env);
        return (code == 0, stdout, detail);
    }

    /// <summary>Like <see cref="Run"/> but returns the NUMERIC process exit code (the run-unit termination status
    /// "passed to the operating system" — ISO §14.9.42.4 GR5 / §14.9.18.4 GR10), not just success. The exit-code
    /// tests assert the value; the golden harness only needs the <c>ok</c> bool.
    ///
    /// <para>⛔ A run that does not COMPLETE no longer returns anything (plan §11 A12). It used to return
    /// <c>-1</c> plus whatever partial stdout had been captured — often the empty string — which the callers
    /// then compared against a golden, so a contention timeout was reported as a value mismatch. It now raises
    /// <c>HarnessNonObservationException</c> via <see cref="ProcessObserver.ObserveOrThrow"/>, which retries
    /// once serially first. No caller had to change: the fix is in the dispatch, and all ~730 call sites
    /// inherit it.</para></summary>
    public static (int exitCode, string stdout, string detail) RunExit(string dllPath, string workDir,
        string? stdinFile = null, IReadOnlyDictionary<string, string>? env = null)
    {
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"") { WorkingDirectory = workDir };
        // ⚖ DETERMINISM (DESIGN-locale-facility §10 T-F): the run unit's USER and SYSTEM default locales are the
        // environment's (owner decision Q2 — COBOL_USER_LOCALE / COBOL_SYSTEM_LOCALE, else the host culture), so a
        // golden that collates under the default locale would pass on one author's machine and fail on a runner
        // with another regional setting. The harness pins BOTH to the root ("INVARIANT") for every program it
        // runs; a golden that needs a locale SETs it (SET LOCALE … TO locale-name) or names it (IS LOCALE name),
        // and a test that wants another default passes it in `env` (which overrides the pin).
        psi.Environment[CobolNet.Runtime.LocaleState.UserDefaultVariable] = "INVARIANT";
        psi.Environment[CobolNet.Runtime.LocaleState.SystemDefaultVariable] = "INVARIANT";
        if (env is not null)
            foreach (var (k, v) in env) psi.Environment[k] = v;
        // ACCEPT device input (ISO §14.9.1 F1): pipe the NIST .dat to stdin (EOF when none) — guard.sh parity.
        var obs = ProcessObserver.ObserveOrThrow(psi, stdinFile is null ? null : File.ReadAllText(stdinFile));
        return (obs.ExitCode, Normalize(obs.Stdout), Normalize(obs.Stderr));
    }

    /// <summary>Create a fresh isolated temp directory for one compile-and-run.</summary>
    public static string NewTempDir(string tag)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"CobolNet_Diff_{tag}_{Guid.NewGuid():N}"[..40]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Best-effort recursive delete of a temp directory.</summary>
    public static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
    }
}

/// <summary>
/// The greenfield WiseOwl COBOL compiler under test: drives <see cref="CompilerDriver"/> (COBOL → typed-native C# →
/// Roslyn) into an isolated temp dir, then runs the produced assembly.
/// </summary>
/// <param name="dialectLevel">The targeted ISO edition (default 85 — the differential harness compiles at the
/// legacy oracle's edition; spec-pinned tests of post-85 features pass their own, e.g. 2023 for the wide tier).</param>
public sealed class CobolNetCompiler(int dialectLevel = 85) : ICompilerUnderTest
{
    public string Name => "cobolnet";

    public (bool ok, string stdout, string detail) CompileAndRun(string source)
    {
        string dir = CutRunner.NewTempDir("cn");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            src = CompiledProgramCache.StageSource(src, source);

            var result = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: dialectLevel));
            if (!result.Success)
                return (false, "", $"[cobolnet compile] {result.Status}: {string.Join("\n", result.Errors)}");

            return CutRunner.Run(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>Compile and run <paramref name="source"/>, then return the RAW BYTES the program left in
    /// <paramref name="fileName"/> inside its run directory — the physical medium, before any line-ending or
    /// trailing-space normalization.
    ///
    /// <para>⛔ THIS IS THE ONLY WAY TO ASSERT A PHYSICAL FILE LAYOUT. <see cref="CompileAndRun"/> deletes the
    /// run directory in its <c>finally</c>, so every test written through it can observe a program only through
    /// stdout — and a rule about WHERE ON THE MEDIUM a record lands (ISO §13.18.34.4 GR4/GR5 margins, §14.9.51.4
    /// GR25 g)'s reposition-not-form-feed) is invisible there. kb/Work PB523 is the defect that proves it: the
    /// LINAGE margins were evaluated and discarded for the whole life of the connector while every LINAGE test
    /// stayed green, because they all read LINAGE-COUNTER and never the file.</para></summary>
    /// <returns><c>bytes</c> is null when the program did not create the file.</returns>
    public (bool ok, string stdout, string detail, byte[]? bytes) CompileRunAndReadFile(string source, string fileName)
    {
        string dir = CutRunner.NewTempDir("cn");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            src = CompiledProgramCache.StageSource(src, source);

            var result = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: dialectLevel));
            if (!result.Success)
                return (false, "", $"[cobolnet compile] {result.Status}: {string.Join("\n", result.Errors)}", null);

            var (ok, stdout, detail) = CutRunner.Run(dll, dir);
            string produced = Path.Combine(dir, fileName);
            return (ok, stdout, detail, File.Exists(produced) ? File.ReadAllBytes(produced) : null);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>Compile each of <paramref name="companions"/> into its OWN assembly beside
    /// <paramref name="source"/>, then compile and run <paramref name="source"/> — the SEPARATELY COMPILED
    /// module case, resolved at run time by <c>ProgramTable</c>'s sibling-module probe (§8.4.6.3 rule 4).
    ///
    /// <para>This is not a convenience: some rules are only reachable ACROSS a compilation boundary and are
    /// untestable in a single group. §14.9.18.4 GR1b is the case that forced it — an exception condition
    /// staged by <c>GOBACK … RAISING</c> is raised in the activator only if the ACTIVATOR enabled checking for
    /// it, and within one compilation group any <c>>>TURN</c> anywhere makes the whole group EC-active, so an
    /// unchecked-activator test cannot be written as one program.</para></summary>
    public (bool ok, string stdout, string detail) CompileAndRunWith(string source, params string[] companions)
    {
        string dir = CutRunner.NewTempDir("cn");
        try
        {
            foreach (string companion in companions)
            {
                // The assembly must be NAMED for its PROGRAM-ID: ProgramTable's sibling-module probe locates a
                // separately-compiled program by looking for <program-name>.dll beside the caller (§8.4.6.3
                // rule 4). A generic "companion0.dll" compiles fine and is then never found at run time, which
                // surfaces as EC-PROGRAM-NOT-FOUND rather than as a harness error.
                var m = System.Text.RegularExpressions.Regex.Match(
                    companion, @"PROGRAM-ID\.\s*([A-Za-z0-9][A-Za-z0-9-]*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (!m.Success) return (false, "", "[harness] a companion module has no PROGRAM-ID to name its assembly after");
                string name = m.Groups[1].Value;
                string csrc = Path.Combine(dir, name + ".cob");
                csrc = CompiledProgramCache.StageSource(csrc, companion);
                var cr = CompiledProgramCache.Compile(new CompilerDriver.Options(
                    csrc, Path.Combine(dir, name + ".dll"), DialectLevel: dialectLevel));
                if (!cr.Success)
                    return (false, "", $"[cobolnet compile {name}] {cr.Status}: {string.Join("\n", cr.Errors)}");
            }

            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            src = CompiledProgramCache.StageSource(src, source);
            var result = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: dialectLevel));
            if (!result.Success)
                return (false, "", $"[cobolnet compile] {result.Status}: {string.Join("\n", result.Errors)}");

            return CutRunner.Run(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>Compile+run and return the NUMERIC process exit code — the STOP RUN / GOBACK termination status
    /// "passed to the operating system" (ISO §14.9.42.4 GR5 / §14.9.18.4 GR10). A compile failure returns
    /// <c>(-1, "", detail)</c>.</summary>
    public (int exitCode, string stdout, string detail) CompileAndRunExit(string source)
    {
        string dir = CutRunner.NewTempDir("cn");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            src = CompiledProgramCache.StageSource(src, source);

            var result = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: dialectLevel));
            if (!result.Success)
                return (-1, "", $"[cobolnet compile] {result.Status}: {string.Join("\n", result.Errors)}");

            return CutRunner.RunExit(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}

/// <summary>
/// The legacy byte-engine compiler as the differential oracle: drives the in-process <c>CobolSharp.Compiler</c>
/// <see cref="LegacyCompilation"/> API (the same path <c>EndToEndTestBase</c> uses, 364-NIST-green) into an
/// isolated temp dir, then runs the produced assembly. Default (permissive) dialect, matching the legacy base.
/// </summary>
public sealed class LegacyCompiler : ICompilerUnderTest
{
    public string Name => "legacy";

    public (bool ok, string stdout, string detail) CompileAndRun(string source)
    {
        string dir = CutRunner.NewTempDir("lg");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            src = CompiledProgramCache.StageSource(src, source);

            var compilation = new LegacyCompilation();
            var result = compilation.Compile(src, dll);
            if (!result.Success)
                return (false, "", $"[legacy compile] {string.Join("\n", result.Diagnostics.Select(d => d.ToString()))}");

            // Deploy the legacy runtime next to the output (defensive — mirrors EndToEndTestBase's multi-program path).
            string runtime = typeof(LegacyState).Assembly.Location;
            string dest = Path.Combine(dir, Path.GetFileName(runtime));
            if (!File.Exists(dest)) File.Copy(runtime, dest);

            return CutRunner.Run(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
