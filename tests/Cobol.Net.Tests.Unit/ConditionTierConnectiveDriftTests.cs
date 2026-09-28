// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE CONSTANT-CONDITIONAL-EXPRESSION TIERS ARE THE CONDITION TIERS' CONNECTIVES OVER A COMPILE-TIME LEAF, AND THE
/// EXCLUSIVE-OR CONNECTIVE IS SPELLED ONCE (kb/Work PB1371, PB1390).
/// <para><b>ISO §7.3.8.2 SR1 d)</b>: a constant conditional expression may be "A complex condition as specified in
/// 8.8.4.9, Complex conditions", so its connectives are §8.8.4.9's — AND, OR, EXCLUSIVE-OR / XOR and NOT, with
/// §8.8.4.11.3's precedence and Table 5's ban on 'NOT NOT'. ANTLR has no parameterized rules, so the cce tiers
/// (<c>cceOr</c> / <c>cceXor</c> / <c>cceAnd</c> / <c>cceNot</c>) are a second SPELLING of the condition tiers over a
/// different leaf — and a second spelling is the shape that rots: the cce copy never received the 2023 XOR tier and
/// let NOT recurse, so <c>&gt;&gt;IF 1 = 2 XOR 1 = 1</c> was a malformed-expression error and <c>&gt;&gt;IF NOT NOT
/// 1 = 1</c> compiled. Each cce tier's body is therefore compared to its condition tier's with every OPERAND rule
/// name masked: what is left — the connective tokens, the xorOperator rule, the repetition and the optional NOT — must
/// be identical.</para>
/// <para><b>One exclusive-or node.</b> The 2023 introduction gate (<c>LogicalOperatorGate</c>) recognizes the one
/// <c>xorOperator</c> rule; a tier that wrote the XOR / EXCLUSIVE_OR tokens itself would be a connective the gate
/// never sees (the partial-expression spine's XOR was exactly that, kb/Work PB1392). So no parser rule but
/// <c>xorOperator</c> — and <c>reservedGatedWord</c> (generated into CobolWords.g4 from cobol-words.json), where the
/// words are user-defined below 2023 — may name the tokens.</para>
/// </summary>
public sealed class ConditionTierConnectiveDriftTests
{
    private static string GrammarDir => Path.Combine(TestRepo.Root, "src", "Cobol.Net.Frontend", "Grammar");

    /// <summary>A grammar file with its line comments removed BEFORE any rule is located (a prose comment can hold a
    /// ';' or a rule-shaped line — the PartialExpressionSpineDriftTests lesson).</summary>
    private static string Grammar(string file) =>
        Regex.Replace(File.ReadAllText(Path.Combine(GrammarDir, "Core", file)), @"//[^\r\n]*", "");

    /// <summary>A rule's body, whitespace-collapsed — whether the rule is written on one line or several.</summary>
    private static string RuleBody(string rule)
    {
        var m = Regex.Match(Grammar("CobolExpressions.g4"), $@"^{rule}\s*:(?<body>.*?);",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, $"grammar rule '{rule}' not found — if it was renamed this guard must move with it");
        return Regex.Replace(m.Groups["body"].Value, @"\s+", " ").Trim();
    }

    /// <summary>Every parser-rule reference (a lower-case-initial name) masked to <c>_</c> — except
    /// <c>xorOperator</c>, which IS a connective.</summary>
    private static string ConnectiveShape(string body) =>
        Regex.Replace(body, @"\b(?!xorOperator\b)[a-z]\w*\b", "_");

    [Theory]
    // cce tier        the condition tier it spells over a compile-time leaf
    [InlineData("cceOr", "logicalOrExpression")]
    [InlineData("cceXor", "logicalXorExpression")]
    [InlineData("cceAnd", "logicalAndExpression")]
    [InlineData("cceNot", "unaryLogicalExpression")]
    public void EachCceTier_HasTheConnectiveShapeOfItsConditionTier(string cceRule, string conditionRule)
    {
        string cce = ConnectiveShape(RuleBody(cceRule));
        string condition = ConnectiveShape(RuleBody(conditionRule));
        Assert.Equal(condition, cce);
    }

    /// <summary>The comparison above must compare SOMETHING: the four shapes are the four printed connective
    /// forms, so a masking bug that reduced every body to <c>_</c> cannot pass as agreement.</summary>
    [Fact]
    public void TheConnectiveShapes_AreTheFourPrintedForms()
    {
        Assert.Equal("_ ( OR _ )*", ConnectiveShape(RuleBody("logicalOrExpression")));
        Assert.Equal("_ ( xorOperator _ )*", ConnectiveShape(RuleBody("logicalXorExpression")));
        Assert.Equal("_ ( AND _ )*", ConnectiveShape(RuleBody("logicalAndExpression")));
        Assert.Equal("NOT? _", ConnectiveShape(RuleBody("unaryLogicalExpression")));
    }

    /// <summary>The XOR / EXCLUSIVE_OR tokens are named by <c>xorOperator</c> (the connective) and
    /// <c>reservedGatedWord</c> (the user-defined word below 2023, whose §8.9 reservation the funnel enforces) and by
    /// no other parser rule in any grammar file.</summary>
    [Fact]
    public void TheExclusiveOrTokens_AreSpelledOnlyByXorOperator()
    {
        var namers = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(GrammarDir, "*.g4", SearchOption.AllDirectories))
        {
            string text = Regex.Replace(File.ReadAllText(file), @"//[^\r\n]*", "");
            if (Regex.IsMatch(text, @"^\s*lexer\s+grammar\b", RegexOptions.Multiline)) continue;
            foreach (string chunk in text.Split(';'))
            {
                var m = Regex.Match(chunk, @"\A\s*(?<name>[a-z]\w*)\s*:(?<body>.*)\z", RegexOptions.Singleline);
                if (m.Success && Regex.IsMatch(m.Groups["body"].Value, @"\b(XOR|EXCLUSIVE_OR)\b"))
                    namers.Add(m.Groups["name"].Value);
            }
        }
        Assert.Equal(["reservedGatedWord", "xorOperator"], namers);
        Assert.Equal("XOR | EXCLUSIVE_OR", RuleBody("xorOperator"));
    }
}
