// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Runtime;
using Microsoft.CodeAnalysis;

namespace CobolNet;

/// <summary>
/// Drives the WiseOwl COBOL compile pipeline end-to-end: COBOL source → typed-native C# → a runnable .NET assembly.
/// This is the library entry point the CLI (<c>Cobol.Net.Cli</c>) and the tests call; it owns no console I/O and
/// no process control, returning a structured <see cref="Result"/> the caller maps to exit codes / assertions.
/// </summary>
public static class CompilerDriver
{
    /// <summary>Inputs to a single compilation.</summary>
    /// <param name="SourcePath">Path to the COBOL source file.</param>
    /// <param name="OutputPath">Output assembly path; defaults to the source path with a <c>.dll</c> extension.</param>
    /// <param name="NistTestName">When set, enables NIST CCVS placeholder preprocessing for that test.</param>
    /// <param name="DialectLevel">ISO dialect year the parser admits (85 / 2002 / 2014 / 2023). DEFAULTS to the LATEST
    /// edition (2023) when unspecified — an unflagged compile targets the newest standard (owner decision, DEVLOG 519;
    /// docs/VERSION_TEST_MATRIX_DESIGN.md §10 #2). Callers that target a specific edition (the NIST harness at 85, the
    /// differential harness, per-edition conformance) pass it explicitly, so the default flip does not affect them.</param>
    /// <param name="CopyPaths">Directories searched for COPY copybooks, in order.</param>
    /// <param name="Permissive">The strict/permissive severity axis (CLI <c>--permissive</c>, orthogonal to
    /// <paramref name="DialectLevel"/>): permissive accepts constructs the targeted edition REMOVED, emitting a
    /// warning and the pre-removal semantics (the documented migration mode, VERSION_TEST_MATRIX_DESIGN §10 #1).
    /// Strict (the default for every named <c>--std</c>) rejects them. Introduction gating is unaffected.</param>
    /// <param name="CheckOnly">Parse + bind + the version-conformance pass ONLY — do NOT run the Roslyn C#→IL
    /// backend and write no <c>.dll</c>/<c>.g.cs</c>. Every edition-gating diagnostic (the "does this compile at
    /// edition X" question the INV-1 continuity sweep asks) is produced in Phase 1/2, BEFORE the backend, so a check-only
    /// compile is verdict-equivalent to a full one for that question while skipping the backend — the dominant
    /// cost. Backend (C#-type) errors are NOT surfaced (they are not an edition-continuity concern).</param>
    /// <param name="SignEncoding">The DISPLAY over-punch convention for a signed item whose sign is not written
    /// SEPARATE CHARACTER (CLI <c>--sign-encoding</c>; kb/Work PB803, owner decision 2026-09-09). ISO §13.18.52.4
    /// GR4 leaves the representation to the implementor and GR5 b) the valid-sign set — Annex A.1 items 177 and
    /// 178 — and the owner's answer is both conventions behind one option, defaulting to
    /// <see cref="Runtime.SignEncoding.Ibm"/> (IBM / Micro Focus compatibility). Orthogonal to
    /// <paramref name="DialectLevel"/>: every edition grants the same latitude.</param>
    /// <param name="SourceFormat">The reference format the compilation group starts in (CLI <c>--source-format</c>;
    /// kb/Work PB1362): fixed form, ISO §7.3.24.3 2)'s default, unless free form or the documented auto-detection
    /// extension is selected (§4.2.10 3) — docs/CONFORMANCE.md DOC-A.1-158).</param>
    public sealed record Options(
        string SourcePath,
        string? OutputPath = null,
        string? NistTestName = null,
        int DialectLevel = 2023,
        IReadOnlyList<string>? CopyPaths = null,
        bool Permissive = false,
        bool CheckOnly = false,
        SignEncoding SignEncoding = SignEncoding.Ibm,
        Frontend.Preprocessor.InitialReferenceFormat SourceFormat = Frontend.Preprocessor.InitialReferenceFormat.Fixed);

    /// <summary>Which phase a compilation reached (drives the CLI's exit code).</summary>
    public enum Outcome { Success, SourceNotFound, FrontendError, BindError, BackendError }

