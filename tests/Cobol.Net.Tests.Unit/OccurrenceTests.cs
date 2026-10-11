// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <see cref="Occurrence"/> — an occurrence number as a subscript reads it, with the exact value beside the
/// saturated <c>long</c> (kb/Work PB2695). ISO §8.4.2.3.4 GR2 — "If the value of the subscript is not a positive
/// integer or is less than one or is greater than the highest permissible occurrence number, the EC-BOUND-SUBSCRIPT
/// exception condition is set to exist" — needs an out-of-range subscript to STAY out of range through the narrowing
/// (so <see cref="Occurrence.Value"/> saturates), and a diagnostic needs the value the program holds (so
/// <see cref="Occurrence.ToString"/> is exact). The end-to-end termination text is asserted in the conformance
/// project (<c>WideSubscriptDiagnosticTests</c>); these are the seams a COBOL program cannot reach: the unsigned-wide
/// carrier, the scale arithmetic, and the expression intake's capacity.
/// </summary>
public sealed class OccurrenceTests
{
    private static NumProfile Profile(int digits, int fractionDigits, bool signed = false) => new()
    {
        Digits = digits, FractionDigits = fractionDigits, Signed = signed,
        Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
    };

    /// <summary>An <see cref="Int128"/> from its decimal text — C# has no literal past <c>ulong</c>.</summary>
    private static Int128 I(string digits) => Int128.Parse(digits, CultureInfo.InvariantCulture);

    [Fact]
    public void InRange_IsThePlainLong_AndConvertsBothWays()
    {
        Occurrence o = CobolTable.Occ(3L, Profile(4, 0));
        Assert.Equal(3L, o.Value);
        Assert.Equal("3", o.ToString());
        long asLong = o;
        Occurrence fromLong = asLong;
        Assert.Equal(3L, asLong);
        Assert.Equal(3L, fromLong.Value);
    }

    [Fact]
    public void WidePositive_SaturatesTheValue_AndKeepsTheExactText()
    {
        Occurrence o = CobolTable.Occ(I("100000000000000000001"), Profile(21, 0));
        Assert.Equal(long.MaxValue, o.Value);
        Assert.Equal("100000000000000000001", o.ToString());
    }

    [Fact]
    public void WideNegative_SaturatesTheValue_AndKeepsTheExactText()
    {
        Occurrence o = CobolTable.Occ(I("-100000000000000000001"), Profile(21, 0, signed: true));
        Assert.Equal(long.MinValue, o.Value);
        Assert.Equal("-100000000000000000001", o.ToString());
    }

    /// <summary>The message names the item's VALUE, not its storage: 10^20 + 1 stored at scale 2 is 10^22 + 100.</summary>
    [Fact]
    public void ScaledItem_NamesTheValueNotTheStorage()
    {
        Occurrence o = CobolTable.Occ(I("10000000000000000000100"), Profile(23, 2));
        Assert.Equal(long.MaxValue, o.Value);
        Assert.Equal("100000000000000000001", o.ToString());
    }

    /// <summary>A trailing-P item stores digits that are multiples of 10^|scale| (§13.18.40.4 GR14): <c>5</c> at scale
    /// −20 is 5 × 10^20, past <c>long</c>, and the text says so.</summary>
    [Fact]
    public void TrailingP_NamesTheValueNotTheStorage()
    {
        Occurrence o = CobolTable.Occ(5L, Profile(1, -20));
        Assert.Equal(long.MaxValue, o.Value);
        Assert.Equal("500000000000000000000", o.ToString());
    }

    /// <summary>The unsigned-wide carrier (a 16-byte unsigned COMP-5 item) holds values past <c>Int128</c>: the
    /// occurrence is read as the UInt128 it is, so neither the saturation nor the text goes through an Int128 that
    /// cannot hold it.</summary>
    [Fact]
    public void UnsignedWide_PastInt128_KeepsTheExactText()
    {
        Occurrence o = CobolTable.Occ(UInt128.MaxValue, Profile(39, 0));
        Assert.Equal(long.MaxValue, o.Value);
        Assert.Equal("340282366920938463463374607431768211455", o.ToString());
    }

