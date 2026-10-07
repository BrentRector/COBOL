// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Serial SEARCH (ISO §14.9.37 Format 1, §14.9.37.4 GR3–GR4): scan from the index's CURRENT setting, AT END on past-end,
/// WHEN arms in order (first true wins), VARYING a same-table index / other item in step, and GO TO out of a
/// WHEN body. Pinned to the legacy oracle (NIST-85 green across the table series). SEARCH ALL (Format 2) is
/// implemented and has its own spec-derived suite — <see cref="SearchIndexRangeSpecTests"/> for the GR9 index
/// range and this class's Format-1 counterpart, <c>SearchAllFormat2RuleCoverageDriftTests</c> for its syntax
/// rules; nothing about Format 2 is pinned to an oracle here.
/// </summary>
public sealed class SearchDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string procedure) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SRCH.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-TBL.
           05 ITM PIC XX OCCURS 5 TIMES INDEXED BY IX-1.
        01 WS-N PIC 99.
        PROCEDURE DIVISION.
        MAIN-PARA.
            MOVE "AABBCCDDEE" TO WS-TBL.
        {{procedure}}
        """;

    [Fact]
    public void Search_FindsFromCurrentIndexSetting()
        => AssertMatchesGolden(Program("""
                SET IX-1 TO 1.
                SEARCH ITM
                    AT END DISPLAY "NOT-FOUND"
                    WHEN ITM(IX-1) = "CC"
                        SET WS-N TO IX-1
                        DISPLAY "AT " WS-N
                END-SEARCH.
                STOP RUN.
            """));

    [Fact]
    public void Search_AtEnd_WhenStartedPastMatch()
        => AssertMatchesGolden(Program("""
                SET IX-1 TO 4.
                SEARCH ITM
                    AT END DISPLAY "NOT-FOUND"
                    WHEN ITM(IX-1) = "CC" DISPLAY "FOUND"
                END-SEARCH.
                STOP RUN.
            """));

    [Fact]
    public void Search_MultipleWhens_FirstTrueWins()
        => AssertMatchesGolden(Program("""
                SET IX-1 TO 1.
                SEARCH ITM
                    AT END DISPLAY "NOT-FOUND"
                    WHEN ITM(IX-1) = "DD" DISPLAY "DD-ARM"
                    WHEN ITM(IX-1) = "BB" DISPLAY "BB-ARM"
                END-SEARCH.
                STOP RUN.
            """));

    [Fact]
    public void Search_VaryingOtherItem_InStep()
        => AssertMatchesGolden(Program("""
                MOVE 10 TO WS-N.
                SET IX-1 TO 1.
                SEARCH ITM VARYING WS-N
                    AT END DISPLAY "NOT-FOUND"
                    WHEN ITM(IX-1) = "DD" DISPLAY "N=" WS-N
                END-SEARCH.
                STOP RUN.
            """));

    [Fact]
    public void Search_GoToOutOfWhenBody()
        => AssertMatchesGolden(Program("""
                SET IX-1 TO 1.
                SEARCH ITM
                    AT END GO TO MISS-PARA
                    WHEN ITM(IX-1) = "EE" GO TO HIT-PARA
                END-SEARCH.
            MISS-PARA.
                DISPLAY "MISS".
                STOP RUN.
            HIT-PARA.
                DISPLAY "HIT".
                STOP RUN.
            """));
}
