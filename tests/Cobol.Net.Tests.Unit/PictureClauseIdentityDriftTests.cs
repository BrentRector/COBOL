// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Editions;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ "THE SAME PICTURE CLAUSE" HAS ONE IDENTITY, AND EVERY ASKER READS IT (kb/Work PB1166). The standard asks for
/// identical elementary descriptions in five places — §8.5.3.1 type equivalence, §9.3.6 method matching, §9.3.8.2.3
/// interface conformance (overrides, IMPLEMENTS), §14.8.2.3.2 BY REFERENCE arguments, §14.8.3.3 returning items —
/// each with the same two exceptions: currency symbols match only when their currency STRINGS do, and period /
/// comma symbols only when DECIMAL-POINT IS COMMA is in effect for both source elements or neither. Before PB1166
/// the activation / signature comparator (<see cref="OoConformance.DescriptionMismatch"/>) compared a per-category
/// subset of the picture — the edit mask text, or nothing — so a class under DECIMAL-POINT IS COMMA returned
/// <c>Z9,99</c> into a non-DPC <c>Z9,99</c> receiver that then read 1234 for 12.34, <c>PIC A(5)</c> passed for
/// <c>PIC X(5)</c>, and ALIGNED / DYNAMIC LENGTH were compared nowhere.
/// <para>This pins the properties that keep the identity single: (1) the comparator honours every axis of
/// <see cref="PictureClauseIdentity"/>, measured over pictures ANALYZED by <see cref="PictureAnalyzer"/> — never
/// hand-built profiles that could omit the field; (2) the universal-dispatch relation's §9.3.6 match
/// (<see cref="ActivationRelations.Matches"/> over <see cref="ActivationDescriptions"/>, kb/Work PB480) agrees with
/// the comparator over every pair of elementary items; (3) the non-PICTURE clauses are read
/// through the one <see cref="DescriptionClauses"/> predicate the §8.5.3.1 compare reads too.</para>
/// </summary>
public sealed class PictureClauseIdentityDriftTests
{
    private static CurrencyDefinition Alnum(string text) => new(text, CobolNet.Common.LiteralClass.Alphanumeric);

    private static readonly IReadOnlyDictionary<char, CurrencyDefinition> Dollar = new Dictionary<char, CurrencyDefinition> { ['$'] = Alnum("$") };

    private static DataItem Item(string picture, IReadOnlyDictionary<char, CurrencyDefinition>? currencies = null,
        bool dpc = false, Usage usage = Usage.Display)
    {
        var ed = new EditionContext(2023);
        var pic = PictureAnalyzer.Analyze(picture, usage, ed, "data item 'P'", currencies: currencies ?? Dollar,
            decimalPointIsComma: dpc);
        Assert.False(pic.IsRecovery, $"PIC {picture} did not analyze");
        return new DataItem { Level = 1, CobolName = "P", CsName = "P", Pic = pic, PictureText = picture };
    }

    [Fact]
    public void EveryAnalyzedPicture_CarriesItsClauseIdentity()
    {
        foreach (string p in new[] { "X(5)", "A(5)", "9(4)V99", "S9(3)", "ZZ9.99", "$$$9.99", "N(3)", "1(8)", "XBX" })
            Assert.NotNull(Item(p).Pic!.Clause);
    }

    [Fact]
    public void CurrencySymbols_MatchIffTheirStringsMatch()
    {
        var usdDollar = Item("$$$9.99", new Dictionary<char, CurrencyDefinition> { ['$'] = Alnum("USD") });
        var usdU = Item("UUU9.99", new Dictionary<char, CurrencyDefinition> { ['U'] = Alnum("USD") });
        var eurDollar = Item("$$$9.99", new Dictionary<char, CurrencyDefinition> { ['$'] = Alnum("EUR") });
        Assert.Null(OoConformance.DescriptionMismatch(usdDollar, usdU));        // §14.8.2.3.2 rule 2 a): same string
        Assert.Contains("currency string", OoConformance.DescriptionMismatch(usdDollar, eurDollar));
    }

