// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO/IEC 1989:2023 §14.6.3 3) (Explicit and implicit transfers of control), the fatal-declarative sentence
/// (<c>cite.py --check 14.6.3 "the run unit is terminated abnormally as specified in 14.6.12"</c> → OK):
/// "In the declarative section for a fatal exception condition, there is no next executable statement after either
/// the last statement when the paragraph in which it appears is not being executed under the control of some other
/// COBOL statement, or ... In these cases, unless the exception occurs in a MERGE statement, a SORT statement, or an
/// I-O statement for which the implementor specifies otherwise, the run unit is terminated abnormally as specified
/// in 14.6.12, Abnormal run unit termination." (With §14.6.13.1.3 5): "If execution of the declarative completes
/// normally the execution of the run unit is terminated abnormally", cite.py OK.)
/// <para>An abnormal termination is observable only as the exit code, the stderr surface and the stdout that stops,
/// and a corpus golden must exit 0, so this sentence is pinned here. The rest of §14.6.3 3) (empty procedures,
/// EXIT PROGRAM, the implicit GOBACK) is the golden <c>2023/l1_transfer_of_control_implicit</c>.</para>
/// <para>The fatal condition is EC-SIZE-ZERO-DIVIDE (§14.7.5 2): "if the divisor in a divide operation ... is zero,
/// the EC-SIZE-ZERO-DIVIDE exception condition is set to exist", Fatal in Table 13), raised by a COMPUTE with no
/// SIZE ERROR phrase, so §14.6.13.1.3 1) does not apply and 5) selects the USE declarative. The declarative has no
/// RESUME, so it completes normally and there is no next executable statement.</para>
/// </summary>
public sealed class TransferOfControlFatalDeclarativeTests
{
    private static void AssertAbnormal(int exit, string stdout, string stderr, string expectedStdout)
    {
        Assert.Equal(expectedStdout, stdout);                       // nothing after the declarative runs
        Assert.Equal(1, exit);                                      // §14.6.12 — the abnormal-termination exit
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains("EC-SIZE-ZERO-DIVIDE", stderr);
    }

    /// <summary>First alternative: the raising statement is in a paragraph reached by fall-through (not under the
    /// control of another statement), so after the declarative's last statement there is no next executable
    /// statement — AFTER-FATAL never prints.</summary>
    [Fact]
    public void FatalDeclarative_OnMainLine_TerminatesAbnormally()
    {
        const string source = """
            >>TURN EC-SIZE-ZERO-DIVIDE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1TOCF1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9(3) VALUE 7.
            01 Z PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D-SZ SECTION.
                USE AFTER EXCEPTION CONDITION EC-SIZE-ZERO-DIVIDE.
            D-SZ-P.
                DISPLAY "DECL-RAN".
            END DECLARATIVES.
            MAIN SECTION.
            M-P.
                DISPLAY "BEFORE".
                COMPUTE X = X / Z.
                DISPLAY "AFTER-FATAL".
                STOP RUN.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, "BEFORE\nDECL-RAN");
    }

    /// <summary>First alternative again, with the raising statement inside a paragraph PERFORMed from another
    /// section. The declarative's last statement is not in the range of PERFORM PX, so control never returns to the
    /// PERFORM — neither AFTER-FATAL-IN-PX (the rest of the performed paragraph) nor AFTER-PERFORM prints. The second
    /// alternative (the declarative's own last statement inside an active PERFORM range) is not exercised.</summary>
    [Fact]
    public void FatalDeclarative_UnderAnActivePerformFromAnotherSection_TerminatesAbnormally()
    {
        const string source = """
            >>TURN EC-SIZE-ZERO-DIVIDE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1TOCF2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9(3) VALUE 7.
            01 Z PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D-SZ SECTION.
                USE AFTER EXCEPTION CONDITION EC-SIZE-ZERO-DIVIDE.
            D-SZ-P.
                DISPLAY "DECL-RAN".
            END DECLARATIVES.
            MAIN SECTION.
            M-P.
                DISPLAY "BEFORE".
                PERFORM PX.
                DISPLAY "AFTER-PERFORM".
                STOP RUN.
            S2 SECTION.
            PX.
                COMPUTE X = X / Z.
                DISPLAY "AFTER-FATAL-IN-PX".
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, "BEFORE\nDECL-RAN");
    }
}
