// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using CobolNet.Validation;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD FOR <c>formatWord</c> (kb/Work PB764) — "a keyword slot may not borrow <c>cobolWord</c>",
/// measured instead of remembered.
///
/// <para>ISO §5.2.2 / §5.2.3: a keyword or optional word standing where its general format prints it is a USE of
/// the word, outside §8.3.2.1 rule 1 ("Reserved words shall not be used as user-defined words or system-names").
/// A §8.9-reserved word with NO lexer token arrives as IDENTIFIER, and the §8.9 funnel
/// (<c>VersionConformancePass.VisitCobolWord</c>) screens every IDENTIFIER <c>cobolWord</c> position-blind — so a
/// grammar rule that matches such a keyword through <c>cobolWord</c> draws a FALSE COBOLNET0901 at every edition
/// that reserves it (PB704: <c>SORT … WITH DUPLICATES IN ORDER</c>). The funnel used to paper over that with a
/// LADDER of per-slot exemptions, one of which was always missing. The keyword slots now parse as
/// <c>formatWord</c> (IDENTIFIER, never <c>cobolWord</c>), and the ladder is gone.</para>
///
/// <para>WHAT THIS PINS, derived from three sources and none of them a hand list: the WORDS are every §8.9-reserved
/// word (<c>reserved-words.json</c>) the compiled lexer has no token for; the POSITIONS are every one the
/// positive conformance corpus actually writes, parsed by the real frontend at the golden's own edition; the
/// ALLOWANCES are the funnel's NON-positional reasons (a bare §15 function-argument phrase word, a construct
/// declined whole, a predefined register) — each owned by its own code. So a new general format that spells a
/// keyword through <c>cobolWord</c> fails HERE even if someone re-grows a ladder arm to silence its golden.</para>
/// </summary>
public sealed class FormatWordDriftTests
{
    private static readonly string[] Editions = ["85", "2002", "2014", "2023"];
    private static readonly Regex Literal = new("\"[^\"]*\"|'[^']*'", RegexOptions.CultureInvariant);

