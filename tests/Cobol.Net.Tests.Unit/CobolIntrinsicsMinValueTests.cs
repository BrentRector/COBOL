// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE INT128.MINVALUE MAGNITUDE SITES OF THE EXACT INTRINSIC FAMILY (kb/Work PB2698).
/// <para>A signed 16-byte binary item legally holds −2^127 (§13.18.60.4 GR12), and <c>-Int128.MinValue</c> wraps to
/// itself, so any "absolute value" written as a negated ternary returns a NEGATIVE number for it. PB2639 and PB2641
/// made the <c>CobolNum</c>, edit and float-conversion sites total over <see cref="CobolDec.UAbs"/>; this class pins
/// the intrinsic sites that landing left: FUNCTION ABS (whose result 2^127 is past the carrier: the size error), the
/// saturation test of the NUMVAL-F shift, and the quotient of a divisor of <see cref="Int128.MinValue"/> on the SDIDI
/// arm of <see cref="CobolNum.Divide"/> — plus the unsigned-wide twins of the unary arms (an unsigned 16-byte operand
/// above <see cref="Int128.MaxValue"/> reached them as a Roslyn CS1503).</para>
/// </summary>
public sealed class CobolIntrinsicsMinValueTests
{
    // ── ABS (§15.7.4): the magnitude of every Int128 value, MinValue included ───────────────────────────────────

    [Theory]
    [InlineData("0", "0")]
    [InlineData("-5", "5")]
    [InlineData("5", "5")]
    [InlineData("170141183460469231731687303715884105727", "170141183460469231731687303715884105727")]
    [InlineData("-170141183460469231731687303715884105727", "170141183460469231731687303715884105727")]
    public void AbsScaled_IsTheMagnitude_ForEveryValueWhoseMagnitudeFits(string value, string expected) =>
        Assert.Equal(Int128.Parse(expected), CobolIntrinsics.AbsScaled(Int128.Parse(value)));

    /// <summary>|−2^127| = 2^127 is past the native intermediate data item (DOC-A.1-123), so ABS raises the size
    /// error condition (§14.7.5 rule 5, range checked — DOC-A.1-179) and never returns the negative number
    /// <c>-v</c> wraps to. The pre-fix body returned <see cref="Int128.MinValue"/> itself.</summary>
    [Fact]
    public void AbsScaled_OfMinValue_IsTheSizeError_NotANegativeNumber()
    {
        var e = Assert.Throws<CobolSizeError>(() => CobolIntrinsics.AbsScaled(Int128.MinValue));
        Assert.Equal("EC-SIZE-OVERFLOW", e.EcName);
        Assert.Contains("FUNCTION ABS", e.Message);
    }

    // ── The unsigned-wide twins of the other unary arms (an unsigned 16-byte item above Int128.MaxValue) ───────

    [Theory]
    [InlineData(0UL, 0L)]
    [InlineData(1UL, 1L)]
    public void SignOfU_IsZeroOrPlusOne(ulong v, long expected) => Assert.Equal(expected, CobolIntrinsics.SignOfU(v));

    [Fact]
    public void SignOfU_AboveInt128Max_IsPlusOne_NotASizeError() =>
        Assert.Equal(1L, CobolIntrinsics.SignOfU(UInt128.MaxValue));

    [Fact]
    public void IntegerPartU_KeepsTheWholeContainerAtScaleZero() =>
        Assert.Equal(UInt128.MaxValue, CobolIntrinsics.IntegerPartU(UInt128.MaxValue, 0));

    [Fact]
    public void IntegerPartU_DropsTheFraction() =>
        Assert.Equal(UInt128.MaxValue / 100, CobolIntrinsics.IntegerPartU(UInt128.MaxValue, 2));

    [Fact]
    public void IntegerPartU_AtANegativeScale_MultipliesAndEscapesPastTheContainer()
    {
        Assert.Equal((UInt128)1200, CobolIntrinsics.IntegerPartU(12, -2));
        var e = Assert.Throws<CobolSizeError>(() => CobolIntrinsics.IntegerPartU(UInt128.MaxValue, -1));
        Assert.Equal("EC-SIZE-OVERFLOW", e.EcName);
    }

    [Fact]
    public void FractionPartU_IsTheDigitsBelowTheScale() =>
        Assert.Equal((Int128)55, CobolIntrinsics.FractionPartU(UInt128.MaxValue, 2));   // ...211455 mod 100

    [Fact]
    public void FractionPartU_AtAnIntegerScale_IsZero() =>
        Assert.Equal(Int128.Zero, CobolIntrinsics.FractionPartU(UInt128.MaxValue, 0));

    // ── INTEGER / INTEGER-PART on the carrier: the ×10^k of a negative scale escapes loud, never wraps ──────────

