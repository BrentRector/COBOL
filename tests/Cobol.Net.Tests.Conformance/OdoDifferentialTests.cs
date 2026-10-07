// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// OCCURS DEPENDING ON (ISO/IEC 1989:2023 §13.18.38 Format 2): allocation at the MAXIMUM occurrence count
/// (§8.5.1.8 — "the physical capacity is fixed at compile time; the logical capacity may vary"), the GR7
/// current-count rule, and the GR8 group-operand extents — every quadrant plus the SEARCH / SEARCH ALL bound
/// (§14.9.37.4 GR4/GR9), STRING/UNSTRING senders (§14.9.43 GR3a / §14.9.48 GR11), and VALUE-at-maximum
/// initialization (§13.18.63 GR6). Differentially pinned to the legacy oracle, which is NIST-85 green on the
/// ODO acceptance programs NC235A/NC247A — these facts are reductions of those programs' test paragraphs.
/// </summary>
public sealed class OdoDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string id, string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {procedure}
            STOP RUN.
        """;

    /// <summary>The NC247A record shape: a 1-digit depending count, a 9-character fixed part, and the table-only
    /// subgroup whose ODO tail depends on a count OUTSIDE it (but inside the containing record).</summary>
    private const string OdoRecord = """
        01 REC.
           05 CNT PIC 9 VALUE 9.
           05 HDR PIC X(9) VALUE " ACTIVE: ".
           05 TAIL-GRP.
              10 ITM PIC X OCCURS 0 TO 9 TIMES
                 DEPENDING ON CNT
                 ASCENDING KEY IS ITM INDEXED BY IX-A.
        01 OUT-19 PIC X(19).
        """;

    [Fact] // §13.18.38 GR8b (sending side): depending item INSIDE the sending group → the current-count extent.
    public void Gr8b_GroupSend_DependingInside_UsesCurrentCount()
        => AssertMatchesGolden(Program("ODOT1", OdoRecord, """
                MOVE "123456789" TO TAIL-GRP.
                MOVE 3 TO CNT.
                MOVE REC TO OUT-19.
                DISPLAY OUT-19.
            """));

    [Fact] // §13.18.38 GR8a: depending item OUTSIDE the operand group → the current-count part is used in BOTH
           // directions; INSPECT (§14.9.22, item identification §14.6.4 step 6) sees only the live occurrences.
    public void Gr8a_Inspect_DependingOutside_CurrentCountBothWays()
        => AssertMatchesGolden(Program("ODOT2", OdoRecord + "\n01 N1 PIC 9.", """
                MOVE "123456789" TO TAIL-GRP.
                MOVE 3 TO CNT.
                MOVE 0 TO N1.
                INSPECT TAIL-GRP TALLYING N1 FOR ALL "7".
                DISPLAY N1.
                MOVE 9 TO CNT.
                MOVE 0 TO N1.
                INSPECT TAIL-GRP TALLYING N1 FOR ALL "7".
                DISPLAY N1.
            """));

    [Fact] // §13.18.38 GR8b (receiving side): depending item INSIDE the receiving group → "the maximum length of
           // the group will be used" — all MAX occurrences receive (NC247A MOV-TEST-F1-6 reduction).
    public void Gr8b_GroupReceive_DependingInside_UsesMaximumLength()
        => AssertMatchesGolden(Program("ODOT3", """
            01 SRC-REC.
               05 S-CNT PIC 9 VALUE 9.
               05 S-GRP.
                  10 S-ITM PIC X OCCURS 0 TO 9 TIMES DEPENDING ON S-CNT.
            01 DST-REC.
               05 D-CNT PIC 9 VALUE 0.
               05 D-GRP.
                  10 D-ITM PIC X OCCURS 0 TO 9 TIMES DEPENDING ON D-CNT.
            """, """
                MOVE "PQRSTUVWX" TO S-GRP.
                MOVE 3 TO D-CNT.
                MOVE SRC-REC TO DST-REC.
                DISPLAY D-GRP.
            """));

    [Fact] // §13.18.38 GR8a (receiving side): depending item OUTSIDE the receiving group → only the current-count
           // part is used; character positions past the count are NOT modified.
    public void Gr8a_GroupReceive_DependingOutside_PastCountUnchanged()
        => AssertMatchesGolden(Program("ODOT4", OdoRecord, """
                MOVE "123456789" TO TAIL-GRP.
                MOVE 3 TO CNT.
                MOVE "XYZ" TO TAIL-GRP.
                MOVE 9 TO CNT.
                DISPLAY TAIL-GRP.
            """));

    [Fact] // §8.5.1.8 + §8.4.2.3.4 GR2: the array is allocated at the MAXIMUM occurrence count — an occurrence in
           // (current..max] is legal to reference (bound is MAX, not the current count). Writing ITM(9) with the
           // count at 3, then raising the count, surfaces the stored value (NC247A INIT-WRK-AREA fills all 9).
    public void Allocation_AtMaximumOccurrences_HighSubscriptIsAddressable()
        => AssertMatchesGolden(Program("ODOT5", OdoRecord, """
                MOVE "123456789" TO TAIL-GRP.
                MOVE 3 TO CNT.
                MOVE "Q" TO ITM (9).
                MOVE 9 TO CNT.
                DISPLAY TAIL-GRP.
            """));

    [Fact] // §14.9.37.4 GR4 + §13.18.38 GR7: a serial SEARCH of an occurs-depending table reaches AT END past the
           // CURRENT count, not the maximum (NC247A SCH-TEST-F1-1/-F1-2 reduction).
    public void SerialSearch_AtEnd_PastCurrentCount()
        => AssertMatchesGolden(Program("ODOT6", OdoRecord, """
                MOVE "123456789" TO TAIL-GRP.
                MOVE 3 TO CNT.
                SET IX-A TO 1.
                SEARCH ITM
                    AT END DISPLAY "AT-END"
                    WHEN ITM (IX-A) = "7" DISPLAY "FOUND"
                END-SEARCH.
                MOVE 9 TO CNT.
                SET IX-A TO 1.
                SEARCH ITM
                    AT END DISPLAY "AT-END-2"
                    WHEN ITM (IX-A) = "7" DISPLAY "FOUND-2"
                END-SEARCH.
            """));

    [Fact] // §14.9.37.4 GR9: SEARCH ALL of an occurs-depending table is bounded by the last element of the
           // table — the CURRENT depending count (NC247A SCH-TEST-F2-3/-4 reduction).
    public void SearchAll_BoundedByCurrentCount()
        => AssertMatchesGolden(Program("ODOT7", OdoRecord, """
                MOVE "123456789" TO TAIL-GRP.
                MOVE 3 TO CNT.
                SEARCH ALL ITM
                    AT END DISPLAY "AT-END"
                    WHEN ITM (IX-A) = "7" DISPLAY "FOUND"
                END-SEARCH.
                MOVE 9 TO CNT.
                SEARCH ALL ITM
                    AT END DISPLAY "AT-END-2"
                    WHEN ITM (IX-A) = "7" DISPLAY "FOUND-2"
                END-SEARCH.
            """));

    [Fact] // §14.9.43.4 GR3a + §13.18.38 GR8: a STRING sending group with an ODO tail contributes its
           // current-count content under DELIMITED BY SIZE (NC247A STR-TEST-GF-2 reduction).
    public void String_OdoGroupSender_CurrentExtent()
        => AssertMatchesGolden(Program("ODOT8", OdoRecord, """
                MOVE "123456789" TO TAIL-GRP.
                MOVE 3 TO CNT.
                MOVE SPACES TO OUT-19.
                STRING TAIL-GRP "-T" DELIMITED BY SIZE
                    INTO OUT-19.
                DISPLAY OUT-19.
            """));

    [Fact] // §14.9.48.4 GR11 + §13.18.38 GR8: the UNSTRING sending item's size is its current-count extent —
           // receiver-sized segmentation stops at the live data (NC247A UST-TEST-GF-2 reduction).
    public void Unstring_OdoRecordSource_CurrentExtent()
        => AssertMatchesGolden(Program("ODOT9", OdoRecord + "\n01 W10 PIC X(10).\n01 W20 PIC X(20).", """
                MOVE "123456789" TO TAIL-GRP.
                MOVE 3 TO CNT.
                MOVE SPACES TO W10 W20.
                UNSTRING REC INTO W10 W20.
                DISPLAY W20.
            """));
}
