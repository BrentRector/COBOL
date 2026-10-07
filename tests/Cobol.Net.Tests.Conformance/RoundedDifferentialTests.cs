// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The ROUNDED phrase on arithmetic statements (ISO/IEC 1989:2023 §14.7.4): a bare <c>ROUNDED</c> rounds
/// NEAREST-AWAY-FROM-ZERO (§14.7.4.3 r1 / §11.9.6 r2), <c>ROUNDED MODE IS x</c> selects one of the eight modes, and
/// no phrase truncates toward zero (r2). The MODE-variant cases are pinned to <b>hand-computed spec values</b> (the
/// rounding-mode definitions in §14.7.4.3 r3–r10), not to the committed golden — the legacy's NIST-85 corpus only ever
/// exercises bare ROUNDED, so it is a weak witness for the seven other modes (process rule: the spec is authority,
/// the oracle is a net). Bare ROUNDED (the COBOL-85 default) is cross-checked against the legacy.
/// </summary>
public sealed class RoundedDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new(dialectLevel: 2014);   // OPTIONS / ROUNDED MODE IS are ISO-2014+ features

    /// <summary>Compile+run with WiseOwl COBOL and assert its stdout equals the hand-computed spec value.</summary>
    private static void AssertOutput(string source, string expected)
    {
        var (ok, outp, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(expected, outp);
    }

    /// <summary>Assert WiseOwl COBOL produces byte-identical stdout to the committed golden.</summary>
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source, 2014);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RND.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    /// <summary><c>COMPUTE R ROUNDED MODE IS {mode} = {expr}.</c> DISPLAY R; R is an unsigned PIC 9(3) integer.</summary>
    private static string ComputeInto(string pic, string mode, string expr) =>
        Program($"01 R {pic}.", $"    COMPUTE R ROUNDED MODE IS {mode} = {expr}.\n    DISPLAY R.");

    // ── 25 / 10 = 2.5 — the half-way value that separates every mode (positive). ───────────────────────────────
    [Theory]
    [InlineData("NEAREST-EVEN", "002")]            // tie → even digit (2) — §14.7.4.3 r5
    [InlineData("NEAREST-AWAY-FROM-ZERO", "003")]  // tie → away from zero — r4
    [InlineData("NEAREST-TOWARD-ZERO", "002")]     // tie → toward zero — r6
    [InlineData("AWAY-FROM-ZERO", "003")]          // any dropped fraction → away — r3
    [InlineData("TOWARD-GREATER", "003")]          // ceiling — r8
    [InlineData("TOWARD-LESSER", "002")]           // floor — r9
    [InlineData("TRUNCATION", "002")]              // toward zero — r10
    public void ComputeRounded_Half_Positive(string mode, string expected)
        => AssertOutput(ComputeInto("PIC 9(3)", mode, "25 / 10"), expected);

    // 35 / 10 = 3.5 — the OTHER NEAREST-EVEN tie, rounding UP to the even digit 4 (distinguishes banker's rounding
    // from a fixed round-down). §14.7.4.3 r5.
    [Fact]
    public void ComputeRounded_NearestEven_3_5_RoundsUpToEven()
        => AssertOutput(ComputeInto("PIC 9(3)", "NEAREST-EVEN", "35 / 10"), "004");

    // ── -25 / 10 = -2.5 — the sign-sensitive modes (TOWARD-GREATER vs TOWARD-LESSER vs AWAY-FROM-ZERO differ only
    //    on a negative tie). Receiver is SIGN LEADING SEPARATE so the DISPLAY image reads "-00n" / "+00n". ───────
    [Theory]
    [InlineData("NEAREST-EVEN", "-002")]            // tie → even (−2)
    [InlineData("AWAY-FROM-ZERO", "-003")]          // away from zero (−3)
    [InlineData("TOWARD-GREATER", "-002")]          // toward +∞ (−2)
    [InlineData("TOWARD-LESSER", "-003")]           // toward −∞ (−3)
    [InlineData("NEAREST-TOWARD-ZERO", "-002")]     // tie → toward zero (−2)
    public void ComputeRounded_Half_Negative(string mode, string expected)
        => AssertOutput(ComputeInto("PIC S9(3) SIGN IS LEADING SEPARATE", mode, "-25 / 10"), expected);

    // Rounding at a fractional receiver scale (not just integer): 10 / 3 = 3.333… into PIC 9V9 (scale 1).
    [Fact]
    public void ComputeRounded_FractionalReceiverScale()
        => AssertOutput(ComputeInto("PIC 9V9", "NEAREST-AWAY-FROM-ZERO", "20 / 3"), "67");   // 6.666… → 6.7

    // ── No ROUNDED phrase → TRUNCATION (§14.7.4.3 r2) — the regression guard that the default is unchanged. ────
    [Fact]
    public void Compute_NoRounded_Truncates()
        => AssertOutput(Program("01 R PIC 9(3).", "    COMPUTE R = 29 / 10.\n    DISPLAY R."), "002");   // 2.9 → 2

    // ── ROUNDED on the other arithmetic verbs (the mode threads through every resultant, not just COMPUTE). ────
    [Fact]
    public void DivideGivingRounded_ModeIs()
        // 25 / 10 = 2.5, NEAREST-EVEN → 2.
        => AssertOutput(Program("01 R PIC 9(3).", "    DIVIDE 10 INTO 25 GIVING R ROUNDED MODE IS NEAREST-EVEN.\n    DISPLAY R."), "002");

    [Fact]
    public void AddToRounded_InPlace()
        // 1.5 + 0.25 = 1.75 into PIC 9V9 (scale 1), NEAREST-AWAY → 1.8.
        => AssertOutput(Program("01 R PIC 9V9 VALUE 1.5.", "    ADD 0.25 TO R ROUNDED.\n    DISPLAY R."), "18");

    [Fact]
    public void MultiplyByRounded_InPlace_NearestEven()
        // 2.5 × 1.5 = 3.75 into PIC 9V9 (scale 1), NEAREST-EVEN → 3.8 (8 is the even digit).
        => AssertOutput(Program("01 R PIC 9V9 VALUE 2.5.", "    MULTIPLY 1.5 BY R ROUNDED MODE IS NEAREST-EVEN.\n    DISPLAY R."), "38");

    // ── Bare ROUNDED (no MODE) = NEAREST-AWAY-FROM-ZERO, the COBOL-85 default — cross-checked against the legacy. ─
    [Fact]
    public void BareRounded_DivideGiving_MatchesLegacy()
        => AssertMatchesGolden(Program("01 R PIC 9(3).", "    DIVIDE 10 INTO 25 GIVING R ROUNDED.\n    DISPLAY R."));   // 2.5 → 3

    [Fact]
    public void BareRounded_Compute_MatchesLegacy()
        => AssertMatchesGolden(Program("01 R PIC 9V9.", "    COMPUTE R ROUNDED = 20 / 3.\n    DISPLAY R."));   // 6.666… → 6.7

    [Fact]
    public void NoRounded_Compute_MatchesLegacy()
        => AssertMatchesGolden(Program("01 R PIC 9(3).", "    COMPUTE R = 29 / 10.\n    DISPLAY R."));   // 2.9 → 2 (truncates)
}
