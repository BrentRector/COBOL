// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PINNED BYTES (V59 step 3 — the codec). Every expected value here is computed FROM the pinned
/// representation, never from observed output: ISO/IEC 1989:2023 §13.18.60.4 GR4 gives USAGE BINARY "a radix of
/// 2" and leaves the rest to the implementor, GR11 gives PACKED-DECIMAL "a radix of 10 … the minimum possible
/// configuration", and §4.2.16 (Annex A.1 items 205/215) obliges us to DOCUMENT our choice — so these vectors
/// ARE that documentation, executable.
/// <para>
/// WiseOwl COBOL's choices, each following the IBM / Micro Focus / GnuCOBOL survey so a data file interchanges:
/// BINARY is two's complement BIG-ENDIAN in exactly <c>StorageLength</c> bytes; PACKED is BCD two digits per
/// byte with a trailing sign nibble <c>0xC</c>/<c>0xD</c>, or <c>0xF</c> when the item has no operational sign;
/// PACKED WITH NO SIGN (COBOL-2023) has no sign nibble at all.
/// </para>
/// <para>
/// The profiles are built through the COMPILER's own <c>PicInfo</c> mapping, so a test passing here is a
/// statement about what the compiler actually emits, not about a hand-written profile.
/// </para>
/// </summary>
public sealed class RecordImageCodecTests
{
    private static NumProfile P(Usage usage, int digits, bool signed, int scale = 0, bool noSign = false)
    {
        var pic = new PicInfo(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: scale,
            Signed: signed) { PackedNoSign = noSign };
        return new NumProfile
        {
            Digits = digits,
            FractionDigits = scale,
            Signed = signed,
            SignKind = usage is Usage.Display ? NumericSign.TrailingOverpunch : NumericSign.BinaryMinus,
            Truncation = pic.Truncation,
            ByteForm = pic.ByteForm,
            StorageLength = pic.StorageWidth,
        };
    }

