// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The run-unit TERMINATION epilogue (ISO §14.6.11 normal / §14.6.12 abnormal run-unit termination), owned by the
/// runtime's RunMain boundary (<c>ProgramTable.RunMain</c>) so it is RUN-UNIT-scoped — each observable applies to the
/// whole run unit, incl. a separately-compiled CALLed module, not just the main compilation group (§24 review ledger
/// V52/V53, the sibling class the V47 review surfaced). <b>V52</b>: a fatal exception condition (§14.6.13.1.3 #7)
/// terminates the run unit with the documented abnormal surface — a stderr diagnostic + a nonzero exit — even when the
/// MAIN group is EC-free (so a compile-time EC scan of the main group would never emit a catch). <b>V53</b>: §14.6.11(2)'s
/// implicit CLOSE of ALL open run-unit connectors reaches a file opened by a sibling module even when the main declares
/// no files. Both are asserted through separate processes (the exit code / on-disk file is the observable).
/// </summary>
public sealed class RunUnitTerminationTests
{
    private static void CompileTo(string source, string dir, string name)
    {
        string src = Path.Combine(dir, name + ".cob");
        src = CompiledProgramCache.StageSource(src, source);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r.Success, $"compile {name}: {string.Join("; ", r.Errors)}");
    }

    // ── V52: a fatal EC in an EC-free MAIN reaches the §14.6.12 abnormal-termination surface (not a raw CLR crash) ──

    /// <summary>V52: a null BASED-item dereference raises EC-DATA-PTR-NULL (Fatal, §13.18.5 GR3) UNCONDITIONALLY —
    /// it is not gated on the EC model, so this program declares no EC feature and its generated <c>Main</c> carries
    /// no fatal-EC catch. The runtime's RunMain boundary must still convert the fatal into the documented
    /// abnormal-termination surface (§14.6.12: the OS "shall indicate an abnormal termination"): a stderr diagnostic
    /// + a nonzero exit — NOT an unhandled .NET exception. stdout shows only the pre-fatal line.</summary>
    [Fact]
    public void FatalEc_InEcFreeMain_RaisesAbnormalTerminationSurface()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V52PTR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BP PIC X(4) BASED.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "before".
                DISPLAY BP.
                DISPLAY "after".
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.Equal("before", stdout);                             // the fatal aborts before "after"
        Assert.Equal(1, exit);                                      // §14.6.12 nonzero abnormal-termination exit
        Assert.Contains("abnormal run-unit termination", stderr);  // the documented surface, not a raw CLR trace
        Assert.Contains("EC-DATA-PTR-NULL", stderr);
    }

    /// <summary>V52 cross-assembly: the fatal originates in a SEPARATELY-COMPILED CALLed module while the main group
    /// is EC-free and file-free. The abnormal surface is still produced — the main's <c>Main</c> never saw the sub's
    /// descriptors, so only a run-unit-side boundary (RunMain) can catch it. This is the exact shape the V52 finding
    /// filed.</summary>
    [Fact]
    public void FatalEc_InSeparatelyCompiledSub_RaisesAbnormalTerminationSurface()
    {
        const string main = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V52XMN.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "before".
                CALL "V52XSB".
                DISPLAY "after".
                STOP RUN.
            """;
        const string sub = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V52XSB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BP PIC X(4) BASED.
            PROCEDURE DIVISION.
            SUBP.
                DISPLAY BP.
                GOBACK.
            """;
        string dir = CutRunner.NewTempDir("v52xasm");
        try
        {
            CompileTo(main, dir, "V52XMN");
            CompileTo(sub, dir, "V52XSB");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "V52XMN.dll"), dir);
            Assert.Equal("before", stdout);                             // "after" never prints — the fatal ends the run unit
            Assert.Equal(1, exit);
            Assert.Contains("abnormal run-unit termination", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    // ── V53: the §14.6.11 implicit CLOSE reaches a file opened by a separately-compiled sub ──

    /// <summary>V53: §14.6.11(2) — "an implicit CLOSE … for each file that is in the open mode … for all open files
    /// in the run unit". A file-less MAIN CALLs a separately-compiled sub that OPENs OUTPUT and WRITEs a record
    /// WITHOUT closing; the run-unit-termination implicit CLOSE (runtime-side, RunMain) must still flush and close
    /// the sub's connector so its output is on disk. Pre-fix the CloseAll was gated on the MAIN group's own file
    /// declarations, so a file-less main never closed the sibling's file.</summary>
    [Fact]
    public void ImplicitClose_ReachesFileOpenedBySeparatelyCompiledSub()
    {
        const string main = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V53MN.
            PROCEDURE DIVISION.
            MAIN.
                CALL "V53SB".
                STOP RUN.
            """;
        const string sub = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V53SB.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "v53out.dat" ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(5).
            PROCEDURE DIVISION.
            SUBP.
                OPEN OUTPUT F.
                MOVE "HELLO" TO F-REC.
                WRITE F-REC.
                GOBACK.
            """;
        string dir = CutRunner.NewTempDir("v53xasm");
        try
        {
            CompileTo(main, dir, "V53MN");
            CompileTo(sub, dir, "V53SB");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "V53MN.dll"), dir);
            Assert.Equal(0, exit);
            string outFile = Path.Combine(dir, "v53out.dat");
            Assert.True(File.Exists(outFile), $"the sub's output file was not created at run-unit termination (stderr: {stderr})");
            Assert.Contains("HELLO", File.ReadAllText(outFile));
        }
        finally { CutRunner.TryDelete(dir); }
    }

    // ── V52 (CALL/CANCEL family): a fatal CobolCallException reaches the same abnormal surface, not a raw crash ──

    /// <summary>V52 completion: a CALL to a program not in the run unit, with NO ON EXCEPTION phrase and no
    /// EC-PROGRAM checking, is a fatal §14.9.4.4 GR3b failure (EC-PROGRAM-NOT-FOUND — Table 13 Fatal). It reaches the
    /// run-unit boundary as a <c>CobolCallException</c> (a sibling of <c>CobolFatalException</c>, not a subclass), and
    /// RunMain must convert it to the SAME §14.6.12 abnormal-termination surface — the CobolCallException docstring's
    /// "the run unit terminates loudly (abnormal termination)" — not a raw CLR crash (exit 127). Covers the whole
    /// CALL/CANCEL fatal family, incl. V52's own cross-assembly sibling-module-not-located case.</summary>
    [Fact]
    public void FatalCallNotFound_InEcFreeMain_RaisesAbnormalTerminationSurface()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V52CALL.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "before".
                CALL "NOSUCHPG".
                DISPLAY "after".
                STOP RUN.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.Equal("before", stdout);
        Assert.Equal(1, exit);
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains("EC-PROGRAM-NOT-FOUND", stderr);
    }

    // ── V52 × V53: the abnormal path still ATTEMPTS the §14.6.11 implicit CLOSE (both relocated pieces fire) ──

    /// <summary>§14.6.12 requires the abnormal-termination path to ATTEMPT the §14.6.11 normal-termination operations
    /// ("performs all operations that are possible"). A program that OPENs OUTPUT + WRITEs an unclosed record and
    /// THEN hits a fatal EC must both exit 1 (abnormal) AND have its buffered record flushed/closed — RunMain's
    /// fatal catch and its finally-CloseAll must both fire, in order, on the abnormal path. Locks the interaction of
    /// the two pieces this change relocated (the completeness critic's coverage gap).</summary>
    [Fact]
    public void AbnormalTermination_StillClosesOpenFiles()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V52F.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "v52f.dat" ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 BP PIC X(4) BASED.
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT F.
                MOVE "HELLO" TO F-REC.
                WRITE F-REC.
                DISPLAY BP.
                STOP RUN.
            """;
        string dir = CutRunner.NewTempDir("v52f");
        try
        {
            CompileTo(source, dir, "V52F");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "V52F.dll"), dir);
            Assert.Equal(1, exit);                                      // abnormal — the fatal fires after the WRITE
            Assert.Contains("abnormal run-unit termination", stderr);
            string outFile = Path.Combine(dir, "v52f.dat");
            Assert.True(File.Exists(outFile), $"an open file must be flushed/closed even on abnormal termination (stderr: {stderr})");
            Assert.Contains("HELLO", File.ReadAllText(outFile));       // §14.6.12 → §14.6.11(2) best-effort CLOSE
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
