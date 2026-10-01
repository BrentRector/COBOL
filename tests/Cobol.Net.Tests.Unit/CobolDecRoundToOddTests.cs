// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A PRODUCT OR QUOTIENT REDUCED TO 34 DIGITS KEEPS THE FACT THAT IT WAS INEXACT (kb/Work PB1143).
/// <para>When the native lane forms a product past 38 scaled digits on the SDIDI (<c>NumericRenderer.Multiply</c>) the
/// receiver's ROUNDED mode rounds it ONCE — and has to see whether anything lay beyond the 34th digit.
/// <c>CobolDec.MulToOdd</c> / <c>DivToOdd</c> reduce by ROUND-TO-ODD: an inexact result keeps an odd last digit, so
/// rounding it to a coarser precision (a receiver holds ≤ 31 digit positions) in ANY mode equals rounding the exact
/// value. The rows below are products whose exact value has digits beyond the 34th, rounded at scale 30 by every mode
/// the §14.7.4.3 ROUNDED phrase offers; each expected value is the exact product rounded once, by Python
/// <c>fractions</c> / <c>decimal</c> — an independent implementation of the rounding rules.</para>
/// </summary>
public sealed class CobolDecRoundToOddTests
{
    // A = B = 1.000000000000000000000000000001 (a PIC 9V9(30) value: 31 digits, scaled 10^30).
    private static readonly CobolDec One1 = new(Int128.Parse("1000000000000000000000000000001"), -30);

    // The exact A × B = 1 + 2e-30 + 1e-60 = 1.000000000000000000000000000002000000000000000000000000000001
    // (61 digits).  Rounded once at scale 30 the integer significands (×10^30) are:
    //   TRUNCATION / NEAREST-*: 1000000000000000000000000000002   (the tail 1e-60 is below half)
    //   AWAY-FROM-ZERO / TOWARD-GREATER (positive): 1000000000000000000000000000003   (a nonzero tail goes UP)
    //   TOWARD-LESSER (positive): ...002
    [Theory]
    [InlineData(CobolRounding.Truncation, "1000000000000000000000000000002")]
    [InlineData(CobolRounding.NearestAwayFromZero, "1000000000000000000000000000002")]
    [InlineData(CobolRounding.NearestEven, "1000000000000000000000000000002")]
    [InlineData(CobolRounding.AwayFromZero, "1000000000000000000000000000003")]
    [InlineData(CobolRounding.TowardGreater, "1000000000000000000000000000003")]
    [InlineData(CobolRounding.TowardLesser, "1000000000000000000000000000002")]
    public void AProductWithATailPastTheThirtyFourthDigit_RoundsAsTheExactProductWould(CobolRounding mode, string expected) =>
        Assert.Equal(Int128.Parse(expected), CobolDec.MulToOdd(One1, One1).ToUnscaled(30, mode));

    [Fact]
    public void ThePlainTruncatingProduct_LostTheTail_WhichIsWhyTheOddFormExists()
    {
        // The same product through Mul(…, Truncation) is exactly …002000 at 34 digits, so AWAY-FROM-ZERO sees no tail.
        Assert.Equal(Int128.Parse("1000000000000000000000000000002"),
            CobolDec.Mul(One1, One1, CobolRounding.Truncation).ToUnscaled(30, CobolRounding.AwayFromZero));
    }

    [Fact]
    public void AnExactProduct_IsUnchanged_AndAProhibitedTransferOfAnInexactOneRaises()
    {
        var two = new CobolDec(2, 0);
        var half = new CobolDec(5, -1);
        Assert.Equal(0, CobolDec.Compare(new CobolDec(1, 0), CobolDec.MulToOdd(two, half)));            // exactly 1, no jam
        // 1 + 2e-30 + 1e-60 is not representable at scale 30: PROHIBITED (§14.7.4.3 r7) must see that.
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.MulToOdd(One1, One1).ToUnscaled(30, CobolRounding.Prohibited));
        Assert.Equal("EC-SIZE-TRUNCATION", ex.EcName);
        // …while the exactly-representable product 1.5 × 2.25 = 3.375 is accepted at scale 30 under PROHIBITED.
        var a = new CobolDec(15, -1);
        var b = new CobolDec(225, -2);
        Assert.Equal(Int128.Parse("3375000000000000000000000000000"), CobolDec.MulToOdd(a, b).ToUnscaled(30, CobolRounding.Prohibited));
    }

    [Theory]
    [InlineData(CobolRounding.Truncation, "3333333333333333333333333333333")]
    [InlineData(CobolRounding.NearestAwayFromZero, "3333333333333333333333333333333")]
    [InlineData(CobolRounding.AwayFromZero, "3333333333333333333333333333334")]
    public void AnInexactQuotient_RoundsAsTheExactQuotientWould(CobolRounding mode, string expected) =>
        // 10 ÷ 3 = 3.333…: at scale 30 the unscaled significand is 3333333333333333333333333333333 (31 digits) ± 1.
        Assert.Equal(Int128.Parse(expected), CobolDec.DivToOdd(new CobolDec(10, 0), new CobolDec(3, 0)).ToUnscaled(30, mode));
}
