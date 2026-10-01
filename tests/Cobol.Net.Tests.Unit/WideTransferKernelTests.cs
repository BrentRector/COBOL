// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE FINAL TRANSFER OF A NATIVE PRODUCT OR QUOTIENT PAST THE Int128 CARRIER, EXACT FOR EVERY RECEIVER WIDTH
/// (kb/Work PB1143, the train review's finding N1). <see cref="CobolDec.MulAtScale"/> and
/// <see cref="CobolDec.QuotientAtScale"/> round the exact 256-bit product / quotient ONCE with the receiver's mode
/// (§14.7.4.3), where the SDIDI's 34 digits (<see cref="CobolDec.MulToOdd"/>) hid the tail from a 16-byte COMP-5
/// receiver that holds 38. Every expected value is exact decimal arithmetic worked by hand in the comments.
/// </summary>
public sealed class WideTransferKernelTests
{
    private static Int128 Big(string digits) => Int128.Parse(digits);

    // E = 12345678901234567891, F = 1234567890123456789: E * F = 15241578753238836751425087877625361999 (38 digits).
    private static readonly Int128 E = Big("12345678901234567891"), F = Big("1234567890123456789");
    private static readonly Int128 EF = Big("15241578753238836751425087877625361999");

    [Fact]
    public void ThePb1143Finding_TheLowDigitsOfA38DigitProductSurvive()
    {
        // The SDIDI landing gave ...5370000 here: 34 digits kept, the last four lost.
        Assert.Equal(EF, CobolDec.MulAtScale(E, 0, F, 0, 0, CobolRounding.Truncation, checkedTransfer: false));
        Assert.Equal(EF, CobolDec.MulAtScale(E, 0, F, 0, 0, CobolRounding.Prohibited, checkedTransfer: true));   // exact: nothing to refuse
        Assert.Equal(-EF, CobolDec.MulAtScale(-E, 0, F, 0, 0, CobolRounding.NearestEven, checkedTransfer: true));
    }

    // P = 1234567890123456789.5, Q = 9876543210987654321.5 (scale 1): P * Q = ...190824.25 (scale 2 value 12193263113702179527930193561668190824.25).
    private static readonly Int128 P = Big("12345678901234567895"), Q = Big("98765432109876543215");

    [Theory]
    [InlineData(CobolRounding.Truncation, "12193263113702179527930193561668190824")]
    [InlineData(CobolRounding.NearestAwayFromZero, "12193263113702179527930193561668190824")]   // .25 is below one half
    [InlineData(CobolRounding.NearestEven, "12193263113702179527930193561668190824")]
    [InlineData(CobolRounding.AwayFromZero, "12193263113702179527930193561668190825")]
    [InlineData(CobolRounding.TowardGreater, "12193263113702179527930193561668190825")]
    [InlineData(CobolRounding.TowardLesser, "12193263113702179527930193561668190824")]
    public void ATail_IsRoundedOnce_ByTheReceiversMode(CobolRounding mode, string expected) =>
        Assert.Equal(Big(expected), CobolDec.MulAtScale(P, 1, Q, 1, 0, mode, checkedTransfer: true));

    [Fact]
    public void ANegativeTail_IsRoundedByItsSign()
    {
        // -...190824.25: AWAY-FROM-ZERO is -...825, TOWARD-GREATER (up) is -...824, TOWARD-LESSER (down) is -...825.
        Assert.Equal(-Big("12193263113702179527930193561668190825"), CobolDec.MulAtScale(-P, 1, Q, 1, 0, CobolRounding.AwayFromZero, true));
        Assert.Equal(-Big("12193263113702179527930193561668190824"), CobolDec.MulAtScale(-P, 1, Q, 1, 0, CobolRounding.TowardGreater, true));
        Assert.Equal(-Big("12193263113702179527930193561668190825"), CobolDec.MulAtScale(-P, 1, Q, 1, 0, CobolRounding.TowardLesser, true));
    }

    [Fact]
    public void ATie_FollowsTheModesTieRule_AndAStickyTailBreaksIt()
    {
        // P * 9876543210987654321.0 = ...462429.50: an exact tie. 429 is odd.
        Int128 q2 = Big("98765432109876543210");
        Int128 even = Big("12193263113702179527312909616606462430"), down = Big("12193263113702179527312909616606462429");
        Assert.Equal(even, CobolDec.MulAtScale(P, 1, q2, 1, 0, CobolRounding.NearestEven, true));
        Assert.Equal(even, CobolDec.MulAtScale(P, 1, q2, 1, 0, CobolRounding.NearestAwayFromZero, true));
        Assert.Equal(down, CobolDec.MulAtScale(P, 1, q2, 1, 0, CobolRounding.NearestTowardZero, true));
        // 0.5 × (1 + 10^-30) = 0.5 + 0.5e-30, a product of two scale-30 operands, is ABOVE the half: NEAREST-TOWARD-ZERO
        // and NEAREST-EVEN both round it up, where the bare half would have stayed down (0 is even).
        Int128 a = Big("500000000000000000000000000000"), b = Big("1000000000000000000000000000001");
        Assert.Equal(1, CobolDec.MulAtScale(a, 30, b, 30, 0, CobolRounding.NearestTowardZero, true));
        Assert.Equal(1, CobolDec.MulAtScale(a, 30, b, 30, 0, CobolRounding.NearestEven, true));
    }

