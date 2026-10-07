// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The numeric-edited receiver stack (ISO §13.18.40.4 editing rules via <c>CobolEdit.Format</c>): zero
/// suppression/replacement (Z *), fixed and floating insertion (cs + - CR DB), simple/special insertion
/// (B 0 / , .), MOVE editing (§14.9.25.4 GR5), arithmetic GIVING into edited receivers with ROUNDED applied
/// BEFORE editing at the mask's scale (§14.7.4/§14.7.7), DIVIDE … REMAINDER (§14.9.12 GR7 — the remainder uses
/// the TRUNCATED intermediate quotient), and the alphanumeric→numeric MOVE (§14.9.25.4 GR6 unsigned-integer
/// treatment via <c>CobolNum.FromAlphanumeric</c>). Pinned to the committed golden (NIST-85 green).
/// </summary>
public sealed class NumericEditedDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. EDTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {procedure}
            STOP RUN.
        """;

    [Fact]
    public void ZeroSuppression_AndPoint()
        => AssertMatchesGolden(Program("01 E1 PIC ZZ,ZZ9.99.\n01 N1 PIC 9(5)V99 VALUE 1234.5.", """
                MOVE N1 TO E1.
                DISPLAY ">" E1 "<".
                MOVE ZERO TO E1.
                DISPLAY ">" E1 "<".
            """));

    [Fact]
    public void AsteriskFill_CheckProtect()
        => AssertMatchesGolden(Program("01 E1 PIC **,**9.99.\n01 N1 PIC 9(5)V99 VALUE 42.5.", """
                MOVE N1 TO E1.
                DISPLAY ">" E1 "<".
            """));

    /// <summary>SPEC-PINNED (not differential): a non-negative value renders CR as TWO SPACES — the positions stay
    /// part of the field (ISO §13.18.40.4 fixed insertion: "…replaced by spaces"). The legacy trims the trailing
    /// spaces on DISPLAY (its known §14.9.11.4 GR6 non-conformance), so it cannot oracle the full-width image.</summary>
    [Fact]
    public void FixedCurrency_AndCreditDebit()
    {
        var (ok, output, detail) = new CobolNetCompiler().CompileAndRun(Program(
            "01 E1 PIC $ZZ9.99CR.\n01 N1 PIC S9(3)V99 VALUE -72.10.", """
                MOVE N1 TO E1.
                DISPLAY ">" E1 "<".
                MOVE 72.10 TO N1.
                MOVE N1 TO E1.
                DISPLAY ">" E1 "<".
            """));
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(">$ 72.10CR<\n>$ 72.10  <", output);
    }

    [Fact]
    public void FloatingCurrency()
        => AssertMatchesGolden(Program("01 E1 PIC $$,$$9.99.\n01 N1 PIC 9(4)V99 VALUE 3.07.", """
                MOVE N1 TO E1.
                DISPLAY ">" E1 "<".
            """));

    [Fact]
    public void TrailingMinus_AndSimpleInsertion()
        => AssertMatchesGolden(Program("01 E1 PIC ZZZ9-.\n01 E2 PIC 99B99/99.\n01 N1 PIC S9(4) VALUE -123.", """
                MOVE N1 TO E1.
                DISPLAY ">" E1 "<".
                MOVE 123456 TO E2.
                DISPLAY ">" E2 "<".
            """));

    [Fact]
    public void ArithmeticGiving_RoundedBeforeEditing()
        => AssertMatchesGolden(Program("01 E1 PIC $ZZ9.99.\n01 A PIC 9(3)V999 VALUE 24.035.\n01 B PIC 9 VALUE 3.", """
                MULTIPLY A BY B GIVING E1 ROUNDED.
                DISPLAY ">" E1 "<".
            """));

    [Fact]
    public void DivideRemainder_Basic()
        => AssertMatchesGolden(Program("01 Q PIC 9(3).\n01 R PIC 9(3).\n01 D PIC 9(4) VALUE 1234.", """
                DIVIDE D BY 97 GIVING Q REMAINDER R.
                DISPLAY Q " " R.
                DIVIDE 97 INTO D GIVING Q REMAINDER R.
                DISPLAY Q " " R.
            """));

    [Fact]
    public void DivideRemainder_RoundedQuotient_TruncatedIntermediate()
        => AssertMatchesGolden(Program("01 Q PIC 9(3).\n01 R PIC 9(3).", """
                DIVIDE 1000 BY 300 GIVING Q ROUNDED REMAINDER R.
                DISPLAY Q " " R.
            """));

    [Fact]
    public void AlphanumericEdited_SimpleInsertion()
        => AssertMatchesGolden(Program("01 E1 PIC XBXBXBX.\n01 E2 PIC XX/XX/XX.\n01 E3 PIC 990099.", """
                MOVE "NPLD" TO E1.
                DISPLAY ">" E1 "<".
                MOVE "311224" TO E2.
                DISPLAY ">" E2 "<".
                MOVE 3107 TO E3.
                DISPLAY ">" E3 "<".
            """));

    [Fact]
    public void AllSymbolMask_AsteriskFillOnZero()
        => AssertMatchesGolden(Program("01 E1 PIC ****.\n01 N1 PIC 9(4) VALUE 0.", """
                MOVE N1 TO E1.
                DISPLAY ">" E1 "<".
                MOVE 42 TO N1.
                MOVE N1 TO E1.
                DISPLAY ">" E1 "<".
            """));

    [Fact]
    public void AlphanumericToNumeric_UnsignedInteger()
        => AssertMatchesGolden(Program("01 X1 PIC X(5) VALUE \"00347\".\n01 N1 PIC 9(5).", """
                MOVE X1 TO N1.
                DISPLAY N1.
                ADD 3 TO N1.
                DISPLAY N1.
            """));
}
