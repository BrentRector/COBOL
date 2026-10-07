// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G2d capability checkpoint: <b>signed-numeric DISPLAY</b> (COBOLNET_DESIGN §6.4). A signed item carries its sign
/// in its DISPLAY image per its USAGE + SIGN clause — IBM-ASCII over-punch for USAGE DISPLAY (trailing by default,
/// leading under SIGN LEADING), a separate <c>+</c>/<c>-</c> under SIGN SEPARATE, or a binary leading minus for
/// COMP/COMP-3/COMP-5. The legacy is a sound oracle here (this is conforming behavior — the bracket form exposes the
/// sign character, which is not a trailing space, so the legacy's DISPLAY trailing-trim quirk does not apply).
/// </summary>
public sealed class SignedDisplayDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SGNTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Theory]
    // USAGE DISPLAY over-punch — trailing (the no-SIGN-clause default): { A..I positive / } J..R negative.
    [InlineData("01 N PIC S9(3) VALUE -42.", "[04K]")]
    [InlineData("01 N PIC S9(3) VALUE 42.", "[04B]")]
    [InlineData("01 N PIC S9(3) VALUE -150.", "[15}]")]
    [InlineData("01 N PIC S9(3) VALUE 0.", "[00{]")]
    [InlineData("01 N PIC S9V99 VALUE -3.5.", "[35}]")]
    // SIGN LEADING over-punch (onto the first digit).
    [InlineData("01 N PIC S9(3) SIGN IS LEADING VALUE -37.", "[}37]")]
    [InlineData("01 N PIC S9(3) SIGN IS LEADING VALUE 37.", "[A37]")]
    // SIGN SEPARATE — an always-present +/- character.
    [InlineData("01 N PIC S9(3) SIGN LEADING SEPARATE VALUE -37.", "[-037]")]
    [InlineData("01 N PIC S9(3) SIGN LEADING SEPARATE VALUE 37.", "[+037]")]
    [InlineData("01 N PIC S9(3) SIGN TRAILING SEPARATE VALUE -37.", "[037-]")]
    // COMP / COMP-3 / COMP-5 — a leading minus only when negative; positive/zero is bare.
    [InlineData("01 N PIC S9(3) COMP VALUE -42.", "[-042]")]
    [InlineData("01 N PIC S9(3) COMP VALUE 42.", "[042]")]
    [InlineData("01 N PIC S9(3) COMP-3 VALUE -42.", "[-042]")]
    [InlineData("01 N PIC S9(4) COMP-5 VALUE -300.", "[-0300]")]
    public void SignedDisplay(string ws, string _)
        => AssertMatchesGolden(Program(ws, "    DISPLAY \"[\" N \"]\"."));

    [Theory]
    // The store path: a negative value MOVEd / computed into a signed item, then displayed.
    [InlineData("01 N PIC S9(3).", "    MOVE -42 TO N.\n    DISPLAY \"[\" N \"]\".")]
    [InlineData("01 N PIC S9(3).", "    COMPUTE N = 10 - 52.\n    DISPLAY \"[\" N \"]\".")]
    [InlineData("01 A PIC S9(3) VALUE 30.\n01 N PIC S9(3).",
                "    SUBTRACT 70 FROM A GIVING N.\n    DISPLAY \"[\" N \"]\".")]
    public void SignedStoreThenDisplay(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));
}
