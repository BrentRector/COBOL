// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A REFERENCE-MODIFIER POSITION THAT LEFT <c>int</c>'S (OR <c>long</c>'S) RANGE IS NAMED BY ITS VALUE (kb/Work PB2938,
/// the reference-modification twin of PB2695's <see cref="WideSubscriptDiagnosticTests"/>). ISO §8.4.3.3.4 5) — "If the
/// evaluation of leftmost-position or length results in a non-integer value, a zero value, or a value that references a
/// position outside the area of identifier-1, the EC-BOUND-REF-MOD exception condition is set to exist" — is raised
/// whatever the magnitude, and the runtime SATURATES a position so that stays true; but the detail the fatal termination
/// prints used to be the saturated <c>int</c>, so <c>X(A:1)</c> with <c>A PIC 9(21)</c> holding 10^20 + 1 was reported as
/// <c>reference modification (2147483647:1)</c> — a position the program never computed. The standard fixes no message
/// text and a COBOL program cannot read it (a corpus golden compares stdout and must exit 0), so the TERMINATION
/// SURFACE is asserted here, per arm of the dispatch that reads a position: an item read through each carrier, the
/// expression arm (<c>BoundPositionValue</c>'s 38-digit temporary), a literal and a named constant past <c>long</c>,
/// the three consumers of the checked range test (read, write, ADDRESS OF) and the ACCEPT receiver's transfer width.
/// <para>The condition's behavior with checking OFF is unchanged and is asserted too: the run unit ends at the reference
/// (the owner's loud-abort rule, kb/Work R60).</para>
/// </summary>
public sealed class WideRefModDiagnosticTests
{
    private const string Saturated32 = "2147483647";
    private const string Saturated64 = "9223372036854775807";

    private const string Fields = """
        01 X PIC X(10) VALUE "ABCDEFGHIJ".
                    01 Y PIC X(10) VALUE SPACES.
        """;

