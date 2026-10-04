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

    /// <summary>⛔ THE REPORT WRITER'S integer-n POSITIONS ARE ALL LITERAL POSITIONS (kb/Work PB1947). ISO §13.10.3
    /// SR2 — "constant-name-1 may be used anywhere that a format specifies a literal of the class and category of
    /// constant-name-1" — and §5.5 1) calls every <c>integer-n</c> "a fixed-point integer literal", so a constant-name
    /// stands at every integer position of the PAGE, LINE, COLUMN and NEXT GROUP clauses and of OCCURS … STEP. The
    /// report-writer grammar therefore spells NONE of them with the bare <c>integerLiteral</c>: it writes
    /// <c>integerOperand</c>, so the clause that is added next admits a constant-name without anyone remembering to.
    /// (A <c>COBOL0309 "A literal value is expected here, not a data-name"</c> on <c>LINE PLUS KL</c> was the defect.)
    /// </summary>
    [Fact]
    public void ReportWriterGrammar_SpellsNoBareIntegerLiteral()
    {
        string path = System.IO.Path.Combine(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "Core", "CobolReportWriter.g4");
        string body = System.Text.RegularExpressions.Regex.Replace(
            System.IO.File.ReadAllText(path), @"//[^\r\n]*", string.Empty);
        Assert.Contains("integerOperand", body);   // a ZERO-population "clean" result is the failure this prevents
        Assert.DoesNotMatch(@"\bintegerLiteral\b", body);
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
