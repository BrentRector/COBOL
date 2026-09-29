// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The <c>&gt;&gt;PROPAGATE</c> directive (ISO §7.3.21; PHASE-12 wave 4/5): the COBOL-2002+ compiler directive that
/// controls automatic exception-condition propagation to the activating runtime element (GR1/GR2; default OFF,
/// GR4). These cases pin the directive's RECOGNITION and EDITION GATE (introduction gate, provisional COBOL-2002 per
/// the roadmap decision-1 policy — §7.3.21 is live in the 2023 spec, so the 2002-vs-2014 edge cannot be pinned
/// in-repo); the propagation itself is <c>AutomaticPropagationTests</c> and the golden
/// <c>2002/w73d2_pb1119_propagate_on</c> (kb/Work PB1119). Below 2002 it is the registry's
/// COBOLNET0900 — the ONE introduction band every compiler directive now shares (kb/Work PB725 reconciled this
/// stage's bespoke COBOLNET0883 introduction gate onto it, and kb/Work PB794 RETIRED the rest of 0883 by making
/// §7.3.21.2's { ON | OFF } the row's directiveOperand column, checked by the one COBOLNET1911 producer) —
/// never a silent stray token; at 2002+ a well-formed <c>&gt;&gt;PROPAGATE ON|OFF</c> is recognized-and-consumed and
/// the program compiles (the run behavior is the <c>propagate_directive</c> conformance corpus).
/// </summary>
public sealed class PropagateDirectiveTests
{
    private static string Prog(string directive) => directive + "\n" + """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PROPG.
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY "X".
            STOP RUN.
        """;

    /// <summary>A well-formed >>PROPAGATE ON/OFF is recognized (recognized-and-consumed) at COBOL-2002+.</summary>
    [Theory]
    [InlineData(2002, "      >>PROPAGATE ON")]
    [InlineData(2014, "      >>PROPAGATE ON")]
    [InlineData(2023, "      >>PROPAGATE OFF")]
    [InlineData(2014, "      >>PROPAGATE")]
    public void WellFormed_RecognizedAt2002Plus(int edition, string directive)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(directive), edition);
        Assert.True(ok, $">>PROPAGATE must be recognized at COBOL-{edition}:\n{string.Join("\n", diag)}");
    }

    /// <summary>The introduction gate: >>PROPAGATE is rejected below 2002 with the registry's COBOLNET0900 —
    /// the same code every other compiler directive's introduction edge uses (kb/Work PB725) — never a silent
    /// stray token that would surface as a generic parse error.</summary>
    [Fact]
    public void BelowIntroduction_Rejected0900()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("      >>PROPAGATE ON"), 85);
        Assert.False(ok, ">>PROPAGATE must be rejected at COBOL-85 (introduced 2002)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
        Assert.DoesNotContain(diag, d => d.Contains("COBOLNET0883"));   // 0883 no longer owns the edition half
    }

    /// <summary>A malformed operand (not ON/OFF) is the §7.3.21.2 syntax diagnostic, never a silent accept — and
    /// it is the SHARED one: kb/Work PB794 made §7.3.3 SR6 ("compiler-instruction is composed … as specified in
    /// the syntax of each directive") ONE producer for the whole family, so this directive's own COBOLNET0883 is
    /// retired and the diagnostic is COBOLNET1911, off the propagate-directive-2002 row's directiveOperand.</summary>
    [Fact]
    public void MalformedOperand_Rejected1911()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("      >>PROPAGATE MAYBE"), 2014);
        Assert.False(ok, ">>PROPAGATE with a non-ON/OFF operand must be rejected (ISO §7.3.21.2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1911");
        Assert.DoesNotContain(diag, d => d.Contains("COBOLNET0883"));   // retired at PB794 — never reallocate
    }

    /// <summary>§7.3.3 SR3/SR4 — a directive "may be followed only by space characters and an optional inline
    /// comment". This stage sliced its own operand and knew nothing of that, so a conforming
    /// <c>&gt;&gt;PROPAGATE ON *&gt; on</c> drew the malformed-operand error (kb/Work PB794).</summary>
    [Fact]
    public void TrailingInlineComment_IsNotPartOfTheOperand()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("      >>PROPAGATE ON *> propagate from here"), 2023);
        Assert.True(ok, "a trailing inline comment is legal after a directive (ISO §7.3.3 SR3/SR4):\n"
            + string.Join("\n", diag));
    }
}