    /// <summary>§8.9-reserved (at some edition) spellings the lexer never tokenizes — they can only ever arrive as
    /// IDENTIFIER, which is exactly the population the funnel screens position-blind.</summary>
    private static HashSet<string> TokenlessReservedWords()
    {
        // CobolKeywordTokens.TryTokenType runs the pipeline's own lexer over the word: false = it lexes as IDENTIFIER.
        return CobolWordsDriftTests.LoadReservedIntervals()
            .Where(kv => kv.Value.Any(r => r) && !CobolNet.Frontend.Parsing.CobolKeywordTokens.TryTokenType(kv.Key, out _))
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IEnumerable<T> Descendants<T>(IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T t) yield return t;
            foreach (var d in Descendants<T>(child)) yield return d;
        }
    }

    /// <summary>The funnel's NON-positional allowances — deliberately restated here rather than called, so a new
    /// exemption must be a named decision in two places (the whole point of retiring the ladder).</summary>
    private static bool IsNonPositionalUse(CobolParserCore.CobolWordContext w, string word)
    {
        if (VersionConformancePass.IsBareFunctionArgumentWord(w)) return true;          // §15 phrase word
        if (DeclinedFacilityPass.EnclosingDeclinedConstruct(w) is not null) return true; // refused whole
        if (word == "EXCEPTION-OBJECT")                                                  // §8.4.3.6 register
            for (Antlr4.Runtime.RuleContext? a = w.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.StatementContext) return true;
        return word.StartsWith("DEBUG-", StringComparison.Ordinal);                     // '85 debug registers
    }

    /// <summary>Violations in one parsed program: every IDENTIFIER <c>cobolWord</c> whose spelling is a tokenless
    /// §8.9-reserved word the program's edition reserves, and that is not a non-positional use.</summary>
    private static IEnumerable<string> Violations(CobolParserCore.CompilationUnitContext tree, CnFrontend fe,
        int edition, HashSet<string> words, string label)
    {
        var reserved = ReservedWordSet.Compose(fe.Directives.CobolWordsMap);
        foreach (var w in Descendants<CobolParserCore.CobolWordContext>(tree))
        {
            if (w.Start.Type != CobolLexer.IDENTIFIER) continue;
            string word = w.Start.Text.ToUpperInvariant();
            if (!words.Contains(word) || !reserved.RejectsAt(word, edition) || IsNonPositionalUse(w, word)) continue;
            string parent = w.Parent is { } p ? CobolParserCore.ruleNames[p.RuleIndex] : "?";
            yield return $"{label}:{w.Start.Line} '{word}' in {parent}";
        }
    }

    private static (CobolParserCore.CompilationUnitContext? Tree, CnFrontend Fe) Parse(string path, int edition)
    {
        var fe = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = edition };
        fe.AddCopySearchPath(Path.GetDirectoryName(path)!);
        return (fe.Parse(path, new DiagnosticBag()), fe);
    }

    private static List<string> EnabledPositives(string edition)
    {
        string manifest = TestRepo.Tests("conformance", edition, "manifest.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
        return doc.RootElement.GetProperty("enabled").EnumerateArray().Select(e => e.GetString()!).ToList();
    }

    /// <summary>⛔ THE OBLIGATION. In every ENABLED positive golden that writes a tokenless §8.9-reserved word, each
    /// occurrence at an edition that reserves it is a <c>formatWord</c> (a keyword) — never a <c>cobolWord</c> —
    /// unless it is a non-positional use. Plus the population assertions that keep it from passing vacuously
    /// (feedback_verdict_evidence_invariant): the word set holds the known keywords, the corpus probe parsed real
    /// programs, and those programs DO carry formatWord keywords of every family PB764 moved.</summary>
    [Fact]
    public void TextKeywords_NeverBorrowCobolWord_AcrossTheCorpus()
    {
        var words = TokenlessReservedWords();
        foreach (string known in new[] { "LOCALE", "NESTED", "COBOL", "USER-DEFAULT", "SYSTEM-DEFAULT" })
            Assert.True(words.Contains(known), $"'{known}' is not in the tokenless §8.9 set — the derivation broke "
                + "(or the word gained a lexer token: then its keyword slots are tokens and it leaves this guard)");
        // The prefilter only decides which goldens are worth a parse: the words the edition reserves, in source text
        // with the comments removed (a `*>` tail or a fixed-form column-7 `*` line) — the parse decides the rest.
        var candidates = new List<(string Ed, int Edition, string Name, string Path)>();
        foreach (string ed in Editions)
        {
            int edition = int.Parse(ed);
            var reservedHere = words.Where(w => ReservedWordSet.Default.RejectsAt(w, edition)).ToList();
            var wordPattern = new Regex(@"(?<![A-Z0-9-])(" + string.Join("|", reservedHere.Select(Regex.Escape)) + @")(?![A-Z0-9-])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            foreach (string name in EnabledPositives(ed))
            {
                string path = TestRepo.Tests("conformance", ed, name + ".cob");
                if (!File.Exists(path)) continue;
                string code = string.Join('\n', File.ReadLines(path)
                    .Where(l => !(l.Length > 6 && l[6] is '*' or '/'))
                    .Select(l => l.IndexOf("*>", StringComparison.Ordinal) is int c and >= 0 ? l[..c] : l)
                    .Select(l => Literal.Replace(l, "")));
                if (wordPattern.IsMatch(code)) candidates.Add((ed, edition, name, path));
            }
        }

        var violations = new System.Collections.Concurrent.ConcurrentBag<string>();
        var keywordTexts = new System.Collections.Concurrent.ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        int parsed = 0, keywordCount = 0;
        Parallel.ForEach(candidates, c =>
        {
            var (tree, fe) = Parse(c.Path, c.Edition);
            if (tree is null) return;
            Interlocked.Increment(ref parsed);
            foreach (string v in Violations(tree, fe, c.Edition, words, $"{c.Ed}/{c.Name}")) violations.Add(v);
            foreach (var f in Descendants<CobolParserCore.FormatWordContext>(tree))
            {
                Interlocked.Increment(ref keywordCount);
                keywordTexts.TryAdd(f.GetText(), 0);
            }
        });
        Assert.True(parsed >= 20, $"only {parsed} golden(s) carrying a tokenless reserved word were parsed — the probe is blind");
        Assert.True(keywordCount >= 20, $"only {keywordCount} formatWord keyword(s) in the corpus — the grammar stopped producing them");
        Assert.True(violations.Count == 0,
            $"{violations.Count} general-format keyword(s) borrow cobolWord, which the §8.9 funnel screens as a user-defined "
            + "word (a false COBOLNET0901 — or a ladder exemption hiding one). Route the slot through `formatWord` "
            + "(ISO §5.2.2/§5.2.3; DESIGN-frontend-grammar \"A KEYWORD SLOT MAY NOT BORROW cobolWord\"): "
            + string.Join("; ", violations.Take(12)));
        foreach (string family in new[] { "LOCALE", "NESTED", "USER-DEFAULT", "CLASSIFICATION", "LC_ALL" })
            Assert.True(keywordTexts.ContainsKey(family), $"no golden parses '{family}' as a formatWord — its slot regressed to cobolWord, "
                + "or the corpus lost its witness");
    }

    /// <summary>Prove the guard can FAIL (feedback_green_gates_arent_evidence): a reserved tokenless word in a NAME
    /// slot is exactly what the obligation reports — and a keyword in its formatWord slot is not.</summary>
    [Fact]
    public void TheGuard_ReportsABorrowedWord_AndPassesAKeyword()
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_fw_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path,
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. FWPROBE.\n       DATA DIVISION.\n"
            + "       WORKING-STORAGE SECTION.\n       01 P USAGE POINTER.\n       PROCEDURE DIVISION.\n"
            + "           SET LOCALE LC_ALL TO USER-DEFAULT.\n           DISPLAY LOCALE.\n           STOP RUN.\n");
        try
        {
            var (tree, fe) = Parse(path, 2023);
            Assert.NotNull(tree);
            var found = Violations(tree!, fe, 2023, TokenlessReservedWords(), "probe").ToList();
            Assert.True(found.Count == 1 && found[0].Contains("'LOCALE'", StringComparison.Ordinal),
                "expected exactly the DISPLAY operand LOCALE (a name slot) to be reported, got: " + string.Join("; ", found));
            Assert.Equal(3, Descendants<CobolParserCore.FormatWordContext>(tree!).Count());   // LOCALE LC_ALL USER-DEFAULT
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
