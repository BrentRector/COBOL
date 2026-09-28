// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The witness for Annex A.1 item 65 — "EXIT and GOBACK statements (execution continuation in a non-COBOL runtime
/// element)" — and the determination <c>docs/CONFORMANCE.md</c> DOC-A.1-65 records for it. ISO/IEC 1989:2023
/// §14.9.18.4 1) a) (cite.py OK): "If the activating runtime element is a non-COBOL element, execution continues in
/// the activating element in an implementor-defined fashion"; EXIT PROGRAM reaches the same rule through
/// §14.9.14.4 3) (cite.py OK: "execution proceeds as specified in 14.9.18, GOBACK statement, General rules 3 and 4").
/// <para>DETERMINATION PINNED: the only non-COBOL activator is a .NET host, which loads the compiled assembly, runs
/// its public <c>__CobolModule.Register()</c> and activates a program through
/// <c>ProgramRegistry.CallProgram(name, callerPath, args, returning)</c>. When the program executes GOBACK or EXIT
/// PROGRAM the host's call RETURNS NORMALLY and the host continues at its own call site: no .NET exception reaches
/// it and the statements after the GOBACK / EXIT PROGRAM are not executed.</para>
/// <para>The corpus cannot carry this (every golden's activator is a COBOL main), so the non-COBOL element is a real
/// one: a C# host program compiled here with Roslyn and run as its own process beside the COBOL modules, exactly
/// as a user's host would be.</para>
/// </summary>
public sealed class NonCobolActivatorReturnTests
{
    private static void CompileCobol(string source, string dir, string name)
    {
        string src = Path.Combine(dir, name + ".cob");
        src = CompiledProgramCache.StageSource(src, source);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r.Success, $"compile {name}: {string.Join("; ", r.Errors)}");
    }

    private const string GobackSub = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. L1HSTGB.
        PROCEDURE DIVISION.
        P1.
            DISPLAY "IN GOBACK SUB".
            GOBACK.
            DISPLAY "NOT REACHED AFTER GOBACK".
        END PROGRAM L1HSTGB.
        """;

    private const string ExitProgramSub = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. L1HSTEP.
        PROCEDURE DIVISION.
        P1.
            DISPLAY "IN EXIT-PROGRAM SUB".
            EXIT PROGRAM.
        P2.
            DISPLAY "NOT REACHED AFTER EXIT PROGRAM".
        END PROGRAM L1HSTEP.
        """;

    /// <summary>The non-COBOL activating element. Every line it prints is a checkpoint of "execution continues in
    /// the activating element"; a .NET exception escaping CallProgram would end it before HOST DONE.</summary>
    private const string Host = """
        using System;
        using System.IO;
        using CobolNet.Runtime;

        public static class L1HostProgram
        {
            public static int Main()
            {
                foreach (var module in new[] { "L1HSTGB", "L1HSTEP" })
                {
                    var asm = System.Reflection.Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, module + ".dll"));
                    asm.GetType("__CobolModule")!.GetMethod("Register", Type.EmptyTypes)!.Invoke(null, null);
                }
                Console.WriteLine("HOST BEFORE");
                ProgramRegistry.CallProgram("L1HSTGB", "", Array.Empty<CobolArg>(), null);
                Console.WriteLine("HOST RESUMED AFTER GOBACK");
                ProgramRegistry.CallProgram("L1HSTEP", "", Array.Empty<CobolArg>(), null);
                Console.WriteLine("HOST RESUMED AFTER EXIT PROGRAM");
                ProgramRegistry.CallProgram("L1HSTGB", "", Array.Empty<CobolArg>(), null);
                Console.WriteLine("HOST DONE");
                return 0;
            }
        }
        """;

    private static void CompileHost(string dir)
    {
        var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var refs = tpa
            .Where(p => Path.GetFileNameWithoutExtension(p) is "System.Private.CoreLib" or "System.Runtime"
                or "System.Console" or "System.Reflection" or "netstandard")
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Append(MetadataReference.CreateFromFile(typeof(CobolNet.Runtime.ProgramRegistry).Assembly.Location))
            .ToList();
        var compilation = CSharpCompilation.Create("L1HSTHOST",
            [CSharpSyntaxTree.ParseText(Host)], refs,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));
        var emit = compilation.Emit(Path.Combine(dir, "L1HSTHOST.dll"));
        Assert.True(emit.Success, string.Join("\n", emit.Diagnostics));
        // The host is a framework-dependent app like the COBOL modules beside it: reuse their runtimeconfig.
        File.Copy(Path.Combine(dir, "L1HSTGB.runtimeconfig.json"), Path.Combine(dir, "L1HSTHOST.runtimeconfig.json"));
    }

    [Fact]
    public void GobackAndExitProgram_ReturnToANonCobolHost_WhichContinuesAtItsCallSite()
    {
        string dir = CutRunner.NewTempDir("l1host");
        try
        {
            CompileCobol(GobackSub, dir, "L1HSTGB");
            CompileCobol(ExitProgramSub, dir, "L1HSTEP");
            CompileHost(dir);
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "L1HSTHOST.dll"), dir);
            Assert.Equal(
                "HOST BEFORE\n" +
                "IN GOBACK SUB\n" +
                "HOST RESUMED AFTER GOBACK\n" +
                "IN EXIT-PROGRAM SUB\n" +
                "HOST RESUMED AFTER EXIT PROGRAM\n" +
                "IN GOBACK SUB\n" +
                "HOST DONE", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
