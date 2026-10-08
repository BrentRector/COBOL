// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A PARTIAL EXPRESSION IS A CONDITION WITH ITS LEFTMOST OPERAND MISSING — NOT A SECOND CONDITION LANGUAGE
/// (kb/Work PB398).
///
/// <para><b>ISO §14.9.13.3 SR5</b> defines partial-expression-1 by its LEFT EDGE alone: "A selection object is a
/// partial-expression if the leftmost portion of the selection object is a relational operator, a class condition
/// without the identifier, a sign condition without the identifier, or a sign condition without the arithmetic
/// expression." <b>SR7 d)</b> then fixes everything to its right: "Partial-expression-1 shall be a sequence of
/// COBOL words such that, were it preceded by the corresponding selection subject, a conditional expression would
/// result" — so <c>WHEN &gt; 5 AND &lt; 10</c> and <c>WHEN NUMERIC OR = 0</c> are conforming source and their
/// tails are ORDINARY condition tails.</para>
///
/// <para><b>Why a drift test and not a comment.</b> The partial spine mirrors the three logical tiers, and a mirror
/// is exactly the shape that rots: add an operator to <c>logicalOrExpression</c> and the partial spine silently
/// stops accepting it, rejecting legal source with no test to notice. So the tails are not compared to a
/// hard-coded string — they are EXTRACTED from the condition rules themselves and required to be identical. The
/// only licensed difference between the two spines is the LEADING element, which is the whole of SR5.</para>
/// </summary>
public sealed class PartialExpressionSpineDriftTests
{
    /// <summary>The grammar with its line comments REMOVED — before any rule is located, not after. A prose comment
    /// may contain both a ';' (a §-reference's own punctuation, e.g. "§8.8.4.8; 2002+") and a rule-shaped line, so
    /// extracting first and stripping second truncates <c>comparisonExpression</c> at its first alternative, and a
    /// rule whose comment block sits between its name and its ':' (<c>valueClause</c>) is not found at all. Both
    /// were measured on the first run of this test — the failure branch, fired once, per feedback
    /// green_gates_arent_evidence.</summary>
    private static string Grammar(string file) => Regex.Replace(File.ReadAllText(Path.Combine(TestRepo.Root,
        "src", "Cobol.Net.Frontend", "Grammar", "Core", file)), @"//[^\r\n]*", "");

