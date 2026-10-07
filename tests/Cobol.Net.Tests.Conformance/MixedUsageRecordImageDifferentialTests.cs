// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The mixed-usage record-image codec (COBOLNET_DESIGN §14.4/§8.2, the Phase-1E Tier-C lift): a fixed-point
/// BINARY/PACKED leaf's image inside a record/group image is its TRUE BYTES — radix-2 two's complement
/// (big-endian) or BCD with a <c>0xC</c>/<c>0xD</c>/<c>0xF</c> sign nibble, of exactly
/// <c>PicInfo.StorageWidth</c> (V59). This is implementor-defined territory: ISO/IEC 1989:2023 §13.18.60.4 GR4 —
/// "Each implementor specifies the precise effect of the USAGE BINARY clause upon the … representation of the data
/// item …, including the representation of any algebraic sign" — and GR11 the same for PACKED-DECIMAL; group MOVE
/// (§14.9.25.4 GR4) / group compare (§8.8.4.2.1) are statements about that representation. The facts here are
/// therefore <b>spec-pinned</b>, never legacy-pinned (the NIST ST goldens stay the regression net — they observe
/// COMP values only through numeric moves, never raw bytes). What the spec DOES fix regardless of representation is pinned throughout: SORT/MERGE keys compare by
/// ALGEBRAIC value (§14.9.40 GR8 / §8.8.4.2.4 — "regardless of the manner in which their usage is described"),
/// DUPLICATES IN ORDER stability (GR3b), the RELEASE/RETURN record-area round trip (§14.9.32 GR2 / §14.9.34 GR3),
/// group-move positional fill without conversion (§14.9.25.4 GR4), and SAME RECORD AREA as an implicit leftmost-
/// aligned redefinition (§12.4.6.4.4 GR2). Exercised end-to-end by NIST ST108A/ST127A/ST133A/ST134A.
/// </summary>
public sealed class MixedUsageRecordImageDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new();

    /// <summary>Spec-pinned assertion (memory feedback_use_the_spec): the expected output is derived by hand from
    /// the cited general rules + the documented digit-image representation, not from an oracle run.</summary>
    private static void AssertSpec(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    /// <summary>A file-sort program over one SD with INPUT/OUTPUT PROCEDUREs (whole-SECTION ranges, §14.9.40
    /// GR10/GR13). The output procedure RETURNs until AT END, running <paramref name="perRecord"/> for each.</summary>
    private static string SortProgram(string id, string sdRecord, string ws, string sortClauses, string releases,
        string perRecord) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT SF ASSIGN TO "{id}-WORK".
        DATA DIVISION.
        FILE SECTION.
        SD SF.
        {sdRecord}
        WORKING-STORAGE SECTION.
        01 WS-EOF PIC X VALUE "N".
        {ws}
        PROCEDURE DIVISION.
        MAIN-SEC SECTION.
        MAIN-1.
            SORT SF {sortClauses}
                INPUT PROCEDURE IS LOAD-SEC
                OUTPUT PROCEDURE IS DUMP-SEC.
            STOP RUN.
        LOAD-SEC SECTION.
        LOAD-1.
        {releases}
        DUMP-SEC SECTION.
        DUMP-1.
            RETURN SF AT END MOVE "Y" TO WS-EOF
                NOT AT END
        {perRecord}
            END-RETURN.
            IF WS-EOF = "N" GO TO DUMP-1.
        """;

    private static string Program(string id, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    // ── SORT keys over signed BINARY/PACKED leaves (ISO §14.9.40 GR8 / §8.8.4.2.4 — algebraic order) ─────────

    /// <summary>An ASCENDING signed-COMP key orders NEGATIVES algebraically (−8000 &lt; −14 &lt; 0 &lt; +1 &lt; +99)
    /// — §14.9.40 GR8 via §8.8.4.2.4: numeric comparison is "with respect to the algebraic value of the operands
    /// regardless of the manner in which their usage is described". The load-bearing piece is the decode of the key
    /// WINDOW through the key item's own profile (<c>CobolSort.Key.Profile</c>): reading two's-complement bytes as
    /// text would sort every negative LAST, since a negative's first byte is 0xFF (hazard 1 — silent if untested).</summary>
    [Fact]
    public void Sort_SignedBinaryKey_Ascending_NegativesOrderAlgebraically()
        => AssertSpec(SortProgram("MXSRT1", """
            01 S-REC.
               05 S-KEY PIC S9(4) COMP.
               05 S-TXT PIC X(4).
            """,
            "01 WS-ED PIC +9(4).",
            "ON ASCENDING KEY S-KEY",
            """
                MOVE +99 TO S-KEY MOVE "R1  " TO S-TXT RELEASE S-REC.
                MOVE -14 TO S-KEY MOVE "R2  " TO S-TXT RELEASE S-REC.
                MOVE 0 TO S-KEY MOVE "R3  " TO S-TXT RELEASE S-REC.
                MOVE -8000 TO S-KEY MOVE "R4  " TO S-TXT RELEASE S-REC.
                MOVE +1 TO S-KEY MOVE "R5  " TO S-TXT RELEASE S-REC.
            """,
            "        MOVE S-KEY TO WS-ED DISPLAY \"K=\" WS-ED \" \" S-TXT"),
            "K=-8000 R4\nK=-0014 R2\nK=+0000 R3\nK=+0001 R5\nK=+0099 R1");

    /// <summary>The DESCENDING direction over the same signed-COMP key (GR8b — higher algebraic value first; the
    /// ST133A −199-first shape).</summary>
    [Fact]
    public void Sort_SignedBinaryKey_Descending_HighestFirst()
        => AssertSpec(SortProgram("MXSRT2", """
            01 S-REC.
               05 S-KEY PIC S9(4) COMP.
               05 S-TXT PIC X(4).
            """,
            "01 WS-ED PIC +9(4).",
            "ON DESCENDING KEY S-KEY",
            """
                MOVE -14 TO S-KEY MOVE "R1  " TO S-TXT RELEASE S-REC.
                MOVE +99 TO S-KEY MOVE "R2  " TO S-TXT RELEASE S-REC.
                MOVE -8000 TO S-KEY MOVE "R3  " TO S-TXT RELEASE S-REC.
            """,
            "        MOVE S-KEY TO WS-ED DISPLAY \"K=\" WS-ED \" \" S-TXT"),
            "K=+0099 R2\nK=-0014 R1\nK=-8000 R3");

    /// <summary>DUPLICATES IN ORDER over equal signed-COMP keys returns the duplicates in RELEASE order (§14.9.40
    /// GR3b — the sequence phase must be stable; the ST127A duplicate-key-stability shape).</summary>
    [Fact]
    public void Sort_SignedBinaryKey_DuplicatesInOrder_PreservesReleaseOrder()
        => AssertSpec(SortProgram("MXSRT3", """
            01 S-REC.
               05 S-KEY PIC S9(4) COMP.
               05 S-TXT PIC X(4).
            """,
            "01 WS-ED PIC +9(4).",
            "ON ASCENDING KEY S-KEY WITH DUPLICATES IN ORDER",
            """
                MOVE -5 TO S-KEY MOVE "A1  " TO S-TXT RELEASE S-REC.
                MOVE +7 TO S-KEY MOVE "B1  " TO S-TXT RELEASE S-REC.
                MOVE -5 TO S-KEY MOVE "A2  " TO S-TXT RELEASE S-REC.
                MOVE -5 TO S-KEY MOVE "A3  " TO S-TXT RELEASE S-REC.
            """,
            "        MOVE S-KEY TO WS-ED DISPLAY \"K=\" WS-ED \" \" S-TXT"),
            "K=-0005 A1\nK=-0005 A2\nK=-0005 A3\nK=+0007 B1");

    /// <summary>A signed PACKED-DECIMAL (COMP-3) key orders algebraically too — one codec covers both usages
    /// (NumericByteForm: BINARY images as radix-2 bytes, PACKED as BCD with a trailing sign nibble).</summary>
    [Fact]
    public void Sort_SignedPackedKey_Ascending_NegativesOrderAlgebraically()
        => AssertSpec(SortProgram("MXSRT4", """
            01 S-REC.
               05 P-KEY PIC S9(3) COMP-3.
               05 P-TXT PIC X(2).
            """,
            "01 WS-ED PIC +9(3).",
            "ON ASCENDING KEY P-KEY",
            """
                MOVE -5 TO P-KEY MOVE "N1" TO P-TXT RELEASE S-REC.
                MOVE +3 TO P-KEY MOVE "P1" TO P-TXT RELEASE S-REC.
                MOVE -300 TO P-KEY MOVE "N2" TO P-TXT RELEASE S-REC.
            """,
            "        MOVE P-KEY TO WS-ED DISPLAY \"K=\" WS-ED \" \" P-TXT"),
            "K=-300 N2\nK=-005 N1\nK=+003 P1");

    // ── RELEASE/RETURN round trip (ISO §14.9.32 GR2 / §14.9.34 GR3 — the record area carries the image) ──────

    /// <summary>RELEASE then RETURN round-trips signed COMP values EXACTLY — ±0 and ±max at 2/6/8 digits (the
    /// overpunch encode/decode tables are mutual inverses; the brief's hazard 7-iv verification). The checks
    /// compare algebraically (numeric relation, §8.8.4.2.4), so they hold under ANY correct representation.</summary>
    [Fact]
    public void ReleaseReturn_SignedCompLeaves_RoundTripZeroAndMax()
        => AssertSpec(SortProgram("MXSRT5", """
            01 R-REC.
               05 R-SEQ PIC 9.
               05 R-K2 PIC S99 COMP.
               05 R-K6 PIC S9(6) COMP.
               05 R-K8 PIC S9(8) COMP.
            """,
            "",
            "ON ASCENDING KEY R-SEQ",
            """
                MOVE 1 TO R-SEQ MOVE 0 TO R-K2 MOVE -999999 TO R-K6
                MOVE 99999999 TO R-K8 RELEASE R-REC.
                MOVE 2 TO R-SEQ MOVE -99 TO R-K2 MOVE 999999 TO R-K6
                MOVE -99999999 TO R-K8 RELEASE R-REC.
            """,
            """
                    IF R-SEQ = 1
                        IF R-K2 = 0 AND R-K6 = -999999 AND R-K8 = 99999999
                            DISPLAY "ROW1 OK" ELSE DISPLAY "ROW1 BAD" END-IF
                    ELSE
                        IF R-K2 = -99 AND R-K6 = 999999 AND R-K8 = -99999999
                            DISPLAY "ROW2 OK" ELSE DISPLAY "ROW2 BAD" END-IF
                    END-IF
            """),
            "ROW1 OK\nROW2 OK");

    // ── Whole-group MOVE / compare over the digit-image representation (§14.9.25.4 GR4 / §8.8.4.2.1) ─────────

    /// <summary>A NON-ALIGNED mixed-group MOVE (3 source leaves → 4 receiver leaves — the ST127A 10→11 shape) is a
    /// positional representation copy "without consideration for the individual elementary or group items" (§14.9.25.4 GR4):
    /// the source's image is <c>"AB" + FF 85 + "CDE"</c> — a <c>PIC S9(4) COMP</c> holding −123 is TWO bytes of
    /// two's complement, 0x10000 − 123 = 0xFF85 (§13.18.60.4 GR4, radix 2; V59) — and it refills the receiver's
    /// windows as AB | FF | 85 | CDE. Each one-byte <c>PIC S9(2) COMP</c> receiver then reads its byte as signed:
    /// 0xFF ⇒ −1, 0x85 ⇒ −123. (−123 exceeds a 2-digit picture, which is exactly what GR4 permits: the copy is of
    /// REPRESENTATION, and the resulting content need not be consistent with the receiver's description —
    /// §14.6.13.2 leaves the subsequent numeric use undefined, so this pins OUR deterministic decode.)</summary>
    [Fact]
    public void Move_NonAlignedMixedGroups_FillsPositionallyByImage()
        => AssertSpec(Program("MXMOV1", """
            01 G-SRC.
               05 SRC-A PIC X(2) VALUE "AB".
               05 SRC-N PIC S9(4) COMP VALUE -123.
               05 SRC-B PIC X(3) VALUE "CDE".
            01 G-DST.
               05 DST-A PIC X(2).
               05 DST-N1 PIC S9(2) COMP.
               05 DST-N2 PIC S9(2) COMP.
               05 DST-B PIC X(3).
            """, """
                MOVE G-SRC TO G-DST.
                IF DST-A = "AB" AND DST-N1 = -1 AND DST-N2 = -123 AND DST-B = "CDE"
                    DISPLAY "OK"
                ELSE
                    DISPLAY "BAD"
                END-IF.
            """),
            "OK");

    /// <summary>A SIGNED NEGATIVE binary leaf's group image is its TWO'S-COMPLEMENT BYTES, big-endian, and FIXED
    /// width: −7 in <c>PIC S9(4) COMP</c> is 0x10000 − 7 = <c>FF F9</c> — TWO bytes (§13.18.60.4 GR4 radix 2, the
    /// pinned 1-2-4-8 ladder), so the group is 4 and the following leaf does not shift. Two failure modes this
    /// pins at once: the retired inline concat formatted the leaf with its own BinaryMinus profile
    /// (<c>"-0007"</c>, VARIABLE width — a negative value shifted every following leaf), and the zoned digit image
    /// that replaced it made the same group 6 characters wide while <c>FUNCTION BYTE-LENGTH</c> said 4 (V59). The
    /// The WIDTH is asserted two ways that a shift would break: <c>FUNCTION LENGTH</c> of the group is 4, and an
    /// alphanumeric REDEFINES finds "AB" at positions 3-4 — under the retired zoned image the leaf occupied four
    /// positions and "AB" sat at 5-6. (This harness compiles at COBOL-85, so the byte VALUES themselves are pinned
    /// where the intrinsics exist: the 2023 golden <c>v59_byte_image</c> reads each byte with FUNCTION ORD, and
    /// <c>RecordImageCodecTests</c> pins the hex vectors at the codec.)</summary>
    [Fact]
    public void GroupImage_SignedNegativeBinaryLeaf_IsTwosComplementBytes()
        => AssertSpec(Program("MXCMP1", """
            01 G1.
               05 G1-N PIC S9(4) COMP VALUE -7.
               05 G1-X PIC X(2) VALUE "AB".
            01 G1R REDEFINES G1.
               05 G1R-NUM PIC X(2).
               05 G1R-TXT PIC X(2).
            01 L1 PIC 9(2).
            """, """
                MOVE FUNCTION LENGTH(G1) TO L1.
                DISPLAY L1.
                IF G1R-TXT = "AB" DISPLAY "AT2" ELSE DISPLAY "SHIFTED" END-IF.
            """),
            "04\nAT2");

    /// <summary>A fixed-OCCURS COMP child participates in the group image (ISO §14.9 — every OCCURS position is
    /// part of the whole group; the retired MixedGroupImage bailed on OCCURS entirely): a group-to-group move
    /// round-trips every element's value through the concatenated per-occurrence images.</summary>
    [Fact]
    public void Move_MixedGroupWithOccursCompChild_RoundTripsElements()
        => AssertSpec(Program("MXOCC1", """
            01 GO-A.
               05 GOA-X PIC X VALUE "A".
               05 GOA-T PIC S9(3) COMP OCCURS 3.
            01 GO-B.
               05 GOB-X PIC X.
               05 GOB-T PIC S9(3) COMP OCCURS 3.
            """, """
                MOVE -1 TO GOA-T (1).
                MOVE 52 TO GOA-T (2).
                MOVE -999 TO GOA-T (3).
                MOVE GO-A TO GO-B.
                IF GOB-X = "A" AND GOB-T (1) = -1 AND GOB-T (2) = 52
                        AND GOB-T (3) = -999
                    DISPLAY "OK"
                ELSE
                    DISPLAY "BAD"
                END-IF.
            """),
            "OK");

    // ── The mixed FD record codec (WRITE sends AsImage, READ distributes via FromImage — §8.2) ───────────────

    /// <summary>WRITE of a mixed FD record then READ back restores every leaf's VALUE — negative, zero, positive
    /// (the ST133A tape-copy shape): the record codec is the generated AsImage/FromImage pair, and sequential
    /// retrieval is write order (§14.9.30).</summary>
    [Fact]
    public void MixedFdRecord_WriteThenReadBack_RoundTripsValues()
        => AssertSpec($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MXFIO1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "MXFIO1-D".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC.
               05 F-X PIC X(3).
               05 F-K PIC S9(8) COMP.
               05 F-T PIC X(2).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC X VALUE "N".
            01 WS-ED PIC +9(8).
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT F.
                MOVE "AAA" TO F-X MOVE -199 TO F-K MOVE "T1" TO F-T WRITE F-REC.
                MOVE "BBB" TO F-X MOVE 0 TO F-K MOVE "T2" TO F-T WRITE F-REC.
                MOVE "CCC" TO F-X MOVE 99 TO F-K MOVE "T3" TO F-T WRITE F-REC.
                CLOSE F.
                OPEN INPUT F.
                PERFORM UNTIL WS-EOF = "Y"
                    READ F AT END MOVE "Y" TO WS-EOF
                        NOT AT END
                        MOVE F-K TO WS-ED
                        DISPLAY "R=" F-X " " WS-ED " " F-T
                    END-READ
                END-PERFORM.
                CLOSE F.
                STOP RUN.
            """,
            "R=AAA -00000199 T1\nR=BBB +00000000 T2\nR=CCC +00000099 T3");

    // ── SAME RECORD AREA over a COMP leaf (ISO §12.4.6.4.4 GR2 — the ST134A Tier-B leg) ──────────────────────

    /// <summary>SAME RECORD AREA makes the named files' record areas ONE storage area — "equivalent to an implicit
    /// redefinition of the area, with records aligned on the leftmost byte position" (§12.4.6.4.4 GR2). With a COMP
    /// leaf the class is Tier B under the digit-image representation: a store through one record's leaf is visible
    /// through the other record's same-offset leaf, character image and algebraic value alike.</summary>
    [Fact]
    public void SameRecordArea_WithCompLeaf_RecordsShareOneArea()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MXSRA1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F-A ASSIGN TO "MXSRA1-A".
                SELECT F-B ASSIGN TO "MXSRA1-B".
            I-O-CONTROL.
                SAME RECORD AREA FOR F-A F-B.
            DATA DIVISION.
            FILE SECTION.
            FD F-A.
            01 A-REC.
               05 A-X PIC X(2).
               05 A-K PIC S9(4) COMP.
            FD F-B.
            01 B-REC.
               05 B-X PIC X(2).
               05 B-K PIC S9(4) COMP.
            PROCEDURE DIVISION.
            MAIN.
                MOVE "ZZ" TO A-X.
                MOVE -42 TO A-K.
                IF B-X = "ZZ" AND B-K = -42
                    DISPLAY "SHARED"
                ELSE
                    DISPLAY "BAD"
                END-IF.
                STOP RUN.
            """,
            "SHARED");
}
