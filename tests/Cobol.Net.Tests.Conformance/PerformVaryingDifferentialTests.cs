// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// PERFORM VARYING (ISO §14.9.28, the VARYING phrase of Formats 1 and 2 — §14.9.28.4 GR12–13): nested induction
/// loops, TEST BEFORE and TEST AFTER shapes, omitted BY (=1), FROM/BY re-evaluated per use, index-name and
/// numeric induction variables, both inline and out-of-line. Plus the two fixes the NC243A torture exposed:
/// ALL "literal" repeats to a GROUP receiver's width (§8.3.3.6.4 GR2), and an out-of-range subscript continues
/// benignly with checking off (§8.4.2.3.4 GR2 — CobolTable.At). Pinned to the legacy oracle (NIST-85 green
/// across the VARYING series).
/// <para>⛔ THESE TESTS DO NOT MEASURE THE RESET/AUGMENT ORDERING, and this comment used to claim they did.
/// Every FROM operand below is a LITERAL, and GR13 e) 2's order — reset the inner induction variable, THEN
/// augment the one to its left — is invisible unless an AFTER level's FROM READS the level to its left. The
/// claim stood over a body of <c>AssertSameAsLegacy</c> differentials for as long as the emitter implemented the
/// opposite order (kb/Work PB436). The ordering is measured by the spec-derived golden
/// <c>conformance:85/pb436_varying_after_from_outer</c>, which writes <c>AFTER B FROM A</c> in both the inline
/// and out-of-line spellings, at two and three levels, with index-name levels, and pins the TEST AFTER arm
/// (GR13 c) 4, the opposite order) beside it.</para>
/// </summary>
public sealed class PerformVaryingDifferentialTests
{
    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    private static string Program(string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. VARYTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        {procedure}
        """;

    [Fact]
    public void Varying_OneLevel_TestBefore()
        => AssertSameAsLegacy(Program("01 WS-I PIC 99.\n01 WS-OUT PIC X(10) VALUE SPACES.", """
            MAIN-PARA.
                PERFORM SHOW VARYING WS-I FROM 1 BY 2 UNTIL WS-I > 9.
                DISPLAY WS-I.
                STOP RUN.
            SHOW.
                DISPLAY WS-I.
            """));

    [Fact]
    public void Varying_OneLevel_TestAfter()
        => AssertSameAsLegacy(Program("01 WS-I PIC 99.", """
            MAIN-PARA.
                PERFORM SHOW WITH TEST AFTER VARYING WS-I FROM 5 BY 1 UNTIL WS-I >= 5.
                DISPLAY WS-I.
                STOP RUN.
            SHOW.
                DISPLAY "RAN " WS-I.
            """));

    [Fact]
    public void Varying_TwoLevels_IterationOrder()
        => AssertSameAsLegacy(Program("01 WS-I PIC 9.\n01 WS-J PIC 9.", """
            MAIN-PARA.
                PERFORM SHOW VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 2
                    AFTER WS-J FROM 1 BY 1 UNTIL WS-J > 3.
                DISPLAY "END " WS-I " " WS-J.
                STOP RUN.
            SHOW.
                DISPLAY WS-I WS-J.
            """));

    /// <summary>SPEC-PINNED (not differential): an omitted BY phrase means augment 1 (ISO §14.9.28 GR12 — "for any
    /// BY phrase that is omitted, the augment value is 1"); three 1..2 levels run the body 2×2×2 = 8 times. The
    /// LEGACY binder crashes (IndexOutOfRange) on omitted BY with multiple AFTER levels, so it cannot oracle this.</summary>
    [Fact]
    public void Varying_ThreeLevels_OmittedBy()
    {
        var (ok, output, detail) = new CobolNetCompiler().CompileAndRun(Program(
            "01 WS-I PIC 9.\n01 WS-J PIC 9.\n01 WS-K PIC 9.\n01 WS-N PIC 999 VALUE 0.", """
            MAIN-PARA.
                PERFORM BUMP VARYING WS-I FROM 1 UNTIL WS-I > 2
                    AFTER WS-J FROM 1 UNTIL WS-J > 2
                    AFTER WS-K FROM 1 UNTIL WS-K > 2.
                DISPLAY WS-N.
                STOP RUN.
            BUMP.
                ADD 1 TO WS-N.
            """));
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal("008", output);
    }

    [Fact]
    public void Varying_IndexInduction_TableSearch()
        => AssertSameAsLegacy(Program("""
            01 WS-TBL.
               05 ITM PIC XX OCCURS 4 TIMES INDEXED BY IX-1.
            01 WS-FOUND PIC X VALUE "N".
            """, """
            MAIN-PARA.
                MOVE "AABBCCDD" TO WS-TBL.
                PERFORM LOOK VARYING IX-1 FROM 1 BY 1 UNTIL IX-1 > 4.
                DISPLAY WS-FOUND.
                STOP RUN.
            LOOK.
                IF ITM(IX-1) = "CC" MOVE "Y" TO WS-FOUND.
            """));

    [Fact]
    public void Varying_Inline()
        => AssertSameAsLegacy(Program("01 WS-I PIC 99.\n01 WS-N PIC 999 VALUE 0.", """
            MAIN-PARA.
                PERFORM VARYING WS-I FROM 2 BY 3 UNTIL WS-I > 11
                    ADD WS-I TO WS-N
                END-PERFORM.
                DISPLAY WS-N " " WS-I.
                STOP RUN.
            """));

    [Fact]
    public void AllLiteral_RepeatsToGroupWidth()
        => AssertSameAsLegacy(Program("""
            01 WS-GRP.
               05 PART PIC X(5) OCCURS 3 TIMES.
            """, """
            MAIN-PARA.
                MOVE ALL "ABC" TO WS-GRP.
                DISPLAY PART(1) "/" PART(2) "/" PART(3).
                STOP RUN.
            """));

    [Fact]
    public void OutOfRangeSubscript_ReadContinuesBenignly()
        => AssertSameAsLegacy(Program("""
            01 WS-TBL.
               05 ITM PIC XX OCCURS 3 TIMES INDEXED BY IX-1.
            """, """
            MAIN-PARA.
                MOVE "AABBCC" TO WS-TBL.
                PERFORM LOOK VARYING IX-1 FROM 1 BY 1 UNTIL IX-1 > 3.
                IF ITM(IX-1) = "ZZ" DISPLAY "IMPOSSIBLE" ELSE DISPLAY "BENIGN".
                STOP RUN.
            LOOK.
                CONTINUE.
            """));
}
