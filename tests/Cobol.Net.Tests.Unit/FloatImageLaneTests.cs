// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE BYTE-LEVEL FLOAT LANE PINS (kb/Work PB164 wave 2). The conformance golden round-trips a float group
/// through FormatImageFloat AND ParseImageFloat, so it is SELF-INVERSE: a byte-order, width or reinterpret
/// error that both lanes share cancels out and the golden stays green (the wave-2 review fleet's finding).
/// These facts state the bytes THEMSELVES — the IEEE 754 interchange encodings §13.18.60.4 GR14/GR15 pin
/// (rendered per §13.18.60.4 GR19's endianness, big-endian being our documented §11.9.8.3 SR1 default) —
/// so each lane is measured against the standard, not against its own inverse. The unsigned wide-carrier
/// lanes (wave 1's ulong/UInt128 FormatImage/StoreImage) get the same treatment.
/// </summary>
public sealed class FloatImageLaneTests
{
    private static NumProfile Ieee(NumericByteForm form, bool little = false) => new()
    {
        Digits = 0,
        FractionDigits = 0,
        Signed = true,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = form,
        StorageLength = form is NumericByteForm.Ieee32 ? 4 : 8,
        FloatLittleEndian = little,
    };

    private static string Chars(params int[] bytes)
    {
        var chars = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++) chars[i] = StorageByte.ToChar((byte)bytes[i]);   // the storage-byte law
        return new string(chars);
    }

    // 1.5f = 1.1b × 2^0 → sign 0, exponent 127 (0x7F), fraction 0x400000 → bits 0x3FC00000.
    private static readonly string Big32OnePointFive = Chars(0x3F, 0xC0, 0x00, 0x00);
    // -2.25 = -1.001b × 2^1 → sign 1, exponent 1024 (0x400), fraction 0x2000000000000 → bits 0xC002000000000000.
    private static readonly string Big64MinusTwoTwentyFive = Chars(0xC0, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

    [Fact]
    public void FormatImageFloat_Ieee32_IsBigEndianBinary32()
        => Assert.Equal(Big32OnePointFive, CobolNum.FormatImageFloat(1.5, Ieee(NumericByteForm.Ieee32)));

    [Fact]
    public void FormatImageFloat_Ieee64_IsBigEndianBinary64()
        => Assert.Equal(Big64MinusTwoTwentyFive, CobolNum.FormatImageFloat(-2.25, Ieee(NumericByteForm.Ieee64)));

    [Fact]
    public void ParseImageFloat_InvertsBothWidths()
    {
        Assert.Equal(1.5, CobolNum.ParseImageFloat(Big32OnePointFive, Ieee(NumericByteForm.Ieee32)));
        Assert.Equal(-2.25, CobolNum.ParseImageFloat(Big64MinusTwoTwentyFive, Ieee(NumericByteForm.Ieee64)));
    }

    /// <summary>HIGH-ORDER-RIGHT (§13.18.60.4 GR19b via the OPTIONS FLOAT-BINARY clause, §11.9.8): the SAME
    /// interchange encoding with the byte sequence exactly reversed — asserted against literal bytes, not
    /// against the big-endian lane's output reversed by the test (that would re-trust the subject).</summary>
    [Fact]
    public void FloatLittleEndian_ReversesTheByteSequence()
    {
        var little32 = Ieee(NumericByteForm.Ieee32, little: true);
        var little64 = Ieee(NumericByteForm.Ieee64, little: true);
        Assert.Equal(Chars(0x00, 0x00, 0xC0, 0x3F), CobolNum.FormatImageFloat(1.5, little32));
        Assert.Equal(Chars(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0xC0), CobolNum.FormatImageFloat(-2.25, little64));
        Assert.Equal(1.5, CobolNum.ParseImageFloat(Chars(0x00, 0x00, 0xC0, 0x3F), little32));
        Assert.Equal(-2.25, CobolNum.ParseImageFloat(Chars(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0xC0), little64));
    }

    /// <summary>A double entering an Ieee32 profile narrows to binary32 FIRST (the item's own precision) —
    /// 0.1d and 0.1f have different bit patterns, so a lane that formatted the double's high bytes instead of
    /// narrowing would produce a different (and unparseable-as-float) image.</summary>
    [Fact]
    public void Ieee32Lane_NarrowsToBinary32BeforeFormatting()
    {
        string image = CobolNum.FormatImageFloat(0.1, Ieee(NumericByteForm.Ieee32));
        Assert.Equal(4, image.Length);
        Assert.Equal((double)0.1f, CobolNum.ParseImageFloat(image, Ieee(NumericByteForm.Ieee32)));
    }

    [Fact]
    public void StoreImageFloat_TargetsTheCarrierTypes()
    {
        Assert.Equal(1.5f, CobolNum.StoreImage(Big32OnePointFive, Ieee(NumericByteForm.Ieee32), 0f));
        Assert.Equal(-2.25, CobolNum.StoreImage(Big64MinusTwoTwentyFive, Ieee(NumericByteForm.Ieee64), 0d));
    }

    /// <summary>The wave-1 UNSIGNED wide-carrier lanes, byte-level: a ulong-carried COMP-5 (unsigned 8-byte
    /// container) formats its full container value big-endian and stores it back bit-identically — including
    /// the top-bit-set half of the range a signed decode would mangle.</summary>
    [Fact]
    public void UnsignedCarrierLane_FormatsFullContainerBigEndian()
    {
        var profile = new NumProfile
        {
            Digits = 10,
            FractionDigits = 0,
            Signed = false,
            Truncation = NumericTruncation.BinaryCapacity,
            ByteForm = NumericByteForm.Binary,
            StorageLength = 8,
        };
        Assert.Equal(Chars(0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08),
            CobolNum.FormatImage(0x0102030405060708UL, profile));
        Assert.Equal(Chars(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF),
            CobolNum.FormatImage(ulong.MaxValue, profile));
        Assert.Equal(ulong.MaxValue,
            CobolNum.StoreImage(Chars(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF), profile, 0UL));
    }

    /// <summary>⛔ THE 16-BYTE UNSIGNED LANE ABOVE 2^127 (kb/Work PB164 — the Step D review's sweep-note #409).
    /// The 8-byte fact above could never have caught this: every <c>ulong</c> reinterprets to a NON-NEGATIVE
    /// <c>Int128</c>, so it never reached the branch that was wrong. A 16-byte container value ≥ 2^127 does
    /// reinterpret negative, and <c>FormatBinaryImage</c> applied §14.9.25.4 GR6b — "when an unsigned numeric
    /// item is the receiving item, the ABSOLUTE VALUE of the sending value is used" — to that bit pattern,
    /// NEGATING it. An item holding 2^128−1 encoded as <c>00…01</c>: a silent wrong answer in the lane wave 1
    /// had just introduced. 2^127 alone round-tripped BY ACCIDENT (−Int128.MinValue is Int128.MinValue), which
    /// is why the +1 case is pinned beside it — a single sample here would have passed over the defect.
    /// <para>These are the bytes §13.18.60.4 GR4 asks for ("a radix of 2 is used", most significant byte first)
    /// over the full container range GR12 gives the item, stated directly rather than through the lane's own
    /// inverse.</para></summary>
    [Fact]
    public void UnsignedWideCarrierLane_AboveInt128MaxValue_KeepsItsBits()
    {
        var profile = new NumProfile
        {
            Digits = 31,
            FractionDigits = 0,
            Signed = false,
            Truncation = NumericTruncation.BinaryCapacity,
            ByteForm = NumericByteForm.Binary,
            StorageLength = 16,
        };
        string allOnes = Chars(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                               0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        string twoPow127 = Chars(0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        string twoPow127Plus1 = Chars(0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x01);

        Assert.Equal(allOnes, CobolNum.FormatImage(UInt128.MaxValue, profile));
        Assert.Equal(twoPow127, CobolNum.FormatImage(UInt128.One << 127, profile));
        Assert.Equal(twoPow127Plus1, CobolNum.FormatImage((UInt128.One << 127) + 1, profile));

        // …and the parse twin brings each one back unchanged (the round trip, now that both halves are right).
        Assert.Equal(UInt128.MaxValue, CobolNum.ParseImageU128(allOnes, profile));
        Assert.Equal(UInt128.One << 127, CobolNum.ParseImageU128(twoPow127, profile));
        Assert.Equal((UInt128.One << 127) + 1, CobolNum.ParseImageU128(twoPow127Plus1, profile));
        Assert.Equal(UInt128.MaxValue, CobolNum.StoreImage(allOnes, profile, (UInt128)0));
    }
}
