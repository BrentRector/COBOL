// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The OPTIONS paragraph (ISO/IEC 1989:2023 §11.9), now fully parsed into a structured clause tree. The first
/// applied consumer is DEFAULT ROUNDED (§11.9.6): a <b>bare</b> <c>ROUNDED</c> phrase (no MODE) uses the program's
/// DEFAULT ROUNDED mode rather than the NEAREST-AWAY-FROM-ZERO fallback (§14.7.4.3 r1). Mode-specific results are
/// pinned to hand-computed spec values; the former legacy engine (which also honors DEFAULT ROUNDED) is cross-checked.
/// </summary>
public sealed class OptionsDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new(dialectLevel: 2014);   // OPTIONS / ROUNDED MODE IS are ISO-2014+ features

    private static void AssertOutput(string source, string expected)
    {
        var (ok, outp, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(expected, outp);
    }

    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source, 2014);

    /// <summary>A program whose IDENTIFICATION DIVISION carries an <paramref name="options"/> paragraph body.</summary>
    private static string Program(string options, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. OPTRND.
        OPTIONS.
        {options}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    // ── DEFAULT ROUNDED feeds a bare ROUNDED phrase (§11.9.6 r1 / §14.7.4.3 r1). 25/10 = 2.5. ───────────────────
    [Fact]
    public void BareRounded_HonorsDefaultRounded_NearestEven()
    {
        // DEFAULT ROUNDED MODE IS NEAREST-EVEN ⇒ bare ROUNDED rounds 2.5 → 2 (even), NOT 3 (the bare-default).
        var src = Program("           DEFAULT ROUNDED MODE IS NEAREST-EVEN.",
            "01 R PIC 9(3).", "    COMPUTE R ROUNDED = 25 / 10.\n    DISPLAY R.");
        AssertOutput(src, "002");
        AssertMatchesGolden(src);   // the former legacy engine also applies DEFAULT ROUNDED
    }

    [Fact]
    public void BareRounded_HonorsDefaultRounded_Truncation()
    {
        // DEFAULT ROUNDED MODE IS TRUNCATION ⇒ bare ROUNDED truncates 2.5 → 2.
        var src = Program("           DEFAULT ROUNDED MODE IS TRUNCATION.",
            "01 R PIC 9(3).", "    COMPUTE R ROUNDED = 25 / 10.\n    DISPLAY R.");
        AssertOutput(src, "002");
        AssertMatchesGolden(src);
    }

    [Fact]
    public void NoDefaultRounded_BareRounded_IsNearestAway()
        // With no DEFAULT ROUNDED clause (but an OPTIONS paragraph present), a bare ROUNDED is the §11.9.6 r2 default
        // NEAREST-AWAY-FROM-ZERO: 2.5 → 3.
        => AssertOutput(Program("           ARITHMETIC IS NATIVE.",
            "01 R PIC 9(3).", "    COMPUTE R ROUNDED = 25 / 10.\n    DISPLAY R."), "003");

    // ── A multi-clause OPTIONS paragraph parses and runs (ARITHMETIC + DEFAULT ROUNDED), order-independent.
    //    (ARITHMETIC IS STANDARD-DECIMAL — plain STANDARD, the 2014 mode, is rejected: dropped by ISO 2023.) ─────
    [Fact]
    public void MultiClauseOptions_ParsesAndRuns()
        => AssertMatchesGolden(Program(
            "           ARITHMETIC IS STANDARD-DECIMAL\n           DEFAULT ROUNDED MODE IS NEAREST-EVEN.",
            "01 R PIC 9(3).", "    COMPUTE R ROUNDED = 35 / 10.\n    DISPLAY R."));   // 3.5 → 4 (even)

    // ── A bare OPTIONS header with no clauses (§11.9.3 — period optional) still compiles. ───────────────────────
    [Fact]
    public void BareOptionsHeader_Compiles()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OPTBARE.
            OPTIONS.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BARE".
                STOP RUN.
            """);
}
