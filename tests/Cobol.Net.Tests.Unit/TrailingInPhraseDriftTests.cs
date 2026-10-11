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
/// ⛔ A TRAILING <c>IN word</c> AFTER AN OPERAND THAT CAN BE AN IDENTIFIER IS DECIDED BY SYMBOL, AND THIS TEST
/// IS WHY EVERY SUCH PHRASE IS (kb/Work PB843).
/// <para><c>IN</c> is also the qualification connective (<c>qualification : (OF | IN) cobolWord</c>), and the
/// parser's greedy <c>dataReferenceSuffix*</c> loop takes the word as a qualifier before any enclosing rule's
/// optional <c>(IN cobolWord)?</c> is tried. So over an identifier the enclosing phrase is NEVER parsed as such:
/// §14.9.13.2's range-expression <c>WHEN WS-LO THRU WS-HI IN AL</c> was COBOLNET1639 on 'WS-HI IN AL' for as long
/// as the phrase existed. ISO §8.3.2.2 makes the word's TYPE decide it ("a given user-defined word may be used as
/// only one type of user-defined word"), so the binder that owns the rule must re-read the last qualifier by
/// symbol — <c>EvaluateBinder.BindRangeHigh</c> + <c>ReferenceResolver.WithoutTrailingSuffix</c>.</para>
/// <para>This test scans EVERY grammar rule for an operand that can end in a data reference followed by an
/// <c>IN cobolWord</c> phrase, and requires each to be a rule whose symbol-aware consumer is named here. A new
/// rule of that shape fails until its binder re-reads the qualifier — the silent parse this note spent its life
/// in cannot recur unannounced.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class TrailingInPhraseDriftTests
{
    /// <summary>Rule → the binder file and member that re-read its trailing qualifier by symbol.</summary>
    private static readonly Dictionary<string, (string File, string Member)> Consumers = new(StringComparer.Ordinal)
    {
        ["valueRange"] = (Path.Combine("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "EvaluateBinder.cs"),
            "BindRangeHigh"),
        // §13.18.63.2 formats 3 and 5 print the SAME `[ IN alphabet-name-1 ]` phrase after the literal list, and a
        // constant-name (§13.10.3 SR2) is an identifier-ending operand there too (kb/Work PB983).
        ["valueClause"] = (Path.Combine("Cobol.Net.Compiler", "Binding", "DataBinder.cs"), "RangeAlphabetPhraseOf"),
    };

    /// <summary>Operand rules whose parse can END in a <c>dataReference</c> (and so in a greedy qualifier).</summary>
    private const string IdentifierEndingOperand =
        @"(?:valueOperand|valueClauseOperand|valueItem|dataReference|arithmeticExpression|comparisonOperand|identifier)";

    private static IReadOnlyList<string> RulesWithTrailingInAfterIdentifier()
    {
        var hits = new List<string>();
        string grammarRoot = TestRepo.Src(Path.Combine("Cobol.Net.Frontend", "Grammar"));
        foreach (string file in Directory.EnumerateFiles(grammarRoot, "*.g4", SearchOption.AllDirectories))
        {
            string text = Regex.Replace(File.ReadAllText(file), @"//[^\r\n]*", "");
            foreach (string chunk in text.Split(';'))
            {
                var m = Regex.Match(chunk, @"\A\s*(?<name>[a-z]\w*)\s*:(?<body>.*)\z", RegexOptions.Singleline);
                if (!m.Success) continue;
                if (Regex.IsMatch(m.Groups["body"].Value,
                        IdentifierEndingOperand + @"[\s)*?]*\(?\s*IN\??\s+cobolWord"))
                    hits.Add(m.Groups["name"].Value);
            }
        }
        return hits;
    }

    [Fact]
    public void EveryTrailingInPhraseAfterAnIdentifier_HasASymbolAwareConsumer()
    {
        var rules = RulesWithTrailingInAfterIdentifier();
        // A run must assert its population (feedback_verdict_evidence_invariant): valueRange IS such a rule, so
        // an empty scan means the scan broke, not that the grammar is clean.
        Assert.Contains("valueRange", rules);
        Assert.Contains("valueClause", rules);
        var unowned = rules.Where(r => !Consumers.ContainsKey(r)).ToList();
        Assert.True(unowned.Count == 0,
            $"grammar rule(s) {string.Join(", ", unowned)} follow an identifier-ending operand with `IN cobolWord`. "
            + "The greedy qualification loop takes that word as a QUALIFIER, so the phrase is never parsed over an "
            + "identifier. Re-read the last qualifier by symbol in the rule's binder (the EvaluateBinder."
            + "BindRangeHigh shape, over ReferenceResolver.WithoutTrailingSuffix) and name it in Consumers "
            + "(kb/Work PB843; ISO §8.3.2.2).");
        foreach (var (rule, (file, member)) in Consumers)
        {
            string src = File.ReadAllText(TestRepo.Src(file));
            Assert.True(src.Contains(member + "(", StringComparison.Ordinal)
                    && src.Contains("WithoutTrailingSuffix(", StringComparison.Ordinal),
                $"{rule}'s consumer {member} no longer re-reads the trailing qualifier through "
                + "ReferenceResolver.WithoutTrailingSuffix in " + file);
        }
    }

    /// <summary>⛔ `IN` IS AN OPTIONAL WORD IN ALL THREE FORMATS THAT PRINT `[ IN alphabet-name-1 ]` — §14.9.13.2's
    /// range-expression and §13.18.63.2 formats 3 and 5 (none underlines it; §5.2.3) — and this pins the two
    /// places that honour it together (kb/Work PB983): the EVALUATE rule admits the word with IN omitted, where
    /// position alone decides it, and the VALUE clause's binder peels an IN-less LAST operand that names an
    /// alphabet, because there only the symbol can. Either half regressing re-opens the rejection of legal
    /// source on one of the two clauses §14.7.8 governs in one sentence.</summary>
    [Fact]
    public void RangeAlphabetPhrase_InIsOptional_InEveryFormatThatPrintsIt()
    {
        string grammar = Regex.Replace(File.ReadAllText(TestRepo.Src(Path.Combine("Cobol.Net.Frontend", "Grammar",
            "Core", "CobolExpressions.g4"))), @"//[^\r\n]*", "");
        Assert.Matches(new Regex(@"\bvalueRange\s*:[^;]*\(\s*IN\?\s+cobolWord\s*\)\?"), grammar);
        string binder = File.ReadAllText(TestRepo.Src(Path.Combine("Cobol.Net.Compiler", "Binding", "DataBinder.cs")));
        int at = binder.IndexOf("RangeAlphabetPhraseOf(Core.ValueClauseContext", StringComparison.Ordinal);
        Assert.True(at >= 0, "DataBinder.RangeAlphabetPhraseOf — the VALUE clause's IN-omitted peel — is gone");
        string body = binder.Substring(at, Math.Min(2500, binder.Length - at));
        Assert.Contains("IsAlphabetName(", body);
        Assert.Contains("value.IN() is not null", body);
    }
}
