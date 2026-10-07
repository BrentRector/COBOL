// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Data-description level-number structure (ISO/IEC 1989:2023 §13.18.38): level 77 is an INDEPENDENT elementary item —
/// always top-level (like 01), regardless of its numeric value. The binder's level-number stack must treat 77 as a
/// root so a 77 that follows a group does NOT nest under the group's still-open subordinate item (which would
/// mis-qualify every later reference — the NC102A `THREE`/`P-COUNT` bug). Pinned to the legacy oracle.
/// </summary>
public sealed class DataLevelDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. LEVTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    [Fact]
    public void Level77_AfterGroup_IsARoot_NotNested()
        // STANDALONE (77) follows a group whose last subordinate is level 05; 77 > 05 must NOT nest it under the group.
        => AssertMatchesGolden(Program(
            "01 G.\n   05 A PIC X(3) VALUE \"ABC\".\n   05 B PIC 9(3) VALUE 123.\n77 STANDALONE PIC 9(5) VALUE 42.",
            "    ADD 1 TO STANDALONE.\n    DISPLAY STANDALONE."));   // 00043 — resolved as the root, not G.STANDALONE

    [Fact]
    public void Level77_AfterElementary01_IsARoot()
        // The exact NC102A shape: a 77 numeric item right after a numeric-edited elementary 01.
        => AssertMatchesGolden(Program(
            "01 EDF PIC $*9.99 VALUE ZERO.\n77 THREE PIC 9 VALUE 3.\n77 P-COUNT PIC 9(6).",
            "    MOVE THREE TO P-COUNT.\n    DISPLAY P-COUNT."));   // 000003

    [Fact]
    public void Level77_BetweenGroups_EachResolvesIndependently()
        => AssertMatchesGolden(Program(
            "01 G1.\n   05 X PIC 9(3) VALUE 111.\n77 MIDDLE PIC 9(3) VALUE 222.\n01 G2.\n   05 Y PIC 9(3) VALUE 333.",
            "    DISPLAY X \"-\" MIDDLE \"-\" Y."));   // 111-222-333 — three independent items
}
