// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY PARSER RULE NAME IS DEFINED IN EXACTLY ONE GRAMMAR FILE (kb/Work PB2292). <c>CobolParserCore.g4</c> imports
/// the <c>Core/*.g4</c> fragments, and when two imported grammars define the same rule ANTLR keeps the FIRST definition
/// it finds and drops the other without a diagnostic. So a duplicated name is never two rules: it is one rule chosen
/// by the import order. <c>className</c> was defined in <c>CobolExpressions.g4</c> (the class-condition operand) and in
/// <c>CobolOO.g4</c> (the OO class-name), the Expressions definition won, and every OO class-name slot (CLASS-ID,
/// END CLASS, INHERITS, OBJECT REFERENCE) took the class-condition keyword set, so <c>CLASS-ID. NUMERIC.</c> parsed.
/// The architecture review's per-fragment gate map (DESIGN-architecture-review.md §8.3 item 6, "the gate of the
/// fragment that DEFINES its rule") relies on the same fact.
/// </summary>
public sealed class GrammarRuleUniquenessDriftTests
{
    /// <summary>A parser rule definition: a lower-case rule name at the start of a line, followed by its colon on the
    /// same line or alone on the line (the colon then leads the next). Directive lines (<c>parser grammar X;</c>,
    /// <c>options {</c>, <c>import …;</c>) carry more text after the first word and do not match.</summary>
    private static readonly Regex RuleDefinition = new(@"^(?<name>[a-z][A-Za-z0-9_]*)[ \t]*(?::|\r?$)", RegexOptions.Multiline);

    private static IEnumerable<(string File, string Rule)> ParserRuleDefinitions()
    {
        string grammarRoot = TestRepo.Src("Cobol.Net.Frontend", "Grammar");
        foreach (string path in Directory.EnumerateFiles(grammarRoot, "*.g4", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(path);
            if (Regex.IsMatch(text, @"^\s*lexer\s+grammar\b", RegexOptions.Multiline)) continue;
            foreach (Match m in RuleDefinition.Matches(text))
                yield return (Path.GetRelativePath(grammarRoot, path), m.Groups["name"].Value);
        }
    }

    [Fact]
    public void NoParserRuleName_IsDefinedTwice()
    {
        var definitions = ParserRuleDefinitions().ToList();
        Assert.True(definitions.Count > 500,
            $"only {definitions.Count} parser rule definitions found under Grammar/ — the scan is broken, not the grammar");
        Assert.Contains(definitions, d => d.Rule == "classConditionName");
        Assert.Contains(definitions, d => d.Rule == "className");

        var duplicated = definitions.GroupBy(d => d.Rule, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} [{string.Join(", ", g.Select(d => d.File))}]")
            .ToList();
        Assert.True(duplicated.Count == 0,
            "parser rule name(s) defined more than once — ANTLR silently keeps the FIRST imported definition, so the "
            + "other is dead and its callers get the wrong rule (kb/Work PB2292): " + string.Join("; ", duplicated));
    }
}
