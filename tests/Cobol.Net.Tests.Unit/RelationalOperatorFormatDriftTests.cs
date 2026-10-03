// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ONE RELATIONAL-OPERATOR RULE IS THE PRINTED SET OF ISO §8.8.4.2.2 FORMAT 1, AND NOTHING WIDER (kb/Work PB1034).
/// <para>The standard prints eleven alternatives, read off the RENDERED page (PDF p217, printed folio 187) because
/// the diagram's brackets decide it: <c>IS [NOT] GREATER THAN</c>, <c>IS [NOT] &gt;</c>, <c>IS [NOT] LESS THAN</c>,
/// <c>IS [NOT] &lt;</c>, <c>IS [NOT] EQUAL TO</c>, <c>IS [NOT] =</c>, <c>IS &lt;&gt;</c>, <c>IS GREATER THAN OR EQUAL
/// TO</c>, <c>IS &gt;=</c>, <c>IS LESS THAN OR EQUAL TO</c>, <c>IS &lt;=</c> — <c>IS</c> not underlined and so optional,
/// <c>THAN</c> and <c>TO</c> likewise, <c>[NOT]</c> on the first six only. The grammar's <c>comparisonOperator</c> was
/// a SUPERSET (NOT &gt;=, NOT &lt;=, NOT GREATER [THAN] OR EQUAL [TO], NOT LESS [THAN] OR EQUAL [TO], EQUAL [THAN]),
/// <c>MapOperator</c> folded every extra into a valid operator, and the only screen was the START KEY phrase's —
/// so <c>IF A NOT &gt;= B</c> compiled and ran in every edition.</para>
/// <para>The rule is shared by every condition (IF, EVALUATE, PERFORM UNTIL, SEARCH WHEN, the abbreviated tails and the
/// compile-time directive relation), so this test expands the rule's alternatives into spellings and requires them to
/// EQUAL the expansion of the printed set — an extra spelling or a missing one fails — and requires the one other
/// rule that writes a NOT before the operator (<c>cceRelationOrBoolean</c>) to have stopped doing so.</para>
/// </summary>
public sealed class RelationalOperatorFormatDriftTests
{
    private static string Grammar() => File.ReadAllText(TestRepo.Src(
        "Cobol.Net.Frontend", "Grammar", "Core", "CobolExpressions.g4"));

    /// <summary>The body of grammar rule <paramref name="rule"/>, comments stripped, up to its terminating
    /// semicolon.</summary>
    private static string RuleBody(string rule)
    {
        string text = Regex.Replace(Grammar(), @"//[^\r\n]*", "");
        var m = Regex.Match(text, @"(?m)^" + Regex.Escape(rule) + @"\s*:(?<body>[^;]*);");
        Assert.True(m.Success, $"grammar rule {rule} was not found in CobolExpressions.g4");
        return m.Groups["body"].Value;
    }

    /// <summary>Every spelling (a space-joined token sequence) one alternative admits, expanding each <c>X?</c>.</summary>
    private static IEnumerable<string> Spellings(string alternative)
    {
        IEnumerable<string> acc = [""];
        foreach (string tok in alternative.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            bool optional = tok.EndsWith('?');
            string word = optional ? tok[..^1] : tok;
            acc = acc.SelectMany(prefix => optional
                ? new[] { prefix, (prefix + " " + word).Trim() }
                : [(prefix + " " + word).Trim()]).ToList();
        }
        return acc;
    }

    /// <summary>§8.8.4.2.2 Format 1 as printed, in the grammar's own token names (EQUALS = '=', GT = '>', LT = '<',
    /// NOTEQUAL = '&lt;&gt;', GTEQUAL = '&gt;=', LTEQUAL = '&lt;='). The brace group's eleven alternatives, with the
    /// [NOT] bracket on the first six only.</summary>
    private static readonly string[] Printed =
    [
        "IS? NOT? GREATER THAN?", "IS? NOT? GT",
        "IS? NOT? LESS THAN?", "IS? NOT? LT",
        "IS? NOT? EQUAL TO?", "IS? NOT? EQUALS",
        "IS? NOTEQUAL",
        "IS? GREATER THAN? OR EQUAL TO?", "IS? GTEQUAL",
        "IS? LESS THAN? OR EQUAL TO?", "IS? LTEQUAL",
    ];

    [Fact]
    public void ComparisonOperator_IsTheSpellingsOfThePrintedFormat1_AndNoOther()
    {
        var grammar = RuleBody("comparisonOperator").Split('|')
            .SelectMany(Spellings).ToHashSet(StringComparer.Ordinal);
        var printed = Printed.SelectMany(Spellings).ToHashSet(StringComparer.Ordinal);
        Assert.True(printed.Count > 30, "the printed-set expansion is implausibly small — the test measured nothing");

        var extra = grammar.Except(printed).Order(StringComparer.Ordinal).ToList();
        var missing = printed.Except(grammar).Order(StringComparer.Ordinal).ToList();
        Assert.True(extra.Count == 0,
            "comparisonOperator admits spellings ISO §8.8.4.2.2 Format 1 does not print: " + string.Join(" | ", extra)
            + "\nThe optional [NOT] is bracketed on GREATER THAN, >, LESS THAN, <, EQUAL TO and = only, and EQUAL's "
            + "optional word is TO, never THAN.");
        Assert.True(missing.Count == 0,
            "comparisonOperator lacks printed spellings: " + string.Join(" | ", missing));
    }

    /// <summary>The compile-time relation (§7.3.8.2 SR1 a)) is "formed according to the rules in 8.8.4.2", so it
    /// takes comparisonOperator as it stands: a free <c>IS? NOT?</c> written before the operator is a second,
    /// unscreened NOT (`&gt;&gt;IF X NOT &gt;= 3`).</summary>
    [Fact]
    public void TheDirectiveRelation_WritesNoNotOfItsOwnBeforeTheOperator()
    {
        string cce = RuleBody("cceRelationOrBoolean");
        Assert.Contains("comparisonOperator", cce);
        Assert.DoesNotMatch(@"NOT\s*\?\s*comparisonOperator", cce);
    }

    /// <summary>The expansion's failure branch, fired once: the pre-PB1034 alternatives add spellings.</summary>
    [Fact]
    public void TheCheck_ActuallyFails_OnThePb1034Superset()
    {
        var printed = Printed.SelectMany(Spellings).ToHashSet(StringComparer.Ordinal);
        string[] preAlternatives = ["IS? NOT GTEQUAL", "IS? NOT GREATER THAN? OR EQUAL TO?", "IS? EQUAL THAN"];
        var extra = preAlternatives.SelectMany(Spellings).Except(printed).ToList();
        Assert.Contains("NOT GTEQUAL", extra);
        Assert.Contains("NOT GREATER OR EQUAL", extra);
        Assert.Contains("EQUAL THAN", extra);
    }
}