    private static string RuleBody(string file, string rule)
    {
        var m = Regex.Match(Grammar(file), $@"^{rule}\s*$\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, $"grammar rule '{rule}' not found — if it was renamed this guard must move with it");
        return Normalize(m.Groups["body"].Value);
    }

    /// <summary>Whitespace collapsed — so the comparison is of GRAMMAR, not of formatting.</summary>
    private static string Normalize(string s) => Regex.Replace(s, @"\s+", " ").Trim();

    /// <summary>Everything after the tier's FIRST element: the repeated connective group(s). That is precisely the
    /// part SR7 d) says a partial expression shares with a condition.</summary>
    private static string TailAfter(string body, string firstElement)
    {
        Assert.StartsWith(firstElement, body, StringComparison.Ordinal);
        return body[firstElement.Length..].Trim();
    }

    [Theory]
    // partial tier            its first element        the condition tier it mirrors   that tier's first element
    [InlineData("partialExpression", "partialXorExpression", "logicalOrExpression", "logicalXorExpression")]
    [InlineData("partialXorExpression", "partialAndExpression", "logicalXorExpression", "logicalAndExpression")]
    [InlineData("partialAndExpression", "partialComparison", "logicalAndExpression", "unaryLogicalExpression")]
    public void EachPartialTier_HasTheSameTailAsTheConditionTierItMirrors(
        string partialRule, string partialHead, string conditionRule, string conditionHead)
    {
        string partialTail = TailAfter(RuleBody("CobolExpressions.g4", partialRule), partialHead);
        string conditionTail = TailAfter(RuleBody("CobolExpressions.g4", conditionRule), conditionHead);
        Assert.Equal(conditionTail, partialTail);
    }

    /// <summary>⛔ THE SUCCEEDING TIERS ARE THE SAME MIRROR (kb/Work PB1390). Every connective's operand — at the OR,
    /// XOR and AND tier alike — is a SUCCEEDING tier whose leftmost element may be §8.8.4.12's abbreviated relation
    /// ("any relation condition except the first may be abbreviated", §8.8.4.12.1). They differ from the leading
    /// tiers in the leading element only, so their tails are extracted from the leading tiers and required
    /// identical: the abbreviated tail used to be spelled three different ways, one per tier, and each spelling
    /// rejected a different legal mix.</summary>
    [Theory]
    // succeeding tier            its first element          the leading tier it mirrors   that tier's first element
    [InlineData("succeedingXorExpression", "succeedingAndExpression", "logicalXorExpression", "logicalAndExpression")]
    [InlineData("succeedingAndExpression", "succeedingCondition", "logicalAndExpression", "unaryLogicalExpression")]
    public void EachSucceedingTier_HasTheSameTailAsTheLeadingTierItMirrors(
        string succeedingRule, string succeedingHead, string leadingRule, string leadingHead)
    {
        string succeedingTail = TailAfter(RuleBody("CobolExpressions.g4", succeedingRule), succeedingHead);
        string leadingTail = TailAfter(RuleBody("CobolExpressions.g4", leadingRule), leadingHead);
        Assert.Equal(leadingTail, succeedingTail);
    }

    /// <summary>Every connective of every leading tier takes a SUCCEEDING operand, and a succeeding condition is an
    /// abbreviated relation or an ordinary (NOT-)condition — nothing else. A connective that took a leading-tier
    /// operand again would reject `A = B OR &lt; C` at that tier (the PB1390 shape).</summary>
    [Fact]
    public void EveryConnective_TakesASucceedingOperand()
    {
        Assert.Equal("logicalXorExpression ( OR succeedingXorExpression )*",
            RuleBody("CobolExpressions.g4", "logicalOrExpression"));
        Assert.Equal("logicalAndExpression ( xorOperator succeedingAndExpression )*",
            RuleBody("CobolExpressions.g4", "logicalXorExpression"));
        Assert.Equal("unaryLogicalExpression ( AND succeedingCondition )*",
            RuleBody("CobolExpressions.g4", "logicalAndExpression"));
        Assert.Equal("abbreviatedRelation | unaryLogicalExpression",
            RuleBody("CobolExpressions.g4", "succeedingCondition"));
    }

    /// <summary>SR5's three shapes, each named in <c>partialComparison</c>, and each the SAME shape
    /// <c>comparisonExpression</c> writes with a leading <c>comparisonOperand</c>. A shape that appears in one and
    /// not the other means one of the two spellings of a rule can express something the other cannot.</summary>
    [Fact]
    public void PartialComparison_IsComparisonExpression_MinusItsLeadingOperand()
    {
        string partial = RuleBody("CobolExpressions.g4", "partialComparison");
        string written = RuleBody("CobolExpressions.g4", "comparisonExpression");

        // SR5 shape 1 — "a class condition without the identifier".
        Assert.Contains("IS? NOT? classConditionName", partial, StringComparison.Ordinal);
        Assert.Contains("comparisonOperand IS? NOT? classConditionName", written, StringComparison.Ordinal);
        // SR5 shapes 3 and 4 — "a sign condition without the identifier, or a sign condition without the
        // arithmetic expression": §8.8.4.7.2's two operand forms occupy ONE elided position, so one alternative.
        Assert.Contains("IS? NOT? (POSITIVE | NEGATIVE | ZERO)", partial, StringComparison.Ordinal);
        Assert.Contains("comparisonOperand IS? NOT? (POSITIVE | NEGATIVE | ZERO)", written, StringComparison.Ordinal);
        // SR5 shape 2 — "the leftmost portion … is a relational operator": §8.8.4.12's already-elided relation,
        // reused rather than re-spelled, so the operator list cannot differ between the two.
        Assert.Contains("abbreviatedRelation", partial, StringComparison.Ordinal);
        Assert.Equal("comparisonOperator comparisonOperand", RuleBody("CobolExpressions.g4", "abbreviatedRelation"));
    }

    /// <summary>⛔ <c>partialExpression</c> comes LAST among <c>evaluateWhenItem</c>'s shapes. <c>classConditionName</c>'s
    /// user-word alternative matches ONE bare word, so an earlier position would claim every bare identifier-2 —
    /// the mirror image of the ordering hazard <c>EvaluateSelectionObjectArityDriftTests</c> pins on the
    /// <c>valueOperand</c>/<c>condition</c> pair, and unanswerable in the grammar for the same reason: Table 15
    /// makes the object's legality depend on the SUBJECT.</summary>
    [Fact]
    public void WhenItem_PutsPartialExpression_AfterTheOtherShapes()
    {
        var alts = RuleBody("CobolControlFlow.g4", "evaluateWhenItem")
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim()).Where(a => a.Length > 0).ToArray();
        int partial = Array.IndexOf(alts, "partialExpression");
        int value = Array.IndexOf(alts, "valueOperand");
        int range = Array.IndexOf(alts, "valueRange");
        int cond = Array.IndexOf(alts, "condition");
        Assert.True(partial >= 0, $"evaluateWhenItem lost its partial-expression alternative (ISO §14.9.13.3 SR5); "
            + $"got [{string.Join(" | ", alts)}]");
        Assert.True(partial > value && partial > range && partial > cond,
            "evaluateWhenItem's partialExpression alternative moved ahead of valueOperand / valueRange / condition. "
            + "classConditionName's cobolWord alternative matches a bare word, so it would claim every bare identifier-2 "
            + $"object. Order: [{string.Join(" | ", alts)}]");
    }

    /// <summary>The range's <c>IN alphabet-name-1</c> phrase (ISO §14.9.13.2's range-expression; §14.7.8 rule 2) —
    /// present, and present on the EVALUATE range specifically. Its VALUE-clause twin lives in
    /// <c>CobolData.g4</c>'s <c>valueClause</c>, where §14.7.8's first sentence puts it. IN is an OPTIONAL word in
    /// both (not underlined, §5.2.3; kb/Work PB983): the EVALUATE rule admits it omitted, and the VALUE clause's
    /// IN-less spelling is peeled by symbol in the binder (<c>TrailingInPhraseDriftTests</c> pins that half).</summary>
    [Fact]
    public void ValueRange_CarriesTheInAlphabetPhrase()
    {
        Assert.Equal("valueOperand (THRU | THROUGH) valueOperand (IN? cobolWord)?",
            RuleBody("CobolExpressions.g4", "valueRange"));
        Assert.Contains("(IN cobolWord)?", RuleBody("CobolData.g4", "valueClause"), StringComparison.Ordinal);
    }
}
