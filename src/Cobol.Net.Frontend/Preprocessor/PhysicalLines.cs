// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// THE LINE-ENTRY STAGE of the reference-format pipeline (kb/Work PB1800): the one place where decoded source or
/// library text becomes LINES, so that the four determinations ISO §6.1 leaves to the implementor are each written
/// exactly once and every later stage (the Auto format detector, the directive scan, the fixed-form and free-form
/// converters) reads the same lines.
/// <list type="number">
/// <item><b>Terminators</b> (docs/CONFORMANCE.md DOC-A.1-156; §6.1 3) b) "The implementor shall specify any control
/// characters that terminate a free-form line"): a LINE FEED ends a line, and a CARRIAGE RETURN immediately before it
/// is part of that terminator. A lone CR — one not followed by a LINE FEED — is an ordinary character of the line (the
/// GnuCOBOL reader's rule, <c>ppinput</c>; rule-1 precedence). The end of the text ends its last line, so a text with
/// <c>n</c> LINE FEEDs has <c>n</c> + 1 lines.</item>
/// <item><b>Tabs</b> (DOC-A.1-157; §6.1 1) c) "The implementor shall specify the meaning of lines and character
/// positions"): a horizontal tab U+0009 advances to the next tab stop — positions 1, 9, 17, 25, … — and is replaced by
/// the one to eight spaces that fill the gap, EVERYWHERE, an alphanumeric literal included (owner decision, kb/Work
/// R55: the tab is an input-medium positioning control, not a character of the literal; <c>X"09"</c> is how a
/// program puts a TAB in data). No tab survives this stage, so no later stage has a tab rule.</item>
/// <item><b>Archive markers</b> (kb/Work PB1803): the NIST CCVS archive-control lines <c>*HEADER,…</c> and
/// <c>*END-OF,…</c> that delimit members of <c>newcob.val</c> are blanked — and ONLY when the CCVS dialect is on
/// (<c>--nist</c>), because they are never valid COBOL and in any other compilation a column-7 <c>R</c> or a free-form
/// word must be diagnosed, not discarded. A marker becomes an EMPTY line, so the line numbering is untouched.</item>
/// </list>
/// Nothing outside this type splits raw text on line ends, trims a CR, or looks for a tab
/// (<c>PhysicalLinesDriftTests</c>). The free-form 255-position limit (§6.1 3) a)) is asked where a line is read as
/// free form, on the expanded <see cref="PhysicalLine.Text"/> (an expanded tab counts the positions it fills), because a
/// fixed-form line may run past margin R and only its program-text area counts.
/// </summary>
public static class PhysicalLines
{
    /// <summary>The tab stops are every 8 positions — DOC-A.1-157 (GnuCOBOL's default <c>tab-width</c>; rule 1).</summary>
    public const int TabWidth = 8;

    /// <summary>Read <paramref name="decodedText"/> as lines. <paramref name="ccvsIndicators"/> is the NIST CCVS
    /// dialect (<c>--nist</c>): it alone turns the archive-marker strip on.</summary>
    public static PhysicalLineSet Read(string decodedText, bool ccvsIndicators = false)
    {
        var lines = new List<PhysicalLine>();
        int start = 0;
        while (true)
        {
            int lf = decodedText.IndexOf('\n', start);
            bool last = lf < 0;
            int end = last ? decodedText.Length : lf;
            // A CR before the LINE FEED is part of the terminator; a CR at the very end of the text has no LINE FEED
            // after it and is a character of the line.
            if (!last && end > start && decodedText[end - 1] == '\r') end--;
            lines.Add(Line(lines.Count + 1, decodedText.AsSpan(start, end - start), ccvsIndicators));
            if (last) break;
            start = lf + 1;
        }
        return new PhysicalLineSet([.. lines]);
    }

    private static PhysicalLine Line(int number, ReadOnlySpan<char> raw, bool ccvsIndicators)
    {
        if (raw.IndexOf('\t') < 0)
            return new PhysicalLine(number, IsArchiveMarker(raw, ccvsIndicators) ? "" : raw.ToString(), Tabs: null);

        var text = new StringBuilder(raw.Length + TabWidth);
        var toExpanded = new int[raw.Length + 1];
        for (int i = 0; i < raw.Length; i++)
        {
            toExpanded[i] = text.Length;
            if (raw[i] != '\t') { text.Append(raw[i]); continue; }
            // The next tab stop strictly after the tab's own position: positions 1, 9, 17, … (0-based 0, 8, 16, …).
            text.Append(' ');
            while (text.Length % TabWidth != 0) text.Append(' ');
        }
        toExpanded[raw.Length] = text.Length;
        var toPhysical = new int[text.Length + 1];
        for (int i = 0; i < raw.Length; i++)
            for (int e = toExpanded[i]; e < toExpanded[i + 1]; e++) toPhysical[e] = i;
        toPhysical[text.Length] = raw.Length;
        string expanded = text.ToString();
        return IsArchiveMarker(expanded, ccvsIndicators)
            ? new PhysicalLine(number, "", Tabs: null)
            : new PhysicalLine(number, expanded, new TabMap(toExpanded, toPhysical));
    }

    private static bool IsArchiveMarker(ReadOnlySpan<char> line, bool ccvsIndicators)
    {
        if (!ccvsIndicators) return false;
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("*HEADER,", StringComparison.Ordinal)
               || trimmed.StartsWith("*END-OF,", StringComparison.Ordinal);
    }
}

/// <summary>One line of source or library text as the line-entry stage defines it (<see cref="PhysicalLines"/>).</summary>
/// <param name="Number">The 1-based number of the line in its file.</param>
/// <param name="Text">The line without its terminator, every tab expanded to spaces.</param>
/// <param name="Tabs">The map between expanded and physical positions, present only when the line held a tab.</param>
public readonly record struct PhysicalLine(int Number, string Text, TabMap? Tabs);

/// <summary>The lines of one file, in order — what EVERY reference-format consumer reads instead of the raw text.</summary>
public sealed class PhysicalLineSet(PhysicalLine[] lines)
{
    public int Count => lines.Length;

    public PhysicalLine this[int index] => lines[index];

    public ReadOnlySpan<PhysicalLine> Lines => lines;

    /// <summary>The lines from <paramref name="start"/> up to, not including, <paramref name="end"/> (0-based).</summary>
    public ReadOnlySpan<PhysicalLine> Range(int start, int end) => lines.AsSpan(start, end - start);
}

/// <summary>The correspondence between the positions of an expanded line and the characters of the physical line it was
/// read from — what a diagnostic needs to name the column the programmer sees in their editor (kb/Work PB1801). The
/// spaces a tab expanded to all map to the tab's own index.</summary>
public sealed class TabMap
{
    private readonly int[] _toExpanded;
    private readonly int[] _toPhysical;

    internal TabMap(int[] toExpanded, int[] toPhysical)
    {
        _toExpanded = toExpanded;
        _toPhysical = toPhysical;
    }

    /// <summary>The 0-based expanded position where the character at 0-based physical index <paramref name="physical"/>
    /// begins (the physical length maps to the expanded length).</summary>
    public int ToExpanded(int physical) => _toExpanded[physical];

    /// <summary>The 0-based physical index of the character that produced the 0-based expanded position
    /// <paramref name="expanded"/> (the expanded length maps to the physical length).</summary>
    public int ToPhysical(int expanded) => _toPhysical[expanded];
}
