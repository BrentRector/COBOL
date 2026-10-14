// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §8.7.1 — the arithmetic operators "shall be preceded by a space and followed by a space except that no space is
/// required between a left parenthesis and a unary operator or between a unary operator and a left parenthesis"
/// (kb/Work PB2017). The corpus entries <c>negative/pb2017-*</c> pin the refusals by code; these facts pin what a
/// <c>.err</c> substring cannot — HOW MANY diagnostics, WHICH ones are absent, and the directive lane.
/// </summary>
public sealed class OperatorSpacingDiagnosticTests
{
    private static string Program(string statement, bool decimalComma = false) =>
        "       IDENTIFICATION DIVISION.\n"
        + "       PROGRAM-ID. PB2017F.\n"
        + (decimalComma
            ? "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           DECIMAL-POINT IS COMMA.\n"
            : "")
        + "       DATA DIVISION.\n"
        + "       WORKING-STORAGE SECTION.\n"
        + "       01 R PIC S9(4)V99.\n"
        + "       01 A PIC S9(4) VALUE -1.\n"
        + "       01 X PIC X(10) VALUE \"ABCDEFGHIJ\".\n"
        + "       01 Y PIC X(10).\n"
        + "       PROCEDURE DIVISION.\n"
        + "       MAIN.\n"
        + $"           {statement}\n"
        + "           STOP RUN.\n";

    [Theory] // every edition: §8.7.1 carries no edition qualifier
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void UnspacedBinaryOperator_IsOneDiagnostic_AtEveryEdition(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("COMPUTE R = 2*3."), edition);
        Assert.False(ok);
        string error = Assert.Single(errors);
        Assert.Contains("COBOLNET2993", error);
        Assert.Contains("§8.7.1", error);
        Assert.Contains("(11,", error);  // sited at the operator's own line
    }

    [Fact] // the misleading advice (kb/Work PB2017): under DECIMAL-POINT IS COMMA the comma of *,1 is the literal's decimal
           // point — the rule broken is the * 's, and "write a space after the comma" would compute 1 times 1, not 0,1
    public void CommaDecimalPointAfterAnOperator_IsTheOperatorsViolation_NotAMissingSeparator()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("COMPUTE R = 1 *,1.", decimalComma: true), 2023);
        Assert.False(ok);
        string error = Assert.Single(errors);
        Assert.Contains("COBOLNET2993", error);
        Assert.DoesNotContain("COBOLNET2631", error);
        Assert.Contains("write a space after it", error);
    }

    [Theory] // a reference modifier's unary operator (kb/Work PB2017 finisher): its colon is a separator (§8.3.5 7)) but not the
             // space §8.7.1 requires, and its left parenthesis exempts only the side of the operator it touches
    [InlineData("MOVE X(1:-(A)) TO Y.", "preceded", "write a space before it")]
    [InlineData("MOVE X(-A:1) TO Y.", "followed", "write a space after it")]
    [InlineData("MOVE X(1:-A) TO Y.", "preceded or followed", "write a space on both sides of it")]
    public void UnaryOperatorInAReferenceModifier_IsOneDiagnostic(string statement, string side, string fix)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program(statement), 2023);
        Assert.False(ok);
        string error = Assert.Single(errors);
        Assert.Contains("COBOLNET2993", error);
        Assert.Contains($"unary arithmetic operator '-' is not immediately {side} by", error);
        Assert.Contains(fix, error);
    }

    [Theory] // the conforming spellings of the same reference modifiers
    [InlineData("MOVE X(1: -(A)) TO Y.")]
    [InlineData("MOVE X(- A:1) TO Y.")]
    [InlineData("MOVE X(-(A):1) TO Y.")]
    [InlineData("MOVE X(1: - A) TO Y.")]
    public void ConformingReferenceModifierUnarySpellings_Compile(string statement)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program(statement), 2023);
        Assert.True(ok, string.Join("; ", errors));
    }

    [Theory] // the conforming spellings the diagnostic's advice leads to
    [InlineData("COMPUTE R = 1 * ,1.")]       // the comma begins the literal ,1 = 0,1
    [InlineData("COMPUTE R = 1 *, 1.")]       // a comma followed by a space is a separator usable where the space is (§8.3.5 2))
    [InlineData("COMPUTE R = 1,5 * 2.")]
    [InlineData("COMPUTE R = (1 + ,5) * 2.")]
    public void ConformingCommaSpellings_Compile(string statement)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program(statement, decimalComma: true), 2023);
        Assert.True(ok, string.Join("; ", errors));
    }

    [Fact] // a compiler-directive operand is lexed apart from the unit (kb/Work PB1616): its arithmetic is held to §8.7.1 too
    public void DirectiveOperand_IsHeldToTheSameRule()
    {
        const string unit = "       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. PB2017D.\n"
            + "       PROCEDURE DIVISION.\n"
            + "       MAIN.\n"
            + "           STOP RUN.\n";
        var (okBad, errorsBad, _) = EditionHarness.CompileFull("       >>DEFINE X AS 2*3\n" + unit, 2023);
        Assert.False(okBad);
        Assert.Single(errorsBad, e => e.Contains("COBOLNET2993") && e.Contains("§8.7.1"));
        var (okGood, errorsGood, _) = EditionHarness.CompileFull("       >>DEFINE X AS 2 * 3\n" + unit, 2023);
        Assert.True(okGood, string.Join("; ", errorsGood));
    }
}
