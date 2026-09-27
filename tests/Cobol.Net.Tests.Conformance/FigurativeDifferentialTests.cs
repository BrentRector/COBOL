// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Figurative constants (ZERO / SPACE …) in operand position — MOVE source, DISPLAY operand, and comparison
/// (ISO §8.3.3.6). Ubiquitous in real COBOL (every NC program uses <c>MOVE ZERO</c>/<c>SPACES</c>), so this is on
/// the G3-core path to running a real NC program through the differential harness. The constant materializes to the
/// receiving / other operand's category and width.
/// </summary>
public sealed class FigurativeDifferentialTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    private static void AssertSpec(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FIGTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Theory]
    // MOVE a figurative to a numeric / alphanumeric receiver (filled to width); trailing-clean → legacy-valid.
    [InlineData("01 N PIC 9(4).", "    MOVE ZERO TO N.\n    DISPLAY N.")]
    [InlineData("01 X PIC X(5).", "    MOVE ZEROS TO X.\n    DISPLAY \"[\" X \"]\".")]   // "00000" trailing-clean
    [InlineData("01 N PIC 9(3) VALUE 7.", "    MOVE ZERO TO N.\n    DISPLAY N.")]
    // DISPLAY of a figurative → one occurrence (GR3).
    [InlineData("01 FILLER PIC X.", "    DISPLAY \"[\" ZERO \"]\".")]
    [InlineData("01 FILLER PIC X.", "    DISPLAY \"[\" SPACE \"]\".")]
    public void FigurativeMoveDisplay(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));

    [Theory]
    // MOVE SPACES to alphanumeric, exposed by a trailing "]" → spec-pinned (legacy trims trailing spaces).
    [InlineData("01 X PIC X(4).", "    MOVE SPACES TO X.\n    DISPLAY \"[\" X \"]\".", "[    ]")]
    public void FigurativeMove_FullFieldWidth(string ws, string proc, string expected) => AssertSpec(Program(ws, proc), expected);

    [Theory]
    // Comparison against a figurative — materialized to the other operand's category/width.
    [InlineData("01 N PIC 9(3) VALUE 0.", "    IF N = ZERO DISPLAY \"ISZERO\" ELSE DISPLAY \"NONZERO\" END-IF.")]
    [InlineData("01 N PIC 9(3) VALUE 5.", "    IF N = ZERO DISPLAY \"ISZERO\" ELSE DISPLAY \"NONZERO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE SPACES.", "    IF X = SPACES DISPLAY \"ALLSPACE\" ELSE DISPLAY \"NOT\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"AB\".", "    IF X = SPACES DISPLAY \"ALLSPACE\" ELSE DISPLAY \"NOT\" END-IF.")]
    [InlineData("01 N PIC 9(3) VALUE 0.", "    IF N IS ZERO DISPLAY \"SIGNZERO\" END-IF.")]
    public void FigurativeComparison(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));

    [Theory]
    // Figurative ZERO as a numeric-expression operand (ISO §8.3.3.6.3 r1a / §8.3.3.6.4 r4 — ZERO is the numeric value 0). Previously the
    // arithmetic path rendered the word "ZERO" as a C# identifier (CS0103 ZEROL), breaking NC118A/119A/175A/177A.
    [InlineData("01 N PIC 9(3) VALUE 5.", "    ADD ZERO TO N.\n    DISPLAY N.")]                      // 005
    [InlineData("01 N PIC 9(3) VALUE 5.", "    COMPUTE N = N + ZERO.\n    DISPLAY N.")]                // 005
    [InlineData("01 N PIC 9(3) VALUE 8.", "    SUBTRACT ZERO FROM N.\n    DISPLAY N.")]                // 008
    [InlineData("01 N PIC 9(3) VALUE 4.", "    COMPUTE N = ZERO + N * 2.\n    DISPLAY N.")]            // 008 — ZERO as a leaf
    public void FigurativeZero_InArithmetic(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));

    [Theory]
    // ALL <figurative-word> VALUE init (equivalent to the bare figurative): ALL ZEROS on a numeric → 0; ALL ZEROS /
    // ALL SPACES on alphanumeric → fill. Previously the raw word reached the initializer (CS0103 ALLZEROSL), NC201A.
    [InlineData("01 N PIC 9(5) VALUE ALL ZEROS.", "    ADD 7 TO N.\n    DISPLAY N.")]                  // 00007
    [InlineData("01 X PIC X(4) VALUE ALL ZEROS.", "    DISPLAY X.")]                                   // 0000
    [InlineData("01 X PIC X(4) VALUE ALL SPACES.", "    DISPLAY \"[\" X.")]                            // [
    public void FigurativeAll_ValueInit(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));

    [Theory]
    // A figurative ZERO in a level-88 VALUE on a NUMERIC conditional variable: the membership test must compare the
    // value against 0, not render the word "ZERO" as an identifier (CS0103 ZERO00L at scale — NC250A).
    [InlineData("01 N PIC 9(3) VALUE 0.\n   88 IS-Z VALUE ZERO.", "    IF IS-Z DISPLAY \"Z\" ELSE DISPLAY \"NZ\".")]     // Z
    [InlineData("01 N PIC 9(3) VALUE 5.\n   88 IS-Z VALUE ZERO.", "    IF IS-Z DISPLAY \"Z\" ELSE DISPLAY \"NZ\".")]     // NZ
    [InlineData("01 N PIC 9V99 VALUE 0.\n   88 IS-Z VALUE ZERO.", "    IF IS-Z DISPLAY \"Z\" ELSE DISPLAY \"NZ\".")]     // Z (scaled)
    public void FigurativeZero_InLevel88Value(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));
}
