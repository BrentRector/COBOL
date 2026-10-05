// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE ONE LINE-LEVEL ANSWER TO "DOES A COMPILATION UNIT START HERE?" for the text-level directive stage that must
/// know where the first unit begins — the §7.3.10.3 SR1 COBOL-WORDS placement rule ("may be specified only before the
/// first IDENTIFICATION DIVISION within a compilation group"), whose boundary <c>CobolWordsDirectiveProcessor</c> reads
/// and <c>DirectiveSiteProcessor.JudgeFirstUnitPlacement</c> judges against.
/// <para>ISO §11.2.1 prints the division header in BRACKETS — <c>[ IDENTIFICATION DIVISION. ]</c> — so a unit's
/// identification division may begin on its PROGRAM-ID, FUNCTION-ID, CLASS-ID or INTERFACE-ID paragraph with no
/// header line at all (kb/Work PB829). The stages each carried a private copy of this test, and they had
/// drifted: the COBOL-WORDS copy knew only the header, so a directive AFTER a header-less <c>PROGRAM-ID.</c> line
/// was accepted in silence. FACTORY, OBJECT and METHOD-ID are absent on purpose: they open units NESTED in a class
/// definition, which the CLASS-ID line has already started.</para>
/// </summary>
internal static class CompilationUnitStart
{
    /// <summary>True when <paramref name="trimmed"/> (a line with its leading blanks removed) opens a compilation
    /// unit's identification division — the header (or its <c>ID DIVISION</c> abbreviation), or, the header being
    /// optional, the unit's own first paragraph.</summary>
    public static bool IsAt(string trimmed)
        => CobolNames.StartsWith(trimmed, "IDENTIFICATION DIVISION")
        || CobolNames.StartsWith(trimmed, "ID DIVISION")
        || CobolNames.StartsWith(trimmed, "PROGRAM-ID")
        || CobolNames.StartsWith(trimmed, "CLASS-ID")
        || CobolNames.StartsWith(trimmed, "FUNCTION-ID")
        || CobolNames.StartsWith(trimmed, "INTERFACE-ID");

    /// <summary>The 1-based line of the first compilation unit of <paramref name="text"/>, or <see cref="int.MaxValue"/>
    /// when none begins — for a text with NO <c>&gt;&gt;COBOL-WORDS</c> directive left in it, where no synonym can spell a
    /// header and the plain test is exact (a PUSH or POP naming COBOL-WORDS is consumed, and its line blanked, before the
    /// directive stage runs, but its placement is still judged against this boundary).</summary>
    public static int FirstLine(string text)
    {
        int line = 1;
        for (int start = 0; start <= text.Length; line++)
        {
            int end = text.IndexOf('\n', start);
            if (end < 0) end = text.Length;
            if (IsAt(text[start..end].TrimSpacesStart())) return line;
            start = end + 1;
        }
        return int.MaxValue;
    }

    /// <summary>The same question asked of the line AS THE GROUP'S OWN DIRECTIVES READ IT (kb/Work PB1373): after
    /// <c>&gt;&gt;COBOL-WORDS EQUATE "IDENTIFICATION" WITH "IDENT"</c> (or SUBSTITUTE), <c>IDENT DIVISION.</c> IS the
    /// identification division header — §7.3.10.4 GR2 lets the equated word be "used in any syntax requiring the use of"
    /// the reserved word — so the first two words of the line are read through the <paramref name="words"/> synonyms
    /// before the test. Only the leading words can make the line a unit start, and only a SYNONYM can change one (a
    /// de-reserved spelling stays a unit start here: the line is then an error of its own, and the wrong answer costs
    /// that program one more diagnostic, never a legal program its source).</summary>
    public static bool IsAt(string trimmed, CobolWordsMap words)
    {
        if (IsAt(trimmed)) return true;
        if (words.Synonyms.Count == 0) return false;
        var read = new System.Text.StringBuilder(trimmed.Length);
        int i = 0;
        for (int word = 0; word < 2 && i < trimmed.Length; word++)
        {
            int start = i;
            while (i < trimmed.Length && CobolCharacterRepertoire.IsWordCharacter(trimmed[i])) i++;
            string written = trimmed[start..i];
            read.Append(written.Length > 0 && words.Synonyms.TryGetValue(written, out string? canonical) ? canonical : written);
            int gap = i;
            while (i < trimmed.Length && CobolSpace.IsSeparator(trimmed[i])) i++;
            read.Append(trimmed, gap, i - gap);
        }
        read.Append(trimmed, i, trimmed.Length - i);
        return IsAt(read.ToString());
    }
}
