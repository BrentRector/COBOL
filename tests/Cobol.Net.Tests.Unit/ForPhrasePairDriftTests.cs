// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY REPEATED FOR ALPHANUMERIC / FOR NATIONAL PHRASE IS READ BY THE ONE CHOICE-INDICATOR READER (kb/Work PB1075).
/// <para>Five general formats print the same brace with choice indicators, <c>{ | FOR ALPHANUMERIC IS x-1 | FOR
/// NATIONAL IS x-2 | }</c> (ISO §5.2.6.4: "any single alternative may be specified only once"), and the grammar parses
/// each as a <c>xxxForPhrase+</c> superset. Each binder used to walk its own array: four wrote their own "specified
/// more than once" message and the file-level COLLATING SEQUENCE clause let the LAST repeat win in silence. The rule
/// now lives in <c>ChoiceIndicators.ForPhrasePair</c>; this test keeps the NEXT clause automatic by deriving the
/// repeated FOR-phrase rules from the grammar and requiring every binder read of one to be an argument of a
/// <c>ChoiceIndicators</c> call.</para>
/// </summary>
public sealed class ForPhrasePairDriftTests
{
    private static readonly Regex RepeatedForPhrase = new(@"\b(\w+ForPhrase)\s*\+", RegexOptions.Compiled);

    /// <summary>The FOR-phrase rules some grammar rule repeats with <c>+</c> — derived, never listed by hand.</summary>
    private static SortedSet<string> RepeatedForPhraseRules()
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var g4 in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4",
                     SearchOption.AllDirectories))
            foreach (Match m in RepeatedForPhrase.Matches(File.ReadAllText(g4)))
                names.Add(m.Groups[1].Value);
        return names;
    }

    /// <summary>⛔ PROVE THE SCANNER FINDS WHAT IT GUARDS before its absence of findings is trusted: the grammar
    /// carries the three repeated FOR-phrase rules the five formats use.</summary>
    [Fact]
    public void TheGrammarScan_FindsTheRepeatedForPhrases()
    {
        var rules = RepeatedForPhraseRules();
        Assert.Contains("collatingForPhrase", rules);
        Assert.Contains("codeSetForPhrase", rules);
        Assert.Contains("classificationForPhrase", rules);
    }

    /// <summary>Every binder read of a repeated FOR-phrase array is on a line that calls <c>ChoiceIndicators</c>, and
    /// every such rule is read at least once (a rule nobody reads would make the zero vacuous).</summary>
    [Fact]
    public void EveryBinderRead_GoesThroughChoiceIndicators()
    {
        var reads = RepeatedForPhraseRules().ToDictionary(r => r, _ => 0);
        var offenders = new List<string>();
        foreach (var cs in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler", "Binding"), "*.cs",
                     SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(cs);
            for (int i = 0; i < lines.Length; i++)
                foreach (var rule in reads.Keys.ToList())
                {
                    if (!lines[i].Contains($".{rule}()", StringComparison.Ordinal)) continue;
                    reads[rule]++;
                    if (!lines[i].Contains("ChoiceIndicators.", StringComparison.Ordinal))
                        offenders.Add($"{Path.GetFileName(cs)}:{i + 1}: {lines[i].Trim()}");
                }
        }
        Assert.True(offenders.Count == 0,
            "a binder walks a repeated FOR ALPHANUMERIC / FOR NATIONAL phrase array itself — pass it to "
            + "ChoiceIndicators.ForPhrasePair / AlphabetPair, the one reader of §5.2.6.4's at-most-once rule "
            + "(kb/Work PB1075):\n" + string.Join("\n", offenders));
        Assert.All(reads, kv => Assert.True(kv.Value > 0, $"no binder reads '{kv.Key}' — re-derive this guard"));
    }
}
