// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DIVIDE KERNEL'S RADIX ALIGNMENT NEVER WRAPS (kb/Work PB1143's DIVIDE sibling).
/// <para><c>CobolNum.Divide</c> forms <c>round(a × 10^exp / b)</c> with <c>exp = bScale + resultScale − aScale</c>,
/// and its alignment multiply used to run unchecked in <see cref="Int128"/>: a 10-digit dividend over a
/// <c>PIC 9V9(20)</c> divisor into a <c>PIC 9(10)V9(10)</c> receiver needs 1.2·10^9 × 10^30 = 1.2·10^39 — past the
/// carrier although the QUOTIENT, 411 522 630, is tiny — and the wrapped dividend produced a wrong quotient with no
/// condition. An alignment that does not fit is now formed on the SDIDI (<c>CobolDec.DivToOdd</c>, round-to-odd at 34
/// digits) and rounded ONCE at the result scale; a quotient the carrier cannot hold is the §14.7.5 case-5 size
/// error. Every row below names which arm it takes: the alignment <c>a × 10^exp</c> (or <c>b × 10^−exp</c>) fits
/// <see cref="Int128"/> (max 1.7014·10^38) or it does not.</para>
/// </summary>
public sealed class CobolNumDivideAlignmentTests
{
    private static readonly Int128 Three = Int128.Parse("300000000000000000000");     // 3.0 at scale 20

    [Fact]
    public void AnAlignmentPastTheCarrier_StillGivesTheExactQuotient()
    {
        // exp = 20 + 10 − 0 = 30: 1234567890 × 10^30 = 1.2e39 does NOT fit (the wide arm).
        // 1234567890 ÷ 3.0 = 411522630, i.e. 411522630.0000000000 at scale 10.
        Assert.Equal(Int128.Parse("4115226300000000000"), CobolNum.Divide(1234567890, 0, Three, 20, 10, CobolRounding.Truncation));
    }

    [Fact]
    public void TheWideArm_RoundsOnceAtTheResultScale_InEveryMode()
    {
        // 1234567892 ÷ 3.0 = 411522630.6666…: exp = 30 again, so every row takes the wide arm.
        Assert.Equal(Int128.Parse("4115226306666666666"), CobolNum.Divide(1234567892, 0, Three, 20, 10, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("4115226306666666667"), CobolNum.Divide(1234567892, 0, Three, 20, 10, CobolRounding.NearestAwayFromZero));
        Assert.Equal(Int128.Parse("4115226306666666667"), CobolNum.Divide(1234567892, 0, Three, 20, 10, CobolRounding.AwayFromZero));
        Assert.Equal(Int128.Parse("-4115226306666666667"), CobolNum.Divide(-1234567892, 0, Three, 20, 10, CobolRounding.AwayFromZero));
        Assert.Equal(Int128.Parse("-4115226306666666666"), CobolNum.Divide(-1234567892, 0, Three, 20, 10, CobolRounding.TowardGreater));
        // ... and an exact tie stays a tie: 823045261 ÷ 2.0 is exactly 411522630.5 at scale 0, so NEAREST-EVEN and
        // NEAREST-AWAY part company on it (exp = 30 + 0 − 0 = 30: 823045261 × 10^30 = 8.2e38 does not fit — the wide
        // arm; the SDIDI quotient is exact here, round-to-odd never fires).
        Int128 two = Int128.Parse("2000000000000000000000000000000");          // 2.0 at scale 30
        Assert.Equal((Int128)411522630, CobolNum.Divide(823045261, 0, two, 30, 0, CobolRounding.NearestEven));
        Assert.Equal((Int128)411522631, CobolNum.Divide(823045261, 0, two, 30, 0, CobolRounding.NearestAwayFromZero));
    }

    [Fact]
    public void TheDivisorSideAlignment_PastTheCarrier_TakesTheWideArmToo()
    {
        // exp = 0 + 10 − 35 = −25 and b × 10^25 = 10^20 × 10^25 = 10^45 does NOT fit: the wide arm, divisor side.
        // a = 10^38 at scale 35 is 1000; 1000 ÷ 10^20 = 10^-17, which is 0 at scale 10 when truncated and one unit
        // (10^-10) away from zero — the remainder is nonzero, so the directed mode must see it.
        Int128 a = Int128.Parse("100000000000000000000000000000000000000");   // 10^38
        Int128 b = Int128.Parse("100000000000000000000");                      // 10^20
        Assert.Equal((Int128)0, CobolNum.Divide(a, 35, b, 0, 10, CobolRounding.Truncation));
        Assert.Equal((Int128)1, CobolNum.Divide(a, 35, b, 0, 10, CobolRounding.AwayFromZero));
        Assert.Equal((Int128)0, CobolNum.Divide(a, 35, b, 0, 10, CobolRounding.NearestAwayFromZero));
    }

    [Fact]
    public void TheNarrowArm_IsUnchanged()
    {
        // exp = 0 + 5 − 0 = 5: 1234567890 × 10^5 fits — the Int128 divide it always was.
        Assert.Equal((Int128)41152263000000, CobolNum.Divide(1234567890, 0, 3, 0, 5, CobolRounding.Truncation));
        // 5 ÷ 3.0 at scale 12: exp = 32, 5 × 10^32 fits (narrow), 1.666666666666…67 rounded.
        Assert.Equal((Int128)1_666_666_666_667, CobolNum.Divide(5, 0, Three, 20, 12, CobolRounding.NearestAwayFromZero));
        // 10 ÷ 3 at scale 36: exp = 36, 10 × 10^36 = 10^37 fits.
        Assert.Equal(Int128.Parse("3333333333333333333333333333333333333"), CobolNum.Divide(10, 0, 3, 0, 36, CobolRounding.Truncation));
    }

    [Fact]
    public void AQuotientBeyondTheCarrier_IsTheSizeError_NotAWrappedValue()
    {
        var e = Assert.Throws<CobolSizeError>(() =>
            CobolNum.Divide(Int128.Parse("10000000000000000000000000000000000000"), 0, 1, 0, 5, CobolRounding.Truncation));
        Assert.Equal("EC-SIZE-OVERFLOW", e.EcName);
    }

    [Fact]
    public void ADivisionByZero_StillRaisesTheDivideCondition_AndZeroOverAnythingIsZero()
    {
        Assert.Throws<CobolSizeError>(() => CobolNum.Divide(1, 0, 0, 0, 2, CobolRounding.Truncation));
        Assert.Equal((Int128)0, CobolNum.Divide(0, 0, 7, 50, 0, CobolRounding.Truncation));
    }
}
