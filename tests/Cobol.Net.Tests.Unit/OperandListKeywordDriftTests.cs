// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ WHICH READING WINS WHEN A WORD COULD BE EITHER AN OPERAND OR THE NEXT KEYWORD (kb/Work PB805).
/// <para>A greedy operand list whose items can be a <c>cobolWord</c> will absorb any keyword token that
/// <c>cobolWord</c> admits, and the construct that keyword begins vanishes. The migration mode admits every word
/// §8.9 took away from an older edition (<c>ConstructAvailability.Removed</c>), so under <c>--permissive</c>
/// <c>01 G VALUE N"AB" GROUP-USAGE NATIONAL.</c> lost its §13.18.29 GROUP-USAGE clause to the VALUE list while
/// the strict compile accepted it. The answer used to be written per word and per list — PROPERTY in two VALUE
/// loops, DEFAULT in INITIALIZE, five phrase words in DELETE FILE — and left to ALL(*) lookahead luck for the
/// rest. It is now ONE generated predicate, <c>{!keywordContinuesHere()}?</c> on every keyword-token
/// alternative of <c>cobolWord</c>, whose follow set is computed from the ATN (CobolParserCoreBase).</para>
/// <para>These tests are DERIVED: the words from <c>reserved-words.json</c>, the clause-initial tokens from the
/// parser's own ATN, the predicate's presence from the generated grammar — so the next restored word, the next
/// clause and the next list are covered with no edit here (CLAUDE.md rule 5).</para>
/// </summary>
public sealed class OperandListKeywordDriftTests
{
    private static readonly int[] Editions = [85, 2002, 2014, 2023];

