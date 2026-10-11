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
/// ⛔ THE FALSELY-PERMISSIVE TWIN OF THE OCR'S FALSELY-RESTRICTIVE BIAS, RE-DERIVED FROM THE TRANSCRIPTION.
///
/// <para>ISO §5.2.6.2 makes BRACKETS the only convention that lets a portion of a general format be omitted, and
/// §5.2.2 makes an underlined keyword required subject to those conventions. So a conditional phrase printed on
/// its own line with NO brackets is MANDATORY, and a grammar that writes it <c>phrase?</c> silently UNDER-REJECTS
/// — the mirror of the audit that hunts optional words we wrongly require
/// (<c>scripts/spec/audit_grammar_optional_words.py</c>), and the defect kb/Work PB350 reports: RETURN's AT END
/// was optional in the grammar, so a RETURN with no AT END compiled at every edition and, at end of data, control
/// fell THROUGH the statement onto a record area §14.9.34.4 GR3 leaves undefined.</para>
///
/// <para>ONE such line is not a pattern worth a hand-maintained list (CLAUDE.md rule 8) — so this SCRAPES them
/// out of <c>specs/ISO_COBOL.md</c> instead, and asserts that every one is adjudicated WITH THE MECHANISM THAT
/// ENFORCES IT, and that each adjudication's stated mechanism is still there. A transcription repair, a new
/// edition's format, or a grammar restructure that moves the enforcement point fails here.</para>
///
/// <para>⚠ The scrape asserts its own SHAPE before comparing — the block count, that RETURN's unbracketed line IS
/// found, and that READ's BRACKETED one is NOT — so a regex that stopped matching anything fails loudly instead
/// of passing vacuously (feedback_green_gates_arent_evidence).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class RequiredFormatPhraseDriftTests
{
    /// <summary>The words that lead a CONDITIONAL PHRASE in the statement general formats. This is the family the
    /// compiler models with optional phrase rules, and therefore the family where an unbracketed line is a
    /// under-rejection risk; every other letter-initial continuation line (GIVING, REMAINDER, USING, INTO …) is a
    /// required OPERAND phrase the statement's own rule already requires positionally.</summary>
    private static readonly HashSet<string> ConditionalLeadWords =
        new(StringComparer.Ordinal) { "AT", "NOT", "INVALID", "ON", "WHEN" };

    /// <summary>Every unbracketed conditional-phrase line in a §14.9 general format, and the ONE mechanism that
    /// makes it mandatory in this compiler. Adding a row is an adjudication: name the enforcement point, and add
    /// its re-derivation to <see cref="EachAdjudicatedMechanism_IsStillInPlace"/> — an adjudication no test
    /// re-derives is a dead lookup (feedback_a_dead_lookup_is_also_unverified).</summary>
    private static readonly Dictionary<string, string> Adjudicated = new(StringComparer.Ordinal)
    {
        ["14.9.34.2|AT"] = "RETURN's AT END — bind-time COBOLNET1850 via "
            + "StatementValidation.ScreenOmittedRequiredPhrase from SortBinder.BindReturn (kb/Work PB350); the "
            + "grammar keeps `returnAtEndPhrase?` on purpose so the diagnostic can name the rule",
        ["14.9.37.2|WHEN"] = "SEARCH ALL's WHEN — already required BY THE GRAMMAR: "
            + "`searchAllWhenClause+` (and `searchWhenClause+` for Format 1), so no bind screen is needed",
    };

    /// <summary>(clause, lead word, the printed line) for every unbracketed conditional-phrase line in a §14.9
    /// general format, plus the number of format blocks scanned.</summary>
    private static (List<(string Clause, string Lead, string Line)> Hits, int Blocks) ScrapeUnbracketedPhraseLines()
    {
        string[] lines = File.ReadAllText(TestRepo.Specs("ISO_COBOL.md")).Split('\n');
        var hits = new List<(string, string, string)>();
        string? clause = null;
        bool inPre = false;
        int blocks = 0;

        foreach (string raw in lines)
        {
            string line = raw.TrimEnd('\r');
            var head = Regex.Match(line, @"^#+\s+(\d+(?:\.\d+)*)\s+General formats?\s*$");
            if (head.Success) { clause = head.Groups[1].Value; continue; }
            if (clause is null || !clause.StartsWith("14.9.", StringComparison.Ordinal)) continue;
            if (line.StartsWith("<pre", StringComparison.Ordinal)) { inPre = true; continue; }
            if (inPre && line.StartsWith("</pre>", StringComparison.Ordinal)) { inPre = false; blocks++; continue; }
            if (!inPre) continue;

            // Strip the underline markup, then keep only lines whose FIRST character is a letter: a bracket,
            // brace, choice bar or box-drawing rule at the left edge means the line is enclosed, and §5.2.6.2 /
            // §5.2.6.4 make an enclosed portion omissible.
            string text = Regex.Replace(line, "</?u>", "").Trim();
            if (text.Length == 0 || !char.IsLetter(text[0])) continue;
            string lead = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].TrimEnd('.', ',');
            if (ConditionalLeadWords.Contains(lead)) hits.Add((clause, lead, text));
        }

        // Shape before content. 114 blocks were measured on 2026-09-05; a large drop means the scrape lost the
        // format blocks and every assertion below would pass on an empty set.
        Assert.True(blocks >= 100, $"scraped only {blocks} §14.9 general-format blocks — the scrape lost the spec");
        return (hits, blocks);
    }

    [Fact]   // Every unbracketed conditional phrase in a statement format is adjudicated with its mechanism.
    public void EveryUnbracketedConditionalPhrase_IsScreenedOrAdjudicated()
    {
        var (hits, _) = ScrapeUnbracketedPhraseLines();
        var unadjudicated = hits.Where(h => !Adjudicated.ContainsKey($"{h.Clause}|{h.Lead}")).ToList();
        Assert.True(unadjudicated.Count == 0,
            "a §14.9 general format prints a conditional phrase with NO brackets, and nothing in this compiler "
            + "makes it mandatory. ISO §5.2.6.2 makes brackets the only convention that permits omission, so the "
            + "phrase is required: screen it (StatementValidation.ScreenOmittedRequiredPhrase) or require it in "
            + "the grammar, then adjudicate it here with the mechanism:\n  "
            + string.Join("\n  ", unadjudicated.Select(h => $"§{h.Clause} — \"{h.Line}\"")));
    }

    [Fact]   // The negative control: the scrape really does tell bracketed from unbracketed.
    public void TheScrape_SeesReturnsUnbracketedLine_AndNotReadsBracketedOne()
    {
        var (hits, _) = ScrapeUnbracketedPhraseLines();

        // §14.9.34.2 prints `AT END imperative-statement-1` bare, between a bracketed [ NOT AT END … ] and a
        // bracketed [ END-RETURN ] — the line kb/Work PB350 is about.
        Assert.Contains(hits, h => h.Clause == "14.9.34.2" && h.Line.StartsWith("AT END imperative-statement-1",
            StringComparison.Ordinal));

        // §14.9.30.2 Format 1 prints READ's AT END / NOT AT END pair inside BRACKETS WITH CHOICE INDICATORS,
        // which §5.2.6.4 reads as "zero or more of the alternatives" — so READ's phrase really is optional and
        // must NOT be reported. A scrape that could not tell the two pages apart would fail here, and that
        // asymmetry is exactly what `readAtEnd` depends on.
        Assert.DoesNotContain(hits, h => h.Clause == "14.9.30.2");

        // Exactly the adjudicated rows, no more: a new one is a decision, never a silent pass.
        Assert.Equal(Adjudicated.Keys.OrderBy(k => k, StringComparer.Ordinal),
            hits.Select(h => $"{h.Clause}|{h.Lead}").Distinct().OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]   // Each adjudication names a mechanism; re-derive that the mechanism is still where it says.
    public void EachAdjudicatedMechanism_IsStillInPlace()
    {
        string io = File.ReadAllText(TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolIO.g4"));
        string flow = File.ReadAllText(TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolControlFlow.g4"));
        string binder = File.ReadAllText(TestRepo.Src(
            "Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "SortBinder.cs"));

        // §14.9.34.2 — the grammar deliberately keeps the phrase OPTIONAL so the binder can name the rule …
        // (comments stripped first: the rule's own comment block NAMES `returnAtEndPhrase?` to explain why.)
        Assert.Matches(new Regex(@"returnStatement\s*:.*?returnAtEndPhrase\?", RegexOptions.Singleline),
            Regex.Replace(io, @"//[^\r\n]*", ""));
        // … and the binder screen is the enforcement point, testing the BLOCK (which covers the reversed
        // NOT-only arm too) rather than the phrase node (which would not).
        Assert.Contains("ScreenOmittedRequiredPhrase(atEnd is null", binder, StringComparison.Ordinal);

        // §14.9.37.2 — SEARCH's WHEN needs no screen because the grammar already requires one or more.
        Assert.Matches(@"searchWhenClause\+", flow);
        Assert.Matches(@"searchAllWhenClause\+", flow);
    }
}
