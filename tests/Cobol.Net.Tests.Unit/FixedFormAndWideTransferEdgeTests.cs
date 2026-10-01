// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The EDGES of the two review-finding fixes that <see cref="FixedFormRecordTests"/> and
/// <see cref="WideTransferKernelTests"/> do not reach (the test-gap review of kb/Work PB1562 / PB1143): a layout with
/// fixed material before the first and after the last dynamic member and a multi-character unit; the sticky-only tail of
/// a product (round digit zero, a nonzero digit further down) in every mode and sign; negative quotients and ties; and
/// the three ways a quotient or a product leaves the carrier. Every expected value is exact decimal arithmetic.
/// </summary>
public sealed class FixedFormAndWideTransferEdgeTests
{
    private static Int128 Big(string digits) => Int128.Parse(digits);

    [Fact]
    public void AMemberWithFixedMaterialOnBothSides_AndAMultiCharacterUnit_IsPaddedByItsUnitWidth()
    {
        // K · member (maximum 3 units of 2 characters) · XYZ: fixed run "KXYZ" (4), the component at fixed offset 1.
        var layout = new CobolContiguousLayout(4, [1], [2], [3L]);
        var extents = layout.ExtentsOf(new CobolVarGroup("KXYZ", ["abcd"]));       // two units written
        string image = "K" + "abcd" + "XYZ";
        string form = layout.ToFixedForm(image, extents);
        Assert.Equal("K" + "abcd" + "  " + "XYZ", form);                            // one more unit (2 characters) of padding
        Assert.Equal(4 + 3 * 2, form.Length);
        var read = layout.Decompose(form, null, fixedForm: true);
        Assert.Equal("abcd", read.Dyn(0));
        Assert.Equal("KXYZ", read.Fixed);
    }

    [Theory]
    [InlineData(CobolRounding.Truncation, 0)]
    [InlineData(CobolRounding.NearestAwayFromZero, 0)]
    [InlineData(CobolRounding.NearestEven, 0)]
    [InlineData(CobolRounding.AwayFromZero, 1)]
    [InlineData(CobolRounding.TowardGreater, 1)]
    [InlineData(CobolRounding.TowardLesser, 0)]
    public void AStickyOnlyTail_RoundsByItsSign_Positive(CobolRounding mode, int expected) =>
        // 1e-30 × 1e-30 = 1e-60 into scale 0: round digit 0, a nonzero digit far below.
        Assert.Equal(expected, (int)CobolDec.MulAtScale(1, 30, 1, 30, 0, mode, true));

    [Theory]
    [InlineData(CobolRounding.Truncation, 0)]
    [InlineData(CobolRounding.NearestEven, 0)]
    [InlineData(CobolRounding.AwayFromZero, -1)]
    [InlineData(CobolRounding.TowardGreater, 0)]
    [InlineData(CobolRounding.TowardLesser, -1)]
    public void AStickyOnlyTail_RoundsByItsSign_Negative(CobolRounding mode, int expected) =>
        Assert.Equal(expected, (int)CobolDec.MulAtScale(-1, 30, 1, 30, 0, mode, true));

    [Fact]
    public void AProductPastTheCarrier_AtAFinerScale_IsTheSizeError_Checked_AndTheLowOrderDigits_Unchecked()
    {
        Int128 g = Big("99999999999999999999");            // g × g × 100 is a 42-digit value
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.MulAtScale(g, 0, g, 0, 2, CobolRounding.Truncation, true));
        Assert.Equal("EC-SIZE-TRUNCATION", ex.EcName);
        Assert.Equal(Big("99999999999999980000000000000000000100"),
            CobolDec.MulAtScale(g, 0, g, 0, 2, CobolRounding.Truncation, false));
    }

    // −A / 4 with A = 12345678901234567890123456789012345678: −3086419725308641972530864197253086419.5, an exact tie.
    private static readonly Int128 A38 = Big("12345678901234567890123456789012345678");
    private static readonly Int128 Four = Big("4000000000000000000000000000000");

    [Theory]
    [InlineData(CobolRounding.Truncation, "-3086419725308641972530864197253086419")]
    [InlineData(CobolRounding.NearestEven, "-3086419725308641972530864197253086420")]
    [InlineData(CobolRounding.NearestTowardZero, "-3086419725308641972530864197253086419")]
    [InlineData(CobolRounding.NearestAwayFromZero, "-3086419725308641972530864197253086420")]
    [InlineData(CobolRounding.TowardGreater, "-3086419725308641972530864197253086419")]
    [InlineData(CobolRounding.TowardLesser, "-3086419725308641972530864197253086420")]
    public void ANegativeQuotientTie_RoundsByItsSign(CobolRounding mode, string expected) =>
        Assert.Equal(Big(expected), CobolDec.QuotientAtScale(-A38, Four, 30, mode));

    [Fact]
    public void ANegativeDivisor_FlipsTheSign_AndAnExactQuotientHasNoRemainder()
    {
        Assert.Equal(-A38, CobolDec.QuotientAtScale(A38, -Big("1000000000000000000000000000000"), 30, CobolRounding.Prohibited));
        Assert.False(CobolDec.QuotientHasRemainder(A38, Big("1000000000000000000000000000000"), 30));
        Assert.True(CobolDec.QuotientHasRemainder(A38, Four, 30));
        // An exponent past 10^38 is exact too (the old test answered "inexact" without looking): 1 / 0.1 at scale 31 → 10^32.
        Assert.False(CobolDec.QuotientHasRemainder(1, Big("1000000000000000000000000000000"), 62));
    }

    [Fact]
    public void AQuotientPastTheCarrier_IsTheSizeError_OnBothOfItsPaths()
    {
        // The numerator's high half reaches the divisor: the quotient is 2^128 or more.
        var wide = Assert.Throws<CobolSizeError>(() => CobolDec.QuotientAtScale(Int128.MaxValue, 1, 38, CobolRounding.Truncation));
        Assert.Equal("EC-SIZE-OVERFLOW", wide.EcName);
        // 1.2e38 × 10 / 5 = 2.4e38: below 2^128 but past Int128.MaxValue.
        var past = Assert.Throws<CobolSizeError>(() => CobolDec.QuotientAtScale(Big("120000000000000000000000000000000000000"), 5, 1, CobolRounding.Truncation));
        Assert.Equal("EC-SIZE-OVERFLOW", past.EcName);
    }
}
