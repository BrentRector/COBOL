// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB2659 — the activation the run unit's stack cannot hold, end to end, on the arms the manifest goldens cannot
/// observe because the run unit ends abnormally (the golden harness requires exit 0). The resource is the stack an
/// activation needs (docs/CONFORMANCE.md DOC-A.1-14, -89, -102; <c>ActivationStack</c>): ISO §14.9.4.4 GR3c and
/// §8.4.3.2.4 GR6c make it EC-PROGRAM-RESOURCES for a CALL and a function activation, §14.9.23.4 GR7b EC-OO-METHOD for
/// a method. Each condition here is fatal and unhandled, so the run unit ends through the §14.6.12 abnormal termination
/// — the documented diagnostic and exit code 1 — and its §14.6.11 2) implicit CLOSE: "An implicit CLOSE statement
/// without any phrases is executed for each file that is in the open mode". Before the fix each program died of a CLR
/// stack overflow at a depth of about 250: no diagnostic a COBOL programmer can act on, and no CLOSE. The CALL arm
/// with ON EXCEPTION is the golden <c>conformance:2002/pb2659_call_resources_recursion</c>.
/// </summary>
public sealed class ActivationResourcesTests
{
    private static void CompileTo(string source, string dir, string name)
    {
        string src = CompiledProgramCache.StageSource(Path.Combine(dir, name + ".cob"), source);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"),
            DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r.Success, $"compile {name}: {string.Join("; ", r.Errors)}");
    }

    /// <summary>A RECURSIVE program with no ON EXCEPTION phrase and checking off: GR3h item 3 leaves the condition to
    /// §14.6.13.1, and the run unit ends abnormally — after the §14.6.11 CLOSE has written the record the program wrote
    /// before it began to recurse.</summary>
    [Fact]
    public void UnhandledCall_PastTheStack_EndsAbnormally_AndClosesTheRunUnitsFiles()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2659CL RECURSIVE.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT OUT-F ASSIGN TO "pb2659cl.txt"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD OUT-F.
            01 OUT-R PIC X(20).
            WORKING-STORAGE SECTION.
            01 DEPTH PIC 9(9) VALUE 0.
            PROCEDURE DIVISION.
                IF DEPTH = 0
                    OPEN OUTPUT OUT-F
                    MOVE "WRITTEN BEFORE" TO OUT-R
                    WRITE OUT-R
                    DISPLAY "OPENED"
                END-IF
                ADD 1 TO DEPTH
                CALL "PB2659CL"
                DISPLAY "NOT REACHED"
                GOBACK.
            """;
        string dir = CutRunner.NewTempDir("pb2659cl");
        try
        {
            CompileTo(source, dir, "PB2659CL");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "PB2659CL.dll"), dir);
            Assert.Equal("OPENED", stdout);
            Assert.Equal(1, exit);
            Assert.Contains("abnormal run-unit termination: CALL 'PB2659CL'", stderr, StringComparison.Ordinal);
            Assert.Contains("ISO §14.9.4.4 GR3c — EC-PROGRAM-RESOURCES", stderr, StringComparison.Ordinal);
            Assert.DoesNotContain("Stack overflow", stderr, StringComparison.Ordinal);
            Assert.Equal("WRITTEN BEFORE", File.ReadAllText(Path.Combine(dir, "pb2659cl.txt")).TrimEnd());
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>The function arm (§8.4.3.2.4 GR6c): a user-defined function that references itself with no end.</summary>
    [Fact]
    public void RecursiveFunction_PastTheStack_IsEcProgramResources()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. PB2659FN.
            DATA DIVISION.
            LINKAGE SECTION.
            01 N PIC 9(9).
            01 R PIC 9(9).
            PROCEDURE DIVISION USING N RETURNING R.
                ADD 1 TO N GIVING R
                MOVE FUNCTION PB2659FN(R) TO R
                GOBACK.
            END FUNCTION PB2659FN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2659FM.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION PB2659FN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9(9) VALUE 0.
            PROCEDURE DIVISION.
                DISPLAY "STARTED"
                MOVE FUNCTION PB2659FN(X) TO X
                DISPLAY "NOT REACHED"
                STOP RUN.
            END PROGRAM PB2659FM.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.Equal("STARTED", stdout);
        Assert.Equal(1, exit);
        Assert.Contains("FUNCTION 'PB2659FN'", stderr, StringComparison.Ordinal);
        Assert.Contains("ISO §8.4.3.2.4 GR6c — EC-PROGRAM-RESOURCES", stderr, StringComparison.Ordinal);
    }

    /// <summary>The method arm (§14.9.23.4 GR7b): a factory method that invokes itself with no end.</summary>
    [Fact]
    public void RecursiveMethod_PastTheStack_IsEcOoMethod()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            CLASS-ID. PB2659CC.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS PB2659CC.
            IDENTIFICATION DIVISION.
            FACTORY.
            PROCEDURE DIVISION.
            METHOD-ID. DIVE.
            PROCEDURE DIVISION.
                INVOKE SELF "DIVE"
                GOBACK.
            END METHOD DIVE.
            END FACTORY.
            END CLASS PB2659CC.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2659CM.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS PB2659CC.
            PROCEDURE DIVISION.
                DISPLAY "STARTED"
                INVOKE PB2659CC "DIVE"
                DISPLAY "NOT REACHED"
                STOP RUN.
            END PROGRAM PB2659CM.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.Equal("STARTED", stdout);
        Assert.Equal(1, exit);
        Assert.Contains("EC-OO-METHOD (fatal): INVOKE 'DIVE' of class 'PB2659CC'", stderr, StringComparison.Ordinal);
        Assert.Contains("ISO §14.9.23.4 GR7b", stderr, StringComparison.Ordinal);
    }
}
