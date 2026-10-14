// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The witness for owner decision R48 and Annex A.1 item 167 (<c>docs/CONFORMANCE.md</c> DOC-A.1-167): under a .NET host,
/// a STOP RUN or a fatal exception condition that terminates the run unit ends the RUN UNIT only. The outermost
/// activation boundary, <c>ProgramTable.CallProgram</c> with <c>callerPath: null</c> (an activator outside every
/// program; DESIGN-external-repository §4.1, §4.3 "The exception contract" item 3), performs the §14.6.11 actions and
/// hands the host ONE documented exception, <c>RunUnitEndedException</c>, carrying the exit status and whether the
/// termination was abnormal. The host process continues, and its next activation is in a new run unit that inherits
/// nothing.
/// <para>ISO/IEC 1989:2023 §14.2.3 13) (cite.py OK): "When either the activating or the activated runtime element is
/// other than a COBOL runtime element, the implementor shall specify the restrictions and mechanisms for all supported
/// language products". §14.9.42.4 1) and 6) (cite.py OK): STOP performs "The operations described in 14.6.11, Normal run
/// unit termination" and "Execution of the run unit terminates and control is transferred to the operating system"; a
/// host is that system's counterpart here. §14.6.11 2) (cite.py OK): "An implicit CLOSE statement without any phrases is
/// executed for each file that is in the open mode". §14.6.12 (cite.py OK): "The operating system shall indicate an
/// abnormal termination of the run unit if such a capability exists within the operating system".</para>
/// <para>The non-COBOL element is real: a C# host compiled here with Roslyn and run as its own process beside the COBOL
/// modules, as a user's host would be (<see cref="NonCobolElement"/>).</para>
/// </summary>
public sealed class HostRunUnitEndingTests
{
    /// <summary>Counts its activations in WORKING-STORAGE (last-used state within one run unit, §14.6.2.3.3), so the
    /// count shows whether a later activation is in the same run unit or a new one.</summary>
    private const string Counter = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R48CNT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-COUNT PIC 9 VALUE 0.
        PROCEDURE DIVISION.
        P1.
            ADD 1 TO WS-COUNT.
            DISPLAY "COUNT " WS-COUNT.
            GOBACK.
        END PROGRAM R48CNT.
        """;

    /// <summary>Opens a file, writes a record and never closes it, then STOP RUN with a status: the §14.6.11 2) implicit
    /// CLOSE is what puts the record on disk, and the status is what the host receives.</summary>
    private const string Stopper = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R48STP.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "r48out.dat" ORGANIZATION IS LINE SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 F-REC PIC X(5).
        PROCEDURE DIVISION.
        P1.
            OPEN OUTPUT F.
            MOVE "HELLO" TO F-REC.
            WRITE F-REC.
            DISPLAY "IN STOP SUB".
            STOP RUN WITH ERROR STATUS 7.
            DISPLAY "NOT REACHED AFTER STOP RUN".
        END PROGRAM R48STP.
        """;

    /// <summary>A NULL BASED-item reference raises EC-DATA-PTR-NULL, a fatal condition, unconditionally: the run unit
    /// terminates abnormally (§14.6.12).</summary>
    private const string Fatal = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R48FTL.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 BP PIC X(4) BASED.
        PROCEDURE DIVISION.
        P1.
            DISPLAY "IN FATAL SUB".
            DISPLAY BP.
            DISPLAY "NOT REACHED AFTER FATAL".
        END PROGRAM R48FTL.
        """;

    /// <summary>The host. It never names a run-unit-internal signal: every ending arrives as the one documented type, and
    /// a <c>StopRun</c> or <c>CobolFatalException</c> escaping to it would end the process before HOST DONE with a
    /// nonzero exit and a .NET trace on stderr.</summary>
    private const string Host = """
        using System;
        using System.IO;
        using CobolNet.Runtime;

        public static class R48HostProgram
        {
            private static void Activate(string name)
            {
                try { ProgramRegistry.CallProgram(name, null, Array.Empty<CobolArg>(), null); }
                catch (RunUnitEndedException ended)
                {
                    Console.WriteLine($"ENDED STATUS={ended.ExitStatus} ABNORMAL={ended.Abnormal}");
                    if (ended.Abnormal) Console.WriteLine("REASON NAMES EC-DATA-PTR-NULL=" + ended.Message.Contains("EC-DATA-PTR-NULL"));
                }
            }

            public static int Main()
            {
                Activate("R48CNT");
                Activate("R48CNT");
                Activate("R48STP");
                Console.WriteLine("FILE " + File.ReadAllText("r48out.dat").TrimEnd());
                Activate("R48CNT");
                Activate("R48FTL");
                Activate("R48CNT");
                RunUnit.Run(_ =>
                {
                    Activate("R48CNT");
                    Activate("R48STP");
                    Activate("R48CNT");
                });
                Console.WriteLine("HOST DONE");
                return 0;
            }
        }
        """;

    [Fact]
    public void StopRunAndFatalCondition_EndTheRunUnitOnly_AndReachTheHostAsOneDocumentedException()
    {
        string dir = CutRunner.NewTempDir("r48host");
        try
        {
            NonCobolElement.CompileCobol(dir, "R48CNT", Counter);
            NonCobolElement.CompileCobol(dir, "R48STP", Stopper);
            NonCobolElement.CompileCobol(dir, "R48FTL", Fatal);
            NonCobolElement.CompileCSharp(dir, "R48HOST", Host, runtimeConfigOf: "R48CNT");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "R48HOST.dll"), dir);
            Assert.Equal(
                "COUNT 1\n" +
                "COUNT 2\n" +                                   // the same run unit: last-used state
                "IN STOP SUB\n" +
                "ENDED STATUS=7 ABNORMAL=False\n" +             // §14.9.42.4 5): the status reaches the host
                "FILE HELLO\n" +                                // §14.6.11 2): the implicit CLOSE ran
                "COUNT 1\n" +                                   // a new run unit: initial state
                "IN FATAL SUB\n" +
                "ENDED STATUS=1 ABNORMAL=True\n" +              // §14.6.12: the abnormal termination is indicated
                "REASON NAMES EC-DATA-PTR-NULL=True\n" +
                "COUNT 1\n" +
                "COUNT 1\n" +                                   // RunUnit.Run begins a fresh run unit
                "IN STOP SUB\n" +
                "ENDED STATUS=7 ABNORMAL=False\n" +
                "COUNT 1\n" +
                "HOST DONE", stdout);
            Assert.Equal(0, exit);                              // the host's own exit status, untouched by the run units
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
