// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Expressions;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1592 — the compile-time arithmetic MODE is the edition's (ISO §7.3.6.3 GR2 + Annex E.2 items 6 and 21):
/// at 2002 and 2014 the previous standards PRESCRIBED standard arithmetic ("The previous COBOL Standard required the
/// use of an arithmetic mode that is no longer supported", E.2 6); the one mode 2023 removed is Standard Arithmetic,
/// E.2 21), which WiseOwl COBOL runs on the SDIDI decimal engine (34 digits, decimal128 range, §8.8.1.5.2) with the
/// standard-decimal default intermediate rounding NEAREST-AWAY-FROM-ZERO (§11.9.11.2 GR3 a); from 2023 the mode is
/// the documented System.Decimal one (docs/CONFORMANCE.md DOC-A.1-29). Every expected value below is derived from
/// those rules by hand, then §7.3.6.3 GR3 (truncate the final result to its integer part), never read off the
/// compiler.
/// </summary>
public sealed class CompileTimeArithmeticModeTests
{
    private static (CompileTimeExpressionEvaluator.CtNumber? Result, CompileTimeExpressionEvaluatorTests.CollectingDiag Diag)
        Eval(string text, int edition) => CompileTimeExpressionEvaluatorTests.Eval(text, edition: edition);

    /// <summary>The ONE selection, read through the ONE per-edition behaviour register.</summary>
    [Theory]
    [InlineData(2002, false)]
    [InlineData(2014, false)]
    [InlineData(2023, true)]
    public void Selection_IsStandardArithmeticBelow2023(int edition, bool implementorDefined)
    {
        Assert.Equal(implementorDefined,
            DialectBehaviors.IsActive(DialectBehavior.CompileTimeArithmeticImplementorDefined, edition));
        Assert.Same(implementorDefined ? CompileTimeArithmetic.SystemDecimal : CompileTimeArithmetic.Standard,
            CompileTimeArithmetic.For(EditionInfo.Of(edition)));
    }

