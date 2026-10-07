// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// MOVE Table 16 legality + alignment for the boolean/national rows and columns (ISO §14.9.25.3 Table 16
/// :28839–28852; Phase 4a M2-DATA-3/4). Every constructible Yes cell involving a boolean or national operand
/// runs end-to-end (alignment per §14.6.8.5 — national-space right fill — and §14.6.8.6 — boolean-zero right
/// fill); every constructible No cell asserts the COBOLNET0819 category-legality diagnostic. The SR7 figurative
/// rows (:28817 — only ZERO and a boolean-charactered ALL may send to a boolean receiver) and the zero-length
/// literal rule (GR2 :28895 — a zero-length literal behaves as SPACE) are pinned here too. National-edited
/// senders/receivers are NOT constructible this increment (the national-edited-2002 pending row) — their cells
/// activate with that row.
/// </summary>
public sealed class NationalBooleanMoveTests
{
    private static string Prog(string pid, string ws, string proc) => $"""
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

    // ── The Yes cells: boolean receiver (Table 16 columns Boolean = Yes for AN / National / Boolean senders;
    //    §8.3.3.6.3 — ZERO and a digit-only-0/1 ALL literal are the legal figurative senders per SR7) ────────

    /// <summary>Receiver category BOOLEAN, every legal sender: alphanumeric (Table 16 :28846 AN→Boolean = Yes),
    /// national (National→Boolean = Yes), boolean, MOVE ZERO (§8.3.3.6 GR4 — boolean zeros by context), and
    /// ALL of boolean characters (SR7's surviving figurative). Right fill is boolean ZEROS (§14.6.8.6
    /// :24304–24308), never spaces.</summary>
    [Fact]
    public void BooleanReceiver_LegalSenders_StoreWithZeroFill()
    {
        string src = Prog("NBMOV01", """
            01 AX PIC X(2) VALUE "01".
            01 NN PIC N(2) VALUE N"10".
            01 BB PIC 1(2) VALUE B"11".
            01 BR PIC 1(4).
            """, """
            MOVE AX TO BR.
            DISPLAY "AN=" BR.
            MOVE NN TO BR.
            DISPLAY "NAT=" BR.
            MOVE BB TO BR.
            DISPLAY "B=" BR.
            MOVE ZERO TO BR.
            DISPLAY "ZERO=" BR.
            MOVE ALL "01" TO BR.
            DISPLAY "ALL=" BR.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("AN=0100\nNAT=1000\nB=1100\nZERO=0000\nALL=0101", stdout);
    }

    /// <summary>Receiver category NATIONAL, every legal sender: alphabetic, alphanumeric, alphanumeric-edited,
    /// integer numeric (Table 16 :28849 Numeric-int→National = Yes), numeric-edited, boolean, national — plus
    /// the zero-length N"" literal (MOVE GR2 :28895 — behaves as SPACE). Conversion is the §14.9.25 GR6a
    /// alphanumeric→national correspondence (Latin-1 identity under D-N4); right fill is NATIONAL SPACES
    /// (§14.6.8.5 :24297–24301). The trailing "!" sentinel makes the space fill observable through the
    /// harness's per-line trailing-space trim.</summary>
    [Fact]
    public void NationalReceiver_LegalSenders_StoreWithNationalSpaceFill()
    {
        string src = Prog("NBMOV02", """
            01 AA PIC A(2) VALUE "AB".
            01 AX PIC X(2) VALUE "XY".
            01 AE PIC XBX VALUE "P Q".
            01 NUM PIC 9(3) VALUE 42.
            01 NED PIC ZZ9.
            01 BB PIC 1(3) VALUE B"101".
            01 NS PIC N(2) VALUE N"OK".
            01 NW PIC N(4).
            """, """
            MOVE AA TO NW.
            DISPLAY "A=" NW "!".
            MOVE AX TO NW.
            DISPLAY "X=" NW "!".
            MOVE AE TO NW.
            DISPLAY "E=" NW "!".
            MOVE NUM TO NW.
            DISPLAY "N=" NW "!".
            MOVE 42 TO NED.
            MOVE NED TO NW.
            DISPLAY "D=" NW "!".
            MOVE BB TO NW.
            DISPLAY "B=" NW "!".
            MOVE NS TO NW.
            DISPLAY "S=" NW "!".
            MOVE N"" TO NW.
            DISPLAY "Z=" NW "!".
            """);
        // 2014: the zero-length N"" sender is a COBOL-2014 literal format (kb/Work PB895, VCR row 7.30).
        var (ok, stdout, detail) = new CobolNetCompiler(2014).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("A=AB  !\nX=XY  !\nE=P Q !\nN=042 !\nD= 42 !\nB=101 !\nS=OK  !\nZ=    !", stdout);
    }

    /// <summary>Sender category BOOLEAN, the remaining legal receivers: alphanumeric (Table 16 :28846
    /// Boolean→AN = Yes — the '0'/'1' characters move as characters, space fill) and alphanumeric-edited
    /// (the editing mask applies). Boolean→Boolean and Boolean→National ride the receiver facts above.</summary>
    [Fact]
    public void BooleanSender_AlphanumericReceivers_MoveAsCharacters()
    {
        string src = Prog("NBMOV19", """
            01 BB PIC 1(4) VALUE B"1010".
            01 AX PIC X(6).
            01 AE PIC XBX.
            """, """
            MOVE BB TO AX.
            DISPLAY "AN=" AX "!".
            MOVE BB TO AE.
            DISPLAY "ED=" AE.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("AN=1010  !\nED=1 0", stdout);
    }

    /// <summary>Sender category NATIONAL to the numeric family: Table 16 :28847 col Numeric = Yes — the
    /// national digits convert as an unsigned integer (GR6d3), and a numeric-edited receiver edits the
    /// converted value. (National→AN/AN-edited/Alphabetic are the No cells below; DISPLAY-OF §15.26 is the
    /// sanctioned narrowing, itself residue.)</summary>
    [Fact]
    public void NationalSender_NumericReceivers_ConvertAsUnsignedInteger()
    {
        string src = Prog("NBMOV20", """
            01 ND PIC N(3) VALUE N"042".
            01 NU PIC 9(3).
            01 NE PIC ZZ9.
            """, """
            MOVE ND TO NU.
            DISPLAY "NUM=" NU.
            MOVE ND TO NE.
            DISPLAY "NED=" NE.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("NUM=042\nNED= 42", stdout);
    }

    // ── The No cells: each is COBOLNET0819 at bind (ISO §14.9.25.3 SR10 + Table 16 :28839–28852; the SR7
    //    figurative rows :28817). Version-invariant at ≥2002 — the operands themselves are 0900-gated below. ──

    public static TheoryData<string, string, string, string> IllegalCells() => new()
    {
        // sender → boolean receiver (Table 16 Boolean column = No)
        { "NBMOV03", "01 AA PIC A(4).\n01 BW PIC 1(4).", "MOVE AA TO BW.", "Alphabetic → Boolean" },
        { "NBMOV04", "01 AE PIC XBX.\n01 BW PIC 1(4).", "MOVE AE TO BW.", "AN-edited → Boolean (:28846)" },
        { "NBMOV05", "01 NU PIC 9(3).\n01 BW PIC 1(4).", "MOVE NU TO BW.", "Numeric → Boolean" },
        { "NBMOV06", "01 NE PIC ZZ9.\n01 BW PIC 1(4).", "MOVE NE TO BW.", "Numeric-edited → Boolean" },
        // boolean sender (Table 16 Boolean row = No)
        { "NBMOV07", "01 BW PIC 1(4).\n01 AA PIC A(4).", "MOVE BW TO AA.", "Boolean → Alphabetic" },
        { "NBMOV08", "01 BW PIC 1(4).\n01 NU PIC 9(3).", "MOVE BW TO NU.", "Boolean → Numeric" },
        { "NBMOV09", "01 BW PIC 1(4).\n01 NE PIC ZZ9.", "MOVE BW TO NE.", "Boolean → Numeric-edited" },
        // national sender (Table 16 National row :28847 = No; the ISO-re-baselined N2A leg)
        { "NBMOV10", "01 NW PIC N(4).\n01 AA PIC A(4).", "MOVE NW TO AA.", "National → Alphabetic" },
        { "NBMOV11", "01 NW PIC N(4).\n01 AX PIC X(4).", "MOVE NW TO AX.", "National → Alphanumeric (:28847)" },
        { "NBMOV12", "01 NW PIC N(4).\n01 AE PIC XBX.", "MOVE NW TO AE.", "National → AN-edited (:28847)" },
        // national receiver: a NON-INTEGER numeric sender is illegal (Table 16 — only integer numeric → national)
        { "NBMOV13", "01 NI PIC 9V9.\n01 NW PIC N(4).", "MOVE NI TO NW.", "Numeric non-integer → National" },
        // SR7 (:28817): a non-boolean figurative shall not send to a boolean receiver (no boolean
        // SPACE/QUOTE/HIGH-VALUE/LOW-VALUE exists, §8.3.3.6)
        { "NBMOV14", "01 BW PIC 1(4).", "MOVE SPACE TO BW.", "SPACE → Boolean (SR7)" },
        { "NBMOV15", "01 BW PIC 1(4).", "MOVE QUOTE TO BW.", "QUOTE → Boolean (SR7)" },
        { "NBMOV16", "01 BW PIC 1(4).", "MOVE HIGH-VALUE TO BW.", "HIGH-VALUE → Boolean (SR7)" },
        { "NBMOV17", "01 BW PIC 1(4).", "MOVE LOW-VALUE TO BW.", "LOW-VALUE → Boolean (SR7)" },
        { "NBMOV18", "01 BW PIC 1(4).", "MOVE ALL \"AB\" TO BW.", "ALL non-boolean-chars → Boolean (SR7)" },
    };

    [Theory]
    [MemberData(nameof(IllegalCells))]
    public void IllegalMoveCell_Rejects0819(string pid, string ws, string move, string cell)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, ws, move), 2002);
        Assert.False(ok, $"{cell} must be rejected (ISO §14.9.25.3 Table 16 / SR7)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0819");
    }
}
