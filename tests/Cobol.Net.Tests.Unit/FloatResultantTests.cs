// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The float arm of the arithmetic store (kb/Work PB1196) and the checked binary64 intermediate (kb/Work PB1147,
/// PB1581). Every expected value is derived from the rule and the binary formats' grids, never from a run:
/// §14.7.4.3 r3–r10 pick between the two representable neighbors of an inexact value (TRUNCATION the one nearer
/// zero, TOWARD-GREATER the larger, the NEAREST-* modes the nearer with their own tie rule), r7 makes PROHIBITED an
/// exactness test, and §14.7.5 case 3 makes a value past the format's largest finite magnitude — after the mode's
/// rounding — the size error condition.
/// </summary>
public sealed class FloatResultantTests
{
    private const bool Single = true, Binary64 = false;

    // 0.1f is 0.100000001490116…, ABOVE 0.1; its predecessor 0.0999999940395… is below.
    private static readonly double TenthUp32 = 0.1f, TenthDown32 = MathF.BitDecrement(0.1f);
    // The binary64 0.1 is 0.1000000000000000055…, ABOVE 0.1.
    private static readonly double TenthDown64 = Math.BitDecrement(0.1);

    [Theory]
    [InlineData(CobolRounding.Truncation, false)]           // r10 — nearer to zero
    [InlineData(CobolRounding.TowardLesser, false)]         // r9  — the smaller
    [InlineData(CobolRounding.TowardGreater, true)]         // r8  — the larger
    [InlineData(CobolRounding.AwayFromZero, true)]          // r3  — farther from zero
    [InlineData(CobolRounding.NearestEven, true)]           // r5  — 0.1f is the nearer (no tie)
    public void Binary32_TenthTakesTheModesNeighbor(CobolRounding mode, bool up)
    {
        double want = up ? TenthUp32 : TenthDown32;
        Assert.Equal(want, FloatResultant.FromScaled(1, 1, mode, Single));
        Assert.Equal(want, FloatResultant.FromDec(new CobolDec(1, -1), mode, Single));
        Assert.Equal(want, FloatResultant.FromReal(0.1, mode, Single));   // the binary64 0.1 has the same neighbors
        Assert.Equal(-(up ? TenthUp32 : TenthDown32),                      // the magnitude rule, mirrored
            FloatResultant.FromScaled(-1, 1, mode switch
            {
                CobolRounding.TowardLesser => CobolRounding.TowardGreater,
                CobolRounding.TowardGreater => CobolRounding.TowardLesser,
                _ => mode,
            }, Single));
    }

    [Theory]
    // 16777217 = 2^24 + 1 is the exact midpoint of 16777216 (significand even) and 16777218 (odd).
    [InlineData(16777217L, CobolRounding.NearestAwayFromZero, 16777218.0)]   // r4 — tie away from zero
    [InlineData(16777217L, CobolRounding.NearestTowardZero, 16777216.0)]     // r6 — tie toward zero
    [InlineData(16777217L, CobolRounding.NearestEven, 16777216.0)]           // r5 — tie to the even significand
    [InlineData(16777217L, CobolRounding.Truncation, 16777216.0)]
    [InlineData(16777217L, CobolRounding.AwayFromZero, 16777218.0)]
    // 16777219 is the midpoint of 16777218 (odd significand) and 16777220 (even).
    [InlineData(16777219L, CobolRounding.NearestEven, 16777220.0)]
    [InlineData(16777219L, CobolRounding.NearestTowardZero, 16777218.0)]
    [InlineData(16777219L, CobolRounding.NearestAwayFromZero, 16777220.0)]
    public void Binary32_TiesFollowTheModesTieRule(long value, CobolRounding mode, double want) =>
        Assert.Equal(want, FloatResultant.FromScaled(value, 0, mode, Single));

