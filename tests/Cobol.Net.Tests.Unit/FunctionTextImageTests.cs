// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The text image of an intrinsic FUNCTION's returned value (DA2). ISO §15.4 places the value in a "temporary
/// elementary data item" whose characteristics are IMPLEMENTOR-DEFINED under native arithmetic (§15.4.1), and
/// §14.9.11.4 GR1 independently makes the DISPLAY conversion implementor-defined — so this table is not derived
/// from the standard, it IS the determination, and `docs/CONFORMANCE.md` documents it as an Annex A.1 item.
/// <para>
/// ⛔ WHY THE RULE IS "LITERAL FORM" AND NOT A PADDED FIELD: an intrinsic over constant arguments is FOLDED to a
/// numeric literal at compile time, and a numeric literal renders as its own text. Any padded rendering would
/// therefore make <c>DISPLAY FUNCTION LENGTH(X)</c> (folded) and <c>DISPLAY FUNCTION ORD(C)</c> (computed) print
/// in different formats — the optimization would be USER-VISIBLE. These facts pin the one rule; the golden
/// `da2_function_as_text` pins that both compiler paths actually reach it.
/// </para>
/// </summary>
public sealed class FunctionTextImageTests
{
    /// <summary>Integers render as significant digits with no padding, and a negative carries a leading minus —
    /// never a zoned over-punch, because a function result is a temporary, not a stored DISPLAY item.</summary>
    [Theory]
    [InlineData(66, 0, "66")]          // FUNCTION ORD("A") — §15.70.1, lowest ordinal is 1
    [InlineData(5, 0, "5")]
    [InlineData(0, 0, "0")]
    [InlineData(-14, 0, "-14")]        // FUNCTION MIN(3 -14 0 8 -3)
    [InlineData(-1, 0, "-1")]
    public void Integers_RenderAsSignificantDigits(long unscaled, int scale, string expected) =>
        Assert.Equal(expected, CobolNum.FormatFunctionText(unscaled, scale));

    /// <summary>A scaled value shows exactly `scale` fraction digits, including trailing zeros — the scale is a
    /// property of the returned value, so dropping them would understate its precision.</summary>
    [Theory]
    [InlineData(4250, 2, "42.50")]
    [InlineData(-4250, 2, "-42.50")]
    [InlineData(1, 2, "0.01")]         // ⛔ the leading zero: 0.01, never .01
    [InlineData(-1, 2, "-0.01")]
    [InlineData(0, 2, "0.00")]
    [InlineData(100, 2, "1.00")]
    public void ScaledValues_ShowExactlyTheScale(long unscaled, int scale, string expected) =>
        Assert.Equal(expected, CobolNum.FormatFunctionText(unscaled, scale));

    /// <summary>A NEGATIVE scale is P-scaling: the P positions are trailing zeros that occupy no storage
    /// (§13.18.40.3 symbol-P operations item b), so the text restores them.</summary>
    [Theory]
    [InlineData(12, -2, "1200")]
    [InlineData(-12, -2, "-1200")]
    public void NegativeScale_RestoresThePZeros(long unscaled, int scale, string expected) =>
        Assert.Equal(expected, CobolNum.FormatFunctionText(unscaled, scale));

    /// <summary>An SDIDI intermediate (STANDARD-DECIMAL arithmetic, §8.8.1.5) routes through the SAME rule, so a
    /// function result does not change appearance with the arithmetic mode in effect.</summary>
    [Theory]
    [InlineData(4250, -2, "42.50")]
    [InlineData(66, 0, "66")]
    [InlineData(-14, 0, "-14")]
    public void StandardDecimal_RendersThroughTheSameRule(long sig, int exp, string expected) =>
        Assert.Equal(expected, new CobolDec(sig, exp).ToFunctionText());

