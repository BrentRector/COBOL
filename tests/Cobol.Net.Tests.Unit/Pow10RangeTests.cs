// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB2633 and PB648 - <c>Pow10.AsWide</c> / <c>FiveAsWide</c> answer ONLY inside their tables. The fallback loop
/// they used to carry wrapped the Int128 silently past 10^38 and returned 1 for a negative exponent, which is how
/// <c>CobolIntrinsics.FromDoubleBounded</c> at a trailing-P receiver's scale of -2 formed 10^39 as a negative number
/// and clamped every ACOS result to -1 (= -100 in PIC S9PP). The bounded-codomain quantizer now treats a negative
/// scale explicitly.
/// </summary>
/// <remarks>Every expected value is computed by BigInteger arithmetic on the rule, never observed: the limit at a scale
/// is floor(max37 / 10^(37 - scale)), and 10^k for k &gt; 37 exceeds every scale-37 codomain constant (each is below
/// 10^38), so the limit is zero from scale -1 down.</remarks>
public sealed class Pow10RangeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(18)]
    [InlineData(37)]
    [InlineData(38)]
    public void AsWide_InTable_IsTheExactPower(int n) =>
        Assert.Equal((Int128)BigInteger.Pow(10, n), Pow10.AsWide(n));

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(39)]
    [InlineData(40)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void AsWide_OutsideTheTable_FailsLoudly_NeverWrapsOrAnswersOne(int n) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Pow10.AsWide(n));

    [Theory]
    [InlineData(0)]
    [InlineData(27)]
    [InlineData(54)]
    public void FiveAsWide_InTable_IsTheExactPower(int n) =>
        Assert.Equal((Int128)BigInteger.Pow(5, n), Pow10.FiveAsWide(n));

    [Theory]
    [InlineData(-1)]
    [InlineData(55)]
    [InlineData(56)]
    public void FiveAsWide_OutsideTheTable_FailsLoudly(int n) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Pow10.FiveAsWide(n));

    public static TheoryData<string, Int128> Codomains => new()
    {
        { "pi", CobolIntrinsics.CodomainPi37 },
        { "half-pi", CobolIntrinsics.CodomainHalfPi37 },
        { "below-one", CobolIntrinsics.CodomainBelowOne37 },
    };

    /// <summary>The clamp's limit is floor(max37 / 10^(37 - scale)) at EVERY scale a receiver can have, trailing-P
    /// (negative) scales included: a quantized value far beyond the bound lands exactly on the limit, and one inside
    /// it is untouched. Scale -2 was -1 and scale -11 was +1 before PB2633 (the wrapped 10^39 and 10^48).</summary>
    [Theory]
    [MemberData(nameof(Codomains))]
    public void FromDoubleBounded_ClampsToTheExactLimit_AtEveryReceiverScale(string name, Int128 max37)
    {
        for (int scale = -31; scale <= 31; scale++)
        {
            BigInteger limit = scale < 0 ? BigInteger.Zero : (BigInteger)max37 / BigInteger.Pow(10, 37 - scale);
            // 8.0 (and 8.0e30 at a negative scale, so it still quantizes past the bound) is outside every codomain:
            // a clamp that works lands exactly on the limit.
            Int128 far = CobolIntrinsics.FromDoubleBounded(scale >= 0 ? 8.0 : 8.0e30, scale, CobolRounding.NearestAwayFromZero, max37);
            Assert.True(far == (Int128)limit, $"{name} scale {scale}: clamp landed {far}, limit {limit}");
            Int128 inside = CobolIntrinsics.FromDoubleBounded(0.5, scale, CobolRounding.Truncation, max37);
            Assert.True(inside >= 0, $"{name} scale {scale}: 0.5 landed {inside} (a negative limit?)");
        }
    }

    /// <summary>The caller the hardening exposed: an Int128 significand reaches 39 digits, where the numerator pre-scale's
    /// headroom (38 - digits) is -1 and <c>AsWide(-1)</c> used to answer 1 while the quotient's exponent still counted
    /// the step, so 10^38 x 10^-35 (= 1000) over 10^20 came out ten times too large. Exact: 1000 / 10^20 = 10^-17.</summary>
    [Fact]
    public void CobolDecDiv_OfA39DigitNumerator_ScalesByTheIntendedPower()
    {
        var a = new CobolDec(Int128.Parse("100000000000000000000000000000000000000"), -35);
        var b = new CobolDec(Int128.Parse("100000000000000000000"), 0);
        Assert.Equal(0, CobolDec.Compare(CobolDec.Div(a, b, CobolRounding.NearestEven), new CobolDec(1, -17)));
    }

    /// <summary>The PB2633 repro, exactly: ACOS(0.5) = 1.0471975... truncated into PIC S9PP / PIC 9PP / PIC S9P is ZERO
    /// units (hundreds / tens), where the wrapped power made it -1 (= -100).</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(-11)]
    [InlineData(-31)]
    public void FromDoubleBounded_AcosAtATrailingPScale_LandsZeroUnits(int scale)
    {
        Assert.Equal(Int128.Zero, CobolIntrinsics.FromDoubleBounded(Math.Acos(0.5), scale, CobolRounding.Truncation, CobolIntrinsics.CodomainPi37));
        Assert.Equal(Int128.Zero, CobolIntrinsics.FromDoubleBounded(Math.Acos(-1.0), scale, CobolRounding.NearestAwayFromZero, CobolIntrinsics.CodomainPi37));
        Assert.Equal(Int128.Zero, CobolIntrinsics.FromDoubleBounded(Math.Asin(-1.0), scale, CobolRounding.NearestAwayFromZero, CobolIntrinsics.CodomainHalfPi37));
    }
}
