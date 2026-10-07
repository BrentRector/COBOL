// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The MOVE figurative-constant edition matrix (roadmap Phase 2 W2 track A — VCR rows 1 / 92 / 128).
/// ISO/IEC 1989:2023 §14.9.25.3 SR5: a digit-only <c>ALL "literal"</c> (or ALL symbolic-character representing a
/// digit) may move to an INTEGER numeric item — the sole surviving figurative→numeric MOVE, itself obsolete at
/// 2023 (the SR5 NOTE; Annex F.2 item 2 → COBOLNET0903); "in all other cases" the move of an alphanumeric
/// figurative constant (SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal") to a numeric or numeric-edited item
/// is prohibited — NEWLY removed by 2023 (Annex E.2 item 1 bullet 1: permitted through ISO 2014 → COBOLNET0902,
/// error strict / warning permissive, silent below 2023). Value semantics: §8.3.3.6.4 GR2 (character-by-character
/// repetition to the associated size) + §14.9.25.4 GR6d3b (a figurative sending operand takes the RECEIVER's
/// digit count, fraction digits included). Pre-removal semantics of the prohibited shapes are a provisional
/// adjudication (ratified owner decision 1): the fill characters become the receiver's character
/// image, so IS NUMERIC is false and numeric reads decode deterministically (§14.6.13.2).
/// </summary>
public sealed class MoveEditionDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet85 = new();   // dialect 85 (default)

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

    /// <summary>Assert WiseOwl COBOL (at --std 85) produces <paramref name="expected"/>: the spec-derived value, or for a
    /// prohibited pre-removal shape the provisional pinned value (fill characters stored at the receiver's fixed width,
    /// NOT NUMERIC, deterministic zero decode), as each case's comment says.</summary>
    private static void AssertSpec(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet85.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    /// <summary>Compile-and-run at an explicit edition/severity axis (the 2023 --permissive pre-removal leg).</summary>
    private static (bool ok, string stdout, string detail) RunAt(string source, int edition, bool permissive = false)
    {
        string dir = CutRunner.NewTempDir("mved");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            src = CompiledProgramCache.StageSource(src, source);
            var result = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll,
                DialectLevel: edition, Permissive: permissive, SourceFormat: InitialReferenceFormat.Auto));
            if (!result.Success)
                return (false, "", $"[compile] {result.Status}: {string.Join("\n", result.Errors)}");
            return CutRunner.Run(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    // ═══ Item 1 — the ALL-digit value semantics (§8.3.3.6.4 GR2 + §14.9.25.4 GR6d3b) ══════════════════════════

    [Fact]
    // §14.9.25.4 GR6d3b: the figurative takes the receiver's digit count and is "replicated in this item, from
    // left to right" — ALL "5" → PIC 9(3) stores 555 (was the BoundAllLiteral runtime-loud latent bug).
    public void AllDigitToInteger_FillsEveryDigitPosition_Sr5_Gr6d3b()
        => AssertSpec(Program("MVEDA1", "01 W-INT PIC 9(3).",
            "    MOVE ALL \"5\" TO W-INT.\n    DISPLAY \"R[\" W-INT \"]\"."), "R[555]");

    [Fact]
    // GR6d3b: "if the receiving item is not an integer, the number of digits includes both those to the right
    // and the left of the decimal point" — ALL "5" → PIC 9V9 is 5.5 (digit image "55"; legacy-confirmed).
    public void AllDigitToNonInteger_FillsFractionDigits_Gr6d3b()
        => AssertSpec(Program("MVEDA2", "01 W-FRAC PIC 9V9.",
            "    MOVE ALL \"5\" TO W-FRAC.\n    DISPLAY \"R[\" W-FRAC \"]\"."), "R[55]");

    [Fact]
    // §8.3.3.6.4 GR2 repetition with right truncation — ALL "57" → PIC 9(3) stores 575. A multi-character ALL
    // associated with a numeric item is §8.3.3.6.3 SR3's, removed by ISO 2002 (the gate below); at 85 it is the
    // '85 OBSOLETE element and admitted with exactly this repeat-truncate value (kb/Work PB422's determination,
    // VCR Table 7 row 7.13 — a derived edge; the golden twin is 85/pb422_all_multichar_numeric_85).
    public void AllDigitMultiCharacter_RepeatTruncate_At85_Sr3ObsoleteElement()
        => AssertSpec(Program("MVEDA3", "01 W-INT PIC 9(3).",
            "    MOVE ALL \"57\" TO W-INT.\n    DISPLAY \"R[\" W-INT \"]\"."), "R[575]");

    [Fact]
    // A digit-only ALL to an integer receiver is VALID at 2023 (SR5's exception) — the 0903 obsolete flag is a
    // warning, never a failure, and the value semantics are identical.
    public void AllDigitToInteger_StillRunsAt2023_Sr5Exception()
    {
        var (ok, stdout, detail) = RunAt(Program("MVEDA4", "01 W-INT PIC 9(4).",
            "    MOVE ALL \"7\" TO W-INT.\n    DISPLAY \"R[\" W-INT \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Equal("R[7777]", stdout);
    }

    // ═══ Item 4 — the pre-removal figurative→numeric semantics (provisional, ratified decision 1) ═════════════

    [Fact]
    // Annex E.2 item 1 bullet 1: MOVE SPACE TO a numeric item was permitted through 2014. Pre-removal semantics
    // (legacy-adjudicated, provisional): the receiver's character image is space-filled at its fixed width
    // (§8.3.3.6.4 GR2; §14.6.8 fixed-width alignment) and the item is then NOT NUMERIC (§8.8.4.4 — its content
    // is not digits). Pinned (no legacy cross-check): the legacy renders the space-filled numeric as EMPTY, an
    // artifact inconsistent with its own numeric-edited receiver ("   ") and QUOTE fill (three quotes).
    public void SpaceToNumeric_ImageFill_NotNumeric_At85_E2Item1()
        => AssertSpec(Program("MVEDB1", "01 W-INT PIC 9(3).", """
                MOVE SPACE TO W-INT.
                DISPLAY "R[" W-INT "]".
                IF W-INT IS NUMERIC
                    DISPLAY "C[NUMERIC]"
                ELSE
                    DISPLAY "C[NOT-NUMERIC]".
            """), "R[   ]\nC[NOT-NUMERIC]");

    [Fact]
    // QUOTE fill (§8.3.3.6.4 GR8: one or more quotation marks) — the legacy oracle's RUNTIME confirms three
    // quotes but its front end rejects the compile (CBL0906, stricter than ISO 2014), so the value is pinned.
    public void QuoteToNumeric_ImageFill_At85_E2Item1()
        => AssertSpec(Program("MVEDB2", "01 W-INT PIC 9(3).",
            "    MOVE QUOTE TO W-INT.\n    DISPLAY \"R[\" W-INT \"]\"."), "R[\"\"\"]");

    [Fact]
    // A space-filled numeric read in a numeric context decodes deterministically to 0 (§14.6.13.2 — incompatible
    // data; a non-digit contributes no digit), so ADD 1 yields 001. The legacy oracle agrees end-to-end here.
    public void SpaceFilledNumeric_DecodesZeroInArithmetic_1461312()
        => AssertSpec(Program("MVEDB3", "01 W-INT PIC 9(3).",
            "    MOVE SPACE TO W-INT.\n    ADD 1 TO W-INT.\n    DISPLAY \"R[\" W-INT \"]\"."), "R[001]");

    [Fact]
    // A NUMERIC-EDITED receiver is string-backed: the figurative fills its width (legacy-confirmed "   ").
    public void SpaceToNumericEdited_FillsWidth_At85_E2Item1()
        => AssertSpec(Program("MVEDB4", "01 W-ED PIC ZZ9.",
            "    MOVE SPACE TO W-ED.\n    DISPLAY \"R[\" W-ED \"]\"."), "R[   ]");

    [Fact]
    // ALL "literal" containing a non-digit → numeric: character repetition to the receiver width (GR2) — the
    // legacy oracle stores "XXX" (provisional pre-removal semantics; 0902-removed at 2023).
    public void NonDigitAllToNumeric_CharacterFill_At85()
        => AssertSpec(Program("MVEDB5", "01 W-INT PIC 9(3).",
            "    MOVE ALL \"X\" TO W-INT.\n    DISPLAY \"R[\" W-INT \"]\"."), "R[XXX]");

    [Fact]
    // The pre-removal semantics MUST also run at 2023 --permissive (VERSION_TEST_MATRIX_DESIGN §10 #1 — the
    // migration posture preserves pre-removal behavior under the 0902 warning).
    public void SpaceToNumeric_RunsAt2023Permissive_PreRemovalSemantics()
    {
        var (ok, stdout, detail) = RunAt(Program("MVEDB6", "01 W-INT PIC 9(3).", """
                MOVE SPACE TO W-INT.
                DISPLAY "R[" W-INT "]".
                IF W-INT IS NUMERIC
                    DISPLAY "C[NUMERIC]"
                ELSE
                    DISPLAY "C[NOT-NUMERIC]".
            """), 2023, permissive: true);
        Assert.True(ok, detail);
        Assert.Equal("R[   ]\nC[NOT-NUMERIC]", stdout);
    }

    // ═══ Item 2 — the 0903 obsolete flag ("move-all-digit-integer-obsolete-2023"; SR5 NOTE / F.2 item 2) ══════

    private static readonly string AllDigitIntegerSource = Program("MVEDC1", "01 W-INT PIC 9(3).",
        "    MOVE ALL \"5\" TO W-INT.\n    DISPLAY W-INT.");

    [Fact]
    // The SR5 NOTE / Annex F.2 item 2: obsolete AT 2023 — a COBOLNET0903 warning naming the construct.
    public void AllDigitToInteger_Obsolete0903_At2023()
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(AllDigitIntegerSource, 2023);
        Assert.True(ok);
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0903");
        EditionHarness.AssertHasDiagnostic(warnings, "digit-only ALL");
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    // Below 2023 the move is simply valid (no ISO edition before 2023 flags it) — silent on both channels.
    public void AllDigitToInteger_Silent_Pre2023(int edition)
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(AllDigitIntegerSource, edition);
        Assert.True(ok);
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0903");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0902");
    }

    // ═══ Item 3 — the 0902 removal gate ("move-alphanumeric-figurative-removed-2023"; SR5 / E.2 item 1) ═══════

    private static string SpaceToNumericSource(string id) => Program(id, "01 W-NUM PIC 9(3).",
        "    MOVE SPACE TO W-NUM.");

    [Fact]
    // §14.9.25.3 SR5 prohibits the move at 2023 — an error under the strict (default) axis.
    public void SpaceToNumeric_Error0902_At2023Strict_Sr5()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(SpaceToNumericSource("MVEDD1"), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0902");
        EditionHarness.AssertHasDiagnostic(errors, "MOVE SPACE TO W-NUM");
    }

    [Fact]
    // Permissive: the removal is a WARNING and the pre-removal semantics are preserved (§10 #1 posture).
    public void SpaceToNumeric_Warning0902_At2023Permissive()
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(SpaceToNumericSource("MVEDD2"), 2023, permissive: true);
        Assert.True(ok);
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0902");
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    // Permitted through 2014 (Annex E.2 item 1 bullet 1 — "not even flagged obsolete in 2014"): silent.
    public void SpaceToNumeric_Silent_Pre2023(int edition)
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(SpaceToNumericSource("MVEDD3"), edition);
        Assert.True(ok);
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0902");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0903");
    }

    [Theory]
    [InlineData("QUOTE")]
    [InlineData("HIGH-VALUE")]
    [InlineData("LOW-VALUE")]
    [InlineData("SPACES")]
    // Every SR5-listed alphanumeric figurative is gated — including a NUMERIC-EDITED receiver ("to either a
    // numeric item or a numeric-edited item is prohibited").
    public void AlphanumericFigurativeToNumericEdited_Error0902_At2023(string figurative)
    {
        var source = Program("MVEDD4", "01 W-ED PIC ZZ9.", $"    MOVE {figurative} TO W-ED.");
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0902");
        var (ok85, _, w85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85);
        EditionHarness.AssertNoDiagnostic(w85, "COBOLNET0902");
    }

    [Fact]
    // A digit-only ALL to a NON-INTEGER numeric receiver is outside SR5's exception ("to an integer numeric
    // item") — 0902 at 2023, permitted (with the digit-fill value) before.
    public void DigitAllToNonIntegerReceiver_Error0902_At2023_Sr5()
    {
        var source = Program("MVEDD5", "01 W-FRAC PIC 9V9.", "    MOVE ALL \"5\" TO W-FRAC.");
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0902");
        var (ok85, _, w85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85);
        EditionHarness.AssertNoDiagnostic(w85, "COBOLNET0902");
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    // §8.3.3.6.3 SR3: an ALL literal LONGER THAN ONE character shall not be associated with a numeric or
    // numeric-edited item. It is NOT an SR5 row (SR5's digit-only exception has no length qualifier), so the
    // diagnostic names SR3 and its own edge — removed 2002, not 2023 (kb/Work PB422; VCR Table 7 row 7.13).
    public void MultiCharacterDigitAll_Error0902_From2002_Sr3(int edition)
    {
        var source = Program("MVEDD6", "01 W-INT PIC 9(3).", "    MOVE ALL \"57\" TO W-INT.");
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0902");
        EditionHarness.AssertHasDiagnostic(errors, "§8.3.3.6.3 SR3");
        EditionHarness.AssertNoDiagnostic(errors, "§14.9.25.3 SR5");
    }

    [Theory]
    [InlineData("IF W-INT = ALL \"57\" DISPLAY \"EQ\" END-IF.")]
    [InlineData("IF ALL \"57\" NOT = W-INT DISPLAY \"NE\" END-IF.")]
    [InlineData("EVALUATE W-INT WHEN ALL \"57\" DISPLAY \"EQ\" END-EVALUATE.")]
    [InlineData("PERFORM UNTIL W-INT = ALL \"57\" MOVE 575 TO W-INT END-PERFORM.")]
    // §8.3.3.6.4 GR2's NOTE 1 — "compared with it" is an association too, so SR3 reaches every relation (the
    // one relation checkpoint, either operand order): accepted at 85, 0902 from 2002, a warning under
    // --permissive with the comparison still evaluated.
    public void MultiCharacterAll_ComparedWithNumeric_Sr3(string statement)
    {
        var source = Program("MVEDD8", "01 W-INT PIC 9(3) VALUE 575.", "    " + statement);
        var (ok85, e85, w85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85, string.Join(Environment.NewLine, e85));
        EditionHarness.AssertNoDiagnostic(w85, "COBOLNET0902");
        foreach (var edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
            Assert.False(ok);
            EditionHarness.AssertHasDiagnostic(errors, "§8.3.3.6.3 SR3");
        }
        var (okP, _, wP) = EditionHarness.CompileFull(source, 2023, permissive: true);
        Assert.True(okP);
        EditionHarness.AssertHasDiagnostic(wP, "§8.3.3.6.3 SR3");
    }

    [Theory]
    [InlineData("IF W-INT = ALL \"5\" DISPLAY \"EQ\" END-IF.")]
    [InlineData("IF W-X = ALL \"57\" DISPLAY \"EQ\" END-IF.")]
    [InlineData("IF W-INT(1:2) = ALL \"57\" DISPLAY \"EQ\" END-IF.")]
    [InlineData("IF W-GRP = ALL \"57\" DISPLAY \"EQ\" END-IF.")]
    // SR3's scope boundary: a ONE-character ALL literal, an alphanumeric item, a reference-modified view of a
    // numeric item (operated upon as alphanumeric, §8.4.3.3.4 GR2) and a group are none of them SR3's.
    public void Sr3ScopeBoundary_NotRefused_At2023(string statement)
    {
        var source = Program("MVEDD9",
            """
            01 W-INT PIC 9(3) VALUE 575.
            01 W-X PIC X(3).
            01 W-GRP.
               05 W-GN PIC 9(3).
            """, "    " + statement);
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, 2023);
        Assert.True(ok, string.Join(Environment.NewLine, errors));
        EditionHarness.AssertNoDiagnostic(warnings, "§8.3.3.6.3 SR3");
    }

    [Fact]
    // A digit-only ALL to a NUMERIC-EDITED receiver is outside the exception too (SR5: "integer numeric item").
    public void DigitAllToNumericEdited_Error0902_At2023_Sr5()
    {
        var source = Program("MVEDD7", "01 W-ED PIC ZZ9.", "    MOVE ALL \"5\" TO W-ED.");
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0902");
    }

    // ═══ Exemptions (§14.9.25.3 SR5 scope boundaries) ══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    // ZERO is the NUMERIC figurative (§8.3.3.6.4 GR4; Table 17) — SR5's prohibition names only the alphanumeric
    // figuratives, so MOVE ZERO TO a numeric item is clean at every edition.
    public void ZeroToNumeric_ExemptEverywhere_Gr4(int edition)
    {
        var source = Program("MVEDE1", "01 W-INT PIC 9(3).", "    MOVE ZERO TO W-INT.\n    DISPLAY W-INT.");
        var (ok, _, warnings) = EditionHarness.CompileFull(source, edition);
        Assert.True(ok);
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0902");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0903");
    }

    [Fact]
    // A GROUP receiver is a §14.9.25.4 GR4 character copy (no numeric conversion) — SR5 does not reach it, even
    // when the group's leaves are numeric: clean at 2023 strict.
    public void GroupReceiver_Exempt_At2023_Gr4()
    {
        var source = Program("MVEDE2", "01 W-GRP.\n   05 W-N PIC 9(3).", "    MOVE SPACE TO W-GRP.");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, 2023);
        Assert.True(ok, string.Join("\n", errors));
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0902");
    }

    // ═══ The constructs.json row sources (activation readiness — the matrix theories' shapes) ═════════════════

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    // Row "move-alphanumeric-figurative-removed-2023": its source must compile STRICT at every pre-removal
    // edition and error 0902 at 2023 (asserted above) — the flip-to-active precondition.
    public void MatrixRowSource_MoveFigurative_CompilesStrictPreRemoval(int edition)
    {
        var source = "IDENTIFICATION DIVISION.\nPROGRAM-ID. MVFIG.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n"
            + "01 W-NUM PIC 9(3).\nPROCEDURE DIVISION.\nMAIN.\n    MOVE SPACE TO W-NUM.\n    STOP RUN.\n";
        var (ok, errors) = EditionHarness.Compile(source, edition);
        Assert.True(ok, string.Join("\n", errors));
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    // Row "move-all-digit-integer-obsolete-2023": its source compiles strict at EVERY edition (0903 is a
    // warning), with the 0903 present exactly at ≥2023.
    public void MatrixRowSource_MoveAllDigit_CompilesEverywhere(int edition)
    {
        var source = "IDENTIFICATION DIVISION.\nPROGRAM-ID. MVALLD.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n"
            + "01 W-INT PIC 9(3).\nPROCEDURE DIVISION.\nMAIN.\n    MOVE ALL \"5\" TO W-INT.\n    DISPLAY W-INT.\n"
            + "    STOP RUN.\n";
        var (ok, _, warnings) = EditionHarness.CompileFull(source, edition);
        Assert.True(ok);
        if (edition >= 2023) EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0903");
        else EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0903");
    }

    // ── The W2 adversarial-review fixes (DEVLOG 595) ────────────────────────────────────────────────────────

    private static readonly string QuoteToNumeric =
        Program("MVQTN2", "01 W-NUM PIC 9(3).", "    MOVE QUOTE TO W-NUM.");

    /// <summary>QUOTE→numeric is the ONE figurative the change annex tracks separately: designated OBSOLETE
    /// by ISO 2014 (Annex E.2 item 21), removed 2023 — row move-quote-numeric-obsolete-2014 warns 0903 at
    /// 2014 and stays SILENT at 85/2002 (the review's correction to VCR row 1's blanket wording).</summary>
    [Theory]
    [InlineData(85, false)]
    [InlineData(2002, false)]
    [InlineData(2014, true)]
    public void QuoteToNumeric_Obsolete0903_ExactlyFrom2014_E2Item21(int edition, bool expectFlag)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(QuoteToNumeric, edition);
        Assert.True(ok, string.Join("\n", errors));
        if (expectFlag) EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0903");
        else EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0903");
    }

    /// <summary>The QUOTE row's removal edge: 0902 error strict at 2023, warning + pre-removal run permissive.</summary>
    [Fact]
    public void QuoteToNumeric_Removed0902_At2023()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(QuoteToNumeric, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0902");
    }

    /// <summary>§14.9.25.3 SR1 — "The class of identifier-1 or identifier-2 shall not be index" — is
    /// version-INVARIANT (§13.18.60 GR10 admits only SET/SEARCH/relation references), never an Annex-E
    /// removal row: COBOLNET0809 error at EVERY edition, on the receiver AND the sender arm, and it stays an
    /// ERROR under --permissive (the review caught the 0902 row mislabeling this "permitted through 2014").</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void MoveIndexOperand_Sr1Rejected_EveryEdition(int edition)
    {
        var source = Program("MVIXSR1", "01 IXI USAGE INDEX.\n01 W PIC 9(3).",
            "    MOVE SPACE TO IXI.\n    MOVE 5 TO IXI.\n    MOVE IXI TO W.");
        var (ok, errors) = EditionHarness.Compile(source, edition);
        Assert.False(ok);
        Assert.Equal(3, errors.Count(e => e.Contains("COBOLNET0809")));
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0902");   // never the edition row
    }

    [Fact]
    public void MoveIndexOperand_Sr1_StaysErrorUnderPermissive()
    {
        var source = Program("MVIXSR2", "01 IXI USAGE INDEX.", "    MOVE SPACE TO IXI.");
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023, permissive: true);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0809");
    }

    /// <summary>The ref-mod round-trip-loss fix: a MOVE into a reference-modified slice of a numeric-DISPLAY
    /// item deposits CHARACTERS (§8.4.3.3.4 — the unique result is elementary alphanumeric; §8.4.3.3.4 GR5 the
    /// slice is a subset of the item's character positions). Before the fix the spliced image round-tripped
    /// through the backing long and silently lost the spaces (printed [003]/NUM); the item is now image-backed
    /// at bind time for EVERY sender kind. Pinned (no legacy cross-check: the greenfield value matches the
    /// spec derivation; §14.6.13.2 gives the deterministic NOT-NUMERIC verdict).</summary>
    [Fact]
    public void RefModSlice_FigurativeAndLiteralSenders_PreserveCharacters()
        => AssertSpec(Program("MVRFM1", "01 N PIC 9(3) VALUE 123.\n01 M PIC 9(3) VALUE 456.", """
                MOVE SPACE TO N (1:2).
                DISPLAY "[" N "]".
                IF N IS NUMERIC DISPLAY "NUM" ELSE DISPLAY "NOTNUM".
                MOVE "AB" TO M (1:2).
                DISPLAY "[" M "]".
            """), "[  3]\nNOTNUM\n[AB6]\n");

    /// <summary>The ref-mod receiver is ALPHANUMERIC (§8.4.3.3.4), so a figurative MOVE into a slice is legal
    /// at EVERY edition — no 0902 even at 2023 strict (the SR5 exemption the gate honors).</summary>
    [Fact]
    public void RefModSlice_FigurativeMove_Legal_At2023Strict()
    {
        var source = Program("MVRFM2", "01 N PIC 9(3) VALUE 123.", "    MOVE SPACE TO N (1:2).");
        var (ok, errors) = EditionHarness.Compile(source, 2023);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>HIGH-VALUE / LOW-VALUE pre-removal fills (§8.3.3.6.4 GR6/GR7 — the highest/lowest ordinal
    /// character repeated to the receiver's width), pinned by COMPARISON (the fill bytes are unprintable):
    /// the stored image equals the same figurative, and IS NUMERIC is false. Pinned, no legacy cross-check —
    /// legacy CBL0906 compile-rejects these at every standard (documented non-conformance vs E.2 item 1).</summary>
    [Fact]
    public void HighLowValue_PreRemoval_ImageFill_At85()
        => AssertSpec(Program("MVHLV1", "01 N PIC 9(3) VALUE 123.", """
                MOVE HIGH-VALUE TO N.
                IF N = HIGH-VALUES DISPLAY "EQHV" ELSE DISPLAY "NEHV".
                IF N IS NUMERIC DISPLAY "NUM" ELSE DISPLAY "NOTNUM".
                MOVE LOW-VALUE TO N.
                IF N = LOW-VALUES DISPLAY "EQLV" ELSE DISPLAY "NELV".
            """), "EQHV\nNOTNUM\nEQLV\n");
}
