// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>ISO §14.9.39.4 GR33/GR34/GR35 — the canonical ISO/IEC 60559:2020 Clause 3 values SET Format 15
/// stores, and the Annex A.1 item 176 NaN-payload determination that rides GR34/GR35 (kb/Work PB452).
/// <para>⛔ THESE ARE A PUBLISHED CONFORMANCE CLAIM, not an incidental encoding: A.1 item 176 says the payload
/// "shall be documented in the implementor's user documentation", docs/CONFORMANCE.md §7 DOC-A.1-176 documents
/// it, and this file is what makes the documented value and the emitted value the same thing. The suite DECODES
/// the constants (is it a NaN? which side of §6.2.1's quiet bit? whose sign?) rather than string-matching them,
/// so a re-spelling that changed the VALUE would fail while a re-spelling that did not would pass.</para>
/// <para>The COBOL-observable witnesses are `conformance:cases/14.9.39-SET/pb452_set_content_float` (infinite, which sign, a
/// NaN unequal to itself) and `conformance:cases/8.8.4.4-Simple-class-condition/pb225_float_class_conditions`, whose §8.8.4.4.4 GR3 j)/k)
/// FLOAT-NOT-A-NUMBER-QUIET / -SIGNALING class conditions (kb/Work PB225) observe the quiet/signaling
/// distinction; <c>CobolFloatClassTests</c> pins that every value this table writes classifies as what it was
/// written as.</para>
/// </summary>
public sealed class IeeeSpecialsTests
{
    [Theory]
    [InlineData(33, false, false)]
    [InlineData(33, false, true)]
    [InlineData(33, true, false)]
    [InlineData(33, true, true)]
    [InlineData(34, false, false)]
    [InlineData(34, false, true)]
    [InlineData(34, true, false)]
    [InlineData(34, true, true)]
    [InlineData(35, false, false)]
    [InlineData(35, false, true)]
    [InlineData(35, true, false)]
    [InlineData(35, true, true)]
    public void EveryCanonicalValue_DecodesToWhatItsGeneralRuleRequires(int generalRule, bool single, bool negative)
    {
        var which = Of(generalRule);
        ulong bits = IeeeSpecials.Bits(which, single, negative);

        // GR33/GR34/GR35's own last sentence: "If the SIGN phrase is specified, the sign of the content is set
        // according to the SIGN specification, otherwise the sign is positive."
        ulong signMask = single ? 0x8000_0000UL : 0x8000_0000_0000_0000UL;
        Assert.Equal(negative, (bits & signMask) != 0);

        // ISO/IEC 60559:2020 Clause 3: an infinity and a NaN share the all-ones biased exponent; a ZERO
        // significand is the infinity, a nonzero one a NaN.
        ulong expMask = single ? 0x7F80_0000UL : 0x7FF0_0000_0000_0000UL;
        ulong sigMask = single ? 0x007F_FFFFUL : 0x000F_FFFF_FFFF_FFFFUL;
        Assert.Equal(expMask, bits & expMask);
        Assert.Equal(which is IeeeSpecial.Infinity, (bits & sigMask) == 0);

        // §6.2.1: the LEADING significand bit is the quiet bit — set = quiet, clear = signaling; a signaling
        // NaN needs some other significand bit nonzero, which is what the documented payload of 1 supplies.
        ulong quietBit = single ? 0x0040_0000UL : 0x0008_0000_0000_0000UL;
        if (which is IeeeSpecial.QuietNaN) Assert.Equal(quietBit, bits & quietBit);
        if (which is IeeeSpecial.SignalingNaN)
        {
            Assert.Equal(0UL, bits & quietBit);
            Assert.NotEqual(0UL, bits & sigMask & ~quietBit);
        }
    }

    [Theory]
    [InlineData(33, false, false)]
    [InlineData(33, false, true)]
    [InlineData(33, true, false)]
    [InlineData(33, true, true)]
    [InlineData(34, false, false)]
    [InlineData(34, false, true)]
    [InlineData(34, true, false)]
    [InlineData(34, true, true)]
    [InlineData(35, false, false)]
    [InlineData(35, false, true)]
    [InlineData(35, true, false)]
    [InlineData(35, true, true)]
    public void TheStoredImage_CarriesTheDocumentedBits(int generalRule, bool single, bool negative)
    {
        var which = Of(generalRule);
        // ⛔ THE STORED IMAGE, NOT THE EXPRESSION (kb/Work PB961). This test used to evaluate the emitted
        // expression by hand and passed while every stored FLOAT-BINARY-32 signaling NaN was QUIET: the
        // expression was right, and the pipeline behind it — a JIT constant carried as a double, and a binary32
        // image lane that widened through double — quieted it. So it now follows the value the whole way: the
        // text the emitter writes (RuntimeApi.FloatFromBits over IeeeSpecials.Bits) names the runtime call and
        // the literal, the literal is fed to THAT call, and the result is stored through the carrier's own image
        // lane, in both byte orders, then read back — as bits, and as the carrier value the typed store takes.
        ulong expected = IeeeSpecials.Bits(which, single, negative);
        string text = RuntimeApi.FloatFromBits(expected, single);
        Assert.StartsWith(single ? "CobolFloat.FromBinary32Bits(0x" : "CobolFloat.FromBinary64Bits(0x", text);
        ulong literal = Convert.ToUInt64(new string([.. text[(text.IndexOf("0x", StringComparison.Ordinal) + 2)..].TakeWhile(Uri.IsHexDigit)]), 16);
        Assert.Equal(expected, literal);
        foreach (bool little in new[] { false, true })
        {
            var profile = new NumProfile
            {
                Digits = 0,
                FractionDigits = 0,
                Signed = true,
                Truncation = NumericTruncation.DigitCount,
                ByteForm = single ? NumericByteForm.Ieee32 : NumericByteForm.Ieee64,
                StorageLength = single ? 4 : 8,
                FloatLittleEndian = little,
            };
            string image = single
                ? CobolNum.FormatImageSingle(CobolFloat.FromBinary32Bits((uint)literal), profile)
                : CobolNum.FormatImageFloat(CobolFloat.FromBinary64Bits(literal), profile);
            Assert.Equal(expected, CobolNum.ImageFloatBits(image, profile));
            ulong back = single
                ? BitConverter.SingleToUInt32Bits(CobolNum.StoreImage(image, profile, 0f))
                : BitConverter.DoubleToUInt64Bits(CobolNum.StoreImage(image, profile, 0d));
            Assert.Equal(expected, back);
        }
    }

    /// <summary>The §14.9.39.4 general rule a theory row names — GR33 is the infinity, GR34 the quiet NaN,
    /// GR35 the signaling one. The rows are keyed by RULE NUMBER so the internal enum stays out of a public test
    /// signature and each row reads as the obligation it pins.</summary>
    private static IeeeSpecial Of(int generalRule) => generalRule switch
    {
        33 => IeeeSpecial.Infinity,
        34 => IeeeSpecial.QuietNaN,
        35 => IeeeSpecial.SignalingNaN,
        _ => throw new ArgumentOutOfRangeException(nameof(generalRule), generalRule, "not a §14.9.39.4 Format-15 float rule"),
    };
}
