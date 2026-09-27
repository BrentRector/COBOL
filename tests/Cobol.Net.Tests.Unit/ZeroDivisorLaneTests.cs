// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY VALUE LANE'S QUOTIENT KERNEL RAISES A ZERO DIVISOR, CHECKED OR NOT (kb/Work PB1605). ISO §14.7.5 case 2 —
/// "if the divisor in a divide operation or in a DIVIDE statement is zero" — is the size error condition in every
/// context, and no-phrase rule 2 names it EC-SIZE-ZERO-DIVIDE (a Fatal condition in Table 13). Whether checking is
/// enabled decides only the DISPOSITION (the SIZE ERROR phrase, a declarative, or — checking off — CONFORMANCE.md
/// DOC-A.1-70's abnormal termination), never whether the condition exists, so the UNCHECKED kernels owe the raise as
/// much as the checked twins do. The dispatch has three arms — the scaled <see cref="Int128"/> carrier
/// (<see cref="CobolNum.Divide"/>), binary64 (<see cref="CobolFloat.Div"/>) and the standard-decimal intermediate
/// (<see cref="CobolDec.Div"/>) — and before PB1605 only the third raised unchecked: the first answered 0 and the
/// second ±Infinity or NaN, and the run went on with that value. Each arm and each checked twin is asserted here so
/// the next lane cannot return a value for a quotient that does not exist.
/// </summary>
public sealed class ZeroDivisorLaneTests
{
    private static void AssertZeroDivide(Action divide)
    {
        var e = Assert.Throws<CobolSizeError>(divide);
        Assert.Equal("EC-SIZE-ZERO-DIVIDE", e.EcName);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(0, 0)]      // 0 / 0 is a zero divisor too — the dividend never matters
    [InlineData(-7, 2)]
    public void ScaledCarrier_BothKernels_RaiseAZeroDivisor(long dividend, int divisorScale)
    {
        foreach (CobolRounding mode in new[] { CobolRounding.Truncation, CobolRounding.Prohibited })
        {
            AssertZeroDivide(() => CobolNum.Divide(dividend, 0, 0, divisorScale, 2, mode));
            AssertZeroDivide(() => CobolNum.DivideOrThrow(dividend, 0, 0, divisorScale, 2, mode));
        }
    }

    [Theory]
    [InlineData(5.0, false)]
    [InlineData(5.0, true)]      // a negative zero is zero (ISO/IEC 60559: -0 == +0; xUnit folds -0.0 into 0.0,
                                 // so the sign travels as a flag)
    [InlineData(0.0, false)]     // the bare operator gave NaN here
    [InlineData(-1.5, false)]    // …and -Infinity here
    public void Binary64_BothKernels_RaiseAZeroDivisor(double dividend, bool negativeZero)
    {
        double divisor = negativeZero ? -0.0 : 0.0;
        Assert.Equal(negativeZero, double.IsNegative(divisor));
        AssertZeroDivide(() => CobolFloat.Div(dividend, divisor));
        AssertZeroDivide(() => CobolFloat.DivChecked(dividend, divisor));
    }

    [Fact]
    public void StandardDecimal_RaisesAZeroDivisor()
        => AssertZeroDivide(() => CobolDec.Div(CobolDec.From(5, 0), CobolDec.From(0, 3), CobolRounding.NearestEven));

    /// <summary>The control: a nonzero divisor is untouched by the rule — the unchecked binary64 quotient is still
    /// the IEEE operator (overflow to Infinity included, as the unchecked sum and product), and the scaled quotient
    /// still lands at the requested scale (5 / 2 at scale 2 = 250 unscaled).</summary>
    [Fact]
    public void NonzeroDivisor_IsTheOrdinaryQuotient()
    {
        Assert.Equal(2.5, CobolFloat.Div(5.0, 2.0));
        Assert.Equal(double.PositiveInfinity, CobolFloat.Div(double.MaxValue, 0.5));
        Assert.Equal((Int128)250, CobolNum.Divide(5, 0, 2, 0, 2, CobolRounding.Truncation));
    }
}
