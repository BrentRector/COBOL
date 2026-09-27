// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// NATIONAL (ISO §8.5.2.10, PIC N / USAGE NATIONAL) and BOOLEAN (§8.5.2.5, PIC 1 / USAGE BIT) declaration and
/// clause facts — Phase 4a M2-DATA-3/4. The declaration band: COBOLNET0881 for the illegal USAGE×PICTURE shapes
/// (§13.18.60.4 SR5 :22722 / SR12 :22744 / SR20 :22764 and the picture-less elementary forms), COBOLNET0898 for
/// VALUE category mismatches (§13.18.63 SR5 :23232 / SR10 :23254) and boolean THROUGH (SR29 :23327),
/// COBOLNET0899 for the NAMED staged legs (national-edited pictures, national-form numerics/boolean, FD national
/// records) and the loud byte-addressed guards (D-N2: REDEFINES / EXTERNAL cells refuse a 2-byte national leaf).
/// The positive half locks JUSTIFIED (§13.18.32 SR3/GR1-2 :19264–19273), level-88 boolean conditions, national
/// relation ordering (§8.8.4.2.9/10 :9692–9715), and the group character image (a national/boolean leaf is
/// string-stored, so groups containing them MOVE/DISPLAY in char space).
/// </summary>
public sealed class NationalBooleanDataTests
{
    private static string Prog(string pid, string ws, string proc = "") => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
            {proc}
            STOP RUN.
        """;

    // ── COBOLNET0881 — the illegal USAGE × PICTURE declaration shapes (ISO §13.18.60.4) ────────────────────

    public static TheoryData<string, string, string> IllegalUsageShapes() => new()
    {
        // SR20 (:22764): PIC N ⇒ only NATIONAL may be specified — an explicit non-NATIONAL usage is illegal.
        { "NBDAT01", "01 WS-A PIC N(4) USAGE DISPLAY.", "PIC N with explicit USAGE DISPLAY (SR20)" },
        // SR5 (:22722): USAGE BIT ⇒ the PICTURE shall be a boolean picture.
        { "NBDAT02", "01 WS-B PIC X(4) USAGE BIT.", "USAGE BIT with an alphanumeric PICTURE (SR5)" },
        // A picture-less ELEMENTARY USAGE NATIONAL/BIT entry has no legal shape (§13.18.40 GR9/GR8 — the
        // category needs its picture; unlike BINARY-CHAR there is no picture-less factory).
        { "NBDAT03", "01 WS-C USAGE NATIONAL.", "picture-less elementary USAGE NATIONAL" },
        { "NBDAT04", "01 WS-D USAGE BIT.", "picture-less elementary USAGE BIT" },
        // SR12 (:22744) + §13.18.40.3 SR30 (:20395): USAGE NATIONAL never combines with an alphabetic/
        // alphanumeric PICTURE.
        { "NBDAT05", "01 WS-E PIC A(4) USAGE NATIONAL.", "USAGE NATIONAL with an alphabetic PICTURE (SR12/SR30)" },
    };

    [Theory]
    [MemberData(nameof(IllegalUsageShapes))]
    public void IllegalUsagePictureShape_Rejects0881(string pid, string wsEntry, string shape)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, wsEntry), 2002);
        Assert.False(ok, $"{shape} must be rejected (ISO §13.18.60.4)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0881");
    }

    // ── COBOLNET0898 — VALUE clause category mismatches (ISO §13.18.63 SR5/SR10, both directions) ──────────

    public static TheoryData<string, string, string> IllegalValueShapes() => new()
    {
        // SR5 (:23232): a national item's VALUE literal shall be a national literal (or a legal figurative).
        { "NBDAT06", "01 WS-F PIC N(4) VALUE \"AB\".", "alphanumeric VALUE literal on a national item (SR5)" },
        // ...and an N"…" literal is only for national receivers.
        { "NBDAT07", "01 WS-G PIC X(4) VALUE N\"AB\".", "national VALUE literal on an alphanumeric item (SR5)" },
        // SR10 (:23254): a boolean item's VALUE literal shall be a boolean literal.
        { "NBDAT08", "01 WS-H PIC 1(2) VALUE \"01\".", "quoted-alphanumeric VALUE on a boolean item (SR10)" },
        { "NBDAT09", "01 WS-I PIC X(4) VALUE B\"01\".", "boolean VALUE literal on an alphanumeric item (SR10)" },
        // SR10: length shall not exceed the item's size.
        { "NBDAT10", "01 WS-J PIC 1(2) VALUE B\"0101\".", "boolean VALUE longer than the item (SR10)" },
        // SR29 (:23327): THROUGH phrases shall not be specified for boolean values (level-88).
        { "NBDAT11", "01 WS-K PIC 1(2).\n   88 WS-K-RANGE VALUE B\"00\" THRU B\"11\".",
          "boolean level-88 VALUE THRU (SR29)" },
    };

    [Theory]
    [MemberData(nameof(IllegalValueShapes))]
    public void IllegalValueShape_Rejects0898(string pid, string wsEntry, string shape)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, wsEntry), 2002);
        Assert.False(ok, $"{shape} must be rejected (ISO §13.18.63)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0898");
    }

    // ── §13.18.60.3 SR12 — ALL FIVE PICTURE SHAPES COMPILE at every national-bearing edition ─────────

    /// <summary>⛔ THIS SET USED TO PIN A REFUSAL, and that is why it is written this way now (kb/Work PB646;
    /// the same correction kb/Work PB492 made for national-edited and kb/Work PB327 made for the FD record
    /// below). SR12 admits FIVE picture shapes under usage national — "a boolean, national, national-edited,
    /// numeric, or numeric-edited data item" — and a GREEN test asserting COBOLNET0899 on three of them read as
    /// a DECISION that they were not supported ([[green_test_can_hold_a_gap_open]]). They are supported; the
    /// theory enumerates all five, so a shape cannot be dropped from the permission without this failing.</summary>
    public static TheoryData<string, string, string> Sr12Shapes() => new()
    {
        { "NBDAT13", "01 WS-M PIC N(3).", "national (§13.18.40.4 GR9)" },
        { "NBDAT14", "01 WS-N PIC NNBNN.", "national-edited (GR10)" },
        { "NBDAT16", "01 WS-P PIC 9(3) USAGE NATIONAL.", "national-form numeric" },
        { "NBDAT17", "01 WS-Q PIC ZZ9 USAGE NATIONAL.", "national-form numeric-edited" },
        { "NBDAT18", "01 WS-R PIC 1(4) USAGE NATIONAL.", "national-form boolean (GR8)" },
    };

    /// <summary>Every SR12 shape BINDS at 2002 and at 2023, and each is REJECTED at 85 by the introduction gate
    /// alone (COBOLNET0900 — the national-data-2002 / national-edited-2002 registry rows). The PAIR of
    /// assertions is what distinguishes "live and edition-gated" from "still refused, differently".</summary>
    [Theory]
    [MemberData(nameof(Sr12Shapes))]
    public void LiveNationalFormShape_CompilesAtNationalBearingEditions(string pid, string wsEntry, string shape)
    {
        foreach (int edition in new[] { 2002, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid + edition, wsEntry), edition);
            Assert.True(ok, $"{shape} is admitted by ISO §13.18.60.3 SR12 and shall compile at --std {edition}"
                + $" — got: {string.Join(" | ", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog(pid + "85", wsEntry), 85);
        Assert.False(ok85, $"{shape} is national data, a COBOL-2002 introduction, and shall be gated at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
    }

    /// <summary>An FD record with a national leaf COMPILES AND ROUND-TRIPS, and its disk image is the UTF-16BE
    /// pair serialization (kb/Work PB327 — this test USED to pin the refusal, the shape
    /// feedback_green_test_can_hold_a_gap_open warns about: a green test pinning a loud stage reads as a
    /// decision). §13.18.60.4 GR8 leaves a national character's storage size to the implementor and D-N1 pins
    /// TWO bytes, high-order first, so `05 F-N PIC N(4). 05 F-X PIC X(4).` is a TWELVE-byte record — the same
    /// geometry §14.9.30.4 GR14/GR15 count in bytes and §12.4.5.12.4 GR4 places keys in. The assertion reads the
    /// record back through the alphanumeric member and through a REDEFINES of the national one, so a
    /// one-byte-per-position layout (F-X would land at byte 4) is excluded by position, not by width.</summary>
    [Fact]
    public void FdRecord_WithNationalLeaf_RoundTripsAsUtf16BePairs()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBDAT15.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "NBDAT15.DAT".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC.
               05 F-N PIC N(4).
               05 F-X PIC X(4).
            WORKING-STORAGE SECTION.
            01 W-N PIC N(4).
            01 W-B PIC X(8).
            01 W-O PIC 9(3).
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT F.
                MOVE N"WXYZ" TO F-N.
                MOVE "pqrs" TO F-X.
                WRITE F-REC.
                CLOSE F.
                OPEN INPUT F.
                READ F AT END DISPLAY "EOF".
                DISPLAY "N=[" F-N "] X=[" F-X "]".
                MOVE F-N TO W-N.
                MOVE FUNCTION CONVERT(W-N ANY ANUM HEX) TO W-B.
                DISPLAY "HEX=" W-B.
                CLOSE F.
                STOP RUN.
            """;
        // 2023, not 2002: the observation uses FUNCTION CONVERT (§15.19, introduced by ISO/IEC 1989:2023 —
        // COBOLNET1502). The SUBJECT is edition-independent (national data is 2002), and the 2002 arm of the
        // record layout is covered by `conformance:2002/pb327_national_fd_record`.
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        // The record is 4 national positions (8 bytes) + 4 alphanumeric (4 bytes) = 12 bytes; F-X therefore
        // starts at byte 8, and reading "pqrs" back proves it. HEX is the UTF-16BE serialization of N"WXYZ":
        // U+0057 U+0058 U+0059 U+005A → 00570058 0059005A (§15.19.4 r4 hexes the source's own bits).
        Assert.Equal("N=[WXYZ] X=[pqrs]\nHEX=00570058", stdout);
    }

    /// <summary>REDEFINES over a national item OVERLAYS ITS BYTES, and the overlay is the UTF-16BE pair
    /// serialization (kb/Work PB231 — RESIDUE-11 discharged; these two tests USED to pin the refusal, which is
    /// exactly the shape feedback_green_test_can_hold_a_gap_open warns about: a green test pinning a loud stage
    /// reads as a decision).
    /// <para>The derivation: §13.18.44.4 GR1 associates the storage of the two entries over "an area sufficient
    /// to contain the number of bits required by the data item referenced by the subject of the entry", and
    /// §13.18.60.4 GR8 leaves a national character's size to the implementor — WiseOwl COBOL pins TWO bytes,
    /// UTF-16BE (D-N1). So <c>01 A PIC N(4).</c> is an EIGHT-byte area and <c>01 B REDEFINES A PIC X(8).</c>
    /// overlays it exactly. N"AB" is U+0041 U+0042, whose bytes are 00 41 00 42 — the high-order byte FIRST, so
    /// B's second character is "A" and its fourth is "B". Reading them positionally is the assertion: a
    /// little-endian pair or a one-byte-per-position layout would put "A" in position 1.</para></summary>
    [Fact]
    public void Redefines_OverNationalItem_OverlaysItsUtf16BeBytes()
    {
        string src = Prog("NBDAT16", """
            01 A PIC N(4).
            01 B REDEFINES A PIC X(8).
            """, """
            MOVE N"AB" TO A
            DISPLAY "B2=[" B(2:1) "] B4=[" B(4:1) "] LEN=" FUNCTION LENGTH(B).
            """);
        var (ok, output, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, $"REDEFINES over a national item is legal source — §13.18.44.3 restricts data-name-2 "
            + $"only by class object/pointer, strong typing and variable length (SR14/SR17), never by a "
            + $"national usage: {detail}");
        Assert.Contains("B2=[A] B4=[B] LEN=8", output, StringComparison.Ordinal);
    }

    /// <summary>An EXTERNAL group containing a national leaf is ORDINARY cell-backed storage (kb/Work PB231 —
    /// the same discharge as the REDEFINES twin above, through the same gate: §13.18.22 conditions EXTERNAL on
    /// nothing subordinate at all, and the run-unit cell is a byte area in which a national position takes its
    /// two §13.18.60.4 GR8 bytes). The alphanumeric leaf after it proves the DISPLACEMENT: GX starts at byte 8,
    /// not byte 4, so writing GN cannot disturb it.</summary>
    [Fact]
    public void ExternalGroup_WithNationalLeaf_IsCellBacked()
    {
        string src = Prog("NBDAT17", """
            01 G EXTERNAL.
               05 GN PIC N(4).
               05 GX PIC X(4).
            """, """
            MOVE N"WXYZ" TO GN
            MOVE "abcd" TO GX
            DISPLAY "GN=[" GN "] GX=[" GX "]".
            """);
        var (ok, output, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, $"an EXTERNAL record with a national leaf is legal source: {detail}");
        Assert.Contains("GN=[WXYZ] GX=[abcd]", output, StringComparison.Ordinal);
    }

    // ── COBOLNET0844 — boolean relation misuse (ISO §8.8.4.2) ───────────────────────────────────────────────

    /// <summary>Boolean operands compare with Format 2 EQUALITY only (§8.8.4.2 F2 :9566–9581); an ordering
    /// operator on a boolean operand is COBOLNET0844 (§8.8.4.2.2 — F1 SR2/SR3 exclude class boolean
    /// :9608–9610).</summary>
    [Fact]
    public void BooleanOrderingRelation_Rejects0844()
    {
        string src = Prog("NBDAT18", "01 BW PIC 1(2) VALUE B\"01\".", """
            IF BW < B"1" DISPLAY "LT=YES" END-IF.
            """);
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "an ordering relation on boolean operands must be rejected (ISO §8.8.4.2.2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
    }

    /// <summary>A boolean operand may compare only with another boolean operand (F2 — both operands of class
    /// boolean); a boolean-vs-alphanumeric relation mix is COBOLNET0844.</summary>
    [Fact]
    public void BooleanVsAlphanumericRelation_Rejects0844()
    {
        string src = Prog("NBDAT19", "01 BW PIC 1(2) VALUE B\"01\".\n01 AX PIC X(2) VALUE \"01\".", """
            IF BW = AX DISPLAY "EQ=YES" END-IF.
            """);
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "a boolean-vs-alphanumeric relation must be rejected (ISO §8.8.4.2 F2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
    }

    // ── Positive facts ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>ISO §13.18.32.4 GR2, the RECEIVER-LARGER direction, over EVERY usage the rule names its fill for
    /// (kb/Work PB737 added the third): "the data is aligned at the rightmost character position or boolean
    /// position in the data item with zero fill for the leftmost boolean positions and space fill for the
    /// leftmost character positions", then "For data items … described as usage national, national zeros …
    /// national spaces"; "For data items … described as usage bit, bit zeros shall be used for zero fill".
    /// JUSTIFIED is legal on both categories by §13.18.32.3 SR3. The "!" sentinel keeps the national right end
    /// observable. The DISPLAY-usage half of GR2 is pinned by `conformance:2023/pb339_into_current_record` leg A.
    /// <para>BB is the USAGE BIT arm — the sentence a PIC 1(4) DISPLAY-carrier receiver does NOT exercise, since
    /// GR2 names the fill per USAGE, not per category. Expected `0011` by the same derivation as BJ: a two-
    /// position sender right-aligned in four boolean positions, the leftmost two filled with bit zeros.</para>
    /// </summary>
    [Fact]
    public void JustifiedRight_NationalAndBoolean_LeftFill()
    {
        string src = Prog("NBDAT20", """
            01 NJ PIC N(4) JUSTIFIED RIGHT.
            01 BJ PIC 1(4) JUSTIFIED RIGHT.
            01 BB USAGE BIT PIC 1(4) JUSTIFIED RIGHT.
            """, """
            MOVE N"AB" TO NJ.
            DISPLAY "NJ=" NJ "!".
            MOVE B"11" TO BJ.
            DISPLAY "BJ=" BJ.
            MOVE B"11" TO BB.
            DISPLAY "BB=" BB.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("NJ=  AB!\nBJ=0011\nBB=0011", stdout);
    }

    /// <summary>kb/Work PB737 — ISO §13.18.32.4 GR1, the SENDER-LARGER direction, over the usages the rule names:
    /// "When the receiving data item is described with the JUSTIFIED clause and the sending operand is larger
    /// than the receiving data item, the leftmost character positions or boolean positions of the sending operand
    /// shall be truncated." The DISPLAY-alphanumeric half is pinned by
    /// `conformance:2023/pb339_into_current_record` leg B and by
    /// `AcceptDifferentialTests.Day_JustifiedReceiver_RightJustifiesAndLeftTruncates`; the rule's OWN wording
    /// names boolean positions as well, and a national receiver is legal under §13.18.32.3 SR3, so both were
    /// implemented-but-unevidenced until this test.
    /// <para>Derivation, stated before the program was run. Five sending positions into three receiving
    /// positions truncates the LEFTMOST two in every case: N"ABCDE" → NJ = national CDE; B"11001" → BJ = 001;
    /// the same into a USAGE BIT receiver (BB) = 001, because GR1 truncates boolean POSITIONS irrespective of
    /// the carrier; "ABCDE" into an alphabetic PIC A(3) JUSTIFIED (AJ) = CDE. Truncation on the RIGHT — the
    /// non-JUSTIFIED §14.6.8.5/§14.6.8.6 rule — would give ABC/110/110/ABC, so each leg discriminates.</para>
    /// </summary>
    [Fact]
    public void JustifiedRight_SenderLarger_TruncatesLeftmostAcrossUsages()
    {
        string src = Prog("NBDAT24", """
            01 NJ PIC N(3) JUSTIFIED RIGHT.
            01 BJ PIC 1(3) JUSTIFIED RIGHT.
            01 BB USAGE BIT PIC 1(3) JUSTIFIED RIGHT.
            01 AJ PIC A(3) JUSTIFIED RIGHT.
            """, """
            MOVE N"ABCDE" TO NJ.
            DISPLAY "NJ=" NJ "!".
            MOVE B"11001" TO BJ.
            DISPLAY "BJ=" BJ.
            MOVE B"11001" TO BB.
            DISPLAY "BB=" BB.
            MOVE "ABCDE" TO AJ.
            DISPLAY "AJ=" AJ "!".
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("NJ=CDE!\nBJ=001\nBB=001\nAJ=CDE!", stdout);
    }

    /// <summary>A level-88 condition with a boolean-literal VALUE (ISO §13.18.63 SR10 + §8.8.4.5): the
    /// condition holds exactly when the parent equals the boolean value — the boolean equality relation
    /// (§8.8.4.2.8, by VALUE regardless of usage).</summary>
    [Fact]
    public void Level88_BooleanValue_ConditionTrueAndFalse()
    {
        string src = Prog("NBDAT21", """
            01 FLAG PIC 1 VALUE B"0".
               88 IS-ON VALUE B"1".
            """, """
            IF IS-ON DISPLAY "ON=YES" ELSE DISPLAY "ON=NO" END-IF.
            MOVE B"1" TO FLAG.
            IF IS-ON DISPLAY "ON=YES" ELSE DISPLAY "ON=NO" END-IF.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("ON=NO\nON=YES", stdout);
    }

    /// <summary>National relation ordering and the space-extension rule (ISO §8.8.4.2.9/10 :9692–9715): the
    /// shorter operand right-extends with national spaces (so N(4) "AB␠␠" = N"AB"), and ordering follows the
    /// national program collating sequence (D-N3 — the UTF-16 ordinal default; the ALPHANUMERIC program
    /// collating sequence never applies).</summary>
    [Fact]
    public void NationalComparison_OrderingAndSpaceExtension()
    {
        string src = Prog("NBDAT22", "01 NA PIC N(4) VALUE N\"AB\".", """
            IF NA = N"AB" DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO" END-IF.
            IF NA < N"AC" DISPLAY "LT=YES" ELSE DISPLAY "LT=NO" END-IF.
            IF N"AC" > NA DISPLAY "GT=YES" ELSE DISPLAY "GT=NO" END-IF.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("EQ=YES\nLT=YES\nGT=YES", stdout);
    }

    /// <summary>An ALPHANUMERIC group containing a national leaf carries that leaf's STORAGE BYTES — two per
    /// character position, high-order first (§13.18.60.4 GR8; D-N1) — so its image is 2n wide, not n
    /// (kb/Work PB327; this test used to assert one character per position, and that assertion is what kept
    /// `FUNCTION LENGTH(G)` and the group's own image disagreeing: measured before the fix,
    /// `01 G. 05 A PIC X(2). 05 N PIC N(3).` answered LENGTH 8 while `MOVE G TO R` moved 5 characters).
    /// <para>The standard states the consequence itself, at §13.18.29.4 GR2's NOTE: "Without the GROUP-USAGE
    /// NATIONAL clause, the content of such a group item would be treated as category alphanumeric, possibly
    /// leading to corruption or invalid handling of data." The remedy it names — GROUP-USAGE NATIONAL — is the
    /// AsNat() carrier face, exercised by `pb79_group_usage_national`. A DISPLAY-form boolean leaf is one
    /// character per boolean position either way (§13.18.60.4 GR7 / D-B1), which is why GB is unchanged.</para>
    /// <para>Observed through CONVERT … HEX rather than raw, because the UTF-16BE high bytes of Latin characters
    /// are NUL and no golden in this corpus carries one (a NUL in expected output would poison the
    /// scripted-write corruption check).</para></summary>
    [Fact]
    public void GroupWithNationalLeaf_ImagesItsUtf16BeBytes()
    {
        string src = Prog("NBDAT23", """
            01 G.
               05 GN PIC N(3).
               05 GB PIC 1(3).
               05 GX PIC X(2).
            01 H PIC X(11).
            01 L PIC 9(3).
            01 HX PIC X(22).
            """, """
            MOVE N"AB" TO GN.
            MOVE B"101" TO GB.
            MOVE "ZZ" TO GX.
            MOVE FUNCTION LENGTH(G) TO L.
            DISPLAY "L=" L.
            MOVE G TO H.
            MOVE FUNCTION CONVERT(H ANY ANUM HEX) TO HX.
            DISPLAY "H=" HX.
            """);
        // 2023 for FUNCTION CONVERT (§15.19; COBOLNET1502) — see the sibling test above.
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        // G is 3 national positions (6 bytes) + 3 DISPLAY boolean positions (3 bytes) + 2 alphanumeric = 11
        // bytes, and FUNCTION LENGTH — which has always counted a national position as its two bytes — agrees.
        // The image: N"AB" padded to three national positions is U+0041 U+0042 U+0020 → 00 41 00 42 00 20;
        // B"101" is the three characters '1','0','1' → 31 30 31; "ZZ" → 5A 5A.
        Assert.Equal("L=011\nH=0041004200203130315A5A", stdout);
    }
}
