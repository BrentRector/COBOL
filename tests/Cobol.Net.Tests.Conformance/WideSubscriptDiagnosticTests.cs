// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A SUBSCRIPT (OR OCCURS DEPENDING COUNT) THAT LEFT <c>long</c>'S RANGE IS NAMED BY ITS VALUE (kb/Work PB2695).
/// ISO §8.4.2.3.4 GR2 — "If the value of the subscript is not a positive integer or is less than one or is greater
/// than the highest permissible occurrence number, the EC-BOUND-SUBSCRIPT exception condition is set to exist" — is
/// raised whatever the magnitude, and <c>CobolNum.Position</c> SATURATES the occurrence number to <c>long</c> so that
/// stays true; but the detail the fatal termination prints used to be the saturated <c>long</c>, so a
/// <c>PIC 9(21)</c> subscript holding 10^20 + 1 was reported as <c>9223372036854775807</c> — a value the program
/// never computed. The standard fixes no message text and the COBOL program cannot read it (a corpus golden compares
/// stdout and must exit 0), so the TERMINATION SURFACE is asserted here, per arm of the dispatch that reads a
/// position: the fixed-table accessor, an OCCURS DEPENDING count at an element reference and at a group's extent, a
/// dynamic-capacity table's sending reference and its two receiving ones, and the expression arm
/// (<c>BoundPositionValue</c>'s 38-digit temporary).
/// <para>The condition's behavior with checking OFF is unchanged and is asserted too: the reference reads the benign
/// scratch slot and the run unit continues.</para>
/// </summary>
public sealed class WideSubscriptDiagnosticTests
{
    private const string Saturated = "9223372036854775807";

    private static (int exit, string stdout, string stderr) Run(string id, string turn, string data, string procedure)
    {
        string source = $"""
            {turn}
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2695{id}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {data}
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "BEFORE"
                {procedure}
                DISPLAY "AFTER"
                STOP RUN.
            """;
        return new CobolNetCompiler(2023).CompileAndRunExit(source);
    }

    private static void AssertNamesTheValue(int exit, string stdout, string stderr, string ec, string expected)
    {
        Assert.Equal("BEFORE", stdout);                              // the statement raised; nothing after it ran
        Assert.Equal(1, exit);                                       // §14.6.12 — the abnormal-termination exit
        Assert.Contains($"{ec} (fatal)", stderr);
        Assert.Contains(expected, stderr);
        Assert.DoesNotContain(Saturated, stderr);                    // the clamp is never what the message prints
    }

    private const string Table = "01 T.\n               05 EL PIC 9 OCCURS 3 TIMES VALUE 5.\n            01 R PIC 9 VALUE 0.";