    [Fact]
    public void Binary32_IsOneRounding_NeverThroughBinary64First()
    {
        // 1.0000000596046448 lies 2.46e-17 ABOVE the binary32 midpoint 1 + 2^-24 — below half a binary64 ulp, so a
        // binary64 conversion lands ON the midpoint and a second rounding ties to even (1.0). One rounding: 1 + 2^-23.
        Assert.Equal(1 + Math.ScaleB(1, -23), FloatResultant.FromScaled(10000000596046448, 16, CobolRounding.NearestEven, Single));
        Assert.Equal(1 + Math.ScaleB(1, -23), FloatResultant.FromScaled(10000000596046448, 16, CobolRounding.NearestTowardZero, Single));
    }

    [Fact]
    public void Prohibited_IsAnExactnessTest()
    {
        Assert.False(FloatResultant.TryFromScaled(16777217, 0, CobolRounding.Prohibited, Single, out _));
        Assert.True(FloatResultant.TryFromScaled(16777216, 0, CobolRounding.Prohibited, Single, out double exact));
        Assert.Equal(16777216.0, exact);
        Assert.False(FloatResultant.TryFromScaled(1, 1, CobolRounding.Prohibited, Binary64, out _));   // 0.1
        Assert.True(FloatResultant.TryFromScaled(5, 1, CobolRounding.Prohibited, Binary64, out double half));
        Assert.Equal(0.5, half);
        Assert.True(FloatResultant.TryFromReal(0.1, CobolRounding.Prohibited, Binary64, out _));     // a binary64 IS one
        Assert.False(FloatResultant.TryFromReal(0.1, CobolRounding.Prohibited, Single, out _));
        // Unchecked, the inexact PROHIBITED value lands TRUNCATED (CONFORMANCE.md DOC-A.1-70).
        Assert.Equal(TenthDown32, FloatResultant.FromScaled(1, 1, CobolRounding.Prohibited, Single));
        // Checked, every other mode commits an inexact value.
        Assert.True(FloatResultant.TryFromScaled(1, 1, CobolRounding.Truncation, Single, out double t));
        Assert.Equal(TenthDown32, t);
    }

    [Fact]
    public void Binary64_ExactValueTakesTheModesNeighbor()
    {
        Assert.Equal(TenthDown64, FloatResultant.FromScaled(1, 1, CobolRounding.Truncation, Binary64));
        Assert.Equal(0.1, FloatResultant.FromScaled(1, 1, CobolRounding.TowardGreater, Binary64));
        Assert.Equal(0.1, FloatResultant.FromScaled(1, 1, CobolRounding.NearestEven, Binary64));
        Assert.Equal(-TenthDown64, FloatResultant.FromScaled(-1, 1, CobolRounding.TowardGreater, Binary64));
        Assert.Equal(-0.1, FloatResultant.FromScaled(-1, 1, CobolRounding.TowardLesser, Binary64));
        // A binary64 intermediate is representable in a binary64 resultant under every mode.
        Assert.Equal(1.0 / 3.0, FloatResultant.FromReal(1.0 / 3.0, CobolRounding.TowardGreater, Binary64));
    }

    [Theory]
    // 2^53 + 1 is the midpoint of 2^53 (even) and 2^53 + 2 — the same value spelled at scale 0 and scale 1 (the
    // 5^scale cofactor must cancel for a decimal to be a binary midpoint at all).
    [InlineData(9007199254740993L, 0, CobolRounding.NearestEven, 9007199254740992.0)]
    [InlineData(9007199254740993L, 0, CobolRounding.NearestAwayFromZero, 9007199254740994.0)]
    [InlineData(9007199254740993L, 0, CobolRounding.NearestTowardZero, 9007199254740992.0)]
    [InlineData(90071992547409930L, 1, CobolRounding.NearestAwayFromZero, 9007199254740994.0)]
    [InlineData(90071992547409930L, 1, CobolRounding.NearestTowardZero, 9007199254740992.0)]
    [InlineData(9007199254740993L, 0, CobolRounding.Truncation, 9007199254740992.0)]
    [InlineData(9007199254740993L, 0, CobolRounding.AwayFromZero, 9007199254740994.0)]
    // 2^53 + 1.1 is NOT a tie: every nearest mode takes the nearer 2^53 + 2.
    [InlineData(90071992547409931L, 1, CobolRounding.NearestTowardZero, 9007199254740994.0)]
    [InlineData(90071992547409931L, 1, CobolRounding.NearestEven, 9007199254740994.0)]
    public void Binary64_TiesAreExact(long unscaled, int scale, CobolRounding mode, double want) =>
        Assert.Equal(want, FloatResultant.FromScaled(unscaled, scale, mode, Binary64));

