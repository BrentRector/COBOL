// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions;
using Microsoft.CodeAnalysis;

namespace CobolNet.CodeGen;

/// <summary>The selectable code-generation backends (DESIGN-backend-abstraction / DESIGN-codegen-backend §2.2;
/// the dual-backend mandate, COBOLNET_REARCHITECTURE_PLAN §3): <see cref="Roslyn"/> renders typed-native C# and
/// compiles it with Roslyn (the primary backend); <see cref="Cil"/> is the future direct-IL backend
/// (Mono.Cecil, PHASE 16).</summary>
public enum BackendId
{
    /// <summary>The C#-via-Roslyn backend (primary; the only one implemented today).</summary>
    Roslyn,
    /// <summary>The direct-CIL backend (PHASE 16 — not yet implemented).</summary>
    Cil,
}

/// <summary>Per-compilation backend inputs (DESIGN-codegen-backend §2.2).</summary>
/// <param name="OutputPath">The output assembly path (the backend creates the directory).</param>
/// <param name="AssemblyName">The emitted assembly's simple name (the COBOL PROGRAM-ID).</param>
/// <param name="Edition">The targeted edition. The Roslyn backend does not consult it — emit is
/// edition-agnostic by the P3 pipeline contract (every edition diagnostic precedes emit) — but the seam carries
/// it for backend-neutral plumbing a future backend may need (e.g. per-edition runtime feature switches, P16).</param>
/// <param name="EmitPdb">RESERVED (declared by the design contract; not yet honored — P7 Step 1 is
/// no-behavior-change, and today's backend emits no symbols. A later phase wires it).</param>
/// <param name="WriteSource">Write the generated intermediate source (the <c>.g.cs</c>) next to the assembly.
/// Meaningful only for a source-producing backend; a direct-IL backend ignores it.</param>
/// <param name="Inputs">The compilation's ambient-input record (kb/Work PB985) — a backend that reads anything
/// outside the bound tree (the WHEN-COMPILED clock) reads it through this, so the record stays complete.</param>
public sealed record BackendOptions(string OutputPath, string AssemblyName, EditionInfo Edition,
    bool EmitPdb = true, bool WriteSource = true, CobolNet.Frontend.CompilationInputs? Inputs = null);

/// <summary>The outcome of one backend emission (DESIGN-codegen-backend §2.2).</summary>
/// <param name="Success">True iff the assembly was produced.</param>
/// <param name="Diagnostics">The backend's compile diagnostics (errors + warnings; empty for a clean emit).</param>
/// <param name="GeneratedSourcePath">The intermediate-source path (<c>.g.cs</c>), when the backend produced one —
/// set even on failure (the source is written before compilation, the primary debugging artifact).</param>
/// <param name="AssemblyPath">The produced assembly path on success; null on failure.</param>
/// <param name="OutputFiles">EVERY file the emission wrote, as full paths — the intermediate source, the assembly
/// and whatever packaging deployed beside it (kb/Work PB985): the complete output set a build cache must store
/// and restore, stated by the component that wrote it rather than re-derived by the caller.</param>
/// <param name="Library">The assembly is a library — the group has no main program (DESIGN-external-repository §7.2) —
/// so a host loads it and nothing launches it.</param>
public sealed record BackendArtifact(bool Success, IReadOnlyList<Diagnostic> Diagnostics,
    string? GeneratedSourcePath, string? AssemblyPath, IReadOnlyList<string> OutputFiles, bool Library);

/// <summary>
/// THE backend seam (P7 Step 1; DESIGN-codegen-backend §2.2): everything after the Binder phase — rendering the
/// immutable <see cref="BoundCompilation"/> to an executable — sits behind this interface, so a second backend
/// (direct CIL, PHASE 16) is a new implementation, never a fork of the pipeline. <see cref="Emit"/> performs NO
/// binding and NO edition gating (the P3/P6 contract: it is unreachable when any diagnostics exist).
/// </summary>
internal interface ICodeGenBackend
{
    /// <summary>Which backend this is.</summary>
    BackendId Id { get; }

    /// <summary>Render <paramref name="program"/> and produce the executable per <paramref name="options"/>.</summary>
    BackendArtifact Emit(BoundCompilation program, BackendOptions options);
}

/// <summary>Constructs the selected backend (DESIGN-codegen-backend §2.2).</summary>
internal static class BackendFactory
{
    /// <summary>Create the backend for <paramref name="id"/>.
    /// <para><b>INTERIM SHAPE (P7 Step 1 deviation, recorded in the phase ledger):</b> the design's factory is
    /// parameterless, but until P9 relocates the OO bind bodies off <see cref="CSharpEmitter"/> (the documented
    /// P6→P9 seam, deleted at P9 Step 4), <c>EmitBound</c> historically ran on the SAME emitter instance that hosted
    /// <c>Bind</c> — so the driver passes that instance through here. When P9 makes the emitter stateless w.r.t.
    /// binding, this parameter disappears and the factory matches the design verbatim.</para></summary>
    public static ICodeGenBackend For(BackendId id, CSharpEmitter bindHost) => id switch
    {
        BackendId.Roslyn => new RoslynBackend(bindHost),
        _ => throw new NotSupportedException($"backend {id} is not implemented (PHASE 16)"),
    };
}
