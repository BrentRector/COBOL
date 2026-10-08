// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE LINE-ENTRY STAGE IS THE ONLY PLACE A TEXT BECOMES LINES (kb/Work PB1800). Before it, six places split or
/// indexed the raw text on their own, so a TAB (DOC-A.1-157), the terminator set (DOC-A.1-156), the 255-position
/// limit (§6.1 3) a)) and the CCVS archive markers (PB1803) each had to be written into every splitter — and were
/// written into none. <c>PhysicalLines</c> reads lines; everything after it sees no tab and no CR LF, so a later
/// stage that spells <c>'\t'</c> or <c>'\r'</c> is dead code that looks like a rule, and a raw-text consumer that
/// splits on its own is the next place a determination is forgotten. Source-form guards, because no behavioral test
/// can see either: the code compiles and every golden passes.
/// <para><b>A column is a character position, never a string index (kb/Work PB1966).</b> A supplementary-plane character
/// is two UTF-16 units and one position (DOC-A.1-157), so <c>line[6]</c>, <c>line[7..72]</c> or <c>line.Length &gt; 72</c>
/// puts margin R, the indicator area and the free-form 255 limit early on a line that holds one. Every column read goes
/// through <c>FixedFormLine</c> and <c>CharacterPositions</c>.</para>
/// </summary>
public sealed class PhysicalLinesDriftTests
{
    private static readonly Regex TabOrCr = new(@"\\[rt]", RegexOptions.Compiled);

    private static IEnumerable<string> PipelineFiles()
    {
        foreach (string f in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Preprocessor"), "*.cs"))
            if (Path.GetFileName(f) != "PhysicalLines.cs") yield return f;
        yield return TestRepo.Src("Cobol.Net.Frontend", "Parsing", "SeparatorRule.cs");
        yield return TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolLexer.g4");
    }

    private static IEnumerable<(string File, int Line, string Text)> CodeLines(string file)
    {
        string[] lines = File.ReadAllLines(file);
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;   // a comment, /// included
            yield return (Path.GetRelativePath(TestRepo.Root, file), i + 1, lines[i]);
        }
    }

    [Fact] // No stage after the line-entry stage has a tab or a CR rule: none survives it.
    public void NoPipelineStage_SpellsATabOrACarriageReturn()
    {
        var offenders = PipelineFiles().SelectMany(CodeLines).Where(l => TabOrCr.IsMatch(l.Text))
            .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}").ToList();
        Assert.True(offenders.Count == 0,
            "A stage after PhysicalLines spells a tab or a carriage return. The line-entry stage expands every tab "
            + "(DOC-A.1-157) and takes CR LF to LF (DOC-A.1-156), so no later stage sees either — a rule for one is dead "
            + "code that reads like a determination. Put the determination in PhysicalLines. Offending sites:\n  "
            + string.Join("\n  ", offenders));
    }

    [Fact] // The raw text is split in ONE place; the reference-format walkers read PhysicalLine values.
    public void TheReferenceFormatWalkers_NeverSplitRawText()
    {
        var offenders = Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Preprocessor"), "ReferenceFormatProcessor*.cs")
            .SelectMany(CodeLines).Where(l => l.Text.Contains(".Split(", StringComparison.Ordinal)
                                              || l.Text.Contains(".Lines(", StringComparison.Ordinal)
                                              || l.Text.Contains("ReadAllLines", StringComparison.Ordinal))
            .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}").ToList();
        Assert.True(offenders.Count == 0,
            "A reference-format walker splits text itself. Lines come from PhysicalLines.Read, once, at the entry — "
            + "that is what keeps the terminator set, the tab stops, the 255-position check and the archive-marker strip "
            + "single-sourced (kb/Work PB1800). Offending sites:\n  " + string.Join("\n  ", offenders));
    }

    [Fact] // One reader: the production code calls PhysicalLines.Read at exactly one site.
    public void PhysicalLinesRead_HasOneProductionCaller()
    {
        var callers = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Src(), file);
            if (rel.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || rel.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || Path.GetFileName(file) == "PhysicalLines.cs") continue;
            callers.AddRange(CodeLines(file).Where(l => l.Text.Contains("PhysicalLines.Read(", StringComparison.Ordinal))
                .Select(l => $"{l.File}:{l.Line}"));
        }
        Assert.True(callers.Count == 1 && callers[0].Contains("ReferenceFormatProcessor.cs", StringComparison.Ordinal),
            "PhysicalLines.Read has one production caller, the reference-format walker's entry "
            + "(NormalizeToFreeFormMapped). Callers found:\n  " + string.Join("\n  ", callers));
    }

    private static readonly Regex ColumnAsIndex = new(
        @"\[\s*(FixedFormLine\.)?(IndicatorColumn|SourceAreaStart|SourceAreaWidth|MarginR|SequenceAreaLength)\b"
        + @"|\.Length\s*[<>]=?\s*(FixedFormLine\.)?(IndicatorColumn|SourceAreaStart|MarginR|FreeFormMaxPositions|72|255)\b"
        + @"|AsSpan\(\s*(FixedFormLine\.)?(MarginR|SourceAreaStart)\b|\[(6|7)\]\s+(is|==|!=)",
        RegexOptions.Compiled);

    [Fact] // A column is a CHARACTER POSITION: no stage indexes a line, or compares its length, by a column number.
    public void NoPipelineStage_ReadsAColumnAsAStringIndex()
    {
        var files = PipelineFiles().Append(TestRepo.Src("Cobol.Net.Frontend", "Pipeline", "Frontend.cs"))
            .Where(f => Path.GetFileName(f) is not ("FixedFormLine.cs" or "CharacterPositions.cs"));
        var offenders = files.SelectMany(CodeLines).Where(l => ColumnAsIndex.IsMatch(l.Text))
            .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}").ToList();
        Assert.True(offenders.Count == 0,
            "A stage reads a fixed-form column, or the free-form 255-position limit, as a UTF-16 index. A position is one "
            + "CHARACTER (DOC-A.1-157, kb/Work PB1966): a supplementary-plane letter is two units and one position, so the "
            + "read lands early on any line holding one. Read the columns through FixedFormLine (sequence area, indicator, "
            + "program-text area, margin R) and count positions with CharacterPositions. Offending sites:\n  "
            + string.Join("\n  ", offenders));
    }
}
