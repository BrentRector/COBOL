// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE CHARACTER IMAGE OF A NUMERIC FUNCTION'S RETURNED VALUE IS THE LITERAL FORM IN THE PROGRAM'S DECIMAL
/// SEPARATOR (kb/Work PB2507). §14.9.11.4 GR1 leaves the device conversion to the implementor (CONFORMANCE.md
/// DOC-A.1-92 fixes the form: the literal form of the value, exactly the result's scale), and §12.3.7.4 GR14 a)
/// makes the comma "the character written in numeric literals to represent the decimal separator" under
/// DECIMAL-POINT IS COMMA, so the image carries the comma — which is GnuCOBOL 3.2's separator for these values too
/// (the precedence order of CLAUDE.md rule 1: the standard leaves the choice, GnuCOBOL decides).
///
/// <para>The corpus goldens (85/pb2507_function_result_decimal_comma and the two 2023 ones) pin every DISPLAY lane.
/// What only a C# host can reach is the one text channel a STRICT program cannot: a noninteger function value moved to
/// a character receiver is refused by §14.9.25.3 SR10 (COBOLNET0819), and <c>--permissive</c> accepts it "as the
/// function's literal text" — with TWO receivers the value is first FROZEN in the §15.4 temporary
/// (<c>SendingValueTemp.Materialize</c>), whose image must be spelled as the un-frozen call is. And the control: a
/// program WITHOUT the clause keeps the period on every lane, so the fix is the clause's, not a new default.</para>
/// </summary>
public sealed class FunctionTextDecimalPointTests
{
    private static string Program(bool comma, string options = "") => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB2507H.
        {options}
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
        {(comma ? "    DECIMAL-POINT IS COMMA." : "    CLASS DIGITS IS \"0\" THRU \"9\".")}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 N PIC S9V99 VALUE {(comma ? "-1,5" : "-1.5")}.
        01 T PIC X(12).
        01 U PIC X(12).
        PROCEDURE DIVISION.
            MOVE FUNCTION ABS(N) TO T
            DISPLAY "[" T "]"
            MOVE FUNCTION ABS(N) TO T U
            DISPLAY "[" T "][" U "]"
            DISPLAY FUNCTION ABS(N)
            STOP RUN.
        """;

    private static string Run(bool comma, string options = "")
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(Program(comma, options), 2023, permissive: true);
        Assert.True(ok, detail);
        return stdout.Replace("\r\n", "\n").TrimEnd('\n') + "\n";   // the harness trims the final line end; state it once
    }

    [Fact]
    public void UnderDecimalPointIsComma_TheFrozenTemporaryAndTheCallAgree_OnTheComma()
    {
        // One receiver renders the call; two receivers freeze it into the §15.4 temporary first. The temporary
        // is a 9-fraction-digit working item, so its literal form carries nine digits (PB1007) — in the comma.
        Assert.Equal("[1,50        ]\n[1,500000000 ][1,500000000 ]\n1,50\n", Run(comma: true));
    }

    [Fact]
    public void UnderStandardDecimal_TheSdidiLaneAgrees()
    {
        Assert.Equal("[1,50        ]\n[1,500000000 ][1,500000000 ]\n1,50\n",
            Run(comma: true, options: "OPTIONS. ARITHMETIC IS STANDARD-DECIMAL."));
    }

    [Fact]
    public void WithoutTheClause_EveryLaneKeepsThePeriod()
    {
        Assert.Equal("[1.50        ]\n[1.500000000 ][1.500000000 ]\n1.50\n", Run(comma: false));
    }
}
