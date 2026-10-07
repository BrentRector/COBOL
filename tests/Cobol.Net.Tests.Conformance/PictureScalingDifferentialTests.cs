// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// PICTURE <c>P</c> scaling positions (ISO/IEC 1989:2023 §13.18.40): an assumed-zero digit position that holds no
/// storage but shifts the implied decimal point. TRAILING P (<c>99P</c>) scales the stored digits up → a NEGATIVE
/// fraction scale; LEADING P (<c>P(4)9</c>) puts the point left of every digit. WiseOwl COBOL carries this as a single
/// signed scale through the whole numeric pipeline. These reproduce the exact P-pictures the NC101A MULTIPLY tests use
/// (the gap that surfaced once NC101A ran end-to-end); each evaluates the computation and DISPLAYs a literal verdict,
/// pinned to the committed golden (which scales P correctly — it is 364-NIST-green, NC101A included).
/// </summary>
public sealed class PictureScalingDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PSCALE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    [Fact]
    public void TrailingP_MultiplyScalesUp()
        // S99P holds digits that are multiples of 10 (scale −1): MOVE -990 stores 99×(−10); ×0.1 → −90.
        => AssertMatchesGolden(Program(
            "01 P1 PIC S99P.\n01 R1 PIC S9(5).",
            """
                MOVE -990 TO P1.
                MULTIPLY 0.1 BY P1.
                MOVE P1 TO R1.
                IF R1 = -90 DISPLAY "T-PASS" ELSE DISPLAY "T-FAIL".
            """));

    [Fact]
    public void LeadingP_SmallFractionMultiply()
        // P(4)9 = .00001 (the digit is the 5th fractional place): .00001 × 12345.6 = .123456 → ROUNDED .1.
        => AssertMatchesGolden(Program(
            "01 P2 PIC P(4)9 VALUE .00001.\n01 B2 PIC 9(5)V9 VALUE 12345.6.",
            """
                MULTIPLY P2 BY B2 ROUNDED.
                IF B2 = 0.1 DISPLAY "L-PASS" ELSE DISPLAY "L-FAIL".
            """));

    [Fact]
    public void LeadingP_NineDigitScale()
        // SP(8)9 = .000000001 (scale 9): × 111111111111111111 = 111111111.111111111 → into S9(18) = 111111111.
        => AssertMatchesGolden(Program(
            "01 P3 PIC SP(8)9 VALUE .000000001.\n01 B3 PIC S9(18).",
            """
                MOVE 111111111111111111 TO B3.
                MULTIPLY P3 BY B3.
                IF B3 = 111111111 DISPLAY "N-PASS" ELSE DISPLAY "N-FAIL".
            """));

    [Fact]
    public void TrailingP_ValueInitialization()
        // 99P(4) VALUE 990000 (scale −4): the stored digits are 99, the assumed point 4 places right → value 990000.
        => AssertMatchesGolden(Program(
            "01 P4 PIC 99P(4) VALUE 990000.",
            """
                IF P4 = 990000 DISPLAY "V-PASS" ELSE DISPLAY "V-FAIL".
            """));

    [Fact]
    public void TrailingP_MultiResultMultiply()
        // The NC101A F1-17 shape: MULTIPLY a BY r1 ROUNDED r2 … — a multiplier and several in-place receivers of
        // mixed P/V scales, each ri ← ri × a.
        => AssertMatchesGolden(Program(
            "01 M PIC P(4)9 VALUE .00001.\n01 R-A PIC 9(5)V9 VALUE 12345.6.\n01 R-B PIC 99P(4) VALUE 990000.",
            """
                MULTIPLY M BY R-A ROUNDED R-B.
                IF R-A = 0.1 DISPLAY "A-PASS" ELSE DISPLAY "A-FAIL".
                IF R-B = 0 DISPLAY "B-PASS" ELSE DISPLAY "B-FAIL".
            """));
}
