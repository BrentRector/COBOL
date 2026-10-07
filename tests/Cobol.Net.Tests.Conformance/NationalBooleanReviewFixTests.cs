// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Regression locks for the Phase-4a national/boolean ADVERSARIAL-REVIEW wave (the DEVLOG 615/617/619 cadence).
/// Each fact pins one confirmed finding so it cannot silently re-open:
/// <list type="bullet">
/// <item>Apostrophe-delimited N'…'/B'…' literals decode (ISO §8.3.3.1 — both quote forms equal-standing; the
///   delimiters are not part of the value; a doubled opening quote is one embedded quote).</item>
/// <item>§14.9.25.3 SR8 — a BINARY-CHAR/-SHORT/-LONG/-DOUBLE sender requires a numeric/numeric-edited receiver.</item>
/// <item>§8.3.3.6.4 GR2 / §8.4.3.3 GR5 — a figurative MOVE fills EVERY position of a ref-mod slice.</item>
/// <item>§8.8.4.2.8 — a boolean figurative relation right-extends with boolean zeros (not spaces).</item>
/// <item>§13.18.63 SR4/SR5/SR24→SR10 — level-88 VALUE category conformance, both directions.</item>
/// <item>§8.8.4.4.3 SR8/SR4 — class conditions on boolean operands.</item>
/// <item>§15.50 — N"…"/B"…" as intrinsic-function arguments (the keyword-omitted parser leg).</item>
/// <item>The ALL-prefixed figurative VALUE (ALL SPACES / ALL ZEROS) is NOT falsely rejected.</item>
/// <item>SET condition-name TO TRUE fills a figurative-word 88 VALUE, not the word's characters.</item>
/// </list>
/// </summary>
public sealed class NationalBooleanReviewFixTests
{
    private static string Prog(string pid, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
            {proc}
            STOP RUN.
        """;

    [Fact]
    public void ApostropheLiterals_Decode_NationalAndBoolean()
    {
        string src = Prog("NBRF01", """
            01 N5 PIC N(5).
            01 B5 PIC 1(5).
            01 N6 PIC N(6).
            01 NV PIC N(3) VALUE N'ABC'.
            """, """
            MOVE N'AB' TO N5.
            DISPLAY "A=[" N5 "]".
            MOVE B'101' TO B5.
            DISPLAY "B=[" B5 "]".
            MOVE N'IT''S' TO N6.
            DISPLAY "C=[" N6 "]".
            DISPLAY "D=[" NV "]".
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("A=[AB   ]\nB=[10100]\nC=[IT'S  ]\nD=[ABC]", stdout.TrimEnd('\n'));
    }

    [Fact]
    public void MoveBinaryFamilyToNational_Rejected_SR8()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("NBRF02", """
            01 BL USAGE BINARY-LONG VALUE 42.
            01 N5 PIC N(5).
            """, "MOVE BL TO N5."), 2002);
        Assert.False(ok, "MOVE BINARY-LONG TO national must be rejected (ISO §14.9.25.3 SR8)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0819");
        EditionHarness.AssertHasDiagnostic(errors, "SR8");
    }

    [Fact]
    public void FigurativeMove_NationalRefModSlice_FillsEveryPosition()
    {
        string src = Prog("NBRF03", "01 N5 PIC N(5).", """
            MOVE N"ABCDE" TO N5.
            MOVE ZERO TO N5(2:3).
            DISPLAY "R=[" N5 "]".
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("R=[A000E]", stdout.TrimEnd('\n'));   // §8.3.3.6.4 GR2 — not 'A0  E'
    }

    [Fact]
    public void BooleanFigurativeRelation_ZeroExtends_NotSpace()
    {
        string src = Prog("NBRF04", "01 B-FLAG PIC 1(4) VALUE B\"0011\".", """
            IF B-FLAG(1:2) = ZERO DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO".
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("EQ=YES", stdout.TrimEnd('\n'));   // §8.8.4.2.8 — "00" vs boolean-zero-extended ZERO
    }

    [Theory]
    [InlineData("01 A-VAR PIC X(2).\n   88 F VALUE B\"01\".")]   // B" under alphanumeric — SR4
    [InlineData("01 B-VAR PIC 1(2).\n   88 F VALUE \"01\".")]    // plain "…" under boolean — SR10 via SR24
    public void Level88ValueCategory_Mismatch_Rejected0898(string ws)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("NBRF05", ws, "CONTINUE."), 2002);
        Assert.False(ok, "a cross-category 88 VALUE must be rejected (ISO §13.18.63 SR4/SR5/SR24→SR10)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0898");
    }

    [Theory]
    [InlineData("IF B-BIT IS NUMERIC CONTINUE.")]      // SR8 — USAGE BIT is not display/national/numeric
    [InlineData("IF B-BIT IS ALPHABETIC CONTINUE.")]   // SR4 — no ALPHABETIC on a boolean operand
    public void ClassCondition_OnBooleanOperand_Rejected0844(string proc)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Prog("NBRF06", "01 B-BIT PIC 1(4) USAGE BIT VALUE B\"0101\".", proc), 2002);
        Assert.False(ok, "a class condition on a boolean operand must be rejected (ISO §8.8.4.4.3 SR8/SR4)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
    }

    [Fact]
    public void NationalBooleanLiterals_AsIntrinsicArguments_Parse()
    {
        string src = Prog("NBRF07", "01 L PIC 9(4).", """
            MOVE FUNCTION LENGTH(N"AB") TO L.
            DISPLAY "NL=" L.
            MOVE FUNCTION LENGTH(B"1011") TO L.
            DISPLAY "BL=" L.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("NL=0002\nBL=0004", stdout.TrimEnd('\n'));   // ISO §15.50 — length in character positions
    }

    [Fact]
    public void AllFigurativeValue_OnNationalAndBoolean_NotFalselyRejected()
    {
        string src = Prog("NBRF08", """
            01 N4 PIC N(4) VALUE ALL SPACES.
            01 B4 PIC 1(4) VALUE ALL ZEROS.
            """, """
            DISPLAY "N=[" N4 "]".
            DISPLAY "B=[" B4 "]".
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("N=[    ]\nB=[0000]", stdout.TrimEnd('\n'));
    }

    [Fact]
    public void SetConditionToTrue_FigurativeValue_FillsNotWord()
    {
        string src = Prog("NBRF09", """
            01 B-FLAG PIC 1(4) VALUE B"1111".
               88 B-OFF VALUE ZERO.
            """, """
            SET B-OFF TO TRUE.
            DISPLAY "S=[" B-FLAG "]".
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("S=[0000]", stdout.TrimEnd('\n'));   // §14.9.39 F5 + §8.3.3.6.4 GR2 — not "ZERO"
    }
}
