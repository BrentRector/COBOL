// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Subscripted Tier-B REDEFINES views (GAP-1) + the B2 layout accounting (ISO §13.18.44: a redefined table lays
/// its occurrences end-to-end in the ONE backing; an inner REDEFINES starts at its target's first position and
/// adds no width; a sibling table contributes width × OCCURS), plus NEXT SENTENCE in an IF's THEN phrase
/// (§14.9.19.4 GR4 — the ELSE-phrase rule GR6 is exercised by the spec-derived golden
/// tests/conformance/85/pb414_next_sentence_last_sentence_inline.cob, not here). Pinned to the
/// former legacy engine (NIST-85 green over the REDEFINES table series) — a regression net, never a rule's witness.
/// </summary>
public sealed class RedefinesSubscriptedViewDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    [Fact]
    public void TableRedefinesPicture_SubscriptedElementReads()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TABLE-A PIC X(10) VALUE "0102030405".
            01 TABLE-1 REDEFINES TABLE-A.
               02 ELM PIC 99 OCCURS 5 TIMES INDEXED BY IX-1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY ELM(1) " " ELM(3) " " ELM(5).
                SET IX-1 TO 4.
                DISPLAY ELM(IX-1).
                STOP RUN.
            """);

    [Fact]
    public void SubscriptedViewWrite_VisibleThroughBacking()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TABLE-A PIC X(10) VALUE "AABBCCDDEE".
            01 TABLE-1 REDEFINES TABLE-A.
               02 ELM PIC XX OCCURS 5 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ZZ" TO ELM(3).
                DISPLAY TABLE-A.
                STOP RUN.
            """);

    [Fact]
    public void NestedOccursInRedefines_TwoSubscripts()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TABLE-A PIC X(12) VALUE "ABCDEFGHIJKL".
            01 TABLE-1 REDEFINES TABLE-A.
               02 ROW-G OCCURS 2 TIMES.
                  03 CEL PIC XX OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY CEL(1, 1) " " CEL(1, 3) " " CEL(2, 1) " " CEL(2, 3).
                STOP RUN.
            """);

    [Fact]
    public void SiblingAfterTable_OffsetCountsAllOccurrences()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 AREA-A PIC X(8) VALUE "11223344".
            01 AREA-1 REDEFINES AREA-A.
               02 T PIC XX OCCURS 3 TIMES.
               02 TAIL-X PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY TAIL-X.
                STOP RUN.
            """);

    /// <summary>NEXT SENTENCE transfers to the implicit CONTINUE after the current sentence's period. BOTH cases
    /// write it in the THEN phrase, so the rule under test is §14.9.19.4 GR4 twice: the first executes it (the
    /// statement that trails the IF in the SAME sentence is skipped and the FOLLOWING sentence runs — the IF is
    /// closed by END-IF, because NEXT SENTENCE followed by a statement inside one THEN phrase matches neither
    /// §14.9.19.2 format and is refused, COBOLNET2269, kb/Work PB446; END-IF over a Format-2 body is the
    /// DESIGN-frontend-grammar §3.11 determination), and in the second the condition is FALSE, so GR5 ignores the THEN phrase and the ELSE arm
    /// runs. The IF branches are both exercised; the NEXT SENTENCE PLACEMENT is one arm, and the other —
    /// NEXT SENTENCE in an ELSE phrase, §14.9.19.4 GR6 — is measured by the spec-derived golden
    /// tests/conformance/85/pb414_next_sentence_last_sentence_inline.cob (kb/Work PB414).</summary>
    [Fact]
    public void NextSentence_SkipsTrailOfOwnSentence()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC 9 VALUE 1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF W = 1 NEXT SENTENCE END-IF
                    DISPLAY "TRUE-TAIL".
                DISPLAY "SENTENCE-2".
                MOVE 2 TO W.
                IF W = 1 NEXT SENTENCE ELSE DISPLAY "ELSE-ARM".
                DISPLAY "SENTENCE-4".
                STOP RUN.
            """);
}
