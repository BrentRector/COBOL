// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ THE MAGNITUDE OF AN Int128 CARRIER VALUE IS <see cref="CobolDec.UAbs"/>'S, AND IT IS
/// MinValue-SAFE (kb/Work PB2639). ISO §14.9.25.4 GR6 b — "When an unsigned numeric item is the receiving item,
/// the absolute value of the sending value is used" — and §14.7.5 case 3 — a size error only when the result is
/// "further from zero than permitted for the associated resultant data item" — are both about the absolute
/// value, and the signed 16-byte minimum, −2^127, has one: 2^127, which an unsigned 16-byte container holds.
/// <c>Int128.Abs</c> throws on it, and the throw surfaced as a FALSE size error on the statement lane and as a raw
/// <see cref="OverflowException"/> on <see cref="CobolNum.StoreOrRaise(Int128, int, in NumProfile, CobolRounding)"/>.</summary>
public sealed class CobolNumMagnitudeTests
{
    private static NumProfile Bin(int bytes, bool signed) => new()
    {
        Digits = bytes switch { 1 => 3, 2 => 5, 4 => 10, 8 => signed ? 19 : 20, _ => signed ? 39 : 39 },
        FractionDigits = 0,
        Signed = signed,
        Truncation = NumericTruncation.BinaryCapacity,
        ByteForm = NumericByteForm.Binary,
        StorageLength = bytes,
    };

    [Fact]
    public void Magnitude_OfMinValue_IsTwoToTheOneTwentySeven()
    {
        Assert.Equal((UInt128)1 << 127, CobolDec.UAbs(Int128.MinValue));
        Assert.Equal((UInt128)Int128.MaxValue, CobolDec.UAbs(Int128.MaxValue));
        Assert.Equal((UInt128)5, CobolDec.UAbs(-5));
        Assert.Equal((UInt128)0, CobolDec.UAbs(0));
    }

    /// <summary>An unsigned 16-byte receiver HOLDS 2^127: the checked store accepts it and returns the
    /// container's bits (MinValue — the 16-byte tier's documented contract, whose <c>unchecked((UInt128))</c>
    /// reinterprets them exactly).</summary>
    [Fact]
    public void TryStore_Unsigned16Byte_AcceptsTheMagnitudeOfMinValue()
    {
        Assert.True(CobolNum.TryStore(Int128.MinValue, 0, Bin(16, signed: false), CobolRounding.Truncation, out Int128 stored));
        Assert.Equal(Int128.MinValue, stored);
        Assert.Equal(Int128.MinValue, CobolNum.Store(Int128.MinValue, 0, Bin(16, signed: false)));
    }

    /// <summary>A narrower unsigned container does not: 2^127 is past it, which is the size error the throw
    /// produced by accident — now by the comparison — and the no-phrase store keeps the container's low-order
    /// residue (2^127 mod 2^64 = 0), where it used to throw.</summary>
    [Fact]
    public void Unsigned8Byte_RejectsMinValueByRangeAndWrapsItToZero()
    {
        Assert.False(CobolNum.TryStore(Int128.MinValue, 0, Bin(8, signed: false), CobolRounding.Truncation, out _));
        Assert.Equal(Int128.Zero, CobolNum.Store(Int128.MinValue, 0, Bin(8, signed: false)));
    }

    /// <summary>The raising lane (CALL / INVOKE argument crossings under EC-SIZE checking) raised a raw
    /// <see cref="OverflowException"/>; it is the §14.7.5 size error like every sibling.</summary>
    [Fact]
    public void StoreOrRaise_OfMinValue_IsASizeErrorNotAnOverflowException()
    {
        Assert.Throws<CobolSizeError>(() => CobolNum.StoreOrRaise(Int128.MinValue, 0, Bin(8, signed: false)));
        Assert.Equal(Int128.MinValue, CobolNum.StoreOrRaise(Int128.MinValue, 0, Bin(16, signed: false)));
        Assert.Equal(Int128.MinValue, CobolNum.StoreOrRaise(Int128.MinValue, 0, Bin(16, signed: true)));
    }

    /// <summary>The widening store-cap form keeps the decimal low-order digits of MinValue (a 39-digit
    /// magnitude): widening by one place drops everything above the 37th digit of 2^127 BEFORE the multiply,
    /// exactly as it does for any other 39-digit magnitude.</summary>
    [Fact]
    public void RescaleStoreCap_OfMinValue_KeepsTheLowOrderDigits()
    {
        var keep = System.Numerics.BigInteger.Pow(10, 37);
        var expected = -(System.Numerics.BigInteger.Pow(2, 127) % keep * 10);
        Assert.Equal((Int128)expected, CobolNum.RescaleStoreCap(Int128.MinValue, 0, 1, CobolRounding.Truncation));
    }
}
