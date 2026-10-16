// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB3163 — A RUN UNIT EMBEDDED IN A HOST PROCESS NEVER WRITES THE HOST'S EXIT CODE (docs/CONFORMANCE.md
/// DOC-A.1-167: "STOP RUN, and a fatal exception condition that terminates the run unit (§14.6.12), end the RUN UNIT
/// ONLY — never the host process"; "A standalone <c>cobol</c> executable IS its own host"). The termination status of a
/// run unit — STOP's STATUS operand (§14.9.42.4 GR5: the value is "passed to the operating system") or the
/// abnormal-termination indication (§14.6.12) — is the RUN UNIT's own state (<c>RunUnit.ExitStatus</c>): the process exit code
/// is the PROCESS's, and only the boundary that IS the process entry (the generated <c>Main</c>'s
/// <c>ProgramRegistry.RunModule</c>) delivers the status to it. §14.6.1 — "A run unit is an independent entity that may be
/// executed without communicating with, or being coordinated with, any other run unit" — is the standard's side of it: a
/// run unit that rewrote a process-wide cell would coordinate with every other run unit, and with the host, through it.
/// <para>The host here is a real C# program compiled with Roslyn and run as its own process, exactly as an embedding
/// host would be. It sets its OWN exit code (5) first, then runs three run units through the host entry
/// (<c>ProgramRegistry.RunMain</c>): one ending in <c>STOP RUN WITH ERROR STATUS 24</c>, one in a fatal CALL of an
/// absent program (§14.9.4.4 GR3, §14.6.13.1.3 rule 7 — the abnormal termination), and a status-free one beginning
/// after them. Each reports the process exit code and the run unit's own status. Expected, derived: the process exit code
/// stays 5 throughout (the host's own), the run unit's status is 24, then 1 (ERROR ⇒ 1, the item-192 mapping), then 0,
/// and the host process ends with the exit code it set. Before the fix the first two overwrote it (24, then 1) and a
/// new run unit's start restated it to 0.</para>
/// </summary>
public sealed class RunUnitHostExitCodeTests
{
    private const string StopProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. HXSTOP.
        PROCEDURE DIVISION.
        P1.
            STOP RUN WITH ERROR STATUS 24.
        """;

    private const string FatalProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. HXFATAL.
        PROCEDURE DIVISION.
        P1.
            CALL "HXABSENT".
            GOBACK.
        """;

    private const string QuietProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. HXQUIET.
        PROCEDURE DIVISION.
        P1.
            GOBACK.
        """;

    private const string Host = """
        using System;
        using System.IO;
        using CobolNet.Runtime;

        public static class HxHost
        {
            private static void Enter(string module, string path)
            {
                ProgramRegistry.Reset();
                var asm = System.Reflection.Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, module + ".dll"));
                var registrar = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<CobolNet.Runtime.Repository.CobolRepositoryAttribute>(asm)!.Registrar!;
                asm.GetType(registrar)!.GetMethod("EnsureRegistered", Type.EmptyTypes)!.Invoke(null, null);
                try { ProgramRegistry.RunMain(path); }
                catch (StopRun) { }
                Console.WriteLine(path + " process=" + Environment.ExitCode + " run-unit=" + RunUnit.Current.ExitStatus);
            }

            public static void Main()
            {
                Environment.ExitCode = 5;
                Enter("HXSTOP", "HXSTOP");
                Enter("HXFATAL", "HXFATAL");
                Enter("HXQUIET", "HXQUIET");
            }
        }
        """;

    [Fact]
    public void ARunUnitEndedUnderAHost_LeavesTheHostProcessExitCodeAlone()
    {
        string dir = CutRunner.NewTempDir("hxexit");
        try
        {
            NonCobolElement.CompileCobol(dir, "HXSTOP", StopProgram);
            NonCobolElement.CompileCobol(dir, "HXFATAL", FatalProgram);
            NonCobolElement.CompileCobol(dir, "HXQUIET", QuietProgram);
            NonCobolElement.CompileCSharp(dir, "HXHOST", Host, runtimeConfigOf: "HXSTOP");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "HXHOST.dll"), dir);
            Assert.Equal(
                "HXSTOP process=5 run-unit=24\n" +
                "HXFATAL process=5 run-unit=1\n" +
                "HXQUIET process=5 run-unit=0", stdout);
            Assert.StartsWith("abnormal run-unit termination: ", stderr);   // §14.6.12 — the diagnostic is the run unit's own output
            Assert.Equal(5, exit);                                           // the process ends with the exit code its host set
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
