// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The CHECKED arm of Annex A.1 item 218 — "USE statement (action taken following execution of the USE procedure
/// when I-O status value indicates a fatal exception condition)" — and the one arm no corpus golden can witness,
/// because the golden harness requires exit 0 and this arm ends in abnormal run-unit termination.
///
/// <para><b>The determination</b> (docs/CONFORMANCE.md §7, DOC-A.1-218): WiseOwl COBOL continues the run unit
/// (§9.1.13.1 "The implementor may either continue or terminate the execution of the run unit") and does NOT
/// "specify otherwise" under §14.6.13.1.3 rule 3 — "If the exception condition is a fatal EC-I-O exception
/// condition, then the rules for 9.1.13, I-O status apply, then if the implementor has not specified otherwise,
/// the following rules apply for all fatal exception conditions." So with EC-I-O checking ENABLED the rules after
/// rule 3 decide the action once the USE procedure has run:</para>
/// <list type="bullet">
/// <item>rule 5 — "If execution of the declarative completes normally the execution of the run unit is terminated
/// abnormally" (a format 3 USE naming EC-I-O, a higher-level name than EC-I-O-PERMANENT-ERROR);</item>
/// <item>rule 7 — "If checking for the exception condition is enabled, execution of the run unit is terminated
/// abnormally" (after a format 1 USE procedure, which names no exception-name, so rule 5 does not select it,
/// and there is no PROPAGATE directive for rule 6).</item>
/// </list>
/// <para>The unchecked arm (continue) is pinned by conformance:{85,2002,2014,2023}/pb344_use_mode_read_exception.
/// A checked declarative that ends in RESUME witnesses nothing of this item: its continuation is fixed for every
/// implementation by §14.9.33.4 GR2 a) and rule 5 NOTE 2, and control never returns to the input-output control
/// system, which is where the §9.1.13.1 implementor choice is made.</para>
///
/// <para><b>Every expected value is derived from the rules, not observed.</b> OPEN INPUT of a non-optional file
/// whose physical file is not present sets I-O status '35' (§9.1.13.6 rule 5), whose first digit '3' makes it
/// fatal and maps it to EC-I-O-PERMANENT-ERROR (§9.1.13.1). The status is set "prior to the execution of any
/// applicable exception processing statements" (§9.1.13.1), so the USE procedure DISPLAYs 35. The run unit then
/// terminates, so the DISPLAY after the OPEN is never reached: stdout is exactly BEFORE and the handler line.</para>
/// </summary>
public sealed class DocA1Item218WitnessTests
{
    private static readonly CobolNetCompiler CobolNet = new(2023);

    /// <summary>Compile-and-run; assert ABNORMAL termination (nonzero exit) whose stderr names the condition, and
    /// the exact stdout produced before it.</summary>
    private static void AssertFatal(string source, string ecName, string expectedStdout)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.False(ok, $"expected abnormal termination on {ecName}; ran clean with stdout:\n{stdout}");
        Assert.Contains(ecName, detail);
        Assert.Equal(expectedStdout, stdout);
    }

    [Fact]   // §14.6.13.1.3 rule 3 -> §9.1.13 runs the format 1 USE procedure, then rule 7 terminates.
    public void CheckedFatalIo_Format1UseCompletesNormally_TerminatesAbnormally()
        => AssertFatal("""
            >>TURN EC-I-O CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D218A.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d218a-no-such-file.dat"
                    ORGANIZATION IS SEQUENTIAL
                    FILE STATUS IS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(8).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            DECLARATIVES.
            ERR-S SECTION.
                USE AFTER STANDARD ERROR PROCEDURE ON F.
            ERR-P.
                DISPLAY "IN USE FS=" FS.
            END DECLARATIVES.
            MAIN-S SECTION.
            MAIN-P.
                DISPLAY "BEFORE".
                OPEN INPUT F.
                DISPLAY "AFTER-OPEN FS=" FS.
                STOP RUN.
            """, "EC-I-O-PERMANENT-ERROR", "BEFORE\nIN USE FS=35");

    [Fact]   // §14.6.13.1.3 rule 5: the format 3 declarative for EC-I-O runs and completes normally -> terminate.
    public void CheckedFatalIo_Format3UseCompletesNormally_TerminatesAbnormally()
        => AssertFatal("""
            >>TURN EC-I-O CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D218B.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d218b-no-such-file.dat"
                    ORGANIZATION IS SEQUENTIAL
                    FILE STATUS IS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(8).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-S SECTION.
                USE AFTER EXCEPTION CONDITION EC-I-O.
            EC-P.
                DISPLAY "IN USE FS=" FS.
            END DECLARATIVES.
            MAIN-S SECTION.
            MAIN-P.
                DISPLAY "BEFORE".
                OPEN INPUT F.
                DISPLAY "AFTER-OPEN FS=" FS.
                STOP RUN.
            """, "EC-I-O-PERMANENT-ERROR", "BEFORE\nIN USE FS=35");
}