    [Fact]
    public void FloorAndTruncate_AtANegativeScale_RaiseTheSizeErrorPastTheCarrier()
    {
        Int128 big = Int128.MaxValue / 10 + 1;
        Assert.Equal(big * 10 - 10, CobolIntrinsics.Floor(big - 1, -1));       // still inside: exact
        Assert.Equal("EC-SIZE-OVERFLOW", Assert.Throws<CobolSizeError>(() => CobolIntrinsics.Floor(big, -1)).EcName);
        Assert.Equal("EC-SIZE-OVERFLOW", Assert.Throws<CobolSizeError>(() => CobolIntrinsics.Truncate(big, -1)).EcName);
    }

    // ── The NUMVAL-F shift (Rescaled) and the binary32 feed: the scan's magnitude is non-negative, so MinValue is
    //    unreachable — measured here at the largest significand the cap admits, so the claim is a test, not a hope ──

    private const string ThirtyFourNines = "9999999999999999999999999999999999";

    [Fact]
    public void NumvalF_AtTheLargestAdmittedSignificand_SaturatesAtAScaleThatLeavesTheCarrier()
    {
        // 10^34 − 1 shifted by +5 digits exceeds Int128: the CHECKED landing saturates, the sign applied after.
        Assert.Equal(Int128.MaxValue, CobolIntrinsics.NumvalF(ThirtyFourNines, 5, digitCap: 34, checkedLanding: true));
        Assert.Equal(-Int128.MaxValue, CobolIntrinsics.NumvalF("-" + ThirtyFourNines, 5, digitCap: 34, checkedLanding: true));
        Assert.Equal(Int128.Parse(ThirtyFourNines), CobolIntrinsics.NumvalF(ThirtyFourNines, 0, digitCap: 34, checkedLanding: true));
    }

    [Fact]
    public void NumvalFSingle_AtTheLargestAdmittedSignificand_IsTheFiniteBinary32() =>
        Assert.Equal(-1e34f, (float)CobolIntrinsics.NumvalFSingle("-" + ThirtyFourNines, digitCap: 34));

    // ── Native integer exponentiation (PowNativeIntDec): the base's magnitude is unsigned ───────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PowNativeIntDec_OfInt128MinValue_IsTheDocumentedApproximation_NotAnOverflowException(int exponent)
    {
        // |Int128.MinValue| = 2^127 > Int128.MaxValue, so every power leaves the carrier: the owner-decided double
        // approximation (2026-08-03), whose sign follows the base's and the exponent's parity. The pre-fix body
        // threw OverflowException from Int128.Abs(b) before reaching it.
        CobolDec p = CobolIntrinsics.PowNativeIntDec(Int128.MinValue, exponent);
        double expected = Math.Pow(-Math.Pow(2, 127), exponent);
        Assert.Equal(expected, p.ToDouble());
    }

    [Fact]
    public void PowNativeIntDec_StaysExactForTheLargestMagnitudeThatFits()
    {
        // (-2)^126 = 2^126 and 2^126 both fit; the exact loop keeps every digit and the sign by parity.
        Assert.Equal(CobolDec.From((Int128)1 << 126, 0), CobolIntrinsics.PowNativeIntDec(-2, 126));
        Assert.Equal(CobolDec.From(-((Int128)1 << 125), 0), CobolIntrinsics.PowNativeIntDec(-2, 125));
    }

    // ── CobolNum.Divide with a divisor of Int128.MinValue on the SDIDI arm (exp < 0, the divisor cannot be scaled) ──
    // a is at scale 4, the result at scale 0, so the alignment would multiply the divisor by 10^4 and leave the carrier.
    // |quotient| < 1e-30: every mode lands on the sign-dependent 0 or ±1 unit that §14.7.4.3 owes.

    [Theory]
    [InlineData(CobolRounding.Truncation, 12345, 0)]
    [InlineData(CobolRounding.NearestAwayFromZero, 12345, 0)]
    [InlineData(CobolRounding.NearestEven, 12345, 0)]
    [InlineData(CobolRounding.NearestTowardZero, 12345, 0)]
    [InlineData(CobolRounding.AwayFromZero, 12345, -1)]       // positive ÷ negative: a tiny negative, away from zero
    [InlineData(CobolRounding.TowardGreater, 12345, 0)]       // ceiling of a tiny negative is 0
    [InlineData(CobolRounding.TowardLesser, 12345, -1)]       // floor of a tiny negative is −1
    [InlineData(CobolRounding.Truncation, -12345, 0)]
    [InlineData(CobolRounding.NearestAwayFromZero, -12345, 0)]
    [InlineData(CobolRounding.AwayFromZero, -12345, 1)]       // negative ÷ negative: a tiny positive
    [InlineData(CobolRounding.TowardGreater, -12345, 1)]
    [InlineData(CobolRounding.TowardLesser, -12345, 0)]
    public void Divide_ByInt128MinValue_OnTheSdidiArm_RoundsTheTinyQuotientByItsSign(CobolRounding mode, int dividend, int expected) =>
        Assert.Equal((Int128)expected, CobolNum.Divide(dividend, 4, Int128.MinValue, 0, 0, mode));

}
