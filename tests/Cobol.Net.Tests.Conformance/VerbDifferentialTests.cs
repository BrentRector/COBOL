// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Differential regression net for the core verbs already implemented (DEVLOG 460–463) — arithmetic, IF/ELSE,
/// inline PERFORM — pinned to the legacy oracle on the NIST acceptance basis. This locks current behavior in BEFORE
/// the G2 bound-tree/data-model rebuild re-routes every operand through <c>ReferenceResolver</c>→<c>Place</c>, so a
/// regression in the rewrite is caught immediately (the advisor's "verify against the oracle" generalized).
/// <para>
/// Scope per the G-staging: single-paragraph, straight-line code only — NO out-of-line PERFORM / GO TO (the current
/// "paragraphs run in sequence" stopgap double-executes a performed-then-fallen-through paragraph; the PC
/// dispatcher is G4), NO ROUNDED / ON SIZE ERROR (the <c>CobolInt</c>/<c>TryStore</c> engine is G3), NO signed
/// DISPLAY (overpunch is G2d). All results are numeric (never trailing-trimmed), so the legacy is a sound oracle.
/// </para>
/// </summary>
public sealed class VerbDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. VERBTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {procedure}
            STOP RUN.
        """;

    [Theory]
    // ADD … TO and ADD … GIVING (integer).
    [InlineData("01 R PIC 9(4) VALUE 10.", "    ADD 5 TO R.\n    DISPLAY R.")]
    [InlineData("01 A PIC 9(3) VALUE 7.\n01 B PIC 9(3) VALUE 8.\n01 R PIC 9(4).",
                "    ADD A B GIVING R.\n    DISPLAY R.")]
    // SUBTRACT … FROM and … GIVING.
    [InlineData("01 R PIC 9(4) VALUE 20.", "    SUBTRACT 3 FROM R.\n    DISPLAY R.")]
    [InlineData("01 A PIC 9(3) VALUE 50.\n01 R PIC 9(4).", "    SUBTRACT 8 FROM A GIVING R.\n    DISPLAY R.")]
    // MULTIPLY and DIVIDE (truncating quotient).
    [InlineData("01 R PIC 9(4) VALUE 6.", "    MULTIPLY 3 BY R.\n    DISPLAY R.")]
    [InlineData("01 R PIC 9(4).", "    DIVIDE 12 BY 4 GIVING R.\n    DISPLAY R.")]
    [InlineData("01 R PIC 9(4).", "    DIVIDE 4 INTO 13 GIVING R.\n    DISPLAY R.")]   // 13/4 = 3 (truncated)
    public void Arithmetic(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));

    [Theory]
    // COMPUTE with operator precedence and parentheses; scaled receiver truncates.
    [InlineData("01 R PIC 9(4).", "    COMPUTE R = (2 + 3) * 4.\n    DISPLAY R.")]
    [InlineData("01 R PIC 9(4).", "    COMPUTE R = 2 + 3 * 4.\n    DISPLAY R.")]          // precedence → 14
    [InlineData("01 R PIC 9(2)V99.", "    COMPUTE R = 10 / 3.\n    DISPLAY R.")]          // 3.33 truncated
    [InlineData("01 A PIC 9(3) VALUE 12.\n01 R PIC 9(4).", "    COMPUTE R = A * A.\n    DISPLAY R.")]
    public void Compute(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));

    [Theory]
    // IF / ELSE with numeric and alphanumeric comparisons, AND/OR.
    [InlineData("01 A PIC 9 VALUE 5.", "    IF A > 3\n        DISPLAY \"BIG\"\n    ELSE\n        DISPLAY \"SMALL\"\n    END-IF.")]
    [InlineData("01 A PIC 9 VALUE 5.", "    IF A = 5\n        DISPLAY \"FIVE\"\n    END-IF.")]
    [InlineData("01 A PIC 9 VALUE 5.", "    IF A > 1 AND A < 9\n        DISPLAY \"MID\"\n    END-IF.")]
    [InlineData("01 A PIC 9 VALUE 5.", "    IF A < 1 OR A = 5\n        DISPLAY \"HIT\"\n    END-IF.")]
    [InlineData("01 NM PIC X(3) VALUE \"BOB\".", "    IF NM = \"BOB\"\n        DISPLAY \"Y\"\n    ELSE\n        DISPLAY \"N\"\n    END-IF.")]
    public void IfElse(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));

    [Theory]
    // Inline PERFORM (a real C# loop — no control-flow dispatcher needed): n TIMES and UNTIL.
    [InlineData("01 I PIC 9.", "    PERFORM 3 TIMES\n        DISPLAY \"X\"\n    END-PERFORM.")]
    [InlineData("01 I PIC 9(2) VALUE 0.",
                "    PERFORM UNTIL I = 3\n        DISPLAY I\n        ADD 1 TO I\n    END-PERFORM.")]
    public void InlinePerform(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));
}
