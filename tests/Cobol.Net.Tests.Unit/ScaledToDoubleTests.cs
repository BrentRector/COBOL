// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The ONE scaled-value→double conversion (kb/Work PB115): <c>CobolFloat.ScaledToDouble</c> must return the
/// CORRECTLY-ROUNDED double of <c>unscaled × 10^(−scale)</c>. The defect it replaced: the emit lane divided by a
/// repeated-multiplication 10^scale (one ulp low at scale ≥ 23), so a LEGAL <c>ASIN(|x| ≤ 1)</c> argument
/// (§15.10.3 r2) arrived above 1.0 and evaluated NaN; <c>CobolDec.ToDouble</c> failed independently through
/// <c>Math.Pow</c>. The oracle here is the IEEE-correct decimal parse — the conversion's own definition.
/// </summary>
public sealed class ScaledToDoubleTests
{
    private static double Oracle(Int128 unscaled, int scale) =>
        double.Parse(unscaled.ToString(CultureInfo.InvariantCulture) + "E" + (-scale),
            NumberStyles.Float, CultureInfo.InvariantCulture);

    [Fact]
    public void TheAsinWitness_31Nines_IsExactlyOne()
    {
        // 0.9999999999999999999999999999999 (31 nines): the nearest double IS 1.0 (the value sits well inside
        // 1.0's half-ulp), so a conforming ASIN argument stays in the closed domain and Math.Asin is π/2 —
        // never NaN. This is the exact shape the refuter's find named.
        Int128 nines31 = Int128.Parse(new string('9', 31), CultureInfo.InvariantCulture);
        Assert.Equal(1.0, CobolFloat.ScaledToDouble(nines31, 31));
        Assert.False(double.IsNaN(Math.Asin(CobolFloat.ScaledToDouble(nines31, 31))));
    }

    [Fact]
    public void EveryAllNinesScale_StaysInsideTheAsinDomain()
    {
        // The whole family: |0.9…9| at every scale 1..31 must convert to ≤ 1.0 — the old divisor made several
        // of them land above it.
        for (int scale = 1; scale <= 31; scale++)
        {
            Int128 nines = Int128.Parse(new string('9', scale), CultureInfo.InvariantCulture);
            double d = CobolFloat.ScaledToDouble(nines, scale);
            Assert.True(d <= 1.0, $"scale {scale}: {d:R} exceeds 1.0");
            Assert.False(double.IsNaN(Math.Asin(d)), $"scale {scale}: ASIN(NaN)");
        }
    }

    [Theory]
    [InlineData("1", 0)]
    [InlineData("-1", 0)]
    [InlineData("123456", 2)]
    [InlineData("5", 1)]
    [InlineData("999999999999999999", 18)]                 // the fast path's edge (fits 2^53? no — slow path)
    [InlineData("9007199254740992", 10)]                   // 2^53 exactly — the fast-path bound
    [InlineData("9007199254740993", 10)]                   // 2^53+1 — the slow path
    [InlineData("1", 23)]                                  // the first scale the old divisor got wrong
    [InlineData("99999999999999999999999", 23)]
    [InlineData("1234567890123456789012345678901", 25)]    // the CobolDec.ToDouble overshoot scale
    [InlineData("170141183460469231731687303715884105727", 31)]   // Int128.MaxValue
    [InlineData("-170141183460469231731687303715884105727", 31)]
    [InlineData("42", -5)]                                 // a PICTURE-P trailing-scaled operand: 42 × 10^5
    [InlineData("7", -22)]
    [InlineData("123", -30)]                               // negative scale past the exact-power bound
    public void MatchesTheCorrectlyRoundedOracle(string unscaledText, int scale)
    {
        Int128 unscaled = Int128.Parse(unscaledText, CultureInfo.InvariantCulture);
        Assert.Equal(Oracle(unscaled, scale), CobolFloat.ScaledToDouble(unscaled, scale));
    }

    [Fact]
    public void SweepsTheOracle_AcrossScalesAndMagnitudes()
    {
        // A deterministic sweep (no Random — reproducibility): digit strings of every length 1..38 at scales
        // -25..31, each compared to the parse oracle. This is the drift net for any future "faster" path.
        int examined = 0;
        for (int len = 1; len <= 38; len++)
        {
            Int128 v = Int128.Parse("9" + new string('3', len - 1), CultureInfo.InvariantCulture);
            for (int scale = -25; scale <= 31; scale += 7)
            {
                Assert.Equal(Oracle(v, scale), CobolFloat.ScaledToDouble(v, scale));
                Assert.Equal(Oracle(-v, scale), CobolFloat.ScaledToDouble(-v, scale));
                examined += 2;
            }
        }
        Assert.True(examined >= 600, $"only {examined} pairs examined");
    }

    /// <summary>⛔ kb/Work PB2641: <c>-Int128.MinValue</c> is <c>Int128.MinValue</c> (the unary minus is unchecked), so a
    /// negated-ternary magnitude test took the "exact small value" path for -2^127 and <c>(long)unscaled</c> kept the
    /// low 64 bits — 0. -2^127 is a legal signed 16-byte COMP-5 value (<c>LOWEST-ALGEBRAIC</c> of one), so the
    /// conversion owes the oracle's answer at EVERY scale, the exact-path scales (±22) and the parse path alike.
    /// §14.6.8.3 GR2: the algebraic value is converted "in a manner consistent with ... ISO/IEC 60559".</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(-1)]
    [InlineData(-22)]
    [InlineData(-23)]
    public void TheSignedMinimum_ConvertsToItsOwnMagnitude_NeverZero(int scale)
    {
        double actual = CobolFloat.ScaledToDouble(Int128.MinValue, scale);
        Assert.Equal(Oracle(Int128.MinValue, scale), actual);
        Assert.True(actual < 0, $"scale {scale}: {actual:R}");
    }

    [Fact]
    public void CobolDecToDouble_RidesTheSameConversion()
    {
        // The refuter's independent-lane find: (double)Sig * Math.Pow(10, Exp) overshot at scale 25.
        var dec = CobolDec.From(Int128.Parse(new string('9', 25), CultureInfo.InvariantCulture), 25);
        Assert.Equal(Oracle(Int128.Parse(new string('9', 25), CultureInfo.InvariantCulture), 25), dec.ToDouble());
        Assert.True(dec.ToDouble() <= 1.0);
    }
}
