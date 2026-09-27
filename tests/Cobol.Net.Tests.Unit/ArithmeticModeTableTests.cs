// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <see cref="ArithmeticModes"/> — the per-mode spec facts, asserted EXHAUSTIVELY over
/// <see cref="ArithmeticMode"/> rather than for the modes that happen to be reachable.
///
/// <para><b>What this replaces.</b> The NUMVAL digit cap was a two-state ternary
/// (<c>num.StandardDecimal ? 34 : ""</c>), so STANDARD-BINARY fell through the else-branch and would have taken
/// the NATIVE cap of 31 where §15.93.4 r1b sub-note 3 says 35. Unreachable — the mode is declined at bind — but
/// that is precisely the kind of entry that is never contradicted and so is never found wrong
/// (<c>feedback_a_dead_lookup_is_also_unverified</c>). The table now states all four modes and this test walks
/// the enum, so a fifth mode cannot be added and silently inherit somebody's else-branch.</para>
/// </summary>
public sealed class ArithmeticModeTableTests
{
    [Fact]
    public void NumvalDigitCap_IsTotalOverEveryArithmeticMode()
    {
        foreach (ArithmeticMode mode in Enum.GetValues<ArithmeticMode>())
        {
            int cap = ArithmeticModes.NumvalDigitCap(mode);   // throws for an unmapped mode — that IS the assertion
            Assert.InRange(cap, 31, 35);
        }
    }

    [Theory]
    // §15.93.4 r1b (and its §15.94.4 twin) states the cap in three sub-notes, one per mode:
    [InlineData(ArithmeticMode.Native, 31)]           //  2) "…greater than 31 digits is the 32nd digit…"
    [InlineData(ArithmeticMode.StandardBinary, 35)]   //  3) "…the argument has more than 35 digits…"
    [InlineData(ArithmeticMode.StandardDecimal, 34)]  //  4) "…the argument has more than 34 digits…"
    // Not named by the sub-notes: the 2002 STANDARD mode routes to the same SDIDI decimal engine, so it takes
    // STANDARD-DECIMAL's cap. Asserted so the reasoning is pinned and not re-derived from scratch each time.
    [InlineData(ArithmeticMode.Standard, 34)]
    public void NumvalDigitCap_PinnedToSpec(ArithmeticMode mode, int expected) =>
        Assert.Equal(expected, ArithmeticModes.NumvalDigitCap(mode));

    [Fact]
    public void DefaultDigitCap_IsTheNativeOne_SoOmissionMeansNative()
    {
        // The emitted call omits `digitCap:` when the mode's cap equals the runtime default. If these two ever
        // disagreed, every native compilation would silently emit — or silently omit — the wrong cap.
        Assert.Equal(ArithmeticModes.DefaultDigitCap, ArithmeticModes.NumvalDigitCap(ArithmeticMode.Native));
    }

    [Fact]
    public void StandardBinaryCap_IsUnreachable_TheOtherHalfOfTheDoubleDefence()
    {
        // The entry exists and is correct, and it must stay UNREACHABLE: the render lane is the second line of
        // defence behind the bind-time decline, never a substitute for it. If a compilation could reach the
        // renderer under StandardBinary, this cap would start deciding real output — so the assertion that
        // matters is the pair: the value is the spec's, AND the mode never survives binding
        // (ArithmeticModeScreenDriftTests proves the decline for every unit kind).
        Assert.Equal(35, ArithmeticModes.NumvalDigitCap(ArithmeticMode.StandardBinary));
        Assert.NotEqual(ArithmeticModes.DefaultDigitCap, ArithmeticModes.NumvalDigitCap(ArithmeticMode.StandardBinary));
    }

    // ── kb/Work PB194 — the mode SET, and the bounds it selects, live in ONE place ────────────────────────────

    [Fact]
    public void IsDecimalEngine_AndExponentRange_AreTotalOverEveryArithmeticMode()
    {
        foreach (ArithmeticMode mode in Enum.GetValues<ArithmeticMode>())
        {
            _ = ArithmeticModes.IsDecimalEngine(mode);                  // throws for an unmapped mode — the assertion
            var (far, near) = ArithmeticModes.IntermediateExponentRange(mode);
            Assert.True(far > 0 && near < 0, $"{mode}: the intermediate range must straddle zero");
            Assert.False(string.IsNullOrWhiteSpace(ArithmeticModes.IntermediateName(mode)));
        }
    }

    [Theory]
    // ARITHMETIC IS STANDARD (2002; obsolete 2014, removed 2023) is NOT a third engine — its standard
    // intermediate data item for every operand WiseOwl COBOL can carry IS the standard-decimal one, so every
    // mode-conditioned decision must answer for it exactly as it answers for STANDARD-DECIMAL. Two sites in
    // IntrinsicBinder had dropped it from the set and screened it against binary64 instead (measured: a
    // PIC 9(3)E+999 argument REJECTED under STANDARD, accepted under STANDARD-DECIMAL).
    [InlineData(ArithmeticMode.Native, false, 308, -324)]
    [InlineData(ArithmeticMode.Standard, true, 6145, -6176)]
    [InlineData(ArithmeticMode.StandardDecimal, true, 6145, -6176)]
    // §8.8.1.4.2 NOTE 3 — the SBIDI's ±(2**16384 − 2**16271) ≈ 1.19E+4932 and 2**-16494 ≈ 6.5E−4966.
    // Unreachable (declined at bind), recorded anyway — the NumvalDigitCap precedent above.
    [InlineData(ArithmeticMode.StandardBinary, false, 4932, -4966)]
    public void IntermediateExponentRange_PinnedToSpec(ArithmeticMode mode, bool decimalEngine, int far, int near)
    {
        Assert.Equal(decimalEngine, ArithmeticModes.IsDecimalEngine(mode));
        Assert.Equal((far, near), ArithmeticModes.IntermediateExponentRange(mode));
    }