    /// <summary>Expressions whose value DIFFERS between the two modes, each derived from its mode's rule:
    /// <list type="bullet">
    /// <item><c>9999999999999999999999999999 * 10</c> — the exact product 99999999999999999999999999990 has 29
    /// digits: within the SDIDI's 34 (2002/2014), beyond System.Decimal's 7.9E28 (2023: refused, SR2).</item>
    /// <item><c>0.0000000000000000000000000015 / 2 * 10000000000000000000000000000</c> — 1.5E-27 / 2 = 7.5E-28 is
    /// exact in the SDIDI, ×1E28 = 7.5, GR3 → 7; System.Decimal holds 28 places, so 7.5E-28 is a tie that rounds
    /// to the even digit, 8E-28, ×1E28 = 8.</item>
    /// </list></summary>
    [Theory]
    [InlineData("9999999999999999999999999999 * 10", 2002, "99999999999999999999999999990")]
    [InlineData("9999999999999999999999999999 * 10", 2014, "99999999999999999999999999990")]
    [InlineData("9999999999999999999999999999 * 10", 2023, null)]
    [InlineData("0.0000000000000000000000000015 / 2 * 10000000000000000000000000000", 2002, "7")]
    [InlineData("0.0000000000000000000000000015 / 2 * 10000000000000000000000000000", 2014, "7")]
    [InlineData("0.0000000000000000000000000015 / 2 * 10000000000000000000000000000", 2023, "8")]
    public void ModeDependentValues_FollowTheEditionsMode(string expr, int edition, string? expected)
    {
        var (r, diag) = Eval(expr, edition);
        if (expected is null)
        {
            Assert.Null(r);
            Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule
                                               && x.Message.Contains(".NET decimal evaluation range", StringComparison.Ordinal)
                                               && x.Message.Contains("SR2", StringComparison.Ordinal));
            return;
        }
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, r!.Value.Literal);
        Assert.False(r.Value.WasSingleLiteral);
    }

    /// <summary>Division ROUNDS in both modes, so these agree: 2/3 = 0.666…667 (34 digits, away from zero) ×3 =
    /// 2.000…0001 → 34 digits 2.000…000 → 2; 100/7 = 14.2857…1429 ×7 = 100.000…0003 → 100; 1/3 ×3 = 0.999…999 → 0.
    /// (A truncating division would give 1, 99 and 0.)</summary>
    [Theory]
    [InlineData("2 / 3 * 3", "2")]
    [InlineData("100 / 7 * 7", "100")]
    [InlineData("1 / 3 * 3", "0")]
    [InlineData("-7 / 2", "-3")]
    public void ModeIndependentValues_AgreeAtEveryEdition(string expr, string expected)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (r, diag) = Eval(expr, edition);
            Assert.Empty(diag.Reports);
            Assert.Equal(expected, r!.Value.Literal);
        }
    }

    /// <summary>The standard-decimal default intermediate rounding is NEAREST-AWAY-FROM-ZERO (§11.9.11.2 GR3 a),
    /// observed at a TIE. 1000000000000000000000000000001 × 10005 = 10005000000000000000000000000010005 exactly —
    /// 35 digits, so the 34-digit SDIDI drops a final 5 with nothing after it: away from zero keeps
    /// …0010010, nearest-even would keep …0010000. Subtracting the exact 10^30 × 10005 exposes the choice:
    /// 10010 (away) where nearest-even gives 10000. (At 2023 the 31-digit operand is outside System.Decimal.)</summary>
    [Fact]
    public void StandardArithmetic_RoundsATieAwayFromZero()
    {
        const string expr = "1000000000000000000000000000001 * 10005 - 1000000000000000000000000000000 * 10005";
        var (r, diag) = Eval(expr, 2002);
        Assert.Empty(diag.Reports);
        Assert.Equal("10010", r!.Value.Literal);
    }

    /// <summary>§7.3.6.3 GR3 makes the final result "an integer numeric literal", and a fixed-point numeric literal
    /// has at most 31 digits (§8.3.3.3.2). Standard arithmetic's 34-digit intermediates can carry a product no
    /// literal can spell — two 31-digit operands give about 1E62 — so the result is refused LOUDLY, never stored as a
    /// 62-digit "literal". At 2023 the same operands never enter System.Decimal's range (SR2).</summary>
    [Fact]
    public void FinalResult_BeyondTheLiteralCapacity_IsRefused()
    {
        const string expr = "9999999999999999999999999999999 * 9999999999999999999999999999999";
        var (r, diag) = Eval(expr, 2002);
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule
                                           && x.Message.Contains("62 digits", StringComparison.Ordinal)
                                           && x.Message.Contains("§7.3.6.3 GR3", StringComparison.Ordinal)
                                           && x.Message.Contains("§8.3.3.3.2", StringComparison.Ordinal));

        var (r23, diag23) = Eval(expr, 2023);
        Assert.Null(r23);
        Assert.Contains(diag23.Reports, x => x.Message.Contains("SR2", StringComparison.Ordinal)
                                             && !x.Message.Contains("SR1b", StringComparison.Ordinal));
    }

    /// <summary>The capacity's own edge: a 31-digit final result IS a valid integer literal and is kept (the refusal
    /// above starts at 32).</summary>
    [Fact]
    public void FinalResult_AtTheLiteralCapacity_IsKept()
    {
        var (r, diag) = Eval("9999999999999999999999999999999 + 0", 2002);
        Assert.Empty(diag.Reports);
        Assert.Equal("9999999999999999999999999999999", r!.Value.Literal);

        var (r32, diag32) = Eval("9999999999999999999999999999999 + 1", 2002);   // 10^31: 32 digits
        Assert.Null(r32);
        Assert.Contains(diag32.Reports, x => x.Message.Contains("32 digits", StringComparison.Ordinal));
    }

    /// <summary>A SINGLE numeric literal is a literal, not an arithmetic expression (§7.3.11.4 GR5), so no arithmetic
    /// mode bounds it: a legal 30-digit fixed-point literal (§8.3.3.3.2 allows 31) keeps its value at 2023 too. It
    /// used to be refused there as "exceeding the .NET decimal evaluation range" — a mode that never applied to it.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void SoleLiteral_IsNotBoundByTheArithmeticMode(int edition)
    {
        var (r, diag) = Eval("123456789012345678901234567890", edition);
        Assert.Empty(diag.Reports);
        Assert.True(r!.Value.WasSingleLiteral);
        Assert.Equal("123456789012345678901234567890", r.Value.Literal);
    }

    /// <summary>An operand the mode cannot hold is the §7.3.6.2 SR2 limit, reported as SR2 — not as an SR1 b)
    /// "shall be fixed-point numeric literals" violation it is not (the operand IS a fixed-point literal).</summary>
    [Fact]
    public void OperandBeyondTheModesRange_IsReportedAsSr2()
    {
        var (r, diag) = Eval("123456789012345678901234567890 + 1", 2023);
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Message.Contains("SR2", StringComparison.Ordinal)
                                           && !x.Message.Contains("SR1b", StringComparison.Ordinal));

        var (r02, diag02) = Eval("123456789012345678901234567890 + 1", 2002);
        Assert.Empty(diag02.Reports);
        Assert.Equal("123456789012345678901234567891", r02!.Value.Literal);
    }

    /// <summary>An operand literal past the §8.3.3.3.2 digit capacity is refused by that rule, in every mode.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void OperandLiteral_BeyondTheLiteralCapacity_IsRefused(int edition)
    {
        var (r, diag) = Eval("12345678901234567890123456789012 + 1", edition);
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Message.Contains("32 digit positions", StringComparison.Ordinal)
                                           && x.Message.Contains("§8.3.3.3.2", StringComparison.Ordinal));
    }

    /// <summary>Numeric value equality is by VALUE (§7.3.11.3 SR2 redefinition / §7.3.8 relation), so two carrier
    /// spellings of one value — 1 and 1.0 — are equal and hash alike.</summary>
    [Fact]
    public void CarrierEquality_IsByValue()
    {
        Assert.True(CtNumeric.TryParseLiteral("1", out var one));
        Assert.True(CtNumeric.TryParseLiteral("1.0", out var onePointZero));
        var a = CtValue.Numeric(one, "1");
        var b = CtValue.Numeric(onePointZero, "1.0");
        Assert.Equal(a, b);
        Assert.True(a.RelationalEquals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, CtValue.Numeric(new CobolDec(2, 0), "2"));
    }
}
