// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Annex A.1 item 103 — "I-O status (action taken when a fatal exception condition exists and checking for the
/// condition is not enabled)" — and the arm no corpus golden can witness, because the golden harness requires exit 0
/// and this arm ends in abnormal run-unit termination (kb/Work PB322 determination E).
///
/// <para><b>The determination</b> (docs/CONFORMANCE.md §7, DOC-A.1-103): §9.1.13.1 — "Certain classes of I-O status
/// values indicate fatal exception conditions. These are: any that begin with the digit 3, 4, or 7, and any that
/// begin with the digit 9 that the implementor defines as fatal" — and then "The implementor may either continue or
/// terminate the execution of the run unit". WiseOwl COBOL continues only when the program is equipped to see the
/// failure: a FILE STATUS clause on the file, a USE procedure applying to the statement, or the statement's own ON
/// EXCEPTION phrase. With none of them the run unit terminates abnormally (CLAUDE.md rule 1: GnuCOBOL's default error
/// handler ends the run unit for a file with no FILE STATUS clause).</para>
///
/// <para><b>Every expected value is derived from the rules, not observed.</b> OPEN INPUT of a non-optional file whose
/// physical file is not present sets I-O status '35' (§9.1.13.6 rule 5), whose first digit '3' makes it fatal and
/// maps it to EC-I-O-PERMANENT-ERROR (§9.1.13.1). A WRITE to a file that is not open sets '48' (§9.1.13.7), fatal
/// by its first digit '4' and EC-I-O-LOGIC-ERROR. The at end condition ('10', first digit 1) is not fatal.</para>
/// </summary>
public sealed class DocA1Item103WitnessTests
{
    private static readonly CobolNetCompiler CobolNet = new(2023);

