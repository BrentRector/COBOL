// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// REDEFINES Tier A (ISO/IEC 1989:2023 §13.18.44; COBOLNET_DESIGN §4.2): a same-storage-type pun — identical PIC, or
/// numeric-over-numeric of the same digit count (a 12-digit DISPLAY long reinterpreted at a different implied scale).
/// One stored field; every other name is a pass-through carrying its own scale/profile, so the shared unscaled value
/// reinterprets for free (NO byte[]). A write through any view is visible through every other (one backing). Pinned
/// to the committed golden.
/// </summary>
public sealed class RedefinesTierADifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. REDEFA.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Fact]
    public void IdenticalPic_WriteOriginal_ReadView()
        => AssertMatchesGolden(Program(
            "01 WS-A PIC 9(4) VALUE 1234.\n01 WS-B REDEFINES WS-A PIC 9(4).",
            "    MOVE 5678 TO WS-A.\n    DISPLAY WS-B."));   // 5678 — the view sees the original's write

    [Fact]
    public void IdenticalPic_WriteView_ReadOriginal()
        => AssertMatchesGolden(Program(
            "01 WS-A PIC 9(4) VALUE 1111.\n01 WS-B REDEFINES WS-A PIC 9(4).",
            "    MOVE 9999 TO WS-B.\n    DISPLAY WS-A."));   // 9999 — one shared backing, coherent both ways

    [Fact]
    public void NumericOverNumeric_SameDigitsDifferentScale()
        // PIC 9(6)V9(6) holds 22.222222 as the 12 unscaled digits 000022222222; the S9(12) view reads the same
        // unscaled value at scale 0 → 000022222222.
        => AssertMatchesGolden(Program(
            "01 WS-A PIC 9(6)V9(6).\n01 WS-B REDEFINES WS-A PIC 9(12).",
            "    MOVE 22.222222 TO WS-A.\n    DISPLAY WS-B."));   // 000022222222

    [Fact]
    public void ScaledArithmeticIntoSharedStorage_ReadViewAsInteger()
        // The NC101A MPY-TEST-F1-6 shape (a scaled MULTIPLY into a S9(6)V9(6), then read its S9(12) same-storage
        // redefiner as a 12-digit integer) — exact (×4 of 2.5 = 10.0) so it isolates REDEFINES from ROUNDED (the
        // ROUNDED phrase is a separate G3 numeric gap, not wired yet).
        => AssertMatchesGolden(Program("""
            01 WS-DS-06V06 PIC S9(6)V9(6).
            01 WS-DS-12V00-S REDEFINES WS-DS-06V06 PIC S9(12).
            """,
            "    MOVE 2.5 TO WS-DS-06V06.\n    MULTIPLY 4 BY WS-DS-06V06.\n    DISPLAY WS-DS-12V00-S."));   // 00001000000{

    [Fact]
    public void SignedNumericAlias_OverpunchImage()
        => AssertMatchesGolden(Program(
            "01 WS-A PIC S9(4) VALUE -42.\n01 WS-B REDEFINES WS-A PIC S9(4).",
            "    DISPLAY WS-B."));   // 004K — the view formats the shared signed value
}
