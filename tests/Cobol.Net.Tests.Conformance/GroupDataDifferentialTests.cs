// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G2-1a capability checkpoint: <b>group items</b> (→ nested <c>record struct</c>s), <b>qualified references</b>
/// (OF/IN, resolved through the <c>ByName</c> multimap), <b>figurative-constant VALUE</b> (ZERO/SPACE), and
/// member access through <c>ReferenceResolver</c>→<c>Place</c> in DISPLAY / MOVE / arithmetic. Pinned to the legacy
/// oracle on the NIST acceptance basis, or to the spec where the legacy's DISPLAY trailing-trim is non-conforming.
/// Scope still excludes OCCURS subscripts + ref-mod (G2-1b), level-88 (G2c), signed-DISPLAY overpunch (G2d), and
/// whole-group DISPLAY (G6) — those reference forms fail loud (<c>NotImplemented</c>) until their slice lands.
/// </summary>
public sealed class GroupDataDifferentialTests
{
    private static readonly ICompilerUnderTest Legacy = new LegacyCompiler();
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    private static void AssertSpec(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    private static string Program(string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. GRPTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {procedure}
            STOP RUN.
        """;

    private const string Rec = """
        01 WS-REC.
           05 WS-NAME PIC X(6) VALUE "BOB".
           05 WS-NUM  PIC 9(4) VALUE 42.
        """;

    [Fact]
    public void GroupLeaf_Display()
        => AssertSameAsLegacy(Program(Rec, "    DISPLAY WS-NAME.\n    DISPLAY WS-NUM."));

    [Fact]
    public void GroupLeaf_MoveThenDisplay()
        => AssertSameAsLegacy(Program(Rec, """
                MOVE "CAT" TO WS-NAME.
                MOVE 7 TO WS-NUM.
                DISPLAY WS-NAME.
                DISPLAY WS-NUM.
            """));

    [Fact]
    public void GroupLeaves_Arithmetic()
        => AssertSameAsLegacy(Program("""
            01 WS-REC.
               05 A PIC 9(3) VALUE 10.
               05 B PIC 9(3) VALUE 20.
               05 R PIC 9(4).
            """, "    ADD A B GIVING R.\n    DISPLAY R."));

    [Fact]
    public void NestedGroup_MemberAccess()
        => AssertSameAsLegacy(Program("""
            01 OUTER.
               05 MID.
                  10 LEAF  PIC 9(3) VALUE 5.
                  10 LEAF2 PIC X(2) VALUE "Z".
            """, "    DISPLAY LEAF.\n    DISPLAY LEAF2."));

    [Fact]
    public void QualifiedName_OfIn()
        => AssertSameAsLegacy(Program("""
            01 REC-A.
               05 FLD PIC X(3) VALUE "AAA".
            01 REC-B.
               05 FLD PIC X(3) VALUE "BBB".
            """, "    DISPLAY FLD OF REC-A.\n    DISPLAY FLD IN REC-B."));

    [Fact]
    public void QualifiedName_NestedScope()
        => AssertSameAsLegacy(Program("""
            01 GRP-A.
               05 SUB-A.
                  10 AMT PIC 9(3) VALUE 7.
            01 GRP-B.
               05 AMT PIC 9(3) VALUE 9.
            """, "    DISPLAY AMT OF SUB-A.\n    DISPLAY AMT OF GRP-B."));

    [Theory]
    // Figurative-constant VALUE that is trailing-clean → legacy is a valid oracle.
    [InlineData("01 WS-Z PIC 9(4) VALUE ZERO.", "    DISPLAY WS-Z.")]
    [InlineData("01 WS-Z PIC 9(2) VALUE ZEROS.", "    DISPLAY WS-Z.")]
    [InlineData("01 WS-AZ PIC X(3) VALUE ZEROS.", "    DISPLAY WS-AZ.")]
    public void FigurativeValue(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));

    [Theory]
    // Figurative VALUE exposing internal trailing fill (a trailing "]") → spec-pinned (the legacy trims).
    [InlineData("01 WS-S PIC X(5) VALUE SPACES.", "    DISPLAY \"[\" WS-S \"]\".", "[     ]")]
    [InlineData("01 WS-AZ PIC X(4) VALUE ZEROS.", "    DISPLAY \"[\" WS-AZ \"]\".", "[0000]")]
    public void FigurativeValue_FullFieldWidth(string ws, string proc, string expected)
        => AssertSpec(Program(ws, proc), expected);

    // ── Whole-group image over an OCCURS subordinate (ISO §14.9 group move / §8.8.4.1 group compare) ──────────
    // A group move treats the whole group, INCLUDING every fixed-OCCURS position, as one alphanumeric item; the
    // expected results are derived from the spec, then cross-checked against the legacy oracle (a regression net).

    private static void AssertSpecAndLegacy(string source, string expected)
    {
        string want = CutRunner.Normalize(expected);
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(want, cout);                       // primary: conformance to the ISO spec
        var (lok, lout, ldetail) = Legacy.CompileAndRun(source);
        Assert.True(lok, $"legacy oracle failed: {ldetail}");
        Assert.Equal(want, lout);                       // cross-check: the oracle agrees with the spec value
    }

    [Fact]
    // OCCURS of an alphanumeric element: a whole-group MOVE distributes the image across every occurrence (ISO §14.9),
    // the group compares as its concatenated image (§8.8.4.1), and a subscripted write is visible in the group image.
    public void WholeGroupImage_OccursOfAlphanumeric()
        => AssertSpecAndLegacy(Program("01 TBL.\n   02 ELEM PIC X(2) OCCURS 3.",
            """
                MOVE "AABBCC" TO TBL.
                IF TBL = "AABBCC" DISPLAY "EQ1" ELSE DISPLAY "NE1" END-IF.
                MOVE "XY" TO ELEM (2).
                IF TBL = "AAXYCC" DISPLAY "EQ2" ELSE DISPLAY "NE2" END-IF.
                IF ELEM (3) = "CC" DISPLAY "EQ3" ELSE DISPLAY "NE3" END-IF.
            """), "EQ1\nEQ2\nEQ3");

    [Fact]
    // OCCURS of a numeric-DISPLAY element: the leaves store their zoned character image in the whole-group image, yet
    // a subscripted reference still reads/writes the numeric value (ISO §14.9; the image facility and the numeric
    // pipeline share one string-stored representation).
    public void WholeGroupImage_OccursOfNumericDisplay()
        => AssertSpecAndLegacy(Program("01 NTBL.\n   02 NE PIC 9(3) OCCURS 3.\n01 WS-OUT PIC X(9).",
            """
                MOVE 12 TO NE (1).
                MOVE 34 TO NE (2).
                MOVE 56 TO NE (3).
                ADD 1 TO NE (2).
                MOVE NTBL TO WS-OUT.
                IF WS-OUT = "012035056" DISPLAY "EQ" ELSE DISPLAY "NE" END-IF.
                DISPLAY "NE2=" NE (2).
            """), "EQ\nNE2=035");

    [Fact]
    // OCCURS of a GROUP element (multi-level): the table image is each occurrence's group image concatenated, and
    // FromImage distributes back into each occurrence's subordinate leaves (ISO §14.9).
    public void WholeGroupImage_OccursOfGroup()
        => AssertSpecAndLegacy(Program("01 T2.\n   02 G OCCURS 2.\n      03 GA PIC X(2).\n      03 GB PIC X.",
            """
                MOVE "P1QR2S" TO T2.
                IF GA (1) = "P1" DISPLAY "A1" ELSE DISPLAY "NA1" END-IF.
                IF GB (2) = "S" DISPLAY "B2" ELSE DISPLAY "NB2" END-IF.
                IF T2 = "P1QR2S" DISPLAY "EQ" ELSE DISPLAY "NE" END-IF.
            """), "A1\nB2\nEQ");
}