    private static void AssertTerminates(string source, string ecName, string statement, string expectedStdout)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.False(ok, $"expected abnormal termination on {ecName}; ran clean with stdout:\n{stdout}");
        Assert.Contains(ecName, detail);
        Assert.Contains($"({statement})", detail);
        Assert.Equal(expectedStdout, stdout);
    }

    private static void AssertContinues(string source, string expectedStdout)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"expected the run unit to continue; it ended abnormally:\n{detail}");
        Assert.Equal(expectedStdout, stdout);
    }

    [Fact]   // §9.1.13.6 rule 5 → '35' is fatal; no FILE STATUS, no USE procedure → the run unit terminates.
    public void FatalStatus_NoFileStatusNoUse_TerminatesAbnormally()
        => AssertTerminates("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103A.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d103a-no-such-file.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(8).
            PROCEDURE DIVISION.
            MAIN-P.
                DISPLAY "BEFORE".
                OPEN INPUT F.
                DISPLAY "AFTER-OPEN".
                STOP RUN.
            """, "EC-I-O-PERMANENT-ERROR", "OPEN", "BEFORE");

    [Fact]   // §9.1.13.7: a WRITE to a closed file is '48' (logic error, fatal); the same rule for a second statement kind.
    public void FatalLogicErrorStatus_OnWrite_NoFileStatusNoUse_TerminatesAbnormally()
        => AssertTerminates("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103B.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d103b.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(8).
            PROCEDURE DIVISION.
            MAIN-P.
                DISPLAY "BEFORE".
                MOVE "AAAA" TO F-REC.
                WRITE F-REC.
                DISPLAY "AFTER-WRITE".
                STOP RUN.
            """, "EC-I-O-LOGIC-ERROR", "WRITE", "BEFORE");

    [Fact]   // CONTROL: a FILE STATUS clause means the program sees the status and carries on.
    public void FatalStatus_WithFileStatusClause_Continues()
        => AssertContinues("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103C.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d103c-no-such-file.dat"
                    ORGANIZATION IS SEQUENTIAL
                    FILE STATUS IS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(8).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-P.
                OPEN INPUT F.
                DISPLAY "AFTER-OPEN FS=" FS.
                STOP RUN.
            """, "AFTER-OPEN FS=35");

    [Fact]   // CONTROL: an applicable USE procedure (§14.9.49.4 GR3) means the program is equipped for the failure.
    public void FatalStatus_WithApplicableUseProcedure_Continues()
        => AssertContinues("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103D.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d103d-no-such-file.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(8).
            PROCEDURE DIVISION.
            DECLARATIVES.
            ERR-S SECTION.
                USE AFTER STANDARD ERROR PROCEDURE ON F.
            ERR-P.
                DISPLAY "IN USE".
            END DECLARATIVES.
            MAIN-S SECTION.
            MAIN-P.
                OPEN INPUT F.
                DISPLAY "AFTER-OPEN".
                STOP RUN.
            """, "IN USE\nAFTER-OPEN");

    [Fact]   // CONTROL: the statement's own ON EXCEPTION phrase covers every unsuccessful family (§14.9.10.4 GR20 c).
    public void FatalStatus_WithOnExceptionPhrase_Continues()
        => AssertContinues("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103E.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d103e-no-such-file.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(8).
            PROCEDURE DIVISION.
            MAIN-P.
                OPEN OUTPUT F.
                DELETE FILE F ON EXCEPTION DISPLAY "ON-EXCEPTION".
                DISPLAY "AFTER-DELETE".
                STOP RUN.
            """, "ON-EXCEPTION\nAFTER-DELETE");

    [Fact]   // CONTROL: the at end condition ('10') is not fatal, so a file with no FILE STATUS clause carries on.
    public void NonFatalAtEnd_NoFileStatus_Continues()
        => AssertContinues("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103F.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d103f.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(4).
            PROCEDURE DIVISION.
            MAIN-P.
                OPEN OUTPUT F.
                MOVE "AAAA" TO F-REC.
                WRITE F-REC.
                CLOSE F.
                OPEN INPUT F.
                READ F AT END DISPLAY "UNEXPECTED" END-READ.
                READ F AT END DISPLAY "AT-END" END-READ.
                CLOSE F.
                DISPLAY "DONE".
                STOP RUN.
            """, "AT-END\nDONE");

    // ── The SORT/MERGE implicit transfers (kb/Work PB2700) ──────────────────────────────────────────────────────────
    // Each as-if statement is performed "as if an OPEN statement with the INPUT phrase ... had been executed"
    // (§14.9.40.4 GR12 a), §14.9.24.4 GR7 a)) — "as if an OPEN statement with the OUTPUT and SHARING WITH NO OTHER
    // phrases had been executed" for a GIVING file (GR15 a), GR12 a)) — so its fatal status meets the same §9.1.13.1
    // determination as the explicit OPEN's. The verb's rules ("the SORT is terminated") are what the CONTINUED run
    // unit does with the statement. Before PB2700 the implicit OPEN's hook never asked the question: the SORT ended
    // and the program carried on silently with nothing sorted. The covered arms (FILE STATUS, USE) are the corpus
    // golden 85/pb2700_sort_merge_missing_using_covered.

    [Fact]   // §9.1.13.6 rule 5 → the USING file's implicit OPEN INPUT is '35'; nothing covers it → the run unit ends.
    public void SortUsingFileMissing_NoFileStatusNoUse_TerminatesAbnormally()
        => AssertTerminates("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103G.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d103g-no-such-file.dat"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SW ASSIGN TO "l1d103g.tmp".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(4).
            SD SW.
            01 SW-REC PIC X(4).
            PROCEDURE DIVISION.
            MAIN-S SECTION.
            MAIN-P.
                DISPLAY "BEFORE".
                SORT SW ON ASCENDING KEY SW-REC USING F OUTPUT PROCEDURE IS OP-S.
                DISPLAY "AFTER-SORT".
                STOP RUN.
            OP-S SECTION.
            OP-P.
                DISPLAY "IN-OP".
            """, "EC-I-O-PERMANENT-ERROR", "SORT USING implicit OPEN", "BEFORE");

    [Fact]   // The MERGE twin: GR7 a) names only a nonfatal OPEN status, so the fatal '35' is §9.1.13.1's to dispose of.
    public void MergeUsingFileMissing_NoFileStatusNoUse_TerminatesAbnormally()
        => AssertTerminates("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103H.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F1 ASSIGN TO "l1d103h-no-such-file-1.dat"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT F2 ASSIGN TO "l1d103h-no-such-file-2.dat"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SW ASSIGN TO "l1d103h.tmp".
            DATA DIVISION.
            FILE SECTION.
            FD F1.
            01 F1-REC PIC X(4).
            FD F2.
            01 F2-REC PIC X(4).
            SD SW.
            01 SW-REC PIC X(4).
            PROCEDURE DIVISION.
            MAIN-S SECTION.
            MAIN-P.
                DISPLAY "BEFORE".
                MERGE SW ON ASCENDING KEY SW-REC USING F1 F2 OUTPUT PROCEDURE IS OP-S.
                DISPLAY "AFTER-MERGE".
                STOP RUN.
            OP-S SECTION.
            OP-P.
                DISPLAY "IN-OP".
            """, "EC-I-O-PERMANENT-ERROR", "MERGE USING implicit OPEN", "BEFORE");

    [Fact]   // The GIVING arm: OPEN OUTPUT cannot create a file in a directory that does not exist ('30', fatal).
    public void SortGivingOpenFails_NoFileStatusNoUse_TerminatesAbnormally()
        => AssertTerminates("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103I.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT G ASSIGN TO "l1d103i-no-such-dir/g.dat"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SW ASSIGN TO "l1d103i.tmp".
            DATA DIVISION.
            FILE SECTION.
            FD G.
            01 G-REC PIC X(4).
            SD SW.
            01 SW-REC PIC X(4).
            PROCEDURE DIVISION.
            MAIN-S SECTION.
            MAIN-P.
                DISPLAY "BEFORE".
                SORT SW ON ASCENDING KEY SW-REC INPUT PROCEDURE IS IP-S GIVING G.
                DISPLAY "AFTER-SORT".
                STOP RUN.
            IP-S SECTION.
            IP-P.
                MOVE "AAAA" TO SW-REC.
                RELEASE SW-REC.
            """, "EC-I-O-PERMANENT-ERROR", "SORT GIVING implicit OPEN", "BEFORE");

    [Fact]   // CONTROL: with checking ENABLED for the condition, §14.6.13.1.3 2) gives the status to the SORT's own rules
             // (kb/Work PB993): the SORT is terminated and the run unit continues — the "not enabled" half of the question.
    public void SortUsingFileMissing_CheckingEnabled_FollowsTheVerbRule()
        => AssertContinues("""
            >>TURN EC-I-O CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L1D103J.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "l1d103j-no-such-file.dat"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SW ASSIGN TO "l1d103j.tmp".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(4).
            SD SW.
            01 SW-REC PIC X(4).
            PROCEDURE DIVISION.
            MAIN-S SECTION.
            MAIN-P.
                SORT SW ON ASCENDING KEY SW-REC USING F OUTPUT PROCEDURE IS OP-S.
                DISPLAY "AFTER-SORT " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            OP-S SECTION.
            OP-P.
                DISPLAY "IN-OP".
            """, "AFTER-SORT EC-I-O-PERMANENT-ERROR");
}
