// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE STANDARD-NUMERIC-TIME-FORM FLOOR IS ENFORCED ON EVERY CARRIER (kb/Work PB1379).
/// <para>§7.3.17.4 GR4/GR5: a standard numeric time form value "shall be greater than or equal to zero and less than
/// 86,401 / 86,400". The formatted-time family screens the LANDED (unscaled, scale) pair, and the float and SDIDI
/// landings used to truncate toward zero first — a COMP-2 <c>−1.0E−10</c> became 0 and was formatted as midnight.
/// <c>CobolDate.SecondsOfReal</c> / <c>SecondsOfDec</c> round toward negative infinity instead, so a negative value
/// lands negative and every bound is crossed by the landed value exactly when it is crossed by the exact one.</para>
/// </summary>
public sealed class CobolDateSecondsLandingTests
{
    [Theory]
    [InlineData(-1.0E-10)]
    [InlineData(-5e-324)]                    // the smallest negative subnormal
    [InlineData(-0.999999999)]
    [InlineData(double.NegativeInfinity)]
    public void ANegativeBinary64Value_LandsNegative(double seconds) =>
        Assert.True(CobolDate.SecondsOfReal(seconds) < 0);

    [Theory]
    [InlineData(0.0, 0L)]
    [InlineData(1.5, 1_500_000_000L)]
    [InlineData(86399.0, 86_399_000_000_000L)]
    [InlineData(18867.8125, 18_867_812_500_000L)]
    public void AValueInTheForm_LandsAsItsTruncation(double seconds, long unscaledAtScale9) =>
        Assert.Equal((Int128)unscaledAtScale9, CobolDate.SecondsOfReal(seconds));

    [Fact]
    public void NaNAndPositiveInfinity_LandOutsideTheForm()
    {
        Assert.True(CobolDate.SecondsOfReal(double.NaN) < 0);                   // neither bound holds for a NaN
        Assert.Equal(Int128.MaxValue, CobolDate.SecondsOfReal(double.PositiveInfinity));
    }

    [Fact]
    public void TheSdidiLanding_KeepsTheSignAndSaturatesPastTheCarrier()
    {
        Assert.True(CobolDate.SecondsOfDec(new CobolDec(-1, -30)) < 0);          // −1E−30 is below zero
        Assert.Equal((Int128)0, CobolDate.SecondsOfDec(new CobolDec(0, 0)));
        Assert.Equal((Int128)1_500_000_000_000_000_000, CobolDate.SecondsOfDec(new CobolDec(15, -1)));
        Assert.Equal(Int128.MaxValue, CobolDate.SecondsOfDec(new CobolDec(1, 40)));   // 10^40 seconds: out of the form, not a size error
        Assert.Equal(Int128.MinValue, CobolDate.SecondsOfDec(new CobolDec(-1, 40)));
    }

    /// <summary>End to end through the formatted-time body: the landed negative takes the §15.3 out-of-range path
    /// (the substituted zero-length result with checking off, the fatal EC-ARGUMENT-FUNCTION with it on) — under
    /// LEAP-SECOND OFF and ON alike — where it used to format as midnight.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATinyNegativeFloat_TakesTheOutOfRangePath(bool leapSecond)
    {
        Int128 landed = CobolDate.SecondsOfReal(-1.0E-10);
        Assert.Equal("", CobolDate.FormattedTime("hhmmss", landed, 9, 0, leapSecond));
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = true;
        try
        {
            Assert.Throws<CobolFatalException>(() => CobolDate.FormattedTime("hhmmss", landed, 9, 0, leapSecond));
            Assert.Throws<CobolFatalException>(() => CobolDate.FormattedDatetime("YYYYMMDDThhmmss", 1, landed, 9, 0, leapSecond));
        }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    /// <summary>§15.54.4 r2 — "a character-string containing hours, minutes, and seconds of the time specified by
    /// argument-1" (the leap second's seconds subfield is 60 under ON, §15.3.3.3): the ONE reading
    /// of whole seconds as a time of day makes the leap second (86 400 under ON) the time 23:59:60, and every other
    /// value the ordinary hh:mm:ss — shared by FORMATTED-TIME, FORMATTED-DATETIME and LOCALE-TIME-FROM-SECONDS.</summary>
    [Theory]
    [InlineData(0L, false, 0, 0, 0)]
    [InlineData(3661L, false, 1, 1, 1)]
    [InlineData(86399L, false, 23, 59, 59)]
    [InlineData(86399L, true, 23, 59, 59)]
    [InlineData(86400L, true, 23, 59, 60)]
    public void TimeOfDay_ReadsTheLeapSecondAs235960(long whole, bool leap, int hh, int mm, int ss) =>
        Assert.Equal((hh, mm, ss), CobolDate.TimeOfDay(whole, leap));
}
