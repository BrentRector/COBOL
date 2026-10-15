// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The witness for Annex A.1 item 65 — "EXIT and GOBACK statements (execution continuation in a non-COBOL runtime
/// element)" — and the determination <c>docs/CONFORMANCE.md</c> DOC-A.1-65 records for it. ISO/IEC 1989:2023
/// §14.9.18.4 1) a) (cite.py OK): "If the activating runtime element is a non-COBOL element, execution continues in
/// the activating element in an implementor-defined fashion"; EXIT PROGRAM reaches the same rule through
/// §14.9.14.4 3) (cite.py OK: "execution proceeds as specified in 14.9.18, GOBACK statement, General rules 3 and 4").
/// <para>DETERMINATION PINNED: the only non-COBOL activator is a .NET host, which references the compiled assembly
/// and activates a program through the program's generated host entry, <c>Cobol.&lt;assembly&gt;.&lt;NAME&gt;.Call()</c>
/// (DESIGN-external-repository §4.3; A.1 item 141), the one host interface. When the program executes GOBACK or EXIT
/// PROGRAM the host's call RETURNS NORMALLY and the host continues at its own call site: no .NET exception reaches
/// it and the statements after the GOBACK / EXIT PROGRAM are not executed.</para>
/// <para>The corpus cannot carry this (every golden's activator is a COBOL main), so the non-COBOL element is a real
/// one: a C# host program compiled here with Roslyn and run as its own process beside the COBOL modules, exactly
/// as a user's host would be.</para>
/// </summary>
public sealed class NonCobolActivatorReturnTests
{
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
    /// the activating element"; a .NET exception escaping a host entry would end it before HOST DONE.</summary>
    private const string Host = """
        using System;

        public static class L1HostProgram
        {
            public static int Main()
            {
                Console.WriteLine("HOST BEFORE");
                Cobol.L1HSTGB.L1HSTGB.Call();
                Console.WriteLine("HOST RESUMED AFTER GOBACK");
                Cobol.L1HSTEP.L1HSTEP.Call();
                Console.WriteLine("HOST RESUMED AFTER EXIT PROGRAM");
                Cobol.L1HSTGB.L1HSTGB.Call();
                Console.WriteLine("HOST DONE");
                return 0;
            }
        }
        """;

    [Fact]
    public void GobackAndExitProgram_ReturnToANonCobolHost_WhichContinuesAtItsCallSite()
    {
        string dir = CutRunner.NewTempDir("l1host");
        try
        {
            NonCobolElement.CompileCobol(dir, "L1HSTGB", GobackSub);
            NonCobolElement.CompileCobol(dir, "L1HSTEP", ExitProgramSub);
            NonCobolElement.CompileCSharp(dir, "L1HSTHOST", Host, "L1HSTGB", "L1HSTGB", "L1HSTEP");
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
