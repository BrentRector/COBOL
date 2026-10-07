// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The SET statement's COBOL-85 table-handling surface (ISO §14.9.39 Formats 1–2; COBOLNET_DESIGN §3.5/§12.3):
/// index-name receivers/senders (a C# <c>long</c> occurrence number), USAGE INDEX data items (unchanged copy,
/// GR2b), numeric receivers of an index's occurrence number (GR2c), UP/DOWN BY index arithmetic, the once-evaluated
/// sender (GR2/GR3), and index-names in relation conditions (ISO §13.18.38). Pinned to the legacy oracle (it is
/// NIST-85 green over the whole table series).
/// </summary>
public sealed class SetIndexDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SETTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {procedure}
            STOP RUN.
        """;

    private const string Table = """
        01 WS-TBL.
           05 ITM PIC 99 OCCURS 5 TIMES INDEXED BY IX-1 IX-2.
        01 WS-N PIC 9999.
        """;

    [Fact]
    public void SetIndexToLiteral_ThenSubscript()
        => AssertMatchesGolden(Program(Table, """
                MOVE 11 TO ITM(1).
                MOVE 22 TO ITM(2).
                MOVE 33 TO ITM(3).
                SET IX-1 TO 3.
                DISPLAY ITM(IX-1).
            """));

    [Fact]
    public void SetIndexUpDownBy()
        => AssertMatchesGolden(Program(Table, """
                MOVE 11 TO ITM(1).
                MOVE 55 TO ITM(5).
                SET IX-1 TO 1.
                SET IX-1 UP BY 4.
                DISPLAY ITM(IX-1).
                SET IX-1 DOWN BY 4.
                DISPLAY ITM(IX-1).
            """));

    [Fact]
    public void SetNumericToIndex_OccurrenceNumber()
        => AssertMatchesGolden(Program(Table, """
                SET IX-1 TO 4.
                SET WS-N TO IX-1.
                DISPLAY WS-N.
            """));

    [Fact]
    public void SetIndexToIndex()
        => AssertMatchesGolden(Program(Table, """
                MOVE 44 TO ITM(4).
                SET IX-1 TO 4.
                SET IX-2 TO IX-1.
                DISPLAY ITM(IX-2).
            """));

    [Fact]
    public void UsageIndexDataItem_RoundTrip()
        => AssertMatchesGolden(Program(Table + "\n77 WS-IDX USAGE INDEX.", """
                MOVE 22 TO ITM(2).
                SET IX-1 TO 2.
                SET WS-IDX TO IX-1.
                SET IX-2 TO WS-IDX.
                DISPLAY ITM(IX-2).
            """));

    [Fact]
    public void SetMultipleReceivers_SenderEvaluatedOnce()
        => AssertMatchesGolden(Program(Table, """
                MOVE 33 TO ITM(3).
                SET IX-1 IX-2 TO 3.
                DISPLAY ITM(IX-1) ITM(IX-2).
            """));

    [Fact]
    public void IndexInRelationCondition()
        => AssertMatchesGolden(Program(Table, """
                SET IX-1 TO 3.
                IF IX-1 = 3 DISPLAY "EQ3" ELSE DISPLAY "NE3".
                IF IX-1 > 1 DISPLAY "GT1".
            """));

    [Fact]
    public void SetIndexToDataNameValue()
        => AssertMatchesGolden(Program(Table, """
                MOVE 55 TO ITM(5).
                MOVE 5 TO WS-N.
                SET IX-1 TO WS-N.
                DISPLAY ITM(IX-1).
            """));
}
