// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SDIDI SUBSIDIARY QUOTIENT OF DIVIDE ... REMAINDER (ISO §14.9.12.4 6) c), kb/Work PB3017): the SDIDI quotient
/// truncated toward zero at the GIVING receiver's scale and reduced to the receiver's digit count, the significand
/// kept at the quotient's own exponent so the 34-digit invariant of every <see cref="CobolDec"/> operation holds and the
/// Int128 carrier plays no part.
/// </summary>
public sealed class CobolDecSubsidiaryQuotientTests
{
    private static string Show(CobolDec d) => $"{d.Sig}E{d.Exp}";

    [Theory]
    [InlineData(1234567, -3, 0, 0, "1234E0")]            // 1234.567 truncated at scale 0
    [InlineData(1234567, -3, 2, 0, "123456E-2")]          // 1234.567 truncated at scale 2 (never rounded)
    [InlineData(-1234567, -3, 0, 0, "-1234E0")]           // truncation is toward zero, sign kept
    [InlineData(1234567, -3, 3, 0, "1234567E-3")]         // already at the scale: unchanged
    [InlineData(1234567, -3, 5, 0, "1234567E-3")]         // finer scale than the quotient: unchanged, not re-expressed
    [InlineData(5, -3, 0, 0, "0E0")]                      // 0.005 truncates to zero
    [InlineData(123456, 0, 0, 4, "3456E0")]               // the low-order 4 digits of 123456
    [InlineData(-123456, 0, 0, 4, "-3456E0")]             // sign preserved by the cap
    [InlineData(1234567, -3, 2, 4, "3456E-2")]            // 1234.567 truncated at scale 2 is 1234.56 (123456E-2); its low 4 digits 3456E-2
    [InlineData(1, 4, 0, 4, "0E0")]                       // 10^4 at 4 digits: every kept digit is below the first nonzero one
    [InlineData(123, 3, 0, 5, "23E3")]                    // 123000 capped at 5 digits is 23000
    public void TruncatesAtTheScaleThenKeepsTheLowOrderDigits(long sig, int exp, int scale, int digits, string expected) =>
        Assert.Equal(expected, Show(CobolDec.SubsidiaryQuotient(new CobolDec(sig, exp), scale, digits)));

    /// <summary>The case that motivated it: a quotient near 4.86E+37 (an unsigned 16-byte dividend divided by 7) is an
    /// SDIDI with a positive exponent, an integer already, and passes through untouched however wide the receiver.</summary>
    [Fact]
    public void AQuotientCoarserThanTheScale_ComesBackUntouched_PastTheInt128Carrier()
    {
        var q = new CobolDec(Int128.Parse("4861176670299120906619637249025260"), 4);          // 4.86E+37, 38 integer digits
        Assert.Equal(q, CobolDec.SubsidiaryQuotient(q, 0, 0));
        Assert.Equal(q, CobolDec.SubsidiaryQuotient(q, 3, 0));
        var huge = new CobolDec(Int128.Parse("1428571428571428571428571428571429"), 6);       // 1.43E+39: past every container
        Assert.Equal(huge, CobolDec.SubsidiaryQuotient(huge, 0, 0));
    }

    [Fact]
    public void AFractionDeeperThanTheCarrierHolds_TruncatesToZero_WithoutThrowing()
    {
        Assert.Equal(new CobolDec(0, 0), CobolDec.SubsidiaryQuotient(new CobolDec(123, -60), 0, 0));
        Assert.Equal(new CobolDec(0, 0), CobolDec.SubsidiaryQuotient(new CobolDec(0, 5), 0, 3));
    }
}