    /// <summary>A scale that divides a UInt128 back inside <c>long</c>'s range makes it a VALID occurrence. The read
    /// used to saturate every value past Int128 "whatever its fraction", so this one positioned the saturated
    /// <c>long</c> instead of 3402823669209384634.</summary>
    [Fact]
    public void UnsignedWide_PastInt128_WithAScaleThatBringsItBackInside_IsThatOccurrence()
    {
        UInt128 pow20 = 1;
        for (int i = 0; i < 20; i++) pow20 *= 10;
        UInt128 value = (UInt128)3402823669209384634UL * pow20;
        Assert.True(value > (UInt128)Int128.MaxValue);
        Occurrence o = CobolTable.Occ(value, Profile(39, 20));
        Assert.Equal(3402823669209384634L, o.Value);
        Assert.Equal("3402823669209384634", o.ToString());
    }

    /// <summary>The expression intake keeps the EXACT integer up to the 38-digit segment temporary's capacity and
    /// saturates by sign past it (never wraps: a wrapped value could land back inside 1..n).</summary>
    [Fact]
    public void ExpressionIntake_IsExactToTheTemporarysCapacity_AndSaturatesBySign()
    {
        Int128 cap = I("99999999999999999999999999999999999999");
        Assert.Equal(I("1000000000000000000000"), CobolTable.OccValue(I("1000000000000000000000"), 0));
        Assert.Equal(I("100000000000000000001"), CobolTable.OccValue(I("10000000000000000000100"), 2));
        Assert.Equal(cap, CobolTable.OccValue(Int128.MaxValue, 0));
        Assert.Equal(-cap, CobolTable.OccValue(Int128.MinValue + 1, 0));
        Assert.Equal(cap, CobolTable.OccValue(1, -40));
        Assert.Equal((Int128)5000, CobolTable.OccValue(5, -3));
        Assert.Equal(cap, CobolTable.OccValueReal(1e300));
        Assert.Equal(-cap, CobolTable.OccValueReal(double.NegativeInfinity));
        Assert.Equal((Int128)0, CobolTable.OccValueReal(double.NaN));
        Assert.Equal((Int128)123456789012, CobolTable.OccValueReal(123456789012.0));
    }

    /// <summary>At the temporary's capacity the intake is exact up to 10^38 − 1 and clamps the exact value past it,
    /// for the SDIDI and binary64 carriers too: the double nearest 10^38 (99999999999999997748809823456034029568) is
    /// BELOW 10^38, so a pre-check written at 1e38 saturated values the temporary holds exactly (train 1053 review).</summary>
    [Fact]
    public void ExpressionIntake_AtTheCapacity_IsExactForEveryCarrier()
    {
        Int128 cap = I("99999999999999999999999999999999999999");
        Assert.Equal(I("99999999999999998000000000000000000000"),
            CobolTable.OccValueDec(CobolDec.From(99999999999999998, -21)));
        Assert.Equal(cap, CobolTable.OccValueDec(CobolDec.From(1, -38)));
        Assert.Equal(-cap, CobolTable.OccValueDec(CobolDec.From(-1, -38)));
        Assert.Equal(I("99999999999999997748809823456034029568"), CobolTable.OccValueReal(1e38));
        Assert.Equal(cap, CobolTable.OccValueReal(1.6e38));
        Assert.Equal(-cap, CobolTable.OccValueReal(-1.6e38));
    }

    /// <summary>The temporary's value goes back through the subscript read, which is where the narrowing — and so the
    /// exact text — lives: an expression past <c>long</c> is named by its value.</summary>
    [Fact]
    public void ExpressionThenRead_NamesTheComputedValue()
    {
        Int128 position = CobolTable.OccValue(I("100000000000000000006"), 0);
        Occurrence o = CobolTable.Occ(position, Profile(38, 0, signed: true));
        Assert.Equal(long.MaxValue, o.Value);
        Assert.Equal("100000000000000000006", o.ToString());
    }
}