    [Fact]
    public void Overflow_IsAfterTheModesRounding()
    {
        // 4E+38 is past binary32's 2^128: every mode overflows; unchecked lands ISO/IEC 60559's result.
        Assert.False(FloatResultant.TryFromScaled(4, -38, CobolRounding.Truncation, Single, out _));
        Assert.Equal(float.MaxValue, FloatResultant.FromScaled(4, -38, CobolRounding.Truncation, Single));
        Assert.Equal(double.PositiveInfinity, FloatResultant.FromScaled(4, -38, CobolRounding.NearestEven, Single));
        // float.MaxValue + 1 is below 2^128: TRUNCATION takes MaxValue (fits), AWAY-FROM-ZERO takes 2^128 (overflow).
        UInt128 justPast = UInt128.Parse("340282346638528859811704183484516925441");
        Assert.True(FloatResultant.TryFromUnsignedScaled(justPast, 0, CobolRounding.Truncation, Single, out double max));
        Assert.Equal(float.MaxValue, max);
        Assert.False(FloatResultant.TryFromUnsignedScaled(justPast, 0, CobolRounding.AwayFromZero, Single, out _));
        // Binary64: 1.79769313486231585E+308 is past MaxValue + ½ulp but below 2^1024.
        var nearTop = new CobolDec(Int128.Parse("179769313486231585"), 291);
        Assert.True(FloatResultant.TryFromDec(nearTop, CobolRounding.Truncation, Binary64, out double m64));
        Assert.Equal(double.MaxValue, m64);
        Assert.False(FloatResultant.TryFromDec(nearTop, CobolRounding.NearestEven, Binary64, out _));
        Assert.False(FloatResultant.TryFromDec(new CobolDec(2, 308), CobolRounding.Truncation, Binary64, out _));
        Assert.Equal(-double.MaxValue, FloatResultant.FromDec(new CobolDec(-2, 308), CobolRounding.Truncation, Binary64));
    }

    [Fact]
    public void UnsignedWide_PastTheSignedCarrier_ComparesInHalves()
    {
        // 2^128 − 1 (an unsigned 16-byte COMP-5 at its container maximum): its binary64 neighbours are 2^128 − 2^75
        // and 2^128, and it is 1 below the latter — the nearest is 2^128, TRUNCATION the one below.
        double top = Math.ScaleB(1, 128);
        Assert.Equal(top, FloatResultant.FromUnsignedScaled(UInt128.MaxValue, 0, CobolRounding.NearestEven, Binary64));
        Assert.Equal(Math.BitDecrement(top), FloatResultant.FromUnsignedScaled(UInt128.MaxValue, 0, CobolRounding.Truncation, Binary64));
        // Into binary32 it lies between MaxValue and 2^128 and nearer the latter: overflow unless the mode truncates.
        Assert.Equal(float.MaxValue, FloatResultant.FromUnsignedScaled(UInt128.MaxValue, 0, CobolRounding.Truncation, Single));
        Assert.False(FloatResultant.TryFromUnsignedScaled(UInt128.MaxValue, 0, CobolRounding.NearestEven, Single, out _));
    }