    private static (int exit, string stdout, string stderr) Run(string id, string turn, string data, string procedure)
    {
        string source = $"""
            {turn}
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2938{id}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {Fields}
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

    private const string On = ">>TURN EC-BOUND-REF-MOD CHECKING ON";

    private static void AssertNamesThePositions(int exit, string stdout, string stderr, string expected)
    {
        Assert.Equal("BEFORE", stdout);                              // the statement raised; nothing after it ran
        Assert.Equal(1, exit);                                       // §14.6.12 — the abnormal-termination exit
        Assert.Contains("EC-BOUND-REF-MOD (fatal)", stderr);
        Assert.Contains(expected, stderr);
        Assert.DoesNotContain(Saturated32, stderr);                  // neither clamp is ever what the message prints
        Assert.DoesNotContain(Saturated64, stderr);
    }

    /// <summary>A leftmost position that is a data item, through each wide carrier: a 21-digit unsigned item
    /// (<c>Int128</c>), a signed one (the NEGATIVE extreme), a scaled one — whose value, not its storage, is what the
    /// message names — a 31-digit one, and a ten-digit item that fits <c>long</c> but not <c>int</c>.</summary>
    [Theory]
    [InlineData("S1", "01 A PIC 9(21) VALUE 100000000000000000001.", "(100000000000000000001:1)")]
    [InlineData("S2", "01 A PIC S9(21) VALUE -100000000000000000001.", "(-100000000000000000001:1)")]
    [InlineData("S3", "01 A PIC 9(21)V99 VALUE 100000000000000000001.00.", "(100000000000000000001:1)")]
    [InlineData("S4", "01 A PIC 9(31) VALUE 9999999999999999999999999999999.", "(9999999999999999999999999999999:1)")]
    [InlineData("S5", "01 A PIC 9(10) VALUE 5000000000.", "(5000000000:1)")]
    public void LeftmostItem_NamesTheValueTheProgramHolds(string id, string decl, string expected)
    {
        var (exit, stdout, stderr) = Run(id, On, decl, "MOVE X(A:1) TO Y");
        AssertNamesThePositions(exit, stdout, stderr, expected);
    }

    /// <summary>The same item as a SPECIFIED length: the position is floored one above the omitted-length sentinel, and the
    /// floor keeps the value the program holds (a hugely negative length is a violation, never "to the end").</summary>
    [Theory]
    [InlineData("L1", "01 A PIC 9(21) VALUE 100000000000000000001.", "(1:100000000000000000001)")]
    [InlineData("L2", "01 A PIC S9(21) VALUE -100000000000000000001.", "(1:-100000000000000000001)")]
    [InlineData("L3", "01 A PIC S9(10) VALUE -5000000000.", "(1:-5000000000)")]
    [InlineData("L4", "01 A PIC 9(10) VALUE 5000000000.", "(1:5000000000)")]
    public void LengthItem_NamesTheValueTheProgramHolds(string id, string decl, string expected)
    {
        var (exit, stdout, stderr) = Run(id, On, decl, "MOVE X(1:A) TO Y");
        AssertNamesThePositions(exit, stdout, stderr, expected);
    }

    /// <summary>The expression arm: <c>X(A + 5:1)</c> materializes its position into the segment temporary before the
    /// statement, and that store used to hold the <c>long</c>-saturated position. The third case's product is a native
    /// <c>Int128</c> intermediate of 38 digits, which the 38-digit temporary holds exactly.</summary>
    [Theory]
    [InlineData("E1", "01 A PIC 9(21) VALUE 100000000000000000001.", "MOVE X(A + 5:1) TO Y", "(100000000000000000006:1)")]
    [InlineData("E2", "01 A PIC 9(21) VALUE 100000000000000000001.", "MOVE X(1:A - 7) TO Y", "(1:99999999999999999994)")]
    [InlineData("E3", "01 A PIC 9(19) VALUE 9999999999999999999.", "MOVE X(A * A:1) TO Y", "(99999999999999999980000000000000000001:1)")]
    [InlineData("E4", "01 A PIC S9(21) VALUE -100000000000000000001.", "MOVE X(-A:1) TO Y", "(100000000000000000001:1)")]
    [InlineData("E5", "01 A PIC 9(21) VALUE 100000000000000000001.", "MOVE X(- (A):1) TO Y", "(-100000000000000000001:1)")]
    public void ExpressionPosition_NamesTheValueTheExpressionComputes(string id, string decl, string statement, string expected)
    {
        var (exit, stdout, stderr) = Run(id, On, decl, statement);
        AssertNamesThePositions(exit, stdout, stderr, expected);
    }

    /// <summary>An integer LITERAL, or a named integer constant, past <c>long</c> (<c>PositionWideConstant</c>): the value
    /// the program wrote is the value named, with the sign it carried. The literal arm needs no declaration.</summary>
    [Theory]
    [InlineData("K1", "", "MOVE X(100000000000000000001:1) TO Y", "(100000000000000000001:1)")]
    [InlineData("K2", "", "MOVE X(1:100000000000000000001) TO Y", "(1:100000000000000000001)")]
    [InlineData("K3", "", "MOVE X(-100000000000000000001:1) TO Y", "(-100000000000000000001:1)")]
    [InlineData("K4", "", "MOVE X(1:-100000000000000000001) TO Y", "(1:-100000000000000000001)")]
    [InlineData("K5", "", "MOVE X(1:-(100000000000000000001)) TO Y", "(1:-100000000000000000001)")]
    [InlineData("K6", "01 BIG CONSTANT AS 100000000000000000001.", "MOVE X(BIG:1) TO Y", "(100000000000000000001:1)")]
    [InlineData("K7", "", "MOVE X(5000000000:1) TO Y", "(5000000000:1)")]
    public void LiteralPosition_NamesTheLiteralTheProgramWrote(string id, string decl, string statement, string expected)
    {
        var (exit, stdout, stderr) = Run(id, On, decl, statement);
        AssertNamesThePositions(exit, stdout, stderr, expected);
    }

    /// <summary>A SUBSCRIPT literal past <c>long</c> is the same <c>PositionWideConstant</c>: found by this sweep as the
    /// literal arm of PB2695, which handled only the item and ODO-count arms.</summary>
    [Theory]
    [InlineData("T1", "MOVE EL(100000000000000000001) TO R", "subscript 100000000000000000001 is outside 1..3")]
    [InlineData("T2", "MOVE EL(-100000000000000000001) TO R", "subscript -100000000000000000001 is outside 1..3")]
    public void SubscriptLiteral_NamesTheLiteralTheProgramWrote(string id, string statement, string expected)
    {
        var (exit, stdout, stderr) = Run(id, ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON",
            "01 TB.\n                05 EL PIC 9 OCCURS 3 TIMES VALUE 5.\n            01 R PIC 9 VALUE 0.", statement);
        Assert.Equal("BEFORE", stdout);
        Assert.Equal(1, exit);
        Assert.Contains("EC-BOUND-SUBSCRIPT (fatal)", stderr);
        Assert.Contains(expected, stderr);
        Assert.DoesNotContain(Saturated64, stderr);
    }

    /// <summary>The three consumers of the ONE checked range test: the write (<c>CobolString.SpliceInto</c>, whose area
    /// is a "receiver"), ADDRESS OF (<c>CobolString.RefModStartOffset</c>) and an ACCEPT receiver's transfer width.</summary>
    [Theory]
    [InlineData("W1", "MOVE Y TO X(A:1)", "(100000000000000000001:1) out of range for a 10-position receiver")]
    [InlineData("W2", "MOVE Y TO X(1:A)", "(1:100000000000000000001) out of range for a 10-position receiver")]
    [InlineData("W3", "SET P TO ADDRESS OF X(A:1)", "(100000000000000000001:1) out of range for a 10-position item")]
    [InlineData("W4", "ACCEPT X(1:A) FROM DATE", "(1:100000000000000000001) out of range for a 10-position receiver")]
    [InlineData("W5", "ACCEPT X(A:) FROM DATE", "(100000000000000000001:) out of range for a 10-position receiver")]
    [InlineData("W6", "MOVE FUNCTION UPPER-CASE(X)(A:1) TO Y", "(100000000000000000001:1) out of range for a 10-position item")]
    public void WriteAddressAndAcceptConsumers_NameTheValueTheProgramHolds(string id, string statement, string expected)
    {
        var (exit, stdout, stderr) = Run(id, On,
            "01 A PIC 9(21) VALUE 100000000000000000001.\n            01 P USAGE POINTER.", statement);
        AssertNamesThePositions(exit, stdout, stderr, expected);
    }

    /// <summary>A position that is not an integer keeps naming the value (unchanged by PB2938: the fraction test reads the
    /// item's own value, not a clamp).</summary>
    [Fact]
    public void FractionalWideItem_NamesTheValue()
    {
        var (exit, stdout, stderr) = Run("F1", On, "01 S PIC 9(21)V99 VALUE 100000000000000000001.50.", "MOVE X(S:1) TO Y");
        AssertNamesThePositions(exit, stdout, stderr, "position 100000000000000000001.50 is not an integer");
    }

    /// <summary>Checking OFF: the reference ends the run unit with the same words (the owner's loud-abort rule,
    /// kb/Work R60 — the checked range test is the only one, so the detail is shared).</summary>
    [Fact]
    public void CheckingOff_WideItem_EndsTheRunUnitNamingTheValue()
    {
        var (exit, stdout, stderr) = Run("C1", "", "01 A PIC 9(21) VALUE 100000000000000000001.", "MOVE X(A:1) TO Y");
        Assert.Equal("BEFORE", stdout);
        Assert.NotEqual(0, exit);
        Assert.Contains("(100000000000000000001:1)", stderr);
        Assert.DoesNotContain(Saturated32, stderr);
    }

    /// <summary>The compile-time screen of a LITERAL position names the literal too (COBOLNET2670, checking not enabled:
    /// ISO §14.6.13.1.3 8): "the implementor is not required to produce executable code" for a fatal condition the compiler detects). It printed the <c>long</c> clamp.</summary>
    [Theory]
    [InlineData("N1", "MOVE X(100000000000000000001:1) TO Y", "leftmost-position 100000000000000000001 is not a position of the 10-position item")]
    [InlineData("N2", "MOVE X(1:100000000000000000001) TO Y", "the reference modification (1:100000000000000000001) extends past the 10-position item")]
    public void LiteralScreen_WithoutChecking_NamesTheLiteral(string id, string statement, string expected)
    {
        var (exit, _, detail) = Run(id, "", "", statement);
        Assert.Equal(-1, exit);                                      // refused at compile time: no run
        Assert.Contains("COBOLNET2670", detail);
        Assert.Contains(expected, detail);
        Assert.DoesNotContain(Saturated64, detail);
    }
}
