// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The three-operand <c>… TO/FROM … GIVING …</c> arithmetic forms (ISO/IEC 1989:2023 §14.9.2 ADD / §14.9.44
/// SUBTRACT, Format 2 of each): the
/// TO/FROM operand participates in the sum/difference but is NOT a receiver — only the GIVING operands receive.
/// Pinned to the committed golden.
/// </summary>
public sealed class ArithmeticGivingDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ARGIV.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    private const string Vars = "01 A PIC 9(4) VALUE 0010.\n01 B PIC 9(4) VALUE 0003.\n01 C PIC 9(4).";

    [Fact]
    public void AddToGiving_IncludesTheToOperand()
        // ADD A TO B GIVING C  →  C = B + A = 13 (the TO operand B is an addend; B is not modified).
        => AssertMatchesGolden(Program(Vars, "    ADD A TO B GIVING C.\n    DISPLAY C \"|\" B."));   // 0013|0003

    [Fact]
    public void AddSeveralGiving()
        => AssertMatchesGolden(Program(Vars, "    ADD A B GIVING C.\n    DISPLAY C."));   // 0013

    [Fact]
    public void SubtractFromGiving()
        // SUBTRACT A FROM B GIVING C  →  C = B - A (B not modified).
        => AssertMatchesGolden(Program("01 A PIC 9(4) VALUE 0003.\n01 B PIC 9(4) VALUE 0010.\n01 C PIC 9(4).",
            "    SUBTRACT A FROM B GIVING C.\n    DISPLAY C \"|\" B."));   // 0007|0010

    [Fact]
    public void MultiplyGiving()
        => AssertMatchesGolden(Program(Vars, "    MULTIPLY A BY B GIVING C.\n    DISPLAY C."));   // 0030

    [Fact]
    public void DivideIntoGiving()
        => AssertMatchesGolden(Program("01 A PIC 9(4) VALUE 0003.\n01 B PIC 9(4) VALUE 0012.\n01 C PIC 9(4).",
            "    DIVIDE A INTO B GIVING C.\n    DISPLAY C."));   // 0004
}
