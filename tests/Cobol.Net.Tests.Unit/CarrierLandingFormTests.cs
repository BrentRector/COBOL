// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using System.Text.RegularExpressions;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB77 — every carrier's landing into a fixed-point receiver has TWO forms, chosen by the LANDING and never
/// by the value: the CHECKED landing (ON SIZE ERROR / EC-SIZE checking) SATURATES past the Int128 carrier so the
/// receiver's capacity check raises the size error (PB13); the UNCHECKED landing (a MOVE — ISO §14.6.8.2 r4
/// "truncation on either end"; the no-phrase arithmetic store — §14.6.13.1.3 item 8; INVOKE BY CONTENT) keeps the
/// LOW-ORDER digits, never a sentinel. The SDIDI carrier had both since PB74 (<c>CobolDec.ToUnscaledChecked</c> /
/// <c>ToUnscaled</c>); the native exact family (<c>CobolIntrinsics.Rescaled</c>) and the float family
/// (<c>CobolFloat.ToScaled</c>, <c>CobolIntrinsics.FromDouble</c>) had only the saturating one, so
/// <c>MOVE FUNCTION NUMVAL-F("5E+30") TO PIC V9(9)</c> stored 884105727 — the low digits of <c>Int128.MaxValue</c>.
/// </summary>
/// <remarks>Every expected value is derived by EXACT arithmetic on the sending value (Python <c>Decimal</c> over
/// the binary64's exact expansion), never observed: 1.0E+40 as a double is 10000000000000000303786028427003666890752;
/// -2.5E+40 is -25000000000000000155002161260194579873792.</remarks>
public sealed class CarrierLandingFormTests
{
    private static readonly Int128 Ten38 = Int128.Parse("100000000000000000000000000000000000000");

    // ── the exact-expansion kernel ────────────────────────────────────────────────────────────────────────────

