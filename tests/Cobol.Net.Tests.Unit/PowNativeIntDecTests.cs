// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB2632 - native integer exponentiation (<c>CobolIntrinsics.PowNativeIntDec</c>) answers a base of 0, 1 or -1
/// directly: ISO §8.8.1.2 r6a admits a zero base with any positive exponent and nothing bounds the exponent of 1 or -1,
/// so the old one-multiplication-per-unit-of-exponent loop never finished for <c>B ** 999999999999999999</c>. The
/// assertions below return at once; a regression is a test run that does not return, which the gate's test-host
/// timeout reports (a wall-clock assertion in the test itself is forbidden).
/// </summary>
public sealed class PowNativeIntDecTests
{
    private static BigInteger Value(CobolDec d) =>
        d.Exp >= 0 ? (BigInteger)d.Sig * BigInteger.Pow(10, d.Exp) : throw new InvalidOperationException($"not an integer: {d}");

    [Theory]
    [InlineData(1, 999999999999999999, 1)]
    [InlineData(1, 999999999999999998, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(0, 999999999999999999, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(-1, 999999999999999999, -1)]
    [InlineData(-1, 999999999999999998, 1)]
    [InlineData(-1, 0, 1)]
    [InlineData(-1, 1, -1)]
    public void ABaseOfZeroOneOrMinusOne_IsAnswered_WithoutLoopingOverTheExponent(long b, long e, long expected) =>
        Assert.Equal(new BigInteger(expected), Value(CobolIntrinsics.PowNativeIntDec(b, e)));

    [Fact]
    public void ABaseOfOneOrMinusOne_WithAnExponentBeyondLong_StillAnswersAtOnce()
    {
        Int128 e = Int128.Parse("99999999999999999999999999999999999999");
        Assert.Equal(BigInteger.One, Value(CobolIntrinsics.PowNativeIntDec(1, e)));
        Assert.Equal(BigInteger.MinusOne, Value(CobolIntrinsics.PowNativeIntDec(-1, e)));
        Assert.Equal(BigInteger.One, Value(CobolIntrinsics.PowNativeIntDec(-1, e - 1)));
    }

    /// <summary>The §8.8.1.2 r6a screen is unchanged: 0 ** 0 and a zero base with a negative exponent are
    /// EC-SIZE-EXPONENTIATION.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    public void AZeroBaseWithoutAPositiveExponent_RaisesTheExponentiationSizeError(long b, long e) =>
        Assert.Throws<CobolSizeError>(() => CobolIntrinsics.PowNativeIntDec(b, e));

    /// <summary>The exact loop for a larger base is unchanged, up to the last power the carrier holds
    /// (2^126) and past it, where the documented double approximation takes over.</summary>
    [Theory]
    [InlineData(2, 10)]
    [InlineData(-3, 3)]
    [InlineData(7, 40)]
    [InlineData(2, 126)]
    [InlineData(-2, 125)]
    [InlineData(10, 38)]
    public void ALargerBase_IsStillExact_WhileItFitsTheCarrier(long b, int e) =>
        Assert.Equal(BigInteger.Pow(b, e), Value(CobolIntrinsics.PowNativeIntDec(b, e)));

    [Fact]
    public void ALargerBase_PastTheCarrier_TakesTheDoubleApproximation()
    {
        double approx = CobolIntrinsics.PowNativeIntDec(2, 200).ToDouble();
        Assert.Equal(Math.Pow(2, 200), approx, precision: 0);
    }
}