    [Fact]
    public void Prohibited_RaisesOnlyWhenChecked_AndTruncatesOtherwise()
    {
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.MulAtScale(P, 1, Q, 1, 0, CobolRounding.Prohibited, checkedTransfer: true));
        Assert.Equal("EC-SIZE-TRUNCATION", ex.EcName);
        Assert.Equal(Big("12193263113702179527930193561668190824"),
            CobolDec.MulAtScale(P, 1, Q, 1, 0, CobolRounding.Prohibited, checkedTransfer: false));   // DOC-A.1-70
    }

    [Fact]
    public void AProductPastTheCarrier_IsTheSizeError_Checked_AndKeepsTheLowOrderDigits_Unchecked()
    {
        // 99999999999999999999 × 99999999999999999999 = 9999999999999999999800000000000000000001 (40 digits).
        Int128 g = Big("99999999999999999999");
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.MulAtScale(g, 0, g, 0, 0, CobolRounding.Truncation, checkedTransfer: true));
        Assert.Equal("EC-SIZE-TRUNCATION", ex.EcName);
        // The low-order 38 digits of the 40-digit product: its first two digits dropped.
        Assert.Equal(Big("99999999999999999800000000000000000001"),
            CobolDec.MulAtScale(g, 0, g, 0, 0, CobolRounding.Truncation, checkedTransfer: false));
    }

    [Fact]
    public void ARoundedResultAtAFinerScale_AppendsZerosExactly()
    {
        // 2.5 (25 at scale 1) × 4 into scale 3: 10.000 = 10000.
        Assert.Equal(10000, CobolDec.MulAtScale(25, 1, 4, 0, 3, CobolRounding.Truncation, true));
        // 20-digit × 20-digit integers into scale 2: 100 digits... past the carrier — the checked size error.
        Int128 g = Big("99999999999999999999");
        Assert.Throws<CobolSizeError>(() => CobolDec.MulAtScale(g, 0, g, 0, 2, CobolRounding.Truncation, true));
    }

    // A = 12345678901234567890123456789012345678 / 4 = ...6419.5 (a tie); the radix alignment 10^30 leaves the carrier.
    private static readonly Int128 A38 = Big("12345678901234567890123456789012345678");
    private static readonly Int128 Four = Big("4000000000000000000000000000000");   // PIC 9V9(30) holding 4

    [Theory]
    [InlineData(CobolRounding.Truncation, "3086419725308641972530864197253086419")]
    [InlineData(CobolRounding.NearestEven, "3086419725308641972530864197253086420")]     // 419 is odd
    [InlineData(CobolRounding.NearestTowardZero, "3086419725308641972530864197253086419")]
    [InlineData(CobolRounding.NearestAwayFromZero, "3086419725308641972530864197253086420")]
    [InlineData(CobolRounding.AwayFromZero, "3086419725308641972530864197253086420")]
    public void ThePb1143Sibling_ADivideWhoseAlignmentLeavesTheCarrier_KeepsAll38Digits(CobolRounding mode, string expected) =>
        Assert.Equal(Big(expected), CobolDec.QuotientAtScale(A38, Four, 30, mode));

    [Fact]
    public void TheDivideKernel_RoutesThroughIt_AndTheCheckedProhibitedTestIsExactOnTheWideNumerator()
    {
        // CobolNum.Divide: 12345678901234567890123456789012345678 / 4.0 (scale 30), result scale 0: the alignment is 10^30.
        Assert.Equal(Big("3086419725308641972530864197253086420"), CobolNum.Divide(A38, 0, Four, 30, 0, CobolRounding.NearestAwayFromZero));
        Assert.Throws<CobolSizeError>(() => CobolNum.DivideOrThrow(A38, 0, Four, 30, 0, CobolRounding.Prohibited));   // the .5 is a real tail
        Assert.Equal(A38,
            CobolNum.DivideOrThrow(A38, 0, Big("1000000000000000000000000000000"), 30, 0, CobolRounding.Prohibited));   // A / 1.0: exact
    }

    [Fact]
    public void ARemainderOfMoreThanHalfTheCarrier_DoesNotWrapTheTieTest()
    {
        // 2·|rem| wraps Int128 once |rem| ≥ 2^126 (8.5e37); the test is written without the doubling. (10^38 − 1) / 1.2e38 is
        // 0.8333…: the quotient is 0 and the remainder is the whole dividend, 1e38 — doubled, 2e38 wraps negative and the
        // tie test read "below one half".
        Int128 dividend = Big("100000000000000000000000000000000000000") - 1, divisor = Big("120000000000000000000000000000000000000");
        Assert.Equal(0, CobolDec.QuotientAtScale(dividend, divisor, 0, CobolRounding.Truncation));
        Assert.Equal(1, CobolDec.QuotientAtScale(dividend, divisor, 0, CobolRounding.NearestAwayFromZero));
        Assert.Equal(1, CobolDec.QuotientAtScale(dividend, divisor, 0, CobolRounding.NearestEven));
    }
}
