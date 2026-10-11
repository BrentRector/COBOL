// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The standard-decimal OPERAND lift of a fixed-point value wider than the SDIDI's 34 digits (kb/Work PB2918):
/// ISO §8.8.1.5.2 r1 — "If the operand value cannot be expressed exactly in an SDIDI, the value is rounded
/// according to the rules in 11.9.11, INTERMEDIATE ROUNDING clause" (cite.py --check, OK 8.8.1.5.2 1)). A 16-byte
/// binary container owns 39 digits, so <c>Int128.MinValue</c> (−2^127) and the whole unsigned range [0, 2^128) arrive
/// at the lift. Expected values are computed from the rule with decimal arithmetic outside the runtime.
/// </summary>
public sealed class CobolDecWideLiftTests
{
    // -2^127 = -170141183460469231731687303715884105728: dropped digits 05728, round digit 0, so every mode that
    // rounds to nearest keeps -1701411834604692317316873037158841 × 10^5 (§11.9.11 admits only NEAREST-AWAY-FROM-ZERO,
    // NEAREST-EVEN, PROHIBITED and TRUNCATION).
    private static readonly Int128 Sig34Of2p127 = Int128.Parse("1701411834604692317316873037158841");

    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero)]
    [InlineData(CobolRounding.NearestEven)]
    [InlineData(CobolRounding.Truncation)]
    public void SignedMinimum_RoundsToTheSdidiPerMode(CobolRounding mode)
    {
        CobolDec d = CobolDec.FromParsed(Int128.MinValue, 0, mode);
        Assert.Equal(new CobolDec(-Sig34Of2p127, 5), d);
    }

    [Fact]
    public void SignedMinimum_Prohibited_IsTheTruncationCondition()
    {
        var e = Assert.Throws<CobolSizeError>(() => CobolDec.FromParsed(Int128.MinValue, 0, CobolRounding.Prohibited));
        Assert.Equal("EC-SIZE-TRUNCATION", e.EcName);   // §11.9.11.2 r3d
    }

    // 3 × 2^124 = 63802943797675961899382738893456539648: dropped digits 9648, round digit 9.
    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, "6380294379767596189938273889345654")]
    [InlineData(CobolRounding.NearestEven, "6380294379767596189938273889345654")]
    [InlineData(CobolRounding.Truncation, "6380294379767596189938273889345653")]
    public void ThirtyEightDigitValue_RoundsOnceAtThirtyFourDigits(CobolRounding mode, string sig)
    {
        Int128 v = Int128.Parse("63802943797675961899382738893456539648");
        Assert.Equal(new CobolDec(Int128.Parse(sig), 4), CobolDec.FromParsed(v, 0, mode));
        Assert.Equal(new CobolDec(Int128.Parse(sig), 4 - 2), CobolDec.FromParsed(v, -2, mode));   // scale 2: value / 100
    }

    [Fact]
    public void ThirtyFourDigitValue_PassesExactly()
    {
        Int128 v = -Int128.Parse("1000000000000000000000000000000000");
        Assert.Equal(new CobolDec(v, -3), CobolDec.FromParsed(v, -3, CobolRounding.Prohibited));
    }

    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, "3402823669209384634633746074317682")]
    [InlineData(CobolRounding.Truncation, "3402823669209384634633746074317682")]
    public void UnsignedMaximum_RoundsToTheSdidiPerMode(CobolRounding mode, string sig)
    {
        // 2^128 - 1 = 340282366920938463463374607431768211455: dropped digits 11455.
        Assert.Equal(new CobolDec(Int128.Parse(sig), 5), CobolDec.FromUnsigned(UInt128.MaxValue, 0, mode));
    }

    [Fact]
    public void UnsignedLift_ZeroAndExactValuesAreUnrounded()
    {
        Assert.Equal(CobolDec.From(0, 0), CobolDec.FromUnsigned(0, 3, CobolRounding.Prohibited));
        Assert.Equal(new CobolDec(12345, -2), CobolDec.FromUnsigned(12345, 2, CobolRounding.Prohibited));
    }

    [Fact]
    public void RoundedMinimum_Plus1_IsTheSameSdidi_AndLandsInASignedSixteenByteReceiver()
    {
        // COMPUTE R = S + 1, S = -2^127, R PIC S9(19) COMP-5: the sum is -1701411834604692317316873037158841E5 again
        // (-...099999 rounds away to -...100000), whose value fits Int128, so the CHECKED transfer holds it.
        CobolDec s = CobolDec.FromParsed(Int128.MinValue, 0, CobolRounding.NearestAwayFromZero);
        CobolDec sum = CobolDec.Add(s, CobolDec.From(1, 0), CobolRounding.NearestAwayFromZero);
        Assert.Equal(s, sum);
        Assert.Equal(-Int128.Parse("170141183460469231731687303715884100000"),
            sum.ToUnscaledChecked(0, CobolRounding.Truncation));
    }

    [Fact]
    public void ToUnscaled_AcceptsEveryValueTheCarrierHolds_NotOnlyThirtyEightDigits()
    {
        // 39-digit values below 2^127 fit Int128; the old digit-count test (> 38) refused them all.
        CobolDec big = new(Int128.Parse("1701411834604692317316873037158841"), 5);   // 1.70141…E+38 < 2^127
        Assert.Equal(Int128.Parse("170141183460469231731687303715884100000"), big.ToUnscaledChecked(0, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("170141183460469231731687303715884100000"), big.ToUnscaledIntermediate(0, CobolRounding.Truncation));
        // Past 2^127 the carrier cannot hold it: the checked transfer is the size error, never the low-order digits.
        CobolDec past = new(Int128.Parse("1701411834604692317316873037158842"), 5);
        Assert.Throws<CobolSizeError>(() => past.ToUnscaledChecked(0, CobolRounding.Truncation));
        Assert.Throws<CobolSizeError>(() => past.ToUnscaledIntermediate(0, CobolRounding.Truncation));
    }

    [Fact]
    public void ToUnscaled_TheMinimumItself_IsNotAnOverflowOfAbs()
    {
        // Int128.Abs(MinValue) throws OverflowException; the carrier-fit test is written on the signed value.
        CobolDec min = new(Int128.MinValue, 0);
        Assert.Equal(Int128.MinValue, min.ToUnscaledChecked(0, CobolRounding.Truncation));
    }

    // ── The UNSIGNED 16-byte landing (kb/Work PB2966) ─────────────────────────────────────────────────────────────
    // An unsigned PIC 9(19) COMP-5 item owns [0, 2^128) (§13.18.60.4 GR12, cite.py --check OK 13.18.60.4 12)), so a
    // standard-decimal result in [2^127, 2^128) is not "further from zero than permitted for the associated resultant
    // data item" (§14.7.5 case 3, cite.py --check OK 14.7.5 3)). The Int128 landing could not carry it.

    private static readonly NumProfile Unsigned16 = new()
    {
        Digits = 19,
        FractionDigits = 0,
        Signed = false,
        Truncation = NumericTruncation.BinaryCapacity,
        ByteForm = NumericByteForm.Binary,
        StorageLength = 16,
    };

    // 2^128 - 1 lifts to 340282366920938463463374607431768200000 (34 digits, the 5 dropped digits 11455 round down).
    private static readonly UInt128 RoundedMaximum = UInt128.Parse("340282366920938463463374607431768200000");

    [Fact]
    public void UnsignedMaximum_PlusZero_LandsInTheUnsignedSixteenByteReceiver()
    {
        CobolDec w = CobolDec.FromUnsigned(UInt128.MaxValue, 0, CobolRounding.NearestAwayFromZero);
        CobolDec sum = CobolDec.Add(w, CobolDec.From(0, 0), CobolRounding.NearestAwayFromZero);
        Assert.Equal(RoundedMaximum, sum.ToUnscaledMagnitude(0, CobolRounding.Truncation, checkedTransfer: true));
        Assert.Equal(RoundedMaximum, sum.ToUnscaledMagnitude(0, CobolRounding.Truncation, checkedTransfer: false));

        // Through the receiver's own store: the container's bits, which the emitted unchecked((UInt128)) cast reads back.
        Assert.True(CobolNum.TryStore(sum, Unsigned16, CobolRounding.Truncation, out Int128 stored));
        Assert.Equal(RoundedMaximum, unchecked((UInt128)stored));
        Assert.Equal(RoundedMaximum, unchecked((UInt128)CobolNum.Store(sum, Unsigned16, CobolRounding.Truncation)));
        Assert.Equal(RoundedMaximum, unchecked((UInt128)CobolNum.StoreOrRaise(sum, Unsigned16, CobolRounding.Truncation)));
    }

    [Fact]
    public void UnsignedReceiver_StoresTheMagnitudeOfANegativeResultPastInt128()
    {
        CobolDec negative = new(-Int128.Parse("3402823669209384634633746074317682"), 5);
        Assert.Equal(RoundedMaximum, negative.ToUnscaledMagnitude(0, CobolRounding.Truncation, checkedTransfer: true));
    }

    [Fact]
    public void UnsignedReceiver_AResultPastTwoToThe128_IsTheCheckedSizeError_AndTheUncheckedLowOrderDigits()
    {
        CobolDec past = new(Int128.Parse("6805647338418769269267492148635364"), 5);   // 6.8E+38 > 2^128
        Assert.Throws<CobolSizeError>(() => past.ToUnscaledMagnitude(0, CobolRounding.Truncation, checkedTransfer: true));
        Assert.False(CobolNum.TryStore(past, Unsigned16, CobolRounding.Truncation, out _));
        // The unchecked disposition is the signed twin's: the low-order digits a <= 38-digit store can use.
        // 680564733841876926926749214863536400000 keeps its 33 low-order significand digits (805647338418769269267492148635364)
        // followed by the five scale zeros.
        Assert.Equal(UInt128.Parse("80564733841876926926749214863536400000"),
            past.ToUnscaledMagnitude(0, CobolRounding.Truncation, checkedTransfer: false));
    }

    [Fact]
    public void UnsignedMagnitude_AtAFractionScale_RoundsWithTheReceiversMode()
    {
        CobolDec d = new(Int128.Parse("123456"), -3);   // 123.456
        Assert.Equal((UInt128)123, d.ToUnscaledMagnitude(0, CobolRounding.Truncation, checkedTransfer: true));
        Assert.Equal((UInt128)124, d.ToUnscaledMagnitude(0, CobolRounding.AwayFromZero, checkedTransfer: true));
        Assert.Equal((UInt128)123456, d.ToUnscaledMagnitude(3, CobolRounding.Prohibited, checkedTransfer: true));
        Assert.Throws<CobolSizeError>(() => d.ToUnscaledMagnitude(0, CobolRounding.Prohibited, checkedTransfer: true));   // §14.7.4.3 r7
    }

    [Fact]
    public void SignedSixteenByteReceiver_StillTakesTheInt128Landing()
    {
        var signed16 = Unsigned16 with { Signed = true };
        CobolDec past = new(Int128.Parse("3402823669209384634633746074317682"), 5);   // past 2^127 - 1
        Assert.False(CobolNum.TryStore(past, signed16, CobolRounding.Truncation, out _));
    }
}
