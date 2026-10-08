// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE ONE BUILDER OF A REAL NON-COBOL ELEMENT FOR A TEST (kb/Work PB2671). The corpus cannot carry a rule whose
/// subject is a non-COBOL runtime element (every golden's activator is the operating system), so the tests that pin
/// those determinations build one: a C# assembly compiled here with Roslyn and placed beside WiseOwl COBOL modules,
/// which a .NET host loads exactly as a user's would. Before this the Roslyn reference set and the host's runtime
/// configuration were written out in each such test, three copies that could drift (a host missing the runtime
/// reference compiles in one test and not the next).
/// </summary>
internal static class NonCobolElement
{
    /// <summary>Compile COBOL <paramref name="source"/> to <c>&lt;dir&gt;/&lt;name&gt;.dll</c> at
    /// <paramref name="dialect"/>, asserting success — a module a host loads by its file name.</summary>
    public static void CompileCobol(string dir, string name, string source, int dialect = 2023)
    {
        string src = CompiledProgramCache.StageSource(Path.Combine(dir, name + ".cob"), source);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"),
            DialectLevel: dialect, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r.Success, $"compile {name}: {string.Join("; ", r.Errors)}");
    }

    /// <summary>Compile C# <paramref name="source"/> to <c>&lt;dir&gt;/&lt;assemblyName&gt;.dll</c> against the
    /// framework and the WiseOwl COBOL runtime. A HOST (<paramref name="runtimeConfigOf"/> names a COBOL module in
    /// <paramref name="dir"/>) is a console application and takes that module's runtime configuration, because it is
    /// a framework-dependent app like the modules beside it; a LIBRARY (<paramref name="runtimeConfigOf"/> null) is a
    /// class library.</summary>
    public static void CompileCSharp(string dir, string assemblyName, string source, string? runtimeConfigOf)
    {
        var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var refs = tpa
            .Where(p => Path.GetFileNameWithoutExtension(p) is "System.Private.CoreLib" or "System.Runtime"
                or "System.Console" or "System.Reflection" or "netstandard")
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Append(MetadataReference.CreateFromFile(typeof(CobolNet.Runtime.ProgramRegistry).Assembly.Location))
            .ToList();
        var compilation = CSharpCompilation.Create(assemblyName, [CSharpSyntaxTree.ParseText(source)], refs,
            new CSharpCompilationOptions(runtimeConfigOf is null ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Enable));
        var emit = compilation.Emit(Path.Combine(dir, assemblyName + ".dll"));
        Assert.True(emit.Success, string.Join("\n", emit.Diagnostics));
        if (runtimeConfigOf is not null)
            File.Copy(Path.Combine(dir, runtimeConfigOf + ".runtimeconfig.json"), Path.Combine(dir, assemblyName + ".runtimeconfig.json"));
    }
}
