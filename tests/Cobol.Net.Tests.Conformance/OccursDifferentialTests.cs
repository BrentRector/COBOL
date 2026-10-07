// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G2-1b capability checkpoint: <b>OCCURS tables</b> (→ <c>T[]</c> arrays, element-initialized) and <b>subscripted
/// references</b> (literal, data-name, and relative <c>name ± k</c> subscripts) resolved through
/// <c>ReferenceResolver</c>→<c>Place</c> — each subscript attached to its OCCURS level as <c>[expr - 1]</c>
/// (COBOLNET_DESIGN §3.2/§3.4). Pinned to the committed golden on the NIST acceptance basis (all results numeric or
/// trailing-clean, so the legacy is a sound oracle). Reference modification (<c>(s:l)</c>) is still G2-1c — those
/// references fail loud until then.
/// </summary>
public sealed class OccursDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. OCCTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {procedure}
            STOP RUN.
        """;

    [Fact]
    public void ElementaryTable_LiteralSubscripts()
        => AssertMatchesGolden(Program("01 WS-TBL.\n   05 ITM OCCURS 3 PIC 9(2).", """
                MOVE 11 TO ITM(1).
                MOVE 22 TO ITM(2).
                MOVE 33 TO ITM(3).
                DISPLAY ITM(1) ITM(2) ITM(3).
            """));

    [Fact]
    public void ElementaryTable_VariableSubscript()
        => AssertMatchesGolden(Program("""
            01 WS-TBL.
               05 ITM OCCURS 3 PIC 9(2).
            01 WS-I PIC 9 VALUE 2.
            """, """
                MOVE 11 TO ITM(1).
                MOVE 22 TO ITM(2).
                MOVE 33 TO ITM(3).
                DISPLAY ITM(WS-I).
            """));

    [Fact]
    public void ElementaryTable_RelativeSubscript()
        => AssertMatchesGolden(Program("""
            01 WS-TBL.
               05 ITM OCCURS 3 PIC 9(2).
            01 WS-I PIC 9 VALUE 1.
            """, """
                MOVE 11 TO ITM(1).
                MOVE 22 TO ITM(2).
                MOVE 33 TO ITM(3).
                DISPLAY ITM(WS-I + 1).
                DISPLAY ITM(WS-I + 2).
            """));

    [Fact]
    public void GroupTable_SubscriptedMemberAccess()
        => AssertMatchesGolden(Program("""
            01 WS-GRP.
               05 ROW OCCURS 2.
                  10 A PIC 9(2).
                  10 B PIC X(3).
            """, """
                MOVE 7 TO A(1).
                MOVE "XYZ" TO B(1).
                MOVE 9 TO A(2).
                MOVE "PQR" TO B(2).
                DISPLAY A(1) "|" B(1).
                DISPLAY A(2) "|" B(2).
            """));

    [Fact]
    public void Table_ArithmeticWithSubscripts()
        => AssertMatchesGolden(Program("""
            01 WS-TBL.
               05 ITM OCCURS 3 PIC 9(3).
            01 WS-R PIC 9(4).
            """, """
                MOVE 100 TO ITM(1).
                MOVE 200 TO ITM(2).
                ADD ITM(1) ITM(2) GIVING WS-R.
                DISPLAY WS-R.
                COMPUTE ITM(3) = ITM(1) + ITM(2).
                DISPLAY ITM(3).
            """));

    [Fact]
    public void Table_ValueInitializedElements()
        => AssertMatchesGolden(Program("01 WS-TBL.\n   05 ITM OCCURS 3 PIC 9(2) VALUE 5.",
            "    DISPLAY ITM(1) ITM(2) ITM(3)."));
}
