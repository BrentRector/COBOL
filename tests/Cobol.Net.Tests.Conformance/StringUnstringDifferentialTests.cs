// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// STRING (ISO §14.9.43) + UNSTRING (ISO §14.9.48): the semantically subtle general rules — delimiter runs and
/// SIZE defaulting, the never-space-filled receiver and pointer mechanics, ON/NOT ON OVERFLOW, the OR'd / ALL
/// delimiter scans, DELIMITER IN / COUNT IN / TALLYING, and the two GR15 overflow situations vs plain source
/// exhaustion. Differential against the legacy oracle (NIST NC217A/NC218A-green) everywhere the legacy is sound;
/// the one legacy gap this family closes (the STRING pointer &lt; 1 check, §14.9.43.4 GR8) is spec-pinned.
/// Space-padded fields are displayed ONE PER LINE so the legacy's known DISPLAY trailing-space trim cannot
/// surface as an internal-spaces diff (see <see cref="CutRunner.Normalize"/>).
/// </summary>
public sealed class StringUnstringDifferentialTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    /// <summary>For the case the LEGACY engine gets wrong (no pointer &lt; 1 check): assert the SPEC-derived
    /// output directly, with the governing § on the fact.</summary>
    private static void AssertSpecPinned(string source, string expected)
    {
        var (ok, output, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(expected, output);
    }

    private static string Program(string programId, string ws, string procedure) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{programId}}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {{ws}}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {{procedure}}
        """;

    // ── STRING ─────────────────────────────────────────────────────────────────────────────────────────────

    // §14.9.43.2 format + SR9: a DELIMITED phrase governs the whole run of senders before it; the trailing
    // phraseless run (legal only immediately before INTO) is DELIMITED BY SIZE. GR3b: the delimiter cuts each
    // governed sender and is itself never transferred. Expected: "AB" + "EF" + "IJK".
    [Fact]
    public void String_DelimitedPhraseGovernsItsRun_TrailingRunIsSize()
        => AssertSameAsLegacy(Program("STRRUN", """
            01 WS-A PIC X(5) VALUE "AB*CD".
            01 WS-B PIC X(5) VALUE "EF*GH".
            01 WS-C PIC X(3) VALUE "IJK".
            01 WS-R PIC X(15) VALUE SPACES.
            """, """
                STRING WS-A WS-B DELIMITED BY "*" WS-C INTO WS-R.
                DISPLAY WS-R.
                STOP RUN.
            """));

    // §14.9.43.4 GR3b: a delimiter that is never encountered — here one LONGER than the sender — moves the whole
    // sender (the NC217A "ABCDEFG" case).
    [Fact]
    public void String_DelimiterLongerThanSender_MovesWholeSender()
        => AssertSameAsLegacy(Program("STRLNG", """
            01 WS-R PIC X(10) VALUE SPACES.
            """, """
                STRING "ABCDEF" DELIMITED BY "ABCDEFG" INTO WS-R.
                DISPLAY WS-R.
                STOP RUN.
            """));

    // §14.9.43.4 GR4/GR6/GR7: the transfer starts at the POINTER value, the pointer advances exactly one per
    // character moved (final value = start + characters written), and every receiver position outside the
    // written window keeps its PRIOR content — no space filling. Expected: "12AB5678", pointer 05.
    [Fact]
    public void String_WithPointer_WritesWindow_PreservesRest_AdvancesPointer()
        => AssertSameAsLegacy(Program("STRPTR", """
            01 WS-R PIC X(8) VALUE "12345678".
            01 WS-P PIC 99 VALUE 3.
            """, """
                STRING "AB" DELIMITED BY SIZE INTO WS-R WITH POINTER WS-P.
                DISPLAY WS-R.
                DISPLAY WS-P.
                STOP RUN.
            """));

    // §14.9.43.4 GR8/GR9: a sender exceeding the receiver overflows at the first character past the end — the
    // transferred prefix stays stored, ON OVERFLOW runs, NOT ON OVERFLOW is ignored (GR8e); a fitting statement
    // runs NOT ON OVERFLOW, and the receiver again keeps its untouched tail (GR7): "ABCD" then "XYCD".
    [Fact]
    public void String_Overflow_RunsOnOverflow_FitRunsNotOnOverflow()
        => AssertSameAsLegacy(Program("STROVF", """
            01 WS-R PIC X(4) VALUE SPACES.
            """, """
                STRING "ABCDEF" DELIMITED BY SIZE INTO WS-R
                    ON OVERFLOW DISPLAY "OVF"
                    NOT ON OVERFLOW DISPLAY "FIT"
                END-STRING.
                DISPLAY WS-R.
                STRING "XY" DELIMITED BY SIZE INTO WS-R
                    ON OVERFLOW DISPLAY "OVF2"
                    NOT ON OVERFLOW DISPLAY "FIT2"
                END-STRING.
                DISPLAY WS-R.
                STOP RUN.
            """));

    // §14.9.43.4 GR8 — SPEC-PINNED (the legacy engine never checked the < 1 arm): with POINTER 0, the check
    // BEFORE the first character move finds the pointer less than one, so NOTHING transfers (GR8a), ON OVERFLOW
    // runs (GR8c), the receiver is fully preserved (GR7), and the pointer — changed only by character moves
    // (GR6), of which there were none — writes back unchanged.
    [Fact]
    public void String_PointerBelowOne_OverflowsWithoutTransfer_SpecPinned()
        => AssertSpecPinned(Program("STRPL1", """
            01 WS-R PIC X(6) VALUE "ABCDEF".
            01 WS-P PIC 9(4) VALUE 0.
            """, """
                STRING "XY" DELIMITED BY SIZE INTO WS-R WITH POINTER WS-P
                    ON OVERFLOW DISPLAY "OVERFLOW"
                    NOT ON OVERFLOW DISPLAY "OK"
                END-STRING.
                DISPLAY WS-R.
                DISPLAY WS-P.
                STOP RUN.
            """),
            "OVERFLOW\nABCDEF\n0000");

    // ── UNSTRING ───────────────────────────────────────────────────────────────────────────────────────────

    // §14.9.48.4 GR8: two contiguous delimiters give the current receiver an EMPTY examination — space-filled
    // when alphanumeric, ZERO-filled when numeric. Expected: spaces, "000", "23".
    [Fact]
    public void Unstring_ContiguousDelimiters_SpaceFillAlphanumeric_ZeroFillNumeric()
        => AssertSameAsLegacy(Program("UNSFIL", """
            01 WS-S PIC X(5) VALUE "1,,23".
            01 WS-A PIC XX VALUE "XX".
            01 WS-N PIC 999 VALUE 999.
            01 WS-B PIC XXX VALUE "YYY".
            """, """
                UNSTRING WS-S DELIMITED BY "," INTO WS-A WS-N WS-B.
                DISPLAY WS-A.
                DISPLAY WS-N.
                DISPLAY WS-B.
                STOP RUN.
            """));

    // §14.9.48.4 GR7: ALL collapses one-or-more contiguous occurrences into a single delimiting occurrence;
    // GR11e/GR4: COUNT IN is the examined characters EXCLUDING delimiter characters. Expected fields X / Y / Z9,
    // counts 1 1 2.
    [Fact]
    public void Unstring_AllPhrase_CollapsesContiguousRuns_CountExcludesDelimiters()
        => AssertSameAsLegacy(Program("UNSALL", """
            01 WS-S PIC X(9) VALUE "X00Y000Z9".
            01 WS-A PIC XX.
            01 WS-B PIC XX.
            01 WS-C PIC XX.
            01 WS-K1 PIC 9.
            01 WS-K2 PIC 9.
            01 WS-K3 PIC 9.
            """, """
                UNSTRING WS-S DELIMITED BY ALL "0"
                    INTO WS-A COUNT IN WS-K1
                         WS-B COUNT IN WS-K2
                         WS-C COUNT IN WS-K3.
                DISPLAY WS-A.
                DISPLAY WS-B.
                DISPLAY WS-C.
                DISPLAY WS-K1 WS-K2 WS-K3.
                STOP RUN.
            """));

    // §14.9.48.4 GR10: OR'd delimiters — the EARLIEST match in the sender wins regardless of listing order, and a
    // same-position tie goes to the delimiter listed FIRST ("AB" before "A" matches "AB"; reversed, "A" wins and
    // the next examination resumes inside what "AB" would have consumed). GR11d: DELIMITER IN receives the one
    // matched occurrence per the MOVE rules.
    [Fact]
    public void Unstring_OrDelimiters_EarliestWins_TieGoesToFirstListed()
        => AssertSameAsLegacy(Program("UNSOR", """
            01 WS-S PIC X(5) VALUE "ZABQW".
            01 WS-S2 PIC X(5) VALUE "Z*Q,W".
            01 WS-R1 PIC XX.
            01 WS-R2 PIC XXX.
            01 WS-D1 PIC XX.
            """, """
                UNSTRING WS-S DELIMITED BY "AB" OR "A"
                    INTO WS-R1 DELIMITER IN WS-D1 WS-R2.
                DISPLAY WS-R1.
                DISPLAY WS-D1.
                DISPLAY WS-R2.
                UNSTRING WS-S DELIMITED BY "A" OR "AB"
                    INTO WS-R1 DELIMITER IN WS-D1 WS-R2.
                DISPLAY WS-R1.
                DISPLAY WS-D1.
                DISPLAY WS-R2.
                UNSTRING WS-S2 DELIMITED BY "," OR "*"
                    INTO WS-R1 DELIMITER IN WS-D1 WS-R2.
                DISPLAY WS-R1.
                DISPLAY WS-D1.
                DISPLAY WS-R2.
                STOP RUN.
            """));

    // §14.9.48.4 GR11d/GR11e/GR14: DELIMITER IN takes each matched delimiter, COUNT IN the examined character
    // count, and TALLYING ADDS the number of receivers acted upon to the item's STARTING value (5 + 3 = 08).
    [Fact]
    public void Unstring_DelimiterIn_CountIn_TallyingAddsToCurrentValue()
        => AssertSameAsLegacy(Program("UNSDCT", """
            01 WS-S PIC X(9) VALUE "AB,CDE*FG".
            01 WS-R1 PIC XXX.
            01 WS-R2 PIC XXX.
            01 WS-R3 PIC XXX.
            01 WS-D1 PIC X.
            01 WS-D2 PIC X.
            01 WS-C1 PIC 9.
            01 WS-C2 PIC 9.
            01 WS-T PIC 99 VALUE 5.
            """, """
                UNSTRING WS-S DELIMITED BY "," OR "*"
                    INTO WS-R1 DELIMITER IN WS-D1 COUNT IN WS-C1
                         WS-R2 DELIMITER IN WS-D2 COUNT IN WS-C2
                         WS-R3
                    TALLYING IN WS-T.
                DISPLAY WS-R1.
                DISPLAY WS-D1 WS-C1.
                DISPLAY WS-R2.
                DISPLAY WS-D2 WS-C2.
                DISPLAY WS-R3.
                DISPLAY WS-T.
                STOP RUN.
            """));

    // §14.9.48.4 GR11b (no DELIMITED ⇒ examine exactly receiver-size characters from the POINTER position),
    // GR13 (pointer advances per character examined — final 08), and GR15b/GR16c: every receiver acted upon with
    // sender characters still unexamined IS the overflow condition — ON OVERFLOW runs.
    [Fact]
    public void Unstring_NoDelimited_SizeBounded_UnexaminedRemainder_Overflows()
        => AssertSameAsLegacy(Program("UNSSZ", """
            01 WS-S PIC X(8) VALUE "ABCDEFGH".
            01 WS-P PIC 99 VALUE 3.
            01 WS-R1 PIC XX.
            01 WS-R2 PIC XXX.
            """, """
                UNSTRING WS-S INTO WS-R1 WS-R2 WITH POINTER WS-P
                    ON OVERFLOW DISPLAY "OVF"
                    NOT ON OVERFLOW DISPLAY "NOVF"
                END-UNSTRING.
                DISPLAY WS-R1.
                DISPLAY WS-R2.
                DISPLAY WS-P.
                STOP RUN.
            """));

    // §14.9.48.4 GR15/GR11g: exhausting the SENDER before the receivers is NOT an overflow — the remaining
    // receivers are not acted upon (left untouched, tally not bumped) and NOT ON OVERFLOW runs (GR17): "NOVF",
    // WS-R3 keeps "Z", tally 2.
    [Fact]
    public void Unstring_SourceExhausted_IsNotOverflow_RemainingReceiversUntouched()
        => AssertSameAsLegacy(Program("UNSEXH", """
            01 WS-S PIC X(3) VALUE "A,B".
            01 WS-R1 PIC X.
            01 WS-R2 PIC X.
            01 WS-R3 PIC X VALUE "Z".
            01 WS-T PIC 9 VALUE 0.
            """, """
                UNSTRING WS-S DELIMITED BY "," INTO WS-R1 WS-R2 WS-R3
                    TALLYING IN WS-T
                    ON OVERFLOW DISPLAY "OVF"
                    NOT ON OVERFLOW DISPLAY "NOVF"
                END-UNSTRING.
                DISPLAY WS-R1 WS-R2.
                DISPLAY WS-R3.
                DISPLAY WS-T.
                STOP RUN.
            """));
}
