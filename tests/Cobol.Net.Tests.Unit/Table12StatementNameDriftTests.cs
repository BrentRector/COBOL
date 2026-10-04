// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <see cref="Table12StatementNames"/> IS ISO Table 12's 'Statement name' COLUMN, AND THIS RE-DERIVES IT FROM
/// THE SPEC — BOTH DIRECTIONS.
///
/// <para>§15.32.3 r3 makes the column the answer FUNCTION EXCEPTION-STATEMENT must give, and the resolver keys
/// on the parse rule because the spelled-token axis is wrong in a way no token can repair (kb/Work R04:
/// <c>GO PARA.</c> is a GO TO statement whose tokens never contain TO). A resolver nothing re-derives is a
/// hand-maintained list (CLAUDE.md rule 5) and a table nothing re-derives has never been contradicted
/// (feedback_a_dead_lookup_is_also_unverified) — so this scrapes Table 12 out of <c>specs/ISO_COBOL.md</c> and
/// the <c>statement</c> alternatives out of the grammar, feeds every alternative through the SAME
/// <see cref="Table12StatementNames.NameOfRule"/> path the compiler uses, and asserts containment forward
/// (every alternative resolves into the table or the adjudicated non-Table-12 set) and coverage backward
/// (every table row is produced by some alternative — a row with no rule is grammar or transcription drift).
/// A newly added statement rule fails here until it resolves.</para>
///
/// <para>⚠ Both scrapes assert the SHAPE they found before comparing, so a transcription reformat or a grammar
/// restructure fails loudly instead of silently comparing nothing (the vacuous-pass trap).</para>
/// </summary>
public sealed class Table12StatementNameDriftTests
{
    /// <summary>Grammar rules with no row in the 2023 Table 12, each with the reason its projected name is the
    /// adjudicated answer. Adding a name here is an adjudication, not a formality — conforming source cannot
    /// observe any of these under checking (&gt;&gt;TURN is COBOL-2002+ and each is pre-2002, non-procedural,
    /// or binds loud), and the projected name is strictly better than a wrong sibling's name if one escapes.</summary>
    private static readonly Dictionary<string, string> NonTable12 = new(StringComparer.Ordinal)
    {
        ["ALTER"] = "COBOL-85 element deleted from the standard before Table 12's 2023 edition",
        ["ENTRY"] = "a non-ISO extension statement the grammar carries; no Table 12 row at any edition",
        ["ENTER"] = "COBOL-85 obsolete element deleted from the standard before 2023",
        ["USE"] = "a declaratives header the grammar routes through `statement`; not a procedural statement",
        ["NEXT SENTENCE"] = "no 2023 Table 12 row (survives only as the IF/SEARCH-era branch form)",
        ["INLINE METHOD INVOCATION"] = "2023 inline method invocation used as a statement; no Table 12 row",
    };

    /// <summary>Table 12's 'Statement name' column, scraped from the transcription at its anchor.</summary>
    private static List<string> ScrapeTable12()
    {
        string spec = File.ReadAllText(Path.Combine(TestRepo.Root, "specs", "ISO_COBOL.md"));
        int anchor = spec.IndexOf("<a id=\"table-12\">", StringComparison.Ordinal);
        Assert.True(anchor >= 0, "the `table-12` anchor is gone from specs/ISO_COBOL.md — this guard must follow it");

        var rowLines = spec[anchor..].Split('\n').Select(l => l.TrimEnd('\r'))
            .SkipWhile(l => !l.StartsWith('|')).TakeWhile(l => l.StartsWith('|')).ToList();
        // Row 0 = the header, row 1 = the |---| separator, rows 2.. = the data.
        var names = rowLines.Skip(2).Select(l => l.Split('|')[1].Trim().Trim('*').Trim()).ToList();

        // Shape before content: 50 rows ACCEPT…WRITE, GO TO the only multi-word name. A count drift here means
        // the TRANSCRIPTION changed and the resolver's premise must be re-derived, not patched.
        Assert.Equal(50, names.Count);
        Assert.Equal("ACCEPT", names[0]);
        Assert.Equal("WRITE", names[^1]);
        Assert.Equal(["GO TO"], names.Where(n => n.Contains(' ')));
        return names;
    }

