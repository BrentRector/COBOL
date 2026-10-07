// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// INSPECT (ISO §14.9.22): the GR8 shared comparison cycle (one ordered operand list per statement, across ALL
/// counters), the per-operand GR9 BEFORE/AFTER regions (with the not-found asymmetry), GR4d signed-numeric
/// de-sign/re-sign, GR12/GR17 tally/replace adjectives, GR19 format-3 ordering, GR20/GR23 CONVERTING, and the
/// 2023-only BACKWARD gate. Differential facts pin to the legacy oracle (NIST NC115A/NC122A/NC216A/NC221A-green);
/// spec-pinned facts cover the places the legacy deviates from the spec (SR6 figurative replacement expansion)
/// and the post-85 surface the 85-dialect oracle cannot host (BACKWARD).
/// </summary>
public sealed class InspectDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    /// <summary>A SPEC-derived expectation (cited at the call site) — used where the legacy oracle is non-conforming
    /// or cannot host the dialect; <paramref name="dialectLevel"/> selects the targeted edition.</summary>
    private static void AssertSpecPinned(string source, string expected, int dialectLevel = 85)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(dialectLevel).CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(expected, stdout);
    }

    private static string Program(string data, string procedure) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. INSP.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {{data}}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {{procedure}}
            STOP RUN.
        """;

    // §14.9.22.4 GR8a/b + GR12b: ALL the statement's operands form ONE ordered comparison cycle — the earlier
    // ALL "A" consumes the leading 'A', so LEADING "AH" never matches at its first eligible point and tallies 0.
    [Fact]
    public void Tally_SharedCycle_EarlierAllStarvesLeading()
        => AssertMatchesGolden(Program("""
            01 WS-X PIC X(6) VALUE "AHAHXX".
            01 C1 PIC 99 VALUE 0.
            01 C2 PIC 99 VALUE 0.
            """, """
                INSPECT WS-X TALLYING C1 FOR ALL "A" C2 FOR LEADING "AH".
                DISPLAY C1 " " C2.
            """));

    // §14.9.22.4 GR4d: a signed numeric identifier-1 is inspected as though moved to an UNSIGNED item — the
    // operational sign is removed, so "-" tallies 0 and the magnitude digit "5" tallies 1 in -12345.
    [Fact]
    public void Tally_SignedNumericTarget_InspectsDeSignedDigits()
        => AssertMatchesGolden(Program("""
            01 WS-S PIC S9(5) VALUE -12345.
            01 C1 PIC 9 VALUE 0.
            01 C2 PIC 9 VALUE 0.
            """, """
                INSPECT WS-S TALLYING C1 FOR ALL "-" C2 FOR ALL "5".
                DISPLAY C1 " " C2.
            """));

    // §14.9.22.4 GR4d (second sentence): the replacing cycle runs over the de-signed digits and the ORIGINAL sign
    // is retained on completion — -12345 with ALL "1" BY "9" becomes -92345 (displayed with its overpunch sign).
    [Fact]
    public void Replace_SignedNumericTarget_SignRetained()
        => AssertMatchesGolden(Program("""
            01 WS-S PIC S9(5) VALUE -12345.
            """, """
                INSPECT WS-S REPLACING ALL "1" BY "9".
                DISPLAY WS-S.
            """));

    // §14.9.22.4 GR17d: FIRST replaces only the leftmost matched occurrence, and the rule applies to EACH
    // successive FIRST phrase independently — two FIRST "X" phrases replace the first two X's, differently.
    [Fact]
    public void Replace_First_EachSuccessivePhraseReplacesOneOccurrence()
        => AssertMatchesGolden(Program("""
            01 WS-X PIC X(8) VALUE "XAXBXCXD".
            """, """
                INSPECT WS-X REPLACING FIRST "X" BY "1" FIRST "X" BY "2".
                DISPLAY WS-X.
            """));

    // §14.9.22.4 GR9b/c: per-operand regions fixed before the first cycle. AFTER "Q" BEFORE "Z" bounds CHARACTERS
    // to the two characters between them; an AFTER delimiter NOT found makes the operand never eligible (count 0),
    // while a BEFORE delimiter not found behaves as if BEFORE were absent (whole item) — the GR9 asymmetry.
    [Fact]
    public void Tally_Regions_BeforeAfterBounds_AndNotFoundAsymmetry()
        => AssertMatchesGolden(Program("""
            01 WS-X PIC X(8) VALUE "AAQBBZCC".
            01 C1 PIC 99 VALUE 0.
            01 C2 PIC 99 VALUE 0.
            01 C3 PIC 99 VALUE 0.
            """, """
                INSPECT WS-X TALLYING C1 FOR CHARACTERS AFTER "Q" BEFORE "Z".
                INSPECT WS-X TALLYING C2 FOR CHARACTERS AFTER "M".
                INSPECT WS-X TALLYING C3 FOR CHARACTERS BEFORE "M".
                DISPLAY C1 " " C2 " " C3.
            """));

    // §14.9.22.4 GR20/GR23: CONVERTING maps each character of literal-4 positionally to literal-5; a character
    // duplicated in literal-4 ("ABA") maps by its FIRST occurrence — 'A' goes to 'X', never to 'Z'.
    [Fact]
    public void Convert_DuplicateFromCharacter_FirstOccurrenceWins()
        => AssertMatchesGolden(Program("""
            01 WS-X PIC X(6) VALUE "ABCABC".
            """, """
                INSPECT WS-X CONVERTING "ABA" TO "XYZ".
                DISPLAY WS-X.
            """));

    // §14.9.22.3 SR6 / §14.9.22.4 GR14: a FIGURATIVE literal-3 is expanded to the size of literal-1 — ALL "AB" BY
    // SPACES replaces each "AB" with two spaces. SPEC-PINNED: the legacy binds the figurative as one character and
    // silently skips the unequal-size operand, leaving the field unchanged (a known legacy non-conformance).
    [Fact]
    public void Replace_FigurativeReplacement_ExpandsToPatternSize_SpecPinned()
        => AssertSpecPinned(Program("""
            01 WS-X PIC X(6) VALUE "ABCABD".
            """, """
                INSPECT WS-X REPLACING ALL "AB" BY SPACES.
                DISPLAY "[" WS-X "]".
            """), "[  C  D]");

    // §14.9.22.4 GR19: a format 3 executes as TWO successive statements — the tallying pass over the ORIGINAL
    // content (counts the B's), then the replacing pass (rewrites them) — never one merged pass.
    [Fact]
    public void Format3_TallyingRunsBeforeReplacing()
        => AssertMatchesGolden(Program("""
            01 WS-X PIC X(6) VALUE "AAABBB".
            01 C1 PIC 9 VALUE 0.
            """, """
                INSPECT WS-X TALLYING C1 FOR ALL "B" REPLACING ALL "B" BY "A".
                DISPLAY C1 " " WS-X.
            """));

    // §14.9.22.4 GR3 NOTE 1 (the spec's own example): INSPECT BACKWARD "A12C21D12EF" TALLYING CHARACTERS BEFORE
    // "12" returns 2, not 5 — the BEFORE boundary is established in the (reversed) scan direction. BACKWARD is
    // 2023-only (E.3.3 item 34), so this compiles at --std 2023.
    [Fact]
    public void Backward_CharactersBefore_SpecNote1Example()
        => AssertSpecPinned(Program("""
            01 WS-X PIC X(11) VALUE "A12C21D12EF".
            01 C1 PIC 9 VALUE 0.
            """, """
                INSPECT BACKWARD WS-X TALLYING C1 FOR CHARACTERS BEFORE "12".
                DISPLAY C1.
            """), "2", dialectLevel: 2023);

    // BACKWARD was introduced by ISO/IEC 1989:2023 (§14.9.22.2; VERSION_CHANGE_REFERENCE row 77): compiling it at
    // --std 85 must be REJECTED with the edition-gate diagnostic, not silently accepted.
    [Fact]
    public void Backward_RejectedBelow2023()
    {
        var (ok, _, detail) = new CobolNetCompiler(85).CompileAndRun(Program("""
            01 WS-X PIC X(5) VALUE "ABCDE".
            01 C1 PIC 9 VALUE 0.
            """, """
                INSPECT BACKWARD WS-X TALLYING C1 FOR CHARACTERS.
                DISPLAY C1.
            """));
        Assert.False(ok, "INSPECT BACKWARD must be rejected at --std 85");
        Assert.Contains("COBOLNET0845", detail);
    }
}