    [Fact]
    public void BelowTheSubnormals_TheModesStillChoose()
    {
        Assert.Equal(0.0, FloatResultant.FromScaled(1, 50, CobolRounding.Truncation, Single));
        Assert.Equal(float.Epsilon, FloatResultant.FromScaled(1, 50, CobolRounding.AwayFromZero, Single));
        Assert.Equal(0.0, FloatResultant.FromScaled(1, 50, CobolRounding.NearestEven, Single));
        Assert.False(FloatResultant.TryFromScaled(1, 50, CobolRounding.Prohibited, Single, out _));
        Assert.Equal(double.Epsilon, FloatResultant.FromDec(new CobolDec(1, -400), CobolRounding.TowardGreater, Binary64));
    }

    [Fact]
    public void NonFiniteValues_TransferUnchanged()
    {
        Assert.True(FloatResultant.TryFromReal(double.PositiveInfinity, CobolRounding.Prohibited, Single, out double inf));
        Assert.Equal(double.PositiveInfinity, inf);
        Assert.True(double.IsNaN(FloatResultant.FromReal(double.NaN, CobolRounding.Truncation, Single)));
    }

    // ── The checked binary64 intermediate (§14.7.5 cases 2 and 5) ─────────────────────────────────────────────

    private static string Ec(Action body) => Assert.Throws<CobolSizeError>(body).EcName;

    [Fact]
    public void CheckedOperations_RaiseTheTable13Condition()
    {
        Assert.Equal("EC-SIZE-ZERO-DIVIDE", Ec(() => CobolFloat.DivChecked(1, 0)));
        Assert.Equal("EC-SIZE-ZERO-DIVIDE", Ec(() => CobolFloat.DivChecked(1, -0.0)));
        Assert.Equal("EC-SIZE-ZERO-DIVIDE", Ec(() => CobolFloat.DivChecked(double.NaN, 0)));
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolFloat.MulChecked(1e300, 1e300)));
        Assert.Equal("EC-SIZE-UNDERFLOW", Ec(() => CobolFloat.MulChecked(1e-300, 1e-300)));
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolFloat.AddChecked(double.MaxValue, double.MaxValue)));
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolFloat.SubChecked(double.MaxValue, -double.MaxValue)));
        Assert.Equal("EC-SIZE-UNDERFLOW", Ec(() => CobolFloat.DivChecked(double.Epsilon, 2)));   // 2^-1075 → 0
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolIntrinsics.PowNativeRealChecked(1e300, 2)));
        Assert.Equal("EC-SIZE-UNDERFLOW", Ec(() => CobolIntrinsics.PowNativeRealChecked(1e-300, 2)));
        // In range, a zero operand, a subnormal result, and a non-finite operand are not the condition.
        Assert.Equal(0.0, CobolFloat.MulChecked(0, 1e-300));
        Assert.Equal(double.Epsilon, CobolFloat.DivChecked(double.Epsilon, 1));
        Assert.Equal(double.PositiveInfinity, CobolFloat.MulChecked(double.PositiveInfinity, 2));
        Assert.Equal(0.0, CobolIntrinsics.PowNativeRealChecked(0, 2));
    }

    [Fact]
    public void ListBodies_FormTheirExpressionInTheIntermediate()
    {
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolIntrinsics.SumReal(1e308, 1e308)));
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolIntrinsics.MeanReal(1e308, 1e308)));
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolIntrinsics.RangeReal(1e308, -1e308)));
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolIntrinsics.MidrangeReal(1.5e308, 1.5e308)));
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolIntrinsics.MedianReal(1.5e308, 1.5e308)));
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolIntrinsics.Variance(1e200, -1e200)));
        Assert.Equal("EC-SIZE-UNDERFLOW", Ec(() => CobolIntrinsics.Variance(1e-200, 2e-200)));
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolIntrinsics.PresentValue(0.5, 1e308, 1e308)));
        Assert.Equal(double.PositiveInfinity, CobolIntrinsics.SumReal(double.PositiveInfinity, 1));
        Assert.Equal(0.0, CobolIntrinsics.Variance(1e-200, 1e-200));
    }
}