    /// <summary>The fixed-table accessor, a subscript ITEM read through each wide carrier: a 21-digit unsigned item
    /// (<c>Int128</c>), a signed one (the NEGATIVE extreme, <c>long.MinValue</c> side), and a scaled one — whose
    /// value, not its storage, is what the message names.</summary>
    [Theory]
    [InlineData("A1", "01 A PIC 9(21) VALUE 100000000000000000001.", "subscript 100000000000000000001 is outside 1..3")]
    [InlineData("A2", "01 A PIC S9(21) VALUE -100000000000000000001.", "subscript -100000000000000000001 is outside 1..3")]
    [InlineData("A3", "01 A PIC 9(21)V99 VALUE 100000000000000000001.00.", "subscript 100000000000000000001 is outside 1..3")]
    [InlineData("A4", "01 A PIC 9(31) VALUE 9999999999999999999999999999999.", "subscript 9999999999999999999999999999999 is outside 1..3")]
    public void FixedTable_ItemSubscript_NamesTheValueTheProgramHolds(string id, string decl, string expected)
    {
        var (exit, stdout, stderr) = Run(id, ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON", $"{Table}\n            {decl}", "MOVE EL(A) TO R");
        AssertNamesTheValue(exit, stdout, stderr, "EC-BOUND-SUBSCRIPT", expected);
    }

    /// <summary>The expression arm: <c>EL(A + 5)</c> materializes its position into the segment temporary before the
    /// statement, and that store used to hold the <c>long</c>-saturated position. The third case's product is a native
    /// <c>Int128</c> intermediate of 38 digits (10^38 − 2·10^19 + 1), which the 38-digit temporary holds exactly.</summary>
    [Theory]
    [InlineData("E1", "01 A PIC 9(21) VALUE 100000000000000000001.", "MOVE EL(A + 5) TO R", "subscript 100000000000000000006 is outside 1..3")]
    [InlineData("E2", "01 A PIC 9(21) VALUE 100000000000000000001.", "MOVE EL(A / 1 - 7) TO R", "subscript 99999999999999999994 is outside 1..3")]
    [InlineData("E3", "01 A PIC 9(19) VALUE 9999999999999999999.", "MOVE EL(A * A) TO R", "subscript 99999999999999999980000000000000000001 is outside 1..3")]
    public void FixedTable_ExpressionSubscript_NamesTheValueTheExpressionComputes(string id, string decl, string statement, string expected)
    {
        var (exit, stdout, stderr) = Run(id, ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON", $"{Table}\n            {decl}", statement);
        AssertNamesTheValue(exit, stdout, stderr, "EC-BOUND-SUBSCRIPT", expected);
    }

    /// <summary>An OCCURS DEPENDING count past <c>long</c> at an element reference (§13.18.38.4 GR7).</summary>
    [Fact]
    public void OdoElementReference_WideCount_NamesTheCount()
    {
        const string data = """
            01 N PIC 9(21) VALUE 100000000000000000000.
                        01 G.
                           05 EL PIC 9 OCCURS 1 TO 3 TIMES DEPENDING ON N VALUE 5.
                        01 R PIC 9 VALUE 0.
            """;
        var (exit, stdout, stderr) = Run("O1", ">>TURN EC-BOUND-ODO CHECKING ON", data, "MOVE EL(1) TO R");
        AssertNamesTheValue(exit, stdout, stderr, "EC-BOUND-ODO", "OCCURS DEPENDING value 100000000000000000000 is outside 1..3");
    }

    /// <summary>An OCCURS DEPENDING count past <c>long</c> at the group's current extent (§13.18.38 GR8 over GR7).</summary>
    [Fact]
    public void OdoGroupExtent_WideCount_NamesTheCount()
    {
        const string data = """
            01 N PIC 9(21) VALUE 100000000000000000000.
                        01 G.
                           05 EL PIC 9 OCCURS 1 TO 3 TIMES DEPENDING ON N VALUE 5.
                        01 S PIC X(8) VALUE SPACES.
            """;
        var (exit, stdout, stderr) = Run("O2", ">>TURN EC-BOUND-ODO CHECKING ON", data, "MOVE G TO S");
        AssertNamesTheValue(exit, stdout, stderr, "EC-BOUND-ODO", "OCCURS DEPENDING value 100000000000000000000 is outside 1..3");
    }

    private const string Dyn = """
        01 D.
                       05 DT PIC X OCCURS DYNAMIC CAPACITY IN CAP FROM 2 TO 5 VALUE "A".
                    01 X PIC X VALUE SPACE.
                    01 A PIC 9(21) VALUE 100000000000000000001.
        """;

    /// <summary>A dynamic-capacity table's SENDING reference (§8.5.1.9.2: the same rule as a fixed table whose size is
    /// the current capacity).</summary>
    [Fact]
    public void DynamicTable_SendingReference_NamesTheValue()
    {
        var (exit, stdout, stderr) = Run("D1", ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON", Dyn, "MOVE DT(A) TO X");
        AssertNamesTheValue(exit, stdout, stderr, "EC-BOUND-SUBSCRIPT",
            "subscript 100000000000000000001 is outside 1..2, the current capacity");
    }

    /// <summary>A RECEIVING reference past <c>long</c> is a GROWTH request (§8.5.1.9.3), refused by the implementor
    /// maximum (§8.5.1.9.6 2): EC-BOUND-TABLE-LIMIT names the capacity the program asked for.</summary>
    [Fact]
    public void DynamicTable_ReceivingReference_NamesTheRequestedCapacity()
    {
        var (exit, stdout, stderr) = Run("D2", ">>TURN EC-BOUND-TABLE-LIMIT CHECKING ON", Dyn, "MOVE \"Z\" TO DT(A)");
        AssertNamesTheValue(exit, stdout, stderr, "EC-BOUND-TABLE-LIMIT",
            "OCCURS DYNAMIC growth to 100000000000000000001 exceeds the implementor maximum");
    }

    /// <summary>A NEGATIVE receiving reference is §8.4.2.3.4 2)'s "not a positive integer", not a growth case.</summary>
    [Fact]
    public void DynamicTable_ReceivingReference_NegativeWide_NamesTheValue()
    {
        string data = Dyn.Replace("PIC 9(21) VALUE 100000000000000000001", "PIC S9(21) VALUE -100000000000000000001");
        var (exit, stdout, stderr) = Run("D3", ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON", data, "MOVE \"Z\" TO DT(A)");
        AssertNamesTheValue(exit, stdout, stderr, "EC-BOUND-SUBSCRIPT",
            "subscript -100000000000000000001 is not a positive integer");
    }

    /// <summary>A REPEATING report entry's sum counter is a table (§13.18.54.4 GR8 a); kb/Work PB1271), and its
    /// subscript is §8.4.2.3.4 GR2's: <c>CobolReport.SumImage</c> takes the same <c>Occurrence</c> a table accessor does.</summary>
    [Fact]
    public void ReportSumCounter_WideSubscript_NamesTheValue()
    {
        const string source = """
            >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2695RS.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT PRT ASSIGN TO "pb2695rs.txt".
            DATA DIVISION.
            FILE SECTION.
            FD  PRT REPORT IS R1.
            WORKING-STORAGE SECTION.
            01  WS-X PIC 9 VALUE 3.
            01  WS-I PIC 9 VALUE 0.
            01  A    PIC 9(21) VALUE 100000000000000000001.
            REPORT SECTION.
            RD  R1 CONTROL IS FINAL PAGE LIMIT 30 LINES.
            01  CFG TYPE IS CONTROL FOOTING FINAL.
                03 LINE PLUS 1.
                   05 CF-U COLUMN 1 13 25 PIC 9999 SUM WS-X.
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT PRT.
                INITIATE R1.
                DISPLAY "BEFORE"
                MOVE CF-U (A) TO WS-I
                DISPLAY "AFTER"
                STOP RUN.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        AssertNamesTheValue(exit, stdout, stderr, "EC-BOUND-SUBSCRIPT",
            "sum counter subscript 100000000000000000001 is outside 1..3");
    }

    /// <summary>Checking OFF is unchanged: the reference reads the benign scratch slot and the run unit continues.</summary>
    [Fact]
    public void CheckingOff_WideSubscript_ContinuesThroughTheScratchSlot()
    {
        var (exit, stdout, stderr) = Run("C1", "", $"{Table}\n            01 A PIC 9(21) VALUE 100000000000000000001.",
            "MOVE EL(A) TO R\n                DISPLAY \"R=\" R");
        Assert.Equal(0, exit);
        Assert.Equal("BEFORE\nR=0\nAFTER", stdout.Replace("\r\n", "\n"));
        Assert.DoesNotContain("EC-BOUND-SUBSCRIPT", stderr);
    }
}
