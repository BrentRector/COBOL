// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>A store into a FLOATING-POINT numeric-edited item rounds its significand by the statement's mode
/// (kb/Work PB2638). §14.7.4.3 rule 4: "the arithmetic value is rounded to the nearest value that can be
/// represented in the resultant identifier" — for <c>+9.99E+99</c> a three-digit significand — and rules 3, 5, 6, 8 and 9
/// give the other modes; rule 7: PROHIBITED and an inexact value is the size error condition with the resultant
/// unchanged. A MOVE passes TRUNCATION (§14.6.8.4 rule 2). §14.7.5 case 3 is tested "after … any applicable
/// rounding", so a carry out of the last digit is renormalized and its exponent re-checked.</summary>
public sealed class CobolEditFloatRoundingTests
{
    private const string Mask = "+9.99E+99";

    [Theory]
    // Nearest-away-from-zero (the bare ROUNDED): the tie goes away from zero (rule 4's last sentence).
    [InlineData(1235, 3, CobolRounding.NearestAwayFromZero, "+1.24E+00")]
    [InlineData(-1235, 3, CobolRounding.NearestAwayFromZero, "-1.24E+00")]
    [InlineData(1234, 3, CobolRounding.NearestAwayFromZero, "+1.23E+00")]
    [InlineData(1236, 3, CobolRounding.NearestAwayFromZero, "+1.24E+00")]
    // Nearest-even and nearest-toward-zero differ only on the tie.
    [InlineData(1225, 3, CobolRounding.NearestEven, "+1.22E+00")]
    [InlineData(1235, 3, CobolRounding.NearestEven, "+1.24E+00")]
    [InlineData(1225, 3, CobolRounding.NearestTowardZero, "+1.22E+00")]
    [InlineData(1226, 3, CobolRounding.NearestTowardZero, "+1.23E+00")]
    // Directed modes (rules 3, 8 and 9) see the SIGN.
    [InlineData(1231, 3, CobolRounding.AwayFromZero, "+1.24E+00")]
    [InlineData(-1231, 3, CobolRounding.AwayFromZero, "-1.24E+00")]
    [InlineData(1231, 3, CobolRounding.TowardGreater, "+1.24E+00")]
    [InlineData(-1239, 3, CobolRounding.TowardGreater, "-1.23E+00")]
    [InlineData(1239, 3, CobolRounding.TowardLesser, "+1.23E+00")]
    [InlineData(-1231, 3, CobolRounding.TowardLesser, "-1.24E+00")]
    // No ROUNDED phrase (rule 2) and a PROHIBITED unchecked landing truncate.
    [InlineData(1239, 3, CobolRounding.Truncation, "+1.23E+00")]
    [InlineData(-1239, 3, CobolRounding.Truncation, "-1.23E+00")]
    // A carry out of the last digit renormalizes: 9.996 -> 10.0 -> 1.00E+01.
    [InlineData(9996, 3, CobolRounding.NearestAwayFromZero, "+1.00E+01")]
    [InlineData(-9996, 3, CobolRounding.NearestAwayFromZero, "-1.00E+01")]
    public void TryFormatFloat_RoundsTheSignificandByTheMode(long unscaled, int scale, CobolRounding mode, string expected)
    {
        Assert.True(CobolEdit.TryFormatFloat(unscaled, scale, Mask, out string image, mode));
        Assert.Equal(expected, image);
        // The unchecked landing agrees (it differs only in not reporting).
        Assert.Equal(expected, CobolEdit.FormatFloatStore(unscaled, scale, Mask, mode));
    }

    /// <summary>A MOVE (TRUNCATION) never rounds — the MOVE disposition is the one that was always right.</summary>
    [Fact]
    public void FormatFloatStore_WithTruncation_IsTheMoveStore() =>
        Assert.Equal("+1.23E+00", CobolEdit.FormatFloatStore(1239, 3, Mask, CobolRounding.Truncation));

    /// <summary>Rule 7: PROHIBITED and a value the three-digit significand cannot hold exactly is the size error
    /// condition; an exact value is stored. The unchecked store lands the inexact one TRUNCATED (DOC-A.1-70) and
    /// raises nothing.</summary>
    [Fact]
    public void Prohibited_IsASizeErrorOnlyWhenInexact()
    {
        Assert.False(CobolEdit.TryFormatFloat(1234, 3, Mask, out _, CobolRounding.Prohibited));
        Assert.True(CobolEdit.TryFormatFloat(1230, 3, Mask, out string exact, CobolRounding.Prohibited));
        Assert.Equal("+1.23E+00", exact);
        Assert.Equal("+1.23E+00", CobolEdit.FormatFloatStore(1234, 3, Mask, CobolRounding.Prohibited));
    }

    /// <summary>The rounding precedes the range test (§14.7.5 case 3, "after … any applicable rounding"): a value
    /// that rounds past the exponent's capacity is a size error and the same value truncated is not.</summary>
    [Fact]
    public void ACarryPastTheExponentCapacity_IsASizeErrorAfterRounding()
    {
        const string OneDigitExponent = "+9.99E+9";
        Assert.False(CobolEdit.TryFormatFloat(9995000000, 0, OneDigitExponent, out _, CobolRounding.NearestAwayFromZero));
        Assert.True(CobolEdit.TryFormatFloat(9995000000, 0, OneDigitExponent, out string image, CobolRounding.Truncation));
        Assert.Equal("+9.99E+9", image);
    }

    /// <summary>The same rounding for the other two sender carriers (a standard-decimal intermediate and a
    /// binary64) — one kernel behind the three entries.</summary>
    [Fact]
    public void TheDecAndDoubleSendersRoundLikeTheScaledOne()
    {
        Assert.True(CobolEdit.TryFormatFloat(new CobolDec(1235, -3), Mask, out string d, CobolRounding.NearestAwayFromZero));
        Assert.Equal("+1.24E+00", d);
        Assert.True(CobolEdit.TryFormatFloat(1.235, Mask, out string r, CobolRounding.NearestAwayFromZero));
        Assert.Equal("+1.24E+00", r);
    }
}
