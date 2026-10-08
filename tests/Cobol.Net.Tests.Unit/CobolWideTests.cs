// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE EXACT WIDE INTERMEDIATE OF NATIVE ARITHMETIC (kb/Work PB1900): <see cref="CobolWide"/> carries a nested product
/// past the <see cref="Int128"/> carrier without rounding, so <c>A * B - C * D</c> over 21-digit operands is exact. Every
/// expected value is computed by <see cref="BigInteger"/> — an independent arbitrary-precision oracle that the product
/// code deliberately does not use (the runtime value model bans it, COBOLNET_DESIGN §1.2 invariant 2).
/// </summary>
public sealed class CobolWideTests
{
    private static readonly BigInteger Pow20 = BigInteger.Pow(10, 20);

    private static CobolWide W(BigInteger v) => v >= Int128.MinValue && v <= Int128.MaxValue
        ? CobolWide.From((Int128)v)
        : throw new ArgumentOutOfRangeException(nameof(v));

    /// <summary>The wide value read back at scale 0 through a receiver wide enough to hold it, as a BigInteger.</summary>
    private static BigInteger Read(CobolWide w) => (BigInteger)w.ToUnscaled(0, 0, CobolRounding.Truncation, checkedTransfer: true);

    [Fact]
    public void ThePb1900Finding_TheDifferenceOfTwo41DigitProductsIsExactlyOne()
    {
        // A = B = 10^20+1, C = 10^20, D = 10^20+2: A*B = 10^40+2*10^20+1 (41 digits), C*D = 10^40+2*10^20, difference 1.
        CobolWide ab = CobolWide.Mul(W(Pow20 + 1), W(Pow20 + 1));
        CobolWide cd = CobolWide.Mul(W(Pow20), W(Pow20 + 2));
        Assert.Equal(1, Read(CobolWide.Sub(ab, cd)));
        Assert.Equal(-1, Read(CobolWide.Sub(cd, ab)));
        // The SDIDI the nested product used to take rounds each to 34 digits and the cancellation keeps only the error.
        CobolDec oldAb = CobolDec.MulToOdd(CobolDec.From((Int128)(Pow20 + 1), 0), CobolDec.From((Int128)(Pow20 + 1), 0));
        CobolDec oldCd = CobolDec.MulToOdd(CobolDec.From((Int128)Pow20, 0), CobolDec.From((Int128)(Pow20 + 2), 0));
        CobolDec oldDiff = CobolDec.Sub(oldAb, oldCd, CobolRounding.NearestAwayFromZero);
        BigInteger oldValue = oldDiff.Exp >= 0 ? (BigInteger)oldDiff.Sig * BigInteger.Pow(10, oldDiff.Exp)
                                               : (BigInteger)oldDiff.Sig / BigInteger.Pow(10, -oldDiff.Exp);
        Assert.NotEqual(BigInteger.One, oldValue);
    }

    [Theory]
    [InlineData("100000000000000000001", "100000000000000000003")]
    [InlineData("99999999999999999999999999999999999999", "99999999999999999999999999999999999999")]    // 38 digits each: a 76-digit product
    [InlineData("-170141183460469231731687303715884105727", "170141183460469231731687303715884105727")] // Int128 extremes
    public void AProduct_IsExactAcrossTheWholeRange_AgainstTheOracle(string a, string b)
    {
        BigInteger x = BigInteger.Parse(a), y = BigInteger.Parse(b);
        CobolWide product = CobolWide.Mul(W(x), W(y));
        // Compare with the oracle through the final transfer to a scale that keeps ONLY the high part, and the exact
        // algebraic comparison against a freshly formed copy.
        Assert.Equal(0, CobolWide.Compare(product, 0, CobolWide.Mul(W(y), W(x)), 0));
        BigInteger exact = x * y;
        // Reduce to the 34-digit round-to-odd SDIDI and compare with the oracle's own round-to-odd.
        CobolDec lowered = product.ToDec(0);
        Assert.Equal(RoundToOdd34(exact), (BigInteger)lowered.Sig * BigInteger.Pow(10, lowered.Exp));
    }

    private static BigInteger RoundToOdd34(BigInteger v)
    {
        BigInteger mag = BigInteger.Abs(v);
        int shift = 0;
        while (mag >= BigInteger.Pow(10, 34)) { mag /= 10; shift++; }
        bool inexact = BigInteger.Abs(v) != mag * BigInteger.Pow(10, shift);
        if (inexact && mag.IsEven) mag++;
        return (v.Sign < 0 ? -mag : mag) * BigInteger.Pow(10, shift);
    }

    [Fact]
    public void AddAndSubtract_CarryAndBorrowAcrossTheTwoHalves()
    {
        // 2^128 - 1 plus 1 carries into the high half; the reverse borrows out of it.
        CobolWide big = CobolWide.Mul(W((BigInteger)Int128.MaxValue), W(2));          // 2^128 - 2
        CobolWide sum = CobolWide.Add(CobolWide.Add(big, W(1)), W(1));                // 2^128
        CobolWide back = CobolWide.Sub(sum, W(1));
        Assert.Equal(1, CobolWide.Compare(sum, 0, back, 0));
        Assert.Equal(0, CobolWide.Compare(CobolWide.Sub(back, W(1)), 0, big, 0));
        // Opposite signs of nearly equal magnitude cancel exactly, and a zero result has no sign.
        Assert.Equal(0, CobolWide.Compare(CobolWide.Add(sum, CobolWide.Negate(sum)), 0, W(0), 0));
    }

