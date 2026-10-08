// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A BINARY32 RECEIVER IS ROUNDED ONCE, NEVER THROUGH BINARY64 (kb/Work PB1110; ISO §14.6.8.3 rule 2: "in a
/// manner consistent with the specifications of ISO/IEC 60559:2020" — one correctly rounded conversion under
/// roundTiesToEven). The defect shape: a decimal a hair above a binary32 midpoint (by less than half a binary64
/// ulp) rounds ONTO the midpoint in binary64, and the <c>(float)</c> cast then ties to even, storing the LOWER
/// neighbour. <c>CobolFloat.ScaledToSingle</c> is the ONE scaled-value→binary32 conversion; every decimal-sender
/// landing into a binary32 item (MOVE, ACCEPT's device transfer, the SDIDI <c>CobolDec.ToSingle</c>) calls it.
/// <para>The oracle is CONSTRUCTED, not parsed: the midpoint between two adjacent binary32 values in [1, 2) is the
/// dyadic <c>(2m+1)/2^24</c>, exactly <c>(2m+1)·5^24 / 10^24</c> at scale 24, so the exact midpoint, one unit
/// above it and one unit below it have a known correctly rounded answer for every mantissa m — and a binary64
/// detour gets every one of the "one above" cases wrong.</para>
/// </summary>
public sealed class ScaledToSingleTests
{
    private static readonly Int128 FiveToTheTwentyFour = 59604644775390625L;   // 5^24

    /// <summary>The binary32 whose value is <c>mantissa × 2^-23</c> (a value in [1, 2) when
    /// 2^23 ≤ mantissa &lt; 2^24) — exact, both operands are binary32 values.</summary>
    private static float AtMantissa(long mantissa) => mantissa / 8388608f;

    /// <summary>Mantissas covering the binade's edges and a deterministic spread of even and odd ones.</summary>
    public static TheoryData<long> Mantissas()
    {
        var data = new TheoryData<long>();
        for (long m = 8388608; m < 16777216; m += 40503) data.Add(m);   // starts at 1.0 (even)
        data.Add(8388609);          // 1 + 2^-23 (odd)
        data.Add(16777214);         // the last two mantissas of the binade
        data.Add(16777215);
        return data;
    }

    [Fact]
    public void TheConstantFiveToTheTwentyFour_IsExact() =>
        Assert.Equal((Int128)System.Numerics.BigInteger.Pow(5, 24), FiveToTheTwentyFour);

    [Theory]
    [MemberData(nameof(Mantissas))]
    public void TheExactMidpoint_TiesToTheEvenMantissa(long m)
    {
        Int128 midpoint = (2 * (Int128)m + 1) * FiveToTheTwentyFour;
        long even = m % 2 == 0 ? m : m + 1;
        Assert.Equal(AtMantissa(even), CobolFloat.ScaledToSingle(midpoint, 24));
        Assert.Equal(-AtMantissa(even), CobolFloat.ScaledToSingle(-midpoint, 24));
    }

    [Theory]
    [MemberData(nameof(Mantissas))]
    public void OneUnitAboveTheMidpoint_RoundsUp_WhereABinary64DetourTiesDown(long m)
    {
        Int128 above = (2 * (Int128)m + 1) * FiveToTheTwentyFour + 1;
        Assert.Equal(AtMantissa(m + 1), CobolFloat.ScaledToSingle(above, 24));
        Assert.Equal(-AtMantissa(m + 1), CobolFloat.ScaledToSingle(-above, 24));
        // The bug this pins: the binary64 detour lands on the midpoint, and the cast ties to the even neighbour.
        float detour = (float)CobolFloat.ScaledToDouble(above, 24);
        Assert.Equal(AtMantissa(m % 2 == 0 ? m : m + 1), detour);
    }

    [Theory]
    [MemberData(nameof(Mantissas))]
    public void OneUnitBelowTheMidpoint_RoundsDown(long m)
    {
        Int128 below = (2 * (Int128)m + 1) * FiveToTheTwentyFour - 1;
        Assert.Equal(AtMantissa(m), CobolFloat.ScaledToSingle(below, 24));
        Assert.Equal(-AtMantissa(m), CobolFloat.ScaledToSingle(-below, 24));
    }

