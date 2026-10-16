// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ WHERE A LITERAL BEGINS AND ENDS, AND WHAT A COMMENT INDICATOR IS, ARE <see cref="CobolSourceText"/>'S, AND NO TEXT
/// STAGE SPELLS THEM AGAIN (kb/Work PB2621). ISO §8.3.5 5) gives the opening delimiters (a quotation symbol, and the
/// prefixed <c>B" N" X" BX" NX"</c>) and the closing one, §8.3.3.2.3 3) makes a doubled quotation symbol content, and
/// §6.2.3.1 makes <c>*&gt;</c> the floating comment indicator. Those facts were written five times in three types — three
/// of which each called itself THE ONE — and the copies had begun to differ. Source-form guards over every stage that
/// reads program text before the lexer: a stage that spells the comment indicator, the quotation-symbol pair, or a
/// doubled-quotation test is growing a second literal scanner, so it asks <see cref="CobolSourceText"/> instead.
/// </summary>
public sealed class LiteralScanDriftTests
{
    /// <summary>The stages that read program text before the lexer: all of the preprocessor (the §6.5 conversion, text
    /// manipulation, the directive stages), the directive-line parse they share, and the pipeline driver that reads
    /// physical lines when it locates an END-PERFORM.</summary>
    private static IEnumerable<string> StageFiles()
    {
        foreach (string f in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Preprocessor"), "*.cs"))
            yield return f;
        yield return TestRepo.Src("Cobol.Net.Frontend", "Pipeline", "Frontend.cs");
        yield return TestRepo.Src("Cobol.Net.Editions", "CompilerDirectiveLine.cs");
    }

    private static IEnumerable<(string File, int Line, string Text)> CodeLines(string file)
    {
        string[] lines = File.ReadAllLines(file);
        for (int i = 0; i < lines.Length; i++)
            if (!lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal))   // a comment, /// included
                yield return (Path.GetFileName(file), i + 1, lines[i]);
    }

    /// <summary>The three spellings of a second literal or comment scanner: the comment indicator as a string or as
    /// its two characters, the quotation-symbol pair, and the doubled-quotation test (<c>x[i + 1] == quote</c>).</summary>
    private static readonly Regex SecondScanner = new(
        @"""\*>""|'\*'.*'>'|'""'\s*(or|,)\s*'\\''|'\\''\s*(or|,)\s*'""'|\[[^\]]*\+\s*1\s*\]\s*==\s*\w*[Qq]uote\b",
        RegexOptions.Compiled);

    /// <summary>The stages that still carry their own scan, each to be moved onto <see cref="CobolSourceText"/> by the
    /// follow-up the PB2621 landing reported: this wave's declared file set did not include them. The list only
    /// shrinks — <see cref="EveryPendingStage_StillSpellsItsOwnScan"/> fails once a stage is moved, so its entry
    /// goes with it.</summary>
    private static readonly Dictionary<string, string> PendingStages = new(StringComparer.Ordinal)
    {
        ["CobolWordsDirectiveProcessor.cs"] = "Tokenize / ReadQuoted read the quotation symbols and the doubled one",
        ["ConditionalCompilationProcessor.cs"] = "FindRangeWord skips literals with its own quotation-symbol test",
        ["Frontend.cs"] = "the END-PERFORM locator spells the comment indicator",
    };

    private static List<(string File, int Line, string Text)> Spellings()
        => StageFiles().SelectMany(CodeLines).Where(l => SecondScanner.IsMatch(l.Text)).ToList();

    [Fact]
    public void NoTextStage_SpellsTheLiteralOrCommentReading()
    {
        var offenders = Spellings().Where(l => !PendingStages.ContainsKey(l.File))
            .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}").ToList();
        Assert.True(offenders.Count == 0,
            "A text stage spells the comment indicator, the quotation-symbol pair or a doubled-quotation test. ISO §8.3.5 "
            + "5), §8.3.3.2.3 3) and §6.2.3.1 are one fact about source text: ask CobolSourceText (CommentIndicator, "
            + "CommentIndicatorAt, IsQuotationSymbol, TryOpeningDelimiterAt, ClosingDelimiterAt, LiteralEnd, "
            + "LiteralContent, InlineCommentAt) instead — kb/Work PB2621. Offending sites:\n  "
            + string.Join("\n  ", offenders));
    }

    [Fact]
    public void EveryPendingStage_StillSpellsItsOwnScan()
    {
        var stillPending = Spellings().Select(l => l.File).ToHashSet(StringComparer.Ordinal);
        var moved = PendingStages.Keys.Where(f => !stillPending.Contains(f)).ToList();
        Assert.True(moved.Count == 0,
            "These stages no longer spell their own literal or comment scan — remove them from PendingStages so the "
            + "guard covers them: " + string.Join(", ", moved));
    }

    [Fact] // The guard sees each spelling it names (a pattern that matches nothing guards nothing).
    public void TheGuard_RecognizesEverySpelling()
    {
        foreach (string spelling in new[]
                 {
                     "if (text.StartsWith(\"*>\")) return;",
                     "else if (c == '*' && text[i + 1] == '>') return i;",
                     "else if (c is '\"' or '\\'') quote = c;",
                     "private static readonly char[] Quotes = ['\"', '\\''];",
                     "if (i + 1 < s.Length && s[i + 1] == quote) i++;",
                 })
            Assert.Matches(SecondScanner, spelling);
        Assert.DoesNotMatch(SecondScanner, "if (CobolSourceText.CommentIndicatorAt(text, i)) return i;");
    }
}