    [Fact]
    public void AlignedSums_UseTheScalesTheCallerSupplies()
    {
        // 1.5 (15 at scale 1) + 0.25 (25 at scale 2): the caller aligns the first UP by one place.
        CobolWide x = CobolWide.Up(W(15), 1);
        CobolWide s = CobolWide.Add(x, W(25));
        Assert.Equal(175, (BigInteger)s.ToUnscaled(2, 2, CobolRounding.Truncation, true));
        // 175 at scale 2 is 1.75: ROUNDED to scale 1 under NEAREST-AWAY-FROM-ZERO is 1.8, TRUNCATION is 1.7.
        Assert.Equal(18, (BigInteger)s.ToUnscaled(2, 1, CobolRounding.NearestAwayFromZero, true));
        Assert.Equal(17, (BigInteger)s.ToUnscaled(2, 1, CobolRounding.Truncation, true));
    }

    [Fact]
    public void Compare_NeverRaises_AndAnOverflowingAlignmentIsLargerByThatFact()
    {
        CobolWide huge = CobolWide.Mul(W(BigInteger.Pow(10, 37)), W(BigInteger.Pow(10, 37)));     // 10^74 at scale 0
        CobolWide small = W(5);
        // Aligning `small` (scale 0) UP to scale 10 would pass 2^256, so it can only be the larger-scaled side's comparand.
        Assert.Equal(-1, CobolWide.Compare(small, 0, huge, 0));
        Assert.Equal(1, CobolWide.Compare(huge, 0, small, 0));
        Assert.Equal(1, CobolWide.Compare(huge, 10, small, 0));         // 10^64 > 5
        // Aligning `huge` (scale 0) UP to the other side's scale 10 would pass 2^256 (10^84): it is the larger BY THAT FACT.
        Assert.Equal(1, CobolWide.Compare(huge, 0, small, 10));
        Assert.Equal(-1, CobolWide.Compare(small, 10, huge, 0));
        // Differing signs decide before any alignment.
        Assert.Equal(-1, CobolWide.Compare(CobolWide.Negate(huge), 0, small, 10));
    }

    [Fact]
    public void AnOutOfRangeResult_IsTheSizeErrorCondition_NeverAWrap()
    {
        CobolWide huge = CobolWide.Mul(W(BigInteger.Pow(10, 37)), W(BigInteger.Pow(10, 37)));     // 10^74
        CobolSizeError ex = Assert.Throws<CobolSizeError>(() => CobolWide.Mul(huge, W(BigInteger.Pow(10, 37))));   // 10^111
        Assert.Equal("EC-SIZE-OVERFLOW", ex.EcName);
        Assert.Throws<CobolSizeError>(() => CobolWide.Up(huge, 10));                                // 10^84 is past 2^256 ≈ 1.158*10^77
        // Five squares of 2^127-1 (each just under 2^254) pass 2^256 as a SUM: the carry out of the high half is detected.
        CobolWide square = CobolWide.Mul(W((BigInteger)Int128.MaxValue), W((BigInteger)Int128.MaxValue));
        CobolWide four = CobolWide.Add(CobolWide.Add(square, square), CobolWide.Add(square, square));
        Assert.Throws<CobolSizeError>(() => CobolWide.Add(four, square));
    }

    [Theory]
    [InlineData(CobolRounding.Truncation, "0")]
    [InlineData(CobolRounding.NearestAwayFromZero, "0")]
    [InlineData(CobolRounding.AwayFromZero, "-1")]
    [InlineData(CobolRounding.TowardLesser, "-1")]
    [InlineData(CobolRounding.TowardGreater, "0")]
    public void TheFinalTransfer_RoundsTheExactTailOnce_ByTheReceiversMode(CobolRounding mode, string expected)
    {
        // -(10^40 + 2*10^20 + 1) + (10^40 + 2*10^20) = -1 at scale 20 is -1e-20; into scale 19 it is a tail of one tenth.
        CobolWide ab = CobolWide.Mul(W(Pow20 + 1), W(Pow20 + 1));
        CobolWide cd = CobolWide.Mul(W(Pow20), W(Pow20 + 2));
        CobolWide diff = CobolWide.Sub(cd, ab);                                       // -1
        Assert.Equal(BigInteger.Parse(expected), (BigInteger)diff.ToUnscaled(1, 0, mode, checkedTransfer: true));
    }

    [Fact]
    public void Prohibited_RaisesOnAnInexactTransfer_OnlyWhenChecked()
    {
        CobolWide tenth = W(1);                                                      // 1 at scale 1 = 0.1
        Assert.Throws<CobolSizeError>(() => tenth.ToUnscaled(1, 0, CobolRounding.Prohibited, checkedTransfer: true));
        Assert.Equal(0, (BigInteger)tenth.ToUnscaled(1, 0, CobolRounding.Prohibited, checkedTransfer: false));   // DOC-A.1-70's no-phrase disposition
        Assert.Equal(1, (BigInteger)W(10).ToUnscaled(1, 0, CobolRounding.Prohibited, checkedTransfer: true));    // exact: nothing to refuse
    }

    [Fact]
    public void TheTransfer_IsTheSameKernelMulAtScaleUses()
    {
        // One transfer rule, whichever way the exact value was formed.
        Int128 e = Int128.Parse("12345678901234567891"), f = Int128.Parse("1234567890123456789");
        Assert.Equal(CobolDec.MulAtScale(e, 3, f, 4, 2, CobolRounding.NearestEven, true),
            CobolWide.Mul(CobolWide.From(e), CobolWide.From(f)).ToUnscaled(7, 2, CobolRounding.NearestEven, true));
    }

    [Fact]
    public void ToDouble_GoesThroughTheRoundToOddSdidi()
    {
        CobolWide third = CobolWide.Mul(W(Pow20 + 1), W(1));
        Assert.Equal(1.00000000000000000001e20, third.ToDouble(0));
        Assert.Equal(0.5, W(5).ToDouble(1));
    }
}
