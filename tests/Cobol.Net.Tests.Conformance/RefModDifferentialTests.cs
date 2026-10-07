// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G2-1c: reference modification <c>item(start:length)</c> (ISO §8.4.3.3.4) — read (DISPLAY / MOVE source /
/// comparison) and write (MOVE into a slice), with literal and data-name start/length and the "to the end" form.
/// Pinned to the committed golden.
/// </summary>
public sealed class RefModDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new();

    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RMTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    private const string Six = "01 X PIC X(6) VALUE \"ABCDEF\".";

    [Theory]
    // Read: literal start/length, the to-end form, and a data-name start.
    [InlineData(Six, "    DISPLAY X(2:3).")]                                        // BCD
    [InlineData(Six, "    DISPLAY X(1:1).")]                                        // A
    [InlineData(Six, "    DISPLAY X(3:).")]                                         // CDEF (to end)
    [InlineData(Six + "\n01 P PIC 9 VALUE 2.", "    DISPLAY X(P:2).")]              // BC (variable start)
    [InlineData(Six + "\n01 P PIC 9 VALUE 2.\n01 L PIC 9 VALUE 3.", "    DISPLAY X(P:L).")]   // BCD (variable both)
    public void Read(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));

    [Theory]
    // Write: replace a slice (exact / longer-source truncate).
    [InlineData(Six, "    MOVE \"XY\" TO X(2:2).\n    DISPLAY X.")]                  // AXYDEF
    [InlineData(Six, "    MOVE \"PQRST\" TO X(2:3).\n    DISPLAY X.")]               // APQREF (source truncated to 3)
    public void Write(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));

    [Fact]
    public void Write_ShorterSource_SpaceFills()
    {
        // MOVE "Z" TO X(4:3) → positions 4-6 = "Z  "; the trailing fill is exposed by the "]", so this is spec-pinned
        // (cobolnet emits the full field per ISO §14.9.11.4 GR6; the legacy trims the trailing spaces — non-conforming).
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(Program(Six, "    MOVE \"Z\" TO X(4:3).\n    DISPLAY \"[\" X \"]\"."));
        Assert.True(cok, cdetail);
        Assert.Equal(CutRunner.Normalize("[ABCZ  ]"), cout);
    }

    [Fact]
    public void MoveRefModSource()
        => AssertMatchesGolden(Program("01 X PIC X(5) VALUE \"HELLO\".\n01 Y PIC X(3).", """
                MOVE X(1:3) TO Y.
                DISPLAY Y.
                MOVE X(4:2) TO Y.
                DISPLAY Y.
            """));

    [Theory]
    [InlineData("01 X PIC X(5) VALUE \"HELLO\".", "    IF X(1:2) = \"HE\" DISPLAY \"Y\" ELSE DISPLAY \"N\" END-IF.")]
    [InlineData("01 X PIC X(5) VALUE \"HELLO\".", "    IF X(3:3) = \"LLO\" DISPLAY \"Y\" ELSE DISPLAY \"N\" END-IF.")]
    public void Comparison(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));
}