    /// <summary>The result of a compilation.</summary>
    /// <param name="Status">The phase reached / the failure category.</param>
    /// <param name="OutputDll">The output assembly path (set once an output location is known).</param>
    /// <param name="GeneratedCsPath">The emitted <c>.g.cs</c> path (set once C# is generated).</param>
    /// <param name="Errors">Human-readable diagnostics for a non-success outcome.</param>
    /// <param name="Warnings">Non-failing edition diagnostics (obsolete/archaic 0903 flags; removed constructs
    /// under <see cref="Options.Permissive"/>). Present on EVERY outcome, success included — the CLI prints them
    /// to stderr always (P2.1).</param>
    public sealed record Result(
        Outcome Status,
        string OutputDll,
        string? GeneratedCsPath,
        IReadOnlyList<string> Errors,
        IReadOnlyList<string> Warnings)
    {
        /// <summary>True iff a runnable assembly was produced.</summary>
        public bool Success => Status == Outcome.Success;

        /// <summary>The compilation's AMBIENT inputs (kb/Work PB985): every file it read, every path it probed
        /// (found or not), every environment variable a directive consulted, and whether it read the clock. With
        /// <see cref="Options"/> and the compiler's own bits these are EVERYTHING the output depends on — the
        /// dependency record an incremental build or a compiled-program cache keys on.</summary>
        public Frontend.CompilationInputs Inputs { get; init; } = new();

        /// <summary>Every file this compilation WROTE, as full paths (the <c>.g.cs</c>, the assembly, the packaged
        /// runtime config and runtime) — empty when it wrote nothing.</summary>
        public IReadOnlyList<string> OutputFiles { get; init; } = [];

        /// <summary>What the compilation itself transferred to its operator, in order — the lines of every DISPLAY directive
        /// it processed (ISO §7.3.12.4 GR1, kb/Work PB1538), each with the compiler stream it is written to. Present on
        /// every outcome the front end ran for; the CLI writes them before the diagnostics.</summary>
        public IReadOnlyList<Frontend.Diagnostics.CompileOutputLine> CompileOutput { get; init; } = [];
    }

