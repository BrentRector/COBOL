// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1669 — under <c>--permissive</c> at 2002 and later the boolean operators B-AND / B-OR / B-XOR / B-NOT are
/// reserved words (ISO §8.9) that migration mode still admits as the user-defined words an older program declared
/// (§8.3.2.1 rule 1: "Reserved words shall not be used as user-defined words"). The standard spelling must not be
/// rejected for it: §8.8.2 rule 6 ("The permissible combinations of operands, operators, and parentheses in a boolean
/// expression are specified in Table 4") marks "(" a permissible symbol after every binary boolean operator and after
/// B-NOT, so <c>IF BZ B-OR (BW)</c> is a boolean condition. What tells it from the legacy table <c>B-OR (1)</c> is
/// whether the PROGRAM DECLARES the word, and the lexer — which decides at lex time whether a '(' opens a subscript —
/// learns that from the parse (<c>TokenRetypes.LexesDifferentlyFrom</c>).
/// </summary>
public sealed class BooleanOperatorPermissiveTests
{
    // Expected values, derived (BW = 1, BZ = 0, BR = result): C1 0 OR 1 = T; C2 NOT (1 XOR 0) = F; C3 1 AND (0 OR 0) = F;
    // K1 0 OR (1 AND 1) = 1 (§8.8.4.3.4 GR1: true when the value is 1).
    private const string Operators = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1669A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BW PIC 1 USAGE BIT VALUE B"1".
       01 BZ PIC 1 USAGE BIT VALUE B"0".
       01 BR PIC 1 USAGE BIT.
       PROCEDURE DIVISION.
           IF BZ B-OR (BW)
               DISPLAY "C1 T" ELSE DISPLAY "C1 F" END-IF.
           IF B-NOT (BW B-XOR BZ)
               DISPLAY "C2 T" ELSE DISPLAY "C2 F" END-IF.
           IF BW B-AND (BZ B-OR BZ)
               DISPLAY "C3 T" ELSE DISPLAY "C3 F" END-IF.
           COMPUTE BR = BZ B-OR (BW B-AND BW).
           DISPLAY "K1 " BR.
           STOP RUN.
       """;

    // The legacy table keeps working beside an operator that takes a parenthesis: B-OR is DECLARED (a data-name here,
    // subscripted), B-AND is not (the operator). Expected: B-OR(2) = 5; 1 AND (1) = T.
    private const string LegacyTableBesideOperator = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1669B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BW PIC 1 USAGE BIT VALUE B"1".
       01 BT.
          05 B-OR PIC 9 OCCURS 3.
       PROCEDURE DIVISION.
           MOVE 5 TO B-OR(2).
           DISPLAY "T " B-OR(2).
           IF BW B-AND (BW)
               DISPLAY "L T" ELSE DISPLAY "L F" END-IF.
           STOP RUN.
       """;

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AParenthesizedOperand_AfterABooleanOperator_IsTheOperatorUnderPermissive(int edition)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(Operators, edition, permissive: true);
        Assert.True(ok, detail);
        Assert.Equal(new[] { "C1 T", "C2 F", "C3 F", "K1 1" }, Lines(stdout));
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void ADeclaredLegacyOperatorWord_StaysASubscriptedDataName_BesideAnUndeclaredOperator(int edition)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(LegacyTableBesideOperator, edition, permissive: true);
        Assert.True(ok, detail);
        Assert.Equal(new[] { "T 5", "L T" }, Lines(stdout));
    }

    private static string[] Lines(string stdout) => stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
}
