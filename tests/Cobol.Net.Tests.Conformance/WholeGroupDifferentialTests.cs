// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G6 (core): whole-group MOVE / DISPLAY / compare for a DISPLAY-homogeneous (all-character) group via the generated
/// <c>AsImage()</c>/<c>FromImage()</c> facility (COBOLNET_DESIGN §14.4). A group is treated as alphanumeric (ISO
/// §14.9.24): its character image is the concatenation of its leaves. Mixed-usage groups (numeric/COMP leaves) are
/// the Tier-C byte island, still loud. Pinned to the legacy oracle (spec-pinned where the legacy DISPLAY
/// trailing-trim quirk shows).
/// </summary>
public sealed class WholeGroupDifferentialTests
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
        PROGRAM-ID. WGTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    private const string Rec = """
        01 WS-REC.
           05 WS-A PIC X(3) VALUE "ABC".
           05 WS-B PIC X(3) VALUE "DEF".
        """;

    [Fact]
    public void DisplayGroup() => AssertSameAsLegacy(Program(Rec, "    DISPLAY WS-REC."));   // ABCDEF

    [Fact]
    public void GroupToAlphanumeric()
        => AssertSameAsLegacy(Program(Rec + "\n01 WS-LINE PIC X(6).",
            "    MOVE WS-REC TO WS-LINE.\n    DISPLAY WS-LINE."));   // ABCDEF

    [Fact]
    public void AlphanumericToGroup()
        => AssertSameAsLegacy(Program("""
            01 WS-DST.
               05 D1 PIC X(3).
               05 D2 PIC X(3).
            01 WS-S PIC X(6) VALUE "123456".
            """, "    MOVE WS-S TO WS-DST.\n    DISPLAY D1 \"|\" D2."));   // 123|456

    [Fact]
    public void GroupToGroup()
        => AssertSameAsLegacy(Program(Rec + """

            01 WS-DST.
               05 P1 PIC X(2).
               05 P2 PIC X(4).
            """, "    MOVE WS-REC TO WS-DST.\n    DISPLAY P1 \"|\" P2."));   // AB|CDEF

    [Fact]
    public void NestedGroupImage()
        => AssertSameAsLegacy(Program("""
            01 WS-OUTER.
               05 WS-HEAD PIC X(2) VALUE "HD".
               05 WS-MID.
                  10 WS-X PIC X(2) VALUE "XY".
                  10 WS-Y PIC X(2) VALUE "ZW".
            """, "    DISPLAY WS-OUTER."));   // HDXYZW

    [Theory]
    [InlineData("    IF WS-REC = \"ABCDEF\" DISPLAY \"EQ\" ELSE DISPLAY \"NE\" END-IF.")]
    [InlineData("    IF WS-REC = \"ABCXXX\" DISPLAY \"EQ\" ELSE DISPLAY \"NE\" END-IF.")]
    public void GroupCompare(string proc) => AssertSameAsLegacy(Program(Rec, proc));

    [Fact]
    public void MoveSpacesToGroup_FillsToWidth()
        // MOVE SPACES TO a 6-char group → all spaces; the bracket exposes the fill, so spec-pinned (the legacy
        // trims trailing spaces in DISPLAY — non-conforming per ISO §14.9.11.4 GR6).
        => AssertSpec(Program(Rec, "    MOVE SPACES TO WS-REC.\n    DISPLAY \"[\" WS-REC \"]\"."), "[      ]");
}
