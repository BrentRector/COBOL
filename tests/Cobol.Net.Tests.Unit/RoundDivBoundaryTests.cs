// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ONE ROUNDING KERNEL IS EXACT OVER EVERY DIVISOR ITS CALLERS PASS (kb/Work PB2640).
/// <para><c>CobolNum.RoundDiv</c> decides the three NEAREST-* modes (§14.7.4.3 GR4/GR5/GR6: "rounded to the nearest
/// value that can be represented", a tie being two values equally near) from the remainder. It used to compare
/// <c>2·|rem|</c> with the divisor, and on the <see cref="Int128"/> lane the doubling wraps NEGATIVE once
/// <c>|rem|</c> passes 2^126 (about 8.5e37): <c>COMPUTE Q ROUNDED = (A * B) / (C * D)</c> over four
/// <c>PIC 9(19)</c> items, 9.025e37 ÷ 9.409e37 = 0.959, stored 0 with no size error. The kernel now compares
/// <c>|rem|</c> with its complement <c>divisor − |rem|</c>, which cannot overflow on any carrier. The
/// <see cref="UInt128"/> lane wraps at 2^127 in the same way and the <see cref="BigInteger"/> lane never does, so
/// the rows run all three carriers over the same shapes: the answer is the carrier-independent exact one.</para>
/// </summary>
public sealed class RoundDivBoundaryTests
{
    private static readonly Int128 BigDivisor = Int128.Parse("94090000000000000000000000000000000000");   // 9.409e37 > 2^126

    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, 1)]
    [InlineData(CobolRounding.NearestEven, 1)]
    [InlineData(CobolRounding.NearestTowardZero, 1)]
    [InlineData(CobolRounding.Truncation, 0)]
    [InlineData(CobolRounding.AwayFromZero, 1)]
    [InlineData(CobolRounding.TowardGreater, 1)]
    [InlineData(CobolRounding.TowardLesser, 0)]
    public void ARemainderPast2To126_AboveHalf_RoundsUpInEveryNearestMode(CobolRounding mode, int expected)
    {
        // 9.025e37 / 9.409e37 = 0.959: q = 0, |rem| = 9.025e37 > 2^126, 2·|rem| = 1.805e38 > Int128.MaxValue.
        Int128 value = Int128.Parse("90250000000000000000000000000000000000");
        Assert.Equal((Int128)expected, CobolNum.RoundDiv(value, BigDivisor, mode));
    }

    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, -1)]
    [InlineData(CobolRounding.NearestEven, -1)]
    [InlineData(CobolRounding.NearestTowardZero, -1)]
    [InlineData(CobolRounding.Truncation, 0)]
    [InlineData(CobolRounding.AwayFromZero, -1)]
    [InlineData(CobolRounding.TowardGreater, 0)]
    [InlineData(CobolRounding.TowardLesser, -1)]
    public void TheNegativeTwin_RoundsAwayFromZero_AndEveryModeKeepsItsDirection(CobolRounding mode, int expected)
    {
        Int128 value = Int128.Parse("-90250000000000000000000000000000000000");
        Assert.Equal((Int128)expected, CobolNum.RoundDiv(value, BigDivisor, mode));
    }

    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero)]
    [InlineData(CobolRounding.NearestEven)]
    [InlineData(CobolRounding.NearestTowardZero)]
    public void ARemainderBelowHalf_OnTheSameDivisor_StaysDown(CobolRounding mode)
    {
        // 4.0e37 / 9.409e37 = 0.425 — the control that never wrapped.
        Assert.Equal(Int128.Zero, CobolNum.RoundDiv(Int128.Parse("40000000000000000000000000000000000000"), BigDivisor, mode));
    }

    /// <summary>The tie, at the largest even divisor whose half is a legal remainder: 2·rem == divisor holds exactly
    /// and the three NEAREST modes diverge on it (away up, toward-zero down, even keeps the even quotient). The
    /// quotient is 0 (even) and then 1 (odd) so NEAREST-EVEN is exercised in both directions.</summary>
    [Fact]
    public void AnExactTie_AtTheTopOfTheCarrier_IsDecidedPerMode()
    {
        Int128 divisor = Int128.MaxValue - 1;                       // 2^127 − 2, even
        Int128 half = divisor / 2;                                   // 2^126 − 1
        Assert.Equal((Int128)1, CobolNum.RoundDiv(half, divisor, CobolRounding.NearestAwayFromZero));
        Assert.Equal((Int128)0, CobolNum.RoundDiv(half, divisor, CobolRounding.NearestTowardZero));
        Assert.Equal((Int128)0, CobolNum.RoundDiv(half, divisor, CobolRounding.NearestEven));        // 0 is even
        Assert.Equal((Int128)(-1), CobolNum.RoundDiv(-half, divisor, CobolRounding.NearestAwayFromZero));
        Assert.Equal((Int128)0, CobolNum.RoundDiv(-half, divisor, CobolRounding.NearestTowardZero));
        // Quotient 1 (odd) plus the same half: the value is 1.5 divisors; it needs 2^127−2 + half < 2^127 — not
        // representable, so the odd-quotient tie is covered on a divisor whose 1.5 multiple fits.
        Int128 small = 1_000_000_000_000_000_000;
        Assert.Equal((Int128)2, CobolNum.RoundDiv(small + small / 2, small, CobolRounding.NearestEven));   // 1.5 → 2 (even)
        Assert.Equal((Int128)2, CobolNum.RoundDiv(2 * small + small / 2, small, CobolRounding.NearestEven)); // 2.5 → 2 (even)
        Assert.Equal((Int128)3, CobolNum.RoundDiv(2 * small + small / 2, small, CobolRounding.NearestAwayFromZero));
    }

    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, 1)]
    [InlineData(CobolRounding.NearestEven, 1)]
    [InlineData(CobolRounding.NearestTowardZero, 1)]
    public void TheUnsignedWideLane_AgreesPast2To127(CobolRounding mode, int expected)
    {
        // The UInt128 carrier doubles into its own wrap at 2^127 of remainder: 2.9e38 / 3.0e38 = 0.967.
        UInt128 divisor = UInt128.Parse("300000000000000000000000000000000000000");
        UInt128 value = UInt128.Parse("290000000000000000000000000000000000000");
        Assert.Equal((UInt128)expected, CobolNum.RoundDiv(value, divisor, mode));
    }

    [Fact]
    public void EveryCarrierGivesTheSameAnswer_OverASweepOfRemaindersAroundTheHalf()
    {
        // The BigInteger lane cannot overflow, so it is the oracle for the other two on the shapes that reach the
        // wrap: divisors above 2^126, remainders on both sides of the half.
        Int128[] divisors = [BigDivisor, Int128.MaxValue, Int128.MaxValue - 1, (Int128)1 << 126];
        CobolRounding[] nearest = [CobolRounding.NearestAwayFromZero, CobolRounding.NearestEven, CobolRounding.NearestTowardZero];
        foreach (Int128 d in divisors)
            foreach (Int128 offset in new Int128[] { -3, -2, -1, 0, 1, 2, 3 })
                foreach (CobolRounding mode in nearest)
                {
                    Int128 rem = d / 2 + offset;
                    if (rem <= 0 || rem >= d) continue;
                    BigInteger expected = CobolNum.RoundDiv((BigInteger)rem, (BigInteger)d, mode);
                    Assert.Equal(expected, (BigInteger)CobolNum.RoundDiv(rem, d, mode));
                    Assert.Equal(-expected, (BigInteger)CobolNum.RoundDiv(-rem, d, mode));
                    Assert.Equal(expected, (BigInteger)CobolNum.RoundDiv((UInt128)rem, (UInt128)d, mode));
                }
    }

    /// <summary>The sibling in the same chain (kb/Work PB2640's sweep, the PB2641 family): <c>Int128.MinValue</c> (-2^127,
    /// a legal signed 16-byte COMP-5 value) has no signed negation, so <c>Divide</c>'s "make the divisor positive" step
    /// left <c>Min ÷ -3</c> NEGATIVE and <c>Min ÷ -1</c> silently <c>Min</c>, and a divisor of exactly <c>Min</c> reached
    /// <c>RoundDiv</c> negative. The quotient is formed on the magnitudes and signed afterwards.</summary>
    [Fact]
    public void TheSignedMinimum_AsDividendOrDivisor_DividesOnItsMagnitude()
    {
        Int128 min = Int128.MinValue;
        Int128 third = Int128.Parse("56713727820156410577229101238628035242");     // trunc(2^127 / 3)
        Assert.Equal(-third, CobolNum.Divide(min, 0, 3, 0, 0, CobolRounding.Truncation));
        Assert.Equal(third, CobolNum.Divide(min, 0, -3, 0, 0, CobolRounding.Truncation));
        Assert.Equal(min, CobolNum.Divide(min, 0, 1, 0, 0, CobolRounding.Truncation));
        Assert.Equal((Int128)1, CobolNum.Divide(min, 0, min, 0, 0, CobolRounding.Truncation));
        Assert.Equal((Int128)0, CobolNum.Divide(7, 0, min, 0, 0, CobolRounding.NearestAwayFromZero));
        // The quotient 2^127 is the one value a POSITIVE result cannot hold: the §14.7.5 case-5 size error, not a wrap.
        var ex = Assert.Throws<CobolSizeError>(() => CobolNum.Divide(min, 0, -1, 0, 0, CobolRounding.Truncation));
        Assert.Equal("EC-SIZE-OVERFLOW", ex.EcName);
    }

    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, -1)]
    [InlineData(CobolRounding.NearestEven, 0)]
    [InlineData(CobolRounding.NearestTowardZero, 0)]
    [InlineData(CobolRounding.AwayFromZero, -1)]
    [InlineData(CobolRounding.TowardGreater, 0)]
    [InlineData(CobolRounding.TowardLesser, -1)]
    public void ADivisorOfTheSignedMinimum_KeepsTheQuotientsSign_InEveryMode(CobolRounding mode, int expected)
    {
        // 2^126 ÷ -2^127 = -0.5 exactly: a tie for the NEAREST modes, and a NEGATIVE quotient for the directed ones.
        Assert.Equal((Int128)expected, CobolNum.Divide((Int128)1 << 126, 0, Int128.MinValue, 0, 0, mode));
    }

    [Fact]
    public void TheWideAlignment_AcceptsADivisorOfTheSignedMinimum()
    {
        // exp = 1 and a = Min: the dividend scales into the 256-bit numerator (QuotientAtScale), whose divisor 2^127
        // used to be refused. (Min × 10) ÷ Min = 10.
        Assert.Equal((Int128)10, CobolNum.Divide(Int128.MinValue, 0, Int128.MinValue, 0, 1, CobolRounding.Truncation));
    }

    [Fact]
    public void TheDivideEntry_RoundsTheIssueQuotientUp()
    {
        // CobolNum.Divide(A·B, 0, C·D, 0, 0, mode): the shape NumericRenderer.Divide emits for the COMPUTE of the issue.
        Int128 ab = Int128.Parse("90250000000000000000000000000000000000");
        Assert.Equal((Int128)1, CobolNum.Divide(ab, 0, BigDivisor, 0, 0, CobolRounding.NearestAwayFromZero));
        Assert.Equal((Int128)1, CobolNum.Divide(ab, 0, BigDivisor, 0, 0, CobolRounding.NearestEven));
        Assert.Equal((Int128)(-1), CobolNum.Divide(-ab, 0, BigDivisor, 0, 0, CobolRounding.NearestTowardZero));
    }
}
