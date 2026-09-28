// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §15.3 type 6 — "An arithmetic expression that will always result in an integer value or an integer data item
/// shall be specified" — judged on the expression's VALUE when its non-integral part is a literal constant
/// (kb/Work PB617). <c>IntrinsicArgumentRules.CollectAdditive</c> used to accumulate data items only, so a literal's
/// fraction never entered the integrality net: <c>FUNCTION CHAR(1.5 + 1)</c> (= 2.5) compiled clean while the bare
/// <c>CHAR(1.5)</c> was refused. Each row is an argument operand and whether the screen must refuse it; the admitted
/// rows pin the soundness half (the constant is a SUM — <c>1.5 + 0.5</c> is 2 — and a term the fold cannot value or
/// prove integral fails open).
/// </summary>
public sealed class IntegerArgumentConstantWitnessTests
{
    private static BoundExpr Lit(string text) => new BoundNumLiteral(text);

    private static BoundExpr Bin(BoundExpr l, char op, BoundExpr r) => new BoundBinary(l, op, r);

    /// <summary>A reference to an elementary numeric item of <paramref name="digits"/> / <paramref name="scale"/>.</summary>
    private static BoundExpr Item(string name, int digits, int scale) =>
        new BoundNumRef(new MemberPlace(
            new AccessPath([new RootFieldSegment(name.Replace('-', '_'))]),
            new DataItem
            {
                Level = 1, CobolName = name, CsName = name.Replace('-', '_'),
                Pic = new PicInfo(PicCategory.Numeric, Usage.Display, Length: digits, Digits: digits, Scale: scale, Signed: false),
            }));

    private static readonly BoundExpr IntItem = Item("W-I", 3, 0);
    private static readonly BoundExpr ScaledItem = Item("W-S", 2, 1);

    public static TheoryData<string, BoundExpr, bool> Cases() => new()
    {
        // Refused: the literal constant is not an integer and every other term is one.
        { "1.5 + 1", Bin(Lit("1.5"), '+', Lit("1")), true },
        { "(1.5) parenthesized", Lit("1.5"), true },
        { "W-I + 0.5", Bin(IntItem, '+', Lit("0.5")), true },
        { "W-I * 2 + 0.5", Bin(Bin(IntItem, '*', Lit("2")), '+', Lit("0.5")), true },
        { "3 / 2 + 1", Bin(Bin(Lit("3"), '/', Lit("2")), '+', Lit("1")), true },
        { "1 / 3", Bin(Lit("1"), '/', Lit("3")), true },
        { "W-S - W-S + 0.5", Bin(Bin(ScaledItem, '-', ScaledItem), '+', Lit("0.5")), true },
        { "- 0.25 + 2", Bin(new BoundNegate(Lit("0.25")), '+', Lit("2")), true },
        { "2 ** -1", new BoundPower(Lit("2"), new BoundNegate(Lit("1"))), true },
        // Admitted: the constant is integral, or a term defeats the proof (fail open).
        { "1.5 + 0.5", Bin(Lit("1.5"), '+', Lit("0.5")), false },
        { "3 - 1", Bin(Lit("3"), '-', Lit("1")), false },
        { "6.5E1", Bin(Lit("6.5E1"), '+', Lit("0")), false },
        { "W-I + 1.5 + 1.5", Bin(Bin(IntItem, '+', Lit("1.5")), '+', Lit("1.5")), false },
        { "0.5 * 4", Bin(Lit("0.5"), '*', Lit("4")), false },
        { "2 ** 6 + 1", Bin(new BoundPower(Lit("2"), Lit("6")), '+', Lit("1")), false },
        { "W-S * 2 + 65.5 (fail open)", Bin(Bin(ScaledItem, '*', Lit("2")), '+', Lit("65.5")), false },
        { "W-I / 2 + 0.5 (fail open)", Bin(Bin(IntItem, '/', Lit("2")), '+', Lit("0.5")), false },
        { "1 / 0 + 0.5 (zero divide is not this screen's)", Bin(Bin(Lit("1"), '/', Lit("0")), '+', Lit("0.5")), false },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheIntegerScreen_JudgesTheLiteralConstantByValue(string shape, BoundExpr expr, bool refused)
    {
        string? why = IntrinsicArgumentRules.IntegerViolation(new BoundComputedOperand(expr));
        if (refused)
            Assert.True(why is not null,
                $"'{shape}' is never an integer for any valuation, so ISO §15.3 type 6 does not admit it — but the "
                + "integer screen admitted it (kb/Work PB617: the additive spine must fold its literal terms).");
        else
            Assert.True(why is null,
                $"'{shape}' must stay admitted (its value is integral, or the screen cannot prove otherwise and must "
                + $"fail open) — but the screen refused it: {why}");
    }
}
