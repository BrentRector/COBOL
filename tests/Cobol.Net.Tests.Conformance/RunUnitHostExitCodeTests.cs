// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB3163 — A RUN UNIT EMBEDDED IN A HOST PROCESS NEVER WRITES THE HOST'S EXIT CODE (docs/CONFORMANCE.md
/// DOC-A.1-167: "STOP RUN, and a fatal exception condition that terminates the run unit (§14.6.12), end the RUN UNIT
/// ONLY — never the host process"; "A standalone <c>cobol</c> executable IS its own host"). The termination status of a
/// run unit — STOP's STATUS operand (§14.9.42.4 GR5: the value is "passed to the operating system") or the
/// abnormal-termination indication (§14.6.12) — is the RUN UNIT's own state: under a host it travels in the
/// <c>RunUnitEndedException</c> the outermost boundary throws (owner decision R48), and only the boundary that IS the
/// process entry (the generated <c>Main</c>) delivers a status to the process exit code. §14.6.1 — "A run unit is an
/// independent entity that may be executed without communicating with, or being coordinated with, any other run unit" —
/// is the standard's side of it: a run unit that rewrote a process-wide cell would coordinate with every other run unit,
/// and with the host, through it.
/// <para>The host here is a real C# program compiled with Roslyn and run as its own process, exactly as an embedding
/// host would be. It sets its OWN exit code (5) first, then activates four programs through their generated host entries
/// (<c>Cobol.&lt;assembly&gt;.&lt;NAME&gt;.Call</c>, DESIGN-external-repository §4.3, the one host surface of §4.4): one
/// ending in <c>STOP RUN WITH ERROR STATUS 24</c>, one ending abnormally on a fatal EC-DATA-PTR-NULL (a NULL BASED
/// reference, §14.6.12), one whose CALL of an absent program fails (§14.9.4.4 GR3 b), which the host receives as the
/// condition itself and which ends no run unit, and a status-free one. After each it reports the process exit code.
/// Expected, derived: the process exit code stays 5 throughout (the host's own); the host receives status 24 (normal),
/// then status 1 (ERROR ⇒ 1, the item-192 mapping; abnormal), then EC-PROGRAM-NOT-FOUND; the run unit current after the
/// last activation has status 0; and the host process ends with the exit code it set.</para>
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
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 BP PIC X(4) BASED.
        PROCEDURE DIVISION.
        P1.
            DISPLAY BP.
            GOBACK.
        """;

    private const string NotFoundProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. HXNOTFND.
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
        using CobolNet.Runtime;

        public static class HxHost
        {
            private static void Enter(string name, Action call)
            {
                string outcome;
                try { call(); outcome = "returned"; }
                catch (RunUnitEndedException ended) { outcome = $"ended status={ended.ExitStatus} abnormal={ended.Abnormal}"; }
                catch (CobolCallException failed) { outcome = "call failed " + failed.EcName; }
                Console.WriteLine(name + " process=" + Environment.ExitCode + " " + outcome);
            }

            public static void Main()
            {
                Environment.ExitCode = 5;
                Enter("HXSTOP", Cobol.HXSTOP.HXSTOP.Call);
                Enter("HXFATAL", Cobol.HXFATAL.HXFATAL.Call);
                Enter("HXNOTFND", Cobol.HXNOTFND.HXNOTFND.Call);
                Enter("HXQUIET", Cobol.HXQUIET.HXQUIET.Call);
                Console.WriteLine("run-unit=" + RunUnit.Current.ExitStatus);
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
            NonCobolElement.CompileCobol(dir, "HXNOTFND", NotFoundProgram);
            NonCobolElement.CompileCobol(dir, "HXQUIET", QuietProgram);
            NonCobolElement.CompileCSharp(dir, "HXHOST", Host, "HXSTOP", "HXSTOP", "HXFATAL", "HXNOTFND", "HXQUIET");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "HXHOST.dll"), dir);
            Assert.Equal(
                "HXSTOP process=5 ended status=24 abnormal=False\n" +    // §14.9.42.4 GR5: the status reaches the host
                "HXFATAL process=5 ended status=1 abnormal=True\n" +     // §14.6.12: the abnormal termination is indicated
                "HXNOTFND process=5 call failed EC-PROGRAM-NOT-FOUND\n" + // §14.9.4.4 GR3 b: the host receives the condition
                "HXQUIET process=5 returned\n" +
                "run-unit=0", stdout);
            Assert.Equal("", stderr);    // every ending reached the host as a value, none as a process-level report
            Assert.Equal(5, exit);       // the process ends with the exit code its host set
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
