// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
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
/// positions"): a horizontal tab U+0009 advances to the next tab stop — positions 1, 9, 17, 25, …, a position being one
/// CHARACTER and not one UTF-16 unit (<see cref="CharacterPositions"/>, kb/Work PB1966) — and is replaced by
/// the one to eight spaces that fill the gap, EVERYWHERE, an alphanumeric literal included (owner decision, kb/Work
/// R55: the tab is an input-medium positioning control, not a character of the literal; <c>X"09"</c> is how a
/// program puts a TAB in data). No tab survives this stage, so no later stage has a tab rule. A line that held a tab
/// or a supplementary-plane character keeps a <see cref="ColumnMap"/> from each expanded index to the character
/// position the file holds it at, which is the ONE column unit every diagnostic is named in (kb/Work PB2983).</item>
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
        if (raw.IndexOf('\t') < 0 && !CharacterPositions.HasSurrogate(raw))
            return new PhysicalLine(number, IsArchiveMarker(raw, ccvsIndicators) ? "" : raw.ToString(), Columns: null);

        var text = new StringBuilder(raw.Length + TabWidth);
        var toPhysical = new List<int>(raw.Length + TabWidth + 1);
        int position = 0;   // the 0-based character position the next EXPANDED character lands on (not text.Length)
        int physical = 0;   // the 0-based character position of the character being read, in the line as the file holds it
        for (int i = 0; i < raw.Length; physical++)
        {
            int units = CharacterPositions.UnitsAt(raw, i);
            if (raw[i] != '\t')
            {
                // A supplementary-plane character is two UTF-16 units but ONE position; both units map to it.
                text.Append(raw.Slice(i, units));
                for (int u = 0; u < units; u++) toPhysical.Add(physical);
                position++;
                i += units;
                continue;
            }
            // The next tab stop strictly after the tab's own position: positions 1, 9, 17, … (0-based 0, 8, 16, …).
            // Every space the tab fills maps to the tab, which is ONE position of the file.
            text.Append(' ');
            toPhysical.Add(physical);
            position++;
            while (position % TabWidth != 0) { text.Append(' '); toPhysical.Add(physical); position++; }
            i++;
        }
        toPhysical.Add(physical);
        string expanded = text.ToString();
        return IsArchiveMarker(expanded, ccvsIndicators)
            ? new PhysicalLine(number, "", Columns: null)
            : new PhysicalLine(number, expanded, new ColumnMap([.. toPhysical]));
    }

    private static bool IsArchiveMarker(ReadOnlySpan<char> line, bool ccvsIndicators)
    {
        if (!ccvsIndicators) return false;
        var trimmed = line.TrimSpacesStart();
        return trimmed.StartsWith("*HEADER,", StringComparison.Ordinal)
               || trimmed.StartsWith("*END-OF,", StringComparison.Ordinal);
    }
}

/// <summary>One line of source or library text as the line-entry stage defines it (<see cref="PhysicalLines"/>).</summary>
/// <param name="Number">The 1-based number of the line in its file.</param>
/// <param name="Text">The line without its terminator, every tab expanded to spaces.</param>
/// <param name="Columns">The map from an index of <paramref name="Text"/> to the character position the file holds it at,
/// present only when the line held a tab or a supplementary-plane character — on any other line the index IS the
/// position.</param>
public readonly record struct PhysicalLine(int Number, string Text, ColumnMap? Columns);

/// <summary>The lines of one file, in order — what EVERY reference-format consumer reads instead of the raw text.</summary>
public sealed class PhysicalLineSet(PhysicalLine[] lines)
{
    public PhysicalLine this[int index] => lines[index];

    public ReadOnlySpan<PhysicalLine> Lines => lines;
}

/// <summary>The correspondence between the indexes of an expanded line and the CHARACTER POSITIONS of the physical line it
/// was read from — what a diagnostic needs to name the column the programmer sees in their editor (kb/Work PB1801,
/// PB2983). The spaces a tab expanded to all map to the tab's own position (a tab counts one column, as in every
/// editor), and the two UTF-16 units of a supplementary-plane character map to its one position
/// (<see cref="CharacterPositions"/>), so a column never depends on how many code units the text is encoded in.</summary>
public sealed class ColumnMap
{
    private readonly int[] _toPhysical;

    internal ColumnMap(int[] toPhysical) => _toPhysical = toPhysical;

    /// <summary>The 0-based character position, in the physical line, of the character that produced the 0-based expanded
    /// index <paramref name="expanded"/> (the expanded length maps to the physical length in positions, and an index past
    /// it continues the line one for one — the program-text area of a short fixed-form line is read as space-filled to
    /// margin R).</summary>
    public int ToPhysical(int expanded)
    {
        int end = _toPhysical.Length - 1;   // the expanded length
        return expanded <= end ? _toPhysical[expanded] : _toPhysical[end] + (expanded - end);
    }
}
