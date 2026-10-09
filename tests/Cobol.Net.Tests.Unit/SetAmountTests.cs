// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <see cref="SetAmount.Land(Int128, int, out Int128)"/>, the exact-lane landing every SET-family amount and the
/// START … WITH LENGTH count go through, over the whole SIGN of the scale. A positive scale is a fraction (the
/// divisibility test is the integrality test); a NEGATIVE scale is a trailing-P amount whose VALUE is the stored
/// digits × 10^|scale| (ISO §13.18.40.4 GR14: "The symbol 'P' specifies the location of an assumed decimal point when
/// that point is not within the number that appears in the data item"). kb/Work PB2697: the negative half used to
/// hand back the stored digits, so <c>SET IX UP BY N</c> with <c>N PIC 9PP VALUE 100</c> moved the index by 1.
/// </summary>
public sealed class SetAmountTests
{
    /// <summary>Each row is (stored digits, scale, the VALUE as an integer). The trailing-P rows are the defect's
    /// half; the positive-scale and zero-scale rows pin that the existing lanes did not move.</summary>
    [Theory]
    [InlineData(1L, -2, 100L)]          // PIC 9PP holding 100
    [InlineData(-7L, -1, -70L)]         // PIC S9P holding -70
    [InlineData(0L, -60, 0L)]           // zero at a scale past Pow10's table is still zero, never a throw
    [InlineData(42L, 0, 42L)]
    [InlineData(200L, 2, 2L)]           // 2.00 IS an integer value
    public void AnIntegerValue_LandsAsThatValue(long stored, int scale, long value)
    {
        Assert.Equal(SetAmountLanding.Integer, SetAmount.Land(stored, scale, out Int128 whole));
        Assert.Equal((Int128)value, whole);
    }

    [Fact]
    public void AFraction_IsNotAnInteger()
        => Assert.Equal(SetAmountLanding.NotAnInteger, SetAmount.Land(25, 1, out _));

    /// <summary>A trailing-P widening the <see cref="Int128"/> carrier cannot form IS an integer, so it is never
    /// <see cref="SetAmountLanding.NotAnInteger"/>; it is <see cref="SetAmountLanding.BeyondCarrier"/>, each format's
    /// RANGE leg — decided before the multiply, so the product never wraps back into a plausible value.</summary>
    [Theory]
    [InlineData(1L, -39)]               // 10^39 > Int128.MaxValue ≈ 1.7 × 10^38
    [InlineData(2L, -38)]               // 2 × 10^38 > Int128.MaxValue
    [InlineData(-2L, -38)]
    [InlineData(1L, -60)]               // past Pow10's table: a nonzero value cannot be formed at all
    public void ATrailingPValuePastTheCarrier_IsBeyondCarrier(long stored, int scale)
        => Assert.Equal(SetAmountLanding.BeyondCarrier, SetAmount.Land(stored, scale, out _));

    [Fact]
    public void TheLargestTrailingPValueTheCarrierHolds_StillLands()
    {
        Assert.Equal(SetAmountLanding.Integer, SetAmount.Land(1, -38, out Int128 whole));
        Assert.Equal(Int128.Parse("100000000000000000000000000000000000000"), whole);
    }
}