    [Fact]
    public void StandardAndStandardDecimal_AreOneEngine_InEveryTable()
    {
        // The invariant PB194 violated, stated once. A fact that differs between these two modes is a bug in
        // the fact, not a distinction — everything reachable routes to the same CobolDec engine.
        Assert.Equal(ArithmeticModes.IsDecimalEngine(ArithmeticMode.StandardDecimal),
            ArithmeticModes.IsDecimalEngine(ArithmeticMode.Standard));
        Assert.Equal(ArithmeticModes.IntermediateExponentRange(ArithmeticMode.StandardDecimal),
            ArithmeticModes.IntermediateExponentRange(ArithmeticMode.Standard));
        Assert.Equal(ArithmeticModes.NumvalDigitCap(ArithmeticMode.StandardDecimal),
            ArithmeticModes.NumvalDigitCap(ArithmeticMode.Standard));
        Assert.Equal(ArithmeticModes.IntermediateName(ArithmeticMode.StandardDecimal),
            ArithmeticModes.IntermediateName(ArithmeticMode.Standard));
    }

    [Fact]
    public void TheSdidiExponentBounds_AreWrittenDownExactlyOnce()
    {
        // ⛔ THE DRIFT GUARD (kb/Work PB194). The fourth copy is one edit away, and the two copies that
        // existed were found only because a survey happened to read both files. 6145 is the SDIDI's STRICT
        // UPPER DECADE BOUND — the exponent its all-nines maximum 9.999…E+6144 = (1 − 10⁻³⁴)·10^6145 sits just
        // under, which is the quantity the caller's over-approximated `intDigits + MaxExp` is compared against
        // (decimal128's ADJUSTED exponent bound is 6144, a different number, and this comment used to name that
        // one — kb/Work PB275). It appears nowhere else in the compiler; if it does, the mode set has been
        // written down again.
        var offenders = Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && Path.GetFileName(f) != "OptionsModel.cs"
                     && File.ReadAllText(f).Contains("6145", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "the SDIDI exponent bound is written down outside ArithmeticModes.IntermediateExponentRange: "
            + string.Join(", ", offenders));
    }

    // ── kb/Work PB452 — the mode's intermediate EXTREME VALUES (SET format 15, §14.9.39.4 GR32 b / GR36 b) ──

    [Fact]
    public void IntermediateExtremes_AreTotalOverEveryArithmeticMode()
    {
        foreach (ArithmeticMode mode in Enum.GetValues<ArithmeticMode>())
        {
            var (far, near) = ArithmeticModes.IntermediateExtremes(mode);   // throws for an unmapped mode
            Assert.False(string.IsNullOrWhiteSpace(far));
            Assert.False(string.IsNullOrWhiteSpace(near));
            // A magnitude, never a signed value: GR32 c / GR36 c apply the sign separately.
            Assert.NotEqual('-', far[0]);
            Assert.NotEqual('-', near[0]);
        }
    }

    [Fact]
    public void IntermediateExtremes_AgreeWithTheDecadeBoundsTheyAreNot()
    {
        // ⛔ THE TWO TABLES ANSWER DIFFERENT QUESTIONS AND MUST STILL AGREE. IntermediateExponentRange is a
        // DECADE BOUND for a well-formedness screen; IntermediateExtremes is the representable value itself.
        // The invariant that ties them: every extreme's own decimal exponent lies within [Closest, Farthest].
        // It is `<=` at the top, not `<`, because IntermediateExponentRange's own doc comment says the column
        // is read under two conventions (kb/Work PB194/PB275) — the SDIDI row is a STRICT bound (6145, one
        // past 9.999…E+6144) while the binary64 row is the maximum's OWN exponent (308, because 9.99E+308 is
        // not representable and 309 would admit it). Conflating the two tables would store 10^6145 into a
        // decimal128, a value it cannot hold, which is the mistake this pair is shaped to prevent.
        foreach (ArithmeticMode mode in Enum.GetValues<ArithmeticMode>())
        {
            var (farExp, nearExp) = ArithmeticModes.IntermediateExponentRange(mode);
            var (far, near) = ArithmeticModes.IntermediateExtremes(mode);
            Assert.True(Exp10Of(far) <= farExp, $"{mode}: {far} is past 1E{farExp}");
            Assert.True(Exp10Of(near) >= nearExp, $"{mode}: {near} is below 1E{nearExp}");
        }

        // floor(log10(|literal|)) from the text alone — no binary float in the path (the values reach 1E±6176).
        static int Exp10Of(string lit)
        {
            int e = lit.IndexOf('E', StringComparison.OrdinalIgnoreCase);
            int exp = e < 0 ? 0 : int.Parse(lit[(e + 1)..], System.Globalization.CultureInfo.InvariantCulture);
            string mant = e < 0 ? lit : lit[..e];
            int dot = mant.IndexOf('.');
            string digits = dot < 0 ? mant : mant[..dot] + mant[(dot + 1)..];
            int intDigits = dot < 0 ? mant.Length : dot;
            int lead = 0;
            while (lead < digits.Length && digits[lead] == '0') lead++;
            return exp + intDigits - lead - 1;
        }
    }

    [Fact]
    public void IntermediateExtremes_AreOneEngineForStandardAndStandardDecimal() =>
        // The PB194 invariant, extended to the new table the moment it exists rather than after it drifts.
        Assert.Equal(ArithmeticModes.IntermediateExtremes(ArithmeticMode.StandardDecimal),
            ArithmeticModes.IntermediateExtremes(ArithmeticMode.Standard));
}
