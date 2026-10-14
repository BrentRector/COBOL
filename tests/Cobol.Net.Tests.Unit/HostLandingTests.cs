// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DOOR RULES OF THE .NET HOST ENTRY ARE ONE FUNCTION, AND IT LOSES NOTHING (DESIGN-external-repository §4.3, §17.3
/// "The door rules are one function"; kb/Work PB2099). ISO §14.2.3 9) and 10) land a host value into a record of the
/// formal's description by "a COMPUTE statement without the ROUNDED phrase" (numeric) or "a MOVE statement" (otherwise),
/// and §14.2.3 13) lets the implementor state the restrictions for a non-COBOL activator: the one stated for .NET is that
/// the landing must be exact. This pins <see cref="HostLanding"/> on the loss fixture the design names — high order, low
/// order, sign, P positions, each binary capacity, string length, boolean characters — refused, the exact boundary values
/// accepted, and the validity decode agreeing with <see cref="CobolNum.IsNumericImage"/> per byte form. Every expected
/// value is derived from the description's capacity rule, never from the code's output.
/// </summary>
public sealed class HostLandingTests
{
    private static NumProfile Display(int digits, int scale, bool signed) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = signed, SignKind = NumericSign.TrailingOverpunch,
        Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
    };

    private static NumProfile Packed(int digits, int scale, bool signed) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = signed, SignKind = NumericSign.BinaryMinus,
        Truncation = NumericTruncation.PackedDecimal, ByteForm = NumericByteForm.Packed, StorageLength = digits / 2 + 1,
    };

    /// <summary>A binary item whose truncation is by capacity (BINARY-CHAR … BINARY-DOUBLE, COMP-5): its whole
    /// <paramref name="bytes"/>-byte container (ISO §13.18.60.4 12); kb/Work R10).</summary>
    private static NumProfile Capacity(int digits, int scale, bool signed, int bytes) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = signed, SignKind = NumericSign.BinaryMinus,
        Truncation = NumericTruncation.BinaryCapacity, ByteForm = NumericByteForm.Binary, StorageLength = bytes,
    };

    private static void Refused(Action land, string parameter)
    {
        var e = Assert.Throws<ArgumentOutOfRangeException>(land);
        Assert.Equal(parameter, e.ParamName);
    }

    [Fact]
    public void ScaledDecimal_LandsExactly_AndRefusesEveryLoss()
    {
        var amount = Display(9, 2, signed: true);   // PIC S9(7)V99
        Assert.Equal((Int128)1234567, HostLanding.Land(12345.67m, amount, "LK_AMOUNT", null));
        Assert.Equal((Int128)1234560, HostLanding.Land(12345.6m, amount, "LK_AMOUNT", null));
        Assert.Equal((Int128)1234560, HostLanding.Land(12345.600m, amount, "LK_AMOUNT", null));   // trailing zeros are exact
        Assert.Equal((Int128)999999999, HostLanding.Land(9999999.99m, amount, "LK_AMOUNT", null));
        Assert.Equal((Int128)(-999999999), HostLanding.Land(-9999999.99m, amount, "LK_AMOUNT", null));
        Refused(() => HostLanding.Land(12345.678m, amount, "LK_AMOUNT", null), "LK_AMOUNT");    // low order
        Refused(() => HostLanding.Land(10000000m, amount, "LK_AMOUNT", null), "LK_AMOUNT");     // high order
        Refused(() => HostLanding.Land(-1.5m, Display(9, 2, signed: false), "U", null), "U");   // sign
        Assert.Equal((Int128)0, HostLanding.Land(-0.00m, Display(9, 2, signed: false), "U", null)); // a zero has no sign to lose
    }

    [Fact]
    public void Integer_RefusesTheHighOrderDigitAndTheSign()
    {
        var count = Display(4, 0, signed: false);   // PIC 9(4)
        Assert.Equal((Int128)9999, HostLanding.Land(9999L, count, "N", null));
        Assert.Equal((Int128)0, HostLanding.Land(0L, count, "N", null));
        Refused(() => HostLanding.Land(10000L, count, "N", null), "N");
        Refused(() => HostLanding.Land(-1L, count, "N", null), "N");
        var wide = Display(31, 0, signed: true);    // PIC S9(31)
        Int128 max31 = Int128.Parse("9999999999999999999999999999999");
        Assert.Equal(max31, HostLanding.Land(max31, wide, "W", null));
        Refused(() => HostLanding.Land(max31 + 1, wide, "W", null), "W");
    }

    [Fact]
    public void PPositions_HoldOnlyZeros()
    {
        var hundreds = Display(2, -3, signed: false);   // PIC 99PPP: a multiple of 1000 from 0 to 99000
        Assert.Equal((Int128)12, HostLanding.Land(12000L, hundreds, "P", null));
        Assert.Equal((Int128)99, HostLanding.Land(99000L, hundreds, "P", null));
        Refused(() => HostLanding.Land(12345L, hundreds, "P", null), "P");    // nonzero digits in the P positions
        Refused(() => HostLanding.Land(100000L, hundreds, "P", null), "P");
        var leading = Display(2, 4, signed: false);     // PIC VPP99: 0.0000 to 0.0099
        Assert.Equal((Int128)99, HostLanding.Land(0.0099m, leading, "Q", null));
        Refused(() => HostLanding.Land(0.0100m, leading, "Q", null), "Q");
    }

    [Fact]
    public void EachBinaryCapacity_IsItsContainer()
    {
        var shortSigned = Capacity(4, 0, signed: true, bytes: 2);      // BINARY-SHORT SIGNED: [-32768, 32767]
        Assert.Equal((Int128)32767, HostLanding.Land(32767L, shortSigned, "S", null));
        Assert.Equal((Int128)(-32768), HostLanding.Land(-32768L, shortSigned, "S", null));
        Refused(() => HostLanding.Land(32768L, shortSigned, "S", null), "S");
        Refused(() => HostLanding.Land(-32769L, shortSigned, "S", null), "S");
        var shortUnsigned = Capacity(4, 0, signed: false, bytes: 2);   // BINARY-SHORT UNSIGNED: [0, 65535]
        Assert.Equal((Int128)65535, HostLanding.Land(65535L, shortUnsigned, "U", null));
        Refused(() => HostLanding.Land(65536L, shortUnsigned, "U", null), "U");
        var doubleUnsigned = Capacity(18, 0, signed: false, bytes: 8);  // BINARY-DOUBLE UNSIGNED: [0, 2^64)
        Int128 top = (Int128)ulong.MaxValue;
        Assert.Equal(top, HostLanding.Land(top, doubleUnsigned, "D", null));
        Refused(() => HostLanding.Land(top + 1, doubleUnsigned, "D", null), "D");
        var wideUnsigned = Capacity(20, 0, signed: false, bytes: 16);   // PIC 9(20) COMP-5: [0, 2^128)
        Assert.Equal(unchecked((Int128)UInt128.MaxValue), HostLanding.Land(UInt128.MaxValue, wideUnsigned, "X", null));
        var scaledShort = Capacity(4, 2, signed: true, bytes: 2);       // PIC S99V99 COMP-5: [-327.68, 327.67]
        Assert.Equal((Int128)32767, HostLanding.Land(327.67m, scaledShort, "C", null));
        Refused(() => HostLanding.Land(327.68m, scaledShort, "C", null), "C");
        Refused(() => HostLanding.Land(1.001m, scaledShort, "C", null), "C");
    }

    [Fact]
    public void CobolDec_LandsPastDecimalsReach()
    {
        var thirty = Display(30, 10, signed: true);   // PIC S9(20)V9(10): past decimal's 28 digits
        var value = new CobolDec(Int128.Parse("123456789012345678901234567890"), -10);
        Assert.Equal(value.Sig, HostLanding.Land(value, thirty, "B", null));
        Refused(() => HostLanding.Land(new CobolDec(1, -11), thirty, "B", null), "B");   // an eleventh fraction digit
        Refused(() => HostLanding.Land(new CobolDec(-1, 0), Display(30, 10, signed: false), "B", null), "B");
    }

    [Fact]
    public void FloatingPointFormal_IsNotLandedThroughTheFixedPointCompute()
    {
        var single = new NumProfile
        {
            Digits = 0, FractionDigits = 0, Signed = true, Truncation = NumericTruncation.DigitCount,
            ByteForm = NumericByteForm.Ieee32,
        };
        Assert.Throws<InvalidOperationException>(() => HostLanding.Land(1L, single, "F", null));
    }

    [Fact]
    public void Text_IsTheMovesAlignment_AndRefusesTruncationAndForeignBooleanCharacters()
    {
        Assert.Equal("AB  ", HostLanding.LandText("AB", 4, justified: false, boolean: false, "T", null));
        Assert.Equal("  AB", HostLanding.LandText("AB", 4, justified: true, boolean: false, "T", null));
        Assert.Equal("ABCD", HostLanding.LandText("ABCD", 4, justified: false, boolean: false, "T", null));
        Refused(() => HostLanding.LandText("ABCDE", 4, justified: false, boolean: false, "T", null), "T");
        Assert.Throws<ArgumentNullException>(() => HostLanding.LandText(null, 4, justified: false, boolean: false, "T", null));
        // §13.18.32.4 2): zero fill for boolean positions — on the right ordinarily, on the left under JUSTIFIED.
        Assert.Equal("010100", HostLanding.LandText("0101", 6, justified: false, boolean: true, "B", null));
        Assert.Equal("0011", HostLanding.LandText("11", 4, justified: true, boolean: true, "B", null));
        Refused(() => HostLanding.LandText("012", 4, justified: false, boolean: true, "B", null), "B");
        var e = Assert.Throws<ArgumentOutOfRangeException>(
            () => HostLanding.LandText("TOO LONG", 4, justified: false, boolean: false, "LK_REC", "LK-NAME"));
        Assert.Equal("LK_REC", e.ParamName);
        Assert.Contains("LK-NAME", e.Message);
        Assert.Equal("ABC", HostLanding.LandVariable("ABC", 5, boolean: false, "V", null));
        Assert.Equal(new string('X', 9000), HostLanding.LandVariable(new string('X', 9000), CobolArg.Unstated, false, "V", null));
        Refused(() => HostLanding.LandVariable("ABCDEF", 5, boolean: false, "V", null), "V");
    }

    [Fact]
    public void OneVariable_IsOneStorage_OnlyForOneDescription()
    {
        Assert.True(HostLanding.SharesStorage(sameVariable: true, sameDescription: true, "A", "B"));
        Assert.False(HostLanding.SharesStorage(sameVariable: false, sameDescription: false, "A", "B"));
        var e = Assert.Throws<ArgumentException>(
            () => HostLanding.SharesStorage(sameVariable: true, sameDescription: false, "LK_A", "LK_B"));
        Assert.Contains("LK_A", e.Message);
        Assert.Contains("LK_B", e.Message);
    }

    [Fact]
    public void OccursArray_HasTheTablesLength()
    {
        int[] three = [1, 2, 3];
        Assert.Same(three, HostLanding.RequireOccurrences(three, 3, 3, 3, "R", "T"));
        Assert.Throws<ArgumentException>(() => HostLanding.RequireOccurrences(three, 4, 4, 4, "R", "T"));
        Assert.Throws<ArgumentNullException>(() => HostLanding.RequireOccurrences<int>(null, 3, 3, 3, "R", "T"));
        Assert.Same(three, HostLanding.RequireOccurrences(three, 3, 1, 5, "R", "T"));   // OCCURS 1 TO 5 DEPENDING ON = 3
        Assert.Throws<ArgumentException>(() => HostLanding.RequireOccurrences(three, 6, 1, 5, "R", "T"));
    }

    [Fact]
    public void Decodes_AreExact()
    {
        Assert.Equal(12345.67m, HostLanding.Decimal(1234567, 2));
        Assert.Equal(-0.0000000000000000000000000001m, HostLanding.Decimal(-1, 28));
        Assert.Equal(9999999999999999999999999999m, HostLanding.Decimal(Int128.Parse("9999999999999999999999999999"), 0));
        Assert.Equal((Int128)12000, HostLanding.Integer(12, -3));
        Assert.Equal((Int128)(-42), HostLanding.Integer(-42, 0));
        // Outside the row's reach is loud, never a truncated significand or a wrapped product.
        Assert.Throws<OverflowException>(() => HostLanding.Decimal((Int128)1 << 96, 0));
        Assert.Throws<OverflowException>(() => HostLanding.Decimal(1, 29));
        Assert.Throws<OverflowException>(() => HostLanding.Integer(Int128.MaxValue / 10 + 1, -1));
        Assert.Equal(new CobolDec(5, -3), HostLanding.Dec(5, 3));
        FormatException? failure = null;
        Assert.True(HostLanding.TryDec((UInt128)Int128.MaxValue, 2, "W", ref failure, out var below));
        Assert.Equal(new CobolDec(Int128.MaxValue, -2), below);
        Assert.Null(failure);
        Assert.False(HostLanding.TryDec((UInt128)Int128.MaxValue + 1, 2, "W", ref failure, out _));
        Assert.Contains("'W'", failure!.Message);
    }

    [Fact]
    public void ValidityDecode_AgreesWithTheClassTestNumeric_PerByteForm()
    {
        var zoned = Display(4, 0, signed: false);
        var packed = Packed(5, 2, signed: true);
        var binary = new NumProfile
        {
            Digits = 4, FractionDigits = 0, Signed = false, SignKind = NumericSign.BinaryMinus,
            Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Binary, StorageLength = 2,
        };
        (string Image, NumProfile Item)[] fixture =
        [
            ("0123", zoned), ("9999", zoned), ("ABC ", zoned), ("12 4", zoned), ("    ", zoned),
            (CobolNum.FormatImage(-12345, packed), packed), (CobolNum.FormatImage(99999, packed), packed), ("ÿÿÿ", packed),
            (CobolNum.FormatImage(9999, binary), binary), (CobolNum.FormatImage(10000, binary), binary),
        ];
        foreach (var (image, item) in fixture)
        {
            FormatException? failure = null;
            bool decoded = HostLanding.TryDecode(image, item, "LK-N", ref failure, out Int128 unscaled);
            Assert.Equal(CobolNum.IsNumericImage(image, item), decoded);
            Assert.Equal(decoded, failure is null);
            if (decoded) Assert.Equal(CobolNum.ParseImage(image, item), unscaled);
        }
        // The values, from the descriptions: zoned "0123" is 123; the packed image of -123.45 decodes to it.
        FormatException? none = null;
        Assert.True(HostLanding.TryDecode("0123", zoned, "N", ref none, out Int128 v));
        Assert.Equal((Int128)123, v);
        Assert.True(HostLanding.TryDecode(CobolNum.FormatImage(-12345, packed), packed, "N", ref none, out v));
        Assert.Equal((Int128)(-12345), v);
        // A PIC 9(4) BINARY item's image of 10000 is not a value of its PICTURE (the class test is false).
        Assert.False(CobolNum.IsNumericImage(CobolNum.FormatImage(10000, binary), binary));
        // The FIRST refusal is kept: the entry throws it, naming the formal and quoting the characters.
        FormatException? first = null;
        Assert.False(HostLanding.TryDecode("ABC ", zoned, "HRET", ref first, out _));
        Assert.False(HostLanding.TryDecode("XYZ ", zoned, "OTHER", ref first, out _));
        Assert.Contains("'HRET'", first!.Message);
        Assert.Contains("[ABC ]", first.Message);
        Assert.False(HostLanding.TryDecode(null, zoned, "NONE", ref none, out _));
    }
}
