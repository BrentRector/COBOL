// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A NAME SLOT IS <c>cobolWord</c>, NEVER A BARE <c>IDENTIFIER</c> (kb/Work PB2721, PB2732) — the converse of
/// <see cref="FormatWordDriftTests"/>' "a keyword slot may not borrow cobolWord".
/// <para>A user-defined word is DECLARED through <c>cobolWord</c> (<c>dataName</c>, <c>programName</c>, the SELECT
/// file-name), and ISO §8.3.2.1 3) lets a §8.10 context-sensitive word be one outside its construct. Many such words
/// carry a lexer token (CAPACITY, CYCLE, BYTES, …) that <c>cobolWord</c> admits and <c>IDENTIFIER</c> does not, so a
/// REFERENCE slot spelled <c>IDENTIFIER</c> refuses a name its declaration accepted: <c>01 CAPACITY TYPEDEF …</c>
/// then <c>TYPE CAPACITY</c>, and <c>FUNCTION-ID. CAPACITY.</c> then <c>FUNCTION CAPACITY(V)</c>, were COBOL0001.</para>
/// <para>WHAT THIS PINS, read from the parser grammar files themselves: every <c>IDENTIFIER</c> a parser rule spells
/// is either led by a semantic predicate (a keyword recognized by its text, the <c>{wordAhead("…")}?</c> family) or
/// stands in one of the rules <see cref="ArguedSites"/> names, each with the reason it is no user-defined-word slot.
/// A new rule that spells a name as <c>IDENTIFIER</c> fails here, and an argued rule that stops spelling it fails
/// too, so the list cannot rot into an exemption nobody needs.</para>
/// </summary>
public sealed class UserWordSlotDriftTests
{
    /// <summary>The rules that may spell a bare IDENTIFIER, and why each is no user-defined-word slot.</summary>
    private static readonly Dictionary<string, string> ArguedSites = new(StringComparer.Ordinal)
    {
        ["cobolWord"] = "the name rule itself — IDENTIFIER is its base arm (CobolWords.g4, generated)",
        ["formatWord"] = "a general-format KEYWORD with no lexer token, matched by text (kb/Work PB764)",
        ["enterOperand"] = "ENTER language-name-1 / routine-name-1: a system-name and an external routine, not a "
            + "word in the program's name space (the X3.23-1985 ENTER statement, removed by COBOL-2002)",
        ["genericClause"] = "the unrecognizedClause error production's word run, refused by name (kb/Work PB829)",
        ["decimalPointClause"] = "DECIMAL-POINT IS COMMA: the keyword COMMA has no lexer token",
        ["screenLineClause"] = "the keyword MINUS has no lexer token (the declined screen module, kb/Work PB260)",
        ["screenColumnClause"] = "the keyword MINUS has no lexer token (the declined screen module, kb/Work PB260)",
    };

    private static readonly Regex Comment = new(@"//[^\n]*|/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <summary>A rule head at the start of a line: a lower-case rule name, its optional <c>[args]</c>,
    /// <c>returns [..]</c> and <c>locals [..]</c>, then ':' and the body to the rule's ';'. Actions are removed first, so
    /// the only ';' left in the text end a rule or a grammar/import statement (which has no ':' and so never matches).</summary>
    private static readonly Regex Rule = new(
        @"^([a-z][A-Za-z0-9_]*)\s*(?:\[[^\]]*\]\s*)?(?:returns\s*\[[^\]]*\]\s*)?(?:locals\s*\[[^\]]*\]\s*)?:(.*?);",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <summary>Every (rule, IDENTIFIER occurrence) of <paramref name="grammar"/> that is not led by a predicate.</summary>
    internal static IEnumerable<string> BareIdentifierRules(string grammar)
    {
        string text = Comment.Replace(grammar, "");
        text = StripActions(text);
        foreach (Match m in Rule.Matches(text))
        {
            var tokens = Regex.Split(m.Groups[2].Value, @"[\s()|]+").Where(t => t.Length > 0).ToList();
            for (int i = 0; i < tokens.Count; i++)
                if (tokens[i] is "IDENTIFIER" or "IDENTIFIER?" or "IDENTIFIER*" or "IDENTIFIER+"
                    && (i == 0 || tokens[i - 1] != "PRED"))
                    yield return m.Groups[1].Value;
        }
    }

    /// <summary>Replaces each brace block (an action, a predicate, an <c>options</c> body) with <c>PRED</c> when a '?'
    /// follows it (a semantic predicate) and with nothing otherwise. Braces nest (a predicate may call a method whose
    /// argument is a brace-free string), so the scan counts depth.</summary>
    private static string StripActions(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '{') { sb.Append(text[i]); continue; }
            int depth = 0, j = i;
            for (; j < text.Length; j++)
            {
                if (text[j] == '{') depth++;
                else if (text[j] == '}' && --depth == 0) break;
            }
            bool predicate = j + 1 < text.Length && text[j + 1] == '?';
            sb.Append(predicate ? " PRED " : " ");
            i = predicate ? j + 1 : j;
        }
        return sb.ToString();
    }

