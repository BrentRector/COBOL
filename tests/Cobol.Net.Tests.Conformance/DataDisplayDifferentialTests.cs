// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G2 verification checkpoint: a program that declares elementary data and <c>DISPLAY</c>s it (COBOLNET_DESIGN
/// §16 G2). Two kinds of assertion, both grounded in the ISO/IEC 1989:2023 specification:
/// <list type="bullet">
///   <item><see cref="AssertSameAsLegacy"/> — the legacy byte-engine oracle (364-NIST-green) is a sound reference
///         here, so WiseOwl COBOL stdout must equal legacy stdout <b>on the NIST acceptance basis</b> (per-line
///         trailing-space stripped — the guard's <c>normalize()</c>). Used wherever that normalization makes the
///         two agree (single/trailing operand, numeric).</item>
///   <item><see cref="AssertSpec"/> — pinned to the <b>spec-correct</b> value with an ISO citation, used where the
///         legacy is non-conforming. The legacy trims trailing spaces off an alphanumeric DISPLAY operand, which
///         ISO §14.9.11.4 GR1/GR6 forbid ("the content of each operand … the size … is the sum of the sizes of
///         the operands"); WiseOwl COBOL emits the full field, so a test that exposes <i>internal</i> trailing spaces
///         (a trailing <c>"]"</c>) is pinned to the spec, not the quirk.</item>
/// </list>
/// Scope is narrow per the G-staging: elementary-item DISPLAY only (no whole-group DISPLAY — <c>AsImage</c> is G6),
/// straight-line code only (no PERFORM / GO TO — the PC dispatcher is G4), no numeric-edited PICTUREs
/// (<c>CobolEdit</c> is G3) and no signed-DISPLAY overpunch (<c>FormatDisplaySigned</c> is G2d). New fragments are
/// added here as the G2 data model grows (groups, OCCURS, qualified/subscripted references, level-88).
/// </summary>
public sealed class DataDisplayDifferentialTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    /// <summary>Compile + run <paramref name="source"/> on both engines; assert identical stdout (NIST basis).</summary>
    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    /// <summary>
    /// Compile + run <paramref name="source"/> on WiseOwl COBOL and assert its stdout equals the <b>spec-correct</b>
    /// <paramref name="expected"/> (NIST-basis normalized). Used where the legacy oracle is non-conforming.
    /// </summary>
    private static void AssertSpec(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed to compile/run the fragment: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    /// <summary>Wrap a WORKING-STORAGE body + a PROCEDURE body into a minimal compilable program.</summary>
    private static string Program(string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DIFFTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {procedure}
            STOP RUN.
        """;

    [Fact]
    public void Display_Literal()
        => AssertSameAsLegacy(Program("01 FILLER PIC X.", """    DISPLAY "HELLO, COBOL.NET".  """));

    [Theory]
    // Alphanumeric: VALUE padding, value shorter/equal than capacity (trailing-trim makes legacy a valid oracle).
    [InlineData("01 WS-A PIC X(6) VALUE \"BOB\".", "    DISPLAY WS-A.")]
    [InlineData("01 WS-A PIC X(3) VALUE \"BOB\".", "    DISPLAY WS-A.")]
    [InlineData("01 WS-A PIC A(5) VALUE \"AB\".", "    DISPLAY WS-A.")]
    // Unsigned integer: zero-padding to the digit count (numeric is never trimmed — legacy agrees).
    [InlineData("01 WS-N PIC 9(4) VALUE 5.", "    DISPLAY WS-N.")]
    [InlineData("01 WS-N PIC 9(4) VALUE 1234.", "    DISPLAY WS-N.")]
    // Scaled fixed-point: the DISPLAY image is the unscaled digits, no decimal point.
    [InlineData("01 WS-N PIC 9(3)V99 VALUE 3.5.", "    DISPLAY WS-N.")]
    [InlineData("01 WS-N PIC 9(3)V99 VALUE 12.34.", "    DISPLAY WS-N.")]
    // Multiple operands, all trailing-clean: numeric + trailing literal.
    [InlineData("01 WS-N PIC 9(3) VALUE 7.", "    DISPLAY \"N=\" WS-N \" END\".")]
    public void Display_Data(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));

    [Theory]
    // Full-field-width DISPLAY: a trailing "]" exposes the operand's internal trailing spaces, which ISO
    // §14.9.11.4 GR6 requires be transferred (the legacy trims them — non-conforming — so we pin the spec value).
    [InlineData("01 WS-A PIC X(10) VALUE \"HI\".", "    DISPLAY \"[\" WS-A \"]\".", "[HI        ]")]
    [InlineData("01 WS-A PIC X(5) VALUE \"AB\".", "    DISPLAY \"<\" WS-A \">\".", "<AB   >")]
    [InlineData("01 WS-A PIC X(4).", "    DISPLAY \"[\" WS-A \"]\".", "[    ]")]   // no VALUE → all spaces
    public void Display_FullFieldWidth(string ws, string proc, string expected)
        => AssertSpec(Program(ws, proc), expected);

    [Theory]
    // MOVE then DISPLAY: literal/numeric source into a receiver, observing receiver rules (legacy-valid cases).
    [InlineData("01 WS-A PIC X(6).", "    MOVE \"CAT\" TO WS-A.\n    DISPLAY WS-A.")]
    [InlineData("01 WS-N PIC 9(5).", "    MOVE 42 TO WS-N.\n    DISPLAY WS-N.")]
    [InlineData("01 WS-N PIC 9(3)V99.", "    MOVE 1.5 TO WS-N.\n    DISPLAY WS-N.")]
    // Numeric → numeric MOVE with a scale change (the source fraction is truncated).
    [InlineData("01 WS-A PIC 9(3)V99 VALUE 12.34.\n01 WS-B PIC 9(3).",
                "    MOVE WS-A TO WS-B.\n    DISPLAY WS-B.")]
    public void MoveThenDisplay(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));

    [Theory]
    // Alphanumeric → alphanumeric MOVE: left-justified, space-filled to the receiver width (ISO §14.9.24 GR4).
    // The trailing "]" exposes the fill, so this is spec-pinned (legacy trims).
    [InlineData("01 WS-A PIC X(3) VALUE \"XY\".\n01 WS-B PIC X(6) VALUE \"......\".",
                "    MOVE WS-A TO WS-B.\n    DISPLAY \"[\" WS-B \"]\".", "[XY    ]")]
    public void MoveThenDisplay_FullFieldWidth(string ws, string proc, string expected)
        => AssertSpec(Program(ws, proc), expected);
}
