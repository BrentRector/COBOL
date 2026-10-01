// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DISCARDED TAIL OF AN SDIDI ADDITION IS A SIGNED QUANTITY (kb/Work PB1510).
/// <para><c>CobolDec.AddSigned</c> aligns the operand with the smaller magnitude by shifting it DOWN; C# division
/// truncates toward zero, so the shifted operand is a little closer to zero than the exact one and the dropped tail
/// has ITS sign. The tail used to be folded into an unsigned sticky bit that <c>Round34Wide</c> reads as excess in the
/// RESULT's direction — correct when the tail and the result agree in sign, wrong by a whole unit of the 34th digit
/// when they do not: <c>1 − 1.0E−50</c> held 1.000…0, and under INTERMEDIATE ROUNDING IS TRUNCATION
/// (§11.9.11.2 GR3 e: "the nearest value in that format that is nearer to zero than the intermediate value")
/// the result must be 0.999…9, thirty-four nines.</para>
/// <para>Every expected value below is the EXACT sum rounded to 34 significant digits in the named mode, computed
/// with Python's <c>decimal</c> (precision 34, ROUND_DOWN / ROUND_HALF_UP / ROUND_HALF_EVEN) over an exact sum —
/// an independent implementation of the rounding rule, never this compiler's own output.</para>
/// </summary>
public sealed class CobolDecSignedTailTests
{
    /// <summary>A decimal text as the exact SDIDI it denotes.</summary>
    private static CobolDec Dec(string text)
    {
        bool neg = text.StartsWith('-');
        string t = neg ? text[1..] : text;
        int point = t.IndexOf('.');
        string digits = point < 0 ? t : t.Remove(point, 1);
        int frac = point < 0 ? 0 : t.Length - point - 1;
        int e = 0;
        int x = digits.IndexOf('E');
        if (x >= 0) { e = int.Parse(digits[(x + 1)..], System.Globalization.CultureInfo.InvariantCulture); digits = digits[..x]; frac = point < 0 ? 0 : x - point; }
        Int128 sig = Int128.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
        return new CobolDec(neg ? -sig : sig, e - frac);
    }

    private static void Assert34(string expected, CobolDec actual) =>
        Assert.True(CobolDec.Compare(Dec(expected), actual) == 0, $"expected {expected}, got Sig={actual.Sig} Exp={actual.Exp}");

    private const string AllNines = "0.9999999999999999999999999999999999";   // 1 − 1E−34, thirty-four nines

    [Theory]
    // (a, op, b, mode, expected) — the registrar's repro (1 − 1E−50) in each mode, and its three sign mirrors.
    [InlineData("1", "-", "1E-50", CobolRounding.Truncation, AllNines)]
    [InlineData("1", "-", "1E-50", CobolRounding.NearestAwayFromZero, "1")]
    [InlineData("1", "-", "1E-50", CobolRounding.NearestEven, "1")]
    [InlineData("1E-50", "-", "1", CobolRounding.Truncation, "-" + AllNines)]
    [InlineData("1E-50", "-", "1", CobolRounding.NearestAwayFromZero, "-1")]
    [InlineData("-1", "+", "1E-50", CobolRounding.Truncation, "-" + AllNines)]
    [InlineData("-1", "+", "1E-50", CobolRounding.NearestEven, "-1")]
    // Same-sign tails keep the old (correct) reading: the tail is excess in the result's own direction.
    [InlineData("1", "+", "1E-50", CobolRounding.Truncation, "1")]
    [InlineData("-1", "-", "1E-50", CobolRounding.Truncation, "-1")]
    // The registrar's NEAREST case: 1 − (5.00…01E−35) lies BELOW the tie, so the nearest 34-digit value is 34 nines.
    [InlineData("1", "-", "5.00000000000000000000000001E-35", CobolRounding.NearestAwayFromZero, AllNines)]
    [InlineData("1", "-", "5.00000000000000000000000001E-35", CobolRounding.NearestEven, AllNines)]
    // The exact tie (digit 35 is 5 and nothing follows): away-from-zero and even both go to 1 (34 nines is odd).
    [InlineData("1", "-", "5E-35", CobolRounding.NearestAwayFromZero, "1")]
    [InlineData("1", "-", "5E-35", CobolRounding.NearestEven, "1")]
    [InlineData("1", "-", "5E-35", CobolRounding.Truncation, AllNines)]
    // The control the registrar measured: the exponent gap fits the scratch and was always right.
    [InlineData("1", "-", "1E-36", CobolRounding.Truncation, AllNines)]
    // A 32-digit integer minus a far-smaller operand: the integer part must not round up.
    [InlineData("12345678901234567890123456789012", "-", "7E-30", CobolRounding.Truncation, "12345678901234567890123456789011.99")]
    [InlineData("12345678901234567890123456789012", "-", "7E-30", CobolRounding.NearestEven, "12345678901234567890123456789012")]
    public void TheDroppedTail_CarriesItsOwnSign(string a, string op, string b, CobolRounding mode, string expected)
    {
        var x = Dec(a);
        var y = Dec(b);
        Assert34(expected, op == "+" ? CobolDec.Add(x, y, mode) : CobolDec.Sub(x, y, mode));
    }

    /// <summary>INTERMEDIATE ROUNDING IS PROHIBITED (§11.9.11.2 3) d)) must see an inexact signed tail as inexact, and an
    /// exactly representable difference must not raise.</summary>
    [Fact]
    public void Prohibited_RaisesOnAnInexactSignedTail_AndNotOnAnExactDifference()
    {
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.Sub(Dec("1"), Dec("1E-50"), CobolRounding.Prohibited));
        Assert.Equal("EC-SIZE-TRUNCATION", ex.EcName);
        Assert.Throws<CobolSizeError>(() => CobolDec.Add(Dec("-1"), Dec("1E-50"), CobolRounding.Prohibited));
        Assert34("0.5", CobolDec.Sub(Dec("1"), Dec("0.5"), CobolRounding.Prohibited));
        Assert34("0.9999999999999999999999999999999999", CobolDec.Sub(Dec("1"), Dec("1E-34"), CobolRounding.Prohibited));
    }

    /// <summary>The corner a signed-sticky jam alone gets wrong: a 38-digit operand (<c>CobolDec.From</c> keeps the
    /// full Int128 significand) cancelling the shifted other one down to nothing, where the dropped tail's OWN
    /// digits are the whole result. 10^38 − (10^38 + 1) is exactly −1: the high operand (10^37 × 10^1) fills the
    /// scratch, the low one (−(10^38 + 1) × 10^0) is shifted down one digit to −10^37 with the tail −0.1 dropped, and
    /// the shifted sum is ZERO — the result is the tail itself, which only an exact computation returns.</summary>
    [Fact]
    public void ACancellationDownToTheTailsOwnDigits_IsExact()
    {
        var a = new CobolDec(Int128.Parse("10000000000000000000000000000000000000"), 1);               // 10^37 × 10^1
        var b = new CobolDec(-Int128.Parse("100000000000000000000000000000000000001"), 0);             // −(10^38 + 1)
        Assert34("-1", CobolDec.Add(a, b, CobolRounding.NearestAwayFromZero));
        Assert34("-1", CobolDec.Add(a, b, CobolRounding.Truncation));
        Assert34("-1", CobolDec.Add(b, a, CobolRounding.Truncation));      // addition commutes
    }
}
