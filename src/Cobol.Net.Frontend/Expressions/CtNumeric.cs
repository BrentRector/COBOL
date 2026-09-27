// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Common;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Expressions;

/// <summary>
/// The compile-time NUMERIC VALUE CARRIER (kb/Work PB1592): every numeric compile-time value — a numeric literal, a
/// compilation-variable or constant-name value, an intermediate or final result of a §7.3.6 compile-time arithmetic
/// expression — is a <see cref="CobolDec"/>, an exact significand × 10^exponent with the SDIDI's 34-digit precision
/// and decimal128 range (ISO §8.8.1.5.2). The carrier is NOT an arithmetic mode: it holds every valid fixed-point
/// literal (at most 31 digits, §8.3.3.3.2) exactly, and every value either mode of <see cref="CompileTimeArithmetic"/>
/// produces, so a single literal (§7.3.11.4 GR5 — "treated as a literal, not as an arithmetic-expression") keeps its
/// value whatever the edition's arithmetic mode is, and only a value that ENTERS an arithmetic expression meets the
/// mode's own range (<see cref="CompileTimeArithmetic.Enter"/>).
/// <para>This class holds the carrier's mode-independent facts — parse, integer part, digit count, text, hash — so
/// no consumer re-derives them from a second numeric type.</para>
/// </summary>
public static class CtNumeric
{
    /// <summary>The carrier value of a canonical (dot-decimal) numeric literal of either form, through the ONE exact
    /// literal parser (<see cref="NumericLiteral.TryParseExact"/>) and the SDIDI's one rounding funnel. Exact for
    /// every significand of at most 34 digits — every fixed-point literal; only a 35- or 36-digit floating-point
    /// significand (§8.3.3.3.3) rounds, and such a literal reaches the carrier only as a constant entry's sole
    /// operand, whose value is carried by its TEXT. False when the text is not a numeric literal or its value lies
    /// outside the decimal128 range (§8.8.1.5.2 r2).</summary>
    public static bool TryParseLiteral(string canonicalText, out CobolDec value)
    {
        value = default;
        if (!NumericLiteral.TryParseExact(canonicalText, out var sig, out int exp10)) return false;
        try
        {
            value = CobolDec.FromParsed(sig, exp10, CobolRounding.NearestEven);
            return true;
        }
        catch (CobolSizeError)
        {
            return false;
        }
    }

    /// <summary>The exact carrier value of a .NET <see cref="decimal"/> — its 96-bit coefficient and scale.</summary>
    public static CobolDec FromDecimal(decimal d)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(d, bits);
        Int128 magnitude = ((Int128)(uint)bits[2] << 64) | ((Int128)(uint)bits[1] << 32) | (uint)bits[0];
        int scale = (bits[3] >> 16) & 0xFF;
        return new CobolDec(bits[3] < 0 ? -magnitude : magnitude, -scale);
    }

    /// <summary>The integer part of <paramref name="v"/>, truncated toward zero — the §15.49 INTEGER-PART value
    /// §7.3.6.3 GR3 applies to an arithmetic expression's final result. Exact; the result's exponent is at least
    /// zero.</summary>
    public static CobolDec IntegerPart(CobolDec v)
    {
        if (v.Sig == 0) return new CobolDec(0, 0);
        if (v.Exp >= 0) return v;
        // A carrier significand has at most 34 digits (< 10^38), so dropping 38 or more digits leaves zero.
        return -v.Exp >= 38 ? new CobolDec(0, 0) : new CobolDec(v.ToUnscaled(0, CobolRounding.Truncation), 0);
    }

    /// <summary>True when <paramref name="v"/> has no fractional part.</summary>
    public static bool IsInteger(CobolDec v) => CobolDec.Compare(IntegerPart(v), v) == 0;

    /// <summary>The number of digits of an integer value as <see cref="IntegerPart"/> returns it (exponent at least
    /// zero); zero has one digit.</summary>
    public static int IntegerDigits(CobolDec integer)
    {
        if (integer.Sig == 0) return 1;
        Int128 m = Int128.Abs(integer.Sig);
        int digits = 1;
        while (m >= 10) { m /= 10; digits++; }
        return digits + Math.Max(0, integer.Exp);
    }

    /// <summary>The value of an integer carrier value of at most 38 digits as an <see cref="Int128"/> — exact, since
    /// the truncating transfer drops no nonzero digit of an integer.</summary>
    public static Int128 ToInt128(CobolDec integer) => integer.ToUnscaledChecked(0, CobolRounding.Truncation);

    /// <summary>The integer-literal text of an integer value as <see cref="IntegerPart"/> returns it (§7.3.6.3 GR3:
    /// "the resultant value shall be considered to be an integer numeric literal") — sign, digits, no point; zero is
    /// <c>0</c>.</summary>
    public static string ToIntegerText(CobolDec integer)
    {
        if (integer.Sig == 0) return "0";
        string digits = Int128.Abs(integer.Sig).ToString(CultureInfo.InvariantCulture)
            + new string('0', Math.Max(0, integer.Exp));
        return integer.Sig < 0 ? "-" + digits : digits;
    }

    /// <summary>The value in the scientific text form a <see cref="decimal"/> parse accepts
    /// (<c>significandEexponent</c>) — the one bridge from the carrier into System.Decimal, so a value enters that
    /// mode through the same parse (and the same rounding of excess fraction digits) a literal always did.</summary>
    public static string ToScientificText(CobolDec v) =>
        v.Sig.ToString(CultureInfo.InvariantCulture) + "E" + v.Exp.ToString(CultureInfo.InvariantCulture);

    /// <summary>A hash consistent with value equality (<see cref="CobolDec.Compare"/> == 0): trailing zeros of the
    /// significand fold into the exponent, so <c>1</c>, <c>1.0</c> and <c>10E-1</c> hash alike.</summary>
    public static int ValueHash(CobolDec v)
    {
        if (v.Sig == 0) return 0;
        Int128 sig = v.Sig;
        int exp = v.Exp;
        while (sig % 10 == 0) { sig /= 10; exp++; }
        return HashCode.Combine(sig, exp);
    }
}
