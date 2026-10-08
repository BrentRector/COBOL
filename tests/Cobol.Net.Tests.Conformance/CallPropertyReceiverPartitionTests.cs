// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// A CALL whose receiver is an object PROPERTY (RETURNING or BY REFERENCE, kb/Work PB2078) runs the property's SET at the
/// return, and that SET is NOT part of the activation. ISO §14.9.4.4 3) h): "If the program was not successfully called
/// and an exception condition was set to exist" selects the ON EXCEPTION phrase; 3) i): "If the program was successfully
/// called, after control is returned from the called program the ON EXCEPTION phrase, if specified, is ignored." The
/// emitted SET used to run inside the activation's try, so a condition the SET method raised (here its own CALL of a
/// program that is not in the run unit) took the OUTER ON EXCEPTION phrase and the run continued, although the outer
/// program was successfully called (train 1039b review). The abnormal arm is observable only as the exit code, the
/// stderr surface and the stdout that stops, so it is asserted here rather than as a corpus golden.
/// </summary>
public sealed class CallPropertyReceiverPartitionTests
{
    private const string Class = """
        IDENTIFICATION DIVISION.
        CLASS-ID. PB2078CK INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-BAL PIC 9(4) VALUE 1.
        PROCEDURE DIVISION.
        METHOD-ID. GET PROPERTY BAL.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-R PIC 9(4).
        PROCEDURE DIVISION RETURNING LK-R.
        MAIN.
            MOVE W-BAL TO LK-R.
        END METHOD.
        METHOD-ID. SET PROPERTY BAL.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-V PIC 9(4).
        PROCEDURE DIVISION USING LK-V.
        MAIN.
            DISPLAY "IN SET " LK-V
            IF LK-V = 9 CALL "PB2078ZZ" END-IF
            MOVE LK-V TO W-BAL.
        END METHOD.
        END OBJECT.
        END CLASS PB2078CK.
        """;

    private static string Program(string id, string callee) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS PB2078CK
            PROPERTY BAL.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 O USAGE OBJECT REFERENCE PB2078CK.
        01 I PIC 9 VALUE 1.
        PROCEDURE DIVISION.
        MAIN.
            INVOKE PB2078CK "NEW" RETURNING O
            CALL "{callee}" USING I RETURNING BAL OF O
                ON EXCEPTION DISPLAY "ON EXCEPTION BAL=" BAL OF O
                NOT ON EXCEPTION DISPLAY "NOT ON EXCEPTION"
            END-CALL
            DISPLAY "AFTER CALL"
            STOP RUN.

        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB2078CS.
        DATA DIVISION.
        LINKAGE SECTION.
        01 K PIC 9.
        01 RV PIC 9(4).
        PROCEDURE DIVISION USING K RETURNING RV.
            MOVE 9 TO RV
            GOBACK.
        END PROGRAM PB2078CS.
        END PROGRAM {id}.

        {Class}
        """;

    /// <summary>§14.9.4.4 3) i): PB2078CS IS successfully called, so the condition the SET method's own CALL raises is
    /// not the outer CALL's to divert; with no handler it terminates the run unit (§14.9.4.4 3) h) 3, at the inner CALL),
    /// exactly as the same failed CALL does inside the called program itself.</summary>
    [Fact]
    public void ConditionRaisedByThePropertySet_IsNotTheCallsOnException()
    {
        var (exit, stdout, stderr) = new CobolNetCompiler(2014).CompileAndRunExit(Program("PB2078CA", "PB2078CS"));
        Assert.Equal("IN SET 0009", stdout);                        // neither phrase, nor AFTER CALL, runs
        Assert.Equal(1, exit);                                      // §14.6.12, the abnormal-termination exit
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains("EC-PROGRAM-NOT-FOUND", stderr);
    }

    /// <summary>§14.9.4.4 3) h) 1: a FAILED activation takes ON EXCEPTION and stores nothing, so the property's SET never
    /// runs and the GET in the phrase reads the property's initial value.</summary>
    [Fact]
    public void FailedActivation_TakesOnException_AndRunsNoSet()
    {
        var (exit, stdout, stderr) = new CobolNetCompiler(2014).CompileAndRunExit(Program("PB2078CB", "PB2078NO"));
        Assert.True(exit == 0, stderr);
        Assert.Equal("ON EXCEPTION BAL=0001\nAFTER CALL", stdout);
    }
}