    /// <summary>Compile <paramref name="options"/> to a runnable console assembly.</summary>
    public static Result Compile(Options options)
    {
        // kb/Work PB985 — the ONE ambient-input record of this compilation: every read below that is not the
        // options themselves (the source-existence probe, the default-library probe, the front end's source and
        // copybook reads, a directive's environment read, the emitter's WHEN-COMPILED clock) goes through it.
        var inputs = new Frontend.CompilationInputs();
        if (!inputs.FileExists(options.SourcePath))
            return new Result(Outcome.SourceNotFound, "", null, [$"source file not found: {options.SourcePath}"], [])
                { Inputs = inputs };

        // Phase 1 — front-end: preprocess + parse.
        var diagnostics = new DiagnosticBag();
        var frontend = new Frontend.Frontend
        {
            Inputs = inputs,
            NistTestName = options.NistTestName,
            DialectLevel = options.DialectLevel,
            // The preprocessor-level removal gates (VCR 2/4, W3 — DEVLOG 598) honor the same severity axis.
            Permissive = options.Permissive,
            InitialFormat = options.SourceFormat,   // kb/Work PB1362 — §7.3.24.3 2) fixed by default
        };
        foreach (string dir in options.CopyPaths ?? [])
            frontend.AddCopySearchPath(dir);
        // The implementor-defined DEFAULT COBOL LIBRARY (ISO §7.2.3.4 GR3 — "the implementor defines the
        // mechanism for identifying the default COBOL library"): in NIST mode it is the `copylib/` directory
        // sibling to the source's directory — the convention the legacy CLI auto-discovers
        // (CobolSharp.CLI/Program.cs::RunCompile), so the SM COPY suite resolves its K*/KP*/ALTLB texts.
        // Appended AFTER caller-supplied paths so an explicit --copy outranks the convention.
        if (options.NistTestName is not null
            && Path.GetDirectoryName(Path.GetFullPath(options.SourcePath)) is { } srcDir
            && Path.GetFullPath(Path.Combine(srcDir, "..", "copylib")) is { } copylib
            && inputs.DirectoryExists(copylib))
            frontend.AddCopySearchPath(copylib);

        var tree = frontend.Parse(options.SourcePath, diagnostics);
        // Frontend WARNINGS (the preprocessor 0902-permissive/0903 gates) ride Result.Warnings on every
        // outcome, merged ahead of the edition channel's — same contract as edition warnings.
        var feWarnings = diagnostics.Diagnostics.Where(d => !d.IsError).Select(d => d.ToString()!).ToList();
        if (tree is null || diagnostics.HasErrors)
            return new Result(Outcome.FrontendError, "", null,
                diagnostics.Diagnostics.Where(d => d.IsError).Select(d => d.ToString()!).ToList(), feWarnings)
                { Inputs = inputs, CompileOutput = diagnostics.CompileOutput };

        // Phase 2 — BIND under the targeted EDITION, then emit typed-native C#. The Binder phase
        // (BinderDriver.Bind behind emitter.Bind, P6) runs the DECLARED manifest whose NAMED TERMINAL pass is the
        // ONE version-conformance funnel (P3's two-arm pass — the SOLE edition gate: its parse-tree arm fires the
        // syntactic introduction/removal/phrase gates + the §8.9 reserved-word funnel on RECOGNITION, its
        // bound-tree arm the genuinely-semantic gates), so the returned BoundCompilation already carries EVERY
        // edition diagnostic on the sink — for BOTH a full compile and a CheckOnly verdict. Edition-gating
        // diagnostics (the four-compilers rule: a construct the targeted edition lacks or forbids REJECTS the
        // program) are semantic errors, not runtime guards; they fail the compile here, BEFORE emit — a removed
        // or not-yet-introduced construct may have no emit path at all (exit criterion 9: no codegen on an
        // errored tree). frontend.TurnEvents are the >>TURN directive events (ISO §7.3.25) that build the
        // group's compile-time TurnState (deep-dive D10).
        var edition = new Binding.EditionContext(options.DialectLevel, options.Permissive)
        {
            // kb/Work PB82: bind-time diagnostics and the bound tree's user-facing lines (DEBUG-LINE,
            // EXCEPTION-LOCATION) name the file and line the USER edits, through the frontend's origin map.
            LineMap = frontend.LineMap,
            SourceFile = options.SourcePath,
            // kb/Work PB803: the compilation's over-punch convention travels to the emitter on the SAME
            // per-compilation object the edition does (DataBinder.Edition → EmitContext.SignEncoding), so every
            // NumProfile and every signed class condition this compile emits states one convention.
            SignEncoding = options.SignEncoding,
        };
        var emitter = new CSharpEmitter();
        var bound = emitter.Bind(tree, edition, frontend.Directives);   // Phase 2a — BIND (terminal = conformance pass)
        if (edition.Diagnostics.Count > 0)
            return new Result(Outcome.BindError, "", null, edition.Diagnostics, [.. feWarnings, .. edition.Warnings])
                { Inputs = inputs, CompileOutput = diagnostics.CompileOutput };

        // Check-only: every edition-gating diagnostic is now produced (parse + the bind manifest incl. its
        // terminal conformance pass), so the compile VERDICT is settled — the verdict already includes the pass's
        // edition diagnostics, and no C# text is ever built on this path. Skip emit AND Phase 3 (the Roslyn C#→IL
        // backend + the dll/g.cs writes) — the dominant cost — since no runnable assembly is wanted (the INV-1
        // continuity sweep / CLI `check-batch`).
        if (options.CheckOnly)
            return new Result(Outcome.Success, "", null, [], [.. feWarnings, .. edition.Warnings]) { Inputs = inputs, CompileOutput = diagnostics.CompileOutput };

        // Phases 2b + 3 — the BACKEND (the ICodeGenBackend seam, P7 Step 1): render the bound tree and compile
        // the result. Everything after Bind sits behind the seam so a second backend (direct CIL, PHASE 16) is a
        // new implementation, not a pipeline fork. Emit is unreachable when any diagnostics exist (above).
        string outputDll = options.OutputPath ?? Path.ChangeExtension(options.SourcePath, ".dll");
        var backend = BackendFactory.For(BackendId.Roslyn, emitter);   // the emitter = the P6→P9 bind host
        var artifact = backend.Emit(bound, new BackendOptions(
            outputDll, Path.GetFileNameWithoutExtension(outputDll), edition.Edition, Inputs: inputs));
        if (!artifact.Success)
            return new Result(Outcome.BackendError, outputDll, artifact.GeneratedSourcePath,
                artifact.Diagnostics
                    .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                    .Select(d => d.ToString())
                    .ToList(), [.. feWarnings, .. edition.Warnings])
                { Inputs = inputs, CompileOutput = diagnostics.CompileOutput, OutputFiles = artifact.OutputFiles };

        return new Result(Outcome.Success, outputDll, artifact.GeneratedSourcePath, [],
            [.. feWarnings, .. edition.Warnings]) { Inputs = inputs, CompileOutput = diagnostics.CompileOutput, OutputFiles = artifact.OutputFiles };
    }
}
