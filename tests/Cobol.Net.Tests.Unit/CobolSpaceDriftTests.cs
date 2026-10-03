// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SPACE IN SOURCE TEXT IS <see cref="CobolSpace"/>'S, AND THE TEXT STAGES NEVER ASK .NET WHAT WHITE SPACE IS
/// (kb/Work PB1543, PB1660). ISO §8.3.5 1): "The COBOL character space is a separator" — the only one — and §6.3.6: "A
/// blank line is one that contains only space characters between margin C and margin R". .NET's Unicode
/// <c>White_Space</c> set (<c>char.IsWhiteSpace</c>, a parameterless <c>Trim</c> / <c>TrimStart</c> / <c>TrimEnd</c>,
/// <c>Split()</c>, <c>string.IsNullOrWhiteSpace</c>) adds U+00A0, U+2000–U+200A, U+3000 and more, so a stage that uses it
/// reads two directive operands where the programmer wrote one and discards a line that holds program text. PB1543 put
/// the one predicate at the COPY / REPLACE sites and left the directive-operand and blank-line stages on the other
/// set; this is the guard that stops the next stage from growing a second separator set. Source-form guards, because no
/// behavioral test can see a stage that happens to be fed only ASCII.
/// </summary>
public sealed class CobolSpaceDriftTests
{
    /// <summary>Every stage that reads source text as separated words or as lines: all of the preprocessor but the CCVS
    /// placeholder rewriter (<c>NistPreprocessor</c>, a dialect text substitution over the fixed CCVS archive), the
    /// directive-line parse and operand catalog every directive stage shares, and the pipeline driver that classifies
    /// lines when it locates an END-PERFORM.</summary>
    private static IEnumerable<string> StageFiles()
    {
        foreach (string f in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Preprocessor"), "*.cs"))
            if (Path.GetFileName(f) != "NistPreprocessor.cs") yield return f;
        yield return TestRepo.Src("Cobol.Net.Frontend", "Pipeline", "Frontend.cs");
        yield return TestRepo.Src("Cobol.Net.Editions", "CompilerDirectiveLine.cs");
        yield return TestRepo.Src("Cobol.Net.Editions", "CompilerDirectiveCatalog.cs");
    }

    private static IEnumerable<(string File, int Line, string Text)> CodeLines(string file)
    {
        string[] lines = File.ReadAllLines(file);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;   // a comment, /// included
            yield return (Path.GetRelativePath(TestRepo.Root, file), i + 1, lines[i]);
        }
    }

    /// <summary>The spellings of .NET's White_Space set: the predicate, the parameterless trims, the parameterless split
    /// and the null-separator split, and the blank test.</summary>
    private static readonly Regex UnicodeWhiteSpace = new(
        @"char\.IsWhiteSpace|IsNullOrWhiteSpace|\.Trim(Start|End)?\(\)|\.Split\(\)|\.Split\(\(char\[\]\?\)null|\.Split\(null",
        RegexOptions.Compiled);

    [Fact]
    public void NoTextStage_AsksUnicodeWhiteSpace()
    {
        var offenders = StageFiles().SelectMany(CodeLines).Where(l => UnicodeWhiteSpace.IsMatch(l.Text))
            .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}").ToList();
        Assert.True(offenders.Count == 0,
            "A text stage splits, trims or classifies on .NET's Unicode White_Space set. ISO §8.3.5 1) makes only the COBOL "
            + "character space a separator and §6.3.6 a blank line only spaces, so U+00A0 and its kin are characters of the "
            + "word they stand in. Ask CobolSpace (IsSeparator, IsBlank, TrimSpaces / TrimSpacesStart / TrimSpacesEnd, "
            + "SplitSpaces) instead — kb/Work PB1660. Offending sites:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>The separator set spelled as a character list or a pattern — the two ways a second copy of
    /// <see cref="CobolSpace.IsSeparator"/> is written.</summary>
    private static readonly Regex SeparatorSetSpelling = new(@"' ' or '\\n'|'\\n' or ' '|' ', '\\n'|'\\n', ' '", RegexOptions.Compiled);

    [Fact]
    public void TheSeparatorSet_IsSpelledInExactlyOnePlace()
    {
        var spellings = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Src(), file);
            if (rel.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || rel.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || rel.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")
                || Path.GetFileName(file) == "CobolSpace.cs") continue;
            spellings.AddRange(CodeLines(file).Where(l => SeparatorSetSpelling.IsMatch(l.Text))
                .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}"));
        }
        Assert.True(spellings.Count == 0,
            "The separator-space set (the space and the line end, the lexer's WS rule) is spelled outside CobolSpace. One "
            + "rule, one place: ask CobolSpace.IsSeparator / CobolSpace.Separators. Offending sites:\n  "
            + string.Join("\n  ", spellings));
    }

    [Fact] // The set itself is the lexer's WS rule: the space and the line feed, and nothing Unicode calls white space.
    public void CobolSpace_IsTheSpaceAndTheLineEnd_AndNotUnicodeWhiteSpace()
    {
        char nbsp = (char)0x00A0, emSpace = (char)0x2003, ideographicSpace = (char)0x3000, nextLine = (char)0x0085;
        Assert.True(CobolSpace.IsSeparator(' ') && CobolSpace.IsSeparator((char)10));
        foreach (char c in new[] { nbsp, emSpace, ideographicSpace, nextLine, (char)9, (char)13, (char)11, (char)12 })
            Assert.False(CobolSpace.IsSeparator(c), $"U+{(int)c:X4} is not the COBOL character space");
        Assert.True(CobolSpace.IsBlank("  " + (char)10 + " "));
        Assert.False(CobolSpace.IsBlank(" " + nbsp + " "));
        Assert.Equal("A" + nbsp + "B", ("  A" + nbsp + "B ").TrimSpaces());
        Assert.Equal(["A" + nbsp + "B", "C"], ("  A" + nbsp + "B   C" + (char)10).SplitSpaces());
    }
}