    /// <summary>⛔ §14.9.25.4 GR6a — "If the sending operand is described as being signed numeric, the operational
    /// sign is not moved." A GENERAL rule with NO implementor latitude: the §15.4.1 / §14.9.11.4 GR1 latitude this
    /// determination rests on covers the FORM of the text (padding, radix), never whether the sign travels. So
    /// every de-signing context — MOVE to an alphanumeric/national/edited receiver, a text relation (§8.8.4.2.5
    /// routes it through the MOVE rules), INSPECT — gets the MAGNITUDE.
    /// <para>This shipped WRONG for one build: the fix intercepted the operand at AsString's entry, ahead of the
    /// visitor that carries <c>deSign</c>, and simply dropped the flag — so a signed ITEM de-signed while a
    /// FUNCTION returning the same value kept its minus, in identical statements.</para></summary>
    [Theory]
    [InlineData(-14, 0, "14")]
    [InlineData(14, 0, "14")]
    [InlineData(-4250, 2, "42.50")]
    [InlineData(0, 0, "0")]
    public void DeSign_DropsTheOperationalSign(long unscaled, int scale, string expected)
    {
        Assert.Equal(expected, CobolNum.FormatFunctionText(unscaled, scale, deSign: true));
        Assert.Equal(expected, new CobolDec(unscaled, -scale).ToFunctionText(deSign: true));
    }

    /// <summary>⛔ kb/Work PB2507 — the image is the LITERAL FORM, so under DECIMAL-POINT IS COMMA its decimal separator is
    /// the comma (§12.3.7.4 GR14 a: "the character written in numeric literals to represent the decimal separator shall
    /// be the comma"), on BOTH value lanes. The default stays the C#-facing period a compile-time fold's canonical text
    /// needs; a value with no fraction digits has no separator to spell.</summary>
    [Theory]
    [InlineData(15, 1, "1,5")]
    [InlineData(225, 2, "2,25")]
    [InlineData(-150, 2, "-1,50")]
    [InlineData(1, 2, "0,01")]
    [InlineData(0, 2, "0,00")]
    [InlineData(7, 0, "7")]
    [InlineData(12, -2, "1200")]
    public void CommaMode_SpellsTheDecimalSeparatorAsTheComma(long unscaled, int scale, string expected)
    {
        Assert.Equal(expected, CobolNum.FormatFunctionText(unscaled, scale, commaMode: true));
        Assert.Equal(expected, new CobolDec(unscaled, -scale).ToFunctionText(commaMode: true));
    }

    [Fact]
    public void CommaMode_ComposesWithDeSign_AndTheDefaultIsThePeriod()
    {
        Assert.Equal("1,5", CobolNum.FormatFunctionText(-15, 1, deSign: true, commaMode: true));
        Assert.Equal("1,5", new CobolDec(-15, -1).ToFunctionText(deSign: true, commaMode: true));
        Assert.Equal("-1.5", CobolNum.FormatFunctionText(-15, 1));
    }

    /// <summary>The magnitude is the UNSIGNED one (kb/Work PB2641): negating <c>Int128.MinValue</c> leaves it negative,
    /// which printed "--170141183460469231731687303715884105728".</summary>
    [Fact]
    public void TheSignedMinimum_PrintsOneMinusSign()
    {
        Assert.Equal("-170141183460469231731687303715884105728", CobolNum.FormatFunctionText(Int128.MinValue, 0));
        Assert.Equal("-1701411834604692317316873037158841057,28", CobolNum.FormatFunctionText(Int128.MinValue, 2, commaMode: true));
        Assert.Equal("170141183460469231731687303715884105728", CobolNum.FormatFunctionText(Int128.MinValue, 0, deSign: true));
    }

    /// <summary>DISPLAY is NOT a de-signing context — §14.9.11.4 GR1 transfers the operand's content, and the sign
    /// is part of the value being shown. The default must therefore keep it, or the two contexts collapse.</summary>
    [Theory]
    [InlineData(-14, 0, "-14")]
    [InlineData(-4250, 2, "-42.50")]
    public void WithoutDeSign_TheSignIsKept(long unscaled, int scale, string expected) =>
        Assert.Equal(expected, CobolNum.FormatFunctionText(unscaled, scale));

    /// <summary>The property that actually matters, stated directly: a value reached by the FOLD (which renders
    /// as the literal's own text) and the same value reached by COMPUTATION render identically. This is the fact
    /// that would have caught the original defect class.</summary>
    [Theory]
    [InlineData(7, "7")]
    [InlineData(5, "5")]
    [InlineData(-14, "-14")]
    public void FoldAndCompute_AreIndistinguishable(long value, string literalText)
    {
        // The fold path emits the literal's text verbatim; the computed path formats at scale 0.
        Assert.Equal(literalText, CobolNum.FormatFunctionText(value, 0));
    }
}
