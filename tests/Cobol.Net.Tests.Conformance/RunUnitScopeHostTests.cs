// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1069 — RUN-UNIT STATE IS SCOPED TO THE RUN UNIT when a host runs a compiled program TWICE in one .NET
/// process (docs/CONFORMANCE.md DOC-A.1-167 / DOC-A.1-168). Each execution of the program's generated <c>Main</c>
/// is a run unit of its own: the driver's first statement begins it (<c>ProgramRegistry.Reset()</c> →
/// <c>RunUnit.Begin()</c>), and the second run unit must inherit nothing from the first.
/// <para>The rules, each through <c>cite.py --check</c>:</para>
/// <list type="bullet">
/// <item>§14.6.1 — "A run unit is an independent entity that may be executed without communicating with, or being
/// coordinated with, any other run unit" — with §12.3.7.4 4) "The implementor defines the scope (program, run unit,
/// etc.) of each external switch", which this implementation defines as the RUN UNIT (NOTE 1's case; SwitchStore).
/// So SET SW1 TO ON in run unit 1 is not visible to run unit 2, whose switch starts from its external setting:
/// none, i.e. OFF.</item>
/// <item>§9.3.14.2 — "A factory object is created before it is first referenced by a run unit" and "deleted after
/// it is last referenced by a run unit" (so run unit 2 references a NEW factory object, whose factory data is
/// again its VALUE-initialized state), which is also what §14.6.11 4) "All instance objects are destroyed" needs
/// of every object reachable from factory data.</item>
/// </list>
/// <para>Expected output, derived: each run unit prints "SW1 OFF" (no external setting, and the previous run
/// unit's SET does not reach it) and "COUNT 1" (its own factory's CNT, VALUE 0, bumped once). Before the fix the
/// second printed "SW1 ON" and "COUNT 2": the switch store lived on the SAME ambient RunUnit object, which the
/// driver only partly reset, and the factory object was a process-lifetime static.</para>
/// <para>A standalone executable is one run unit per process, so the corpus cannot see this: the activator here is
/// a real C# host compiled with Roslyn and run as its own process, exactly as an embedding host would be.</para>
/// </summary>
public sealed class RunUnitScopeHostTests
{
    private const string Program = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RUSCOPE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF.
        REPOSITORY.
            CLASS RUSCNT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 N PIC 9.
        PROCEDURE DIVISION.
        P1.
            IF SW1-ON
                DISPLAY "SW1 ON"
            ELSE
                DISPLAY "SW1 OFF"
            END-IF
            SET SW1 TO ON
            INVOKE RUSCNT "BUMP" RETURNING N
            DISPLAY "COUNT " N
            GOBACK.
        END PROGRAM RUSCOPE.

        IDENTIFICATION DIVISION.
        CLASS-ID. RUSCNT.
        IDENTIFICATION DIVISION.
        FACTORY.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 CNT PIC 9 VALUE 0.
        PROCEDURE DIVISION.
        METHOD-ID. BUMP.
        DATA DIVISION.
        LINKAGE SECTION.
        01 R PIC 9.
        PROCEDURE DIVISION RETURNING R.
            ADD 1 TO CNT
            MOVE CNT TO R.
        END METHOD BUMP.
        END FACTORY.
        IDENTIFICATION DIVISION.
        OBJECT.
        END OBJECT.
        END CLASS RUSCNT.
        """;

    /// <summary>The host: run the compiled program's entry point twice in ONE process.</summary>
    private const string Host = """
        using System;
        using System.IO;

        public static class RuScopeHost
        {
            public static int Main()
            {
                var asm = System.Reflection.Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "RUSCOPE.dll"));
                for (int run = 1; run <= 2; run++)
                {
                    Console.WriteLine("RUN " + run);
                    asm.EntryPoint!.Invoke(null, null);
                }
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
            .ToList();
        var compilation = CSharpCompilation.Create("RUSCOPEHOST",
            [CSharpSyntaxTree.ParseText(Host)], refs,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));
        var emit = compilation.Emit(Path.Combine(dir, "RUSCOPEHOST.dll"));
        Assert.True(emit.Success, string.Join("\n", emit.Diagnostics));
        File.Copy(Path.Combine(dir, "RUSCOPE.runtimeconfig.json"), Path.Combine(dir, "RUSCOPEHOST.runtimeconfig.json"));
    }

    [Fact]
    public void ASecondRunUnitInOneProcess_InheritsNoSwitchAndNoFactoryData()
    {
        string dir = CutRunner.NewTempDir("ruscope");
        try
        {
            string src = CompiledProgramCache.StageSource(Path.Combine(dir, "RUSCOPE.cob"), Program);
            var r = CompiledProgramCache.Compile(
                new CompilerDriver.Options(src, Path.Combine(dir, "RUSCOPE.dll"), DialectLevel: 2002));
            Assert.True(r.Success, $"compile RUSCOPE: {string.Join("; ", r.Errors)}");
            CompileHost(dir);
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "RUSCOPEHOST.dll"), dir);
            Assert.Equal(
                "RUN 1\n" +
                "SW1 OFF\n" +
                "COUNT 1\n" +
                "RUN 2\n" +
                "SW1 OFF\n" +
                "COUNT 1", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