    /// <summary>The 38 low-order digits of the exact binary64 expansion, sign kept, at scale 0.</summary>
    [Fact]
    public void LowOrderDigits_PastTheCarrier_AreTheExactExpansions()
    {
        Assert.Equal(Int128.Parse("303786028427003666890752"), CobolFloat.LowOrderDigits(1.0e40, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("-155002161260194579873792"), CobolFloat.LowOrderDigits(-2.5e40, 0, CobolRounding.Truncation));
    }

    /// <summary>A landing scale multiplies the exact expansion (the fraction digits of an integer are zeros); an
    /// exact half at a 31-digit scale — 20000000.5 × 10^31 = 2×10^38 + 5×10^30 — keeps its low 38 digits.</summary>
    [Fact]
    public void LowOrderDigits_AtScale_MultiplyTheExactExpansion()
    {
        Assert.Equal(Int128.Parse("303786028427003666890752") * 1_000_000_000, CobolFloat.LowOrderDigits(1.0e40, 9, CobolRounding.Truncation) % Ten38);
        Assert.Equal(Int128.Parse("5000000000000000000000000000000"), CobolFloat.LowOrderDigits(20000000.5, 31, CobolRounding.Truncation));
    }

    /// <summary>The kernel is correct for EVERY finite double (so a future exact-everywhere determination is one
    /// branch away): a fraction rounds per the COBOL mode through the ONE <c>RoundDiv</c> kernel, with the sign's
    /// directed modes honoured.</summary>
    [Theory]
    [InlineData(2.5, CobolRounding.Truncation, 2)]
    [InlineData(2.5, CobolRounding.NearestAwayFromZero, 3)]
    [InlineData(2.5, CobolRounding.NearestEven, 2)]
    [InlineData(3.5, CobolRounding.NearestEven, 4)]
    [InlineData(2.5, CobolRounding.NearestTowardZero, 2)]
    [InlineData(2.75, CobolRounding.NearestTowardZero, 3)]
    [InlineData(-2.5, CobolRounding.TowardLesser, -3)]
    [InlineData(-2.5, CobolRounding.TowardGreater, -2)]
    [InlineData(-2.5, CobolRounding.AwayFromZero, -3)]
    [InlineData(0.1, CobolRounding.Truncation, 0)]
    [InlineData(1.0e25, CobolRounding.Truncation, 10000000000000000905969664.0)]
    public void LowOrderDigits_InsideTheCarrier_RoundsExactlyPerMode(double v, CobolRounding mode, double expected) =>
        Assert.Equal((Int128)expected, CobolFloat.LowOrderDigits(v, 0, mode));

    // ── the float family's two landings ───────────────────────────────────────────────────────────────────────

    /// <summary>Inside the carrier the two landings are ONE function (the same product, the same rounding).</summary>
    [Theory]
    [InlineData(1.15, 2)]
    [InlineData(-8.2, 1)]
    [InlineData(1.0e25, 0)]
    [InlineData(123456.789, 9)]
    public void ToScaledUnchecked_InsideTheCarrier_IsToScaled(double v, int scale)
    {
        foreach (CobolRounding mode in Enum.GetValues<CobolRounding>())
            Assert.Equal(CobolFloat.ToScaled(v, scale, mode), CobolFloat.ToScaledUnchecked(v, scale, mode));
    }

    /// <summary>Past the carrier: the CHECKED landing saturates (the store's capacity check raises); the UNCHECKED one
    /// lands the exact low-order digits — and a non-finite value lands zero, never a sentinel to truncate.</summary>
    [Fact]
    public void ToScaled_PastTheCarrier_SaturatesChecked_LandsLowOrderDigitsUnchecked()
    {
        Assert.Equal(Int128.MaxValue, CobolFloat.ToScaled(1.0e40, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.MinValue, CobolFloat.ToScaled(-2.5e40, 5, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("303786028427003666890752"), CobolFloat.ToScaledUnchecked(1.0e40, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("-155002161260194579873792"), CobolFloat.ToScaledUnchecked(-2.5e40, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.MaxValue, CobolFloat.ToScaled(double.PositiveInfinity, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.Zero, CobolFloat.ToScaledUnchecked(double.PositiveInfinity, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.Zero, CobolFloat.ToScaledUnchecked(double.NaN, 3, CobolRounding.Truncation));
    }

    /// <summary>The working-scale quantizer (<c>FromDouble</c>, the arithmetic-receiver landing) follows the same
    /// two-form rule: checked saturates, unchecked lands the low-order digits at the working scale.</summary>
    [Fact]
    public void FromDouble_PastTheCarrier_FollowsTheLanding()
    {
        Assert.Equal(Int128.MaxValue, CobolIntrinsics.FromDouble(1.0e40, 9, CobolRounding.NearestAwayFromZero, checkedLanding: true));
        Assert.Equal(Int128.Parse("303786028427003666890752") * 1_000_000_000 % Ten38, CobolIntrinsics.FromDouble(1.0e40, 9, CobolRounding.NearestAwayFromZero) % Ten38);
        Assert.Equal(Int128.Zero, CobolIntrinsics.FromDouble(double.PositiveInfinity, 9, CobolRounding.NearestAwayFromZero));
        Assert.Equal(Int128.MaxValue, CobolIntrinsics.FromDouble(double.PositiveInfinity, 9, CobolRounding.NearestAwayFromZero, checkedLanding: true));
        Assert.Equal(CobolIntrinsics.FromDouble(2.5, 0, CobolRounding.Truncation), CobolIntrinsics.FromDouble(2.5, 0, CobolRounding.Truncation, checkedLanding: true));   // inside: one function
    }

    // ── the native exact family's two landings ───────────────────────────────────────────────────────────────

    /// <summary>NUMVAL-F("5E+30") at working scale 9 is 5×10^39 — past the carrier: checked saturates, unchecked
    /// keeps the low-order digits (all zero, so 0 — what a MOVE into PIC V9(9) / PIC 9(5) must store, §14.6.8.2 r4).
    /// <para>⛔ NUMVAL AND NUMVAL-C ARE NOT HERE, AND THAT IS THE POINT (kb/Work PB251). §15.67.4 r1 / §15.68.4 r1
    /// fix their returned value with no arithmetic-mode qualification, so they have no scaled-<c>Int128</c>
    /// projection to land at all — the SDIDI carries every §15.67.3 r3-conforming argument exactly, and a
    /// 31-digit significand that USED to keep only its low 29 digits × 10^9 here is now simply the value.
    /// NUMVAL-F keeps this landing because §15.69.4 r2 grants its native value an approximation.</para></summary>
    [Fact]
    public void NumvalFamily_PastTheCarrier_FollowsTheLanding()
    {
        Assert.Equal(Int128.MaxValue, CobolIntrinsics.NumvalF("5E+30", 9, checkedLanding: true));
        Assert.Equal(Int128.Zero, CobolIntrinsics.NumvalF("5E+30", 9));
        Assert.Equal(Int128.Parse("-1000000000000000"), CobolIntrinsics.NumvalF("-1E+6", 9));   // inside: unchanged
        // The same 31-digit argument through the ONE NUMVAL projection: exact, with no landing to choose.
        Assert.Equal(Int128.Parse("1234567890123456789012345678901"),
            CobolIntrinsics.NumvalDec("1234567890123456789012345678901").ToUnscaled(0, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("1234567890123456789012345678901"),
            CobolIntrinsics.NumvalCDec("$1234567890123456789012345678901", "$").ToUnscaled(0, CobolRounding.Truncation));
    }

    // ── the NATIVE Int128 carrier's two landings (kb/Work PB639) ─────────────────────────────────────────────

    private static NumProfile Pic(int digits, int scale, bool signed = true,
        NumericTruncation truncation = NumericTruncation.DigitCount, int storage = 0) => new()
        {
            Digits = digits, FractionDigits = scale, Signed = signed, SignKind = NumericSign.TrailingOverpunch,
            Truncation = truncation, ByteForm = NumericByteForm.Zoned,
            StorageLength = storage == 0 ? digits : storage,
        };

    /// <summary>The Int128 lane had a low-order landing (<c>RescaleStoreCap</c>) and a loud one
    /// (<c>RescaleEscape</c>) but its CHECKED landing was the plain wrapping <c>Rescale</c> with only a
    /// PROHIBITED test bolted on — so the carrier's own binary wrap reached both the store's capacity check and
    /// the edit's. This pins all three forms of the ONE alignment.</summary>
    [Fact]
    public void NativeCarrier_HasThreeLandings_AndOnlyOneOfThemWraps()
    {
        Int128 v = Int128.Parse("1000000000000000000000000000000");      // 10^30, 31 digits (ISO §8.3.3.3.2)
        // UNCHECKED: the low-order digits of 10^39 — §14.6.13.1.3 item 8 / DOC-A.1-70.
        Assert.Equal(Int128.Zero, CobolNum.RescaleStoreCap(v, 0, 9, CobolRounding.Truncation));
        // CHECKED: the §14.7.5 case-3 size error, so the receiver can be left unchanged.
        Assert.Throws<CobolSizeError>(() => CobolNum.RescaleChecked(v, 0, 9, CobolRounding.Truncation));
        // LOUD (a value-semantics alignment, §14.7.5 case 5).
        Assert.Throws<CobolSizeError>(() => CobolNum.RescaleEscape(v, 0, 9, CobolRounding.Truncation));
        // Inside the carrier all three are ONE function.
        foreach (CobolRounding mode in Enum.GetValues<CobolRounding>())
        {
            Assert.Equal(CobolNum.Rescale(123456, 2, 9, mode), CobolNum.RescaleStoreCap(123456, 2, 9, mode));
            Assert.Equal(CobolNum.Rescale(123456, 2, 9, mode), CobolNum.RescaleChecked(123456, 2, 9, mode));
        }
    }

    /// <summary>The store's unchecked landing IS the low-order digits at the receiver's scale, for the
    /// arithmetic store and the MOVE alike (they are the same call). Derived, not observed: 10^30 aligned at
    /// scale 9 is 10^39, whose low-order 18 digits are all zero.</summary>
    [Fact]
    public void Store_PastTheCarrier_KeepsTheLowOrderDigits()
    {
        Int128 v = Int128.Parse("1000000000000000000000000000000");
        Assert.Equal(Int128.Zero, CobolNum.Store(v, 0, Pic(18, 9)));
        // 123456789012345678 × 10^2 = 12345678901234567800; PIC S9(4)V99's low-order 6 digits are 567800.
        Assert.Equal((Int128)567800, CobolNum.Store(Int128.Parse("123456789012345678"), 0, Pic(6, 2)));
        // A BinaryCapacity receiver's low-order digits are the CONTAINER's residue: 10^39 mod 2^64.
        Assert.Equal((Int128)6873995514006732800,
            CobolNum.Store(v, 0, Pic(18, 9, truncation: NumericTruncation.BinaryCapacity, storage: 8)));
        // In-carrier stores are untouched by the split.
        Assert.Equal((Int128)123456000000, CobolNum.Store(123456, 3, Pic(18, 9)));
    }

    /// <summary>⛔ THE CHECKED ARM'S DISCRIMINATOR. ceil(2^128 / 10^9) aligned at scale 9 WRAPS to 231788544 —
    /// which fits <c>PIC S9(9)V9(9)</c> — so the capacity check passed and the store committed, where §14.7.5
    /// storing rule 2 requires the receiver unchanged. A capacity test may only ever see an alignment the
    /// carrier actually formed.</summary>
    [Fact]
    public void TryStore_AWideningThatWrapsBackIntoCapacity_IsStillASizeError()
    {
        Int128 v = Int128.Parse("340282366920938463463374607432");
        Assert.Equal((Int128)231788544, unchecked(v * 1_000_000_000));           // the wrap this guard must not see
        Assert.False(CobolNum.TryStore(v, 0, Pic(18, 9), CobolRounding.Truncation, out Int128 stored));
        Assert.Equal(Int128.Zero, stored);
        Assert.False(CobolNum.TryStore(v, 0, Pic(18, 9, truncation: NumericTruncation.BinaryCapacity, storage: 8),
            CobolRounding.Truncation, out _));
        Assert.False(CobolNum.TryStore(Int128.Parse("1000000000000000000000000000000"), 0, Pic(18, 9),
            CobolRounding.Truncation, out _));                                   // the control that always worked
        Assert.True(CobolNum.TryStore(123456, 3, Pic(18, 9), CobolRounding.Truncation, out stored));
        Assert.Equal((Int128)123456000000, stored);                              // an in-capacity store still stores
    }

    /// <summary>The ONE widening predicate the three siblings share, at its boundary and at Int128.MinValue
    /// (which <c>Int128.Abs</c> throws on — the R10 bits contract can deliver it, kb/Work PB288).</summary>
    [Fact]
    public void WideningFits_IsExactAtTheBoundary_AndSurvivesMinValue()
    {
        Assert.True(CobolNum.WideningFits(Int128.MaxValue / 10, 1));
        Assert.False(CobolNum.WideningFits(Int128.MaxValue / 10 + 1, 1));
        Assert.True(CobolNum.WideningFits(Int128.MinValue / 10, 1));
        Assert.False(CobolNum.WideningFits(Int128.MinValue / 10 - 1, 1));
        Assert.True(CobolNum.WideningFits(Int128.MinValue, 0));      // no widening: always representable
        Assert.False(CobolNum.WideningFits(Int128.MinValue, 1));
        Assert.True(CobolNum.WideningFits(0, 100));                  // zero never overflows
        Assert.False(CobolNum.WideningFits(1, 39));                  // past the decimal reach of the carrier
        // The three siblings answer the same question, so MinValue never reaches an Int128.Abs.
        Assert.Equal(Int128.Zero, CobolNum.RescaleStoreCap(Int128.MinValue, 0, 9, CobolRounding.Truncation) % 1_000_000_000);
        Assert.Throws<CobolSizeError>(() => CobolNum.RescaleEscape(Int128.MinValue, 0, 9, CobolRounding.Truncation));
        Assert.False(CobolNum.TryStore(Int128.MinValue, 0, Pic(18, 9), CobolRounding.Truncation, out _));
    }

    /// <summary>The EDITED receiver is the same store with an edit applied, so its two landings must agree with
    /// the numeric ones: <c>Format</c> keeps the low-order digits, <c>TryFormat</c> refuses a widening the
    /// carrier cannot form (before PB639 it capacity-checked the WRAP and reported "fits").</summary>
    [Fact]
    public void EditedReceiver_FollowsTheSameTwoLandings()
    {
        Int128 v = Int128.Parse("1000000000000000000000000000000");
        Assert.Equal("        0.000000000", CobolEdit.Format(v, 0, "ZZZZZZZZ9.999999999"));
        Assert.False(CobolEdit.TryFormat(Int128.Parse("340282366920938463463374607432"), 0,
            "ZZZZZZZZ9.999999999", out _));
        Assert.False(CobolEdit.TryFormat(v, 0, "ZZZZZZZZ9.999999999", out _));      // the control
        Assert.True(CobolEdit.TryFormat(123456, 3, "ZZZZZZZZ9.999999999", out string ok));
        Assert.Equal("      123.456000000", ok);
    }

    // ── the emitter names the landing at every site ─────────────────────────────────────────────────────────

    /// <summary>A RECEIVER-BOUND alignment has its own spelling with NO default form — <c>NumRescaleStore</c>,
    /// whose two arms are <c>RescaleStoreCap</c> and <c>RescaleChecked</c> — and <c>ArithmeticEmitter</c> renders
    /// nothing else. <c>RuntimeApi.NumRescale</c> (the plain wrapping <c>CobolNum.Rescale</c>) carried a
    /// <c>checkedPath = false</c> DEFAULT, the arithmetic store's edited landing took it, and a widening past the
    /// carrier wrapped in binary before any capacity rule ran (kb/Work PB639). This is the same guard shape
    /// <see cref="FloatToScaled_TakesTheLandingForm_WithNoDefault"/> puts on the float carrier.</summary>
    [Fact]
    public void EveryReceiverBoundAlignment_NamesItsLandingForm()
    {
        string api = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Roslyn", "RuntimeApi.cs"));
        Assert.Matches(new Regex(@"public static string NumRescaleStore\(string value, string fromScale, "
            + @"string toScale, CobolRounding mode, bool checkedPath\)"), api);
        Assert.Contains("nameof(CobolNum.RescaleStoreCap)", api);
        // …and the plain rescale may never regrow a landing-form default.
        Assert.Matches(new Regex(@"public static string NumRescale\(string value, string fromScale, "
            + @"string toScale, CobolRounding mode\)"), api);
        Assert.DoesNotContain("bool checkedPath = ", api);

        // The edited receiver's landing is ONE helper (kb/Work PB2163: the masked and LOCALE arms of StoreArith each
        // spelled the carrier switch): NumericRenderer.EditedLanding is the only receiver-bound rescale render, and
        // both edited arms, checked and unchecked, call it.
        string ae = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "ArithmeticEmitter.cs"));
        string nr = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "NumericRenderer.cs"));
        Assert.DoesNotContain("RuntimeApi.NumRescale(", ae);
        Assert.DoesNotContain("RuntimeApi.NumRescaleStore(", ae);
        Assert.Single(Regex.Matches(nr, @"RuntimeApi\.NumRescaleStore\("));
        Assert.Equal(4, Regex.Matches(ae, @"NumericRenderer\.EditedLanding\(").Count);
    }

    /// <summary>Every fixed-point numeric store in the compiler funnels through the ONE landing
    /// (<c>NumericRenderer.StoreArgs</c> → <c>CobolNum.Store</c>/<c>TryStore</c>/<c>StoreU</c>/<c>TryStoreU</c>),
    /// so fixing the landing fixes the arithmetic store, the MOVE and the INVOKE BY CONTENT expression at once
    /// (kb/Work PB84). A site that spelled its own <c>CobolNum.Store</c> would be a second copy of the
    /// determination — the shape PB639 had to repair in four places because the EDITED landing was a second copy.
    /// </summary>
    [Fact]
    public void NoEmitterSpellsItsOwnNumericStore()
    {
        foreach (string file in new[] { "ArithmeticEmitter.cs", "MoveEmitter.cs", "OoEmitter.cs", "StringEmitter.cs" })
        {
            string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", file));
            Assert.DoesNotContain("nameof(CobolNum.Store)", src);
            Assert.DoesNotContain("CobolNum.Rescale(", src);
        }
    }

    /// <summary>The float landing has NO default form: <c>RuntimeApi.FloatToScaled</c> takes the landing as a
    /// required argument, so a new site must say which store it is — the compiler enforces it. This pins the
    /// signature so a "convenience" default cannot creep back.</summary>
    [Fact]
    public void FloatToScaled_TakesTheLandingForm_WithNoDefault()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Roslyn", "RuntimeApi.cs"));
        var m = Regex.Match(src, @"public static string FloatToScaled\(string value, string scale, CobolRounding mode, bool checkedLanding\)");
        Assert.True(m.Success, "RuntimeApi.FloatToScaled must take `bool checkedLanding` with NO default (kb/Work PB77) — the "
            + "landing form is the caller's statement to make.");
        Assert.Contains("nameof(CobolFloat.ToScaledUnchecked)", src);
    }

    /// <summary>The quantizer / exact-family renders that reach the runtime with a working scale carry the ONE
    /// <c>CheckedFlag</c> — <c>FromDouble</c>, <c>FromDoubleBounded</c>, native <c>**</c>, NUMVAL-F. A site that
    /// renders <c>FromDouble(...)</c> or a NUMVAL-F value call without it lands a no-phrase store on the sentinel
    /// again.
    /// <para>⛔ NUMVAL AND NUMVAL-C ARE NO LONGER ON THIS LIST, AND THE SECOND HALF ASSERTS THEY DO NOT COME BACK
    /// (kb/Work PB251). §15.67.4 r1 / §15.68.4 r1 fix their returned value with no arithmetic-mode qualification,
    /// so they render on the SDIDI carrier in every mode and have no working scale to land at — which is a
    /// STRONGER statement than carrying the flag, because there is no landing left to get wrong. NUMVAL-F keeps
    /// both because §15.69.4 r2 grants its native value an approximation.</para></summary>
    [Fact]
    public void EveryWorkingScaleRender_CarriesTheCheckedFlag()
    {
        string ir = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        string nr = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "NumericRenderer.cs"));
        foreach (Match m in Regex.Matches(ir + nr, @"RuntimeApi\.Intrinsic\(""FromDouble(Bounded)?"",[^;]*;"))
            Assert.True(m.Value.Contains("{CheckedFlag}", StringComparison.Ordinal),
                $"a FromDouble render without the landing form (kb/Work PB77): {m.Value}");
        // NUMVAL-F: the value render (not the TEST- validator, which returns a position, scale 0) — the case arm
        // from its label to the next case label.
        foreach (string name in new[] { "NumvalF" })
        {
            int at = ir.IndexOf($"case \"{name}\":", StringComparison.Ordinal);
            Assert.True(at >= 0, $"the {name} value render is gone from IntrinsicRenderer — re-point this guard.");
            int next = ir.IndexOf("case \"", at + 8, StringComparison.Ordinal);
            string arm = ir[at..(next < 0 ? ir.Length : next)];
            Assert.True(arm.Contains("{CheckedFlag}", StringComparison.Ordinal),
                $"the {name} value render lacks the landing form (kb/Work PB77): {arm}");
        }
        // ⛔ AND NUMVAL / NUMVAL-C HAVE NO WORKING-SCALE RENDER AT ALL (kb/Work PB251). A `case "Numval"` here
        // would mean a compile-time scale had come back for a value §15.67.4 r1 fixes — the defect that printed
        // 0.123456 for FUNCTION NUMVAL("0.1234567"). Asserting its ABSENCE is what keeps the removal permanent;
        // asserting the flag on a resurrected arm would only make the wrong answer well-formed.
        foreach (string name in new[] { "Numval", "NumvalC" })
            Assert.DoesNotContain($"case \"{name}\":", ir, StringComparison.Ordinal);
    }

    // ── ONE binary64, ONE landing: the EXACT value (kb/Work PB623) ───────────────────────────────────────────

    /// <summary>⛔ THE LANDING IS THE EXACT VALUE OF THE BINARY64, never a binary64 product of it (kb/Work PB623).
    /// ISO §14.6.8.2 rule 1 — "If the sending operand is an intermediate data item or a data item described with a
    /// standard floating-point usage, the value is treated as if it had been converted to a fixed-point value" —
    /// and rule 4's "truncation on either end" of THAT value. Both entry points answer with it, which is what
    /// §15.4.1 ("the returned value is the same for all instances of a given function within a single execution of
    /// the runtime element") requires of the MOVE channel and the arithmetic channel: <c>MOVE FUNCTION TAN(x)</c>
    /// used to store 16331239353195368.96 where <c>COMPUTE</c> of the same call stored 16331239353195369.92,
    /// because each formed <c>v × 10^scale</c> in binary64 first and past 2^53 that product is itself rounded.
    /// <para>Every expectation is the EXACT expansion of the sending double, computed offline by exact decimal
    /// arithmetic over its significand, never observed:
    /// 1.633123935319537E+16 is exactly 16331239353195370 (= 8165619676597685 × 2);
    /// 0.1 is 0.1000000000000000055511151231257827021181583404541015625;
    /// 8.2 is 8.199999999999999289457264239899814128875732421875;
    /// 0.3 is 0.299999999999999988897769753748434595763683319091796875;
    /// 1.0E+9 at the NEGATIVE scale of a trailing-P receiver is 10000000 × 10^2.</para></summary>
    [Theory]
    [InlineData(1.633123935319537e16, 2, "1633123935319537000")]
    [InlineData(0.1, 19, "1000000000000000055")]
    [InlineData(0.1, 1, "1")]
    [InlineData(0.1, 9, "100000000")]
    [InlineData(-0.1, 9, "-100000000")]
    [InlineData(8.2, 1, "81")]
    [InlineData(0.3, 1, "2")]
    [InlineData(1.15, 2, "114")]
    [InlineData(1.0e25, 0, "10000000000000000905969664")]
    [InlineData(1.0e30, 8, "100000000000000001988462483865600000000")]
    [InlineData(1.0e9, -2, "10000000")]          // a trailing-P receiver: 10^scale is a DIVISOR, not 10^0
    [InlineData(1.5, 1, "15")]
    public void BothLandings_AreTheExactValue_AtEveryScale(double v, int scale, string expected)
    {
        Int128 want = Int128.Parse(expected);
        Assert.Equal(want, CobolFloat.ToScaled(v, scale, CobolRounding.Truncation));
        Assert.Equal(want, CobolFloat.ToScaledUnchecked(v, scale, CobolRounding.Truncation));
    }

    /// <summary>The arithmetic channel's quantizer is the SAME landing (kb/Work PB623): <c>CobolIntrinsics.FromDouble</c>
    /// is where <c>COMPUTE r = FUNCTION …</c> lands its working scale, and it held its own <c>d × 10^scale</c>
    /// product until this. Its own mode is NEAREST-AWAY-FROM-ZERO (the working-scale rounding), so that is what it
    /// has to agree with — the VALUE is shared, only the mode and the past-carrier form are its own.
    /// <para>⚠ WHAT THIS DOES AND DOES NOT PROVE, measured rather than assumed: run against the PRE-FIX runtime it
    /// PASSES, because both entry points then held the SAME wrong formula and so agreed at any one scale. What
    /// actually diverged in a program is that the two channels land at DIFFERENT scales — the MOVE at the
    /// receiver's, the COMPUTE at a working scale it then rescales from — so the shared product's rounding error
    /// differed. That end-to-end invariant is pinned by the goldens (<c>pb623_float_landing_exact</c> legs A1/A2 in
    /// 2002/2014/2023), and this is the STRUCTURAL pin that keeps a second formula from coming back.</para></summary>
    [Theory]
    [InlineData(1.633123935319537e16, 2)]
    [InlineData(0.1, 19)]
    [InlineData(8.2, 9)]
    [InlineData(-0.3, 12)]
    [InlineData(1.0e25, 3)]
    [InlineData(1.0e9, -2)]
    public void FromDouble_IsTheSameLandingAsTheMove(double v, int scale)
    {
        // ⛔ AND THE MODE IS NOW PART OF WHAT "THE SAME LANDING" MEANS (kb/Work PB647). Before it, this assertion
        // could only be written at ONE mode, because the arithmetic quantizer hard-coded NEAREST-AWAY-FROM-ZERO
        // while the MOVE landing took the receiver's — so the two channels agreed on the VALUE and disagreed on
        // the ANSWER (`MOVE FUNCTION SQRT(3) TO S` 1.732050807 against `COMPUTE S = FUNCTION SQRT(3)`
        // 1.732050808 in one PIC 9V9(9)). Sweeping every mode is the drift test: a mode re-introduced inside
        // either landing fails here rather than in a golden six months later.
        foreach (CobolRounding mode in System.Enum.GetValues<CobolRounding>())
            Assert.Equal(CobolFloat.ToScaledUnchecked(v, scale, mode),
                         CobolIntrinsics.FromDouble(v, scale, mode));
    }

    /// <summary>THE DRIFT TEST for the carrier fast path (kb/Work PB623). <see cref="ExactOracle"/> below is a
    /// deliberately separate, unoptimized <see cref="System.Numerics.BigInteger"/> implementation of "this
    /// double's exact value at this scale", so the sweep is a DIFFERENTIAL against the definition and not the
    /// landing agreeing with itself: the landing answers from an <see cref="Int128"/> multiply-and-shift whenever
    /// man·5^scale fits the carrier, and this is what proves that bound. The magnitudes straddle 2^53 (where the
    /// old binary64 product started rounding), the 10^22 largest exactly-representable power of ten, and the
    /// Int128 carrier itself — where the two LANDING FORMS diverge and are checked apart (kb/Work PB77: checked
    /// saturates, unchecked keeps the low-order digits).</summary>
    [Fact]
    public void EveryLanding_MatchesAnIndependentExactOracle_AcrossTheCarrierBoundaries()
    {
        double[] values =
        [
            0.1, 0.3, 1.5, 2.5, 8.2, 1.15, 123456.789, 9007199254740991.0, 9007199254740992.0,
            1.633123935319537e16, 1.0e22, 1.0e23, 1.0e25, 1.0e30, 1.0e37, 5.0e37, 1.7e38, 1.0e40,
            double.Epsilon, 1.0e-300, 0.0, -0.0,
        ];
        int checkedRows = 0, uncheckedRows = 0;
        foreach (double mag in values)
        {
            foreach (double v in new[] { mag, -mag })
            {
                for (int scale = -3; scale <= 34; scale++)
                {
                    foreach (var mode in new[] { CobolRounding.Truncation, CobolRounding.NearestAwayFromZero })
                    {
                        BigInteger exact = ExactOracle(v, scale, mode);
                        string why = $"v={v:R} scale={scale} mode={mode}";
                        if (exact >= (BigInteger)Int128.MinValue && exact <= (BigInteger)Int128.MaxValue)
                        {
                            Assert.True((Int128)exact == CobolFloat.ToScaled(v, scale, mode), "checked landing: " + why);
                            Assert.True((Int128)exact == CobolFloat.ToScaledUnchecked(v, scale, mode), "unchecked landing: " + why);
                            checkedRows++;
                        }
                        else
                        {
                            // Past the carrier the LANDING FORM decides, and the two forms are different answers.
                            Assert.True((exact > 0 ? Int128.MaxValue : Int128.MinValue) == CobolFloat.ToScaled(v, scale, mode),
                                "past-carrier saturation: " + why);
                            Assert.True((Int128)(exact % BigInteger.Pow(10, 38)) == CobolFloat.ToScaledUnchecked(v, scale, mode),
                                "past-carrier low-order digits: " + why);
                            uncheckedRows++;
                        }
                    }
                }
            }
        }
        // A run asserts its own population: both regimes have to be exercised, or the sweep proved half a rule.
        Assert.True(checkedRows > 1000, $"in-carrier rows: {checkedRows}");
        Assert.True(uncheckedRows > 100, $"past-carrier rows: {uncheckedRows}");
    }

    /// <summary>The exact value of a finite binary64 at a decimal scale, rounded per mode — written here a SECOND
    /// time, plainly and always over <see cref="System.Numerics.BigInteger"/>, so the sweep above tests the
    /// runtime's carrier fast path against the definition rather than against itself. A double is ±man·2^exp
    /// exactly, so v·10^scale = ±man·5^scale·2^(exp+scale).</summary>
    private static BigInteger ExactOracle(double v, int scale, CobolRounding mode)
    {
        long bits = BitConverter.DoubleToInt64Bits(v);
        int biased = (int)((bits >> 52) & 0x7FF);
        BigInteger man = bits & 0xF_FFFF_FFFF_FFFFL;
        if (biased == 0) biased = 1; else man += BigInteger.One << 52;
        int exp = biased - 1075;
        BigInteger num = man, den = BigInteger.One;
        if (scale >= 0) num *= BigInteger.Pow(5, scale); else den = BigInteger.Pow(5, -scale);
        if (exp + scale >= 0) num <<= exp + scale; else den <<= -(exp + scale);
        BigInteger q = BigInteger.DivRem(num, den, out BigInteger rem);
        if (!rem.IsZero && mode == CobolRounding.NearestAwayFromZero && rem * 2 >= den) q += 1;
        return bits < 0 ? -q : q;                                   // magnitude-then-sign: truncation is toward zero
    }

    /// <summary>The ROUNDED MODE PROHIBITED gate asks the SAME exact value the landing rounds (kb/Work PB623;
    /// ISO §14.7.4.3 item 7 — "If the PROHIBITED phrase is specified, and the arithmetic value cannot be
    /// represented exactly in the resultant identifier, the EC-SIZE-TRUNCATION exception condition is set to
    /// exist … and the content of the resultant identifier is unchanged"). The old product test could not:
    /// <c>0.1 * 10.0</c> is exactly 1.0 in binary64, so it called a value with 55 fraction digits representable in
    /// ONE — a gate saying "exact" over a landing that then truncates 54 digits away.
    /// <para>The exact expansions decide every row: 0.1 has 55 fraction digits, 8.2 has 48, 0.3 has 54, 1.5 has
    /// one, and 1.0E+25 and 2.0 are integers.</para></summary>
    [Theory]
    [InlineData(0.1, 1, true)]
    [InlineData(0.1, 54, true)]
    [InlineData(0.1, 55, false)]
    [InlineData(8.2, 1, true)]
    [InlineData(8.2, 47, true)]
    [InlineData(8.2, 48, false)]
    [InlineData(0.3, 54, false)]
    [InlineData(1.5, 1, false)]
    [InlineData(1.5, 0, true)]
    [InlineData(2.0, 0, false)]
    [InlineData(1.0e25, 0, false)]
    [InlineData(0.0, 0, false)]
    public void InexactAtScale_AsksTheExactValue_NotABinary64Product(double v, int scale, bool inexact) =>
        Assert.Equal(inexact, CobolFloat.InexactAtScale(v, scale));

    // ── the edited landing (kb/Work PB2163) ──────────────────────────────────────────────────────────────────

    /// <summary>The numeric-EDITED receiver's landing (<c>NumericRenderer.EditedLanding</c>, the ONE carrier switch
    /// the masked and LOCALE arithmetic arms and the MOVE share) is TOTAL over <see cref="NumXCarrier"/> and always
    /// hands the edit an <c>Int128</c>: the unsigned-wide carrier used to fall to the native arm and pass its
    /// <c>UInt128</c> through unconverted — at its own scale verbatim — which was CS1503 on accepted source. A fifth
    /// carrier fails this test (the switch throws on a carrier it does not map) before a user meets it.</summary>
    [Fact]
    public void EditedLanding_IsTotalOverTheNumXCarriers_AndNamesItsLandingForm()
    {
        foreach (var carrier in Enum.GetValues<NumXCarrier>())
        {
            var x = carrier switch
            {
                NumXCarrier.Scaled => new NumX("e", 2),
                NumXCarrier.UnsignedWide => new NumX("u", 2, U: true),
                NumXCarrier.Sdidi => new NumX("d", 0, Dec: true),
                NumXCarrier.Binary64 => new NumX("r", 0, Real: true),
                _ => throw new InvalidOperationException($"the test builds no operand for the new carrier {carrier}"),
            };
            foreach (bool checkedLanding in new[] { true, false })
            {
                string landed = NumericRenderer.EditedLanding(x, 2, CobolRounding.NearestEven, checkedLanding);
                string expected = (carrier, checkedLanding) switch
                {
                    (NumXCarrier.Scaled, _) => "e",   // already at the receiver's scale: no rescale
                    (NumXCarrier.UnsignedWide, true) => "CobolNum.RescaleCheckedU(u, 2, 2, CobolRounding.NearestEven)",
                    (NumXCarrier.UnsignedWide, false) => "CobolNum.RescaleStoreCapU(u, 2, 2, CobolRounding.NearestEven)",
                    (NumXCarrier.Sdidi, true) => "(d).ToUnscaledChecked(2, CobolRounding.NearestEven)",
                    (NumXCarrier.Sdidi, false) => "(d).ToUnscaled(2, CobolRounding.NearestEven)",
                    (_, true) => "CobolFloat.ToScaled(r, 2, CobolRounding.NearestEven)",
                    (_, false) => "CobolFloat.ToScaledUnchecked(r, 2, CobolRounding.NearestEven)",
                };
                Assert.Equal(expected, landed);
            }
        }
    }

    /// <summary>The two unsigned-wide edited kernels (kb/Work PB2163), against values derived by hand. The
    /// container maximum 2^128 − 1 = 340282366920938463463374607431768211455 (39 digits) widened to scale 2 keeps
    /// its low-order 38 digits at that scale, the no-phrase disposition (DOC-A.1-70) and a MOVE's truncation; the
    /// checked kernel refuses it (§14.7.5 case 3: past every edited receiver's capacity) and refuses a
    /// PROHIBITED-inexact narrowing (§14.7.4.3 r7); a narrowing rounds the whole value by the mode.</summary>
    [Fact]
    public void UnsignedWideEditedKernels_KeepTheLowOrderDigits_OrRaise()
    {
        Assert.Equal(Int128.Parse("28236692093846346337460743176821145500"),
            CobolNum.RescaleStoreCapU(UInt128.MaxValue, 0, 2, CobolRounding.Truncation));
        Assert.Equal((Int128)124, CobolNum.RescaleStoreCapU(12355, 2, 0, CobolRounding.NearestAwayFromZero));
        Assert.Equal((Int128)123, CobolNum.RescaleStoreCapU(12355, 2, 0, CobolRounding.Truncation));
        Assert.Equal((Int128)1234500, CobolNum.RescaleCheckedU(12345, 0, 2, CobolRounding.Truncation));
        Assert.Equal((Int128)124, CobolNum.RescaleCheckedU(12355, 2, 0, CobolRounding.NearestAwayFromZero));
        Assert.Equal("EC-SIZE-TRUNCATION", Assert.Throws<CobolSizeError>(
            () => CobolNum.RescaleCheckedU(UInt128.MaxValue, 0, 0, CobolRounding.Truncation)).EcName);
        Assert.Equal("EC-SIZE-TRUNCATION", Assert.Throws<CobolSizeError>(
            () => CobolNum.RescaleCheckedU(UInt128.MaxValue / 10, 0, 2, CobolRounding.Truncation)).EcName);
        Assert.Equal("EC-SIZE-TRUNCATION", Assert.Throws<CobolSizeError>(
            () => CobolNum.RescaleCheckedU(12355, 2, 0, CobolRounding.Prohibited)).EcName);
        Assert.Equal("+3.4028E+38", CobolEdit.FormatFloatStoreU(UInt128.MaxValue, 0, "+9.9999E+99", CobolRounding.Truncation));
        Assert.True(CobolEdit.TryFormatFloatU(12345, 0, "+9.9999E+99", out string img, CobolRounding.Truncation));
        Assert.Equal("+1.2345E+04", img);
        // Past Int128 the floating-point lane folds one digit round-to-odd, and that fold may neither invent nor hide
        // an inexact result: 3 × 10^38 is exact at five significant digits, while 2^128 − 1 and 3 × 10^38 + 5 (whose
        // only nonzero discarded digit is the one the fold drops) are not (§14.7.4.3 r7); AWAY-FROM-ZERO rounds up.
        UInt128 threeE38 = UInt128.Parse("300000000000000000000000000000000000000");
        Assert.True(CobolEdit.TryFormatFloatU(threeE38, 0, "+9.9999E+99", out string exact, CobolRounding.Prohibited));
        Assert.Equal("+3.0000E+38", exact);
        Assert.False(CobolEdit.TryFormatFloatU(UInt128.MaxValue, 0, "+9.9999E+99", out _, CobolRounding.Prohibited));
        Assert.False(CobolEdit.TryFormatFloatU(threeE38 + 5, 0, "+9.9999E+99", out _, CobolRounding.Prohibited));
        Assert.Equal("+3.4029E+38", CobolEdit.FormatFloatStoreU(UInt128.MaxValue, 0, "+9.9999E+99", CobolRounding.AwayFromZero));
    }
}