    /// <summary>The standard-decimal sender reaches the SAME conversion (<c>CobolDec.ToSingle</c>), and so does
    /// the MOVE landing that checks overflow on it.</summary>
    [Theory]
    [MemberData(nameof(Mantissas))]
    public void TheSdidiSender_AndItsCheckedLanding_AgreeWithTheScaledConversion(long m)
    {
        Int128 above = (2 * (Int128)m + 1) * FiveToTheTwentyFour + 1;
        var dec = CobolDec.From(above, 24);
        Assert.Equal(AtMantissa(m + 1), dec.ToSingle());
        Assert.Equal((double)AtMantissa(m + 1), CobolFloat.StoreChecked(dec, single: true));
    }

    /// <summary>The fast path (an exact binary32 divide) and the decimal-parse path agree at their shared edges:
    /// 2^24 and 10^10 are the largest exact operands, one past either bound takes the parse.</summary>
    [Theory]
    [InlineData("16777216", 0)]
    [InlineData("16777216", 10)]
    [InlineData("16777217", 10)]
    [InlineData("16777216", 11)]
    [InlineData("1", 10)]
    [InlineData("1", 11)]
    [InlineData("16777215", -10)]
    [InlineData("16777217", -10)]
    [InlineData("3", -11)]
    [InlineData("-16777217", 3)]
    [InlineData("100000001490116119384765625", 27)]      // 0.1f exactly: a binary32 must hold it unchanged
    public void TheFastAndSlowPaths_AgreeWithTheDecimalParse(string unscaled, int scale)
    {
        float oracle = float.Parse(unscaled + "E" + (-scale), NumberStyles.Float, CultureInfo.InvariantCulture);
        Assert.Equal(oracle, CobolFloat.ScaledToSingle(Int128.Parse(unscaled, CultureInfo.InvariantCulture), scale));
    }

    /// <summary>⛔ kb/Work PB2641, the binary32 twin: the signed 16-byte minimum -2^127 negated to itself and passed the
    /// "magnitude ≤ 2^24" test, so <c>(float)(int)unscaled</c> returned 0 at every scale in [-10, 10] — scale 0
    /// included (§14.6.8.3 GR2: one conversion consistent with ISO/IEC 60559).</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-11)]
    public void TheSignedMinimum_ConvertsToItsOwnMagnitude_NeverZero(int scale)
    {
        float actual = CobolFloat.ScaledToSingle(Int128.MinValue, scale);
        float oracle = float.Parse(Int128.MinValue.ToString(CultureInfo.InvariantCulture) + "E" + (-scale),
            NumberStyles.Float, CultureInfo.InvariantCulture);
        Assert.Equal(oracle, actual);
        Assert.True(actual < 0, $"scale {scale}: {actual:R}");
    }

    [Fact]
    public void OverflowIsInfinity_AndUnderflowIsZeroOrSubnormal()
    {
        Assert.True(float.IsPositiveInfinity(CobolFloat.ScaledToSingle(1, -39)));
        Assert.True(float.IsNegativeInfinity(CobolFloat.ScaledToSingle(-1, -39)));
        Assert.Equal(0f, CobolFloat.ScaledToSingle(0, 31));
        Assert.Equal(float.Epsilon, CobolFloat.ScaledToSingle(1, 45));          // 1E-45 rounds to the smallest subnormal
        Assert.Equal(0f, CobolFloat.ScaledToSingle(1, 46));                     // 1E-46 is closer to zero than half of it
    }

    /// <summary>NUMVAL-F's single-rounded value (the ACCEPT device transfer into a binary32 receiver) is the same
    /// conversion of the scan's exact significand and scale.</summary>
    [Theory]
    [InlineData("1.0000000596046448", 1.00000011920928955078125)]
    [InlineData("-1.0000000596046448", -1.00000011920928955078125)]
    [InlineData("1.0000000596046447", 1.0)]
    [InlineData("1.000000059604644775390625", 1.0)]                    // the exact midpoint ties to the even mantissa
    [InlineData("1.000000059604644775390625000001", 1.00000011920928955078125)]
    [InlineData("1.0000000596046448E+0", 1.00000011920928955078125)]
    [InlineData("10000000596046448E-16", 1.00000011920928955078125)]
    [InlineData("0.1", 0.100000001490116119384765625)]
    public void NumvalFSingle_IsOneRoundingOfTheExactText(string text, double expected) =>
        Assert.Equal(expected, CobolIntrinsics.NumvalFSingle(text));
}
