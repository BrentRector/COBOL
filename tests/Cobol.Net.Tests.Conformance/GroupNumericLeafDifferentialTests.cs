// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G6 (DISPLAY-homogeneous, with numeric-DISPLAY leaves): whole-group MOVE / DISPLAY / compare for a group that mixes
/// alphanumeric and <b>numeric USAGE-DISPLAY</b> leaves (e.g. the CCVS <c>TEST-RESULTS</c> record with its
/// <c>DOTVALUE PIC 99</c>). Per ISO/IEC 1989:2023 §14.9 MOVE general rule 4, a whole-group move is a pure character
/// copy with no conversion, "filled without consideration for the individual elementary or group items", so a numeric-DISPLAY
/// subordinate can legitimately hold non-numeric characters (spaces). WiseOwl COBOL stores such a leaf as its character
/// image (a <c>string</c>) — NOT a lossy native <c>long</c> — so the group image is byte-faithful (no byte[] — the
/// owner-locked no-byte-substrate model; COBOLNET_DESIGN §14.4 / §4 Tier-B). Numeric use of the leaf decodes via
/// <c>CobolNum.ParseDisplay</c> / formats via <c>FormatDisplay</c>. Pinned to the committed golden, spec-pinned where
/// the legacy DISPLAY trailing-trim quirk shows internal spaces.
/// </summary>
public sealed class GroupNumericLeafDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new();

    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static void AssertSpec(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. GNLTEST.
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
           05 WS-A PIC X(2) VALUE "AB".
           05 WS-N PIC 99.
        """;

    [Fact]
    public void NumericValueFormatsInGroupImage()
        // MOVE 7 TO WS-N → "07"; the whole-group DISPLAY shows "AB07" (DISPLAY WS-REC forces WS-N's image storage).
        => AssertMatchesGolden(Program(Rec, "    MOVE 7 TO WS-N.\n    DISPLAY WS-REC."));   // AB07

    [Fact]
    public void SpacesLandInNumericPosition_TheCcvsDotvalueCase()
        // MOVE a short literal to the group → the trailing numeric leaf's positions get SPACES (ISO §14.9.25.4 GR4: no
        // conversion), NOT "00". The bracket exposes the internal spaces, so spec-pinned (legacy trims them).
        => AssertSpec(
            Program("""
                01 WS-REC.
                   05 WS-A PIC X(2).
                   05 WS-N PIC 99 VALUE 42.
                   05 WS-B PIC X(2) VALUE "ZZ".
                """, "    MOVE \"X\" TO WS-REC.\n    DISPLAY \"[\" WS-REC \"]\"."),
            "[X     ]");   // 'X' + 5 spaces (WS-A tail, WS-N, WS-B all space-filled by the group move)

    [Fact]
    public void MoveSpacesToGroupWithNumericLeaf()
        => AssertSpec(Program(Rec, "    MOVE SPACES TO WS-REC.\n    DISPLAY \"[\" WS-REC \"]\"."), "[    ]");

    [Fact]
    public void NumericUseOfAnImageStoredLeaf_RoundTripsViaParseDisplay()
        // WS-N is stored as its image (DISPLAY WS-REC makes WS-REC whole-referenced); numeric ADD must decode it.
        => AssertMatchesGolden(Program(Rec,
            "    MOVE 25 TO WS-N.\n    ADD 1 TO WS-N.\n    DISPLAY WS-REC."));   // AB26

    [Fact]
    public void GroupToGroupCopyPreservesNumericLeafImage()
        => AssertMatchesGolden(Program("""
            01 WS-SRC.
               05 S-A PIC X(2) VALUE "AB".
               05 S-N PIC 99 VALUE 12.
            01 WS-DST.
               05 D-A PIC X(2).
               05 D-N PIC 99.
            """, "    MOVE WS-SRC TO WS-DST.\n    DISPLAY WS-DST."));   // AB12

    [Fact]
    public void SignedNumericLeafShowsOverpunchInGroupImage()
        => AssertMatchesGolden(Program("""
            01 WS-REC.
               05 WS-A PIC X(2) VALUE "AB".
               05 WS-N PIC S99 VALUE -42.
            """, "    DISPLAY WS-REC."));   // AB4K  (trailing over-punch: -42 → "4K")

    [Fact]
    public void NestedGroupWithNumericLeaf()
        => AssertMatchesGolden(Program("""
            01 WS-OUTER.
               05 WS-HEAD PIC X(2) VALUE "HD".
               05 WS-MID.
                  10 WS-X PIC X(2) VALUE "XY".
                  10 WS-CNT PIC 999 VALUE 7.
            """, "    DISPLAY WS-OUTER."));   // HDXY007

    [Fact]
    public void NumericLeafNotWholeReferenced_StaysNativeAndUnaffected()
        // WS-REC is never used as a whole, so WS-N stays a native long; ordinary numeric DISPLAY still works.
        => AssertMatchesGolden(Program(Rec, "    MOVE 9 TO WS-N.\n    DISPLAY WS-N."));   // 09
}
