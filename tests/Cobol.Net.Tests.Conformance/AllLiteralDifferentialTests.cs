// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The figurative constant <c>ALL literal-1</c> (ISO/IEC 1989:2023 §8.3.3.6.4, Format 6). In a width-specified context
/// — a VALUE clause, a fixed-length receiver, a level-88 VALUE, or a compared-with operand — the literal is repeated
/// character by character until its length is ≥ the associated width, then truncated from the right to that width
/// (GR2). In a length-unspecified context (DISPLAY), the literal is used once (GR3c). Each result is derived from the
/// spec and cross-checked against the legacy oracle.
/// </summary>
public sealed class AllLiteralDifferentialTests
{
    private static readonly ICompilerUnderTest Legacy = new LegacyCompiler();
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSpecAndLegacy(string source, string expected)
    {
        string want = CutRunner.Normalize(expected);
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(want, cout);                       // primary: conformance to ISO §8.3.3.6.4
        var (lok, lout, ldetail) = Legacy.CompileAndRun(source);
        Assert.True(lok, $"legacy oracle failed: {ldetail}");
        Assert.Equal(want, lout);                       // cross-check: the oracle agrees with the spec value
    }

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ALLLIT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Theory]
    // VALUE ALL "literal" repeated to the item width (GR2): exact multiple, and a non-multiple truncated from the right.
    [InlineData("01 D PIC X(6) VALUE ALL \"ABC\".", "    DISPLAY \"R=\" D.", "R=ABCABC")]
    [InlineData("01 D PIC X(7) VALUE ALL \"ABC\".", "    DISPLAY \"R=\" D.", "R=ABCABCA")]
    [InlineData("01 D PIC XXX VALUE ALL \"Z\".",    "    DISPLAY \"R=\" D.", "R=ZZZ")]
    // MOVE ALL "literal" fills the receiver to its width (GR2).
    [InlineData("01 R PIC X(6).", "    MOVE ALL \"AB\" TO R.\n    DISPLAY \"R=\" R.", "R=ABABAB")]
    public void AllLiteral_WidthContexts(string ws, string proc, string expected)
        => AssertSpecAndLegacy(Program(ws, proc), expected);

    [Fact]
    // A comparison repeats ALL "literal" to the OTHER operand's width (GR2): C6 "ABCABC" equals ALL "ABC" but not ALL "AB".
    public void AllLiteral_ComparisonUsesOtherOperandWidth()
        => AssertSpecAndLegacy(Program("01 C6 PIC X(6) VALUE \"ABCABC\".",
            """
                IF C6 = ALL "ABC" DISPLAY "EQ1" ELSE DISPLAY "NE1" END-IF.
                IF C6 = ALL "AB"  DISPLAY "EQ2" ELSE DISPLAY "NE2" END-IF.
            """), "EQ1\nNE2");

    [Fact]
    // A level-88 VALUE ALL "literal" is repeated to the conditional variable's width (GR2).
    public void AllLiteral_Level88Value()
        => AssertSpecAndLegacy(Program("01 FL PIC X(6) VALUE \"BACBAC\".\n   88 IS-ALL-BAC VALUE ALL \"BAC\".",
            "    IF IS-ALL-BAC DISPLAY \"YES\" ELSE DISPLAY \"NO\" END-IF."), "YES");

    [Fact]
    // DISPLAY is a length-UNSPECIFIED context (GR3c): ALL "literal" is used once.
    public void AllLiteral_DisplayUsesLiteralOnce()
        => AssertSpecAndLegacy(Program("01 FILLER PIC X.",
            "    DISPLAY \"R=\" ALL \"XY\"."), "R=XY");
}