    private static IEnumerable<string> ParserGrammars() =>
        Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).Contains("Lexer", StringComparison.Ordinal));

    /// <summary>⛔ THE OBLIGATION. No parser rule outside <see cref="ArguedSites"/> spells a bare IDENTIFIER, and
    /// every argued rule still does (a stale entry is an exemption nobody needs).</summary>
    [Fact]
    public void NoNameSlot_IsABareIdentifier()
    {
        var files = ParserGrammars().ToList();
        Assert.True(files.Count >= 10, $"only {files.Count} parser grammar file(s) found — the scan is blind");
        int rules = files.Sum(f => Rule.Matches(StripActions(Comment.Replace(File.ReadAllText(f), ""))).Count);
        Assert.True(rules > 500, $"only {rules} parser rule(s) read — the rule scan broke");
        var found = files.SelectMany(f => BareIdentifierRules(File.ReadAllText(f))).ToHashSet(StringComparer.Ordinal);
        var unargued = found.Where(r => !ArguedSites.ContainsKey(r)).OrderBy(r => r, StringComparer.Ordinal).ToList();
        Assert.True(unargued.Count == 0,
            $"rule(s) [{string.Join(", ", unargued)}] spell a bare IDENTIFIER. A user-defined-word slot is `cobolWord` — the "
            + "word set its declaration admits, §8.10 context-sensitive words with tokens included (ISO §8.3.2.1 3), kb/Work "
            + "PB2721); a keyword recognized by text is `{wordAhead(\"…\")}? IDENTIFIER` or `formatWord`.");
        var stale = ArguedSites.Keys.Where(r => !found.Contains(r)).OrderBy(r => r, StringComparer.Ordinal).ToList();
        Assert.True(stale.Count == 0, $"argued rule(s) [{string.Join(", ", stale)}] no longer spell a bare IDENTIFIER — "
            + "drop the entry");
    }

    /// <summary>The three slots kb/Work PB2721 and PB2732 moved stay on <c>cobolWord</c>: type-name-1, the word after
    /// FUNCTION, and each INDEXED BY index-name (a DEFINING word, never a reference).</summary>
    [Fact]
    public void TheMovedSlots_ReadCobolWord()
    {
        string all = string.Join("\n", ParserGrammars().Select(f => StripActions(Comment.Replace(File.ReadAllText(f), ""))));
        string Body(string rule) => Rule.Matches(all).Single(m => m.Groups[1].Value == rule).Groups[2].Value;
        Assert.Matches(@"TYPE\s+TO\?\s+cobolWord", Body("typeClause"));
        Assert.Matches(@"^\s*cobolWord\s*\|\s*intrinsicKeywordName\s*$", Body("functionName"));
        Assert.Matches(@"^\s*cobolWord\s+\(COMMA\?\s+cobolWord\)\*\s*$", Body("indexNameList"));
        Assert.DoesNotContain("dataReferenceList", Body("occursClause"), StringComparison.Ordinal);
    }

    /// <summary>Prove the guard can FAIL (feedback_green_gates_arent_evidence): a bare IDENTIFIER in a rule is
    /// reported, a predicate-led one is not, and a comment or action never counts.</summary>
    [Fact]
    public void TheGuard_ReportsABareIdentifier_AndPassesAPredicateLedOne()
    {
        const string grammar = "parser grammar Probe;\noptions { tokenVocab = CobolLexer; }\n"
            + "nameRef\n    : TYPE IDENTIFIER   // IDENTIFIER in a comment\n    ;\n"
            + "keywordByText\n    : {wordAhead(\"SHORT\")}? IDENTIFIER { declareName(TokenStream.LT(-1)); }\n    ;\n"
            + "oneLine : IDENTIFIER+ ;\n";
        Assert.Equal(["nameRef", "oneLine"], BareIdentifierRules(grammar).ToArray());
    }
}
