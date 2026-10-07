// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Breadth net over expression and multi-target verb forms, pinned to the committed golden. Stood up to widen coverage
/// of the paths the G2-2 bound-tree rebuild touches (nested/parenthesized arithmetic, precedence, exponentiation,
/// negative literals, multi-target MOVE/ADD/SUBTRACT/COMPUTE GIVING, and a DISPLAY mixing operand kinds) BEFORE the
/// rebuild re-routes them — so a behavior drift in the rewrite is caught. Straight-line, truncation-only.
/// </summary>
public sealed class ExpressionDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new();

    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    /// <summary>Pin WiseOwl COBOL output to the spec-correct value where the legacy is non-conforming (its DISPLAY
    /// trims an alphanumeric operand's trailing spaces, contra ISO §14.9.11.4 GR6).</summary>
    private static void AssertSpec(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. EXPRTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    private const string Vars = """
        01 A PIC 9(3) VALUE 10.
        01 B PIC 9(3) VALUE 3.
        01 C PIC 9(3) VALUE 7.
        01 D PIC 9(3) VALUE 2.
        01 R PIC 9(5).
        """;

    [Theory]
    // Precedence, parentheses, nesting, negative literals, exponentiation.
    [InlineData("    COMPUTE R = A + B * C.\n    DISPLAY R.")]                       // 10 + 21 = 31
    [InlineData("    COMPUTE R = (A + B) * C.\n    DISPLAY R.")]                     // 13 * 7 = 91
    [InlineData("    COMPUTE R = (A + B) * (C - D).\n    DISPLAY R.")]               // 13 * 5 = 65
    [InlineData("    COMPUTE R = A - B - C.\n    DISPLAY R.")]                       // left-assoc 0
    [InlineData("    COMPUTE R = A * B + C * D.\n    DISPLAY R.")]                   // 30 + 14 = 44
    [InlineData("    COMPUTE R = A ** 2.\n    DISPLAY R.")]                          // 100
    [InlineData("    COMPUTE R = B ** 3.\n    DISPLAY R.")]                          // 27
    [InlineData("    COMPUTE R = A + 100.\n    DISPLAY R.")]                         // 110
    public void NumericExpressions(string proc) => AssertMatchesGolden(Program(Vars, proc));

    [Theory]
    // Scaled COMPUTE with parentheses and division (truncation into a scaled receiver).
    [InlineData("01 X PIC 9(2)V99.", "    COMPUTE X = 100 / 7.\n    DISPLAY X.")]         // 14.28 → 1428
    [InlineData("01 X PIC 9(3)V9.",  "    COMPUTE X = (5 + 5) / 4.\n    DISPLAY X.")]      // 2.5 → 0025
    public void ScaledExpressions(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));

    [Fact]
    public void MultiTarget_Move()
        => AssertMatchesGolden(Program("01 A PIC 9(3).\n01 B PIC 9(3).\n01 C PIC 9(3).", """
                MOVE 7 TO A B C.
                DISPLAY A B C.
            """));

    [Fact]
    public void MultiTarget_AddTo()
        => AssertMatchesGolden(Program("01 A PIC 9(3) VALUE 1.\n01 B PIC 9(3) VALUE 2.", """
                ADD 10 TO A B.
                DISPLAY A B.
            """));

    [Fact]
    public void MultiTarget_AddGiving()
        => AssertMatchesGolden(Program("01 A PIC 9(3) VALUE 4.\n01 B PIC 9(3) VALUE 5.\n01 C PIC 9(4).\n01 D PIC 9(4).", """
                ADD A B GIVING C D.
                DISPLAY C D.
            """));

    [Fact]
    public void MultiOperand_Subtract()
        => AssertMatchesGolden(Program("01 A PIC 9(3) VALUE 2.\n01 B PIC 9(3) VALUE 3.\n01 C PIC 9(3) VALUE 20.", """
                SUBTRACT A B FROM C.
                DISPLAY C.
            """));

    [Fact]
    public void MultiTarget_Compute()
        => AssertMatchesGolden(Program("01 X PIC 9(3) VALUE 6.\n01 A PIC 9(4).\n01 B PIC 9(4).", """
                COMPUTE A B = X * 2.
                DISPLAY A B.
            """));

    [Fact]
    public void Display_MixedOperandKinds()
        // TXT (X(4)="HI") sits mid-DISPLAY, so its 2 trailing spaces are transferred per ISO §14.9.11.4 GR6 (the
        // legacy trims them — non-conforming — so this is spec-pinned). NUM→0042, SGN -7→00P (trailing over-punch).
        => AssertSpec(Program("""
            01 NUM PIC 9(4) VALUE 42.
            01 TXT PIC X(4) VALUE "HI".
            01 SGN PIC S9(3) VALUE -7.
            """, """
                DISPLAY "N=" NUM " T=" TXT " S=" SGN " END".
            """), "N=0042 T=HI   S=00P END");
}
