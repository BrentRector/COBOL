// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Linq;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using CobolNet.Validation;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §5.5 1)'s NONZERO default is enforced by ONE screen (<c>Validation/IntegerOperandPass</c>, kb/Work PB859)
/// over every <c>integerLiteral</c> — "the term 'integer-n' … refers to a fixed-point integer literal that shall be
/// unsigned and nonzero unless otherwise specified in the associated rules". The screen is automatic; what is NOT
/// automatic is the EXCEPTION question — does this format's rule otherwise-specify? — and a new grammar rule that
/// writes <c>integerLiteral</c> gets the default silently. So the exception table's key set is DERIVED from the
/// generated parser here, in both directions: every rule context exposing an <c>integerLiteral()</c> accessor must
/// be classified, and every classified type must still expose one.
/// </summary>
public sealed class IntegerOperandSlotDriftTests
{
    /// <summary>Every rule that spells an <c>integer-n</c>: with the bare <c>integerLiteral</c> or with the shared
    /// <c>integerOperand</c> (a literal OR a constant-name, ISO §13.10.3 SR2 — kb/Work PB1947). The
    /// <c>integerOperand</c> rule is the carrier, never a clause, so it is the one rule left out: its OWNER is what
    /// <see cref="IntegerOperandRules.Slots"/> classifies.</summary>
    private static Type[] RulesThatWriteIntegerLiteral() =>
        typeof(CobolParserCore).GetNestedTypes()
            .Where(t => t != typeof(CobolParserCore.IntegerOperandContext)
                && t.GetMethods().Any(m => m.Name is "integerLiteral" or "integerOperand" && m.DeclaringType == t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Every rule that spells a BARE <c>integerLiteral</c> — an <c>integer-n</c> position that admits only a
    /// written literal. The <c>integerOperand</c> carrier is the one rule left out (it is the SHARED spelling, whose
    /// <c>integerLiteral</c> arm is the literal half of "a literal or a constant-name").</summary>
    private static Type[] RulesThatWriteBareIntegerLiteral() =>
        typeof(CobolParserCore).GetNestedTypes()
            .Where(t => t != typeof(CobolParserCore.IntegerOperandContext)
                && t.GetMethods().Any(m => m.Name == "integerLiteral" && m.DeclaringType == t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>⛔ EVERY integer-n POSITION OF THE GRAMMAR IS A LITERAL POSITION A CONSTANT-NAME STANDS IN (kb/Work
    /// PB1947 for the report writer, PB1948 for the rest). ISO §13.10.3 SR2 — "constant-name-1 may be used anywhere that
    /// a format specifies a literal of the class and category of constant-name-1" — and §5.5 1) calls every
    /// <c>integer-n</c> "a fixed-point integer literal", so a clause that prints one writes <c>integerOperand</c>
    /// (a literal or a constant-name), never the bare <c>integerLiteral</c>. The set of rules that still write the bare
    /// one is DERIVED from the generated parser and must equal <see cref="IntegerOperandRules.LiteralOnlySlots"/>,
    /// each row carrying the argument for why a constant-name does not stand there: the clause that is added next admits
    /// a constant-name without anyone remembering to, or is red until it says why not. (A <c>COBOL0309 "A literal value
    /// is expected here, not a data-name"</c> on <c>LINE PLUS KL</c>, <c>RESERVE KR AREAS</c> or <c>BLOCK CONTAINS KB
    /// RECORDS</c> was the defect.)</summary>
    [Fact]
    public void EveryRuleWritingABareIntegerLiteral_IsAnArguedLiteralOnlySlot()
    {
        Assert.True(RulesThatWriteIntegerLiteral().Count(t => t.GetMethods().Any(m => m.Name == "integerOperand" && m.DeclaringType == t)) >= 15,
            "fewer than 15 rules write integerOperand — the scrape is blind");
        var unargued = RulesThatWriteBareIntegerLiteral().Where(t => !IntegerOperandRules.LiteralOnlySlots.ContainsKey(t))
            .Select(t => t.Name).ToArray();
        Assert.True(unargued.Length == 0,
            "grammar rule(s) spell a bare integerLiteral — an integer-n position takes `integerOperand` so an integer "
            + "constant-name stands there (ISO §13.10.3 SR2), or the rule is argued in IntegerOperandRules.LiteralOnlySlots: "
            + string.Join(", ", unargued));
    }

    [Fact]
    public void EveryLiteralOnlySlot_StillWritesABareIntegerLiteral_AndCarriesItsReason()
    {
        var bare = RulesThatWriteBareIntegerLiteral().ToHashSet();
        var stale = IntegerOperandRules.LiteralOnlySlots.Keys.Where(t => !bare.Contains(t)).Select(t => t.Name).ToArray();
        Assert.True(stale.Length == 0, "IntegerOperandRules.LiteralOnlySlots names rule(s) that no longer write a bare "
            + "integerLiteral: " + string.Join(", ", stale));
        Assert.All(IntegerOperandRules.LiteralOnlySlots, kv => Assert.False(string.IsNullOrWhiteSpace(kv.Value), kv.Key.Name));
        // Every argued rule is also a classified one — the zero permission is asked of its operand all the same.
        var unclassified = IntegerOperandRules.LiteralOnlySlots.Keys.Where(t => !IntegerOperandRules.Slots.ContainsKey(t))
            .Select(t => t.Name).ToArray();
        Assert.True(unclassified.Length == 0, "LiteralOnlySlots rule(s) missing from Slots: " + string.Join(", ", unclassified));
    }

    [Fact]
    public void EveryGrammarRuleWritingIntegerLiteral_IsClassified()
    {
        var derived = RulesThatWriteIntegerLiteral();
        Assert.True(derived.Length >= 30, $"only {derived.Length} rules write integerLiteral — the scrape is blind");
        var missing = derived.Where(t => !IntegerOperandRules.Slots.ContainsKey(t)).Select(t => t.Name).ToArray();
        Assert.True(missing.Length == 0,
            "grammar rule(s) write integerLiteral but are not classified in IntegerOperandRules.Slots — read the "
            + "format's associated rules for a zero permission (§5.5 1) 'unless otherwise specified') and add a row: "
            + string.Join(", ", missing));
    }

    [Fact]
    public void EveryClassifiedRule_StillWritesIntegerLiteral()
    {
        var derived = RulesThatWriteIntegerLiteral().ToHashSet();
        var stale = IntegerOperandRules.Slots.Keys.Where(t => !derived.Contains(t)).Select(t => t.Name).ToArray();
        Assert.True(stale.Length == 0, "IntegerOperandRules.Slots names rule(s) that no longer write integerLiteral: "
            + string.Join(", ", stale));
    }

    /// <summary>A zero permission is a claim about the STANDARD, so it carries its rule; a not-governed row carries
    /// its reason. Neither may be blank.</summary>
    [Fact]
    public void EveryException_CarriesItsBasis()
    {
        foreach (var slot in ExceptionSlots())
        {
            Assert.False(string.IsNullOrWhiteSpace(slot.Basis));
            if (slot.Kind == IntegerSlotKind.ZeroPermitted) Assert.StartsWith("ISO §", slot.Basis);
        }
    }

    /// <summary>The host-limit screen's exemption set (kb/Work PB1058) names grammar rules that are classified here
    /// too — a rule dropped from the grammar cannot linger in the exemption and silently exempt nothing.</summary>
    [Fact]
    public void EveryFullValueSlot_IsAClassifiedRule()
    {
        Assert.NotEmpty(IntegerOperandRules.FullValueSlots);
        var stale = IntegerOperandRules.FullValueSlots.Where(t => !IntegerOperandRules.Slots.ContainsKey(t))
            .Select(t => t.Name).ToArray();
        Assert.True(stale.Length == 0, "FullValueSlots names rule(s) IntegerOperandRules.Slots does not: "
            + string.Join(", ", stale));
    }

    /// <summary>⛔ The ONE binder reader never throws (kb/Work PB1058): a value past the host limit — up to the
    /// 31-digit literal maximum, §8.3.3.3.2 — reads as the limit, which the pre-bind screen has already reported.
    /// <c>int.Parse</c> at this position took the compiler down with an unhandled <c>OverflowException</c>.</summary>
    [Theory]
    [InlineData("2147483647", 2147483647)]
    [InlineData("2147483648", int.MaxValue)]
    [InlineData("77777777777", int.MaxValue)]
    [InlineData("9999999999999999999999999999999", int.MaxValue)]
    [InlineData("0000000000000000000000000000012", 12)]
    public void HostValue_SaturatesAtTheLimit(string digits, int expected)
    {
        var tree = new CobolParserCore.IntegerLiteralContext(null, 0);
        tree.AddChild(new Antlr4.Runtime.Tree.TerminalNodeImpl(new Antlr4.Runtime.CommonToken(CobolParserCore.INTEGERLIT, digits)));
        Assert.Equal(expected, IntegerOperandRules.HostValue(tree));
        Assert.Equal(expected == int.MaxValue && digits != "2147483647", IntegerOperandRules.BeyondHostLimit(tree));
    }

    private static System.Collections.Generic.IEnumerable<IntegerSlot> ExceptionSlots() =>
        IntegerOperandRules.Slots.Values
            .Select(c => { try { return c(null!, null!); } catch (Exception) { return IntegerSlot.Default; } })
            .Where(s => s.Kind != IntegerSlotKind.NonZero);
}
