// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB2630 - the SDIDI INTEGER (ISO §15.44.4 r1), INTEGER-PART (§15.49.4 r1) and FRACTION-PART (§15.42.4 r1)
/// are computed ON THE DECIMAL: <c>CobolDec.FloorToInteger</c> / <c>TruncateToInteger</c> never land the value on the
/// Int128 carrier, so a magnitude of 10^38 or more keeps every digit. The unchecked final-transfer arm they used
/// kept only the low-order digits (INTEGER(10^40) answered 0, INTEGER(-10^40) answered -1).
/// </summary>
/// <remarks>Every expected value is computed with BigInteger on the definition (floor division of the exact value),
/// never observed.</remarks>
public sealed class CobolDecIntegerPartTests
{
    /// <summary>(significand, exponent) shapes that straddle every branch: past the carrier, at it, integers with
    /// trailing fraction digits, fractions of both signs, and exponents deeper than the carrier's 38 digits.</summary>
    public static TheoryData<string, int> Shapes => new()
    {
        { "1", 40 },
        { "-1", 40 },
        { "1234567890123456789012345678901234", 7 },
        { "-1234567890123456789012345678901234", 7 },
        { "1234567890123456789012345678901234", 0 },
        { "1234567890123456789012345678901234", -4 },
        { "-1234567890123456789012345678901234", -4 },
        { "1234567890123456789012345678901234", -33 },
        { "-1234567890123456789012345678901234", -33 },
        { "1234567890123456789012345678901234", -34 },
        { "-1234567890123456789012345678901234", -34 },
        { "1234567890123456789012345678901234", -45 },
        { "-1234567890123456789012345678901234", -45 },
        { "275", -2 },
        { "-275", -2 },
        { "200", -2 },
        { "-200", -2 },
        { "5", -1 },
        { "-5", -1 },
        { "0", -3 },
        { "0", 5 },
    };

    private static (BigInteger Floor, BigInteger Trunc) Expected(BigInteger sig, int exp)
    {
        if (exp >= 0) { BigInteger v = sig * BigInteger.Pow(10, exp); return (v, v); }
        BigInteger den = BigInteger.Pow(10, -exp);
        BigInteger q = BigInteger.DivRem(sig, den, out BigInteger rem);   // truncates toward zero
        BigInteger floor = sig < 0 && !rem.IsZero ? q - 1 : q;
        return (floor, q);
    }

    private static BigInteger ValueOf(CobolDec d) =>
        d.Exp >= 0 ? (BigInteger)d.Sig * BigInteger.Pow(10, d.Exp) : throw new InvalidOperationException($"not an integer shape: {d}");

    [Theory]
    [MemberData(nameof(Shapes))]
    public void FloorAndTruncateToInteger_AreExact_AtEveryMagnitude(string sig, int exp)
    {
        BigInteger s = BigInteger.Parse(sig);
        var d = new CobolDec((Int128)s, exp);
        var (floor, trunc) = Expected(s, exp);
        CobolDec f = d.FloorToInteger(), t = d.TruncateToInteger();
        Assert.True(f.Exp >= 0 && t.Exp >= 0, "the integer part has a non-negative exponent");
        Assert.Equal(floor, ValueOf(f));
        Assert.Equal(trunc, ValueOf(t));
        Assert.Equal(floor, ValueOf(CobolIntrinsics.FloorDec(d)));
        Assert.Equal(trunc, ValueOf(CobolIntrinsics.TruncDec(d)));
    }

    /// <summary>FRACTION-PART is argument - INTEGER-PART(argument): zero for a value with no fraction digits at any
    /// magnitude (10^40 was 10^40), and the signed fraction otherwise.</summary>
    [Theory]
    [InlineData("1", 40)]
    [InlineData("-1", 40)]
    [InlineData("1234567890123456789012345678901234", 7)]
    public void FractionPart_OfAnIntegerBeyondTheCarrier_IsZero(string sig, int exp) =>
        Assert.Equal(0, CobolDec.Compare(CobolIntrinsics.FractionPartDec(CobolRounding.NearestEven, new CobolDec((Int128)BigInteger.Parse(sig), exp)), new CobolDec(0, 0)));

    [Fact]
    public void FractionPart_KeepsTheSignedFraction()
    {
        CobolDec r = CobolIntrinsics.FractionPartDec(CobolRounding.NearestEven, new CobolDec(-275, -2));
        Assert.Equal(0, CobolDec.Compare(r, new CobolDec(-75, -2)));
    }

    /// <summary>MOD/REM on past-the-carrier operands ride the same two bodies: the §15.64.4 / §15.77.4 equivalent
    /// arithmetic expressions with the 34-digit quotient. 10^40 / 3 = 3.333...3E+39 (34 digits), times 3 is
    /// 9.999...9E+39 and 10^40 less that is 10^6; both quotients are positive integers, so MOD and REM agree.</summary>
    [Fact]
    public void ModAndRem_OfAQuotientBeyondTheCarrier_FollowTheEquivalentExpression()
    {
        var ten40 = new CobolDec(1, 40);
        var three = new CobolDec(3, 0);
        var million = new CobolDec(1, 6);
        Assert.Equal(0, CobolDec.Compare(CobolIntrinsics.ModDec(CobolRounding.NearestEven, ten40, three), million));
        Assert.Equal(0, CobolDec.Compare(CobolIntrinsics.RemDec(CobolRounding.NearestEven, ten40, three), million));
    }

    /// <summary>The compile-time §7.3.6.3 GR3 truncation reads this same method (it was a second copy of the rule).</summary>
    [Fact]
    public void TruncateToInteger_DropsTheFractionDigitsOfANegativeValue()
    {
        var d = new CobolDec((Int128)BigInteger.Parse("-1234567890123456789012345678901234"), -4);
        Assert.Equal(ValueOf(d.TruncateToInteger()), BigInteger.Parse("-123456789012345678901234567890"));
    }
}
