// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE BOOLEAN-OPERATOR TOKENS ARE ONE LIST, <c>BooleanOperatorTokens</c>, AND EVERY READER ASKS IT (kb/Work PB1412).
/// <para>The list was written four times — the parser's two look-ahead predicates, the condition binder's
/// discriminator and the introduction gate — and the binder's copy named the four COBOL-2002 tokens and omitted the
/// four COBOL-2023 shift tokens the parser's copies had learned when shifts landed, so <c>IF A B-SHIFT-L 1 =
/// B"1000"</c> parsed as a boolean expression and was then taken for a bare operand at bind: two arms of one
/// dispatch, one updated. The extraction is the fix; this test keeps it so.</para>
/// <para>Two halves. <b>Complete</b>: every <c>B_…</c> token the lexer defines is classified, and the classification
/// names no token the lexer does not define, so the next boolean operator word fails here until it is placed in a
/// tier. <b>Singular</b>: no other source file spells an operator token constant, so a fifth private list cannot
/// appear — the one exemption is the §8.9 funnel's user-word list in <c>VersionConformancePass</c>, which answers a
/// DIFFERENT question (which words are reserved at which edition) over the 2002 set only.</para>
/// </summary>
public sealed class BooleanOperatorTokenDriftTests
{
    /// <summary>An upper bound on the token types the lexer defines (a few hundred); the vocabulary answers null
    /// past its last one, so scanning to a generous bound reads every symbolic name without a private table.</summary>
    private const int MaxTokenType = 4096;

    /// <summary>The lexer's symbolic names of the form <c>B_…</c> with their token types — the operator words
    /// (<c>B-AND</c>, <c>B-SHIFT-LC</c>, …) the vocabulary defines.</summary>
    private static List<(string Name, int Type)> LexerOperatorTokens()
    {
        var vocab = CobolLexer.DefaultVocabulary;
        var tokens = new List<(string, int)>();
        for (int t = 1; t <= MaxTokenType; t++)
            if (vocab.GetSymbolicName(t) is { } name && name.StartsWith("B_", StringComparison.Ordinal))
                tokens.Add((name, t));
        return tokens;
    }

    [Fact]
    public void EveryLexerOperatorToken_IsClassified_AndNothingElseIs()
    {
        var lexer = LexerOperatorTokens();
        Assert.True(lexer.Count >= 8, $"the vocabulary scan found only {lexer.Count} B_ tokens — it measured nothing");

        var unclassified = lexer.Where(t => !BooleanOperatorTokens.IsOperator(t.Type)).Select(t => t.Name).ToList();
        Assert.True(unclassified.Count == 0,
            "lexer operator tokens that BooleanOperatorTokens does not classify: " + string.Join(", ", unclassified)
            + " — place each in IsBinary, IsPrefix or IsShift (the three tiers the parser predicates, the binder and "
            + "the introduction gate read).");

        // The converse: the classification claims no token outside the lexer's B_ vocabulary.
        var claimed = Enumerable.Range(1, MaxTokenType)
            .Where(BooleanOperatorTokens.IsOperator).Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t)).ToList();
        Assert.Equal(lexer.Select(t => t.Name).Order(StringComparer.Ordinal),
            claimed.Order(StringComparer.Ordinal));

        // Each token is in exactly one tier: infix (binary or shift) XOR prefix.
        foreach (var (name, type) in lexer)
            Assert.True(BooleanOperatorTokens.IsInfix(type) ^ BooleanOperatorTokens.IsPrefix(type),
                $"{name} must be exactly one of infix and prefix");
    }

    /// <summary>The four SHIFT tokens are the 2023 tier and the other four the 2002 tier — the split the gate's two
    /// construct rows (BooleanShiftOperators2023, BooleanOperators2002) are keyed on.</summary>
    [Fact]
    public void TheEditionTiers_AreTheShiftTokensAndTheRest()
    {
        var lexer = LexerOperatorTokens();
        var shifts = lexer.Where(t => t.Name.StartsWith("B_SHIFT_", StringComparison.Ordinal)).ToList();
        Assert.Equal(4, shifts.Count);
        foreach (var (name, type) in lexer)
        {
            bool isShift = name.StartsWith("B_SHIFT_", StringComparison.Ordinal);
            Assert.Equal(isShift, BooleanOperatorTokens.IsShift(type));
            Assert.Equal(!isShift, BooleanOperatorTokens.IsBinaryOrNot(type));
        }
    }

    /// <summary>No source file but the one list, the generated parser and the lexer word set spells an operator
    /// token constant (<c>CobolLexer.B_AND</c>, <c>Core.B_SHIFT_L</c>, …). The accessor methods the generated
    /// contexts carry (<c>suf.B_SHIFT_LC()</c>) are calls, not constants, and are not a list.</summary>
    [Fact]
    public void NoOtherSourceFile_SpellsAnOperatorTokenConstant()
    {
        var constant = new Regex(@"\b(?:CobolLexer|CobolParserCore|Core)\.B_[A-Z_]+\b(?!\s*\()", RegexOptions.Compiled);
        var exempt = new HashSet<string>(StringComparer.Ordinal)
        {
            "BooleanOperatorTokens.cs",   // THE list
            "CobolLexerWordSet.g.cs",     // generated from cobol-words.json
            // The §8.9 funnel: which cobolWord TOKEN TYPES are checked position-blind for a reserved-word use. B-AND,
            // B-OR, B-XOR and B-NOT are user words at 85 and funnel-0901'd from 2002, so that list holds the 2002 set
            // — a different question from "is this token a boolean operator", and the shifts are never user words.
            "VersionConformancePass.cs",
        };
        var scanned = 0;
        var findings = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Src(), file);
            if (rel.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                || rel.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                || rel.Contains(Path.DirectorySeparatorChar + "Generated" + Path.DirectorySeparatorChar)
                || exempt.Contains(Path.GetFileName(file)))
                continue;
            scanned++;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = Regex.Replace(lines[i], @"//.*$", "");
                if (constant.IsMatch(code)) findings.Add($"{rel}:{i + 1}: {lines[i].Trim()}");
            }
        }
        Assert.True(scanned > 100, $"the scan covered only {scanned} source files — it measured nothing");
        Assert.True(findings.Count == 0,
            "a boolean-operator token constant is spelled outside BooleanOperatorTokens (a second list is how the "
            + "shift tokens were dropped from the binder's, kb/Work PB1412):\n  " + string.Join("\n  ", findings)
            + "\nAsk BooleanOperatorTokens.IsInfix / IsPrefix / IsShift / IsOperator instead.");
    }

    /// <summary>The scan's failure branch, fired once: the pre-PB1412 binder discriminator must be reported.</summary>
    [Fact]
    public void TheScan_ActuallyFails_OnThePb1412Discriminator()
    {
        const string pattern = @"\b(?:CobolLexer|CobolParserCore|Core)\.B_[A-Z_]+\b(?!\s*\()";
        Assert.Matches(pattern, "return term.Symbol.Type is Core.B_AND or Core.B_OR or Core.B_XOR or Core.B_NOT;");
        Assert.DoesNotMatch(pattern, "bool circular = suf.B_SHIFT_LC() is not null || suf.B_SHIFT_RC() is not null;");
    }
}
