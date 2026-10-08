// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The two exact lanes kb/Work PB2617 and PB2616 added, pinned at their kernels (the COBOL-level traces ride the
/// <c>pb2617_*</c> and <c>pb2616_*</c> goldens). Expected values are exact decimal arithmetic by hand.
/// <para><b>PB2617.</b> A scaled or SDIDI base raised to an integer exponent is the exact power (§8.8.1.3's native
/// method follows GnuCOBOL's <c>mpz_pow_ui</c>, CLAUDE.md rule 1): <c>CobolIntrinsics.PowNativeDec</c> at run time,
/// <c>CobolWide.Pow</c> for a compile-time literal exponent. <b>PB2616.</b> A directly rendered position is
/// evaluated in the host's <c>long</c> only when its digit bound proves it cannot wrap (§8.4.2.3.4 1) b)).</para>
/// </summary>
public sealed class NativeExactPowerAndPositionBoundTests
{
    // 1.1234567891 ** 2 = 1.26215515697488187881 (21 digits, scale 20).
    [Fact]
    public void FractionalBase_SquaredIsExact()
        => Assert.Equal(Int128.Parse("126215515697488187881"),
            CobolIntrinsics.PowNativeDec(CobolDec.From(11234567891, 10), 2).ToUnscaledIntermediate(20, CobolRounding.Truncation));

    // 2.0 ** -2 = 0.25 — §8.8.1.5.4 r3's reciprocal of the exact 4.00.
    [Fact]
    public void FractionalBase_NegativeExponentIsTheReciprocal()
        => Assert.Equal(25000, CobolIntrinsics.PowNativeDec(CobolDec.From(20, 1), -2).ToUnscaledIntermediate(5, CobolRounding.Truncation));

    // (−1.5) ** 3 = −3.375 and (−1.5) ** 2 = 2.25: the sign follows the exponent's parity.
    [Theory]
    [InlineData(3, -3375)]
    [InlineData(2, 2250)]
    public void NegativeFractionalBase_SignFollowsParity(int exponent, int expectedAtScale3)
        => Assert.Equal(expectedAtScale3,
            CobolIntrinsics.PowNativeDec(CobolDec.From(-15, 1), exponent).ToUnscaledIntermediate(3, CobolRounding.Truncation));

    // A power past the 256-bit lane (1.1234567891 ** 8 has 88 digits) is the round-to-odd chain: 2.53776255119685728762
    // to 20 places, the exact value truncated.
    [Fact]
    public void FractionalBase_PastTheWideLane_IsTheRoundToOddChain()
        => Assert.Equal(Int128.Parse("253776255119685728762"),
            CobolIntrinsics.PowNativeDec(CobolDec.From(11234567891, 10), 8).ToUnscaledIntermediate(20, CobolRounding.Truncation));

    // §8.8.1.2 rule 6 a): a zero base needs an exponent greater than zero.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ZeroBase_NonPositiveExponent_IsExponentiationSizeError(int exponent)
        => Assert.Throws<CobolSizeError>(() => CobolIntrinsics.PowNativeDec(CobolDec.From(0, 1), exponent));

    // 10^77 < 2^256 fits the wide lane; 10^78 does not.
    [Fact]
    public void WidePow_FitsThroughSeventySevenDigits()
    {
        Assert.True(CobolWide.TryPow(CobolWide.From(10), 77, out _));
        Assert.False(CobolWide.TryPow(CobolWide.From(10), 78, out _));
    }

    // 3037000499² = 9223372030926249001 fits the long; 3037000500² = 9223372037000250000 does not.
    [Theory]
    [InlineData(3037000499L, false)]
    [InlineData(3037000500L, true)]
    public void ProductOfConstants_ExceedsLongExactlyPastItsRange(long factor, bool exceeds)
        => Assert.Equal(exceeds, new PositionBinary(new PositionConstant(factor), PositionOperator.Multiply, new PositionConstant(factor)).ExceedsLongArithmetic);

    // 2^62 + 2^62 = 2^63 is one past long.MaxValue; 2^62 + (2^62 − 1) is exactly long.MaxValue.
    [Theory]
    [InlineData(4611686018427387904L, true)]
    [InlineData(4611686018427387903L, false)]
    public void SumOfConstants_ExceedsLongExactlyPastItsRange(long second, bool exceeds)
        => Assert.Equal(exceeds, new PositionBinary(new PositionConstant(4611686018427387904L), PositionOperator.Add, new PositionConstant(second)).ExceedsLongArithmetic);

    // An operator node answers for itself and for everything beneath it; a bare leaf and a narrow expression do not exceed.
    [Fact]
    public void NestedOperator_ReportsAnExceedingDescendant_AndNarrowShapesStayOnTheLongPath()
    {
        var wide = new PositionBinary(new PositionConstant(3037000500L), PositionOperator.Multiply, new PositionConstant(3037000500L));
        var small = new PositionBinary(new PositionConstant(1), PositionOperator.Subtract, new PositionConstant(2));
        Assert.True(new PositionNegate(new PositionGroup(new PositionBinary(small, PositionOperator.Add, wide))).ExceedsLongArithmetic);
        Assert.False(new PositionGroup(small).ExceedsLongArithmetic);
        Assert.False(new PositionConstant(long.MinValue).ExceedsLongArithmetic);
        Assert.False(new PositionLocal("k").ExceedsLongArithmetic);
    }
}