    [Fact]
    public void PeriodAndCommaSymbols_MatchOnlyUnderTheSameDecimalPointSetting()
    {
        Assert.Contains("DECIMAL-POINT IS COMMA",
            OoConformance.DescriptionMismatch(Item("Z9,99", dpc: true), Item("Z9,99", dpc: false)));
        Assert.Contains("DECIMAL-POINT IS COMMA",
            OoConformance.DescriptionMismatch(Item("Z9.99", dpc: true), Item("Z9.99", dpc: false)));
        Assert.Null(OoConformance.DescriptionMismatch(Item("Z9,99", dpc: true), Item("Z9,99", dpc: true)));
        // A picture with neither symbol is the same clause under either setting — the rule speaks of those two only.
        Assert.Null(OoConformance.DescriptionMismatch(Item("ZZ9", dpc: true), Item("ZZ9", dpc: false)));
    }

    [Fact]
    public void AlphabeticAndAlphanumeric_AreDifferentPictureClauses_AndRepetitionIsNot()
    {
        Assert.Contains("PICTURE mismatch", OoConformance.DescriptionMismatch(Item("A(5)"), Item("X(5)")));
        Assert.Null(OoConformance.DescriptionMismatch(Item("X(3)"), Item("XXX")));
    }

    [Fact]
    public void AlignedAndDynamicLength_AreComparedThroughTheOneSharedPredicate()
    {
        var bit = Item("1(8)", usage: Usage.Bit);
        var aligned = Item("1(8)", usage: Usage.Bit);
        aligned.IsAligned = true;
        Assert.Contains("ALIGNED", OoConformance.DescriptionMismatch(aligned, bit));
        var plain = Item("X");
        var dynamic = Item("X");
        dynamic.IsDynamicLength = true;
        Assert.Contains("DYNAMIC LENGTH", OoConformance.DescriptionMismatch(dynamic, plain));
        Assert.Equal(DescriptionClauses.AlignedOrDynamicLengthMismatch(dynamic, plain),
            OoConformance.DescriptionMismatch(dynamic, plain));
    }

    /// <summary>The universal-dispatch relation agrees with the comparator (kb/Work PB480): over every ordered pair of
    /// ELEMENTARY items drawn from a population that varies each axis the identity carries, §9.3.6 match rule 3 e)
    /// (<see cref="ActivationRelations.Matches"/> over the descriptions <see cref="ActivationDescriptions"/> builds)
    /// holds exactly when the comparator finds the two descriptions identical — the national, boolean and
    /// numeric-edited categories included, which the string descriptor this replaced could not carry. The population
    /// asserts it produced pairs at all.</summary>
    [Fact]
    public void ActivationMatch_AgreesWithTheComparator_OverEveryElementaryPair()
    {
        var usd = new Dictionary<char, CurrencyDefinition> { ['$'] = Alnum("USD") };
        var items = new List<DataItem>
        {
            Item("X(5)"), Item("XXXXX"), Item("A(5)"), Item("X(4)"), Item("XBX"), Item("X9X"),
            Item("9(4)"), Item("9999"), Item("S9(4)"), Item("9(2)V99"), Item("9(2)V99", dpc: true),
            Item("9(4)", usage: Usage.Binary), Item("9(4)", currencies: usd),
            Item("N(3)", usage: Usage.National), Item("N(4)", usage: Usage.National), Item("1(4)"),
            Item("1(4)", usage: Usage.Bit), Item("ZZ9.99"), Item("ZZ9.99", dpc: true), Item("$$9.99"),
            Item("$$9.99", currencies: usd),
        };
        int pairs = 0;
        foreach (var formal in items)
            foreach (var arg in items)
            {
                var df = ActivationDescriptions.Of(formal)!;
                var da = ActivationDescriptions.Of(arg)!;
                pairs++;
                bool identical = OoConformance.DescriptionMismatch(formal, arg) is null;
                bool matches = ActivationRelations.Matches(da, null, df, null!)
                    && ActivationRelations.ParameterViolation(da, df) is null;
                Assert.True(identical == matches,
                    $"PIC {formal.PictureText} vs PIC {arg.PictureText}: comparator says {(identical ? "identical" : "differs")}"
                    + $" but the run-time relation says {(matches ? "matches" : "does not match")} ('{df.Clauses}' / '{da.Clauses}')");
            }
        Assert.True(pairs > 300, $"only {pairs} pairs — the population no longer exercises the invariant");
    }
}
