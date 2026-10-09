// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

using CobolNet.Compiler.Oo;

namespace CobolNet.CodeGen;

using Core = CobolParserCore;

/// <summary>
/// The BIND-HOST facade of the Roslyn backend (P7 Step 9n — the recorded deviation from the original
/// "CSharpEmitter is gone" slogan): the emitter god class is dissolved — emission lives on
/// <see cref="ProgramEmitter"/> (run-unit orchestration) → <see cref="UnitEmitters"/> (the per-unit
/// composition root) → <see cref="DispatchEmitter"/>/<see cref="StatementEmitter"/>/<c>Verbs/*Emitter</c> —
/// and THIS class keeps only the two driver entries (<see cref="Bind"/>/<see cref="EmitBound"/>) — the OO
/// bind bodies moved to <see cref="Compiler.Oo.OoDriver"/> at P9 Step 4 (the IOoBindHost seam is deleted).
/// It emits NOTHING.
/// </summary>
public sealed partial class CSharpEmitter
{
    /// <summary>BIND the WHOLE compilation group in <paramref name="tree"/> to an immutable
    /// <see cref="BoundCompilation"/> (multi-unit run-unit binding — interprogram design D3 / SSOT §18 #8), under
    /// the targeted EDITION (<paramref name="edition"/> — bind-time rejection diagnostics accumulate there; the
    /// driver fails the compile when any exist, BEFORE emit). A thin shim over
    /// <see cref="BinderDriver.Bind"/> (rearch PHASE-06 Step 2 — the Binder phase owns the orchestration; the
    /// OO bind bodies live on <see cref="Compiler.Oo.OoDriver"/> since P9 Step 4). <see cref="EmitBound"/> renders C# from the result — codegen never runs on an errored
    /// tree.</summary>
    internal BoundCompilation Bind(Core.CompilationUnitContext tree, EditionContext? edition = null,
        CobolNet.Frontend.Preprocessor.DirectiveResults? directives = null)
        => new BinderDriver().Bind(tree, edition ?? new EditionContext(2023), directives);

    /// <summary>Render typed-native C# from an already-bound immutable <see cref="BoundCompilation"/> (the emit
    /// half of the bind/emit split) — a fresh <see cref="ProgramEmitter"/> per call; the compilation carries
    /// everything emission needs (incl. the OO class table + interface forests), so the emit side reads NO
    /// bind-host state.</summary>
    internal string EmitBound(BoundCompilation comp, string assemblySimpleName, CobolNet.Frontend.CompilationInputs? inputs = null)
        => new ProgramEmitter(inputs).Emit(comp, assemblySimpleName);

}
