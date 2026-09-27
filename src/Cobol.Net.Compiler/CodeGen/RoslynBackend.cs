// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace CobolNet.CodeGen;

/// <summary>
/// The C#-via-Roslyn backend (the primary <see cref="ICodeGenBackend"/>; P7 Step 1): renders the bound tree to
/// typed-native C# (through the bind-hosting <see cref="CSharpEmitter"/> — the P6→P9 interim, see
/// <see cref="BackendFactory.For"/>), compiles it into a runnable .NET assembly with Roslyn, and writes the
/// accompanying <c>.runtimeconfig.json</c> so the result runs with <c>dotnet &lt;name&gt;.dll</c>.
/// </summary>
internal sealed class RoslynBackend : ICodeGenBackend
{
    private readonly CSharpEmitter _emitter;

    internal RoslynBackend(CSharpEmitter bindHost) => _emitter = bindHost;

    /// <inheritdoc/>
    public BackendId Id => BackendId.Roslyn;

    /// <inheritdoc/>
    /// <remarks>Wraps the pre-seam pipeline VERBATIM (pure indirection, no output change): render C# from the
    /// bound tree (<c>CSharpEmitter.EmitBound</c>), write the <c>.g.cs</c> (when <c>WriteSource</c> — before
    /// compiling, so the debugging artifact survives a failed compile), then <see cref="Compile"/>.</remarks>
    public BackendArtifact Emit(Binding.Model.BoundCompilation program, BackendOptions options)
    {
        string csharp = _emitter.EmitBound(program, options.Inputs);
        var written = new List<string>();

        string outDir = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath)) is { Length: > 0 } d ? d : ".";
        Directory.CreateDirectory(outDir);
        string? csPath = null;
        if (options.WriteSource)
        {
            csPath = Path.ChangeExtension(options.OutputPath, ".g.cs");
            File.WriteAllText(csPath, csharp);
            written.Add(Path.GetFullPath(csPath));
        }

        var result = Compile(csharp, options.OutputPath, options.AssemblyName);
        if (result.Success)
        {
            written.Add(Path.GetFullPath(options.OutputPath));
            written.AddRange(AssemblyPackager.Package(options.OutputPath));   // P7 Step 2 — packaging off Compile
        }
        return new BackendArtifact(result.Success, result.Diagnostics, csPath,
            result.Success ? options.OutputPath : null, written);
    }

    /// <summary>The outcome of a backend compilation.</summary>
    /// <param name="Success">True if the assembly was produced.</param>
    /// <param name="Diagnostics">Roslyn diagnostics (errors + warnings) from compiling the generated C#.</param>
    public readonly record struct Result(bool Success, IReadOnlyList<Diagnostic> Diagnostics);

    /// <summary>
    /// Compile <paramref name="csharpSource"/> to a console assembly at <paramref name="outputDllPath"/>.
    /// PURE C#→assembly since P7 Step 2: the packaging side effects (runtimeconfig + runtime deploy) live in
    /// <see cref="AssemblyPackager"/>, invoked by <see cref="Emit"/> on success — the only writes here are the
    /// assembly itself and the failed-emit cleanup.
    /// </summary>
    /// <param name="assemblyName">The emitted assembly's simple name (the COBOL PROGRAM-ID).</param>
    public static Result Compile(string csharpSource, string outputDllPath, string assemblyName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            csharpSource, new CSharpParseOptions(LanguageVersion.Latest));

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            ReferenceAssemblies(),
            new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                // The generated source is machine-emitted and self-consistent; nullable warnings on it are noise.
                nullableContextOptions: NullableContextOptions.Disable,
                // §15.99.3 r3's object-code half, decided EXPLICITLY (kb/Work PB120): deterministic PE — the
                // COFF TimeDateStamp becomes a content hash and the MVID is content-derived, so the generated
                // object code provides NO compilation date and time and r3's "if provided in … the generated
                // object code" condition is not engaged (the only compilation time a produced program carries
                // is WHEN-COMPILED's own baked constant). Identical source + references → byte-identical
                // assembly (reproducible builds, the modern toolchain norm — csc has defaulted to
                // /deterministic since VS2017; only the Roslyn API's back-compat default is false).
                deterministic: true));

        EmitResult emit = compilation.Emit(outputDllPath);
        if (!emit.Success)
        {
            // A failed Emit leaves a partial/0-byte output file behind; running it dies with the misleading
            // "hostpolicy.dll required" error (the swept phantom-RUNERR class — ST133A/ST134A/SQ203A were
            // misread as environmental flakes). The output file exists only when the compile SUCCEEDED.
            try { if (File.Exists(outputDllPath)) File.Delete(outputDllPath); } catch (IOException) { }   // not a compilation input: cleanup of this compile's own output
        }

        return new Result(emit.Success, emit.Diagnostics);
    }

    /// <summary>
    /// The framework reference set: every assembly currently loaded as a trusted-platform assembly. This makes
    /// the generated program able to reference the same BCL the compiler itself runs on (System.Runtime,
    /// System.Console, System.Linq, …) without bundling a reference pack. CACHED for the process lifetime — the
    /// TPA set and the deployed runtime DLL are stable per process, and building the set does ~180
    /// <see cref="MetadataReference.CreateFromFile(string)"/> calls; the in-process test battery compiles thousands
    /// of times per run (rearchitecture P0 step 2 — behavior-neutral: an identical set, computed once).
    /// </summary>
    private static readonly Lazy<ImmutableArray<MetadataReference>> _referenceAssemblies =
        new(BuildReferenceAssemblies, LazyThreadSafetyMode.ExecutionAndPublication);

    private static ImmutableArray<MetadataReference> ReferenceAssemblies() => _referenceAssemblies.Value;

    private static ImmutableArray<MetadataReference> BuildReferenceAssemblies()
    {
        string tpa = (string)(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "");   // not a compilation input: the host's own framework reference set
        var refs = tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(static p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(static p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();
        // The WiseOwl COBOL runtime the generated program calls (CobolNum / CobolString).
        if (File.Exists(AssemblyPackager.RuntimePath))   // not a compilation input: the runtime is part of the compiler's own deployment
            refs.Add(MetadataReference.CreateFromFile(AssemblyPackager.RuntimePath));
        return [.. refs];
    }
}
