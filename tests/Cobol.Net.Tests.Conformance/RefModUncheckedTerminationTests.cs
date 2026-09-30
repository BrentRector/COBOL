// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// An UNCHECKED out-of-range reference modification ends the run unit (kb/Work PB1707 part 2; owner decision R60;
/// docs/CONFORMANCE.md §3 "DETERMINATION — an unchecked out-of-range reference modification ends the run unit").
/// The arm no corpus golden can witness, because the golden harness requires exit 0 and this arm ends in abnormal
/// run-unit termination (the same reason <see cref="DocA1Item218WitnessTests"/> is a test class).
///
/// <para><b>Every expected value is derived from the rules.</b> §8.4.3.3.4 5) b): leftmost-position is a position of
/// the item; 5) c): a specified length is a positive nonzero integer and "The sum of leftmost-position and length
/// minus the value one shall be less than or equal to the number of positions in the data item referenced by
/// identifier-1" (both cite.py OK). A violation is the fatal EC-BOUND-REF-MOD. With checking not enabled the
/// standard names no outcome for the violated <c>shall</c>, so the owner's 2026-07-28 loud-abort rule decides: the
/// run unit terminates and nothing is clamped or padded. The positions are DATA ITEMS here on purpose — a literal
/// violation is refused at compile time (COBOLNET2670, negative:pb1707-refmod-literal-out-of-range) and never runs.
/// Stdout is exactly what was displayed before the terminating statement.</para>
/// </summary>
public sealed class RefModUncheckedTerminationTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler(2023);

    /// <summary>A program holding CX = "ABCDE" and the position data items, running <paramref name="statements"/>
    /// between a BEFORE and an AFTER marker.</summary>
    private static string Program(string pid, string statements, string directives = "") => $"""
        {directives}
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 CX PIC X(5) VALUE "ABCDE".
        01 N  PIC 9(11) VALUE 77777777777.
        01 M  PIC 99 VALUE 10.
        01 Z  PIC 9 VALUE 0.
        01 S  PIC 9 VALUE 6.
        01 R  PIC X(12) VALUE SPACES.
        PROCEDURE DIVISION.
        MAIN-P.
            DISPLAY "BEFORE".
            {statements}
            DISPLAY "AFTER".
            STOP RUN.
        """;

    private static void AssertTerminates(string source, string expectedStdout)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.False(ok, $"expected abnormal termination; ran clean with stdout:\n{stdout}");
        Assert.Contains("EC-BOUND-REF-MOD", detail);
        Assert.Equal(expectedStdout, stdout);
    }

    [Fact]   // 5) c): 1 + 10 - 1 = 10 > 5. The defect printed "ABCDE" plus five spaces.
    public void Read_LengthPastTheItem_Terminates()
        => AssertTerminates(Program("RM70A", "DISPLAY CX(1:M)."), "BEFORE");

    [Fact]   // PB1707's own repro: the saturated length must not reach an allocation ("Out of memory.").
    public void Read_SaturatedLength_Terminates_NotOutOfMemory()
        => AssertTerminates(Program("RM70B", "DISPLAY CX(1:N)."), "BEFORE");

    [Fact]   // 5) b): leftmost-position 0 is not a position of the item.
    public void Read_LeftmostZero_Terminates()
        => AssertTerminates(Program("RM70C", "DISPLAY CX(Z:1)."), "BEFORE");

    [Fact]   // 5) b) on the OMITTED-length form: only the leftmost is range-tested, and 6 > 5.
    public void Read_OmittedLength_LeftmostPastTheItem_Terminates()
        => AssertTerminates(Program("RM70D", "DISPLAY CX(S:)."), "BEFORE");

    [Fact]   // 5) c): a zero length is not positive (REF-MOD-ZERO-LENGTH is not on).
    public void Read_ZeroLength_Terminates()
        => AssertTerminates(Program("RM70E", "DISPLAY CX(1:Z)."), "BEFORE");

    [Fact]   // The WRITE twin answers the same violation the same way (it used to clamp and continue).
    public void Write_LengthPastTheItem_Terminates()
        => AssertTerminates(Program("RM70F", "MOVE \"XY\" TO CX(4:M)."), "BEFORE");

    [Fact]   // The same predicate serves a reference modification as a MOVE's sending operand.
    public void Move_Sender_LengthPastTheItem_Terminates()
        => AssertTerminates(Program("RM70G", "MOVE CX(2:M) TO R."), "BEFORE");

    [Fact]   // §8.5.1.12's current extent is the item: at NN = 2 the group has 2 positions, so position 3 is outside it.
    public void Read_PastAnOccursDependingGroupsCurrentExtent_Terminates()
        => AssertTerminates("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RM70J.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NN PIC 9 VALUE 2.
            01 G.
               05 GT PIC X OCCURS 1 TO 4 DEPENDING ON NN.
            01 P  PIC 9 VALUE 3.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE "ABCD" TO G.
                DISPLAY "BEFORE".
                DISPLAY G(P:1).
                DISPLAY "AFTER".
                STOP RUN.
            """, "BEFORE");

    [Fact]   // In range nothing changes: CX(2:3) is BCD, CX(5:) is E, and the last position alone is E.
    public void InRange_Unchanged()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Program("RM70H", """
            MOVE 5 TO M.
            DISPLAY CX(2:3).
            DISPLAY CX(M:).
            DISPLAY CX(M:1).
            MOVE "XY" TO CX(4:2).
            DISPLAY CX.
            """));
        Assert.True(ok, detail);
        Assert.Equal("BEFORE\nBCD\nE\nE\nABCXY\nAFTER", stdout);
    }

    [Fact]   // Checking ON is unchanged: the condition is raised, the declarative runs, RESUME continues after the statement.
    public void Checked_StillRaisesAndResumes()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun("""
            >>TURN EC-BOUND-REF-MOD CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RM70I.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 CX PIC X(5) VALUE "ABCDE".
            01 M  PIC 99 VALUE 10.
            PROCEDURE DIVISION.
            DECLARATIVES.
            H SECTION.
                USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
            H-P.
                DISPLAY "CAUGHT".
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                DISPLAY "BEFORE".
                DISPLAY CX(1:M).
                DISPLAY "AFTER".
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Equal("BEFORE\nCAUGHT\nAFTER", stdout);
    }
}