    private static string Bytes(params int[] bytes)
    {
        var chars = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++) chars[i] = (char)bytes[i];
        return new string(chars);
    }

    private static string Hex(string image) => string.Join(" ", image.Select(c => ((int)c).ToString("X2")));

    private static void AssertImage(string expected, string actual) =>
        Assert.Equal(Hex(expected), Hex(actual));   // hex on failure — a raw Latin-1 diff is unreadable

    // ── BINARY: radix 2, two's complement, big-endian, StorageLength bytes (§13.18.60.4 GR4) ──

    [Fact]
    public void Binary_Unsigned_IsBigEndianTwosComplement()
    {
        AssertImage(Bytes(0x04, 0xD2), CobolNum.FormatImage(1234, P(Usage.Binary, 4, signed: false)));
        AssertImage(Bytes(0x63), CobolNum.FormatImage(99, P(Usage.Binary, 2, signed: false)));
        AssertImage(Bytes(0x07, 0x5B, 0xCD, 0x15), CobolNum.FormatImage(123456789, P(Usage.Binary, 9, signed: false)));
    }

    [Fact]
    public void Binary_Signed_NegativeIsTwosComplement()
    {
        AssertImage(Bytes(0x04, 0xD2), CobolNum.FormatImage(1234, P(Usage.Binary, 4, signed: true)));
        AssertImage(Bytes(0xFB, 0x2E), CobolNum.FormatImage(-1234, P(Usage.Binary, 4, signed: true)));   // 65536−1234
        AssertImage(Bytes(0xFF), CobolNum.FormatImage(-1, P(Usage.Binary, 2, signed: true)));
        AssertImage(Bytes(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF),
            CobolNum.FormatImage(-1, P(Usage.Binary, 18, signed: true)));
    }

    /// <summary>An UNSIGNED receiver holds the absolute value (ISO §14.9.25.4 GR8), so the image can never carry
    /// a two's-complement negative for one — a sign that leaked into the bytes would read back as an enormous
    /// positive.</summary>
    [Fact]
    public void Binary_Unsigned_StoresTheMagnitude()
    {
        AssertImage(Bytes(0x00, 0x05), CobolNum.FormatImage(-5, P(Usage.Binary, 4, signed: false)));
        Assert.Equal(5, CobolNum.ParseImage(Bytes(0x00, 0x05), P(Usage.Binary, 4, signed: false)));
    }

    /// <summary>The 16-byte tier: §13.18.60.4 GR4 requires storage "sufficient … to contain the maximum range of
    /// values implied by the associated decimal picture character-string", and a signed 19-digit picture
    /// (max 10^19−1) does not fit 8 bytes (2^63−1 ≈ 9.22×10^18).</summary>
    [Fact]
    public void Binary_AboveEighteenDigits_TakesTheSixteenByteTier()
    {
        var p = P(Usage.Binary, 19, signed: true);
        Assert.Equal(16, p.StorageLength);
        Int128 v = Int128.Parse("1000000000000000000");   // 10^18 = 0x0DE0B6B3A7640000
        AssertImage(Bytes(0, 0, 0, 0, 0, 0, 0, 0, 0x0D, 0xE0, 0xB6, 0xB3, 0xA7, 0x64, 0x00, 0x00),
            CobolNum.FormatImage(v, p));
        Assert.Equal(v, CobolNum.ParseImage(CobolNum.FormatImage(v, p), p));
        Assert.Equal(-v, CobolNum.ParseImage(CobolNum.FormatImage(-v, p), p));
    }

    // ── PACKED-DECIMAL: radix 10 BCD, sign nibble 0xC/0xD/0xF (§13.18.60.4 GR11) ──

    [Fact]
    public void Packed_SignNibbleIsCorDorF()
    {
        AssertImage(Bytes(0x01, 0x23, 0x4C), CobolNum.FormatImage(1234, P(Usage.Packed, 4, signed: true)));
        AssertImage(Bytes(0x01, 0x23, 0x4D), CobolNum.FormatImage(-1234, P(Usage.Packed, 4, signed: true)));
        AssertImage(Bytes(0x01, 0x23, 0x4F), CobolNum.FormatImage(1234, P(Usage.Packed, 4, signed: false)));
    }

    /// <summary>The implied decimal point occupies no nibble — the image is the UNSCALED digit run, exactly as
    /// the value is held in storage (§13.18.40.4: V occupies no position). <c>PIC S9(3)V99 COMP-3</c> = 5 digits.</summary>
    [Fact]
    public void Packed_ScaleIsImplied_NotRepresented()
    {
        AssertImage(Bytes(0x12, 0x34, 0x5C), CobolNum.FormatImage(12345, P(Usage.Packed, 5, signed: true, scale: 2)));
    }

    /// <summary>⛔ THE COLLISION: at an ODD digit count the signed and WITH NO SIGN forms occupy the SAME number
    /// of bytes and lay them out DIFFERENTLY. This is why the byte FORM, never the width, decides whether a
    /// sign nibble is present — a decoder inferring it from the byte count reads 123's last digit as a sign.</summary>
    [Fact]
    public void Packed_WithNoSign_SameWidth_DifferentBytes()
    {
        var signed3 = P(Usage.Packed, 3, signed: true);
        var noSign3 = P(Usage.Packed, 3, signed: false, noSign: true);
        Assert.Equal(2, signed3.StorageLength);
        Assert.Equal(2, noSign3.StorageLength);
        AssertImage(Bytes(0x12, 0x3C), CobolNum.FormatImage(123, signed3));
        AssertImage(Bytes(0x01, 0x23), CobolNum.FormatImage(123, noSign3));
        Assert.Equal(123, CobolNum.ParseImage(Bytes(0x01, 0x23), noSign3));
    }

    [Fact]
    public void Packed_WithNoSign_EvenDigits_IsPureBcd()
    {
        AssertImage(Bytes(0x12, 0x34), CobolNum.FormatImage(1234, P(Usage.Packed, 4, signed: false, noSign: true)));
    }

    /// <summary>Every packed producer agrees on the sign READING: 0xB and 0xD are negative, everything else
    /// positive. A file written by another COBOL system decodes correctly.</summary>
    [Theory]
    [InlineData(0x0C, 1234)]
    [InlineData(0x0F, 1234)]
    [InlineData(0x0A, 1234)]
    [InlineData(0x0E, 1234)]
    [InlineData(0x0D, -1234)]
    [InlineData(0x0B, -1234)]
    public void Packed_ForeignSignNibbles_Decode(int nibble, int expected)
    {
        var p = P(Usage.Packed, 4, signed: true);
        Assert.Equal(expected, CobolNum.ParseImage(Bytes(0x01, 0x23, 0x40 | nibble), p));
    }

    // ── ZONED: unchanged, and the codec routes to it ──

    [Fact]
    public void Zoned_RoutesToTheDisplayImage()
    {
        var p = P(Usage.Display, 4, signed: false);
        Assert.Equal("1234", CobolNum.FormatImage(1234, p));
        Assert.Equal(CobolNum.FormatDisplay(1234, p), CobolNum.FormatImage(1234, p));
        Assert.Equal(1234, CobolNum.ParseImage("1234", p));
    }

    // ── The invariants that hold for EVERY form ──

    public static TheoryData<Usage, int, bool> Grid()
    {
        var data = new TheoryData<Usage, int, bool>();
        foreach (var usage in new[] { Usage.Display, Usage.Binary, Usage.Packed })
            foreach (int digits in new[] { 1, 2, 3, 4, 5, 9, 10, 18 })
                foreach (bool signed in new[] { false, true })
                    data.Add(usage, digits, signed);
        return data;
    }

    /// <summary>THE ONE-WIDTH INVARIANT at the codec: an item's image is EXACTLY the bytes it occupies in
    /// storage — the property <c>FUNCTION BYTE-LENGTH</c> (§15.14.4 r1) and <c>FUNCTION LENGTH</c> (§15.50.4
    /// GR3) cannot disagree about.</summary>
    [Theory]
    [MemberData(nameof(Grid))]
    public void ImageIsExactlyTheStorageWidth(Usage usage, int digits, bool signed)
    {
        var p = P(usage, digits, signed);
        int expected = usage is Usage.Display ? digits : p.StorageLength;
        Assert.Equal(expected, CobolNum.FormatImage(0, p).Length);
        Assert.Equal(expected, CobolNum.FormatImage(signed ? -1 : 1, p).Length);
    }

    /// <summary>Every byte the codec emits fits the Latin-1 carrier the record image is (chars 0–255 map 1:1 to
    /// bytes) — a char above 0xFF would be a byte the framing cannot write.</summary>
    [Theory]
    [MemberData(nameof(Grid))]
    public void EveryEmittedCharIsAByte(Usage usage, int digits, bool signed)
    {
        var p = P(usage, digits, signed);
        foreach (char c in CobolNum.FormatImage(signed ? -1 : 1, p)) Assert.InRange(c, (char)0, (char)0xFF);
    }

    [Theory]
    [MemberData(nameof(Grid))]
    public void RoundTrips(Usage usage, int digits, bool signed)
    {
        var p = P(usage, digits, signed);
        Int128 cap = Int128.One;
        for (int i = 0; i < digits; i++) cap *= 10;
        foreach (Int128 v in new[] { Int128.Zero, Int128.One, cap - 1, cap / 2 })
        {
            Assert.Equal(v, CobolNum.ParseImage(CobolNum.FormatImage(v, p), p));
            if (signed) Assert.Equal(-v, CobolNum.ParseImage(CobolNum.FormatImage(-v, p), p));
            else Assert.Equal(v, CobolNum.ParseImage(CobolNum.FormatImage(-v, p), p));   // magnitude (GR8)
        }
    }

    /// <summary>A form-less profile (USAGE INDEX, §13.18.60.4 GR10) at a byte boundary is a compiler invariant
    /// break. It fails LOUD — inventing bytes for it is the class of defect the codec exists to retire.</summary>
    [Fact]
    public void NoByteForm_FailsLoud()
    {
        var index = new NumProfile
        {
            Digits = 0, FractionDigits = 0, Signed = false,
            Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.None,
        };
        Assert.Throws<InvalidOperationException>(() => CobolNum.FormatImage(1, index));
        Assert.Throws<InvalidOperationException>(() => CobolNum.ParseImage("x", index));
    }
}
