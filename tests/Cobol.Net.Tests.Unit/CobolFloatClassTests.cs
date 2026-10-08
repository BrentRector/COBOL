// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>ISO §8.8.4.4.4 GR3 g)–m) and n) 1. b. — the floating-point class conditions, decided by
/// <see cref="CobolFloatClass"/> on the carrier's raw ISO/IEC 60559:2020 bits (kb/Work PB225).
/// <para>⛔ TWO AGREEMENTS ARE PINNED HERE, because each is one quantity with two surfaces:</para>
/// <list type="bullet">
/// <item>every canonical value SET Format 15 WRITES (<see cref="IeeeSpecials.Bits"/>, §14.9.39.4 GR33–GR35)
/// is classified as the kind it was written as — so <c>SET CONTENT OF X TO FLOAT-NOT-A-NUMBER-SIGNALING</c>
/// followed by <c>IF X IS FLOAT-NOT-A-NUMBER-SIGNALING</c> is TRUE by construction, on both carriers;</item>
/// <item>the float extremes <see cref="AlgebraicRanges"/> states (the HIGHEST-/SMALLEST-ALGEBRAIC and SET
/// FARTHEST-FROM-ZERO / NEAREST-TO-ZERO values) are exactly the bit patterns GR3 g)/m) test for.</item>
/// </list></summary>
public sealed class CobolFloatClassTests
{
    [Theory]
    [InlineData(IeeeSpecial.Infinity, FloatClassTest.Infinity)]
    [InlineData(IeeeSpecial.QuietNaN, FloatClassTest.QuietNaN)]
    [InlineData(IeeeSpecial.SignalingNaN, FloatClassTest.SignalingNaN)]
    public void EverySetFormat15Value_ClassifiesAsWhatItWasWrittenAs(IeeeSpecial which, FloatClassTest expected)
    {
        FloatClassTest[] specials =
            [FloatClassTest.Infinity, FloatClassTest.NotANumber, FloatClassTest.QuietNaN, FloatClassTest.SignalingNaN];
        foreach (bool single in new[] { true, false })
            foreach (bool negative in new[] { true, false })
            {
                ulong bits = IeeeSpecials.Bits(which, single, negative);
                foreach (var t in specials)
                {
                    // GR3 i): FLOAT-NOT-A-NUMBER is true for EITHER NaN.
                    bool want = t == expected || (t is FloatClassTest.NotANumber && which is not IeeeSpecial.Infinity);
                    Assert.True(want == CobolFloatClass.IsBits(bits, single, t),
                        $"{which} (single={single}, negative={negative}, bits=0x{bits:X}) answered "
                        + $"{!want} for {t}");
                }
                // n) 1. b.: none of the three is a finite numeric value.
                Assert.False(CobolFloatClass.IsBits(bits, single, FloatClassTest.Finite));
            }
    }

    [Theory]
    [InlineData(Usage.FloatBinary32)]
    [InlineData(Usage.FloatBinary64)]
    public void AlgebraicRangeExtremes_AreTheBitPatternsGr3gAndMTest(Usage usage)
    {
        var pic = new PicInfo(PicCategory.Numeric, usage, Length: 0, Digits: 0, Scale: 0, Signed: true);
        var range = AlgebraicRanges.Of(pic)!.Value;
        bool single = usage is Usage.FloatBinary32;
        foreach (string text in new[] { range.Farthest, range.FarthestNegative! })
            Assert.True(Classify(text, single, FloatClassTest.FarthestFromZero), $"{usage} farthest {text}");
        foreach (string text in new[] { range.Nearest!, "-" + range.Nearest })
            Assert.True(Classify(text, single, FloatClassTest.NearestToZero), $"{usage} nearest {text}");
        // The neighbours are NOT extremes (so the test is a real equality, not a range).
        Assert.False(CobolFloatClass.Is(single ? float.MaxValue / 2 : double.MaxValue / 2, FloatClassTest.FarthestFromZero));
        Assert.False(CobolFloatClass.Is(single ? float.Epsilon * 2 : double.Epsilon * 2, FloatClassTest.NearestToZero));
    }

    [Fact]
    public void ZeroAndOrdinaryValues_AreFiniteAndNothingElse()
    {
        foreach (double v in new[] { 0.0, -0.0, 1.5, -1e300 })
        {
            Assert.True(CobolFloatClass.Is(v, FloatClassTest.Finite));
            foreach (var t in new[] { FloatClassTest.Infinity, FloatClassTest.NotANumber, FloatClassTest.FarthestFromZero,
                                      FloatClassTest.NearestToZero })
                Assert.False(CobolFloatClass.Is(v, t), $"{v} answered true for {t}");
        }
    }

    /// <summary>A binary32 signaling NaN built from a NON-CONSTANT bit pattern survives to the class test — the
    /// float overload never widens.</summary>
    [Fact]
    public void Binary32SignalingNaN_FromRuntimeBits_IsSignaling()
    {
        uint bits = uint.Parse("2139095041");       // 0x7F800001, deliberately not a JIT-visible constant
        float f = BitConverter.UInt32BitsToSingle(bits);
        Assert.True(CobolFloatClass.Is(f, FloatClassTest.SignalingNaN));
        Assert.False(CobolFloatClass.Is(f, FloatClassTest.QuietNaN));
    }

    private static bool Classify(string text, bool single, FloatClassTest t) => single
        ? CobolFloatClass.Is(float.Parse(text, System.Globalization.CultureInfo.InvariantCulture), t)
        : CobolFloatClass.Is(double.Parse(text, System.Globalization.CultureInfo.InvariantCulture), t);
}
