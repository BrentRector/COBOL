// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The ON SIZE ERROR phrase and the size error condition (ISO/IEC 1989:2023 §14.7.5), two-phase: a per-receiver
/// capacity overflow leaves only that receiver unchanged and the others stored (rule 2), a zero divisor (case 2) or
/// a ROUNDED MODE IS PROHIBITED inexact result (§14.7.4.3 r7) raises the condition with no receiver changed, and the
/// ON / NOT ON SIZE ERROR imperative runs once afterward. Pinned to hand-computed spec values; the legacy oracle
/// (NIST-exercised for ON SIZE ERROR) is cross-checked. The "receiver unchanged" cases are the silent-corruption
/// class — each asserts the receiver's post-value, not just the imperative output.
/// </summary>
public sealed class OnSizeErrorDifferentialTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    // ROUNDED MODE IS is an ISO-2014+ phrase (§14.7.4) — the PROHIBITED facts compile at 2014.
    private static readonly ICompilerUnderTest CobolNet2014 = new CobolNetCompiler(dialectLevel: 2014);

    private static void AssertOutput(string source, string expected, bool needs2014 = false)
    {
        var (ok, outp, detail) = (needs2014 ? CobolNet2014 : CobolNet).CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(expected, outp);
    }

    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SIZ.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    // ── Capacity overflow → ON SIZE ERROR fires, receiver UNCHANGED (§14.7.5 rule 1/case 3). ────────────────────
    [Fact]
    public void Overflow_FiresOnSizeError_ReceiverUnchanged()
    {
        // 99 + 50 = 149 does not fit PIC 9(2) (max 99) → ON SIZE ERROR; R keeps its initial VALUE 07 (an explicit
        // VALUE makes the unchanged-receiver image deterministic — an unvalued numeric differs across engines).
        var src = Program("01 R PIC 9(2) VALUE 7.", "    ADD 99 50 GIVING R ON SIZE ERROR DISPLAY \"OVF\".\n    DISPLAY R.");
        AssertOutput(src, "OVF\n07");
        AssertSameAsLegacy(src);
    }

    [Fact]
    public void NoOverflow_FiresNotOnSizeError()
    {
        var src = Program("01 R PIC 9(2).", "    ADD 1 2 GIVING R NOT ON SIZE ERROR DISPLAY \"OK\".\n    DISPLAY R.");
        AssertOutput(src, "OK\n03");
        AssertSameAsLegacy(src);
    }

    [Fact]
    public void BothPhrases_ErrorPath_RunsOnSizeErrorOnly()
        => AssertOutput(Program("01 R PIC 9(2).",
            "    ADD 99 50 GIVING R ON SIZE ERROR DISPLAY \"E\" NOT ON SIZE ERROR DISPLAY \"N\".\n    DISPLAY R."),
            "E\n00");

    [Fact]
    public void BothPhrases_OkPath_RunsNotOnSizeErrorOnly()
        => AssertOutput(Program("01 R PIC 9(2).",
            "    ADD 1 2 GIVING R ON SIZE ERROR DISPLAY \"E\" NOT ON SIZE ERROR DISPLAY \"N\".\n    DISPLAY R."),
            "N\n03");

    // ── ROUNDED MODE IS PROHIBITED: an inexact result is a size error (§14.7.4.3 r7); an exact result is not. ────
    [Fact]
    public void ProhibitedInexact_FiresOnSizeError_ReceiverUnchanged()
    {
        // 10 / 3 = 3.333… is inexact at scale 0 → PROHIBITED size error; R unchanged (00).
        var src = Program("01 R PIC 9(2).",
            "    COMPUTE R ROUNDED MODE IS PROHIBITED = 10 / 3 ON SIZE ERROR DISPLAY \"PROH\".\n    DISPLAY R.");
        AssertOutput(src, "PROH\n00", needs2014: true);
    }

    [Fact]
    public void ProhibitedExact_DoesNotFire()
        // 9 / 3 = 3 is exact → no size error → NOT path; R = 03.
        => AssertOutput(Program("01 R PIC 9(2).",
            "    COMPUTE R ROUNDED MODE IS PROHIBITED = 9 / 3 ON SIZE ERROR DISPLAY \"P\" NOT ON SIZE ERROR DISPLAY \"OK\".\n    DISPLAY R."),
            "OK\n03", needs2014: true);

    // ── Divide by zero → ON SIZE ERROR fires, receiver UNCHANGED (§14.7.5 case 2). ──────────────────────────────
    [Fact]
    public void DivideByZero_FiresOnSizeError_ReceiverUnchanged()
        => AssertOutput(Program("01 R PIC 9(2) VALUE 7.",
            "    DIVIDE 5 BY 0 GIVING R ON SIZE ERROR DISPLAY \"D0\".\n    DISPLAY R."),
            "D0\n07");   // R keeps 07

    // ── Multiple receivers: only the overflowing one is unchanged; the others ARE stored (§14.7.5 rule 2). ──────
    [Fact]
    public void MultiReceiver_PartialStore_OnlyOverflowingUnchanged()
    {
        // 50 → R1 PIC 9(3) = 050 (stored); R2 PIC 9(1) overflows (50 > 9) → unchanged at its VALUE 8; SIZE ERROR fires.
        var src = Program("01 R1 PIC 9(3) VALUE 1.\n01 R2 PIC 9(1) VALUE 8.",
            "    ADD 50 GIVING R1 R2 ON SIZE ERROR DISPLAY \"E\".\n    DISPLAY R1.\n    DISPLAY R2.");
        AssertOutput(src, "E\n050\n8");
        AssertSameAsLegacy(src);
    }

    // ── Intermediate overflow of the long engine → ON SIZE ERROR fires (§14.7.5 case 5; the phrase ENABLES the
    //    check). 9999999999 × 9999999999 ≈ 9.9e19 overflows long; the checked(...) the store wraps the value in
    //    throws, caught as the size error. The legacy reaches the same outcome via its wider intermediate then a
    //    capacity overflow — both leave R unchanged and run the imperative. ───────────────────────────────────────
    [Fact]
    public void IntermediateOverflow_FiresOnSizeError_ReceiverUnchanged()
    {
        var src = Program("01 R PIC 9(4) VALUE 1234.",
            "    COMPUTE R = 9999999999 * 9999999999 ON SIZE ERROR DISPLAY \"OVF\".\n    DISPLAY R.");
        AssertOutput(src, "OVF\n1234");
        AssertSameAsLegacy(src);
    }

    // ── No ON SIZE ERROR phrase: a normal in-range computation is unaffected (the unchecked path). ──────────────
    [Fact]
    public void NoPhrase_NormalArithmetic_Unaffected()
        => AssertSameAsLegacy(Program("01 R PIC 9(2).", "    ADD 1 2 GIVING R.\n    DISPLAY R."));

    // ── ROUNDED MODE IS PROHIBITED into a numeric-EDITED receiver: an inexact transfer is a size error
    //    (§14.7.4.3 r7 → EC-SIZE-TRUNCATION), receiver UNCHANGED. The DEVLOG-610-audited leak: the edited-store
    //    path used plain CobolNum.Rescale (silent truncation) while the numeric path's TryStore checked it; the
    //    RescaleChecked cure makes all three receiver categories agree. 2014-only (ROUNDED MODE is 2014+). The
    //    fix lives in the ONE shared StoreArith edited branch, so it holds across COMPUTE / ADD-GIVING / DIVIDE. ──
    [Theory]
    [InlineData("COMPUTE E ROUNDED MODE IS PROHIBITED = A")]                 // 2.25 → 9.9 inexact
    [InlineData("ADD A B GIVING E ROUNDED MODE IS PROHIBITED")]             // 1.15+1.10 = 2.25
    [InlineData("DIVIDE A BY B GIVING E ROUNDED MODE IS PROHIBITED")]       // 1.15/1.10 inexact
    public void ProhibitedInexact_EditedReceiver_FiresSizeError_Unchanged(string verb)
        => AssertOutput(Program(
            "01 A PIC 9V99 VALUE 1.15.\n        01 B PIC 9V99 VALUE 1.10.\n        01 E PIC 9.9.",
            $"    MOVE 8.8 TO E.\n    {verb}\n        ON SIZE ERROR DISPLAY \"SE\".\n    DISPLAY E."),
            "SE\n8.8", needs2014: true);

    // The EXACT counterpart binds without a size error and stores (2.20 into 9.9 is exact).
    [Fact]
    public void ProhibitedExact_EditedReceiver_Stores()
        => AssertOutput(Program(
            "01 A PIC 9V99 VALUE 2.20.\n        01 E PIC 9.9.",
            "    MOVE 8.8 TO E.\n    COMPUTE E ROUNDED MODE IS PROHIBITED = A\n        "
            + "NOT ON SIZE ERROR DISPLAY \"OK\".\n    DISPLAY E."),
            "OK\n2.2", needs2014: true);
}