    /// <summary>The rule reference of every <c>statement</c> alternative, scraped from the grammar (comments
    /// stripped first — sibling rules are NAMED in the rule's comments). Asserts each alternative is a single
    /// rule reference, the shape <see cref="Table12StatementNames.NameOf"/> depends on.</summary>
    private static List<string> ScrapeStatementAlternatives()
    {
        string grammar = File.ReadAllText(TestRepo.Src(
            Path.Combine("Cobol.Net.Frontend", "Grammar", "CobolParserCore.g4")));
        grammar = Regex.Replace(grammar, @"//[^\r\n]*", "");
        var m = Regex.Match(grammar, @"^statement\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, "the `statement` rule is gone from CobolParserCore.g4 — this guard must follow it");

        var alternatives = new List<string>();
        foreach (string alt in m.Groups["body"].Value.Split('|'))
        {
            var refs = Regex.Matches(alt, @"\b([a-zA-Z]\w*Statement)\b").Select(x => x.Groups[1].Value).ToList();
            Assert.True(refs.Count == 1,
                $"a `statement` alternative is not a single rule reference (found {refs.Count} in \"{alt.Trim()}\") "
                + "— Table12StatementNames.NameOf resolves child 0's rule and must follow this restructure");
            alternatives.Add(refs[0]);
        }
        Assert.True(alternatives.Count >= 55,
            $"scraped only {alternatives.Count} statement alternatives — the scrape lost the rule body");
        return alternatives;
    }

    [Fact]   // Forward: every grammar alternative resolves to a Table 12 name or an adjudicated exemption.
    public void EveryStatementRule_ResolvesToTable12_OrAdjudicatedExemption()
    {
        var table = ScrapeTable12();
        var unadjudicated = ScrapeStatementAlternatives()
            .Select(rule => (rule, name: Table12StatementNames.NameOfRule(rule)))
            .Where(x => !table.Contains(x.name) && !NonTable12.ContainsKey(x.name)).ToList();
        Assert.True(unadjudicated.Count == 0,
            "statement rule(s) resolve to a name that is neither a Table 12 row nor an adjudicated exemption — "
            + "map them in Table12StatementNames or adjudicate them here, with the reason:\n  "
            + string.Join("\n  ", unadjudicated.Select(x => $"{x.rule} → \"{x.name}\"")));
    }

    [Fact]   // Backward: every Table 12 row is produced by at least one grammar alternative.
    public void EveryTable12Row_IsProducedBySomeStatementRule()
    {
        var produced = ScrapeStatementAlternatives().Select(Table12StatementNames.NameOfRule).ToHashSet(StringComparer.Ordinal);
        var orphans = ScrapeTable12().Where(n => !produced.Contains(n)).ToList();
        Assert.True(orphans.Count == 0,
            "Table 12 row(s) with no grammar alternative resolving to them — grammar or transcription drift:\n  "
            + string.Join("\n  ", orphans));
    }

    [Fact]   // The R04 pin: the row that exposed the token axis, resolved through the general mechanism.
    public void GoTo_ResolvesFromTheRule_NeverFromTokens()
        => Assert.Equal("GO TO", Table12StatementNames.NameOfRule("goToStatement"));

    // ── The other two columns: conditional phrase and explicit scope terminator (kb/Work PB351) ─────────────

    /// <summary>Table 12's three columns per row — (statement name, conditional phrase, explicit scope terminator) —
    /// scraped the way <see cref="ScrapeTable12"/> scrapes the first.</summary>
    private static List<(string Name, string Phrase, string Terminator)> ScrapeTable12Rows()
    {
        string spec = File.ReadAllText(Path.Combine(TestRepo.Root, "specs", "ISO_COBOL.md"));
        int anchor = spec.IndexOf("<a id=\"table-12\">", StringComparison.Ordinal);
        Assert.True(anchor >= 0, "the `table-12` anchor is gone from specs/ISO_COBOL.md");
        var rows = spec[anchor..].Split('\n').Select(l => l.TrimEnd('\r'))
            .SkipWhile(l => !l.StartsWith('|')).TakeWhile(l => l.StartsWith('|')).Skip(2)
            .Select(l => l.Split('|'))
            .Select(c => (c[1].Trim(), c[2].Trim(), c[3].Trim())).ToList();
        Assert.Equal(50, rows.Count);
        return rows;
    }

    /// <summary>⛔ THE TERMINATOR COLUMN IS WHAT <see cref="ConditionalStatements"/> ASKS, SO IT IS RE-DERIVED HERE.
    /// §14.5.1 makes a statement conditional when a conditional phrase is written and "its explicit scope terminator"
    /// is not; the classifier finds the terminator as the token <c>END_</c> + the statement name. That holds only if
    /// every terminator Table 12 prints is spelled <c>END-</c> + the row's name and the lexer carries that token, and
    /// only if every row with a conditional phrase HAS a terminator — each asserted from the scraped table, so a
    /// transcription or lexer change that breaks the premise fails here instead of silently classifying a
    /// conditional statement as imperative.</summary>
    [Fact]
    public void EveryTable12Terminator_IsEndNameAndIsTheTokenTheClassifierAsks()
    {
        var rows = ScrapeTable12Rows();
        var withTerminator = rows.Where(r => r.Terminator.Length > 0).ToList();
        Assert.True(withTerminator.Count >= 20, $"only {withTerminator.Count} terminators scraped — the scrape broke");
        foreach (var (name, _, terminator) in withTerminator)
        {
            Assert.Equal("END-" + name, terminator);
            int? token = ConditionalStatements.TerminatorFor(name);
            Assert.True(token is not null, $"no END_ lexer token for Table 12's {terminator}");
            Assert.Equal("END_" + name.Replace(' ', '_'),
                CobolNet.Frontend.Generated.CobolParserCore.DefaultVocabulary.GetSymbolicName(token!.Value));
        }
        var phraseWithoutTerminator = rows.Where(r => r.Phrase.Length > 0 && r.Terminator.Length == 0).ToList();
        Assert.True(phraseWithoutTerminator.Count == 0, "Table 12 row(s) with a conditional phrase but no explicit "
            + "scope terminator: " + string.Join(", ", phraseWithoutTerminator.Select(r => r.Name)));
    }

    /// <summary>⛔ "A CONDITIONAL PHRASE IS WRITTEN" IS READ AS "THE STATEMENT'S OWN SYNTAX HOLDS A statementBlock",
    /// AND THIS HOLDS THAT PROXY SOUND. Every grammar rule from which <c>statementBlock</c> is reachable (without
    /// passing through another <c>statement</c>) must belong to a Table 12 row that HAS a conditional phrase — or be
    /// PERFORM, whose inline imperative-statement-1 is not a conditional phrase but whose inline formats REQUIRE
    /// END-PERFORM, so <see cref="ConditionalStatements.IsConditional"/> can never call it conditional. A new
    /// statement rule that carries an imperative operand without a conditional phrase fails here until the
    /// classifier is taught what it is.</summary>
    [Fact]
    public void OnlyARowWithAConditionalPhrase_OrTheTerminatedPerform_HoldsAStatementBlock()
    {
        var grammarRules = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(
            TestRepo.Src(Path.Combine("Cobol.Net.Frontend", "Grammar")), "*.g4", SearchOption.AllDirectories))
        {
            string text = Regex.Replace(File.ReadAllText(file), @"//[^\r\n]*", "");
            foreach (Match r in Regex.Matches(text, @"^([a-z]\w*)\s*:(?<body>.*?);",
                         RegexOptions.Multiline | RegexOptions.Singleline))
                grammarRules[r.Groups[1].Value] = r.Groups["body"].Value;
        }
        Assert.True(grammarRules.ContainsKey("statementBlock") && grammarRules.Count > 300,
            $"scraped {grammarRules.Count} parser rules — the grammar scrape broke");

        bool Reaches(string rule)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { "statement", "sentence" };
            var work = new Stack<string>([rule]);
            while (work.Count > 0)
            {
                string r = work.Pop();
                if (!seen.Add(r) || !grammarRules.TryGetValue(r, out string? body)) continue;
                foreach (Match m in Regex.Matches(body, @"\b[a-z]\w*\b"))
                {
                    if (m.Value == "statementBlock") return true;
                    work.Push(m.Value);
                }
            }
            return false;
        }

        var phraseRows = ScrapeTable12Rows().Where(r => r.Phrase.Length > 0).Select(r => r.Name)
            .ToHashSet(StringComparer.Ordinal);
        var holders = ScrapeStatementAlternatives().Where(Reaches).ToList();
        Assert.True(holders.Count >= 20, $"only {holders.Count} statement rules reach statementBlock — the walk broke");
        var unexplained = holders.Select(r => (Rule: r, Name: Table12StatementNames.NameOfRule(r)))
            .Where(x => !phraseRows.Contains(x.Name) && x.Name != "PERFORM").ToList();
        Assert.True(unexplained.Count == 0, "statement rule(s) hold an imperative operand with no Table 12 conditional "
            + "phrase: " + string.Join(", ", unexplained.Select(x => $"{x.Rule} → {x.Name}")));

        // PERFORM's exemption is its REQUIRED terminator: no inline alternative may make END-PERFORM optional.
        Assert.DoesNotMatch(@"END_PERFORM\s*\?", grammarRules["performStatement"]);
        Assert.Contains("END_PERFORM", grammarRules["performStatement"], StringComparison.Ordinal);
    }
}