    /// <summary>Every keyword-token <c>cobolWord</c> alternative carries the predicate, and IDENTIFIER does not.
    /// A keyword alternative without it is a word some operand list can swallow again.</summary>
    [Fact]
    public void EveryKeywordAlternativeOfCobolWord_CarriesTheListEndPredicate()
    {
        string path = TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolWords.g4");
        int keywordAlts = 0;
        var bare = new List<string>();
        bool inRule = false;
        foreach (var raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line == "cobolWord") { inRule = true; continue; }
            if (!inRule) continue;
            if (line == ";") break;
            var m = Regex.Match(line, @"^[:|]\s*(\{[^}]*\}\?)?\s*([A-Z][A-Z0-9_]*)\s*$");
            if (!m.Success || m.Groups[2].Value == "IDENTIFIER") continue;
            keywordAlts++;
            if (!m.Groups[1].Value.Contains("!keywordContinuesHere()", StringComparison.Ordinal)) bare.Add(m.Groups[2].Value);
        }
        Assert.True(keywordAlts > 40, $"only {keywordAlts} keyword alternatives read — the parse of {path} broke");
        Assert.True(bare.Count == 0,
            $"{bare.Count} cobolWord keyword alternative(s) lack {{!keywordContinuesHere()}}?: [{string.Join(",", bare.Take(8))}] "
            + "— re-run scripts/gen-cobol-words.ps1 (kb/Work PB805)");
    }

    /// <summary>No hand-written lookahead predicate survives in the grammar: <c>TokenStream.LA(</c> in a
    /// <c>.g4</c> predicate is the per-word list-guard family this mechanism retired, and a new one would be the
    /// second mechanism for the same job (feedback_one_mechanism_per_job).</summary>
    [Fact]
    public void NoHandWrittenLookaheadListGuard_InTheGrammar()
    {
        string dir = TestRepo.Src("Cobol.Net.Frontend", "Grammar");
        var hits = Directory.EnumerateFiles(dir, "*.g4", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).Contains("Lexer", StringComparison.Ordinal))
            .SelectMany(f => File.ReadLines(f).Select((l, i) => (f, i, l)))
            .Where(x => { string code = x.l.Split("//")[0]; return code.Contains("TokenStream.LA(", StringComparison.Ordinal); })
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}").ToList();
        Assert.True(hits.Count == 0, $"hand-written lookahead predicate(s) in parser grammar: [{string.Join(", ", hits)}] "
            + "— end an operand list through keywordContinuesHere(), never a per-word guard (kb/Work PB805)");
    }

    /// <summary>⛔ THE GROUP-USAGE REGRESSION, over its whole derived population. For every word §8.9 reserves at
    /// edition E and left free at an older one (the migration mode restores it at E) whose token can BEGIN a data
    /// description clause (the FIRST set of <c>dataDescriptionClause</c>, read off the ATN), a VALUE clause
    /// followed by that word must keep exactly ONE operand under <c>--permissive</c> — the word opens the next
    /// clause and is never a second VALUE operand. The strict compile is asserted too (the gate already keeps the
    /// word out of <c>cobolWord</c> there), so a regression on either axis fails here.</summary>
    [Fact]
    public void RestoredWord_ThatOpensADataClause_IsNeverAbsorbedByTheValueList()
    {
        var reserved = CobolWordsDriftTests.LoadReservedIntervals();
        var gate = CobolWordsDriftTests.DerivedGateSet();
        var probe = NewParser("01 G.", EditionInfo.Of(2023));
        var atn = probe.Atn;
        var clauseFirst = atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_dataDescriptionClause]);

        // Control: an ordinary two-operand entry still reads two operands (the probe measures something).
        Assert.Equal(2, ValueOperandCount("01 G VALUE \"AB\" X.", EditionInfo.Of(2023, permissive: true)));

        var failures = new List<string>();
        int pairs = 0;
        bool sawGroupUsage = false;
        foreach (string token in gate.OrderBy(t => t, StringComparer.Ordinal))
        {
            int type = CobolLexer.DefaultVocabulary is var v
                ? Enumerable.Range(1, ((Antlr4.Runtime.Vocabulary)v).getMaxTokenType()).FirstOrDefault(t => v.GetSymbolicName(t) == token) : 0;
            if (type == 0 || !clauseFirst.Contains(type)) continue;
            string word = CobolWordsDriftTests.ToWord(token);
            if (!reserved.TryGetValue(word, out var flags)) continue;
            for (int i = 1; i < Editions.Length; i++)
            {
                if (!flags[i] || !flags[..i].Contains(false)) continue;   // restored only where §8.9 took it away
                pairs++;
                sawGroupUsage |= word == "GROUP-USAGE";
                foreach (bool permissive in (bool[])[false, true])
                {
                    int n = ValueOperandCount($"01 G VALUE \"AB\" {word} .", EditionInfo.Of(Editions[i], permissive));
                    if (n != 1 && failures.Count < 8)
                        failures.Add($"{word}@{Editions[i]}{(permissive ? " --permissive" : "")}: {n} VALUE operands");
                }
            }
        }
        Assert.True(pairs > 0 && sawGroupUsage, $"the derivation found {pairs} pairs and GROUP-USAGE={sawGroupUsage} — it broke");
        Assert.True(failures.Count == 0,
            $"a VALUE operand list absorbed a clause-opening word (kb/Work PB805; §13.16.3 4) clauses in any order): "
            + string.Join(" | ", failures));
    }

    private static CobolParserCore NewParser(string text, EditionInfo edition)
    {
        var lexer = new CobolLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        var parser = new CobolParserCore(new CommonTokenStream(lexer)) { Edition = edition };
        parser.RemoveErrorListeners();
        return parser;
    }

    /// <summary>The number of operands the first VALUE clause of a single data description entry parsed with
    /// (a syntax error after the clause is expected and ignored — only the list's extent is measured).</summary>
    private static int ValueOperandCount(string entry, EditionInfo edition)
    {
        var tree = NewParser(entry, edition).dataDescriptionEntry();
        var value = Descendants(tree).OfType<CobolParserCore.ValueClauseContext>().FirstOrDefault();
        return value is null ? -1 : Descendants(value).OfType<CobolParserCore.ValueClauseOperandContext>().Count();
    }

    private static IEnumerable<IParseTree> Descendants(IParseTree t)
    {
        yield return t;
        for (int i = 0; i < t.ChildCount; i++)
            foreach (var d in Descendants(t.GetChild(i))) yield return d;
    }
}
